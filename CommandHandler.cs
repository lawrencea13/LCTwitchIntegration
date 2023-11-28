using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalChat
{
    internal class CommandHandler : MonoBehaviour
    {
        void Awake()
        {
            // register method
            Plugin.Instance.ChatMessageListeners += ChatMessageListener;
        }


        void ChatMessageListener(string username, string message)
        {
            Plugin.mls.LogInfo($"{username}: {message}");
        }


    }
}
