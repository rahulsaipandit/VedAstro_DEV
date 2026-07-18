using System.Collections.Generic;
using System.Threading.Tasks;

namespace VedAstro.Data.Cache
{
    /// <summary>
    /// Replaces Library/Logic/AzureCache.cs's blob-storage operations with a local-disk
    /// equivalent. Every cached item is identified by a "caller ID" / file name string,
    /// exactly like the old blob name (e.g. "Travis1985-EventsChart-20010202...").
    /// </summary>
    public interface IChartImageCache
    {
        /// <summary>Lists all cached item names whose name starts with the given prefix.</summary>
        List<string> ListBlobs(string searchKeyword);

        Task<bool> IsExist(string callerId);

        /// <summary>Reads cached content as UTF-8 text.</summary>
        Task<string> GetDataString(string callerId);

        /// <summary>Reads cached content as raw bytes.</summary>
        Task<byte[]> GetDataBytes(string callerId);

        /// <summary>Opens a read stream directly to the cached file (fast path - avoids buffering into memory).</summary>
        System.IO.Stream OpenRead(string callerId);

        /// <summary>Adds/overwrites a cached text item.</summary>
        Task Add(string fileName, string value, string mimeType = "", Dictionary<string, string>? metadata = null);

        /// <summary>Adds/overwrites a cached binary item.</summary>
        Task Add(string fileName, byte[] value, string mimeType = "", Dictionary<string, string>? metadata = null);

        Task Delete(string callerId);

        /// <summary>Deletes every cached item whose name is prefixed with the given person ID.</summary>
        Task DeleteCacheRelatedToPerson(string personId);

        /// <summary>Gets the mime type recorded for a cached item, if any (empty string if not set).</summary>
        Task<string> GetMimeType(string callerId);
    }
}
