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

        private static readonly Color ButtonColorLocked = new Color(0.35f, 0.35f, 0.35f, 0.85f);
        private static readonly Color ButtonColorReady  = new Color(1f, 0.078f, 0.576f, 1f);

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
        }

        private void HandleRestorationProgressChanged(double _)
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

            // Drive the background image color directly so locked always looks grey and
            // ready always looks hot pink — Unity's disabled-color tint alone is too subtle.
            Image img = rebirthTriggerButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = unlocked ? ButtonColorReady : ButtonColorLocked;

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
                    txt.color = Color.white;
                }
                else
                {
                    txt.text = $"SNOTTING LOCKED\n{NumberFormatter.Format(spent)} / {NumberFormatter.Format(unlockThreshold.GetValueOrDefault())}";
                    txt.fontSizeMin = 16f;
                    txt.fontSizeMax = 36f;
                    txt.color = new Color(0.75f, 0.75f, 0.75f, 1f);
                }
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
