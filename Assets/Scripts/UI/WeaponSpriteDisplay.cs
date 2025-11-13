using UnityEngine;
using UnityEngine.UI;
using WAD64.Core;
using WAD64.Weapons;
using WAD64.Player;

namespace WAD64.UI
{
    /// <summary>
    /// Отображает и анимирует спрайты оружия в стиле DOOM.
    /// Подписывается на события WeaponManager для синхронизации с игрой.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class WeaponSpriteDisplay : MonoBehaviour, IUIElement
    {
        [Header("References")]
        [SerializeField] private Image weaponImage;

        [Header("Weapon Sprite Data")]
        [Tooltip("Массив данных спрайтов для каждого оружия")]
        [SerializeField] private WeaponSpriteData[] weaponSpriteData = new WeaponSpriteData[0];

        [Header("Animation Settings")]
        [SerializeField] private bool playIdleAnimation = true;
        [SerializeField] private bool playFireAnimation = true;
        [SerializeField] private bool playReloadAnimation = true;

        [Header("Weapon Bobbing")]
        [Tooltip("Включить покачивание оружия при ходьбе")]
        [SerializeField] private bool enableBobbing = true;
        [Tooltip("Вертикальная амплитуда покачивания (в пикселях)")]
        [SerializeField] private float verticalBobbingAmount = 10f;
        [Tooltip("Горизонтальная амплитуда покачивания (в пикселях)")]
        [SerializeField] private float horizontalBobbingAmount = 5f;
        [Tooltip("Скорость покачивания (циклов в секунду)")]
        [SerializeField] private float bobbingSpeed = 2f;
        [Tooltip("Минимальная скорость для активации покачивания")]
        [SerializeField] private float minSpeedForBobbing = 0.1f;
        [Tooltip("Скорость плавного перехода к покачиванию")]
        [SerializeField] private float bobbingTransitionSpeed = 5f;

        // State
        private WeaponManager weaponManager;
        private WeaponSpriteData currentSpriteData;
        private Weapon currentWeapon;
        private AnimationState currentState = AnimationState.Idle;

        // Animation timing
        private float animationTimer = 0f;
        private int currentFrameIndex = 0;
        private bool isPlayingOneShot = false;

        // Reload synchronization
        private bool isReloading = false;
        private float reloadStartTime = 0f;

        // Bobbing
        private RectTransform rectTransform;
        private Vector2 baseAnchoredPosition;
        private float bobbingTimer = 0f;
        private float currentBobbingIntensity = 0f;
        private PlayerMovement playerMovement;

        private enum AnimationState
        {
            Idle,
            Fire,
            Reload,
            Empty
        }

        private void Awake()
        {
            if (weaponImage == null)
            {
                weaponImage = GetComponent<Image>();
            }

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
            }
        }

        private void Start()
        {
            // Пытаемся найти WeaponManager сразу
            TryInitializeWeaponManager();

            // Находим PlayerMovement для проверки движения
            TryInitializePlayerMovement();
        }

        private void TryInitializePlayerMovement()
        {
            if (CoreReferences.PlayerMovement != null)
            {
                playerMovement = CoreReferences.PlayerMovement as PlayerMovement;
            }
            else if (CoreReferences.Player != null)
            {
                var playerController = CoreReferences.Player as PlayerController;
                if (playerController != null)
                {
                    playerMovement = playerController.Movement;
                }
            }
        }

        private void TryInitializeWeaponManager()
        {
            // Находим WeaponManager через CoreReferences
            if (CoreReferences.WeaponManager != null)
            {
                weaponManager = CoreReferences.WeaponManager as WeaponManager;
                if (weaponManager != null)
                {
                    SubscribeToWeaponEvents();
                    OnWeaponChanged(weaponManager.CurrentWeapon);
                    Debug.Log($"WeaponSpriteDisplay: Successfully initialized with WeaponManager. Current weapon: {weaponManager.CurrentWeapon?.WeaponName ?? "null"}");
                }
            }
        }

