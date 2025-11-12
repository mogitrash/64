using UnityEngine;
using WAD64.Core;

namespace WAD64.Enemies
{
  /// <summary>
  /// Состояния врага для FSM
  /// </summary>
  public enum EnemyState
  {
    Patrol,    // Патрулирование между точками
    Aggro,     // Преследование игрока
    Attack,    // Атака игрока
    Dead       // Смерть
  }

  /// <summary>
  /// Система ИИ врага с простой FSM (Finite State Machine).
  /// Управляет состояниями: Patrol → Aggro → Attack → Dead
  /// </summary>
  public class EnemyAI : MonoBehaviour
  {
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerLayer = 1; // Default layer
    [SerializeField] private float detectionUpdateInterval = 0.2f; // Оптимизация: проверка не каждый кадр

    [Header("State Settings")]
    [SerializeField] private float aggroLoseDistance = 15f; // Дистанция, на которой теряем агро
    [SerializeField] private float attackCooldown = 1f;

    private EnemyMovement enemyMovement;
    private EnemyHealth enemyHealth;
    private EnemyState currentState = EnemyState.Patrol;
    private Transform playerTransform;
    private float lastDetectionUpdate;
    private float lastAttackTime;
    private bool playerDetected = false;

    // Events
    public System.Action<EnemyState> OnStateChanged;

    // Properties
    public EnemyState CurrentState => currentState;
    public bool IsPlayerDetected => playerDetected;
    public float DistanceToPlayer => playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;

    private void Awake()
    {
      enemyMovement = GetComponent<EnemyMovement>();
      enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
      // Получаем ссылку на игрока
      if (CoreReferences.Player != null)
      {
        playerTransform = CoreReferences.Player.transform;
      }

      // Подписываемся на события здоровья
      if (enemyHealth != null)
      {
        enemyHealth.OnEnemyDied += OnEnemyDied;
      }

      // Устанавливаем начальное состояние
      SetState(EnemyState.Patrol);
    }

    private void Update()
    {
      // Обновляем обнаружение игрока с интервалом (оптимизация)
      if (Time.time - lastDetectionUpdate >= detectionUpdateInterval)
      {
        UpdatePlayerDetection();
        lastDetectionUpdate = Time.time;
      }

      // Обновляем текущее состояние
      UpdateState();
    }

    private void UpdatePlayerDetection()
    {
      // Проверяем наличие игрока через CoreReferences
      if (playerTransform == null && CoreReferences.Player != null)
      {
        playerTransform = CoreReferences.Player.transform;
      }

      if (playerTransform == null)
      {
        playerDetected = false;
        return;
      }

      float distance = Vector3.Distance(transform.position, playerTransform.position);

      // Используем гистерезис: разные пороги для входа и выхода из состояния Aggro
      if (currentState == EnemyState.Patrol)
      {
        // В Patrol: используем detectionRadius для обнаружения (вход в Aggro)
        // Проверяем и через OverlapSphere, и по расстоянию для надежности
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        playerDetected = hits.Length > 0 && distance <= detectionRadius;
      }
      else if (currentState == EnemyState.Aggro || currentState == EnemyState.Attack)
      {
        // В Aggro/Attack: используем aggroLoseDistance для потери агро (выход из Aggro)
        // Игрок считается обнаруженным, если он ближе aggroLoseDistance
        playerDetected = distance <= aggroLoseDistance;
      }
    }

    private void UpdateState()
    {
      switch (currentState)
      {
        case EnemyState.Patrol:
          UpdatePatrol();
          break;
        case EnemyState.Aggro:
          UpdateAggro();
          break;
        case EnemyState.Attack:
          UpdateAttack();
          break;
        case EnemyState.Dead:
          // В состоянии Dead ничего не делаем
          break;
      }
    }

    private void UpdatePatrol()
    {
      // Проверяем расстояние напрямую для более стабильной работы
      // Используем detectionRadius для входа в Aggro
      if (playerTransform != null)
      {
        float distance = DistanceToPlayer;
        // Дополнительная проверка: игрок должен быть в радиусе обнаружения
        if (playerDetected && distance <= detectionRadius)
        {
          SetState(EnemyState.Aggro);
          return;
        }
      }

      // Продолжаем патрулирование
      if (enemyMovement != null)
      {
        enemyMovement.Patrol();
      }
    }

