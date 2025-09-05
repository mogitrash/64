using UnityEngine;
using System.Collections.Generic;
using WAD64.Core;

namespace WAD64.Managers
{
    /// <summary>
    /// Менеджер аудио. Управляет воспроизведением звуковых эффектов и музыки.
    /// Поддерживает объектный пулинг для оптимизации производительности.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private int poolSize = 10;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClipCollection weaponSounds;
        [SerializeField] private AudioClipCollection enemySounds;
        [SerializeField] private AudioClipCollection pickupSounds;
        [SerializeField] private AudioClipCollection environmentSounds;

        // Audio Source Pool
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();

        // Properties
        public float MasterVolume 
        { 
            get => masterVolume; 
            set 
            { 
                masterVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            } 
        }

        public float MusicVolume 
        { 
            get => musicVolume; 
            set 
            { 
                musicVolume = Mathf.Clamp01(value);
                UpdateMusicVolume();
            } 
        }

        public float SfxVolume 
        { 
            get => sfxVolume; 
            set 
            { 
                sfxVolume = Mathf.Clamp01(value);
                UpdateSfxVolume();
            } 
        }

        private void Awake()
        {
            // Убеждаемся, что AudioManager единственный
            if (CoreReferences.AudioManager != null && CoreReferences.AudioManager != this)
            {
                Debug.LogWarning("[AudioManager] Another AudioManager already exists! Destroying this one.");
                Destroy(gameObject);
                return;
            }

            InitializeAudioSources();
            CreateAudioSourcePool();
        }

        private void Start()
        {
            // Запускаем фоновую музыку
            if (backgroundMusic != null)
            {
                PlayMusic(backgroundMusic, true);
            }
        }

        #region Initialization

        private void InitializeAudioSources()
        {
            // Создаем основные AudioSource, если их нет
            if (musicSource == null)
            {
                GameObject musicGO = new GameObject("MusicSource");
                musicGO.transform.SetParent(transform);
                musicSource = musicGO.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxGO = new GameObject("SFXSource");
                sfxGO.transform.SetParent(transform);
                sfxSource = sfxGO.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            UpdateAllVolumes();
        }

        private void CreateAudioSourcePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject audioGO = new GameObject($"PooledAudioSource_{i}");
                audioGO.transform.SetParent(transform);
                AudioSource audioSource = audioGO.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioGO.SetActive(false);
                
                audioSourcePool.Enqueue(audioSource);
            }
        }

        #endregion

        #region Music Control

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || musicSource == null) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        #endregion

        #region SFX Control

        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            // Используем основной SFX источник для простых звуков
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volumeMultiplier * sfxVolume * masterVolume);
            }
        }

        public void PlaySFX3D(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetPooledAudioSource();
            if (source != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = volumeMultiplier * sfxVolume * masterVolume;
                source.spatialBlend = 1f; // 3D звук
                source.Play();

                // Возвращаем в пул после воспроизведения
                StartCoroutine(ReturnToPoolAfterPlay(source, clip.length));
            }
        }

        public void PlayWeaponSound(string weaponType, string soundType)
        {
            AudioClip clip = weaponSounds?.GetClip($"{weaponType}_{soundType}");
            if (clip != null)
            {
                PlaySFX(clip);
            }
        }

        public void PlayEnemySound(string enemyType, string soundType, Vector3 position)
        {
            AudioClip clip = enemySounds?.GetClip($"{enemyType}_{soundType}");
            if (clip != null)
            {
                PlaySFX3D(clip, position);
            }
        }

        public void PlayPickupSound(string pickupType)
        {
            AudioClip clip = pickupSounds?.GetClip(pickupType);
            if (clip != null)
            {
                PlaySFX(clip);
            }
        }

        public void PlayEnvironmentSound(string soundName, Vector3 position)
        {
            AudioClip clip = environmentSounds?.GetClip(soundName);
            if (clip != null)
            {
                PlaySFX3D(clip, position);
            }
        }

        #endregion

        #region Audio Source Pool

        private AudioSource GetPooledAudioSource()
        {
            if (audioSourcePool.Count > 0)
            {
                AudioSource source = audioSourcePool.Dequeue();
                source.gameObject.SetActive(true);
                activeAudioSources.Add(source);
                return source;
            }

            // Если пул пуст, создаем новый источник
            GameObject audioGO = new GameObject("DynamicAudioSource");
            audioGO.transform.SetParent(transform);
            AudioSource newSource = audioGO.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            activeAudioSources.Add(newSource);
            
            return newSource;
        }

        private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration + 0.1f);
            ReturnToPool(source);
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;

            activeAudioSources.Remove(source);
            source.Stop();
            source.clip = null;
            source.spatialBlend = 0f;
            source.gameObject.SetActive(false);
            audioSourcePool.Enqueue(source);
        }

        #endregion

        #region Volume Control

        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
            UpdateSfxVolume();
        }

        private void UpdateMusicVolume()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        private void UpdateSfxVolume()
        {
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume * masterVolume;
            }

            // Обновляем громкость активных источников
            foreach (var source in activeAudioSources)
            {
                if (source != null)
                {
                    source.volume = sfxVolume * masterVolume;
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Останавливаем все активные источники
            foreach (var source in activeAudioSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }
    }

    /// <summary>
    /// Коллекция аудиоклипов для организованного хранения звуков
    /// </summary>
    [System.Serializable]
    public class AudioClipCollection
    {
        [System.Serializable]
        public class AudioClipEntry
        {
            public string name;
            public AudioClip clip;
        }

        [SerializeField] private AudioClipEntry[] clips;

        public AudioClip GetClip(string name)
        {
            foreach (var entry in clips)
            {
                if (entry.name == name)
                {
                    return entry.clip;
                }
            }
            return null;
        }
    }
}