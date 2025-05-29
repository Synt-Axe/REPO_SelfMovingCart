using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(InputManager))]
    class InputManagerPatch
    {
        [HarmonyPatch("GetMovement")]
        [HarmonyPrefix]
        static bool GetMovementPatch(ref Vector2 __result)
        {
            if (PlayerControllerPatch.cartControlMode)
            {
                __result = Vector2.zero;
                return false;
            }
            return true;
        }

        [HarmonyPatch("GetMovementX")]
        [HarmonyPrefix]
        static bool GetMovementXPatch(ref float __result)
        {
            if (PlayerControllerPatch.cartControlMode)
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        [HarmonyPatch("GetMovementY")]
        [HarmonyPrefix]
        static bool GetMovementYPatch(ref float __result)
        {
            if (PlayerControllerPatch.cartControlMode)
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        public static float GetMovementX()
        {
            Dictionary<InputKey, InputAction> inputActions = ReflectionHelper.GetPrivateField<Dictionary<InputKey, InputAction>>(InputManager.instance, "inputActions");

            if (inputActions.TryGetValue(InputKey.Movement, out var value))
            {
                return value.ReadValue<Vector2>().x;
            }
            return 0f;
        }

        public static float GetMovementY()
        {
            Dictionary<InputKey, InputAction> inputActions = ReflectionHelper.GetPrivateField<Dictionary<InputKey, InputAction>>(InputManager.instance, "inputActions");

            if (inputActions.TryGetValue(InputKey.Movement, out var value))
            {
                return value.ReadValue<Vector2>().y;
            }
            return 0f;
        }
    }
}
