using Unity.Mathematics;
using Unity.Burst;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Burst-compiled static utility for calculating activity performance based on genetics
    /// Provides common performance calculation logic for all activity types
    /// Performance: All methods are Burst-compiled for native performance on all platforms
    /// </summary>
    [BurstCompile]
    public static class ActivityPerformanceCalculator
    {
        /// <summary>
        /// Calculates weighted performance score from genetics
        /// Burst-compiled for optimal performance
        /// </summary>
        /// <param name="genetics">Monster genetics component</param>
        /// <param name="primaryStat">Primary stat value (0.0 to 1.0)</param>
        /// <param name="secondaryStat">Secondary stat value (0.0 to 1.0)</param>
        /// <param name="tertiaryStat">Tertiary stat value (0.0 to 1.0)</param>
        /// <param name="primaryWeight">Weight for primary stat</param>
        /// <param name="secondaryWeight">Weight for secondary stat</param>
        /// <param name="tertiaryWeight">Weight for tertiary stat</param>
        /// <returns>Weighted performance score (0.0 to 1.0)</returns>
        [BurstCompile]
        public static float CalculateWeightedPerformance(
            float primaryStat,
            float secondaryStat,
            float tertiaryStat,
            float primaryWeight,
            float secondaryWeight,
            float tertiaryWeight)
        {
            // Normalize weights to sum to 1.0
            float totalWeight = primaryWeight + secondaryWeight + tertiaryWeight;
            if (totalWeight <= 0) return 0f;

            float normalizedPrimary = primaryWeight / totalWeight;
            float normalizedSecondary = secondaryWeight / totalWeight;
            float normalizedTertiary = tertiaryWeight / totalWeight;

            // Calculate weighted sum
            float basePerformance =
                (primaryStat * normalizedPrimary) +
                (secondaryStat * normalizedSecondary) +
                (tertiaryStat * normalizedTertiary);

            return math.clamp(basePerformance, 0f, 1f);
        }

        /// <summary>
        /// Applies difficulty modifier to performance
        /// Higher difficulty requires better stats for same performance
        /// Burst-compiled for optimal performance
        /// </summary>
        [BurstCompile]
        public static float ApplyDifficultyModifier(
            float basePerformance,
            ActivityDifficulty difficulty,
            float difficultyMultiplier)
        {
            // Difficulty makes it harder to achieve high performance
            // Easy: performance * 1.2 (easier to succeed)
            // Normal: performance * 1.0 (baseline)
            // Hard: performance * 0.85
            // Expert: performance * 0.7
            // Master: performance * 0.5

            float modifier = difficulty switch
            {
                ActivityDifficulty.Easy => 1.2f,
                ActivityDifficulty.Normal => 1.0f,
                ActivityDifficulty.Hard => 0.85f,
                ActivityDifficulty.Expert => 0.7f,
                ActivityDifficulty.Master => 0.5f,
                _ => 1.0f
            };

            return math.clamp(basePerformance * modifier, 0f, 1f);
        }

        /// <summary>
        /// Applies equipment and mastery bonuses
        /// Burst-compiled for optimal performance
        /// </summary>
        [BurstCompile]
        public static float ApplyBonuses(
            float basePerformance,
            float equipmentBonus,
            float masteryBonus)
        {
            // Equipment adds flat bonus (0.0 to 1.0)
            // Mastery multiplies performance (1.0 to 1.5)
            float withEquipment = math.clamp(basePerformance + equipmentBonus, 0f, 1f);
            float withMastery = withEquipment * masteryBonus;

            return math.clamp(withMastery, 0f, 1f);
        }

        /// <summary>
        /// Adds random variation to simulate execution variance
        /// Burst-compiled for optimal performance
        /// </summary>
        /// <param name="basePerformance">Base performance score</param>
        /// <param name="variationPercent">Variation percentage (0.0 to 1.0)</param>
        /// <param name="random">Unity.Mathematics.Random instance for Burst compatibility</param>
        [BurstCompile]
        public static float AddRandomVariation(float basePerformance, float variationPercent, ref Random random)
        {
            float variation = random.NextFloat(-variationPercent, variationPercent);
            return math.clamp(basePerformance * (1f + variation), 0f, 1f);
        }

        /// <summary>
        /// Determines rank based on performance score
        /// Burst-compiled for optimal performance
        /// </summary>
        [BurstCompile]
        public static ActivityResultStatus GetRankFromScore(
            float performanceScore,
            RankThresholds thresholds)
        {
            if (performanceScore >= thresholds.platinum)
                return ActivityResultStatus.Platinum;
            if (performanceScore >= thresholds.gold)
                return ActivityResultStatus.Gold;
            if (performanceScore >= thresholds.silver)
                return ActivityResultStatus.Silver;
            if (performanceScore >= thresholds.bronze)
                return ActivityResultStatus.Bronze;

            return ActivityResultStatus.Failed;
        }

        /// <summary>
        /// Calculates experience gain with bonus scaling
        /// Burst-compiled for optimal performance
        /// </summary>
        [BurstCompile]
        public static int CalculateExperienceGain(
            int baseExperience,
            ActivityDifficulty difficulty,
            float performanceScore)
        {
            // Base XP * difficulty multiplier * performance multiplier
            float difficultyMult = difficulty switch
            {
                ActivityDifficulty.Easy => 0.5f,
                ActivityDifficulty.Normal => 1.0f,
                ActivityDifficulty.Hard => 1.5f,
                ActivityDifficulty.Expert => 2.0f,
                ActivityDifficulty.Master => 3.0f,
                _ => 1.0f
            };

            float performanceMult = math.lerp(0.5f, 1.5f, performanceScore);

            return (int)math.round(baseExperience * difficultyMult * performanceMult);
        }

        /// <summary>
        /// Calculates currency rewards with scaling
        /// Burst-compiled for optimal performance
        /// </summary>
        [BurstCompile]
        public static int CalculateCurrencyReward(
            int baseReward,
            ActivityDifficulty difficulty,
            float performanceScore)
        {
            float difficultyMult = difficulty switch
            {
                ActivityDifficulty.Easy => 0.5f,
                ActivityDifficulty.Normal => 1.0f,
                ActivityDifficulty.Hard => 1.5f,
                ActivityDifficulty.Expert => 2.0f,
                ActivityDifficulty.Master => 3.0f,
                _ => 1.0f
            };

            float performanceMult = math.lerp(0.5f, 2.0f, performanceScore);

            return (int)math.round(baseReward * difficultyMult * performanceMult);
        }

        /// <summary>
        /// Extracts genetic stats for performance calculation
        /// Burst-compiled for optimal performance
        /// </summary>
        [BurstCompile]
        public static void ExtractGeneticStats(
            in CreatureGeneticsComponent genetics,
            out float strength,
            out float agility,
            out float intelligence,
            out float vitality,
            out float social,
            out float adaptability)
        {
            // Extract normalized stat values (0.0 to 1.0)
            strength = math.clamp(genetics.strength / 100f, 0f, 1f);
            agility = math.clamp(genetics.agility / 100f, 0f, 1f);
            intelligence = math.clamp(genetics.intelligence / 100f, 0f, 1f);
            vitality = math.clamp(genetics.vitality / 100f, 0f, 1f);
            social = math.clamp(genetics.social / 100f, 0f, 1f);
            adaptability = math.clamp(genetics.adaptability / 100f, 0f, 1f);
        }
    }
}
