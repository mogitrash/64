using UnityEngine;

namespace WAD64.Player
{
    /// <summary>
    /// FPS камера с продвинутыми возможностями:
    /// - Плавный mouse look
    /// - FOV эффекты (бег, прицеливание)
    /// - Camera shake
    /// - Head bobbing
    /// - Smooth camera transitions
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        
        [Header("Look Constraints")]
        [SerializeField] private float minLookAngle = -90f;
        [SerializeField] private float maxLookAngle = 90f;
        [SerializeField] private bool lockCursor = true;
        
        [Header("FOV Effects")]
        [SerializeField] private float baseFOV = 75f;
        [SerializeField] private float runFOV = 85f;
        [SerializeField] private float aimFOV = 45f;
        [SerializeField] private float fovTransitionSpeed = 8f;
        
        [Header("Head Bobbing")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float bobSmoothness = 8f;
        
        [Header("Camera Shake")]
        [SerializeField] private float shakeDecay = 5f;
        [SerializeField] private float maxShakeIntensity = 2f;
        
        [Header("Smooth Transitions")]
        [SerializeField] private float positionSmoothness = 10f;
        [SerializeField] private float rotationSmoothness = 12f;

        // Components
        private InputHandler inputHandler;
        private PlayerMovement playerMovement;
        
        // Camera rotation
        private float verticalRotation;
        private float horizontalRotation;
        private Vector2 currentMouseDelta;
        private Vector2 currentMouseDeltaVelocity;
        
        // FOV management
        private float targetFOV;
        private float currentFOV;
        
        // Head bobbing
        private float bobTimer;
        private Vector3 bobOffset;
        private Vector3 originalCameraPosition;
        
        // Camera shake
        private Vector3 shakeOffset;
        private float shakeIntensity;
        private float shakeTimer;
        
        // Smooth positioning
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        
        // State tracking
        private bool isAiming;
        private bool isRunning;
        private float currentMovementSpeed;

        // Properties
        public Camera Camera => playerCamera;
        public float CurrentFOV => currentFOV;
        public bool IsAiming => isAiming;

        private void Awake()
        {
            // Получаем компоненты
            inputHandler = GetComponentInParent<InputHandler>();
            playerMovement = GetComponentInParent<PlayerMovement>();
            
            // Настройка камеры
            if (playerCamera == null)
                playerCamera = GetComponent<Camera>();
            
            if (cameraHolder == null)
                cameraHolder = transform;
            
            // Инициализация
            targetFOV = baseFOV;
            currentFOV = baseFOV;
            originalCameraPosition = cameraHolder.localPosition;
            
            // Настройка курсора
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Start()
        {
            // Подписываемся на события
            if (inputHandler != null)
            {
                // События прицеливания будут добавлены позже
            }
            
            // Начальная ротация
            Vector3 currentRotation = transform.eulerAngles;
            horizontalRotation = currentRotation.y;
            verticalRotation = currentRotation.x;
        }

        private void Update()
        {
            UpdateMouseLook();
            UpdateFOV();
            UpdateHeadBobbing();
            UpdateCameraShake();
            UpdateCameraTransforms();
        }

        private void LateUpdate()
        {
            ApplyCameraEffects();
        }

        #region Mouse Look

        private void UpdateMouseLook()
        {
            if (inputHandler == null) return;
            
            Vector2 mouseDelta = inputHandler.LookInput;
            
            // Сглаживание движения мыши
            currentMouseDelta = Vector2.SmoothDamp(
                currentMouseDelta, 
                mouseDelta, 
                ref currentMouseDeltaVelocity, 
                1f / rotationSmoothness
            );
            
            // Применяем чувствительность и инверсию
            float mouseX = currentMouseDelta.x * mouseSensitivity;
            float mouseY = currentMouseDelta.y * mouseSensitivity * (invertY ? 1f : -1f);
            
            // Обновляем ротацию
            horizontalRotation += mouseX;
            verticalRotation += mouseY;
            
            // Ограничиваем вертикальную ротацию
            verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
            
            // Применяем ротацию
            targetRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        }

        #endregion

        #region FOV Management

        private void UpdateFOV()
        {
            // Определяем целевой FOV на основе состояния
            if (isAiming)
            {
                targetFOV = aimFOV;
            }
            else if (isRunning && currentMovementSpeed > 0.1f)
            {
                targetFOV = Mathf.Lerp(baseFOV, runFOV, currentMovementSpeed / 10f);
            }
            else
            {
                targetFOV = baseFOV;
            }
            
            // Плавно переходим к целевому FOV
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);
            
            // Применяем к камере
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = currentFOV;
            }
        }

        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        public void SetRunning(bool running)
        {
            isRunning = running;
        }

