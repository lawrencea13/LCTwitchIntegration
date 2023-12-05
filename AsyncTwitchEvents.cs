using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;



namespace LethalChat
{
    internal class AsyncTwitchEvents
    {


        // Channel Username: 
        // Channel ID: 

        // Client ID: 
        // Client Secret: 

        // Access Token: 
        // Refresh Token: 

        // no need to touch for now lol, got a couple of these reqs
        // quick link: https://twitchtokengenerator.com/quick/wPWEbdMclu
        // info requestor: https://twitchtokengenerator.com/request/ffx3n9o


        // not yet utilized -- nvm, never going to be utilized, INSTEAD we handle it in the handler class
        /*
        private static string clientId = "";
        private static string authToken = "";
        private static string clientSecret = "";
        private static string channelId = "";
        private static string userName = "";
        */


        #region NOTIF_DATA
        // HEADERS
        private const string TWITCH_MESSAGE_ID = "Twitch-Eventsub-Message-Id";
        private const string TWITCH_MESSAGE_TIMESTAMP = "Twitch-Eventsub-Message-Timestamp";
        private const string TWITCH_MESSAGE_SIGNATURE = "Twitch-Eventsub-Message-Signature";
        private const string MESSAGE_TYPE = "Twitch-Eventsub-Message-Type";
        // EVENTS
        private const string MESSAGE_TYPE_VERIFICATION = "webhook_callback_verification";
        private const string MESSAGE_TYPE_NOTIFICATION = "notification";
        private const string MESSAGE_TYPE_REVOCATION = "revocation";
        // HMAC
        private const string HMAC_PREFIX = "sha256=";
        //

        #endregion

        public delegate void PollCompletedEvent(string pollID);
        public delegate void ChannelPointRedeemedEvent(string rewardID);


        public static event PollCompletedEvent PollCompleted;
        public static event ChannelPointRedeemedEvent ChannelPointRedeemed;


        private static HttpListener listener;


        internal static void Initialize()
        {
            // dude I can't get this fuCKING API to work so fuck it, we ball from scratch

            Plugin.mls.LogInfo("we have started listening for BULLSHIT");
            // use asterisk instead of localhost due to local hosting issues
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");
            listener.Prefixes.Add("http://*:8080/eventsub/");
            Start();

        }


        public static void Start()
        {
            listener.Start();
            Plugin.mls.LogInfo("Listening for requests...");

            // CORRECTION: used to freeze the game, now that it is asyncronous, we g2g
            // This server essentially listens for events on the identified channel // none identified atm local testing
            Task.Run(async () => await ServerListen());

        }

        private static async Task ServerListen()
        {
            while (true)
            {
                // idk how long a good delay is, 25 ms should be fine
                await Task.Delay(25);
                var context = listener.GetContext();
                ProcessRequest(context);
            }
        }


        private static void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            using (var reader = new StreamReader(request.InputStream))
            {
                // this line of code pisses me off
                var body = reader.ReadToEnd();

                var secret = GetSecret();

                // add body to this method until I make a different way to do this, ffs
                var message = GetHmacMessage(request, body);

                var hmacCompare = GetHmac(secret, message);
                var hmac = HMAC_PREFIX + hmacCompare;                


                if (VerifyMessage(hmacCompare, request.Headers[TWITCH_MESSAGE_SIGNATURE]))
                {
                    //Plugin.mls.LogInfo("Signatures match");

                    var notification = Newtonsoft.Json.JsonConvert.DeserializeObject<TwitchNotification>(body);

                    if (MESSAGE_TYPE_NOTIFICATION.Equals(request.Headers[MESSAGE_TYPE], StringComparison.OrdinalIgnoreCase))
                    {
                        
                        Plugin.mls.LogInfo($"Event type: {notification.Subscription.Type}");
                        var jsonData = (JObject)JsonConvert.DeserializeObject(body);

                        if (notification.Subscription.Type == "channel.channel_points_custom_reward_redemption.add")
                        {
                            // convert to generic json data
                            
                            //Plugin.mls.LogInfo($"Cost: {jsonData["event"]["reward"]["cost"]}");
                            //Plugin.mls.LogInfo($"Reward ID: {jsonData["event"]["reward"]["id"]}");

                            string rewardID = jsonData["event"]["reward"]["id"].ToString();
                            string rewardCost = jsonData["event"]["reward"]["cost"].ToString();
                            ChannelPointRedeemed.Invoke(rewardID);
                            //handleEventRedeemChannelPoints(rewardID, rewardCost);

                        }
                        else if (notification.Subscription.Type == "channel.poll.end")
                        {
                            string pollID = jsonData["event"]["id"].ToString();
                            PollCompleted.Invoke(pollID);
                            //handleEventChannelPollEnded(pollID);
                        }


                        //Plugin.mls.LogInfo(Newtonsoft.Json.JsonConvert.SerializeObject(notification.Event, Newtonsoft.Json.Formatting.Indented));

                        response.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                    else if (MESSAGE_TYPE_VERIFICATION.Equals(request.Headers[MESSAGE_TYPE], StringComparison.OrdinalIgnoreCase))
                    {
                        var challenge = notification.Challenge;
                        var challengeBytes = Encoding.UTF8.GetBytes(challenge);
                        response.ContentType = "text/plain";
                        response.ContentLength64 = challengeBytes.Length;
                        response.OutputStream.Write(challengeBytes, 0, challengeBytes.Length);
                    }
                    else if (MESSAGE_TYPE_REVOCATION.Equals(request.Headers[MESSAGE_TYPE], StringComparison.OrdinalIgnoreCase))
                    {
                        Plugin.mls.LogInfo($"{notification.Subscription.Type} notifications revoked!");
                        Plugin.mls.LogInfo($"Reason: {notification.Subscription.Status}");
                        Plugin.mls.LogInfo($"Condition: {Newtonsoft.Json.JsonConvert.SerializeObject(notification.Subscription.Condition, Newtonsoft.Json.Formatting.Indented)}");

                        response.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        Plugin.mls.LogInfo($"Unknown message type: {request.Headers[MESSAGE_TYPE]}");
                    }
                }
                else
                {
                    Plugin.mls.LogInfo("403"); // Signatures didn't match. return a throbby
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                }
            }

            response.Close();
        }

