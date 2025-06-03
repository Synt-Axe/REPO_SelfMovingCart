using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
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
        public static int controlledCart = -1; // -1 = closest, otherwise it's the index of the cart to control.

        static TextMeshProUGUI switchCartTMP;
        static Coroutine switchCartTextCoroutine;

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

        public static CartSelfMovementManager GetCartToOrder(Vector3 position)
        {
            if (controlledCart > -1) return carts[controlledCart];

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

        public static void OrderCart(int orderType, Vector3 playerPosition)
        {
            CartSelfMovementManager cart = GetCartToOrder(playerPosition);
            if (cart == null) return;

            cart.cartTargetSync.GoToTarget(orderType, playerPosition);
        }

        public static void SwitchCarts()
        {
            if (controlledCart + 1 < carts.Count) controlledCart++;
            else controlledCart = -1;
            
            // Show ui.
            if(!ConfigManager.alwaysShowCartModeText.Value) SwitchCartUI();
        }

        static void SwitchCartUI()
        {
            if (switchCartTextCoroutine != null) PlayerController.instance.StopCoroutine(switchCartTextCoroutine);

            // If the ui hasn't been initialized yet, we initialize it.
            if (switchCartTMP == null)
            {
                Transform instructionUI = CreateUIElement();

                // Getting the relevant components.
                RectTransform instructionRectTransform = instructionUI.GetComponent<RectTransform>();
                switchCartTMP = instructionUI.GetComponent<TextMeshProUGUI>();

                // Set anchor to the center of the screen
                instructionRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                instructionRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                instructionRectTransform.pivot = new Vector2(0.5f, 0.5f);
                instructionRectTransform.sizeDelta = new Vector2(400f, 50f);

                // Customizing the text.
                instructionRectTransform.anchoredPosition = new Vector2(0f, -20f);
                switchCartTMP.alignment = TextAlignmentOptions.Center;
                switchCartTMP.fontSize = 22;

                instructionUI.gameObject.SetActive(true);
            }

            switchCartTMP.color = new Color(1f, 0.95f, 0.5f, 1f);

            string text = "Cart Mode: Nearest Cart";
            if (controlledCart != -1) 
            {
                CartSelfMovementManager cart = carts[controlledCart];
                int val = ReflectionHelper.GetPrivateField<int>(cart.GetComponent<PhysGrabCart>(), "haulCurrent");
                int distance = Mathf.RoundToInt(Vector3.Distance(PlayerController.instance.transform.position, cart.transform.position));

                text = $"Cart Mode: Cart #{controlledCart + 1} (Val: {val}, Dist: {distance}m)";
            }

            switchCartTMP.text = text;

            switchCartTextCoroutine = PlayerController.instance.StartCoroutine(FadeOutSwitchCartText());
        }

        static IEnumerator FadeOutSwitchCartText()
        {
            yield return new WaitForSeconds(1f);

            Color color = switchCartTMP.color;
            float fadeSpeed = 2f; // Adjust this to control fade speed

            while (color.a > 0f)
            {
                color.a -= fadeSpeed * Time.deltaTime;
                color.a = Mathf.Max(0f, color.a); // Prevent going below 0
                switchCartTMP.color = color;
                yield return null; // Wait one frame
            }

            // Ensure it's completely transparent at the end
            color.a = 0f;
            switchCartTMP.color = color;

            switchCartTextCoroutine = null;
        }

        static Transform CreateUIElement()
        {
            GameObject hud = GameObject.Find("Game Hud");
            GameObject haul = GameObject.Find("Tax Haul");
            if (hud == null || haul == null)
                return null;

            TMP_FontAsset font = haul.GetComponent<TMP_Text>().font;
            GameObject textInstance = new GameObject("Cart Mode HUD");
            textInstance.SetActive(false);
            textInstance.AddComponent<TextMeshProUGUI>();

            TextMeshProUGUI textComponent = textInstance.GetComponent<TextMeshProUGUI>();
            textComponent.font = font;
            textComponent.color = Color.white;
            textComponent.fontSize = 22f;
            textComponent.enableWordWrapping = false;
            textComponent.alignment = TextAlignmentOptions.Center;

            textInstance.transform.SetParent(hud.transform, false);

            // Set the position.
            RectTransform component = textInstance.GetComponent<RectTransform>();

            component.pivot = new Vector2(0.5f, 0.5f);
            component.anchoredPosition = new Vector2(0f, -20f);
            component.anchorMin = new Vector2(0.5f, 0.5f);
            component.anchorMax = new Vector2(0.5f, 0.5f);
            component.sizeDelta = new Vector2(400f, 50f);

            return textInstance.transform;
        }
    }
}
