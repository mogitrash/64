using UnityEngine;
using WAD64.Core;

namespace WAD64.Player
{
    /// <summary>
    /// Система здоровья игрока с поддержкой:
    /// - Базовое здоровье и броня
    /// - Регенерация здоровья
    /// - Временная неуязвимость после получения урона
    /// - Различные типы урона
    /// - События для интеграции с UI и другими системами
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxArmor = 100f;
        [SerializeField] private float currentArmor = 0f;
        [SerializeField] private float armorAbsorption = 0.5f; // Процент урона, поглощаемого броней

        [Header("Regeneration")]
        [SerializeField] private bool enableHealthRegen = true;
        [SerializeField] private float healthRegenRate = 5f; // HP в секунду
        [SerializeField] private float healthRegenDelay = 3f; // Задержка после получения урона
        [SerializeField] private float maxRegenHealth = 25f; // Максимум, до которого регенерируется здоровье

        [Header("Invincibility")]
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private bool flashOnDamage = true;
        [SerializeField] private float flashDuration = 0.1f;

        [Header("Death Settings")]
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private bool autoRespawn = true;

        [Header("Visual Effects")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color damageFlashColor = Color.red;

        // Components
        private PlayerCamera playerCamera;
        
        // Health state
        private float lastDamageTime;
        private float invincibilityTimer;
        private bool isInvincible;
        private bool isDead;
        
        // Visual effects
        private Coroutine flashCoroutine;
        private Color[] originalColors;
        private Material[] materials;
        
        // Regeneration
        private float regenTimer;

        // Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<float, float> OnArmorChanged;  // current, max
        public System.Action<float> OnDamageTaken;          // damage amount
        public System.Action OnPlayerDied;
        public System.Action OnPlayerRespawned;
        public System.Action OnHealthRegenStarted;
        public System.Action OnHealthRegenStopped;

        // Properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentArmor => currentArmor;
        public float MaxArmor => maxArmor;
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public float ArmorPercent => maxArmor > 0 ? currentArmor / maxArmor : 0f;
        public bool IsDead => isDead;
        public bool IsInvincible => isInvincible;
        public bool IsRegenerating => enableHealthRegen && Time.time - lastDamageTime > healthRegenDelay && currentHealth < maxRegenHealth && !isDead;

        private void Awake()
        {
            playerCamera = GetComponentInChildren<PlayerCamera>();
            
            // Инициализация визуальных эффектов
            InitializeVisualEffects();
            
            // Устанавливаем начальное здоровье
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Уведомляем о начальном состоянии
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        private void Update()
        {
            UpdateInvincibility();
            UpdateRegeneration();
        }

        #region Damage System

        public void TakeDamage(float damage, DamageType damageType = DamageType.Normal, Vector3 damageSource = default)
        {
            if (isDead || isInvincible || damage <= 0f) return;

            // Применяем модификаторы урона
            float finalDamage = CalculateFinalDamage(damage, damageType);
            
            // Распределяем урон между броней и здоровьем
            float armorDamage = 0f;
            float healthDamage = finalDamage;

            if (currentArmor > 0f)
            {
                armorDamage = finalDamage * armorAbsorption;
                healthDamage = finalDamage * (1f - armorAbsorption);
                
                // Применяем урон к броне
                currentArmor = Mathf.Max(0f, currentArmor - armorDamage);
                OnArmorChanged?.Invoke(currentArmor, maxArmor);
            }

            // Применяем урон к здоровью
            currentHealth = Mathf.Max(0f, currentHealth - healthDamage);
            lastDamageTime = Time.time;
            
            // Запускаем неуязвимость
            StartInvincibility();
            
            // Визуальные эффекты
            if (flashOnDamage)
            {
                StartDamageFlash();
            }
            
            // Camera shake
            if (playerCamera != null)
            {
                float shakeIntensity = Mathf.Clamp(finalDamage / 50f, 0.1f, 1f);
                playerCamera.AddCameraShake(shakeIntensity, 0.3f);
            }

            // Уведомляем о получении урона
            OnDamageTaken?.Invoke(finalDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            // Проверяем смерть
            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }

            // Интеграция с UI
            if (CoreReferences.UIManager != null)
            {
                var uiManager = CoreReferences.UIManager as Managers.UIManager;
                if (uiManager != null)
                {
                    uiManager.ShowDamageFlash();
                }
            }
        }

        private float CalculateFinalDamage(float baseDamage, DamageType damageType)
        {
            float multiplier = 1f;
            
            switch (damageType)
            {
                case DamageType.Normal:
                    multiplier = 1f;
                    break;
                case DamageType.Explosion:
                    multiplier = 1.2f;
                    break;
                case DamageType.Fall:
                    multiplier = 0.8f;
                    break;
                case DamageType.Environmental:
                    multiplier = 1.5f;
                    break;
            }
            
            return baseDamage * multiplier;
        }

        #endregion

        #region Healing System

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void AddArmor(float amount)
        {
            if (isDead || amount <= 0f) return;
            
            currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        public void FullHeal()
        {
            currentHealth = maxHealth;
            currentArmor = maxArmor;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        #endregion

        #region Regeneration

        private void UpdateRegeneration()
        {
            if (!enableHealthRegen || isDead || currentHealth >= maxRegenHealth) return;
            
            bool shouldRegen = Time.time - lastDamageTime > healthRegenDelay;
            
            if (shouldRegen && currentHealth < maxRegenHealth)
            {
                if (regenTimer <= 0f)
                {
                    OnHealthRegenStarted?.Invoke();
                    regenTimer = 0.1f; // Интервал регенерации
                }
                
                regenTimer -= Time.deltaTime;
                
                if (regenTimer <= 0f)
                {
                    float regenAmount = healthRegenRate * 0.1f; // 0.1 секунды интервал
                    currentHealth = Mathf.Min(maxRegenHealth, currentHealth + regenAmount);
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    regenTimer = 0.1f;
                }
            }
            else if (regenTimer > 0f)
            {
                OnHealthRegenStopped?.Invoke();
                regenTimer = 0f;
            }
        }

        #endregion

        #region Invincibility

        private void StartInvincibility()
        {
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }

        private void UpdateInvincibility()
        {
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0f)
                {
                    isInvincible = false;
                }
            }
        }

        #endregion

        #region Death & Respawn

        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            currentHealth = 0f;
            
            OnPlayerDied?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            // Уведомляем GameManager
            if (CoreReferences.GameManager != null)
            {
                var gameManager = CoreReferences.GameManager as Managers.GameManager;
                if (gameManager != null)
                {
                    gameManager.OnPlayerDeathHandler();
                }
            }
            
            if (autoRespawn)
            {
                Invoke(nameof(Respawn), respawnDelay);
            }
        }

        public void Respawn()
        {
            isDead = false;
            currentHealth = maxHealth;
            currentArmor = 0f;
            isInvincible = false;
            invincibilityTimer = 0f;
            lastDamageTime = 0f;
            
            OnPlayerRespawned?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        #endregion

        #region Visual Effects

        private void InitializeVisualEffects()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }
            
            if (renderers.Length > 0)
            {
                materials = new Material[renderers.Length];
                originalColors = new Color[renderers.Length];
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].material != null)
                    {
                        materials[i] = renderers[i].material;
                        if (materials[i].HasProperty("_Color"))
                        {
                            originalColors[i] = materials[i].color;
                        }
                    }
                }
            }
        }

        private void StartDamageFlash()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            // Меняем цвет на красный
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].HasProperty("_Color"))
                {
                    materials[i].color = damageFlashColor;
                }
            }
            
            yield return new WaitForSeconds(flashDuration);
            
            // Возвращаем оригинальный цвет
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].HasProperty("_Color"))
                {
                    materials[i].color = originalColors[i];
                }
            }
            
            flashCoroutine = null;
        }

        #endregion

        #region Public Methods

        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxArmor(float newMaxArmor)
        {
            maxArmor = Mathf.Max(0f, newMaxArmor);
            currentArmor = Mathf.Min(currentArmor, maxArmor);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        public bool CanTakeDamage()
        {
            return !isDead && !isInvincible;
        }

        #endregion
    }

    /// <summary>
    /// Типы урона для различных модификаторов
    /// </summary>
    public enum DamageType
    {
        Normal,
        Explosion,
        Fall,
        Environmental
    }
}