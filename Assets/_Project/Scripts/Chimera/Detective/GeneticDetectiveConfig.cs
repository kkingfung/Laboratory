using System;
using UnityEngine;

namespace Laboratory.Chimera.Detective
{
    /// <summary>
    /// Configuration ScriptableObject for the Genetic Detective System.
    /// Allows designers to tune mystery generation, investigation mechanics, and reward systems.
    /// </summary>
    [CreateAssetMenu(fileName = "GeneticDetectiveConfig", menuName = "Chimera/Detective/Detective Config")]
    public class GeneticDetectiveConfig : ScriptableObject
    {
        [Header("Mystery Generation")]
        [Tooltip("Daily probability of generating new mysteries")]
        [Range(0.001f, 0.2f)]
        public float mysteryGenerationRate = 0.02f;

        [Tooltip("Maximum number of active mysteries simultaneously")]
        [Range(1, 20)]
        public int maxActiveMysteries = 5;

        [Tooltip("Days before mysteries become cold cases")]
        [Range(7f, 90f)]
        public float caseTimeoutDays = 30f;

        [Tooltip("Enable automatic mystery generation")]
        public bool enableAutoGeneration = true;

        [Header("Mystery Type Weights")]
        [Tooltip("Relative probability of each mystery type")]
        public MysteryTypeWeight[] mysteryTypeWeights = new MysteryTypeWeight[]
        {
            new MysteryTypeWeight { type = MysteryType.MysteryCreature, weight = 0.3f },
            new MysteryTypeWeight { type = MysteryType.BreedingCrime, weight = 0.2f },
            new MysteryTypeWeight { type = MysteryType.BloodlineInvestigation, weight = 0.25f },
            new MysteryTypeWeight { type = MysteryType.GeneticAnomaly, weight = 0.15f },
            new MysteryTypeWeight { type = MysteryType.IllegalExperimentation, weight = 0.1f }
        };

        [Header("Investigation Mechanics")]
        [Tooltip("Base forensic analysis accuracy")]
        [Range(0.5f, 1f)]
        public float baseForensicAccuracy = 0.85f;

        [Tooltip("Evidence quality decay rate per hour")]
        [Range(0f, 0.1f)]
        public float evidenceDecayRate = 0.005f;

        [Tooltip("Maximum evidence samples per case")]
        [Range(5, 50)]
        public int maxEvidencePerCase = 20;

        [Tooltip("Minimum evidence quality for reliable analysis")]
        [Range(0.1f, 0.8f)]
        public float minReliableEvidenceQuality = 0.4f;

        [Header("Difficulty Settings")]
        public DifficultyConfig[] difficultyConfigs = new DifficultyConfig[]
        {
            new DifficultyConfig
            {
                difficulty = CaseDifficulty.Novice,
                baseReward = 100,
                requiredEvidence = 3,
                solutionSteps = new Vector2Int(2, 4),
                timeLimit = 14f,
                hintsAvailable = 3
            },
            new DifficultyConfig
            {
                difficulty = CaseDifficulty.Intermediate,
                baseReward = 250,
                requiredEvidence = 5,
                solutionSteps = new Vector2Int(4, 7),
                timeLimit = 21f,
                hintsAvailable = 2
            },
            new DifficultyConfig
            {
                difficulty = CaseDifficulty.Expert,
                baseReward = 500,
                requiredEvidence = 8,
                solutionSteps = new Vector2Int(6, 10),
                timeLimit = 30f,
                hintsAvailable = 1
            },
            new DifficultyConfig
            {
                difficulty = CaseDifficulty.Master,
                baseReward = 1000,
                requiredEvidence = 12,
                solutionSteps = new Vector2Int(8, 15),
                timeLimit = 45f,
                hintsAvailable = 0
            },
            new DifficultyConfig
            {
                difficulty = CaseDifficulty.Legendary,
                baseReward = 2500,
                requiredEvidence = 20,
                solutionSteps = new Vector2Int(12, 25),
                timeLimit = 60f,
                hintsAvailable = 0
            }
        };

        [Header("Detective Progression")]
        [Tooltip("Starting detective reputation")]
        [Range(0f, 100f)]
        public float startingDetectiveReputation = 10f;

        [Tooltip("Base reputation gain for solving cases")]
        [Range(10f, 200f)]
        public float baseSolutionReputation = 50f;

        [Tooltip("Reputation multiplier for speed bonus")]
        [Range(1f, 3f)]
        public float speedBonusMultiplier = 1.3f;

        [Tooltip("Reputation multiplier for high-quality evidence")]
        [Range(1f, 2f)]
        public float evidenceQualityMultiplier = 1.5f;

        [Header("Specialization System")]
        [Tooltip("Enable detective specialization")]
        public bool enableSpecialization = true;

