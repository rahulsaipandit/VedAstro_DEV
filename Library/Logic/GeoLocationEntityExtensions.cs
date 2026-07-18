using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// GeoLocation-aware helper methods for the geolocation cache entities that physically live
    /// in VedAstro.Data (Data/Entities/GeoLocationEntities.cs) as plain POCOs - kept here as
    /// extension methods (rather than instance methods on the entities) because GeoLocation and
    /// JArray/GeoLocation.FromJson are VedAstro.Library types, and VedAstro.Data must not
    /// reference Library (Library references Data, not the reverse, to avoid a circular project
    /// reference). Mirrors the PersonListEntityExtensions.cs pattern used elsewhere.
    /// LocationManager.cs calls these via `dynamic` dispatch (row.ToGeoLocation()); dynamic
    /// extension-method binding resolves against the call site's namespace, and both this file
    /// and LocationManager.cs are in namespace VedAstro.Library, so no `using` is needed.
    /// </summary>
    public static class GeoLocationEntityExtensions
    {
        public static GeoLocation ToGeoLocation(this AddressGeoLocationEntity row)
        {
            try
            {
                //if empty name then fail
                if (string.IsNullOrEmpty(row.PartitionKey)) { return GeoLocation.Empty; }

                return new GeoLocation(row.PartitionKey, row.Longitude, row.Latitude);
            }
            catch (Exception)
            {
                return GeoLocation.Empty;
            }
        }

        public static GeoLocation ToGeoLocation(this CoordinatesGeoLocationEntity row)
        {
            try
            {
                //if empty name then fail
                if (string.IsNullOrEmpty(row.Name)) { return GeoLocation.Empty; }

                //convert from string to numbers
                var latitude = double.Parse(row.PartitionKey);
                var longitude = double.Parse(row.RowKey);

                return new GeoLocation(row.Name, longitude, latitude);
            }
            //if any fails then empty
            catch (Exception)
            {
                return GeoLocation.Empty;
            }
        }

        public static GeoLocation ToGeoLocation(this IpAddressGeoLocationEntity row)
        {
            try
            {
                //if empty name then fail
                if (string.IsNullOrEmpty(row.LocationName)) { return GeoLocation.Empty; }

                return new GeoLocation(row.LocationName, row.Longitude, row.Latitude);
            }
            //if any fails then empty
            catch (Exception)
            {
                return GeoLocation.Empty;
            }
        }

        /// <summary>Used by API SearchLocation</summary>
        public static List<GeoLocation> ToGeoLocationList(this SearchAddressGeoLocationEntity row)
        {
            try
            {
                //if empty name then return empty
                if (string.IsNullOrEmpty(row.PartitionKey)) { return new List<GeoLocation>(); }

                //parse string into jobject
                var parsedListJson = JArray.Parse(row.Results);

                var returnList = new List<GeoLocation>();
                //convert each jobject list into geo location
                foreach (var geoLocationJson in parsedListJson)
                {
                    returnList.Add(GeoLocation.FromJson(geoLocationJson));
                }

                return returnList;
            }
            catch (Exception)
            {
                return new List<GeoLocation>();
            }
        }
    }
}
