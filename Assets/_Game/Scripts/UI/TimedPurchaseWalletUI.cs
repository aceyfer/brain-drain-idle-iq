using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// THE WALLET: a persistent, re-readable list of every still-active timed God Tier Store
    /// purchase (currently the Brain Freeze family -- see GodTierStoreItemData.isConsumable),
    /// each shown with its own live countdown. Fully code-built and self-bootstrapping -- no
    /// prefab, no scene wiring (PocketPanelUI/DialogueLogPanelUI tab-bar precedent, Bible §8's
    /// "own it in code"). Builds its own open button parented two slots below the scene's
    /// existing "Dia-Log" button (LogOpenButton) -- computed independently from LogOpenButton's
    /// own rect rather than by finding PocketOpenButton, since Start() order between this class
    /// and PocketPanelUI is not guaranteed -- and its own CanvasGroup-gated panel.
    ///
    /// Deliberately separate from PocketPanelUI ("THE POCKET"): that panel is a narrative log of
    /// collected LITERATES cards and holds no purchase/timer state of its own. THE WALLET exists
    /// specifically so a player who buys two 24-hour timed items (even the exact same item twice
    /// back to back) can see both purchases and both remaining-time countdowns separately --
    /// GodTierStoreManager.ActiveTimedPurchases already tracks each purchase as its own ledger
    /// entry (see that class), this panel just renders it and keeps it ticking once a second
    /// while open. The underlying gameplay effect (PlayerIQManager's IQ floor) still merges every
    /// purchase into one stacked expiry as it always has -- this panel's per-item countdowns are
    /// a display convenience on top of that, not a change to how the floor itself is computed.
    /// </summary>
    public sealed class TimedPurchaseWalletUI : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";
        private const string DiaLogButtonName = "LogOpenButton";
        private const float ButtonGap = 12f;

        // Mirrors PocketPanelUI's chip/text palette so the two panels read as siblings, with a
        // gold accent (GodTierStoreSlotUI.AvailableColor) marking this one as the paid-store tie-in.
        private static readonly Color PanelChipColor = new Color(0.06f, 0.06f, 0.1f, 0.94f);
        private static readonly Color RowColor = new Color(0.14f, 0.12f, 0.05f, 1f);
        private static readonly Color AccentColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);
        private static readonly Color CloseFillColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color ButtonFillColor = new Color(1f, 0.84f, 0f, 0.22f);
        private static readonly Color MutedTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private static TimedPurchaseWalletUI instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing
        /// placed one in the scene (matches PocketPanelUI/FTUEManager -- nothing else calls into
        /// this class, so without this THE WALLET would silently never build).</summary>
        public static TimedPurchaseWalletUI Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<TimedPurchaseWalletUI>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("TimedPurchaseWalletUI");
                    instance = hostObject.AddComponent<TimedPurchaseWalletUI>();
                }

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private RectTransform canvasRect;
        private CanvasGroup panelGroup;
        private RectTransform contentRoot;
        private GameObject emptyState;
        private bool isVisible;
        private bool built;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent != null)
            {
                transform.SetParent(systemsParent.transform, false);
            }
        }

        private void Start()
        {
            Build();
            SubscribeToEvents();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void SubscribeToEvents()
        {
            if (GodTierStoreManager.Instance != null)
            {
                GodTierStoreManager.Instance.OnItemsChanged -= HandleItemsChanged;
                GodTierStoreManager.Instance.OnItemsChanged += HandleItemsChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
                GameManager.Instance.OnSecondTick += HandleSecondTick;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GodTierStoreManager.Instance != null)
            {
                GodTierStoreManager.Instance.OnItemsChanged -= HandleItemsChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
            }
        }

        /// <summary>A purchase just happened (or the owned/ledger state was restored from a
        /// save) -- refresh immediately so a just-bought item's row appears without waiting for
        /// the next second tick. No-ops while closed; Open() already does a fresh RebuildList().</summary>
        private void HandleItemsChanged()
        {
            if (!isVisible) return;
            RebuildList();
        }

        /// <summary>Keeps every visible countdown honest once a second. Rebuilding the whole
        /// (always-tiny) list each tick is simpler and cheap enough here than diffing per-row
        /// labels, and it doubles as the mechanism that drops a row the instant it hits zero.</summary>
        private void HandleSecondTick()
        {
            if (!isVisible) return;
            RebuildList();
        }

        /// <summary>
        /// Locates the scene's Dia-Log button (LogOpenButton) to (a) find the HUD Canvas to parent
        /// into and (b) anchor THE WALLET button two slots beneath it -- one slot reserved for
        /// PocketPanelUI's own button, whether or not that script has built yet this frame. Bails
        /// gracefully if the button isn't present -- THE WALLET is non-critical and never blocks boot.
        /// </summary>
        private void Build()
        {
            if (built)
            {
                return;
            }

            GameObject diaLogButton = GameObject.Find(DiaLogButtonName);
            RectTransform diaLogRect = diaLogButton != null ? diaLogButton.GetComponent<RectTransform>() : null;
            if (diaLogRect == null)
            {
                Debug.LogWarning("[TimedPurchaseWalletUI] Dia-Log button (LogOpenButton) not found; THE WALLET button not built.", this);
                return;
            }

            canvasRect = diaLogRect.parent as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogWarning("[TimedPurchaseWalletUI] Dia-Log button's parent is not a RectTransform; THE WALLET button not built.", this);
                return;
            }

            BuildOpenButton(diaLogRect);
            BuildPanel();
            SetPanelHidden(true);
            built = true;
        }

        private void BuildOpenButton(RectTransform diaLogRect)
        {
            GameObject buttonObject = new GameObject("WalletOpenButton", typeof(RectTransform));
            buttonObject.transform.SetParent(canvasRect, false);
            buttonObject.transform.SetAsLastSibling();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = diaLogRect.anchorMin;
            rect.anchorMax = diaLogRect.anchorMax;
            rect.pivot = diaLogRect.pivot;
            rect.sizeDelta = diaLogRect.sizeDelta;
            // Two slots below Dia-Log (Pocket occupies the first slot) -- computed from Dia-Log's
            // own rect alone so this doesn't depend on PocketPanelUI having built its button yet.
            rect.anchoredPosition = diaLogRect.anchoredPosition + new Vector2(0f, -2f * (diaLogRect.sizeDelta.y + ButtonGap));

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonFillColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleOpen);

            CreateStretchedLabel(buttonObject.transform, "WALLET", Color.white, 26f, 14f, FontStyles.Bold);
        }

        private void BuildPanel()
        {
            GameObject panelObject = new GameObject("WalletPanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvasRect, false);
            panelObject.transform.SetAsLastSibling();

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 1400f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = PanelChipColor;
            panelImage.raycastTarget = true; // catch-all so taps on the panel don't fall through

            panelGroup = panelObject.AddComponent<CanvasGroup>();

            BuildTitle(panelObject.transform);
            BuildCloseButton(panelObject.transform);
            BuildScrollList(panelObject.transform);
        }

        private void BuildTitle(Transform parent)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(parent, false);

            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            titleRect.sizeDelta = new Vector2(-140f, 72f); // leave the top-right corner for the X

            TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
            title.text = "THE WALLET";
            title.color = AccentColor;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.fontSize = 40f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 22f;
            title.fontSizeMax = 40f;
            title.raycastTarget = false;
        }

        private void BuildCloseButton(Transform parent)
        {
            GameObject closeObject = new GameObject("WalletCloseButton", typeof(RectTransform));
            closeObject.transform.SetParent(parent, false);

            RectTransform closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            closeRect.sizeDelta = new Vector2(64f, 64f);

            Image image = closeObject.AddComponent<Image>();
            image.color = CloseFillColor;

            Button button = closeObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Close);

            // Plain ASCII "X" -- avoids the LiberationSans-SDF glyph-fallback console spam that
            // bit the convert arrow in §16 B4 (de5d4c0), same as PocketPanelUI's close button.
            CreateStretchedLabel(closeObject.transform, "X", Color.white, 36f, 20f, FontStyles.Bold);
        }

        private void BuildScrollList(Transform parent)
        {
            GameObject scrollObject = new GameObject("WalletScroll", typeof(RectTransform));
            scrollObject.transform.SetParent(parent, false);

            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(24f, 24f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -112f); // clear the title band

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Viewport uses RectMask2D, never legacy Mask -- same trap/precedent as
            // PocketPanelUI/DialogueLogPanelUI's viewport (see PocketPanelUI's comment).
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportObject.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRect;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            contentRoot = contentRect;

            BuildEmptyState();
        }

        private void BuildEmptyState()
        {
            emptyState = new GameObject("EmptyState", typeof(RectTransform));
            emptyState.transform.SetParent(contentRoot, false);

            TextMeshProUGUI text = emptyState.AddComponent<TextMeshProUGUI>();
            text.text = "THE WALLET IS EMPTY.\nBUY A TIMED ITEM TO SEE IT HERE.";
            text.color = MutedTextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 30f;
            text.raycastTarget = false;

            LayoutElement layoutElement = emptyState.AddComponent<LayoutElement>();
            layoutElement.minHeight = 220f;
        }

        private void ToggleOpen()
        {
            if (isVisible)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (!built)
            {
                return;
            }

            panelGroup.transform.SetAsLastSibling();

            RebuildList();
            SetPanelHidden(false);
            isVisible = true;
        }

        public void Close()
        {
            SetPanelHidden(true);
            isVisible = false;
        }

        /// <summary>Single owner of the panel's hidden/shown state (code-owned presentation state,
        /// Bible §8). Alpha + raycast gating, never GameObject SetActive -- matches
        /// PocketPanelUI/DialogueLogPanelUI.SetPanelHidden.</summary>
        private void SetPanelHidden(bool hidden)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = hidden ? 0f : 1f;
            panelGroup.blocksRaycasts = !hidden;
            panelGroup.interactable = !hidden;
        }

        /// <summary>Rebuilds the row list from GodTierStoreManager's live ledger. Called from
        /// Open(), from HandleItemsChanged (a purchase happened while already open), and once a
        /// second from HandleSecondTick while visible -- the second-tick pass is also what drops
        /// a row the moment its countdown reaches zero (ActiveTimedPurchases prunes on read).</summary>
        private void RebuildList()
        {
            if (contentRoot == null) return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = contentRoot.GetChild(i);
                if (emptyState != null && child == emptyState.transform)
                {
                    continue;
                }
                Destroy(child.gameObject);
            }

            GodTierStoreManager manager = GodTierStoreManager.Instance;
            IReadOnlyList<ActiveTimedPurchase> purchases = manager != null
                ? manager.ActiveTimedPurchases
                : null;

            bool anyActive = purchases != null && purchases.Count > 0;
            if (emptyState != null)
            {
                emptyState.SetActive(!anyActive);
            }

            if (!anyActive) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            for (int i = 0; i < purchases.Count; i++)
            {
                BuildRow(purchases[i], manager, now);
            }
        }

        /// <summary>One active purchase, shown as its display name (resolved fresh from the
        /// matching GodTierStoreItemData -- see ActiveTimedPurchase's own doc comment for why)
        /// plus a live "time remaining" readout. Non-interactive -- there is nothing to tap here,
        /// unlike THE POCKET's re-openable cards.</summary>
        private void BuildRow(ActiveTimedPurchase purchase, GodTierStoreManager manager, long now)
        {
            GameObject rowObject = new GameObject("WalletRow", typeof(RectTransform));
            rowObject.transform.SetParent(contentRoot, false);

            Image image = rowObject.AddComponent<Image>();
            image.color = RowColor;
            image.raycastTarget = false;

            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(rowObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 12f);
            labelRect.offsetMax = new Vector2(-20f, -12f);

            string displayName = ResolveDisplayName(manager, purchase.itemId);
            long secondsRemaining = Math.Max(0L, purchase.expiryUnixSeconds - now);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = $"{displayName}\n<color=#FFD700><font-weight=bold>{FormatRemaining(secondsRemaining)} remaining</font-weight></color>";
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Left;
            label.fontSize = 28f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 28f;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
        }

        /// <summary>Resolves an itemId back to its configured displayName. Falls back to the raw
        /// itemId if the ScriptableObject can't be found -- should not normally happen, but a
        /// ledger entry outliving its authoring asset (e.g. removed from the store's item list
        /// mid-development) must never crash the panel.</summary>
        private static string ResolveDisplayName(GodTierStoreManager manager, string itemId)
        {
            if (manager != null)
            {
                IReadOnlyList<GodTierStoreItemData> items = manager.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].itemId == itemId)
                    {
                        return items[i].displayName;
                    }
                }
            }

            return itemId;
        }

        /// <summary>"2d 04h 12m", "04h 12m", or "12m 03s" depending on magnitude -- always two
        /// units, never more precision than the player needs for a multi-hour/day countdown.</summary>
        private static string FormatRemaining(long totalSeconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);

            if (span.TotalDays >= 1d)
            {
                return $"{(int)span.TotalDays}d {span.Hours:00}h {span.Minutes:00}m";
            }

            if (span.TotalHours >= 1d)
            {
                return $"{(int)span.TotalHours}h {span.Minutes:00}m";
            }

            return $"{span.Minutes}m {span.Seconds:00}s";
        }

        private static void CreateStretchedLabel(Transform parent, string text, Color color, float maxSize, float minSize, FontStyles style)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = color;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = maxSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = minSize;
            label.fontSizeMax = maxSize;
            label.raycastTarget = false;
        }
    }
}