        [Tooltip("Cases required to unlock specialization")]
        [Range(5, 50)]
        public int casesForSpecialization = 10;

        public SpecializationConfig[] specializationConfigs = new SpecializationConfig[]
        {
            new SpecializationConfig
            {
                specialization = DetectiveSpecialization.GeneticForensics,
                displayName = "Genetic Forensics Expert",
                bonusAccuracy = 0.15f,
                bonusEvidenceQuality = 0.2f,
                description = "Specializes in analyzing genetic evidence and DNA forensics"
            },
            new SpecializationConfig
            {
                specialization = DetectiveSpecialization.BreedingCrimes,
                displayName = "Breeding Crime Investigator",
                bonusAccuracy = 0.1f,
                bonusReward = 0.25f,
                description = "Expert in investigating illegal breeding activities"
            },
            new SpecializationConfig
            {
                specialization = DetectiveSpecialization.BloodlineExpert,
                displayName = "Bloodline Detective",
                bonusAccuracy = 0.12f,
                bonusReward = 0.2f,
                description = "Specialist in tracing creature lineages and bloodlines"
            },
            new SpecializationConfig
            {
                specialization = DetectiveSpecialization.AnomalySpecialist,
                displayName = "Genetic Anomaly Investigator",
                bonusAccuracy = 0.2f,
                bonusReward = 0.3f,
                description = "Expert in investigating unexplained genetic phenomena"
            }
        };

        [Header("Evidence Types")]
        public EvidenceTypeConfig[] evidenceTypeConfigs = new EvidenceTypeConfig[]
        {
            new EvidenceTypeConfig
            {
                type = EvidenceType.GeneticSample,
                baseQuality = 0.7f,
                analysisTime = 30f,
                requiredTools = new[] { "Genetic Analyzer", "DNA Sequencer" },
                description = "Biological sample containing genetic material"
            },
            new EvidenceTypeConfig
            {
                type = EvidenceType.EnvironmentalTrace,
                baseQuality = 0.5f,
                analysisTime = 15f,
                requiredTools = new[] { "Environmental Scanner" },
                description = "Traces of creature presence in environment"
            },
            new EvidenceTypeConfig
            {
                type = EvidenceType.BreedingRecord,
                baseQuality = 0.8f,
                analysisTime = 10f,
                requiredTools = new[] { "Data Terminal" },
                description = "Official breeding facility records"
            },
            new EvidenceTypeConfig
            {
                type = EvidenceType.WitnessTestimony,
                baseQuality = 0.4f,
                analysisTime = 60f,
                requiredTools = new[] { "Interview Room" },
                description = "Testimony from witnesses or suspects"
            },
            new EvidenceTypeConfig
            {
                type = EvidenceType.DocumentaryEvidence,
                baseQuality = 0.6f,
                analysisTime = 20f,
                requiredTools = new[] { "Document Analyzer" },
                description = "Written records and documentation"
            }
        };

        [Header("Cold Case System")]
        [Tooltip("Enable cold case investigations")]
        public bool enableColdCases = true;

        [Tooltip("Probability of cold case reactivation per day")]
        [Range(0.001f, 0.1f)]
        public float coldCaseReactivationRate = 0.05f;

        [Tooltip("Bonus reward for solving cold cases")]
        [Range(100, 2000)]
        public int coldCaseBonusReward = 500;

        [Tooltip("Maximum number of active cold cases")]
        [Range(1, 10)]
        public int maxActiveColdCases = 5;

        [Header("Forensic Tools")]
        public ForensicToolConfig[] forensicTools = new ForensicToolConfig[]
        {
            new ForensicToolConfig
            {
                toolName = "Basic DNA Analyzer",
                accuracyBonus = 0.05f,
                speedBonus = 0f,
                unlockReputation = 0f,
                description = "Standard genetic analysis equipment"
            },
            new ForensicToolConfig
            {
                toolName = "Advanced Genetic Sequencer",
                accuracyBonus = 0.15f,
                speedBonus = 0.1f,
                unlockReputation = 100f,
                description = "High-precision genetic analysis with mutation detection"
            },
            new ForensicToolConfig
            {
                toolName = "Quantum DNA Processor",
                accuracyBonus = 0.25f,
                speedBonus = 0.2f,
                unlockReputation = 500f,
                description = "Cutting-edge quantum genetic analysis technology"
            }
        };

        [Header("Reward System")]
        [Tooltip("Base currency reward multiplier")]
        [Range(0.5f, 3f)]
        public float baseRewardMultiplier = 1f;

        [Tooltip("Experience points per solved case")]
        [Range(10, 500)]
        public int baseExperienceReward = 100;

        [Tooltip("Bonus items for legendary cases")]
        public bool enableLegendaryRewards = true;

