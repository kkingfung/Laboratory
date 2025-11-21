namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// Defines the life stages of creatures in Project Chimera
    ///
    /// 5-STAGE EMOTIONAL JOURNEY:
    /// - Baby (0-20%): Forgiving, learning, building trust
    /// - Child (20-40%): Developing personality, moderate sensitivity
    /// - Teen (40-60%): Personality solidifying, less forgiving
    /// - Adult (60-85%): Fully formed personality, deeply affected by treatment
    /// - Elderly (85-100%): Deepest bonds, ultimate partnership achievement
    ///
    /// Visual Growth: "Watch your partner mature from baby → child → teen → adult → elderly companion"
    /// </summary>
    public enum LifeStage
    {
        /// <summary>
        /// Baby stage (0-20% of lifespan)
        /// - Very forgiving (90%)
        /// - Short memory (20%)
        /// - Fast bonding and recovery
        /// - Building foundational trust
        /// </summary>
        Baby = 0,

        /// <summary>
        /// DEPRECATED: Use Baby instead
        /// Kept for backward compatibility
        /// </summary>
        [System.Obsolete("Use Baby instead")]
        Infant = 0,

        /// <summary>
        /// Child stage (20-40% of lifespan)
        /// - Moderately forgiving (70%)
        /// - Developing memory (40%)
        /// - Personality forming
        /// - Learning cooperation
        /// </summary>
        Child = 1,

        /// <summary>
        /// DEPRECATED: Use Child instead
        /// Kept for backward compatibility
        /// </summary>
        [System.Obsolete("Use Child instead")]
        Juvenile = 1,

        /// <summary>
        /// Teen stage (40-60% of lifespan)
        /// - Less forgiving (40%)
        /// - Strong memory (70%)
        /// - Personality solidifying
        /// - Testing boundaries
        /// </summary>
        Teen = 2,

        /// <summary>
        /// DEPRECATED: Use Teen instead
        /// Kept for backward compatibility
        /// </summary>
        [System.Obsolete("Use Teen instead")]
        Adolescent = 2,

        /// <summary>
        /// Adult stage (60-85% of lifespan)
        /// - Very low forgiveness (20%)
        /// - Permanent memory (95%)
        /// - Fully formed personality
        /// - Deep emotional bonds
        /// - Actions have lasting consequences
        /// </summary>
        Adult = 3,

        /// <summary>
        /// Elderly stage (85-100% of lifespan)
        /// THE ULTIMATE PARTNERSHIP - Chimeras who reach old age with you
        ///
        /// Emotional Characteristics:
        /// - Wise forgiveness (35% - more than adults, less than teens)
        /// - Perfect memory (99% - remembers everything)
        /// - PROFOUND emotional bonds
        /// - Deep cooperation with trusted partner
        /// - Devastating if neglected/abandoned
        /// - Represents lifetime of shared experiences
        ///
        /// Personality Mechanics:
        /// - Baseline personality LOCKED when reaching elderly stage
        /// - Extremely resistant to personality changes (5% learning rate)
        /// - Temporary changes auto-revert to baseline over time
        /// - Represents deeply ingrained lifetime habits and character
        ///
        /// ACHIEVEMENT: Reaching elderly stage proves exceptional partnership
        /// </summary>
        Elderly = 4,

        /// <summary>
        /// DEPRECATED: Was used for old 6-stage system
        /// Now properly implemented as Elderly (stage 4)
        /// </summary>
        [System.Obsolete("Use Elderly (new stage 4)")]
        Elder = 4,

        /// <summary>
        /// DEPRECATED: Removed - focus on emotional depth, not special powers
        /// Elderly is the final stage (no Ancient)
        /// </summary>
        [System.Obsolete("Use Elderly - no Ancient stage")]
        Ancient = 4
    }
    
    /// <summary>
    /// Helper methods for life stage calculations
    /// </summary>
    public static class LifeStageExtensions
    {
        /// <summary>
        /// Calculates life stage based on age and species lifespan
        ///
        /// 5-STAGE EMOTIONAL JOURNEY:
        /// - 0-20%: Baby (forgiving, learning, fast bonding)
        /// - 20-40%: Child (developing personality, moderate sensitivity)
        /// - 40-60%: Teen (personality solidifying, less forgiving)
        /// - 60-85%: Adult (fully formed, deeply affected by treatment)
        /// - 85-100%: Elderly (ultimate partnership, profound bonds)
        /// </summary>
        public static LifeStage CalculateLifeStage(int ageInDays, int maxLifespanDays)
        {
            float agePercentage = (float)ageInDays / maxLifespanDays;

            return agePercentage switch
            {
                < 0.20f => LifeStage.Baby,    // 0-20%: Baby stage
                < 0.40f => LifeStage.Child,   // 20-40%: Child stage
                < 0.60f => LifeStage.Teen,    // 40-60%: Teen stage
                < 0.85f => LifeStage.Adult,   // 60-85%: Adult stage
                _ => LifeStage.Elderly         // 85-100%: Elderly stage (ACHIEVEMENT!)
            };
        }

        /// <summary>
        /// Calculates life stage from age percentage (0.0-1.0)
        /// </summary>
        public static LifeStage CalculateLifeStageFromPercentage(float agePercentage)
        {
            return agePercentage switch
            {
                < 0.20f => LifeStage.Baby,
                < 0.40f => LifeStage.Child,
                < 0.60f => LifeStage.Teen,
                < 0.85f => LifeStage.Adult,
                _ => LifeStage.Elderly
            };
        }
        
        /// <summary>
        /// Gets emotional/cooperation modifiers for this life stage
        ///
        /// NEW VISION: Stats matter less, cooperation and personality matter more!
        /// - Equipment affects personality > stats
        /// - Victory comes from player skill + chimera cooperation
        /// - No stat decline with age, relationships deepen instead
        /// </summary>
        public static (float physicalCapacity, float cooperation, float emotionalDepth, float energyLevel, float personalityStrength) GetLifeStageModifiers(this LifeStage stage)
        {
            return stage switch
            {
                // Baby: High energy, low cooperation (learning), shallow emotions, weak personality
                LifeStage.Baby => (0.4f, 0.3f, 0.2f, 1.0f, 0.1f),

                // Child: Growing capacity, moderate cooperation, developing emotions, forming personality
                LifeStage.Child => (0.7f, 0.6f, 0.5f, 0.9f, 0.4f),

                // Teen: Near-full capacity, inconsistent cooperation, strong emotions, personality solidifying
                LifeStage.Teen => (0.9f, 0.7f, 0.8f, 0.8f, 0.7f),

                // Adult: Full capacity, deep cooperation, profound emotions, fully formed personality
                LifeStage.Adult => (1.0f, 1.0f, 1.0f, 0.8f, 1.0f),

                // Elderly: ULTIMATE PARTNERSHIP - Maintained capacity, perfect cooperation, transcendent emotional depth
                // Represents lifetime of shared experiences and deepest possible bond
                LifeStage.Elderly => (1.0f, 1.2f, 1.2f, 0.7f, 1.1f),

                _ => (1.0f, 1.0f, 1.0f, 0.8f, 1.0f)
            };
        }

        /// <summary>
        /// DEPRECATED: Use GetLifeStageModifiers instead
        /// Old stat-based system removed in favor of cooperation-based gameplay
        /// </summary>
        [System.Obsolete("Use GetLifeStageModifiers - stats matter less, cooperation matters more")]
        public static (float health, float attack, float defense, float speed, float intelligence) GetStatModifiers(this LifeStage stage)
        {
            // Legacy compatibility - map to new system
            var (physical, coop, _, energy, _) = GetLifeStageModifiers(stage);
            return (physical, physical, physical, energy, coop);
        }
        
        /// <summary>
        /// Checks if this life stage can breed
        ///
        /// NEW VISION: Adults and Elderly can breed
        /// Focus on quality partnerships and emotional maturity
        /// </summary>
        public static bool CanBreed(this LifeStage stage)
        {
            return stage == LifeStage.Adult || stage == LifeStage.Elderly;
        }

        /// <summary>
        /// Gets breeding efficiency for this life stage
        ///
        /// NEW VISION: No decline with age - relationships and bonds deepen instead
        /// Elderly chimeras can still breed (if bond is strong)
        /// </summary>
        public static float GetBreedingEfficiency(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Adult => 1.0f,
                LifeStage.Elderly => 1.0f, // No decline - maintained with good care
                _ => 0.0f
            };
        }

        /// <summary>
        /// Gets the age percentage range for this life stage
        /// </summary>
        public static (float min, float max) GetAgePercentageRange(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => (0.00f, 0.20f),
                LifeStage.Child => (0.20f, 0.40f),
                LifeStage.Teen => (0.40f, 0.60f),
                LifeStage.Adult => (0.60f, 0.85f),
                LifeStage.Elderly => (0.85f, 1.00f),
                _ => (0.85f, 1.00f)
            };
        }

        /// <summary>
        /// Gets a display name for this life stage
        /// </summary>
        public static string GetDisplayName(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => "Baby",
                LifeStage.Child => "Child",
                LifeStage.Teen => "Teen",
                LifeStage.Adult => "Adult",
                LifeStage.Elderly => "Elderly",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets a description of the emotional characteristics at this stage
        /// </summary>
        public static string GetEmotionalDescription(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => "Forgiving and trusting, learning about the world. Easy to bond with.",
                LifeStage.Child => "Personality developing. Moderately sensitive to treatment.",
                LifeStage.Teen => "Testing boundaries. Less forgiving, memories starting to stick.",
                LifeStage.Adult => "Fully formed personality. Deeply affected by treatment. Actions have lasting consequences.",
                LifeStage.Elderly => "ULTIMATE PARTNERSHIP. Wise, deeply bonded companion. Remembers everything. Devastating if neglected.",
                _ => ""
            };
        }
    }
}

