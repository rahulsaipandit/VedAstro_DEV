using Microsoft.EntityFrameworkCore;
using VedAstro.Library;

namespace VedAstro.Data
{
    /// <summary>
    /// EF Core context for the Postgres-backed replacement of the old Azure Table Storage tables.
    /// Every table keeps its original PartitionKey/RowKey composite-key shape (mapped to
    /// partition_key/row_key text columns) instead of a redesigned schema - see migration plan.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PersonListEntity> PersonList => Set<PersonListEntity>();
        public DbSet<PersonShareRow> PersonShareList => Set<PersonShareRow>();
        public DbSet<UserDataListEntity> UserDataList => Set<UserDataListEntity>();
        public DbSet<LifeEventRow> LifeEventList => Set<LifeEventRow>();
        public DbSet<CallStatusEntity> CallTracker => Set<CallStatusEntity>();
        public DbSet<WebsiteErrorLogEntity> WebsiteErrorLog => Set<WebsiteErrorLogEntity>();
        public DbSet<WebsiteDebugLogEntity> WebsiteDebugLog => Set<WebsiteDebugLogEntity>();
        public DbSet<CallInfoStatisticEntity> CallInfoStatistic => Set<CallInfoStatisticEntity>();
        public DbSet<OpenAPIErrorBookEntity> OpenAPIErrorBook => Set<OpenAPIErrorBookEntity>();
        public DbSet<SubscriberCallRecordEntity> SubscriberCallRecords => Set<SubscriberCallRecordEntity>();
        public DbSet<AnonymousIpCallRecordEntity> AnonymousIpCallRecords => Set<AnonymousIpCallRecordEntity>();

        // 6 previously-dead statistic tables, now wired up for real
        public DbSet<IpAddressStatisticEntity> IpAddressStatistic => Set<IpAddressStatisticEntity>();
        public DbSet<WebPageStatisticEntity> WebPageStatistic => Set<WebPageStatisticEntity>();
        public DbSet<RequestUrlStatisticEntity> RequestUrlStatistic => Set<RequestUrlStatisticEntity>();
        public DbSet<SubscriberStatisticEntity> SubscriberStatistic => Set<SubscriberStatisticEntity>();
        public DbSet<UserAgentStatisticEntity> UserAgentStatistic => Set<UserAgentStatisticEntity>();
        public DbSet<RawRequestStatisticEntity> RawRequestStatistic => Set<RawRequestStatisticEntity>();

        // 7 geolocation cache tables backing LocationManager.cs's "VedAstro" provider tier
        // (used to be 9 raw Azure Table Storage clients hit directly, bypassing AzureTable.cs).
        public DbSet<AddressGeoLocationEntity> AddressGeoLocation => Set<AddressGeoLocationEntity>();
        public DbSet<CoordinatesGeoLocationEntity> CoordinatesGeoLocation => Set<CoordinatesGeoLocationEntity>();
        public DbSet<GeoLocationTimezoneEntity> GeoLocationTimezone => Set<GeoLocationTimezoneEntity>();
        public DbSet<GeoLocationTimezoneMetadataEntity> GeoLocationTimezoneMetadata => Set<GeoLocationTimezoneMetadataEntity>();
        public DbSet<IpAddressGeoLocationEntity> IpAddressGeoLocation => Set<IpAddressGeoLocationEntity>();
        public DbSet<IpAddressGeoLocationMetadataEntity> IpAddressGeoLocationMetadata => Set<IpAddressGeoLocationMetadataEntity>();
        public DbSet<SearchAddressGeoLocationEntity> SearchAddressGeoLocation => Set<SearchAddressGeoLocationEntity>();

        // 3 tables backing MatchMLPipeline (standalone offline console tool, not part of the
        // live API/Website) - previously raw Azure Table Storage rows.
        public DbSet<MarriageInfoDatasetEntity> MarriageInfoDataset => Set<MarriageInfoDatasetEntity>();
        public DbSet<BodyInfoDatasetEntity> BodyInfoDataset => Set<BodyInfoDatasetEntity>();
        public DbSet<PersonNameEmbeddingsEntity> PersonNameEmbeddings => Set<PersonNameEmbeddingsEntity>();
        public DbSet<MarriageTrainingDatasetEntity> MarriageTrainingDataset => Set<MarriageTrainingDatasetEntity>();

        // Chat history / feedback rating (ChatAPI.cs) and the experimental Cohere-embeddings
        // preset-question search index - previously a no-op in-memory stub, now real tables.
        public DbSet<ChatMessageEntity> ChatMessage => Set<ChatMessageEntity>();
        public DbSet<PresetQuestionEmbeddingsEntity> PresetQuestionEmbeddings => Set<PresetQuestionEmbeddingsEntity>();

