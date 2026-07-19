using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using VedAstro.Data;
using VedAstro.Data.Cache;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace API
{
    // `partial` + `public` so `Microsoft.AspNetCore.Mvc.Testing`'s `WebApplicationFactory<Program>`
    // can be used from a separate test assembly (standard ASP.NET Core minimal-API testing
    // pattern) - lets tests spin up the whole app in-memory and call it over HTTP/TestServer
    // without a real Kestrel socket, and override DI registrations (Postgres connection string,
    // chart cache directory) via `WebApplicationFactory.WithWebHostBuilder(...)`.
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Bridge ASP.NET Core configuration (appsettings.json / appsettings.Development.json)
            // into process environment variables so existing code that reads secrets via
            // Environment.GetEnvironmentVariable(...) (Library/Logic/SecretsEnv.cs,
            // ApiStatistic's "EnableLogging", ThrottleManager's "AnonymousIpCallThreshold", etc)
            // keeps working unchanged - this was implicitly how Azure Functions'
            // local.settings.json "Values" worked (it populated env vars for the process).
            foreach (var kvp in builder.Configuration.AsEnumerable())
            {
                if (kvp.Value != null && Environment.GetEnvironmentVariable(kvp.Key) == null)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
            }

            // Kestrel: bind to the same port Website/Localhost_Setup.md's "Local API" toggle expects
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(7071);
            });

            // ---- DI registration ----

            // NOTE: connection string is resolved lazily from DI's IConfiguration (rather than a
            // `builder.Configuration.GetConnectionString(...)` local captured here at
            // builder-configuration-time) so that test hosts (Microsoft.AspNetCore.Mvc.Testing's
            // WebApplicationFactory<Program>) can override it via ConfigureAppConfiguration/
            // ConfigureWebHost - those overrides land in builder.Configuration before the host is
            // actually built, but a value captured into a local variable this early would miss
            // them entirely, always falling back to appsettings.json's real Postgres connection string.
            //
            // AddDbContextFactory (not AddDbContext): repositories are captured once at startup
            // into the static Repositories locator below and reused for the app's whole lifetime,
            // so they must not hold a single shared AppDbContext - DbContext isn't thread-safe,
            // and a failed SaveChangesAsync (e.g. a constraint violation from bad input) leaves a
            // shared instance's change tracker corrupted for every later call until process
            // restart. IDbContextFactory<AppDbContext> is itself thread-safe and fine to hold
            // long-lived; EfKeyedRepository creates a fresh, short-lived context per operation.
            builder.Services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
                options.UseNpgsql(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")));

            // Singleton (not Scoped): each repository only holds the thread-safe context factory
            // above, not a stateful DbContext, so it's safe to resolve once and reuse forever.
            builder.Services.AddSingleton<IPersonRepository, PersonRepository>();
            builder.Services.AddSingleton<IPersonShareRepository, PersonShareRepository>();
            builder.Services.AddSingleton<IUserDataRepository, UserDataRepository>();
            builder.Services.AddSingleton<ILifeEventRepository, LifeEventRepository>();
            builder.Services.AddSingleton<ICallTrackerRepository, CallTrackerRepository>();
            builder.Services.AddSingleton<IWebsiteErrorLogRepository, WebsiteErrorLogRepository>();
            builder.Services.AddSingleton<IWebsiteDebugLogRepository, WebsiteDebugLogRepository>();
            builder.Services.AddSingleton<IOpenAPIErrorBookRepository, OpenAPIErrorBookRepository>();
            builder.Services.AddSingleton<ISubscriberCallRecordRepository, SubscriberCallRecordRepository>();
            builder.Services.AddSingleton<IAnonymousIpCallRecordRepository, AnonymousIpCallRecordRepository>();
            builder.Services.AddSingleton<ICallInfoStatisticRepository, CallInfoStatisticRepository>();
            builder.Services.AddSingleton<IIpAddressStatisticRepository, IpAddressStatisticRepository>();
            builder.Services.AddSingleton<IWebPageStatisticRepository, WebPageStatisticRepository>();
            builder.Services.AddSingleton<IRequestUrlStatisticRepository, RequestUrlStatisticRepository>();
            builder.Services.AddSingleton<ISubscriberStatisticRepository, SubscriberStatisticRepository>();
            builder.Services.AddSingleton<IUserAgentStatisticRepository, UserAgentStatisticRepository>();
            builder.Services.AddSingleton<IRawRequestStatisticRepository, RawRequestStatisticRepository>();

            // Geolocation cache tier (LocationManager.cs's "VedAstro" provider)
            builder.Services.AddSingleton<IAddressGeoLocationRepository, AddressGeoLocationRepository>();
            builder.Services.AddSingleton<ICoordinatesGeoLocationRepository, CoordinatesGeoLocationRepository>();
            builder.Services.AddSingleton<IGeoLocationTimezoneRepository, GeoLocationTimezoneRepository>();
            builder.Services.AddSingleton<IGeoLocationTimezoneMetadataRepository, GeoLocationTimezoneMetadataRepository>();
            builder.Services.AddSingleton<IIpAddressGeoLocationRepository, IpAddressGeoLocationRepository>();
            builder.Services.AddSingleton<IIpAddressGeoLocationMetadataRepository, IpAddressGeoLocationMetadataRepository>();
            builder.Services.AddSingleton<ISearchAddressGeoLocationRepository, SearchAddressGeoLocationRepository>();

            // Chat (ChatAPI.cs) - history/feedback rating + experimental preset-question search index
            builder.Services.AddSingleton<IChatMessageRepository, ChatMessageRepository>();
            builder.Services.AddSingleton<IPresetQuestionEmbeddingsRepository, PresetQuestionEmbeddingsRepository>();

            // Saved match reports (MatchAPI.cs's SaveMatchReport/GetMatchReportList)
            builder.Services.AddSingleton<ISavedMatchReportRepository, SavedMatchReportRepository>();

            // same lazy-resolution reasoning as the connection string above - resolved from DI
            // when the singleton is first requested (after the host is fully built), not captured
            // from builder.Configuration this early, so test hosts can override ChartCacheDirectory.
            builder.Services.AddSingleton<IChartImageCache>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var chartCacheDir = configuration["ChartCacheDirectory"] ?? "ChartCache";
                var chartCacheFullPath = Path.IsPathRooted(chartCacheDir)
                    ? chartCacheDir
                    : Path.Combine(environment.ContentRootPath, chartCacheDir);
                return new LocalDiskChartImageCache(chartCacheFullPath);
            });

            // CORS: matches the old host.json/local.settings.json "CORS": "*" wide-open local-dev setting
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Call-Status");
                });
            });

            var app = builder.Build();

            app.UseCors("AllowAll");

            // Firebase Admin SDK - verifies ID tokens produced by the React Native client's
            // Firebase Auth sign-in (see WebsiteNative/src/lib/firebase and SignInAPI.cs's
            // /api/SignInFirebase/Token/{token}). Degrades gracefully when the service account
            // key isn't configured, same pattern as LOCAL_LLM_BASE_URL/SendEmail elsewhere in
            // this codebase - the old Blazor site's direct Google/Facebook endpoints are
            // unaffected either way.
            var firebaseServiceAccountKeyPath = builder.Configuration["FirebaseServiceAccountKeyPath"];
            if (!string.IsNullOrWhiteSpace(firebaseServiceAccountKeyPath) && File.Exists(firebaseServiceAccountKeyPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(firebaseServiceAccountKeyPath)
                });
            }
            else
            {
                Console.WriteLine("WARN: FirebaseServiceAccountKeyPath not configured or file not found - " +
                                   "/api/SignInFirebase will fail until a real service account key is set " +
                                   "(Firebase Console > Project Settings > Service Accounts > Generate new private key).");
            }

            // ---- Wire the Library's static Repositories locator (see Library/Logic/Repositories.cs) ----
            // Data-access static classes living in Library (CallTracker.cs, Tools.cs, ApiStatistic.cs,
            // UserData.cs, LocationManager.cs) call through Repositories.* instead of taking a DI
            // dependency directly, since they're static classes that predate this migration.
            // Repositories are registered Singleton and resolved once here - safe because each one
            // only holds the thread-safe IDbContextFactory<AppDbContext> above, not a stateful
            // DbContext, so a single long-lived instance per repository is fine.
            var services = app.Services;

            Repositories.Person = services.GetRequiredService<IPersonRepository>();
            Repositories.PersonShare = services.GetRequiredService<IPersonShareRepository>();
            Repositories.UserData = services.GetRequiredService<IUserDataRepository>();
            Repositories.LifeEvent = services.GetRequiredService<ILifeEventRepository>();
            Repositories.CallTracker = services.GetRequiredService<ICallTrackerRepository>();
            Repositories.WebsiteErrorLog = services.GetRequiredService<IWebsiteErrorLogRepository>();
            Repositories.WebsiteDebugLog = services.GetRequiredService<IWebsiteDebugLogRepository>();
            Repositories.OpenAPIErrorBook = services.GetRequiredService<IOpenAPIErrorBookRepository>();
            Repositories.SubscriberCallRecords = services.GetRequiredService<ISubscriberCallRecordRepository>();
            Repositories.AnonymousIpCallRecords = services.GetRequiredService<IAnonymousIpCallRecordRepository>();
            Repositories.CallInfoStatistic = services.GetRequiredService<ICallInfoStatisticRepository>();
            Repositories.IpAddressStatistic = services.GetRequiredService<IIpAddressStatisticRepository>();
            Repositories.WebPageStatistic = services.GetRequiredService<IWebPageStatisticRepository>();
            Repositories.RequestUrlStatistic = services.GetRequiredService<IRequestUrlStatisticRepository>();
            Repositories.SubscriberStatistic = services.GetRequiredService<ISubscriberStatisticRepository>();
            Repositories.UserAgentStatistic = services.GetRequiredService<IUserAgentStatisticRepository>();
            Repositories.RawRequestStatistic = services.GetRequiredService<IRawRequestStatisticRepository>();
            Repositories.AddressGeoLocation = services.GetRequiredService<IAddressGeoLocationRepository>();
            Repositories.CoordinatesGeoLocation = services.GetRequiredService<ICoordinatesGeoLocationRepository>();
            Repositories.GeoLocationTimezone = services.GetRequiredService<IGeoLocationTimezoneRepository>();
            Repositories.GeoLocationTimezoneMetadata = services.GetRequiredService<IGeoLocationTimezoneMetadataRepository>();
            Repositories.IpAddressGeoLocation = services.GetRequiredService<IIpAddressGeoLocationRepository>();
            Repositories.IpAddressGeoLocationMetadata = services.GetRequiredService<IIpAddressGeoLocationMetadataRepository>();
            Repositories.SearchAddressGeoLocation = services.GetRequiredService<ISearchAddressGeoLocationRepository>();
            Repositories.ChartCache = services.GetRequiredService<IChartImageCache>();
            Repositories.ChatMessage = services.GetRequiredService<IChatMessageRepository>();
            Repositories.PresetQuestionEmbeddings = services.GetRequiredService<IPresetQuestionEmbeddingsRepository>();
            Repositories.SavedMatchReport = services.GetRequiredService<ISavedMatchReportRepository>();

            // ---- Route registration (one Map*Endpoints per old API/FrontDesk/*.cs file) ----
            app.MapGeneralEndpoints();
            app.MapOpenApiEndpoints();
            app.MapPersonEndpoints();
            app.MapBirthTimeFinderEndpoints();
            app.MapEventsChartEndpoints();
            app.MapMatchEndpoints();
            app.MapSignInEndpoints();
            app.MapSubscriptionEndpoints();
            app.MapWebsiteLoggerEndpoints();

            app.Run();
        }
    }
}
