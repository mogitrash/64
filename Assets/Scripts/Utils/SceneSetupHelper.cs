using UnityEngine;
using WAD64.Weapons;
using WAD64.Player;
using WAD64.Managers;

namespace WAD64.Utils
{
    /// <summary>
    /// Временный хелпер для настройки сцены после рефакторинга.
    /// Настраивает ссылки на объекты в сцене вместо префабов.
    /// </summary>
    public class SceneSetupHelper : MonoBehaviour
    {
        [ContextMenu("Setup Scene References")]
        public void SetupSceneReferences()
        {
            // Настройка WeaponManager
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
            {
                // Находим оружие в сцене
                var weapons = weaponManager.GetComponentsInChildren<Weapon>();
                if (weapons != null && weapons.Length > 0)
                {
                    // Используем рефлексию для установки SerializeField
                    var field = typeof(WeaponManager).GetField("availableWeapons", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(weaponManager, weapons);
                        Debug.Log($"WeaponManager: Назначено {weapons.Length} оружий из сцены.");
                    }
                }
            }

            // Настройка PlayerMovement - GroundCheck
            var playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                var groundCheck = playerMovement.transform.Find("GroundCheck");
                if (groundCheck != null)
                {
                    var field = typeof(PlayerMovement).GetField("groundCheck", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(playerMovement, groundCheck);
                        Debug.Log("PlayerMovement: GroundCheck назначен.");
                    }
                }
            }

            Debug.Log("Настройка сцены завершена!");
        }
    }
}
