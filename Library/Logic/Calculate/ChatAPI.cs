using SwissEphNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using Fizzler;
using System.Collections.Concurrent;

using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections;
namespace VedAstro.Library
{
    public class PredictionSettings
    {
        public string ServerUrl { get; set; }
        public string ApiKey { get; set; }
        public double MaxTokens { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public object[] SysMessage { get; set; }
    }


    public static class ChatAPI
    {
        // NOTE: this class used to also hold a static Azure.AI.OpenAI `OpenAIClient` field here,
        // used only by 4 legacy/dead GPT-4-flavored helpers (RemoveAnyDisclaimers,
        // ImproveFinalAnswer, PickOutMostRelevantPredictions_GPT4, AnswerQuestionDirectly) that
        // had zero live callers (verified via repo-wide grep) - the live chat pipeline goes
        // through ProcessPrediction/the Cohere/Mistral/local-LLM HTTP calls below instead. Those
        // 4 dead methods were deleted (rather than ported) so Azure.AI.OpenAI could be dropped
        // from Library.csproj/API.csproj per the Postgres migration's package cleanup.

        private static List<string> followupQuestions = new List<string> { "Why?", "How?", "Tell me more..." };

        //#             +> FOLLOW-UP --> specialized lite llm call
        //#             |
        //# QUESTION ---+> GIVE FEEDBACK --> 
        //#             |
        //#             +> UNRELATED --> full llama raq synthesis



