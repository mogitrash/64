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
        private Weapon[] instantiatedWeapons; // Инстанцированные оружия
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
        public int WeaponCount => instantiatedWeapons != null ? instantiatedWeapons.Length : 0;
        public int CurrentWeaponIndex => currentWeaponIndex;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();

            // Валидация: проверяем наличие оружия на сцене
            if (availableWeapons == null || availableWeapons.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name}: availableWeapons пуст! Добавьте оружие в сцену и назначьте в Inspector.");
            }

            InitializeWeapons();
        }

        private void Start()
        {
            // Регистрируем в CoreReferences
            CoreReferences.WeaponManager = this;

            // Подписываемся на события ввода
            SubscribeToInput();

            // Выбираем стартовое оружие
            if (instantiatedWeapons != null && instantiatedWeapons.Length > 0 &&
                startingWeaponIndex >= 0 && startingWeaponIndex < instantiatedWeapons.Length)
            {
                SwitchToWeapon(startingWeaponIndex);
            }

        }

        private void InitializeWeapons()
        {
            if (availableWeapons == null || availableWeapons.Length == 0)
            {
                instantiatedWeapons = new Weapon[0];
                return;
            }

            // Работаем только с инстансами, которые уже есть в сцене
            instantiatedWeapons = new Weapon[availableWeapons.Length];

            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (availableWeapons[i] == null)
                {
                    Debug.LogWarning($"{gameObject.name}: availableWeapons[{i}] не назначен!");
                    continue;
                }

                // Проверяем, что это инстанс в сцене, а не префаб
                if (availableWeapons[i].gameObject.scene.name == null)
                {
                    Debug.LogError($"{gameObject.name}: availableWeapons[{i}] является префабом! Используйте инстансы из сцены. Добавьте оружие в сцену и назначьте в Inspector.");
                    continue;
                }

                // Используем инстанс из сцены напрямую
                instantiatedWeapons[i] = availableWeapons[i];

                if (instantiatedWeapons[i] != null)
                {
                    // Деактивируем оружие
                    instantiatedWeapons[i].gameObject.SetActive(false);

                    // Подписываемся на события оружия
                    instantiatedWeapons[i].OnWeaponFired += OnCurrentWeaponFired;
                    instantiatedWeapons[i].OnReloadCompleted += OnCurrentWeaponReloaded;
                    instantiatedWeapons[i].OnEmptyClip += OnCurrentWeaponEmpty;
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
            if (instantiatedWeapons == null || weaponIndex < 0 ||
                weaponIndex >= instantiatedWeapons.Length || instantiatedWeapons[weaponIndex] == null)
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
            currentWeapon = instantiatedWeapons[weaponIndex];
            currentWeapon.gameObject.SetActive(true);

            OnWeaponChanged?.Invoke(currentWeapon);

            return true;
        }

        /// <summary>
        /// Переключается на следующее оружие
        /// </summary>
        public void SwitchToNextWeapon()
        {
            if (instantiatedWeapons == null || instantiatedWeapons.Length <= 1) return;

            int nextIndex = (currentWeaponIndex + 1) % instantiatedWeapons.Length;
            SwitchToWeapon(nextIndex);
        }

        /// <summary>
        /// Переключается на предыдущее оружие
        /// </summary>
        public void SwitchToPreviousWeapon()
        {
            if (instantiatedWeapons == null || instantiatedWeapons.Length <= 1) return;

            int prevIndex = currentWeaponIndex - 1;
            if (prevIndex < 0) prevIndex = instantiatedWeapons.Length - 1;

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
                if (autoSwitchOnEmpty && currentWeapon.NeedsReload &&
                    instantiatedWeapons != null && instantiatedWeapons.Length > 1)
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
            if (instantiatedWeapons == null || instantiatedWeapons.Length <= 1)
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
            if (instantiatedWeapons != null && weaponIndex >= 0 && weaponIndex < instantiatedWeapons.Length)
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

            if (instantiatedWeapons != null && instantiatedWeapons.Length > 0)
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

            // Увеличиваем массивы
            System.Array.Resize(ref availableWeapons, availableWeapons.Length + 1);
            availableWeapons[availableWeapons.Length - 1] = weapon;

            if (instantiatedWeapons == null)
            {
                instantiatedWeapons = new Weapon[1];
            }
            else
            {
                System.Array.Resize(ref instantiatedWeapons, instantiatedWeapons.Length + 1);
            }
            instantiatedWeapons[instantiatedWeapons.Length - 1] = weapon;

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
            if (instantiatedWeapons == null || weaponIndex < 0 || weaponIndex >= instantiatedWeapons.Length) return;

            Weapon weaponToRemove = instantiatedWeapons[weaponIndex];
            if (weaponToRemove != null)
            {
                // Отписываемся от событий
                weaponToRemove.OnWeaponFired -= OnCurrentWeaponFired;
                weaponToRemove.OnReloadCompleted -= OnCurrentWeaponReloaded;
                weaponToRemove.OnEmptyClip -= OnCurrentWeaponEmpty;
            }

            // Удаляем из массива инстанцированных оружий
            var newArray = new Weapon[instantiatedWeapons.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < instantiatedWeapons.Length; i++)
            {
                if (i != weaponIndex)
                {
                    newArray[newIndex++] = instantiatedWeapons[i];
                }
            }
            instantiatedWeapons = newArray;

            // Удаляем из массива префабов
            if (availableWeapons != null && weaponIndex < availableWeapons.Length)
            {
                var newPrefabArray = new Weapon[availableWeapons.Length - 1];
                newIndex = 0;
                for (int i = 0; i < availableWeapons.Length; i++)
                {
                    if (i != weaponIndex)
                    {
                        newPrefabArray[newIndex++] = availableWeapons[i];
                    }
                }
                availableWeapons = newPrefabArray;
            }

            // Корректируем текущий индекс
            if (currentWeaponIndex >= weaponIndex && currentWeaponIndex > 0)
            {
                currentWeaponIndex--;
            }

            // Если удалили текущее оружие, переключаемся на другое
            if (currentWeapon == weaponToRemove && instantiatedWeapons.Length > 0)
            {
                SwitchToWeapon(Mathf.Min(currentWeaponIndex, instantiatedWeapons.Length - 1));
            }
            else if (instantiatedWeapons.Length == 0)
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
            if (instantiatedWeapons == null || instantiatedWeapons.Length == 0)
                return "No weapons available";

            string info = $"Weapons ({currentWeaponIndex + 1}/{instantiatedWeapons.Length}):\n";

            for (int i = 0; i < instantiatedWeapons.Length; i++)
            {
                if (instantiatedWeapons[i] != null)
                {
                    string marker = i == currentWeaponIndex ? ">>> " : "    ";
                    info += $"{marker}{i}: {instantiatedWeapons[i].WeaponName} " +
                           $"({instantiatedWeapons[i].CurrentAmmo}/{instantiatedWeapons[i].MaxAmmo})\n";
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
            if (instantiatedWeapons != null)
            {
                for (int i = 0; i < instantiatedWeapons.Length; i++)
                {
                    if (instantiatedWeapons[i] != null)
                    {
                        instantiatedWeapons[i].OnWeaponFired -= OnCurrentWeaponFired;
                        instantiatedWeapons[i].OnReloadCompleted -= OnCurrentWeaponReloaded;
                        instantiatedWeapons[i].OnEmptyClip -= OnCurrentWeaponEmpty;
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