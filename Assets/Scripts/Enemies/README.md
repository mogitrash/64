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
  - `EnemyAI` - логика состояний FSM (Patrol → Aggro → Attack → Dead)
  - `EnemyMovement` - движение и навигация (CharacterController, патрулирование, обход препятствий)
  - `EnemyHealth` - здоровье и урон (реализует `IDamageable`)
  - `EnemyAttack` - логика атак игрока
  - `AngleToPlayer` - вычисление угла к игроку для анимации спрайта (8 направлений)
  - `EnemyAnimation` - управление аниматором на основе угла к игроку
  - `EnemySpriteLook` - альтернативный компонент для поворота спрайта к игроку (опционально)

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
- Проверяйте обнаружение не каждый кадр, а раз в 0.1-0.5 секунды (реализовано через `detectionUpdateInterval`)
- Используйте таймеры для контроля частоты проверок
- Кешируйте ссылку на игрока через `CoreReferences.Player`

**4. Гистерезис обнаружения (реализовано в EnemyAI):**
- Используются разные пороги для входа и выхода из состояния Aggro
- `detectionRadius` - для обнаружения игрока (вход в Aggro)
- `aggroLoseDistance` - для потери агро (выход из Aggro)
- Предотвращает "дрожание" состояний при граничных значениях расстояния

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
// Движение между точками (реализовано в EnemyMovement)
public void Patrol()
{
    if (waypoints == null || waypoints.Length == 0)
    {
        Stop();
        return;
    }
    
    Transform currentWaypoint = waypoints[currentWaypointIndex];
    float distance = Vector3.Distance(transform.position, currentWaypoint.position);
    
    if (distance <= waypointReachDistance)
    {
        MoveToNextWaypoint();
    }
    else
    {
        MoveTo(currentWaypoint.position);
    }
}
```

**Гравитация (реализовано в EnemyMovement):**
- Используется `CharacterController` для движения
- Применяется гравитация для вертикального движения
- `groundedGravity` - небольшая гравитация на земле для стабильности
- `gravity` - полная гравитация в воздухе

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

### 6. Визуализация и анимация спрайтов

#### ✅ **AngleToPlayer - Вычисление угла к игроку**
- Вычисляет угол между направлением врага и направлением к игроку
- Использует `Vector3.SignedAngle` для определения направления
- Возвращает индекс направления (0-7) для 8-направленной анимации:
  - 0: Вперед (front)
  - 1-3: Назад (back, левая/правая стороны)
  - 4: Прямо назад
  - 5-7: Вперед (левая/правая стороны)
- Флипает спрайт по горизонтали (X) при отрицательном угле

```csharp
// Вычисление угла к игроку
angle = Vector3.SignedAngle(from: targetDir, to: transform.forward, axis: Vector3.up);

// Флип спрайта по горизонтали
Vector3 tempScale = Vector3.one;
if (angle > 0)
{
    tempScale.x = -1f; // Только X, не весь Vector3!
}
spriteRenderer.transform.localScale = tempScale;
```

#### ✅ **EnemyAnimation - Управление аниматором**
- Получает индекс направления от `AngleToPlayer`
- Устанавливает параметр `spriteRotation` в аниматоре для 8-направленной анимации
- Подписывается на событие смерти `EnemyController.OnEnemyDied`
- Проигрывает анимацию смерти через trigger параметр `Death` в Animator
- Работает с 8-направленной анимацией спрайтов и анимацией смерти

```csharp
// Подписка на событие смерти
enemyController.OnEnemyDied += PlayDeathAnimation;

