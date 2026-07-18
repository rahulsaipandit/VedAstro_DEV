using System;
using System.Linq;

namespace VedAstro.Library;

public static class CallTracker
{

    public static bool IsRunning(string callerId)
    {

        try
        {
            var found = Repositories.CallTracker.Query()
                .Where(call => call.PartitionKey == callerId)
                .FirstOrDefault();

            //if old call found check if running else default false
            var foundIsRunning = found?.IsRunning ?? false;

            return foundIsRunning;
        }
        catch (Exception e)
        {
            //APILogger.Error(e); //log it

#if DEBUG
            Console.WriteLine($"FAILURE!!! : {e.Message} /n {e.StackTrace}");
#endif
            return false;
        }

    }

    /// <summary>
    /// Marks the call as running
    /// </summary>
    public static void CallStart(string callerId)
    {
        //set the call as running
        CallStatusEntity customerEntity = new CallStatusEntity()
        {
            PartitionKey = callerId,
            RowKey = "",
            IsRunning = true
        };

        //creates record if no exist, update if already there
        Repositories.CallTracker.UpsertAsync(customerEntity).GetAwaiter().GetResult();

    }

    /// <summary>
    /// Marks the call as not running
    /// </summary>
    public static void CallEnd(string callerId)
    {
        //set the call as running
        CallStatusEntity customerEntity = new()
        {
            PartitionKey = callerId,
            RowKey = "",
            IsRunning = false //mark as done
        };

        //creates record if no exist, update if already there
        Repositories.CallTracker.UpsertAsync(customerEntity).GetAwaiter().GetResult();
    }

    /// <summary>
    /// clear the record for the call
    /// </summary>
    public static void DeleteCall(string callerId)
    {
        // Query for the entity to be deleted
        var entityToDelete = Repositories.CallTracker.Query()
            .Where(call => call.PartitionKey == callerId)
            .FirstOrDefault();

        if (entityToDelete != null)
        {
            // Delete the entity
            Repositories.CallTracker.DeleteAsync(entityToDelete.PartitionKey, entityToDelete.RowKey).GetAwaiter().GetResult();
        }

    }

}
