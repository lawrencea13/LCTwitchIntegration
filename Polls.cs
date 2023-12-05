using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;

namespace LethalChat
{
    internal class Polls
    {
        //internal static List<string> currentPollIDs = new List<string>();


        public static string CreatePoll(string authorization, string clientID, string channelID, string title, List<string> titles, int duration, bool channelPointVoting, int costPerVote)
        {
            List<Choice> pollChoices = new List<Choice>();
            foreach (string choiceTitle in titles) { pollChoices.Add(new Choice { Title = choiceTitle }); }

            PollRequest pollRequest = new PollRequest
            {
                BroadcasterId = channelID,
                Title = title,
                Choices = pollChoices,
                Duration = duration,
                ChannelPointsVotingEnabled = channelPointVoting,
                ChannelPointsPerVote = costPerVote
            };

            // no need to handle here, returns empty if failed
            return Task.Run(async () => await CreatePollAsync(clientID, authorization, pollRequest)).Result;
        }
        private static async Task<string> CreatePollAsync(string clientID, string auth, PollRequest jsonData)
        {
            string apiUrl = "https://api.twitch.tv/helix/polls";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + auth);
                    client.DefaultRequestHeaders.Add("Client-Id", clientID);

                    string jsonBody = JsonConvert.SerializeObject(jsonData);

                    using (HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                    {
                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            // I can't test this shit but if my understanding is correct based on the docs
                            // response.Content should contain a json object as a string that has data as the main point then id under that
                            Plugin.mls.LogInfo("Poll created successfully.");
                            
                            string body = await response.Content.ReadAsStringAsync();
                            
                            var responseBody = (JObject)JsonConvert.DeserializeObject(body);

                            return (string)responseBody["data"]["id"];
                            //currentPollIDs.Add((string)responseBody["data"]["id"]);
                            // we also have direct access to the PollRequest object which allows us to use that to get the choices and store them
                            // so when we go to use the response from the poll, we don't need to parse the data, we can just store the data here
                            // then access it later

                        }
                        else
                        {
                            Plugin.mls.LogInfo($"Error: {response.StatusCode} - {response.ReasonPhrase}");

                            string responseContent = await response.Content.ReadAsStringAsync();
                            Plugin.mls.LogInfo($"Response Content: {responseContent}");

                            return string.Empty;
                        }
                    }


                }

            }
            catch (Exception ex)
            {
                Plugin.mls.LogInfo($"Error: {ex.Message}");
                return string.Empty;
            }
        }
    }

    internal class PollRequest
    {
        [JsonProperty("broadcaster_id")]
        public string BroadcasterId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("channel_points_voting_enabled")]
        public bool ChannelPointsVotingEnabled { get; set; }

        [JsonProperty("channel_points_per_vote")]
        public int ChannelPointsPerVote { get; set; }
    }

    internal class Choice
    {
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
