using UnityEngine;
using WAD64.Core;

namespace WAD64.Core
{
    /// <summary>
    /// Главная точка входа в игру. Инициализирует все системы и менеджеры.
    /// Должен быть единственным в сцене и запускаться первым.
    /// </summary>
    public class GameEntryPoint : MonoBehaviour
    {
        [Header("Player Setup")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Managers")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject poolManagerPrefab;
        [SerializeField] private GameObject uiManagerPrefab;

        [Header("Level References")]
        [SerializeField] private Transform levelRoot;
        [SerializeField] private Transform enemySpawnRoot;
        [SerializeField] private Transform pickupSpawnRoot;

        [Header("Settings")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool logInitializationSteps = true;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// Инициализирует все системы игры в правильном порядке
        /// </summary>
        public void InitializeGame()
        {
            Log("Starting game initialization...");

            // 1. Очищаем предыдущие ссылки (на случай перезапуска)
            CoreReferences.ClearReferences();

            // 2. Инициализируем базовые ссылки
            InitializeBasicReferences();

            // 3. Создаем и инициализируем менеджеры
            InitializeManagers();

            // 4. Создаем игрока
            InitializePlayer();

            // 5. Финальная проверка
            ValidateInitialization();

            Log("Game initialization completed!");
        }

        private void InitializeBasicReferences()
        {
            Log("Initializing basic references...");

            // Камера
            CoreReferences.MainCamera = Camera.main;
            if (CoreReferences.MainCamera == null)
            {
                CoreReferences.MainCamera = FindObjectOfType<Camera>();
            }

            // Уровневые ссылки
            CoreReferences.LevelRoot = levelRoot;
            CoreReferences.EnemySpawnRoot = enemySpawnRoot;
            CoreReferences.PickupSpawnRoot = pickupSpawnRoot;

            Log("Basic references initialized.");
        }

        private void InitializeManagers()
        {
            Log("Initializing managers...");


            // UI Manager
            if (uiManagerPrefab != null)
            {
                var uiManagerGO = Instantiate(uiManagerPrefab);
                CoreReferences.UIManager = uiManagerGO.GetComponent<MonoBehaviour>();
                DontDestroyOnLoad(uiManagerGO);
            }

            // Game Manager (последний, так как может зависеть от других)
            if (gameManagerPrefab != null)
            {
                var gameManagerGO = Instantiate(gameManagerPrefab);
                var gameManager = gameManagerGO.GetComponent<MonoBehaviour>();
                CoreReferences.GameManager = gameManager;
                DontDestroyOnLoad(gameManagerGO);

                // Инициализируем Game Manager через рефлексию (временное решение)
                var initMethod = gameManager.GetType().GetMethod("Initialize");
                if (initMethod != null)
                {
                    initMethod.Invoke(gameManager, null);
                }
            }

            Log("Managers initialized.");
        }

        private void InitializePlayer()
        {
            Log("Initializing player...");

            GameObject playerGO = null;

            // Сначала пытаемся найти игрока в сцене
            var existingPlayer = FindObjectOfType<WAD64.Player.PlayerController>();
            if (existingPlayer != null)
            {
                playerGO = existingPlayer.gameObject;
                Log("Found existing player in scene");

                // Активируем игрока, если он отключен
                if (!playerGO.activeInHierarchy)
                {
                    playerGO.SetActive(true);
                }

                // Перемещаем к точке спавна, если она задана
                if (playerSpawnPoint != null)
                {
                    existingPlayer.Teleport(playerSpawnPoint.position, playerSpawnPoint.rotation);
                }
            }
            // Если игрока нет в сцене, создаем из префаба
            else if (playerPrefab != null)
            {
                Vector3 spawnPosition = playerSpawnPoint != null ?
                    playerSpawnPoint.position :
                    Vector3.zero;

                Quaternion spawnRotation = playerSpawnPoint != null ?
                    playerSpawnPoint.rotation :
                    Quaternion.identity;

                playerGO = Instantiate(playerPrefab, spawnPosition, spawnRotation);
                Log("Created player from prefab");
            }
            else
            {
                Debug.LogError("[GameEntryPoint] No player found in scene and no player prefab assigned!");
                return;
            }

            // Регистрируем компоненты игрока
            CoreReferences.Player = playerGO.GetComponent<WAD64.Player.PlayerController>();
            CoreReferences.PlayerMovement = playerGO.GetComponent<WAD64.Player.PlayerMovement>();
            CoreReferences.PlayerHealth = playerGO.GetComponent<WAD64.Player.PlayerHealth>();
            CoreReferences.PlayerCamera = playerGO.GetComponentInChildren<WAD64.Player.PlayerCamera>();

            // Регистрируем главную камеру
            Camera playerCamera = playerGO.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                CoreReferences.MainCamera = playerCamera;
            }

            Log("Player initialized.");
        }

        private void ValidateInitialization()
        {
            Log("Validating initialization...");

            if (!CoreReferences.AreEssentialReferencesInitialized())
            {
                Debug.LogError("[GameEntryPoint] Critical initialization failure! Essential references are missing.");
                return;
            }

            Log("All essential systems initialized successfully!");

#if UNITY_EDITOR
            // В редакторе выводим подробную информацию
            Debug.Log($"[GameEntryPoint] Initialization Summary:\n" +
                     $"Player: {(CoreReferences.Player != null ? "✓" : "✗")}\n" +
                     $"GameManager: {(CoreReferences.GameManager != null ? "✓" : "✗")}\n" +
                     $"UIManager: {(CoreReferences.UIManager != null ? "✓" : "✗")}\n" +
                     $"MainCamera: {(CoreReferences.MainCamera != null ? "✓" : "✗")}");
#endif
        }

        private void Log(string message)
        {
            if (logInitializationSteps)
            {
                Debug.Log($"[GameEntryPoint] {message}");
            }
        }

        private void OnValidate()
        {
            // Автоматически находим основные ссылки в сцене
            if (levelRoot == null)
                levelRoot = GameObject.Find("Level")?.transform;

            if (enemySpawnRoot == null)
                enemySpawnRoot = GameObject.Find("EnemySpawns")?.transform;

            if (pickupSpawnRoot == null)
                pickupSpawnRoot = GameObject.Find("PickupSpawns")?.transform;

            if (playerSpawnPoint == null)
                playerSpawnPoint = GameObject.Find("PlayerSpawn")?.transform;
        }
    }
}