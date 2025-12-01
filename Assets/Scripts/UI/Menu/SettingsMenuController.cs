using System;
using UnityEngine;

namespace WAD64.UI.Menu
{
    /// <summary>
    /// Управляет показом меню настроек и уведомляет подписчиков о закрытии.
    /// </summary>
    public class SettingsMenuController : MonoBehaviour
    {
        public enum SettingsOrigin
        {
            None,
            MainMenu,
            PauseMenu
        }

        [SerializeField] private GameObject settingsPanel;

        private SettingsOrigin currentOrigin = SettingsOrigin.None;

        public event Action<SettingsOrigin> OnSettingsClosed;

        public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

        private void Awake()
        {
            if (settingsPanel == null)
            {
                settingsPanel = gameObject;
            }
        }

        /// <summary>
        /// Показывает меню настроек.
        /// </summary>
        public void Open(SettingsOrigin origin)
        {
            currentOrigin = origin;
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }

            // Разблокируем и показываем курсор для работы с UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Скрывает меню и уведомляет подписчиков.
        /// </summary>
        /// <param name="notify">Если true, вызывает событие OnSettingsClosed. Если false, просто закрывает панель без уведомления.</param>
        public void CloseSettings(bool notify = true)
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (notify)
            {
                OnSettingsClosed?.Invoke(currentOrigin);
            }
        }

        /// <summary>
        /// Альтернативное имя для кнопки «Назад».
        /// </summary>
        public void BackToPreviousMenu() => CloseSettings();
    }
}


