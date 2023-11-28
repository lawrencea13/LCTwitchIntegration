using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;


namespace LethalChat
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "Poseidon.LethalChat.TwitchIntegration";
        private const string modName = "LethalChat";
        private const string modVersion = "0.0.1";

        private readonly Harmony harmony = new Harmony(modGUID);
        internal static ManualLogSource mls;

        internal static Plugin Instance;

        internal string CLIENTID;
        internal string CLIENTSECRET;





        #region IRCCHATHANDLING

        internal TcpClient twitchClient;
        internal StreamReader reader;
        internal StreamWriter writer;

        private Queue<string> linesToSend;

        private int numLinesSent;
        private int numLinesPerInterval = 15;
        private float interval = 30;
        private float intervalRemaining;

        public string username, password, channelName;

        public delegate void ChatMessageListener(string source, string parameters);
        public event ChatMessageListener ChatMessageListeners;

        #endregion


        void Awake()
        {
            this.gameObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo($"Loaded {modName}.");

            AsyncTwitchEvents.Initialize();


            /*
            #region IRCSETUP
#if DEBUG
            password = "";
            username = "";
            channelName = "";


#endif

            

            Connect();
            GameObject cmdHndlr = new UnityEngine.GameObject("CommandHandler");
            UnityEngine.Object.DontDestroyOnLoad(cmdHndlr);
            cmdHndlr.hideFlags = HideFlags.HideAndDontSave;
            cmdHndlr.AddComponent<CommandHandler>();



            // create send message queue to avoid rate limiting
            linesToSend = new Queue<string>();

            #endregion
            */
        }

      

        void Update()
        {
            /*
            #region IRCHANDLING
            if (!twitchClient.Connected)
            {
                Connect();
            }

            ReadChat();
            SendQueuedMessages();

            #endregion
            */
            //mls.LogInfo("running stupid update task");
            //AsyncTwitchEvents.Update();


        }

        private void OnDestroy()
        {
            // disconnect if destroyed to avoid connectivity issues in the future
            // including when the game is stopped
            Disconnect();
        }


        #region IRCFUNCTIONS



        private void Connect()
        {
            try
            {
                twitchClient = new TcpClient("irc.chat.twitch.tv", 6667);
                reader = new StreamReader(twitchClient.GetStream());
                writer = new StreamWriter(twitchClient.GetStream());

                writer.WriteLine("PASS oauth:" + password);
                writer.WriteLine("NICK " + username);
                writer.WriteLine("USER  " + username + " 8 * :" + username);
                writer.WriteLine("JOIN #" + channelName);
                writer.Flush();

                numLinesSent = 3;
                intervalRemaining = interval;
            }
            catch (Exception e)
            {
                Disconnect();
                mls.LogError($"We were unable to connect: {e.Message}");
            }
           
        }

        private void Disconnect()
        {
            twitchClient?.Close();
            reader?.Close();
            writer?.Close();
        }

        private void ReadChat()
        {
            if(twitchClient.Available > 0 || reader.Peek() > 0)
            {
                var message = reader.ReadLine();

                ProcessLine(message);
            }
        }

        private void ProcessLine(string line)
        {
            // parse the message into sub parts
            var (command, source, parameters) = ParseMessage(line);

            // do something based on the command
            if(command == "PING")
            {
                SendLine("PONG " + parameters, true);
                numLinesSent++;
            }
            else if(command == "PRIVMSG")
            {
                ChatMessageListeners?.Invoke(source, parameters);
            }
            //else
            //{
            //    mls.LogInfo($"Unrecognized command: {command}");
            //}
        }


        private void SendLine(string line, bool sendNow = false)
        {
            if (sendNow)
            {
                writer.WriteLine(line);
                writer.Flush();
            }
            else
            {
                // add to queue
                linesToSend.Enqueue(line);
            }
        }

        private void SendQueuedMessages()
        {
            // called every update
            intervalRemaining -= Time.deltaTime;

            if(intervalRemaining < 0)
            {
                // reset interval

                numLinesSent = 0;
                intervalRemaining = interval;
            }


            bool didWriteLines = false;

            
            while(numLinesSent < numLinesPerInterval && linesToSend.Count > 0)
            {
                didWriteLines = true;
                writer.WriteLine(linesToSend.Dequeue());
                numLinesSent++;
            }

            if (didWriteLines) { writer.Flush(); } // only flush if lines wrote
        }


        // I sure as hell didn't write this lmao
        private (string, string, string) ParseMessage(string message)
        {
            string command;
            string source = null;
            string parameters = null;

            // starting index
            int idx = 0;

            string rawCommandComponent;
            string rawSourceComponent = null;
            string rawParametersComponent = null;

            int endIdx;

            if (message[idx] == '@')
            {
                endIdx = message.IndexOf(' ');

                idx = endIdx + 1;
            }

            if (message[idx] == ':')
            {
                idx += 1;
                endIdx = message.IndexOf(' ', idx);
                rawSourceComponent = message.Substring(idx, endIdx - idx);
                idx = endIdx + 1;
            }

            endIdx = message.IndexOf(":", idx);
            if(endIdx == -1)
            {
                endIdx = message.Length;
            }
            rawCommandComponent = message.Substring(idx, endIdx - idx).Trim();

            if(endIdx != message.Length)
            {
                idx = endIdx + 1;
                rawParametersComponent = message.Substring(idx);
            }

            command = ParseCommand(rawCommandComponent);
            if(command != null)
            {
                source = ParseSource(rawSourceComponent);

                parameters = rawParametersComponent?.Trim();
            }

            return (command, source, parameters);

        }

        private string ParseCommand(string rawCommandComponent) 
        {
            if (string.IsNullOrEmpty(rawCommandComponent))
            {
                return null;
            }

            var commandParts = rawCommandComponent.Split(' ');
            return commandParts[0];
        }

        private string ParseSource(string rawSourceComponent)
        {
            if(string.IsNullOrEmpty(rawSourceComponent))
            {
                return null;
            }
            string[] sourceParts = rawSourceComponent.Split('!');
            if(sourceParts.Length == 2) { return sourceParts[0]; }
            else { return null; }

        }

        #endregion
    }
}
