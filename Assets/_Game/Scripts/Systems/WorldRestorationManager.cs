using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Tracks cumulative Points spent on World Restoration and cross-fades which of N sibling
    /// backdrop GameObjects is visible, mirroring DioramaManager's rank-backdrop pattern but
    /// keyed on restoration progress instead of cumulative Brain Power. A separate, additive
    /// progression layer on top of PlayerIQ -- not a replacement for it.
    /// </summary>
    public sealed class WorldRestorationManager : MonoBehaviour
    {
        [Header("Stages")]
        [SerializeField] private List<WorldRestorationStage> stages = new();

        [Header("Backdrop Visuals")]
        [SerializeField] private GameObject[] restorationStageObjects;
        [Tooltip("Alpha units per second for the cross-fade between stages. Higher = snappier.")]
        [SerializeField] private float fadeSpeed = 1f;

        private static WorldRestorationManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static WorldRestorationManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<WorldRestorationManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("WorldRestorationManager (Auto)");
                    instance = hostObject.AddComponent<WorldRestorationManager>();
                }

                return instance;
            }
        }

        /// <summary>Lifetime Points spent on restoration. Only ever increases.</summary>
        public double CumulativePointsSpentOnRestoration { get; private set; }

        /// <summary>The currently resolved stage, or null before the first resolution has run / no stages configured.</summary>
        public WorldRestorationStage CurrentStage { get; private set; }

        /// <summary>
        /// 0-100 progress toward the final configured stage's pointsRequired. 0 if no stages
        /// are configured yet. Backs ChapterUnlockConditionType.WorldRestorationPercent.
        /// </summary>
        public double RestorationPercent
        {
            get
            {
                if (stages.Count == 0)
                {
                    return 0d;
                }

                double finalThreshold = stages[stages.Count - 1].pointsRequired;
                if (finalThreshold <= 0d)
                {
                    return 0d;
                }

                return Math.Min(100d, (CumulativePointsSpentOnRestoration / finalThreshold) * 100d);
            }
        }

        /// <summary>Fired whenever restoration progress increases. Passes the new cumulative Points spent.</summary>
        public event Action<double> OnRestorationProgressChanged;

        /// <summary>Fired whenever the resolved stage actually changes. Passes the new stage.</summary>
        public event Action<WorldRestorationStage> OnRestorationStageChanged;

        private SpriteRenderer[] spriteRenderers;
        private int activeIndex;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[WorldRestorationManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SortStages();
            CacheSpriteRenderers();
        }

        private void Start()
        {
            ApplyStageForCumulativePoints(snapImmediately: true);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (spriteRenderers == null)
            {
                return;
            }

            float step = fadeSpeed * Time.deltaTime;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer sr = spriteRenderers[i];
                if (sr == null)
                {
                    continue;
                }

                float target = i == activeIndex ? 1f : 0f;
                Color c = sr.color;
                if (!Mathf.Approximately(c.a, target))
                {
                    c.a = Mathf.MoveTowards(c.a, target, step);
                    sr.color = c;
                }
            }
        }

        /// <summary>
        /// Attempts to spend Points on restoration. Returns true when the spend succeeds (i.e.
        /// the player has enough Points). On success, permanently increases restoration
        /// progress and re-resolves the active stage/backdrop.
        /// </summary>
        public bool TrySpendPointsOnRestoration(double amount)
        {
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendPoints(amount))
            {
                return false;
            }

            CumulativePointsSpentOnRestoration += amount;
            OnRestorationProgressChanged?.Invoke(CumulativePointsSpentOnRestoration);
            ApplyStageForCumulativePoints(snapImmediately: false);

            return true;
        }

        /// <summary>Directly restores progress from a save file.</summary>
        public void LoadState(double restoredCumulativePointsSpent)
        {
            CumulativePointsSpentOnRestoration = restoredCumulativePointsSpent;
            OnRestorationProgressChanged?.Invoke(CumulativePointsSpentOnRestoration);
            ApplyStageForCumulativePoints(snapImmediately: true);
        }

        private void ApplyStageForCumulativePoints(bool snapImmediately)
        {
            WorldRestorationStage resolved = ResolveStage(CumulativePointsSpentOnRestoration);
            activeIndex = ResolveActiveIndex(resolved);

            if (snapImmediately && spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] == null)
                    {
                        continue;
                    }

                    Color c = spriteRenderers[i].color;
                    c.a = i == activeIndex ? 1f : 0f;
                    spriteRenderers[i].color = c;
                }
            }

            if (resolved == null || resolved == CurrentStage)
            {
                return;
            }

            CurrentStage = resolved;
            OnRestorationStageChanged?.Invoke(CurrentStage);
        }

        private WorldRestorationStage ResolveStage(double cumulativePointsSpent)
        {
            WorldRestorationStage resolved = null;

            for (int i = 0; i < stages.Count; i++)
            {
                WorldRestorationStage stage = stages[i];
                if (stage == null)
                {
                    continue;
                }

                if (cumulativePointsSpent >= stage.pointsRequired)
                {
                    resolved = stage;
                }
                else
                {
                    break;
                }
            }

            return resolved;
        }

        private int ResolveActiveIndex(WorldRestorationStage resolved)
        {
            if (restorationStageObjects == null || restorationStageObjects.Length == 0)
            {
                return 0;
            }

            int index = resolved != null ? resolved.stageIndex : 0;
            return Mathf.Clamp(index, 0, restorationStageObjects.Length - 1);
        }

        private void CacheSpriteRenderers()
        {
            if (restorationStageObjects == null)
            {
                spriteRenderers = null;
                return;
            }

            spriteRenderers = new SpriteRenderer[restorationStageObjects.Length];
            for (int i = 0; i < restorationStageObjects.Length; i++)
            {
                if (restorationStageObjects[i] != null)
                {
                    // Keep all objects active so alpha alone controls visibility.
                    restorationStageObjects[i].SetActive(true);
                    spriteRenderers[i] = restorationStageObjects[i].GetComponent<SpriteRenderer>();
                }
            }
        }

        private void SortStages()
        {
            stages.RemoveAll(stage => stage == null);
            stages.Sort((a, b) => a.pointsRequired.CompareTo(b.pointsRequired));
        }
    }
}
