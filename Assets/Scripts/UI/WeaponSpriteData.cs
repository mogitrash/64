using UnityEngine;

namespace WAD64.UI
{
    /// <summary>
    /// Данные анимации спрайтов оружия в стиле DOOM.
    /// Создается как ScriptableObject для каждого типа оружия.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponSpriteData", menuName = "WAD64/Weapon Sprite Data")]
    public class WeaponSpriteData : ScriptableObject
    {
        [Header("Weapon Info")]
        [Tooltip("Название оружия (должно совпадать с WeaponName)")]
        public string weaponName;

        [Header("Idle Animation")]
        [Tooltip("Спрайты для idle анимации (циклическая)")]
        public Sprite[] idleSprites = new Sprite[0];

        [Tooltip("Скорость idle анимации (кадров в секунду)")]
        public float idleAnimationSpeed = 10f;

        [Header("Fire Animation")]
        [Tooltip("Спрайты для анимации выстрела (проигрывается один раз)")]
        public Sprite[] fireSprites = new Sprite[0];

        [Tooltip("Скорость анимации выстрела (кадров в секунду)")]
        public float fireAnimationSpeed = 20f;

        [Header("Reload Animation")]
        [Tooltip("Спрайты для анимации перезарядки (проигрывается один раз, синхронизируется с реальным временем перезарядки)")]
        public Sprite[] reloadSprites = new Sprite[0];

        [Header("Empty Animation")]
        [Tooltip("Спрайт для пустой обоймы (опционально)")]
        public Sprite emptySprite;

        /// <summary>
        /// Проверяет валидность данных
        /// </summary>
        public bool IsValid()
        {
            return idleSprites != null && idleSprites.Length > 0;
        }
    }
}

