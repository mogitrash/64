# Menu - Система меню

## Назначение
Система меню управляет главным меню, меню паузы и настройками игры. Все компоненты работают через центральный `MenuManager` для координации между различными меню.

## Основные компоненты

### MenuManager
- **Назначение**: Центральный менеджер всех меню (паузы, настроек, главного меню)
- **Ответственность**:
  - Координация между различными меню
  - Управление показом/скрытием меню
  - Отслеживание состояния открытых меню
  - Обработка переключения между меню паузы и настроек
- **Методы**:
  - `ShowPauseMenu()` / `HidePauseMenu()` - управление меню паузы
  - `ShowSettingsMenu(origin)` / `HideSettingsMenu()` - управление меню настроек
  - `HideAllMenus()` - закрытие всех меню (используется при возобновлении игры)
  - `IsSettingsOpen()` / `IsPauseMenuOpen()` / `IsAnyMenuOpen()` - проверка состояния меню
- **Особенности**: 
  - Singleton паттерн
  - Автоматическое восстановление меню паузы после закрытия настроек
  - При закрытии всех меню (Resume) Settings закрывается без уведомления, чтобы не показывать панель паузы обратно

### PauseMenuController
- **Назначение**: Управление меню паузы в игровой сцене
- **Ответственность**:
  - Показ/скрытие панели паузы
  - Управление курсором (блокировка/разблокировка)
  - Обработка кнопок: Continue, Settings, Main Menu
- **Методы**:
  - `Show()` / `Hide()` - показ/скрытие меню с управлением курсором
  - `HidePanelOnly()` - скрытие только панели без блокировки курсора (для перехода в Settings)
  - `ResumeGame()` - возобновление игры через GameManager
  - `OpenSettings()` - открытие меню настроек
  - `GoToMainMenu()` - переход в главное меню
- **Настройки**:
  - `pausePanel` - ссылка на GameObject панели паузы
  - `mainMenuSceneName` - имя сцены главного меню
- **Свойства**:
  - `IsOpen` - проверка, открыто ли меню паузы

### SettingsMenuController
- **Назначение**: Управление меню настроек
- **Ответственность**:
  - Показ/скрытие панели настроек
  - Отслеживание источника открытия (MainMenu или PauseMenu)
  - Уведомление подписчиков о закрытии меню
- **Методы**:
  - `Open(origin)` - открытие меню настроек с указанием источника
  - `CloseSettings(notify)` - закрытие меню (с опциональным уведомлением)
  - `BackToPreviousMenu()` - альтернативное имя для кнопки "Назад"
- **События**:
  - `OnSettingsClosed` - вызывается при закрытии меню (если `notify = true`)
- **Настройки**:
  - `settingsPanel` - ссылка на GameObject панели настроек
- **Свойства**:
  - `IsOpen` - проверка, открыто ли меню настроек
- **Особенности**: 
  - При закрытии из PauseMenu автоматически возвращается в меню паузы
  - Параметр `notify` позволяет закрыть меню без уведомления (используется при HideAllMenus)

### MainMenuController
- **Назначение**: Управление главным меню
- **Ответственность**:
  - Обработка кнопок главного меню: Play, Settings, Quit
- **Методы**:
  - `PlayGame()` - загрузка игровой сцены
  - `OpenSettings()` - открытие меню настроек
  - `QuitGame()` - выход из игры (в редакторе останавливает Play Mode)
- **Настройки**:
  - `gameSceneName` - имя игровой сцены

### AudioSettings
- **Назначение**: Управление настройками аудио (master и music volume)
- **Ответственность**:
  - Управление громкостью master (через `AudioListener.volume`)
  - Управление громкостью музыки (через `MusicManager`)
  - Сохранение настроек в `SettingsData` и `PlayerPrefs`
  - Синхронизация слайдеров с текущими значениями
- **Методы для слайдеров**:
  - `UpdateVolumeFromSlider(float value)` / `UpdateVolumeFromSlider()` - установка master volume из слайдера
  - `UpdateMusicVolumeFromSlider(float value)` / `UpdateMusicVolumeFromSlider()` - установка music volume из слайдера
- **Настройки**:
  - `settingsData` - ссылка на ScriptableObject с настройками
  - `volumeSlider` - слайдер для master volume
  - `musicVolumeSlider` - слайдер для music volume
- **Интеграция**: 
  - Автоматически загружает сохраненные значения при старте
  - Синхронизирует слайдеры при открытии меню
  - Сохраняет изменения в SettingsData и PlayerPrefs
- **Использование в Unity Inspector**:
  - Для master volume слайдера: `On Value Changed` → `AudioSettings.UpdateVolumeFromSlider` (с параметром float или без)
  - Для music volume слайдера: `On Value Changed` → `AudioSettings.UpdateMusicVolumeFromSlider` (с параметром float или без)

### SettingsData (ScriptableObject)
- **Назначение**: Хранение настроек игры
- **Создание**: `Assets → Create → WAD64 → Settings → Settings Data`
- **Поля**:
  - `MasterVolume` - общая громкость (0-1, Range)
  - `MusicVolume` - громкость музыки (0-1, Range)
