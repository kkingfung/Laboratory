using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Activities.Puzzle
{
    /// <summary>
    /// Puzzle Academy activity implementation
    /// Performance based on: Intelligence (problem-solving), Adaptability (pattern recognition), Social (collaborative puzzles)
    /// </summary>
    public class PuzzleActivity : IActivity
    {
        private readonly PuzzleConfig _config;

        public ActivityType Type => ActivityType.Puzzle;

        public PuzzleActivity(PuzzleConfig config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calculates puzzle-solving performance from genetics
        /// Primary: Intelligence (logical reasoning and problem-solving)
        /// Secondary: Adaptability (pattern recognition and learning)
        /// Tertiary: Social (collaborative puzzle solving)
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

            // Puzzle formula: Intelligence (primary) + Adaptability (patterns) + Social (hints/collaboration)
            float basePerformance = ActivityPerformanceCalculator.CalculateWeightedPerformance(
                intelligence,     // Primary: 60% - Core problem-solving ability
                adaptability,     // Secondary: 25% - Pattern recognition
                social,          // Tertiary: 15% - Collaborative solving
                _config.primaryStatWeight,
                _config.secondaryStatWeight,
                _config.tertiaryStatWeight);

            // Apply puzzle type modifier based on creature specialization
            float puzzleTypeBonus = CalculatePuzzleTypeBonus(intelligence, adaptability);
            basePerformance = Mathf.Clamp01(basePerformance + puzzleTypeBonus);

            // Apply difficulty modifier (harder puzzles require better cognitive abilities)
            float difficultyMultiplier = _config.GetDifficultyMultiplier(difficulty);
            float withDifficulty = ActivityPerformanceCalculator.ApplyDifficultyModifier(
                basePerformance, difficulty, difficultyMultiplier);

            // Apply equipment and mastery bonuses
            // Mastery is very powerful for puzzles (learning from patterns)
            float enhancedMastery = Mathf.Lerp(masteryBonus, masteryBonus * 1.25f, _config.masteryLearningBonus);
            float withBonuses = ActivityPerformanceCalculator.ApplyBonuses(
                withDifficulty, equipmentBonus, enhancedMastery);

            // Minimal variance for puzzles (more deterministic than combat/racing)
            float finalPerformance = ActivityPerformanceCalculator.AddRandomVariation(
                withBonuses, _config.puzzleVariance);

            return Mathf.Clamp01(finalPerformance);
        }

        /// <summary>
        /// Calculates bonus based on puzzle-solving specialization
        /// High intelligence OR high adaptability gets bonus
        /// </summary>
        private float CalculatePuzzleTypeBonus(float intelligence, float adaptability)
        {
            // Reward high intelligence (logic) or high adaptability (patterns)
            float maxCognitiveStat = Mathf.Max(intelligence, adaptability);

            if (maxCognitiveStat > 0.8f)
            {
                // Expert bonus for cognitive excellence
                return 0.08f; // Up to +8% bonus
            }
            else if (maxCognitiveStat > 0.6f)
            {
                // Good cognitive ability bonus
                return 0.04f;
            }

            return 0f;
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

            // Puzzle rewards scale with speed bonus
            float speedBonus = CalculateSpeedBonus(completionTime, difficulty);
            coins = Mathf.RoundToInt(coins * speedBonus);
            experience = Mathf.RoundToInt(experience * speedBonus);

            return new ActivityResult
            {
                activityType = ActivityType.Puzzle,
                difficulty = difficulty,
                status = rank,
                completionTime = completionTime,
                performanceScore = performanceScore,
                coinsEarned = coins,
                experienceGained = experience,
                tokensEarned = tokens,
                // Contribution breakdown for analytics
                intelligenceContribution = _config.primaryStatWeight,
                adaptabilityContribution = _config.secondaryStatWeight,
                socialContribution = _config.tertiaryStatWeight
            };
        }

        /// <summary>
        /// Calculates speed bonus for fast puzzle completion
        /// </summary>
        private float CalculateSpeedBonus(float completionTime, ActivityDifficulty difficulty)
        {
            float targetTime = _config.GetBaseDuration(difficulty);
            float timeRatio = completionTime / targetTime;

            if (timeRatio < 0.5f)
                return 1.5f; // 50% bonus for blazing fast
            else if (timeRatio < 0.75f)
                return 1.25f; // 25% bonus for fast
            else if (timeRatio > 1.5f)
                return 0.8f; // -20% penalty for slow

            return 1.0f; // No modifier for normal speed
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
        /// Gets base duration for puzzle activity
        /// </summary>
        public float GetBaseDuration(ActivityDifficulty difficulty)
        {
            return _config.GetBaseDuration(difficulty);
        }
    }
}
