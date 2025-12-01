using UnityEngine;
using UnityEngine.UI;
using WAD64.Managers;

namespace WAD64.UI.Menu
{
    /// <summary>
    /// Управляет общим уровнем аудио в игре и сохраняет выбор пользователя.
    /// </summary>
    public class AudioSettings : MonoBehaviour
    {
        private const string VolumePrefKey = "MasterVolume";
        private const string MusicVolumePrefKey = "MusicVolume";

        [SerializeField] private SettingsData settingsData;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        private void Awake()
        {
            LoadVolume();
            LoadMusicVolume();
        }

        private void OnEnable()
        {
            SyncSliderWithCurrentVolume();
            SyncMusicSliderWithCurrentVolume();
        }

        /// <summary>
        /// Меняет громкость и сохраняет значение.
        /// </summary>
        public void SetMasterVolume(float value)
        {
            ApplyVolume(value);
        }

        /// <summary>
        /// Устанавливает master volume из значения слайдера (для привязки к onValueChanged с параметром float).
        /// </summary>
        public void UpdateVolumeFromSlider(float value)
        {
            ApplyVolume(value, updateSlider: false);
        }

        /// <summary>
        /// Устанавливает master volume из значения слайдера (для привязки к onValueChanged без параметра).
        /// </summary>
        public void UpdateVolumeFromSlider()
        {
            if (volumeSlider != null)
            {
                ApplyVolume(volumeSlider.value, updateSlider: false);
            }
        }

        /// <summary>
        /// Устанавливает music volume из значения слайдера (для привязки к onValueChanged с параметром float).
        /// </summary>
        public void UpdateMusicVolumeFromSlider(float value)
        {
            ApplyMusicVolume(value, updateSlider: false);
        }

        /// <summary>
        /// Устанавливает music volume из значения слайдера (для привязки к onValueChanged без параметра).
        /// </summary>
        public void UpdateMusicVolumeFromSlider()
        {
            if (musicVolumeSlider != null)
            {
                ApplyMusicVolume(musicVolumeSlider.value, updateSlider: false);
            }
        }

        /// <summary>
        /// Загружает громкость из PlayerPrefs или ScriptableObject.
        /// </summary>
        private void LoadVolume()
        {
            float defaultVolume = settingsData != null ? settingsData.MasterVolume : 1f;

            float storedVolume = PlayerPrefs.GetFloat(VolumePrefKey, defaultVolume);
            ApplyVolume(storedVolume, updateSlider: false);
        }

        /// <summary>
        /// Синхронизирует слайдер с текущим уровнем громкости.
        /// </summary>
        private void SyncSliderWithCurrentVolume()
        {
            if (volumeSlider == null) return;

            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        private void ApplyVolume(float value, bool updateSlider = true)
        {
            float clamped = Mathf.Clamp01(value);
            AudioListener.volume = clamped;

            if (settingsData != null)
            {
                settingsData.MasterVolume = clamped;
            }

            PlayerPrefs.SetFloat(VolumePrefKey, clamped);

            if (updateSlider && volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(clamped);
            }
        }

        /// <summary>
        /// Принудительно обновляет слайдер (можно вызвать из UI, если настройки откроются повторно).
        /// </summary>
        public void RefreshSlider()
        {
            SyncSliderWithCurrentVolume();
            SyncMusicSliderWithCurrentVolume();
        }

        /// <summary>
        /// Загружает громкость музыки из PlayerPrefs или ScriptableObject.
        /// </summary>
        private void LoadMusicVolume()
        {
            float defaultVolume = settingsData != null ? settingsData.MusicVolume : 1f;

            float storedVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, defaultVolume);
            ApplyMusicVolume(storedVolume, updateSlider: false);
        }

        /// <summary>
        /// Синхронизирует слайдер музыки с текущим уровнем громкости.
        /// </summary>
        private void SyncMusicSliderWithCurrentVolume()
        {
            if (musicVolumeSlider == null) return;

            float currentVolume = MusicManager.Instance != null
                ? MusicManager.Instance.GetMusicVolume()
                : PlayerPrefs.GetFloat(MusicVolumePrefKey, 1f);

            musicVolumeSlider.SetValueWithoutNotify(currentVolume);
        }

        /// <summary>
        /// Применяет громкость музыки и сохраняет значение.
        /// </summary>
        private void ApplyMusicVolume(float value, bool updateSlider = true)
        {
            float clamped = Mathf.Clamp01(value);

            // Устанавливаем громкость через MusicManager
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetMusicVolume(clamped);
            }

            if (settingsData != null)
            {
                settingsData.MusicVolume = clamped;
            }

            PlayerPrefs.SetFloat(MusicVolumePrefKey, clamped);

            if (updateSlider && musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(clamped);
            }
        }
    }
}


