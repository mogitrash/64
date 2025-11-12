using UnityEngine;
using WAD64.Core;
using WAD64.Player;

namespace WAD64.Enemies
{
    /// <summary>
    /// Система атаки врага. Наносит урон игроку при приближении.
    /// </summary>
    public class EnemyAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackDuration = 0.5f; // Длительность атаки

        [Header("Attack Detection")]
        [SerializeField] private bool useSphereCast = true;
        [SerializeField] private float attackSphereRadius = 0.5f;

        private EnemyAI enemyAI;
        private float lastAttackTime;
        private bool isAttacking = false;
        private float attackTimer;

        // Events
        public System.Action OnAttackPerformed;
        public System.Action OnAttackStarted;
        public System.Action OnAttackEnded;

        // Properties
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public bool CanAttack => Time.time - lastAttackTime >= attackCooldown && !isAttacking;
        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();
        }

        private void Update()
        {
            // Обновляем таймер атаки
            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    EndAttack();
                }
            }

            // Проверяем возможность атаки
            if (enemyAI != null && enemyAI.CurrentState == EnemyState.Attack)
            {
                if (CanAttack && IsPlayerInRange())
                {
                    PerformAttack();
                }
            }
        }

        /// <summary>
        /// Выполняет атаку
        /// </summary>
        public void PerformAttack()
        {
            if (!CanAttack) return;

            StartAttack();
            lastAttackTime = Time.time;

            // Наносим урон игроку
            if (TryDamagePlayer())
            {
                OnAttackPerformed?.Invoke();
            }
        }

        private void StartAttack()
        {
            isAttacking = true;
            attackTimer = attackDuration;
            OnAttackStarted?.Invoke();
        }

        private void EndAttack()
        {
            isAttacking = false;
            attackTimer = 0f;
            OnAttackEnded?.Invoke();
        }

        private bool IsPlayerInRange()
        {
            if (CoreReferences.Player == null) return false;

            float distance = Vector3.Distance(transform.position, CoreReferences.Player.transform.position);
            return distance <= attackRange;
        }

        private bool TryDamagePlayer()
        {
            if (CoreReferences.Player == null) return false;

            PlayerController player = CoreReferences.Player as PlayerController;
            if (player == null || player.Health == null) return false;

            // Проверяем, что игрок действительно в радиусе атаки
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > attackRange) return false;

            // Проверка прямой видимости (опционально)
            if (useSphereCast)
            {
                Vector3 direction = (player.transform.position - transform.position).normalized;
                if (Physics.SphereCast(transform.position, attackSphereRadius, direction, out RaycastHit hit, attackRange))
                {
                    // Проверяем, что попали в игрока
                    if (hit.collider.GetComponent<PlayerController>() == null)
                    {
                        return false; // Препятствие между врагом и игроком
                    }
                }
            }

            // Наносим урон
            player.Health.TakeDamage(attackDamage);
            return true;
        }

        /// <summary>
        /// Устанавливает урон атаки
        /// </summary>
        public void SetAttackDamage(float damage)
        {
            attackDamage = Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Устанавливает cooldown атаки
        /// </summary>
        public void SetAttackCooldown(float cooldown)
        {
            attackCooldown = Mathf.Max(0f, cooldown);
        }

        private void OnDrawGizmosSelected()
        {
            // Радиус атаки
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Направление атаки
            if (CoreReferences.Player != null)
            {
                Vector3 direction = (CoreReferences.Player.transform.position - transform.position).normalized;
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, direction * attackRange);

                if (useSphereCast)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(transform.position + direction * attackRange, attackSphereRadius);
                }
            }
        }
    }
}

