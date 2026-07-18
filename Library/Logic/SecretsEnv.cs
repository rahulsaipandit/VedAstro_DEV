using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Optional cloud secrets read from environment variables (set via API/local.settings.json for
    /// local dev). Nullable by design - callers must check for null/empty and degrade gracefully
    /// rather than throw, since these are genuinely optional for local development.
    /// </summary>
    public static partial class Secrets
    {
        public static string? VedAstroApiStorageConnStr => Environment.GetEnvironmentVariable("VedAstroApiStorageConnStr");
        public static string? VedAstroCentralStorageConnStr => Environment.GetEnvironmentVariable("VedAstroCentralStorageConnStr");
        public static string? AzureGeoLocationStorageConnStr => Environment.GetEnvironmentVariable("AzureGeoLocationStorageConnStr");
        public static string? AutoEmailerConnectString => Environment.GetEnvironmentVariable("AutoEmailerConnectString");
        public static string? AzureOpenAIAPIKey => Environment.GetEnvironmentVariable("AzureOpenAIAPIKey");
    }
}
