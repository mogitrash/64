using UnityEngine;
using WAD64.Core;
using WAD64.Player;

namespace WAD64.Weapons
{
    /// <summary>
    /// Управляет оружием игрока: смена, стрельба, перезарядка.
    /// Интегрируется с InputHandler и PlayerController.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("Weapon Setup")]
        [SerializeField] private Weapon[] availableWeapons = new Weapon[0];
        [SerializeField] private int startingWeaponIndex = 0;
        [SerializeField] private bool autoSwitchOnEmpty = false;

        // State
        private Weapon currentWeapon;
        private int currentWeaponIndex = 0;
        private InputHandler inputHandler;

        // Events
        public System.Action<Weapon> OnWeaponChanged;
        public System.Action<Weapon> OnWeaponFired;
        public System.Action<Weapon> OnWeaponReloaded;

        // Properties
        public Weapon CurrentWeapon => currentWeapon;
        public bool HasWeapon => currentWeapon != null;
        public int WeaponCount => availableWeapons != null ? availableWeapons.Length : 0;
        public int CurrentWeaponIndex => currentWeaponIndex;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();

            InitializeWeapons();
        }

        private void Start()
        {
            // Регистрируем в CoreReferences
            CoreReferences.WeaponManager = this;

            // Подписываемся на события ввода
            SubscribeToInput();

            // Выбираем стартовое оружие
            if (availableWeapons.Length > 0 && startingWeaponIndex >= 0 && startingWeaponIndex < availableWeapons.Length)
            {
                SwitchToWeapon(startingWeaponIndex);
            }

        }

        private void InitializeWeapons()
        {
            if (availableWeapons == null)
            {
                availableWeapons = new Weapon[0];
                return;
            }

            // Убеждаемся что все оружия неактивны в начале
            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (availableWeapons[i] != null)
                {
                    availableWeapons[i].gameObject.SetActive(false);

                    // Подписываемся на события оружия
                    availableWeapons[i].OnWeaponFired += OnCurrentWeaponFired;
                    availableWeapons[i].OnReloadCompleted += OnCurrentWeaponReloaded;
                    availableWeapons[i].OnEmptyClip += OnCurrentWeaponEmpty;
                }
            }
        }

        private void SubscribeToInput()
        {
            if (inputHandler == null) return;

            inputHandler.OnFirePressed += OnFirePressed;
            inputHandler.OnReloadPressed += OnReloadPressed;
            inputHandler.OnWeaponSwitch += OnWeaponSwitch;
            inputHandler.OnWeaponNumberPressed += OnWeaponNumberPressed;
        }

        #region Weapon Control

        /// <summary>
        /// Переключается на указанное оружие
        /// </summary>
        public bool SwitchToWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= availableWeapons.Length || availableWeapons[weaponIndex] == null)
            {
                return false;
            }

            // Деактивируем текущее оружие
            if (currentWeapon != null)
            {
                currentWeapon.gameObject.SetActive(false);
            }

            // Активируем новое оружие
            currentWeaponIndex = weaponIndex;
            currentWeapon = availableWeapons[weaponIndex];
            currentWeapon.gameObject.SetActive(true);

            OnWeaponChanged?.Invoke(currentWeapon);

            return true;
        }

        /// <summary>
        /// Переключается на следующее оружие
        /// </summary>
        public void SwitchToNextWeapon()
        {
            if (availableWeapons.Length <= 1) return;

            int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;
            SwitchToWeapon(nextIndex);
        }

        /// <summary>
        /// Переключается на предыдущее оружие
        /// </summary>
        public void SwitchToPreviousWeapon()
        {
            if (availableWeapons.Length <= 1) return;

            int prevIndex = currentWeaponIndex - 1;
            if (prevIndex < 0) prevIndex = availableWeapons.Length - 1;

            SwitchToWeapon(prevIndex);
        }

        #endregion

        #region Input Events

        private void OnFirePressed()
        {
            if (currentWeapon == null) return;

            bool fired = currentWeapon.TryFire();

            if (fired)
            {
                // Автоматическая смена оружия при окончании патронов
                if (autoSwitchOnEmpty && currentWeapon.NeedsReload && availableWeapons.Length > 1)
                {
                    SwitchToNextWeapon();
                }
            }
        }

        private void OnReloadPressed()
        {
            if (currentWeapon == null)
            {
                return;
            }

            currentWeapon.TryReload();
        }

        private void OnWeaponSwitch(float direction)
        {
            if (availableWeapons.Length <= 1)
            {
                return;
            }

            if (direction > 0)
            {
                SwitchToNextWeapon();
            }
            else if (direction < 0)
            {
                SwitchToPreviousWeapon();
            }
        }

        private void OnWeaponNumberPressed(int weaponIndex)
        {
            if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
            {
                SwitchToWeapon(weaponIndex);
            }
        }

        #endregion

        #region Weapon Events

        private void OnCurrentWeaponFired()
        {
            OnWeaponFired?.Invoke(currentWeapon);
        }

        private void OnCurrentWeaponReloaded()
        {
            OnWeaponReloaded?.Invoke(currentWeapon);
        }

        private void OnCurrentWeaponEmpty()
        {
            // Автоматическая перезарядка если патронов нет
            if (currentWeapon.NeedsReload)
            {
                currentWeapon.TryReload();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализирует WeaponManager с массивом оружий (используется при автоматическом создании)
        /// </summary>
        public void InitializeWithWeapons(Weapon[] weapons)
        {
            availableWeapons = weapons ?? new Weapon[0];
            InitializeWeapons();

            if (availableWeapons.Length > 0)
            {
                SwitchToWeapon(0);
            }
        }

        /// <summary>
        /// Добавляет оружие в арсенал
        /// </summary>
        public void AddWeapon(Weapon weapon)
        {
            if (weapon == null) return;

            // Увеличиваем массив
            System.Array.Resize(ref availableWeapons, availableWeapons.Length + 1);
            availableWeapons[availableWeapons.Length - 1] = weapon;

            // Настраиваем оружие
            weapon.gameObject.SetActive(false);
            weapon.OnWeaponFired += OnCurrentWeaponFired;
            weapon.OnReloadCompleted += OnCurrentWeaponReloaded;
            weapon.OnEmptyClip += OnCurrentWeaponEmpty;
        }

        /// <summary>
        /// Убирает оружие из арсенала
        /// </summary>
        public void RemoveWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= availableWeapons.Length) return;

            Weapon weaponToRemove = availableWeapons[weaponIndex];
            if (weaponToRemove != null)
            {
                // Отписываемся от событий
                weaponToRemove.OnWeaponFired -= OnCurrentWeaponFired;
                weaponToRemove.OnReloadCompleted -= OnCurrentWeaponReloaded;
                weaponToRemove.OnEmptyClip -= OnCurrentWeaponEmpty;
            }

            // Удаляем из массива
            var newArray = new Weapon[availableWeapons.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (i != weaponIndex)
                {
                    newArray[newIndex++] = availableWeapons[i];
                }
            }
            availableWeapons = newArray;

            // Корректируем текущий индекс
            if (currentWeaponIndex >= weaponIndex && currentWeaponIndex > 0)
            {
                currentWeaponIndex--;
            }

            // Если удалили текущее оружие, переключаемся на другое
            if (currentWeapon == weaponToRemove && availableWeapons.Length > 0)
            {
                SwitchToWeapon(Mathf.Min(currentWeaponIndex, availableWeapons.Length - 1));
            }
            else if (availableWeapons.Length == 0)
            {
                currentWeapon = null;
                currentWeaponIndex = 0;
            }
        }

        /// <summary>
        /// Получает информацию обо всех оружиях для отладки
        /// </summary>
        public string GetDebugInfo()
        {
            if (availableWeapons.Length == 0)
                return "No weapons available";

            string info = $"Weapons ({currentWeaponIndex + 1}/{availableWeapons.Length}):\n";

            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (availableWeapons[i] != null)
                {
                    string marker = i == currentWeaponIndex ? ">>> " : "    ";
                    info += $"{marker}{i}: {availableWeapons[i].WeaponName} " +
                           $"({availableWeapons[i].CurrentAmmo}/{availableWeapons[i].MaxAmmo})\n";
                }
            }

            if (currentWeapon != null)
            {
                info += "\nCurrent Weapon Details:\n" + currentWeapon.GetDebugInfo();
            }

            return info;
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от событий ввода
            if (inputHandler != null)
            {
                inputHandler.OnFirePressed -= OnFirePressed;
                inputHandler.OnReloadPressed -= OnReloadPressed;
                inputHandler.OnWeaponSwitch -= OnWeaponSwitch;
                inputHandler.OnWeaponNumberPressed -= OnWeaponNumberPressed;
            }

            // Отписываемся от событий оружий
            if (availableWeapons != null)
            {
                for (int i = 0; i < availableWeapons.Length; i++)
                {
                    if (availableWeapons[i] != null)
                    {
                        availableWeapons[i].OnWeaponFired -= OnCurrentWeaponFired;
                        availableWeapons[i].OnReloadCompleted -= OnCurrentWeaponReloaded;
                        availableWeapons[i].OnEmptyClip -= OnCurrentWeaponEmpty;
                    }
                }
            }

            // Очищаем ссылку в CoreReferences
            if (CoreReferences.WeaponManager == this)
            {
                CoreReferences.WeaponManager = null;
            }
        }
    }
}