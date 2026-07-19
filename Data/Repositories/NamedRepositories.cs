using Microsoft.EntityFrameworkCore;
using VedAstro.Library;

namespace VedAstro.Data.Repositories
{
    // Named interfaces per the migration plan, each just a marker over the shared generic
    // CRUD surface (IKeyedRepository<T>) - kept separate so DI can register/resolve them
    // individually and so call-sites read clearly (Repositories.Person vs Repositories.UserData).

    public interface IPersonRepository : IKeyedRepository<PersonListEntity> { }
    public class PersonRepository : EfKeyedRepository<PersonListEntity>, IPersonRepository
    {
        public PersonRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    /// <summary>Read-only in practice (no write path exists in the original code either).</summary>
    public interface IPersonShareRepository : IKeyedRepository<PersonShareRow> { }
    public class PersonShareRepository : EfKeyedRepository<PersonShareRow>, IPersonShareRepository
    {
        public PersonShareRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IUserDataRepository : IKeyedRepository<UserDataListEntity> { }
    public class UserDataRepository : EfKeyedRepository<UserDataListEntity>, IUserDataRepository
    {
        public UserDataRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface ILifeEventRepository : IKeyedRepository<LifeEventRow> { }
    public class LifeEventRepository : EfKeyedRepository<LifeEventRow>, ILifeEventRepository
    {
        public LifeEventRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface ICallTrackerRepository : IKeyedRepository<CallStatusEntity> { }
    public class CallTrackerRepository : EfKeyedRepository<CallStatusEntity>, ICallTrackerRepository
    {
        public CallTrackerRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IWebsiteErrorLogRepository : IKeyedRepository<WebsiteErrorLogEntity> { }
    public class WebsiteErrorLogRepository : EfKeyedRepository<WebsiteErrorLogEntity>, IWebsiteErrorLogRepository
    {
        public WebsiteErrorLogRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IWebsiteDebugLogRepository : IKeyedRepository<WebsiteDebugLogEntity> { }
    public class WebsiteDebugLogRepository : EfKeyedRepository<WebsiteDebugLogEntity>, IWebsiteDebugLogRepository
    {
        public WebsiteDebugLogRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IOpenAPIErrorBookRepository : IKeyedRepository<OpenAPIErrorBookEntity> { }
    public class OpenAPIErrorBookRepository : EfKeyedRepository<OpenAPIErrorBookEntity>, IOpenAPIErrorBookRepository
    {
        public OpenAPIErrorBookRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface ISubscriberCallRecordRepository : IKeyedRepository<SubscriberCallRecordEntity> { }
    public class SubscriberCallRecordRepository : EfKeyedRepository<SubscriberCallRecordEntity>, ISubscriberCallRecordRepository
    {
        public SubscriberCallRecordRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IAnonymousIpCallRecordRepository : IKeyedRepository<AnonymousIpCallRecordEntity> { }
    public class AnonymousIpCallRecordRepository : EfKeyedRepository<AnonymousIpCallRecordEntity>, IAnonymousIpCallRecordRepository
    {
        public AnonymousIpCallRecordRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    // ---- Statistics: CallInfo (live) + the 6 previously-dead tables (now wired up) ----

    public interface ICallInfoStatisticRepository : IKeyedRepository<CallInfoStatisticEntity> { }
    public class CallInfoStatisticRepository : EfKeyedRepository<CallInfoStatisticEntity>, ICallInfoStatisticRepository
    {
        public CallInfoStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IIpAddressStatisticRepository : IKeyedRepository<IpAddressStatisticEntity> { }
    public class IpAddressStatisticRepository : EfKeyedRepository<IpAddressStatisticEntity>, IIpAddressStatisticRepository
    {
        public IpAddressStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IWebPageStatisticRepository : IKeyedRepository<WebPageStatisticEntity> { }
    public class WebPageStatisticRepository : EfKeyedRepository<WebPageStatisticEntity>, IWebPageStatisticRepository
    {
        public WebPageStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IRequestUrlStatisticRepository : IKeyedRepository<RequestUrlStatisticEntity> { }
    public class RequestUrlStatisticRepository : EfKeyedRepository<RequestUrlStatisticEntity>, IRequestUrlStatisticRepository
    {
        public RequestUrlStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface ISubscriberStatisticRepository : IKeyedRepository<SubscriberStatisticEntity> { }
    public class SubscriberStatisticRepository : EfKeyedRepository<SubscriberStatisticEntity>, ISubscriberStatisticRepository
    {
        public SubscriberStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IUserAgentStatisticRepository : IKeyedRepository<UserAgentStatisticEntity> { }
    public class UserAgentStatisticRepository : EfKeyedRepository<UserAgentStatisticEntity>, IUserAgentStatisticRepository
    {
        public UserAgentStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IRawRequestStatisticRepository : IKeyedRepository<RawRequestStatisticEntity> { }
    public class RawRequestStatisticRepository : EfKeyedRepository<RawRequestStatisticEntity>, IRawRequestStatisticRepository
    {
        public RawRequestStatisticRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    // ---- Geolocation cache tier (LocationManager.cs's "VedAstro" provider) ----

