using UnityEngine;
using UnityEngine.SceneManagement;

namespace WAD64.UI.Menu
{
    /// <summary>
    /// Контролирует поведение главного меню (Play, Settings, Exit).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "SampleScene";

        public void PlayGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogWarning("MainMenuController: название игровой сцены не задано.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings()
        {
            MenuManager.Instance?.ShowSettingsMenu(SettingsMenuController.SettingsOrigin.MainMenu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            // В редакторе останавливаем Play Mode
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // В билде закрываем приложение
            Application.Quit();
#endif
        }
    }
}


