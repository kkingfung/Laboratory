namespace Laboratory.Core.Enums
{
    /// <summary>
    /// Types of damage that can be applied
    /// </summary>
    public enum DamageType : byte
    {
        Normal = 0,
        Critical = 1,
        Physical = 2,
        Fire = 3,
        Ice = 4,
        Lightning = 5,
        Poison = 6,
        Explosive = 7,
        Piercing = 8,
        Magic = 9,
        Fall = 10,
        Drowning = 11,
        True = 255  // True damage that bypasses all resistances
    }
}