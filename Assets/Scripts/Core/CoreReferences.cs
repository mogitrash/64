using UnityEngine;

namespace WAD64.Core
{
    /// <summary>
    /// Централизованное хранилище ссылок на основные системы проекта.
    /// Инициализируется через GameEntryPoint и используется для быстрого доступа к ключевым компонентам.
    /// </summary>
    public static class CoreReferences
    {
        // === PLAYER REFERENCES ===
        public static MonoBehaviour Player { get; set; }
        public static MonoBehaviour PlayerMovement { get; set; }
        public static MonoBehaviour PlayerHealth { get; set; }
        public static MonoBehaviour WeaponManager { get; set; }

        // === MANAGERS ===
        public static MonoBehaviour GameManager { get; set; }
        public static MonoBehaviour AudioManager { get; set; }
        public static MonoBehaviour PoolManager { get; set; }
        public static MonoBehaviour UIManager { get; set; }

        // === CAMERA ===
        public static Camera MainCamera { get; set; }
        public static MonoBehaviour PlayerCamera { get; set; }

        // === LEVEL REFERENCES ===
        public static Transform LevelRoot { get; set; }
        public static Transform EnemySpawnRoot { get; set; }
        public static Transform PickupSpawnRoot { get; set; }

        /// <summary>
        /// Проверяет, что все критически важные ссылки инициализированы
        /// </summary>
        public static bool AreEssentialReferencesInitialized()
        {
            return Player != null &&
                   MainCamera != null;
            // GameManager пока не обязателен для тестирования
        }

        /// <summary>
        /// Очищает все ссылки (используется при смене уровня или перезапуске)
        /// </summary>
        public static void ClearReferences()
        {
            Player = null;
            PlayerMovement = null;
            PlayerHealth = null;
            WeaponManager = null;

            GameManager = null;
            AudioManager = null;
            PoolManager = null;
            UIManager = null;

            MainCamera = null;
            PlayerCamera = null;

            LevelRoot = null;
            EnemySpawnRoot = null;
            PickupSpawnRoot = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Debug-информация о состоянии ссылок (только в редакторе)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LogReferenceStatus()
        {
            Debug.Log($"[CoreReferences] Essential references initialized: {AreEssentialReferencesInitialized()}");
        }
#endif
    }
}