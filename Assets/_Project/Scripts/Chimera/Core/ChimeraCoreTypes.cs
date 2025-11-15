namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// Rarity levels for creatures and items
    /// </summary>
    public enum RarityLevel : byte
    {
        /// <summary>Common creatures - found frequently</summary>
        Common = 0,
        /// <summary>Uncommon creatures - moderately rare</summary>
        Uncommon = 1,
        /// <summary>Rare creatures - hard to find</summary>
        Rare = 2,
        /// <summary>Epic creatures - very rare</summary>
        Epic = 3,
        /// <summary>Legendary creatures - extremely rare</summary>
        Legendary = 4,
        /// <summary>Mythical creatures - nearly impossible to find</summary>
        Mythical = 5,
        /// <summary>Unique creatures - one of a kind</summary>
        Unique = 255
    }

    /// <summary>
    /// Types of creature attributes
    /// </summary>
    public enum AttributeType : byte
    {
        /// <summary>Physical strength</summary>
        Strength = 0,
        /// <summary>Physical agility and speed</summary>
        Agility = 1,
        /// <summary>Mental intelligence</summary>
        Intelligence = 2,
        /// <summary>Physical constitution and health</summary>
        Constitution = 3,
        /// <summary>Magical affinity</summary>
        Magic = 4,
        /// <summary>Social charisma</summary>
        Charisma = 5
    }

    /// <summary>
    /// Creature element affinities
    /// </summary>
    public enum ElementType : byte
    {
        /// <summary>No elemental affinity</summary>
        None = 0,
        /// <summary>Fire element</summary>
        Fire = 1,
        /// <summary>Water element</summary>
        Water = 2,
        /// <summary>Earth element</summary>
        Earth = 3,
        /// <summary>Air element</summary>
        Air = 4,
        /// <summary>Lightning element</summary>
        Lightning = 5,
        /// <summary>Ice element</summary>
        Ice = 6,
        /// <summary>Nature element</summary>
        Nature = 7,
        /// <summary>Dark element</summary>
        Dark = 8,
        /// <summary>Light element</summary>
        Light = 9
    }
}