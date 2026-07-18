using System;

namespace VedAstro.Library
{
    // These 6 mirror Library/Data/Statistic/*.cs (kept for reference, superseded by these
    // Azure-free versions living in VedAstro.Data). Previously declared-but-never-wired-up
    // in ApiStatistic.cs; now backed by real Postgres tables via IStatisticsRepository.

    public class IpAddressStatisticEntity : IPartitionRowKeyEntity
    {
        public static IpAddressStatisticEntity Empty = new IpAddressStatisticEntity();

        /// <summary>Ip Address</summary>
        public string PartitionKey { get; set; }

        /// <summary>year-month record, e.g. "2019-10"</summary>
        public string RowKey { get; set; }

        public double CallsPerSecond { get; set; }
        public DateTimeOffset? PerSecondTimestamp { get; set; }

        public double CallsPerMinute { get; set; }
        public DateTimeOffset? PerMinuteTimestamp { get; set; }

        public double CallsPerHour { get; set; }
        public DateTimeOffset? PerHourTimestamp { get; set; }

        public double CallsPerDay { get; set; }
        public DateTimeOffset? PerDayTimestamp { get; set; }

        public double CallsPerMonth { get; set; }
        public DateTimeOffset? PerMonthTimestamp { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class WebPageStatisticEntity : IPartitionRowKeyEntity
    {
        public static WebPageStatisticEntity Empty = new WebPageStatisticEntity();

        /// <summary>webpage</summary>
        public string PartitionKey { get; set; }

        /// <summary>date</summary>
        public string RowKey { get; set; }

        public double CallCount { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class RequestUrlStatisticEntity : IPartitionRowKeyEntity
    {
        public static RequestUrlStatisticEntity Empty = new RequestUrlStatisticEntity();

        /// <summary>Requested URL</summary>
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public double CallCount { get; set; }

        /// <summary>hash that links to metadata (not shatter or wax)</summary>
        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class SubscriberStatisticEntity : IPartitionRowKeyEntity
    {
        public static SubscriberStatisticEntity Empty = new SubscriberStatisticEntity();

        /// <summary>HOST address</summary>
        public string PartitionKey { get; set; }

        /// <summary>year and month, e.g. 2010-04</summary>
        public string RowKey { get; set; }

        public double CallCount { get; set; }

        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class UserAgentStatisticEntity : IPartitionRowKeyEntity
    {
        public static UserAgentStatisticEntity Empty = new UserAgentStatisticEntity();

        /// <summary>User Agent</summary>
        public string PartitionKey { get; set; }

        /// <summary>year and month, e.g. 2010-04</summary>
        public string RowKey { get; set; }

        public double CallCount { get; set; }

        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// Represents raw statistics of an HTTP request. High cost, used sparingly.
    /// </summary>
    [Serializable]
    public class RawRequestStatisticEntity : IPartitionRowKeyEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        [System.ComponentModel.Description("Accept")]
        public string Accept { get; set; }
        [System.ComponentModel.Description("Accept-Charset")]
        public string AcceptCharset { get; set; }
        [System.ComponentModel.Description("Accept-Encoding")]
        public string AcceptEncoding { get; set; }
        [System.ComponentModel.Description("Accept-Language")]
        public string AcceptLanguage { get; set; }
        [System.ComponentModel.Description("Authorization")]
        public string Authorization { get; set; }
        [System.ComponentModel.Description("Cache-Control")]
        public string CacheControl { get; set; }
        [System.ComponentModel.Description("Connection")]
        public string Connection { get; set; }
        [System.ComponentModel.Description("Cookie")]
        public string Cookie { get; set; }
        [System.ComponentModel.Description("Content-Length")]
        public string ContentLength { get; set; }
        [System.ComponentModel.Description("Content-MD5")]
        public string ContentMD5 { get; set; }
        [System.ComponentModel.Description("Content-Type")]
        public string ContentType { get; set; }
        [System.ComponentModel.Description("Date")]
        public string Date { get; set; }
        [System.ComponentModel.Description("Expect")]
        public string Expect { get; set; }
        [System.ComponentModel.Description("From")]
        public string From { get; set; }
        [System.ComponentModel.Description("Host")]
        public string Host { get; set; }
        [System.ComponentModel.Description("If-Match")]
        public string IfMatch { get; set; }
        [System.ComponentModel.Description("If-Modified-Since")]
        public string IfModifiedSince { get; set; }
        [System.ComponentModel.Description("If-None-Match")]
        public string IfNoneMatch { get; set; }
        [System.ComponentModel.Description("If-Range")]
        public string IfRange { get; set; }
        [System.ComponentModel.Description("If-Unmodified-Since")]
        public string IfUnmodifiedSince { get; set; }
        [System.ComponentModel.Description("Max-Forwards")]
        public string MaxForwards { get; set; }
        [System.ComponentModel.Description("Pragma")]
        public string Pragma { get; set; }
        [System.ComponentModel.Description("Proxy-Authorization")]
        public string ProxyAuthorization { get; set; }
        [System.ComponentModel.Description("Range")]
        public string Range { get; set; }
        [System.ComponentModel.Description("Referer")]
        public string Referer { get; set; }
        [System.ComponentModel.Description("TE")]
        public string TE { get; set; }
        [System.ComponentModel.Description("Upgrade")]
        public string Upgrade { get; set; }
        [System.ComponentModel.Description("User-Agent")]
        public string UserAgent { get; set; }
        [System.ComponentModel.Description("Via")]
        public string Via { get; set; }
        [System.ComponentModel.Description("Warning")]
        public string Warning { get; set; }
        [System.ComponentModel.Description("sec-fetch-referer")]
        public string SecFetchReferer { get; set; }
        [System.ComponentModel.Description("sec-fetch-origin")]
        public string SecFetchOrigin { get; set; }
        [System.ComponentModel.Description("sec-fetch-dest")]
        public string SecFetchDest { get; set; }
        [System.ComponentModel.Description("sec-fetch-mode")]
        public string SecFetchMode { get; set; }
        [System.ComponentModel.Description("sec-fetch-site")]
        public string SecFetchSite { get; set; }
        [System.ComponentModel.Description("sec-fetch-user")]
        public string SecFetchUser { get; set; }
        [System.ComponentModel.Description("sec-ch-ua-platform")]
        public string SecChUaPlatform { get; set; }
        [System.ComponentModel.Description("sec-ch-ua")]
        public string SecChUa { get; set; }
        [System.ComponentModel.Description("sec-ch-ua-mobile")]
        public string SecChUaMobile { get; set; }
        [System.ComponentModel.Description("sec-ch-ua-full-version")]
        public string SecChUaFullVersion { get; set; }
        [System.ComponentModel.Description("sec-ch-ua-arch")]
        public string SecChUaArch { get; set; }
        [System.ComponentModel.Description("sec-ch-ua-model")]
        public string SecChUaModel { get; set; }
        [System.ComponentModel.Description("sec-ch-ua-platform-version")]
        public string SecChUaPlatformVersion { get; set; }
        [System.ComponentModel.Description("X-Azure-ClientIP")]
        public string XAzureClientIP { get; set; }
        [System.ComponentModel.Description("X-Forwarded-For")]
        public string XForwardedFor { get; set; }
        [System.ComponentModel.Description("X-Forwarded-Host")]
        public string XForwardedHost { get; set; }
        [System.ComponentModel.Description("X-Forwarded-Proto")]
        public string XForwardedProto { get; set; }
        [System.ComponentModel.Description("X-Real-IP")]
        public string XRealIP { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string CalculateCombinedHash()
        {
            var propertyValues = new System.Text.StringBuilder();
            var properties = this.GetType().GetProperties();

            foreach (var property in properties)
            {
                var hasDescriptionAttribute = Attribute.IsDefined(property, typeof(System.ComponentModel.DescriptionAttribute));
                if (hasDescriptionAttribute)
                {
                    var value = property.GetValue(this) as string;
                    propertyValues.Append(value ?? "");
                }
            }

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(propertyValues.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
