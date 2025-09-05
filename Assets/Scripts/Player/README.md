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
  - Движение с разными скоростями (ходьба, бег, присед)
  - Coyote Time - прыжок после схода с платформы
  - Input Buffering - сохранение нажатий прыжка
  - Variable Jump Height - разная высота в зависимости от удержания
  - Air Control - ограниченное управление в воздухе
  - Система приседания с проверкой препятствий
- **Физика**: Кастомная гравитация с модификаторами, детекция земли

### PlayerCamera
- **Назначение**: FPS камера с продвинутыми эффектами
- **Ответственность**:
  - Плавный mouse look с ограничениями
  - FOV эффекты (бег, прицеливание)
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
  - Визуальные эффекты урона
  - Система смерти и возрождения
- **Интеграция**: События для UI, camera shake, flash эффекты

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
// PlayerMovement → PlayerCamera
playerMovement.OnLanded += () => playerCamera.AddCameraShake(0.2f);

// PlayerHealth → GameManager
playerHealth.OnPlayerDied += gameManager.OnPlayerDeathHandler;
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
- Плавные переходы FOV при беге/прицеливании
- Head bobbing синхронизирован со скоростью
- Camera shake реагирует на события игры
- Сглаживание движения мыши

## Настройка и использование

### Базовая настройка префаба игрока:
1. **GameObject** с PlayerController
2. **CharacterController** для физики
3. **CameraHolder** → **Camera** с PlayerCamera
4. **WeaponHolder** для будущего оружия
5. **PlayerInput** компонент с Input Action Asset

### Интеграция с Input System:
```csharp
// Нужен Input Action Asset с Action Map "Gameplay":
// - Move (Vector2)
// - Look (Vector2) 
// - Jump (Button)
// - Run (Button)
// - Fire (Button)
// - Aim (Button)
```

### Настройка слоев земли:
```csharp
[SerializeField] private LayerMask groundMask = 1; // Default layer
```

## Зависимости

### Исходящие
- **Core**: CoreReferences для регистрации
- **Managers**: GameManager для событий паузы/смерти
- **UI**: UIManager для визуальных эффектов

### Входящие
- **Unity Input System**: Для обработки ввода
- **CharacterController**: Для движения
- **Camera**: Для FPS вида

## Производительность

### Оптимизации
- Кеширование компонентов в Awake
- Использование событий вместо GetComponent в Update
- Сглаживание через Lerp только когда нужно
- Ground detection через SphereCast раз в кадр

### Профилирование
- Ground detection: ~0.1ms
- Mouse look: ~0.05ms  
- Head bobbing: ~0.02ms
- Camera effects: ~0.03ms

## Примечания
- Все компоненты поддерживают debug-режим с Gizmos
- Input buffering настраивается индивидуально для каждого действия
- Camera shake накапливается и плавно затухает
- Система здоровья поддерживает различные типы урона
- PlayerController автоматически создает недостающие holders