        [Header("Performance Settings")]
        [Tooltip("Maximum active investigations per player")]
        [Range(1, 10)]
        public int maxActiveInvestigationsPerPlayer = 3;

        [Tooltip("Evidence processing frequency (seconds)")]
        [Range(1f, 60f)]
        public float evidenceProcessingInterval = 5f;

        [Tooltip("Case update frequency (seconds)")]
        [Range(60f, 3600f)]
        public float caseUpdateInterval = 300f;

        /// <summary>
        /// Gets the difficulty configuration for a specific case difficulty
        /// </summary>
        public DifficultyConfig GetDifficultyConfig(CaseDifficulty difficulty)
        {
            foreach (var config in difficultyConfigs)
            {
                if (config.difficulty == difficulty)
                    return config;
            }

            // Return default if not found
            return difficultyConfigs[0];
        }

        /// <summary>
        /// Gets the specialization configuration for a detective specialization
        /// </summary>
        public SpecializationConfig GetSpecializationConfig(DetectiveSpecialization specialization)
        {
            foreach (var config in specializationConfigs)
            {
                if (config.specialization == specialization)
                    return config;
            }

            // Return default if not found
            return new SpecializationConfig
            {
                specialization = specialization,
                displayName = specialization.ToString(),
                bonusAccuracy = 0f,
                bonusReward = 0f,
                description = "General detective work"
            };
        }

        /// <summary>
        /// Gets the evidence type configuration
        /// </summary>
        public EvidenceTypeConfig GetEvidenceTypeConfig(EvidenceType evidenceType)
        {
            foreach (var config in evidenceTypeConfigs)
            {
                if (config.type == evidenceType)
                    return config;
            }

            // Return default if not found
            return evidenceTypeConfigs[0];
        }

        /// <summary>
        /// Calculates the total reward for solving a case
        /// </summary>
        public int CalculateCaseReward(CaseDifficulty difficulty, float solutionTime, float timeLimit, float evidenceQuality)
        {
            var difficultyConfig = GetDifficultyConfig(difficulty);
            float reward = difficultyConfig.baseReward * baseRewardMultiplier;

            // Speed bonus
            if (solutionTime < timeLimit * 0.5f)
                reward *= speedBonusMultiplier;

            // Evidence quality bonus
            if (evidenceQuality > 0.8f)
                reward *= evidenceQualityMultiplier;

            return Mathf.RoundToInt(reward);
        }

        /// <summary>
        /// Calculates reputation gain for solving a case
        /// </summary>
        public float CalculateReputationGain(CaseDifficulty difficulty, float solutionTime, float timeLimit, DetectiveSpecialization specialization, MysteryType mysteryType)
        {
            var difficultyConfig = GetDifficultyConfig(difficulty);
            float reputation = baseSolutionReputation;

            // Difficulty multiplier
            reputation *= difficulty switch
            {
                CaseDifficulty.Novice => 0.5f,
                CaseDifficulty.Intermediate => 1f,
                CaseDifficulty.Expert => 1.5f,
                CaseDifficulty.Master => 2f,
                CaseDifficulty.Legendary => 3f,
                _ => 1f
            };

            // Speed bonus
            if (solutionTime < timeLimit * 0.5f)
                reputation *= speedBonusMultiplier;

            // Specialization bonus
            if (IsSpecializationMatch(specialization, mysteryType))
                reputation *= 1.2f;

            return reputation;
        }