        private void Update()
        {
            // Если WeaponManager еще не найден, пытаемся найти его каждый кадр (до первого успеха)
            if (weaponManager == null)
            {
                TryInitializeWeaponManager();
            }

            // Если PlayerMovement еще не найден, пытаемся найти его каждый кадр (до первого успеха)
            if (playerMovement == null)
            {
                TryInitializePlayerMovement();
            }

            UpdateAnimation();
            UpdateBobbing();
        }

        private void OnDestroy()
        {
            UnsubscribeFromWeaponEvents();
        }

        #region Setup

        private WeaponSpriteData FindSpriteDataForWeapon(Weapon weapon)
        {
            if (weapon == null || weaponSpriteData == null)
                return null;

            string weaponName = weapon.WeaponName;

            foreach (var data in weaponSpriteData)
            {
                if (data != null && data.weaponName == weaponName)
                {
                    return data;
                }
            }

            return null;
        }

        #endregion

        #region Animation

        private void UpdateAnimation()
        {
            if (currentSpriteData == null || weaponImage == null)
                return;

            animationTimer += Time.deltaTime;

            switch (currentState)
            {
                case AnimationState.Idle:
                    UpdateIdleAnimation();
                    break;
                case AnimationState.Fire:
                    UpdateFireAnimation();
                    break;
                case AnimationState.Reload:
                    UpdateReloadAnimation();
                    break;
                case AnimationState.Empty:
                    UpdateEmptyAnimation();
                    break;
            }
        }

        private void UpdateIdleAnimation()
        {
            if (!playIdleAnimation || currentSpriteData.idleSprites == null ||
                currentSpriteData.idleSprites.Length == 0)
            {
                return;
            }

            float frameTime = 1f / currentSpriteData.idleAnimationSpeed;

            if (animationTimer >= frameTime)
            {
                animationTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % currentSpriteData.idleSprites.Length;
                UpdateSprite(currentSpriteData.idleSprites[currentFrameIndex]);
            }
        }

        private void UpdateFireAnimation()
        {
            if (!playFireAnimation || currentSpriteData.fireSprites == null ||
                currentSpriteData.fireSprites.Length == 0)
            {
                ReturnToIdle();
                return;
            }

            float frameTime = 1f / currentSpriteData.fireAnimationSpeed;

            if (animationTimer >= frameTime)
            {
                animationTimer = 0f;
                currentFrameIndex++;

                if (currentFrameIndex >= currentSpriteData.fireSprites.Length)
                {
                    ReturnToIdle();
                }
                else
                {
                    UpdateSprite(currentSpriteData.fireSprites[currentFrameIndex]);
                }
            }
        }

        private void UpdateReloadAnimation()
        {
            if (!playReloadAnimation || currentSpriteData.reloadSprites == null ||
                currentSpriteData.reloadSprites.Length == 0)
            {
                ReturnToIdle();
                return;
            }

            // Синхронизируем анимацию с реальным прогрессом перезарядки
            if (isReloading && currentWeapon != null)
            {
                float reloadProgress = currentWeapon.ReloadProgress;

                // Вычисляем текущий кадр на основе прогресса перезарядки
                int targetFrameIndex = Mathf.FloorToInt(reloadProgress * currentSpriteData.reloadSprites.Length);
                targetFrameIndex = Mathf.Clamp(targetFrameIndex, 0, currentSpriteData.reloadSprites.Length - 1);

                // Обновляем спрайт если кадр изменился
                if (targetFrameIndex != currentFrameIndex)
                {
                    currentFrameIndex = targetFrameIndex;
                    UpdateSprite(currentSpriteData.reloadSprites[currentFrameIndex]);
                }

                // Если перезарядка завершена, возвращаемся к idle
                if (reloadProgress >= 1f)
                {
                    ReturnToIdle();
                }
            }
            else
            {
                // Если синхронизация не работает (оружие не найдено или не перезаряжается), возвращаемся к idle
                ReturnToIdle();
            }
        }

        private void UpdateEmptyAnimation()
        {
            if (currentSpriteData.emptySprite != null)
            {
                UpdateSprite(currentSpriteData.emptySprite);
            }
            else
            {
                ReturnToIdle();
            }
        }

        private void UpdateSprite(Sprite sprite)
        {
            if (weaponImage != null && sprite != null)
            {
                weaponImage.sprite = sprite;
            }
        }

