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
        public static ConfigEntry<string> cartSwitchKey;

        public static ConfigEntry<string> goToPlayerKey;
        public static ConfigEntry<string> goToShipKey;
        public static ConfigEntry<string> goToExtractionKey;
        public static ConfigEntry<string> extractKey;

        public static ConfigEntry<string> cartRemoteControlModeKey;

        public static ConfigEntry<string> goForwardKey;
        public static ConfigEntry<string> goBackwardsKey;
        public static ConfigEntry<string> turnRightKey;
        public static ConfigEntry<string> turnLeftKey;

        public static void Initialize(ConfigFile cfg)
        {
            cartSwitchKey = cfg.Bind<string>("Controls", "CartSwitchKey", "r", "The key used to select whether your orders will be excuted by the nearest cart, or a particular cart.");

            goToPlayerKey = cfg.Bind<string>("Controls", "GoToPlayerKey", "f", "The key used to order the cart to come to the player.");
            goToShipKey = cfg.Bind<string>("Controls", "GoToShipKey", "g", "The key used to order the cart to go to the ship.");
            goToExtractionKey = cfg.Bind<string>("Controls", "GoToExtractionKey", "v", "The key used to order the cart to go to the extraction (Stands outside extraction).");
            extractKey = cfg.Bind<string>("Controls", "ExtractKey", "b", "The key used to order the cart to go to go inside extraction.");

            cartRemoteControlModeKey = cfg.Bind<string>("Controls", "CartRemoteControlModeKey", "capsLock", "If you hold this key, WASD keys will stop controlling the player movement and will control the cart instead (A more practical alternative to using the arrow keys).");

            goForwardKey = cfg.Bind<string>("Controls", "GoForwardKey", "upArrow", "The key used to order the cart to go forward.");
            goBackwardsKey = cfg.Bind<string>("Controls", "GoBackwardsKey", "downArrow", "The key used to order the cart to go backwards.");
            turnRightKey = cfg.Bind<string>("Controls", "TurnRightKey", "rightArrow", "The key used to order the cart to turn right");
            turnLeftKey = cfg.Bind<string>("Controls", "TurnLeftKey", "leftArrow", "The key used to order the cart to turn left");

            // Update old key names to new ones
            UpdateDeprecatedKeys();

            // Save the config file to persist changes
            cfg.Save();
        }

        private static void UpdateDeprecatedKeys()
        {
            // Dictionary of old key names to new key names
            var keyMappings = new Dictionary<string, string>
            {
                { "up", "upArrow" },
                { "down", "downArrow" },
                { "left", "leftArrow" },
                { "right", "rightArrow" }
            };

            // Check and update each arrow key
            if (keyMappings.ContainsKey(goForwardKey.Value))
            {
                goForwardKey.Value = keyMappings[goForwardKey.Value];
            }

            if (keyMappings.ContainsKey(goBackwardsKey.Value))
            {
                goBackwardsKey.Value = keyMappings[goBackwardsKey.Value];
            }

            if (keyMappings.ContainsKey(turnRightKey.Value))
            {
                turnRightKey.Value = keyMappings[turnRightKey.Value];
            }

            if (keyMappings.ContainsKey(turnLeftKey.Value))
            {
                turnLeftKey.Value = keyMappings[turnLeftKey.Value];
            }
        }
    }
}
