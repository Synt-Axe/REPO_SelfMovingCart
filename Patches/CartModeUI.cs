using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SelfMovingCart.Patches
{
    [HarmonyPatch(typeof(RoundDirector))]
    public static class CartModeUI
    {
        private static GameObject cartModeTextInstance;
        private static TextMeshProUGUI cartModeText;

        private static void SetCoordinates(RectTransform component)
        {
            // Top-right corner, moved up by 80 pixels
            component.pivot = new Vector2(1f, 1f);
            component.anchoredPosition = new Vector2(20f, 100f); // -20 Y - 80 = -100
            component.anchorMin = new Vector2(0f, 0f);
            component.anchorMax = new Vector2(0f, 0f);
            component.sizeDelta = new Vector2(0f, 0f);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdateCartModeUI()
        {
            // Only show UI if in an active level
            if (!SemiFunc.RunIsLevel())
            {
                if (cartModeTextInstance != null)
                    cartModeTextInstance.SetActive(false);
                return;
            }

            if (cartModeTextInstance == null)
            {
                GameObject hud = GameObject.Find("Game Hud");
                GameObject haul = GameObject.Find("Tax Haul");
                if (hud == null || haul == null)
                    return;

                TMP_FontAsset font = haul.GetComponent<TMP_Text>().font;
                cartModeTextInstance = new GameObject("Cart Mode HUD");
                cartModeTextInstance.SetActive(false);
                cartModeTextInstance.AddComponent<TextMeshProUGUI>();

                cartModeText = cartModeTextInstance.GetComponent<TextMeshProUGUI>();
                cartModeText.font = font;
                cartModeText.color = new Color(1f, 0.95f, 0.5f, 1f);
                cartModeText.fontSize = 22f;
                cartModeText.enableWordWrapping = false;
                cartModeText.alignment = TextAlignmentOptions.TopLeft;
                cartModeText.horizontalAlignment = HorizontalAlignmentOptions.Left;
                cartModeText.verticalAlignment = VerticalAlignmentOptions.Top;

                cartModeTextInstance.transform.SetParent(hud.transform, false);

                RectTransform component = cartModeTextInstance.GetComponent<RectTransform>();
                SetCoordinates(component);
            }

            // Determine cart mode
            string modeText;
            if (PhysGrabCartPatch.controlledCart == -1)
            {
                modeText = "Cart Mode: Nearest Cart";
            }
            else
            {
                var cart = PhysGrabCartPatch.carts[PhysGrabCartPatch.controlledCart];
                int val = ReflectionHelper.GetPrivateField<int>(cart.GetComponent<PhysGrabCart>(), "haulCurrent");
                int distance = Mathf.RoundToInt(Vector3.Distance(PlayerController.instance.transform.position, cart.transform.position));
                modeText = $"Cart Mode: Cart #{PhysGrabCartPatch.controlledCart + 1} (Val: {val}, Dist: {distance}m)";
            }

            cartModeText.SetText(modeText);
            cartModeTextInstance.SetActive(true);
        }
    }
}