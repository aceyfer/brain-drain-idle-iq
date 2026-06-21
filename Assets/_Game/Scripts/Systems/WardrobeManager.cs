using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Owns which OutfitData is currently equipped on the Player Character. Unlock is derived
    /// live from RebirthManager.RebirthCount (no separate "owned outfits" tracking needed,
    /// since RebirthCount only ever increases -- once unlocked, always unlocked). Equip choice
    /// is the one piece of real state here, since it doesn't follow automatically from
    /// RebirthCount the way CharacterAppearanceStage's auto-progression does.
    /// </summary>
    public sealed class WardrobeManager : MonoBehaviour
    {
        [Header("Outfits")]
        [SerializeField] private List<OutfitData> outfits = new();

        [Header("Visual")]
        [Tooltip("SpriteRenderer the equipped outfit's sprite is applied to. Expected to be a layer on the Player Character, on top of its base CharacterAppearanceStage sprite.")]
        [SerializeField] private SpriteRenderer outfitRenderer;

        private static WardrobeManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static WardrobeManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<WardrobeManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("WardrobeManager (Auto)");
                    instance = hostObject.AddComponent<WardrobeManager>();
                }

                return instance;
            }
        }

        /// <summary>Read-only view of all configured outfit templates, for UI population (locked and unlocked alike).</summary>
        public IReadOnlyList<OutfitData> Outfits => outfits;

        /// <summary>The currently equipped outfit, or null if nothing has been equipped yet.</summary>
        public OutfitData EquippedOutfit { get; private set; }

        /// <summary>Fired whenever the equipped outfit changes. Passes the newly equipped outfit (may be null).</summary>
        public event Action<OutfitData> OnEquippedOutfitChanged;

        private bool hasManuallyEquipped;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[WardrobeManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChanged;
            }

            if (EquippedOutfit == null)
            {
                AutoEquipLowestUnlockedIfNoneChosen();
            }
        }

        private void OnDestroy()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>Returns true when the player's current RebirthCount meets the outfit's unlock requirement.</summary>
        public bool IsUnlocked(OutfitData outfit)
        {
            return outfit != null
                && RebirthManager.Instance != null
                && RebirthManager.Instance.RebirthCount >= outfit.minRebirthCountToUnlock;
        }

        /// <summary>
        /// Attempts to equip an unlocked outfit. Returns false if the outfit is null or not yet
        /// unlocked, leaving the current equip unchanged.
        /// </summary>
        public bool TryEquipOutfit(OutfitData outfit)
        {
            if (!IsUnlocked(outfit))
            {
                return false;
            }

            hasManuallyEquipped = true;
            EquipOutfit(outfit);
            return true;
        }

        /// <summary>Directly restores the equipped outfit from a save file by its stable outfitId.</summary>
        public void LoadState(string equippedOutfitId)
        {
            if (string.IsNullOrWhiteSpace(equippedOutfitId))
            {
                return;
            }

            OutfitData restored = FindById(equippedOutfitId);
            if (restored == null)
            {
                return;
            }

            hasManuallyEquipped = true;
            EquipOutfit(restored);
        }

        private void HandleRebirthCountChanged(int _)
        {
            if (!hasManuallyEquipped)
            {
                AutoEquipLowestUnlockedIfNoneChosen();
            }
        }

        /// <summary>
        /// Fresh-save default: before the player has ever manually chosen an outfit, keep them
        /// dressed in whatever's unlocked rather than left bare. Stops the moment a manual
        /// choice (or a restored save) sets hasManuallyEquipped.
        /// </summary>
        private void AutoEquipLowestUnlockedIfNoneChosen()
        {
            OutfitData lowestUnlocked = null;

            for (int i = 0; i < outfits.Count; i++)
            {
                OutfitData candidate = outfits[i];
                if (candidate == null || !IsUnlocked(candidate))
                {
                    continue;
                }

                if (lowestUnlocked == null || candidate.minRebirthCountToUnlock < lowestUnlocked.minRebirthCountToUnlock)
                {
                    lowestUnlocked = candidate;
                }
            }

            if (lowestUnlocked != null && lowestUnlocked != EquippedOutfit)
            {
                EquipOutfit(lowestUnlocked);
            }
        }

        private void EquipOutfit(OutfitData outfit)
        {
            if (outfit == EquippedOutfit)
            {
                return;
            }

            EquippedOutfit = outfit;

            if (outfitRenderer != null)
            {
                outfitRenderer.sprite = outfit != null ? outfit.sprite : null;
            }

            OnEquippedOutfitChanged?.Invoke(EquippedOutfit);
        }

        private OutfitData FindById(string outfitId)
        {
            for (int i = 0; i < outfits.Count; i++)
            {
                if (outfits[i] != null && outfits[i].outfitId == outfitId)
                {
                    return outfits[i];
                }
            }

            return null;
        }
    }
}
