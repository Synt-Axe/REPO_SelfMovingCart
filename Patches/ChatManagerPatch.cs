using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(ChatManager))]
    class ChatManagerPatch
    {
        public static ChatManager.ChatState chatState;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(ref ChatManager.ChatState ___chatState)
        {
            chatState = ___chatState;
        }
    }
}