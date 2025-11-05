using UnityEngine;
using UnityEngine.UI;
using WAD64.Core;
using WAD64.Managers;

namespace WAD64.UI
{
    /// <summary>
    /// Компонент для отображения прицела (крестика) по центру экрана.
    /// Автоматически скрывается при паузе/меню через подписку на события GameManager.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class Crosshair : MonoBehaviour, IUIElement
    {
        [Header("References")]
        [SerializeField] private Image topLine;
        [SerializeField] private Image bottomLine;
        [SerializeField] private Image leftLine;
        [SerializeField] private Image rightLine;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual Settings")]
        [SerializeField] private float lineLength = 20f;
        [SerializeField] private float lineWidth = 2f;
        [SerializeField] private float lineGap = 5f;
        [SerializeField] private Color crosshairColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 5f;

        private GameManager gameManager;
        private float targetAlpha = 1f;
        private float currentAlpha = 1f;
        private bool isVisible = true;

        private void Awake()
        {
            // Находим или создаем CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Находим дочерние Image компоненты
            FindOrCreateLineImages();
        }

        private void Start()
        {
            // Находим GameManager через CoreReferences
            if (CoreReferences.GameManager != null)
            {
                gameManager = CoreReferences.GameManager;
                SubscribeToGameEvents();
                UpdateVisibility(!gameManager.IsPaused && !gameManager.IsGameOver);
            }

            // Настраиваем RectTransform для центрирования
            SetupRectTransform();
        }

        private void Update()
        {
            // Плавное изменение прозрачности
            if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
                canvasGroup.alpha = currentAlpha;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
        }

        private void FindOrCreateLineImages()
        {
            // Находим существующие линии
            Image[] images = GetComponentsInChildren<Image>();
            
            foreach (Image img in images)
            {
                if (img.gameObject == gameObject) continue; // Пропускаем Image на самом Crosshair, если он есть

                string name = img.gameObject.name.ToLower();
                if (name.Contains("top"))
                {
                    topLine = img;
                }
                else if (name.Contains("bottom"))
                {
                    bottomLine = img;
                }
                else if (name.Contains("left"))
                {
                    leftLine = img;
                }
                else if (name.Contains("right"))
                {
                    rightLine = img;
                }
            }

            // Создаем недостающие линии
            if (topLine == null)
            {
                topLine = CreateLineImage("TopLine");
            }

            if (bottomLine == null)
            {
                bottomLine = CreateLineImage("BottomLine");
            }

            if (leftLine == null)
            {
                leftLine = CreateLineImage("LeftLine");
            }

            if (rightLine == null)
            {
                rightLine = CreateLineImage("RightLine");
            }

            // Настраиваем линии
            SetupLines();
        }

        private Image CreateLineImage(string name)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(transform, false);
            
            Image image = lineObj.AddComponent<Image>();
            image.color = crosshairColor;

            RectTransform rectTransform = lineObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localPosition = Vector3.zero;

            return image;
        }

        private void SetupLines()
        {
            // Верхняя линия
            if (topLine != null)
            {
                RectTransform rect = topLine.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(lineWidth, lineLength);
                rect.anchoredPosition = new Vector2(0f, lineGap + lineLength * 0.5f);
            }

            // Нижняя линия
            if (bottomLine != null)
            {
                RectTransform rect = bottomLine.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(lineWidth, lineLength);
                rect.anchoredPosition = new Vector2(0f, -(lineGap + lineLength * 0.5f));
            }

            // Левая линия
            if (leftLine != null)
            {
                RectTransform rect = leftLine.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(lineLength, lineWidth);
                rect.anchoredPosition = new Vector2(-(lineGap + lineLength * 0.5f), 0f);
            }

            // Правая линия
            if (rightLine != null)
            {
                RectTransform rect = rightLine.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(lineLength, lineWidth);
                rect.anchoredPosition = new Vector2(lineGap + lineLength * 0.5f, 0f);
            }

            // Устанавливаем цвета
            if (topLine != null) topLine.color = crosshairColor;
            if (bottomLine != null) bottomLine.color = crosshairColor;
            if (leftLine != null) leftLine.color = crosshairColor;
            if (rightLine != null) rightLine.color = crosshairColor;
        }

        private void SetupRectTransform()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Центрируем прицел
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
        }

        private void SubscribeToGameEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGamePaused += OnGamePaused;
                gameManager.OnGameResumed += OnGameResumed;
                gameManager.OnGameOver += OnGameOver;
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGamePaused -= OnGamePaused;
                gameManager.OnGameResumed -= OnGameResumed;
                gameManager.OnGameOver -= OnGameOver;
            }
        }

        private void OnGamePaused()
        {
            UpdateVisibility(false);
        }

        private void OnGameResumed()
        {
            UpdateVisibility(true);
        }

        private void OnGameOver()
        {
            UpdateVisibility(false);
        }

        private void UpdateVisibility(bool visible)
        {
            isVisible = visible;
            targetAlpha = visible ? 1f : 0f;
        }

        /// <summary>
        /// Настраивает UI компонент (заменяет рефлексию)
        /// Для Crosshair этот метод не используется, так как компонент настраивается автоматически
        /// </summary>
        public void SetupUI(Image image)
        {
            // Crosshair не использует SetupUI для настройки, так как использует дочерние Image
            // Метод оставлен для совместимости с IUIElement
        }

        /// <summary>
        /// Обновляет визуальные параметры прицела (можно вызвать из инспектора или кода)
        /// </summary>
        public void RefreshCrosshair()
        {
            SetupLines();
        }
    }
}