        private static bool handleEventChannelPollEnded(string pollID)
        {
            // deprecated due to new system
            /*
            if(Polls.currentPollIDs.Contains(pollID))
            {
                Polls.currentPollIDs.Remove(pollID);
            }
            else
            {
                // did not create poll, return
                return false;
            }
            */


            // handle poll action based on the pollID
            return true;
        }

        private static bool handleEventRedeemChannelPoints(string eventID, string eventCost)
        {
            bool success = int.TryParse(eventCost, out int response);
            int cost;
            string id;

            if (success)
            {
                cost = response;
                id = eventID;
            }
            else
            {
                // return false as cost is a bad boi
                return false;
            }

            Plugin.mls.LogInfo($"Cost: {cost}");
            Plugin.mls.LogInfo($"ID: {id}");

            // use id and cost to handle events :)
            // return true if succesful or return false if unsuccesful, can be utilized to tell a streamer to refund the purchase or whatever

            return true;
        }

        private static string GetSecret()
        {
            // TODO: Get secret from secure storage. This is the secret you pass 
            // when you subscribed to the event.
            
            return "Still not my secret :)";
        }

        private static string GetHmacMessage(HttpListenerRequest request, string l_body)
        {
            string messageId = request.Headers[TWITCH_MESSAGE_ID];
            string timestamp = request.Headers[TWITCH_MESSAGE_TIMESTAMP];

            return messageId + timestamp + l_body;
        }

        private static string GetHmac(string secret, string message)
        {

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            //Plugin.mls.LogInfo($"Secret: {BitConverter.ToString(secretBytes)}");
            //Plugin.mls.LogInfo($"Message: {BitConverter.ToString(messageBytes)}");

            using (var hmac = new HMACSHA256(secretBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                string calculatedHmac = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Debug information
                //Plugin.mls.LogInfo($"Calculated HMAC: {calculatedHmac}");

                return calculatedHmac;
            }

        }

        // this shit is stupid man, I spent over an HOUR writing this just for it to not be all that necessary.
        // Since I have it, I'm keeping it
        public static bool VerifyMessage(string hmac, string verifySignature)
        {
            // remove prefix so we can convert hex to byte
            if (verifySignature.StartsWith(HMAC_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                verifySignature = verifySignature.Substring(HMAC_PREFIX.Length);
            }


            byte[] hmacBytes = HexStringToByteArray(hmac);
            byte[] verifySignatureBytes = HexStringToByteArray(verifySignature);

            return TimingSafeEqual(hmacBytes, verifySignatureBytes);
        }

        private static bool TimingSafeEqual(byte[] a, byte[] b)
        {
            // ESSENTIALLY check lenths and nulls, then check the mf bytes to see the difference
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int result = 0;

            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                // do not continue mf
                throw new ArgumentException("Invalid hexadecimal string");

            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
        
    }

    
    public class TwitchNotification
    {
        public string Challenge { get; set; }
        public TwitchSubscription Subscription { get; set; }
        public object Event { get; set; }
    }

    public class TwitchSubscription
    {
        public string Type { get; set; }
        public string Status { get; set; }
        public object Condition { get; set; }
    }
    
        
}
