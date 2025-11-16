using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities
{
    /// <summary>
    /// Component for activity center entities
    /// </summary>
    public struct ActivityCenterComponent : IComponentData
    {
        /// <summary>Activity center ID</summary>
        public uint CenterId;

        /// <summary>Primary activity type for this center</summary>
        public ActivityType ActivityType;

        /// <summary>Supported activity types (bitfield)</summary>
        public int SupportedActivities;

        /// <summary>Maximum participants</summary>
        public int MaxParticipants;

        /// <summary>Current participant count</summary>
        public int CurrentParticipants;

        /// <summary>Duration of activities in seconds</summary>
        public float ActivityDuration;

        /// <summary>Difficulty level (0-5 scale)</summary>
        public float DifficultyLevel;

        /// <summary>Center capacity (0-1)</summary>
        public float Capacity;

        /// <summary>Whether center is currently active</summary>
        public bool IsActive;

        /// <summary>Quality rating (0.5-2.0 scale)</summary>
        public float QualityRating;

        /// <summary>Activity quality modifier</summary>
        public float QualityModifier;

        /// <summary>Owner creature entity (Entity.Null if no owner)</summary>
        public Entity OwnerCreature;
    }

    /// <summary>
    /// Placeholder for ActivityCenterSystem referenced in equipment code
    /// </summary>
    public partial class ActivityCenterSystem : SystemBase
    {
        /// <summary>
        /// Check if an activity center supports a specific activity type
        /// </summary>
        public bool SupportsActivity(ActivityType activityType, uint centerId)
        {
            // Placeholder implementation
            return true;
        }

        /// <summary>
        /// Get activity centers that support a specific activity
        /// </summary>
        public uint[] GetCentersForActivity(ActivityType activityType)
        {
            // Placeholder implementation
            return new uint[0];
        }
    }
}