        // Saved match reports (MatchAPI.cs's SaveMatchReport/GetMatchReportList) - genuinely new
        // persistence, the old Blazor site's client called an endpoint that never existed server-side.
        public DbSet<SavedMatchReportEntity> SavedMatchReport => Set<SavedMatchReportEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureKeyedTable<PersonListEntity>(modelBuilder, "person_list");
            ConfigureKeyedTable<PersonShareRow>(modelBuilder, "person_share_list");
            ConfigureKeyedTable<UserDataListEntity>(modelBuilder, "user_data_list");
            ConfigureKeyedTable<LifeEventRow>(modelBuilder, "life_event_list");
            ConfigureKeyedTable<CallStatusEntity>(modelBuilder, "call_tracker");
            ConfigureKeyedTable<WebsiteErrorLogEntity>(modelBuilder, "website_error_log");
            ConfigureKeyedTable<WebsiteDebugLogEntity>(modelBuilder, "website_debug_log");
            ConfigureKeyedTable<CallInfoStatisticEntity>(modelBuilder, "call_info_statistic");
            ConfigureKeyedTable<OpenAPIErrorBookEntity>(modelBuilder, "open_api_error_book");
            ConfigureKeyedTable<SubscriberCallRecordEntity>(modelBuilder, "subscriber_call_records");
            ConfigureKeyedTable<AnonymousIpCallRecordEntity>(modelBuilder, "anonymous_ip_call_records");

            ConfigureKeyedTable<IpAddressStatisticEntity>(modelBuilder, "ip_address_statistic");
            ConfigureKeyedTable<WebPageStatisticEntity>(modelBuilder, "web_page_statistic");
            ConfigureKeyedTable<RequestUrlStatisticEntity>(modelBuilder, "request_url_statistic");
            ConfigureKeyedTable<SubscriberStatisticEntity>(modelBuilder, "subscriber_statistic");
            ConfigureKeyedTable<UserAgentStatisticEntity>(modelBuilder, "user_agent_statistic");
            ConfigureKeyedTable<RawRequestStatisticEntity>(modelBuilder, "raw_request_statistic");

            ConfigureKeyedTable<AddressGeoLocationEntity>(modelBuilder, "address_geo_location");
            ConfigureKeyedTable<CoordinatesGeoLocationEntity>(modelBuilder, "coordinates_geo_location");
            ConfigureKeyedTable<GeoLocationTimezoneEntity>(modelBuilder, "geo_location_timezone");
            ConfigureKeyedTable<GeoLocationTimezoneMetadataEntity>(modelBuilder, "geo_location_timezone_metadata");
            ConfigureKeyedTable<IpAddressGeoLocationEntity>(modelBuilder, "ip_address_geo_location");
            ConfigureKeyedTable<IpAddressGeoLocationMetadataEntity>(modelBuilder, "ip_address_geo_location_metadata");
            ConfigureKeyedTable<SearchAddressGeoLocationEntity>(modelBuilder, "search_address_geo_location");

            ConfigureKeyedTable<MarriageInfoDatasetEntity>(modelBuilder, "marriage_info_dataset");
            ConfigureKeyedTable<BodyInfoDatasetEntity>(modelBuilder, "body_info_dataset");
            ConfigureKeyedTable<PersonNameEmbeddingsEntity>(modelBuilder, "person_name_embeddings");
            ConfigureKeyedTable<MarriageTrainingDatasetEntity>(modelBuilder, "marriage_training_dataset");

            ConfigureKeyedTable<ChatMessageEntity>(modelBuilder, "chat_message");
            ConfigureKeyedTable<PresetQuestionEmbeddingsEntity>(modelBuilder, "preset_question_embeddings");

            ConfigureKeyedTable<SavedMatchReportEntity>(modelBuilder, "saved_match_report");
        }

        /// <summary>
        /// Every ported table shares the same shape: composite (PartitionKey, RowKey) key,
        /// mapped to partition_key/row_key columns - mirrors the original Azure Table row key.
        /// </summary>
        private static void ConfigureKeyedTable<TEntity>(ModelBuilder modelBuilder, string tableName)
            where TEntity : class, IPartitionRowKeyEntity
        {
            var entity = modelBuilder.Entity<TEntity>();
            entity.ToTable(tableName);
            entity.HasKey(e => new { e.PartitionKey, e.RowKey });
            entity.Property(e => e.PartitionKey).HasColumnName("partition_key").IsRequired();
            entity.Property(e => e.RowKey).HasColumnName("row_key").IsRequired();
        }
    }
}
