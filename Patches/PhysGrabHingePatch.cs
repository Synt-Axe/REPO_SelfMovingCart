using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(PhysGrabHinge))]
    class PhysGrabHingePatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePatch(PhysGrabHinge __instance)
        {
            __instance.gameObject.AddComponent<DoorTracker>();
        }

        [HarmonyPatch("HingeBreakImpulse")]
        [HarmonyPostfix]
        static void HingeBreakImpulsePatch(PhysGrabHinge __instance)
        {
            __instance.GetComponent<DoorTracker>().isBroken = true;
        }

        [HarmonyPatch("DestroyHinge")]
        [HarmonyPostfix]
        static void DestroyHingePatch(PhysGrabHinge __instance)
        {
            __instance.GetComponent<DoorTracker>().isDestroyed = true;
        }
    }

    class DoorTracker : MonoBehaviour
    {
        public bool isBroken = false;
        public bool isDestroyed = false;
        void Start()
        {
            isBroken = false;
            isDestroyed = false;
        }
    }
}
