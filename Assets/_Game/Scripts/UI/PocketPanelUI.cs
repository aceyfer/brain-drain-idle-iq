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
        // 2026-10-05 PALETTE LOCKDOWN audit follow-up: was a near-black non-token grey -- snapped
        // to the exact Base token, alpha unchanged.
        private static readonly Color PanelChipColor = new Color(Palette.Base.r, Palette.Base.g, Palette.Base.b, 0.94f);
        // 2026-10-04 PALETTE LOCKDOWN: was near-cyan, not the exact token.
        private static readonly Color ButtonFillColor = new Color(Palette.Cyan.r, Palette.Cyan.g, Palette.Cyan.b, 0.22f);
        private static readonly Color CloseFillColor = new Color(1f, 1f, 1f, 0.12f);
        // 2026-10-04 PALETTE LOCKDOWN audit follow-up: was flat grey 153 -- now White at 70%
        // alpha (empty-state copy, not a locked/unaffordable state, so Dim's role doesn't fit).
        private static readonly Color MutedTextColor = new Color(Palette.White.r, Palette.White.g, Palette.White.b, 0.7f);

        // 2026-10-04 art pass (B): business-card restyle. Ink matches IntelCardUI's own
        // CardBodyInk (#1B0F2E) rather than the lighter PaperTextColor above, since the tagline
        // needs real contrast at 70% alpha against cream paper.
        private static readonly Color32 CardInk = new Color32(0x1B, 0x0F, 0x2E, 255);
        private static Sprite paperSprite;
        private static Sprite shadowSprite;
        private static Sprite monogramRingSprite;
        private static Sprite stampNewSprite;
        private static bool spritesLoaded;
        private static bool loggedMissingReadFlag;

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

            CreateStretchedLabel(buttonObject.transform, "POCKET", Color.white, 26f, 20f, FontStyles.Bold);

            // 2026-10-05 ART PASS 2: was UniversalButtonBorderApplier.Instance?.ApplyToButton --
            // this button now owns its own static Alert_Frame look instead (see
            // AlertFrameButtonStyle's doc comment for why it's excluded from that system).
            AlertFrameButtonStyle.Apply(button);
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
            layout.spacing = 18f; // 2026-10-04 art pass (B): business-card spacing
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
            text.fontSizeMin = 22f;
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
            NudgeModalScope.SetOpen(panelGroup.gameObject, !hidden);
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

        /// <summary>
        /// 2026-10-04 art pass (B): loads the 4 business-card sprites once (Resources/UI/Generated,
        /// see CardAndEventArtGenerator) rather than per-card -- cards rebuild every Open() and
        /// every live collection event, so a per-card Resources.Load would repeat needlessly.
        /// Static + a loaded flag since every PocketPanelUI instance (there's only ever one, but
        /// nothing enforces that at the type level) shares the same art.
        /// </summary>
        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded) { return; }
            paperSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Paper");
            shadowSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Shadow");
            monogramRingSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Monogram_Ring");
            stampNewSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Stamp_New");
            spritesLoaded = true;
        }

        /// <summary>One collected card, shown as a tilted business card (front text split into
        /// name + tagline, same as the full IntelCardUI flip's front per item C); tapping re-opens
        /// the full card verbatim through IntelCardUI (its own sortingOrder-500 overlay floats
        /// above this panel). onConfirmed is null: re-reading only closes, it never re-fires FTUE
        /// state.</summary>
        private void BuildSpine(IntelCardCatalog.LiteratesCard card)
        {
            EnsureSpritesLoaded();
            SplitFrontTitle(card.Front, out string businessName, out string tagline);

            GameObject spineObject = new GameObject("CardSpine", typeof(RectTransform));
            spineObject.transform.SetParent(contentRoot, false);

            // Hit area lives on the unrotated root so the click rect stays a plain rectangle
            // regardless of the visual tilt applied to TiltGroup below.
            Image hitArea = spineObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            Button button = spineObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            button.onClick.AddListener(() =>
                IntelCardUI.Show(IntelCardSkin.LiteratesCard, card.Front, card.Back, card.Confirm, null));

            LayoutElement layoutElement = spineObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 160f;

            GameObject tiltObject = new GameObject("TiltGroup", typeof(RectTransform));
            tiltObject.transform.SetParent(spineObject.transform, false);
            RectTransform tiltRect = tiltObject.GetComponent<RectTransform>();
            tiltRect.anchorMin = Vector2.zero;
            tiltRect.anchorMax = Vector2.one;
            tiltRect.offsetMin = new Vector2(10f, 10f);
            tiltRect.offsetMax = new Vector2(-10f, -10f);
            // Deterministic +/-1.5 degree tilt from a stable hash of the card id -- never changes
            // between opens (card ids are save/derivation keys, never renumbered, per
            // IntelCardCatalog's own doc comment), unlike System.String.GetHashCode() which isn't
            // guaranteed stable across runtimes/processes.
            float tiltDegrees = (StableHash01(card.Id) * 2f - 1f) * 1.5f;
            tiltRect.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees);

            if (shadowSprite != null)
            {
                GameObject shadowObject = new GameObject("Shadow", typeof(RectTransform));
                shadowObject.transform.SetParent(tiltObject.transform, false);
                RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                // Shifts the whole stretched rect by (6,-6) without changing its size -- offsetMin
                // and offsetMax both move by the same delta.
                shadowRect.offsetMin = new Vector2(6f, -6f);
                shadowRect.offsetMax = new Vector2(6f, -6f);
                Image shadowImage = shadowObject.AddComponent<Image>();
                shadowImage.sprite = shadowSprite;
                shadowImage.type = Image.Type.Sliced;
                shadowImage.raycastTarget = false;
            }

            GameObject paperObject = new GameObject("Paper", typeof(RectTransform));
            paperObject.transform.SetParent(tiltObject.transform, false);
            RectTransform paperRect = paperObject.GetComponent<RectTransform>();
            paperRect.anchorMin = Vector2.zero;
            paperRect.anchorMax = Vector2.one;
            paperRect.offsetMin = Vector2.zero;
            paperRect.offsetMax = Vector2.zero;
            Image paperImage = paperObject.AddComponent<Image>();
            if (paperSprite != null)
            {
                paperImage.sprite = paperSprite;
                paperImage.type = Image.Type.Sliced;
            }
            else
            {
                paperImage.color = PaperColor; // fallback if the generator hasn't been run
            }
            paperImage.raycastTarget = false;

            const float ringSize = 84f;
            GameObject ringObject = new GameObject("Ring", typeof(RectTransform));
            ringObject.transform.SetParent(tiltObject.transform, false);
            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0f, 0.5f);
            ringRect.anchorMax = new Vector2(0f, 0.5f);
            ringRect.pivot = new Vector2(0f, 0.5f);
            ringRect.sizeDelta = new Vector2(ringSize, ringSize);
            ringRect.anchoredPosition = new Vector2(20f, 0f);
            Image ringImage = ringObject.AddComponent<Image>();
            ringImage.sprite = monogramRingSprite;
            ringImage.raycastTarget = false;

            TextMeshProUGUI initials = CreateInkLabel(ringObject.transform, GetInitials(businessName), 28f, 18f, FontStyles.Bold, false);
            initials.alignment = TextAlignmentOptions.Center;

            float textLeft = 20f + ringSize + 16f;

            GameObject nameObject = new GameObject("NameText", typeof(RectTransform));
            nameObject.transform.SetParent(tiltObject.transform, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(textLeft, 0f);
            nameRect.offsetMax = new Vector2(-16f, -8f);
            TextMeshProUGUI nameLabel = CreateInkLabel(nameObject.transform, businessName.ToUpperInvariant(), 26f, 20f, FontStyles.Bold, false);
            nameLabel.alignment = TextAlignmentOptions.BottomLeft;

            GameObject taglineObject = new GameObject("TaglineText", typeof(RectTransform));
            taglineObject.transform.SetParent(tiltObject.transform, false);
            RectTransform taglineRect = taglineObject.GetComponent<RectTransform>();
            taglineRect.anchorMin = Vector2.zero;
            taglineRect.anchorMax = new Vector2(1f, 0.5f);
            taglineRect.offsetMin = new Vector2(textLeft, 8f);
            taglineRect.offsetMax = new Vector2(-16f, 0f);
            TextMeshProUGUI taglineLabel = CreateInkLabel(taglineObject.transform, tagline, 22f, 20f, FontStyles.Italic, true);
            taglineLabel.alignment = TextAlignmentOptions.TopLeft;

            // "Unread" has no backing flag anywhere in this codebase -- FTUEManager.
            // CollectedLiteratesCardIds IS the seen-flag set, so every card reaching this method
            // was by definition already shown once as a modal. Per the brief's own fallback:
            // skip the stamp and log once (not per-card) rather than fabricate a flag.
            if (!loggedMissingReadFlag)
            {
                loggedMissingReadFlag = true;
                Debug.Log("[PocketPanelUI] No unread/read flag exists for Pocket cards (CollectedLiteratesCardIds is the seen-flag set itself) -- BizCard_Stamp_New is not applied to any card.");
            }
        }

        /// <summary>Splits "BUSINESS NAME — \"tagline\"" the way every IntelCardCatalog.Front
        /// string is already formatted. Falls back to the whole string as the name with an empty
        /// tagline if the separator isn't present (defensive; every current entry has it).</summary>
        private static void SplitFrontTitle(string front, out string name, out string tagline)
        {
            const string separator = " — ";
            int index = front != null ? front.IndexOf(separator, System.StringComparison.Ordinal) : -1;
            if (index < 0)
            {
                name = front ?? string.Empty;
                tagline = string.Empty;
                return;
            }
            name = front.Substring(0, index).Trim();
            tagline = front.Substring(index + separator.Length).Trim();
        }

        /// <summary>1-2 letter monogram from a business name's word-initials, e.g. "GARY'S
        /// DISCOUNT MATTRESS EMPORIUM" -> per-word initials "GDME" -> first two -> "GD". Skips
        /// words with no alphabetic character at all (e.g. "#2").</summary>
        private static string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }

            var acronym = new System.Text.StringBuilder();
            foreach (string word in name.Split(' '))
            {
                foreach (char c in word)
                {
                    if (char.IsLetter(c))
                    {
                        acronym.Append(char.ToUpperInvariant(c));
                        break;
                    }
                }
                if (acronym.Length >= 2) { break; }
            }

            return acronym.ToString();
        }

        /// <summary>Stable (process/runtime-independent) [0,1) hash -- FNV-1a over the string's
        /// chars. System.String.GetHashCode() is explicitly documented as not guaranteed stable
        /// across .NET versions/processes, which would violate "never changes between opens."</summary>
        private static float StableHash01(string s)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (s != null)
                {
                    foreach (char c in s)
                    {
                        hash ^= c;
                        hash *= 16777619u;
                    }
                }
                return (hash & 0xFFFFFFu) / (float)0x1000000u;
            }
        }

        private static TMP_FontAsset cardBoldFontAsset;
        private static TMP_FontAsset cardRegularFontAsset;
        private static bool cardFontsLoaded;

        private static void EnsureCardFontsLoaded()
        {
            if (cardFontsLoaded) { return; }
            cardFontsLoaded = true;
            cardBoldFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            cardRegularFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        /// <summary>
        /// 2026-10-04 fix: runtime diagnostics (logged once per label, now removed) showed
        /// NameText rendering via LiberationSans SDF's FAUX bold (boldStyle 0.75 -- a synthetic
        /// weight-boost on a font asset with no real bold face) at zero face dilate, which reads
        /// thin/grey against the business-card paper's grain. Oswald Bold SDF is a real bold
        /// face already used project-wide (Oswald_CyanGlow.mat/Oswald_GoldUnderlay.mat), so no
        /// faux-bold simulation is needed once assigned -- the Bold style flag is cleared to
        /// avoid double-thickening an already-bold glyph. Explicit assignment either way (never
        /// touches TMP_Settings.defaultFontAsset or the shared LiberationSans SDF material).
        /// </summary>
        private static void AssignCardInkFont(TextMeshProUGUI label, bool bold)
        {
            EnsureCardFontsLoaded();
            TMP_FontAsset font = bold ? cardBoldFontAsset : cardRegularFontAsset;
            if (font == null) { return; }
            label.font = font;
            if (bold) { label.fontStyle &= ~FontStyles.Bold; }
        }

        /// <summary>
        /// label.fontMaterial (not fontSharedMaterial) clones a per-instance material the first
        /// time it's accessed -- bumping _FaceDilate there thickens this one label's ink without
        /// touching the shared default material or any other text in the project. Guarded by
        /// isActiveAndEnabled: TMP_Text's lazy material/clone setup only runs once a label has
        /// actually been active, matching the same guard UniversalButtonBorderApplier already
        /// uses for its own per-label material touch (confirmed live NullReferenceException risk
        /// otherwise). Every Pocket card label is active at creation (the panel hides via
        /// CanvasGroup alpha, never SetActive), so this always applies immediately here.
        /// </summary>
        private static void ApplyCardInkFaceDilate(TextMeshProUGUI label)
        {
            if (label == null || !label.isActiveAndEnabled) { return; }
            Material instanceMaterial = label.fontMaterial;
            if (instanceMaterial != null && instanceMaterial.HasProperty("_FaceDilate"))
            {
                instanceMaterial.SetFloat("_FaceDilate", 0.15f);
            }
        }

        private static TextMeshProUGUI CreateInkLabel(Transform parent, string text, float maxSize, float minSize, FontStyles style, bool fadedInk)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            // 2026-10-04 fix: 0.7 -> 0.75 per play-test ("taglines are nearly invisible"); names
            // (fadedInk=false) were already full ink #1B0F2E at alpha 1.0 -- unchanged.
            label.color = fadedInk ? new Color(CardInk.r / 255f, CardInk.g / 255f, CardInk.b / 255f, 0.75f) : (Color)CardInk;
            label.fontStyle = style;
            label.fontSize = maxSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = minSize;
            label.fontSizeMax = maxSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            bool isBold = (style & FontStyles.Bold) != 0;
            AssignCardInkFont(label, isBold);
            ApplyCardInkFaceDilate(label);

            return label;
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
