using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(PlayerController))]
    class PlayerControllerPatch
    {
        static bool lastFrameHadMovement = false;
        static CartSelfMovementManager closestCart = null;

        public static bool cartControlMode = false;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(PlayerController __instance)
        {
            if (ChatManagerPatch.chatState != ChatManager.ChatState.Active) // Ignore input when player is writing in chat.
            {

                /**************************************************/
                /****************** Cart Switch ******************/
                /**************************************************/
                InputControl valSwitch = ((InputControl)Keyboard.current)[ConfigManager.cartSwitchKey.Value];
                if (((ButtonControl)valSwitch).wasPressedThisFrame)
                {
                    PhysGrabCartPatch.SwitchCarts();
                }

                /**************************************************/
                /****************** Cart Control ******************/
                /**************************************************/
                // To Player
                InputControl val1 = ((InputControl)Keyboard.current)[ConfigManager.goToPlayerKey.Value];
                if (((ButtonControl)val1).wasPressedThisFrame)
                {
                    PhysGrabCartPatch.OrderCart(0, __instance.transform.position);
                }

                // To Ship
                InputControl val2 = ((InputControl)Keyboard.current)[ConfigManager.goToShipKey.Value];
                if (((ButtonControl)val2).wasPressedThisFrame)
                {
                    PhysGrabCartPatch.OrderCart(1, __instance.transform.position);
                }

                // To Near Extraction
                InputControl val3 = ((InputControl)Keyboard.current)[ConfigManager.goToExtractionKey.Value];
                if (((ButtonControl)val3).wasPressedThisFrame)
                {
                    PhysGrabCartPatch.OrderCart(2, __instance.transform.position);
                }

                // To Inside Extraction
                InputControl val4 = ((InputControl)Keyboard.current)[ConfigManager.extractKey.Value];
                if (((ButtonControl)val4).wasPressedThisFrame)
                {
                    PhysGrabCartPatch.OrderCart(3, __instance.transform.position);
                }
            }

            /****************************************************/
            /****************** Remote Control ******************/
            /****************************************************/
            // Arrows
            InputControl valUp = ((InputControl)Keyboard.current)[ConfigManager.goForwardKey.Value];
            InputControl valDown = ((InputControl)Keyboard.current)[ConfigManager.goBackwardsKey.Value];
            InputControl valLeft = ((InputControl)Keyboard.current)[ConfigManager.turnLeftKey.Value];
            InputControl valRight = ((InputControl)Keyboard.current)[ConfigManager.turnRightKey.Value];

            bool isUpArrow = ((ButtonControl)valUp).isPressed;
            bool isDownArrow = ((ButtonControl)valDown).isPressed;
            bool isLeftArrow = ((ButtonControl)valLeft).isPressed;
            bool isRightArrow = ((ButtonControl)valRight).isPressed;

            // WASD
            InputControl valCartControlModeKey = ((InputControl)Keyboard.current)[ConfigManager.cartRemoteControlModeKey.Value];
            cartControlMode = ((ButtonControl)valCartControlModeKey).isPressed;

            if (cartControlMode)
            {
                isUpArrow = isUpArrow || InputManagerPatch.GetMovementY() > 0f;
                isDownArrow = isDownArrow || InputManagerPatch.GetMovementY() < 0f;
                isLeftArrow = isLeftArrow || InputManagerPatch.GetMovementX() < 0f;
                isRightArrow = isRightArrow || InputManagerPatch.GetMovementX() > 0f;
            }

            bool madeMovement = isUpArrow || isDownArrow || isRightArrow || isLeftArrow;

            if(madeMovement || lastFrameHadMovement)
            {
                CartSelfMovementManager tempClosestCart = PhysGrabCartPatch.GetCartToOrder(__instance.transform.position);
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
