using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Adds a consistent border/frame overlay to every UI Button in the scene -- including
    /// buttons that live inside a currently-hidden modal (RebirthModal, confirm/cancel dialogs,
    /// etc.), found via FindObjectsInactive.Include so they're covered the moment their modal
    /// opens, not just whatever happens to be active at Start. Frame sprite swaps with World
    /// Restoration stage: grimy/cracked early, clean gold-glow once the world is healed, via the
    /// same OnRestorationStageChanged event BackgroundStageView already keys off. Never polls
    /// stage in Update.
    ///
    /// Runs its scan once at Start. A Button instantiated later at runtime (e.g. a dynamically
    /// spawned shop row) is not retroactively covered -- there is no such case in this scene
    /// today (ShopUIController's rows are pre-built, not instantiated per-purchase), but if one
    /// is ever added, call EnsureBorderOn(button) on it directly rather than re-running this
    /// scan against the whole scene.
    ///
    /// 2026-08-31: added per Aceyfer's "universal button borders...evolution stage of 1-6"
    /// request. Generated/wired by ArtExpansionTool.
    /// </summary>
    public sealed class UniversalButtonBorderApplier : MonoBehaviour
    {
        [Tooltip("6 border sprites in World Restoration stage order (index 0..5).")]
        [SerializeField] private Sprite[] stageBorderSprites;

        [Tooltip("How far the border frame extends beyond each button's own edges, in pixels. Safe to leave alone -- matches the padding baked into the generated sprites' 9-slice border.")]
        [SerializeField] private float outsetPixels = 5f;

        private const string BorderChildName = "UniversalBorder (Generated)";

        private readonly List<Image> managedBorders = new List<Image>();

        private void Start()
        {
            ApplyToAllButtons();
            SubscribeToRestorationEvents();
            ApplySpriteForStage(ResolveCurrentStageIndex());
        }

        private void OnDestroy()
        {
            UnsubscribeFromRestorationEvents();
        }

        private void ApplyToAllButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Image border = EnsureBorderOn(buttons[i]);
                if (border != null)
                {
                    managedBorders.Add(border);
                }
            }

            Debug.Log($"[UniversalButtonBorderApplier] Bordered {managedBorders.Count} button(s) in the scene.");
        }

        /// <summary>Idempotent: finds the existing generated border child if this button already has one instead of duplicating it.</summary>
        public Image EnsureBorderOn(Button button)
        {
            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect == null) { return null; }

            Transform existing = buttonRect.Find(BorderChildName);
            GameObject borderObject = existing != null ? existing.gameObject : null;

            if (borderObject == null)
            {
                borderObject = new GameObject(BorderChildName, typeof(RectTransform));
                borderObject.transform.SetParent(buttonRect, false);
            }

            RectTransform rect = borderObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-outsetPixels, -outsetPixels);
            rect.offsetMax = new Vector2(outsetPixels, outsetPixels);

            Image image = borderObject.GetComponent<Image>();
            if (image == null) { image = borderObject.AddComponent<Image>(); }
            image.type = Image.Type.Sliced;
            image.raycastTarget = false; // purely visual -- must never steal the button's own click
            image.preserveAspect = false;
            image.color = Color.white;

            // Several buttons in this scene arrange their own icon+label children with a
            // Horizontal/VerticalLayoutGroup on the button itself -- without this, that layout
            // group would treat our injected border child as one more element to lay out and
            // collapse it to zero size instead of respecting the explicit anchors/offsets set
            // above. ignoreLayout tells any such group to leave this child alone entirely.
            LayoutElement layoutElement = borderObject.GetComponent<LayoutElement>();
            if (layoutElement == null) { layoutElement = borderObject.AddComponent<LayoutElement>(); }
            layoutElement.ignoreLayout = true;

            borderObject.transform.SetAsLastSibling();
            return image;
        }

        private void SubscribeToRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null) { return; }
            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
            manager.OnRestorationStageChanged += HandleRestorationStageChanged;
        }

        private void UnsubscribeFromRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null) { return; }
            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
        }

        private void HandleRestorationStageChanged(WorldRestorationStage stage)
        {
            ApplySpriteForStage(stage != null ? stage.stageIndex : 0);
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStage?.stageIndex ?? 0;
        }

        private void ApplySpriteForStage(int index)
        {
            if (stageBorderSprites == null || stageBorderSprites.Length == 0) { return; }
            index = Mathf.Clamp(index, 0, stageBorderSprites.Length - 1);
            Sprite sprite = stageBorderSprites[index];
            if (sprite == null) { return; }

            for (int i = 0; i < managedBorders.Count; i++)
            {
                if (managedBorders[i] != null)
                {
                    managedBorders[i].sprite = sprite;
                }
            }
        }
    }
}
