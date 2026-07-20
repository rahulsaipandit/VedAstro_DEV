using VedAstro.Library;
using Person = VedAstro.Library.Person;
using Newtonsoft.Json.Linq;

namespace API
{
    /// <summary>
    /// API Functions related to Person Profiles.
    ///
    /// NOTE on routes: these 6 methods have no `[Function]` attribute in the original codebase -
    /// they were only reachable through OpenAPI.Calculate's reflection dispatcher
    /// (`/api/Calculate/{calculatorName}/{*fullParamString}`), which is preserved as-is (see
    /// OpenAPI.cs's SingleAPICallData, which still includes `typeof(PersonAPI)` in its method
    /// lookup) - so AddPerson/GetPersonList/GetPersonListHash keep working through that generic
    /// route exactly like before. DeletePerson/UpdatePerson/GetPerson, however, are called by
    /// ViewComponents/Code/API/PersonTools.cs via DIRECT routes with no "Calculate/" prefix
    /// (`/api/DeletePerson/...`, `/api/UpdatePerson`, `/api/GetPerson/...`) - those get their own
    /// explicit minimal-API routes below, matching PersonTools.cs exactly.
    /// </summary>
    public static class PersonAPI
    {
        public static void MapPersonEndpoints(this WebApplication app)
        {
            // GET /api/DeletePerson/OwnerId/{ownerId}/PersonId/{personId}
            app.MapGet("/api/DeletePerson/OwnerId/{ownerId}/PersonId/{personId}", async (HttpContext context, string ownerId, string personId) =>
            {
                try
                {
                    var result = await DeletePerson(ownerId, personId);
                    await APITools.PassMessageJson(result, context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e, context);
                }
            });

            // GET /api/GetPerson/OwnerId/{ownerId}/PersonId/{personId}
            app.MapGet("/api/GetPerson/OwnerId/{ownerId}/PersonId/{personId}", async (HttpContext context, string ownerId, string personId) =>
            {
                try
                {
                    var result = await GetPerson(ownerId, personId);
                    await APITools.PassMessageJson(result, context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e, context);
                }
            });

            // POST /api/UpdatePerson  (JSON body = Person.ToJson() shape)
            app.MapPost("/api/UpdatePerson", async (HttpContext context) =>
            {
                try
                {
                    var requestJson = await APITools.ExtractDataFromRequestJson(context);
                    var person = Person.FromJson(requestJson);

                    var result = await UpdatePerson(person.OwnerId, person.Id, person.BirthTime, person.Name, person.Gender, person.Notes, person.LifeEventList);
                    await APITools.PassMessageJson(result, context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e, context);
                }
            });
        }

        #region PERSON

        /// <summary>
        /// Add new person to DB,
        /// returns ID of newly created person so caller can get use it
        /// http://localhost:7071/api/Calculate/AddPerson/OwnerId/xxx/Location/Singapore/Time/00:00/24/06/2024/+08:00/PersonName/James%20Brown/Gender/Male/Notes/%7Brodden:%22AA%22%7D
        /// </summary>
        public static async Task<string> AddPerson(string ownerId, Time birthTime, string personName, Gender gender, string notes = "", bool failIfDuplicate = false)
        {
            //don't allow add for public person's
            if (ownerId == "101") { throw new Exception("You can not add/edit public profiles with ID 101"); }

            //special ID made for human brains 🧠 (unique in whole DB)
            var brandNewHumanReadyId = await PersonManagerTools.GeneratePersonId(ownerId, personName, birthTime.StdYearText, failIfDuplicate);

            //make new person
            var newPerson = new Person(ownerId, brandNewHumanReadyId, personName, birthTime, gender, notes);

            //possible old cache of person with same id lived, so clear cache if any
            //delete data related to person (NOT USER, PERSON PROFILE)
            await ChartCache.DeleteCacheRelatedToPerson(newPerson);

            //creates record if no exist, update if already there
            await Repositories.Person.UpsertAsync(newPerson.ToAzureRow());

            //return ID of newly created person so caller can use it
            return newPerson.Id;

        }

        /// <summary>
        /// Note "Timezone not respected"
        /// </summary>
        public static async Task<string> UpdatePerson(string ownerId, string personId, Time birthTime, string personName, Gender gender, string notes = "", List<LifeEvent> lifeEventList = null)
        {
            //don't allow add for public person's
            if (ownerId == "101") { throw new Exception("You can not add/edit public profiles with ID 101"); }

            //pack the data
            //NOTE: lifeEventList must be threaded through here - previously this always defaulted to
            //an empty list (the Person ctor's default), silently wiping every person's journal
            //entries on every profile save. Found while porting the Journal page to WebsiteNative.
            var personParsed = new Person(ownerId, personId, personName, birthTime, gender, notes, lifeEventList);

            //delete data related to person (NOT USER, PERSON PROFILE)
            await ChartCache.DeleteCacheRelatedToPerson(personParsed);

            //person updated based on Person ID which is immutable
            await Repositories.Person.UpsertAsync(personParsed.ToAzureRow());

            //sync life events table to match exactly what was sent (Person.ToAzureRow() above
            //doesn't carry LifeEventList - it lives in its own table, keyed by person ID as
            //partition key - so it needs its own upsert/delete pass here or edits/deletes made
            //client-side would never actually persist).
            await SyncLifeEvents(personId, personParsed.LifeEventList);

            return "Updated!";

        }

