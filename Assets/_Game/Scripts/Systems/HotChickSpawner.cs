using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Spawns up to 6 Hot Chick visual placeholders alongside the player character, one per
    /// CompanionManager.HotChickCount purchased. UI Image/RectTransform-based, not
    /// SpriteRenderer -- checked the scene directly: PlayerCharacterController's
    /// appearanceImage field is wired (appearanceRenderer is not), confirming this project's
    /// player character is Canvas/UI-based, matching BackgroundPedestrianManager's existing
    /// pattern for "spawn multiple simple visuals near the player" exactly.
    ///
    /// SCENE WIRING NOT DONE: this script is code-complete but not attached to anything in
    /// SampleScene.unity -- per instruction, Unity AI handles that.
    /// </summary>
    public sealed class HotChickSpawner : MonoBehaviour
    {
        private const int MaxSlots = 6;
        private const float PlaceholderWidth = 60f;
        private const float PlaceholderHeight = 120f;
        private static readonly Color PlaceholderPink = new Color32(0xFF, 0x69, 0xB4, 0xFF);

        /// <summary>
        /// Fixed slot offsets in units of slotSpacing, alternating right/left per spec: slot 1
        /// at +1x, slot 2 at -1x, slot 3 at +2x, slot 4 at -2x, slot 5 at +3x, slot 6 at -3x.
        /// </summary>
        private static readonly float[] SlotSpacingMultiples = { 1f, -1f, 2f, -2f, 3f, -3f };

        [Header("Art")]
        [Tooltip("Up to 6 entries, one per slot in purchase order. A null entry at a given index falls back to a procedural pink placeholder rectangle (60x120), matching this project's established placeholder-sprite convention.")]
        [SerializeField] private Sprite[] hotChickSprites = new Sprite[MaxSlots];

        [Header("Placement")]
        [Tooltip("Y position (RectTransform anchoredPosition.y) matching the player character's street level.")]
        [SerializeField] private float streetLevelY;
        [Tooltip("X position of the player character. Slots are offset from this and never placed exactly here.")]
        [SerializeField] private float playerAnchorX;
        [Tooltip("Distance between adjacent same-side slots. Default 80, matching the spec's fixed +-80/160/240 layout (slotSpacing=80 reproduces those exact values; changing it scales all 6 slot positions proportionally).")]
        [SerializeField] private float slotSpacing = 80f;

        [Header("Container")]
        [Tooltip("The RectTransform under Canvas where Hot Chick sprites are spawned -- same container convention as BackgroundPedestrianManager.containerRect.")]
        [SerializeField] private RectTransform containerRect;

        private readonly List<GameObject> spawnedSlots = new(MaxSlots);

        private void Start()
        {
            CompanionManager companion = CompanionManager.Instance;
            int initialCount = companion != null ? companion.HotChickCount : 0;

            for (int i = 0; i < initialCount && i < MaxSlots; i++)
            {
                SpawnSlot(i);
            }

            if (companion != null)
            {
                companion.OnHotChickCountChanged -= HandleHotChickCountChanged;
                companion.OnHotChickCountChanged += HandleHotChickCountChanged;
            }
        }

        private void OnDestroy()
        {
            if (CompanionManager.Instance != null)
            {
                CompanionManager.Instance.OnHotChickCountChanged -= HandleHotChickCountChanged;
            }
        }

        /// <summary>
        /// Reconciles spawnedSlots.Count up to newCount, spawning one slot at a time. Also
        /// covers the on-load sync path (CompanionManager.LoadHotChickCount fires this same
        /// event) without double-spawning, since it only ever spawns the *difference*.
        /// </summary>
        private void HandleHotChickCountChanged(int newCount)
        {
            while (spawnedSlots.Count < newCount && spawnedSlots.Count < MaxSlots)
            {
                SpawnSlot(spawnedSlots.Count);
            }
        }

        private void SpawnSlot(int slotIndex)
        {
            if (containerRect == null || slotIndex < 0 || slotIndex >= MaxSlots)
            {
                return;
            }

            float offsetX = SlotSpacingMultiples[slotIndex] * slotSpacing;

            // Never place a sprite at playerAnchorX. The fixed slot table never resolves to
            // exactly 0 on its own, but guard explicitly in case slotSpacing is ever set to 0.
            if (Mathf.Approximately(offsetX, 0f))
            {
                return;
            }

            var slotGo = new GameObject($"HotChick_{slotIndex + 1}", typeof(RectTransform));
            slotGo.transform.SetParent(containerRect, false);

            var rt = slotGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(PlaceholderWidth, PlaceholderHeight);

            // Bottom-center pivot, same convention as BackgroundPedestrianManager, so all
            // street-level visuals share one consistent anchor scheme.
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(playerAnchorX + offsetX, streetLevelY);

            var img = slotGo.AddComponent<Image>();
            Sprite art = hotChickSprites != null && slotIndex < hotChickSprites.Length ? hotChickSprites[slotIndex] : null;
            if (art != null)
            {
                img.sprite = art;
                img.preserveAspect = true;
            }
            else
            {
                img.color = PlaceholderPink;
            }

            img.raycastTarget = false;

            // Slot positions are fixed and distinct (a multiple of slotSpacing per slot, each
            // at least slotSpacing apart from its same-side neighbor), so spawning at most
            // MaxSlots of them structurally can't overlap -- no separate runtime check needed.
            spawnedSlots.Add(slotGo);
        }
    }
}