// Проигрывание анимации смерти
private void PlayDeathAnimation()
{
    if (animator != null)
    {
        animator.SetTrigger("Death"); // Используется trigger для одноразовой анимации
    }
}
```

**Настройка Animator Controller для анимации смерти:**
- Создайте trigger параметр `Death` в Animator Controller
- Добавьте состояние `death` с анимацией `ImpDeath.anim`
- Создайте переход из Any State (или из всех состояний) в `death` с условием `Death` (trigger)
- Настройки перехода:
  - Снимите галочку "Has Exit Time"
  - Установите "Transition Duration" = 0 (мгновенный переход)
  - Установите "Interruption Source" = None (чтобы анимация не прерывалась)
- В состоянии `death` снимите галочку "Loop Time" (если анимация не должна зацикливаться)
- Убедитесь, что нет обратных переходов из `death` в другие состояния

#### ✅ **EnemySpriteLook - Альтернативный поворот спрайта**
- Простой компонент для поворота спрайта к игроку через `LookAt`
- Используется как альтернатива `AngleToPlayer` для простых случаев
- Выравнивает Y координату для горизонтального поворота

### 7. Типы врагов проекта (планируется)

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

**Примечание**: Типы врагов еще не реализованы. Базовая система готова для расширения.

### 8. Интеграция с существующими системами

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
// EnemyController
public System.Action OnEnemyInitialized;
public System.Action OnEnemyDied;

// EnemyAI
public System.Action<EnemyState> OnStateChanged;

// EnemyHealth
public System.Action<float, float> OnHealthChanged; // current, max
public System.Action OnEnemyDied;

// EnemyAttack
public System.Action OnAttackPerformed;
public System.Action OnAttackStarted;
public System.Action OnAttackEnded;

// Уведомление GameManager (реализовано в EnemyController)
OnEnemyDied?.Invoke();
if (CoreReferences.GameManager != null)
{
    CoreReferences.GameManager.OnEnemyKilledHandler(0); // 0 = базовый тип врага
}
```

### 9. Отладка и визуализация

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

### 10. Структура файлов

```
Assets/Scripts/Enemies/
├── Core/
│   ├── EnemyController.cs          # Главный контроллер, координирует все системы
│   ├── EnemyAI.cs                   # FSM логика (Patrol → Aggro → Attack → Dead)
│   ├── EnemyMovement.cs             # Движение (CharacterController, патрулирование, обход препятствий)
│   ├── EnemyHealth.cs               # Здоровье (IDamageable)
│   ├── EnemyAttack.cs               # Атаки игрока
│   ├── AngleToPlayer.cs             # Вычисление угла к игроку для анимации (8 направлений)
│   ├── EnemyAnimation.cs            # Управление аниматором на основе угла
│   └── EnemySpriteLook.cs           # Альтернативный поворот спрайта к игроку (опционально)
└── README.md                         # Этот файл
```

**Примечание**: Папка `Types/` для специфичных типов врагов (Walker, Runner, Exploder) еще не создана. Базовая система готова для расширения.

### 11. Чеклист реализации

#### ✅ Реализовано:
- [x] Создать базовый `EnemyController` с FSM
- [x] Реализовать состояния: Patrol, Aggro, Attack, Dead
- [x] Добавить `EnemyHealth` с `IDamageable`
- [x] Реализовать простое движение к цели (CharacterController)
- [x] Добавить обнаружение игрока (OverlapSphere + гистерезис)
- [x] Реализовать патрулирование (waypoints с поддержкой циклов)
- [x] Добавить логику атаки (с cooldown и проверкой расстояния)
- [x] Интегрировать с `CoreReferences`
- [x] Добавить Gizmos для отладки (радиусы, waypoints, направление)
- [x] Оптимизировать обновления (detectionUpdateInterval)
- [x] Реализовать обход препятствий (простой raycast)
- [x] Добавить гравитацию для CharacterController
- [x] Реализовать визуализацию спрайтов (AngleToPlayer, EnemyAnimation)
- [x] Добавить события для координации систем (OnEnemyDied, OnStateChanged)

#### ⏳ Планируется:
- [ ] Реализовать Walker, Runner, Exploder типы врагов
- [ ] Добавить Object Pooling для оптимизации
- [ ] Расширить систему анимаций (атака, смерть, урон)
- [ ] Добавить звуковые эффекты для врагов

