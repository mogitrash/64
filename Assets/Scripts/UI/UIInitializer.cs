using UnityEngine;
using UnityEngine.UI;
using WAD64.Core;

namespace WAD64.UI
{
    /// <summary>
    /// Автоматически инициализирует UI компоненты в сцене.
    /// Настраивает ссылки на Image компоненты и устанавливает правильные настройки.
    /// Использует публичные методы вместо рефлексии для лучшей производительности.
    /// </summary>
    public class UIInitializer : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private ArmorBar armorBar;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Находим UI компоненты если они не назначены
            if (healthBar == null)
            {
                healthBar = FindFirstObjectByType<HealthBar>();
            }

            if (armorBar == null)
            {
                armorBar = FindFirstObjectByType<ArmorBar>();
            }

            // Настраиваем HealthBar
            if (healthBar != null)
            {
                SetupHealthBar();
            }

            // Настраиваем ArmorBar
            if (armorBar != null)
            {
                SetupArmorBar();
            }
        }

        private void SetupHealthBar()
        {
            // Находим Image компонент на том же объекте
            Image healthImage = healthBar.GetComponent<Image>();
            if (healthImage != null)
            {
                // Используем публичный метод вместо рефлексии
                healthBar.SetupUI(healthImage);
                // Позиция и размер настраиваются вручную в Unity Editor
            }
        }

        private void SetupArmorBar()
        {
            // Находим Image компонент на том же объекте
            Image armorImage = armorBar.GetComponent<Image>();

            if (armorImage != null)
            {
                // Используем публичный метод вместо рефлексии
                armorBar.SetupUI(armorImage);
                // Позиция и размер настраиваются вручную в Unity Editor
            }
        }

        /// <summary>
        /// Публичный метод для ручной инициализации (можно вызвать из других скриптов)
        /// </summary>
        public void ReinitializeUI()
        {
            InitializeUI();
        }
    }
}