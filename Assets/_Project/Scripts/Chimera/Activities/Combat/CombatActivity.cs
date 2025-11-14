using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Activities.Combat
{
    /// <summary>
    /// Combat Arena activity implementation
    /// Performance based on: Strength (damage), Vitality (defense/HP), Intelligence (tactics)
    /// </summary>
    public class CombatActivity : IActivity
    {
        private readonly CombatConfig _config;

        public ActivityType Type => ActivityType.Combat;

        public CombatActivity(CombatConfig config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calculates combat performance from genetics
        /// Primary: Strength (offensive power and damage)
        /// Secondary: Vitality (defensive capability and HP)
        /// Tertiary: Intelligence (tactical decisions and combos)
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

            // Combat formula: Strength (primary) + Vitality (defense) + Intelligence (tactics)
            float basePerformance = ActivityPerformanceCalculator.CalculateWeightedPerformance(
                strength,         // Primary: 50% - Offensive power
                vitality,         // Secondary: 30% - Defense/HP
                intelligence,     // Tertiary: 20% - Tactical awareness
                _config.primaryStatWeight,
                _config.secondaryStatWeight,
                _config.tertiaryStatWeight);

            // Apply combat style modifier based on creature balance
            float styleBonus = CalculateCombatStyleBonus(strength, vitality, intelligence);
            basePerformance = Mathf.Clamp01(basePerformance + styleBonus);

            // Apply difficulty modifier (harder opponents require better stats)
            float difficultyMultiplier = _config.GetDifficultyMultiplier(difficulty);
            float withDifficulty = ActivityPerformanceCalculator.ApplyDifficultyModifier(
                basePerformance, difficulty, difficultyMultiplier);

            // Apply equipment and mastery bonuses
            float withBonuses = ActivityPerformanceCalculator.ApplyBonuses(
                withDifficulty, equipmentBonus, masteryBonus);

            // Add combat variance (more variance than racing due to tactical choices)
            float finalPerformance = ActivityPerformanceCalculator.AddRandomVariation(
                withBonuses, _config.combatVariance);

            return Mathf.Clamp01(finalPerformance);
        }

        /// <summary>
        /// Calculates bonus based on combat style specialization
        /// Balanced fighters get small bonus, specialists get bonus when stats align
        /// </summary>
        private float CalculateCombatStyleBonus(float strength, float vitality, float intelligence)
        {
            // Check if monster is specialized in one style
            float maxStat = Mathf.Max(strength, vitality, intelligence);
            float minStat = Mathf.Min(strength, vitality, intelligence);
            float statSpread = maxStat - minStat;

            if (statSpread > 0.3f)
            {
                // Specialist bonus (aggressive, defensive, or tactical)
                return 0.05f * statSpread; // Up to +5% bonus
            }
            else if (statSpread < 0.1f)
            {
                // Balanced fighter bonus
                return 0.03f; // Small bonus for versatility
            }

            return 0f; // No bonus for moderate specialization
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

            // Combat rewards are 20% higher than racing (higher risk)
            coins = Mathf.RoundToInt(coins * 1.2f);
            experience = Mathf.RoundToInt(experience * 1.2f);

            return new ActivityResult
            {
                activityType = ActivityType.Combat,
                difficulty = difficulty,
                status = rank,
                completionTime = completionTime,
                performanceScore = performanceScore,
                coinsEarned = coins,
                experienceGained = experience,
                tokensEarned = tokens,
                // Contribution breakdown for analytics
                strengthContribution = _config.primaryStatWeight,
                vitalityContribution = _config.secondaryStatWeight,
                intelligenceContribution = _config.tertiaryStatWeight
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
        /// Gets base duration for combat activity
        /// </summary>
        public float GetBaseDuration(ActivityDifficulty difficulty)
        {
            return _config.GetBaseDuration(difficulty);
        }
    }
}