        private void ReturnToIdle()
        {
            currentState = AnimationState.Idle;
            currentFrameIndex = 0;
            animationTimer = 0f;
            isPlayingOneShot = false;
            isReloading = false;

            // Показываем первый кадр idle анимации
            if (currentSpriteData != null && currentSpriteData.idleSprites != null &&
                currentSpriteData.idleSprites.Length > 0)
            {
                UpdateSprite(currentSpriteData.idleSprites[0]);
            }
        }

        #endregion

        #region Bobbing

        private void UpdateBobbing()
        {
            if (!enableBobbing || rectTransform == null)
                return;

            // Определяем интенсивность покачивания на основе скорости движения
            float targetIntensity = 0f;
            if (playerMovement != null)
            {
                float speed = playerMovement.CurrentSpeed;
                if (speed > minSpeedForBobbing)
                {
                    // Нормализуем скорость относительно максимальной скорости бега (10 по умолчанию)
                    float maxSpeed = 10f; // runSpeed по умолчанию
                    float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
                    targetIntensity = normalizedSpeed;
                }
            }

            // Плавно переходим к целевой интенсивности
            currentBobbingIntensity = Mathf.Lerp(currentBobbingIntensity, targetIntensity,
                Time.deltaTime * bobbingTransitionSpeed);

            // Если интенсивность слишком мала, сбрасываем позицию
            if (currentBobbingIntensity < 0.01f)
            {
                rectTransform.anchoredPosition = baseAnchoredPosition;
                bobbingTimer = 0f;
                return;
            }

            // Обновляем таймер покачивания
            bobbingTimer += Time.deltaTime * bobbingSpeed * currentBobbingIntensity;

            // Вычисляем покачивание по дуге
            // Вертикальное движение: синусоида (вверх-вниз)
            float verticalOffset = Mathf.Sin(bobbingTimer) * verticalBobbingAmount * currentBobbingIntensity;

            // Горизонтальное движение: косинусоида со сдвигом фазы для создания дуги
            // Используем cos с меньшей частотой для создания эффекта дуги
            float horizontalOffset = Mathf.Cos(bobbingTimer * 0.5f) * horizontalBobbingAmount * currentBobbingIntensity;

            // Применяем покачивание к позиции
            rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(horizontalOffset, verticalOffset);
        }

        /// <summary>
        /// Обновляет базовую позицию (вызывается при изменении позиции в редакторе)
        /// </summary>
        public void UpdateBasePosition()
        {
            if (rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
            }
        }

        #endregion

        #region Weapon Events

        private void SubscribeToWeaponEvents()
        {
            if (weaponManager != null)
            {
                weaponManager.OnWeaponChanged += OnWeaponChanged;
                weaponManager.OnWeaponFired += OnWeaponFired;
                weaponManager.OnWeaponReloaded += OnWeaponReloaded;
            }

            // Подписываемся на события текущего оружия для синхронизации перезарядки
            SubscribeToCurrentWeaponEvents();
        }

        private void UnsubscribeFromWeaponEvents()
        {
            if (weaponManager != null)
            {
                weaponManager.OnWeaponChanged -= OnWeaponChanged;
                weaponManager.OnWeaponFired -= OnWeaponFired;
                weaponManager.OnWeaponReloaded -= OnWeaponReloaded;
            }

            // Отписываемся от событий текущего оружия
            UnsubscribeFromCurrentWeaponEvents();
        }

        private void SubscribeToCurrentWeaponEvents()
        {
            if (currentWeapon != null)
            {
                currentWeapon.OnReloadStarted += OnWeaponReloadStarted;
                currentWeapon.OnReloadCompleted += OnWeaponReloadCompleted;
            }
        }

        private void UnsubscribeFromCurrentWeaponEvents()
        {
            if (currentWeapon != null)
            {
                currentWeapon.OnReloadStarted -= OnWeaponReloadStarted;
                currentWeapon.OnReloadCompleted -= OnWeaponReloadCompleted;
            }
        }