        /// <summary>
        /// Makes Repositories.LifeEvent's rows for this person match the given list exactly:
        /// upserts everything present, deletes anything in the DB but no longer in the list.
        /// </summary>
        private static async Task SyncLifeEvents(string personId, List<LifeEvent> lifeEventList)
        {
            var existingIds = Repositories.LifeEvent.Query()
                .Where(row => row.PartitionKey == personId)
                .Select(row => row.RowKey)
                .ToList();

            var newIds = lifeEventList.Select(le => le.Id).ToHashSet();

            foreach (var removedId in existingIds.Where(id => !newIds.Contains(id)))
            {
                await Repositories.LifeEvent.DeleteAsync(personId, removedId);
            }

            foreach (var lifeEvent in lifeEventList)
            {
                await Repositories.LifeEvent.UpsertAsync(lifeEvent.ToAzureRow());
            }
        }

        /// <summary>
        /// Deletes a person's record, uses hash to identify person
        /// Note : user id is not checked here because Person hash
        /// can't even be generated by client side if you don't have access.
        /// Theoretically anybody who gets the hash of the person,
        /// can delete the record by calling this API
        /// </summary>
        public static async Task<string> DeletePerson(string ownerId, string personId)
        {
            //# get full person copy to place in recycle bin
            //query the database
            var personAzureRow = Repositories.Person.Query()
                .FirstOrDefault(row => row.PartitionKey == ownerId && row.RowKey == personId);
            var personToDelete = Person.FromAzureRow(personAzureRow);

            //# delete data related to person (NOT USER, PERSON PROFILE)
            await ChartCache.DeleteCacheRelatedToPerson(personToDelete);

            //# add deleted person to recycle bin
            //await AzureTable.PersonListRecycleBin.UpsertEntityAsync(personAzureRow);

            //# do final delete from MAIN DATABASE
            await Repositories.Person.DeleteAsync(ownerId, personId);

            //# also clean up this person's life events (own table, not cascaded automatically)
            await SyncLifeEvents(personId, new List<LifeEvent>());

            return "Updated!";

        }

        /// <summary>
        /// Gets person list, with auto swap persons from visitor to logged in account if user id is not 101
        /// if user id is 101 (guest), then visitor id can be ommited on call
        /// </summary>
        public static async Task<JArray> GetPersonList(string ownerId, string visitorId = "")
        {
            //STAGE 1 : swap visitor ID with user ID if any (data follows user when log in)
            await SwapUserId(ownerId, visitorId);

            //get raw person data from main person list (partial without life events)
            var foundCalls = Repositories.Person.Query().Where(call => call.PartitionKey == ownerId).ToList();

            //convert partial Person data to full Person with life events
            var personJsonList = new JArray();
            foreach (var call in foundCalls) { personJsonList.Add(Person.FromAzureRow(call).ToJson()); }

            //send to caller
            return personJsonList;

            //------LOCAL FUNCS-----------

            async Task SwapUserId(string ownerId, string visitorId)
            {
                //if both same no swap needed
                if (ownerId == visitorId) { return; }

                //if not yet logged in then skip
                if (ownerId == "101" || ownerId == null) { return; }

                //get all person's under visitor id
                var visitorIdPersons = Repositories.Person.Query().Where(call => call.PartitionKey == visitorId).ToList();

                //if no records, then end here
                if (!visitorIdPersons.Any()) { return; }

                //transfer each person one by one
                foreach (var personOriRecord in visitorIdPersons)
                {
                    //1: make duplicate record with new owner id
                    //overwrite visitor id with user id
                    var modifiedPerson = personOriRecord.Clone();
                    modifiedPerson.PartitionKey = ownerId;
                    await Repositories.Person.AddAsync(modifiedPerson);

                    //2: delete original "visitor" record
                    await Repositories.Person.DeleteAsync(personOriRecord.PartitionKey, personOriRecord.RowKey);
                }

            }

        }

        /// <summary>
        /// Generates hash to verify if list client has is up to date
        /// </summary>
        public static async Task<string> GetPersonListHash(string ownerId, string visitorId = "")
        {
            // Call GetPersonList to get the list of persons
            var personList = await GetPersonList(ownerId, visitorId);

            // Initialize a string to hold the concatenated data
            var concatenatedData = string.Empty;

            // Concatenate the data of all persons in the list
            foreach (var person in personList)
            {
                concatenatedData += person["PersonId"].ToString() +
                                    person["Name"].ToString() +
                                    person["BirthTime"].ToString() +
                                    person["Notes"].ToString();
            }

            // Generate a SHA256 hash of the concatenated data
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concatenatedData));

            // Convert the hash to a hexadecimal string
            var hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

            return hash;
        }

        /// <summary>
        /// Given a person id will get person's data, owner id is needed for privacy protection
        /// </summary>
        public static async Task<Person> GetPerson(string ownerId, string personId)
        {
            //get person from database matching user & owner ID (also checks shared list)
            var foundPerson = Tools.GetPersonById(personId, ownerId);

            //send person to caller
            return foundPerson;
        }

        // GetPersonImage: was already fully commented out (dead code, Bing image search + blob
        // cached avatar) before this migration - left out entirely since Azure.Storage.Blobs is gone.

        #endregion
    }
}
