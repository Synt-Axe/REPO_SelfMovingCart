using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(PlayerController))]
    class PlayerControllerPatch
    {
        static bool lastFrameHadMovement = false;
        static CartSelfMovementManager closestCart = null;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(PlayerController __instance)
        {
            if (ChatManagerPatch.chatState != ChatManager.ChatState.Active) // Ignore input when player is writing in chat.
            {
                /**************************************************/
                /****************** Cart Control ******************/
                /**************************************************/
                // To Player
                if (Input.GetKeyDown(ConfigManager.goToPlayerKey.Value))
                {
                    PhysGrabCartPatch.OrderNearestCart(0, __instance.transform.position);
                }

                // To Ship
                if (Input.GetKeyDown(ConfigManager.goToShipKey.Value))
                {
                    PhysGrabCartPatch.OrderNearestCart(1, __instance.transform.position);
                }
                // To Near Extraction
                if (Input.GetKeyDown(ConfigManager.goToExtractionKey.Value))
                {
                    PhysGrabCartPatch.OrderNearestCart(2, __instance.transform.position);
                }
                // To Inside Extraction
                if (Input.GetKeyDown(ConfigManager.extractKey.Value))
                {
                    PhysGrabCartPatch.OrderNearestCart(3, __instance.transform.position);
                }
            }

            // Remote control
            bool isUpArrow = Input.GetKey(ConfigManager.goForwardKey.Value);
            bool isDownArrow = Input.GetKey(ConfigManager.goBackwardsKey.Value);
            bool isLeftArrow = Input.GetKey(ConfigManager.turnLeftKey.Value);
            bool isRightArrow = Input.GetKey(ConfigManager.turnRightKey.Value);

            bool madeMovement = isUpArrow || isDownArrow || isRightArrow || isLeftArrow;

            if(madeMovement || lastFrameHadMovement)
            {
                CartSelfMovementManager tempClosestCart = PhysGrabCartPatch.GetClosestCart(__instance.transform.position);
                if (tempClosestCart != null)
                {
                    if(tempClosestCart != closestCart && closestCart != null) // If this is a different cart from the one we were controlling before.
                        closestCart.cartTargetSync.MoveCart(false, false, false, false); // Order old cart to stop.

                    closestCart = tempClosestCart;
                    closestCart.cartTargetSync.MoveCart(isUpArrow, isDownArrow, isRightArrow, isLeftArrow);

                    if (!madeMovement)
                        lastFrameHadMovement = false;
                    else
                        lastFrameHadMovement = true;
                } else
                {
                    closestCart = null;
                }
            } else
            {
                closestCart = null;
            }
        }
    }
}
