using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

namespace WAD64.Player
{
    /// <summary>
    /// Обрабатывает все пользовательские вводы с использованием новой Unity Input System.
    /// Предоставляет удобный интерфейс для получения состояния кнопок и осей.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertMouseY = false;

        [Header("Input Buffering")]
        [SerializeField] private float jumpBufferTime = 0.2f;
        [SerializeField] private float fireBufferTime = 0.1f;

        // Input Actions (будут настроены через Input Action Asset)
        private PlayerInput playerInput;
        private InputActionMap gameplayActionMap;

        // Movement inputs
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction runAction;
        private InputAction crouchAction;

        // Combat inputs
        private InputAction fireAction;
        private InputAction aimAction;
        private InputAction reloadAction;
        private InputAction weaponSwitchAction;

        // UI inputs
        private InputAction pauseAction;
        private InputAction interactAction;

        // Input states
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool runPressed;
        private bool crouchPressed;
        private bool firePressed;
        private bool fireHeld;
        private bool aimPressed;
        private bool reloadPressed;
        private bool pausePressed;
        private bool interactPressed;

        // Input buffering
        private float jumpBufferTimer;
        private float fireBufferTimer;

        // Events
        public System.Action OnJumpPressed;
        public System.Action OnJumpReleased;
        public System.Action OnFirePressed;
        public System.Action OnFireReleased;
        public System.Action OnReloadPressed;
        public System.Action OnPausePressed;
        public System.Action OnInteractPressed;
        public System.Action<float> OnWeaponSwitch;

        // Properties
        public Vector2 MoveInput => enableInput ? moveInput : Vector2.zero;
        public Vector2 LookInput => enableInput ? lookInput * mouseSensitivity * (invertMouseY ? new Vector2(1, -1) : Vector2.one) : Vector2.zero;
        public bool IsRunning => enableInput && runPressed;
        public bool IsCrouching => enableInput && crouchPressed;
        public bool IsAiming => enableInput && aimPressed;
        public bool IsFireHeld => enableInput && fireHeld;
        public bool HasJumpBuffered => jumpBufferTimer > 0f;
        public bool HasFireBuffered => fireBufferTimer > 0f;

        private void Awake()
        {
            InitializeInputs();
        }

        private void OnEnable()
        {
            EnableInputs();
        }

        private void OnDisable()
        {
            DisableInputs();
        }

        private void Update()
        {
            UpdateInputBuffers();
            ProcessInputs();
        }

        #region Initialization

        private void InitializeInputs()
        {
            // Получаем PlayerInput компонент
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("[InputHandler] PlayerInput component not found!");
                return;
            }

            // Проверяем, что у PlayerInput есть actions
            if (playerInput.actions == null)
            {
                Debug.LogError("[InputHandler] PlayerInput has no actions assigned!");
                return;
            }

            // Получаем Action Map
            gameplayActionMap = playerInput.actions.FindActionMap("Player");
            if (gameplayActionMap == null)
            {
                Debug.LogError("[InputHandler] Player action map not found! Available maps: " +
                    string.Join(", ", playerInput.actions.actionMaps.Select(map => map.name)));
                return;
            }

            // Инициализируем действия
            InitializeMovementActions();
            InitializeCombatActions();
            InitializeUIActions();

            Debug.Log("[InputHandler] Input system initialized");
        }

        private void InitializeMovementActions()
        {
            moveAction = gameplayActionMap.FindAction("Move");
            lookAction = gameplayActionMap.FindAction("Look");
            jumpAction = gameplayActionMap.FindAction("Jump");
            runAction = gameplayActionMap.FindAction("Sprint");
            crouchAction = gameplayActionMap.FindAction("Crouch");

            // Подписываемся на события
            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
                jumpAction.canceled += OnJumpCanceled;
            }
        }

        private void InitializeCombatActions()
        {
            fireAction = gameplayActionMap.FindAction("Attack");
            // Временно отключаем несуществующие действия
            // aimAction = gameplayActionMap.FindAction("Aim");
            // reloadAction = gameplayActionMap.FindAction("Reload");
            // weaponSwitchAction = gameplayActionMap.FindAction("WeaponSwitch");

            // Подписываемся на события
            if (fireAction != null)
            {
                fireAction.performed += OnFirePerformed;
                fireAction.canceled += OnFireCanceled;
            }

            // if (reloadAction != null)
            // {
            //     reloadAction.performed += OnReloadPerformed;
            // }

            // if (weaponSwitchAction != null)
            // {
            //     weaponSwitchAction.performed += OnWeaponSwitchPerformed;
            // }
        }

        private void InitializeUIActions()
        {
            // Временно отключаем Pause (можно использовать Escape)
            // pauseAction = gameplayActionMap.FindAction("Pause");
            interactAction = gameplayActionMap.FindAction("Interact");

            // Подписываемся на события
            // if (pauseAction != null)
            // {
            //     pauseAction.performed += OnPausePerformed;
            // }

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }
        }

        #endregion

        #region Input Processing

        private void ProcessInputs()
        {
            if (!enableInput) return;

            // Movement inputs
            moveInput = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            lookInput = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

            // Button states
            runPressed = runAction?.IsPressed() ?? false;
            crouchPressed = crouchAction?.IsPressed() ?? false;
            // aimPressed = aimAction?.IsPressed() ?? false;
            fireHeld = fireAction?.IsPressed() ?? false;
        }

        private void UpdateInputBuffers()
        {
            // Jump buffer
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            // Fire buffer
            if (fireBufferTimer > 0f)
            {
                fireBufferTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Input Events

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            jumpPressed = true;
            jumpHeld = true;
            jumpBufferTimer = jumpBufferTime;
            OnJumpPressed?.Invoke();
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            jumpPressed = false;
            jumpHeld = false;
            OnJumpReleased?.Invoke();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            firePressed = true;
            fireBufferTimer = fireBufferTime;
            OnFirePressed?.Invoke();
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            firePressed = false;
            OnFireReleased?.Invoke();
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            reloadPressed = true;
            OnReloadPressed?.Invoke();
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            pausePressed = true;
            OnPausePressed?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            interactPressed = true;
            OnInteractPressed?.Invoke();
        }

        private void OnWeaponSwitchPerformed(InputAction.CallbackContext context)
        {
            float scrollValue = context.ReadValue<float>();
            OnWeaponSwitch?.Invoke(scrollValue);
        }

        #endregion

        #region Public Methods

        public void ConsumeJumpBuffer()
        {
            jumpBufferTimer = 0f;
        }

        public void ConsumeFireBuffer()
        {
            fireBufferTimer = 0f;
        }

        public void EnableInput()
        {
            enableInput = true;
        }

        public void DisableInput()
        {
            enableInput = false;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        public void SetInvertMouseY(bool invert)
        {
            invertMouseY = invert;
        }

        #endregion

        #region Input System Management

        private void EnableInputs()
        {
            gameplayActionMap?.Enable();
        }

        private void DisableInputs()
        {
            gameplayActionMap?.Disable();
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от всех событий
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpPerformed;
                jumpAction.canceled -= OnJumpCanceled;
            }

            if (fireAction != null)
            {
                fireAction.performed -= OnFirePerformed;
                fireAction.canceled -= OnFireCanceled;
            }

            if (reloadAction != null)
            {
                reloadAction.performed -= OnReloadPerformed;
            }

            if (weaponSwitchAction != null)
            {
                weaponSwitchAction.performed -= OnWeaponSwitchPerformed;
            }

            if (pauseAction != null)
            {
                pauseAction.performed -= OnPausePerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
            }
        }
    }
}