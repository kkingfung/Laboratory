using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// CHIMERA POPULATION CAPACITY COMPONENT
    ///
    /// Core Philosophy: "Quality over Quantity"
    /// Players start with capacity for 1 chimera, can grow to max 5 based on bond strength
    ///
    /// Unlock Requirements:
    /// - 2nd chimera: 1 chimera with 0.6+ bond (Establishing first partnership)
    /// - 3rd chimera: 2 chimeras with 0.7+ bond (Proven caretaker)
    /// - 4th chimera: 3 chimeras with 0.8+ bond (Exceptional bonds)
    /// - 5th chimera: 4 chimeras with 0.9+ bond (ULTIMATE ACHIEVEMENT - Master partner)
    ///
    /// Permanent Consequences:
    /// - Sending chimeras away PERMANENTLY reduces max capacity
    /// - Natural death does NOT reduce capacity
    /// - Abandonment/neglect leading to death = capacity reduction
    /// - No way to recover lost capacity (teaches responsibility)
    ///
    /// Design Vision:
    /// "Every chimera matters. Build deep bonds before expanding your family."
    /// </summary>
    public struct ChimeraPopulationCapacity : IComponentData
    {
        // Current capacity limits
        public int currentCapacity;          // Current number of chimeras player has
        public int maxCapacity;              // Maximum allowed (starts at 1, max 5)
        public int baseMaxCapacity;          // Original max before any reductions (starts at 5)

        // Capacity unlock tracking
        public int capacityUnlocked;         // How many slots unlocked through bonds (1-5)
        public int capacityLostPermanently;  // How many slots lost by sending away chimeras

        // Requirements for next unlock
        public int strongBondsRequired;      // Number of strong bonds needed for next unlock
        public float bondStrengthRequired;   // Minimum bond strength required
        public bool canUnlockNext;           // True if player meets requirements

        // Statistics
        public int totalChimerasEverOwned;   // Lifetime count
        public int totalChimerasSentAway;    // Released/abandoned count
        public int totalChimerasNaturalDeath; // Natural death count
        public int currentAliveChimeras;     // Currently alive chimeras

        // Warnings
        public bool atCapacity;              // Cannot acquire more chimeras
        public bool hasLostCapacity;         // Has permanently reduced capacity
    }

    /// <summary>
    /// CAPACITY UNLOCK EVENT - Triggered when player unlocks a new chimera slot
    /// Celebrates achievement of strong bonds
    /// </summary>
    public struct CapacityUnlockEvent : IComponentData
    {
        public Entity playerEntity;
        public int previousMaxCapacity;
        public int newMaxCapacity;
        public int strongBondsCount;         // How many strong bonds player has
        public float averageBondStrength;    // Quality of partnerships
        public float timestamp;
        public FixedString64Bytes achievementText; // "Unlocked 3rd chimera slot!"
    }

    /// <summary>
    /// CAPACITY REDUCTION EVENT - Triggered when player loses capacity permanently
    /// Serious warning about consequences
    /// </summary>
    public struct CapacityReductionEvent : IComponentData
    {
        public Entity playerEntity;
        public Entity chimeraEntity;         // Which chimera was sent away
        public int previousMaxCapacity;
        public int newMaxCapacity;
        public CapacityReductionReason reason;
        public float timestamp;
        public FixedString128Bytes warningText; // "Capacity permanently reduced. You can never get this slot back."
    }

    /// <summary>
    /// CHIMERA ACQUISITION REQUEST - Player wants to acquire a new chimera
    /// System validates against capacity limits
    /// </summary>
    public struct ChimeraAcquisitionRequest : IComponentData
    {
        public Entity playerEntity;
        public Entity chimeraEntity;         // Which chimera to acquire (egg, wild, etc.)
        public AcquisitionMethod method;
        public float requestTime;
    }

    /// <summary>
    /// CHIMERA RELEASE REQUEST - Player wants to send chimera away
    /// Results in permanent capacity reduction (unless temporary rehoming)
    /// </summary>
    public struct ChimeraReleaseRequest : IComponentData
    {
        public Entity playerEntity;
        public Entity chimeraEntity;
        public ReleaseReason reason;
        public bool isTemporary;             // Temporary rehoming vs permanent release
        public float requestTime;
    }

    /// <summary>
    /// BOND STRENGTH TRACKER - Tracks individual chimera bonds for capacity calculation
    /// </summary>
    public struct ChimeraBondTracker : IBufferElementData
    {
        public Entity chimeraEntity;
        public float bondStrength;           // Current bond (0.0-1.0)
        public float peakBondStrength;       // Highest ever achieved
        public float bondTrend;              // Improving or declining?
        public bool countsForCapacity;       // Is this bond strong enough?
        public FixedString32Bytes chimeraName;
    }

    /// <summary>
    /// POPULATION WARNING - Alerts player about population-related issues
    /// </summary>
    public struct PopulationWarning : IComponentData
    {
        public Entity playerEntity;
        public PopulationWarningType warningType;
        public float severity;               // 0.0-1.0 (how urgent)
        public float timestamp;
        public FixedString128Bytes message;
    }

    // Enums

    /// <summary>
    /// Reasons for capacity reduction
    /// </summary>
    public enum CapacityReductionReason : byte
    {
        SentAway = 0,              // Voluntarily released
        Abandoned = 1,             // Neglected until they left
        NeglectDeath = 2,          // Died from poor care
        TraumatizedRelease = 3     // Sent away while traumatized
    }

    /// <summary>
    /// Methods of acquiring chimeras
    /// </summary>
    public enum AcquisitionMethod : byte
    {
        Breeding = 0,              // Bred from existing chimeras
        Hatched = 1,               // Hatched from egg
        Rescued = 2,               // Rescued from wild
        Gifted = 3,                // Received as gift
        Traded = 4                 // Traded with another player
    }

    /// <summary>
    /// Reasons for releasing chimeras
    /// </summary>
    public enum ReleaseReason : byte
    {
        PlayerChoice = 0,          // Player decided to release
        NoCapacity = 1,            // No room for them
        PoorBond = 2,              // Relationship failed
        Rehoming = 3,              // Finding better home (temporary)
        ChimeraChoice = 4          // Chimera chose to leave
    }

    /// <summary>
    /// Types of population warnings
    /// </summary>
    public enum PopulationWarningType : byte
    {
        AtCapacity = 0,                    // Cannot acquire more chimeras
        NearingCapacity = 1,               // 1 slot remaining
        WeakBonds = 2,                     // Bonds declining, risk losing capacity
        CanUnlockMore = 3,                 // Has strong bonds, can unlock next slot
        CapacityLost = 4,                  // Permanent capacity reduction occurred
        AllChimerasAtRisk = 5,             // All bonds below critical threshold
        ReadyForExpansion = 6              // Player ready for more chimeras
    }

    /// <summary>
    /// CAPACITY UNLOCK THRESHOLDS - Defines requirements for each capacity tier
    /// </summary>
    public static class CapacityUnlockThresholds
    {
        // Number of strong bonds required
        public const int UNLOCK_SLOT_2_BONDS_REQUIRED = 1;  // 1 chimera with strong bond
        public const int UNLOCK_SLOT_3_BONDS_REQUIRED = 2;  // 2 chimeras with strong bonds
        public const int UNLOCK_SLOT_4_BONDS_REQUIRED = 3;  // 3 chimeras with strong bonds
        public const int UNLOCK_SLOT_5_BONDS_REQUIRED = 4;  // 4 chimeras with strong bonds

        // Minimum bond strength for each tier
        public const float UNLOCK_SLOT_2_BOND_STRENGTH = 0.6f;  // Establishing partnership
        public const float UNLOCK_SLOT_3_BOND_STRENGTH = 0.7f;  // Proven caretaker
        public const float UNLOCK_SLOT_4_BOND_STRENGTH = 0.8f;  // Exceptional bonds
        public const float UNLOCK_SLOT_5_BOND_STRENGTH = 0.9f;  // ULTIMATE ACHIEVEMENT

        /// <summary>
        /// Gets the requirements for unlocking a specific capacity tier
        /// </summary>
        public static (int bondsRequired, float bondStrength) GetUnlockRequirements(int targetCapacity)
        {
            return targetCapacity switch
            {
                2 => (UNLOCK_SLOT_2_BONDS_REQUIRED, UNLOCK_SLOT_2_BOND_STRENGTH),
                3 => (UNLOCK_SLOT_3_BONDS_REQUIRED, UNLOCK_SLOT_3_BOND_STRENGTH),
                4 => (UNLOCK_SLOT_4_BONDS_REQUIRED, UNLOCK_SLOT_4_BOND_STRENGTH),
                5 => (UNLOCK_SLOT_5_BONDS_REQUIRED, UNLOCK_SLOT_5_BOND_STRENGTH),
                _ => (0, 0f)
            };
        }

        /// <summary>
        /// Gets achievement text for unlocking a capacity tier
        /// </summary>
        public static string GetUnlockAchievementText(int newCapacity)
        {
            return newCapacity switch
            {
                2 => "Second Chimera Unlocked! Your first bond proved your worth.",
                3 => "Third Chimera Unlocked! You're a proven caretaker.",
                4 => "Fourth Chimera Unlocked! Exceptional partnerships achieved!",
                5 => "ULTIMATE ACHIEVEMENT! Fifth Chimera Unlocked! Master Partner!",
                _ => $"Chimera slot {newCapacity} unlocked!"
            };
        }
    }
}
