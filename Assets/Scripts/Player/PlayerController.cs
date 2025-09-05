using UnityEngine;
using WAD64.Core;

namespace WAD64.Player
{
    /// <summary>
    /// Главный контроллер игрока, который координирует работу всех компонентов игрока:
    /// - InputHandler (обработка ввода)
    /// - PlayerMovement (движение и прыжки)
    /// - PlayerCamera (FPS камера)
    /// - PlayerHealth (здоровье и урон)
    /// 
    /// Служит единой точкой доступа к функциональности игрока.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(InputHandler))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private bool enablePlayerOnStart = true;
        [SerializeField] private bool logPlayerEvents = false;

        [Header("Components")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Transform weaponHolder;

        // Core components
        private CharacterController characterController;
        private InputHandler inputHandler;
        private PlayerMovement playerMovement;
        private PlayerCamera playerCamera;
        private PlayerHealth playerHealth;
        
        // State
        private bool isPlayerEnabled = true;
        private bool isInitialized = false;

        // Events
        public System.Action OnPlayerInitialized;
        public System.Action OnPlayerEnabled;
        public System.Action OnPlayerDisabled;

        // Properties
        public bool IsEnabled => isPlayerEnabled;
        public bool IsInitialized => isInitialized;
        public InputHandler Input => inputHandler;
        public PlayerMovement Movement => playerMovement;
        public PlayerCamera Camera => playerCamera;
        public PlayerHealth Health => playerHealth;
        public CharacterController Controller => characterController;
        public Transform CameraHolder => cameraHolder;
        public Transform WeaponHolder => weaponHolder;

        // Quick access properties
        public bool IsGrounded => playerMovement != null && playerMovement.IsGrounded;
        public bool IsMoving => playerMovement != null && playerMovement.CurrentSpeed > 0.1f;
        public bool IsAlive => playerHealth != null && !playerHealth.IsDead;
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Vector3 CameraForward => playerCamera != null ? playerCamera.GetLookDirection() : transform.forward;

        private void Awake()
        {
            InitializeComponents();
            SetupComponentReferences();
        }

        private void Start()
        {
            CompleteInitialization();
            
            if (enablePlayerOnStart)
            {
                EnablePlayer();
            }
        }

        private void Update()
        {
            if (isPlayerEnabled && isInitialized)
            {
                UpdatePlayerLogic();
            }
        }

        #region Initialization

        private void InitializeComponents()
        {
            // Получаем все необходимые компоненты
            characterController = GetComponent<CharacterController>();
            inputHandler = GetComponent<InputHandler>();
            playerMovement = GetComponent<PlayerMovement>();
            playerHealth = GetComponent<PlayerHealth>();
            
            // Ищем камеру
            playerCamera = GetComponentInChildren<PlayerCamera>();
            if (playerCamera == null)
            {
                Debug.LogWarning("[PlayerController] PlayerCamera not found in children!");
            }

            // Создаем недостающие holders
            SetupHolders();

            Log("Components initialized");
        }

        private void SetupHolders()
        {
            // Camera Holder
            if (cameraHolder == null)
            {
                GameObject cameraHolderGO = new GameObject("CameraHolder");
                cameraHolderGO.transform.SetParent(transform);
                cameraHolderGO.transform.localPosition = new Vector3(0, characterController.height * 0.8f, 0);
                cameraHolder = cameraHolderGO.transform;

                // Перемещаем камеру в holder, если она есть
                if (playerCamera != null)
                {
                    playerCamera.transform.SetParent(cameraHolder);
                    playerCamera.transform.localPosition = Vector3.zero;
                    playerCamera.transform.localRotation = Quaternion.identity;
                }
            }

            // Weapon Holder
            if (weaponHolder == null)
            {
                GameObject weaponHolderGO = new GameObject("WeaponHolder");
                weaponHolderGO.transform.SetParent(cameraHolder);
                weaponHolderGO.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                weaponHolder = weaponHolderGO.transform;
            }
        }

        private void SetupComponentReferences()
        {
            // Настраиваем связи между компонентами
            if (playerCamera != null && playerMovement != null)
            {
                // Передаем информацию о движении в камеру
                playerMovement.OnLanded += () => playerCamera.AddCameraShake(0.2f, 0.2f);
                playerMovement.OnJumped += () => playerCamera.AddCameraShake(0.1f, 0.1f);
            }

            // Подписываемся на события здоровья
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied += OnPlayerDied;
                playerHealth.OnPlayerRespawned += OnPlayerRespawned;
            }

            // Подписываемся на события ввода
            if (inputHandler != null)
            {
                inputHandler.OnPausePressed += OnPausePressed;
            }
        }

        private void CompleteInitialization()
        {
            // Регистрируем игрока в CoreReferences
            CoreReferences.Player = this;
            CoreReferences.PlayerMovement = playerMovement;
            CoreReferences.PlayerHealth = playerHealth;
            CoreReferences.PlayerCamera = playerCamera;

            isInitialized = true;
            OnPlayerInitialized?.Invoke();
            
            Log("Player initialization completed");
        }

