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
        private const string modVersion = "0.0.2";

        private readonly Harmony harmony = new Harmony(modGUID);
        internal static ManualLogSource mls;

        internal static Plugin Instance;

        TwitchHandler API;


        void Awake()
        {
            this.gameObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo($"Loaded {modName}.");

            API = new TwitchHandler();

            API.Initialize();
        }


       
        private void OnDestroy()
        {
            API.cleanup();
        }
    }
}
