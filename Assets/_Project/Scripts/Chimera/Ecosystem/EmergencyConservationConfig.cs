using System;
using UnityEngine;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Configuration ScriptableObject for the Emergency Conservation System.
    /// Allows designers to tune emergency detection, response mechanics, and success criteria.
    /// </summary>
    [CreateAssetMenu(fileName = "EmergencyConservationConfig", menuName = "Chimera/Ecosystem/Emergency Conservation Config")]
    public class EmergencyConservationConfig : ScriptableObject
    {
        [Header("Emergency Detection")]
        [Tooltip("Interval between emergency checks (seconds)")]
        [Range(60f, 3600f)]
        public float emergencyCheckInterval = 300f;

        [Tooltip("Critical population threshold for emergencies")]
        [Range(1, 100)]
        public int criticalPopulationThreshold = 20;

        [Tooltip("Recovery population threshold for success")]
        [Range(50, 500)]
        public int recoveryPopulationThreshold = 100;

        [Tooltip("Breeding failure threshold")]
        [Range(0.1f, 0.8f)]
        public float breedingFailureThreshold = 0.3f;

        [Tooltip("Breeding age threshold for crisis detection")]
        [Range(1f, 20f)]
        public float breedingAgeThreshold = 5f;

        [Tooltip("Juvenile survival threshold")]
        [Range(0.1f, 0.9f)]
        public float juvenileSurvivalThreshold = 0.4f;

        [Header("Ecosystem Thresholds")]
        [Tooltip("Ecosystem collapse health threshold")]
        [Range(0.1f, 0.5f)]
        public float ecosystemCollapseThreshold = 0.3f;

        [Tooltip("Food web stability threshold")]
        [Range(0.2f, 0.8f)]
        public float foodWebStabilityThreshold = 0.5f;

        [Tooltip("Habitat connectivity threshold")]
        [Range(0.2f, 0.9f)]
        public float habitatConnectivityThreshold = 0.6f;

        [Tooltip("Habitat loss rate threshold")]
        [Range(0.1f, 1f)]
        public float habitatLossRateThreshold = 0.5f;

        [Header("Genetic Diversity")]
        [Tooltip("Genetic diversity threshold for bottleneck detection")]
        [Range(0.1f, 0.7f)]
        public float geneticDiversityThreshold = 0.4f;

        [Header("Disease and Climate")]
        [Tooltip("Disease outbreak threshold")]
        [Range(0.2f, 0.8f)]
        public float diseaseOutbreakThreshold = 0.4f;

        [Tooltip("Climate stress threshold")]
        [Range(0.3f, 0.9f)]
        public float climateStressThreshold = 0.6f;

        [Header("Response Mechanics")]
        [Tooltip("Base effectiveness multiplier for responses")]
        [Range(0.1f, 5f)]
        public float responseEffectivenessMultiplier = 1f;

        [Tooltip("Time bonus for early response (multiplier)")]
        [Range(1f, 3f)]
        public float earlyResponseBonus = 1.5f;

        [Tooltip("Resource efficiency factor")]
        [Range(0.5f, 2f)]
        public float resourceEfficiencyFactor = 1f;

        [Header("Emergency Durations")]
        public EmergencyDurationConfig[] emergencyDurations = new EmergencyDurationConfig[]
        {
            new EmergencyDurationConfig
            {
                type = EmergencyType.PopulationCollapse,
                baseDuration = 7200f, // 2 hours
                urgencyMultiplier = 0.5f,
                description = "Time to prevent complete population collapse"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.BreedingFailure,
                baseDuration = 10800f, // 3 hours
                urgencyMultiplier = 0.7f,
                description = "Time to restore breeding success"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.JuvenileMortality,
                baseDuration = 5400f, // 1.5 hours
                urgencyMultiplier = 0.6f,
                description = "Time to protect juvenile population"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.EcosystemCollapse,
                baseDuration = 14400f, // 4 hours
                urgencyMultiplier = 0.4f,
                description = "Time to prevent ecosystem breakdown"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.HabitatDestruction,
                baseDuration = 9000f, // 2.5 hours
                urgencyMultiplier = 0.8f,
                description = "Time to stop habitat destruction"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.GeneticBottleneck,
                baseDuration = 18000f, // 5 hours
                urgencyMultiplier = 0.9f,
                description = "Time to address genetic crisis"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.DiseaseOutbreak,
                baseDuration = 3600f, // 1 hour
                urgencyMultiplier = 0.3f,
                description = "Time to contain disease spread"
            },
            new EmergencyDurationConfig
            {
                type = EmergencyType.ClimateChange,
                baseDuration = 21600f, // 6 hours
                urgencyMultiplier = 1f,
                description = "Time to implement climate adaptation"
            }
        };

        [Header("Required Actions")]
        public EmergencyActionConfig[] actionConfigurations = new EmergencyActionConfig[]
        {
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.PopulationSupport,
                baseDuration = 1800f,
                resourceCost = 100f,
                effectiveness = 0.8f,
                description = "Direct population support and care",
                applicableTypes = new[] { EmergencyType.PopulationCollapse, EmergencyType.JuvenileMortality }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.BreedingProgram,
                baseDuration = 3600f,
                resourceCost = 200f,
                effectiveness = 0.9f,
                description = "Implement controlled breeding programs",
                applicableTypes = new[] { EmergencyType.BreedingFailure, EmergencyType.GeneticBottleneck }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.HabitatProtection,
                baseDuration = 2700f,
                resourceCost = 150f,
                effectiveness = 0.7f,
                description = "Protect and secure critical habitat",
                applicableTypes = new[] { EmergencyType.HabitatDestruction, EmergencyType.EcosystemCollapse }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.HabitatRestoration,
                baseDuration = 5400f,
                resourceCost = 300f,
                effectiveness = 0.6f,
                description = "Restore degraded habitat areas",
                applicableTypes = new[] { EmergencyType.HabitatDestruction, EmergencyType.EcosystemCollapse }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.DiseaseControl,
                baseDuration = 1200f,
                resourceCost = 80f,
                effectiveness = 0.9f,
                description = "Implement disease control measures",
                applicableTypes = new[] { EmergencyType.DiseaseOutbreak }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.GeneticManagement,
                baseDuration = 4500f,
                resourceCost = 250f,
                effectiveness = 0.8f,
                description = "Manage genetic diversity and breeding",
                applicableTypes = new[] { EmergencyType.GeneticBottleneck, EmergencyType.BreedingFailure }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.ClimateAdaptation,
                baseDuration = 7200f,
                resourceCost = 400f,
                effectiveness = 0.5f,
                description = "Implement climate adaptation strategies",
                applicableTypes = new[] { EmergencyType.ClimateChange }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.ThreatReduction,
                baseDuration = 2400f,
                resourceCost = 120f,
                effectiveness = 0.7f,
                description = "Reduce immediate threats to species",
                applicableTypes = new[] { EmergencyType.PopulationCollapse, EmergencyType.JuvenileMortality }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.Monitoring,
                baseDuration = 900f,
                resourceCost = 50f,
                effectiveness = 0.3f,
                description = "Establish monitoring and tracking systems",
                applicableTypes = new[] { EmergencyType.PopulationCollapse, EmergencyType.DiseaseOutbreak, EmergencyType.EcosystemCollapse }
            },
            new EmergencyActionConfig
            {
                actionType = EmergencyActionType.Research,
                baseDuration = 3600f,
                resourceCost = 180f,
                effectiveness = 0.4f,
                description = "Conduct emergency research and analysis",
                applicableTypes = new[] { EmergencyType.DiseaseOutbreak, EmergencyType.ClimateChange, EmergencyType.GeneticBottleneck }
            }
        };

        [Header("Success Criteria")]
        [Tooltip("Population increase required for success (percentage)")]
        [Range(0.1f, 2f)]
        public float populationIncreaseRequired = 0.5f;

        [Tooltip("Reproductive success target for breeding emergencies")]
        [Range(0.5f, 1f)]
        public float reproductiveSuccessTarget = 0.7f;

        [Tooltip("Juvenile survival target")]
        [Range(0.6f, 0.95f)]
        public float juvenileSurvivalTarget = 0.8f;

        [Tooltip("Ecosystem health recovery target")]
        [Range(0.6f, 1f)]
        public float ecosystemHealthTarget = 0.75f;

        [Tooltip("Genetic diversity recovery target")]
        [Range(0.5f, 1f)]
        public float geneticDiversityTarget = 0.7f;

        [Header("Escalation Settings")]
        [Tooltip("Time percentage before emergency escalates")]
        [Range(0.1f, 0.8f)]
        public float escalationTimeThreshold = 0.3f;

        [Tooltip("Severity threshold for automatic escalation")]
        public EmergencySeverity escalationSeverityThreshold = EmergencySeverity.Severe;

        [Tooltip("Escalation severity increase")]
        public EmergencySeverity escalationSeverityIncrease = EmergencySeverity.Critical;

        [Header("Player Rewards")]
        [Tooltip("Base experience points for emergency response")]
        [Range(10, 1000)]
        public int baseExperienceReward = 100;

        [Tooltip("Experience multiplier for emergency type")]
        public ExperienceMultiplierConfig[] experienceMultipliers = new ExperienceMultiplierConfig[]
        {
            new ExperienceMultiplierConfig { emergencyType = EmergencyType.PopulationCollapse, multiplier = 2f },
            new ExperienceMultiplierConfig { emergencyType = EmergencyType.EcosystemCollapse, multiplier = 3f },
            new ExperienceMultiplierConfig { emergencyType = EmergencyType.BreedingFailure, multiplier = 1.5f },
            new ExperienceMultiplierConfig { emergencyType = EmergencyType.DiseaseOutbreak, multiplier = 1.8f },
            new ExperienceMultiplierConfig { emergencyType = EmergencyType.ClimateChange, multiplier = 2.5f }
        };

        [Header("Notification Settings")]
        [Tooltip("Enable emergency notifications")]
        public bool enableEmergencyNotifications = true;

        [Tooltip("Notification range for emergency alerts")]
        [Range(1f, 100f)]
        public float notificationRange = 50f;

        [Tooltip("Enable escalation warnings")]
        public bool enableEscalationWarnings = true;

        [Tooltip("Warning time before escalation (seconds)")]
        [Range(60f, 1800f)]
        public float escalationWarningTime = 300f;

        [Header("Performance Settings")]
        [Tooltip("Maximum concurrent emergencies")]
        [Range(1, 50)]
        public int maxConcurrentEmergencies = 10;

        [Tooltip("Emergency processing batch size")]
        [Range(1, 20)]
        public int emergencyProcessingBatchSize = 5;

        [Tooltip("Enable emergency history tracking")]
        public bool enableEmergencyHistory = true;

        [Tooltip("Maximum emergency history entries")]
        [Range(10, 1000)]
        public int maxEmergencyHistoryEntries = 100;

        /// <summary>
        /// Gets the duration for a specific emergency type
        /// </summary>
        public float GetEmergencyDuration(EmergencyType type)
        {
            foreach (var config in emergencyDurations)
            {
                if (config.type == type)
                    return config.baseDuration;
            }
            return 3600f; // Default 1 hour
        }

        /// <summary>
        /// Gets the urgency multiplier for an emergency type
        /// </summary>
        public float GetUrgencyMultiplier(EmergencyType type)
        {
            foreach (var config in emergencyDurations)
            {
                if (config.type == type)
                    return config.urgencyMultiplier;
            }
            return 1f;
        }

        /// <summary>
        /// Gets required actions for an emergency type
        /// </summary>
        public EmergencyAction[] GetRequiredActions(EmergencyType type)
        {
            var actions = new List<EmergencyAction>();

            foreach (var config in actionConfigurations)
            {
                if (System.Array.Exists(config.applicableTypes, t => t == type))
                {
                    actions.Add(new EmergencyAction
                    {
                        type = config.actionType,
                        name = config.actionType.ToString(),
                        description = config.description,
                        resourceRequirement = config.resourceCost,
                        timeRequirement = config.baseDuration,
                        effectiveness = config.effectiveness,
                        prerequisites = new string[0] // Could be expanded
                    });
                }
            }

            return actions.ToArray();
        }

        /// <summary>
        /// Gets the duration for a specific action type
        /// </summary>
        public float GetActionDuration(EmergencyActionType actionType)
        {
            foreach (var config in actionConfigurations)
            {
                if (config.actionType == actionType)
                    return config.baseDuration;
            }
            return 1800f; // Default 30 minutes
        }

        /// <summary>
        /// Gets the resource cost for an action type
        /// </summary>
        public float GetActionResourceCost(EmergencyActionType actionType)
        {
            foreach (var config in actionConfigurations)
            {
                if (config.actionType == actionType)
                    return config.resourceCost;
            }
            return 100f; // Default cost
        }

        /// <summary>
        /// Gets the effectiveness of an action type
        /// </summary>
        public float GetActionEffectiveness(EmergencyActionType actionType)
        {
            foreach (var config in actionConfigurations)
            {
                if (config.actionType == actionType)
                    return config.effectiveness;
            }
            return 0.5f; // Default effectiveness
        }

        /// <summary>
        /// Gets experience multiplier for emergency type
        /// </summary>
        public float GetExperienceMultiplier(EmergencyType type)
        {
            foreach (var config in experienceMultipliers)
            {
                if (config.emergencyType == type)
                    return config.multiplier;
            }
            return 1f; // Default multiplier
        }

        /// <summary>
        /// Calculates experience reward for emergency response
        /// </summary>
        public int CalculateExperienceReward(EmergencyType type, EmergencySeverity severity, float effectiveness, bool isSuccessful)
        {
            float baseReward = baseExperienceReward;

            // Apply type multiplier
            baseReward *= GetExperienceMultiplier(type);

            // Apply severity multiplier
            switch (severity)
            {
                case EmergencySeverity.Minor:
                    baseReward *= 0.8f;
                    break;
                case EmergencySeverity.Moderate:
                    baseReward *= 1f;
                    break;
                case EmergencySeverity.Severe:
                    baseReward *= 1.5f;
                    break;
                case EmergencySeverity.Critical:
                    baseReward *= 2f;
                    break;
            }

            // Apply effectiveness multiplier
            baseReward *= effectiveness;

            // Apply success bonus
            if (isSuccessful)
                baseReward *= 1.5f;

            return Mathf.RoundToInt(baseReward);
        }

        /// <summary>
        /// Determines if an emergency should escalate
        /// </summary>
        public bool ShouldEscalate(ConservationEmergency emergency)
        {
            float timeRatio = 1f - (emergency.timeRemaining / emergency.originalDuration);

            return timeRatio >= escalationTimeThreshold &&
                   emergency.severity >= escalationSeverityThreshold &&
                   !emergency.hasEscalated;
        }

        /// <summary>
        /// Calculates the effectiveness bonus for early response
        /// </summary>
        public float CalculateEarlyResponseBonus(float responseTime, float totalTime)
        {
            float timeRatio = responseTime / totalTime;

            if (timeRatio <= 0.2f) // Responded within first 20% of time
                return earlyResponseBonus;
            if (timeRatio <= 0.5f) // Responded within first 50% of time
                return 1f + (earlyResponseBonus - 1f) * 0.5f;

            return 1f; // No bonus for late response
        }

        /// <summary>
        /// Validates if action is applicable to emergency type
        /// </summary>
        public bool IsActionApplicable(EmergencyActionType action, EmergencyType emergencyType)
        {
            foreach (var config in actionConfigurations)
            {
                if (config.actionType == action)
                {
                    return System.Array.Exists(config.applicableTypes, t => t == emergencyType);
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the most effective actions for an emergency type
        /// </summary>
        public EmergencyActionType[] GetMostEffectiveActions(EmergencyType type, int maxActions = 3)
        {
            var applicableActions = new List<(EmergencyActionType, float)>();

            foreach (var config in actionConfigurations)
            {
                if (System.Array.Exists(config.applicableTypes, t => t == type))
                {
                    applicableActions.Add((config.actionType, config.effectiveness));
                }
            }

            return applicableActions
                .OrderByDescending(a => a.Item2)
                .Take(maxActions)
                .Select(a => a.Item1)
                .ToArray();
        }

        void OnValidate()
        {
            // Validate emergency check interval
            emergencyCheckInterval = Mathf.Clamp(emergencyCheckInterval, 60f, 7200f);

            // Validate population thresholds
            criticalPopulationThreshold = Mathf.Clamp(criticalPopulationThreshold, 1, 1000);
            recoveryPopulationThreshold = Mathf.Max(recoveryPopulationThreshold, criticalPopulationThreshold * 2);

            // Validate rate thresholds
            breedingFailureThreshold = Mathf.Clamp01(breedingFailureThreshold);
            juvenileSurvivalThreshold = Mathf.Clamp01(juvenileSurvivalThreshold);
            diseaseOutbreakThreshold = Mathf.Clamp01(diseaseOutbreakThreshold);

            // Validate ecosystem thresholds
            ecosystemCollapseThreshold = Mathf.Clamp(ecosystemCollapseThreshold, 0.1f, 0.8f);
            foodWebStabilityThreshold = Mathf.Clamp01(foodWebStabilityThreshold);
            habitatConnectivityThreshold = Mathf.Clamp01(habitatConnectivityThreshold);
            habitatLossRateThreshold = Mathf.Clamp01(habitatLossRateThreshold);

            // Validate genetic diversity threshold
            geneticDiversityThreshold = Mathf.Clamp01(geneticDiversityThreshold);

            // Validate climate threshold
            climateStressThreshold = Mathf.Clamp01(climateStressThreshold);

            // Validate response mechanics
            responseEffectivenessMultiplier = Mathf.Clamp(responseEffectivenessMultiplier, 0.1f, 10f);
            earlyResponseBonus = Mathf.Clamp(earlyResponseBonus, 1f, 5f);
            resourceEfficiencyFactor = Mathf.Clamp(resourceEfficiencyFactor, 0.1f, 5f);

            // Validate success criteria
            populationIncreaseRequired = Mathf.Clamp(populationIncreaseRequired, 0.1f, 5f);
            reproductiveSuccessTarget = Mathf.Clamp01(reproductiveSuccessTarget);
            juvenileSurvivalTarget = Mathf.Clamp01(juvenileSurvivalTarget);
            ecosystemHealthTarget = Mathf.Clamp01(ecosystemHealthTarget);
            geneticDiversityTarget = Mathf.Clamp01(geneticDiversityTarget);

            // Validate escalation settings
            escalationTimeThreshold = Mathf.Clamp01(escalationTimeThreshold);

            // Validate rewards
            baseExperienceReward = Mathf.Clamp(baseExperienceReward, 1, 10000);

            // Validate notification settings
            notificationRange = Mathf.Clamp(notificationRange, 1f, 1000f);
            escalationWarningTime = Mathf.Clamp(escalationWarningTime, 30f, 3600f);

            // Validate performance settings
            maxConcurrentEmergencies = Mathf.Clamp(maxConcurrentEmergencies, 1, 100);
            emergencyProcessingBatchSize = Mathf.Clamp(emergencyProcessingBatchSize, 1, 50);
            maxEmergencyHistoryEntries = Mathf.Clamp(maxEmergencyHistoryEntries, 10, 10000);

            // Validate emergency duration configurations
            for (int i = 0; i < emergencyDurations.Length; i++)
            {
                emergencyDurations[i].baseDuration = Mathf.Clamp(emergencyDurations[i].baseDuration, 300f, 86400f);
                emergencyDurations[i].urgencyMultiplier = Mathf.Clamp(emergencyDurations[i].urgencyMultiplier, 0.1f, 2f);

                if (string.IsNullOrEmpty(emergencyDurations[i].description))
                    emergencyDurations[i].description = $"Duration for {emergencyDurations[i].type}";
            }

            // Validate action configurations
            for (int i = 0; i < actionConfigurations.Length; i++)
            {
                actionConfigurations[i].baseDuration = Mathf.Clamp(actionConfigurations[i].baseDuration, 60f, 14400f);
                actionConfigurations[i].resourceCost = Mathf.Clamp(actionConfigurations[i].resourceCost, 1f, 10000f);
                actionConfigurations[i].effectiveness = Mathf.Clamp01(actionConfigurations[i].effectiveness);

                if (string.IsNullOrEmpty(actionConfigurations[i].description))
                    actionConfigurations[i].description = $"Action: {actionConfigurations[i].actionType}";

                if (actionConfigurations[i].applicableTypes == null || actionConfigurations[i].applicableTypes.Length == 0)
                    actionConfigurations[i].applicableTypes = new[] { EmergencyType.PopulationCollapse };
            }

            // Validate experience multipliers
            for (int i = 0; i < experienceMultipliers.Length; i++)
            {
                experienceMultipliers[i].multiplier = Mathf.Clamp(experienceMultipliers[i].multiplier, 0.1f, 10f);
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for emergency durations
    /// </summary>
    [Serializable]
    public struct EmergencyDurationConfig
    {
        [Tooltip("Type of emergency")]
        public EmergencyType type;

        [Tooltip("Base duration in seconds")]
        [Range(300f, 86400f)]
        public float baseDuration;

        [Tooltip("Multiplier based on urgency level")]
        [Range(0.1f, 2f)]
        public float urgencyMultiplier;

        [Tooltip("Description of the time constraint")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Whether this emergency can be extended")]
        public bool canBeExtended;

        [Tooltip("Maximum extension time (seconds)")]
        [Range(0f, 14400f)]
        public float maxExtensionTime;
    }

    /// <summary>
    /// Configuration for emergency actions
    /// </summary>
    [Serializable]
    public struct EmergencyActionConfig
    {
        [Tooltip("Type of action")]
        public EmergencyActionType actionType;

        [Tooltip("Base duration to complete action")]
        [Range(60f, 14400f)]
        public float baseDuration;

        [Tooltip("Resource cost to perform action")]
        [Range(1f, 10000f)]
        public float resourceCost;

        [Tooltip("Effectiveness of this action (0-1)")]
        [Range(0f, 1f)]
        public float effectiveness;

        [Tooltip("Description of the action")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Emergency types this action applies to")]
        public EmergencyType[] applicableTypes;

        [Tooltip("Required player level for this action")]
        [Range(1, 100)]
        public int requiredLevel;

        [Tooltip("Required technologies or unlocks")]
        public string[] prerequisites;

        [Tooltip("Success rate of this action")]
        [Range(0f, 1f)]
        public float successRate;
    }

    /// <summary>
    /// Configuration for experience multipliers
    /// </summary>
    [Serializable]
    public struct ExperienceMultiplierConfig
    {
        [Tooltip("Emergency type")]
        public EmergencyType emergencyType;

        [Tooltip("Experience multiplier")]
        [Range(0.1f, 10f)]
        public float multiplier;

        [Tooltip("Additional rewards for this emergency type")]
        public string[] bonusRewards;

        [Tooltip("Special recognition for handling this emergency")]
        public bool grantsSpecialRecognition;
    }

    #endregion
}