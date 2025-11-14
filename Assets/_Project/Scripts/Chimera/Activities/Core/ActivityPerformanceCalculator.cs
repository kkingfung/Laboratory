using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Static utility for calculating activity performance based on genetics
    /// Provides common performance calculation logic for all activity types
    /// </summary>
    public static class ActivityPerformanceCalculator
    {
        /// <summary>
        /// Calculates weighted performance score from genetics
        /// </summary>
        /// <param name="genetics">Monster genetics component</param>
        /// <param name="primaryStat">Primary stat value (0.0 to 1.0)</param>
        /// <param name="secondaryStat">Secondary stat value (0.0 to 1.0)</param>
        /// <param name="tertiaryStat">Tertiary stat value (0.0 to 1.0)</param>
        /// <param name="primaryWeight">Weight for primary stat</param>
        /// <param name="secondaryWeight">Weight for secondary stat</param>
        /// <param name="tertiaryWeight">Weight for tertiary stat</param>
        /// <returns>Weighted performance score (0.0 to 1.0)</returns>
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

            return Mathf.Clamp01(basePerformance);
        }

        /// <summary>
        /// Applies difficulty modifier to performance
        /// Higher difficulty requires better stats for same performance
        /// </summary>
        public static float ApplyDifficultyModifier(
            float basePerformance,
            ActivityDifficulty difficulty,
            float difficultyMultiplier)
        {
            // Difficulty makes it harder to achieve high performance
            // Easy: performance * 1.2 (easier to succeed)
            // Normal: performance * 1.0 (baseline)
            // Hard: performance * 0.8
            // Expert: performance * 0.6
            // Master: performance * 0.4

            float modifier = difficulty switch
            {
                ActivityDifficulty.Easy => 1.2f,
                ActivityDifficulty.Normal => 1.0f,
                ActivityDifficulty.Hard => 0.85f,
                ActivityDifficulty.Expert => 0.7f,
                ActivityDifficulty.Master => 0.5f,
                _ => 1.0f
            };

            return Mathf.Clamp01(basePerformance * modifier);
        }

        /// <summary>
        /// Applies equipment and mastery bonuses
        /// </summary>
        public static float ApplyBonuses(
            float basePerformance,
            float equipmentBonus,
            float masteryBonus)
        {
            // Equipment adds flat bonus (0.0 to 1.0)
            // Mastery multiplies performance (1.0 to 1.5)
            float withEquipment = Mathf.Clamp01(basePerformance + equipmentBonus);
            float withMastery = withEquipment * masteryBonus;

            return Mathf.Clamp01(withMastery);
        }

        /// <summary>
        /// Adds random variation to simulate execution variance
        /// </summary>
        public static float AddRandomVariation(float basePerformance, float variationPercent = 0.1f)
        {
            float variation = Random.Range(-variationPercent, variationPercent);
            return Mathf.Clamp01(basePerformance * (1f + variation));
        }

        /// <summary>
        /// Determines rank based on performance score
        /// </summary>
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
        /// </summary>
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

            float performanceMult = Mathf.Lerp(0.5f, 1.5f, performanceScore);

            return Mathf.RoundToInt(baseExperience * difficultyMult * performanceMult);
        }

        /// <summary>
        /// Calculates currency rewards with scaling
        /// </summary>
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

            float performanceMult = Mathf.Lerp(0.5f, 2.0f, performanceScore);

            return Mathf.RoundToInt(baseReward * difficultyMult * performanceMult);
        }

        /// <summary>
        /// Extracts genetic stats for performance calculation
        /// </summary>
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
            strength = Mathf.Clamp01(genetics.strength / 100f);
            agility = Mathf.Clamp01(genetics.agility / 100f);
            intelligence = Mathf.Clamp01(genetics.intelligence / 100f);
            vitality = Mathf.Clamp01(genetics.vitality / 100f);
            social = Mathf.Clamp01(genetics.social / 100f);
            adaptability = Mathf.Clamp01(genetics.adaptability / 100f);
        }
    }
}
