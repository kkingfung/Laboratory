using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Consciousness.Core
{
    /// <summary>
    /// PERSONALITY STABILITY COMPONENT
    ///
    /// Core Concept: Elderly chimeras have deeply ingrained personalities that resist change
    ///
    /// Design:
    /// - Baby (0-20%): Personality highly malleable, changes easily (100% learning rate)
    /// - Child (20-40%): Personality forming, moderately malleable (70% learning rate)
    /// - Teen (40-60%): Personality solidifying, less malleable (40% learning rate)
    /// - Adult (60-85%): Personality mostly fixed, resistant to change (20% learning rate)
    /// - Elderly (85-100%): Personality LOCKED, extreme resistance (5% learning rate)
    ///
    /// Elderly Mechanic:
    /// - Baseline personality snapshot taken when reaching elderly stage
    /// - Temporary changes auto-revert to baseline over time
    /// - Represents lifetime of ingrained habits and stable character
    /// </summary>
    public struct PersonalityStabilityComponent : IComponentData
    {
        // Current life stage affecting stability
        public LifeStage currentLifeStage;

        // Age-based modifiers
        public float personalityMalleability;   // How easily personality changes (1.0 = baby, 0.05 = elderly)
        public float reversionSpeed;            // How fast temporary changes revert (0.0 = baby, 1.0 = elderly)

        // Baseline tracking for elderly
        public bool hasLockedBaseline;          // True when elderly stage reached
        public float timeSinceBaselineLock;     // Track time in elderly stage

        // Reversion tracking
        public float timeSinceLastPersonalityChange;
        public bool hasTemporaryDeviations;     // Track if current personality differs from baseline
        public float deviationMagnitude;        // How far from baseline (0.0-1.0)
    }

    /// <summary>
    /// BASELINE PERSONALITY SNAPSHOT - Locked when reaching elderly stage
    /// Stores the "true self" that elderly chimeras revert to
    /// </summary>
    public struct BaselinePersonalityComponent : IComponentData
    {
        // Core personality traits (baseline values locked at elderly transition)
        public byte baselineCuriosity;
        public byte baselinePlayfulness;
        public byte baselineAggression;
        public byte baselineAffection;
        public byte baselineIndependence;
        public byte baselineNervousness;
        public byte baselineStubbornness;
        public byte baselineLoyalty;

        // Timestamp when baseline was locked
        public float lockTimestamp;

        // Flags for tracking
        public bool wasLockedAtElderly;  // Ensures baseline represents mature personality
    }

    /// <summary>
    /// PERSONALITY CHANGE EVENT - Tracks when personality traits are modified
    /// Used to detect deviations and trigger reversion
    /// </summary>
    public struct PersonalityChangeEvent : IComponentData
    {
        public Entity targetCreature;
        public PersonalityTraitType traitChanged;
        public byte previousValue;
        public byte newValue;
        public float changeIntensity;  // How much it changed (0.0-1.0)
        public float timestamp;
        public FixedString64Bytes changeReason; // "Equipment equipped", "Traumatic event", etc.
    }

    /// <summary>
    /// PERSONALITY REVERSION REQUEST - Forces personality to drift back to baseline
    /// Auto-generated for elderly chimeras with deviations
    /// </summary>
    public struct PersonalityReversionRequest : IComponentData
    {
        public Entity targetCreature;
        public float reversionStrength;    // How aggressively to revert (0.0-1.0)
        public bool isGradual;             // True = slow drift, False = immediate snap
        public float requestTime;
    }

    /// <summary>
    /// Personality trait types for tracking changes
    /// </summary>
    public enum PersonalityTraitType : byte
    {
        Curiosity = 0,
        Playfulness = 1,
        Aggression = 2,
        Affection = 3,
        Independence = 4,
        Nervousness = 5,
        Stubbornness = 6,
        Loyalty = 7
    }

    /// <summary>
    /// AGE PERSONALITY PROGRESSION EVENT - Triggered when personality stability changes with age
    /// Alerts player when personality becomes more/less flexible
    /// </summary>
    public struct AgePersonalityProgressionEvent : IComponentData
    {
        public Entity creature;
        public LifeStage previousStage;
        public LifeStage newStage;
        public float previousMalleability;
        public float newMalleability;
        public float timestamp;

        // Warnings for player
        public bool personalityNowLocked;      // Alert: elderly baseline locked
        public bool personalityNowResistant;   // Alert: adult/teen harder to change
        public bool personalityNowFlexible;    // Info: baby/child still forming
    }
}
