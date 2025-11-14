using UnityEngine;

namespace Laboratory.Chimera.Activities.Combat
{
    /// <summary>
    /// Configuration for Combat Arena activity
    /// Defines fighting styles, performance scaling, and rewards
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Chimera/Activities/Combat Config")]
    public class CombatConfig : ActivityConfig
    {
        [Header("Combat-Specific Settings")]
        [Tooltip("Combat variance to simulate tactical choices (0.0 to 0.3)")]
        [Range(0f, 0.3f)]
        public float combatVariance = 0.15f;

        [Tooltip("Combat styles available")]
        public CombatStyle[] availableStyles = new CombatStyle[]
        {
            CombatStyle.Aggressive,
            CombatStyle.Defensive,
            CombatStyle.Tactical,
            CombatStyle.Balanced
        };

        [Header("Combat Style Modifiers")]
        [Tooltip("Aggressive style (high strength focus)")]
        public StyleWeights aggressiveWeights = new StyleWeights
        {
            strength = 0.7f,
            vitality = 0.15f,
            intelligence = 0.15f
        };

        [Tooltip("Defensive style (high vitality focus)")]
        public StyleWeights defensiveWeights = new StyleWeights
        {
            strength = 0.2f,
            vitality = 0.6f,
            intelligence = 0.2f
        };

        [Tooltip("Tactical style (high intelligence focus)")]
        public StyleWeights tacticalWeights = new StyleWeights
        {
            strength = 0.2f,
            vitality = 0.2f,
            intelligence = 0.6f
        };

        [Tooltip("Balanced style (equal weights)")]
        public StyleWeights balancedWeights = new StyleWeights
        {
            strength = 0.33f,
            vitality = 0.34f,
            intelligence = 0.33f
        };

        [Header("Tournament Settings")]
        [Tooltip("Enable tournament bracket mode")]
        public bool enableTournaments = true;

        [Tooltip("Rounds in tournament")]
        public int tournamentRounds = 3;

        [Tooltip("Bonus rewards for tournament wins")]
        [Range(1f, 3f)]
        public float tournamentRewardMultiplier = 2.0f;

        [Header("Equipment Recommendations")]
        [Tooltip("Recommended equipment types for combat")]
        public string[] recommendedEquipment = new string[]
        {
            "Power Gauntlets - Increase strength by 20%",
            "Battle Armor - Boost vitality by 15%",
            "Tactical Visor - Enhance intelligence by 10%",
            "Combat Boots - Improve agility by 12%"
        };

        [Header("Cross-Activity Benefits")]
        [Tooltip("Combat experience helps in Adventure quests")]
        public float adventureBonusFromCombat = 0.15f;

        [Tooltip("Strategy skills provide combat bonus")]
        public float strategyBonusToCombat = 0.10f;

        /// <summary>
        /// Gets stat weights for specific combat style
        /// </summary>
        public StyleWeights GetStyleWeights(CombatStyle style)
        {
            return style switch
            {
                CombatStyle.Aggressive => aggressiveWeights,
                CombatStyle.Defensive => defensiveWeights,
                CombatStyle.Tactical => tacticalWeights,
                CombatStyle.Balanced => balancedWeights,
                _ => balancedWeights
            };
        }

        /// <summary>
        /// Determines optimal combat style based on monster stats
        /// </summary>
        public CombatStyle DetermineOptimalStyle(float strength, float vitality, float intelligence)
        {
            float maxStat = Mathf.Max(strength, vitality, intelligence);

            if (Mathf.Approximately(maxStat, strength))
                return CombatStyle.Aggressive;
            if (Mathf.Approximately(maxStat, vitality))
                return CombatStyle.Defensive;
            if (Mathf.Approximately(maxStat, intelligence))
                return CombatStyle.Tactical;

            return CombatStyle.Balanced;
        }

        private void OnValidate()
        {
            // Ensure combat is set correctly
            activityType = ActivityType.Combat;
            activityName = "Combat Arena";

            // Ensure stat weights sum to ~1.0
            float totalWeight = primaryStatWeight + secondaryStatWeight + tertiaryStatWeight;
            if (Mathf.Abs(totalWeight - 1.0f) > 0.01f)
            {
                Debug.LogWarning($"Combat Config: Stat weights sum to {totalWeight:F2}, should be 1.0");
            }
        }
    }

    /// <summary>
    /// Combat styles that favor different genetic traits
    /// </summary>
    public enum CombatStyle
    {
        Aggressive,  // High strength focus
        Defensive,   // High vitality focus
        Tactical,    // High intelligence focus
        Balanced     // Equal distribution
    }

    /// <summary>
    /// Stat weight configuration for different combat styles
    /// </summary>
    [System.Serializable]
    public struct StyleWeights
    {
        [Range(0f, 1f)] public float strength;
        [Range(0f, 1f)] public float vitality;
        [Range(0f, 1f)] public float intelligence;
    }
}