    private void UpdateAggro()
    {
      if (playerTransform == null)
      {
        SetState(EnemyState.Patrol);
        return;
      }

      float distance = DistanceToPlayer;

      // Если игрок слишком далеко (используем aggroLoseDistance с небольшим запасом для стабильности)
      // Проверяем расстояние напрямую, чтобы избежать рассинхронизации с playerDetected
      if (distance > aggroLoseDistance)
      {
        SetState(EnemyState.Patrol);
        return;
      }

      // Если игрок в радиусе атаки, переходим в состояние Attack
      if (distance <= attackRange)
      {
        SetState(EnemyState.Attack);
        return;
      }

      // Движемся к игроку
      if (enemyMovement != null)
      {
        enemyMovement.MoveTo(playerTransform.position);
      }
    }

    private void UpdateAttack()
    {
      if (playerTransform == null)
      {
        SetState(EnemyState.Patrol);
        return;
      }

      float distance = DistanceToPlayer;

      // Если игрок слишком далеко, переходим в Aggro
      if (distance > attackRange * 1.5f)
      {
        SetState(EnemyState.Aggro);
        return;
      }

      // Если игрок вне радиуса атаки, но близко, продолжаем преследование
      if (distance > attackRange)
      {
        if (enemyMovement != null)
        {
          enemyMovement.MoveTo(playerTransform.position);
        }
        return;
      }

      // Останавливаемся для атаки
      if (enemyMovement != null)
      {
        enemyMovement.Stop();
      }

      // Поворачиваемся к игроку
      if (playerTransform != null)
      {
        Vector3 direction = (playerTransform.position - transform.position);
        direction.y = 0f;
        if (direction.magnitude > 0.1f)
        {
          transform.rotation = Quaternion.LookRotation(direction);
        }
      }

      // Атакуем (логика атаки будет в EnemyAttack)
      // Здесь только проверяем cooldown
      if (Time.time - lastAttackTime >= attackCooldown)
      {
        lastAttackTime = Time.time;
        // Атака будет вызвана через EnemyAttack компонент
      }
    }

    private void SetState(EnemyState newState)
    {
      if (currentState == newState) return;

      EnemyState oldState = currentState;

      // Выход из старого состояния
      ExitState(oldState);

      // Обновляем состояние
      currentState = newState;

      // Вход в новое состояние
      EnterState(newState);

      // Уведомляем слушателей после полного завершения перехода
      OnStateChanged?.Invoke(newState);
    }

    private void EnterState(EnemyState state)
    {
      switch (state)
      {
        case EnemyState.Patrol:
          // Начинаем патрулирование
          break;
        case EnemyState.Aggro:
          // Начинаем преследование
          break;
        case EnemyState.Attack:
          // Готовимся к атаке
          lastAttackTime = 0f; // Сбрасываем cooldown при входе в атаку
          break;
        case EnemyState.Dead:
          // Останавливаем движение
          if (enemyMovement != null)
          {
            enemyMovement.Stop();
          }
          break;
      }
    }

    private void ExitState(EnemyState state)
    {
      switch (state)
      {
        case EnemyState.Patrol:
          // Останавливаем патрулирование
          break;
        case EnemyState.Aggro:
          // Останавливаем преследование
          break;
        case EnemyState.Attack:
          // Завершаем атаку
          break;
      }
    }

    private void OnEnemyDied()
    {
      SetState(EnemyState.Dead);
    }

    private void OnDrawGizmosSelected()
    {
      // Радиус обнаружения
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, detectionRadius);

      // Радиус атаки
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, attackRange);

      // Направление к игроку
      if (playerTransform != null)
      {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, playerTransform.position);
      }

      // Текущее состояние
#if UNITY_EDITOR
      UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, $"State: {currentState}");
#endif
    }
  }
}

