using System.Security.Cryptography;
using VedAstro.Library;

namespace API
{
    /// <summary>
    /// All API calls with no home are here, send them somewhere you think is good
    /// </summary>
    public static class GeneralAPI
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static void MapGeneralEndpoints(this WebApplication app)
        {
            // When browser visit API, they ask for FavIcon, so yeah redirect favicon from website
            app.MapGet("/api/favicon.ico", async (HttpContext context) =>
            {
                string url = URL.WebStable + "/images/favicon.ico";

                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(url);
                context.Response.ContentType = "image/x-icon";
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
            });

            // API Home page
            app.MapGet("/api/Home", async (HttpContext context) =>
            {
                ApiStatistic.Log(context.Request); //logger

                //get chart special API home page and send that to caller
                var apiHomePageTxt = await Tools.GetStringFileHttp(URL.WebStable + "/data/APIHomePage.html");

                await APITools.SendTextToCaller(apiHomePageTxt, context);
            });

            // Gets hash of VedAstro.js file located in direct azure storage
            app.MapGet("/api/GetVedAstroJSHash", async (HttpContext context) =>
            {
                //direct link to JS file without CDN
                string fileUrl = $"{URL.WebStableDirect}/js/VedAstro.js";
                string vedAstroJSHash;

                using (var response = await httpClient.GetAsync(fileUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var sha256 = SHA256.Create())
                        {
                            var hash = sha256.ComputeHash(stream);
                            vedAstroJSHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }

                await APITools.PassMessageJson(vedAstroJSHash, context);
            });

            // Backup fallback to catch invalid calls, gracefully fails.
            // NOTE: registered LAST (via MapFallback) so it only catches unmatched routes,
            // same intent as the old "z" name prefix trick under Azure Functions' routing.
            app.MapFallback(async (HttpContext context) =>
            {
                ApiStatistic.Log(context.Request); //logger

                var message = "Invalid or Outdated Call, please rebuild API URL at vedastro.org/APIBuilder.html";
                await APITools.FailMessageJson(message, context);
            });
        }
    }
}
