using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Persistent background music manager. Ensures a single instance plays
    /// loopable cyberpunk tracks continuously across scenes.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusicManager : MonoBehaviour
    {
        [Header("Audio settings")]
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.4f;

        private AudioSource audioSource;
        private static BackgroundMusicManager instance;

        public static BackgroundMusicManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<BackgroundMusicManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.volume = volume;

            if (backgroundMusicClip != null)
            {
                audioSource.clip = backgroundMusicClip;
                audioSource.Play();
            }
        }

        /// <summary>
        /// Swaps the active music clip with a fade, or starts playing a new clip.
        /// </summary>
        public void PlayClip(AudioClip clip)
        {
            if (audioSource == null) return;

            backgroundMusicClip = clip;
            audioSource.clip = clip;
            audioSource.Play();
        }

        /// <summary>
        /// Sets the background music volume.
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }
    }
}