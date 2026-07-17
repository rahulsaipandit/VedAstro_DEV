using System;
using Azure.Data.Tables;

namespace VedAstro.Library
{
    /// <summary>
    /// Static storage for connection access to Azure tables
    /// </summary>
    public static class AzureTable
    {
        // STORAGE SETTINGS
        // NOTE: connection string based so Azurite (UseDevelopmentStorage=true) works for local dev;
        // nullable because a missing key should not crash the whole class (see MakeClient)
        private static readonly string? ConnStr = Secrets.VedAstroCentralStorageConnStr;

        private static TableClient? MakeClient(string tableName)
        {
            if (string.IsNullOrEmpty(ConnStr)) return null;
            var client = new TableServiceClient(ConnStr).GetTableClient(tableName);
            client.CreateIfNotExists();
            return client;
        }

        // TABLE CLIENTS
        public static readonly TableClient? PersonList = MakeClient("PersonList");

        public static readonly TableClient? SubscriberCallRecords = MakeClient("SubscriberCallRecords");

        public static readonly TableClient? AnonymousIpCallRecords = MakeClient("AnonymousIpCallRecords");

        public static readonly TableClient? UserDataList = MakeClient("UserDataList");

        public static readonly TableClient? LifeEventList = MakeClient("LifeEventList");

        public static readonly TableClient? OpenAPIErrorBook = MakeClient("OpenAPIErrorBook");

        public static readonly TableClient? CallTracker = MakeClient("CallTracker");

        public static readonly TableClient? WebsiteErrorLog = MakeClient("WebsiteErrorLog");

        public static readonly TableClient? WebsiteDebugLog = MakeClient("WebsiteDebugLog");
        public static readonly TableClient? CallInfoStatistic = MakeClient("CallInfoStatistic");

        /// <summary>
        /// Allows multiple users to share one person profile with read & write privileges.
        /// Shared people will also appear in the drop-down list.
        /// </summary>
        public static readonly TableClient? PersonShareList = MakeClient("PersonShareList");
    }
}
