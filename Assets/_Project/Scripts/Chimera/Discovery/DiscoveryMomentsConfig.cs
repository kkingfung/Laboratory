using System;
using UnityEngine;

namespace Laboratory.Chimera.Discovery
{
    /// <summary>
    /// Configuration ScriptableObject for the Discovery Moments System.
    /// Allows designers to fine-tune discovery celebrations, rarity thresholds, and reward systems.
    /// </summary>
    [CreateAssetMenu(fileName = "DiscoveryMomentsConfig", menuName = "Chimera/Discovery/Discovery Moments Config")]
    public class DiscoveryMomentsConfig : ScriptableObject
    {
        [Header("Discovery Detection")]
        [Tooltip("Minimum rarity score required for a Minor discovery")]
        [Range(0.5f, 3f)]
        public float minorDiscoveryThreshold = 1f;

        [Tooltip("Minimum rarity score required for a Notable discovery")]
        [Range(1f, 5f)]
        public float notableDiscoveryThreshold = 2f;

        [Tooltip("Minimum rarity score required for a Major discovery")]
        [Range(2f, 8f)]
        public float majorDiscoveryThreshold = 4f;

        [Tooltip("Minimum rarity score required for an Epic discovery")]
        [Range(4f, 12f)]
        public float epicDiscoveryThreshold = 6f;

        [Tooltip("Minimum rarity score required for a Legendary discovery")]
        [Range(6f, 15f)]
        public float legendaryDiscoveryThreshold = 8f;

        [Tooltip("Minimum rarity score required for a World-Changing discovery")]
        [Range(8f, 20f)]
        public float worldChangingDiscoveryThreshold = 10f;

        [Header("Celebration Settings")]
        [Tooltip("Base duration for discovery celebrations")]
        [Range(1f, 10f)]
        public float baseCelebrationDuration = 3f;

        [Tooltip("Duration multiplier for higher significance discoveries")]
        [Range(1f, 3f)]
        public float significanceDurationMultiplier = 2f;

        [Tooltip("Enable screen-wide particle effects for celebrations")]
        public bool enableParticleEffects = true;

        [Tooltip("Enable screen flash effects for major discoveries")]
        public bool enableScreenFlash = true;

        [Tooltip("Enable camera shake for legendary discoveries")]
        public bool enableCameraShake = true;

        [Tooltip("Maximum celebration intensity (prevents overwhelming effects)")]
        [Range(0.1f, 2f)]
        public float maxCelebrationIntensity = 1f;

        [Header("Discovery Colors")]
        public Color minorDiscoveryColor = Color.green;
        public Color notableDiscoveryColor = Color.blue;
        public Color majorDiscoveryColor = Color.purple;
        public Color epicDiscoveryColor = Color.yellow;
        public Color legendaryDiscoveryColor = Color.red;
        public Color worldChangingDiscoveryColor = Color.white;

        [Header("Naming Rights")]
        [Tooltip("Enable players to name their world-first discoveries")]
        public bool enableNamingRights = true;

        [Tooltip("Enable custom naming for legendary+ discoveries")]
        public bool enableLegendaryNaming = true;

        [Tooltip("Maximum length for custom discovery names")]
        [Range(10, 50)]
        public int maxCustomNameLength = 25;

        [Tooltip("Enable profanity filtering for custom names")]
        public bool enableProfanityFilter = true;

        [Header("Community Features")]
        [Tooltip("Enable server-wide notifications for major discoveries")]
        public bool enableCommunityNotifications = true;

        [Tooltip("Minimum significance level for community notifications")]
        public DiscoverySignificance communityNotificationThreshold = DiscoverySignificance.Major;

        [Tooltip("Maximum community notifications per hour")]
        [Range(1, 20)]
        public int maxCommunityNotificationsPerHour = 5;

        [Tooltip("Enable discovery leaderboards")]
        public bool enableDiscoveryLeaderboards = true;

        [Header("Discovery Journal")]
        [Tooltip("Enable personal discovery journal")]
        public bool enablePersonalJournal = true;

        [Tooltip("Enable community discovery database")]
        public bool enableCommunityDatabase = true;

        [Tooltip("Maximum personal journal entries")]
        [Range(50, 500)]
        public int maxPersonalJournalEntries = 200;

        [Tooltip("Enable automatic screenshots for discoveries")]
        public bool enableAutoScreenshots = true;

        [Header("Rarity Calculations")]
        [Tooltip("Weight multiplier for mutations in rarity calculation")]
        [Range(0.1f, 3f)]
        public float mutationRarityMultiplier = 1.5f;

        [Tooltip("Weight multiplier for enhanced gene expression")]
        [Range(0.1f, 2f)]
        public float enhancedExpressionMultiplier = 1.3f;

        [Tooltip("Weight multiplier for high-generation creatures")]
        [Range(0.05f, 0.5f)]
        public float generationRarityMultiplier = 0.1f;

        [Tooltip("Weight multiplier for genetic purity")]
        [Range(0.1f, 1f)]
        public float purityRarityMultiplier = 0.5f;