- **Особенности**: 
  - Значения автоматически сохраняются при изменении через `AudioSettings`
  - Используется как значение по умолчанию при загрузке настроек
  - Значения также сохраняются в PlayerPrefs для персистентности между сессиями

## Принципы работы

1. **Инициализация**:
   - `MenuManager` использует Singleton паттерн и должен быть один на сцену
   - Все контроллеры меню получают ссылки через SerializeField в Inspector
   - `AudioSettings` загружает сохраненные значения при `Awake()`

2. **Управление состоянием**:
   - Меню отслеживают свое состояние через свойство `IsOpen`
   - `MenuManager` предоставляет методы для проверки состояния всех меню
   - При закрытии Settings из PauseMenu автоматически возвращается панель паузы

3. **Управление курсором**:
   - `PauseMenuController` блокирует/разблокирует курсор при показе/скрытии
   - При переходе в Settings используется `HidePanelOnly()` чтобы не блокировать курсор
   - Settings сам управляет курсором при открытии/закрытии

4. **Сохранение настроек**:
   - Настройки сохраняются в `SettingsData` (ScriptableObject) и `PlayerPrefs`
   - При загрузке сначала проверяется PlayerPrefs, затем SettingsData как значение по умолчанию
   - Изменения применяются мгновенно через `MusicManager` и `AudioListener`

## Зависимости
- **Исходящие**: Managers (GameManager, MusicManager)
- **Входящие**: Core (CoreReferences)
- **Unity компоненты**: UnityEngine.UI (Slider), UnityEngine.SceneManagement

## Интеграция в сцену

### Структура меню:

**MainMenu сцена:**
```
Canvas
├── MainMenuPanel
│   ├── PlayButton (Button → MainMenuController.PlayGame)
│   ├── SettingsButton (Button → MainMenuController.OpenSettings)
│   └── QuitButton (Button → MainMenuController.QuitGame)
└── SettingsPanel (SettingsMenuController)
    ├── AudioSettings (AudioSettings)
    │   ├── MasterVolumeSlider (Slider → AudioSettings.UpdateVolumeFromSlider)
    │   └── MusicVolumeSlider (Slider → AudioSettings.UpdateMusicVolumeFromSlider)
    └── BackButton (Button → SettingsMenuController.BackToPreviousMenu)
```

**Game сцена:**
```
Canvas
├── PausePanel (PauseMenuController)
│   ├── ContinueButton (Button → PauseMenuController.ResumeGame)
│   ├── SettingsButton (Button → PauseMenuController.OpenSettings)
│   └── MainMenuButton (Button → PauseMenuController.GoToMainMenu)
└── SettingsPanel (SettingsMenuController)
    ├── AudioSettings (AudioSettings)
    │   ├── MasterVolumeSlider (Slider → AudioSettings.UpdateVolumeFromSlider)
    │   └── MusicVolumeSlider (Slider → AudioSettings.UpdateMusicVolumeFromSlider)
    └── BackButton (Button → SettingsMenuController.BackToPreviousMenu)
```

### Настройка компонентов:

1. **MenuManager**:
   - Добавить GameObject с компонентом `MenuManager` на сцену
   - Назначить ссылки на `PauseMenuController` и `SettingsMenuController` в Inspector
   - Должен быть один экземпляр на сцену (Singleton)

2. **PauseMenuController**:
   - Добавить компонент на GameObject панели паузы
   - Назначить `pausePanel` (или оставить null - будет использован сам GameObject)
   - Указать `mainMenuSceneName` (например, "MainMenu")

3. **SettingsMenuController**:
   - Добавить компонент на GameObject панели настроек
   - Назначить `settingsPanel` (или оставить null - будет использован сам GameObject)
   - Подключить кнопку "Назад" к методу `BackToPreviousMenu()`

4. **AudioSettings**:
   - Добавить компонент на GameObject внутри SettingsPanel
   - Назначить `settingsData` (ScriptableObject)
   - Назначить `volumeSlider` и `musicVolumeSlider`
   - В Inspector слайдеров настроить `On Value Changed`:
     - Master: `AudioSettings.UpdateVolumeFromSlider` (выбрать версию с float или без)
     - Music: `AudioSettings.UpdateMusicVolumeFromSlider` (выбрать версию с float или без)

5. **SettingsData**:
   - Создать через `Assets → Create → WAD64 → Settings → Settings Data`
   - Настроить значения по умолчанию для `MasterVolume` и `MusicVolume`

## Примечания
- Все меню работают через события и слабо связаны между собой
- `MenuManager` координирует работу всех меню, но не создает их
- При закрытии Settings из `HideAllMenus()` используется `notify: false` чтобы не показывать панель паузы обратно
- Настройки аудио сохраняются автоматически при изменении слайдеров
- `MusicManager` должен быть инициализирован до использования `AudioSettings` (через GameEntryPoint)
- Все меню безопасны к отсутствию компонентов - проверяют ссылки перед использованием

