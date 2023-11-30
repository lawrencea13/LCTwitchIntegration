using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LethalChat
{
    internal class Polls
    {

        internal static void CreatePoll(string authorization, string clientID, string channelID, string title, List<string> titles, int duration, bool channelPointVoting, int costPerVote)
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

            
            Task.Run(async () => await CreatePollAsync(clientID, authorization, pollRequest));
        }
        private static async Task CreatePollAsync(string clientID, string auth, PollRequest jsonData)
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
                            Plugin.mls.LogInfo("Poll created successfully.");
                        }
                        else
                        {
                            Plugin.mls.LogInfo($"Error: {response.StatusCode} - {response.ReasonPhrase}");

                            string responseContent = await response.Content.ReadAsStringAsync();
                            Plugin.mls.LogInfo($"Response Content: {responseContent}");
                        }
                    }


                }

            }
            catch (Exception ex)
            {
                Plugin.mls.LogInfo($"Error: {ex.Message}");
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
