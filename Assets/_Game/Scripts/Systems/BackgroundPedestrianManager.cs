using System.Collections;
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
    /// Spawns and moves 2D pedestrian sprites in the background behind the player character.
    /// Swaps the pedestrian sprite pool dynamically between Dystopian and Utopian sets
    /// based on the active WorldRestorationStage index (polled per-spawn, pre-existing).
    /// Also reflects WorldRestorationManager.RestorationPercent via PedestrianBehaviorStage
    /// (event-subscribed, added 2026-06-22): pedestrians shuffle slower with a slumped tilt at
    /// low restoration, walking upright and faster as restoration climbs toward 100%.
    /// Includes a custom South Park-style wobble walk for early stages and a smooth hover-float for Stage 6.
    /// </summary>
    public sealed class BackgroundPedestrianManager : MonoBehaviour
    {
        [Header("Sprite Pools")]
        [Tooltip("Sprites used during dystopian stages (stage index <= 1).")]
        [SerializeField] private Sprite[] dystopianPedestrianSprites;
        [Tooltip("Sprites used during utopian/restored stages (stage index >= 2).")]
        [SerializeField] private Sprite[] utopianPedestrianSprites;

        [Header("Pedestrian Prefabs")]
        [Tooltip("The 6 pedestrian prefabs in order.")]
        [SerializeField] private GameObject[] pedestrianPrefabs;

        [Header("Spawn Settings")]
        [SerializeField] private float minSpawnDelay = 4f;
        [SerializeField] private float maxSpawnDelay = 10f;

        [Header("Movement Settings")]
        [SerializeField] private float minSpeed = 80f;
        [SerializeField] private float maxSpeed = 160f;
        [SerializeField] private float yOffsetMin = -40f;
        [SerializeField] private float yOffsetMax = 10f;

        [Header("Dimensions")]
        [SerializeField] private float pedestrianWidth = 80f;
        [SerializeField] private float pedestrianHeight = 160f;

        [Header("Container")]
        [Tooltip("The RectTransform under Canvas where pedestrians are spawned. Must render behind the player character.")]
        [SerializeField] private RectTransform containerRect;

        [Header("Chatter Bubbles")]
        [SerializeField] private BrainDrain.UI.ChatterBubble chatterBubblePrefab;
        [SerializeField] private float minChatterInterval = 5f;
        [SerializeField] private float maxChatterInterval = 12f;

        private readonly System.Collections.Generic.List<RectTransform> activePedestrians = new System.Collections.Generic.List<RectTransform>();

        // -- RestorationPercent-driven behavior stage, added 2026-06-22 --
        // Speed multiplier per stage. No new art/sprites, per spec.
        private static readonly float[] StageSpeedMultiplier = { 0.5f, 0.7f, 1.0f, 1.25f, 1.5f };
        private const float StumbleChancePerStep = 0.01f;

        private Coroutine spawnLoop;
        private PedestrianBehaviorStage currentBehaviorStage = PedestrianBehaviorStage.Walking;

        private void Start()
        {
            if (containerRect == null)
            {
                // Self-fallback: look for a child or sibling named PedestrianContainer
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

            spawnLoop = StartCoroutine(SpawnLoopRoutine());
            
            // Add existing/pre-placed pedestrians in the container
            if (containerRect != null)
            {
                for (int i = 0; i < containerRect.childCount; i++)
                {
                    Transform child = containerRect.GetChild(i);
                    RectTransform rt = child as RectTransform;
                    if (rt != null && rt.gameObject.name.StartsWith("Ped"))
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

        /// <summary>
        /// WorldRestorationManager has no dedicated "OnRestorationChanged" event -- this reuses
        /// the existing OnRestorationProgressChanged (fires on every Points spend) as the
        /// "something changed, recheck RestorationPercent" signal rather than adding a
        /// functionally-duplicate event.
        /// </summary>
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

            // Determine active pool based on WorldRestorationStage index
            var restoration = WorldRestorationManager.Instance;
            bool isUtopian = restoration != null && restoration.CurrentStage != null && restoration.CurrentStage.stageIndex >= 2;
            Sprite[] activePool = isUtopian ? utopianPedestrianSprites : dystopianPedestrianSprites;

            if (activePool == null || activePool.Length == 0)
            {
                // Fallback to whichever pool is non-empty, or the person silhouette if both empty
                if (dystopianPedestrianSprites != null && dystopianPedestrianSprites.Length > 0)
                {
                    activePool = dystopianPedestrianSprites;
                }
                else if (utopianPedestrianSprites != null && utopianPedestrianSprites.Length > 0)
                {
                    activePool = utopianPedestrianSprites;
                }
                else
                {
                    // No sprites available
                    return;
                }
            }

            Sprite selectedSprite = activePool[Random.Range(0, activePool.Length)];

            // Create UI GameObject
            var pedGo = new GameObject("Pedestrian", typeof(RectTransform));
            pedGo.transform.SetParent(containerRect, false);

            var rt = pedGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(pedestrianWidth, pedestrianHeight);

            // Anchors and pivot at bottom-center so they slide cleanly on the floor
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            var img = pedGo.AddComponent<Image>();
            img.sprite = selectedSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // Random direction, speed, and vertical offset -- speed scaled by the current
            // RestorationPercent behavior stage (frozen for this pedestrian's whole lifetime,
            // matching the sprite-pool swap's existing "only affects new spawns" behavior).
            PedestrianBehaviorStage stage = currentBehaviorStage;
            bool walkRight = Random.value > 0.5f;
            float speed = Random.Range(minSpeed, maxSpeed) * StageSpeedMultiplier[(int)stage];
            float yOffset = Random.Range(yOffsetMin, yOffsetMax);

            float containerHalfWidth = containerRect.rect.width * 0.5f;
            float spawnX = walkRight ? -containerHalfWidth - (pedestrianWidth * 0.5f) : containerHalfWidth + (pedestrianWidth * 0.5f);
            float targetX = walkRight ? containerHalfWidth + (pedestrianWidth * 0.5f) : -containerHalfWidth - (pedestrianWidth * 0.5f);

            rt.anchoredPosition = new Vector2(spawnX, yOffset);

            // Flip graphic if walking left (assuming sprite faces right naturally)
            if (!walkRight)
            {
                rt.localScale = new Vector3(-1f, 1f, 1f);
            }

            // Determine matching Stage index directly from sprite naming convention
            int pedStage = 1;
            if (selectedSprite != null)
            {
                if (selectedSprite.name.Contains("Stage2")) pedStage = 2;
                else if (selectedSprite.name.Contains("Stage3")) pedStage = 3;
                else if (selectedSprite.name.Contains("Stage4")) pedStage = 4;
                else if (selectedSprite.name.Contains("Stage5")) pedStage = 5;
                else if (selectedSprite.name.Contains("Stage6")) pedStage = 6;
            }

            activePedestrians.Add(rt);
            StartCoroutine(MoveRoutine(rt, targetX, speed, stage, pedStage));
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
            if (chatterBubblePrefab == null || pedestrian == null || containerRect == null) return;

            // Spawn directly under the pedestrian container so that the bubble remains independent of the pedestrian's local scale and wobble
            BrainDrain.UI.ChatterBubble bubble = Instantiate(chatterBubblePrefab, containerRect);
            RectTransform bubbleRt = bubble.GetComponent<RectTransform>();
            if (bubbleRt != null)
            {
                bubbleRt.localScale = Vector3.one;
                bubble.TrackPedestrian(pedestrian, pedestrianHeight + 40f);
            }

            string line = RandomChatterManager.Instance != null ? RandomChatterManager.Instance.GetRandomLine() : "brains...";
            bubble.SetText(line);
        }

        private IEnumerator MoveRoutine(RectTransform rt, float targetX, float speed, PedestrianBehaviorStage stage, int pedStage)
        {
            float direction = Mathf.Sign(targetX - rt.anchoredPosition.x);
            float baseY = rt.anchoredPosition.y;
            float elapsed = Random.Range(0f, 10f); // Desync initial walk cycle phase

            while (rt != null && (direction > 0f ? rt.anchoredPosition.x < targetX : rt.anchoredPosition.x > targetX))
            {
                elapsed += Time.deltaTime;

                // "Occasional stumble" for the Shuffling stage only: a brief full stop most
                // steps don't trigger, no new art needed -- just a pause in the walk cycle.
                if (stage == PedestrianBehaviorStage.Shuffling && Random.value < StumbleChancePerStep)
                {
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }

                // Compute evolving wobble and bounce parameters
                float rotAngle = 0f;
                float verticalOffset = 0f;

                if (pedStage == 1)
                {
                    // Cheesy South Park Wobble Walk: Slow, extreme back-and-forth tilt and high Y-bounce
                    rotAngle = Mathf.Sin(elapsed * 8.5f) * 16f;
                    verticalOffset = Mathf.Abs(Mathf.Sin(elapsed * 8.5f)) * 14f;
                }
                else if (pedStage == 2)
                {
                    rotAngle = Mathf.Sin(elapsed * 10f) * 9f;
                    verticalOffset = Mathf.Abs(Mathf.Sin(elapsed * 10f)) * 8f;
                }
                else if (pedStage == 3)
                {
                    rotAngle = Mathf.Sin(elapsed * 11.5f) * 5.5f;
                    verticalOffset = Mathf.Abs(Mathf.Sin(elapsed * 11.5f)) * 5f;
                }
                else if (pedStage == 4)
                {
                    rotAngle = Mathf.Sin(elapsed * 13f) * 3f;
                    verticalOffset = Mathf.Abs(Mathf.Sin(elapsed * 13f)) * 2.5f;
                }
                else if (pedStage == 5)
                {
                    rotAngle = Mathf.Sin(elapsed * 14f) * 1f;
                    verticalOffset = Mathf.Abs(Mathf.Sin(elapsed * 14f)) * 1f;
                }
                else if (pedStage == 6)
                {
                    // WoW God Armor: Floating Hover. Zero rotational oscillation, smooth hovering float
                    rotAngle = 0f;
                    verticalOffset = Mathf.Sin(elapsed * 3.5f) * 8f;
                }

                // Preserve looking direction when applying rotational wobble
                float lookDirMultiplier = Mathf.Sign(rt.localScale.x);
                rt.localRotation = Quaternion.Euler(0f, 0f, rotAngle * lookDirMultiplier);

                // Advance X translation & apply Y offset
                float nextX = rt.anchoredPosition.x + direction * speed * Time.deltaTime;
                rt.anchoredPosition = new Vector2(nextX, baseY + verticalOffset);

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