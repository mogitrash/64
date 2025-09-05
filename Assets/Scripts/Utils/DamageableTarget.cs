using UnityEngine;
using WAD64.Weapons;

namespace WAD64.Utils
{
    /// <summary>
    /// Простая мишень для тестирования системы оружия.
    /// Реализует интерфейс IDamageable и отображает полученный урон.
    /// </summary>
    public class DamageableTarget : MonoBehaviour, IDamageable
    {
        [Header("Target Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private float damageFlashDuration = 0.2f;

        private float currentHealth;
        private Renderer targetRenderer;
        private Color originalColor;
        private float damageFlashTimer;

        // Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnDestroyed;

        // Properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => currentHealth <= 0f;

        private void Awake()
        {
            currentHealth = maxHealth;
            targetRenderer = GetComponent<Renderer>();

            if (targetRenderer != null)
            {
                originalColor = targetRenderer.material.color;
            }
        }

        private void Update()
        {
            // Обновляем визуальные эффекты урона
            if (damageFlashTimer > 0f)
            {
                damageFlashTimer -= Time.deltaTime;

                if (targetRenderer != null)
                {
                    float flashIntensity = damageFlashTimer / damageFlashDuration;
                    Color currentColor = Color.Lerp(originalColor, damageColor, flashIntensity);
                    targetRenderer.material.color = currentColor;
                }

                if (damageFlashTimer <= 0f && targetRenderer != null)
                {
                    targetRenderer.material.color = originalColor;
                }
            }
        }

        /// <summary>
        /// Реализация интерфейса IDamageable
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);

            // Визуальные эффекты
            TriggerDamageFlash();


            // События
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Проверяем уничтожение
            if (currentHealth <= 0f && oldHealth > 0f)
            {
                HandleDestruction();
            }
        }

        private void TriggerDamageFlash()
        {
            damageFlashTimer = damageFlashDuration;
        }

        private void HandleDestruction()
        {

            OnDestroyed?.Invoke();

            // Можно добавить эффекты разрушения
            // Пока что просто деактивируем объект
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Восстанавливает здоровье мишени
        /// </summary>
        public void RestoreHealth(float amount = 0f)
        {
            if (amount <= 0f)
                amount = maxHealth;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

        }

        /// <summary>
        /// Полностью восстанавливает мишень
        /// </summary>
        [ContextMenu("Restore Target")]
        public void RestoreTarget()
        {
            RestoreHealth(maxHealth);
        }

        private void OnDrawGizmosSelected()
        {
            // Показываем информацию о здоровье
            Gizmos.color = IsDestroyed ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);

            // Процент здоровья как полоска
            if (!IsDestroyed)
            {
                float healthPercent = currentHealth / maxHealth;
                Vector3 barStart = transform.position + Vector3.up * 2.5f + Vector3.left * 0.5f;
                Vector3 barEnd = barStart + Vector3.right * healthPercent;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(barStart, barStart + Vector3.right);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(barStart, barEnd);
            }
        }
    }
}