        public static async Task<string> AutoReplySillyQuestion(string userQuestion)
        {
            //# question too short
            if (userQuestion.Length < 5)
            {
                return "That's not a valid question! Please use your brain dear friendo 🧠<br>" +
                       "Ask a proper question about understanding your horoscope's predictions<br>" +
                       "Use the ready made questions as a template. 😉";
            }

            //# based on common keywords, give quick replies
            var responses = new Dictionary<List<string>, string>
            {
                {["dasa", "when", "month", "week", "day", "hour", "minute", "date", "time", "tomorrow"],
                    "My job is to interpret Horoscope text, not predict <strong>when</strong> things will happen<br>" +
                    "🎯The super accurate <strong>Life Predictor</strong> is what you need!" +
                    "1. Go to <strong>Life Predictor</strong> page<br>" +
                    "2. Click <strong>Calculate</strong>, make a cup of tea while waiting 🍵<br>" +
                    "3. Scroll down and analyse Steve, Marilyn's and Elon's chart 🧐<br>" +
                    "4. Based on that analysis, you can accurately <strong>predict your future</strong> to the week! 🗓️<br>" +
                    "Very easy, give it a try friendo 😁"

                },
                { ["fastAsker"],
                    "You're asking question like a race car 🏎️ (so fast, but going no where)<br>" +
                    "Did you even properly read the replies I've given?😅<br>" +
                    "Please take the time and understand first! 🧐" +
                    "else you're just wasting public compute resources without care."
                },
                { ["show", "zodiac", "sign", "house", "planet", "lagna"],
                    "I'm not a Super AI that can do everything.😅<br>" +
                    "To see <strong>raw astro</strong> details :<br>" +
                    "1. Go to <strong>Horoscope</strong> page<br>" +
                    "2. Click <strong>Calculate</strong> and scroll down! 🖱️<br>" +
                    "It's not that difficult friendo 😁"
                },
                { ["noFeedbackEver"],
                    "You've asked many questions. And yet <strong>not 1 feedback</strong> you gave!😅<br>" +
                    "Why friendo?<br>" +
                    "Is all my answers not worthy of feedback?<br>" +
                    "If you don't rate the quality of the answers,<br>" +
                    "how am supposed to learn and improve? (I'm not a mind reader)<br>" +
                    "Please click Good 👍 or Bad 👎 feedback button" +
                    "...so I can <strong>better serve you</strong> friendo 😁"
                },
                { ["tellFriends"],
                    "You like to chat with me. 💌<br>" +
                    "But you <strong>have not introduced me</strong> to your friends in FB, Insta or Twitter<br>" +
                    "Why friendo? 🤔<br>" +
                    "Don't you want others to be helped also?<br>" +
                    "If you don't share me with friends and family,<br>" +
                    "how is the world we share supposed to improve?<br>" +
                    "Please post about me in FB, Insta or Twitter, " +
                    "...so I can <strong>better serve you</strong> friendo 😁"
                },
            };

            //check and give reply if keyword detected
            return FindResponse(userQuestion);





            //-------LOCALS---------

            string FindResponse(string userQuestion)
            {
                foreach (var pair in responses)
                {
                    if (pair.Key.Any(keyword => userQuestion.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return pair.Value;
                    }
                }
                return "";
            }

        }

        public static async Task<JObject> SendMessageHoroscopeFollowUp(Time birthTime, string followUpQuestion = "", string primaryAnswerHash = "",
            string userId = "", string sessionId = "")
        {
            //log the follow-up first
            await SaveToTable(CreateChatMessage(sessionId, birthTime, followUpQuestion, "Human", userId));

            var noFollowUpAnyMoreOnFailure = new List<string>();

            //based on hash get full question as pure text - may be gone/never existed (bad hash,
            //expired session), so fail gracefully instead of crashing on a null dereference
            var primaryAnswerData = ReadFromTable(sessionId, primaryAnswerHash);
            if (primaryAnswerData == null)
            {
                return await PackageReply(birthTime, userId, followUpQuestion,
                    "Sorry, I couldn't find that earlier answer to follow up on anymore 🙏",
                    noFollowUpAnyMoreOnFailure, sessionId);
            }

            var primaryAnswer = primaryAnswerData.Text;

            //based on primary answer, back track to primary question
            var primaryQuestionMsgNumber = primaryAnswerData.MessageNumber - 1; // go up 1 step
            var primaryQuestionData = ReadFromTableByMessageNumber(sessionId, primaryQuestionMsgNumber);
            if (primaryQuestionData == null)
            {
                return await PackageReply(birthTime, userId, followUpQuestion,
                    "Sorry, I couldn't find the original question for that answer anymore 🙏",
                    noFollowUpAnyMoreOnFailure, sessionId);
            }

            var primaryQuestion = primaryQuestionData.Text;

            //get predictions used as before
            var horoscopePredictions = ChatAPI.GetPredictionAsChunks(birthTime)[0];

            //get reply from LLM 🚅
            var aiReply = await AnswerFollowUpHoroscopeQuestion_CohereCommandRPlus(primaryQuestion,
                 primaryAnswer, horoscopePredictions, followUpQuestion);

            var noFollowUpAnyMore = new List<string>(); //no follow up to a follow-up

            //note: PackageReply saves the AI reply to the log itself
            return await PackageReply(birthTime, userId, followUpQuestion, aiReply, noFollowUpAnyMore, sessionId);
        }

        public static async Task<JObject> HoroscopeChatFeedback(string answerHash, int feedbackScore)
        {

            //find answer record that user has asked to rate
            var recordFound = Repositories.ChatMessage.Query().FirstOrDefault(m => m.RowKey == answerHash);

            //answer may have aged out / never existed - reply gracefully instead of crashing
            if (recordFound == null)
            {
                return new JObject
                {
                    { "SessionId", "" },
                    { "Text", "Sorry, I couldn't find that answer to rate anymore 🙏" },
                    { "TextHtml", "Sorry, I couldn't find that answer to rate anymore 🙏" },
                    { "TextHash", Tools.GenerateId(10) },
                    { "FollowUpQuestions", new JArray() },
                    { "Commands", new JArray() }
                };
            }

            //combine rating
            recordFound.Rating += feedbackScore;

            //save back to DB
            await Repositories.ChatMessage.UpsertAsync(recordFound);

            //# say thanks 🙏
            //# NOTE: DO NOT tell the user explicitly to give more feedback
            //# psychology 101 : give them the sincere motivation to help instead -> better quality/quantity
            //#if we tell the user explicitly, we increase the probability of deterministic failure, pushing the user into 1 of 2 camps
            var aiReply =
                "Congratulation!🫡\n You have just helped improve astrology worldwide🌍\n I have now memorized your feedback,🧠\n now on all my answer will take your feedback into consideration.\n Thank you so much for the rating🙏\n";

            //<h5>🥇 AI Contributor Score : <b>{contribution_score*10}</b></h5>
            var aiHtmlReply = """
                              	
                                  Congratulation!🫡<br>
                                  You have just helped improve astrology worldwide🌍<br><br>
                                  I have now <b>memorized your feedback</b>,🧠<br>
                                  now on all my answer will take your feedback into consideration.<br><br>
                                  Thank you so much for the rating🙏<br><br>
                              """;



            var noFollowUpAnyMore = new List<string>(); //no follow up to a follow-up
            var sameSessionId = recordFound.PartitionKey; //use back same session ID
            var noFeedbackCommand = new List<string>() { "noFeedback" }; //stop the feedback on feedback loop
            var randomHash = Tools.GenerateId(10); //has to be unique else will interfere with client rendering

            //note: not routed through PackageReply - this reply isn't tied to a birth chart/question,
            //so there's no birthTime to log it under; built directly in the same shape instead
            return new JObject
            {
                { "SessionId", sameSessionId },
                { "Text", aiReply },
                { "TextHtml", aiHtmlReply },
                { "TextHash", randomHash },
                { "FollowUpQuestions", new JArray(noFollowUpAnyMore) },
                { "Commands", new JArray(noFeedbackCommand) }
            };
        }

        public static async Task<JObject> SendMessageHoroscope2(Time birthTime, string userQuestion, string sessionId, string userId)
        {
            var aiReplyText = "";

            //#1 quick auto reply silly question
            if (string.IsNullOrEmpty(aiReplyText)) { await AutoReplySillyQuestion(userQuestion); }

            //#2 llm reply about horoscope
            if (string.IsNullOrEmpty(aiReplyText)) { await AnswerHoroscopeQuestion(birthTime, userQuestion); }

            throw new NotImplementedException();
        }

        public static async Task<JObject> SendMessageHoroscope(Time birthTime, string userQuestion, string sessionId, string userId)
        {
            // If session id is empty, generate a new one
            if (string.IsNullOrEmpty(sessionId)) { sessionId = Tools.GenerateId(); }

            //save incoming message to log
            await SaveToTable(CreateChatMessage(sessionId, birthTime, userQuestion, "Human", userId));

            var replyText = "";


            //         USER QUESTION          
            //               │                
            //               ▼                
            //         #0 IS VALID?            
            //               │                
            //           ◄───┴───►            
            //#1 ELECTIONAL       #2 HOROSCOPE    


            //#0 is question valid and sane?
            //var isValid = IsQuestionValid(userQuestion, out replyText);
            //if (!isValid) { return PackageReply(userQuestion, replyText, followupQuestions); } //end here if not valid


            ////#1 is question about Electional astrology?
            //var isElectional = IsElectionalAstrology(birthTime, userQuestion, out replyText);
            //if (isElectional) { return PackageReply(userQuestion, replyText, followupQuestions); } //end here if is Electional


            //#2 answer question about Horoscope
            replyText = await IsHoroscopeAstrology(birthTime, userQuestion);

            //pack nicely and send to user (PackageReply saves the AI reply to the log itself)
            return await PackageReply(birthTime, userId, userQuestion, replyText, followupQuestions, sessionId);
        }

        /// <summary>
        /// Builds a ready-to-save ChatMessageEntity - row-key hashing and message numbering used
        /// to live in ChatMessageEntity's constructor, but that class is now a plain POCO living
        /// in VedAstro.Data (see its header comment), so this Library-side logic (Tools, Time,
        /// GetLastMessageNumberNumberFromSessionId) moved here instead.
        /// </summary>
        private static ChatMessageEntity CreateChatMessage(string sessionId, Time birthTime, string text, string sender, string userId)
        {
            var textHash = Tools.GetStringHashCodeMD5(text, 15);
            var birthTimeSimple = birthTime.ToUrl().Replace("/", "-");
            var rawRowKey = $"{textHash}{birthTimeSimple}-{Tools.GenerateId(5)}";
            var cleanRowKey = System.Text.RegularExpressions.Regex.Replace(rawRowKey, @"[^a-zA-Z0-9\-\.\/_]", "");

            //NOTE: if new session id, will return 0
            var messageNumber = GetLastMessageNumberNumberFromSessionId(sessionId);

            return new ChatMessageEntity
            {
                PartitionKey = sessionId,
                RowKey = cleanRowKey,
                UserId = userId,
                Sender = sender,
                Text = text,
                MessageNumber = messageNumber + 1 // add 1 for next message
            };
        }

        public static int GetLastMessageNumberNumberFromSessionId(string sessionId)
        {
            // Query() materializes eagerly (see EfKeyedRepository.Query()), so this stays a plain
            // synchronous call - safe to use from ChatMessageEntity's constructor.
            var recordFound = Repositories.ChatMessage?.Query()
                .Where(call => call.PartitionKey == sessionId)
                .OrderByDescending(call => call.Timestamp)
                .FirstOrDefault();

            // If no record is found, start with message number 0
            var messageNumber = recordFound?.MessageNumber ?? 0; //set 0 so caller can easily add 1 on top

            return messageNumber;
        }


        public static async Task<JObject> SendMessageMatch(Time maleBirthTime, Time femaleBirthTime, string userQuestion, string sessionId = "")
        {

            throw new NotImplementedException();

        }




        //---------------------------------------------------PRIVATE------------------------------------------------------


        /// <summary>
        /// TODO MARKED FOR OBLIVION 
        /// </summary>
        /// <param name="userQuestion"></param>
        /// <returns></returns>
        public static async Task<List<PresetQuestionEmbeddingsEntity>> FindPresetQuestionEmbeddings_CohereEmbed(string userQuestion)
        {
            //get embeddings for fresh query meat
            var rawEmbeddingsData = await GetEmbeddingsForTextList_CohereEmbed([userQuestion], "search_query");

            var allEmbeddings = rawEmbeddingsData["embeddings"];
            var embed = allEmbeddings[0];
            var queryVector = embed.Select(jv => (double)jv).ToArray();

            //NOTE: the original Azure Table version built an OData range filter over 100
            //individual Vector1..Vector100 columns, but PresetQuestionEmbeddingsEntity only ever
            //stored a single JSON-encoded Embeddings column (see CreatePresetQuestionEmbeddings_CohereEmbed
            //below) - that filter could never have matched anything even before this migration.
            //Real equivalent: pull every preset row and rank by cosine similarity in-memory instead.
            var allPresets = Repositories.PresetQuestionEmbeddings.Query().ToList();

            var recordFound = GetSimilarity(queryVector, allPresets).Values.ToList();

            return recordFound;
        }

        public static async Task LLMSearchAPICall_CohereEmbed(string searchKeywords)
        {
            //#1 EMBED QUERY
            //get embeddings for fresh query meat
            var rawEmbeddingsData = await GetEmbeddingsForTextList_CohereEmbed([searchKeywords], "search_query");

            var allEmbeddings = rawEmbeddingsData["embeddings"];
            var searchKeywordVector = allEmbeddings[0];
            var newQueryEmbeds = searchKeywordVector.Select(jv => (double)jv).ToArray();

            //#2 GET EMBED API DOCS

            var allDocsEmbeddings = Repositories.PresetQuestionEmbeddings.Query()
                .Where(e => e.PartitionKey == "APICall").ToList();


            //var allDocsEmbeddings = docsEmbedRaw["embeddings"];


            //var candidates = new List<double[]>();
            //foreach (var docEmbeddingVVV in allDocsEmbeddings)
            //{
            //    var docEmbedding = JArray.Parse(docEmbeddingVVV.Embeddings);
            //    var newQueryEmbedsgg = docEmbedding.Select(jv => (double)jv).ToArray();

            //    candidates.Add(newQueryEmbedsgg);
            //}


            // Assuming newQueryEmbeds and embeds are defined and initialized
            var similarity = GetSimilarity(newQueryEmbeds, allDocsEmbeddings);
            var methodListAll = OpenAPIMetadata.FromMethodInfoList();

            // Take top 30
            var top30List = similarity.Take(10).ToDictionary(x => x.Key, x => x.Value);

            // Take the top 30

            // Print the top 30
            //var returnList = new List<OpenAPIMetadata>();
            foreach (var item in top30List)
            {

                //var xxdx = methodListAll[item.Value.Position+1];
                Console.WriteLine($"{item.Key} = {item.Value.RowKey}");
            }

            //throw new NotImplementedException();
        }

        public static Dictionary<double, PresetQuestionEmbeddingsEntity> GetSimilarity(double[] target, List<PresetQuestionEmbeddingsEntity> candidatesList)
        {
            //get score for each row and save it in dictionary
            var finalList = new Dictionary<double, PresetQuestionEmbeddingsEntity>() { };

            foreach (var candidate in candidatesList)
            {
                //var similarityScore =  Accord.Math.Distance.Cosine(target, candidate.GetEmbeddingsArray());
                var similarityScore2 = NLPTools.CosineSimilarity(target, candidate.GetEmbeddingsArray());
                finalList.Add(similarityScore2, candidate);
            }

            // Sort dictionary into high score at top
            var sortedList = finalList.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            return sortedList;
        }

        /// <summary>
        /// creates and saves them to DB for finding later
        /// </summary>
        public static async Task CreatePresetQuestionEmbeddings_CohereEmbed()
        {

            var methodListAll = OpenAPIMetadata.FromMethodInfoList().Take(10).ToList();
            //convert to text data
            var methodInfoObjects = new List<string>();
            foreach (var metadata in methodListAll)
            {
                var packed = $"{metadata.Name}:{metadata.SearchText}"; //keep text going in clean, no need JSON structure

                methodInfoObjects.Add(packed);
            }

            var presetQuestions = methodInfoObjects.ToArray();

            //use Cohere API model to get embeddings for presets
            var rawEmbeddingsData = await GetEmbeddingsForTextList_CohereEmbed(presetQuestions, "search_document");

            //array of embeddings for each text
            var allEmbeddings = rawEmbeddingsData["embeddings"];
            var dbReadyRecords = new List<PresetQuestionEmbeddingsEntity>();

            //combine embeddings with their corresponding API call
            for (int presetQIndex = 0; presetQIndex < presetQuestions.Length; presetQIndex++)
            {
                var newRow = new PresetQuestionEmbeddingsEntity();
                //newRow.Tag = methodListAll[presetQIndex].Name; //text that made the numbers
                //newRow.RowKey = Tools.GetStringHashCodeMD5(presetQuestions[presetQIndex]);
                newRow.RowKey = methodListAll[presetQIndex].Name;
                newRow.PartitionKey = "APICall";
                newRow.Embeddings = allEmbeddings[presetQIndex].ToString(Formatting.None);

                dbReadyRecords.Add(newRow);

            }


            //STAGE 2 : SAVE TO DB
            foreach (var embedRow in dbReadyRecords)
            {
                await Repositories.PresetQuestionEmbeddings.UpsertAsync(embedRow);
            }


        }

        public static HttpClientHandler Handler { get; } = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
        };

