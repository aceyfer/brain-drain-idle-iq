using UnityEngine;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single outfit row in the wardrobe scroll list.
    /// Binds to an OutfitData template and reflects locked / unlocked-unequipped / equipped
    /// states, mirroring UpgradeSlotUI's locked/affordable/too-expensive pattern.
    /// </summary>
    public sealed class WardrobeSlotUI : MonoBehaviour
    {
        // Neon, high-contrast palette (bloom-ready) -- matches UpgradeSlotUI's accent scheme.
        private static readonly Color LockedColor = new Color32(0x4A, 0x4E, 0x5D, 0xFF);
        private static readonly Color EquippedColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);
        private static readonly Color UnlockedColor = new Color32(0xFF, 0x00, 0x7F, 0xFF);

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Interaction")]
        [SerializeField] private UnityEngine.UI.Button equipButton;
        [SerializeField] private UnityEngine.UI.Image background;

        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI StatusText { get => statusText; set => statusText = value; }
        public UnityEngine.UI.Button EquipButton { get => equipButton; set => equipButton = value; }
        public UnityEngine.UI.Image Background { get => background; set => background = value; }

        private OutfitData boundData;
        private WardrobeManager boundManager;

        /// <summary>Binds this slot to an outfit template and wires the equip button.</summary>
        public void Bind(OutfitData data, WardrobeManager manager)
        {
            boundData = data;
            boundManager = manager;

            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(HandleEquipClicked);
                equipButton.onClick.AddListener(HandleEquipClicked);
            }
        }

        private void HandleEquipClicked()
        {
            if (boundManager != null && boundData != null)
            {
                boundManager.TryEquipOutfit(boundData);
            }
        }

        /// <summary>Recomputes labels and visual state based on the current equip/unlock status.</summary>
        public void RefreshState()
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            bool unlocked = boundManager.IsUnlocked(boundData);
            bool equipped = unlocked && boundManager.EquippedOutfit == boundData;

            if (nameText != null)
            {
                nameText.text = unlocked ? boundData.outfitName : "??? LOCKED ???";
            }

            if (!unlocked)
            {
                if (statusText != null) statusText.text = $"REQUIRES {boundData.minRebirthCountToUnlock} REBIRTH(S)";
                ApplyAccent(LockedColor);
                if (equipButton != null) equipButton.interactable = false;
                return;
            }

            if (statusText != null)
            {
                statusText.text = equipped ? "EQUIPPED" : "TAP TO EQUIP";
            }

            ApplyAccent(equipped ? EquippedColor : UnlockedColor);

            // Equipping an already-equipped outfit is a harmless no-op, so the button stays
            // interactable rather than disabling on equipped state.
            if (equipButton != null) equipButton.interactable = true;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null)
            {
                // Subtle translucent tint so rows read as glowing panels, not solid blocks.
                background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            }

            if (nameText != null)
            {
                nameText.color = accent;
            }

            if (statusText != null)
            {
                statusText.color = accent;
            }
        }
    }
}
