using UnityEngine;

namespace WAD64.Weapons
{
    /// <summary>
    /// Базовый класс для всех видов оружия в игре.
    /// Определяет основные механики стрельбы, перезарядки и управления боеприпасами.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] protected string weaponName = "Base Weapon";
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float range = 100f;
        [SerializeField] protected float fireRate = 1f; // выстрелов в секунду
        [SerializeField] protected float reloadTime = 2f;

        [Header("Ammo")]
        [SerializeField] protected int maxAmmo = 12;
        [SerializeField] protected int currentAmmo = 12;

        [Header("Accuracy")]
        [SerializeField] protected float spread = 0.02f; // разброс в радианах
        [SerializeField] protected LayerMask hitLayers = ~0;

        [Header("Shotgun Settings")]
        [SerializeField] protected int pelletCount = 1; // Количество дробин (1 = обычный выстрел, >1 = дробовик)
        [SerializeField] protected float pelletSpread = 0.15f; // Разброс дробин в радианах

        [Header("Sprite Animation")]
        [Tooltip("Данные спрайтов для анимации оружия (прикрепляется к префабу)")]
        [SerializeField] protected WAD64.UI.WeaponSpriteData spriteData;

        [Header("Effects")]
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected AudioClip fireSound;
        [SerializeField] protected AudioClip reloadSound;
        [SerializeField] protected AudioClip emptySound;

        [Header("Debug Visualization")]
        [SerializeField] protected bool showHitMarkers = true;
        [SerializeField] protected Color hitMarkerColor = Color.red;
        [SerializeField] protected float hitMarkerSize = 0.1f;
        [SerializeField] protected float hitMarkerDuration = 2f;
        [SerializeField] protected bool showTrajectories = true;
        [SerializeField] protected Color trajectoryColor = Color.yellow;
        [SerializeField] protected float trajectoryDuration = 0.5f;

        // State
        protected bool isReloading = false;
        protected float lastFireTime = 0f;
        protected float reloadStartTime = 0f;

        // Components
        protected Camera playerCamera;
        protected AudioSource audioSource;

        // Events
        public System.Action<int, int> OnAmmoChanged; // current, max
        public System.Action OnWeaponFired;
        public System.Action OnReloadStarted;
        public System.Action OnReloadCompleted;
        public System.Action OnEmptyClip;

