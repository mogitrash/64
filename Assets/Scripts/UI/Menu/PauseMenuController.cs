using UnityEngine;
using UnityEngine.SceneManagement;
using WAD64.Core;

namespace WAD64.UI.Menu
{
  /// <summary>
  /// Управляет поведением меню паузы и кнопками Continue, Settings, Main Menu.
  /// </summary>
  public class PauseMenuController : MonoBehaviour
  {
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool IsOpen => pausePanel != null && pausePanel.activeSelf;

    private void Awake()
    {
      if (pausePanel == null)
      {
        pausePanel = gameObject;
      }

      Hide();
    }

    public void Show()
    {
      if (pausePanel != null)
      {
        pausePanel.SetActive(true);
      }

      // Разблокируем и показываем курсор для работы с UI
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
    }

    public void Hide()
    {
      if (pausePanel != null)
      {
        pausePanel.SetActive(false);
      }

      // Блокируем и скрываем курсор обратно
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }

    /// <summary>
    /// Скрывает только панель без блокировки курсора (используется при переходе в Settings).
    /// </summary>
    public void HidePanelOnly()
    {
      if (pausePanel != null)
      {
        pausePanel.SetActive(false);
      }
      // НЕ блокируем курсор - Settings сам будет управлять им
    }

    public void ResumeGame()
    {
      CoreReferences.GameManager?.ResumeGame();
    }

    public void OpenSettings()
    {
      WAD64.UI.Menu.MenuManager.Instance?.ShowSettingsMenu(SettingsMenuController.SettingsOrigin.PauseMenu);
    }

    public void GoToMainMenu()
    {
      Time.timeScale = 1f;

      // Разблокируем курсор перед переходом в главное меню
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      if (string.IsNullOrWhiteSpace(mainMenuSceneName))
      {
        Debug.LogWarning("PauseMenuController: имя сцены главного меню не задано.");
        return;
      }

      SceneManager.LoadScene(mainMenuSceneName);
    }
  }
}

