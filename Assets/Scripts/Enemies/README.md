# Enemies - Система ИИ врагов

## Назначение
Содержит реализацию простого ИИ для врагов проекта 64.WAD. Использует паттерн FSM (Finite State Machine) для управления поведением врагов.

## Лучшие практики реализации простого ИИ врагов в Unity

### 1. Архитектурные принципы

#### ✅ **Использование FSM (Finite State Machine)**
- **Почему**: Простая, понятная логика, легко расширяемая
- **Реализация**: Enum для состояний + switch/case или отдельные классы состояний
- **Состояния для проекта**: `Patrol` → `Aggro` → `Attack`

#### ✅ **Component-Based Architecture**
- Разделение ответственности:
  - `EnemyController` - главный контроллер, координирует все системы
  - `EnemyMovement` - движение и навигация
  - `EnemyHealth` - здоровье и урон (реализует `IDamageable`)
  - `EnemyAI` - логика состояний FSM
  - `EnemyAttack` - логика атак

#### ✅ **Простота превыше всего**
- Избегайте сложных алгоритмов (A*, pathfinding)
- Используйте простые проверки расстояния и направления
- Для навигации: `Transform.LookAt()` + движение к цели
- Для обхода препятствий: простые raycasts или Physics.OverlapSphere

### 2. Паттерны реализации

#### **Паттерн 1: Enum-based FSM (рекомендуется для простых случаев)**
```csharp
public enum EnemyState
{
    Patrol,
    Aggro,
    Attack,
    Dead
}

private EnemyState currentState;

private void Update()
{
    switch (currentState)
    {
        case EnemyState.Patrol:
            UpdatePatrol();
            break;
        case EnemyState.Aggro:
            UpdateAggro();
            break;
        // ...
    }
}
```

**Преимущества:**
- Простота реализации
- Легко понять и отладить
- Минимум кода

**Недостатки:**
- Может стать громоздким при большом количестве состояний
- Сложнее расширять

#### **Паттерн 2: State Pattern (для более сложных случаев)**
```csharp
public abstract class EnemyState
{
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

public class PatrolState : EnemyState { }
public class AggroState : EnemyState { }
```

**Преимущества:**
- Легко расширять новыми состояниями
- Чистая архитектура
- Каждое состояние изолировано

**Недостатки:**
- Больше кода
- Может быть избыточно для простых случаев

### 3. Обнаружение игрока

#### ✅ **Эффективные методы обнаружения**

**1. SphereCast / OverlapSphere (рекомендуется)**
```csharp
Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
if (hits.Length > 0)
{
    // Игрок обнаружен
}
```

**2. Raycast (для прямой видимости)**
```csharp
if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange))
{
    if (hit.collider.CompareTag("Player"))
    {
        // Игрок виден
    }
}
```

**3. Оптимизация:**
- Проверяйте обнаружение не каждый кадр, а раз в 0.1-0.5 секунды
- Используйте `InvokeRepeating` или таймеры
- Кешируйте ссылку на игрока через `CoreReferences.Player`

### 4. Движение и навигация

#### ✅ **Простое движение без NavMesh**

**Для проекта 64.WAD (без NavMesh):**

```csharp
// Простое движение к цели
Vector3 direction = (target.position - transform.position).normalized;
transform.LookAt(target);
characterController.Move(direction * speed * Time.deltaTime);
```

**Обход препятствий:**
```csharp
// Простая проверка препятствий
if (Physics.Raycast(transform.position, transform.forward, 2f))
{
    // Повернуть в сторону
    transform.Rotate(0, 90f, 0);
}
```

**Патрулирование:**
```csharp
// Движение между точками
if (Vector3.Distance(transform.position, currentWaypoint) < 0.5f)
{
    currentWaypoint = GetNextWaypoint();
}
```

### 5. Оптимизация производительности

#### ✅ **Критические оптимизации**

**1. Обновление не каждый кадр:**
```csharp
private float updateInterval = 0.1f; // Обновлять 10 раз в секунду
private float lastUpdateTime;

private void Update()
{
    if (Time.time - lastUpdateTime < updateInterval) return;
    lastUpdateTime = Time.time;
    
    // Логика ИИ
}
```

**2. Кеширование компонентов:**
```csharp
private CharacterController controller;
private Transform playerTransform;

private void Awake()
{
    controller = GetComponent<CharacterController>();
    playerTransform = CoreReferences.Player?.transform;
}
```

**3. Использование слоев (Layers):**
- Создайте отдельный слой для врагов
- Используйте LayerMask для фильтрации проверок
- Уменьшает количество проверяемых объектов

