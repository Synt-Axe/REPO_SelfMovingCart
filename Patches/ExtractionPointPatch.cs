using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(ExtractionPoint))]
    class ExtractionPointPatch
    {
        public static List<Transform> extractionPoints = new List<Transform>();
        public static List<ExtractionPoint.State> extractionStates = new List<ExtractionPoint.State>();


        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void ExtractionPointStartPatch(ExtractionPoint __instance, ref ExtractionPoint.State ___currentState)
        {
            extractionPoints.Add(__instance.transform);
            extractionStates.Add(___currentState);

            SelfMovingCartBase.mls.LogInfo($"Extraction position: {__instance.transform.position}, rotation: {__instance.transform.rotation}");
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void ExtractionPointUpdatePatch(ExtractionPoint __instance, ref ExtractionPoint.State ___currentState)
        {
            int ind = extractionPoints.IndexOf(__instance.transform);
            extractionStates[ind] = ___currentState;
        }
    }
}
