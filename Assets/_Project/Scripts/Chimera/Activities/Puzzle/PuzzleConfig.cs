using UnityEngine;

namespace Laboratory.Chimera.Activities.Puzzle
{
    /// <summary>
    /// Configuration for Puzzle Academy activity
    /// Defines puzzle types, performance scaling, and rewards
    /// </summary>
    [CreateAssetMenu(fileName = "PuzzleConfig", menuName = "Chimera/Activities/Puzzle Config")]
    public class PuzzleConfig : ActivityConfig
    {
        [Header("Puzzle-Specific Settings")]
        [Tooltip("Puzzle variance (very low for deterministic outcomes)")]
        [Range(0f, 0.1f)]
        public float puzzleVariance = 0.02f;

        [Tooltip("Mastery learning bonus multiplier")]
        [Range(0f, 1f)]
        public float masteryLearningBonus = 0.5f;

        [Tooltip("Puzzle types available")]
        public PuzzleType[] availablePuzzles = new PuzzleType[]
        {
            PuzzleType.Logic,
            PuzzleType.Pattern,
            PuzzleType.Memory,
            PuzzleType.Spatial
        };

        [Header("Puzzle Type Modifiers")]
        [Tooltip("Logic puzzles (high intelligence)")]
        public CognitiveWeights logicWeights = new CognitiveWeights
        {
            intelligence = 0.75f,
            adaptability = 0.15f,
            social = 0.10f
        };

        [Tooltip("Pattern puzzles (high adaptability)")]
        public CognitiveWeights patternWeights = new CognitiveWeights
        {
            intelligence = 0.35f,
            adaptability = 0.50f,
            social = 0.15f
        };

        [Tooltip("Memory puzzles (balanced cognitive)")]
        public CognitiveWeights memoryWeights = new CognitiveWeights
        {
            intelligence = 0.45f,
            adaptability = 0.45f,
            social = 0.10f
        };

        [Tooltip("Spatial puzzles (3D reasoning)")]
        public CognitiveWeights spatialWeights = new CognitiveWeights
        {
            intelligence = 0.60f,
            adaptability = 0.30f,
            social = 0.10f
        };

        [Header("Speed Incentives")]
        [Tooltip("Enable speed bonuses for fast completion")]
        public bool enableSpeedBonuses = true;

        [Tooltip("Maximum speed bonus multiplier")]
        [Range(1f, 2f)]
        public float maxSpeedBonus = 1.5f;

        [Header("Collaborative Puzzles")]
        [Tooltip("Enable multi-monster collaborative puzzles")]
        public bool enableCollaboration = true;

        [Tooltip("Social stat bonus for collaborative puzzles")]
        [Range(0f, 0.3f)]
        public float collaborationBonus = 0.2f;

        [Header("Equipment Recommendations")]
        [Tooltip("Recommended equipment types for puzzles")]
        public string[] recommendedEquipment = new string[]
        {
            "Focus Headband - Increase intelligence by 15%",
            "Pattern Analyzer - Boost adaptability by 12%",
            "Memory Crystal - Enhance puzzle retention",
            "Logic Enhancer - Improve problem-solving speed"
        };

        [Header("Cross-Activity Benefits")]
        [Tooltip("Puzzle skills enhance strategy games")]
        public float strategyBonusFromPuzzles = 0.20f;

        [Tooltip("Puzzle skills improve crafting quality")]
        public float craftingBonusFromPuzzles = 0.15f;

        /// <summary>
        /// Gets cognitive weights for specific puzzle type
        /// </summary>
        public CognitiveWeights GetPuzzleWeights(PuzzleType puzzleType)
        {
            return puzzleType switch
            {
                PuzzleType.Logic => logicWeights,
                PuzzleType.Pattern => patternWeights,
                PuzzleType.Memory => memoryWeights,
                PuzzleType.Spatial => spatialWeights,
                _ => logicWeights
            };
        }

        /// <summary>
        /// Determines optimal puzzle type based on monster stats
        /// </summary>
        public PuzzleType DetermineOptimalPuzzleType(float intelligence, float adaptability)
        {
            if (intelligence > adaptability + 0.2f)
                return PuzzleType.Logic;
            else if (adaptability > intelligence + 0.2f)
                return PuzzleType.Pattern;
            else if (intelligence > 0.7f && adaptability > 0.7f)
                return PuzzleType.Spatial; // Requires both
            else
                return PuzzleType.Memory; // Balanced fallback
        }

        private void OnValidate()
        {
            // Ensure puzzle is set correctly
            activityType = ActivityType.Puzzle;
            activityName = "Puzzle Academy";

            // Ensure stat weights sum to ~1.0
            float totalWeight = primaryStatWeight + secondaryStatWeight + tertiaryStatWeight;
            if (Mathf.Abs(totalWeight - 1.0f) > 0.01f)
            {
                Debug.LogWarning($"Puzzle Config: Stat weights sum to {totalWeight:F2}, should be 1.0");
            }
        }
    }

    /// <summary>
    /// Puzzle types that test different cognitive abilities
    /// </summary>
    public enum PuzzleType
    {
        Logic,      // Pure reasoning (sudoku, logic grids)
        Pattern,    // Pattern matching (sequence prediction)
        Memory,     // Memory challenges (remember sequences)
        Spatial     // 3D spatial reasoning (rotation puzzles)
    }

    /// <summary>
    /// Cognitive stat weight configuration for different puzzle types
    /// </summary>
    [System.Serializable]
    public struct CognitiveWeights
    {
        [Range(0f, 1f)] public float intelligence;
        [Range(0f, 1f)] public float adaptability;
        [Range(0f, 1f)] public float social;
    }
}
