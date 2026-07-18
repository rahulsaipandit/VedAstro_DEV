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
        }

        private const string BirthTimeUrlSegment = "Location/1.3521,103.8198/Time/12:00/15/06/1990/+08:00";

        /// <summary>
        /// Short-timeout, never-throws reachability check for the configured (or default)
        /// local LLM server - must not throw so [SkippableFact] tests can call it unconditionally.
        /// </summary>
        private static async Task<bool> IsLmStudioReachable()
        {
            var baseUrl = Environment.GetEnvironmentVariable("LOCAL_LLM_BASE_URL");
            if (string.IsNullOrWhiteSpace(baseUrl)) { baseUrl = "http://localhost:1234"; }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
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
        public async Task HoroscopeFollowUpChat_ViaCalculateDispatcher_FailsGracefully()
        {
            Skip.IfNot(await IsLmStudioReachable(), "LM Studio not running at LOCAL_LLM_BASE_URL");

            // NOTE: chat message history persistence (ChatAPI.cs's chatTableClient) is an
            // intentional in-memory no-op stub post-migration (see its doc comment) - so a
            // follow-up can never actually find its primary answer by hash. This proves the
            // dispatcher still fails *gracefully* (Fail envelope, HTTP 200) rather than crashing -
            // see final report for this as a flagged TODO, not something fixed here.
            var url = $"/api/Calculate/HoroscopeFollowUpChat/{BirthTimeUrlSegment}/FollowUpQuestion/WhyThough/PrimaryAnswerHash/nonexistent-hash/UserId/chat-test-user/SessionId/nonexistent-session";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
        }

        [SkippableFact]
        public async Task HoroscopeChatFeedback_ViaCalculateDispatcher_FailsGracefully()
        {
            Skip.IfNot(await IsLmStudioReachable(), "LM Studio not running at LOCAL_LLM_BASE_URL");

            // Same stubbed chat-history limitation as the follow-up test above - no record ever
            // exists to rate, so this proves graceful failure rather than an unhandled exception.
            var url = "/api/Calculate/HoroscopeChatFeedback/AnswerHash/nonexistent-hash/FeedbackScore/1";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
        }
    }
}
