using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Breeding.Core
{
    /// <summary>
    /// Core data structures for interactive breeding mini-games
    /// Makes genetic breeding fun and engaging with skill-based mechanics
    /// </summary>
    [Serializable]
    public struct BreedingGameData : IComponentData
    {
        // Game session info
        public FixedString64Bytes GameID;
        public BreedingGameType GameType;
        public BreedingDifficulty Difficulty;
        public float TimeLimit;
        public float ElapsedTime;
        public BreedingGameState State;

        // Parent creatures
        public Entity Parent1;
        public Entity Parent2;
        public VisualGeneticData Parent1Genetics;
        public VisualGeneticData Parent2Genetics;

        // Game mechanics
        public float PlayerSkillBonus;
        public int PerfectMatchesFound;
        public int TotalPossibleMatches;
        public float GeneticHarmonyScore;
        public float SuccessMultiplier;

        // Outcomes
        public VisualGeneticData PredictedOffspring;
        public float BreedingSuccessChance;
        public int PotentialOffspringCount;
        public bool BonusTraitsUnlocked;

        // Mini-game specific data
        public GeneMatchingData GeneMatching;
        public DNASequencingData DNASequencing;
        public TraitBalancingData TraitBalancing;
        public IncubationData Incubation;

        /// <summary>
        /// Initialize breeding game session
        /// </summary>
        public static BreedingGameData CreateSession(Entity parent1, Entity parent2, VisualGeneticData genetics1, VisualGeneticData genetics2, BreedingGameType gameType)
        {
            var gameData = new BreedingGameData
            {
                GameID = new FixedString64Bytes(GenerateGameID()),
                GameType = gameType,
                Difficulty = CalculateDifficulty(genetics1, genetics2),
                TimeLimit = GetTimeLimitForGame(gameType),
                ElapsedTime = 0f,
                State = BreedingGameState.Setup,
                Parent1 = parent1,
                Parent2 = parent2,
                Parent1Genetics = genetics1,
                Parent2Genetics = genetics2,
                PlayerSkillBonus = 1.0f,
                GeneticHarmonyScore = CalculateGeneticHarmony(genetics1, genetics2),
                SuccessMultiplier = 1.0f,
                PredictedOffspring = PredictOffspring(genetics1, genetics2),
                BreedingSuccessChance = CalculateBaseSuccessChance(genetics1, genetics2),
                PotentialOffspringCount = CalculateOffspringCount(genetics1, genetics2),
                BonusTraitsUnlocked = false
            };

            // Initialize game-specific data
            gameData = gameType switch
            {
                BreedingGameType.GeneMatching => InitializeGeneMatching(gameData),
                BreedingGameType.DNASequencing => InitializeDNASequencing(gameData),
                BreedingGameType.TraitBalancing => InitializeTraitBalancing(gameData),
                BreedingGameType.Incubation => InitializeIncubation(gameData),
                _ => gameData
            };

            return gameData;
        }

        /// <summary>
        /// Update game progress and calculate current success rate
        /// </summary>
        public void UpdateGameProgress(float deltaTime, float skillPerformance)
        {
            ElapsedTime += deltaTime;
            PlayerSkillBonus = 0.5f + (skillPerformance * 1.5f); // 0.5x to 2.0x multiplier

            // Update success chance based on player performance
            float timeBonus = Mathf.Max(0.5f, 1.0f - (ElapsedTime / TimeLimit) * 0.3f);
            SuccessMultiplier = PlayerSkillBonus * timeBonus * GeneticHarmonyScore;

            BreedingSuccessChance = Mathf.Clamp01(CalculateBaseSuccessChance(Parent1Genetics, Parent2Genetics) * SuccessMultiplier);

            // Check for bonus unlocks
            if (skillPerformance > 0.9f && !BonusTraitsUnlocked)
            {
                BonusTraitsUnlocked = true;
                PotentialOffspringCount++;
            }
        }

        // Helper methods for initialization
        private static string GenerateGameID()
        {
            return System.Guid.NewGuid().ToString("N")[..8];
        }

        private static BreedingDifficulty CalculateDifficulty(VisualGeneticData genetics1, VisualGeneticData genetics2)
        {
            int totalStats1 = genetics1.Strength + genetics1.Vitality + genetics1.Agility + genetics1.Intelligence + genetics1.Adaptability + genetics1.Social;
            int totalStats2 = genetics2.Strength + genetics2.Vitality + genetics2.Agility + genetics2.Intelligence + genetics2.Adaptability + genetics2.Social;

            int averageStats = (totalStats1 + totalStats2) / 2;
            int statDifference = Mathf.Abs(totalStats1 - totalStats2);

            if (averageStats < 300) return BreedingDifficulty.Beginner;
            if (averageStats < 400 && statDifference < 100) return BreedingDifficulty.Easy;
            if (averageStats < 500 && statDifference < 150) return BreedingDifficulty.Medium;
            if (averageStats < 550 || statDifference > 200) return BreedingDifficulty.Hard;
            return BreedingDifficulty.Expert;
        }

        private static float GetTimeLimitForGame(BreedingGameType gameType)
        {
            return gameType switch
            {
                BreedingGameType.GeneMatching => 45f,
                BreedingGameType.DNASequencing => 60f,
                BreedingGameType.TraitBalancing => 90f,
                BreedingGameType.Incubation => 30f,
                _ => 60f
            };
        }

        private static float CalculateGeneticHarmony(VisualGeneticData genetics1, VisualGeneticData genetics2)
        {
            // Calculate how well the genetics complement each other
            float harmony = 0.5f;

            // Complementary traits boost harmony
            if (genetics1.Strength > 70 && genetics2.Intelligence > 70) harmony += 0.2f;
            if (genetics1.Agility > 70 && genetics2.Vitality > 70) harmony += 0.2f;
            if (genetics1.Social > 70 && genetics2.Adaptability > 70) harmony += 0.2f;

            // Special markers create synergy
            var combinedMarkers = genetics1.SpecialMarkers | genetics2.SpecialMarkers;
            int markerCount = Unity.Mathematics.math.countbits((uint)combinedMarkers);
            harmony += markerCount * 0.1f;

            return Mathf.Clamp01(harmony);
        }

        private static VisualGeneticData PredictOffspring(VisualGeneticData parent1, VisualGeneticData parent2)
        {
            // Simplified Mendelian inheritance prediction
            return new VisualGeneticData
            {
                Strength = (byte)((parent1.Strength + parent2.Strength) / 2 + UnityEngine.Random.Range(-10, 11)),
                Vitality = (byte)((parent1.Vitality + parent2.Vitality) / 2 + UnityEngine.Random.Range(-10, 11)),
                Agility = (byte)((parent1.Agility + parent2.Agility) / 2 + UnityEngine.Random.Range(-10, 11)),
                Intelligence = (byte)((parent1.Intelligence + parent2.Intelligence) / 2 + UnityEngine.Random.Range(-10, 11)),
                Adaptability = (byte)((parent1.Adaptability + parent2.Adaptability) / 2 + UnityEngine.Random.Range(-10, 11)),
                Social = (byte)((parent1.Social + parent2.Social) / 2 + UnityEngine.Random.Range(-10, 11)),
                SpecialMarkers = parent1.SpecialMarkers | parent2.SpecialMarkers
            };
        }

        private static float CalculateBaseSuccessChance(VisualGeneticData genetics1, VisualGeneticData genetics2)
        {
            int totalStats1 = genetics1.Strength + genetics1.Vitality + genetics1.Agility + genetics1.Intelligence + genetics1.Adaptability + genetics1.Social;
            int totalStats2 = genetics2.Strength + genetics2.Vitality + genetics2.Agility + genetics2.Intelligence + genetics2.Adaptability + genetics2.Social;

            float averageQuality = (totalStats1 + totalStats2) / (2f * 600f); // Max possible is 600
            return Mathf.Clamp(0.3f + averageQuality * 0.5f, 0.3f, 0.95f);
        }

        private static int CalculateOffspringCount(VisualGeneticData genetics1, VisualGeneticData genetics2)
        {
            int vitalitySum = genetics1.Vitality + genetics2.Vitality;
            int socialSum = genetics1.Social + genetics2.Social;

            if (vitalitySum > 160 && socialSum > 140) return 3;
            if (vitalitySum > 120 || socialSum > 120) return 2;
            return 1;
        }

        private static BreedingGameData InitializeGeneMatching(BreedingGameData gameData)
        {
            gameData.GeneMatching = new GeneMatchingData
            {
                GridSize = gameData.Difficulty switch
                {
                    BreedingDifficulty.Beginner => 4,
                    BreedingDifficulty.Easy => 5,
                    BreedingDifficulty.Medium => 6,
                    BreedingDifficulty.Hard => 7,
                    BreedingDifficulty.Expert => 8,
                    _ => 5
                },
                MatchesRequired = gameData.Difficulty switch
                {
                    BreedingDifficulty.Beginner => 6,
                    BreedingDifficulty.Easy => 10,
                    BreedingDifficulty.Medium => 15,
                    BreedingDifficulty.Hard => 20,
                    BreedingDifficulty.Expert => 25,
                    _ => 10
                },
                MatchesFound = 0,
                ComboMultiplier = 1.0f,
                TimeBonus = 1.0f
            };

            gameData.TotalPossibleMatches = gameData.GeneMatching.MatchesRequired;
            return gameData;
        }

        private static BreedingGameData InitializeDNASequencing(BreedingGameData gameData)
        {
            gameData.DNASequencing = new DNASequencingData
            {
                SequenceLength = gameData.Difficulty switch
                {
                    BreedingDifficulty.Beginner => 6,
                    BreedingDifficulty.Easy => 8,
                    BreedingDifficulty.Medium => 10,
                    BreedingDifficulty.Hard => 12,
                    BreedingDifficulty.Expert => 15,
                    _ => 8
                },
                CorrectSequences = 0,
                TotalSequences = 3,
                AccuracyBonus = 1.0f,
                SpeedBonus = 1.0f
            };

            gameData.TotalPossibleMatches = gameData.DNASequencing.TotalSequences;
            return gameData;
        }

        private static BreedingGameData InitializeTraitBalancing(BreedingGameData gameData)
        {
            gameData.TraitBalancing = new TraitBalancingData
            {
                TargetBalance = 0.8f,
                CurrentBalance = 0.0f,
                BalanceThreshold = 0.1f,
                StabilityBonus = 1.0f,
                PrecisionBonus = 1.0f
            };

            gameData.TotalPossibleMatches = 1; // Single balance target
            return gameData;
        }

        private static BreedingGameData InitializeIncubation(BreedingGameData gameData)
        {
            gameData.Incubation = new IncubationData
            {
                TargetTemperature = UnityEngine.Random.Range(35f, 42f),
                CurrentTemperature = 25f,
                TemperatureTolerance = 2.0f,
                OptimalTime = 0f,
                TotalTime = gameData.TimeLimit,
                StabilityScore = 0f
            };

            gameData.TotalPossibleMatches = 1; // Single incubation target
            return gameData;
        }
    }

    /// <summary>
    /// Gene matching mini-game data
    /// </summary>
    [Serializable]
    public struct GeneMatchingData
    {
        public int GridSize;
        public int MatchesRequired;
        public int MatchesFound;
        public float ComboMultiplier;
        public float TimeBonus;
        public FixedList64Bytes<GeneCard> ActiveCards;
    }

    /// <summary>
    /// DNA sequencing mini-game data
    /// </summary>
    [Serializable]
    public struct DNASequencingData
    {
        public int SequenceLength;
        public int CorrectSequences;
        public int TotalSequences;
        public float AccuracyBonus;
        public float SpeedBonus;
        public FixedString32Bytes CurrentSequence;
        public FixedString32Bytes TargetSequence;
    }

    /// <summary>
    /// Trait balancing mini-game data
    /// </summary>
    [Serializable]
    public struct TraitBalancingData
    {
        public float TargetBalance;
        public float CurrentBalance;
        public float BalanceThreshold;
        public float StabilityBonus;
        public float PrecisionBonus;
        public float BalanceHistory; // Average balance over time
    }

    /// <summary>
    /// Incubation mini-game data
    /// </summary>
    [Serializable]
    public struct IncubationData
    {
        public float TargetTemperature;
        public float CurrentTemperature;
        public float TemperatureTolerance;
        public float OptimalTime;
        public float TotalTime;
        public float StabilityScore;
    }

    /// <summary>
    /// Gene card for matching game
    /// </summary>
    [Serializable]
    public struct GeneCard
    {
        public byte TraitType;    // 0-5 for the 6 traits
        public byte TraitValue;   // The actual trait value
        public bool IsRevealed;
        public bool IsMatched;
        public Vector2 GridPosition;
    }

    /// <summary>
    /// Breeding game types
    /// </summary>
    public enum BreedingGameType : byte
    {
        GeneMatching,     // Match complementary gene pairs
        DNASequencing,    // Arrange DNA sequences correctly
        TraitBalancing,   // Balance trait distribution
        Incubation,       // Maintain optimal conditions
        RandomSelection   // System picks random game
    }

    /// <summary>
    /// Breeding difficulty levels
    /// </summary>
    public enum BreedingDifficulty : byte
    {
        Beginner,   // Easy mechanics, forgiving timing
        Easy,       // Basic challenge
        Medium,     // Standard difficulty
        Hard,       // Requires skill and timing
        Expert      // Master-level precision required
    }

    /// <summary>
    /// Breeding game states
    /// </summary>
    public enum BreedingGameState : byte
    {
        Setup,        // Initializing game
        Tutorial,     // Showing instructions
        Playing,      // Active gameplay
        Paused,       // Game paused
        Completing,   // Calculating results
        Completed,    // Game finished
        Failed        // Game failed/timed out
    }

    /// <summary>
    /// Breeding game results
    /// </summary>
    [Serializable]
    public struct BreedingGameResults : IComponentData
    {
        public FixedString64Bytes GameID;
        public BreedingGameType GameType;
        public bool Success;
        public float FinalScore;
        public float SkillMultiplier;
        public float TimeBonus;
        public float PerfectionBonus;

        // Breeding outcomes
        public int OffspringCount;
        public bool BonusTraitsEarned;
        public bool PerfectBreeding;
        public float GeneticQualityBonus;

        // Player progression
        public int ExperienceGained;
        public bool LevelUp;
        public bool NewAbilityUnlocked;
    }
}