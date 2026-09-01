using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// §24c THE POCKET: a persistent, re-readable inventory of the LITERATES resistance cards the
    /// player has collected across the §23 FTUE pass (Beats 2/3/5/7/9). Fully code-built and
    /// self-bootstrapping -- no prefab, no scene wiring (IntelCardUI / DialogueLogPanelUI tab-bar
    /// precedent, Bible §8's "own it in code"). Builds its own open button parented next to the
    /// scene's existing "Dia-Log" button (LogOpenButton) and its own CanvasGroup-gated panel; each
    /// collected card renders as a tappable aged-paper spine that re-opens the full card through
    /// IntelCardUI.Show. The collected set is derived from
    /// FTUEManager.CollectedLiteratesCardIds (the persisted seen-flags), so THE POCKET holds no
    /// save state of its own and can never desync from what the player has actually read (Option A,
    /// 2026-07-24). Card copy is read from IntelCardCatalog, the single verbatim-copy source shared
    /// with FTUEManager. Non-modal, like the dialogue log: no dimming backdrop, coexists with the
    /// Dia-Log panel and gameplay -- literally: since the panel can legitimately stay open while
    /// the player keeps playing and collects another card, RebuildList() runs both on Open() and
    /// live via FTUEManager.OnLiteratesCardCollected (2026-08-31 fix; see SubscribeToEvents) so an
    /// already-open Pocket never has to be closed and reopened to reveal a just-collected card.
    /// Closed via its X, its toggle button, or Close().
    /// </summary>
    public sealed class PocketPanelUI : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";
        private const string DiaLogButtonName = "LogOpenButton";
        private const float ButtonGap = 12f;

        // Mirrors IntelCardUI's LiteratesCard palette + DialogueLogPanelUI's chip/tab colors, kept
        // local so THE POCKET ships as a single new file touching nothing else. If these ever
        // drift, IntelCardUI (card colors) and DialogueLogPanelUI (chip/cyan) are the references.
        private static readonly Color PaperColor = new Color(0.90f, 0.85f, 0.72f, 1f);
        private static readonly Color PaperTextColor = new Color(0.18f, 0.14f, 0.08f, 1f);
        private static readonly Color PanelChipColor = new Color(0.06f, 0.06f, 0.1f, 0.94f);
        private static readonly Color ButtonFillColor = new Color(0f, 0.94f, 1f, 0.22f);
        private static readonly Color CloseFillColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color MutedTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private static PocketPanelUI instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing
        /// placed one in the scene (matches FTUEManager/SaveManager -- nothing else calls into
        /// this class, so without this the Pocket would silently never build).</summary>
        public static PocketPanelUI Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<PocketPanelUI>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("PocketPanelUI");
                    instance = hostObject.AddComponent<PocketPanelUI>();
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

        /// <summary>
        /// 2026-08-31 fix: THE POCKET is explicitly non-modal and "coexists with gameplay" (class
        /// comment above), so a player can leave it open while continuing to play and collect a
        /// new LITERATES card in the background -- RebuildList() previously only ran from Open(),
        /// so that card silently didn't appear until the player closed and reopened the panel.
        /// FTUEManager.Instance is self-bootstrapping (creates its own hosting GameObject on
        /// first access, same as this class), so calling it here is safe even before FTUEManager
        /// has otherwise been touched -- matches RebuildList()'s existing FTUEManager.Instance use.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (FTUEManager.Instance == null) return;

            FTUEManager.Instance.OnLiteratesCardCollected -= HandleLiteratesCardCollected;
            FTUEManager.Instance.OnLiteratesCardCollected += HandleLiteratesCardCollected;
        }

        private void UnsubscribeFromEvents()
        {
            if (FTUEManager.Instance == null) return;

            FTUEManager.Instance.OnLiteratesCardCollected -= HandleLiteratesCardCollected;
        }

        /// <summary>Live-refreshes the spine list only while the panel is actually visible --
        /// if it's closed, the next Open() already does a fresh RebuildList(), so refreshing here
        /// too would just be wasted work.</summary>
        private void HandleLiteratesCardCollected()
        {
            if (!isVisible) return;

            RebuildList();
        }

        /// <summary>
        /// Locates the scene's Dia-Log button (LogOpenButton) to (a) find the HUD Canvas to parent
        /// into and (b) anchor THE POCKET button directly beneath it, tracking Dia-Log's exact
        /// placement rather than hardcoding a corner. Find-by-name matches this codebase's existing
        /// convention (FTUEManager's "_Systems"/"ChaosPopUpCanvas" lookups). Bails gracefully if
        /// the button isn't present -- THE POCKET is non-critical and never blocks boot.
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
                Debug.LogWarning("[PocketPanelUI] Dia-Log button (LogOpenButton) not found; THE POCKET button not built.", this);
                return;
            }

            canvasRect = diaLogRect.parent as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogWarning("[PocketPanelUI] Dia-Log button's parent is not a RectTransform; THE POCKET button not built.", this);
                return;
            }

            BuildOpenButton(diaLogRect);
            BuildPanel();
            SetPanelHidden(true);
            built = true;
        }

        private void BuildOpenButton(RectTransform diaLogRect)
        {
            GameObject buttonObject = new GameObject("PocketOpenButton", typeof(RectTransform));
            buttonObject.transform.SetParent(canvasRect, false);
            buttonObject.transform.SetAsLastSibling();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = diaLogRect.anchorMin;
            rect.anchorMax = diaLogRect.anchorMax;
            rect.pivot = diaLogRect.pivot;
            rect.sizeDelta = diaLogRect.sizeDelta;
            // Directly below Dia-Log: drop one button height + a gap. Dia-Log is top-right pivoted
            // (1,1) with a negative y anchoredPosition, so subtracting grows downward.
            rect.anchoredPosition = diaLogRect.anchoredPosition + new Vector2(0f, -(diaLogRect.sizeDelta.y + ButtonGap));

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonFillColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleOpen);

            CreateStretchedLabel(buttonObject.transform, "POCKET", Color.white, 26f, 14f, FontStyles.Bold);
        }

        private void BuildPanel()
        {
            GameObject panelObject = new GameObject("PocketPanel", typeof(RectTransform));
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
            title.text = "THE POCKET";
            title.color = Color.white;
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
            GameObject closeObject = new GameObject("PocketCloseButton", typeof(RectTransform));
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
            // bit the convert arrow in §16 B4 (de5d4c0).
            CreateStretchedLabel(closeObject.transform, "X", Color.white, 36f, 20f, FontStyles.Bold);
        }

        private void BuildScrollList(Transform parent)
        {
            GameObject scrollObject = new GameObject("PocketScroll", typeof(RectTransform));
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

            // Viewport uses RectMask2D, never legacy Mask: RectMask2D clips by rect alone and needs
            // no graphic, so nothing gets culled if the viewport image is transparent -- the exact
            // trap the dialogue log hit in §24a-2 (446af70).
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
            text.text = "THE POCKET IS EMPTY.\nREAD SOMETHING.";
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
        /// DialogueLogPanelUI.SetPanelHidden.</summary>
        private void SetPanelHidden(bool hidden)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = hidden ? 0f : 1f;
            panelGroup.blocksRaycasts = !hidden;
            panelGroup.interactable = !hidden;
        }

        /// <summary>Rebuilds the spine list from the derived collected set. Called from Open()
        /// (a fresh read always reflects the current save state) and, since 2026-08-31, from
        /// HandleLiteratesCardCollected while the panel is already open -- see that method and
        /// the class comment for why the on-open-only version wasn't enough.</summary>
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

            IReadOnlyList<string> ids = FTUEManager.Instance != null
                ? FTUEManager.Instance.CollectedLiteratesCardIds
                : null;

            bool anyCards = ids != null && ids.Count > 0;
            if (emptyState != null)
            {
                emptyState.SetActive(!anyCards);
            }

            if (!anyCards) return;

            for (int i = 0; i < ids.Count; i++)
            {
                if (IntelCardCatalog.TryGet(ids[i], out IntelCardCatalog.LiteratesCard card))
                {
                    BuildSpine(card);
                }
            }
        }

        /// <summary>One collected card, shown as its aged-paper front; tapping re-opens the full
        /// card verbatim through IntelCardUI (its own sortingOrder-500 overlay floats above this
        /// panel). onConfirmed is null: re-reading only closes, it never re-fires FTUE state.</summary>
        private void BuildSpine(IntelCardCatalog.LiteratesCard card)
        {
            GameObject spineObject = new GameObject("CardSpine", typeof(RectTransform));
            spineObject.transform.SetParent(contentRoot, false);

            Image image = spineObject.AddComponent<Image>();
            image.color = PaperColor;

            Button button = spineObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
                IntelCardUI.Show(IntelCardSkin.LiteratesCard, card.Front, card.Back, card.Confirm, null));

            LayoutElement layoutElement = spineObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(spineObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 12f);
            labelRect.offsetMax = new Vector2(-20f, -12f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = card.Front;
            label.color = PaperTextColor;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Left;
            label.fontSize = 28f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 28f;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
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
