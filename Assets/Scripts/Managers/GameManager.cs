using UnityEngine;
using WAD64.Core;

namespace WAD64.Managers
{
    /// <summary>
    /// Основной менеджер игры. Управляет общим состоянием, прогрессом и координирует работу других систем.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private bool isPaused = false;
        [SerializeField] private bool isGameOver = false;
        [SerializeField] private float gameTime = 0f;

        [Header("Level Settings")]
        [SerializeField] private string currentLevelName = "Blockout";
        [SerializeField] private int enemiesKilled = 0;
        [SerializeField] private int pickupsCollected = 0;

        [Header("Debug")]
        [SerializeField] private bool logGameEvents = true;

        // Events
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        public System.Action OnGameOver;
        public System.Action OnLevelCompleted;
        public System.Action<int> OnEnemyKilled;
        public System.Action<string> OnPickupCollected;

        // Properties
        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;
        public float GameTime => gameTime;
        public int EnemiesKilled => enemiesKilled;
        public int PickupsCollected => pickupsCollected;

        private void Awake()
        {
            // Убеждаемся, что GameManager единственный
            if (CoreReferences.GameManager != null && CoreReferences.GameManager != this)
            {
                Debug.LogWarning("[GameManager] Another GameManager already exists! Destroying this one.");
                Destroy(gameObject);
                return;
            }
        }

        public void Initialize()
        {
            Log("GameManager initialized");
            
            // Сброс состояния игры
            ResetGameState();
            
            // Подписка на события игрока (если уже создан)
            SubscribeToPlayerEvents();
        }

        private void Update()
        {
            if (!isPaused && !isGameOver)
            {
                gameTime += Time.deltaTime;
            }

            // Debug controls
#if UNITY_EDITOR
            HandleDebugInput();
#endif
        }

        #region Game State Management

        public void PauseGame()
        {
            if (isPaused || isGameOver) return;

            isPaused = true;
            Time.timeScale = 0f;
            
            Log("Game paused");
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (!isPaused || isGameOver) return;

            isPaused = false;
            Time.timeScale = 1f;
            
            Log("Game resumed");
            OnGameResumed?.Invoke();
        }

        public void GameOver()
        {
            if (isGameOver) return;

            isGameOver = true;
            isPaused = true;
            Time.timeScale = 0f;
            
            Log($"Game Over! Time: {gameTime:F1}s, Enemies: {enemiesKilled}, Pickups: {pickupsCollected}");
            OnGameOver?.Invoke();
        }

        public void RestartLevel()
        {
            Log("Restarting level...");
            
            // Восстанавливаем время
            Time.timeScale = 1f;
            
            // Перезагружаем сцену
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void CompleteLevel()
        {
            if (isGameOver) return;

            Log($"Level completed! Time: {gameTime:F1}s, Enemies: {enemiesKilled}, Pickups: {pickupsCollected}");
            OnLevelCompleted?.Invoke();
        }

        private void ResetGameState()
        {
            isPaused = false;
            isGameOver = false;
            gameTime = 0f;
            enemiesKilled = 0;
            pickupsCollected = 0;
            Time.timeScale = 1f;
        }

        #endregion

        #region Event Handlers

        private void SubscribeToPlayerEvents()
        {
            // Подпишемся на события игрока, когда он будет создан
            // Это будет вызвано из GameEntryPoint после создания игрока
        }

        public void OnEnemyKilledHandler(int enemyType)
        {
            enemiesKilled++;
            Log($"Enemy killed! Total: {enemiesKilled}");
            OnEnemyKilled?.Invoke(enemyType);
        }

        public void OnPickupCollectedHandler(string pickupType)
        {
            pickupsCollected++;
            Log($"Pickup collected: {pickupType}. Total: {pickupsCollected}");
            OnPickupCollected?.Invoke(pickupType);
        }

        public void OnPlayerDeathHandler()
        {
            Log("Player died!");
            GameOver();
        }

        #endregion

        #region Debug

        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartLevel();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameOver();
            }
        }

        private void Log(string message)
        {
            if (logGameEvents)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Восстанавливаем время при уничтожении
            Time.timeScale = 1f;
        }
    }
}