        [Header("Trait Combination Analysis")]
        [Tooltip("Enable detection of synergistic trait combinations")]
        public bool enableSynergyDetection = true;

        [Tooltip("Minimum trait value for combination analysis")]
        [Range(0.5f, 0.9f)]
        public float minTraitValueForCombination = 0.7f;

        [Tooltip("Bonus multiplier for synergistic combinations")]
        [Range(1f, 3f)]
        public float synergyBonusMultiplier = 1.5f;

        [Header("Special Discovery Types")]
        public SpecialDiscoveryConfig[] specialDiscoveries = new SpecialDiscoveryConfig[]
        {
            new SpecialDiscoveryConfig
            {
                name = "Photosynthetic Carnivore",
                requiredTraits = new[] { "Photosynthesis", "Carnivorous" },
                rarityMultiplier = 3f,
                celebrationColor = Color.green,
                description = "A creature that can both photosynthesize and hunt prey - defying natural law!"
            },
            new SpecialDiscoveryConfig
            {
                name = "Temporal Phase Walker",
                requiredTraits = new[] { "Time Manipulation", "Phase Shift" },
                rarityMultiplier = 5f,
                celebrationColor = Color.cyan,
                description = "A being that exists partially outside normal time and space"
            },
            new SpecialDiscoveryConfig
            {
                name = "Elemental Synthesis",
                requiredTraits = new[] { "Fire Resistance", "Ice Resistance", "Lightning Resistance" },
                rarityMultiplier = 4f,
                celebrationColor = Color.magenta,
                description = "Perfect resistance to all elemental forces - a true force of nature"
            }
        };

        [Header("Audio Settings")]
        [Tooltip("Enable celebration audio effects")]
        public bool enableCelebrationAudio = true;

        [Tooltip("Audio volume multiplier for discoveries")]
        [Range(0f, 2f)]
        public float discoveryAudioVolume = 1f;

        public AudioClip[] celebrationSounds = new AudioClip[0];

        [Header("Performance")]
        [Tooltip("Maximum simultaneous celebration effects")]
        [Range(1, 10)]
        public int maxSimultaneousCelebrations = 3;

        [Tooltip("Enable celebration effect pooling for performance")]
        public bool enableEffectPooling = true;

        [Tooltip("Update frequency for celebration effects (Hz)")]
        [Range(10f, 60f)]
        public float celebrationUpdateFrequency = 30f;

        [Header("Player Progression")]
        [Tooltip("Enable discovery-based player progression")]
        public bool enableDiscoveryProgression = true;

        [Tooltip("Points awarded for different discovery types")]
        public DiscoveryPointsConfig discoveryPoints = new DiscoveryPointsConfig
        {
            minorDiscoveryPoints = 10,
            notableDiscoveryPoints = 50,
            majorDiscoveryPoints = 100,
            epicDiscoveryPoints = 250,
            legendaryDiscoveryPoints = 500,
            worldChangingDiscoveryPoints = 1000,
            worldFirstBonus = 2f,
            rarityScoreMultiplier = 10f
        };

