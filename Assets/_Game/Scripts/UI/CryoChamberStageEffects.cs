using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-05 ART PASS 2: wires the code-generated Stage 0 "Cryo Chamber" backdrop
    /// (CryoChamberBackdropGenerator) into BackgroundStageView without a scene write --
    /// self-bootstrapping like PocketPanelUI/RuntimePaletteFixups, no Inspector reference needed.
    /// Three jobs when useCryoBackdropOverride is on: (1) override BackgroundStageView.
    /// stageSprites[0] with the generated room art, (2) layer a second Image using the isolated
    /// pod-glow sprite on top, pulsing its alpha on a slow 4s sine so the pod lights read as
    /// alive, (3) place two small "DECEASED" TMP labels over the two tagged pods -- the generator
    /// bakes a plain Dim rectangle there, not text, matching this project's convention that text
    /// always comes from a TMP component. All three are only ever visible while World Restoration
    /// is actually at stage 0, since the glow/labels would otherwise float over whatever backdrop
    /// a later stage swaps in. The room art itself is swappable: dropping a PNG named
    /// CryoChamber_Backdrop.png into Assets/Resources/UI/Generated/ is picked up automatically
    /// instead of the generated room sprite -- see Start().
    ///
    /// 2026-10-09 play-test fix: this used to override Stage 0's backdrop UNCONDITIONALLY on
    /// every boot, silently replacing whatever Leonardo-painted art was authored in
    /// BackgroundStageView's own stageSprites[0] Inspector slot, with no way back -- confirmed via
    /// BackgroundStageView.OverrideStageSprite (overwrites the live array, no restore path) and
    /// this class's own Start() (self-bootstraps unconditionally via RuntimeInitializeOnLoadMethod,
    /// not gated on current stage at all). All three jobs above are now gated behind
    /// useCryoBackdropOverride (OFF by default) -- the pod-glow/DECEASED-tag overlay is included
    /// in that gate too, not just the backdrop swap: both are positioned at exact pixel
    /// coordinates matching only the generated cryo room's own pod layout, so there is no
    /// "backdrop-independent" cryo effect (a generic tint/frost/particle layer) to keep running
    /// over the real painted art -- showing them without the matching backdrop would just float
    /// glow blobs and DECEASED labels over unrelated art.
    /// </summary>
    public sealed class CryoChamberStageEffects : MonoBehaviour
    {
        [Tooltip("OFF by default so the Leonardo-painted Stage 0 backdrop always shows. Turn this on once a painted (or accepted placeholder) cryo backdrop is ready to ship -- it swaps BackgroundStageView.stageSprites[0] for the generated/drop-in Cryo Chamber room art and enables the pod-glow + DECEASED-tag overlay, which only make sense together with that specific room art.")]
        [SerializeField] private bool useCryoBackdropOverride = false;

        private const float PulsePeriodSeconds = 4f;
        private const float PulseAlphaMin = 0.3f;
        private const float PulseAlphaMax = 0.75f;

        // Normalized (0-1) positions of the two DECEASED tag centers, derived from
        // CryoChamberBackdropGenerator's own 768x1344 pod layout math (pod indices 0 and 5,
        // tag center around y=957 from the top). Unity anchors are bottom-up, the generator's
        // pixel math is top-down, hence the 1f- flip on Y.
        private static readonly Vector2[] DeceasedTagAnchors =
        {
            new Vector2(76.86f / 768f, 1f - 957f / 1344f),
            new Vector2(691.14f / 768f, 1f - 957f / 1344f),
        };

        private static CryoChamberStageEffects instance;

        private GameObject overlayRoot;
        private Image glowImage;
        private BackgroundStageView cachedBackgroundView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) { return; }
            var hostObject = new GameObject("CryoChamberStageEffects");
            instance = hostObject.AddComponent<CryoChamberStageEffects>();
        }

        private void Start()
        {
            BackgroundStageView backgroundView = FindAnyObjectByType<BackgroundStageView>();
            Image backgroundImage = backgroundView != null ? backgroundView.GetComponent<Image>() : null;
            if (backgroundView == null || backgroundImage == null) { return; }
            cachedBackgroundView = backgroundView;

            if (useCryoBackdropOverride)
            {
                // 2026-10-05 play-test fix: a hand-painted replacement is swapped in automatically
                // if Aceyfer drops one into Assets/Resources/UI/Generated/CryoChamber_Backdrop.png
                // -- same folder the generated art already lives in (Resources.Load can't see
                // outside a Resources folder at runtime, so this is the one location a drop-in
                // file can live for this to work in an actual build, not just the Editor). Falls
                // back to the generated room art when no override file is present.
                Sprite roomSprite = Resources.Load<Sprite>("UI/Generated/CryoChamber_Backdrop")
                    ?? Resources.Load<Sprite>("UI/Generated/Stage0_CryoChamber");
                if (roomSprite != null) { backgroundView.OverrideStageSprite(0, roomSprite); }

                BuildOverlay(backgroundImage.transform);
            }

            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager != null)
            {
                manager.OnRestorationStageChanged -= HandleStageChanged;
                manager.OnRestorationStageChanged += HandleStageChanged;
            }

            RefreshVisibility(manager?.CurrentStage?.stageIndex ?? 0);
        }

        private void OnDestroy()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager != null) { manager.OnRestorationStageChanged -= HandleStageChanged; }
        }

        private void HandleStageChanged(WorldRestorationStage stage)
        {
            RefreshVisibility(stage != null ? stage.stageIndex : 0);
        }

        /// <summary>2026-10-09 play-test fix: once the player moves past Stage 0, this now also
        /// tells BackgroundStageView to restore whatever painted art was in slot 0 before the
        /// override -- a no-op if useCryoBackdropOverride is off (nothing was ever overridden to
        /// restore), and harmless to call every stage change regardless of current override
        /// state, since ClearOverride itself is already a safe no-op once already cleared.</summary>
        private void RefreshVisibility(int stageIndex)
        {
            if (overlayRoot != null) { overlayRoot.SetActive(stageIndex == 0); }

            if (stageIndex != 0 && cachedBackgroundView != null)
            {
                cachedBackgroundView.ClearOverride(0);
            }
        }

        private void BuildOverlay(Transform backgroundTransform)
        {
            Sprite glowSprite = Resources.Load<Sprite>("UI/Generated/Stage0_CryoChamber_PodGlow");
            if (glowSprite == null) { return; }

            overlayRoot = new GameObject("CryoChamberOverlay", typeof(RectTransform));
            overlayRoot.transform.SetParent(backgroundTransform.parent, false);
            overlayRoot.transform.SetSiblingIndex(backgroundTransform.GetSiblingIndex() + 1);
            RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject glowObject = new GameObject("PodGlow", typeof(RectTransform), typeof(Image));
            glowObject.transform.SetParent(overlayRoot.transform, false);
            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            glowImage = glowObject.GetComponent<Image>();
            glowImage.sprite = glowSprite;
            glowImage.preserveAspect = false;
            glowImage.raycastTarget = false;
            glowImage.color = new Color(1f, 1f, 1f, PulseAlphaMin);

            for (int i = 0; i < DeceasedTagAnchors.Length; i++)
            {
                BuildDeceasedTag(overlayRoot.transform, DeceasedTagAnchors[i]);
            }
        }

        /// <summary>
        /// 2026-10-05 play-test fix: was 80x24 at 10-14pt dark-on-Dim (the generator's own baked
        /// Dim rectangle) -- unreadable at actual screen size. ~2x larger, and a real Base plate
        /// Image drawn on top of the baked rectangle (opaque, so it fully overrides it) with Cyan
        /// text, per Aceyfer's play-test note. Still a placeholder -- CryoChamberBackdropGenerator
        /// keeps baking the plain Dim rect underneath for whenever this gets replaced with painted
        /// art and this runtime overlay goes away.
        /// </summary>
        private void BuildDeceasedTag(Transform parent, Vector2 normalizedAnchor)
        {
            GameObject plateObject = new GameObject("DeceasedTagPlate", typeof(RectTransform), typeof(Image));
            plateObject.transform.SetParent(parent, false);
            RectTransform plateRect = plateObject.GetComponent<RectTransform>();
            plateRect.anchorMin = plateRect.anchorMax = normalizedAnchor;
            plateRect.pivot = new Vector2(0.5f, 0.5f);
            plateRect.sizeDelta = new Vector2(160f, 48f);

            Image plateImage = plateObject.GetComponent<Image>();
            plateImage.color = Palette.Base;
            plateImage.raycastTarget = false;

            GameObject labelObject = new GameObject("DeceasedTagLabel", typeof(RectTransform));
            labelObject.transform.SetParent(plateObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "DECEASED";
            label.color = Palette.Cyan;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 28f;
            label.raycastTarget = false;
        }

        private void Update()
        {
            if (glowImage == null || overlayRoot == null || !overlayRoot.activeSelf) { return; }

            float phase = (Time.unscaledTime % PulsePeriodSeconds) / PulsePeriodSeconds;
            float sine = (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) * 0.5f;
            Color c = glowImage.color;
            c.a = Mathf.Lerp(PulseAlphaMin, PulseAlphaMax, sine);
            glowImage.color = c;
        }
    }
}
