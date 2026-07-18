using System.Text.Json.Nodes;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace API
{
    public static class SignInAPI
    {
        public static void MapSignInEndpoints(this WebApplication app)
        {
            app.MapGet("/api/SignInGoogle/Token/{token}", async (HttpContext context, string token) =>
            {
                try
                {
                    //validate the token & get data to id the user
                    var validPayload = await GoogleJsonWebSignature.ValidateAsync(token);
                    var userId = validPayload.Subject; //Unique Google User ID
                    var userName = validPayload.Name;
                    var userEmail = validPayload.Email;

                    //using email update or add (if it doesn't exist) NOTE: only updates name and ID
                    await AddOrUpdateUserData(userId, userName, userEmail);

                    //send back to caller only name and id
                    var userDataForWebClient = new JObject();
                    userDataForWebClient["Name"] = userName;
                    userDataForWebClient["Id"] = userId;

                    await APITools.PassMessageJson(userDataForWebClient, context);
                }
                //if any failure, reply as in valid login & log the event
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson("Login Failed", context);
                }
            });

            app.MapGet("/api/SignInFacebook/Token/{token}", async (HttpContext context, string token) =>
            {
                try
                {
                    //validate the the token & get user data
                    var url = $"https://graph.facebook.com/me/?fields=id,name,email&access_token={token}";
                    var reply = await APITools.GetRequest(url);
                    var jsonText = await reply.Content.ReadAsStringAsync();
                    var json = JsonNode.Parse(jsonText);
                    var userId = json["id"].ToString();
                    var userName = json["name"].ToString();
                    var userEmail = json["email"].ToString();

                    //using email update or add (if it doesn't exist) NOTE: only updates name and ID
                    await AddOrUpdateUserData(userId, userName, userEmail);

                    //send back to caller only name and id
                    var userDataForWebClient = new JObject();
                    userDataForWebClient["Name"] = userName;
                    userDataForWebClient["Id"] = userId;

                    await APITools.PassMessageJson(userDataForWebClient, context);
                }
                //if any failure, reply as in valid login & log the event
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson("Login Failed", context);
                }
            });

            // Verifies a Firebase ID token (produced by WebsiteNative's Firebase Auth sign-in,
            // which itself wraps the Google/Facebook OAuth credential obtained via
            // expo-auth-session - see WebsiteNative/src/lib/firebase). Same Pass/Fail envelope
            // and AddOrUpdateUserData call as the two endpoints above, so both the old Blazor
            // site (direct Google/Facebook token verification) and the new RN app (Firebase
            // token verification) write to the same UserData table.
            app.MapGet("/api/SignInFirebase/Token/{token}", async (HttpContext context, string token) =>
            {
                try
                {
                    var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    var userId = decodedToken.Uid;
                    var userName = decodedToken.Claims.TryGetValue("name", out var nameClaim) ? nameClaim?.ToString() : null;
                    var userEmail = decodedToken.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;

                    await AddOrUpdateUserData(userId, userName ?? userEmail ?? userId, userEmail ?? "");

                    var userDataForWebClient = new JObject();
                    userDataForWebClient["Name"] = userName ?? userEmail ?? userId;
                    userDataForWebClient["Id"] = userId;

                    await APITools.PassMessageJson(userDataForWebClient, context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson("Login Failed", context);
                }
            });

            app.MapMethods("/api/FacebookDeauthorize", new[] { "GET", "POST" }, async (HttpContext context) =>
            {
                //TODO change URL in FB
                //facebook pings this when user Deauthorize facebook login
                //https://api.vedastro.org/FacebookDeauthorize

                ApiStatistic.Log(context.Request); //logger

                await APITools.PassMessageJson(context);
            });
        }


        //--------------------PRIVATE FUNC---------------------------------

        /// <summary>
        /// add or update new user data to DB
        /// </summary>
        private static async Task AddOrUpdateUserData(string userId, string userName, string userEmail)
        {
            // Package data
            var userData = new UserData(userId, userName, userEmail);

            // Add/update user data
            await Repositories.UserData.UpsertAsync(userData.ToAzureRow());
        }


    }
}
