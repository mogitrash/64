using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WAD64.Core;

namespace WAD64.Managers
{
    /// <summary>
    /// Менеджер пользовательского интерфейса. Управляет всеми UI элементами и их обновлением.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI weaponText;
        [SerializeField] private TextMeshProUGUI gameTimeText;

        [Header("Game State UI")]
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject levelCompleteScreen;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Crosshair")]
        [SerializeField] private Image crosshair;
        [SerializeField] private Color normalCrosshairColor = Color.white;
        [SerializeField] private Color hitCrosshairColor = Color.red;
        [SerializeField] private float crosshairHitDuration = 0.1f;

        [Header("Damage Indicator")]
        [SerializeField] private Image damageOverlay;
        [SerializeField] private float damageFlashDuration = 0.3f;

        [Header("Settings")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private TextMeshProUGUI fpsText;

        // State tracking
        private float crosshairHitTimer = 0f;
        private float damageFlashTimer = 0f;
        private int frameCount = 0;
        private float fpsTimer = 0f;

        private void Awake()
        {
            // Убеждаемся, что UIManager единственный
            if (CoreReferences.UIManager != null && CoreReferences.UIManager != this)
            {
                Debug.LogWarning("[UIManager] Another UIManager already exists! Destroying this one.");
                Destroy(gameObject);
                return;
            }

            InitializeUI();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdateHUD();
            UpdateCrosshair();
            UpdateDamageOverlay();

            if (showFPS && fpsText != null)
            {
                UpdateFPS();
            }
        }

        #region Initialization

        private void InitializeUI()
        {
            // Настройка начального состояния UI
            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);

            // Настройка кнопок (с заглушками)
            if (resumeButton != null)
                resumeButton.onClick.AddListener(() =>
                {
                    Debug.Log("Resume button clicked");
                    // Будет заменено на CoreReferences.GameManager.ResumeGame() позже
                });

            if (restartButton != null)
                restartButton.onClick.AddListener(() =>
                {
                    Debug.Log("Restart button clicked");
                    // Будет заменено на CoreReferences.GameManager.RestartLevel() позже
                });

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            // Настройка курсора
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[UIManager] UI initialized");
        }

        private void SubscribeToEvents()
        {
            // Подписка на события будет добавлена позже, когда создадим конкретные классы менеджеров
            Debug.Log("[UIManager] Event subscription ready (will be implemented later)");
        }

        #endregion

        #region HUD Updates

        private void UpdateHUD()
        {
            // Обновляем время игры (временно используем Time.time)
            if (gameTimeText != null)
            {
                float time = Time.time;
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                gameTimeText.text = $"{minutes:00}:{seconds:00}";
            }

            // Обновляем здоровье игрока
            UpdatePlayerHealth();

            // Обновляем информацию об оружии
            UpdateWeaponInfo();
        }

        private void UpdatePlayerHealth()
        {
            // Временные значения (будут заменены на реальные позже)
            float currentHealth = 100f;
            float maxHealth = 100f;
            float healthPercent = currentHealth / maxHealth;

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
            }

            if (healthBar != null)
            {
                healthBar.value = healthPercent;
            }
        }

        private void UpdateWeaponInfo()
        {
            // Временные значения (будут заменены на реальные позже)
            if (ammoText != null)
            {
                ammoText.text = "12/30";
            }

            if (weaponText != null)
            {
                weaponText.text = "Pistol";
            }
        }

        #endregion

        #region Crosshair

        private void UpdateCrosshair()
        {
            if (crosshair == null) return;

            if (crosshairHitTimer > 0f)
            {
                crosshairHitTimer -= Time.deltaTime;
                crosshair.color = Color.Lerp(hitCrosshairColor, normalCrosshairColor,
                    1f - (crosshairHitTimer / crosshairHitDuration));
            }
            else
            {
                crosshair.color = normalCrosshairColor;
            }
        }

        public void ShowHitMarker()
        {
            crosshairHitTimer = crosshairHitDuration;
        }

        #endregion

        #region Damage Overlay

        private void UpdateDamageOverlay()
        {
            if (damageOverlay == null) return;

            if (damageFlashTimer > 0f)
            {
                damageFlashTimer -= Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 0.3f, damageFlashTimer / damageFlashDuration);
                Color color = damageOverlay.color;
                color.a = alpha;
                damageOverlay.color = color;
            }
        }

        public void ShowDamageFlash()
        {
            damageFlashTimer = damageFlashDuration;
        }

        #endregion

        #region Game State Events

        public void OnGamePaused()
        {
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OnGameResumed()
        {
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OnGameOver()
        {
            if (gameOverScreen != null)
            {
                gameOverScreen.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OnLevelCompleted()
        {
            if (levelCompleteScreen != null)
            {
                levelCompleteScreen.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #endregion

        #region FPS Counter

        private void UpdateFPS()
        {
            frameCount++;
            fpsTimer += Time.deltaTime;

            if (fpsTimer >= 1f)
            {
                int fps = Mathf.RoundToInt(frameCount / fpsTimer);
                if (fpsText != null)
                {
                    fpsText.text = $"FPS: {fps}";
                }

                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        #endregion

        #region Public Methods

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetCrosshairVisible(bool visible)
        {
            if (crosshair != null)
            {
                crosshair.gameObject.SetActive(visible);
            }
        }

        #endregion
    }
}