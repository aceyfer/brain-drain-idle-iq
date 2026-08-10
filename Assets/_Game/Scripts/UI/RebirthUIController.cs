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
        [Tooltip("The 'REBIRTH' button GameObject in the HUD that opens this modal. Always active; interactable gates on RebirthManager.Instance.SnottingUnlockThreshold.")]
        [SerializeField] private GameObject rebirthTriggerButton;

        private bool triggerSuppressed;

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

            Button btn = rebirthTriggerButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = unlocked;
            }

            // Drive the background image color directly so locked always looks grey and
            // ready always looks hot pink — Unity's disabled-color tint alone is too subtle.
            Image img = rebirthTriggerButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = unlocked ? ButtonColorReady : ButtonColorLocked;
                img.raycastTarget = unlocked;
            }

            Transform innerFill = rebirthTriggerButton.transform.Find("InnerFill");
            Image innerFillImage = innerFill != null ? innerFill.GetComponent<Image>() : null;
            if (innerFillImage != null)
            {
                innerFillImage.raycastTarget = unlocked;
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
