using UnityEngine;

namespace WAD64.Managers
{
    /// <summary>
    /// Управляет фоновой музыкой в игре.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Music Settings")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip[] musicTracks;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loopMusic = true;

        private float musicVolume = 1f;
        private int currentTrackIndex = 0;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Создаем AudioSource если его нет
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = loopMusic;
                musicSource.spatialBlend = 0f; // 2D звук
            }
        }

        private void Start()
        {
            // Загружаем сохраненную громкость музыки
            LoadMusicVolume();

            // Запускаем музыку если нужно
            if (playOnStart && musicTracks != null && musicTracks.Length > 0)
            {
                PlayMusic(0);
            }
        }

        /// <summary>
        /// Устанавливает громкость музыки.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        /// <summary>
        /// Получает текущую громкость музыки.
        /// </summary>
        public float GetMusicVolume()
        {
            return musicVolume;
        }

        /// <summary>
        /// Воспроизводит музыку по индексу.
        /// </summary>
        public void PlayMusic(int trackIndex)
        {
            if (musicTracks == null || trackIndex < 0 || trackIndex >= musicTracks.Length)
            {
                Debug.LogWarning($"MusicManager: трек с индексом {trackIndex} не найден.");
                return;
            }

            if (musicSource == null)
            {
                Debug.LogWarning("MusicManager: AudioSource не назначен.");
                return;
            }

            currentTrackIndex = trackIndex;
            musicSource.clip = musicTracks[trackIndex];
            musicSource.volume = musicVolume;
            musicSource.loop = loopMusic;
            musicSource.Play();
        }

        /// <summary>
        /// Воспроизводит следующий трек.
        /// </summary>
        public void PlayNextTrack()
        {
            if (musicTracks == null || musicTracks.Length == 0) return;

            int nextIndex = (currentTrackIndex + 1) % musicTracks.Length;
            PlayMusic(nextIndex);
        }

        /// <summary>
        /// Воспроизводит предыдущий трек.
        /// </summary>
        public void PlayPreviousTrack()
        {
            if (musicTracks == null || musicTracks.Length == 0) return;

            int prevIndex = currentTrackIndex - 1;
            if (prevIndex < 0) prevIndex = musicTracks.Length - 1;
            PlayMusic(prevIndex);
        }

        /// <summary>
        /// Останавливает музыку.
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Приостанавливает музыку.
        /// </summary>
        public void PauseMusic()
        {
            if (musicSource != null)
            {
                musicSource.Pause();
            }
        }

        /// <summary>
        /// Возобновляет музыку.
        /// </summary>
        public void ResumeMusic()
        {
            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.UnPause();
            }
        }

        /// <summary>
        /// Загружает громкость музыки из PlayerPrefs.
        /// </summary>
        private void LoadMusicVolume()
        {
            const string MusicVolumePrefKey = "MusicVolume";
            float storedVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, 1f);
            SetMusicVolume(storedVolume);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

