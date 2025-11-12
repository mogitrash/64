using UnityEngine;

namespace WAD64.Enemies
{
    /// <summary>
    /// Система движения врага. Простое движение без NavMesh.
    /// Поддерживает движение к цели и патрулирование между waypoints.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float acceleration = 10f;

        [Header("Patrol Settings")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointReachDistance = 0.5f;
        [SerializeField] private bool loopPatrol = true;

        [Header("Obstacle Avoidance")]
        [SerializeField] private bool enableObstacleAvoidance = true;
        [SerializeField] private float obstacleCheckDistance = 2f;
        [SerializeField] private float obstacleAvoidanceAngle = 45f;

        [Header("Physics")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundedGravity = -2f;

        private CharacterController controller;
        private Vector3 currentVelocity;
        private Vector3 verticalVelocity; // Вертикальная скорость для гравитации
        private Vector3 targetDirection;
        private int currentWaypointIndex = 0;
        private bool hasTarget = false;
        private Vector3 targetPosition;

        // Properties
        public float MoveSpeed => moveSpeed;
        public float CurrentSpeed => currentVelocity.magnitude;
        public bool IsMoving => currentVelocity.magnitude > 0.1f;
        public bool HasReachedTarget => !hasTarget || Vector3.Distance(transform.position, targetPosition) < waypointReachDistance;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (controller == null) return;

            // Обновляем гравитацию
            UpdateGravity();

            // Комбинируем горизонтальное движение и вертикальную скорость
            Vector3 finalVelocity = currentVelocity + verticalVelocity;

            // Применяем движение
            if (finalVelocity.magnitude > 0.01f)
            {
                controller.Move(finalVelocity * Time.deltaTime);
            }
        }

        private void UpdateGravity()
        {
            // Проверяем, на земле ли враг
            bool isGrounded = controller.isGrounded;

            if (isGrounded)
            {
                // На земле - применяем небольшую гравитацию для стабильности
                if (verticalVelocity.y < 0)
                {
                    verticalVelocity.y = groundedGravity;
                }
            }
            else
            {
                // В воздухе - применяем гравитацию
                verticalVelocity.y += gravity * Time.deltaTime;
            }
        }

        /// <summary>
        /// Движение к указанной позиции
        /// </summary>
        public void MoveTo(Vector3 target)
        {
            hasTarget = true;
            targetPosition = target;

            Vector3 direction = (target - transform.position);
            direction.y = 0f; // Игнорируем вертикальную составляющую

            // НЕ проверяем достижение цели здесь - это создает конфликт с Patrol()
            // Patrol() сам контролирует переключение waypoints

            // Проверка препятствий
            if (enableObstacleAvoidance)
            {
                direction = AvoidObstacles(direction);
            }

            // Нормализуем направление
            targetDirection = direction.normalized;

            // Поворот к цели
            RotateTowards(targetDirection);

            // Ускорение движения
            Vector3 desiredVelocity = targetDirection * moveSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);
        }

        /// <summary>
        /// Патрулирование между waypoints
        /// </summary>
        public void Patrol()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                Stop();
                return;
            }

            Transform currentWaypoint = waypoints[currentWaypointIndex];
            if (currentWaypoint == null)
            {
                Stop();
                return;
            }

            float distance = Vector3.Distance(transform.position, currentWaypoint.position);


            if (distance <= waypointReachDistance)
            {
                MoveToNextWaypoint();

                // Only move to next waypoint if we haven't stopped (i.e., loopPatrol is true or we haven't reached the end)
                if (hasTarget && waypoints != null && waypoints.Length > 0 && currentWaypointIndex < waypoints.Length)
                {
                    Transform nextWaypoint = waypoints[currentWaypointIndex];
                    if (nextWaypoint != null)
                    {
                        MoveTo(nextWaypoint.position);
                    }
                }
            }
            else
            {
                // Движемся к текущему waypoint
                MoveTo(currentWaypoint.position);
            }
        }

        /// <summary>
        /// Остановка движения
        /// </summary>
        public void Stop()
        {
            hasTarget = false;
            currentVelocity = Vector3.zero;
        }

        /// <summary>
        /// Установка скорости движения
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Установка waypoints для патрулирования
        /// </summary>
        public void SetWaypoints(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
            currentWaypointIndex = 0;
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.magnitude < 0.1f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private Vector3 AvoidObstacles(Vector3 direction)
        {
            // Проверяем препятствие впереди
            if (Physics.Raycast(transform.position, direction.normalized, obstacleCheckDistance))
            {
                // Пробуем повернуть влево
                Vector3 leftDirection = Quaternion.Euler(0, -obstacleAvoidanceAngle, 0) * direction;
                if (!Physics.Raycast(transform.position, leftDirection.normalized, obstacleCheckDistance))
                {
                    return leftDirection;
                }

                // Пробуем повернуть вправо
                Vector3 rightDirection = Quaternion.Euler(0, obstacleAvoidanceAngle, 0) * direction;
                if (!Physics.Raycast(transform.position, rightDirection.normalized, obstacleCheckDistance))
                {
                    return rightDirection;
                }
            }

            return direction;
        }

        private void MoveToNextWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                if (loopPatrol)
                {
                    currentWaypointIndex = 0;
                }
                else
                {
                    currentWaypointIndex = waypoints.Length - 1;
                    Stop();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Показываем waypoints
            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                        if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                        }
                        else if (loopPatrol && waypoints[0] != null)
                        {
                            Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
                        }
                    }
                }

                // Выделяем текущий waypoint
                if (currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Length && waypoints[currentWaypointIndex] != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(waypoints[currentWaypointIndex].position, 0.5f);
                }
            }

            // Показываем направление движения
            if (hasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);
            }

            // Показываем проверку препятствий
            if (enableObstacleAvoidance)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, transform.forward * obstacleCheckDistance);
            }
        }
    }
}

