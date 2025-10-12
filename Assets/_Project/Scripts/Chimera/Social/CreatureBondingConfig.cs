using System;
using UnityEngine;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Configuration ScriptableObject for the Creature Bonding System.
    /// Allows designers to tune bonding mechanics, memory systems, and emotional responses.
    /// </summary>
    [CreateAssetMenu(fileName = "CreatureBondingConfig", menuName = "Chimera/Social/Bonding Config")]
    public class CreatureBondingConfig : ScriptableObject
    {
        [Header("Basic Bonding Settings")]
        [Tooltip("Maximum possible bond strength")]
        [Range(1f, 100f)]
        public float maxBondStrength = 100f;

        [Tooltip("Rate at which bond strength grows with positive interactions")]
        [Range(0.1f, 10f)]
        public float bondGrowthRate = 2f;

        [Tooltip("Rate at which bond strength decays without interaction")]
        [Range(0.01f, 1f)]
        public float bondDecayRate = 0.1f;

        [Tooltip("Time threshold for considering interaction recent (seconds)")]
        [Range(60f, 3600f)]
        public float interactionTimeThreshold = 300f;

        [Tooltip("Number of positive interactions needed for bond growth")]
        [Range(1, 20)]
        public int positiveInteractionThreshold = 5;

        [Header("Bonding Moments")]
        [Tooltip("Bond strength threshold for trust breakthrough")]
        [Range(10f, 80f)]
        public float trustBreakthroughThreshold = 30f;

        [Tooltip("Age threshold for adolescence moment (days)")]
        [Range(1f, 50f)]
        public float adolescenceAgeThreshold = 7f;

        [Tooltip("Age threshold for maturity moment (days)")]
        [Range(10f, 200f)]
        public float maturityAgeThreshold = 30f;

        [Header("Memory System")]
        [Tooltip("Rate at which memories fade over time")]
        [Range(0.001f, 0.1f)]
        public float memoryFadeRate = 0.01f;

        [Tooltip("Threshold for triggering memory recall")]
        [Range(0.1f, 1f)]
        public float memoryTriggerThreshold = 0.5f;

        [Tooltip("Bond strength boost when memory is triggered")]
        [Range(0.1f, 5f)]
        public float memoryBondBoost = 1f;

        [Tooltip("Threshold for memory consolidation")]
        [Range(0.3f, 1f)]
        public float memoryConsolidationThreshold = 0.7f;

        [Tooltip("Rate of memory consolidation")]
        [Range(0.01f, 0.5f)]
        public float memoryConsolidationRate = 0.05f;

        [Header("Memory Strengths")]
        [Tooltip("Memory strength for first meeting")]
        [Range(0.5f, 1f)]
        public float firstMeetingMemoryStrength = 0.8f;

        [Tooltip("Memory strength for trust breakthrough")]
        [Range(0.6f, 1f)]
        public float trustBreakthroughMemoryStrength = 0.9f;

        [Tooltip("Memory strength for life milestones")]
        [Range(0.4f, 0.9f)]
        public float milestoneMemoryStrength = 0.7f;

        [Tooltip("Memory strength for shared discoveries")]
        [Range(0.7f, 1f)]
        public float discoveryMemoryStrength = 0.85f;

        [Header("Legacy Connections")]
        [Tooltip("Genetic similarity threshold for legacy connections")]
        [Range(0.3f, 0.9f)]
        public float legacyConnectionThreshold = 0.6f;

        [Tooltip("Multiplier for legacy memory strength")]
        [Range(1f, 3f)]
        public float legacyMemoryMultiplier = 1.5f;

        [Tooltip("Loyalty bonus for creatures with legacy connections")]
        [Range(0.05f, 0.3f)]
        public float legacyLoyaltyBonus = 0.15f;

        [Tooltip("Interval between legacy connection checks (seconds)")]
        [Range(60f, 3600f)]
        public float legacyCheckInterval = 600f;

        [Tooltip("Threshold for deep memory formation")]
        [Range(0.6f, 1f)]
        public float deepMemoryThreshold = 0.8f;

        [Header("Generational Patterns")]
        [Tooltip("Minimum generations required to establish patterns")]
        [Range(2, 10)]
        public int minGenerationsForPattern = 3;

        [Tooltip("Initial strength for new patterns")]
        [Range(0.1f, 0.5f)]
        public float initialPatternStrength = 0.3f;

        [Tooltip("Increment for pattern strength")]
        [Range(0.01f, 0.2f)]
        public float patternStrengthIncrement = 0.05f;

        [Header("Emotional System")]
        [Tooltip("Duration of emotional states (seconds)")]
        [Range(30f, 600f)]
        public float emotionalStateDuration = 120f;

        [Tooltip("Impact of experiences on bond strength")]
        [Range(0.01f, 0.5f)]
        public float experienceImpact = 0.1f;

        [Header("Bonding Moment Configurations")]
        public BondingMomentConfig[] momentConfigs = new BondingMomentConfig[]
        {
            new BondingMomentConfig
            {
                type = BondingMomentType.FirstMeeting,
                baseIntensity = 0.8f,
                bondImpact = 5f,
                description = "The moment when {0} first trusted you"
            },
            new BondingMomentConfig
            {
                type = BondingMomentType.TrustBreakthrough,
                baseIntensity = 0.9f,
                bondImpact = 10f,
                description = "{0} opened their heart to you completely"
            },
            new BondingMomentConfig
            {
                type = BondingMomentType.Adolescence,
                baseIntensity = 0.6f,
                bondImpact = 3f,
                description = "{0} reached adolescence under your care"
            },
            new BondingMomentConfig
            {
                type = BondingMomentType.Maturity,
                baseIntensity = 0.7f,
                bondImpact = 4f,
                description = "{0} achieved full maturity with your guidance"
            },
            new BondingMomentConfig
            {
                type = BondingMomentType.SharedDiscovery,
                baseIntensity = 1f,
                bondImpact = 8f,
                description = "You and {0} made an incredible discovery together"
            }
        };

        [Header("Trait Rarity Settings")]
        public TraitRarityConfig[] traitRarities = new TraitRarityConfig[]
        {
            new TraitRarityConfig { traitName = "Common", rarityValue = 0.1f },
            new TraitRarityConfig { traitName = "Uncommon", rarityValue = 0.3f },
            new TraitRarityConfig { traitName = "Rare", rarityValue = 0.6f },
            new TraitRarityConfig { traitName = "Epic", rarityValue = 0.8f },
            new TraitRarityConfig { traitName = "Legendary", rarityValue = 1f }
        };

        [Header("Legacy Descriptions")]
        public LegacyDescriptionConfig[] legacyDescriptions = new LegacyDescriptionConfig[]
        {
            new LegacyDescriptionConfig
            {
                connectionType = LegacyConnectionType.DirectDescendant,
                description = "{0} carries the direct bloodline of {1} (similarity: {2:P0})"
            },
            new LegacyDescriptionConfig
            {
                connectionType = LegacyConnectionType.CloseRelative,
                description = "{0} shares close family ties with {1} (similarity: {2:P0})"
            },
            new LegacyDescriptionConfig
            {
                connectionType = LegacyConnectionType.DistantRelative,
                description = "{0} has distant family connections to {1} (similarity: {2:P0})"
            },
            new LegacyDescriptionConfig
            {
                connectionType = LegacyConnectionType.SharedAncestor,
                description = "{0} and {1} share a common ancestor (similarity: {2:P0})"
            }
        };

        [Header("Memory Trigger Conditions")]
        public MemoryTriggerConfig[] triggerConfigs = new MemoryTriggerConfig[]
        {
            new MemoryTriggerConfig
            {
                momentType = BondingMomentType.FirstMeeting,
                conditions = new MemoryTriggerCondition[]
                {
                    new MemoryTriggerCondition
                    {
                        type = TriggerConditionType.EmotionalState,
                        requiredState = BondingEmotionalState.Happy
                    }
                }
            },
            new MemoryTriggerConfig
            {
                momentType = BondingMomentType.TrustBreakthrough,
                conditions = new MemoryTriggerCondition[]
                {
                    new MemoryTriggerCondition
                    {
                        type = TriggerConditionType.BondStrengthThreshold,
                        threshold = 70f
                    }
                }
            }
        };

        /// <summary>
        /// Gets the intensity for a specific bonding moment type
        /// </summary>
        public float GetMomentIntensity(BondingMomentType type)
        {
            foreach (var config in momentConfigs)
            {
                if (config.type == type)
                    return config.baseIntensity;
            }
            return 0.5f; // Default intensity
        }

        /// <summary>
        /// Gets the bond impact for a specific bonding moment type
        /// </summary>
        public float GetMomentBondImpact(BondingMomentType type)
        {
            foreach (var config in momentConfigs)
            {
                if (config.type == type)
                    return config.bondImpact;
            }
            return 1f; // Default impact
        }

        /// <summary>
        /// Gets a formatted description for a bonding moment
        /// </summary>
        public string GetMomentDescription(BondingMomentType type, string[] traits)
        {
            foreach (var config in momentConfigs)
            {
                if (config.type == type)
                {
                    string creatureName = GenerateCreatureName(traits);
                    return string.Format(config.description, creatureName);
                }
            }
            return "A special moment occurred";
        }

        /// <summary>
        /// Gets the rarity value for a specific trait
        /// </summary>
        public float GetTraitRarity(string traitName)
        {
            foreach (var rarity in traitRarities)
            {
                if (rarity.traitName.Equals(traitName, StringComparison.OrdinalIgnoreCase))
                    return rarity.rarityValue;
            }
            return 0.1f; // Default to common
        }

        /// <summary>
        /// Gets a formatted description for a legacy connection
        /// </summary>
        public string GetLegacyDescription(LegacyConnectionType type, string ancestorName, float similarity)
        {
            foreach (var config in legacyDescriptions)
            {
                if (config.connectionType == type)
                {
                    return string.Format(config.description, "Current creature", ancestorName, similarity);
                }
            }
            return $"Connected to {ancestorName}";
        }

        /// <summary>
        /// Gets memory trigger conditions for a bonding moment type
        /// </summary>
        public MemoryTriggerCondition[] GetMomentTriggerConditions(BondingMomentType type)
        {
            foreach (var config in triggerConfigs)
            {
                if (config.momentType == type)
                    return config.conditions;
            }
            return new MemoryTriggerCondition[0];
        }

        /// <summary>
        /// Gets memory trigger conditions for legacy connections
        /// </summary>
        public MemoryTriggerCondition[] GetAncestralTriggerConditions(LegacyConnectionType type)
        {
            // Return conditions based on legacy type
            return new MemoryTriggerCondition[]
            {
                new MemoryTriggerCondition
                {
                    type = TriggerConditionType.EmotionalState,
                    requiredState = BondingEmotionalState.Nostalgic
                }
            };
        }

        /// <summary>
        /// Generates a creature name based on traits
        /// </summary>
        private string GenerateCreatureName(string[] traits)
        {
            if (traits.Length > 0)
                return $"your {traits[0]} companion";
            return "your companion";
        }

        void OnValidate()
        {
            // Ensure reasonable values
            maxBondStrength = Mathf.Clamp(maxBondStrength, 1f, 1000f);
            bondGrowthRate = Mathf.Clamp(bondGrowthRate, 0.1f, 50f);
            bondDecayRate = Mathf.Clamp(bondDecayRate, 0.01f, 10f);

            // Validate thresholds
            trustBreakthroughThreshold = Mathf.Clamp(trustBreakthroughThreshold, 1f, maxBondStrength * 0.8f);
            legacyConnectionThreshold = Mathf.Clamp(legacyConnectionThreshold, 0.1f, 1f);

            // Validate memory settings
            memoryFadeRate = Mathf.Clamp(memoryFadeRate, 0.001f, 1f);
            memoryTriggerThreshold = Mathf.Clamp(memoryTriggerThreshold, 0.1f, 1f);

            // Ensure moment configs are valid
            for (int i = 0; i < momentConfigs.Length; i++)
            {
                momentConfigs[i].baseIntensity = Mathf.Clamp01(momentConfigs[i].baseIntensity);
                momentConfigs[i].bondImpact = Mathf.Clamp(momentConfigs[i].bondImpact, 0f, maxBondStrength);
            }

            // Validate trait rarities
            for (int i = 0; i < traitRarities.Length; i++)
            {
                traitRarities[i].rarityValue = Mathf.Clamp01(traitRarities[i].rarityValue);
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for bonding moments
    /// </summary>
    [Serializable]
    public struct BondingMomentConfig
    {
        [Tooltip("Type of bonding moment")]
        public BondingMomentType type;

        [Tooltip("Base intensity of the moment")]
        [Range(0f, 1f)]
        public float baseIntensity;

        [Tooltip("Impact on bond strength")]
        [Range(0f, 50f)]
        public float bondImpact;

        [Tooltip("Description template for the moment")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Visual effect intensity")]
        [Range(0f, 1f)]
        public float visualIntensity;

        [Tooltip("Audio effect volume")]
        [Range(0f, 1f)]
        public float audioVolume;
    }

    /// <summary>
    /// Configuration for trait rarity values
    /// </summary>
    [Serializable]
    public struct TraitRarityConfig
    {
        [Tooltip("Name of the trait")]
        public string traitName;

        [Tooltip("Rarity value (0-1)")]
        [Range(0f, 1f)]
        public float rarityValue;

        [Tooltip("Color associated with this rarity")]
        public Color rarityColor;

        [Tooltip("Special effects for this rarity")]
        public bool hasSpecialEffects;
    }

    /// <summary>
    /// Configuration for legacy connection descriptions
    /// </summary>
    [Serializable]
    public struct LegacyDescriptionConfig
    {
        [Tooltip("Type of legacy connection")]
        public LegacyConnectionType connectionType;

        [Tooltip("Description template")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Icon for this connection type")]
        public Sprite connectionIcon;

        [Tooltip("Color for this connection type")]
        public Color connectionColor;
    }

    /// <summary>
    /// Configuration for memory trigger conditions
    /// </summary>
    [Serializable]
    public struct MemoryTriggerConfig
    {
        [Tooltip("Bonding moment type this applies to")]
        public BondingMomentType momentType;

        [Tooltip("Trigger conditions for memory recall")]
        public MemoryTriggerCondition[] conditions;

        [Tooltip("Probability of triggering (0-1)")]
        [Range(0f, 1f)]
        public float triggerProbability;

        [Tooltip("Cooldown between triggers (seconds)")]
        [Range(10f, 3600f)]
        public float triggerCooldown;
    }

    #endregion
}