**4. Object Pooling (для будущего):**
- Переиспользование врагов вместо создания/уничтожения
- Особенно важно для Runner и Exploder

### 6. Типы врагов проекта

#### **Walker (Медленный, устойчивый)**
- **Скорость**: Низкая (2-3 м/с)
- **Здоровье**: Высокое (100-150 HP)
- **Поведение**: Медленное патрулирование, агрессивное преследование
- **Атака**: Медленная, но мощная

#### **Runner (Быстрый, слабый)**
- **Скорость**: Высокая (6-8 м/с)
- **Здоровье**: Низкое (30-50 HP)
- **Поведение**: Быстрое патрулирование, быстрое преследование
- **Атака**: Быстрая, но слабая

#### **Exploder (Взрывающийся)**
- **Скорость**: Средняя (4-5 м/с)
- **Здоровье**: Среднее (60-80 HP)
- **Поведение**: Агрессивное преследование, взрыв при смерти
- **Атака**: Взрыв при приближении или смерти

### 7. Интеграция с существующими системами

#### ✅ **Использование CoreReferences**
```csharp
// Получение ссылки на игрока
var player = CoreReferences.Player;
if (player != null)
{
    float distance = Vector3.Distance(transform.position, player.transform.position);
    // Логика обнаружения
}
```

#### ✅ **Реализация IDamageable**
```csharp
public class EnemyHealth : MonoBehaviour, IDamageable
{
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
}
```

#### ✅ **События для координации**
```csharp
public System.Action OnEnemyDied;
public System.Action<float> OnHealthChanged;

// Уведомление GameManager
OnEnemyDied?.Invoke();
if (CoreReferences.GameManager != null)
{
    CoreReferences.GameManager.OnEnemyKilled();
}
```

### 8. Отладка и визуализация

#### ✅ **Gizmos для отладки**
```csharp
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
}
```

#### ✅ **Debug логирование**
```csharp
#if UNITY_EDITOR
Debug.Log($"[{gameObject.name}] State: {currentState}, Health: {currentHealth}");
#endif
```

### 9. Структура файлов

```
Assets/Scripts/Enemies/
├── Core/
│   ├── EnemyController.cs          # Главный контроллер
│   ├── EnemyAI.cs                   # FSM логика
│   ├── EnemyMovement.cs             # Движение
│   ├── EnemyHealth.cs                # Здоровье (IDamageable)
│   └── EnemyAttack.cs                # Атаки
├── Types/
│   ├── Walker/
│   │   └── WalkerEnemy.cs           # Специфичная логика Walker
│   ├── Runner/
│   │   └── RunnerEnemy.cs           # Специфичная логика Runner
│   └── Exploder/
│       └── ExploderEnemy.cs          # Специфичная логика Exploder
└── README.md                         # Этот файл
```

### 10. Чеклист реализации

- [ ] Создать базовый `EnemyController` с FSM
- [ ] Реализовать состояния: Patrol, Aggro, Attack
- [ ] Добавить `EnemyHealth` с `IDamageable`
- [ ] Реализовать простое движение к цели
- [ ] Добавить обнаружение игрока (OverlapSphere)
- [ ] Реализовать патрулирование (waypoints)
- [ ] Добавить логику атаки
- [ ] Интегрировать с `CoreReferences`
- [ ] Добавить Gizmos для отладки
- [ ] Оптимизировать обновления (не каждый кадр)
- [ ] Реализовать Walker, Runner, Exploder

### 11. Рекомендации по коду

#### ✅ **DO (Делайте)**
- Используйте простые проверки расстояния
- Кешируйте ссылки на компоненты
- Разделяйте логику на отдельные методы
- Используйте события для слабой связанности
- Проверяйте `Application.isPlaying` в редакторе

#### ❌ **DON'T (Не делайте)**
- Не используйте NavMesh для простых случаев
- Не обновляйте ИИ каждый кадр
- Не создавайте сложные алгоритмы pathfinding
- Не делайте прямые зависимости между врагами
- Не забывайте проверять на null

### 12. Дополнительные ресурсы

- **Unity Manual**: AI Navigation (если понадобится NavMesh)
- **Unity Learn**: Creating AI for Games
- **Best Practices**: Keep it simple, test frequently, iterate

## Примечания

- Для проекта 64.WAD используется **простая FSM без NavMesh**
- Фокус на **производительности** и **простоте поддержки**
- Архитектура позволяет легко добавлять новые типы врагов
- Все враги должны реализовывать `IDamageable` для совместимости с оружием
