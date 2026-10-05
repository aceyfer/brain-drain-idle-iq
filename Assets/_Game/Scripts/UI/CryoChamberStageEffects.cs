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
    /// Three jobs: (1) override BackgroundStageView.stageSprites[0] with the generated room art,
    /// (2) layer a second Image using the isolated pod-glow sprite on top, pulsing its alpha on a
    /// slow 4s sine so the pod lights read as alive, (3) place two small "DECEASED" TMP labels
    /// over the two tagged pods -- the generator bakes a plain Dim rectangle there, not text,
    /// matching this project's convention that text always comes from a TMP component. All three
    /// are only ever visible while World Restoration is actually at stage 0 (gated the same way
    /// BackgroundStageView itself reacts to OnRestorationStageChanged), since the glow/labels
    /// would otherwise float over whatever backdrop a later stage swaps in.
    /// </summary>
    public sealed class CryoChamberStageEffects : MonoBehaviour
    {
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

            Sprite roomSprite = Resources.Load<Sprite>("UI/Generated/Stage0_CryoChamber");
            if (roomSprite != null) { backgroundView.OverrideStageSprite(0, roomSprite); }

            BuildOverlay(backgroundImage.transform);

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

        private void RefreshVisibility(int stageIndex)
        {
            if (overlayRoot != null) { overlayRoot.SetActive(stageIndex == 0); }
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
                BuildDeceasedLabel(overlayRoot.transform, DeceasedTagAnchors[i]);
            }
        }

        private void BuildDeceasedLabel(Transform parent, Vector2 normalizedAnchor)
        {
            GameObject labelObject = new GameObject("DeceasedTag", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = normalizedAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(80f, 24f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "DECEASED";
            label.color = Palette.Base;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 14f;
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
