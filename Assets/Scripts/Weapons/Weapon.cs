using UnityEngine;

namespace WAD64.Weapons
{
    /// <summary>
    /// Базовый класс для всех видов оружия в игре.
    /// Определяет основные механики стрельбы, перезарядки и управления боеприпасами.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
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

        [Header("Effects")]
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected AudioClip fireSound;
        [SerializeField] protected AudioClip reloadSound;
        [SerializeField] protected AudioClip emptySound;

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
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool IsReloading => isReloading;
        public bool CanFire => !isReloading && currentAmmo > 0 && Time.time >= lastFireTime + (1f / fireRate);
        public bool NeedsReload => currentAmmo == 0;
        public float ReloadProgress => isReloading ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime) : 0f;

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
        /// </summary>
        protected abstract void PerformShot(Vector3 direction);

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
}