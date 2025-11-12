using UnityEngine;
using WAD64.Weapons;

namespace WAD64.Enemies
{
    /// <summary>
    /// Система здоровья врага. Реализует интерфейс IDamageable для получения урона от оружия.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;
        private bool isDead;

        // Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnEnemyDied;

        // Properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Реализация интерфейса IDamageable. Вызывается при получении урона.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead || damage <= 0f) return;

            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);

            // События
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Проверяем смерть
            if (currentHealth <= 0f && oldHealth > 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            currentHealth = 0f;

            OnEnemyDied?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Восстанавливает здоровье врага (для тестирования или респавна)
        /// </summary>
        public void RestoreHealth(float amount = 0f)
        {
            if (amount <= 0f)
                amount = maxHealth;

            isDead = false;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnDrawGizmosSelected()
        {
            // Показываем информацию о здоровье
            Gizmos.color = isDead ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);

            // Процент здоровья как полоска
            if (!isDead && maxHealth > 0)
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