        /// <summary>
        /// Gets the celebration color for a discovery significance level
        /// </summary>
        public Color GetDiscoveryColor(DiscoverySignificance significance)
        {
            return significance switch
            {
                DiscoverySignificance.Minor => minorDiscoveryColor,
                DiscoverySignificance.Notable => notableDiscoveryColor,
                DiscoverySignificance.Major => majorDiscoveryColor,
                DiscoverySignificance.Epic => epicDiscoveryColor,
                DiscoverySignificance.Legendary => legendaryDiscoveryColor,
                DiscoverySignificance.WorldChanging => worldChangingDiscoveryColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Gets the celebration duration for a discovery significance level
        /// </summary>
        public float GetCelebrationDuration(DiscoverySignificance significance)
        {
            float multiplier = significance switch
            {
                DiscoverySignificance.Minor => 0.5f,
                DiscoverySignificance.Notable => 0.7f,
                DiscoverySignificance.Major => 1f,
                DiscoverySignificance.Epic => 1.3f,
                DiscoverySignificance.Legendary => 1.6f,
                DiscoverySignificance.WorldChanging => 2f,
                _ => 1f
            };

            return baseCelebrationDuration * multiplier * significanceDurationMultiplier;
        }

        /// <summary>
        /// Determines discovery significance based on rarity score
        /// </summary>
        public DiscoverySignificance GetDiscoverySignificance(float rarityScore)
        {
            if (rarityScore >= worldChangingDiscoveryThreshold)
                return DiscoverySignificance.WorldChanging;
            if (rarityScore >= legendaryDiscoveryThreshold)
                return DiscoverySignificance.Legendary;
            if (rarityScore >= epicDiscoveryThreshold)
                return DiscoverySignificance.Epic;
            if (rarityScore >= majorDiscoveryThreshold)
                return DiscoverySignificance.Major;
            if (rarityScore >= notableDiscoveryThreshold)
                return DiscoverySignificance.Notable;
            if (rarityScore >= minorDiscoveryThreshold)
                return DiscoverySignificance.Minor;

            return DiscoverySignificance.None;
        }

        /// <summary>
        /// Checks if a trait combination matches any special discovery
        /// </summary>
        public SpecialDiscoveryConfig? GetSpecialDiscovery(string[] presentTraits)
        {
            foreach (var special in specialDiscoveries)
            {
                bool hasAllRequiredTraits = true;
                foreach (var requiredTrait in special.requiredTraits)
                {
                    bool hasThisTrait = false;
                    foreach (var presentTrait in presentTraits)
                    {
                        if (presentTrait.Contains(requiredTrait))
                        {
                            hasThisTrait = true;
                            break;
                        }
                    }
                    if (!hasThisTrait)
                    {
                        hasAllRequiredTraits = false;
                        break;
                    }
                }

                if (hasAllRequiredTraits)
                    return special;
            }

            return null;
        }

        /// <summary>
        /// Calculates discovery points for a given discovery
        /// </summary>
        public float CalculateDiscoveryPoints(DiscoverySignificance significance, float rarityScore, bool isWorldFirst)
        {
            float basePoints = significance switch
            {
                DiscoverySignificance.Minor => discoveryPoints.minorDiscoveryPoints,
                DiscoverySignificance.Notable => discoveryPoints.notableDiscoveryPoints,
                DiscoverySignificance.Major => discoveryPoints.majorDiscoveryPoints,
                DiscoverySignificance.Epic => discoveryPoints.epicDiscoveryPoints,
                DiscoverySignificance.Legendary => discoveryPoints.legendaryDiscoveryPoints,
                DiscoverySignificance.WorldChanging => discoveryPoints.worldChangingDiscoveryPoints,
                _ => 0f
            };

            float totalPoints = basePoints + (rarityScore * discoveryPoints.rarityScoreMultiplier);

            if (isWorldFirst)
                totalPoints *= discoveryPoints.worldFirstBonus;

            return totalPoints;
        }

        void OnValidate()
        {
            // Ensure thresholds are in ascending order
            notableDiscoveryThreshold = Mathf.Max(notableDiscoveryThreshold, minorDiscoveryThreshold);
            majorDiscoveryThreshold = Mathf.Max(majorDiscoveryThreshold, notableDiscoveryThreshold);
            epicDiscoveryThreshold = Mathf.Max(epicDiscoveryThreshold, majorDiscoveryThreshold);
            legendaryDiscoveryThreshold = Mathf.Max(legendaryDiscoveryThreshold, epicDiscoveryThreshold);
            worldChangingDiscoveryThreshold = Mathf.Max(worldChangingDiscoveryThreshold, legendaryDiscoveryThreshold);

            // Clamp other values
            baseCelebrationDuration = Mathf.Clamp(baseCelebrationDuration, 1f, 10f);
            maxCelebrationIntensity = Mathf.Clamp(maxCelebrationIntensity, 0.1f, 2f);
            maxCustomNameLength = Mathf.Clamp(maxCustomNameLength, 10, 50);
            maxCommunityNotificationsPerHour = Mathf.Clamp(maxCommunityNotificationsPerHour, 1, 20);

            // Ensure special discoveries have required data
            for (int i = 0; i < specialDiscoveries.Length; i++)
            {
                if (specialDiscoveries[i].requiredTraits == null || specialDiscoveries[i].requiredTraits.Length == 0)
                {
                    specialDiscoveries[i].requiredTraits = new[] { "Unknown" };
                }
                if (string.IsNullOrEmpty(specialDiscoveries[i].name))
                {
                    specialDiscoveries[i].name = $"Special Discovery {i + 1}";
                }
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for special discovery types that have specific trait requirements
    /// </summary>
    [Serializable]
    public struct SpecialDiscoveryConfig
    {
        [Tooltip("Display name for this special discovery")]
        public string name;

        [Tooltip("Traits required to trigger this special discovery")]
        public string[] requiredTraits;

        [Tooltip("Rarity multiplier applied when this discovery is found")]
        [Range(1f, 10f)]
        public float rarityMultiplier;

        [Tooltip("Special color for this discovery's celebration")]
        public Color celebrationColor;

        [Tooltip("Description of this special discovery")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Enable special effects for this discovery")]
        public bool enableSpecialEffects;

        [Tooltip("Custom celebration duration override")]
        public float customCelebrationDuration;
    }

    /// <summary>
    /// Configuration for discovery point rewards
    /// </summary>
    [Serializable]
    public struct DiscoveryPointsConfig
    {
        [Header("Base Points by Significance")]
        public float minorDiscoveryPoints;
        public float notableDiscoveryPoints;
        public float majorDiscoveryPoints;
        public float epicDiscoveryPoints;
        public float legendaryDiscoveryPoints;
        public float worldChangingDiscoveryPoints;

        [Header("Multipliers")]
        [Tooltip("Multiplier for world-first discoveries")]
        [Range(1f, 5f)]
        public float worldFirstBonus;

        [Tooltip("Points per unit of rarity score")]
        [Range(1f, 50f)]
        public float rarityScoreMultiplier;
    }

    #endregion
}