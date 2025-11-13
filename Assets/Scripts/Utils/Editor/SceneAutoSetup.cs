using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WAD64.Weapons;
using WAD64.Player;
using WAD64.Managers;

namespace WAD64.Utils.Editor
{
    /// <summary>
    /// Автоматически настраивает ссылки в сцене после рефакторинга.
    /// Выполняется при открытии сцены.
    /// </summary>
    [InitializeOnLoad]
    public class SceneAutoSetup
    {
        static SceneAutoSetup()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (scene.name != "SampleScene") return;

            EditorApplication.delayCall += () =>
            {
                SetupSceneReferences();
            };
        }

        [MenuItem("WAD64/Setup Scene References")]
        public static void SetupSceneReferences()
        {
            // Настройка WeaponManager
            var weaponManager = Object.FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
            {
                // Находим оружие в сцене (дочерние объекты WeaponManager)
                var weapons = weaponManager.GetComponentsInChildren<Weapon>();
                if (weapons != null && weapons.Length > 0)
                {
                    // Используем SerializedObject для установки SerializeField
                    var serializedObject = new SerializedObject(weaponManager);
                    var availableWeaponsProperty = serializedObject.FindProperty("availableWeapons");
                    
                    if (availableWeaponsProperty != null)
                    {
                        availableWeaponsProperty.arraySize = weapons.Length;
                        for (int i = 0; i < weapons.Length; i++)
                        {
                            availableWeaponsProperty.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
                        }
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log($"WeaponManager: Назначено {weapons.Length} оружий из сцены.");
                    }
                }
            }

            // Настройка PlayerMovement - GroundCheck
            var playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                var groundCheck = playerMovement.transform.Find("GroundCheck");
                if (groundCheck != null)
                {
                    var serializedObject = new SerializedObject(playerMovement);
                    var groundCheckProperty = serializedObject.FindProperty("groundCheck");
                    
                    if (groundCheckProperty != null)
                    {
                        groundCheckProperty.objectReferenceValue = groundCheck;
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log("PlayerMovement: GroundCheck назначен.");
                    }
                }
            }

            // Настройка UIManager - ссылки на UI элементы
            var uiManager = Object.FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    // Ищем TextMeshProUGUI для FPS и Debug
                    var fpsText = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    var debugText = canvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                    
                    var serializedObject = new SerializedObject(uiManager);
                    
                    // Настраиваем fpsText (первый найденный)
                    if (debugText != null && debugText.Length > 0)
                    {
                        var fpsTextProperty = serializedObject.FindProperty("fpsText");
                        if (fpsTextProperty != null && debugText.Length > 0)
                        {
                            fpsTextProperty.objectReferenceValue = debugText[0];
                        }
                        
                        // Настраиваем debugText (второй найденный, если есть)
                        if (debugText.Length > 1)
                        {
                            var debugTextProperty = serializedObject.FindProperty("debugText");
                            if (debugTextProperty != null)
                            {
                                debugTextProperty.objectReferenceValue = debugText[1];
                            }
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("UIManager: UI ссылки настроены.");
                }
            }

            // Сохраняем сцену
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("Сцена настроена и помечена для сохранения.");
            }
        }
    }
}
