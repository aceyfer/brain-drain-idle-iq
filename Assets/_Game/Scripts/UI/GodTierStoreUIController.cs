using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// God Tier Store popup -- mirrors ShopUIController's build-one-row-per-template,
    /// open/close-as-a-popup pattern. Real-money items, purchased through IapCommerceService/
    /// Unity IAP with server-side validation (§12) -- see GodTierStoreManager's class doc for the
    /// purchase boundary (RequestPurchase/GrantVerifiedEntitlement).
    ///
    /// SCENE WIRING NOT YET DONE: code-complete, but no panel/button/Content hierarchy exists in
    /// SampleScene.unity yet -- see CashShopUIController's identical note.
    /// </summary>
    public sealed class GodTierStoreUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GodTierStoreManager godTierStoreManager;

        [Header("Panel Visibility")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Items")]
        [SerializeField] private RectTransform content;
        [SerializeField] private GodTierStoreSlotUI slotPrefab;

        private readonly List<GodTierStoreSlotUI> spawnedSlots = new(8);
        private bool built;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(OpenShop);
            if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (godTierStoreManager == null)
            {
                godTierStoreManager = GodTierStoreManager.Instance;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += BuildStore;
            }

            if (FTUEManager.Instance != null)
            {
                FTUEManager.Instance.OnLiteratesCardCollected -= RefreshOpenButtonAvailability;
                FTUEManager.Instance.OnLiteratesCardCollected += RefreshOpenButtonAvailability;
            }

            RefreshOpenButtonAvailability();
            BuildStore();
        }

        private void OnDestroy()
        {
            if (godTierStoreManager != null)
            {
                godTierStoreManager.OnItemsChanged -= RefreshAllSlots;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= BuildStore;
            }

            if (FTUEManager.Instance != null)
            {
                FTUEManager.Instance.OnLiteratesCardCollected -= RefreshOpenButtonAvailability;
            }
        }

        /// <summary>
        /// IAP RULES: "no purchase popups during the FTUE, and never auto-open the shop on first
        /// launch." The second half was already true (OpenShop has no caller except openButton's
        /// own onClick) -- this covers the first half. "FTUE" here means the full §23 FTUE card
        /// sequence, not just whichever single card is on screen at a given instant: gated on the
        /// two latest-landing beats (NameRevealSeen, GaryCardSeen) both being true rather than any
        /// single earlier flag, so the shop can't open mid-sequence between cards either. A null
        /// FTUEManager.Instance fails OPEN (don't block), matching this codebase's convention for
        /// a missing dependency rather than permanently hiding the shop over it.
        /// </summary>
        private static bool IsFtueComplete()
        {
            FTUEManager ftue = FTUEManager.Instance;
            return ftue == null || (ftue.NameRevealSeen && ftue.GaryCardSeen);
        }

        private void RefreshOpenButtonAvailability()
        {
            if (openButton != null) { openButton.interactable = IsFtueComplete(); }
        }

        public void OpenShop()
        {
            // Belt-and-suspenders on top of RefreshOpenButtonAvailability disabling the button --
            // also blocks an active FTUE modal (IsModalShowing) specifically, since that can be
            // true even once NameRevealSeen/GaryCardSeen have both landed if some other card is
            // mid-display for an unrelated reason.
            if (!IsFtueComplete() || (FTUEManager.Instance != null && FTUEManager.Instance.IsModalShowing))
            {
                return;
            }

            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
                RefreshAllSlots();
            }
        }

        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        private void BuildStore()
        {
            if (built)
            {
                RefreshAllSlots();
                return;
            }

            if (godTierStoreManager == null)
            {
                godTierStoreManager = GodTierStoreManager.Instance;
            }

            if (godTierStoreManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[GodTierStoreUIController] Missing references; cannot build God Tier Store.", this);
                return;
            }

            IReadOnlyList<GodTierStoreItemData> items = godTierStoreManager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                GodTierStoreItemData data = items[i];
                if (data == null) continue;

                GodTierStoreSlotUI slot = Instantiate(slotPrefab, content);
                slot.name = $"GodTierStoreSlot_{data.itemId}";
                slot.transform.SetSiblingIndex(i);
                slot.Bind(data, godTierStoreManager);
                spawnedSlots.Add(slot);
            }

            built = true;
            godTierStoreManager.OnItemsChanged -= RefreshAllSlots;
            godTierStoreManager.OnItemsChanged += RefreshAllSlots;
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                spawnedSlots[i]?.RefreshState();
            }
        }
    }
}
