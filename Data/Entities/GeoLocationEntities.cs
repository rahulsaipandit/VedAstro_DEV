using System;
using System.Security.Cryptography;
using System.Text;

namespace VedAstro.Library
{
    // 7 geolocation cache tables backing LocationManager.cs's "VedAstro" provider tier
    // (used to be 9 raw Azure Table Storage clients hit directly, bypassing AzureTable.cs).
    // NOTE: these are plain POCOs with no dependency on VedAstro.Library's calculator types
    // (GeoLocation, JArray, etc) because they physically live in VedAstro.Data, which must not
    // reference Library (Library references Data, not the reverse, to avoid a circular project
    // reference). The GeoLocation-aware helper methods that used to live on these classes
    // (ToGeoLocation/ToGeoLocationList) are now extension methods in
    // Library/Logic/GeoLocationEntityExtensions.cs. These previously lived as full classes
    // directly under Library/Data/Statistic/ - that's now superseded by these versions.

    /// <summary>
    /// Facts:
    /// 1 decimal place: 11.1 km, 2: 1.11 km, 3: 111 m, 4: 11.1 m, 5: 1.11 m, 6: 0.111 m
    /// </summary>
    public class AddressGeoLocationEntity : IPartitionRowKeyEntity
    {
        public static AddressGeoLocationEntity Empty = new AddressGeoLocationEntity();

        /// <summary>full formatted named, EXP: Tokyo Japan</summary>
        public string PartitionKey { get; set; }

        /// <summary>cleaned named entered by user, EXP: Japan</summary>
        public string RowKey { get; set; }

        public double Longitude { get; set; }
        public double Latitude { get; set; }

        /// <summary>hash that links to metadata</summary>
        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class CoordinatesGeoLocationEntity : IPartitionRowKeyEntity
    {
        public static CoordinatesGeoLocationEntity Empty = new CoordinatesGeoLocationEntity();

        /// <summary>Latitude (placed here for fast indexing, known by caller)</summary>
        public string PartitionKey { get; set; }

        /// <summary>Longitude (placed here for fast indexing, known by caller)</summary>
        public string RowKey { get; set; }

        /// <summary>Formal location name</summary>
        public string Name { get; set; }

        /// <summary>hash that links to metadata</summary>
        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class GeoLocationTimezoneEntity : IPartitionRowKeyEntity
    {
        public static GeoLocationTimezoneEntity Empty = new GeoLocationTimezoneEntity();

        /// <summary>latitude & longitude in google search friendly format, EXP: -3.9571599,103.8723379</summary>
        public string PartitionKey { get; set; }

        /// <summary>time at place date time format no timezone</summary>
        public string RowKey { get; set; }

        /// <summary>final timezone with combined DST comes in from API</summary>
        public string TimezoneText { get; set; }

        /// <summary>hash that links to metadata</summary>
        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class GeoLocationTimezoneMetadataEntity : IPartitionRowKeyEntity
    {
        public static GeoLocationTimezoneMetadataEntity Empty = new GeoLocationTimezoneMetadataEntity();

        /// <summary>hash that identifies the data, used at query time to check for match</summary>
        public string PartitionKey { get; set; }

        /// <summary>leave empty not needed</summary>
        public string RowKey { get; set; }

        /// <summary>final timezone with combined DST</summary>
        public string TimezoneText { get; set; }

        /// <summary>STD offset always on</summary>
        public string StandardOffset { get; set; }

        /// <summary>daylight timezone only when timezone</summary>
        public string DaylightSavings { get; set; }

        /// <summary>ISO CODE</summary>
        public string Tag { get; set; }

        /// <summary>extra data to verify if DST exists</summary>
        public string Standard_Name { get; set; }

        /// <summary>extra data to verify if DST exists</summary>
        public string Daylight_Name { get; set; }

        /// <summary>ISO name, MS API calls it ID</summary>
        public string ISO_Name { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string CalculateCombinedHash()
        {
            var propertyValues = new StringBuilder()
                .Append(this.TimezoneText ?? "")
                .Append(this.StandardOffset ?? "")
                .Append(this.DaylightSavings ?? "")
                .Append(this.Tag ?? "")
                .Append(this.Standard_Name ?? "")
                .Append(this.Daylight_Name ?? "")
                .Append(this.ISO_Name ?? "");

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(propertyValues.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public class IpAddressGeoLocationEntity : IPartitionRowKeyEntity
    {
        public static IpAddressGeoLocationEntity Empty = new IpAddressGeoLocationEntity();

        /// <summary>Ip Address</summary>
        public string PartitionKey { get; set; }

        /// <summary>empty</summary>
        public string RowKey { get; set; }

        public string LocationName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }

        /// <summary>hash that links to metadata</summary>
        public string MetadataHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    public class IpAddressGeoLocationMetadataEntity : IPartitionRowKeyEntity
    {
        public static IpAddressGeoLocationMetadataEntity Empty = new IpAddressGeoLocationMetadataEntity();

        /// <summary>hash that identifies the data, used at query time to check for match</summary>
        public string PartitionKey { get; set; }

        /// <summary>leave empty not needed</summary>
        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string AsnName { get; set; }
        public string TimezoneName { get; set; }
        public string TimezoneOffset { get; set; }
        public string IsProxy { get; set; }
        public string IsDatacenter { get; set; }
        public string IsAnonymous { get; set; }
        public string IsKnownAttacker { get; set; }
        public string IsKnownAbuser { get; set; }
        public string IsThreat { get; set; }
        public string IsBogon { get; set; }

        public string CalculateCombinedHash()
        {
            var propertyValues = new StringBuilder()
                .Append(this.AsnName ?? "")
                .Append(this.TimezoneName ?? "")
                .Append(this.TimezoneOffset ?? "")
                .Append(this.IsProxy ?? "")
                .Append(this.IsDatacenter ?? "")
                .Append(this.IsAnonymous ?? "")
                .Append(this.IsKnownAttacker ?? "")
                .Append(this.IsKnownAbuser ?? "")
                .Append(this.IsThreat ?? "")
                .Append(this.IsBogon ?? "");

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(propertyValues.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public class SearchAddressGeoLocationEntity : IPartitionRowKeyEntity
    {
        public static SearchAddressGeoLocationEntity Empty = new SearchAddressGeoLocationEntity();

        /// <summary>cleaned text entered by user</summary>
        public string PartitionKey { get; set; }

        /// <summary>empty</summary>
        public string RowKey { get; set; }

        /// <summary>List of GeoLocation in JSON string format</summary>
        public string Results { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
