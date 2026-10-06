using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-06 FREEZE TUTORIAL (Aceyfer add-on to the freeze-inventory amendment): a one-time
    /// popup explaining the Wallet/charge/activate model, shown the first time any freeze enters
    /// the Wallet for ANY reason -- a real purchase or a Cloud Save restore (GodTierStoreManager.
    /// OnFreezeChargeGained, fired from both GrantFreezeInventory and ReconcileFreezeInventory,
    /// plus a one-frame-deferred catch-up check in GodTierStoreManager.Start() for a save that
    /// already has charges but never saw this). Styled like the event popup per spec --
    /// Alert_Frame border, a Surface-colored inner fill (deliberately NOT the fully-transparent
    /// inner body RandomEventUIController uses, so this reads as a distinct "info" surface rather
    /// than reusing that popup's own broadcast-interrupt chrome), Cyan title, White body, an
    /// Alert_Button pill for the confirm action -- but otherwise a bespoke, disposable overlay
    /// (IntelCardUI.Show's "build fresh, destroy on confirm" pattern), not a reuse of
    /// RandomEventUIController's scene-wired GameObject or its fake-close-button dark pattern,
    /// which has no place in a tutorial.
    ///
    /// Self-bootstrapping singleton purely to own the event subscription (Bible §8, same pattern
    /// as every other new UI system this session) -- the actual popup construction/teardown is a
    /// disposable overlay built fresh each time, exactly like IntelCardUI, since it only ever
    /// shows once per save, ever.
    /// </summary>
    public sealed class FreezeTutorialPopupUI : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";
        private const string WalletButtonName = "WalletOpenButton";
        private const int OverlaySortingOrder = 480; // below IntelCardUI's FTUE modals (500)
        private const float WalletPulsePeriodSeconds = 0.6f;

        private const string TitleText = "YOUR FREEZES ARE IN THE WALLET";
        private const string ConfirmText = "GOT IT";
        private static readonly string[] BodyLines =
        {
            "Freezes wait in your Wallet until you use them.",
            "Tap USE to start one. Only one can run at a time.",
            "When it ends, come back and use the next.",
        };

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.75f);

        private static Sprite frameSprite;
        private static Sprite buttonSprite;
        private static bool spritesLoaded;

        private static FreezeTutorialPopupUI instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing
        /// placed one in the scene (matches PocketPanelUI/TimedPurchaseWalletUI).</summary>
        public static FreezeTutorialPopupUI Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<FreezeTutorialPopupUI>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("FreezeTutorialPopupUI");
                    instance = hostObject.AddComponent<FreezeTutorialPopupUI>();
                }

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private GodTierStoreManager subscribedManager;

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
            subscribedManager = GodTierStoreManager.Instance;
            if (subscribedManager != null)
            {
                subscribedManager.OnFreezeChargeGained -= HandleFreezeChargeGained;
                subscribedManager.OnFreezeChargeGained += HandleFreezeChargeGained;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (subscribedManager != null)
            {
                subscribedManager.OnFreezeChargeGained -= HandleFreezeChargeGained;
                subscribedManager = null;
            }

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void HandleFreezeChargeGained(string itemId)
        {
            GodTierStoreManager manager = GodTierStoreManager.Instance;
            if (manager == null || manager.FreezeTutorialSeen)
            {
                return;
            }

            manager.MarkFreezeTutorialSeen();
            Show(itemId, manager);
        }

        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded) { return; }
            frameSprite = Resources.Load<Sprite>("UI/Generated/Alert_Frame");
            buttonSprite = Resources.Load<Sprite>("UI/Generated/Alert_Button");
            spritesLoaded = true;
        }

        private void Show(string itemId, GodTierStoreManager manager)
        {
            EnsureSpritesLoaded();

            GodTierStoreItemData item = ResolveItem(manager, itemId);

            GameObject overlayObject = new GameObject("FreezeTutorialOverlay", typeof(RectTransform));
            NudgeModalScope.SetOpen(overlayObject, true);
            Canvas canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = overlayObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayObject.AddComponent<GraphicRaycaster>();

            GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropObject.transform.SetParent(overlayObject.transform, false);
            RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            Image backdropImage = backdropObject.GetComponent<Image>();
            backdropImage.color = BackdropColor;
            backdropImage.raycastTarget = true; // block input to the game behind, same as IntelCardUI

            GameObject cardObject = new GameObject("TutorialCard", typeof(RectTransform), typeof(Image));
            cardObject.transform.SetParent(overlayObject.transform, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(820f, 900f);
            Image cardImage = cardObject.GetComponent<Image>();
            if (frameSprite != null) { cardImage.sprite = frameSprite; cardImage.type = Image.Type.Sliced; }
            cardImage.color = Color.white; // art is pre-colored; a plain white tint is a pure pass-through
            cardImage.raycastTarget = false;

            GameObject innerObject = new GameObject("InnerFill", typeof(RectTransform), typeof(Image));
            innerObject.transform.SetParent(cardObject.transform, false);
            RectTransform innerRect = innerObject.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(20f, 20f);
            innerRect.offsetMax = new Vector2(-20f, -20f);
            Image innerImage = innerObject.GetComponent<Image>();
            innerImage.color = new Color(Palette.Surface.r, Palette.Surface.g, Palette.Surface.b, 0.96f);
            innerImage.raycastTarget = false;

            GameObject iconObject = new GameObject("RarityTierIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(innerObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -36f);
            iconRect.sizeDelta = new Vector2(160f, 160f);
            Image iconImage = iconObject.GetComponent<Image>();
            Sprite tierSprite = item != null ? FreezeTierVisuals.TierSprite(item.rarityTier) : null;
            if (tierSprite != null) { iconImage.sprite = tierSprite; iconImage.preserveAspect = true; }
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(innerObject.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -220f);
            titleRect.sizeDelta = new Vector2(-64f, 140f);
            TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
            title.text = TitleText;
            title.color = Palette.Cyan;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            // "Text must be readable at 16:9 Portrait: title >=36" -- floor set at exactly that.
            title.fontSize = 44f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 36f;
            title.fontSizeMax = 44f;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.raycastTarget = false;

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(innerObject.transform, false);
            RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -368f);
            bodyRect.sizeDelta = new Vector2(-72f, 340f);
            TextMeshProUGUI body = bodyObject.AddComponent<TextMeshProUGUI>();
            body.text = string.Join("\n\n", BodyLines);
            body.color = Palette.White;
            body.alignment = TextAlignmentOptions.Center;
            // "body >=28" -- floor set at exactly that.
            body.fontSize = 32f;
            body.enableAutoSizing = true;
            body.fontSizeMin = 28f;
            body.fontSizeMax = 32f;
            body.lineSpacing = 6f;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.raycastTarget = false;

            GameObject buttonObject = new GameObject("GotItButton", typeof(RectTransform), typeof(Image));
            buttonObject.transform.SetParent(innerObject.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 40f);
            buttonRect.sizeDelta = new Vector2(320f, 96f);
            Image buttonImage = buttonObject.GetComponent<Image>();
            if (buttonSprite != null) { buttonImage.sprite = buttonSprite; buttonImage.type = Image.Type.Sliced; }
            buttonImage.color = Color.white;
            Button confirmButton = buttonObject.AddComponent<Button>();
            confirmButton.targetGraphic = buttonImage;

            GameObject buttonLabelObject = new GameObject("Label", typeof(RectTransform));
            buttonLabelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = new Vector2(16f, 12f);
            buttonLabelRect.offsetMax = new Vector2(-16f, -12f);
            TextMeshProUGUI buttonLabel = buttonLabelObject.AddComponent<TextMeshProUGUI>();
            buttonLabel.text = ConfirmText;
            buttonLabel.color = Palette.Cyan;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.fontSize = 32f;
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = 24f;
            buttonLabel.fontSizeMax = 32f;
            buttonLabel.raycastTarget = false;

            confirmButton.onClick.AddListener(() =>
            {
                Destroy(overlayObject);
                PulseWalletButton();
            });
        }

        /// <summary>"Then the Wallet button pulses (Glow) once to point the way" -- one full sine
        /// cycle (base color -> Glow -> base color) via AnimationController's existing infinite
        /// glow-pulse primitive, manually stopped after exactly one period rather than left
        /// running, since the spec asks for a single pulse, not a persistent highlight.</summary>
        private void PulseWalletButton()
        {
            GameObject walletButton = GameObject.Find(WalletButtonName);
            Image walletImage = walletButton != null ? walletButton.GetComponent<Image>() : null;
            if (walletImage == null) { return; }

            Color baseColor = walletImage.color;
            AnimationController.PlayColorGlowPulse(walletImage, baseColor, Palette.Glow, WalletPulsePeriodSeconds);
            StartCoroutine(StopWalletPulseAfterOneCycle(walletImage, baseColor));
        }

        private IEnumerator StopWalletPulseAfterOneCycle(Image walletImage, Color baseColor)
        {
            yield return new WaitForSeconds(WalletPulsePeriodSeconds);
            AnimationController.StopColorGlowPulse(walletImage, baseColor);
        }

        private static GodTierStoreItemData ResolveItem(GodTierStoreManager manager, string itemId)
        {
            if (manager == null || string.IsNullOrWhiteSpace(itemId)) { return null; }
            var items = manager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == itemId) { return items[i]; }
            }
            return null;
        }
    }
}