        /// <summary>
        /// Checks if detective specialization matches mystery type
        /// </summary>
        public bool IsSpecializationMatch(DetectiveSpecialization specialization, MysteryType mysteryType)
        {
            return (specialization, mysteryType) switch
            {
                (DetectiveSpecialization.GeneticForensics, MysteryType.MysteryCreature) => true,
                (DetectiveSpecialization.BreedingCrimes, MysteryType.BreedingCrime) => true,
                (DetectiveSpecialization.BloodlineExpert, MysteryType.BloodlineInvestigation) => true,
                (DetectiveSpecialization.AnomalySpecialist, MysteryType.GeneticAnomaly) => true,
                (DetectiveSpecialization.AnomalySpecialist, MysteryType.IllegalExperimentation) => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets weighted random mystery type
        /// </summary>
        public MysteryType GetRandomMysteryType()
        {
            float totalWeight = 0f;
            foreach (var weight in mysteryTypeWeights)
            {
                totalWeight += weight.weight;
            }

            float randomValue = UnityEngine.Random.value * totalWeight;
            float currentWeight = 0f;

            foreach (var weight in mysteryTypeWeights)
            {
                currentWeight += weight.weight;
                if (randomValue <= currentWeight)
                {
                    return weight.type;
                }
            }

            return MysteryType.MysteryCreature; // Default
        }

        void OnValidate()
        {
            // Ensure reasonable values
            mysteryGenerationRate = Mathf.Clamp(mysteryGenerationRate, 0.001f, 0.2f);
            maxActiveMysteries = Mathf.Clamp(maxActiveMysteries, 1, 20);
            caseTimeoutDays = Mathf.Clamp(caseTimeoutDays, 7f, 90f);

            // Ensure difficulty configs are properly ordered
            for (int i = 1; i < difficultyConfigs.Length; i++)
            {
                if (difficultyConfigs[i].baseReward <= difficultyConfigs[i - 1].baseReward)
                {
                    difficultyConfigs[i].baseReward = difficultyConfigs[i - 1].baseReward + 100;
                }
            }

            // Normalize mystery type weights
            float totalWeight = 0f;
            foreach (var weight in mysteryTypeWeights)
            {
                totalWeight += weight.weight;
            }

            if (totalWeight > 0f)
            {
                for (int i = 0; i < mysteryTypeWeights.Length; i++)
                {
                    mysteryTypeWeights[i].weight /= totalWeight;
                }
            }

            // Ensure evidence type configs have required tools
            for (int i = 0; i < evidenceTypeConfigs.Length; i++)
            {
                if (evidenceTypeConfigs[i].requiredTools == null || evidenceTypeConfigs[i].requiredTools.Length == 0)
                {
                    evidenceTypeConfigs[i].requiredTools = new[] { "Basic Equipment" };
                }
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Weight configuration for mystery type generation
    /// </summary>
    [Serializable]
    public struct MysteryTypeWeight
    {
        public MysteryType type;
        [Range(0f, 1f)]
        public float weight;
    }

    /// <summary>
    /// Configuration for case difficulty levels
    /// </summary>
    [Serializable]
    public struct DifficultyConfig
    {
        [Tooltip("The difficulty level this configures")]
        public CaseDifficulty difficulty;

        [Tooltip("Base reward for solving cases of this difficulty")]
        public int baseReward;

        [Tooltip("Minimum evidence required to solve")]
        [Range(1, 50)]
        public int requiredEvidence;

        [Tooltip("Range of solution steps required")]
        public Vector2Int solutionSteps;

        [Tooltip("Time limit in days")]
        [Range(1f, 90f)]
        public float timeLimit;

        [Tooltip("Number of hints available")]
        [Range(0, 10)]
        public int hintsAvailable;

        [Tooltip("Special requirements for this difficulty")]
        public string[] specialRequirements;
    }

    /// <summary>
    /// Configuration for detective specializations
    /// </summary>
    [Serializable]
    public struct SpecializationConfig
    {
        [Tooltip("The specialization this configures")]
        public DetectiveSpecialization specialization;

        [Tooltip("Display name for this specialization")]
        public string displayName;

        [Tooltip("Accuracy bonus for relevant cases")]
        [Range(0f, 0.5f)]
        public float bonusAccuracy;

        [Tooltip("Reward bonus for relevant cases")]
        [Range(0f, 1f)]
        public float bonusReward;

        [Tooltip("Evidence quality bonus")]
        [Range(0f, 0.5f)]
        public float bonusEvidenceQuality;

        [Tooltip("Description of this specialization")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Color associated with this specialization")]
        public Color specializationColor;
    }

    /// <summary>
    /// Configuration for evidence types
    /// </summary>
    [Serializable]
    public struct EvidenceTypeConfig
    {
        [Tooltip("The evidence type this configures")]
        public EvidenceType type;

        [Tooltip("Base quality of this evidence type")]
        [Range(0.1f, 1f)]
        public float baseQuality;

        [Tooltip("Time required to analyze this evidence")]
        [Range(1f, 300f)]
        public float analysisTime;

        [Tooltip("Tools required for analysis")]
        public string[] requiredTools;

        [Tooltip("Description of this evidence type")]
        [TextArea(1, 3)]
        public string description;

        [Tooltip("Icon for this evidence type")]
        public Sprite evidenceIcon;
    }

    /// <summary>
    /// Configuration for forensic tools
    /// </summary>
    [Serializable]
    public struct ForensicToolConfig
    {
        [Tooltip("Name of the forensic tool")]
        public string toolName;

        [Tooltip("Accuracy bonus provided by this tool")]
        [Range(0f, 0.5f)]
        public float accuracyBonus;

        [Tooltip("Analysis speed bonus")]
        [Range(0f, 0.5f)]
        public float speedBonus;

        [Tooltip("Reputation required to unlock this tool")]
        [Range(0f, 1000f)]
        public float unlockReputation;

        [Tooltip("Description of this tool")]
        [TextArea(1, 3)]
        public string description;

        [Tooltip("Icon for this tool")]
        public Sprite toolIcon;

        [Tooltip("Cost to acquire this tool")]
        public int acquisitionCost;
    }

    #endregion
}