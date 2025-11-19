using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Core.Equipment;

namespace Laboratory.Chimera.Equipment
{
    /// <summary>
    /// PERSONALITY EQUIPMENT COMPONENT
    ///
    /// NEW VISION: Equipment affects personality > stats
    ///
    /// Design Philosophy:
    /// - Equipment modifies personality traits temporarily
    /// - Different equipment fits different personalities (preferences)
    /// - Equipment affects cooperation and mood, not raw stats
    /// - Victory comes from player skill + chimera cooperation
    /// - Elderly chimeras resist personality changes (auto-revert to baseline)
    ///
    /// Examples:
    /// - "Playful Ball": +20 Playfulness, +10 Energy (babies love it!)
    /// - "Scholarly Glasses": +15 Curiosity, +10 Intelligence feel
    /// - "Warrior Armor": +25 Aggression, +15 Confidence
    /// - "Peaceful Robe": -20 Aggression, +20 Affection, +10 Serenity
    /// </summary>
    public struct PersonalityEquipmentEffect : IComponentData
    {
        // Equipped item reference
        public int equippedItemId;
        public EquipmentSlot equippedSlot;

        // Personality modifiers (temporary changes while equipped)
        public sbyte curiosityModifier;      // -100 to +100
        public sbyte playfulnessModifier;
        public sbyte aggressionModifier;
        public sbyte affectionModifier;
        public sbyte independenceModifier;
        public sbyte nervousnessModifier;
        public sbyte stubbornessModifier;
        public sbyte loyaltyModifier;

        // Cooperation/mood modifiers (affect gameplay directly)
        public float cooperationModifier;    // -0.5 to +0.5
        public float moodModifier;           // -0.5 to +0.5 (affects happiness)
        public float energyModifier;         // -0.3 to +0.3
        public float stressModifier;         // -0.3 to +0.3 (negative = reduces stress)

        // Equipment fit (how well it matches chimera's personality)
        public float personalityFit;         // 0.0 (poor fit) to 1.0 (perfect fit)
        public bool chimeraLikesEquipment;   // Based on personality preferences

        // Equip time tracking
        public float equipTime;
        public float totalWearTime;
    }

    /// <summary>
    /// EQUIPMENT PERSONALITY PROFILE - Defines what personality the equipment has
    /// Stored in equipment database/config
    /// </summary>
    public struct EquipmentPersonalityProfile : IComponentData
    {
        public int itemId;
        public FixedString64Bytes itemName;

        // Personality modifiers
        public sbyte curiosityModifier;
        public sbyte playfulnessModifier;
        public sbyte aggressionModifier;
        public sbyte affectionModifier;
        public sbyte independenceModifier;
        public sbyte nervousnessModifier;
        public sbyte stubbornnessModifier;
        public sbyte loyaltyModifier;

        // Cooperation/mood effects
        public float cooperationModifier;
        public float moodModifier;
        public float energyModifier;
        public float stressModifier;

        // Personality fit calculation
        public PersonalityArchetype targetArchetype; // What personality this is designed for
        public float minFitScore;                    // Minimum fit score to not upset chimera

        // Visual/cosmetic effects
        public FixedString64Bytes cosmeticDescription; // "Looks scholarly", "Feels aggressive"
        public VisualEquipmentStyle visualStyle;

        // Special properties
        public bool isAgeRestricted;                 // Some equipment only for certain ages
        public LifeStage minimumAge;
        public LifeStage maximumAge;
    }

    /// <summary>
    /// EQUIPMENT PREFERENCE COMPONENT - Tracks chimera's equipment preferences
    /// Based on personality
    /// </summary>
    public struct EquipmentPreferenceComponent : IComponentData
    {
        // Preferred equipment types
        public EquipmentPreferenceFlags preferredTypes;

        // Disliked equipment types
        public EquipmentPreferenceFlags dislikedTypes;

        // Favorite equipment (if any)
        public int favoriteItemId;
        public float favoriteItemBondBonus; // Extra bond strength when wearing favorite

        // Equipment mood tracking
        public float happinessWithCurrentEquipment; // 0.0-1.0
        public int consecutiveDaysWearingFavorite;
    }

    /// <summary>
    /// EQUIPMENT FIT CALCULATOR - Calculates how well equipment fits chimera's personality
    /// </summary>
    public struct EquipmentFitCalculation : IComponentData
    {
        public Entity chimeraEntity;
        public int itemId;
        public float calculatedFitScore;     // 0.0 (terrible) to 1.0 (perfect)
        public FixedString128Bytes fitReason; // "Aggressive chimera + Peaceful Robe = poor fit"
        public bool wouldUpsetChimera;       // true if fit < 0.3
        public float cooperationPenalty;     // -0.3 if very poor fit
    }

    /// <summary>
    /// EQUIPMENT PERSONALITY CHANGE EVENT - Triggered when equipping/unequipping affects personality
    /// </summary>
    public struct EquipmentPersonalityChangeEvent : IComponentData
    {
        public Entity chimeraEntity;
        public int itemId;
        public bool wasEquipped; // true = equipped, false = unequipped
        public float personalityFit;
        public float cooperationChange;
        public float moodChange;
        public float timestamp;
        public FixedString128Bytes description; // "Equipped Playful Ball - Loves it! +0.2 cooperation"
    }

