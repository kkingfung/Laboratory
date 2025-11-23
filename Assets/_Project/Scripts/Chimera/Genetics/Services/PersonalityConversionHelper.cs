using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// PERSONALITY CONVERSION HELPER
    ///
    /// Converts between PersonalityGeneticComponent and CreaturePersonality
    ///
    /// This helper exists in the Genetics assembly because:
    /// - Genetics assembly references Consciousness (can use CreaturePersonality)
    /// - Consciousness cannot reference Genetics (would create circular dependency)
    /// - Therefore, conversion methods must live in Genetics
    /// </summary>
    public static class PersonalityConversionHelper
    {
        /// <summary>
        /// Creates CreaturePersonality from inherited personality genetics
        ///
        /// This is the preferred method for bred chimeras - personality comes from parents,
        /// not randomly generated from stats.
        ///
        /// Integration:
        /// - PersonalityGeneticComponent provides the 8 inherited personality traits
        /// - PersonalityBreedingSystem handles breeding and inheritance
        /// - PersonalityStabilitySystem uses genetic baseline for elderly reversion
        /// </summary>
        public static CreaturePersonality CreatePersonalityFromGenetics(
            PersonalityGeneticComponent genetics,
            VisualGeneticData visualGenetics,
            uint personalitySeed)
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(personalitySeed);

            var personality = new CreaturePersonality
            {
                PersonalitySeed = personalitySeed,
                LearningRate = 0.1f + (visualGenetics.Intelligence / 100f) * 0.4f,
                MemoryStrength = (byte)(50 + visualGenetics.Intelligence / 2),

                // INHERITED personality traits from parents (Phase 8)
                Curiosity = genetics.geneticCuriosity,
                Playfulness = genetics.geneticPlayfulness,
                Aggression = genetics.geneticAggression,
                Affection = genetics.geneticAffection,
                Independence = genetics.geneticIndependence,
                Nervousness = genetics.geneticNervousness,
                Stubbornness = genetics.geneticStubbornness,
                Loyalty = genetics.geneticLoyalty,

                // Initial emotional state
                CurrentMood = EmotionalState.Neutral,
                StressLevel = 0.2f,
                HappinessLevel = 0.6f,
                EnergyLevel = 0.8f,

                // Generate preferences (still from visual genetics)
                FoodLikes = CreaturePersonality.GenerateFoodPreferences(visualGenetics, random),
                PreferredActivities = CreaturePersonality.GenerateActivityPreferences(visualGenetics, random),
                SocialBehavior = CreaturePersonality.GenerateSocialPreferences(visualGenetics, random),
                HabitatLikes = CreaturePersonality.GenerateEnvironmentPreferences(visualGenetics, random)
            };

            return personality;
        }

        /// <summary>
        /// Converts CreaturePersonality to PersonalityGeneticComponent (for generation 1 chimeras)
        /// </summary>
        public static PersonalityGeneticComponent CreateGeneticsFromPersonality(CreaturePersonality personality)
        {
            return new PersonalityGeneticComponent
            {
                geneticCuriosity = personality.Curiosity,
                geneticPlayfulness = personality.Playfulness,
                geneticAggression = personality.Aggression,
                geneticAffection = personality.Affection,
                geneticIndependence = personality.Independence,
                geneticNervousness = personality.Nervousness,
                geneticStubbornness = personality.Stubbornness,
                geneticLoyalty = personality.Loyalty,
                parent1Influence = 500, // 50/50 (no parents)
                parent2Influence = 500,
                mutationCount = 0,
                hasPersonalityMutation = false,
                personalityFitness = PersonalityGeneticsHelper.CalculatePersonalityFitness(new PersonalityGeneticComponent
                {
                    geneticCuriosity = personality.Curiosity,
                    geneticPlayfulness = personality.Playfulness,
                    geneticAggression = personality.Aggression,
                    geneticAffection = personality.Affection,
                    geneticIndependence = personality.Independence,
                    geneticNervousness = personality.Nervousness,
                    geneticStubbornness = personality.Stubbornness,
                    geneticLoyalty = personality.Loyalty
                }),
                temperamentStability = 0.8f
            };
        }
    }
}
