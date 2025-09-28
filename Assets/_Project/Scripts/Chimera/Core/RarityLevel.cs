namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// Rarity classification system for creatures, genetic traits, and items in Project Chimera.
    /// Determines spawn rates, genetic trait occurrence, and overall creature value.
    /// Higher rarity creatures have stronger traits but are much harder to obtain.
    /// </summary>
    public enum RarityLevel
    {
        /// <summary>Standard creatures with basic traits. 70% spawn rate. Common in all biomes.</summary>
        Common = 0,

        /// <summary>Enhanced creatures with one notable trait. 20% spawn rate. Requires specific conditions.</summary>
        Uncommon = 1,

        /// <summary>Superior creatures with multiple strong traits. 7% spawn rate. Found in optimal biomes.</summary>
        Rare = 2,

        /// <summary>Exceptional creatures with rare genetic combinations. 2.5% spawn rate. Requires perfect breeding.</summary>
        Epic = 3,

        /// <summary>Extraordinary creatures with unique abilities. 0.4% spawn rate. Extremely rare genetics.</summary>
        Legendary = 4,

        /// <summary>Ultimate creatures with world-altering powers. 0.1% spawn rate. Near-impossible to obtain.</summary>
        Mythic = 5
    }
}