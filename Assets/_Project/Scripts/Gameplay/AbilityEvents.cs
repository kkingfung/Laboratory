namespace Laboratory.Gameplay.Abilities
{
    #region Event Records

    /// <summary>
    /// Event triggered when an ability's state changes (e.g., cooldown).
    /// </summary>
    public record AbilityStateChangedEvent(int AbilityIndex, bool IsOnCooldown, float CooldownRemaining);

    /// <summary>
    /// Event triggered when an ability is activated.
    /// </summary>
    public record AbilityActivatedEvent(int AbilityIndex);

    #endregion
}
