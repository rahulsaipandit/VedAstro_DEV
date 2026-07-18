using VedAstro.Library;

namespace API
{
    /// <summary>
    /// Group of API calls related to user's API subscription
    /// </summary>
    public static class SubscriptionAPI
    {
        public static void MapSubscriptionEndpoints(this WebApplication app)
        {
            app.MapGet("/api/RegisterSubscription/OwnerId/{ownerId}/APIKey/{apiKey}/", async (HttpContext context, string ownerId, string apiKey) =>
            {
                try
                {
                    // Search for existing record by OwnerId (PartitionKey)
                    var existingRecord = Repositories.UserData.Query()
                        .FirstOrDefault(row => row.PartitionKey == ownerId);

                    if (existingRecord != null)
                    {
                        // Update API key if record exists
                        existingRecord.APIKey = apiKey;
                        await Repositories.UserData.UpsertAsync(existingRecord);
                    }
                    else
                    {
                        // Return error if no record found
                        await APITools.FailMessageJson("No record found for specified OwnerId", context);
                        return;
                    }

                    await APITools.PassMessageJson("API key updated successfully", context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e.Message, context);
                }
            });
        }
    }
}
