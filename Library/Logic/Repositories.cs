using VedAstro.Data.Cache;
using VedAstro.Data.Repositories;

namespace VedAstro.Library
{
    /// <summary>
    /// Static service-locator wired up once at API host startup (API/Program.cs), after the DI
    /// container is built, e.g. `Repositories.Person = app.Services.GetRequiredService&lt;IPersonRepository&gt;();`.
    ///
    /// This exists so data-access static classes that already live in Library (CallTracker.cs,
    /// Tools.cs's person lookups, ApiStatistic.cs, UserData.cs, LocationManager.cs) can keep
    /// their existing public static method signatures - so every caller elsewhere in
    /// Library/API stays unchanged - while the actual I/O moves from Azure Table Storage
    /// (AzureTable.cs, now deleted) to Postgres, without Library needing a project reference to
    /// VedAstro.Data's concrete EF implementations or a DI container of its own.
    /// </summary>
    public static class Repositories
    {
        public static IPersonRepository Person { get; set; }
        public static IPersonShareRepository PersonShare { get; set; }
        public static IUserDataRepository UserData { get; set; }
        public static ILifeEventRepository LifeEvent { get; set; }
        public static ICallTrackerRepository CallTracker { get; set; }
        public static IWebsiteErrorLogRepository WebsiteErrorLog { get; set; }
        public static IWebsiteDebugLogRepository WebsiteDebugLog { get; set; }
        public static IOpenAPIErrorBookRepository OpenAPIErrorBook { get; set; }
        public static ISubscriberCallRecordRepository SubscriberCallRecords { get; set; }
        public static IAnonymousIpCallRecordRepository AnonymousIpCallRecords { get; set; }

        public static ICallInfoStatisticRepository CallInfoStatistic { get; set; }
        public static IIpAddressStatisticRepository IpAddressStatistic { get; set; }
        public static IWebPageStatisticRepository WebPageStatistic { get; set; }
        public static IRequestUrlStatisticRepository RequestUrlStatistic { get; set; }
        public static ISubscriberStatisticRepository SubscriberStatistic { get; set; }
        public static IUserAgentStatisticRepository UserAgentStatistic { get; set; }
        public static IRawRequestStatisticRepository RawRequestStatistic { get; set; }

        // ---- Geolocation cache tier (LocationManager.cs's "VedAstro" provider) ----
        public static IAddressGeoLocationRepository AddressGeoLocation { get; set; }
        public static ICoordinatesGeoLocationRepository CoordinatesGeoLocation { get; set; }
        public static IGeoLocationTimezoneRepository GeoLocationTimezone { get; set; }
        public static IGeoLocationTimezoneMetadataRepository GeoLocationTimezoneMetadata { get; set; }
        public static IIpAddressGeoLocationRepository IpAddressGeoLocation { get; set; }
        public static IIpAddressGeoLocationMetadataRepository IpAddressGeoLocationMetadata { get; set; }
        public static ISearchAddressGeoLocationRepository SearchAddressGeoLocation { get; set; }

        /// <summary>Local-disk chart image cache (replaces AzureCache.cs's blob container).</summary>
        public static IChartImageCache ChartCache { get; set; }

        // ---- Chat (ChatAPI.cs) ----
        public static IChatMessageRepository ChatMessage { get; set; }
        public static IPresetQuestionEmbeddingsRepository PresetQuestionEmbeddings { get; set; }

        // ---- Saved match reports (MatchAPI.cs) ----
        public static ISavedMatchReportRepository SavedMatchReport { get; set; }
    }
}