    /// <summary>
    /// Personality archetypes for equipment matching
    /// </summary>
    public enum PersonalityArchetype : byte
    {
        Playful = 0,       // High playfulness, energy
        Curious = 1,       // High curiosity, intelligence feel
        Aggressive = 2,    // High aggression, confidence
        Gentle = 3,        // High affection, low aggression
        Independent = 4,   // High independence, stubbornness
        Loyal = 5,         // High loyalty, low independence
        Nervous = 6,       // High nervousness, low confidence
        Confident = 7,     // Low nervousness, high independence
        Social = 8,        // High affection, low independence
        Scholar = 9        // High curiosity, low playfulness
    }

    /// <summary>
    /// Equipment preference flags (bitflags for efficiency)
    /// </summary>
    [System.Flags]
    public enum EquipmentPreferenceFlags : uint
    {
        None = 0,
        Playful = 1 << 0,      // Toys, balls, fun items
        Combat = 1 << 1,       // Armor, weapons
        Scholarly = 1 << 2,    // Glasses, books
        Cute = 1 << 3,         // Bows, pretty items
        Practical = 1 << 4,    // Tools, utility items
        Fancy = 1 << 5,        // Decorative, expensive
        Simple = 1 << 6,       // Basic, utilitarian
        Natural = 1 << 7,      // Wood, leather
        Technological = 1 << 8, // Metal, electronic
        Comfortable = 1 << 9,  // Soft, cozy
        Protective = 1 << 10,  // Armor, shields
        Expressive = 1 << 11   // Bright colors, unique
    }

    /// <summary>
    /// Visual equipment styles
    /// </summary>
    public enum VisualEquipmentStyle : byte
    {
        Playful = 0,
        Combat = 1,
        Scholarly = 2,
        Elegant = 3,
        Rugged = 4,
        Mystical = 5,
        Technological = 6,
        Natural = 7,
        Cute = 8,
        Intimidating = 9
    }

    /// <summary>
    /// EQUIPMENT STAT BONUS OVERRIDE - Replaces old stat-based system
    /// Equipment now affects cooperation, not raw stats
    /// </summary>
    [System.Obsolete("Equipment no longer provides stat bonuses - affects personality and cooperation instead")]
    public struct LegacyEquipmentStatBonus : IComponentData
    {
        // Kept for backward compatibility during migration
        // All values should be 0.0 in new system
        public float strengthBonus;
        public float agilityBonus;
        public float intelligenceBonus;
        public float vitalityBonus;
        public float socialBonus;
        public float adaptabilityBonus;
    }

    /// <summary>
    /// Helper class for calculating equipment-personality fit
    /// </summary>
    public static class EquipmentFitCalculator
    {
        /// <summary>
        /// Calculates how well equipment fits a chimera's personality
        /// </summary>
        public static float CalculateFit(
            CreaturePersonality personality,
            EquipmentPersonalityProfile equipment)
        {
            float fitScore = 0.5f; // Start neutral

            // Calculate alignment with personality traits
            // Positive = equipment enhances existing traits (good fit)
            // Negative = equipment conflicts with personality (poor fit)

            if (equipment.playfulnessModifier > 0 && personality.Playfulness > 60)
                fitScore += 0.15f; // Playful chimera + playful equipment = good
            if (equipment.playfulnessModifier < 0 && personality.Playfulness > 70)
                fitScore -= 0.2f; // Playful chimera + serious equipment = bad

            if (equipment.aggressionModifier > 0 && personality.Aggression > 60)
                fitScore += 0.15f;
            if (equipment.aggressionModifier > 0 && personality.Aggression < 30)
                fitScore -= 0.25f; // Gentle chimera + aggressive equipment = very bad

            if (equipment.affectionModifier > 0 && personality.Affection > 60)
                fitScore += 0.15f;

            if (equipment.curiosityModifier > 0 && personality.Curiosity > 60)
                fitScore += 0.1f;

            if (equipment.independenceModifier > 0 && personality.Independence > 60)
                fitScore += 0.1f;
            if (equipment.independenceModifier < 0 && personality.Independence > 70)
                fitScore -= 0.15f; // Independent chimera dislikes clingy equipment

            // Nervous chimeras dislike intimidating equipment
            if (equipment.stressModifier > 0 && personality.Nervousness > 60)
                fitScore -= 0.2f;

            // Loyal chimeras like affectionate equipment
            if (equipment.loyaltyModifier > 0 && personality.Loyalty > 70)
                fitScore += 0.1f;

            return Unity.Mathematics.math.clamp(fitScore, 0f, 1f);
        }

        /// <summary>
        /// Gets cooperation modifier based on equipment fit
        /// </summary>
        public static float GetCooperationModifier(float fitScore)
        {
            if (fitScore > 0.8f) return 0.2f;  // Perfect fit: +20% cooperation
            if (fitScore > 0.6f) return 0.1f;  // Good fit: +10% cooperation
            if (fitScore > 0.4f) return 0.0f;  // Neutral fit: no change
            if (fitScore > 0.2f) return -0.1f; // Poor fit: -10% cooperation
            return -0.3f;                       // Terrible fit: -30% cooperation
        }

        /// <summary>
        /// Determines if equipment would upset chimera
        /// </summary>
        public static bool WouldUpsetChimera(float fitScore, byte nervousness)
        {
            // Nervous chimeras more easily upset
            float threshold = nervousness > 70 ? 0.4f : 0.3f;
            return fitScore < threshold;
        }
    }
}
