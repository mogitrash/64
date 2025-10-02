using UnityEngine;
using UnityEngine.UI;

namespace WAD64.UI
{
  /// <summary>
  /// Интерфейс для UI элементов, которые могут быть автоматически настроены.
  /// Заменяет использование рефлексии для настройки UI компонентов.
  /// </summary>
  public interface IUIElement
  {
    /// <summary>
    /// Настраивает UI компонент с указанным Image
    /// </summary>
    /// <param name="image">Image компонент для настройки</param>
    void SetupUI(Image image);
  }
}
