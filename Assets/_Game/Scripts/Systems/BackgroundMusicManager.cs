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

        [Header("Settings menu")]
        [Tooltip("The 4 CyberWare songs the player can pick from in the Settings menu (loop-safe \"Full\" clips, not the \"with Tail\" versions).")]
        [SerializeField] private AudioClip[] availableTracks = new AudioClip[4];

        private AudioSource audioSource;
        private float preMuteVolume;
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

        /// <summary>Index into availableTracks of whichever song is currently playing.</summary>
        public int CurrentTrackIndex { get; private set; }

        public bool IsMuted { get; private set; }

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

        /// <summary>
        /// Plays the song at the given index in availableTracks via the existing PlayClip and
        /// records it as the current selection. No-ops on an out-of-range index or a missing clip.
        /// </summary>
        public void SelectTrack(int index)
        {
            if (availableTracks == null || index < 0 || index >= availableTracks.Length || availableTracks[index] == null)
            {
                return;
            }

            PlayClip(availableTracks[index]);
            CurrentTrackIndex = index;
        }

        /// <summary>
        /// Mutes via the existing SetVolume(0), remembering the pre-mute volume so unmuting
        /// restores it rather than a hardcoded value. No-ops if already in the requested state,
        /// so muting twice in a row can't clobber the remembered volume with 0.
        /// </summary>
        public void SetMuted(bool muted)
        {
            if (muted == IsMuted)
            {
                return;
            }

            if (muted)
            {
                preMuteVolume = volume;
                SetVolume(0f);
            }
            else
            {
                SetVolume(preMuteVolume);
            }

            IsMuted = muted;
        }
    }
}