using Unity.Entities;

namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// PERSONALITY GENETIC COMPONENT
    ///
    /// Core genetic personality data - moved to Core to break circular dependencies
    ///
    /// Contains the 8 genetic personality traits inherited from parents:
    /// - Curiosity, Playfulness, Aggression, Affection
    /// - Independence, Nervousness, Stubbornness, Loyalty
    ///
    /// This is a fundamental component needed by:
    /// - PersonalityStabilitySystem (Consciousness) - for elderly baseline
    /// - PersonalityBreedingSystem (Genetics) - for inheritance
    /// - CreaturePersonality (Consciousness) - for personality generation
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
}
