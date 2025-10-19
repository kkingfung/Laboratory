using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Genetics system for Monster Town creatures
    /// Defines core genetic traits that affect monster performance in activities
    /// </summary>
    [System.Serializable]
    public struct MonsterGenetics
    {
        [Header("Physical Traits")]
        [Range(0f, 1f)] public float Strength;     // Affects combat, physical activities
        [Range(0f, 1f)] public float Agility;     // Affects racing, platforming, speed-based activities
        [Range(0f, 1f)] public float Vitality;    // Affects health, endurance, recovery

        [Header("Mental Traits")]
        [Range(0f, 1f)] public float Intelligence; // Affects puzzle-solving, strategy games
        [Range(0f, 1f)] public float Creativity;   // Affects artistic activities, problem-solving
        [Range(0f, 1f)] public float Social;       // Affects team activities, social interactions

        [Header("Special Traits")]
        [Range(0f, 1f)] public float Adaptability; // How quickly monster learns new activities
        [Range(0f, 1f)] public float Focus;        // Reduces chance of making mistakes
        [Range(0f, 1f)] public float Luck;         // Random bonus chance in activities

        /// <summary>
        /// Create genetics with random values
        /// </summary>
        public static MonsterGenetics CreateRandom()
        {
            return new MonsterGenetics
            {
                Strength = UnityEngine.Random.Range(0.2f, 0.8f),
                Agility = UnityEngine.Random.Range(0.2f, 0.8f),
                Vitality = UnityEngine.Random.Range(0.2f, 0.8f),
                Intelligence = UnityEngine.Random.Range(0.2f, 0.8f),
                Creativity = UnityEngine.Random.Range(0.2f, 0.8f),
                Social = UnityEngine.Random.Range(0.2f, 0.8f),
                Adaptability = UnityEngine.Random.Range(0.2f, 0.8f),
                Focus = UnityEngine.Random.Range(0.2f, 0.8f),
                Luck = UnityEngine.Random.Range(0.1f, 0.3f) // Luck is generally lower
            };
        }

        /// <summary>
        /// Create genetics with specified base values and variance
        /// </summary>
        public static MonsterGenetics CreateWithVariance(float baseValue, float variance)
        {
            return new MonsterGenetics
            {
                Strength = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Agility = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Vitality = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Intelligence = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Creativity = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Social = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Adaptability = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Focus = Mathf.Clamp01(baseValue + UnityEngine.Random.Range(-variance, variance)),
                Luck = Mathf.Clamp01(0.2f + UnityEngine.Random.Range(-0.1f, 0.1f))
            };
        }

        /// <summary>
        /// Get genetic bonus for specific activity type
        /// </summary>
        public float GetActivityBonus(ActivityType activityType)
        {
            return activityType switch
            {
                // Racing activities favor agility and focus
                ActivityType.Racing => (Agility * 0.6f + Focus * 0.3f + Luck * 0.1f),

                // Combat activities favor strength and vitality
                ActivityType.Combat => (Strength * 0.5f + Vitality * 0.3f + Focus * 0.2f),

                // Platformer activities favor agility and adaptability
                ActivityType.Platformer => (Agility * 0.5f + Adaptability * 0.3f + Focus * 0.2f),

                // Puzzle activities favor intelligence and focus
                ActivityType.Puzzle => (Intelligence * 0.6f + Focus * 0.3f + Adaptability * 0.1f),

                // Rhythm activities favor agility and creativity
                ActivityType.Rhythm => (Agility * 0.4f + Creativity * 0.4f + Focus * 0.2f),

                // Strategy activities favor intelligence and focus
                ActivityType.Strategy => (Intelligence * 0.5f + Focus * 0.3f + Adaptability * 0.2f),

                // Social activities favor social and creativity
                ActivityType.Social => (Social * 0.6f + Creativity * 0.3f + Adaptability * 0.1f),

                // Artistic activities favor creativity
                ActivityType.Art => (Creativity * 0.7f + Focus * 0.2f + Luck * 0.1f),

                // Default case
                _ => GetOverallFitness() * 0.5f
            };
        }

        /// <summary>
        /// Calculate overall genetic fitness (0-1)
        /// </summary>
        public float GetOverallFitness()
        {
            return (Strength + Agility + Vitality + Intelligence + Creativity + Social + Adaptability + Focus + Luck) / 9f;
        }

        /// <summary>
        /// Get the dominant genetic trait
        /// </summary>
        public string GetDominantTrait()
        {
            float maxValue = Mathf.Max(Strength, Agility, Vitality, Intelligence, Creativity, Social, Adaptability, Focus);

            if (maxValue == Strength) return "Strong";
            if (maxValue == Agility) return "Agile";
            if (maxValue == Vitality) return "Resilient";
            if (maxValue == Intelligence) return "Smart";
            if (maxValue == Creativity) return "Creative";
            if (maxValue == Social) return "Social";
            if (maxValue == Adaptability) return "Adaptable";
            if (maxValue == Focus) return "Focused";
            return "Lucky";
        }

        /// <summary>
        /// Apply genetic mutation (for breeding or evolution)
        /// </summary>
        public MonsterGenetics ApplyMutation(float mutationRate = 0.1f)
        {
            var mutated = this;

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Strength = Mathf.Clamp01(Strength + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Agility = Mathf.Clamp01(Agility + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Vitality = Mathf.Clamp01(Vitality + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Intelligence = Mathf.Clamp01(Intelligence + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Creativity = Mathf.Clamp01(Creativity + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Social = Mathf.Clamp01(Social + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Adaptability = Mathf.Clamp01(Adaptability + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Focus = Mathf.Clamp01(Focus + UnityEngine.Random.Range(-0.1f, 0.1f));

            if (UnityEngine.Random.Range(0f, 1f) < mutationRate)
                mutated.Luck = Mathf.Clamp01(Luck + UnityEngine.Random.Range(-0.05f, 0.05f));

            return mutated;
        }

        /// <summary>
        /// Cross two genetics to create offspring genetics
        /// </summary>
        public static MonsterGenetics Crossover(MonsterGenetics parent1, MonsterGenetics parent2, float mutationRate = 0.05f)
        {
            var offspring = new MonsterGenetics
            {
                // Randomly inherit from either parent, with small variance
                Strength = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Strength : parent2.Strength) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Agility = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Agility : parent2.Agility) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Vitality = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Vitality : parent2.Vitality) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Intelligence = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Intelligence : parent2.Intelligence) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Creativity = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Creativity : parent2.Creativity) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Social = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Social : parent2.Social) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Adaptability = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Adaptability : parent2.Adaptability) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Focus = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Focus : parent2.Focus) +
                    UnityEngine.Random.Range(-0.05f, 0.05f)),

                Luck = Mathf.Clamp01(
                    (UnityEngine.Random.Range(0, 2) == 0 ? parent1.Luck : parent2.Luck) +
                    UnityEngine.Random.Range(-0.02f, 0.02f))
            };

            // Apply potential mutation
            return offspring.ApplyMutation(mutationRate);
        }

        /// <summary>
        /// Get genetics as a formatted string for display
        /// </summary>
        public override string ToString()
        {
            return $"STR:{Strength:F2} AGI:{Agility:F2} VIT:{Vitality:F2} INT:{Intelligence:F2} " +
                   $"CRE:{Creativity:F2} SOC:{Social:F2} ADA:{Adaptability:F2} FOC:{Focus:F2} LCK:{Luck:F2}";
        }
    }
}