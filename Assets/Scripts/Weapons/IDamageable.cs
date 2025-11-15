namespace WAD64.Weapons
{
  /// <summary>
  /// Интерфейс для объектов, которые могут получать урон от оружия.
  /// Реализуется компонентами здоровья врагов и игрока.
  /// </summary>
  public interface IDamageable
  {
    /// <summary>
    /// Применяет урон к объекту
    /// </summary>
    /// <param name="damage">Количество урона</param>
    void TakeDamage(float damage);
  }
}

