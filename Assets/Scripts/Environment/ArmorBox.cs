using UnityEngine;

namespace WAD64.Environment
{
    /// <summary>
    /// Ящик с броней. Восстанавливает броню игрока при контакте.
    /// Реализует интерфейс IInteractable для автоматической активации.
    /// </summary>
    public class ArmorBox : MonoBehaviour, IInteractable
    {
        [Header("Armor Settings")]
        [SerializeField] private float armorAmount = 50f;

        [Header("Behavior Settings")]
        [SerializeField] private bool destroyOnInteract = false;

        private bool hasBeenUsed = false;

        private void OnTriggerEnter(Collider other)
        {
            // Проверяем, что объект еще не был использован
            if (hasBeenUsed)
                return;

            // Ищем компонент PlayerController на объекте, вошедшем в триггер
            WAD64.Player.PlayerController playerController = other.GetComponent<WAD64.Player.PlayerController>();

            // Если найден игрок, взаимодействуем с ним
            if (playerController != null)
            {
                Interact(playerController);
            }
        }

        /// <summary>
        /// Реализация интерфейса IInteractable.
        /// Восстанавливает броню игрока и деактивирует/уничтожает объект.
        /// </summary>
        /// <param name="player">Контроллер игрока</param>
        public void Interact(WAD64.Player.PlayerController player)
        {
            if (hasBeenUsed)
                return;

            hasBeenUsed = true;

            // Восстанавливаем броню игрока
            if (player.Health != null)
            {
                player.Health.AddArmor(armorAmount);
            }

            // Деактивируем или уничтожаем объект
            if (destroyOnInteract)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Сброс состояния объекта (для переиспользования или тестирования)
        /// </summary>
        [ContextMenu("Reset Armor Box")]
        public void ResetBox()
        {
            hasBeenUsed = false;
            gameObject.SetActive(true);
        }

        private void OnDrawGizmosSelected()
        {
            // Показываем границы триггера в редакторе
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(transform.position, collider.bounds.size);
            }
        }
    }
}
