using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(TruckHealer))]
    class TruckHealerPatch
    {
        public static Vector3 truckPosition;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void TruckHealerStartPatch(TruckHealer __instance)
        {
            truckPosition = new Vector3(__instance.transform.position.x, 0, __instance.transform.position.z);
        }
    }
}
