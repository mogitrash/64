using UnityEngine;
using UnityEngine.UI;
using WAD64.Player;
using WAD64.Core;

namespace WAD64.UI
{
    /// <summary>
    /// Компонент для отображения полоски здоровья игрока.
    /// Подписывается на события PlayerHealth и обновляет визуальное представление.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class HealthBar : MonoBehaviour, IUIElement
    {
        [Header("References")]
        [SerializeField] private Image fillImage;

        [Header("Visual Settings")]
        [SerializeField] private Color healthyColor = new Color(0.2f, 1f, 0.2f); // Зеленый
        [SerializeField] private Color midHealthColor = new Color(1f, 0.8f, 0f);  // Желтый
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.2f, 0.2f); // Красный
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private float midHealthThreshold = 0.6f;

        [Header("Animation")]
        [SerializeField] private bool smoothTransition = true;
        [SerializeField] private float transitionSpeed = 5f;

        private PlayerHealth playerHealth;
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;

        private void Awake()
        {
            if (fillImage == null)
            {
                fillImage = GetComponent<Image>();
            }
        }

        private void Start()
        {
            // Находим PlayerHealth через CoreReferences
            if (CoreReferences.PlayerHealth != null)
            {
                playerHealth = CoreReferences.PlayerHealth as PlayerHealth;
                if (playerHealth != null)
                {
                    SubscribeToHealthEvents();
                    UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
                }
            }
        }

        private void Update()
        {
            if (smoothTransition && Mathf.Abs(currentFillAmount - targetFillAmount) > 0.01f)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * transitionSpeed);
                fillImage.fillAmount = currentFillAmount;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealthEvents();
        }

        private void SubscribeToHealthEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
            }
        }

        private void UnsubscribeFromHealthEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
            }
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0f) return;

            targetFillAmount = currentHealth / maxHealth;

            if (!smoothTransition)
            {
                currentFillAmount = targetFillAmount;
                fillImage.fillAmount = currentFillAmount;
            }

            UpdateHealthColor(targetFillAmount);
        }

        private void UpdateHealthColor(float healthPercent)
        {
            Color targetColor;

            if (healthPercent <= lowHealthThreshold)
            {
                targetColor = lowHealthColor;
            }
            else if (healthPercent <= midHealthThreshold)
            {
                float t = (healthPercent - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold);
                targetColor = Color.Lerp(lowHealthColor, midHealthColor, t);
            }
            else
            {
                float t = (healthPercent - midHealthThreshold) / (1f - midHealthThreshold);
                targetColor = Color.Lerp(midHealthColor, healthyColor, t);
            }

            fillImage.color = targetColor;
        }

        /// <summary>
        /// Позволяет установить PlayerHealth вручную (для тестирования)
        /// </summary>
        public void SetPlayerHealth(PlayerHealth health)
        {
            UnsubscribeFromHealthEvents();
            playerHealth = health;
            SubscribeToHealthEvents();

            if (playerHealth != null)
            {
                UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        /// <summary>
        /// Настраивает UI компонент (заменяет рефлексию)
        /// </summary>
        public void SetupUI(Image image)
        {
            if (image != null)
            {
                fillImage = image;

                // Настраиваем Image как Filled
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Vertical;
                image.fillAmount = 1f;
                image.color = healthyColor;
            }
        }

    }
}