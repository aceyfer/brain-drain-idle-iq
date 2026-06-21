using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Populates the wardrobe scroll list with one WardrobeSlotUI per outfit template and keeps
    /// each row's locked/unlocked/equipped state in sync with RebirthCount and the current
    /// equip choice. Mirrors ShopUIController's build-and-refresh pattern.
    /// </summary>
    public sealed class WardrobeUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private WardrobeManager wardrobeManager;

        [Header("Layout")]
        [SerializeField] private RectTransform content;
        [SerializeField] private WardrobeSlotUI slotPrefab;

        private readonly List<WardrobeSlotUI> spawnedSlots = new(8);
        private bool built;

        private void Start()
        {
            ResolveDependencies();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += BuildWardrobe;
            }

            BuildWardrobe();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= BuildWardrobe;
            }
        }

        private void ResolveDependencies()
        {
            if (wardrobeManager == null)
            {
                wardrobeManager = WardrobeManager.Instance;
            }
        }

        private void BuildWardrobe()
        {
            if (built)
            {
                RefreshAllSlots();
                return;
            }

            ResolveDependencies();

            if (wardrobeManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[WardrobeUIController] Missing references; cannot build wardrobe.", this);
                return;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            spawnedSlots.Clear();

            IReadOnlyList<OutfitData> templates = wardrobeManager.Outfits;
            for (int i = 0; i < templates.Count; i++)
            {
                OutfitData data = templates[i];
                if (data == null)
                {
                    continue;
                }

                WardrobeSlotUI slot = Instantiate(slotPrefab, content);
                slot.name = $"WardrobeSlot_{data.outfitId}";
                slot.Bind(data, wardrobeManager);
                spawnedSlots.Add(slot);
            }

            built = true;
            SubscribeToEvents();
            RefreshAllSlots();
        }

        private void SubscribeToEvents()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChanged;
            }

            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquippedOutfitChanged -= HandleEquippedOutfitChanged;
                wardrobeManager.OnEquippedOutfitChanged += HandleEquippedOutfitChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
            }

            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquippedOutfitChanged -= HandleEquippedOutfitChanged;
            }
        }

        private void HandleRebirthCountChanged(int _)
        {
            RefreshAllSlots();
        }

        private void HandleEquippedOutfitChanged(OutfitData _)
        {
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                {
                    spawnedSlots[i].RefreshState();
                }
            }
        }
    }
}
