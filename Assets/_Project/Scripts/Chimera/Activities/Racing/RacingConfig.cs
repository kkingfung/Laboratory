using UnityEngine;

namespace Laboratory.Chimera.Activities.Racing
{
    /// <summary>
    /// Configuration for Racing Circuit activity
    /// Defines tracks, performance scaling, and rewards
    /// </summary>
    [CreateAssetMenu(fileName = "RacingConfig", menuName = "Chimera/Activities/Racing Config")]
    public class RacingConfig : ActivityConfig
    {
        [Header("Racing-Specific Settings")]
        [Tooltip("Performance variation to simulate execution variance (0.0 to 0.2)")]
        [Range(0f, 0.2f)]
        public float performanceVariation = 0.05f;

        [Tooltip("Track types available")]
        public TrackType[] availableTracks = new TrackType[]
        {
            TrackType.Land,
            TrackType.Sky,
            TrackType.Water
        };

        [Header("Track Modifiers")]
        [Tooltip("Land track favors balanced stats")]
        public StatWeights landTrackWeights = new StatWeights
        {
            agility = 0.5f,
            vitality = 0.3f,
            adaptability = 0.2f
        };

        [Tooltip("Sky track favors agility heavily")]
        public StatWeights skyTrackWeights = new StatWeights
        {
            agility = 0.7f,
            vitality = 0.2f,
            adaptability = 0.1f
        };

        [Tooltip("Water track favors vitality and adaptability")]
        public StatWeights waterTrackWeights = new StatWeights
        {
            agility = 0.3f,
            vitality = 0.4f,
            adaptability = 0.3f
        };

        [Header("Equipment Recommendations")]
        [Tooltip("Recommended equipment types for racing")]
        public string[] recommendedEquipment = new string[]
        {
            "Speed Boots - Increase agility by 15%",
            "Aerodynamic Gear - Reduce air resistance",
            "Endurance Drink - Boost vitality by 10%",
            "Adaptive Tires - Handle any track surface"
        };

        /// <summary>
        /// Gets stat weights for specific track type
        /// </summary>
        public StatWeights GetTrackWeights(TrackType trackType)
        {
            return trackType switch
            {
                TrackType.Land => landTrackWeights,
                TrackType.Sky => skyTrackWeights,
                TrackType.Water => waterTrackWeights,
                _ => landTrackWeights
            };
        }

        private void OnValidate()
        {
            // Ensure racing is set correctly
            activityType = ActivityType.Racing;
            activityName = "Racing Circuit";

            // Ensure stat weights sum to ~1.0
            float totalWeight = primaryStatWeight + secondaryStatWeight + tertiaryStatWeight;
            if (Mathf.Abs(totalWeight - 1.0f) > 0.01f)
            {
                Debug.LogWarning($"Racing Config: Stat weights sum to {totalWeight:F2}, should be 1.0");
            }
        }
    }

    /// <summary>
    /// Track types that favor different genetic traits
    /// </summary>
    public enum TrackType
    {
        Land,   // Balanced requirements
        Sky,    // High agility requirement
        Water   // High vitality and adaptability
    }

    /// <summary>
    /// Stat weight configuration for different track types
    /// </summary>
    [System.Serializable]
    public struct StatWeights
    {
        [Range(0f, 1f)] public float agility;
        [Range(0f, 1f)] public float vitality;
        [Range(0f, 1f)] public float adaptability;
    }
}
