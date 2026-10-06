using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// THE WALLET: the Brain Freeze family's inventory + activation surface. Fully code-built and
    /// self-bootstrapping -- no prefab, no scene wiring (PocketPanelUI/DialogueLogPanelUI tab-bar
    /// precedent, Bible §8's "own it in code"). Builds its own open button parented two slots
    /// below the scene's existing "Dia-Log" button (LogOpenButton) -- computed independently from
    /// LogOpenButton's own rect rather than by finding PocketOpenButton, since Start() order
    /// between this class and PocketPanelUI is not guaranteed -- and its own CanvasGroup-gated
    /// panel.
    ///
    /// 2026-10-06 FREEZE INVENTORY AMENDMENT (Aceyfer) -- REWRITTEN, replaces the old
    /// "every purchase is its own independent countdown card" model entirely:
    ///   - One row per owned freeze itemId (count > 0): tier icon ("x3" badge baked as live TMP,
    ///     never rasterized), tier-colored name, duration, and a Use button.
    ///   - The currently-active freeze (if any) is a separate card pinned above the rows, with a
    ///     live countdown pill -- matches rule 4's "shown on top with its countdown pill".
    ///   - While ANY freeze is active, every row's Use button is disabled and its label switches
    ///     to "Active — Xd Yh left" instead of "USE" (rule 2) -- same global countdown value on
    ///     every row, since only one freeze can ever run at a time regardless of which item it
    ///     came from.
    ///   - GodTierStoreManager.OnFreezeExpired drives a small auto-fading toast ("Freeze ended.
    ///     Use another? (xN left)") -- purely a notification, it never activates anything itself
    ///     ("never auto-consume" per rule 2). Only shown if the just-expired item still has
    ///     charges left; silent otherwise.
    /// </summary>
    public sealed class TimedPurchaseWalletUI : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";
        private const string DiaLogButtonName = "LogOpenButton";
        private const float ButtonGap = 12f;
        private const float ToastVisibleSeconds = 4f;
        private const float ToastFadeSeconds = 0.4f;

        // 2026-10-07 play-test fix: 94% let the Cryo Chamber backdrop's pod rim-glow (up to 0.75
        // alpha, a near-white cyan -- bright enough that even a few percent bleed-through reads
        // as visible) show through the panel. Bumped to 95% per spec.
        private static readonly Color PanelChipColor = new Color(Palette.Base.r, Palette.Base.g, Palette.Base.b, 0.95f);
        private static readonly Color RowColor = Palette.Surface;
        private static readonly Color AccentColor = Palette.Cyan;
        private static readonly Color CloseFillColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color ButtonFillColor = new Color(Palette.Cyan.r, Palette.Cyan.g, Palette.Cyan.b, 0.22f);
        private static readonly Color MutedTextColor = new Color(Palette.White.r, Palette.White.g, Palette.White.b, 0.7f);
        private static readonly Color UseEnabledColor = Palette.Cyan;
        // 2026-10-07 play-test fix: Palette.Dim's 0.45 alpha (tuned for small locked-label
        // chrome elsewhere) was "faint, hard to read" on a multi-word status string ("Active —
        // Xd Yh left"). Same grey hue, boosted to 0.8 alpha specifically for this readable-dim-
        // text role -- still visually distinct from the Cyan "USE" state, just not illegible.
        private static readonly Color UseDisabledColor = new Color(Palette.Dim.r, Palette.Dim.g, Palette.Dim.b, 0.8f);
        // Disabled pill tint: desaturates/darkens the Alert_Button sprite via a solid (alpha=1)
        // grey multiply rather than a translucent tint -- transparency was exactly what made the
        // old flat disabled fill hard to read, so the disabled state stays fully opaque and
        // conveys "disabled" through hue/brightness instead.
        private static readonly Color DisabledPillTint = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static readonly Color32 PillCyan = new Color32(0x00, 0xDD, 0xEB, 0xFF);
        private static readonly Color32 PillWarning = new Color32(0x80, 0xF4, 0xFF, 0xFF);

        private static Sprite cardSprite;
        private static Sprite shadowSprite;
        private static Sprite pillSprite;
        private static bool spritesLoaded;

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
        private GameObject toastObject;
        private CanvasGroup toastGroup;
        private TextMeshProUGUI toastLabel;
        private Coroutine toastCoroutine;
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
                GodTierStoreManager.Instance.OnFreezeExpired -= HandleFreezeExpired;
                GodTierStoreManager.Instance.OnFreezeExpired += HandleFreezeExpired;
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
                GodTierStoreManager.Instance.OnFreezeExpired -= HandleFreezeExpired;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
            }
        }

        /// <summary>A purchase/activation just happened (or state was restored from a save) --
        /// refresh immediately so it appears without waiting for the next second tick. No-ops
        /// while closed; Open() already does a fresh RebuildList().</summary>
        private void HandleItemsChanged()
        {
            if (!isVisible) return;
            RebuildList();
        }

        /// <summary>Keeps the active-freeze countdown and every row's "Active -- Xd Yh left"
        /// label honest once a second while the panel is open.</summary>
        private void HandleSecondTick()
        {
            if (!isVisible) return;
            RebuildList();
        }

        /// <summary>rule 2: "Freeze ended. Use another? (xN left)" -- only if charges remain,
        /// never auto-consumes anything, fires regardless of whether THE WALLET is currently
        /// open (the player should learn their freeze ended even if they're looking elsewhere).</summary>
        private void HandleFreezeExpired(string expiredItemId, int remainingCount)
        {
            if (remainingCount <= 0) { return; }
            ShowToast($"Freeze ended. Use another? (x{remainingCount} left)");
            if (isVisible) { RebuildList(); }
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
            BuildToast();
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

            CreateStretchedLabel(buttonObject.transform, "WALLET", Color.white, 26f, 20f, FontStyles.Bold);

            AlertFrameButtonStyle.Apply(button);
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
            text.text = "THE WALLET IS EMPTY.\nBUY A FREEZE TO SEE IT HERE.";
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

        /// <summary>Small auto-fading banner, pinned to the top of the main Canvas (not inside
        /// THE WALLET panel -- the player may not have it open when a freeze ends). Built once,
        /// reused for every expiry -- ShowToast restarts its fade coroutine rather than building a
        /// new GameObject each time.
        ///
        /// 2026-10-07 play-test fix: was a flat anchoredPosition (-140) that overlapped the HUD
        /// "BRAIN POWER" header on real device/safe-area heights. Now positioned dynamically just
        /// below HUDController.HeaderPanelRect's own bottom edge every time it's shown (ShowToast
        /// calls PositionBelowHeader) -- same world-corner-to-local-space technique
        /// UINudgePointer already uses to clear the same header, so this can never drift out of
        /// sync with that logic again. Falls back to the original fixed offset if the header
        /// can't be resolved (e.g. a test scene with no HUD). Background is Surface fill + a Glow
        /// Outline border (not a literal sprite retint -- same reasoning DialogueDisplayUI's own
        /// Surface+Glow treatment already documents: tinting a baked Base+Glow sprite toward
        /// Surface would darken its own border toward invisibility, since both colors share one
        /// texture).</summary>
        private const float ToastGapBelowHeaderPixels = 16f;
        private const float ToastFallbackAnchoredY = -220f;

        private RectTransform toastRect;

        private void BuildToast()
        {
            Canvas rootCanvas = canvasRect.GetComponentInParent<Canvas>();
            Transform toastParent = rootCanvas != null ? rootCanvas.transform : canvasRect;

            toastObject = new GameObject("WalletFreezeExpiredToast", typeof(RectTransform));
            toastObject.transform.SetParent(toastParent, false);
            toastObject.transform.SetAsLastSibling();

            toastRect = toastObject.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.anchoredPosition = new Vector2(0f, ToastFallbackAnchoredY);
            toastRect.sizeDelta = new Vector2(620f, 96f);

            Image image = toastObject.AddComponent<Image>();
            image.color = Palette.Surface;
            image.raycastTarget = false;

            Outline glowOutline = toastObject.AddComponent<Outline>();
            glowOutline.effectColor = Palette.Glow;
            glowOutline.effectDistance = new Vector2(2f, 2f);
            glowOutline.useGraphicAlpha = false;

            toastGroup = toastObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(toastObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 8f);
            labelRect.offsetMax = new Vector2(-20f, -8f);

            toastLabel = labelObject.AddComponent<TextMeshProUGUI>();
            toastLabel.color = AccentColor;
            toastLabel.fontStyle = FontStyles.Bold;
            toastLabel.alignment = TextAlignmentOptions.Center;
            toastLabel.fontSize = 28f;
            toastLabel.enableAutoSizing = true;
            toastLabel.fontSizeMin = 18f;
            toastLabel.fontSizeMax = 28f;
            toastLabel.textWrappingMode = TextWrappingModes.Normal;
            toastLabel.raycastTarget = false;

            toastObject.SetActive(false);
        }

        /// <summary>Computes the HUD header's bottom edge in the toast's own parent-local space
        /// (the same world-corner -> screen-point -> local-point pipeline UINudgePointer.
        /// TryGetLocalBottomEdge already uses successfully against this exact header) and docks
        /// the toast's top edge ToastGapBelowHeaderPixels below it. anchorY below is the anchor
        /// reference point expressed in that same local-rect frame (Lerp between parentRect's own
        /// rect.yMin/yMax) -- subtracting it from the measured header edge converts the result
        /// into a valid anchoredPosition for a pivot/anchor of (0.5, 1), independent of whatever
        /// pivot the parent Canvas RectTransform itself happens to use.</summary>
        private void PositionToastBelowHeader()
        {
            if (toastRect == null) { return; }

            RectTransform headerRect = HUDController.Instance != null ? HUDController.Instance.HeaderPanelRect : null;
            RectTransform parentRect = toastRect.parent as RectTransform;
            if (headerRect == null || parentRect == null)
            {
                toastRect.anchoredPosition = new Vector2(0f, ToastFallbackAnchoredY);
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(headerRect);

            Canvas canvas = parentRect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            Vector3[] corners = new Vector3[4];
            headerRect.GetWorldCorners(corners);

            bool any = false;
            float minLocalY = float.PositiveInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint))
                {
                    any = true;
                    if (localPoint.y < minLocalY) { minLocalY = localPoint.y; }
                }
            }

            if (!any)
            {
                toastRect.anchoredPosition = new Vector2(0f, ToastFallbackAnchoredY);
                return;
            }

            float anchorY = Mathf.Lerp(parentRect.rect.yMin, parentRect.rect.yMax, toastRect.anchorMin.y);
            Vector2 pos = toastRect.anchoredPosition;
            pos.y = minLocalY - anchorY - ToastGapBelowHeaderPixels;
            toastRect.anchoredPosition = pos;
        }

        private void ShowToast(string message)
        {
            if (toastObject == null) { return; }
            toastLabel.text = message;
            PositionToastBelowHeader();
            toastObject.SetActive(true);
            if (toastCoroutine != null) { StopCoroutine(toastCoroutine); }
            toastCoroutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            float t = 0f;
            while (t < ToastFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                toastGroup.alpha = Mathf.Clamp01(t / ToastFadeSeconds);
                yield return null;
            }
            toastGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(ToastVisibleSeconds);

            t = 0f;
            while (t < ToastFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                toastGroup.alpha = 1f - Mathf.Clamp01(t / ToastFadeSeconds);
                yield return null;
            }
            toastGroup.alpha = 0f;
            toastObject.SetActive(false);
            toastCoroutine = null;
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
            NudgeModalScope.SetOpen(panelGroup.gameObject, !hidden);
            panelGroup.alpha = hidden ? 0f : 1f;
            panelGroup.blocksRaycasts = !hidden;
            panelGroup.interactable = !hidden;
        }

        /// <summary>Rebuilds the row list from GodTierStoreManager's live inventory + active-freeze
        /// state. Called from Open(), from HandleItemsChanged (a purchase/activation happened
        /// while already open), and once a second from HandleSecondTick while visible (keeps the
        /// active countdown and every row's locked-state label honest).</summary>
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
            if (manager == null)
            {
                if (emptyState != null) { emptyState.SetActive(true); }
                return;
            }

            bool hasActive = manager.HasActiveFreeze;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<GodTierStoreItemData> freezeItems = new List<GodTierStoreItemData>();
            int totalOwnedAcrossItems = 0;
            for (int i = 0; i < manager.Items.Count; i++)
            {
                GodTierStoreItemData item = manager.Items[i];
                if (item == null || item.effectType != GodTierStoreEffectType.BrainFreezeIQImmunity) { continue; }
                int count = manager.GetFreezeInventoryCount(item.itemId);
                if (count > 0) { freezeItems.Add(item); totalOwnedAcrossItems += count; }
            }

            bool anyContent = hasActive || freezeItems.Count > 0;
            if (emptyState != null) { emptyState.SetActive(!anyContent); }
            if (!anyContent) { return; }

            if (hasActive)
            {
                BuildActiveCard(manager, now);
            }

            for (int i = 0; i < freezeItems.Count; i++)
            {
                BuildInventoryRow(freezeItems[i], manager, hasActive);
            }
        }

        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded) { return; }
            cardSprite = Resources.Load<Sprite>("UI/Generated/Wallet_Card");
            shadowSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Shadow");
            pillSprite = Resources.Load<Sprite>("UI/Generated/Alert_Button");
            spritesLoaded = true;
        }

        // 2026-10-06: tier color/icon lookup extracted to FreezeTierVisuals (shared with the new
        // FreezeTutorialPopupUI) -- TierColor/TierSprite below were this file's own private copies.

        /// <summary>The single active-freeze card, pinned above the inventory rows (rule 4: "shown
        /// on top with its countdown pill"). No Use button -- there's nothing to do with a freeze
        /// that's already running.</summary>
        private void BuildActiveCard(GodTierStoreManager manager, long now)
        {
            EnsureSpritesLoaded();

            string activeItemId = manager.ActiveFreezeItemId;
            GodTierStoreItemData activeItem = ResolveItem(manager, activeItemId);
            long expiry = PlayerIQManager.Instance != null ? PlayerIQManager.Instance.BrainFreezeExpiryUnixSeconds : now;
            long secondsRemaining = Math.Max(0L, expiry - now);
            float totalDurationSeconds = activeItem != null ? activeItem.freezeDurationHours * 3600f : 0f;
            bool isLowTime = totalDurationSeconds > 0f && secondsRemaining < totalDurationSeconds * 0.1f;
            GodTierStoreRarityTier tier = activeItem != null ? activeItem.rarityTier : GodTierStoreRarityTier.None;

            GameObject rowObject = new GameObject("ActiveFreezeCard", typeof(RectTransform));
            rowObject.transform.SetParent(contentRoot, false);
            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 180f;

            BuildCardBackground(rowObject.transform);

            BuildTierIcon(rowObject.transform, tier, 0);

            GameObject nameObject = new GameObject("RarityTierNameText", typeof(RectTransform));
            nameObject.transform.SetParent(rowObject.transform, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(140f, 0f);
            nameRect.offsetMax = new Vector2(-24f, -16f);
            TextMeshProUGUI nameLabel = nameObject.AddComponent<TextMeshProUGUI>();
            nameLabel.text = "ACTIVE: " + (activeItem != null ? activeItem.displayName : "FREEZE");
            nameLabel.color = FreezeTierVisuals.TierColor(tier);
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.alignment = TextAlignmentOptions.BottomLeft;
            nameLabel.fontSize = 26f;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 18f;
            nameLabel.fontSizeMax = 26f;
            nameLabel.textWrappingMode = TextWrappingModes.Normal;
            nameLabel.raycastTarget = false;

            BuildCountdownPill(rowObject.transform, secondsRemaining, isLowTime, new Vector2(140f, 20f));
        }

        /// <summary>One owned freeze itemId's row: icon+badge, tier-colored name, duration, and a
        /// Use button that either activates it (ActivateFreeze) or, while another freeze is
        /// already active, shows the shared "Active — Xd Yh left" countdown and is disabled.</summary>
        private void BuildInventoryRow(GodTierStoreItemData item, GodTierStoreManager manager, bool lockedByActiveFreeze)
        {
            EnsureSpritesLoaded();

            int count = manager.GetFreezeInventoryCount(item.itemId);
            GodTierStoreRarityTier tier = item.rarityTier;

            GameObject rowObject = new GameObject("WalletRow", typeof(RectTransform));
            rowObject.transform.SetParent(contentRoot, false);
            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 180f;

            BuildCardBackground(rowObject.transform);
            BuildTierIcon(rowObject.transform, tier, count);

            GameObject nameObject = new GameObject("RarityTierNameText", typeof(RectTransform));
            nameObject.transform.SetParent(rowObject.transform, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.55f);
            nameRect.anchorMax = new Vector2(0.62f, 1f);
            nameRect.offsetMin = new Vector2(140f, 0f);
            nameRect.offsetMax = new Vector2(0f, -14f);
            TextMeshProUGUI nameLabel = nameObject.AddComponent<TextMeshProUGUI>();
            nameLabel.text = item.displayName;
            nameLabel.color = FreezeTierVisuals.TierColor(tier);
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.alignment = TextAlignmentOptions.BottomLeft;
            nameLabel.fontSize = 26f;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 18f;
            nameLabel.fontSizeMax = 26f;
            nameLabel.textWrappingMode = TextWrappingModes.Normal;
            nameLabel.raycastTarget = false;

            GameObject durationObject = new GameObject("DurationText", typeof(RectTransform));
            durationObject.transform.SetParent(rowObject.transform, false);
            RectTransform durationRect = durationObject.GetComponent<RectTransform>();
            durationRect.anchorMin = new Vector2(0f, 0.2f);
            durationRect.anchorMax = new Vector2(0.62f, 0.55f);
            durationRect.offsetMin = new Vector2(140f, 0f);
            durationRect.offsetMax = new Vector2(0f, 0f);
            TextMeshProUGUI durationLabel = durationObject.AddComponent<TextMeshProUGUI>();
            durationLabel.text = FormatDuration(item.freezeDurationHours);
            durationLabel.color = MutedTextColor;
            durationLabel.alignment = TextAlignmentOptions.TopLeft;
            durationLabel.fontSize = 22f;
            durationLabel.enableAutoSizing = true;
            durationLabel.fontSizeMin = 16f;
            durationLabel.fontSizeMax = 22f;
            durationLabel.raycastTarget = false;

            GameObject useButtonObject = new GameObject("UseButton", typeof(RectTransform));
            useButtonObject.transform.SetParent(rowObject.transform, false);
            RectTransform useButtonRect = useButtonObject.GetComponent<RectTransform>();
            useButtonRect.anchorMin = new Vector2(0.64f, 0.22f);
            useButtonRect.anchorMax = new Vector2(0.96f, 0.78f);
            useButtonRect.offsetMin = Vector2.zero;
            useButtonRect.offsetMax = Vector2.zero;
            // 2026-10-07 play-test fix: was a flat translucent tint (0.18 alpha) -- the real
            // Alert_Button pill now, matching every other action button in this codebase. White
            // pass-through when enabled (the sprite's own baked Cyan/Glow reads as-is); a solid
            // (alpha=1) grey multiply when disabled -- see DisabledPillTint's own doc comment for
            // why that stays opaque rather than translucent.
            Image useButtonImage = useButtonObject.AddComponent<Image>();
            if (pillSprite != null) { useButtonImage.sprite = pillSprite; useButtonImage.type = Image.Type.Sliced; }
            Button useButton = useButtonObject.AddComponent<Button>();
            useButton.targetGraphic = useButtonImage;

            GameObject useLabelObject = new GameObject("Label", typeof(RectTransform));
            useLabelObject.transform.SetParent(useButtonObject.transform, false);
            RectTransform useLabelRect = useLabelObject.GetComponent<RectTransform>();
            useLabelRect.anchorMin = Vector2.zero;
            useLabelRect.anchorMax = Vector2.one;
            useLabelRect.offsetMin = new Vector2(10f, 6f);
            useLabelRect.offsetMax = new Vector2(-10f, -6f);
            TextMeshProUGUI useLabel = useLabelObject.AddComponent<TextMeshProUGUI>();
            useLabel.alignment = TextAlignmentOptions.Center;
            useLabel.fontStyle = FontStyles.Bold;
            // "USE label: Cyan bold >=26" -- floor set at exactly that.
            useLabel.fontSize = 30f;
            useLabel.enableAutoSizing = true;
            useLabel.fontSizeMin = 18f; // only reached by the longer "Active -- Xd Yh left" state
            useLabel.fontSizeMax = 30f;
            useLabel.textWrappingMode = TextWrappingModes.Normal;
            useLabel.raycastTarget = false;

            if (lockedByActiveFreeze)
            {
                // rule 2: "every Use button is disabled and shows 'Active — 2d 14h left'" -- the
                // same global countdown on every row, since only one freeze can run regardless of
                // which item it came from.
                long expiry = PlayerIQManager.Instance != null ? PlayerIQManager.Instance.BrainFreezeExpiryUnixSeconds : 0L;
                long remaining = Math.Max(0L, expiry - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                useLabel.text = "Active — " + FormatActiveRemaining(remaining) + " left";
                useLabel.color = UseDisabledColor;
                useButtonImage.color = DisabledPillTint;
                useButton.interactable = false;
            }
            else
            {
                useLabel.text = "USE";
                useLabel.fontSizeMin = 26f; // "USE" alone never needs to shrink below the floor
                useLabel.color = UseEnabledColor;
                useButtonImage.color = Color.white;
                string itemId = item.itemId;
                useButton.onClick.AddListener(() => GodTierStoreManager.Instance?.ActivateFreeze(itemId));
                useButton.interactable = true;
            }
        }

        private void BuildCardBackground(Transform parent)
        {
            if (shadowSprite != null)
            {
                GameObject shadowObject = new GameObject("Shadow", typeof(RectTransform));
                shadowObject.transform.SetParent(parent, false);
                RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                shadowRect.offsetMin = new Vector2(6f, -6f);
                shadowRect.offsetMax = new Vector2(6f, -6f);
                Image shadowImage = shadowObject.AddComponent<Image>();
                shadowImage.sprite = shadowSprite;
                shadowImage.type = Image.Type.Sliced;
                shadowImage.raycastTarget = false;
            }

            GameObject cardObject = new GameObject("Card", typeof(RectTransform));
            cardObject.transform.SetParent(parent, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            Image cardImage = cardObject.AddComponent<Image>();
            if (cardSprite != null) { cardImage.sprite = cardSprite; cardImage.type = Image.Type.Sliced; }
            else { cardImage.color = RowColor; }
            cardImage.raycastTarget = false;
        }

        /// <summary>Tier cup icon with a live "xN" count badge -- badge text always comes from a
        /// TMP component, never baked into the generated PNG (project convention). count == 0
        /// suppresses the badge (used for the active card, where showing a stale leftover count
        /// next to "ACTIVE" would be confusing).</summary>
        private void BuildTierIcon(Transform parent, GodTierStoreRarityTier tier, int count)
        {
            GameObject iconObject = new GameObject("RarityTierIcon", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(20f, 0f);
            iconRect.sizeDelta = new Vector2(104f, 104f);
            Image iconImage = iconObject.AddComponent<Image>();
            Sprite sprite = FreezeTierVisuals.TierSprite(tier);
            if (sprite != null) { iconImage.sprite = sprite; iconImage.type = Image.Type.Simple; iconImage.preserveAspect = true; }
            iconImage.color = Color.white; // pass-through onto the generated sprite's own baked tint
            iconImage.raycastTarget = false;

            if (count > 0)
            {
                GameObject badgeObject = new GameObject("CountBadge", typeof(RectTransform));
                badgeObject.transform.SetParent(iconObject.transform, false);
                RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = new Vector2(-4f, 4f);
                // 2026-10-07 play-test fix: was 44x32, too small to comfortably fit >=22pt bold
                // text -- enlarged to fit the new floor with real padding.
                badgeRect.sizeDelta = new Vector2(56f, 40f);
                Image badgeImage = badgeObject.AddComponent<Image>();
                badgeImage.color = Palette.Base;
                badgeImage.raycastTarget = false;

                GameObject badgeLabelObject = new GameObject("Label", typeof(RectTransform));
                badgeLabelObject.transform.SetParent(badgeObject.transform, false);
                RectTransform badgeLabelRect = badgeLabelObject.GetComponent<RectTransform>();
                badgeLabelRect.anchorMin = Vector2.zero;
                badgeLabelRect.anchorMax = Vector2.one;
                badgeLabelRect.offsetMin = Vector2.zero;
                badgeLabelRect.offsetMax = Vector2.zero;
                TextMeshProUGUI badgeLabel = badgeLabelObject.AddComponent<TextMeshProUGUI>();
                badgeLabel.text = "x" + count;
                badgeLabel.color = Palette.White;
                badgeLabel.fontStyle = FontStyles.Bold;
                badgeLabel.alignment = TextAlignmentOptions.Center;
                // "count badge: >=22pt bold White" -- floor set at exactly that.
                badgeLabel.fontSize = 26f;
                badgeLabel.enableAutoSizing = true;
                badgeLabel.fontSizeMin = 22f;
                badgeLabel.fontSizeMax = 26f;
                badgeLabel.raycastTarget = false;
            }
        }

        private void BuildCountdownPill(Transform parent, long secondsRemaining, bool isLowTime, Vector2 anchoredPosition)
        {
            GameObject pillObject = new GameObject("CountdownPill", typeof(RectTransform));
            pillObject.transform.SetParent(parent, false);
            RectTransform pillRect = pillObject.GetComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0f, 0f);
            pillRect.anchorMax = new Vector2(0f, 0f);
            pillRect.pivot = new Vector2(0f, 0f);
            pillRect.anchoredPosition = anchoredPosition;
            pillRect.sizeDelta = new Vector2(228f, 52f);
            Image pillImage = pillObject.AddComponent<Image>();
            if (pillSprite != null) { pillImage.sprite = pillSprite; pillImage.type = Image.Type.Sliced; }
            pillImage.color = isLowTime ? (Color)PillWarning : (Color)PillCyan;
            pillImage.raycastTarget = false;

            GameObject pillLabelObject = new GameObject("Label", typeof(RectTransform));
            pillLabelObject.transform.SetParent(pillObject.transform, false);
            RectTransform pillLabelRect = pillLabelObject.GetComponent<RectTransform>();
            pillLabelRect.anchorMin = Vector2.zero;
            pillLabelRect.anchorMax = Vector2.one;
            pillLabelRect.offsetMin = new Vector2(8f, 4f);
            pillLabelRect.offsetMax = new Vector2(-8f, -4f);
            TextMeshProUGUI pillLabel = pillLabelObject.AddComponent<TextMeshProUGUI>();
            pillLabel.text = FormatPillCountdown(secondsRemaining);
            pillLabel.color = Color.white;
            pillLabel.fontStyle = FontStyles.Bold;
            pillLabel.alignment = TextAlignmentOptions.Center;
            pillLabel.characterSpacing = 2f;
            pillLabel.fontSize = 26f;
            pillLabel.enableAutoSizing = true;
            pillLabel.fontSizeMin = 20f;
            pillLabel.fontSizeMax = 26f;
            pillLabel.textWrappingMode = TextWrappingModes.NoWrap;
            pillLabel.raycastTarget = false;
        }

        /// <summary>Fixed-width countdown for the active card's pill: "Nd HH:MM" past a day, else
        /// "HH:MM:SS".</summary>
        private static string FormatPillCountdown(long totalSeconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            if (span.TotalDays >= 1d)
            {
                return $"{(int)span.TotalDays}d {span.Hours:00}:{span.Minutes:00}";
            }

            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }

        /// <summary>Matches rule 2's exact wording shape ("2d 14h") for a locked Use button's
        /// label -- coarser than the pill's HH:MM:SS, since this is a secondary/redundant display
        /// of the same countdown already shown in full on the active card above.</summary>
        private static string FormatActiveRemaining(long totalSeconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            if (span.TotalDays >= 1d)
            {
                return $"{(int)span.TotalDays}d {span.Hours}h";
            }
            if (span.TotalHours >= 1d)
            {
                return $"{(int)span.TotalHours}h {span.Minutes}m";
            }
            return $"{span.Minutes}m";
        }

        private static string FormatDuration(float hours)
        {
            int wholeHours = Mathf.RoundToInt(hours);
            if (wholeHours >= 24 && wholeHours % 24 == 0)
            {
                int days = wholeHours / 24;
                return days == 1 ? "24 HOURS" : $"{days} DAYS";
            }
            return $"{wholeHours} HOURS";
        }

        private static GodTierStoreItemData ResolveItem(GodTierStoreManager manager, string itemId)
        {
            if (manager == null || string.IsNullOrWhiteSpace(itemId)) { return null; }
            IReadOnlyList<GodTierStoreItemData> items = manager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == itemId) { return items[i]; }
            }
            return null;
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
