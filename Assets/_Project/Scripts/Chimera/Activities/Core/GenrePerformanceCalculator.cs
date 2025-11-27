using UnityEngine;
using Laboratory.Core.Activities;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Integration layer between GenreConfiguration and PartnershipActivitySystem
    /// Uses genre-specific configurations for accurate performance calculations
    /// Combines player skill, chimera traits, bond strength, and age factors
    /// </summary>
    public static class GenrePerformanceCalculator
    {
        private static GenreLibrary _genreLibrary;

        /// <summary>
        /// Initialize with genre library (call once at startup)
        /// </summary>
        public static void Initialize(GenreLibrary genreLibrary)
        {
            _genreLibrary = genreLibrary;
            Debug.Log("GenrePerformanceCalculator initialized with GenreLibrary");
        }

        /// <summary>
        /// Calculate performance using genre-specific configuration
        /// </summary>
        public static float CalculatePerformance(
            ActivityType activityType,
            float playerSkillValue,
            float chimeraTraitValue,
            float bondStrength,
            int chimeraAgeInDays,
            out float skillImprovement,
            out int currencyReward)
        {
            // Get genre configuration
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
            {
                // Fallback to default calculation
                skillImprovement = 0.01f;
                currencyReward = 100;
                return CalculateDefaultPerformance(playerSkillValue, chimeraTraitValue, bondStrength);
            }

            // Use GenreConfiguration's performance calculation
            float performance = genreConfig.CalculatePerformance(
                playerSkillValue,
                chimeraTraitValue,
                bondStrength,
                chimeraAgeInDays);

            // Calculate outputs
            skillImprovement = genreConfig.CalculateSkillGain(performance);
            currencyReward = genreConfig.CalculateReward(performance);

            return performance;
        }

        /// <summary>
        /// Calculate partnership quality change based on performance
        /// </summary>
        public static float CalculatePartnershipChange(
            ActivityType activityType,
            float performance,
            bool success)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
                return success ? 0.005f : -0.0025f;

            return genreConfig.CalculatePartnershipGain(performance, success);
        }

        /// <summary>
        /// Check if performance meets success criteria
        /// </summary>
        public static bool IsSuccess(ActivityType activityType, float performance)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
                return performance >= 50f; // Default threshold

            return genreConfig.IsSuccess(performance);
        }

        /// <summary>
        /// Get recommended player skill for activity type
        /// </summary>
        public static PlayerSkill GetPrimaryPlayerSkill(ActivityType activityType)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
                return PlayerSkill.Reflexes; // Default

            return genreConfig.primaryPlayerSkill;
        }

        /// <summary>
        /// Get recommended chimera trait for activity type
        /// </summary>
        public static ChimeraTrait GetPrimaryChimeraTrait(ActivityType activityType)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
                return ChimeraTrait.Adaptability; // Default

            return genreConfig.primaryChimeraTrait;
        }

        /// <summary>
        /// Get base duration for activity
        /// </summary>
        public static float GetBaseDuration(ActivityType activityType)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
                return 60f; // Default 1 minute

            return genreConfig.baseDuration;
        }

        /// <summary>
        /// Get difficulty scaling factor
        /// </summary>
        public static float GetDifficultyScaling(ActivityType activityType)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
                return 1.0f;

            return genreConfig.difficultyScaling;
        }

        /// <summary>
        /// Get personality effect multiplier for activity
        /// </summary>
        public static float GetPersonalityEffect(ActivityType activityType, string personalityTrait)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null || genreConfig.personalityEffects == null)
                return 0f;

            foreach (var effect in genreConfig.personalityEffects)
            {
                if (effect.traitName == personalityTrait)
                    return effect.effectMultiplier;
            }

            return 0f;
        }

        /// <summary>
        /// Calculate comprehensive activity result
        /// </summary>
        public static ActivityPerformanceResult CalculateActivityResult(
            ActivityType activityType,
            float playerSkillValue,
            float chimeraTraitValue,
            float bondStrength,
            int chimeraAgeInDays,
            string[] personalityTraits = null)
        {
            var genreConfig = GetGenreConfig(activityType);
            if (genreConfig == null)
            {
                return CalculateDefaultResult(playerSkillValue, chimeraTraitValue, bondStrength);
            }

            // Base performance
            float performance = genreConfig.CalculatePerformance(
                playerSkillValue,
                chimeraTraitValue,
                bondStrength,
                chimeraAgeInDays);

            // Apply personality effects
            if (personalityTraits != null && genreConfig.personalityEffects != null)
            {
                float personalityBonus = 0f;
                foreach (var trait in personalityTraits)
                {
                    personalityBonus += GetPersonalityEffect(activityType, trait);
                }

                // Apply personality bonus (capped at ±30%)
                performance *= (1f + Mathf.Clamp(personalityBonus, -0.3f, 0.3f));
            }

            // Clamp final performance
            performance = Mathf.Clamp(performance, 0f, 100f);

            // Determine success and rank
            bool success = genreConfig.IsSuccess(performance);
            ActivityResultStatus rank = DetermineRank(performance);

            // Calculate rewards
            int currencyReward = genreConfig.CalculateReward(performance);
            float skillGain = genreConfig.CalculateSkillGain(performance);
            float partnershipGain = genreConfig.CalculatePartnershipGain(performance, success);

            return new ActivityPerformanceResult
            {
                performance = performance,
                success = success,
                rank = rank,
                currencyReward = currencyReward,
                skillGain = skillGain,
                partnershipQualityChange = partnershipGain,
                primarySkillTested = genreConfig.primaryPlayerSkill,
                primaryTraitTested = genreConfig.primaryChimeraTrait
            };
        }

        // Helper methods

        private static GenreConfiguration GetGenreConfig(ActivityType activityType)
        {
            if (_genreLibrary == null)
            {
                // Try to load from Resources as fallback
                _genreLibrary = Resources.Load<GenreLibrary>("Configs/GenreLibrary");

                if (_genreLibrary == null)
                {
                    Debug.LogWarning("GenreLibrary not found in Resources/Configs/. " +
                        "Initialize GenrePerformanceCalculator.Initialize() or place GenreLibrary.asset in Resources/Configs/");
                    return null;
                }
            }

            return _genreLibrary.GetGenreConfig(activityType);
        }

        private static float CalculateDefaultPerformance(
            float playerSkillValue,
            float chimeraTraitValue,
            float bondStrength)
        {
            // Simple weighted average
            float basePerformance = (playerSkillValue * 0.6f) + (chimeraTraitValue * 0.4f);

            // Bond strength multiplier (0.7x to 1.3x)
            float bondMultiplier = Mathf.Lerp(0.7f, 1.3f, bondStrength);

            return Mathf.Clamp(basePerformance * bondMultiplier * 100f, 0f, 100f);
        }

        private static ActivityPerformanceResult CalculateDefaultResult(
            float playerSkillValue,
            float chimeraTraitValue,
            float bondStrength)
        {
            float performance = CalculateDefaultPerformance(playerSkillValue, chimeraTraitValue, bondStrength);
            bool success = performance >= 50f;
            ActivityResultStatus rank = DetermineRank(performance);

            return new ActivityPerformanceResult
            {
                performance = performance,
                success = success,
                rank = rank,
                currencyReward = Mathf.RoundToInt(100 * (performance / 100f)),
                skillGain = 0.01f * (performance / 100f),
                partnershipQualityChange = success ? 0.005f : -0.0025f,
                primarySkillTested = PlayerSkill.Reflexes,
                primaryTraitTested = ChimeraTrait.Adaptability
            };
        }

        private static ActivityResultStatus DetermineRank(float performance)
        {
            if (performance >= 95f)
                return ActivityResultStatus.Platinum;
            else if (performance >= 80f)
                return ActivityResultStatus.Gold;
            else if (performance >= 60f)
                return ActivityResultStatus.Silver;
            else if (performance >= 40f)
                return ActivityResultStatus.Bronze;
            else
                return ActivityResultStatus.Failed;
        }
    }

    /// <summary>
    /// Comprehensive activity performance result
    /// </summary>
    public struct ActivityPerformanceResult
    {
        public float performance;
        public bool success;
        public ActivityResultStatus rank;
        public int currencyReward;
        public float skillGain;
        public float partnershipQualityChange;
        public PlayerSkill primarySkillTested;
        public ChimeraTrait primaryTraitTested;
    }
}
