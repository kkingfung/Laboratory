using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// AGE-BASED SENSITIVITY COMPONENT
    ///
    /// Core Concept: Baby chimeras are forgiving, adults are deeply affected by treatment
    ///
    /// Design:
    /// - Baby (0-25% lifespan): High forgiveness (0.8), low memory (0.2), fast recovery
    /// - Child (25-50%): Moderate forgiveness (0.6), moderate memory (0.4), normal recovery
    /// - Teen (50-75%): Low forgiveness (0.4), high memory (0.7), slow recovery
    /// - Adult (75-100%): Very low forgiveness (0.2), permanent memory (0.9), very slow recovery
    ///
    /// This creates meaningful progression where early mistakes are recoverable,
    /// but adult relationships require genuine care and respect.
    /// </summary>
    public struct AgeSensitivityComponent : IComponentData
    {
        // Current life stage affects all bonding
        public LifeStage currentLifeStage;
        public float agePercentage; // 0.0-1.0 of total lifespan

        // Age-based modifiers (calculated from life stage)
        public float forgivenessMultiplier;  // How easily they forgive negative interactions (1.0 = baby, 0.2 = adult)
        public float memoryStrength;         // How strongly they remember experiences (0.2 = baby, 0.9 = adult)
        public float bondDamageMultiplier;   // How much damage affects them (0.5 = baby, 2.0 = adult)
        public float recoverySpeed;          // How fast bonds naturally heal (2.0 = baby, 0.3 = adult)

        // Emotional resilience
        public float emotionalResilience;    // Resistance to stress/trauma (high in babies, low in adults)
        public float trustVulnerability;     // How easily trust can be broken (low in babies, high in adults)

        // Recovery tracking
        public float timeSinceLastNegativeInteraction;
        public bool isInRecoveryPeriod;
        public int consecutivePositiveInteractions; // Track streak for healing
    }

    /// <summary>
    /// BOND DAMAGE EVENT - Records when negative interactions occur
    /// Age determines severity and recovery difficulty
    /// </summary>
    public struct BondDamageEvent : IComponentData
    {
        public Entity targetCreature;
        public BondDamageType damageType;
        public float rawDamageAmount;      // Base damage before age modifiers
        public float actualDamageDealt;    // After age multipliers applied
        public LifeStage creatureAgeAtDamage;
        public float timestamp;
        public bool isRecoverable;         // Some adult damage may be permanent
    }

    /// <summary>
    /// BOND RECOVERY REQUEST - Attempts to heal damaged relationships
    /// Different recovery methods for different situations
    /// </summary>
    public struct BondRecoveryRequest : IComponentData
    {
        public Entity targetCreature;
        public RecoveryMethod method;
        public float recoveryPotential;    // How much this could heal (0.0-1.0)
        public float requestTime;

        // Context for recovery
        public bool isGenuineApology;      // Genuine care vs trying to "game" the system
        public int recentPositiveActions;  // Pattern of good behavior
    }

    /// <summary>
    /// Types of bond damage
    /// </summary>
    public enum BondDamageType
    {
        Neglect = 0,           // Not interacting, not feeding
        Mistreatment = 1,      // Forcing unwanted activities
        Abandonment = 2,       // Leaving for extended periods
        BrokenTrust = 3,       // Promises not kept
        EmotionalHarm = 4,     // Anger, frustration directed at them
        PhysicalStress = 5,    // Overworking, dangerous situations
        SocialIsolation = 6    // Keeping from companions
    }

    /// <summary>
    /// Recovery methods - multiple paths to heal relationships
    /// </summary>
    public enum RecoveryMethod
    {
        SpecialFood = 0,           // Their favorite treats - minor healing
        ThoughtfulGift = 1,        // Equipment they like - moderate healing
        QualityTime = 2,           // Focused positive activities - major healing
        SharedVictory = 3,         // Winning together - major healing
        GenuineApology = 4,        // Time + consistent positive behavior - slow but deep
        RescueFromDanger = 5,      // Protecting them - instant trust boost
        IntroducingFriends = 6     // Social bonding - moderate healing
    }

    /// <summary>
    /// EMOTIONAL SCAR - Permanent marker of severe past trauma
    /// Adults can develop these from serious mistreatment
    /// Babies/children are resilient and rarely develop scars
    /// </summary>
    public struct EmotionalScar : IBufferElementData
    {
        public BondDamageType sourceType;
        public float severityWhenReceived; // How bad it was (0.0-1.0)
        public LifeStage ageWhenReceived;  // Adults traumatized easier
        public float timestamp;
        public bool hasPartiallyHealed;    // Can improve but never fully disappear
        public float healingProgress;      // 0.0 = raw, 1.0 = mostly healed
        public FixedString64Bytes description; // "Abandoned at teen stage"
    }

    /// <summary>
    /// POSITIVE MEMORY - Counter to scars, records wonderful moments
    /// Help balance and heal emotional damage over time
    /// </summary>
    public struct PositiveMemory : IBufferElementData
    {
        public PositiveMemoryType type;
        public float intensityWhenCreated; // How special it was (0.0-1.0)
        public LifeStage ageWhenCreated;
        public float timestamp;
        public float currentStrength; // Fades slowly over time, but slower than negative
        public FixedString64Bytes description; // "First racing victory together"
    }

    /// <summary>
    /// Types of positive memories
    /// </summary>
    public enum PositiveMemoryType
    {
        FirstMeeting = 0,
        FirstVictory = 1,
        OvercomingFear = 2,
        PerfectCooperation = 3,
        ReceivedGift = 4,
        SharedDiscovery = 5,
        RescuedByPartner = 6,
        MilestoneReached = 7
    }

    /// <summary>
    /// AGE STAGE PROGRESSION EVENT - Triggered when chimera ages up
    /// Sensitivity changes, relationship becomes more fragile
    /// </summary>
    public struct AgeStageProgressionEvent : IComponentData
    {
        public Entity creature;
        public LifeStage previousStage;
        public LifeStage newStage;
        public float currentBondStrength;  // Bond at time of transition
        public float timestamp;

        // Warnings for player
        public bool becameLessForgiving;   // Alert: relationship now more fragile
        public bool memoriesNowPermanent;  // Alert: actions have lasting consequences
    }
}
