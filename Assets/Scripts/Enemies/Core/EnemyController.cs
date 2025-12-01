using UnityEngine;
using WAD64.Core;
using WAD64.Managers;

namespace WAD64.Enemies
{
    /// <summary>
    /// Главный контроллер врага, координирующий работу всех компонентов:
    /// - EnemyAI (FSM логика)
    /// - EnemyMovement (движение)
    /// - EnemyHealth (здоровье)
    /// - EnemyAttack (атака)
    /// 
    /// Служит единой точкой доступа к функциональности врага.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyAI))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyAttack))]

    public class EnemyController : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private float destroyDelay = 2f; // Задержка перед уничтожением GameObject после смерти
        [SerializeField] private bool keepCorpseOnScene = false; // Если true, труп остается на сцене вместо уничтожения

        // Core components
        private CharacterController characterController;
        private EnemyAI enemyAI;
        private EnemyMovement enemyMovement;
        private EnemyHealth enemyHealth;
        private EnemyAttack enemyAttack;

        // State
        private bool isInitialized = false;

        // Events
        public System.Action OnEnemyInitialized;
        public System.Action OnEnemyDied;

        // Properties
        public bool IsInitialized => isInitialized;
        public EnemyAI AI => enemyAI;
        public EnemyMovement Movement => enemyMovement;
        public EnemyHealth Health => enemyHealth;
        public EnemyAttack Attack => enemyAttack;
        public CharacterController Controller => characterController;
        public EnemyState CurrentState => enemyAI != null ? enemyAI.CurrentState : EnemyState.Patrol;
        public bool IsDead => enemyHealth != null && enemyHealth.IsDead;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                CompleteInitialization();
            }
        }

        #region Initialization

        private void InitializeComponents()
        {
            // Получаем все необходимые компоненты
            characterController = GetComponent<CharacterController>();
            enemyAI = GetComponent<EnemyAI>();
            enemyMovement = GetComponent<EnemyMovement>();
            enemyHealth = GetComponent<EnemyHealth>();
            enemyAttack = GetComponent<EnemyAttack>();

            // Подписываемся на события
            if (enemyHealth != null)
            {
                enemyHealth.OnEnemyDied += OnHealthDied;
            }

            if (enemyAI != null)
            {
                enemyAI.OnStateChanged += OnStateChanged;
            }
        }

        private void CompleteInitialization()
        {
            if (isInitialized) return;

            isInitialized = true;
            OnEnemyInitialized?.Invoke();
        }

        #endregion

        #region Event Handlers

        private void OnHealthDied()
        {
            OnEnemyDied?.Invoke();

            // Уведомляем GameManager
            if (CoreReferences.GameManager != null)
            {
                // Передаем тип врага (0 = базовый, можно расширить для разных типов)
                CoreReferences.GameManager.OnEnemyKilledHandler(0);
            }

            // Если опция включена, оставляем труп на сцене
            if (keepCorpseOnScene)
            {
                DisableEnemyComponents();
            }
            else
            {
                // Уничтожаем GameObject после задержки (для проигрывания анимации смерти)
                Destroy(gameObject, destroyDelay);
            }
        }

        private void OnStateChanged(EnemyState newState)
        {
            // Логирование для отладки
#if UNITY_EDITOR
            if (newState == EnemyState.Dead)
            {
                Debug.Log($"[{gameObject.name}] Enemy died");
            }
#endif
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает waypoints для патрулирования
        /// </summary>
        public void SetWaypoints(Transform[] waypoints)
        {
            if (enemyMovement != null)
            {
                enemyMovement.SetWaypoints(waypoints);
            }
        }

        /// <summary>
        /// Устанавливает скорость движения
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            if (enemyMovement != null)
            {
                enemyMovement.SetMoveSpeed(speed);
            }
        }

        /// <summary>
        /// Наносит урон врагу
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }

        /// <summary>
        /// Принудительно устанавливает состояние врага
        /// </summary>
        public void SetState(EnemyState state)
        {
            // Это будет реализовано через EnemyAI, если понадобится
            // Пока оставляем пустым, так как состояние управляется через EnemyAI
        }

        #endregion

        #region Corpse Management

        /// <summary>
        /// Отключает все активные компоненты врага, оставляя только визуальную часть (труп)
        /// </summary>
        private void DisableEnemyComponents()
        {
            // Отключаем основные компоненты
            if (enemyAI != null)
            {
                enemyAI.enabled = false;
            }

            if (enemyMovement != null)
            {
                enemyMovement.enabled = false;
            }

            if (enemyAttack != null)
            {
                enemyAttack.enabled = false;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            // Отключаем компоненты визуализации направления (труп не должен поворачиваться)
            AngleToPlayer angleToPlayer = GetComponent<AngleToPlayer>();
            if (angleToPlayer != null)
            {
                angleToPlayer.enabled = false;
            }

            EnemySpriteLook spriteLook = GetComponent<EnemySpriteLook>();
            if (spriteLook != null)
            {
                spriteLook.enabled = false;
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (enemyHealth != null)
            {
                enemyHealth.OnEnemyDied -= OnHealthDied;
            }

            if (enemyAI != null)
            {
                enemyAI.OnStateChanged -= OnStateChanged;
            }
        }
    }
}

