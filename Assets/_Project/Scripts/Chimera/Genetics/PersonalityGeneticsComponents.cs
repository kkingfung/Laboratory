using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Consciousness.Core;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// PERSONALITY GENETICS COMPONENT
    ///
    /// NEW VISION: Personality traits are genetically inheritable
    ///
    /// Design Philosophy:
    /// - Each of the 8 personality traits has genetic representation
    /// - Offspring inherit personality from parents (not randomly generated)
    /// - Personality can mutate during breeding (rare events)
    /// - Genetic personality becomes the baseline for elderly chimeras
    /// - Experiences can modify personality, but genetics set the foundation
    ///
    /// Inheritance Model:
    /// - Blended inheritance (average parents with variation)
    /// - Slight random variation (±15%) for uniqueness
    /// - Personality mutations can shift traits significantly
    /// - Compatible personalities improve breeding success
    ///
    /// Integration Points:
    /// - CreaturePersonality (Phase 1): Defines the 8 personality traits
    /// - PersonalityStabilitySystem (Phase 3.5): Elderly baseline from genetics
    /// - BreedingEngine: Calculates personality inheritance
    /// - Population management: Personality affects breeding compatibility
    /// </summary>
    public struct PersonalityGeneticComponent : IComponentData
    {
        // Genetic baseline for 8 personality traits (0-100)
        // These values are inherited from parents
        public byte geneticCuriosity;
        public byte geneticPlayfulness;
        public byte geneticAggression;
        public byte geneticAffection;
        public byte geneticIndependence;
        public byte geneticNervousness;
        public byte geneticStubbornness;
        public byte geneticLoyalty;

        // Inheritance tracking
        public ushort parent1Influence;      // 0-1000 (percentage × 10 for precision)
        public ushort parent2Influence;      // 0-1000 (should sum to ~1000)
        public byte mutationCount;           // Number of personality mutations
        public bool hasPersonalityMutation;

        // Breeding quality
        public float personalityFitness;     // 0.0-1.0 (how well-balanced personality is)
        public float temperamentStability;   // 0.0-1.0 (less variation = more stable)
    }

    /// <summary>
    /// PERSONALITY INHERITANCE RECORD - Tracks which traits came from which parent
    /// </summary>
    public struct PersonalityInheritanceRecord : IBufferElementData
    {
        public PersonalityTrait trait;
        public byte parentValue;             // Value from the parent
        public byte offspringValue;          // Final value in offspring
        public bool wasInherited;            // True if inherited, false if mutated
        public ParentSource source;          // Which parent contributed
        public sbyte mutationDelta;          // Change due to mutation (0 if none)
    }

    /// <summary>
    /// PERSONALITY MUTATION EVENT - Triggered when personality mutates during breeding
    /// </summary>
    public struct PersonalityMutationEvent : IComponentData
    {
        public Entity offspringEntity;
        public PersonalityTrait affectedTrait;
        public byte originalValue;           // Pre-mutation value
        public byte mutatedValue;            // Post-mutation value
        public sbyte mutationDelta;          // Change amount
        public MutationDirection direction;  // Increase or decrease
        public float mutationSeverity;       // 0.0-1.0 (how significant)
        public float timestamp;
        public FixedString128Bytes description; // "Curiosity greatly increased"
    }

    /// <summary>
    /// PERSONALITY COMPATIBILITY CALCULATION - For breeding compatibility
    /// </summary>
    public struct PersonalityCompatibilityData : IComponentData
    {
        public Entity parent1Entity;
        public Entity parent2Entity;
        public float overallCompatibility;   // 0.0-1.0

        // Trait compatibility breakdown
        public float curiosityCompatibility;
        public float playfulnessCompatibility;
        public float aggressionCompatibility;
        public float affectionCompatibility;
        public float independenceCompatibility;
        public float nervousnessCompatibility;
        public float stubbornnessCompatibility;
        public float loyaltyCompatibility;

        // Compatibility factors
        public float diversityBonus;         // Bonus for diverse personalities
        public float harmonyBonus;           // Bonus for complementary traits
        public float extremesPenalty;        // Penalty for extreme trait combinations

        public float calculationTime;
        public bool isViableMatch;           // True if compatible enough to breed
    }

    /// <summary>
    /// PERSONALITY BREEDING REQUEST - Request to breed with personality consideration
    /// </summary>
    public struct PersonalityBreedingRequest : IComponentData
    {
        public Entity parent1Entity;
        public Entity parent2Entity;
        public float requestTime;
        public bool prioritizeBalance;       // Prefer balanced offspring personality
        public bool allowPersonalityMutations; // Allow personality mutations
        public float mutationRate;           // Override default mutation rate
    }

    /// <summary>
    /// PERSONALITY BREEDING RESULT - Results emphasizing personality inheritance
    /// </summary>
    public struct PersonalityBreedingResult : IComponentData
    {
        public Entity offspringEntity;
        public Entity parent1Entity;
        public Entity parent2Entity;

        // Inheritance breakdown
        public ushort parent1Contribution;   // 0-1000 (percentage × 10)
        public ushort parent2Contribution;   // 0-1000

        // Offspring personality summary
        public byte offspringCuriosity;
        public byte offspringPlayfulness;
        public byte offspringAggression;
        public byte offspringAffection;
        public byte offspringIndependence;
        public byte offspringNervousness;
        public byte offspringStubbornness;
        public byte offspringLoyalty;

        // Mutation tracking
        public byte mutationCount;
        public bool hadSignificantMutation;

        // Quality metrics
        public float personalityBalance;     // 0.0-1.0 (how balanced the personality is)
        public float compatibility;          // Parent compatibility score
        public float offspringFitness;       // Overall personality fitness

        public float timestamp;
        public FixedString128Bytes summary;  // "Playful & curious offspring"
    }

    /// <summary>
    /// Personality trait enum for inheritance tracking
    /// </summary>
    public enum PersonalityTrait : byte
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
    /// Parent source for inheritance tracking
    /// </summary>
    public enum ParentSource : byte
    {
        Blend = 0,      // Average of both parents
        Parent1 = 1,    // Dominant from parent 1
        Parent2 = 2,    // Dominant from parent 2
        Mutation = 3    // New mutation
    }

    /// <summary>
    /// Mutation direction
    /// </summary>
    public enum MutationDirection : byte
    {
        Decrease = 0,
        Increase = 1
    }

    /// <summary>
    /// Helper class for personality genetics calculations
    /// </summary>
    public static class PersonalityGeneticsHelper
    {
        // Inheritance constants
        public const float DEFAULT_MUTATION_RATE = 0.05f;        // 5% chance per trait
        public const int MAX_MUTATION_DELTA = 30;                // Max ±30 change
        public const int BLEND_VARIATION = 15;                   // ±15 variation from average

        // Compatibility thresholds
        public const float MIN_BREEDING_COMPATIBILITY = 0.3f;    // 30% minimum
        public const float OPTIMAL_COMPATIBILITY = 0.7f;         // 70% optimal
        public const float EXTREME_DIFFERENCE_THRESHOLD = 60;    // Traits differ by >60

        /// <summary>
        /// Calculate blended trait value from two parents
        /// </summary>
        public static byte BlendTrait(byte parent1Value, byte parent2Value, Unity.Mathematics.Random random)
        {
            // Average parents
            float average = (parent1Value + parent2Value) / 2f;

            // Add variation for uniqueness (±BLEND_VARIATION)
            int variation = random.NextInt(-BLEND_VARIATION, BLEND_VARIATION + 1);

            int finalValue = (int)average + variation;
            return (byte)Unity.Mathematics.math.clamp(finalValue, 0, 100);
        }

        /// <summary>
        /// Calculate trait compatibility (0.0-1.0)
        /// </summary>
        public static float CalculateTraitCompatibility(byte parent1Value, byte parent2Value)
        {
            float difference = Unity.Mathematics.math.abs(parent1Value - parent2Value);

            // Extreme differences reduce compatibility
            if (difference > EXTREME_DIFFERENCE_THRESHOLD)
            {
                return 0.3f; // Poor compatibility
            }

            // Moderate differences are good (diversity)
            if (difference > 20 && difference < 50)
            {
                return 1.0f; // Perfect diversity
            }

            // Very similar or very different
            return Unity.Mathematics.math.lerp(0.6f, 0.9f, 1f - (difference / 100f));
        }

        /// <summary>
        /// Apply mutation to a trait
        /// </summary>
        public static byte ApplyMutation(byte originalValue, Unity.Mathematics.Random random, out sbyte delta)
        {
            // Random mutation delta (-MAX_MUTATION_DELTA to +MAX_MUTATION_DELTA)
            delta = (sbyte)random.NextInt(-MAX_MUTATION_DELTA, MAX_MUTATION_DELTA + 1);

            int mutatedValue = originalValue + delta;
            return (byte)Unity.Mathematics.math.clamp(mutatedValue, 0, 100);
        }

        /// <summary>
        /// Calculate overall personality fitness
        /// </summary>
        public static float CalculatePersonalityFitness(PersonalityGeneticComponent personality)
        {
            // Balanced personalities are more fit
            float average = (personality.geneticCuriosity + personality.geneticPlayfulness +
                           personality.geneticAggression + personality.geneticAffection +
                           personality.geneticIndependence + personality.geneticNervousness +
                           personality.geneticStubbornness + personality.geneticLoyalty) / 8f;

            // Calculate variance from average
            float variance = 0f;
            variance += Unity.Mathematics.math.abs(personality.geneticCuriosity - average);
            variance += Unity.Mathematics.math.abs(personality.geneticPlayfulness - average);
            variance += Unity.Mathematics.math.abs(personality.geneticAggression - average);
            variance += Unity.Mathematics.math.abs(personality.geneticAffection - average);
            variance += Unity.Mathematics.math.abs(personality.geneticIndependence - average);
            variance += Unity.Mathematics.math.abs(personality.geneticNervousness - average);
            variance += Unity.Mathematics.math.abs(personality.geneticStubbornness - average);
            variance += Unity.Mathematics.math.abs(personality.geneticLoyalty - average);

            variance /= 8f;

            // Fitness is inverse of variance (more balanced = higher fitness)
            return Unity.Mathematics.math.clamp(1f - (variance / 50f), 0.3f, 1f);
        }

        /// <summary>
        /// Get personality description for UI
        /// </summary>
        public static FixedString128Bytes GetPersonalityDescription(PersonalityGeneticComponent personality)
        {
            var traits = new System.Collections.Generic.List<string>();

            if (personality.geneticCuriosity > 70) traits.Add("curious");
            if (personality.geneticPlayfulness > 70) traits.Add("playful");
            if (personality.geneticAggression > 70) traits.Add("fierce");
            else if (personality.geneticAggression < 30) traits.Add("gentle");
            if (personality.geneticAffection > 70) traits.Add("loving");
            if (personality.geneticLoyalty > 70) traits.Add("loyal");
            if (personality.geneticNervousness > 70) traits.Add("anxious");
            if (personality.geneticIndependence > 70) traits.Add("independent");

            if (traits.Count == 0)
                return "balanced";

            string description = string.Join(" & ", traits);
            return new FixedString128Bytes(description);
        }
    }
}
