using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfMovingCart.Patches
{
    class ConfigManager
    {
        public static ConfigEntry<string> goToPlayerKey;
        public static ConfigEntry<string> goToShipKey;
        public static ConfigEntry<string> goToExtractionKey;
        public static ConfigEntry<string> extractKey;

        public static ConfigEntry<string> goForwardKey;
        public static ConfigEntry<string> goBackwardsKey;
        public static ConfigEntry<string> turnRightKey;
        public static ConfigEntry<string> turnLeftKey;

        public static void Initialize(ConfigFile cfg)
        {
            goToPlayerKey = cfg.Bind<string>("Controls", "GoToPlayerKey", "f", "The key used to order the cart to come to the player.");
            goToShipKey = cfg.Bind<string>("Controls", "GoToShipKey", "g", "The key used to order the cart to go to the ship.");
            goToExtractionKey = cfg.Bind<string>("Controls", "GoToExtractionKey", "v", "The key used to order the cart to go to the extraction (Stands outside extraction).");
            extractKey = cfg.Bind<string>("Controls", "ExtractKey", "b", "The key used to order the cart to go to go inside extraction.");

            goForwardKey = cfg.Bind<string>("Controls", "GoForwardKey", "up", "The key used to order the cart to go forward.");
            goBackwardsKey = cfg.Bind<string>("Controls", "GoBackwardsKey", "down", "The key used to order the cart to go backwards.");
            turnRightKey = cfg.Bind<string>("Controls", "TurnRightKey", "right", "The key used to order the cart to turn right");
            turnLeftKey = cfg.Bind<string>("Controls", "TurnLeftKey", "left", "The key used to order the cart to turn left");
        }
    }
}
