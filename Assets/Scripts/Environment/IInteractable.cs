using UnityEngine;

namespace WAD64.Environment
{
    /// <summary>
    /// Интерфейс для интерактивных объектов, с которыми может взаимодействовать игрок.
    /// Объекты реализующие этот интерфейс могут быть активированы при контакте с игроком.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Вызывается когда игрок взаимодействует с объектом.
        /// Обычно вызывается при контакте через триггер.
        /// </summary>
        /// <param name="player">Контроллер игрока, инициировавшего взаимодействие</param>
        void Interact(WAD64.Player.PlayerController player);
    }
}
