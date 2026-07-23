using System.Collections;
using BrainDrain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.Systems
{
    /// <summary>
    /// 5 behavior bands driven by WorldRestorationManager.RestorationPercent (0-100), added
    /// 2026-06-22 -- independent of the existing dystopian/utopian sprite-pool split below,
    /// which keys off WorldRestorationStage.stageIndex instead. Affects movement speed and a
    /// simple posture tilt on newly-spawned pedestrians only (no retroactive update of
    /// already-walking ones, matching how the sprite-pool swap already behaves).
    /// </summary>
    public enum PedestrianBehaviorStage
    {
        SlackJawed,
        Shuffling,
        Walking,
        Aware,
        Engaged
    }

    /// <summary>
    /// Spawns and moves 2D pedestrian sprites in the UI street band behind the player character.
    /// Stage 1 art only in the current view; South Park-style wobble with a Y ceiling clamp.
    /// </summary>
    public sealed class BackgroundPedestrianManager : MonoBehaviour
    {
        [Header("Sprite Pools")]
        [Tooltip("Sprites used during dystopian stages (stage index <= 1).")]
        [SerializeField] private Sprite[] dystopianPedestrianSprites;
        [Tooltip("Sprites used during utopian/restored stages (stage index >= 2).")]
        [SerializeField] private Sprite[] utopianPedestrianSprites;

        [Header("Pedestrian Prefabs")]
        [Tooltip("The 6 pedestrian prefabs in order (Ped1..Ped6). Stage 1 art is read from each prefab.")]
        [SerializeField] private GameObject[] pedestrianPrefabs;

        [Header("Spawn Settings")]
        [SerializeField] private float minSpawnDelay = 4f;
        [SerializeField] private float maxSpawnDelay = 10f;

        [Header("Movement Settings")]
        [SerializeField] private float minSpeed = 80f;
        [SerializeField] private float maxSpeed = 160f;
        [Tooltip("Fixed Y baseline within PedestrianContainer (street floor).")]
        [SerializeField] private float walkBaselineY = 0f;

        [Header("Wobble (Stage 1)")]
        [SerializeField] private float wobbleVerticalAmplitude = 14f;
        [SerializeField] private float wobbleRotationAmplitude = 16f;
        [SerializeField] private float wobbleFrequency = 8.5f;

        [Header("Dimensions")]
        [SerializeField] private float pedestrianWidth = 140f;
        [SerializeField] private float pedestrianHeight = 280f;

        [Header("Container")]
        [Tooltip("The RectTransform under Canvas where pedestrians are spawned. Must render behind the player character.")]
        [SerializeField] private RectTransform containerRect;

        [Header("Chatter Bubbles")]
        [SerializeField] private BrainDrain.UI.ChatterBubble chatterBubblePrefab;
        [SerializeField] private float minChatterInterval = 5f;
        [SerializeField] private float maxChatterInterval = 12f;

        private readonly System.Collections.Generic.List<RectTransform> activePedestrians = new System.Collections.Generic.List<RectTransform>();
        private static readonly System.Collections.Generic.List<Sprite> StageOneSpriteBuffer = new System.Collections.Generic.List<Sprite>(8);
        private static readonly System.Collections.Generic.HashSet<Sprite> OnScreenSpritesBuffer = new System.Collections.Generic.HashSet<Sprite>(8);
        private static readonly System.Collections.Generic.List<Sprite> FilteredSpriteBuffer = new System.Collections.Generic.List<Sprite>(8);

        private static readonly float[] StageSpeedMultiplier = { 0.5f, 0.7f, 1.0f, 1.25f, 1.5f };
        private const float StumbleChancePerStep = 0.01f;

        private Coroutine spawnLoop;
        private PedestrianBehaviorStage currentBehaviorStage = PedestrianBehaviorStage.Walking;

        private void Start()
        {
            if (containerRect == null)
            {
                var canvas = GameObject.Find("Canvas");
                if (canvas != null)
                {
                    var container = canvas.transform.Find("CustomSafeArea/PedestrianContainer");
                    if (container != null)
                    {
                        containerRect = container.GetComponent<RectTransform>();
                    }
                }
            }

            // Legacy world walkers physically removed §19 pass A (2b5919c); UI population below is the only pedestrian system.

            spawnLoop = StartCoroutine(SpawnLoopRoutine());

            if (containerRect != null)
            {
                for (int i = 0; i < containerRect.childCount; i++)
                {
                    Transform child = containerRect.GetChild(i);
                    if (child is RectTransform rt && child.name.StartsWith("Ped"))
                    {
                        activePedestrians.Add(rt);
                    }
                }
            }

            StartCoroutine(ChatterLoopRoutine());

            RefreshBehaviorStage();
            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationProgressChanged += HandleRestorationProgressChanged;
            }
        }

        private void OnDestroy()
        {
            if (spawnLoop != null)
            {
                StopCoroutine(spawnLoop);
                spawnLoop = null;
            }

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
            }
        }

        private void HandleRestorationProgressChanged(double _)
        {
            RefreshBehaviorStage();
        }

        private void RefreshBehaviorStage()
        {
            WorldRestorationManager restoration = WorldRestorationManager.Instance;
            double percent = restoration != null ? restoration.RestorationPercent : 0d;

            if (percent <= 20d) currentBehaviorStage = PedestrianBehaviorStage.SlackJawed;
            else if (percent <= 40d) currentBehaviorStage = PedestrianBehaviorStage.Shuffling;
            else if (percent <= 60d) currentBehaviorStage = PedestrianBehaviorStage.Walking;
            else if (percent <= 80d) currentBehaviorStage = PedestrianBehaviorStage.Aware;
            else currentBehaviorStage = PedestrianBehaviorStage.Engaged;
        }

        private IEnumerator SpawnLoopRoutine()
        {
            while (true)
            {
                float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
                yield return new WaitForSeconds(delay);

                SpawnPedestrian();
            }
        }

        private void SpawnPedestrian()
        {
            if (containerRect == null)
            {
                return;
            }

            Sprite selectedSprite = PickStageOneSprite();
            if (selectedSprite == null)
            {
                Debug.LogWarning("[BackgroundPedestrianManager] No Stage 1 pedestrian sprites available — skipping spawn.", this);
                return;
            }

            var pedGo = new GameObject("Pedestrian", typeof(RectTransform));
            pedGo.transform.SetParent(containerRect, false);

            var rt = pedGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(pedestrianWidth, pedestrianHeight);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            var img = pedGo.AddComponent<Image>();
            img.sprite = selectedSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            PedestrianBehaviorStage stage = currentBehaviorStage;
            bool walkRight = Random.value > 0.5f;
            float speed = Random.Range(minSpeed, maxSpeed) * StageSpeedMultiplier[(int)stage];

            float containerHalfWidth = containerRect.rect.width * 0.5f;
            float spawnX = walkRight ? -containerHalfWidth - (pedestrianWidth * 0.5f) : containerHalfWidth + (pedestrianWidth * 0.5f);
            float targetX = walkRight ? containerHalfWidth + (pedestrianWidth * 0.5f) : -containerHalfWidth - (pedestrianWidth * 0.5f);

            rt.anchoredPosition = new Vector2(spawnX, walkBaselineY);
            rt.localScale = walkRight ? Vector3.one : new Vector3(-1f, 1f, 1f);

            float maxAnchoredY = ComputeMaxAnchoredY(rt);

            activePedestrians.Add(rt);
            StartCoroutine(MoveRoutine(rt, targetX, speed, stage, maxAnchoredY));
        }

        private float ComputeMaxAnchoredY(RectTransform rt)
        {
            float containerHeight = containerRect.rect.height;
            float pedHeight = rt.rect.height > 0f ? rt.rect.height : pedestrianHeight;
            return Mathf.Max(walkBaselineY, containerHeight - pedHeight);
        }

        /// <summary>
        /// Picks Ped1..Ped6 Stage 1 art only; blocks Stage 2–6 sprites. Prefers a sprite that
        /// isn't already walking on screen, so identical pedestrians don't spawn together by
        /// chance; falls back to the full candidate pool once population exceeds sprite
        /// variety (never returns null just because every candidate is already on screen).
        /// </summary>
        private Sprite PickStageOneSprite()
        {
            StageOneSpriteBuffer.Clear();

            if (pedestrianPrefabs != null)
            {
                for (int i = 0; i < pedestrianPrefabs.Length; i++)
                {
                    GameObject prefab = pedestrianPrefabs[i];
                    if (prefab == null)
                    {
                        continue;
                    }

                    SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null && IsStageOneSprite(sr.sprite))
                    {
                        StageOneSpriteBuffer.Add(sr.sprite);
                    }
                }
            }

            if (StageOneSpriteBuffer.Count == 0 && dystopianPedestrianSprites != null)
            {
                for (int i = 0; i < dystopianPedestrianSprites.Length; i++)
                {
                    Sprite sprite = dystopianPedestrianSprites[i];
                    if (IsStageOneSprite(sprite))
                    {
                        StageOneSpriteBuffer.Add(sprite);
                    }
                }
            }

            if (StageOneSpriteBuffer.Count == 0)
            {
                return null;
            }

            OnScreenSpritesBuffer.Clear();
            for (int i = 0; i < activePedestrians.Count; i++)
            {
                RectTransform ped = activePedestrians[i];
                if (ped == null)
                {
                    continue;
                }

                Sprite onScreenSprite = ped.GetComponent<Image>()?.sprite;
                if (onScreenSprite != null)
                {
                    OnScreenSpritesBuffer.Add(onScreenSprite);
                }
            }

            FilteredSpriteBuffer.Clear();
            for (int i = 0; i < StageOneSpriteBuffer.Count; i++)
            {
                if (!OnScreenSpritesBuffer.Contains(StageOneSpriteBuffer[i]))
                {
                    FilteredSpriteBuffer.Add(StageOneSpriteBuffer[i]);
                }
            }

            System.Collections.Generic.List<Sprite> candidates =
                FilteredSpriteBuffer.Count > 0 ? FilteredSpriteBuffer : StageOneSpriteBuffer;
            return candidates[Random.Range(0, candidates.Count)];
        }

        private static bool IsStageOneSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            string name = sprite.name;
            if (name.Contains("Stage2") || name.Contains("Stage3") || name.Contains("Stage4")
                || name.Contains("Stage5") || name.Contains("Stage6"))
            {
                return false;
            }

            return name.Contains("Stage1") || !name.Contains("Stage");
        }

        private IEnumerator ChatterLoopRoutine()
        {
            while (true)
            {
                float delay = Random.Range(minChatterInterval, maxChatterInterval);
                yield return new WaitForSeconds(delay);

                activePedestrians.RemoveAll(p => p == null);

                if (activePedestrians.Count > 0 && chatterBubblePrefab != null)
                {
                    RectTransform chosenPed = activePedestrians[Random.Range(0, activePedestrians.Count)];
                    if (chosenPed != null)
                    {
                        SpawnChatterBubble(chosenPed);
                    }
                }
            }
        }

        private void SpawnChatterBubble(RectTransform pedestrian)
        {
            if (chatterBubblePrefab == null || pedestrian == null || containerRect == null)
            {
                return;
            }

            BrainDrain.UI.ChatterBubble bubble = Instantiate(chatterBubblePrefab, containerRect);
            RectTransform bubbleRt = bubble.GetComponent<RectTransform>();
            if (bubbleRt != null)
            {
                bubbleRt.localScale = Vector3.one;
                bubble.TrackPedestrian(pedestrian, pedestrianHeight * 0.55f);
            }

            string line = "brains...";
            if (RandomChatterManager.Instance != null)
            {
                int rankIndex = GameManager.Instance != null ? GameManager.Instance.CurrentRankIndex : 0;
                line = RandomChatterManager.Instance.GetLineForRank(rankIndex);
            }

            bubble.SetText(line);

            // Recorded here, at the point of actual speech -- not inside GetLineForRank, which
            // could be called speculatively without a bubble ever displaying the result (§24b).
            RandomChatterManager.Instance?.RecordSpokenLine(line);
        }

        private IEnumerator MoveRoutine(RectTransform rt, float targetX, float speed, PedestrianBehaviorStage stage, float maxAnchoredY)
        {
            float direction = Mathf.Sign(targetX - rt.anchoredPosition.x);
            float lookDirMultiplier = Mathf.Sign(rt.localScale.x);
            if (lookDirMultiplier == 0f)
            {
                lookDirMultiplier = 1f;
            }

            float elapsed = Random.Range(0f, 10f);

            while (rt != null && (direction > 0f ? rt.anchoredPosition.x < targetX : rt.anchoredPosition.x > targetX))
            {
                elapsed += Time.deltaTime;

                if (stage == PedestrianBehaviorStage.Shuffling && Random.value < StumbleChancePerStep)
                {
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }

                float rotAngle = Mathf.Sin(elapsed * wobbleFrequency) * wobbleRotationAmplitude;
                float verticalOffset = Mathf.Abs(Mathf.Sin(elapsed * wobbleFrequency)) * wobbleVerticalAmplitude;

                float nextX = rt.anchoredPosition.x + direction * speed * Time.deltaTime;
                float desiredY = walkBaselineY + verticalOffset;
                float clampedY = Mathf.Clamp(desiredY, walkBaselineY, maxAnchoredY);

                rt.anchoredPosition = new Vector2(nextX, clampedY);
                rt.localRotation = Quaternion.Euler(0f, 0f, rotAngle * lookDirMultiplier);
                rt.localScale = new Vector3(lookDirMultiplier, 1f, 1f);

                yield return null;
            }

            if (rt != null)
            {
                activePedestrians.Remove(rt);
                Destroy(rt.gameObject);
            }
        }
    }
}
