using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single generic Cash Shop item row -- mirrors UpgradeSlotUI's
    /// locked/affordable/too-expensive pattern, plus a 4th "owned" state since these items are
    /// one-time permanent purchases, not repeatable building levels. The Hot Chick companion
    /// has its own dedicated row inside CashShopUIController rather than using this slot type,
    /// since it's a single sequential-tier progression, not a list of independent items.
    ///
    /// DEAD BRANCHES (2026-07-10): the seven itemId == "profanity_pack" special cases in this
    /// file are unreachable -- ProfanityPack.asset was deleted and removed from CashShopManager's
    /// items list when profanity moved to the God Tier Store's Bad Words Pack (direct real
    /// currency only, TASKLIST_DETAILS §10). Left in place per the harmless-dead-code convention;
    /// strip them during the parked Shop-3/Points/Cash family reconciliation. NOTE for Phase B:
    /// the owned->toggle behavior these branches implemented (tap owned row to flip profanity
    /// on/off) was the only in-game toggle UI -- the God Shop slot UI must provide an equivalent.
    /// </summary>
    public sealed class CashShopSlotUI : MonoBehaviour
    {
        private static readonly Color LockedColor = new Color32(0x8A, 0x8D, 0x9B, 0xFF);
        private static readonly Color AffordableColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);
        private static readonly Color TooExpensiveColor = new Color32(0xFF, 0x00, 0x7F, 0xFF);
        private static readonly Color OwnedColor = new Color32(0x39, 0xFF, 0x14, 0xFF);

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Interaction")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Image background;

        private CashShopItemData boundData;
        private CashShopManager boundManager;

        public void Bind(CashShopItemData data, CashShopManager manager)
        {
            boundData = data;
            boundManager = manager;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }
        }

        private void HandleBuyClicked()
        {
            if (boundData != null && boundData.itemId == "profanity_pack" && boundManager != null && boundManager.IsItemOwned(boundData))
            {
                if (RandomChatterManager.Instance != null)
                {
                    RandomChatterManager.Instance.ToggleProfanity(!RandomChatterManager.Instance.ProfanityEnabled);
                    SaveManager.Instance?.SaveGame();
                }
            }
            else
            {
                if (boundManager != null && boundManager.TryPurchaseItem(boundData))
                {
                    SaveManager.Instance?.SaveGame();
                }
            }
        }

        public void RefreshState(CurrencyManager currency)
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = boundData.displayName;
                nameText.fontSize = 32f; // Large font
            }

            string bonusText = "";
            if (boundData.itemId == "profanity_pack")
            {
                bonusText = "Unlocks profanity chatter toggle";
            }
            else
            {
                switch (boundData.effectType)
                {
                    case CashShopEffectType.BrainPowerTapPercent:
                        bonusText = $"+{boundData.effectPercent * 100f:F0}% Tap Power";
                        break;
                    case CashShopEffectType.CashPerSecondPercent:
                        bonusText = $"+{boundData.effectPercent * 100f:F0}% Cash/sec";
                        break;
                    case CashShopEffectType.AllMultipliersPercent:
                        bonusText = $"+{boundData.effectPercent * 100f:F0}% All Multipliers";
                        break;
                }
            }

            bool owned = boundManager.IsItemOwned(boundData);
            if (owned)
            {
                if (nameText != null)
                {
                    string ownedPrefix = boundData.itemId == "profanity_pack"
                        ? "<size=18><color=#00F0FF>ONE-TIME UNLOCK</color></size>"
                        : "<size=18><color=#39FF14>ONE-TIME UPGRADE</color></size>";
                    nameText.text = $"{ownedPrefix}\n{boundData.displayName}";
                    nameText.fontSize = 32f;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = $"{boundData.description}\n<color=#00F0FF><font-weight=bold>Effect: {bonusText}</font-weight></color>";
                    descriptionText.fontSize = 24f; // Large font
                }
                if (costText != null)
                {
                    if (boundData.itemId == "profanity_pack")
                    {
                        bool enabled = RandomChatterManager.Instance != null && RandomChatterManager.Instance.ProfanityEnabled;
                        costText.text = enabled ? "PROFANITY: ON" : "PROFANITY: OFF";
                    }
                    else
                    {
                        costText.text = "OWNED";
                    }
                    costText.fontSize = 28f; // Large font
                }
                ApplyAccent(OwnedColor);
                if (buyButton != null) buyButton.interactable = (boundData.itemId == "profanity_pack");
                return;
            }

            bool unlocked = boundManager.IsItemUnlocked(boundData);
            if (!unlocked)
            {
                if (descriptionText != null)
                {
                    descriptionText.text = "Access restricted by the Snotty Council.";
                    descriptionText.fontSize = 24f; // Large font
                }
                if (costText != null)
                {
                    costText.text = $"CLASSIFIED — REBIRTH {boundData.gateRebirthCount} REQUIRED";
                    costText.fontSize = 28f; // Large font
                }
                ApplyAccent(LockedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            bool affordable = currency != null && currency.CanAffordCash(boundData.cost);
            if (nameText != null)
            {
                string availablePrefix = boundData.itemId == "profanity_pack"
                    ? "<size=18><color=#00F0FF>ONE-TIME UNLOCK</color></size>"
                    : "<size=18><color=#00F0FF>ONE-TIME UPGRADE</color></size>";
                nameText.text = $"{availablePrefix}\n{boundData.displayName}";
                nameText.fontSize = 32f;
            }

            if (descriptionText != null)
            {
                string tradeoffNote = boundData.itemId == "profanity_pack"
                    ? ""
                    : "\n<size=16><color=#888888>Permanent boost — cash spent here won't convert to Points.</color></size>";
                descriptionText.text = $"{boundData.description}\n<color=#00F0FF><font-weight=bold>Effect: {bonusText}</font-weight></color>{tradeoffNote}";
                descriptionText.fontSize = 24f; // Large font
            }
            if (costText != null)
            {
                costText.text = $"${NumberFormatter.Format(boundData.cost)}";
                costText.fontSize = 28f; // Large font
            }
            ApplyAccent(affordable ? AffordableColor : TooExpensiveColor);
            if (buyButton != null) buyButton.interactable = true;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null) background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (nameText != null) nameText.color = Color.white; // Stable white for readability
            if (costText != null) costText.color = new Color(1f, 0.92f, 0.016f, 1f); // Warm stable gold for cost
        }
    }
}
