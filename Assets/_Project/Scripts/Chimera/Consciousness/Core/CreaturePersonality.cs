using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Consciousness.Core
{
    /// <summary>
    /// Core personality system that defines each creature's unique behavioral patterns
    /// Generated from genetics but evolves through experiences and interactions
    /// </summary>
    [Serializable]
    public struct CreaturePersonality : IComponentData
    {
        // Core personality traits (0-100 scale)
        public byte Curiosity;        // How much they explore and investigate
        public byte Playfulness;      // Tendency to engage in play behaviors
        public byte Aggression;       // Likelihood to fight or be territorial
        public byte Affection;        // Bonding strength with players and other creatures
        public byte Independence;     // Preference for solitude vs social interaction
        public byte Nervousness;      // Stress response and anxiety levels
        public byte Stubbornness;     // Resistance to training and commands
        public byte Loyalty;          // Faithfulness to bonded individuals

        // Behavioral preferences (genetic + learned)
        public FoodPreferences FoodLikes;
        public ActivityPreferences PreferredActivities;
        public SocialPreferences SocialBehavior;
        public EnvironmentPreferences HabitatLikes;

        // Memory and learning
        public float LearningRate;         // How quickly they adapt (0.0-1.0)
        public byte MemoryStrength;        // How long they remember experiences
        public uint PersonalitySeed;       // For consistent random behaviors

        // Current emotional state
        public EmotionalState CurrentMood;
        public float StressLevel;          // 0.0-1.0
        public float HappinessLevel;       // 0.0-1.0
        public float EnergyLevel;          // 0.0-1.0

        // Relationship tracking
        public FixedString64Bytes FavoritePlayerID;
        public float PlayerBondStrength;   // 0.0-1.0
        public int DaysSinceLastInteraction;

        /// <summary>
        /// Generate personality from genetic data
        /// </summary>
        public static CreaturePersonality GenerateFromGenetics(VisualGeneticData genetics, uint personalitySeed)
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(personalitySeed);

            var personality = new CreaturePersonality
            {
                PersonalitySeed = personalitySeed,
                LearningRate = 0.1f + (genetics.Intelligence / 100f) * 0.4f,
                MemoryStrength = (byte)(50 + genetics.Intelligence / 2),

                // Base traits influenced by genetics
                Curiosity = (byte)CalculateTraitWithVariation(genetics.Intelligence, 30, random),
                Playfulness = (byte)CalculateTraitWithVariation(genetics.Agility, 25, random),
                Aggression = (byte)CalculateTraitWithVariation(genetics.Strength, 20, random),
                Affection = (byte)CalculateTraitWithVariation(genetics.Social, 35, random),
                Independence = (byte)(100 - CalculateTraitWithVariation(genetics.Social, 25, random)),
                Nervousness = (byte)CalculateTraitWithVariation(100 - genetics.Adaptability, 30, random),
                Stubbornness = (byte)CalculateTraitWithVariation(genetics.Strength, 20, random),
                Loyalty = (byte)CalculateTraitWithVariation(genetics.Social, 40, random),

                // Initial emotional state
                CurrentMood = EmotionalState.Neutral,
                StressLevel = 0.2f,
                HappinessLevel = 0.6f,
                EnergyLevel = 0.8f,

                // Generate preferences
                FoodLikes = GenerateFoodPreferences(genetics, random),
                PreferredActivities = GenerateActivityPreferences(genetics, random),
                SocialBehavior = GenerateSocialPreferences(genetics, random),
                HabitatLikes = GenerateEnvironmentPreferences(genetics, random)
            };

            return personality;
        }

        /// <summary>
        /// Calculate trait value with genetic influence and random variation
        /// </summary>
        private static int CalculateTraitWithVariation(byte geneticBase, int variation, Unity.Mathematics.Random random)
        {
            int baseValue = geneticBase;
            int randomVariation = random.NextInt(-variation, variation + 1);
            return Mathf.Clamp(baseValue + randomVariation, 0, 100);
        }

        /// <summary>
        /// Update personality based on experiences
        /// </summary>
        public void UpdateFromExperience(ExperienceType experience, float intensity)
        {
            // Personality slowly evolves based on experiences
            switch (experience)
            {
                case ExperienceType.PositivePlayerInteraction:
                    Affection = (byte)Mathf.Min(100, Affection + (int)(intensity * LearningRate * 10));
                    Loyalty = (byte)Mathf.Min(100, Loyalty + (int)(intensity * LearningRate * 5));
                    break;

                case ExperienceType.NegativePlayerInteraction:
                    Nervousness = (byte)Mathf.Min(100, Nervousness + (int)(intensity * LearningRate * 8));
                    Independence = (byte)Mathf.Min(100, Independence + (int)(intensity * LearningRate * 5));
                    break;

                case ExperienceType.SuccessfulExploration:
                    Curiosity = (byte)Mathf.Min(100, Curiosity + (int)(intensity * LearningRate * 3));
                    break;

                case ExperienceType.Combat:
                    Aggression = (byte)Mathf.Min(100, Aggression + (int)(intensity * LearningRate * 2));
                    break;

                case ExperienceType.PlayTime:
                    Playfulness = (byte)Mathf.Min(100, Playfulness + (int)(intensity * LearningRate * 4));
                    break;
            }
        }

        /// <summary>
        /// Get personality description for UI display
        /// </summary>
        public string GetPersonalityDescription()
        {
            var traits = new System.Collections.Generic.List<string>();

            if (Curiosity > 70) traits.Add("Curious");
            if (Playfulness > 70) traits.Add("Playful");
            if (Aggression > 70) traits.Add("Aggressive");
            else if (Aggression < 30) traits.Add("Gentle");
            if (Affection > 70) traits.Add("Affectionate");
            if (Independence > 70) traits.Add("Independent");
            if (Nervousness > 70) traits.Add("Nervous");
            else if (Nervousness < 30) traits.Add("Confident");
            if (Loyalty > 80) traits.Add("Loyal");
            if (Stubbornness > 70) traits.Add("Stubborn");

            if (traits.Count == 0) traits.Add("Balanced");

            return string.Join(", ", traits);
        }

        // Helper methods for generating preferences
        private static FoodPreferences GenerateFoodPreferences(VisualGeneticData genetics, Unity.Mathematics.Random random)
        {
            return new FoodPreferences
            {
                PrefersMeat = genetics.Strength > 60,
                PrefersVegetation = genetics.Adaptability > 50,
                PrefersSweets = random.NextFloat() > 0.7f,
                FavoriteFood = (FoodType)random.NextInt(0, 5)
            };
        }

        private static ActivityPreferences GenerateActivityPreferences(VisualGeneticData genetics, Unity.Mathematics.Random random)
        {
            return new ActivityPreferences
            {
                LikesExploring = genetics.Intelligence > 50,
                LikesSwimming = random.NextFloat() > 0.6f,
                LikesClimbing = genetics.Agility > 60,
                PrefersDayTime = random.NextFloat() > 0.5f
            };
        }

        private static SocialPreferences GenerateSocialPreferences(VisualGeneticData genetics, Unity.Mathematics.Random random)
        {
            return new SocialPreferences
            {
                PrefersGroups = genetics.Social > 60,
                PrefersLeadership = genetics.Strength > 70 && genetics.Social > 50,
                GoodWithChildren = genetics.Social > 70 && random.NextFloat() > 0.4f
            };
        }

        private static EnvironmentPreferences GenerateEnvironmentPreferences(VisualGeneticData genetics, Unity.Mathematics.Random random)
        {
            return new EnvironmentPreferences
            {
                PrefersWarmth = random.NextFloat() > 0.5f,
                PrefersDarkness = random.NextFloat() > 0.7f,
                LikesWater = genetics.Adaptability > 50,
                PrefersHeights = genetics.Agility > 60
            };
        }
    }

    /// <summary>
    /// Food preferences affecting feeding behaviors
    /// </summary>
    [Serializable]
    public struct FoodPreferences
    {
        public bool PrefersMeat;
        public bool PrefersVegetation;
        public bool PrefersSweets;
        public FoodType FavoriteFood;
    }

    /// <summary>
    /// Activity preferences affecting behavior choices
    /// </summary>
    [Serializable]
    public struct ActivityPreferences
    {
        public bool LikesExploring;
        public bool LikesSwimming;
        public bool LikesClimbing;
        public bool PrefersDayTime;
    }

    /// <summary>
    /// Social behavior preferences
    /// </summary>
    [Serializable]
    public struct SocialPreferences
    {
        public bool PrefersGroups;
        public bool PrefersLeadership;
        public bool GoodWithChildren;
    }

    /// <summary>
    /// Environmental preferences
    /// </summary>
    [Serializable]
    public struct EnvironmentPreferences
    {
        public bool PrefersWarmth;
        public bool PrefersDarkness;
        public bool LikesWater;
        public bool PrefersHeights;
    }

    /// <summary>
    /// Current emotional state
    /// </summary>
    public enum EmotionalState : byte
    {
        Depressed,
        Sad,
        Neutral,
        Content,
        Happy,
        Excited,
        Angry,
        Fearful,
        Playful,
        Loving
    }

    /// <summary>
    /// Types of experiences that shape personality
    /// </summary>
    public enum ExperienceType
    {
        PositivePlayerInteraction,
        NegativePlayerInteraction,
        SuccessfulExploration,
        Combat,
        PlayTime,
        SocialBonding,
        Trauma,
        Achievement
    }

    /// <summary>
    /// Food types for preferences
    /// </summary>
    public enum FoodType
    {
        Berries,
        Fish,
        Meat,
        Vegetables,
        Sweets
    }
}