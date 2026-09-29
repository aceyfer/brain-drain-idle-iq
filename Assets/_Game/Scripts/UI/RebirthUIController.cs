using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        // 2026-09-29 color pass: fill is always the same fixed base everywhere else in the game
        // (UniversalButtonBorderApplier.BaseFillColor) -- THE SNOTTING is the one deliberate hero
        // highlight, but that highlight now lives entirely in the LABEL (cyan + a pulse when
        // ready), not the fill. Previously this button's own fill swapped grey/hot-pink; it no
        // longer varies at all.
        private static readonly Color BaseFillColor = new Color32(0x1B, 0x0F, 0x2E, 0xFF);
        private static readonly Color ReadyLabelColor = new Color32(0x00, 0xDD, 0xEB, 0xFF);
        private static readonly Color LockedLabelColor = new Color(0.6f, 0.6f, 0.6f, 0.45f);

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

            // 2026-09-29 color pass: fill is always the fixed base now, same as every other
            // managed button -- locked/ready no longer swap the button's own fill, only its
            // label (below) and, when ready, a pulse communicate state.
            //
            // sprite = null is required, not optional: this button's own baked Image sprite is
            // the same non-white gradient texture as ConvertButton's old "Gradient" overlay
            // (guid fdb2114c...) -- tinting it produced a muddy maroon/brown instead of a clean
            // flat fill, since BaseFillColor was multiplying against that texture's own baked
            // pixel hues. UniversalButtonBorderApplier now clears this generically too, but this
            // controller re-asserts img.color independently on every visibility refresh (far more
            // often than the applier's own pass), so it must clear sprite the same way or a later
            // refresh would have nothing left to keep it cleared.
            Image img = rebirthTriggerButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = BaseFillColor;
                img.sprite = null;

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

            // Hero highlight: a gentle breathing pulse only while ready, guarded so it (re)starts
            // exactly once on the locked->ready transition rather than every visibility refresh.
            RectTransform triggerRect = rebirthTriggerButton.GetComponent<RectTransform>();
            if (unlocked && !isReadyPulsing)
            {
                AnimationController.PlayAffordablePulse(triggerRect, img);
                isReadyPulsing = true;
            }
            else if (!unlocked && isReadyPulsing)
            {
                AnimationController.StopAffordablePulse(triggerRect);
                isReadyPulsing = false;
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

            multiplierText.text =
                "<b>THE SNOTTING</b>\n\n" +
                "Prestige reset.\n" +
                "Your current run gets wiped:\n" +
                "BP, Cash, Points, Buildings, Restoration, IQ.\n\n" +
                "You keep:\n" +
                "Rank and permanent boosts.\n\n" +
                "Reward:\n" +
                $"+{bpPct}% Brain Power\n" +
                $"+{cashPct}% Cash\n" +
                $"+{tapPct}% Tap Power\n" +
                "Better Cash → Points rate";

            if (!string.IsNullOrEmpty(illumisnottyTitle))
            {
                multiplierText.text += $"\n\nBecoming: {illumisnottyTitle}";
            }

            multiplierText.enableAutoSizing = true;
            multiplierText.fontSizeMin = 14f;
            multiplierText.fontSizeMax = 26f;
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
    }
}
