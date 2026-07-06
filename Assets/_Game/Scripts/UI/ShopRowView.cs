using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.UI
{
    /// <summary>
    /// One pooled row in a shop tab list. Raycast targets are disabled on all graphics
    /// except the buy button. Purchase routes to a stub until EconomyManager exists.
    /// </summary>
    public sealed class ShopRowView : MonoBehaviour
    {
        private static readonly Color LockedColor = new Color32(0x8A, 0x8D, 0x9B, 0xFF);
        private static readonly Color AffordableColor = new Color32(0x2E, 0x7D, 0x32, 0xFF);
        private static readonly Color TooExpensiveColor = new Color32(0x7F, 0x8C, 0x8D, 0xFF);
        private static readonly Color CompleteColor = new Color32(0x39, 0xFF, 0x14, 0xFF);

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image background;

        private string boundItemId;
        private Action<string> purchaseStub;

        private void Awake()
        {
            ApplyRaycastPolicy();
            WireBuyButton();
        }

        public void ConfigurePurchaseStub(Action<string> stub)
        {
            purchaseStub = stub;
        }

        public void Bind(ShopQuery.ShopRowSnapshot snapshot)
        {
            boundItemId = snapshot.ItemId;

            if (nameText != null)
            {
                nameText.text = snapshot.DisplayName;
                nameText.fontSize = 32f;
            }

            if (descriptionText != null)
            {
                descriptionText.text = snapshot.Description;
                descriptionText.fontSize = 26f;
            }

            if (costText != null)
            {
                costText.text = snapshot.CostLabel;
                costText.fontSize = 30f;
            }

            if (countText != null)
            {
                bool showCount = !string.IsNullOrEmpty(snapshot.CountLabel);
                countText.gameObject.SetActive(showCount);
                if (showCount)
                {
                    countText.text = snapshot.CountLabel;
                    countText.fontSize = 28f;
                }
            }

            Color accent = snapshot.State switch
            {
                ShopQuery.ShopRowState.Locked => LockedColor,
                ShopQuery.ShopRowState.Affordable => AffordableColor,
                ShopQuery.ShopRowState.Complete => CompleteColor,
                _ => TooExpensiveColor
            };

            ApplyAccent(accent);

            if (buyButton != null)
            {
                buyButton.interactable = snapshot.BuyInteractable;
            }
        }

        private void WireBuyButton()
        {
            if (buyButton == null)
            {
                return;
            }

            buyButton.onClick.RemoveListener(HandleBuyClicked);
            buyButton.onClick.AddListener(HandleBuyClicked);
        }

        private void HandleBuyClicked()
        {
            if (string.IsNullOrEmpty(boundItemId))
            {
                return;
            }

            purchaseStub?.Invoke(boundItemId);
        }

        private void ApplyRaycastPolicy()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }

            if (buyButton != null && buyButton.targetGraphic != null)
            {
                buyButton.targetGraphic.raycastTarget = true;
            }
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null)
            {
                background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            }

            if (nameText != null)
            {
                nameText.color = Color.white;
            }

            if (descriptionText != null)
            {
                descriptionText.color = Color.white;
            }

            if (costText != null)
            {
                costText.color = accent;
            }

            if (countText != null)
            {
                countText.color = Color.white;
            }
        }
    }
}