        #endregion

        #region Player Control

        public void EnablePlayer()
        {
            if (isPlayerEnabled) return;

            isPlayerEnabled = true;
            
            // Включаем компоненты
            if (inputHandler != null) inputHandler.EnableInput();
            if (characterController != null) characterController.enabled = true;
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerCamera != null) playerCamera.enabled = true;

            OnPlayerEnabled?.Invoke();
            Log("Player enabled");
        }

        public void DisablePlayer()
        {
            if (!isPlayerEnabled) return;

            isPlayerEnabled = false;
            
            // Отключаем компоненты
            if (inputHandler != null) inputHandler.DisableInput();
            if (playerMovement != null) playerMovement.enabled = false;
            if (playerCamera != null) playerCamera.enabled = false;

            OnPlayerDisabled?.Invoke();
            Log("Player disabled");
        }

        private void UpdatePlayerLogic()
        {
            // Обновляем связи между компонентами
            UpdateCameraFeedback();
            UpdateMovementFeedback();
        }

        private void UpdateCameraFeedback()
        {
            if (playerCamera == null || playerMovement == null) return;

            // Передаем состояние движения в камеру
            playerCamera.SetRunning(inputHandler.IsRunning);
            playerCamera.SetMovementSpeed(playerMovement.CurrentSpeed);
        }

        private void UpdateMovementFeedback()
        {
            // Здесь можно добавить дополнительную логику обратной связи
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDied()
        {
            Log("Player died");
            // Можно добавить специальную логику при смерти
            DisablePlayer();
        }

        private void OnPlayerRespawned()
        {
            Log("Player respawned");
            EnablePlayer();
        }

        private void OnPausePressed()
        {
            // Передаем событие паузы в GameManager
            if (CoreReferences.GameManager != null)
            {
                var gameManager = CoreReferences.GameManager as Managers.GameManager;
                if (gameManager != null)
                {
                    if (gameManager.IsPaused)
                        gameManager.ResumeGame();
                    else
                        gameManager.PauseGame();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Телепортирует игрока в указанную позицию
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation = default)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = position;
                if (rotation != default)
                {
                    transform.rotation = rotation;
                }
                characterController.enabled = true;
            }
        }

        /// <summary>
        /// Добавляет импульс к игроку
        /// </summary>
        public void AddForce(Vector3 force)
        {
            if (playerMovement != null)
            {
                // Реализация будет добавлена в PlayerMovement
                Log($"Force applied: {force}");
            }
        }

        /// <summary>
        /// Получает луч от камеры игрока
        /// </summary>
        public Ray GetCameraRay()
        {
            if (playerCamera != null)
            {
                return playerCamera.GetCameraRay();
            }
            return new Ray(transform.position, transform.forward);
        }

        /// <summary>
        /// Устанавливает чувствительность мыши
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            if (inputHandler != null)
                inputHandler.SetMouseSensitivity(sensitivity);
            
            if (playerCamera != null)
                playerCamera.SetMouseSensitivity(sensitivity);
        }

        /// <summary>
        /// Включает/выключает инверсию мыши по Y
        /// </summary>
        public void SetInvertMouseY(bool invert)
        {
            if (inputHandler != null)
                inputHandler.SetInvertMouseY(invert);
            
            if (playerCamera != null)
                playerCamera.SetInvertY(invert);
        }

        #endregion

        #region Utility

        private void Log(string message)
        {
            if (logPlayerEvents)
            {
                Debug.Log($"[PlayerController] {message}");
            }
        }

        /// <summary>
        /// Получает информацию о состоянии игрока для отладки
        /// </summary>
        public string GetPlayerStateInfo()
        {
            return $"Player State:\n" +
                   $"- Enabled: {isPlayerEnabled}\n" +
                   $"- Alive: {IsAlive}\n" +
                   $"- Grounded: {IsGrounded}\n" +
                   $"- Moving: {IsMoving}\n" +
                   $"- Health: {(playerHealth != null ? $"{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}" : "N/A")}\n" +
                   $"- Position: {transform.position}\n" +
                   $"- Velocity: {(playerMovement != null ? playerMovement.Velocity.ToString() : "N/A")}";
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от всех событий
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied -= OnPlayerDied;
                playerHealth.OnPlayerRespawned -= OnPlayerRespawned;
            }

            if (inputHandler != null)
            {
                inputHandler.OnPausePressed -= OnPausePressed;
            }

            // Очищаем ссылки в CoreReferences
            if (CoreReferences.Player == this)
            {
                CoreReferences.Player = null;
                CoreReferences.PlayerMovement = null;
                CoreReferences.PlayerHealth = null;
                CoreReferences.PlayerCamera = null;
            }
        }

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Показываем направление взгляда
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, CameraForward * 3f);
            
            // Показываем границы CharacterController
            if (characterController != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(
                    transform.position + Vector3.up * (characterController.height * 0.5f),
                    new Vector3(characterController.radius * 2f, characterController.height, characterController.radius * 2f)
                );
            }
        }

        #endregion
    }
}