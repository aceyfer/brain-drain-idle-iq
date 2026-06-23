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
    /// </summary>
    public sealed class BackgroundPedestrianManager : MonoBehaviour
    {
        [Header("Sprite Pools")]
        [Tooltip("Sprites used during dystopian stages (stage index <= 1).")]
        [SerializeField] private Sprite[] dystopianPedestrianSprites;
        [Tooltip("Sprites used during utopian/restored stages (stage index >= 2).")]
        [SerializeField] private Sprite[] utopianPedestrianSprites;

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

        // -- RestorationPercent-driven behavior stage, added 2026-06-22 --
        // Speed multiplier and a slumped-posture tilt (degrees) per stage. No new art/sprites,
        // per spec -- these are the only pedestrian-state cues achievable with the existing
        // primitive Image + RectTransform setup.
        private static readonly float[] StageSpeedMultiplier = { 0.5f, 0.7f, 1.0f, 1.25f, 1.5f };
        private static readonly float[] StageTiltDegrees = { -15f, -8f, 0f, 0f, 0f };
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
            rt.localRotation = Quaternion.Euler(0f, 0f, StageTiltDegrees[(int)stage]);

            // Flip graphic if walking left (assuming sprite faces right naturally)
            if (!walkRight)
            {
                rt.localScale = new Vector3(-1f, 1f, 1f);
            }

            StartCoroutine(MoveRoutine(rt, targetX, speed, stage));
        }

        private IEnumerator MoveRoutine(RectTransform rt, float targetX, float speed, PedestrianBehaviorStage stage)
        {
            float direction = Mathf.Sign(targetX - rt.anchoredPosition.x);

            while (rt != null && (direction > 0f ? rt.anchoredPosition.x < targetX : rt.anchoredPosition.x > targetX))
            {
                // "Occasional stumble" for the Shuffling stage only: a brief full stop most
                // steps don't trigger, no new art needed -- just a pause in the walk cycle.
                if (stage == PedestrianBehaviorStage.Shuffling && Random.value < StumbleChancePerStep)
                {
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }

                rt.anchoredPosition += new Vector2(direction * speed * Time.deltaTime, 0f);
                yield return null;
            }

            if (rt != null)
            {
                Destroy(rt.gameObject);
            }
        }
    }
}