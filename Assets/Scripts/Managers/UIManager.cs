using UnityEngine;
using TMPro;
using WAD64.Core;

namespace WAD64.Managers
{
    /// <summary>
    /// Упрощенный менеджер пользовательского интерфейса.
    /// На данном этапе отвечает только за отображение дебаг информации и FPS.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Debug Info")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI debugText;

        // FPS calculation
        private int frameCount = 0;
        private float fpsTimer = 0f;
        private float currentFPS = 0f;

        private void Awake()
        {
            // Убеждаемся, что UIManager единственный
            if (CoreReferences.UIManager != null && CoreReferences.UIManager != this)
            {
                Debug.LogWarning("[UIManager] Another UIManager already exists! Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Debug.Log("[UIManager] Minimal UI initialized");
        }

        private void Update()
        {
            if (showFPS)
            {
                UpdateFPS();
            }

            UpdateDebugInfo();
        }

        #region FPS Counter

        private void UpdateFPS()
        {
            frameCount++;
            fpsTimer += Time.deltaTime;

            if (fpsTimer >= 1f)
            {
                currentFPS = frameCount / fpsTimer;

                if (fpsText != null)
                {
                    fpsText.text = $"FPS: {Mathf.RoundToInt(currentFPS)}";
                }

                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        #endregion

        #region Debug Info

        private void UpdateDebugInfo()
        {
            if (debugText != null)
            {
                string debugInfo = GetDebugInfo();
                debugText.text = debugInfo;
            }
        }

        private string GetDebugInfo()
        {
            var info = new System.Text.StringBuilder();

            info.AppendLine($"Game Time: {Time.time:F1}s");
            info.AppendLine($"Time Scale: {Time.timeScale:F1}");

            // Проверяем состояние основных ссылок
            info.AppendLine("\n--- Core References ---");
            info.AppendLine($"Player: {(CoreReferences.Player != null ? "✓" : "✗")}");
            info.AppendLine($"Main Camera: {(CoreReferences.MainCamera != null ? "✓" : "✗")}");
            info.AppendLine($"Game Manager: {(CoreReferences.GameManager != null ? "✓" : "✗")}");

            return info.ToString();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Показать/скрыть FPS счетчик
        /// </summary>
        public void SetFPSVisible(bool visible)
        {
            showFPS = visible;
            if (fpsText != null)
            {
                fpsText.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Показать/скрыть дебаг информацию
        /// </summary>
        public void SetDebugInfoVisible(bool visible)
        {
            if (debugText != null)
            {
                debugText.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Получить текущий FPS
        /// </summary>
        public float GetCurrentFPS()
        {
            return currentFPS;
        }

        #endregion
    }
}