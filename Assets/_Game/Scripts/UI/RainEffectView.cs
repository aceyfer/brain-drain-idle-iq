using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.UI
{
    /// <summary>
    /// Pooled UI raindrop streaks under a full-screen RectTransform. Purely cosmetic ambient
    /// VFX -- StartRain/StopRain/SetStageIndex are the only public surface, driven by
    /// WeatherManager's random interval/duration loop and WorldRestorationManager's stage
    /// events respectively.
    ///
    /// Built as lightweight UI Images rather than a real ParticleSystem despite two rain VFX
    /// packages already being installed (Assets/Rain Particles, Assets/Rainy VFX): this
    /// project's whole visible game is one Screen Space - Overlay Canvas with no compositing
    /// camera, and a Screen Space - Overlay Canvas always draws after and on top of every
    /// camera-rendered object regardless of camera depth. A raw ParticleSystem from either
    /// package (both are ordinary camera-rendered, world-space Shuriken systems) would render
    /// behind the Canvas and never be visible on screen -- an architecture mismatch, not a
    /// configuration issue either package could be tuned out of. Between the two, Rain
    /// Particles was the better source of real usable art (its own dedicated Rain Sprite / Rain
    /// Ground Hit textures and materials, vs Rainy VFX's base prefab relying on Unity's generic
    /// built-in particle material), so its streak texture is reused here as a UI sprite
    /// (RainStreak.png, copied in by WeatherSystemWireFix) instead of either prefab.
    /// </summary>
    public sealed class RainEffectView : MonoBehaviour
    {
        [SerializeField] private RectTransform dropContainer;
        [SerializeField] private Image[] drops = System.Array.Empty<Image>();

        [Header("Motion")]
        [SerializeField] private float minFallSpeed = 900f;
        [SerializeField] private float maxFallSpeed = 1400f;
        [SerializeField] private float driftXPerSecond = -80f;

        // Stage 0..5, murky/acid-tinted early -> clean and clear late. Same arc as
        // WeatherAtmosphereView's haze table, kept separate rather than shared -- independent
        // trackers using the same interval "by convention, not shared implementation," matching
        // this project's existing pattern (e.g. the IQ-milestone celebration).
        private static readonly Color[] StageTint =
        {
            new Color(0.55f, 0.52f, 0.30f), // 0 -- murky/acid
            new Color(0.58f, 0.56f, 0.38f), // 1
            new Color(0.65f, 0.68f, 0.62f), // 2
            new Color(0.72f, 0.82f, 0.85f), // 3
            new Color(0.85f, 0.92f, 0.96f), // 4
            new Color(0.95f, 0.98f, 1f),    // 5 -- clean
        };

        private float[] fallSpeeds;
        private bool isRaining;

        private void Awake()
        {
            fallSpeeds = new float[drops.Length];
            for (int i = 0; i < drops.Length; i++)
            {
                fallSpeeds[i] = Random.Range(minFallSpeed, maxFallSpeed);
            }

            gameObject.SetActive(false);
        }

        public void SetStageIndex(int index)
        {
            index = Mathf.Clamp(index, 0, StageTint.Length - 1);
            Color tint = StageTint[index];
            foreach (Image drop in drops)
            {
                if (drop != null)
                {
                    drop.color = tint;
                }
            }
        }

        public void StartRain()
        {
            if (drops.Length == 0 || dropContainer == null)
            {
                return;
            }

            isRaining = true;
            RandomizeDropPositions();
            gameObject.SetActive(true);
        }

        public void StopRain()
        {
            isRaining = false;
            gameObject.SetActive(false);
        }

        private void RandomizeDropPositions()
        {
            Rect bounds = dropContainer.rect;
            foreach (Image drop in drops)
            {
                if (drop == null)
                {
                    continue;
                }

                RectTransform dropRect = (RectTransform)drop.transform;
                dropRect.anchoredPosition = new Vector2(
                    Random.Range(bounds.xMin, bounds.xMax),
                    Random.Range(bounds.yMin, bounds.yMax));
            }
        }

        // Only runs while this GameObject is active, i.e. only during a bounded 20-60s rain
        // burst -- Unity skips Update entirely on inactive GameObjects, so this costs nothing
        // while StopRain() has this deactivated between bursts.
        private void Update()
        {
            if (!isRaining || dropContainer == null)
            {
                return;
            }

            Rect bounds = dropContainer.rect;
            float dt = Time.deltaTime;

            for (int i = 0; i < drops.Length; i++)
            {
                Image drop = drops[i];
                if (drop == null)
                {
                    continue;
                }

                RectTransform dropRect = (RectTransform)drop.transform;
                Vector2 pos = dropRect.anchoredPosition;
                pos.y -= fallSpeeds[i] * dt;
                pos.x += driftXPerSecond * dt;

                if (pos.y < bounds.yMin)
                {
                    pos.y = bounds.yMax;
                    pos.x = Random.Range(bounds.xMin, bounds.xMax);
                }
                else if (pos.x < bounds.xMin)
                {
                    pos.x = bounds.xMax;
                }
                else if (pos.x > bounds.xMax)
                {
                    pos.x = bounds.xMin;
                }

                dropRect.anchoredPosition = pos;
            }
        }
    }
}
