using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static HurtCollider;
using static UnityEngine.GraphicsBuffer;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(PhysGrabCart))]
    class PhysGrabCartPatch
    {
        public static List<CartSelfMovementManager> carts = new List<CartSelfMovementManager>();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void CartStartPatch(PhysGrabCart __instance)
        {
            // Small carts are not included.
            if (__instance.isSmallCart) return;

            CartSelfMovementManager cart = __instance.gameObject.AddComponent<CartSelfMovementManager>();
            carts.Add(cart);
        }

        [HarmonyPatch("FixedUpdate")]
        [HarmonyPostfix]
        static void CartFixedUpdatePatch(PhysGrabCart __instance, ref bool ___cartBeingPulled)
        {
            // Small carts are not included.
            if (__instance.isSmallCart) return;

            CartSelfMovementManager cart = __instance.gameObject.GetComponent<CartSelfMovementManager>();
            if (cart == null)
            {
                carts.Remove(cart);
                return;
            }
            cart.isCartBeingPulled = ___cartBeingPulled;
        }

        public static CartSelfMovementManager GetClosestCart(Vector3 position)
        {
            //SelfMovingCartBase.mls.LogInfo($"Number of carts: {carts.Count}.");
            CartSelfMovementManager closestCart = null;
            float closestDist = Mathf.Infinity;
            foreach (CartSelfMovementManager cart in carts)
            {
                if (cart == null) continue;
                if (cart.isCartBeingPulled) continue; // Pulled carts do not count.
                float distance = cart.GetDistanceFrom(position);
                if (distance < closestDist)
                {
                    closestCart = cart;
                    closestDist = distance;
                }
            }
            return closestCart;
        }

        public static void OrderNearestCart(int orderType, Vector3 playerPosition)
        {
            CartSelfMovementManager cart = GetClosestCart(playerPosition);
            if (cart == null) return;

            cart.cartTargetSync.GoToTarget(orderType, playerPosition);
        }
    }
}
