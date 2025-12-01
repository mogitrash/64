using UnityEngine;

namespace WAD64.UI.Menu
{
    /// <summary>
    /// Центральный менеджер меню: паузы, настроек и главного меню.
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [SerializeField] private PauseMenuController pauseMenuController;
        [SerializeField] private SettingsMenuController settingsMenuController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            if (settingsMenuController != null)
            {
                settingsMenuController.OnSettingsClosed += OnSettingsClosed;
            }
        }

        private void OnDisable()
        {
            if (settingsMenuController != null)
            {
                settingsMenuController.OnSettingsClosed -= OnSettingsClosed;
            }
        }

        public void ShowPauseMenu()
        {
            pauseMenuController?.Show();
        }

        public void HidePauseMenu()
        {
            pauseMenuController?.Hide();
        }

        public void ShowSettingsMenu(SettingsMenuController.SettingsOrigin origin)
        {
            if (settingsMenuController == null) return;

            // Если открываем Settings из PauseMenu, скрываем только панель паузы (не блокируя курсор)
            if (origin == SettingsMenuController.SettingsOrigin.PauseMenu)
            {
                // Скрываем панель паузы напрямую, но не вызываем Hide() чтобы не блокировать курсор
                if (pauseMenuController != null)
                {
                    pauseMenuController.HidePanelOnly();
                }
            }

            settingsMenuController.Open(origin);
        }

        public void HideSettingsMenu()
        {
            settingsMenuController?.CloseSettings();
        }

        public void HideAllMenus()
        {
            HidePauseMenu();
            // Закрываем Settings без уведомления, чтобы не показывать панель паузы обратно
            settingsMenuController?.CloseSettings(notify: false);
        }

        /// <summary>
        /// Проверяет, открыто ли меню настроек.
        /// </summary>
        public bool IsSettingsOpen()
        {
            return settingsMenuController != null && settingsMenuController.IsOpen;
        }

        /// <summary>
        /// Проверяет, открыто ли меню паузы.
        /// </summary>
        public bool IsPauseMenuOpen()
        {
            return pauseMenuController != null && pauseMenuController.IsOpen;
        }

        /// <summary>
        /// Проверяет, открыто ли какое-либо меню (Settings или Pause).
        /// </summary>
        public bool IsAnyMenuOpen()
        {
            return IsSettingsOpen() || IsPauseMenuOpen();
        }

        private void OnSettingsClosed(SettingsMenuController.SettingsOrigin origin)
        {
            if (origin == SettingsMenuController.SettingsOrigin.PauseMenu)
            {
                ShowPauseMenu();
            }
        }
    }
}