        // Properties
        public string WeaponName => weaponName;
        public float Damage => damage;
        public float Range => range;
        public float FireRate => fireRate;
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool IsReloading => isReloading;
        public bool CanFire => !isReloading && currentAmmo > 0 && Time.time >= lastFireTime + (1f / fireRate);
        public bool NeedsReload => currentAmmo == 0;
        public float ReloadProgress => isReloading ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime) : 0f;
        public WAD64.UI.WeaponSpriteData SpriteData => spriteData;

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        protected virtual void Start()
        {
            // Получаем камеру игрока
            var playerCamera = WAD64.Core.CoreReferences.MainCamera;
            if (playerCamera != null)
                this.playerCamera = playerCamera;

            // Инициализируем состояние
            currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        protected virtual void Update()
        {
            if (isReloading)
            {
                UpdateReloading();
            }
        }

        /// <summary>
        /// Попытка выстрела
        /// </summary>
        public virtual bool TryFire()
        {
            if (!CanFire)
            {
                if (currentAmmo == 0)
                {
                    PlayEmptySound();
                    OnEmptyClip?.Invoke();
                }
                return false;
            }

            Fire();
            return true;
        }

        /// <summary>
        /// Выполняет выстрел
        /// </summary>
        protected virtual void Fire()
        {
            // Уменьшаем боеприпасы
            currentAmmo--;
            lastFireTime = Time.time;

            // Рассчитываем направление с разбросом
            Vector3 fireDirection = CalculateFireDirection();

            // Выполняем конкретную логику стрельбы
            PerformShot(fireDirection);

            // Эффекты
            PlayFireEffects();

            // События
            OnWeaponFired?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        /// <summary>
        /// Рассчитывает направление выстрела с учетом разброса
        /// </summary>
        protected virtual Vector3 CalculateFireDirection()
        {
            if (playerCamera == null)
                return transform.forward;

            Vector3 baseDirection = playerCamera.transform.forward;

            // Добавляем случайный разброс
            float randomX = Random.Range(-spread, spread);
            float randomY = Random.Range(-spread, spread);

            Vector3 spreadOffset = playerCamera.transform.right * randomX +
                                 playerCamera.transform.up * randomY;

            return (baseDirection + spreadOffset).normalized;
        }

        /// <summary>
        /// Выполняет конкретную логику выстрела (переопределяется в наследниках)
        /// Базовая реализация - hitscan выстрел с нанесением урона
        /// Поддерживает как обычные выстрелы, так и дробовик (множественные дробинки)
        /// </summary>
        protected virtual void PerformShot(Vector3 direction)
        {
            if (playerCamera == null) return;

            Vector3 origin = playerCamera.transform.position;

            // Если это дробовик (pelletCount > 1), стреляем несколькими дробинками
            if (pelletCount > 1)
            {
                PerformShotgunShot(origin, direction);
            }
            else
            {
                // Обычный одиночный выстрел
                PerformSingleShot(origin, direction);
            }
        }

        /// <summary>
        /// Выполняет одиночный выстрел
        /// </summary>
        protected virtual void PerformSingleShot(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;

            if (Physics.Raycast(origin, direction, out hit, range, hitLayers))
            {
                // Визуализация траектории
                if (showTrajectories)
                {
                    Debug.DrawRay(origin, direction * hit.distance, trajectoryColor, trajectoryDuration);
                }

                // Визуализация точки попадания
                if (showHitMarkers)
                {
                    CreateHitMarker(hit.point, hit.normal);
                }

                // Пытаемся нанести урон через IDamageable
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
            }
            else
            {
                // Визуализация промаха (линия до максимальной дальности)
                if (showTrajectories)
                {
                    Debug.DrawRay(origin, direction * range, Color.gray, trajectoryDuration);
                }
            }
        }

        /// <summary>
        /// Выполняет выстрел дробовика (множественные дробинки)
        /// </summary>
        protected virtual void PerformShotgunShot(Vector3 origin, Vector3 direction)
        {
            // Стреляем несколькими дробинками с разбросом
            for (int i = 0; i < pelletCount; i++)
            {
                // Рассчитываем направление для каждой дроби с разбросом
                Vector3 pelletDirection = CalculatePelletDirection(direction);

                RaycastHit hit;
                if (Physics.Raycast(origin, pelletDirection, out hit, range, hitLayers))
                {
                    // Визуализация траектории каждой дроби
                    if (showTrajectories)
                    {
                        Debug.DrawRay(origin, pelletDirection * hit.distance, trajectoryColor, trajectoryDuration);
                    }

                    // Визуализация точки попадания
                    if (showHitMarkers)
                    {
                        CreateHitMarker(hit.point, hit.normal);
                    }

                    // Пытаемся нанести урон через IDamageable
                    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        // Каждая дробинка наносит полный урон (можно изменить на damage / pelletCount)
                        damageable.TakeDamage(damage);
                    }
                }
                else
                {
                    // Визуализация промаха дроби
                    if (showTrajectories)
                    {
                        Debug.DrawRay(origin, pelletDirection * range, Color.gray, trajectoryDuration);
                    }
                }
            }
        }

        /// <summary>
        /// Рассчитывает направление для одной дроби с учетом разброса
        /// Использует простой метод со случайными смещениями по X и Y
        /// </summary>
        protected virtual Vector3 CalculatePelletDirection(Vector3 baseDirection)
        {
            if (playerCamera == null)
                return baseDirection;

            // Добавляем случайный разброс специально для дроби
            // Используем простой метод: случайные смещения по осям камеры
            float randomX = Random.Range(-pelletSpread, pelletSpread);
            float randomY = Random.Range(-pelletSpread, pelletSpread);

            Vector3 spreadOffset = playerCamera.transform.right * randomX +
                                 playerCamera.transform.up * randomY;

            return (baseDirection + spreadOffset).normalized;
        }

        /// <summary>
        /// Создает временный маркер попадания для визуализации
        /// </summary>
        protected virtual void CreateHitMarker(Vector3 position, Vector3 normal)
        {
            // Создаем временный GameObject со сферой
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "HitMarker";
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * hitMarkerSize;

            // Удаляем коллайдер, чтобы не мешал
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            // Настраиваем материал
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = hitMarkerColor;
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0.3f);
                renderer.material = mat;
            }

            // Добавляем компонент для автоматического удаления с плавным исчезновением
            AutoDestroyMarker autoDestroy = marker.AddComponent<AutoDestroyMarker>();
            autoDestroy.duration = hitMarkerDuration;
        }

        /// <summary>
        /// Начинает перезарядку
        /// </summary>
        public virtual bool TryReload()
        {
            if (isReloading || currentAmmo == maxAmmo)
                return false;

            StartReload();
            return true;
        }

        protected virtual void StartReload()
        {
            isReloading = true;
            reloadStartTime = Time.time;

            PlayReloadSound();
            OnReloadStarted?.Invoke();
        }

        protected virtual void UpdateReloading()
        {
            if (Time.time >= reloadStartTime + reloadTime)
            {
                CompleteReload();
            }
        }

        protected virtual void CompleteReload()
        {
            isReloading = false;
            currentAmmo = maxAmmo;

            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        /// <summary>
        /// Принудительно перезаряжает оружие (для дебага)
        /// </summary>
        public virtual void ForceReload()
        {
            if (isReloading) return;

            currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        #region Audio & Effects

        protected virtual void PlayFireEffects()
        {
            // Вспышка
            if (muzzleFlash != null)
                muzzleFlash.Play();

            // Звук
            if (fireSound != null && audioSource != null)
                audioSource.PlayOneShot(fireSound);
        }

        protected virtual void PlayReloadSound()
        {
            if (reloadSound != null && audioSource != null)
                audioSource.PlayOneShot(reloadSound);
        }

        protected virtual void PlayEmptySound()
        {
            if (emptySound != null && audioSource != null)
                audioSource.PlayOneShot(emptySound);
        }

        #endregion

        #region Debug

        /// <summary>
        /// Получает отладочную информацию об оружии
        /// </summary>
        public virtual string GetDebugInfo()
        {
            return $"{weaponName}:\n" +
                   $"  Ammo: {currentAmmo}/{maxAmmo}\n" +
                   $"  Damage: {damage}\n" +
                   $"  Range: {range}m\n" +
                   $"  Fire Rate: {fireRate}/sec\n" +
                   $"  Reloading: {isReloading} ({ReloadProgress:P0})\n" +
                   $"  Can Fire: {CanFire}";
        }

        #endregion
    }

    /// <summary>
    /// Компонент для автоматического удаления маркеров попадания с плавным исчезновением
    /// </summary>
    public class AutoDestroyMarker : MonoBehaviour
    {
        public float duration = 1f;
        private float timer;
        private Vector3 initialScale;

        private void Start()
        {
            timer = duration;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            timer -= Time.deltaTime;

            // Плавное исчезновение в последние 0.2 секунды
            if (timer <= 0.2f && timer > 0f)
            {
                float alpha = timer / 0.2f;
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }

                // Уменьшаем размер
                transform.localScale = initialScale * alpha;
            }

            if (timer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}