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
                if (WasButtonPressedThisFrame(ConfigManager.cartSwitchKey.Value))
                {
                    PhysGrabCartPatch.SwitchCarts();
                }

                /**************************************************/
                /****************** Cart Control ******************/
                /**************************************************/
                // To Player
                if (WasButtonPressedThisFrame(ConfigManager.goToPlayerKey.Value))
                {
                    PhysGrabCartPatch.OrderCart(0, __instance.transform.position);
                }

                // To Ship
                if (WasButtonPressedThisFrame(ConfigManager.goToShipKey.Value))
                {
                    PhysGrabCartPatch.OrderCart(1, __instance.transform.position);
                }

                // To Near Extraction
                if (WasButtonPressedThisFrame(ConfigManager.goToExtractionKey.Value))
                {
                    PhysGrabCartPatch.OrderCart(2, __instance.transform.position);
                }

                // To Inside Extraction
                if (WasButtonPressedThisFrame(ConfigManager.extractKey.Value))
                {
                    PhysGrabCartPatch.OrderCart(3, __instance.transform.position);
                }
            }

            /****************************************************/
            /****************** Remote Control ******************/
            /****************************************************/
            // Arrows
            bool isUpArrow = IsButtonPressed(ConfigManager.goForwardKey.Value);
            bool isDownArrow = IsButtonPressed(ConfigManager.goBackwardsKey.Value);
            bool isLeftArrow = IsButtonPressed(ConfigManager.turnLeftKey.Value);
            bool isRightArrow = IsButtonPressed(ConfigManager.turnRightKey.Value);

            // WASD
            cartControlMode = IsButtonPressed(ConfigManager.cartRemoteControlModeKey.Value);

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



        static List<string> mouseBtns = new List<string>() { "leftButton", "rightButton", "middleButton", "forwardButton", "backButton" };
        static bool WasButtonPressedThisFrame(string btn)
        {
            InputControl val;
            if (mouseBtns.Contains(btn))
            {
                val = ((InputControl)Mouse.current)[btn];
            }
            else
            {
                val = ((InputControl)Keyboard.current)[btn];
            }

            return ((ButtonControl)val).wasPressedThisFrame;
        }

        static bool IsButtonPressed(string btn)
        {
            InputControl val;
            if (mouseBtns.Contains(btn))
            {
                val = ((InputControl)Mouse.current)[btn];
            }
            else
            {
                val = ((InputControl)Keyboard.current)[btn];
            }

            return ((ButtonControl)val).isPressed;
        }
    }
}
