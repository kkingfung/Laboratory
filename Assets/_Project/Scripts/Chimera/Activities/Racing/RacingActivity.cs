using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Activities.Racing
{
    /// <summary>
    /// Racing Circuit activity implementation
    /// Performance based on: Agility (speed/cornering), Vitality (endurance), Adaptability (track variety)
    /// Burst-compatible with Unity.Mathematics for performance
    /// </summary>
    public class RacingActivity : IActivity
    {
        private readonly RacingConfig _config;
        private Random _random;

        public ActivityType Type => ActivityType.Racing;

        public RacingActivity(RacingConfig config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _random = new Random((uint)System.DateTime.Now.Ticks);
        }

        /// <summary>
        /// Calculates racing performance from genetics
        /// Primary: Agility (speed and cornering ability)
        /// Secondary: Vitality (endurance to maintain speed)
        /// Tertiary: Adaptability (handling different track conditions)
        /// </summary>
        public float CalculatePerformance(
            in CreatureGeneticsComponent genetics,
            ActivityDifficulty difficulty,
            float equipmentBonus,
            float masteryBonus)
        {
            // Extract genetic stats
            ActivityPerformanceCalculator.ExtractGeneticStats(
                in genetics,
                out float strength,
                out float agility,
                out float intelligence,
                out float vitality,
                out float social,
                out float adaptability);

            // Racing formula: Agility (primary) + Vitality (endurance) + Adaptability (track handling)
            float basePerformance = ActivityPerformanceCalculator.CalculateWeightedPerformance(
                agility,          // Primary: 50% - Speed and cornering
                vitality,         // Secondary: 30% - Endurance
                adaptability,     // Tertiary: 20% - Track adaptation
                _config.primaryStatWeight,
                _config.secondaryStatWeight,
                _config.tertiaryStatWeight);

            // Apply difficulty modifier (harder tracks require better stats)
            float difficultyMultiplier = _config.GetDifficultyMultiplier(difficulty);
            float withDifficulty = ActivityPerformanceCalculator.ApplyDifficultyModifier(
                basePerformance, difficulty, difficultyMultiplier);

            // Apply equipment and mastery bonuses
            float withBonuses = ActivityPerformanceCalculator.ApplyBonuses(
                withDifficulty, equipmentBonus, masteryBonus);

            // Add slight random variation to simulate execution variance (Burst-compatible)
            float finalPerformance = ActivityPerformanceCalculator.AddRandomVariation(
                withBonuses, _config.performanceVariation, ref _random);

            return math.clamp(finalPerformance, 0f, 1f);
        }

        /// <summary>
        /// Calculates rewards based on performance
        /// </summary>
        public ActivityResult CalculateRewards(
            float performanceScore,
            ActivityDifficulty difficulty,
            float completionTime)
        {
            var rank = GetRank(performanceScore);

            int coins = ActivityPerformanceCalculator.CalculateCurrencyReward(
                _config.GetCoinReward(rank), difficulty, performanceScore);

            int experience = ActivityPerformanceCalculator.CalculateExperienceGain(
                _config.GetExperienceReward(rank), difficulty, performanceScore);

            int tokens = _config.GetTokenReward(rank);

            return new ActivityResult
            {
                activityType = ActivityType.Racing,
                difficulty = difficulty,
                status = rank,
                completionTime = completionTime,
                performanceScore = performanceScore,
                coinsEarned = coins,
                experienceGained = experience,
                tokensEarned = tokens,
                // Contribution breakdown for analytics
                agilityContribution = _config.primaryStatWeight,
                vitalityContribution = _config.secondaryStatWeight,
                adaptabilityContribution = _config.tertiaryStatWeight
            };
        }

        /// <summary>
        /// Gets rank from performance score
        /// </summary>
        public ActivityResultStatus GetRank(float performanceScore)
        {
            return ActivityPerformanceCalculator.GetRankFromScore(
                performanceScore, _config.rankThresholds);
        }

        /// <summary>
        /// Gets base duration for racing activity
        /// </summary>
        public float GetBaseDuration(ActivityDifficulty difficulty)
        {
            return _config.GetBaseDuration(difficulty);
        }
    }
}