### 12. Рекомендации по коду

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

### 13. Дополнительные ресурсы

- **Unity Manual**: AI Navigation (если понадобится NavMesh)
- **Unity Learn**: Creating AI for Games
- **Best Practices**: Keep it simple, test frequently, iterate

## Текущая реализация

### Реализованные компоненты

1. **EnemyController** - Главный контроллер, координирует все системы врага
   - Инициализирует компоненты в `Awake()`
   - Подписывается на события здоровья и ИИ
   - Уведомляет `GameManager` о смерти врага
   - Опция `keepCorpseOnScene` - позволяет оставить труп на сцене вместо уничтожения
   - При смерти отключает все активные компоненты (ИИ, движение, атака), оставляя только визуальную часть

2. **EnemyAI** - FSM система с состояниями:
   - `Patrol` - патрулирование между waypoints
   - `Aggro` - преследование игрока
   - `Attack` - атака игрока вблизи
   - `Dead` - смерть врага
   - Использует гистерезис для стабильного переключения состояний
   - Оптимизировано: обнаружение игрока не каждый кадр

3. **EnemyMovement** - Система движения:
   - Использует `CharacterController` для движения
   - Поддерживает патрулирование с циклами
   - Простой обход препятствий через raycast
   - Гравитация для вертикального движения
   - Плавное ускорение и поворот

4. **EnemyHealth** - Система здоровья:
   - Реализует `IDamageable` для совместимости с оружием
   - События для уведомления о изменении здоровья и смерти
   - Метод `RestoreHealth()` для восстановления

5. **EnemyAttack** - Система атаки:
   - Проверка расстояния до игрока
   - Cooldown между атаками
   - Опциональная проверка прямой видимости (SphereCast)
   - События для анимации и звуков

6. **AngleToPlayer** - Визуализация спрайта:
   - Вычисляет угол к игроку (8 направлений)
   - Флипает спрайт по горизонтали
   - Возвращает индекс направления для аниматора

7. **EnemyAnimation** - Управление анимацией:
   - Устанавливает параметр `spriteRotation` в аниматоре для 8-направленной анимации
   - Подписывается на событие смерти `EnemyController.OnEnemyDied`
   - Проигрывает анимацию смерти через trigger параметр `Death` в Animator
   - Использует trigger вместо bool для одноразовой анимации смерти

8. **EnemySpriteLook** - Альтернативный поворот спрайта:
   - Простой компонент для поворота через `LookAt`
   - Используется как альтернатива `AngleToPlayer`

### Особенности реализации

- **Гистерезис обнаружения**: Разные пороги для входа/выхода из Aggro предотвращают "дрожание" состояний
- **Оптимизация производительности**: Обнаружение игрока обновляется с интервалом (по умолчанию 0.2 сек)
- **Гравитация**: Правильная обработка вертикального движения через CharacterController
- **События**: Слабая связанность через события между компонентами
- **Gizmos**: Визуализация радиусов, waypoints, направлений для отладки
- **Анимация смерти**: Используется trigger параметр `Death` для одноразовой анимации смерти, что предотвращает конфликты с другими переходами
- **Уничтожение GameObject**: После смерти враг уничтожается с задержкой (по умолчанию 2 секунды) для проигрывания анимации смерти
- **Опция трупа на сцене**: Если `keepCorpseOnScene = true`, враг не уничтожается, а остается на сцене как труп. Все активные компоненты (ИИ, движение, атака, CharacterController, AngleToPlayer, EnemySpriteLook) отключаются, остаются только визуальные компоненты (SpriteRenderer, Animator)

## Примечания

- Для проекта 64.WAD используется **простая FSM без NavMesh**
- Фокус на **производительности** и **простоте поддержки**
- Архитектура позволяет легко добавлять новые типы врагов
- Все враги должны реализовывать `IDamageable` для совместимости с оружием
- Типы врагов (Walker, Runner, Exploder) еще не реализованы - базовая система готова для расширения
