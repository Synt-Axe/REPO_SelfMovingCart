using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(RoundDirector))]
    class RoundDirectorPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch()
        {
            // Resetting carts list.
            PhysGrabCartPatch.carts.Clear();

            ExtractionPointPatch.extractionPoints.Clear();
            ExtractionPointPatch.extractionStates.Clear();
        }
    }
}