        // This method retrieves embeddings for a list of texts using the Cohere Embed API.
        // It handles the case where the inputTextList is more than 95, as each API call can only handle 95 texts at a time.
        // It combines the results from all API calls into one JObject.
        public static async Task<JObject> GetEmbeddingsForTextList_CohereEmbed(string[] inputTextList, string inputType)
        {
            JObject finalResult = new JObject();
            var textsPerBatch = 95;

            using var client = new HttpClient(Handler)
            {
                DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereEmbedAPIKey")) },
                BaseAddress = new Uri("https://Cohere-embed-v3-english-bkovp-serverless.westus.inference.ai.azure.com/v1/embed")
            };

            for (int i = 0; i < inputTextList.Length; i += textsPerBatch)
            {
                var batch = inputTextList.Skip(i).Take(textsPerBatch).ToArray();
                var response = await MakeApiCall(client, batch, inputType);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var batchResult = JObject.Parse(result);
                    finalResult.Merge(batchResult, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                }
                else
                {
                    Console.WriteLine($"The request failed with status code: {response.StatusCode}");
                    Console.WriteLine(response.Headers.ToString());
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }

            return finalResult;
        }

        private static async Task<HttpResponseMessage> MakeApiCall(HttpClient client, string[] textArray, string inputType)
        {
            // Create the request body
            var requestBodyRaw = new
            {
                model = "embed-english-v3.0",
                texts = textArray,
                input_type = inputType,
                truncate = "NONE"
            };

            // Convert the request body to JSON and create the content for the request
            var json = JsonConvert.SerializeObject(requestBodyRaw);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Make the API call
            return await client.PostAsync("", content);
        }


        private static async Task<string> AnswerFollowUpHoroscopeQuestion_CohereCommandRPlus(string primaryQuestion, string primaryAnswer, string horoscopePredictions, string followUpQuestion)
        {


            var sysMessageArray = new[]
            {
                new
                {
                    role = "system",
                    content = $"an over confident astrologer, use context text\n" +
                              $"CONTEXT:\n\n{horoscopePredictions}"
                },
                new
                {
                    role = "user",
                    content = $"{primaryQuestion}"
                },
                new
                {
                    role = "assistant",
                    content = $"{primaryAnswer}"
                },
                new
                {
                    role = "user",
                    content = $"{followUpQuestion}?"
                },

            };

            var settings = new PredictionSettings
            {
                ServerUrl = "https://Cohere-command-r-plus-rusng-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                ApiKey = Secrets.Get("azureCohereCommandRPlusAPIKey"),
                MaxTokens = 600,
                Temperature = 0.9,
                TopP = 0.1,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }

        private static async Task<string> AnswerHoroscopeQuestion_CohereCommandRPlus(string userQuestion, string horoscopePredictions)
        {


            var sysMessageArray = new[]
            {
                new
                {
                    role = "system",
                    content = $"confident expert astrologer, based on CONTEXT text, judge based on Weight\n" +
                              $"CONTEXT:\n\n{horoscopePredictions}"
                },
                new
                {
                    role = "user",
                    content = $"{userQuestion}"
                },

            };

            var settings = new PredictionSettings
            {
                ServerUrl = "https://Cohere-command-r-plus-rusng-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                ApiKey = Secrets.Get("azureCohereCommandRPlusAPIKey"),
                MaxTokens = 600,
                Temperature = 0.7,
                TopP = 0.4,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }


        private static async Task<JObject> PackageReply(Time birthTime, string userId, string userQuestion, string aiReplyText, List<string> followUpQuestions, string sessionId, string aiReplyHtml = "", List<string> commands = null)
        {

            //save AI's reply
            var textHash = (await SaveToTable(CreateChatMessage(sessionId, birthTime, aiReplyText, "AI", userId))).RowKey;

            //using user question and LLM make answer more readable in HTML, bolding, paragraphing...etc
            //string textHtml = ChatAPI.HighlightKeywords_MistralSmall(aiReplyText, userQuestion).Result;

            //use back same if not specified custom HTML version
            string finalHtml = string.IsNullOrEmpty(aiReplyHtml) ? aiReplyText : aiReplyHtml;

            //only generate if not specified
            //string finalTextHash = string.IsNullOrEmpty(textHash) ? Tools.GetStringHashCodeMD5(aiReplyText, 15) : aiReplyHtml;
            commands ??= new List<string>();

            var reply = new JObject
            {
                { "SessionId", sessionId },
                { "Text", aiReplyText },
                { "TextHtml", finalHtml },
                { "TextHash", textHash},
                { "FollowUpQuestions", new JArray(followUpQuestions) },
                { "Commands", new JArray(commands) }
            };

            return reply;

        }

        private static ChatMessageEntity ReadFromTableByMessageNumber(string sessionId, int messageNumber)
        {
            var recordFound = Repositories.ChatMessage.Query()
                .FirstOrDefault(call => call.PartitionKey == sessionId && call.MessageNumber == messageNumber);

            return recordFound;

        }

        public static ChatMessageEntity ReadFromTable(string sessionId, string messageHash)
        {
            //NOTE: original Azure Table filter used "RowKey ge messageHash" (a range comparison,
            //since RowKey is textHash+birthTime+randomId, not just the hash) - kept identical here.
            var recordFound = Repositories.ChatMessage.Query()
                .Where(call => call.PartitionKey == sessionId)
                .FirstOrDefault(call => string.CompareOrdinal(call.RowKey, messageHash) >= 0);

            return recordFound;

        }

        public static async Task<ChatMessageEntity> SaveToTable(ChatMessageEntity inputChatMessageEntity)
        {
            await Repositories.ChatMessage.AddAsync(inputChatMessageEntity);

            return inputChatMessageEntity;
        }


        private static bool IsElectionalAstrology(Time birthTime, string userQuestion, out string replyText)
        {
            //#0 extract time range if any
            var tasks0 = new List<Task<string>>
            {
                ExtractTimeRange_MistralLarge(birthTime, userQuestion),
            };
            var timeRangeDataRaw = Task.WhenAll(tasks0).Result.FirstOrDefault(); //call all models in parallel
            var timeRangeDataJson = JObject.Parse(timeRangeDataRaw);

            //see if LLM said it is valid Muhurtha question
            bool.TryParse(timeRangeDataJson["valid"].ToString(), out bool isElectionalQuestion);

            bool isValid = false; //default to Horoscope

            if (isElectionalQuestion)
            {
                //double check if time can be extracted properly
                isValid = IsTimeRangeValid(timeRangeDataJson, out TimeRange timeRangeParsed);
            }


            //if not valid end here
            if (!isValid) { replyText = ""; return false; }
            else
            {
                //todo 
                replyText = "Muhurtha feature still being coded. Please come back next week.";
                return true;
            }
            //muhurtha question confirmed
            //generate chart
            //var chartData = EventsChartManager.GenerateEventsChartForChat(birthTime, timeRangeParsed);

            //


            //----------------------------------LOCALS---------------------------------------


            //check and parses
            bool IsTimeRangeValid(JObject timeRangeJson, out TimeRange timeRange)
            {
                try
                {
                    var startRaw = timeRangeJson["start"].Value<string>();
                    var endRaw = timeRangeJson["end"].Value<string>();

                    var start = new Time($"00:00 {startRaw} {birthTime.StdTimezoneText}", birthTime.GetGeoLocation());
                    var end = new Time($"00:00 {endRaw} {birthTime.StdTimezoneText}", birthTime.GetGeoLocation());

                    timeRange = new TimeRange(start, end);
                    return true;
                }
                catch (Exception e)
                {
                    timeRange = null;
                    return false;
                }
            }

        }

        private static async Task<string> IsHoroscopeAstrology(Time birthTime, string userQuestion)
        {
            //#0 split predictions into sizeable chunks
            var predictionListChunks = ChatAPI.GetPredictionAsChunks(birthTime);

            //#1 pick out most relevant predictions to question
            var relevantPredictions = await PickOutMostRelevantPredictions_MistralSmall(birthTime, userQuestion, predictionListChunks[0]); //call all models in parallel

            //#2 answer question based on relevant predictions
            //var answerLevel1 = await AnswerQuestionDirectly_MistralSmall(relevantPredictions, userQuestion); //call all models in parallel
            var answerLevel1 = await AnswerQuestionDirectly_CohereCommandRPlus(relevantPredictions, userQuestion); //call all models in parallel

            //#3 simplify answer
            //var answerLevel2 = await ImproveFinalAnswer_MistralLarge(answerLevel1[0], userQuestion);
            //var answerLevel2 = await ImproveFinalAnswer_MistralSmall(answerLevel1[0], userQuestion);

            //#4 remove disclaimers if any
            //todo check if contains disclaimers then remove
            //var answerLevel3 = await RemoveAnyDisclaimers_MistralLarge(answerLevel2, userQuestion);

            return answerLevel1;
        }

        private static async Task<string> AnswerHoroscopeQuestion(Time birthTime, string userQuestion)
        {
            //#0 split predictions into sizeable chunks (BV Raman's predictions, from how to judge horoscope book)
            var predictionListChunks = ChatAPI.GetPredictionAsChunks(birthTime);

            //#1 pick out most relevant predictions to question
            var relevantPredictions = await PickOutMostRelevantPredictions_MistralSmall(birthTime, userQuestion, predictionListChunks[0]); //call all models in parallel

            //#2 answer question based on relevant predictions
            var answerLevel1 = await AnswerQuestionDirectly_CohereCommandRPlus(relevantPredictions, userQuestion); //call all models in parallel

            return answerLevel1;
        }

        private static List<string> GetPredictionAsChunks(Time birthTime)
        {
            //calculate predictions for current person
            var predictionList = Tools.GetHoroscopePrediction(birthTime);

            //extract only name and description and place nicely
            // Create a new list to hold the modified prediction objects
            List<dynamic> modifiedPredictionList = new List<dynamic>();

            // Iterate over each object in the predictionList
            foreach (var prediction in predictionList)
            {
                //calculate weigh of each prediction based on shadbala
                //NOTE: sum all weights of all houses and planets
                var weight = prediction.RelatedBody.RelatedHouses.Sum(relatedHouse => Calculate.HouseStrength(relatedHouse, birthTime).ToDouble());

                //add in planets
                weight += prediction.RelatedBody.RelatedPlanets.Sum(relatedHouse => Calculate.PlanetStrength(relatedHouse, birthTime).ToDouble());

                // Create a new anonymous object with the properties you want
                var modifiedPrediction = new
                {
                    Name = prediction.FormattedName,
                    Description = prediction.Description,
                    Relevance = string.Empty,
                    Weight = weight
                };

                // Add the modified prediction object to the new list
                modifiedPredictionList.Add(modifiedPrediction);
            }

            // Sort the list by weight in descending order and remove predictions with zero weight
            modifiedPredictionList = modifiedPredictionList.OrderByDescending(prediction => prediction.Weight).ToList();
            modifiedPredictionList.RemoveAll(prediction => prediction.Weight == 0);


            // Convert the list of modified prediction objects to a JSON string
            var jsonString = JsonConvert.SerializeObject(modifiedPredictionList, Formatting.None);

            var chunkedList = new List<string>() { jsonString };

            return chunkedList;
        }

        private static async Task<string> AnswerQuestionDirectly_MetaLlama3(string answerLevelN, string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            var sysMessage =
                $"analyse life description text, answer question directly\n" +
                $"QUESTION:\n{userQuestion}\n" +
                $"LIFE DESCRIPTION:\n{answerLevelN}";

            // Remove invalid characters
            sysMessage = System.Text.RegularExpressions.Regex.Unescape(sysMessage);

            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = sysMessage
                        }
                    },
                    max_tokens = 2000,
                    temperature = 0.8,
                    top_p = 0.1,
                    best_of = 1,
                    presence_penalty = 0,
                    use_beam_search = "false",
                    ignore_eos = "false",
                    skip_special_tokens = "false",
                    logprobs = "false"
                };

                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureMetaLlama3APIKey"));
                client.BaseAddress = new Uri("https://Meta-Llama-3-70B-Instruct-ydbrc-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static async Task<string> AnswerQuestionDirectly_MistralSmall(string answerLevelN, string userQuestion)
        {

            var sysMessageArray = new[]
            {
                new
                {
                    role = "system",
                    content = $"as expert astrologer analyse life description text, concise answer, based on relevance and weight answer question with reason\n" +
                              $"QUESTION:\n{userQuestion}" +
                              $"\nLIFE DESCRIPTION:\n{answerLevelN}"
                }
            };

            var settings = new PredictionSettings
            {
                ServerUrl = "https://Mistral-small-xcvuv-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                //TryGet: a missing cloud key is fine when LOCAL_LLM_BASE_URL routes this to a local LLM instead (see ProcessPrediction)
                ApiKey = Secrets.TryGet("azureMistralSmallAPIKey"),
                MaxTokens = 600,
                Temperature = 0.5,
                TopP = 0.2,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }

        private static async Task<string> AnswerQuestionDirectly_CohereCommandRPlus(string answerLevelN, string userQuestion)
        {

            var sysMessageArray = new[]
            {
                new
                {
                    role = "user",
                    content = $"as expert astrologer analyse life description text, concise answer, based on relevance and weight answer question with reason\n" +
                              $"QUESTION:\n{userQuestion}" +
                              $"\nLIFE DESCRIPTION:\n{answerLevelN}"
                }
            };

            var settings = new PredictionSettings
            {
                ServerUrl = "https://Cohere-command-r-plus-rusng-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                //TryGet: a missing cloud key is fine when LOCAL_LLM_BASE_URL routes this to a local LLM instead (see ProcessPrediction)
                ApiKey = Secrets.TryGet("azureCohereCommandRPlusAPIKey"),
                MaxTokens = 600,
                Temperature = 0.5,
                TopP = 0.5,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }

        private static async Task<string> HighlightKeywords(string userQuestion, string answerLevel3)
        {
            return answerLevel3;
            throw new NotImplementedException();
        }

        private static async Task<string> HighlightKeywords_MistralLarge(string answerText, string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };


            //var sysMessage = "Provide a confident answer without any disclaimers for the following text:\n" +
            //                 $"```TEXT\n{answerLevel1}```";
            var sysMessage = "Follow rules:\n" +
                             "1. Output ANSWER text in HTML format for use between <p> tag element\n" +
                             "2. Highlight relevant words and phrases in ANSWER that is related to QUESTION\n" +
                             "3. Break long text and organize ANSWER text structure for easy readability." +
                             "4. All html element must be valid to be placed inside <p> tag element\n";


            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role= "system",
                            content= sysMessage
                        },
                        new
                        {
                            role= "user",
                            content= $"QUESTION:\n\n{userQuestion}\n\nANSWER:\n\n{answerText}"
                        },

                    },
                    max_tokens = 300,
                    temperature = 0.8,
                    top_p = 0.4,
                    safe_prompt = "false"
                };


                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureMistralLargeAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static async Task<string> HighlightKeywords_MistralSmall(string answerText, string userQuestion)
        {
            var sysMessage =
                             "1. Output ANSWER text in HTML format for use between <p> tag element\n" +
                             "2. Highlight relevant words and phrases in ANSWER that is related to QUESTION\n" +
                             "3. Break long text and organize ANSWER text structure for easy readability." +
                             "4. All html element must be valid to be placed inside <p> tag element\n\n";


            var sysMessageArray = new[]
            {
                new
                {
                    role = "system",
                    content = "bold and paragraph structure DESCRIPTION text as HTML based on relevance to TOPIC text"
                },
                new
                {
                    role = "user",
                    content = @"{TOPIC:""Quantum Computing"", DESCRIPTION:""Quantum Computing uses principles of quantum mechanics to process information. It’s expected to revolutionize computing by performing complex calculations quickly.""}"
                },
                new
                {
                    role = "assistant",
                    content = "<p><strong>Quantum Computing</strong> uses principles of <strong>quantum mechanics</strong> to process information.<br> It's expected to revolutionize computing by performing complex calculations quickly. </p>"
                },
                new
                {
                    role = "user",
                    content = @"{TOPIC:""Artificial Intelligence"", DESCRIPTION:""Artificial Intelligence (AI) refers to the simulation of human intelligence in machines that are programmed to think like humans and mimic their actions.""}"
                },
                new
                {
                    role = "assistant",
                    content = @"<p>This term refers to the simulation of <strong>human intelligence in machines</strong>.<br>  These machines are <strong>programmed to think</strong> like humans and mimic their actions, <br> which is a significant advancement in the field of technology.</p>"
                },
                new
                {
                    role = "user",
                    content = @$"{{TOPIC:""{userQuestion}"", DESCRIPTION:""{answerText}""}}"
                },
            };

            var settings = new PredictionSettings
            {
                ServerUrl = "https://Cohere-command-r-plus-rusng-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                ApiKey = Secrets.Get("azureCohereCommandRPlusAPIKey"),
                //ServerUrl = "https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                //ApiKey = azureMistralLargeAPIKey,
                //ServerUrl = "https://Mistral-small-xcvuv-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                //ApiKey = azureMistralSmallAPIKey,
                MaxTokens = 8196,
                Temperature = 0.4,
                TopP = 0.1,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }

        private static async Task<string> RemoveAnyDisclaimers_MistralLarge(string answerLevel1, string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };


            var sysMessage = "Provide a confident answer without any disclaimers for the following text:\n" +
                             $"```TEXT\n{answerLevel1}```";


            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = sysMessage
                        }
                    },
                    max_tokens = 300,
                    temperature = 0.8,
                    top_p = 0.4,
                    safe_prompt = "false"
                };


                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        public static async Task<string> RemoveAnyDisclaimers_CohereEmebed(Time birthTime, string userQuestion, string sessionId, string userId)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                  (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            string result = "";

            using (var client = new HttpClient(handler))
            {
                // Request data goes here
                // input_type — Specifies the type of document to be embedded. At the time of writing, there are four options:
                // 
                // search_document: For documents against which search is performed
                // search_query: For query documents
                // classification: For when the embeddings will be used as an input to a text classifier
                // clustering: For when you want to cluster the embeddings

                //  --data-raw '{
                //   "model": "embed-english-v3.0",
                //   "inputs": ["How can I set up a 3rd party contribution to my RRSP?", "Where can I find my unused RRSP contribution?", "How do I link my return with my partners?", "Do I need to complete a return if I moved to Canada this year?", "Can I set up a business account on your platform?", "Do you offer self-directed and managed investments?", "What is the current interest rate in your savings account?"],
                //   "examples": [{"label": "Savings accounts (chequing & savings)", "text": "I want to set up a recurring monthly transfer between my chequing and savings account.  How do I do this?"}, {"label": "Savings accounts (chequing & savings)", "text": "I would like to add my wife to my current chequing account so it'"'"'s a joint account. What do I need to do?"}, {"label": "Savings accounts (chequing & savings)", "text": "Can I set up automated payment for my bills?"}, {"label": "Savings accounts (chequing & savings)", "text": "Interest rates are going up - does this impact the interest rate in my savings account?"}, {"label": "Savings accounts (chequing & savings)", "text": "What is the best option for a student savings account?"}, {"label": "Investments", "text": "My family situation is changing and I need to update my risk profile for my equity investments"}, {"label": "Investments", "text": "Where can I see the YTD return in my investment account?"}, {"label": "Investments", "text": "How can I change my beneficiaries of my investment accounts?"}, {"label": "Investments", "text": "Is crypto an option for my investment account?"}, {"label": "Investments", "text": "How often do you rebalance your investment portfolios?"}, {"label": "Investments", "text": "What is the monthly fee on the investment accounts?"}, {"label": "Investments", "text": "How can I withdraw funds from my investment account?"}, {"label": "Investments", "text": "Can I buy stocks and ETFs listed on non Canadian exchanges?"}, {"label": "Taxes", "text": "How can I minimize my tax exposure?"}, {"label": "Taxes", "text": "I\"m going to be late filing my ${currentYear - 1} tax returns. Is there a penalty?"}, {"label": "Taxes", "text": "I'"'"'m going to have a baby in November - what happens to my taxes?"}, {"label": "Taxes", "text": "How can I see my ${currentYear - 2} tax assessment?"}, {"label": "Taxes", "text": "When will I get my tax refund back?"}, {"label": "Taxes", "text": "How much does it cost to use your tax filing platform?"}, {"label": "RRSP", "text": "I'"'"'d like to increase my monthly RRSP contributions to my RRSP"}, {"label": "RRSP", "text": "I want to take advantage of the First Time Home Buyers program and take money out of my RRSP.  How does the program work?"}, {"label": "RRSP", "text": "What is the ${currentYear} RRSP limit?"}, {"label": "RRSP", "text": "Does your system ensure I won'"'"'t overcontribute to my RRSP?"}, {"label": "RRSP", "text": "How do I set up employer contributions to my RRSP"}]
                // }'

                var requestBodyRaw = new
                {
                    texts = new string[] { "NATO Parliamentary Assembly declares Russia to be a ‘terrorist state’", "Fifa and Qatar in urgent talks after Wales rainbow hats confiscated | Fifa and the Qataris were in talks on the matter on Tuesday, where Fifa reminded their hosts of their assurances before the tournament that everyone was welcome and rainbow flags would be allowed.", "Qatar Bans Beer Sales at World Cup Stadiums", "Biden calls 'emergency' meeting after missile hits Poland" },
                    input_type = "classification",
                    truncate = "NONE"
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBodyRaw);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Replace this with the primary/secondary key, AMLToken, or Microsoft Entra ID token for the endpoint
                const string apiKey = "vM3mewMokdtgovcDOWcCLztwj0IBWlc4";
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("A key should be provided to invoke the endpoint");
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri("https://Cohere-embed-v3-english-bkovp-serverless.westus.inference.ai.azure.com/v1/embed");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Result: {0}", result);
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                }
            }


