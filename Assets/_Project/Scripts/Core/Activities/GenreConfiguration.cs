using UnityEngine;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities
{
    /// <summary>
    /// ScriptableObject configuration for all 47 game genres
    /// Defines mechanics, scoring, and partnership dynamics for each activity type
    /// </summary>
    [CreateAssetMenu(fileName = "GenreConfig", menuName = "Chimera/Genre Configuration")]
    public class GenreConfiguration : ScriptableObject
    {
        [Header("Genre Identity")]
        [Tooltip("The specific genre this configuration represents")]
        public ActivityType genreType;

        [Tooltip("Display name for UI")]
        public string displayName;

        [Tooltip("Genre description for players")]
        [TextArea(3, 5)]
        public string description;

        [Header("Core Mechanics")]
        [Tooltip("Primary skill tested (player contribution)")]
        public PlayerSkill primaryPlayerSkill;

        [Tooltip("Primary chimera trait tested (chimera contribution)")]
        public ChimeraTrait primaryChimeraTrait;

        [Tooltip("Base duration in seconds")]
        [Range(10f, 600f)]
        public float baseDuration = 60f;

        [Tooltip("Difficulty scaling factor")]
        [Range(0.5f, 3.0f)]
        public float difficultyScaling = 1.0f;

        [Header("Scoring System")]
        [Tooltip("Base score multiplier")]
        [Range(0.1f, 10f)]
        public float scoreMultiplier = 1.0f;

        [Tooltip("How player skill affects score (0-1)")]
        [Range(0f, 1f)]
        public float playerSkillWeight = 0.6f;

        [Tooltip("How chimera traits affect score (0-1)")]
        [Range(0f, 1f)]
        public float chimeraTraitWeight = 0.4f;

        [Tooltip("Minimum score for success")]
        [Range(0f, 100f)]
        public float minimumPassingScore = 50f;

        [Header("Partnership Dynamics")]
        [Tooltip("How personality affects performance")]
        public PersonalityEffect[] personalityEffects;

        [Tooltip("Required bond strength for optimal performance")]
        [Range(0f, 1f)]
        public float optimalBondStrength = 0.7f;

        [Tooltip("Age sensitivity (how chimera age affects performance)")]
        [Range(0f, 1f)]
        public float ageSensitivity = 0.5f;

        [Header("Rewards")]
        [Tooltip("Base currency reward")]
        [Range(1, 1000)]
        public int baseCurrencyReward = 100;

        [Tooltip("Base skill mastery gain")]
        [Range(0.001f, 0.1f)]
        public float baseSkillMasteryGain = 0.01f;

        [Tooltip("Partnership quality improvement")]
        [Range(0.001f, 0.05f)]
        public float partnershipQualityGain = 0.005f;

        [Header("Genre-Specific Settings")]
        [Tooltip("Custom genre parameters (JSON)")]
        [TextArea(5, 10)]
        public string genreSpecificData;

        /// <summary>
        /// Calculate activity performance based on player skill and chimera traits
        /// </summary>
        public float CalculatePerformance(float playerSkillValue, float chimeraTraitValue, float bondStrength, int chimeraAge)
        {
            // Base performance from skills
            float performance = (playerSkillValue * playerSkillWeight) + (chimeraTraitValue * chimeraTraitWeight);

            // Bond strength multiplier
            float bondMultiplier = Mathf.Lerp(0.7f, 1.3f, bondStrength / optimalBondStrength);
            performance *= bondMultiplier;

            // Age factor (babies less consistent, adults more reliable)
            float ageFactor = CalculateAgeFactor(chimeraAge);
            performance *= ageFactor;

            // Apply difficulty scaling
            performance *= difficultyScaling;

            // Clamp to 0-100
            return Mathf.Clamp(performance * 100f, 0f, 100f);
        }

        /// <summary>
        /// Calculate reward based on performance
        /// </summary>
        public int CalculateReward(float performance)
        {
            float normalizedPerformance = performance / 100f;
            float rewardMultiplier = Mathf.Lerp(0.5f, 2.0f, normalizedPerformance);
            return Mathf.RoundToInt(baseCurrencyReward * rewardMultiplier * scoreMultiplier);
        }

        /// <summary>
        /// Calculate skill mastery gain
        /// </summary>
        public float CalculateSkillGain(float performance)
        {
            // Better performance = more skill gain
            float normalizedPerformance = performance / 100f;
            return baseSkillMasteryGain * Mathf.Lerp(0.5f, 1.5f, normalizedPerformance);
        }

        /// <summary>
        /// Calculate partnership quality improvement
        /// </summary>
        public float CalculatePartnershipGain(float performance, bool success)
        {
            if (!success) return -partnershipQualityGain * 0.5f; // Failure slightly damages bond

            float normalizedPerformance = performance / 100f;
            return partnershipQualityGain * normalizedPerformance;
        }

        /// <summary>
        /// Check if performance meets success criteria
        /// </summary>
        public bool IsSuccess(float performance)
        {
            return performance >= minimumPassingScore;
        }

        private float CalculateAgeFactor(int ageInDays)
        {
            // Age stages (approximate)
            if (ageInDays < 30) // Baby
            {
                return Mathf.Lerp(0.6f, 0.9f, ageInDays / 30f);
            }
            else if (ageInDays < 90) // Child
            {
                return Mathf.Lerp(0.9f, 1.0f, (ageInDays - 30f) / 60f);
            }
            else if (ageInDays < 180) // Teen
            {
                return 1.0f; // Peak performance
            }
            else if (ageInDays < 365) // Adult
            {
                return Mathf.Lerp(1.0f, 0.95f, (ageInDays - 180f) / 185f);
            }
            else // Elderly
            {
                return Mathf.Lerp(0.95f, 0.8f, Mathf.Min((ageInDays - 365f) / 365f, 1f));
            }
        }
    }

    /// <summary>
    /// Player skills that contribute to genre performance
    /// </summary>
    public enum PlayerSkill
    {
        Aiming,           // FPS, TPS, BulletHell
        Timing,           // Rhythm, Music, Fighting
        Strategy,         // RTS, TurnBased, Chess
        Reflexes,         // Platforming, Racing, Stealth
        ProblemSolving,   // Puzzle, Detective, Physics
        Creativity,       // MusicCreation, Construction, Crafting
        Precision,        // VehicleSim, FlightSim
        Reaction,         // BeatEmUp, HackAndSlash, Arcade
        Planning,         // 4X, GrandStrategy, CityBuilder
        Coordination,     // Sports, EndlessRunner
        Observation,      // HiddenObject, VisualNovel, WalkingSim
        Deduction,        // Detective, PointAndClick
        Memory,           // Match3, CardGame, BoardGame
        Adaptation,       // Roguelike, Roguelite, BattleRoyale
        Negotiation       // Economics, Social
    }

    /// <summary>
    /// Chimera traits that contribute to genre performance
    /// </summary>
    public enum ChimeraTrait
    {
        Speed,            // Racing, EndlessRunner, Platforming
        Agility,          // Platforming, Fighting, Stealth
        Strength,         // HackAndSlash, BeatEmUp, Sports
        Intelligence,     // Strategy, Puzzle, Detective
        Patience,         // Puzzle, Crafting, Farming
        Focus,            // Aiming, Precision activities
        Creativity,       // MusicCreation, Construction
        Endurance,        // Survival, SurvivalHorror
        Bravery,          // Horror, BattleRoyale, Combat
        Curiosity,        // Exploration, Adventure, Metroidvania
        Rhythm,           // Music, RhythmGame
        Precision,        // FlightSim, VehicleSim
        Adaptability,     // Roguelike, AutoBattler
        Leadership,       // RTS, GrandStrategy, TeamGames
        Sociability       // Social, Economics, BoardGame
    }

    /// <summary>
    /// How personality traits affect genre performance
    /// </summary>
    [System.Serializable]
    public struct PersonalityEffect
    {
        [Tooltip("Personality trait name")]
        public string traitName;

        [Tooltip("Effect multiplier (-1 to +1)")]
        [Range(-1f, 1f)]
        public float effectMultiplier;

        [Tooltip("Description of how this trait affects performance")]
        public string description;
    }
}