    public interface IAddressGeoLocationRepository : IKeyedRepository<AddressGeoLocationEntity> { }
    public class AddressGeoLocationRepository : EfKeyedRepository<AddressGeoLocationEntity>, IAddressGeoLocationRepository
    {
        public AddressGeoLocationRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface ICoordinatesGeoLocationRepository : IKeyedRepository<CoordinatesGeoLocationEntity> { }
    public class CoordinatesGeoLocationRepository : EfKeyedRepository<CoordinatesGeoLocationEntity>, ICoordinatesGeoLocationRepository
    {
        public CoordinatesGeoLocationRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IGeoLocationTimezoneRepository : IKeyedRepository<GeoLocationTimezoneEntity> { }
    public class GeoLocationTimezoneRepository : EfKeyedRepository<GeoLocationTimezoneEntity>, IGeoLocationTimezoneRepository
    {
        public GeoLocationTimezoneRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IGeoLocationTimezoneMetadataRepository : IKeyedRepository<GeoLocationTimezoneMetadataEntity> { }
    public class GeoLocationTimezoneMetadataRepository : EfKeyedRepository<GeoLocationTimezoneMetadataEntity>, IGeoLocationTimezoneMetadataRepository
    {
        public GeoLocationTimezoneMetadataRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IIpAddressGeoLocationRepository : IKeyedRepository<IpAddressGeoLocationEntity> { }
    public class IpAddressGeoLocationRepository : EfKeyedRepository<IpAddressGeoLocationEntity>, IIpAddressGeoLocationRepository
    {
        public IpAddressGeoLocationRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IIpAddressGeoLocationMetadataRepository : IKeyedRepository<IpAddressGeoLocationMetadataEntity> { }
    public class IpAddressGeoLocationMetadataRepository : EfKeyedRepository<IpAddressGeoLocationMetadataEntity>, IIpAddressGeoLocationMetadataRepository
    {
        public IpAddressGeoLocationMetadataRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface ISearchAddressGeoLocationRepository : IKeyedRepository<SearchAddressGeoLocationEntity> { }
    public class SearchAddressGeoLocationRepository : EfKeyedRepository<SearchAddressGeoLocationEntity>, ISearchAddressGeoLocationRepository
    {
        public SearchAddressGeoLocationRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    // ---- MatchMLPipeline (standalone offline console tool, not part of the live API/Website) ----

    public interface IMarriageInfoDatasetRepository : IKeyedRepository<MarriageInfoDatasetEntity> { }
    public class MarriageInfoDatasetRepository : EfKeyedRepository<MarriageInfoDatasetEntity>, IMarriageInfoDatasetRepository
    {
        public MarriageInfoDatasetRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IBodyInfoDatasetRepository : IKeyedRepository<BodyInfoDatasetEntity> { }
    public class BodyInfoDatasetRepository : EfKeyedRepository<BodyInfoDatasetEntity>, IBodyInfoDatasetRepository
    {
        public BodyInfoDatasetRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IPersonNameEmbeddingsRepository : IKeyedRepository<PersonNameEmbeddingsEntity> { }
    public class PersonNameEmbeddingsRepository : EfKeyedRepository<PersonNameEmbeddingsEntity>, IPersonNameEmbeddingsRepository
    {
        public PersonNameEmbeddingsRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    // ---- Chat (ChatAPI.cs) - history/feedback rating + experimental preset-question search index ----

    public interface IChatMessageRepository : IKeyedRepository<ChatMessageEntity> { }
    public class ChatMessageRepository : EfKeyedRepository<ChatMessageEntity>, IChatMessageRepository
    {
        public ChatMessageRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    public interface IPresetQuestionEmbeddingsRepository : IKeyedRepository<PresetQuestionEmbeddingsEntity> { }
    public class PresetQuestionEmbeddingsRepository : EfKeyedRepository<PresetQuestionEmbeddingsEntity>, IPresetQuestionEmbeddingsRepository
    {
        public PresetQuestionEmbeddingsRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }

    // ---- Saved match reports (MatchAPI.cs's SaveMatchReport/GetMatchReportList - new persistence, not a port) ----

    public interface ISavedMatchReportRepository : IKeyedRepository<SavedMatchReportEntity> { }
    public class SavedMatchReportRepository : EfKeyedRepository<SavedMatchReportEntity>, ISavedMatchReportRepository
    {
        public SavedMatchReportRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
    }
}
