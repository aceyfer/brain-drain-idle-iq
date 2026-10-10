using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BrainDrain.Systems;
using BrainDrain.Core;

namespace BrainDrain.UI
{
    public sealed class RebirthUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject rebirthModalPanel;

        [Header("Visual Fields")]
        [SerializeField] private TextMeshProUGUI multiplierText;

        [Header("Interactive Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Visibility Gate")]
        [Tooltip("The 'REBIRTH' button GameObject in the HUD that opens this modal. Always active AND always interactable; the unlock gate lives in OpenModal/OnConfirmClicked instead of Button.interactable, so a locked tap can still react (denial shake + narrator line) instead of silently doing nothing.")]
        [SerializeField] private GameObject rebirthTriggerButton;

        [Header("Locked-Tap Denial")]
        [Tooltip("Narrator lines for a locked-Snotting tap. Defaulted in code so this works without Inspector wiring; retune here later if desired.")]
        [SerializeField] private string[] snottingDeniedLines =
        {
            "The Snotting is not for you yet. Spend more. Restore more. Then we'll talk.",
            "Locked. I put the requirement on the button. In a large font. For you specifically.",
            "Still locked. Tapping harder is not a restoration strategy.",
            "That button has a number on it. The number is not your number yet.",
            "Denied. The good news is the requirement is written right there. The bad news is you have to read it.",
        };

        /// <summary>Cooldown on the denial narrator line only -- the shake always plays on every
        /// tap (every tap feels acknowledged), but rapid tapping must not flood the dialogue
        /// queue. Same cooldown philosophy as DialogueManager's own RepeatTriggerCooldownSeconds
        /// (SS20), scoped to this local pool instead of the shared trigger system.</summary>
        private const float SnottingDenialLineCooldownSeconds = 1.5f;

        private bool triggerSuppressed;

        /// <summary>Cached result of the last ApplyTriggerButtonVisibility() unlock computation
        /// -- single source of truth for "is Snotting unlocked", read by OpenModal and
        /// OnConfirmClicked rather than each recomputing the condition separately.</summary>
        private bool isSnottingUnlocked;

        private string lastSnottingDeniedLine;
        private float lastSnottingDenialLineTime = float.NegativeInfinity;

        /// <summary>
        /// Hides the HUD trigger button while overlapping UI (the shop) is open. This
        /// controller stays the SOLE owner of the button's active state -- callers set the
        /// flag, and every visibility re-assert (including OnRestorationProgressChanged)
        /// respects it, so no second system ever fights this one over SetActive
        /// (PROJECT_BIBLE.md §8, the ShopUIController/ShopTabView double-wiring scar).
        /// </summary>
        public void SetTriggerSuppressed(bool suppressed)
        {
            triggerSuppressed = suppressed;
            ApplyTriggerButtonVisibility();
        }

        // 2026-09-29/30 color pass: fill is baked directly into the border art now (see
        // ApplyTriggerButtonVisibility), same as every other framed button -- THE SNOTTING is the
        // one deliberate hero highlight, and that highlight lives entirely in the LABEL (cyan + a
        // pulse when ready), not the fill. Previously this button's own fill swapped grey/hot-
        // pink; it no longer varies -- or exists as a separate color at all -- on this component.
        // 2026-10-05 PALETTE LOCKDOWN audit follow-up: now reference Palette directly instead of
        // their own duplicate literals, so the two can never drift apart again.
        private static readonly Color ReadyLabelColor = Palette.Cyan;
        private static readonly Color LockedLabelColor = Palette.Dim;

        /// <summary>Light cyan tint the ready-state border glow pulses toward, from white -- see
        /// ApplyTriggerButtonVisibility's hero-highlight block.</summary>
        private static readonly Color ReadyGlowColor = new Color32(0x80, 0xF4, 0xFF, 0xFF);