        public void SetMovementSpeed(float speed)
        {
            currentMovementSpeed = speed;
        }

        #endregion

        #region Head Bobbing

        private void UpdateHeadBobbing()
        {
            if (!enableHeadBob || playerMovement == null) return;
            
            // Проверяем, движется ли игрок по земле
            bool isMoving = inputHandler.MoveInput.magnitude > 0.1f && playerMovement.IsGrounded;
            
            if (isMoving)
            {
                // Увеличиваем таймер на основе скорости движения
                float speedMultiplier = currentMovementSpeed / 6f; // 6f - базовая скорость ходьбы
                bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
                
                // Вычисляем смещение
                float horizontalBob = Mathf.Sin(bobTimer) * bobAmplitude;
                float verticalBob = Mathf.Sin(bobTimer * 2f) * bobAmplitude * 0.5f;
                
                bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
            }
            else
            {
                // Плавно возвращаем к исходной позиции
                bobOffset = Vector3.Lerp(bobOffset, Vector3.zero, bobSmoothness * Time.deltaTime);
                bobTimer = 0f;
            }
        }

        #endregion

        #region Camera Shake

        private void UpdateCameraShake()
        {
            if (shakeIntensity > 0f)
            {
                // Генерируем случайное смещение
                shakeOffset = Random.insideUnitSphere * shakeIntensity;
                
                // Уменьшаем интенсивность
                shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0f, shakeDecay * Time.deltaTime);
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        public void AddCameraShake(float intensity, float duration = 0.5f)
        {
            intensity = Mathf.Clamp(intensity, 0f, maxShakeIntensity);
            
            if (intensity > shakeIntensity)
            {
                shakeIntensity = intensity;
                shakeTimer = duration;
            }
        }

        #endregion

        #region Camera Transforms

        private void UpdateCameraTransforms()
        {
            if (cameraHolder == null) return;
            
            // Целевая позиция с эффектами
            targetPosition = originalCameraPosition + bobOffset + shakeOffset;
            
            // Плавно перемещаем камеру
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                targetPosition,
                positionSmoothness * Time.deltaTime
            );
        }

        private void ApplyCameraEffects()
        {
            // Применяем ротацию к игроку (горизонтальная) и камере (вертикальная)
            transform.parent.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        #endregion

        #region Public Methods

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }

        public void SetFOV(float fov)
        {
            baseFOV = Mathf.Clamp(fov, 30f, 120f);
        }

        public void ResetCameraEffects()
        {
            bobOffset = Vector3.zero;
            shakeOffset = Vector3.zero;
            shakeIntensity = 0f;
            bobTimer = 0f;
        }

        public void SetLookConstraints(float minAngle, float maxAngle)
        {
            minLookAngle = Mathf.Clamp(minAngle, -90f, 0f);
            maxLookAngle = Mathf.Clamp(maxAngle, 0f, 90f);
        }

        public void LookAt(Vector3 target, float speed = 5f)
        {
            Vector3 direction = (target - transform.position).normalized;
            float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            
            horizontalRotation = Mathf.LerpAngle(horizontalRotation, targetYaw, speed * Time.deltaTime);
            verticalRotation = Mathf.LerpAngle(verticalRotation, targetPitch, speed * Time.deltaTime);
            
            verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
        }

        #endregion

        #region Utility

        public Ray GetCameraRay()
        {
            if (playerCamera != null)
            {
                return playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            }
            return new Ray(transform.position, transform.forward);
        }

        public Vector3 GetLookDirection()
        {
            return cameraHolder.forward;
        }

        #endregion

        private void OnDestroy()
        {
            // Восстанавливаем курсор
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Показываем направление взгляда
            if (cameraHolder != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * 5f);
            }
            
            // Показываем ограничения взгляда
            Gizmos.color = Color.yellow;
            Vector3 minLookDir = Quaternion.Euler(minLookAngle, 0, 0) * Vector3.forward;
            Vector3 maxLookDir = Quaternion.Euler(maxLookAngle, 0, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position, minLookDir * 3f);
            Gizmos.DrawRay(transform.position, maxLookDir * 3f);
        }
    }
}