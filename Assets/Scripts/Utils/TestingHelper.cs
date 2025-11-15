using UnityEngine;
using WAD64.Core;

namespace WAD64.Utils
{
    /// <summary>
    /// Помощник для тестирования механик игрока.
    /// Отображает информацию о состоянии игрока и предоставляет debug-функции.
    /// </summary>
    public class TestingHelper : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showPlayerInfo = true;
        [SerializeField] private bool showInputInfo = true;
        [SerializeField] private bool showWeaponInfo = true;
        [SerializeField] private bool showDebugKeys = true;
        [SerializeField] private KeyCode toggleInfoKey = KeyCode.F1;
        [SerializeField] private KeyCode damageTestKey = KeyCode.F2;
        [SerializeField] private KeyCode teleportTestKey = KeyCode.F3;

        private bool displayEnabled = false;
        private GUIStyle labelStyle;
        private GUIStyle boxStyle;

        private void Start()
        {
            // Настраиваем стили для GUI
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 8;

            boxStyle = new GUIStyle();
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.7f));
        }

        private void Update()
        {
            HandleDebugInput();
        }

        private void HandleDebugInput()
        {
            // Переключение отображения информации
            if (Input.GetKeyDown(toggleInfoKey))
            {
                displayEnabled = !displayEnabled;
            }

            // Тест урона
            if (Input.GetKeyDown(damageTestKey))
            {
                TestDamage();
            }

            // Тест телепортации
            if (Input.GetKeyDown(teleportTestKey))
            {
                TestTeleport();
            }

            // Тест camera shake
            if (Input.GetKeyDown(KeyCode.F4))
            {
                TestCameraShake();
            }

            // Тест принудительной перезарядки
            if (Input.GetKeyDown(KeyCode.F5))
            {
                TestForceReload();
            }

            // Тест выстрела
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TestShot();
            }
        }

        private void OnGUI()
        {
            if (!displayEnabled) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 500), boxStyle);

            GUILayout.Label("=== 64.WAD Testing Helper ===", labelStyle);
            GUILayout.Space(10);

            if (showDebugKeys)
            {
                DisplayDebugKeys();
                GUILayout.Space(10);
            }

            if (showPlayerInfo)
            {
                DisplayPlayerInfo();
                GUILayout.Space(10);
            }

            if (showInputInfo)
            {
                DisplayInputInfo();
                GUILayout.Space(10);
            }

            if (showWeaponInfo)
            {
                DisplayWeaponInfo();
            }

            GUILayout.EndArea();


        }

        private void DisplayDebugKeys()
        {
            GUILayout.Label("=== Debug Keys ===", labelStyle);
            GUILayout.Label($"{toggleInfoKey}: Toggle Info Display", labelStyle);
            GUILayout.Label($"{damageTestKey}: Test Damage (10 HP)", labelStyle);
            GUILayout.Label($"{teleportTestKey}: Teleport to Spawn", labelStyle);
            GUILayout.Label("F4: Test Camera Shake", labelStyle);
            GUILayout.Label("F5: Force Reload Weapon", labelStyle);
            GUILayout.Label("F6: Test Shot", labelStyle);
            GUILayout.Label("F9: Restart Level", labelStyle);
            GUILayout.Label("1/2: Switch Weapons", labelStyle);
            GUILayout.Label("Mouse Wheel: Cycle Weapons", labelStyle);
            GUILayout.Label("P: Pause/Resume Game", labelStyle);
        }

        private void DisplayPlayerInfo()
        {
            if (CoreReferences.Player == null)
            {
                GUILayout.Label("=== Player Info ===", labelStyle);
                GUILayout.Label("Player not found!", labelStyle);
                return;
            }

            var player = CoreReferences.Player as WAD64.Player.PlayerController;
            var movement = CoreReferences.PlayerMovement as WAD64.Player.PlayerMovement;
            var health = CoreReferences.PlayerHealth as WAD64.Player.PlayerHealth;

            GUILayout.Label("=== Player Info ===", labelStyle);

            if (player != null)
            {
                GUILayout.Label($"Enabled: {player.IsEnabled}", labelStyle);
                GUILayout.Label($"Alive: {player.IsAlive}", labelStyle);
                GUILayout.Label($"Position: {player.Position}", labelStyle);
            }

            if (movement != null)
            {
                GUILayout.Label($"Grounded: {movement.IsGrounded}", labelStyle);
                GUILayout.Label($"Jumping: {movement.IsJumping}", labelStyle);
                GUILayout.Label($"Falling: {movement.IsFalling}", labelStyle);
                GUILayout.Label($"Speed: {movement.CurrentSpeed:F1}", labelStyle);
                GUILayout.Label($"Velocity: {movement.Velocity}", labelStyle);
            }

            if (health != null)
            {
                GUILayout.Label($"Health: {health.CurrentHealth:F0}/{health.MaxHealth:F0}", labelStyle);
                GUILayout.Label($"Armor: {health.CurrentArmor:F0}/{health.MaxArmor:F0}", labelStyle);
                GUILayout.Label($"Invincible: {health.IsInvincible}", labelStyle);
                GUILayout.Label($"Regenerating: {health.IsRegenerating}", labelStyle);
            }
        }

        private void DisplayInputInfo()
        {
            var player = CoreReferences.Player as WAD64.Player.PlayerController;
            if (player?.Input == null)
            {
                GUILayout.Label("=== Input Info ===", labelStyle);
                GUILayout.Label("Input handler not found!", labelStyle);
                return;
            }

            var input = player.Input;

            GUILayout.Label("=== Input Info ===", labelStyle);
            GUILayout.Label($"Move Input: {input.MoveInput}", labelStyle);
            GUILayout.Label($"Look Input: {input.LookInput}", labelStyle);
            GUILayout.Label($"Running: {input.IsRunning}", labelStyle);
            GUILayout.Label($"Fire Held: {input.IsFireHeld}", labelStyle);
            GUILayout.Label($"Jump Buffered: {input.HasJumpBuffered}", labelStyle);
            GUILayout.Label($"Fire Buffered: {input.HasFireBuffered}", labelStyle);
        }

        private void DisplayWeaponInfo()
        {
            var player = CoreReferences.Player as WAD64.Player.PlayerController;
            var weaponManager = CoreReferences.WeaponManager as WAD64.Weapons.WeaponManager;

            GUILayout.Label("=== Weapon Info ===", labelStyle);

            // Расширенная диагностика
            GUILayout.Label($"Player.Weapons: {(player?.Weapons != null ? "Found" : "NULL")}", labelStyle);
            GUILayout.Label($"CoreReferences.WeaponManager: {(weaponManager != null ? "Found" : "NULL")}", labelStyle);

            if (player?.Weapons == null && weaponManager == null)
            {
                GUILayout.Label("WeaponManager not found!", labelStyle);
                GUILayout.Label("Make sure WeaponManager is child of Player!", labelStyle);
                return;
            }

            var manager = player?.Weapons ?? weaponManager;

            GUILayout.Label($"Weapon Count: {manager.WeaponCount}", labelStyle);

            if (!manager.HasWeapon)
            {
                GUILayout.Label("No weapon equipped!", labelStyle);
                GUILayout.Label("Add weapons to WeaponManager in inspector!", labelStyle);
                return;
            }

            var weapon = manager.CurrentWeapon;
            GUILayout.Label($"Current: {weapon.WeaponName}", labelStyle);
            GUILayout.Label($"Ammo: {weapon.CurrentAmmo}/{weapon.MaxAmmo}", labelStyle);
            GUILayout.Label($"Damage: {weapon.Damage}", labelStyle);
            GUILayout.Label($"Range: {weapon.Range}m", labelStyle);
            GUILayout.Label($"Can Fire: {weapon.CanFire}", labelStyle);
            GUILayout.Label($"Reloading: {weapon.IsReloading} ({weapon.ReloadProgress:P0})", labelStyle);
        }

        #region Test Functions

        private void TestDamage()
        {
            var health = CoreReferences.PlayerHealth as WAD64.Player.PlayerHealth;
            if (health != null)
            {
                health.TakeDamage(10f, WAD64.Player.DamageType.Normal);
            }
        }

        private void TestTeleport()
        {
            var player = CoreReferences.Player as WAD64.Player.PlayerController;
            if (player != null)
            {
                // Телепортируем к PlayerSpawn
                GameObject playerSpawn = GameObject.Find("PlayerSpawn");
                if (playerSpawn != null)
                {
                    player.Teleport(playerSpawn.transform.position);
                }
                else
                {
                    player.Teleport(Vector3.zero + Vector3.up);
                }
            }
        }

        private void TestCameraShake()
        {
            var camera = CoreReferences.PlayerCamera as WAD64.Player.PlayerCamera;
            if (camera != null)
            {
                camera.AddCameraShake(1f, 0.5f);
            }
        }

        private void TestForceReload()
        {
            var weaponManager = CoreReferences.WeaponManager as WAD64.Weapons.WeaponManager;
            if (weaponManager?.CurrentWeapon != null)
            {
                weaponManager.CurrentWeapon.ForceReload();
            }
        }

        private void TestShot()
        {
            var weaponManager = CoreReferences.WeaponManager as WAD64.Weapons.WeaponManager;
            if (weaponManager?.CurrentWeapon != null)
            {
                bool fired = weaponManager.CurrentWeapon.TryFire();
            }
        }

        #endregion

        #region Utility

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}