            return result;
        }

        private static async Task<string> ImproveFinalAnswer_MistralLarge(string answerLevelN, string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };


            var sysMessage =
                $"summarize below raw answer as reply to this question, '{userQuestion}?'\n" +
                $"summary is easier to understand,'\n" +
                $"RAW ANSWER:\n{answerLevelN}";


            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = sysMessage
                        }

                    },
                    max_tokens = 300,
                    temperature = 0.8,
                    top_p = 0.4,
                    safe_prompt = "false"
                };


                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)
                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static async Task<string> ImproveFinalAnswer_MistralSmall(string answerLevelN, string userQuestion)
        {
            var handler = CreateHttpClientHandler();
            var sysMessage = PrepareSystemMessage();
            var requestBody = CreateRequestBody(sysMessage);
            var content = new StringContent(requestBody);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var client = new HttpClient(handler))
            {
                HttpResponseMessage response = await PostRequestAsync(client, content);
                return await ProcessResponseAsync(response);
            }


            //------------------------------- LOCALS --------------

            HttpClientHandler CreateHttpClientHandler()
            {
                return new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
                };
            }

            string PrepareSystemMessage()
            {
                var sysMessage =
                    $"summarize below raw answer as reply to this question, '{userQuestion}?'\n" +
                    $"summary is easier to understand,'\n" +
                    $"RAW ANSWER:\n{answerLevelN}";

                return System.Text.RegularExpressions.Regex.Unescape(sysMessage);
            }

            string CreateRequestBody(string sysMessage)
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = sysMessage
                }
            },
                    max_tokens = 300,
                    temperature = 0.8,
                    top_p = 0.5,
                    safe_prompt = "false"
                };

                return JsonConvert.SerializeObject(requestBodyObject);
            }

            async Task<HttpResponseMessage> PostRequestAsync(HttpClient client, StringContent content)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-small-xcvuv-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                return await client.PostAsync("", content);
            }

            async Task<string> ProcessResponseAsync(HttpResponseMessage response)
            {
                if (response.IsSuccessStatusCode)
                {
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    return fullReply.Choices.FirstOrDefault().Message.Content;
                }
                else
                {
                    Console.WriteLine($"The request failed with status code: {response.StatusCode}");
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static async Task<string> PickOutMostRelevantPredictions_MetaLlama3(Time birthTime, string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            //calculate predictions for current person
            var predictionList = Tools.GetHoroscopePrediction(birthTime);

            //convert prediction to text 
            var predictJson = Tools.ListToJson(predictionList);
            var predictText = predictJson.ToString(Formatting.None);


            //prepare LLM call
            var sysMessage = "Output only JSON.\n" +
                             //"From the below horoscope predictions list in JSON format.\n" +
                             $"Return all predictions that is relevant to the question, '{userQuestion}'." +
                             "Sort based on relevance, most relevant at top\n" +
                             $"```JSON\n{predictText}```";

            // Remove invalid characters
            sysMessage = System.Text.RegularExpressions.Regex.Unescape(sysMessage);

            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = sysMessage
                        }
                    },
                    max_tokens = 3000,
                    temperature = 0.8,
                    top_p = 0.1,
                    best_of = 1,
                    presence_penalty = 0,
                    use_beam_search = "false",
                    ignore_eos = "false",
                    skip_special_tokens = "false",
                    logprobs = "false"
                };

                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Meta-Llama-3-70B-Instruct-ydbrc-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)
                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static async Task<string> PickOutMostRelevantPredictions_MistralLarge(Time birthTime, string userQuestion, string predictText)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };



            //prepare LLM call
            var sysMessage = "Output only JSON.\n" +
                             //"From the below horoscope predictions list in JSON format.\n" +
                             $"Return all predictions that is relevant to the question, '{userQuestion}'." +
                             "Sort based on relevance, most relevant at top\n" +
                             $"```JSON\n{predictText}```";

            // Remove invalid characters
            sysMessage = System.Text.RegularExpressions.Regex.Unescape(sysMessage);

            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = sysMessage
                        }
                    },
                    max_tokens = 8192,
                    temperature = 0.8,
                    top_p = 0.1,
                    safe_prompt = "false"
                };


                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)
                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static async Task<string> PickOutMostRelevantPredictions_MistralSmall(Time birthTime, string userQuestion, string predictText)
        {

            var sysMessageArray = new[]
            {
                new
                {
                    role = "system",
                    content =
                        $"Output JSON.\n" +
                        $"Filter DESCRIPTION relevant to the QUESTION.\n" +
                        $"Judge based on weight.\n" +
                        $"Sort based on relevance, most relevant at top.\n\n"

                },
                new
                {
                    role = "user",
                    content = "QUESTION:{userQuestion}" +
                              "LIFE DESCRIPTION:{descriptionText}"
                },
                new
                {
                    role = "assistant",
                    content = @"{name:""{predictionName}"", description:""{description}"", relevance:""{relevanceScore}"", weight:""{weightScore}""}"
                },
                new
                {
                    role = "user",
                    content = $"QUESTION:\n{userQuestion}\n" +
                              $"LIFE DESCRIPTION:\n{predictText}"
                },

            };



            var settings = new PredictionSettings
            {
                ServerUrl = "https://Mistral-small-xcvuv-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                //TryGet: a missing cloud key is fine when LOCAL_LLM_BASE_URL routes this to a local LLM instead (see ProcessPrediction)
                ApiKey = Secrets.TryGet("azureCohereCommandRPlusAPIKey"),
                MaxTokens = 8196,
                Temperature = 0.5,
                TopP = 0.5,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }

        private static async Task<string> PickOutMostRelevantPredictions_CohereCommandR(Time birthTime, string userQuestion, string predictText)
        {

            var sysMessageArray = new[]
            {
                new
                {
                    role = "system",
                    content =
                        $"Output JSON.\n" +
                        $"Filter DESCRIPTION relevant to the QUESTION.\n" +
                        $"Judge based on weight.\n" +
                        $"Sort based on relevance, most relevant at top.\n\n"

                },
                new
                {
                    role = "user",
                    content = "QUESTION:{userQuestion}" +
                              "LIFE DESCRIPTION:{descriptionText}"
                },
                new
                {
                    role = "assistant",
                    content = @"{name:""{predictionName}"", description:""{description}"", relevance:""{relevanceScore}"", weight:""{weightScore}""}"
                },
                new
                {
                    role = "user",
                    content = $"QUESTION:\n{userQuestion}\n" +
                              $"LIFE DESCRIPTION:\n{predictText}"
                },

            };



            var settings = new PredictionSettings
            {
                ServerUrl = "https://Cohere-command-r-plus-rusng-serverless.westus.inference.ai.azure.com/v1/chat/completions",
                ApiKey = Secrets.Get("azureCohereCommandRPlusAPIKey"),
                MaxTokens = 20000,
                Temperature = 1,
                TopP = 1,
                SysMessage = sysMessageArray
            };


            //make call to LLM, NOTE : high time consumption in chain
            var llmReply = await ProcessPrediction(settings);

            return llmReply;

        }

        private static async Task<string> ProcessPrediction(PredictionSettings settings)
        {
            var handler = CreateHttpClientHandler();

            string localModel = null;
            TimeSpan? localTimeout = null;
            // NOTE: this used to be gated behind `#if DEBUG`, which meant it only ever routed to
            // a local LLM in Debug builds - Release-configured environments (e.g. CI running
            // integration tests) always ignored LOCAL_LLM_BASE_URL. Made unconditional so it
            // works the same way regardless of build configuration; behavior is unchanged when
            // the env var isn't set (falls through to whatever settings.ServerUrl/ApiKey were
            // already, i.e. the cloud endpoint).
            var localLlmBase = Environment.GetEnvironmentVariable("LOCAL_LLM_BASE_URL");
            if (!string.IsNullOrEmpty(localLlmBase))
            {
                settings.ServerUrl = localLlmBase.TrimEnd('/') + "/chat/completions";
                settings.ApiKey = Environment.GetEnvironmentVariable("LOCAL_LLM_API_KEY") ?? "local-llm";
                localModel = Environment.GetEnvironmentVariable("LOCAL_LLM_MODEL");

                // Verified against a real LM Studio instance (RTX 5070, GPU acceleration confirmed
                // active via nvidia-smi during generation - this is not a CPU-fallback issue): a
                // MaxTokens budget as large as 8196 (PickOutMostRelevantPredictions_MistralSmall's
                // relevance-filtering step) reliably took 5+ minutes to generate locally regardless
                // of model size (reproduced with both a 26B and a 9B model) - it's the requested
                // output length, not the model, that's the bottleneck at normal local-GPU decode
                // speeds. The cloud endpoints this was tuned for have much higher throughput, so
                // this cap only applies when actually routed to a local server - production/cloud
                // behavior (MaxTokens as configured per call site) is unchanged.
                if (settings.MaxTokens > 2048) { settings.MaxTokens = 2048; }

                //local reasoning models are much slower than cloud endpoints and can burn most of
                //their token budget on hidden "reasoning_content" before ever emitting a real answer -
                //the default HttpClient.Timeout of 100s is routinely too short for this. Raised from
                //300s: even with the MaxTokens cap above, a large input prompt (e.g. a full
                //horoscope prediction list, ~5,000+ tokens) still needs a slow prefill pass before
                //generation starts at all.
                localTimeout = TimeSpan.FromSeconds(600);
                Console.WriteLine($"[ChatAPI] routing LLM call to {settings.ServerUrl} (local MaxTokens={settings.MaxTokens}, timeout={localTimeout})");
            }

            var requestBody = CreateRequestBody(settings.SysMessage, settings.MaxTokens, settings.Temperature, settings.TopP, localModel);
            var content = new StringContent(requestBody);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var client = new HttpClient(handler))
            {
                if (localTimeout.HasValue) { client.Timeout = localTimeout.Value; }
                HttpResponseMessage response = await PostRequestAsync(client, content, settings.ServerUrl, settings.ApiKey);
                return await ProcessResponseAsync(response);
            }
        }

        private static string CreateRequestBody(object[] sysMessage, double maxTokens, double temperature, double topP, string model = null)
        {
            //local OpenAI-compatible servers (e.g. Ollama) require "model" in the body to pick which loaded model answers;
            //Azure's serverless endpoints bind the model via the URL itself, so this is only added when routed locally
            if (!string.IsNullOrEmpty(model))
            {
                var localRequestBodyObject = new
                {
                    model = model,
                    messages = sysMessage,
                    max_tokens = maxTokens,
                    temperature = temperature,
                    top_p = topP,
                    safe_prompt = "false"
                };

                return JsonConvert.SerializeObject(localRequestBodyObject);
            }

            var requestBodyObject = new
            {
                messages = sysMessage,
                max_tokens = maxTokens,
                temperature = temperature,
                top_p = topP,
                safe_prompt = "false"
            };

            return JsonConvert.SerializeObject(requestBodyObject);
        }

        private static async Task<HttpResponseMessage> PostRequestAsync(HttpClient client, StringContent content, string serverUrl, string apiKey)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.BaseAddress = new Uri(serverUrl);

            return await client.PostAsync("", content);
        }


        private static HttpClientHandler CreateHttpClientHandler() =>
            new()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
            };


        private static async Task<string> ProcessResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                string fullReplyRaw = await response.Content.ReadAsStringAsync();
                var fullReply = new LlamaReplyJson(fullReplyRaw);

                return fullReply.Choices.FirstOrDefault().Message.Content;
            }
            else
            {
                Console.WriteLine($"The request failed with status code: {response.StatusCode}");
                Console.WriteLine(response.Headers.ToString());

                string responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
        }


        private static async Task<string> ExtractTimeRange_MistralLarge(Time birthTime, string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```will i get married in 2014?```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", start:""01/01/2014"", end:""31/12/2014""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```will i get married in this year?```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", start:""01/01/{thisYear}"", end:""31/12/{thisYear}""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```when will i get married?```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", start:""01/01/{birthYear+18}"", end:""31/12/{birthYear+50}""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```should i do business next year?```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", start:""01/01/{nextYear}"", end:""31/12/{nextYear}""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```when will i get a job?```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", start:""01/01/{birthYear+18}"", end:""31/12/{birthYear+40}""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```describe my life chart```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", start:""null"", end:""null""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```Are there any financial highs and lows predicted in my birth chart from the year 2024 to 2027?```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", start:""01/01/2024"", end:""31/12/2027""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```My DOB 6th Jan 1982 , Time 5:15 AM and Location Nagpur Maharastra India , I need my job switch information```"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", start:""null"", end:""null""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"extract time range from this question : ```{userQuestion}```"
                        },

                    },
                    max_tokens = 8192,
                    temperature = 0.8,
                    top_p = 0.1,
                    safe_prompt = "false"
                };


                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)
                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
        }

        private static bool IsQuestionValid(string userQuestion, out string replyText)
        {
            // Get the result from the CheckQuestionValidity method
            var tasks1 = new List<Task<string>>
            {
                CheckQuestionValidity_MistralLarge(userQuestion),
            };
            var validityCheckResult = Task.WhenAll(tasks1).Result.FirstOrDefault(); //call all models in parallel


            // Parse the result into a JObject
            var parsedResult = JObject.Parse(validityCheckResult);

            // Extract the "valid" field value as a string
            var isValidText = parsedResult["valid"]?.Value<string>() ?? "false";

            // Convert the string to a boolean
            bool.TryParse(isValidText, out bool isQuestionValid);

            // Extract the "reply" field value as a string, if it exists
            // If it doesn't exist, assign an empty string
            replyText = parsedResult["reply"]?.Value<string>() ?? "";

            // Return the validity of the question
            return isQuestionValid;
        }

        private static async Task<string> CheckQuestionValidity_MistralLarge(string userQuestion)
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            using (var client = new HttpClient(handler))
            {
                var requestBodyObject = new
                {
                    messages = new[]
                    {

                        new
                        {
                            role = "user",
                            content = $"hi"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""yes hi, please ask questions""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"how do you work?"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""i've analysed predictions about your horoscope, and can answer questions about it""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"hi"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""yes hi, let's talk about your horoscope""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"I want to learn astrology"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""i'm here to help you understand a particular horoscope, not teach you astrology text, sorry""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"what can you do?"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""i'm here to help you understand a particular horoscope""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"how to generate vedic astro chart "
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""to get help on using vedastro please contact us, i'm here to help you understand a particular horoscope""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"my birth details are date/month/year at location"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""i don't need your birth details, i've already analysed your horoscope, please ask questions directly""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"can you create my chart based on Jan 1, 2001, Delhi 10am"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""false"", reply:""i don't need your birth details, i've already analysed your horoscope, please ask questions directly""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"describe yogas in my life"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", reply:""""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"character of modi"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", reply:""""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"wil he win in 2024 elections ?"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", reply:""""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"Can you tell me about money flow throughout the year 2024?"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", reply:""""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"Do my stars indicate any entrepreneurial talents or inclinations?"
                        },
                        new
                        {
                            role = "assistant",
                            content = @"{valid:""true"", reply:""""}"
                        },
                        new
                        {
                            role = "user",
                            content = $"{userQuestion}"
                        },

                    },
                    max_tokens = 8192,
                    temperature = 1, //max creativity
                    top_p = 1, //max creativity
                    safe_prompt = "false"
                };


                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.Get("azureCohereCommandRPlusAPIKey"));
                client.BaseAddress = new Uri("https://Mistral-large-cahy-serverless.westus.inference.ai.azure.com/v1/chat/completions");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    //get full reply and parse it
                    string fullReplyRaw = await response.Content.ReadAsStringAsync();
                    var fullReply = new LlamaReplyJson(fullReplyRaw);

                    //return only message text
                    var replyMessage = fullReply.Choices.FirstOrDefault().Message.Content;
                    return replyMessage;
                }
                else
                {
                    //TODO better logging
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }

        }

    }
}
