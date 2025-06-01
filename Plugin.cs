using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SelfMovingCart.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfMovingCart
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class SelfMovingCartBase : BaseUnityPlugin
    {
        private const string modGUID = "Syntaxe.SelfMovingCart";
        private const string modName = "Self Moving Cart";
        private const string modVersion = "1.0.0";


        private readonly Harmony harmony = new Harmony(modGUID);

        private static SelfMovingCartBase Instance;

        public static ManualLogSource mls;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            ConfigManager.Initialize(((BaseUnityPlugin)this).Config);
            mls.LogInfo($"{modGUID} is now awake!");

            harmony.PatchAll(typeof(SelfMovingCartBase));
            harmony.PatchAll(typeof(ExtractionPointPatch));
            harmony.PatchAll(typeof(PhysGrabCartPatch));
            harmony.PatchAll(typeof(PlayerControllerPatch));
            harmony.PatchAll(typeof(RoundDirectorPatch));
            harmony.PatchAll(typeof(TruckHealerPatch));
            harmony.PatchAll(typeof(PhysGrabHingePatch));
            harmony.PatchAll(typeof(ChatManagerPatch));
            harmony.PatchAll(typeof(InputManagerPatch));
            harmony.PatchAll(typeof(CartModeUI)); // <-- Added: Patch for Cart Mode UI
        }
    }
}
