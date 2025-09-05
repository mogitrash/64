using UnityEngine;

namespace WAD64.Weapons
{
    /// <summary>
    /// Дробовик - мощное оружие ближнего боя.
    /// Стреляет несколькими дробями одновременно с большим разбросом.
    /// </summary>
    public class Shotgun : Weapon
    {
        [Header("Shotgun Settings")]
        [SerializeField] private int pelletCount = 8;
        [SerializeField] private float pelletSpread = 0.15f; // больший разброс чем у пистолета
        [SerializeField] private bool createHitMarkers = true;
        [SerializeField] private float markerDuration = 1f;
        [SerializeField] private Color hitMarkerColor = Color.blue;
        [SerializeField] private float markerSize = 0.15f;

        // Debug visualization
        public struct ShotInfo
        {
            public Vector3 origin;
            public Vector3[] hitPoints;
            public bool[] hits;
            public float time;
            public int totalHits;
        }

        private ShotInfo lastShot;

        protected override void Awake()
        {
            base.Awake();

            // Настройки по умолчанию для дробовика
            if (weaponName == "Base Weapon")
                weaponName = "Shotgun";

            // Характеристики дробовика
            damage = 12f; // урон за одну дробь (итого до 96 урона при всех попаданиях)
            range = 25f; // меньшая дальность чем у пистолета
            fireRate = 0.8f; // медленнее чем пистолет
            reloadTime = 3f; // дольше перезарядка
            maxAmmo = 6; // меньше патронов
            spread = 0.05f; // базовый разброс (дополнительно к pelletSpread)
        }

        /// <summary>
        /// Выполняет выстрел дробовика с множественными рейкастами
        /// </summary>
        protected override void PerformShot(Vector3 direction)
        {
            if (playerCamera == null)
            {
                return;
            }

            Vector3 origin = playerCamera.transform.position;
            Vector3[] hitPoints = new Vector3[pelletCount];
            bool[] hits = new bool[pelletCount];
            int totalHits = 0;

            // Стреляем несколькими дробями
            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 pelletDirection = CalculatePelletDirection(direction);

                // Выполняем raycast для каждой дроби
                RaycastHit hit;
                bool hasHit = Physics.Raycast(origin, pelletDirection, out hit, range, hitLayers);

                hits[i] = hasHit;
                hitPoints[i] = hasHit ? hit.point : origin + pelletDirection * range;

                if (hasHit)
                {
                    totalHits++;
                    ProcessPelletHit(hit, i);
                }
            }

            // Сохраняем информацию о выстреле для отладки
            lastShot = new ShotInfo
            {
                origin = origin,
                hitPoints = hitPoints,
                hits = hits,
                time = Time.time,
                totalHits = totalHits
            };

        }

        /// <summary>
        /// Рассчитывает направление одной дроби с дополнительным разбросом
        /// </summary>
        private Vector3 CalculatePelletDirection(Vector3 baseDirection)
        {
            if (playerCamera == null)
                return baseDirection;

            // Добавляем случайный разброс специально для дроби
            float randomX = Random.Range(-pelletSpread, pelletSpread);
            float randomY = Random.Range(-pelletSpread, pelletSpread);

            Vector3 spreadOffset = playerCamera.transform.right * randomX +
                                 playerCamera.transform.up * randomY;

            return (baseDirection + spreadOffset).normalized;
        }

        /// <summary>
        /// Обрабатывает попадание одной дроби
        /// </summary>
        private void ProcessPelletHit(RaycastHit hit, int pelletIndex)
        {

            // Проверяем, есть ли у объекта компонент с уроном
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Эффекты попадания
            CreatePelletHitEffect(hit.point, hit.normal);
        }

        /// <summary>
        /// Создает эффекты попадания дроби
        /// </summary>
        private void CreatePelletHitEffect(Vector3 position, Vector3 normal)
        {
            if (createHitMarkers)
            {
                CreateHitMarker(position);
            }
        }

        /// <summary>
        /// Создает временную сферу в месте попадания дроби
        /// </summary>
        private void CreateHitMarker(Vector3 position)
        {
            // Создаем GameObject для маркера попадания
            GameObject hitMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitMarker.name = "ShotgunHitMarker";
            hitMarker.transform.position = position;
            hitMarker.transform.localScale = Vector3.one * markerSize;

            // Настраиваем материал
            Renderer renderer = hitMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Создаем новый материал с заданным цветом
                Material material = new Material(Shader.Find("Standard"));
                material.color = hitMarkerColor;
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Glossiness", 0.3f);
                renderer.material = material;
            }

            // Убираем коллайдер, чтобы не мешал
            Collider collider = hitMarker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            // Добавляем компонент для автоматического удаления
            hitMarker.AddComponent<AutoDestroyMarker>().duration = markerDuration;
        }

        /// <summary>
        /// Возвращает информацию о последнем выстреле для отладки
        /// </summary>
        public ShotInfo GetLastShotInfo()
        {
            return lastShot;
        }

        /// <summary>
        /// Проверяет, был ли выстрел недавно (для отображения информации)
        /// </summary>
        public bool HasRecentShot()
        {
            return lastShot.time > 0 && (Time.time - lastShot.time) < 2f;
        }

        public override string GetDebugInfo()
        {
            string baseInfo = base.GetDebugInfo();

            baseInfo += $"\nPellets: {pelletCount}";
            baseInfo += $"\nPellet Spread: {pelletSpread:F3}";

            if (lastShot.time > 0)
            {
                float timeSinceShot = Time.time - lastShot.time;
                string shotInfo = $"\nLast Shot ({timeSinceShot:F2}s ago):\n" +
                                $"  Hits: {lastShot.totalHits}/{pelletCount}\n" +
                                $"  Hit Rate: {(lastShot.totalHits / (float)pelletCount):P0}";
                baseInfo += shotInfo;
            }

            return baseInfo;
        }
    }
}