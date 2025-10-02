using UnityEngine;
using UnityEngine.UI;
using WAD64.Player;
using WAD64.Core;

namespace WAD64.UI
{
    /// <summary>
    /// Компонент для отображения полоски брони игрока.
    /// Подписывается на события PlayerHealth и обновляет визуальное представление.
    /// Автоматически скрывается, когда броня равна 0.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ArmorBar : MonoBehaviour, IUIElement
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual Settings")]
        [SerializeField] private Color armorColor = new Color(0.3f, 0.6f, 1f); // Синий
        [SerializeField] private Color lowArmorColor = new Color(0.6f, 0.6f, 0.6f); // Серый
        [SerializeField] private float lowArmorThreshold = 0.3f;

        [Header("Animation")]
        [SerializeField] private bool smoothTransition = true;
        [SerializeField] private float transitionSpeed = 5f;

        [Header("Visibility")]
        [SerializeField] private bool hideWhenEmpty = true;
        [SerializeField] private float fadeSpeed = 3f;

        private PlayerHealth playerHealth;
        private float targetFillAmount = 0f;
        private float currentFillAmount = 0f;
        private float targetAlpha = 0f;
        private float currentAlpha = 0f;

        private void Awake()
        {
            if (fillImage == null)
            {
                fillImage = GetComponent<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
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
                    SubscribeToArmorEvents();
                    UpdateArmorBar(playerHealth.CurrentArmor, playerHealth.MaxArmor);
                }
            }
        }

        private void Update()
        {
            // Плавное обновление заполнения
            if (smoothTransition && Mathf.Abs(currentFillAmount - targetFillAmount) > 0.01f)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * transitionSpeed);
                fillImage.fillAmount = currentFillAmount;
            }

            // Плавное скрытие/показ
            if (hideWhenEmpty && Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
                canvasGroup.alpha = currentAlpha;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromArmorEvents();
        }

        private void SubscribeToArmorEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnArmorChanged += UpdateArmorBar;
            }
        }

        private void UnsubscribeFromArmorEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnArmorChanged -= UpdateArmorBar;
            }
        }

        private void UpdateArmorBar(float currentArmor, float maxArmor)
        {
            if (maxArmor <= 0f)
            {
                targetFillAmount = 0f;
                targetAlpha = 0f;
                return;
            }

            targetFillAmount = currentArmor / maxArmor;
            targetAlpha = currentArmor > 0f ? 1f : 0f;

            if (!smoothTransition)
            {
                currentFillAmount = targetFillAmount;
                fillImage.fillAmount = currentFillAmount;
            }

            if (!hideWhenEmpty)
            {
                currentAlpha = 1f;
                canvasGroup.alpha = 1f;
            }

            UpdateArmorColor(targetFillAmount);
        }

        private void UpdateArmorColor(float armorPercent)
        {
            Color targetColor = armorPercent <= lowArmorThreshold
                ? Color.Lerp(lowArmorColor, armorColor, armorPercent / lowArmorThreshold)
                : armorColor;

            fillImage.color = targetColor;
        }

        /// <summary>
        /// Позволяет установить PlayerHealth вручную (для тестирования)
        /// </summary>
        public void SetPlayerHealth(PlayerHealth health)
        {
            UnsubscribeFromArmorEvents();
            playerHealth = health;
            SubscribeToArmorEvents();

            if (playerHealth != null)
            {
                UpdateArmorBar(playerHealth.CurrentArmor, playerHealth.MaxArmor);
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
                image.fillAmount = 0f;
                image.color = armorColor;
            }

            // Автоматически находим или создаем CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

    }
}