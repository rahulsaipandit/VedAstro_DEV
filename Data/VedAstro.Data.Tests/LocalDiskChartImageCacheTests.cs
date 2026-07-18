using System.Text;
using VedAstro.Data.Cache;
using Xunit;

namespace VedAstro.Data.Tests
{
    /// <summary>No Postgres involved - exercises IChartImageCache against a real temp directory.</summary>
    public class LocalDiskChartImageCacheTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly LocalDiskChartImageCache _cache;

        public LocalDiskChartImageCacheTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "vedastro-chart-cache-tests-" + Guid.NewGuid());
            _cache = new LocalDiskChartImageCache(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir)) { Directory.Delete(_tempDir, recursive: true); }
        }

        [Fact]
        public async Task Add_Text_ThenIsExist_ReturnsTrue()
        {
            await _cache.Add("chart-a", "<svg>a</svg>", "image/svg+xml");

            Assert.True(await _cache.IsExist("chart-a"));
        }

        [Fact]
        public async Task IsExist_ForMissingItem_ReturnsFalse()
        {
            Assert.False(await _cache.IsExist("does-not-exist"));
        }

        [Fact]
        public async Task Add_Text_ThenGetDataString_ReturnsSameContent()
        {
            await _cache.Add("chart-b", "hello world", "text/plain");

            var content = await _cache.GetDataString("chart-b");

            Assert.Equal("hello world", content);
        }

        [Fact]
        public async Task Add_Bytes_ThenGetDataBytes_ReturnsSameContent()
        {
            var bytes = Encoding.UTF8.GetBytes("binary-payload");
            await _cache.Add("chart-c", bytes, "application/octet-stream");

            var result = await _cache.GetDataBytes("chart-c");

            Assert.Equal(bytes, result);
        }

        [Fact]
        public async Task Add_ThenOpenRead_StreamsSameContent()
        {
            await _cache.Add("chart-d", "stream-me", "text/plain");

            using var stream = _cache.OpenRead("chart-d");
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            Assert.Equal("stream-me", content);
        }

        [Fact]
        public async Task Add_WithMimeType_ThenGetMimeType_ReturnsIt()
        {
            await _cache.Add("chart-e", "data", "image/png");

            var mimeType = await _cache.GetMimeType("chart-e");

            Assert.Equal("image/png", mimeType);
        }

        [Fact]
        public async Task GetMimeType_WhenNoneSet_ReturnsEmptyString()
        {
            await _cache.Add("chart-f", "data");

            var mimeType = await _cache.GetMimeType("chart-f");

            Assert.Equal("", mimeType);
        }

        [Fact]
        public async Task Delete_RemovesItem()
        {
            await _cache.Add("chart-g", "data");
            Assert.True(await _cache.IsExist("chart-g"));

            await _cache.Delete("chart-g");

            Assert.False(await _cache.IsExist("chart-g"));
        }

        [Fact]
        public async Task ListBlobs_ReturnsOnlyItemsMatchingPrefix()
        {
            await _cache.Add("Traveller1985-EventsChart-1", "x");
            await _cache.Add("Traveller1985-EventsChart-2", "y");
            await _cache.Add("OtherPerson-EventsChart-1", "z");

            var results = _cache.ListBlobs("Traveller1985");

            Assert.Equal(2, results.Count);
            Assert.All(results, name => Assert.StartsWith("Traveller1985", name));
        }

        [Fact]
        public async Task DeleteCacheRelatedToPerson_RemovesOnlyThatPersonsItems()
        {
            await _cache.Add("PersonA-Chart-1", "x");
            await _cache.Add("PersonA-Chart-2", "y");
            await _cache.Add("PersonB-Chart-1", "z");

            await _cache.DeleteCacheRelatedToPerson("PersonA");

            Assert.False(await _cache.IsExist("PersonA-Chart-1"));
            Assert.False(await _cache.IsExist("PersonA-Chart-2"));
            Assert.True(await _cache.IsExist("PersonB-Chart-1"));
        }

        [Fact]
        public async Task DeleteCacheRelatedToPerson_WithEmptyId_DoesNothing()
        {
            await _cache.Add("PersonC-Chart-1", "x");

            await _cache.DeleteCacheRelatedToPerson("");

            Assert.True(await _cache.IsExist("PersonC-Chart-1"));
        }
    }
}
