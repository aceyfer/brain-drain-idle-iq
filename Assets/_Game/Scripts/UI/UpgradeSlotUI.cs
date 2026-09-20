using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;
using System.Collections.Generic;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single building row in the shop scroll list.
    /// Binds to a BuildingData template and reflects locked / affordable / too-expensive states.
    /// </summary>
    public sealed class UpgradeSlotUI : MonoBehaviour
    {
        // Visual-style-guide semantic palette. The card remains carbon; these colors communicate
        // identity/state through the rail and price instead of flooding the whole row.
        private static readonly Color CardColor = new Color32(0x11, 0x15, 0x1B, 0xF5);
        // Lightened from the original (0x18, 0x20, 0x28) -- that value sat too close to
        // CardColor (0x11, 0x15, 0x1B) to read as a distinct control once the rounded-rect
        // sprite bug below is fixed; see 2026-09-16 button-polish pass.
        private static readonly Color PurchaseSurfaceColor = new Color32(0x25, 0x30, 0x3C, 0xFF);
        private static readonly Color LockedColor = new Color32(0x59, 0x61, 0x6A, 0xFF);
        private static readonly Color LockedPriceColor = new Color32(0xF2, 0xF0, 0xE8, 0xFF);
        private static readonly Color AffordableColor = new Color32(0x75, 0xF0, 0x4C, 0xFF);
        private static readonly Color TooExpensiveColor = new Color32(0x9B, 0xA8, 0xB5, 0xFF);
        private static readonly Color BrainPowerIdentityColor = new Color32(0x00, 0xDD, 0xEB, 0xFF);
        private static readonly Color CashIdentityColor = new Color32(0xF5, 0xC5, 0x42, 0xFF);
        private static readonly HashSet<UnityEngine.UI.Button> PresentationOwnedButtons = new();

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [FormerlySerializedAs("levelText")]
        [SerializeField] private TextMeshProUGUI countText;

        [Header("Interaction")]
        [SerializeField] private UnityEngine.UI.Button buyButton;
        [SerializeField] private UnityEngine.UI.Image background;
        [Tooltip("Thin semantic rail: cyan for BP Upgrades, gold for Cash Investments, ash while locked.")]
        [SerializeField] private UnityEngine.UI.Image identityRail;

        [Tooltip("9-sliced rounded-rect fill for the buy button (defaults to RoundedRect8 if left unassigned in the Inspector). Reapplied every RefreshState -- see EnsureBuyButtonShape's doc comment for why.")]
        [SerializeField] private Sprite buyButtonFillSprite;

        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI DescriptionText { get => descriptionText; set => descriptionText = value; }
        public TextMeshProUGUI CostText { get => costText; set => costText = value; }
        public TextMeshProUGUI CountText { get => countText; set => countText = value; }
        public UnityEngine.UI.Button BuyButton { get => buyButton; set => buyButton = value; }
        public UnityEngine.UI.Image Background { get => background; set => background = value; }
        public UnityEngine.UI.Image IdentityRail { get => identityRail; set => identityRail = value; }

        private BuildingData boundData;
        private UpgradeManager boundManager;

        /// <summary>Cached Image component for buyButton.targetGraphic -- resolved once in Bind() rather than re-cast on every ApplyAccent call.</summary>
        private UnityEngine.UI.Image buyButtonImage;

        /// <summary>Cached result of the last RefreshState purchasability computation (unlocked
        /// AND past the BP gate AND currently affordable) -- read by HandleBuyClicked so a tap
        /// that can't actually purchase gets a shake instead of silently doing nothing, the same
        /// pattern RebirthUIController.isSnottingUnlocked uses for the Snotting button.</summary>
        private bool canPurchaseNow;

        /// <summary>Tracks whether the affordable pulse coroutine is currently running on this
        /// row, so UpdateAffordablePulse only calls Play/Stop on an actual state transition
        /// instead of every RefreshState call. RefreshState fires on every currency change
        /// (ShopUIController.RefreshAllSlots, wired to OnCurrencyChanged/OnCashChanged/etc.),
        /// which for an idle game means on every tap -- restarting PlayAffordablePulse's
        /// coroutine that often would reset its sine phase to 0 each time (StopAndReplace kills
        /// and restarts fresh), turning a smooth breathing pulse into a flicker tied to tap rate.
        /// That is almost certainly the real reason the previous attempt at this read as "too
        /// flashy" rather than the alpha range alone.</summary>
        private bool isPulsing;

        /// <summary>Binds this slot to a building template and wires the buy button.</summary>
        public void Bind(BuildingData data, UpgradeManager manager)
        {
            boundData = data;
            boundManager = manager;

            if (buyButton != null)
            {
                PresentationOwnedButtons.Add(buyButton);
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
                buyButtonImage = buyButton.targetGraphic as UnityEngine.UI.Image;
            }
        }

        internal static bool OwnsButtonPresentation(UnityEngine.UI.Button button)
        {
            return button != null && PresentationOwnedButtons.Contains(button);
        }

        /// <summary>
        /// Denial-shake cooldown shared across every UpgradeSlotUI instance (not per-row) so
        /// browsing a scroll list of unaffordable buildings can't fire overlapping/rapid shakes
        /// on several rows in the same frame -- there's no accompanying narrator line here
        /// (unlike RebirthUIController's single, rare Snotting button) precisely because this can
        /// be tapped far more often across 16 rows, and a line per tap would read as spam.
        /// </summary>
        private static float lastDenialShakeTime = float.NegativeInfinity;
        private const float DenialShakeCooldownSeconds = 0.15f;

        /// <summary>
        /// Assets/Plans/tutorial-direction-and-cogs-trust.md's Option B "first affordable
        /// building" proactive nudge: fires UINudgePointer at the first row that becomes
        /// affordable while the player owns zero buildings. Session-only, not persisted via
        /// SaveManager -- gated on boundManager.BuildingLevels.Count == 0 so a reloaded save with
        /// existing buildings never re-triggers it, and it fires at most once per app launch.
        /// Static (like lastDenialShakeTime above) so it coordinates across every row instance
        /// rather than needing a separate manager class.
        /// </summary>
        private static bool hasShownFirstAffordableNudge;

        private void HandleBuyClicked()
        {
            if (boundManager == null || boundData == null)
            {
                return;
            }

            if (!canPurchaseNow)
            {
                PlayDenial();
                return;
            }

            boundManager.TryBuyBuilding(boundData);
        }

        /// <summary>
        /// Locked/unaffordable-tap feedback -- mirrors RebirthUIController.PlaySnottingDenial's
        /// shake-on-every-tap approach (Assets/Plans/tutorial-direction-and-cogs-trust.md's
        /// "reactive gap" fix) so a building row that can't be bought stops silently swallowing
        /// the click. Shakes the buy button itself when available, falling back to the row
        /// background so the row still reacts even if buyButton's RectTransform isn't wired.
        /// </summary>
        private void PlayDenial()
        {
            if (Time.unscaledTime - lastDenialShakeTime < DenialShakeCooldownSeconds)
            {
                return;
            }
            lastDenialShakeTime = Time.unscaledTime;

            RectTransform shakeTarget = buyButton != null
                ? buyButton.GetComponent<RectTransform>()
                : (background != null ? background.rectTransform : null);

            if (shakeTarget != null)
            {
                AnimationController.PlayDenialShake(shakeTarget);
            }
        }

        /// <summary>Recomputes labels and visual state based on current currency.</summary>
        public void RefreshState(CurrencyManager currency)
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            double cost = boundManager.GetCurrentCost(boundData);
            int level = boundManager.GetBuildingLevel(boundData);
            int worldStageIndex = WorldRestorationManager.Instance != null
                ? WorldRestorationManager.Instance.CurrentStageIndex
                : 0;

            // Reveal = first-affordable latch: owned, or lifetime currency ever reached the real
            // unlock gate (unlockCumulativeBrainPower), not baseCost -- baseCost is the price,
            // not the progression gate, and using it let far-tier buildings (e.g. Brain-Rot
            // Think Tank, gated at 725,000 unlockCumulativeBrainPower) reveal as soon as
            // cumulative currency passed their much lower baseCost instead. Always compared
            // against CumulativeBrainPower regardless of the item's own costType -- the unlock
            // gate is BP-denominated for every building, Cash-priced or not (PROJECT_BIBLE.md
            // §6 lists "Unlock (cum. BP)" as the gate column for both tabs), matching
            // pastPurchaseGate below and ShopQuery.cs's equivalent check. Cumulative BP is
            // monotonic, so this never un-reveals on spend.
            bool isCash = UpgradeManager.IsCashCost(boundData);
            bool everAfforded = currency != null && currency.CumulativeBrainPower >= boundData.unlockCumulativeBrainPower;
            bool unlocked = level > 0 || everAfforded;

            // Purchase still gated by the BP progression gate (economy unchanged).
            bool pastPurchaseGate = currency != null && currency.CumulativeBrainPower >= boundData.unlockCumulativeBrainPower;
            bool affordable = unlocked && pastPurchaseGate && boundManager.CanAffordBuilding(boundData);
            canPurchaseNow = affordable;
            UpdateAffordablePulse(affordable);
            MaybeShowFirstAffordableNudge(affordable);

            if (countText != null)
            {
                countText.text = $"OWNED: {level}";
                countText.fontSize = 28f; // matches ShopRowView's countText scale
            }

            if (descriptionText != null)
            {
                if (unlocked)
                {
                    double bppsMult = (currency != null) ? (currency.RebirthMultiplier * currency.ShopAllMultiplier) : 1d;
                    double cashMult = (currency != null) ? (currency.CashMultiplier * currency.ShopCashMultiplier * currency.ShopAllMultiplier) : 1d;

                    double singleBpps = boundData.baseBrainPowerPerSecond * bppsMult;
                    double singleCash = boundData.baseCashPerSecond * cashMult;

                    string perLevel = "";
                    if (boundData.baseBrainPowerPerSecond > 0)
                        perLevel += $"+{NumberFormatter.Format(singleBpps)} BP/s";
                    if (boundData.baseCashPerSecond > 0)
                    {
                        if (perLevel.Length > 0) perLevel += "  ";
                        perLevel += $"+${NumberFormatter.Format(singleCash)}/s";
                    }
                    if (boundData.tapBrainPowerPerLevel > 0)
                    {
                        if (perLevel.Length > 0) perLevel += "  ";
                        perLevel += $"+{NumberFormatter.Format(boundData.tapBrainPowerPerLevel)} BP/tap";
                    }

                    string totalLine = "";
                    if (level > 0)
                    {
                        double totalBpps = level * boundData.baseBrainPowerPerSecond * bppsMult;
                        double totalCash = level * boundData.baseCashPerSecond * cashMult;

                        string totalBP = boundData.baseBrainPowerPerSecond > 0
                            ? $"+{NumberFormatter.Format(totalBpps)} BP/s"
                            : "";
                        string totalC = boundData.baseCashPerSecond > 0
                            ? $"+${NumberFormatter.Format(totalCash)}/s"
                            : "";
                        string totalTap = boundData.tapBrainPowerPerLevel > 0
                            ? $"+{NumberFormatter.Format(level * boundData.tapBrainPowerPerLevel)} BP/tap"
                            : "";
                        string totalParts = "";
                        if (totalBP.Length > 0) totalParts = totalBP;
                        if (totalC.Length > 0) totalParts += (totalParts.Length > 0 ? "  " : "") + totalC;
                        if (totalTap.Length > 0) totalParts += (totalParts.Length > 0 ? "  " : "") + totalTap;
                        totalLine = $"\n<color=#FFD700>TOTAL ({level}×): {totalParts}</color>";
                    }

                    descriptionText.text = $"{boundData.GetDescription(worldStageIndex)}\n<color=#00F0FF><b>Per level: {perLevel}</b></color>{totalLine}";
                }
                else
                {
                    descriptionText.text = "Access restricted by the Ministry.";
                }
                descriptionText.fontSize = 22f;
            }

            if (!unlocked)
            {
                if (nameText != null)
                {
                    nameText.text = ClassificationTier.GetLabel(boundData.unlockCumulativeBrainPower);
                    nameText.fontSize = 32f; // matches RestorationSlotUI/ShopRowView/CashShopSlotUI's shared shop-row scale
                }
                if (countText != null) countText.text = string.Empty;
                if (costText != null)
                {
                    // Same number as the "REACH X BP" text below once revealed -- both locked
                    // sub-states show unlockCumulativeBrainPower, the real gate. Never $-prefixed
                    // here even for Cash-priced buildings: unlockCumulativeBrainPower is always a
                    // BP figure, and a "$" in front of a BP number is misleading regardless of
                    // the item's own costType. $ formatting is reserved for an actual Cash price
                    // on an unlocked, purchasable row (see below).
                    costText.text = $"{NumberFormatter.Format(boundData.unlockCumulativeBrainPower)} BP REQUIRED";
                    costText.fontSize = 30f; // unified with the unlocked-state cost text below (was 24 vs 26 -- same field, two sizes)
                }
                // The global stage theme gives BuyButton a saturated fill. Keep the locked rail
                // ash, but use bone white for the requirement so it remains readable over that
                // inherited fill instead of putting low-contrast ash text on teal.
                ApplyAccent(LockedPriceColor, LockedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            if (nameText != null)
            {
                nameText.text = boundData.GetDisplayName(worldStageIndex);
                nameText.fontSize = 32f; // matches RestorationSlotUI/ShopRowView/CashShopSlotUI's shared shop-row scale
            }
            if (costText != null)
            {
                costText.text = !pastPurchaseGate
                    ? $"REACH {NumberFormatter.Format(boundData.unlockCumulativeBrainPower)} BP"
                    : isCash
                        ? $"${NumberFormatter.Format(cost)}"
                        : $"{NumberFormatter.Format(cost)} BP";
                costText.fontSize = 30f; // unified with the locked-state cost text above (was 24 vs 26 -- same field, two sizes)
            }

            Color identityColor = isCash ? CashIdentityColor : BrainPowerIdentityColor;
            ApplyAccent(affordable ? AffordableColor : TooExpensiveColor, identityColor);

            // Keep interactable so the player can attempt purchase; manager silently rejects if unaffordable.
            if (buyButton != null) buyButton.interactable = true;
        }

        private void OnDestroy()
        {
            if (buyButton != null)
            {
                PresentationOwnedButtons.Remove(buyButton);
            }

            if (background != null)
            {
                AnimationController.StopAffordablePulse(background.rectTransform);
            }
        }

        private void UpdateAffordablePulse(bool affordable)
        {
            // Re-enabled 2026-08-30 with a narrower, slower pulse (AnimationController's alpha
            // swing went from 0.4-1.0/1.0s to 0.82-1.0/1.3s) after the original 2026-07-era pulse
            // was disabled here as "too flashy" -- see
            // Assets/Plans/tutorial-direction-and-cogs-trust.md, Option A. Only calls Play/Stop on
            // an actual affordable-state transition (see isPulsing's doc comment) -- calling
            // PlayAffordablePulse unconditionally on every RefreshState would restart the
            // coroutine's sine phase on every tap-driven currency change, which is the more
            // likely culprit behind "too flashy" than the alpha range alone. If several
            // affordable rows pulsing at once still reads as too busy in Play Mode, the next step
            // is restricting this to a single recommended row rather than narrowing the range
            // further -- do not just re-disable it silently again.
            if (background == null)
            {
                return;
            }

            if (affordable && !isPulsing)
            {
                AnimationController.PlayAffordablePulse(background.rectTransform, background);
                isPulsing = true;
            }
            else if (!affordable && isPulsing)
            {
                AnimationController.StopAffordablePulse(background.rectTransform);
                isPulsing = false;
            }
        }

        /// <summary>
        /// See hasShownFirstAffordableNudge's doc comment. UINudgePointer.Instance is null until
        /// PlaceholderArtGenerator's "Nudge Pointer" menu item has been run at least once in this
        /// project -- the null-conditional call is the expected no-op path until then, not an
        /// error.
        /// </summary>
        private void MaybeShowFirstAffordableNudge(bool affordable)
        {
            if (!affordable || hasShownFirstAffordableNudge)
            {
                return;
            }

            if (boundManager == null || boundManager.BuildingLevels.Count > 0)
            {
                return;
            }

            RectTransform nudgeTarget = buyButton != null
                ? buyButton.GetComponent<RectTransform>()
                : (background != null ? background.rectTransform : null);

            if (nudgeTarget == null)
            {
                return;
            }

            // Clamp to the enclosing scroll view's viewport, if this row lives in one (it always
            // should, in practice) -- 2026-08-30/31 Play Mode testing found the arrow's own
            // offset+height footprint can rise above a first-row target's top edge far enough to
            // visually collide with whatever sits above the scroll view (here, the shop's tab
            // bar). The viewport is exactly the boundary that already clips the row, so clamping
            // to it keeps the arrow from ever poking out past what's actually visible, at any row
            // position -- not just the first one.
            //
            // includeInactive:true is required here -- RefreshState (and therefore this method)
            // runs continuously as currency changes, via the same background refresh that already
            // drives UpdateAffordablePulse, regardless of whether the player has the shop panel
            // open. The first-affordable moment can easily land while the shop (and everything
            // under it, including this row's ScrollRect ancestor) is still closed/inactive --
            // GetComponentInParent defaults to excluding inactive objects, which silently returned
            // null here and made the clamp above a no-op even though the row genuinely does live
            // in a ScrollRect. The row's RectTransform layout is still valid while inactive, so
            // the clamp math itself works fine once the ScrollRect is actually found.
            UnityEngine.UI.ScrollRect scrollRect = nudgeTarget.GetComponentInParent<UnityEngine.UI.ScrollRect>(true);
            RectTransform clampArea = scrollRect != null
                ? (scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>())
                : null;

            hasShownFirstAffordableNudge = true;
            UINudgePointer.Instance?.PointAt(nudgeTarget, clampArea);
        }

        private void ApplyAccent(Color stateColor, Color identityColor)
        {
            if (background != null)
            {
                background.color = CardColor;
            }

            if (identityRail != null)
            {
                identityRail.color = identityColor;
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
                // Restore the font asset's own material in case the global button theme ran after
                // the slot was populated. Theme materials can carry a dark face color that
                // multiplies this vertex color and destroys contrast even when color is white.
                if (costText.font != null && costText.font.material != null)
                {
                    costText.fontSharedMaterial = costText.font.material;
                }
                costText.faceColor = Color.white;
                costText.color = stateColor;
            }

            if (buyButton != null)
            {
                // UpgradeSlotUI owns this button's semantic state. The universal stage theme may
                // still frame it, but the fill stays quiet so state-colored price text retains
                // contrast instead of competing with a saturated global fill.
                buyButton.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                UnityEngine.UI.ColorBlock colors = buyButton.colors;
                colors.normalColor = PurchaseSurfaceColor;
                colors.highlightedColor = Color.Lerp(PurchaseSurfaceColor, stateColor, 0.18f);
                colors.pressedColor = Color.Lerp(PurchaseSurfaceColor, stateColor, 0.32f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = Color.Lerp(PurchaseSurfaceColor, LockedColor, 0.2f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                buyButton.colors = colors;

                if (buyButton.targetGraphic != null)
                {
                    buyButton.targetGraphic.color = PurchaseSurfaceColor;
                }

                EnsureBuyButtonShape();
            }
        }

        /// <summary>
        /// UNRESOLVED as of 2026-09-16 -- see Assets/Docs/CODEX_VISUAL_STYLE_HANDOFF_2026-09-15.md
        /// for the full investigation. Short version: the original theory (a units mismatch where
        /// RoundedRect8's 16px spriteBorder needs pixelsPerUnitMultiplier tuned down to become
        /// visible) was tested exhaustively at 0.025, 0.1, 0.2, 1, 2, 4, and 20 against the live
        /// "Apex Brain Greens" BuyButton at real device resolution (iPhone 11 Pro, 2436x1125) and
        /// disproven -- every value in that range rendered identically. A `Debug.Log` placed at the
        /// very top of this method (before any null check) never fired even once across multiple
        /// Play sessions, including immediately after a real purchase that visibly updated that same
        /// row's price/owned text -- meaning RefreshState/ApplyAccent visibly ran, but this method,
        /// or the buyButton wiring feeding it, apparently did not. Manually overriding the live
        /// Image.color via the Inspector at runtime DID work and was visible on screen, confirming
        /// the visible button is a real, reachable object -- just not one this method appears to be
        /// touching. Left at the Unity default (1) rather than any of the tested values, since none
        /// were shown to have any confirmed effect on the actual rendered button. Do not resume
        /// tuning this multiplier without first confirming (e.g. via a differently-instrumented
        /// build, PlayMode test, or Frame Debugger) that this method actually executes against the
        /// on-screen button -- otherwise further multiplier changes are guaranteed to be as
        /// inconclusive as this session's.
        /// </summary>
        private void EnsureBuyButtonShape()
        {
            if (buyButtonImage == null)
            {
                return;
            }

            if (buyButtonImage.sprite == null && buyButtonFillSprite != null)
            {
                buyButtonImage.sprite = buyButtonFillSprite;
            }

            buyButtonImage.type = UnityEngine.UI.Image.Type.Sliced;
            buyButtonImage.pixelsPerUnitMultiplier = 1f;
        }
    }
}
