using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Handles the Illumisnotti faction visuals: the HUD Badge in the top-left (membership card),
    /// and the Interference visual effect (red vignette flash + watermark) on negative random events.
    /// </summary>
    public sealed class IllumisnottiManagerUI : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private Sprite roundedRectSprite;
        [SerializeField] private Sprite vignetteSprite;
        [SerializeField] private TMP_FontAsset fontAsset;

        private GameObject badgeGo;
        private TextMeshProUGUI badgeText;
        
        private Image vignetteImg;
        private TextMeshProUGUI watermarkText;

        private Coroutine interferenceRoutine;

        private void Start()
        {
            CreateInterferenceUI();
            CreateHUDBadge();

            // Subscribe to Rebirth/Snotting events
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChanged;
                UpdateBadgeVisibility(RebirthManager.Instance.RebirthCount);
            }
            else
            {
                UpdateBadgeVisibility(0);
            }

            // Subscribe to Random Events
            if (RandomEventManager.Instance != null)
            {
                RandomEventManager.Instance.OnRandomEventTriggered += HandleRandomEventTriggered;
            }
        }

        private void OnDestroy()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
            }

            if (RandomEventManager.Instance != null)
            {
                RandomEventManager.Instance.OnRandomEventTriggered -= HandleRandomEventTriggered;
            }
        }

        private void CreateHUDBadge()
        {
            var safeArea = GameObject.Find("Canvas/CustomSafeArea");
            if (safeArea == null) return;

            // 1. Create Badge base
            badgeGo = new GameObject("Illumisnotti_HUD_Badge", typeof(RectTransform));
            badgeGo.transform.SetParent(safeArea.transform, false);

            var rt = badgeGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(340f, 100f);
            rt.anchoredPosition = new Vector2(30f, -220f); // Positioned nicely below the CurrencyHeader to prevent overlap

            // Add background Image
            var bgImg = badgeGo.AddComponent<Image>();
            bgImg.sprite = roundedRectSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.pixelsPerUnitMultiplier = 2f;
            bgImg.color = HexColor("#0A0714"); // Dark background

            // Add gold/yellow border via Outline component
            var outline = badgeGo.AddComponent<Outline>();
            outline.effectColor = HexColor("#FFCC00"); // Yellow/Gold
            outline.effectDistance = new Vector2(3f, -3f);

            // 2. Create small crown icon from UI primitives
            var crownGo = new GameObject("CrownIcon", typeof(RectTransform));
            crownGo.transform.SetParent(badgeGo.transform, false);
            var crownRT = crownGo.GetComponent<RectTransform>();
            crownRT.anchorMin = new Vector2(0f, 0.5f);
            crownRT.anchorMax = new Vector2(0f, 0.5f);
            crownRT.pivot = new Vector2(0f, 0.5f);
            crownRT.sizeDelta = new Vector2(40f, 40f);
            crownRT.anchoredPosition = new Vector2(15f, 0f);

            // Left spike (rotated square)
            var leftSpike = new GameObject("LeftSpike", typeof(RectTransform), typeof(Image));
            leftSpike.transform.SetParent(crownRT, false);
            var lsRT = leftSpike.GetComponent<RectTransform>();
            lsRT.sizeDelta = new Vector2(8f, 18f);
            lsRT.anchoredPosition = new Vector2(-10f, 4f);
            lsRT.localRotation = Quaternion.Euler(0f, 0f, 25f);
            leftSpike.GetComponent<Image>().color = HexColor("#FFCC00");

            // Right spike (rotated square)
            var rightSpike = new GameObject("RightSpike", typeof(RectTransform), typeof(Image));
            rightSpike.transform.SetParent(crownRT, false);
            var rsRT = rightSpike.GetComponent<RectTransform>();
            rsRT.sizeDelta = new Vector2(8f, 18f);
            rsRT.anchoredPosition = new Vector2(10f, 4f);
            rsRT.localRotation = Quaternion.Euler(0f, 0f, -25f);
            rightSpike.GetComponent<Image>().color = HexColor("#FFCC00");

            // Center spike (tall vertical)
            var centerSpike = new GameObject("CenterSpike", typeof(RectTransform), typeof(Image));
            centerSpike.transform.SetParent(crownRT, false);
            var csRT = centerSpike.GetComponent<RectTransform>();
            csRT.sizeDelta = new Vector2(10f, 24f);
            csRT.anchoredPosition = new Vector2(0f, 8f);
            centerSpike.GetComponent<Image>().color = HexColor("#FFCC00");

            // Base bar
            var baseBar = new GameObject("BaseBar", typeof(RectTransform), typeof(Image));
            baseBar.transform.SetParent(crownRT, false);
            var bbRT = baseBar.GetComponent<RectTransform>();
            bbRT.sizeDelta = new Vector2(28f, 6f);
            bbRT.anchoredPosition = new Vector2(0f, -10f);
            baseBar.GetComponent<Image>().color = HexColor("#FFCC00");

            // 3. Create TextMeshPro Rank text
            var textGo = new GameObject("RankText", typeof(RectTransform));
            textGo.transform.SetParent(badgeGo.transform, false);
            var txtRT = textGo.GetComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0f, 0f);
            txtRT.anchorMax = new Vector2(1f, 1f);
            txtRT.pivot = new Vector2(0.5f, 0.5f);
            txtRT.offsetMin = new Vector2(65f, 10f);
            txtRT.offsetMax = new Vector2(-15f, -10f);

            badgeText = textGo.AddComponent<TextMeshProUGUI>();
            badgeText.font = fontAsset;
            badgeText.fontSize = 20f;
            badgeText.color = HexColor("#FF1493"); // Hot pink
            badgeText.alignment = TextAlignmentOptions.Left;
            badgeText.enableAutoSizing = true;
            badgeText.fontSizeMin = 14f;
            badgeText.fontSizeMax = 22f;
            badgeText.raycastTarget = false;

            UpdateBadgeText(RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0);
            UpdateBadgeVisibility(RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0);
        }

        private void CreateInterferenceUI()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            // 1. Vignette (stretched across entire Canvas)
            var vigGo = new GameObject("Illumisnotti_Interference_Vignette", typeof(RectTransform));
            vigGo.transform.SetParent(canvas.transform, false);
            vigGo.transform.SetAsLastSibling(); // Ensure it sits on top of background/game stage

            var vigRT = vigGo.GetComponent<RectTransform>();
            vigRT.anchorMin = Vector2.zero;
            vigRT.anchorMax = Vector2.one;
            vigRT.offsetMin = Vector2.zero;
            vigRT.offsetMax = Vector2.zero;

            vignetteImg = vigGo.AddComponent<Image>();
            vignetteImg.sprite = vignetteSprite;
            vignetteImg.color = new Color(1f, 0f, 0f, 0f); // Transparent Red
            vignetteImg.raycastTarget = false;

            // 2. Watermark Text (Centered inside vignette)
            var textGo = new GameObject("Illumisnotti_Watermark_Text", typeof(RectTransform));
            textGo.transform.SetParent(vigGo.transform, false);

            var txtRT = textGo.GetComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0.5f, 0.5f);
            txtRT.anchorMax = new Vector2(0.5f, 0.5f);
            txtRT.pivot = new Vector2(0.5f, 0.5f);
            txtRT.sizeDelta = new Vector2(1000f, 200f);
            txtRT.anchoredPosition = new Vector2(0f, 150f); // Position in upper middle

            watermarkText = textGo.AddComponent<TextMeshProUGUI>();
            watermarkText.font = fontAsset;
            watermarkText.text = "ILLUMISNOTTI INTERFERENCE DETECTED";
            watermarkText.fontSize = 44f;
            watermarkText.color = new Color(1f, 1f, 1f, 0f); // Transparent white
            watermarkText.alignment = TextAlignmentOptions.Center;
            watermarkText.fontStyle = FontStyles.Bold;
            watermarkText.raycastTarget = false;

            // Add sharp outline/shadow to watermark text
            var shadow = textGo.AddComponent<Outline>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        private void HandleRebirthCountChanged(int rebirthCount)
        {
            UpdateBadgeVisibility(rebirthCount);
            UpdateBadgeText(rebirthCount);
        }

        private void UpdateBadgeVisibility(int rebirthCount)
        {
            if (badgeGo != null)
            {
                badgeGo.SetActive(rebirthCount >= 1);
            }
        }

        private void UpdateBadgeText(int rebirthCount)
        {
            if (badgeText != null)
            {
                badgeText.text = "RANK: " + GetTitle(rebirthCount);
            }
        }

        private string GetTitle(int rebirthCount)
        {
            if (rebirthCount >= 11) return "BUNKER SUPREME";
            if (rebirthCount >= 6) return "ILLUMINOSNOTTI INTERN";
            if (rebirthCount >= 4) return "BUNKER BUREAUCRAT";
            if (rebirthCount >= 2) return "UNDER-SNOT ELITE";
            return "SNOTTY ROOKIE";
        }

        private void HandleRandomEventTriggered(BrainRotEventData eventData)
        {
            if (eventData == null) return;

            // Check if it is a negative event (penalty to brain power or negative IQ impact)
            bool isNegative = eventData.brainPowerRewardOrPenalty < 0d || eventData.playerIQImpact < 0f;
            if (isNegative)
            {
                if (interferenceRoutine != null)
                {
                    StopCoroutine(interferenceRoutine);
                }
                interferenceRoutine = StartCoroutine(InterferenceRoutine());
            }
        }

        private IEnumerator InterferenceRoutine()
        {
            float elapsed = 0f;
            
            // Phase 1: Rapidly flash red vignette to 0.75 alpha and watermark to 1.0 alpha
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.15f;
                vignetteImg.color = new Color(1f, 0f, 0f, Mathf.Lerp(0f, 0.75f, t));
                watermarkText.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }

            // Phase 2: Hold red vignette full for 1.0 second
            elapsed = 0f;
            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out Vignette over 0.5 seconds
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.5f;
                vignetteImg.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.75f, 0f, t));
                yield return null;
            }
            vignetteImg.color = new Color(1f, 0f, 0f, 0f);

            // Phase 3: Watermark text holds longer and fades out over 2.0 seconds total
            elapsed = 0f;
            while (elapsed < 2.0f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 2.0f;
                watermarkText.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
            watermarkText.color = new Color(1f, 1f, 1f, 0f);
            interferenceRoutine = null;
        }

        private Color HexColor(string hex)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(hex, out color))
            {
                return color;
            }
            return Color.white;
        }
    }
}