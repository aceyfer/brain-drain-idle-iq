using System;
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

        // items 2+3 audit (2026-09): 57 managed buttons' live heights split cleanly at this
        // boundary -- max "should skip" height was 67px (RewardedAdRecovery's WatchAdButton),
        // min "should keep" height was 100px (Hot Chick PurchaseButton/bottom bar) -- so any
        // value in [68,99] works; 90 sits comfortably in the middle. Height-only, deliberately no
        // width check: a width floor would wrongly exclude the legitimate Points Shop tab button
        // (160 wide, 115 tall).
        private const float MinBorderableHeight = 90f;

        private const string BorderChildName = "UniversalBorder (Generated)";

        // Runtime Sprite.name for Assets/_Game/Sprites/UI/Generated/GoldOrangeGradient.png (guid
        // fdb2114c148d91b4ebe462ad57e169ac) -- the leftover baked-color gradient texture found
        // directly on RebirthTriggerButton's own root Image (and, as a separate child rather than
        // its own sprite, on ConvertButton -- see the "Gradient" child handling below). Named
        // match rather than a blanket "clear every managed button's sprite": a scan of every
        // managed button's own Image confirmed everything else either has no sprite, a plain
        // white alpha mask (RoundedRect8), or a Unity built-in background shape -- all safe to
        // tint -- but SettingsButton's gear and any future icon button prove a blind sprite=null
        // isn't safe in general, only for this specific known-bad texture.
        private const string LeftoverGradientSpriteName = "GoldOrangeGradient_0";

        // 2026-09-29 color pass (genre convention -- Egg Inc/AdVenture Capitalist/Idle Miner all
        // use one fixed fill across every button; only STATE (actionable vs not) changes label
        // brightness, never per-button hue). Only the stage border FRAME art still varies by
        // World Restoration stage -- fill and base label color are now constant everywhere.
        // Actionable/dim states and THE SNOTTING's cyan hero highlight are applied by each
        // button's own owning controller (MainUIController, RebirthUIController, etc.), not here
        // -- this class only ever sets the static baseline every bordered/themed button starts
        // from, since it has no idea which buttons are stateful.
        private static readonly Color BaseFillColor = new Color32(0x1B, 0x0F, 0x2E, 0xFF);
        private static readonly Color BaseLabelColor = Color.white;

        private readonly List<Button> managedButtons = new List<Button>();
        private ButtonTheme overrideTheme;

        /// <summary>Scene lookup for late-built buttons (POCKET/WALLET/RECOVER IQ etc.) that construct
        /// themselves at runtime with no defined Start() ordering against this class's own scan.</summary>
        public static UniversalButtonBorderApplier Instance => FindAnyObjectByType<UniversalButtonBorderApplier>();

        /// <summary>
        /// Fires once after every full managed-button theme pass (initial Start(), a World
        /// Restoration stage change, or ApplyOverrideTheme/ClearOverrideTheme) -- after
        /// ApplyThemeToAllManagedButtons has finished resetting every label to the static
        /// BaseLabelColor baseline. Static so a subscriber can wire up in its own Awake() (which
        /// always completes before ANY object's Start(), including this class's) rather than
        /// racing this class's own Start() the same way the late-built-button problem did --
        /// MainUIController/RebirthUIController subscribe here and re-run their own
        /// actionable/dim-vs-bright refresh, fixing labels that otherwise started white on
        /// startup and only corrected on the first currency event.
        /// </summary>
        public static event Action OnThemeApplied;

        /// <summary>Whichever theme is currently being applied -- the override if one is set, otherwise whatever the current World Restoration stage resolves to.</summary>
        public ButtonTheme ActiveTheme => overrideTheme != null ? overrideTheme : ResolveThemeForStage(ResolveCurrentStageIndex());

        /// <summary>
        /// Entry point for a button built after this class's own Start() scan already ran (e.g.
        /// PocketPanelUI/TimedPurchaseWalletUI/RewardedAdRecoveryHudButtonUI, which self-bootstrap
        /// via RuntimeInitializeOnLoadMethod but construct their actual Button GameObject inside
        /// their own Start() -- a same-frame race against DiscoverButtons with no ordering
        /// guarantee either way). Themes it immediately with whatever's currently active, and adds
        /// it to managedButtons so later stage changes keep re-theming it too.
        /// </summary>
        public void ApplyToButton(Button button)
        {
            if (button == null) { return; }
            if (!managedButtons.Contains(button)) { managedButtons.Add(button); }
            ApplyThemeToButton(button, ActiveTheme);
        }

        /// <summary>
        /// 2026-09-30 fix: discovery moved here from Start(). RebirthUIController.Start() (and
        /// potentially other button-owning controllers) independently zeroes its own root Image's
        /// alpha as part of the baked-border-art fill design -- Start() order between different
        /// scripts is NOT guaranteed, so if that happened to run before this class's own Start(),
        /// DiscoverButtons would see an already-zero-alpha root Image with no border child yet
        /// existing (nothing has themed it yet), IsVisualButton would return false, and the button
        /// would be silently excluded from managedButtons forever -- no later rescan ever runs.
        /// Confirmed live: RebirthTriggerButton dropped out entirely ("Bordered 56 buttons", was
        /// 57). Moving the scan to Awake() closes the race at its root rather than patching
        /// IsVisualButton's heuristic further -- ALL objects' Awake() calls finish before ANY
        /// object's Start() begins, a stronger guarantee than execution-order tuning between two
        /// specific scripts' Start() methods.
        /// </summary>
        private void Awake()
        {
            DiscoverButtons();
        }

        private void Start()
        {
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
            if (ownImage == null || ownImage.color.a > 0f) { return true; }

            // 2026-09-30: a FRAMED button's root Image is deliberately driven to zero alpha by
            // ApplyThemeToButton (its visible fill is now baked directly into the border art
            // itself, see ButtonBorder_Stage*.png) -- that's not the same "invisible full-screen
            // tap-catcher" case this alpha check exists to exclude, and those never get a border
            // child. Checking for the border child it already has from a prior successful pass is
            // what lets a framed button keep getting re-themed on later stage changes instead of
            // this check now permanently excluding it the moment its own alpha first reaches 0.
            return button.transform.Find(BorderChildName) != null;
        }

        /// <summary>
        /// Regression fix (God Shop buy buttons losing their border): a plain `rect.height` read
        /// is invalid while the button's hierarchy is inactive (the God Shop tab starts hidden,
        /// so its buttons' own RectTransforms still hold their prefab-authored placeholder size
        /// -- BuyButton's own sizeDelta is a fixed 180x80 -- until a real layout pass runs, which
        /// requires being active). Neither BuyButton nor its immediate parent BuyColumn has a
        /// usable height (BuyButton has no LayoutElement at all; BuyColumn's own MinHeight/
        /// PreferredHeight are both -1/unset, only its width is constrained) -- the row's actual
        /// intended height (210) only exists as a LayoutElement several levels up, on the row
        /// root object. Walks up the hierarchy for the first LayoutElement with a real
        /// (non-negative) preferredHeight or minHeight; falls back to the button's own rect
        /// height only if nothing in the chain has one (true for simple utility buttons like
        /// Dia-Log/close-X, which have no LayoutElement anywhere in their ancestry and whose own
        /// sizeDelta already is the truth).
        /// </summary>
        private static float ResolveEffectiveHeight(RectTransform buttonRect)
        {
            for (Transform t = buttonRect; t != null; t = t.parent)
            {
                LayoutElement layoutElement = t.GetComponent<LayoutElement>();
                if (layoutElement == null) { continue; }
                if (layoutElement.preferredHeight >= 0f) { return layoutElement.preferredHeight; }
                if (layoutElement.minHeight >= 0f) { return layoutElement.minHeight; }
            }

            return buttonRect.rect.height;
        }

        /// <summary>Idempotent: finds the existing generated border child if this button already has one instead of duplicating it. Border-only -- see ApplyThemeToButton for the full border+fill+text application.</summary>
        public Image EnsureBorderOn(Button button)
        {
            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect == null || !IsVisualButton(button)) { return null; }

            // Small utility buttons (Dia-Log, WALLET, POCKET, RECOVER IQ, every close-X) have no
            // room for the sliced border's own fixed pixel margins -- those margins already
            // exceed a 140x50 button's entire size on every stage, so the border art dominates
            // the whole face and swallows the label instead of framing it. Below this height,
            // skip the border child entirely rather than draw one that can only look broken.
            if (ResolveEffectiveHeight(buttonRect) < MinBorderableHeight) { return null; }

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

            // First sibling, not last -- the border sprite is center-filled, so as last sibling
            // it rendered on top of every bordered button's own label/icon children, burying the
            // text (God Shop prices included). The button's own background Image isn't a sibling
            // (it's a component on the button GameObject itself, drawn first regardless); first-
            // sibling here just means the border draws before -- underneath -- whatever label/icon
            // children the button already has.
            borderObject.transform.SetAsFirstSibling();
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

            // UpgradeSlotUI owns its buy-button surface and label colors because those communicate
            // locked / affordable / unavailable state. The stage border sprites are center-filled,
            // so even applying only their frame would paint over that semantic presentation.
            bool rowOwnsPresentation = IsUpgradeSlotBuyButton(button);
            if (rowOwnsPresentation) { return; }

            Image border = EnsureBorderOn(button);
            if (border != null && theme.borderSprite != null)
            {
                border.sprite = theme.borderSprite;
            }

            Image ownImage = button.GetComponent<Image>();

            if (border != null)
            {
                // 2026-09-30: the visible fill is now baked directly into each stage's border art
                // (the frame's own hollow interior is flood-filled with the base purple -- see
                // ButtonBorder_Stage*.png) instead of drawn separately on the root Image or a
                // generated child. Neither a plain flat fill nor a generated inset "pill" child
                // could reliably match the frame's own hand-authored silhouette without poking
                // past it at the corners (a blurry oval, in the pill child's case) -- baking the
                // fill into the art itself sidesteps that geometry mismatch entirely. The root
                // Image becomes a pure invisible click-catcher (raycastTarget untouched, so the
                // tap area is unchanged) and targetGraphic retargets to the border so hover/press/
                // disabled ColorTint still visibly drives something. See IsVisualButton's border-
                // child fallback -- required so this alpha-0 root Image doesn't get mistaken for
                // an invisible full-screen tap-catcher on a later re-theme.
                if (ownImage != null)
                {
                    Color rootColor = ownImage.color;
                    rootColor.a = 0f;
                    ownImage.color = rootColor;
                }

                button.targetGraphic = border;
            }
            else if (ownImage != null)
            {
                // Small utility button below MinBorderableHeight -- no frame to bake a fill into,
                // so the flat genre-convention fill (2026-09-29) stays directly on the root Image,
                // unchanged from before this pass.
                ownImage.color = BaseFillColor;

                // Narrowly scoped, not a blanket clear: some managed buttons use their own Image
                // sprite as a real icon (a scan of every managed button found none today, but
                // nothing guarantees a future one won't), so only the specific known-bad texture
                // gets removed. Tinting a colored sprite can never produce a clean flat fill --
                // BaseFillColor above was multiplying against this texture's own baked hues,
                // reading live as dark maroon/brown next to SHOP's clean purple.
                if (ownImage.sprite != null && ownImage.sprite.name == LeftoverGradientSpriteName)
                {
                    ownImage.sprite = null;
                }
            }

            // Step 1b fix: ConvertButton (and possibly a future button) carries a leftover
            // decorative "Gradient" overlay child -- a translucent Image drawn on top of the
            // button's own root Image, baked with its own gradient-colored sprite. The block
            // above only ever touches the button's OWN Image component, never children, so this
            // child kept rendering its old color regardless of what the base fill above became.
            // Deactivate it generically by name rather than special-casing ConvertButton, in case
            // the same leftover pattern shows up elsewhere later.
            Transform gradientChild = button.transform.Find("Gradient");
            if (gradientChild != null)
            {
                gradientChild.gameObject.SetActive(false);
            }

            // Step 1b fix: Unity's own ColorTint transition (Button.Transition.ColorTint) tints
            // the targetGraphic by m_DisabledColor whenever Button.interactable is false --
            // several buttons ship a ~50%-alpha grey disabledColor (e.g. RestoreButton), which
            // fights this pass's "fill is always the constant base, only the label communicates
            // actionable state" design: a disabled RESTORE looked near-transparent even though
            // its Image.color was correctly set above. Neutralizing disabledColor to opaque white
            // (colorMultiplier already 1) keeps Unity's own interactable gating/click-blocking
            // intact -- per Aceyfer's explicit call not to touch interactable/gating logic --
            // while removing the visual tint. Generic across every managed button, not just
            // RestoreButton, since ConvertUIController's panel-internal buttons use the same
            // interactable-gating pattern and would hit the identical problem once themed.
            if (button.transition == Selectable.Transition.ColorTint)
            {
                ColorBlock colors = button.colors;
                colors.disabledColor = Color.white;
                colors.colorMultiplier = 1f;
                button.colors = colors;
            }

            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (theme.labelFontMaterial != null) { label.fontSharedMaterial = theme.labelFontMaterial; }

                // Baseline is always bright white -- the owning controller (MainUIController,
                // RebirthUIController, GodTierStoreSlotUI) dims this to grey on its own
                // actionable-state refresh, applied after this baseline runs. theme.labelTextColor
                // is no longer consulted; label hue is state-driven now, not stage-driven.
                label.color = BaseLabelColor;

                // Item 7: dark outline for label contrast on busy frames (Stage 5's gold filigree
                // especially). outlineWidth/outlineColor create a per-label MATERIAL INSTANCE
                // derived from whatever fontSharedMaterial this label already has -- unlike
                // assigning a shared material directly (item 7's first attempt, reverted: a
                // material's _MainTex is bound to one specific font atlas, so a label using a
                // different font asset sampled the wrong atlas and rendered as garbled glyphs),
                // this works for any font since it derives from the label's own existing
                // material instead of replacing it. Unconditional, not theme-gated -- every
                // managed button's labels get this regardless of stage.
                //
                // Real regression caught live: TMP_Text.outlineWidth's setter
                // (TextMeshProUGUI.SetOutlineThickness) lazily creates a per-instance font
                // material from m_sharedMaterial -- for a label that has NEVER been active (its
                // Awake/material setup never ran, true for buttons inside a still-hidden modal,
                // which this class deliberately discovers via FindObjectsInactive.Include), that
                // state isn't populated yet and the setter throws a NullReferenceException.
                // Uncaught, that exception aborted ApplyThemeToAllManagedButtons' entire for loop
                // -- every button ordered after the failing one (including the bottom bar and
                // God Shop rows) silently never got themed at all. isActiveAndEnabled skips the
                // known case; the try/catch is a hard backstop so no future/unknown TMP edge
                // case can ever again take the whole batch down over a purely decorative outline.
                // Accepted limitation: a label inside a still-hidden modal won't get the outline
                // until something re-applies the theme to it later -- there's no rescan today.
                if (!label.isActiveAndEnabled) { continue; }

                try
                {
                    label.outlineWidth = 0.2f;
                    label.outlineColor = Color.black;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UniversalButtonBorderApplier] Failed to set label outline on '{label.name}': {ex.Message}", label);
                }
            }
        }

        private static bool IsUpgradeSlotBuyButton(Button button)
        {
            if (UpgradeSlotUI.OwnsButtonPresentation(button))
            {
                return true;
            }

            // BuildShop names each instantiated row UpgradeSlot_<buildingName>. Keep this
            // structural fallback because the global applier and the shop can initialize in
            // either Start order after GameManager becomes ready.
            if (button.name != "BuyButton")
            {
                return false;
            }

            Transform current = button.transform.parent;
            while (current != null)
            {
                if (current.name.StartsWith("UpgradeSlot_"))
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
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

            OnThemeApplied?.Invoke();
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
