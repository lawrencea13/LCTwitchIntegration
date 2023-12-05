using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalChat
{
    // handles the creation and deletion of custom rewards
    internal class CustomRewards
    {

        internal static string createReward(string channelID, string clientID, string auth, string title, int cost, bool generateID)
        {
            return Task.Run(async () => await createRewardAsync(channelID, clientID, auth, title, cost, generateID)).Result;
        }

        internal static bool deleteReward(string rewardID, string channelID, string clientID, string auth)
        {
            return Task.Run(async () => await deleteRewardAsync(rewardID, channelID, clientID, auth)).Result;
        }


        // private version, use non async version to create it
        private static async Task<string> createRewardAsync(string channelID, string clientID, string auth,string Title, int Cost, bool generateID)
        {
            string finalString = string.Empty;
            if(generateID)
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var stringChars = new char[36];
                var random = new System.Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    if(i == 8  || i == 13 || i == 18 || i == 23)
                    {
                        // add hyphens at correct spots to imitate a normally generated ID
                        stringChars[i] = '-';
                    }
                    else
                    {
                        stringChars[i] = chars[random.Next(chars.Length)];
                    }
                }
                finalString = new String(stringChars);
            }

#if DEBUG
            if(finalString != string.Empty)
            {
                Plugin.mls.LogInfo(finalString);
            }
            else
            {
                Plugin.mls.LogInfo("HA you're an idiot");
            }
#endif

            string apiUrl = "https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=" + channelID;



            using (HttpClient client = new HttpClient())
            {
                string jsonBody;
                if (finalString != string.Empty)
                {
                    // Request data
                    var requestData = new
                    {
                        title = Title,
                        cost = Cost,
                        id = finalString
                    };
                    jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                }
                else
                {
                    // Request data
                    var requestData = new
                    {
                        title = Title,
                        cost = Cost
                    };
                    jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                }                

                // Set up headers
                client.DefaultRequestHeaders.Add("client-id", clientID);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + auth);
                //client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Create StringContent with the correct content type
                using (HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    // Send the POST request
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Custom reward created successfully.");
                        string body = await response.Content.ReadAsStringAsync();

                        var responseBody = (JObject)JsonConvert.DeserializeObject(body);

                        if(finalString != string.Empty)
                        {
                            return finalString;
                        }
                        else
                        {
                            return (string)responseBody["data"]["id"];
                        }
                        
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                        
                        // Print the response content for further debugging
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response Content: {responseContent}");
                        return string.Empty;
                    }
                }
            }



        }

        private static async Task<bool> deleteRewardAsync(string rewardID, string channelID, string clientID, string auth)
        {
            // delete a reward
            string apiUrl = $"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={channelID}&id={rewardID}";

            using (HttpClient client = new HttpClient())
            {
                // Set up headers
                client.DefaultRequestHeaders.Add("Client-Id", clientID);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + auth);

                // Send the DELETE request
                HttpResponseMessage response = await client.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Custom reward deleted successfully.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");

                    // Print the response content for further debugging
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response Content: {responseContent}");
                    if (response.StatusCode.Equals(404))
                    {
                        Console.WriteLine("The reward couldn't be deleted since it wasn't found, if it still exists, the content creator should delete it");
                        Console.WriteLine("If the reward still existed, please contact iminxx on discord");

                        return false;
                    }
                    return false;
                }
            }
        }
    }
}
