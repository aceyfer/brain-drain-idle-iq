using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Drives which rank diorama is visible by cross-fading SpriteRenderer alpha.
    /// All dioramas stay active; only their alpha changes (active rank -> 1, others -> 0).
    /// </summary>
    public sealed class DioramaManager : MonoBehaviour
    {
        [Header("Diorama References")]
        [SerializeField] private GameObject[] dioramaObjects;

        [Header("Fade")]
        [Tooltip("Alpha units per second for the cross-fade. Higher = snappier.")]
        [SerializeField] private float fadeSpeed = 3f;

        public GameObject[] DioramaObjects
        {
            get => dioramaObjects;
            set
            {
                dioramaObjects = value;
                CacheSpriteRenderers();
            }
        }

        private SpriteRenderer[] spriteRenderers;
        private int activeIndex;

        private void Awake()
        {
            CacheSpriteRenderers();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += InitializeDioramas;
            }

            InitializeDioramas();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= InitializeDioramas;
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

        private void CacheSpriteRenderers()
        {
            if (dioramaObjects == null)
            {
                spriteRenderers = null;
                return;
            }

            spriteRenderers = new SpriteRenderer[dioramaObjects.Length];
            for (int i = 0; i < dioramaObjects.Length; i++)
            {
                if (dioramaObjects[i] != null)
                {
                    // Keep all objects active so alpha alone controls visibility.
                    dioramaObjects[i].SetActive(true);
                    spriteRenderers[i] = dioramaObjects[i].GetComponent<SpriteRenderer>();
                }
            }
        }

        private void InitializeDioramas()
        {
            UnsubscribeFromEvents();

            var currency = CurrencyManager.Instance;
            double cumulative = currency != null ? currency.CumulativeBrainPower : 0d;
            activeIndex = ResolveActiveIndex(cumulative);

            // Snap alpha immediately on init (no fade from an undefined start state).
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        Color c = spriteRenderers[i].color;
                        c.a = i == activeIndex ? 1f : 0f;
                        spriteRenderers[i].color = c;
                    }
                }
            }

            if (currency != null)
            {
                currency.OnCumulativeBrainPowerChanged += UpdateActiveDiorama;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                currency.OnCumulativeBrainPowerChanged -= UpdateActiveDiorama;
            }
        }

        private void UpdateActiveDiorama(double cumulativeBrainPower)
        {
            activeIndex = ResolveActiveIndex(cumulativeBrainPower);
        }

        private int ResolveActiveIndex(double cumulativeBrainPower)
        {
            if (dioramaObjects == null || dioramaObjects.Length == 0)
            {
                return 0;
            }

            int index = 0;
            if (GameManager.Instance != null)
            {
                var ranks = GameManager.Instance.RankDefinitions;
                if (ranks != null && ranks.Length > 0)
                {
                    for (int i = 0; i < ranks.Length; i++)
                    {
                        if (cumulativeBrainPower >= ranks[i].threshold)
                        {
                            index = i;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return Mathf.Clamp(index, 0, dioramaObjects.Length - 1);
        }
    }
}
