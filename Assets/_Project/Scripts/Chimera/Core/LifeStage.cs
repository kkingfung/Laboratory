namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// Defines the life stages of creatures in Project Chimera
    ///
    /// NEW VISION: Simplified to 4 stages matching emotional partnership growth
    /// - Baby (0-25%): Forgiving, learning, building trust
    /// - Child (25-50%): Developing personality, moderate sensitivity
    /// - Teen (50-75%): Personality solidifying, less forgiving
    /// - Adult (75-100%): Fully formed personality, deeply affected by treatment
    ///
    /// Visual Growth: "Watch your partner mature from baby → child → teen → adult"
    /// </summary>
    public enum LifeStage
    {
        /// <summary>
        /// Baby stage (0-25% of lifespan)
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
        [System.Obsolete("Use Baby instead - simplified to 4 stages")]
        Infant = 0,

        /// <summary>
        /// Child stage (25-50% of lifespan)
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
        [System.Obsolete("Use Child instead - simplified to 4 stages")]
        Juvenile = 1,

        /// <summary>
        /// Teen stage (50-75% of lifespan)
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
        [System.Obsolete("Use Teen instead - simplified to 4 stages")]
        Adolescent = 2,

        /// <summary>
        /// Adult stage (75-100% of lifespan)
        /// - Very low forgiveness (20%)
        /// - Permanent memory (95%)
        /// - Fully formed personality
        /// - Deep emotional bonds
        /// - Actions have lasting consequences
        /// </summary>
        Adult = 3,

        /// <summary>
        /// DEPRECATED: Removed in new vision
        /// Use Adult instead - no stat decline, relationships deepen instead
        /// </summary>
        [System.Obsolete("Use Adult - no Elder stage in new vision")]
        Elder = 3,

        /// <summary>
        /// DEPRECATED: Removed in new vision
        /// Use Adult instead - focus on emotional depth, not special powers
        /// </summary>
        [System.Obsolete("Use Adult - no Ancient stage in new vision")]
        Ancient = 3
    }
    
    /// <summary>
    /// Helper methods for life stage calculations
    /// </summary>
    public static class LifeStageExtensions
    {
        /// <summary>
        /// Calculates life stage based on age and species lifespan
        ///
        /// NEW 4-STAGE SYSTEM:
        /// - 0-25%: Baby (forgiving, learning, fast bonding)
        /// - 25-50%: Child (developing personality, moderate sensitivity)
        /// - 50-75%: Teen (personality solidifying, less forgiving)
        /// - 75-100%: Adult (fully formed, deeply affected by treatment)
        /// </summary>
        public static LifeStage CalculateLifeStage(int ageInDays, int maxLifespanDays)
        {
            float agePercentage = (float)ageInDays / maxLifespanDays;

            return agePercentage switch
            {
                < 0.25f => LifeStage.Baby,   // 0-25%: Baby stage
                < 0.50f => LifeStage.Child,  // 25-50%: Child stage
                < 0.75f => LifeStage.Teen,   // 50-75%: Teen stage
                _ => LifeStage.Adult          // 75-100%: Adult stage
            };
        }

        /// <summary>
        /// Calculates life stage from age percentage (0.0-1.0)
        /// </summary>
        public static LifeStage CalculateLifeStageFromPercentage(float agePercentage)
        {
            return agePercentage switch
            {
                < 0.25f => LifeStage.Baby,
                < 0.50f => LifeStage.Child,
                < 0.75f => LifeStage.Teen,
                _ => LifeStage.Adult
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
        /// NEW VISION: Only Adults can breed (75%+ of lifespan)
        /// Focus on quality partnerships and emotional maturity
        /// </summary>
        public static bool CanBreed(this LifeStage stage)
        {
            return stage == LifeStage.Adult;
        }

        /// <summary>
        /// Gets breeding efficiency for this life stage
        ///
        /// NEW VISION: Adults maintain full breeding efficiency throughout life
        /// No decline - relationships and bonds deepen with age instead
        /// </summary>
        public static float GetBreedingEfficiency(this LifeStage stage)
        {
            return stage == LifeStage.Adult ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Gets the age percentage range for this life stage
        /// </summary>
        public static (float min, float max) GetAgePercentageRange(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => (0.00f, 0.25f),
                LifeStage.Child => (0.25f, 0.50f),
                LifeStage.Teen => (0.50f, 0.75f),
                LifeStage.Adult => (0.75f, 1.00f),
                _ => (0.75f, 1.00f)
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
                _ => ""
            };
        }
    }
}

