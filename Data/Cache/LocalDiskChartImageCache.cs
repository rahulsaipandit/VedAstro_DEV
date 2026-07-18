using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedAstro.Data.Cache
{
    /// <summary>
    /// Local-disk replacement for the Azure Blob "cache" container (Library/Logic/AzureCache.cs).
    /// Files are stored flat under <see cref="RootDirectory"/>, named exactly like the old blob
    /// name (e.g. "Travis1985-EventsChart-20010202..."). Mime type/metadata are kept in a small
    /// JSON sidecar file (".meta.json") next to the data file, mirroring what blob metadata did.
    /// </summary>
    public class LocalDiskChartImageCache : IChartImageCache
    {
        public string RootDirectory { get; }

        public LocalDiskChartImageCache(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            Directory.CreateDirectory(RootDirectory);
        }

        private string PathFor(string callerId) => Path.Combine(RootDirectory, Sanitize(callerId));
        private string MetaPathFor(string callerId) => PathFor(callerId) + ".meta.json";

        /// <summary>
        /// Blob names can contain characters not valid in file names on some platforms (mostly
        /// harmless here since the source strings are ID/date based, but stay defensive).
        /// </summary>
        private static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                sb.Append(invalid.Contains(c) ? '_' : c);
            }
            return sb.ToString();
        }

        public List<string> ListBlobs(string searchKeyword)
        {
            if (!Directory.Exists(RootDirectory)) { return new List<string>(); }

            var prefix = Sanitize(searchKeyword);
            return Directory.EnumerateFiles(RootDirectory)
                .Select(Path.GetFileName)
                .Where(name => name != null && !name.EndsWith(".meta.json") && name.StartsWith(prefix, StringComparison.Ordinal))
                .Select(name => name!)
                .ToList();
        }

        public Task<bool> IsExist(string callerId) => Task.FromResult(File.Exists(PathFor(callerId)));

        public async Task<string> GetDataString(string callerId)
        {
            var path = PathFor(callerId);
            if (!File.Exists(path)) { return ""; }
            return await File.ReadAllTextAsync(path, Encoding.UTF8);
        }

        public async Task<byte[]> GetDataBytes(string callerId)
        {
            var path = PathFor(callerId);
            if (!File.Exists(path)) { return Array.Empty<byte>(); }
            return await File.ReadAllBytesAsync(path);
        }

        public Stream OpenRead(string callerId)
        {
            return new FileStream(PathFor(callerId), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public async Task Add(string fileName, string value, string mimeType = "", Dictionary<string, string>? metadata = null)
        {
            var path = PathFor(fileName);
            await File.WriteAllTextAsync(path, value ?? "", Encoding.UTF8);
            await WriteMeta(fileName, mimeType, metadata);
        }

        public async Task Add(string fileName, byte[] value, string mimeType = "", Dictionary<string, string>? metadata = null)
        {
            var path = PathFor(fileName);
            await File.WriteAllBytesAsync(path, value ?? Array.Empty<byte>());
            await WriteMeta(fileName, mimeType, metadata);
        }

        private async Task WriteMeta(string fileName, string mimeType, Dictionary<string, string>? metadata)
        {
            if (string.IsNullOrEmpty(mimeType) && (metadata == null || metadata.Count == 0)) { return; }

            var meta = new CacheMeta { MimeType = mimeType ?? "", Metadata = metadata ?? new Dictionary<string, string>() };
            var json = JsonSerializer.Serialize(meta);
            await File.WriteAllTextAsync(MetaPathFor(fileName), json, Encoding.UTF8);
        }

        public async Task<string> GetMimeType(string callerId)
        {
            var metaPath = MetaPathFor(callerId);
            if (!File.Exists(metaPath)) { return ""; }

            try
            {
                var json = await File.ReadAllTextAsync(metaPath, Encoding.UTF8);
                var meta = JsonSerializer.Deserialize<CacheMeta>(json);
                return meta?.MimeType ?? "";
            }
            catch
            {
                return "";
            }
        }

        public Task Delete(string callerId)
        {
            var path = PathFor(callerId);
            if (File.Exists(path)) { File.Delete(path); }

            var metaPath = MetaPathFor(callerId);
            if (File.Exists(metaPath)) { File.Delete(metaPath); }

            return Task.CompletedTask;
        }

        public Task DeleteCacheRelatedToPerson(string personId)
        {
            if (string.IsNullOrEmpty(personId) || personId == "Empty") { return Task.CompletedTask; }

            foreach (var name in ListBlobs(personId))
            {
                var path = Path.Combine(RootDirectory, name);
                if (File.Exists(path)) { File.Delete(path); }

                var metaPath = path + ".meta.json";
                if (File.Exists(metaPath)) { File.Delete(metaPath); }
            }

            return Task.CompletedTask;
        }

        private class CacheMeta
        {
            public string MimeType { get; set; } = "";
            public Dictionary<string, string> Metadata { get; set; } = new();
        }
    }
}
