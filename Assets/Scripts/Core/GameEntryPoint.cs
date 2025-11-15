using UnityEngine;
using WAD64.Core;
using WAD64.Managers;
using WAD64.Weapons;
using WAD64.Player;

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

        private void Awake()
        {
            // Автоматическая настройка ссылок в сцене
            AutoSetupSceneReferences();

            if (initializeOnAwake)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// Автоматически настраивает ссылки в сцене после рефакторинга
        /// </summary>
        private void AutoSetupSceneReferences()
        {
            // Настройка WeaponManager - назначаем инстансы оружия из сцены
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
            {
                var weapons = weaponManager.GetComponentsInChildren<Weapon>();
                if (weapons != null && weapons.Length > 0)
                {
                    var field = typeof(WeaponManager).GetField("availableWeapons",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(weaponManager, weapons);
                        Debug.Log($"GameEntryPoint: WeaponManager - назначено {weapons.Length} оружий из сцены.");
                    }
                }
            }

            // Настройка PlayerMovement - GroundCheck
            var playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                var groundCheck = playerMovement.transform.Find("GroundCheck");
                if (groundCheck != null)
                {
                    var field = typeof(PlayerMovement).GetField("groundCheck",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(playerMovement, groundCheck);
                        Debug.Log("GameEntryPoint: PlayerMovement - GroundCheck назначен.");
                    }
                }
            }
        }

        /// <summary>
        /// Инициализирует все системы игры в правильном порядке
        /// </summary>
        public void InitializeGame()
        {
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
        }

        private void InitializeBasicReferences()
        {
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

        }

        private void InitializeManagers()
        {
            // UI Manager - ищем в сцене
            var existingUIManager = FindFirstObjectByType<UIManager>();
            if (existingUIManager != null)
            {
                CoreReferences.UIManager = existingUIManager;
                Debug.Log("UIManager найден в сцене, используется существующий.");
            }
            else
            {
                Debug.LogWarning("UIManager не найден в сцене! Добавьте UIManager в сцену.");
            }

            // Game Manager (последний, так как может зависеть от других) - ищем в сцене
            var existingGameManager = FindFirstObjectByType<GameManager>();
            if (existingGameManager != null)
            {
                CoreReferences.GameManager = existingGameManager;
                Debug.Log("GameManager найден в сцене, используется существующий.");

                // Инициализируем Game Manager
                existingGameManager.Initialize();
            }
            else
            {
                Debug.LogWarning("GameManager не найден в сцене! Добавьте GameManager в сцену.");
            }

        }

        private void InitializePlayer()
        {

            GameObject playerGO = null;

            // Сначала пытаемся найти игрока в сцене
            var existingPlayer = FindObjectOfType<WAD64.Player.PlayerController>();
            if (existingPlayer != null)
            {
                playerGO = existingPlayer.gameObject;

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
            }
            else
            {
                return;
            }

            // Регистрируем компоненты игрока
            CoreReferences.Player = playerGO.GetComponent<WAD64.Player.PlayerController>();
            CoreReferences.PlayerMovement = playerGO.GetComponent<WAD64.Player.PlayerMovement>();
            CoreReferences.PlayerHealth = playerGO.GetComponent<WAD64.Player.PlayerHealth>();
            CoreReferences.PlayerCamera = playerGO.GetComponentInChildren<WAD64.Player.PlayerCamera>();

            // Регистрируем WeaponManager (важно для UI компонентов, которые могут обращаться к нему в Start())
            CoreReferences.WeaponManager = playerGO.GetComponentInChildren<WAD64.Weapons.WeaponManager>();

            // Регистрируем главную камеру
            Camera playerCamera = playerGO.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                CoreReferences.MainCamera = playerCamera;
            }

        }

        private void ValidateInitialization()
        {

            if (!CoreReferences.AreEssentialReferencesInitialized())
            {
                return;
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