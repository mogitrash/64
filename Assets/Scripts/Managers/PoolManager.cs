using UnityEngine;
using System.Collections.Generic;
using WAD64.Core;

namespace WAD64.Managers
{
    /// <summary>
    /// Менеджер объектных пулов. Управляет переиспользованием объектов для оптимизации производительности.
    /// Особенно важен для пуль, эффектов частиц и других часто создаваемых/уничтожаемых объектов.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private PoolConfiguration[] poolConfigurations;
        [SerializeField] private int defaultPoolSize = 50;
        [SerializeField] private bool allowPoolExpansion = true;
        [SerializeField] private bool logPoolOperations = false;

        // Pools storage
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> poolPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();

        // Active objects tracking
        private Dictionary<GameObject, string> activeObjects = new Dictionary<GameObject, string>();

        private void Awake()
        {
            // Убеждаемся, что PoolManager единственный
            if (CoreReferences.PoolManager != null && CoreReferences.PoolManager != this)
            {
                Debug.LogWarning("[PoolManager] Another PoolManager already exists! Destroying this one.");
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        #region Initialization

        private void InitializePools()
        {
            foreach (var config in poolConfigurations)
            {
                if (config.prefab == null)
                {
                    Debug.LogWarning($"[PoolManager] Pool configuration for '{config.poolName}' has no prefab assigned!");
                    continue;
                }

                CreatePool(config.poolName, config.prefab, config.initialSize);
            }

            Log("All pools initialized");
        }

        public void CreatePool(string poolName, GameObject prefab, int initialSize = -1)
        {
            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[PoolManager] Pool '{poolName}' already exists!");
                return;
            }

            int size = initialSize > 0 ? initialSize : defaultPoolSize;
            Queue<GameObject> pool = new Queue<GameObject>();

            // Создаем родительский объект для организации иерархии
            GameObject poolParent = new GameObject($"Pool_{poolName}");
            poolParent.transform.SetParent(transform);
            poolParents[poolName] = poolParent.transform;

            // Заполняем пул объектами
            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, poolParent.transform);
                obj.name = $"{poolName}_{i}";
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            pools[poolName] = pool;
            poolPrefabs[poolName] = prefab;

            Log($"Created pool '{poolName}' with {size} objects");
        }

        #endregion

        #region Pool Operations

        public GameObject GetFromPool(string poolName)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"[PoolManager] Pool '{poolName}' does not exist!");
                return null;
            }

            Queue<GameObject> pool = pools[poolName];

            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                activeObjects[obj] = poolName;
                
                Log($"Retrieved object from pool '{poolName}' (remaining: {pool.Count})");
                return obj;
            }
            else if (allowPoolExpansion)
            {
                // Создаем новый объект, если пул пуст
                GameObject newObj = Instantiate(poolPrefabs[poolName], poolParents[poolName]);
                newObj.name = $"{poolName}_expanded_{System.Guid.NewGuid().ToString("N")[..8]}";
                newObj.SetActive(true);
                activeObjects[newObj] = poolName;
                
                Log($"Expanded pool '{poolName}' with new object");
                return newObj;
            }

            Debug.LogWarning($"[PoolManager] Pool '{poolName}' is empty and expansion is disabled!");
            return null;
        }

        public GameObject GetFromPool(string poolName, Vector3 position, Quaternion rotation)
        {
            GameObject obj = GetFromPool(poolName);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            if (activeObjects.TryGetValue(obj, out string poolName))
            {
                obj.SetActive(false);
                obj.transform.SetParent(poolParents[poolName]);
                
                // Сброс состояния объекта
                ResetObject(obj);
                
                pools[poolName].Enqueue(obj);
                activeObjects.Remove(obj);
                
                Log($"Returned object to pool '{poolName}' (total: {pools[poolName].Count})");
            }
            else
            {
                Debug.LogWarning($"[PoolManager] Trying to return object '{obj.name}' that doesn't belong to any pool!");
            }
        }

        public void ReturnToPool(GameObject obj, float delay)
        {
            if (delay <= 0f)
            {
                ReturnToPool(obj);
            }
            else
            {
                StartCoroutine(ReturnToPoolDelayed(obj, delay));
            }
        }

        private System.Collections.IEnumerator ReturnToPoolDelayed(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(obj);
        }

        #endregion

        #region Specialized Pool Methods

        public GameObject SpawnBullet(string bulletType, Vector3 position, Quaternion rotation)
        {
            return GetFromPool($"Bullet_{bulletType}", position, rotation);
        }

        public GameObject SpawnEffect(string effectType, Vector3 position)
        {
            GameObject effect = GetFromPool($"Effect_{effectType}", position, Quaternion.identity);
            
            // Автоматически возвращаем эффект в пул через время жизни частиц
            if (effect != null)
            {
                ParticleSystem particles = effect.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    float lifetime = particles.main.startLifetime.constantMax + particles.main.duration;
                    ReturnToPool(effect, lifetime);
                }
                else
                {
                    // Если нет системы частиц, возвращаем через 2 секунды по умолчанию
                    ReturnToPool(effect, 2f);
                }
            }
            
            return effect;
        }

        public GameObject SpawnEnemy(string enemyType, Vector3 position, Quaternion rotation)
        {
            return GetFromPool($"Enemy_{enemyType}", position, rotation);
        }

        #endregion

        #region Utility

        private void ResetObject(GameObject obj)
        {
            // Сбрасываем Rigidbody
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Сбрасываем системы частиц
            ParticleSystem particles = obj.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                particles.Stop();
                particles.Clear();
            }

            // Сбрасываем аниматоры
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Rebind();
            }

            // Можно добавить сброс других компонентов по необходимости
        }

        public void ClearPool(string poolName)
        {
            if (!pools.ContainsKey(poolName)) return;

            Queue<GameObject> pool = pools[poolName];
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }

            Log($"Cleared pool '{poolName}'");
        }

        public void ClearAllPools()
        {
            foreach (var poolName in pools.Keys)
            {
                ClearPool(poolName);
            }
            
            pools.Clear();
            poolPrefabs.Clear();
            activeObjects.Clear();
            
            Log("All pools cleared");
        }

        public int GetPoolSize(string poolName)
        {
            return pools.ContainsKey(poolName) ? pools[poolName].Count : 0;
        }

        public int GetActiveObjectCount(string poolName)
        {
            int count = 0;
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == poolName) count++;
            }
            return count;
        }

        #endregion

        #region Debug

        private void Log(string message)
        {
            if (logPoolOperations)
            {
                Debug.Log($"[PoolManager] {message}");
            }
        }

        public void LogPoolStatus()
        {
            Debug.Log("=== Pool Manager Status ===");
            foreach (var kvp in pools)
            {
                string poolName = kvp.Key;
                int available = kvp.Value.Count;
                int active = GetActiveObjectCount(poolName);
                Debug.Log($"Pool '{poolName}': Available={available}, Active={active}");
            }
        }

        #endregion

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }

    /// <summary>
    /// Конфигурация для создания пула объектов
    /// </summary>
    [System.Serializable]
    public class PoolConfiguration
    {
        public string poolName;
        public GameObject prefab;
        public int initialSize = 10;
    }
}