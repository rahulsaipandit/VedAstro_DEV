using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers the Chat-related Calculate dispatcher methods (CoreRelationships.cs's
    /// HoroscopeChat/HoroscopeFollowUpChat/HoroscopeChatFeedback -> ChatAPI.cs), which route
    /// through ProcessPrediction to an OpenAI-compatible local LLM server (e.g. LM Studio) via
    /// LOCAL_LLM_BASE_URL/LOCAL_LLM_API_KEY/LOCAL_LLM_MODEL env vars. These tests self-skip
    /// (not fail) when no local LLM server is reachable, since one won't be running in most
    /// environments (including this one).
    /// </summary>
    public class ChatEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ChatEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();

            // HttpClient's default 100s timeout is fine for the two graceful-reply tests below
            // (no real LLM call), but HoroscopeChat_ViaCalculateDispatcher_ReturnsAiReply drives
            // an actual local model generation, which can legitimately take longer than 100s on
            // consumer hardware - a real generation was observed taking ~5 minutes even after
            // capping MaxTokens for local routing (see ProcessPrediction's local-LLM branch in
            // ChatAPI.cs, which now times out its own call at 10 minutes) - kept a couple minutes
            // above that server-side timeout so this client doesn't race it. Only paid when these
            // tests actually run (i.e. LM Studio is reachable); otherwise they self-skip.
            _client.Timeout = TimeSpan.FromMinutes(12);
        }

        private const string BirthTimeUrlSegment = "Location/1.3521,103.8198/Time/12:00/15/06/1990/+08:00";

        /// <summary>
        /// Short-timeout, never-throws reachability check for the configured (or default)
        /// local LLM server - must not throw so [SkippableFact] tests can call it unconditionally.
        /// 5s (not 1s) because .NET's HttpClient does proxy auto-detection on a process's first
        /// request, which was observed costing more than 1s here even though the server itself
        /// responds instantly (confirmed via a direct curl to the same URL).
        /// </summary>
        private static async Task<bool> IsLmStudioReachable()
        {
            var baseUrl = Environment.GetEnvironmentVariable("LOCAL_LLM_BASE_URL");
            if (string.IsNullOrWhiteSpace(baseUrl)) { baseUrl = "http://localhost:1234"; }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var response = await client.GetAsync(baseUrl, cts.Token);
                return true; // any response at all (even 404/error) means something is listening
            }
            catch
            {
                return false;
            }
        }

        [SkippableFact]
        public async Task HoroscopeChat_ViaCalculateDispatcher_ReturnsAiReply()
        {
            Skip.IfNot(await IsLmStudioReachable(), "LM Studio not running at LOCAL_LLM_BASE_URL");

            var url = $"/api/Calculate/HoroscopeChat/{BirthTimeUrlSegment}/UserQuestion/WillIBeRich/UserId/chat-test-user";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            var payload = (JObject)json["Payload"]!;
            Assert.False(string.IsNullOrWhiteSpace(payload["Text"]?.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(payload["SessionId"]?.ToString()));
        }

        [SkippableFact]
        public async Task HoroscopeFollowUpChat_ViaCalculateDispatcher_ReturnsGracefulReply()
        {
            Skip.IfNot(await IsLmStudioReachable(), "LM Studio not running at LOCAL_LLM_BASE_URL");

            // Chat message history is now real Postgres-backed persistence (ChatAPI.cs's
            // ChatMessage repository - previously an in-memory no-op stub). A nonexistent hash/
            // session genuinely has no record, so this proves the dispatcher returns a graceful
            // in-app reply (Pass envelope, no primary answer found) rather than a null-ref crash.
            var url = $"/api/Calculate/HoroscopeFollowUpChat/{BirthTimeUrlSegment}/FollowUpQuestion/WhyThough/PrimaryAnswerHash/nonexistent-hash/UserId/chat-test-user/SessionId/nonexistent-session";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            var payload = (JObject)json["Payload"]!;
            Assert.Contains("couldn't find", payload["Text"]!.ToString());
        }

        [SkippableFact]
        public async Task HoroscopeChatFeedback_ViaCalculateDispatcher_ReturnsGracefulReply()
        {
            Skip.IfNot(await IsLmStudioReachable(), "LM Studio not running at LOCAL_LLM_BASE_URL");

            // Same real-persistence reasoning as the follow-up test above - no record exists to
            // rate for a nonexistent hash, so this proves a graceful in-app reply rather than the
            // null-ref crash HoroscopeChatFeedback used to hit when the stubbed lookup always
            // returned null (see migration.md's chat-persistence entry).
            var url = "/api/Calculate/HoroscopeChatFeedback/AnswerHash/nonexistent-hash/FeedbackScore/1";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            var payload = (JObject)json["Payload"]!;
            Assert.Contains("couldn't find", payload["Text"]!.ToString());
        }
    }
}
