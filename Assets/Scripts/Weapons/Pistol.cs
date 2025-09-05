using UnityEngine;

namespace WAD64.Weapons
{
    /// <summary>
    /// Пистолет - точное оружие средней дальности.
    /// Использует hitscan для мгновенных попаданий.
    /// </summary>
    public class Pistol : Weapon
    {
        [Header("Pistol Settings")]
        [SerializeField] private bool createHitMarkers = true;
        [SerializeField] private float markerDuration = 1f;
        [SerializeField] private Color hitMarkerColor = Color.red;
        [SerializeField] private float markerSize = 0.2f;

        // Debug visualization
        public struct ShotInfo
        {
            public Vector3 origin;
            public Vector3 hitPoint;
            public float time;
            public bool hit;
        }

        private ShotInfo lastShot;

        protected override void Awake()
        {
            base.Awake();

            // Настройки по умолчанию для пистолета
            if (weaponName == "Base Weapon")
                weaponName = "Pistol";

            // Стандартные характеристики пистолета
            damage = 25f;
            range = 50f;
            fireRate = 3f; // 3 выстрела в секунду
            reloadTime = 2f;
            maxAmmo = 12;
            spread = 0.01f; // небольшой разброс
        }

        /// <summary>
        /// Выполняет выстрел из пистолета с использованием raycast
        /// </summary>
        protected override void PerformShot(Vector3 direction)
        {
            if (playerCamera == null)
            {
                return;
            }

            Vector3 origin = playerCamera.transform.position;

            // Выполняем raycast
            RaycastHit hit;
            bool hasHit = Physics.Raycast(origin, direction, out hit, range, hitLayers);

            // Сохраняем информацию о выстреле для отладки
            lastShot = new ShotInfo
            {
                origin = origin,
                hitPoint = hasHit ? hit.point : origin + direction * range,
                time = Time.time,
                hit = hasHit
            };

            if (hasHit)
            {
                ProcessHit(hit);
            }
        }

        /// <summary>
        /// Обрабатывает попадание
        /// </summary>
        private void ProcessHit(RaycastHit hit)
        {

            // Проверяем, есть ли у объекта компонент с уроном
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Эффекты попадания (в будущем можно добавить частицы пыли/крови)
            CreateHitEffect(hit.point, hit.normal);
        }

        /// <summary>
        /// Создает эффекты попадания
        /// </summary>
        private void CreateHitEffect(Vector3 position, Vector3 normal)
        {
            if (createHitMarkers)
            {
                CreateHitMarker(position);
            }
        }

        /// <summary>
        /// Создает временную сферу в месте попадания
        /// </summary>
        private void CreateHitMarker(Vector3 position)
        {
            // Создаем GameObject для маркера попадания
            GameObject hitMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitMarker.name = "HitMarker";
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
                material.SetFloat("_Glossiness", 0.5f);
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

            if (lastShot.time > 0)
            {
                float timeSinceShot = Time.time - lastShot.time;
                string shotInfo = $"\nLast Shot ({timeSinceShot:F2}s ago):\n" +
                                $"  Hit: {lastShot.hit}\n" +
                                $"  Distance: {Vector3.Distance(lastShot.origin, lastShot.hitPoint):F1}m";
                baseInfo += shotInfo;
            }

            return baseInfo;
        }
    }

    /// <summary>
    /// Компонент для автоматического удаления маркеров попадания
    /// </summary>
    public class AutoDestroyMarker : MonoBehaviour
    {
        public float duration = 1f;
        private float timer;

        private void Start()
        {
            timer = duration;
        }

        private void Update()
        {
            timer -= Time.deltaTime;

            // Плавное исчезновение
            if (timer <= 0.2f)
            {
                float alpha = timer / 0.2f;
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }

                transform.localScale = Vector3.one * (timer / 0.2f) * 0.2f;
            }

            if (timer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Интерфейс для объектов, которые могут получать урон
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}