        /// <summary>Guards PlayAffordablePulse so it only (re)starts on the actual locked-&gt;ready
        /// transition -- ApplyTriggerButtonVisibility can run many times while already unlocked
        /// (every OnRestorationProgressChanged tick), and re-calling PlayAffordablePulse each time
        /// would restart the pulse's phase instead of letting it breathe continuously.</summary>
        private bool isReadyPulsing;

#if UNITY_EDITOR
        public GameObject TriggerButtonObject => rebirthTriggerButton;
#endif

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);

            if (rebirthTriggerButton != null)
            {
                rebirthTriggerButton.SetActive(true);
                // Move above the full-screen MainTapButton (a transparent raycast target that
                // would otherwise intercept every click aimed at this button).
                rebirthTriggerButton.transform.SetAsLastSibling();

                // Wire the trigger button to open the modal. RemoveListener first so a double-
                // Awake (e.g. DontDestroyOnLoad scene reload) can't stack duplicate listeners.
                Button btn = rebirthTriggerButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(OpenModal);
                    btn.onClick.AddListener(OpenModal);
                }

                // Child TextMeshPro elements must NOT be raycast targets — if they are, they
                // consume the pointer event before it reaches the Button component, so clicks
                // appear to do nothing even when the button is interactable.
                foreach (TextMeshProUGUI tmp in rebirthTriggerButton.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.raycastTarget = false;
                }

                // Button exposes no public press/release events of its own (only onClick, which
                // fires on release-inside) -- EventTrigger is the standard way to get PointerDown/
                // PointerUp so the ready-glow coroutine can hand off sole control of border.color
                // to Button.Transition.ColorTint for the press's duration (see
                // PauseReadyGlowForPress's doc comment). Wired once here rather than in
                // ApplyTriggerButtonVisibility, which runs repeatedly.
                EventTrigger trigger = rebirthTriggerButton.GetComponent<EventTrigger>();
                if (trigger == null) { trigger = rebirthTriggerButton.AddComponent<EventTrigger>(); }
                trigger.triggers.Clear();

                EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pointerDownEntry.callback.AddListener(_ => PauseReadyGlowForPress());
                trigger.triggers.Add(pointerDownEntry);

                EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                pointerUpEntry.callback.AddListener(_ => ResumeReadyGlowAfterPress());
                trigger.triggers.Add(pointerUpEntry);
            }

            // Static, subscribed here rather than in Start(): ALL objects' Awake() finish before
            // ANY object's Start() begins, so this is guaranteed to be wired before
            // UniversalButtonBorderApplier's own Start() resets this button to the base fill +
            // white label and fires the event -- same startup-label-race fix as MainUIController.
            UniversalButtonBorderApplier.OnThemeApplied -= HandleThemeApplied;
            UniversalButtonBorderApplier.OnThemeApplied += HandleThemeApplied;
        }

        private void Start()
        {
            ApplyTriggerButtonVisibility();

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationProgressChanged += HandleRestorationProgressChanged;
            }
        }

        private void OnDestroy()
        {
            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
            }

            UniversalButtonBorderApplier.OnThemeApplied -= HandleThemeApplied;
        }

        private void HandleRestorationProgressChanged(double _)
        {
            ApplyTriggerButtonVisibility();
        }

        private void HandleThemeApplied()
        {
            ApplyTriggerButtonVisibility();
        }

        /// <summary>
        /// Forces an immediate re-evaluation of the trigger button's interactable state and
        /// label text. Call this after any cheat or debug action that directly sets restoration
        /// progress, since those bypass the normal OnRestorationProgressChanged path.
        /// </summary>
        public void RefreshTriggerButton()
        {
            ApplyTriggerButtonVisibility();
        }

        private void ApplyTriggerButtonVisibility()
        {
            if (rebirthTriggerButton == null)
            {
                return;
            }

            rebirthTriggerButton.SetActive(!triggerSuppressed);

            // Toggling a child's active state under a HorizontalLayoutGroup (RestorationInteractiveRow,
            // ChildControlWidth/Height both on) does NOT itself trigger that group to recompute --
            // Unity only rebuilds a LayoutGroup automatically on rect/size changes, not plain
            // SetActive calls on a sibling. Without forcing a rebuild here, this button's very first
            // activation (when World Restoration progress first unlocks Snotting) rendered with
            // whatever anchors/anchoredPosition/sizeDelta happened to be last baked into the scene --
            // a large stale Y offset that placed its ArrowIcon over the top HUD's "BRAIN POWER" text
            // instead of inside the button itself, since the layout group never got a chance to
            // recompute the correct in-row position/size before the player saw it.
            RectTransform parentRect = rebirthTriggerButton.transform.parent as RectTransform;
            if (parentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            if (triggerSuppressed)
            {
                return;
            }

            double spent = WorldRestorationManager.Instance != null
                ? WorldRestorationManager.Instance.CumulativePointsSpentOnRestoration
                : 0d;
            // Fails closed: if RebirthManager isn't resolved yet, threshold is null and unlocked
            // stays false -- never reads as unlocked on a missing reference.
            double? unlockThreshold = RebirthManager.Instance?.SnottingUnlockThreshold;
            bool unlocked = unlockThreshold.HasValue && spent >= unlockThreshold.Value;
            isSnottingUnlocked = unlocked;

            // Always interactable now -- a disabled Unity Button never fires onClick, so a
            // locked tap used to run zero code and give zero feedback. The gate moved into
            // OpenModal/OnConfirmClicked instead, which read the cached isSnottingUnlocked
            // above rather than Button.interactable.
            Button btn = rebirthTriggerButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
            }

            // 2026-09-30: the visible fill is now baked directly into the border art itself (see
            // UniversalButtonBorderApplier / ButtonBorder_Stage*.png), same as every other framed
            // button -- this button's own root Image is a pure invisible click-catcher.
            // Previously this method re-opaqued the root Image to BaseFillColor on every
            // visibility refresh (far more often than the shared applier's own pass), which is
            // exactly why THE SNOTTING kept showing a plain opaque square instead of the frame's
            // actual pill shape -- this controller no longer sets a visible fill color at all,
            // only keeps alpha at 0. Hover/press feedback lives on the border Image now
            // (UniversalButtonBorderApplier retargets targetGraphic to it), not here.
            Image img = rebirthTriggerButton.GetComponent<Image>();
            if (img != null)
            {
                Color rootColor = img.color;
                rootColor.a = 0f;
                img.color = rootColor;

                // FIXED 2026-08-30 (found via Codex Play Mode test + temp logging, see
                // Assets/Plans/tutorial-direction-and-cogs-trust.md): this used to be
                // `img.raycastTarget = unlocked`, which left the locked button with zero
                // raycastable graphic (child InnerFill was gated the same way). With nothing on
                // this GameObject left for GraphicRaycaster to hit, a locked tap never reached
                // OpenModal() at all -- it fell through to whatever was underneath (the
                // full-screen MainTapButton), registering as a normal tap instead. That silently
                // defeated the "always interactable, gate inside OpenModal" fix above: the gate
                // logic was correct, but the tap could no longer arrive to be gated. Must stay
                // raycastable in both states now that OpenModal (not Button.interactable) is the
                // real gate.
                img.raycastTarget = true;
            }

            Transform innerFill = rebirthTriggerButton.transform.Find("InnerFill");
            Image innerFillImage = innerFill != null ? innerFill.GetComponent<Image>() : null;
            if (innerFillImage != null)
            {
                innerFillImage.raycastTarget = true;
            }

            TextMeshProUGUI txt = rebirthTriggerButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null)
            {
                txt.enableAutoSizing = true;
                if (unlocked)
                {
                    txt.text = "THE SNOTTING";
                    txt.fontSizeMin = 24f;
                    txt.fontSizeMax = 46.35f;
                    txt.color = ReadyLabelColor;
                }
                else
                {
                    txt.text = $"SNOTTING LOCKED\n{NumberFormatter.Format(spent)} / {NumberFormatter.Format(unlockThreshold.GetValueOrDefault())}";
                    txt.fontSizeMin = 16f;
                    txt.fontSizeMax = 36f;
                    txt.color = LockedLabelColor;
                }
            }

            // Hero highlight: a gentle color glow on the BORDER (which now carries the baked
            // purple fill -- see UniversalButtonBorderApplier), not the root Image, so the glow
            // follows the frame's actual pill shape instead of the root's plain rectangle. Guarded
            // so it (re)starts exactly once on the locked->ready transition rather than every
            // visibility refresh. The border Image is resolved lazily each call (ResolveBorderImage)
            // rather than cached, since the border child may not exist yet the very first time
            // this runs.
            Image border = ResolveBorderImage();
            if (unlocked && !isReadyPulsing)
            {
                if (border != null)
                {
                    AnimationController.PlayColorGlowPulse(border, Color.white, ReadyGlowColor);
                }
                isReadyPulsing = true;
            }
            else if (!unlocked && isReadyPulsing)
            {
                if (border != null)
                {
                    AnimationController.StopColorGlowPulse(border, Color.white);
                }
                isReadyPulsing = false;
            }
        }

        /// <summary>
        /// EnsureBorderOn is idempotent (finds-or-creates the same child every call), so this is
        /// cheap to call every refresh rather than caching a field that could go stale if the
        /// button's border child were ever rebuilt. Returns null before UniversalButtonBorderApplier
        /// exists in the scene -- callers already null-check.
        /// </summary>
        private Image ResolveBorderImage()
        {
            if (rebirthTriggerButton == null) { return null; }
            Button btn = rebirthTriggerButton.GetComponent<Button>();
            if (btn == null) { return null; }
            return UniversalButtonBorderApplier.Instance?.EnsureBorderOn(btn);
        }

        /// <summary>
        /// Button.Transition.ColorTint (targetGraphic = the border Image, see
        /// UniversalButtonBorderApplier) and the ready-state glow coroutine would otherwise both
        /// write border.color every frame during a press, racing each other. Pausing the glow for
        /// the press's duration (wired via EventTrigger in Awake, since Button exposes no public
        /// press/release events of its own) hands ColorTint sole control until release, when the
        /// glow resumes from wherever its own phase has reached -- a possible small color snap on
        /// resume, acceptable for a press that immediately opens the modal anyway.
        /// </summary>
        private void PauseReadyGlowForPress()
        {
            Image border = ResolveBorderImage();
            if (border != null)
            {
                AnimationController.PauseColorGlowPulse(border);
            }
        }

        private void ResumeReadyGlowAfterPress()
        {
            if (!isReadyPulsing) { return; }
            Image border = ResolveBorderImage();
            if (border != null)
            {
                AnimationController.PlayColorGlowPulse(border, Color.white, ReadyGlowColor);
            }
        }

        public void OpenModal()
        {
            if (!isSnottingUnlocked)
            {
                PlaySnottingDenial();
                return;
            }

            if (rebirthModalPanel == null)
            {
                Debug.LogWarning("[RebirthUIController] rebirthModalPanel is not assigned — cannot open The Snotting modal.", this);
                return;
            }

            // Ensure the modal renders above the trigger button and everything else.
            rebirthModalPanel.transform.SetAsLastSibling();
            rebirthModalPanel.SetActive(true);
            UpdateVisuals();

            // 2026-10-11 play-test fix round 2: UniversalButtonBorderApplier's own
            // ButtonBorder_Stage 9-slice is designed for a compact ~190x50 button -- stretched
            // across these buttons' new wide/short rect, only the frame's fixed-size corner
            // pieces stayed recognizable gold, reading as a plain black box with gold nubs at the
            // edges. CancelButton/ConfirmButton are now excluded from that system entirely
            // (AlertFrameButtonStyle.ManagedButtonNames) in favor of THIS class's own Alert_Frame
            // look -- same system WALLET/POCKET/RECOVER IQ/Dia-Log already use, which fits a wide
            // pill shape correctly. Re-applied every open (cheap, idempotent) rather than once in
            // Awake, so it's always current against the buttons' live size.
            if (confirmButton != null) { AlertFrameButtonStyle.Apply(confirmButton); }
            if (cancelButton != null) { AlertFrameButtonStyle.Apply(cancelButton); }

            // AlertFrameButtonStyle.Apply colors every label Cyan -- correct for SELL OUT (the
            // existing CTA accent), but ABORT needs to read as the visually safer/neutral default
            // per Aceyfer's explicit call, not match SELL OUT's accent color. Re-neutralized here,
            // after Apply, rather than skipping Apply for cancelButton (it still needs the same
            // frame sprite and padding Apply sets up).
            if (cancelButton != null)
            {
                TextMeshProUGUI cancelLabel = cancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (cancelLabel != null) { cancelLabel.color = Palette.White; }
            }

            // AlertFrameButtonStyle.Apply floors autosize at 16pt -- this modal's labels need 30.
            ReassertSnottingButtonLabelFloors();

            RectTransform panelRect = rebirthModalPanel.GetComponent<RectTransform>();
            CanvasGroup panelCanvasGroup = rebirthModalPanel.GetComponent<CanvasGroup>();
            AnimationController.PlayPopupSpawn(panelRect, panelCanvasGroup);
        }

        /// <summary>
        /// Locked-tap feedback. The shake always plays -- every tap should feel acknowledged --
        /// but the narrator line respects SnottingDenialLineCooldownSeconds so rapid tapping
        /// can't flood the dialogue queue.
        /// </summary>
        private void PlaySnottingDenial()
        {
            if (rebirthTriggerButton != null)
            {
                RectTransform rt = rebirthTriggerButton.GetComponent<RectTransform>();
                AnimationController.PlayDenialShake(rt);
            }

            if (Time.time - lastSnottingDenialLineTime < SnottingDenialLineCooldownSeconds)
            {
                return;
            }
            lastSnottingDenialLineTime = Time.time;

            string line = PickSnottingDeniedLine();
            if (!string.IsNullOrEmpty(line) && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowPriorityLine(line);
            }
        }

        /// <summary>
        /// Random pick from snottingDeniedLines, never repeating the immediately-previous line.
        /// This is the smallest analogue of DialogueManager.TryFireLine's own anti-repeat
        /// fallback tier (candidates.Where(line => line != lastPlayedLine)) -- its full 10-entry
        /// history window is tuned for a ~112-line shared pool and doesn't scale down
        /// meaningfully to this 5-line local array, so this mirrors the tier that does apply
        /// rather than inventing a second anti-repeat approach.
        /// </summary>
        private string PickSnottingDeniedLine()
        {
            if (snottingDeniedLines == null || snottingDeniedLines.Length == 0)
            {
                return null;
            }

            List<string> candidates = new List<string>();
            foreach (string candidate in snottingDeniedLines)
            {
                if (candidate != lastSnottingDeniedLine)
                {
                    candidates.Add(candidate);
                }
            }
            if (candidates.Count == 0)
            {
                candidates.AddRange(snottingDeniedLines);
            }

            string chosen = candidates[Random.Range(0, candidates.Count)];
            lastSnottingDeniedLine = chosen;
            return chosen;
        }

        public void CloseModal()
        {
            if (rebirthModalPanel != null)
            {
                rebirthModalPanel.SetActive(false);
            }
        }

        private void UpdateVisuals()
        {
            if (multiplierText == null || RebirthManager.Instance == null)
            {
                return;
            }

            int bpPct   = (int)(RebirthManager.Instance.PendingMultiplierIncrease     * 100);
            int cashPct = (int)(RebirthManager.Instance.PendingCashMultiplierIncrease * 100);
            int tapPct  = (int)(RebirthManager.Instance.PendingTapMultiplierIncrease  * 100);
            int nextTier = RebirthManager.Instance.RebirthCount + 1;
            string illumisnottyTitle = RebirthManager.GetIllumisnottyTitle(nextTier).ToUpper();

            // 2026-10-10 play-test fix: was one undifferentiated block at a 14-26pt floor/ceiling
            // inside a 10%-tall anchor band (fixed separately in RebirthModalReadabilityFix) --
            // rich-text size/color tags now give it real title > lose > keep/gain hierarchy
            // instead of relying on font size alone to separate sections. cyanHex is pulled from
            // Palette.Cyan rather than a hardcoded literal so this can never drift from the
            // palette if that color is ever retuned.
            string cyanHex = ColorUtility.ToHtmlStringRGB(Palette.Cyan);
            multiplierText.text =
                "<size=130%><b>THE SNOTTING</b></size>\n\n" +
                "<b>YOU LOSE</b>\n" +
                "Brain Power, Cash, Points\n" +
                "Buildings, World Restoration, IQ\n\n" +
                "<b>YOU KEEP</b>\n" +
                "Rank and permanent boosts\n\n" +
                $"<b><color=#{cyanHex}>REWARD</color></b>\n" +
                $"+{bpPct}% Brain Power\n" +
                $"+{cashPct}% Cash\n" +
                $"+{tapPct}% Tap Power\n" +
                "Better Cash → Points rate";

            if (!string.IsNullOrEmpty(illumisnottyTitle))
            {
                multiplierText.text += $"\n\nBecoming: {illumisnottyTitle}";
            }

            // Strong contrast against RebirthModalReadabilityFix's solid Palette.Surface panel,
            // regardless of whatever tint this TMP component's own scene-authored baseline was.
            multiplierText.color = Palette.White;
            multiplierText.enableAutoSizing = true;
            multiplierText.fontSizeMin = 28f;
            multiplierText.fontSizeMax = 40f;
        }

        private void OnConfirmClicked()
        {
            if (!isSnottingUnlocked)
            {
                // Should be unreachable -- OpenModal's own gate is what's supposed to keep a
                // locked state from ever reaching this modal now that the trigger button is
                // always interactable. RebirthManager.TriggerRebirth() has no precondition
                // check of its own, so this is the last line of defense; if this ever logs,
                // something let a locked state through and that's a real bug to chase, not a
                // silent no-op.
                Debug.LogWarning("[RebirthUIController] OnConfirmClicked fired while Snotting is locked -- this should be unreachable.", this);
                CloseModal();
                return;
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.TriggerRebirth();
            }
            CloseModal();
        }

        private void OnCancelClicked()
        {
            CloseModal();
        }

        /// <summary>2026-10-11 play-test fix round 2: now that CancelButton/ConfirmButton use
        /// AlertFrameButtonStyle.Apply instead of UniversalButtonBorderApplier (see OpenModal),
        /// the system stomping the floor changed too -- Apply itself floors autosize at 16pt.
        /// Called directly after Apply in OpenModal; no longer needs an OnThemeApplied hook since
        /// Apply is a one-shot call this class already re-runs on every open, not an ongoing
        /// per-stage re-theme.</summary>
        private void ReassertSnottingButtonLabelFloors()
        {
            ReassertButtonLabelFloor(confirmButton);
            ReassertButtonLabelFloor(cancelButton);
        }

        private static void ReassertButtonLabelFloor(Button button)
        {
            if (button == null) { return; }
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) { return; }

            label.enableAutoSizing = true;
            label.fontSizeMin = 30f;
            if (label.fontSizeMax < 30f) { label.fontSizeMax = 40f; }
        }
    }
}
