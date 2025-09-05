# Player - Система игрока

## Назначение
Содержит все компоненты, отвечающие за функциональность игрока в 64.WAD: движение, камеру, здоровье, обработку ввода и координацию всех систем.

## Основные компоненты

### PlayerController
- **Назначение**: Главный контроллер, координирующий все компоненты игрока
- **Ответственность**:
  - Инициализация и настройка всех компонентов
  - Регистрация в CoreReferences
  - Управление состоянием игрока (включен/выключен)
  - Обработка событий между компонентами
  - Единая точка доступа к функциональности игрока
- **Требования**: CharacterController, InputHandler, PlayerMovement, PlayerHealth

### InputHandler
- **Назначение**: Обработка пользовательского ввода с новой Unity Input System
- **Ответственность**:
  - Обработка движения, взгляда, прыжков, стрельбы
  - Input buffering для прыжков и стрельбы
  - События для других компонентов
  - Настройка чувствительности и инверсии мыши
- **Особенности**: Поддержка Input Action Asset, буферизация команд

### PlayerMovement
- **Назначение**: Продвинутая система движения FPS контроллера
- **Ответственность**:
  - Движение с разными скоростями (ходьба, бег)
  - Coyote Time - прыжок после схода с платформы
  - Input Buffering - сохранение нажатий прыжка
  - Variable Jump Height - разная высота в зависимости от удержания
  - Air Control - ограниченное управление в воздухе
- **Физика**: Кастомная гравитация с модификаторами, детекция земли

### PlayerCamera
- **Назначение**: FPS камера с продвинутыми эффектами
- **Ответственность**:
  - Плавный mouse look с ограничениями
  - FOV эффекты (бег)
  - Head bobbing при движении
  - Camera shake от урона и действий
  - Smooth transitions для всех параметров
- **Эффекты**: Динамический FOV, качание головы, тряска камеры

### PlayerHealth
- **Назначение**: Система здоровья и урона
- **Ответственность**:
  - Базовое здоровье и система брони
  - Регенерация здоровья с задержкой
  - Временная неуязвимость после урона
  - Различные типы урона с модификаторами
  - Camera shake эффекты при уроне
  - Система смерти и возрождения
- **Интеграция**: События для UI, camera shake, UI flash эффекты

## Архитектурные принципы

### 1. Component-Based Design
Каждый компонент отвечает за свою область:
```csharp
// PlayerController координирует все
public PlayerMovement Movement => playerMovement;
public PlayerCamera Camera => playerCamera;
public PlayerHealth Health => playerHealth;
```

### 2. Event-Driven Communication
Компоненты общаются через события:
```csharp
// PlayerController координирует события между компонентами
playerHealth.OnPlayerDied += OnPlayerDied;

// PlayerHealth → GameManager (через PlayerController)
// GameManager подписывается на события через CoreReferences.Player
playerHealth.OnHealthChanged += (health, maxHealth) => UpdateUI();
playerHealth.OnPlayerDied += () => TriggerGameOverScreen();
```

### 3. Modern FPS Mechanics

#### Coyote Time
```csharp
// Можно прыгать 0.15с после схода с платформы
if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f)
    PerformJump();
```

#### Input Buffering
```csharp
// Сохраняем нажатие прыжка на 0.2с
if (jumpPressed) jumpBufferTimer = jumpBufferTime;
```

#### Variable Jump Height
```csharp
// Разная высота в зависимости от удержания кнопки
if (velocity.y > 0 && jumpInputReleased)
    gravityMultiplier = lowJumpMultiplier;
```

### 4. Smooth User Experience
- Плавные переходы FOV при беге
- Head bobbing синхронизирован со скоростью
- Camera shake реагирует на события игры
- Сглаживание движения мыши

## Настройка и использование

### Базовая настройка префаба игрока:

#### Иерархия GameObject:
```
Player (Root)
├── PlayerController.cs
├── CharacterController  
├── InputHandler.cs
├── PlayerMovement.cs  
├── PlayerHealth.cs
├── PlayerInput (Input Action Asset)
└── CameraHolder (Transform)
    └── Main Camera
        └── PlayerCamera.cs
```

#### Настройка компонентов:
1. **PlayerController**: Главный скрипт, все остальные добавляются автоматически через RequireComponent
2. **CharacterController**: 
   - Height: 2.0
   - Radius: 0.5  
   - Step Offset: 0.3
3. **PlayerInput**: 
   - Behavior: Send Messages
   - Actions: InputSystem_Actions asset
4. **CameraHolder**: Пустой Transform для позиционирования камеры
5. **Camera**: Обычная Camera с PlayerCamera скриптом

### Интеграция с Input System:
```csharp
// Нужен Input Action Asset с Action Map "Player":
// - Move (Vector2)
// - Look (Vector2) 
// - Jump (Button)
// - Sprint (Button) 
// - Attack (Button)
// - Interact (Button)
```

### Настройка слоев земли:
```csharp
[SerializeField] private LayerMask groundMask = 1; // Default layer
```

## Структура и Namespace

### Namespace Organization
```csharp
namespace WAD64.Player  // Все компоненты игрока
{
    public class PlayerController : MonoBehaviour
    public class InputHandler : MonoBehaviour  
    public class PlayerMovement : MonoBehaviour
    public class PlayerCamera : MonoBehaviour
    public class PlayerHealth : MonoBehaviour
}
```

### Required Components (Автоматические зависимости)
```csharp
// PlayerController
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputHandler))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]

// PlayerMovement
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputHandler))]
```

## Зависимости

### Исходящие
- **WAD64.Core**: CoreReferences для глобальной регистрации
- **WAD64.Managers**: GameManager для событий паузы/смерти, UIManager для визуальных эффектов

### Входящие
- **Unity Input System**: PlayerInput компонент и Input Action Asset
- **CharacterController**: Для физики движения
- **Camera**: Для FPS вида и эффектов

## Производительность

### Оптимизации
- Кеширование компонентов в Awake
- Использование событий вместо GetComponent в Update
- Сглаживание через Lerp только когда нужно
- Ground detection через SphereCast раз в кадр

### Профилирование
- **Ground detection**: SphereCast раз в кадр, минимальное влияние на производительность
- **Mouse look**: Простые математические операции, очень быстро
- **Head bobbing**: Синусоидальные вычисления только при движении
- **Camera effects**: FOV transitions и shake через Lerp, оптимизировано
- **Input handling**: Обработка через Unity Input System, эффективная буферизация

## События и API

### Основные события системы:
```csharp
// PlayerController
public System.Action OnPlayerInitialized;
public System.Action OnPlayerEnabled;  
public System.Action OnPlayerDisabled;

// PlayerHealth
public System.Action OnPlayerDied;
public System.Action<float, float> OnHealthChanged; // current, max
public System.Action<float> OnDamageTaken;

// InputHandler
public System.Action OnJumpPressed;
public System.Action OnFirePressed;
public System.Action OnInteractPressed;
```

### Публичный API:
```csharp
// Доступ через CoreReferences.Player
PlayerController player = CoreReferences.Player;

// Управление состоянием
player.SetPlayerEnabled(false);  // Отключить управление
player.Health.TakeDamage(25f);   // Нанести урон
player.Movement.SetMovementEnabled(false); // Заблокировать движение

// Получение состояния
bool isGrounded = player.Movement.IsGrounded;
float currentHealth = player.Health.CurrentHealth;
Vector2 inputMove = player.Input.MoveInput;
```

## Примечания
- Все компоненты поддерживают debug-режим с Gizmos
- Input buffering настраивается индивидуально для каждого действия
- Camera shake накапливается и плавно затухает при уроне
- Система здоровья поддерживает различные типы урона с модификаторами
- PlayerController автоматически создает недостающие holders
