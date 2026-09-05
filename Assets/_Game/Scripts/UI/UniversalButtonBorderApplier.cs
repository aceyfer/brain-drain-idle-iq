using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Applies a reusable "button theme" (border sprite, optional fill recolor/resprite, optional
    /// label text styling -- see ButtonTheme) to every UI Button in the scene, including buttons
    /// that live inside a currently-hidden modal (RebirthModal, confirm/cancel dialogs, etc.),
    /// found via FindObjectsInactive.Include so they're covered the moment their modal opens, not
    /// just whatever happens to be active at Start.
    ///
    /// The active theme swaps with World Restoration stage -- grimy/cracked early, clean
    /// gold-glow once the world is healed -- via the same OnRestorationStageChanged event
    /// WeatherAtmosphereView/BackgroundStageView already key off. Never polls stage in Update. A
    /// future novelty/seasonal pack (zombie, Christmas, etc.) can temporarily replace the
    /// stage-driven theme via ApplyOverrideTheme/ClearOverrideTheme without touching this class or
    /// any individual button.
    ///
    /// Class name predates this broader theme system (originally border-only, per Aceyfer's
    /// 2026-08-31 "universal button borders" request) -- kept as-is rather than renamed, since the
    /// scene already has a GameObject wired to this exact component by GUID and an invasive rename
    /// isn't worth the risk for a cosmetic name change (same judgment call as keeping
    /// RebirthManager's C# name through "The Snotting" rename).
    ///
    /// Runs its scan once at Start. A Button instantiated later at runtime (e.g. a dynamically
    /// spawned shop row) is not retroactively covered -- there is no such case in this scene today
    /// (ShopUIController's rows are pre-built, not instantiated per-purchase), but if one is ever
    /// added, call ApplyThemeToButton(button, ActiveTheme) on it directly rather than re-running
    /// this scan against the whole scene.
    /// </summary>
    public sealed class UniversalButtonBorderApplier : MonoBehaviour
    {
        [Tooltip("6 button themes in World Restoration stage order (index 0..5). Border, fill, and label text all come from whichever theme is active -- see ButtonTheme.")]
        [SerializeField] private ButtonTheme[] stageThemes;

        [Tooltip("How far the border frame extends beyond each button's own edges, in pixels. Safe to leave alone -- matches the padding baked into the generated sprites' 9-slice border.")]
        [SerializeField] private float outsetPixels = 5f;

        private const string BorderChildName = "UniversalBorder (Generated)";

        private readonly List<Button> managedButtons = new List<Button>();
        private ButtonTheme overrideTheme;

        /// <summary>Whichever theme is currently being applied -- the override if one is set, otherwise whatever the current World Restoration stage resolves to.</summary>
        public ButtonTheme ActiveTheme => overrideTheme != null ? overrideTheme : ResolveThemeForStage(ResolveCurrentStageIndex());

        private void Start()
        {
            DiscoverButtons();
            SubscribeToRestorationEvents();
            ApplyThemeToAllManagedButtons(ActiveTheme);
        }

        private void OnDestroy()
        {
            UnsubscribeFromRestorationEvents();
        }

        private void DiscoverButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsVisualButton(buttons[i]))
                {
                    managedButtons.Add(buttons[i]);
                }
            }

            Debug.Log($"[UniversalButtonBorderApplier] Bordered {managedButtons.Count} button(s) in the scene.");
        }

        /// <summary>
        /// Invisible full-screen tap-catchers (MainTapButton, TapButton) signal "not a real visual
        /// button" via their own Image.color.a == 0 -- their RectTransforms span the entire
        /// screen, so theming them (border, and especially an opaque fill color) would stretch
        /// visible art across the whole screen, rendering as a huge vignette/glow over the HUD
        /// (the actual root cause of a reported "golden flash" bug). Checking alpha rather than
        /// sprite-presence matters: ShopButton/ConvertButton/RestoreButton are legitimately
        /// visible via a solid tint color with no sprite at all, so a sprite-based filter would
        /// wrongly exclude those too.
        /// </summary>
        private static bool IsVisualButton(Button button)
        {
            if (button.transform as RectTransform == null) { return false; }
            Image ownImage = button.GetComponent<Image>();
            return ownImage == null || ownImage.color.a > 0f;
        }

        /// <summary>Idempotent: finds the existing generated border child if this button already has one instead of duplicating it. Border-only -- see ApplyThemeToButton for the full border+fill+text application.</summary>
        public Image EnsureBorderOn(Button button)
        {
            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect == null || !IsVisualButton(button)) { return null; }

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

        /// <summary>
        /// Applies border, and whichever optional fill/text fields the theme sets, to a single
        /// button. Public so a future dynamically-spawned button (there is no such case today,
        /// see class doc) can opt in without this class re-scanning the whole scene.
        /// </summary>
        public void ApplyThemeToButton(Button button, ButtonTheme theme)
        {
            if (button == null || theme == null || !IsVisualButton(button)) { return; }

            Image border = EnsureBorderOn(button);
            if (border != null && theme.borderSprite != null)
            {
                border.sprite = theme.borderSprite;
            }

            Image ownImage = button.GetComponent<Image>();
            if (ownImage != null)
            {
                if (theme.overrideFillColor) { ownImage.color = theme.fillColor; }
                if (theme.fillSprite != null) { ownImage.sprite = theme.fillSprite; }
            }

            if (theme.labelFontMaterial != null || theme.overrideTextColor)
            {
                foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (theme.labelFontMaterial != null) { label.fontSharedMaterial = theme.labelFontMaterial; }
                    if (theme.overrideTextColor) { label.color = theme.labelTextColor; }
                }
            }
        }

        /// <summary>
        /// Temporarily replaces the stage-driven theme with `theme` on every managed button --
        /// the entry point a future novelty/seasonal pack (zombie, Christmas, etc.) calls to
        /// reskin the whole game's buttons without touching this class or any individual button.
        /// Stage changes are ignored while an override is active; call ClearOverrideTheme() to
        /// revert to whatever the current World Restoration stage resolves to.
        /// </summary>
        public void ApplyOverrideTheme(ButtonTheme theme)
        {
            overrideTheme = theme;
            ApplyThemeToAllManagedButtons(theme);
        }

        /// <summary>Reverts to the stage-driven theme, re-resolved fresh from the current World Restoration stage rather than whatever was cached before the override started.</summary>
        public void ClearOverrideTheme()
        {
            overrideTheme = null;
            ApplyThemeToAllManagedButtons(ResolveThemeForStage(ResolveCurrentStageIndex()));
        }

        private void ApplyThemeToAllManagedButtons(ButtonTheme theme)
        {
            if (theme == null) { return; }
            for (int i = 0; i < managedButtons.Count; i++)
            {
                ApplyThemeToButton(managedButtons[i], theme);
            }
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
            // A novelty/seasonal override takes priority over stage progression -- stage changes
            // underneath it are still tracked (ActiveTheme/ResolveThemeForStage always reads the
            // live stage), just not applied to buttons until ClearOverrideTheme() runs.
            if (overrideTheme != null) { return; }
            ApplyThemeToAllManagedButtons(ResolveThemeForStage(stage != null ? stage.stageIndex : 0));
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStage?.stageIndex ?? 0;
        }

        private ButtonTheme ResolveThemeForStage(int index)
        {
            if (stageThemes == null || stageThemes.Length == 0) { return null; }
            index = Mathf.Clamp(index, 0, stageThemes.Length - 1);
            return stageThemes[index];
        }
    }
}
