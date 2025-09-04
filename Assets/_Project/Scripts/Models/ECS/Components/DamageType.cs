namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Temporary stub for DamageType enum.
    /// TODO: Move this to the appropriate assembly or implement properly.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magic,
        Fire,
        Ice,
        Lightning,
        Poison,
        True, // True damage that ignores resistances
        Normal, // Standard physical damage
        Environmental, // Damage from environmental hazards
        Critical, // Critical damage multiplier
        InstantKill, // Instant death damage
        Piercing, // Piercing damage that bypasses some armor
        Explosive // Area damage from explosions
    }
}
