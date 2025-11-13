using UnityEngine;
using WAD64.Core;

namespace WAD64.Player
{
    /// <summary>
    /// Продвинутая система движения для FPS контроллера с поддержкой современных платформенных механик:
    /// - Coyote Time (прыжок после схода с платформы)
    /// - Input Buffering (сохранение нажатий)
    /// - Variable Jump Height (разная высота прыжка)
    /// - Air Control (управление в воздухе)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(InputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 25f;
        [SerializeField] private float airAcceleration = 8f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float jumpTime = 0.5f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private float groundCheckRadius = 0.4f;
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField] private Transform groundCheck;

        [Header("Coyote Time & Buffering")]
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.2f;


        [Header("Physics")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundedGravity = -2f;

        // Components
        private CharacterController controller;
        private InputHandler inputHandler;

        // Movement state
        private Vector3 velocity;
        private Vector3 moveDirection;
        private float currentSpeed;
        private bool isGrounded;
        private bool wasGroundedLastFrame;

        // Jump mechanics
        private float jumpVelocity;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool isJumping;
        private bool jumpInputReleased;

        private Vector3 centerOffset;

        // Ground detection
        private RaycastHit groundHit;
        private float groundAngle;
        private Vector3 groundNormal;

        // Events
        public System.Action OnLanded;
        public System.Action OnJumped;
        public System.Action OnStartFalling;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsJumping => isJumping;
        public bool IsFalling => velocity.y < 0 && !isGrounded;
        public float CurrentSpeed => currentSpeed;
        public Vector3 Velocity => velocity;
        public float GroundAngle => groundAngle;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputHandler = GetComponent<InputHandler>();


            // Вычисляем скорость прыжка на основе желаемой высоты
            // Поскольку gravity уже отрицательное, используем Mathf.Abs
            jumpVelocity = Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(gravity));

            // Валидация: проверяем наличие GroundCheck на сцене
            if (groundCheck == null)
            {
                Debug.LogWarning($"{gameObject.name}: GroundCheck не назначен! Создайте Transform для проверки земли и назначьте в Inspector.");
            }
        }

        private void Start()
        {
            // Подписываемся на события ввода
            if (inputHandler != null)
            {
                inputHandler.OnJumpPressed += OnJumpInput;
                inputHandler.OnJumpReleased += OnJumpReleased;
            }
        }

        private void Update()
        {
            UpdateGroundDetection();
            UpdateCoyoteTime();
            UpdateJumpBuffer();

            // Проверяем buffered jump
            CheckBufferedJump();

            UpdateMovement();
            UpdateGravity();

            // Применяем движение
            controller.Move(velocity * Time.deltaTime);

            // Обновляем состояния после движения
            UpdatePostMovement();
        }

        #region Ground Detection

        private void UpdateGroundDetection()
        {
            wasGroundedLastFrame = isGrounded;

            // Основная проверка земли
            bool wasGrounded = isGrounded;
            isGrounded = Physics.SphereCast(
                groundCheck.position + Vector3.up * groundCheckRadius,
                groundCheckRadius,
                Vector3.down,
                out groundHit,
                groundCheckDistance + groundCheckRadius,
                groundMask
            );


            if (isGrounded)
            {
                groundNormal = groundHit.normal;
                groundAngle = Vector3.Angle(groundNormal, Vector3.up);

                // Если угол слишком крутой, считаем что не на земле
                if (groundAngle > controller.slopeLimit)
                {
                    isGrounded = false;
                }
            }

            // События приземления и начала падения
            if (isGrounded && !wasGroundedLastFrame)
            {
                OnLanded?.Invoke();
                isJumping = false;
            }
            else if (!isGrounded && wasGroundedLastFrame)
            {
                OnStartFalling?.Invoke();
            }
        }

        #endregion

        #region Movement

        private void UpdateMovement()
        {
            Vector2 input = inputHandler.MoveInput;
            Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;

            // Преобразуем направление относительно камеры
            Transform cameraTransform = GetCameraTransform();
            if (cameraTransform == null)
            {
                // Если камеры нет, используем направление игрока
                moveDirection = transform.TransformDirection(inputDirection);
            }
            else
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();
                moveDirection = (forward * input.y + right * input.x).normalized;
            }

            // Определяем целевую скорость
            float targetSpeed = GetTargetSpeed();

            // Применяем ускорение/замедление
            float accel = isGrounded ?
                (inputDirection.magnitude > 0 ? acceleration : deceleration) :
                airAcceleration;

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

            // Применяем движение по горизонтали
            if (isGrounded && moveDirection.magnitude > 0)
            {
                // На земле - проецируем движение на поверхность
                Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
                velocity.x = slopeMoveDirection.x * currentSpeed;
                velocity.z = slopeMoveDirection.z * currentSpeed;
            }
            else if (!isGrounded)
            {
                // В воздухе - улучшенный контроль
                float airControl = 0.7f;
                velocity.x = Mathf.MoveTowards(velocity.x, moveDirection.x * currentSpeed, airAcceleration * airControl * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, moveDirection.z * currentSpeed, airAcceleration * airControl * Time.deltaTime);
            }
            else
            {
                // Стоим на месте
                velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0, deceleration * Time.deltaTime);
            }
        }

        private float GetTargetSpeed()
        {
            if (inputHandler.MoveInput.magnitude == 0) return 0f;

            if (inputHandler.IsRunning) return runSpeed;
            return walkSpeed;
        }

        #endregion

        #region Jump Mechanics

        private void UpdateCoyoteTime()
        {
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void UpdateJumpBuffer()
        {
            if (inputHandler.HasJumpBuffered)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        private void CheckBufferedJump()
        {
            // Если есть buffered jump и мы можем прыгнуть
            if (jumpBufferCounter > 0f && (isGrounded || coyoteTimeCounter > 0f) && !isJumping)
            {
                PerformJump();
            }
        }

        private void OnJumpInput()
        {
            jumpInputReleased = false;

            // Устанавливаем jumpBuffer при нажатии
            jumpBufferCounter = jumpBufferTime;

            // Проверяем возможность прыжка (coyote time + input buffering)
            if (isGrounded || coyoteTimeCounter > 0f)
            {
                PerformJump();
            }
        }

        private void OnJumpReleased()
        {
            jumpInputReleased = true;
        }

        private void PerformJump()
        {
            velocity.y = jumpVelocity;
            isJumping = true;
            jumpInputReleased = false;

            // Сбрасываем счетчики
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
            inputHandler.ConsumeJumpBuffer();

            OnJumped?.Invoke();
        }

        #endregion

        #region Gravity & Physics

        private void UpdateGravity()
        {
            if (isGrounded && !isJumping)
            {
                // На земле и не прыгаем - небольшая сила вниз для лучшего контакта
                velocity.y = groundedGravity;
            }
            else
            {
                // В воздухе или прыгаем - применяем гравитацию с модификаторами
                float gravityMultiplier = 1f;

                // Variable jump height - если кнопка отпущена рано, падаем быстрее
                if (velocity.y > 0 && jumpInputReleased)
                {
                    gravityMultiplier = lowJumpMultiplier;
                }
                // Быстрое падение
                else if (velocity.y < 0)
                {
                    gravityMultiplier = fallMultiplier;
                }

                velocity.y += gravity * gravityMultiplier * Time.deltaTime;

                // Ограничиваем максимальную скорость падения
                velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

                // Если мы были в прыжке и начали падать, прыжок закончен
                if (isJumping && velocity.y <= 0)
                {
                    isJumping = false;
                }
            }
        }

        #endregion


        #region Post Movement Updates

        private void UpdatePostMovement()
        {
            // Обновляем позицию точки проверки земли
            if (groundCheck != null)
            {
                groundCheck.localPosition = new Vector3(0, -controller.height * 0.5f, 0);
            }
        }

        #endregion

        #region Utility

        private Transform GetCameraTransform()
        {
            // Сначала пытаемся найти через CoreReferences
            if (CoreReferences.PlayerCamera != null)
            {
                return CoreReferences.PlayerCamera.transform;
            }

            // Если не найдено, ищем Camera.main
            if (Camera.main != null)
            {
                return Camera.main.transform;
            }

            // Если ничего не найдено, ищем любую активную камеру
            Camera camera = FindObjectOfType<Camera>();
            if (camera != null)
            {
                return camera.transform;
            }

            return null;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
                Gizmos.DrawRay(groundCheck.position, Vector3.down * (groundCheckDistance + groundCheckRadius));
            }

            // Показываем направление движения
            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, moveDirection * 2f);

                // Показываем скорость
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position + Vector3.up, velocity);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (inputHandler != null)
            {
                inputHandler.OnJumpPressed -= OnJumpInput;
                inputHandler.OnJumpReleased -= OnJumpReleased;
            }
        }
    }
}