        private void OnWeaponChanged(Weapon weapon)
        {
            // Отписываемся от событий старого оружия
            UnsubscribeFromCurrentWeaponEvents();

            if (weapon == null)
            {
                if (weaponImage != null)
                {
                    weaponImage.sprite = null;
                }
                currentSpriteData = null;
                currentWeapon = null;
                isReloading = false;
                return;
            }

            currentWeapon = weapon;

            // Находим данные спрайтов для этого оружия
            currentSpriteData = FindSpriteDataForWeapon(weapon);

            if (currentSpriteData == null || !currentSpriteData.IsValid())
            {
                Debug.LogWarning($"WeaponSpriteDisplay: No sprite data found for weapon '{weapon.WeaponName}'. " +
                    $"Available sprite data: {(weaponSpriteData != null ? weaponSpriteData.Length : 0)} entries. " +
                    $"Make sure weaponName in ScriptableObject matches '{weapon.WeaponName}'");
                if (weaponImage != null)
                {
                    weaponImage.sprite = null;
                }
                return;
            }

            Debug.Log($"WeaponSpriteDisplay: Found sprite data for '{weapon.WeaponName}'");

            // Подписываемся на события нового оружия
            SubscribeToCurrentWeaponEvents();

            // Обновляем базовую позицию (на случай если позиция изменилась в редакторе)
            UpdateBasePosition();

            // Возвращаемся к idle анимации
            ReturnToIdle();
        }

        private void OnWeaponFired(Weapon weapon)
        {
            if (weapon != weaponManager?.CurrentWeapon || currentSpriteData == null)
                return;

            if (playFireAnimation && currentSpriteData.fireSprites != null &&
                currentSpriteData.fireSprites.Length > 0)
            {
                currentState = AnimationState.Fire;
                currentFrameIndex = 0;
                animationTimer = 0f;
                isPlayingOneShot = true;
                UpdateSprite(currentSpriteData.fireSprites[0]);
            }

            // Проверяем пустую обойму
            if (weapon.CurrentAmmo == 0 && currentSpriteData.emptySprite != null)
            {
                currentState = AnimationState.Empty;
                UpdateSprite(currentSpriteData.emptySprite);
            }
        }

        private void OnWeaponReloaded(Weapon weapon)
        {
            // Это событие от WeaponManager (вызывается при завершении перезарядки)
            // Используется как fallback, основная синхронизация через OnWeaponReloadStarted
        }

        private void OnWeaponReloadStarted()
        {
            if (currentWeapon == null || currentSpriteData == null)
                return;

            if (playReloadAnimation && currentSpriteData.reloadSprites != null &&
                currentSpriteData.reloadSprites.Length > 0)
            {
                currentState = AnimationState.Reload;
                currentFrameIndex = 0;
                animationTimer = 0f;
                isPlayingOneShot = true;
                isReloading = true;
                reloadStartTime = Time.time;

                // Показываем первый кадр анимации перезарядки
                UpdateSprite(currentSpriteData.reloadSprites[0]);
            }
        }

        private void OnWeaponReloadCompleted()
        {
            if (currentState == AnimationState.Reload)
            {
                // Перезарядка завершена, показываем последний кадр и возвращаемся к idle
                if (currentSpriteData != null && currentSpriteData.reloadSprites != null &&
                    currentSpriteData.reloadSprites.Length > 0)
                {
                    // Показываем последний кадр перед возвратом к idle
                    UpdateSprite(currentSpriteData.reloadSprites[currentSpriteData.reloadSprites.Length - 1]);
                }

                isReloading = false;
                ReturnToIdle();
            }
        }

        #endregion

        #region IUIElement

        public void SetupUI(Image image)
        {
            if (image != null)
            {
                weaponImage = image;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает данные спрайтов вручную
        /// </summary>
        public void SetWeaponSpriteData(WeaponSpriteData[] data)
        {
            weaponSpriteData = data;

            // Обновляем текущее оружие если есть
            if (weaponManager != null && weaponManager.CurrentWeapon != null)
            {
                OnWeaponChanged(weaponManager.CurrentWeapon);
            }
        }

        /// <summary>
        /// Принудительно запускает анимацию выстрела (для тестирования)
        /// </summary>
        public void PlayFireAnimation()
        {
            if (currentSpriteData != null)
            {
                OnWeaponFired(weaponManager?.CurrentWeapon);
            }
        }

        #endregion
    }
}

