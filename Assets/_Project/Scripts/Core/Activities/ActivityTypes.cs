using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Core.Activities
{
    /// <summary>
    /// Types of activities that creatures can participate in
    /// </summary>
    public enum ActivityType : byte
    {
        /// <summary>No activity</summary>
        None = 0,
        /// <summary>Combat activity</summary>
        Combat = 1,
        /// <summary>Racing activity</summary>
        Racing = 2,
        /// <summary>Puzzle-solving activity</summary>
        Puzzle = 3,
        /// <summary>Exploration activity</summary>
        Exploration = 4,
        /// <summary>Social activity</summary>
        Social = 5,
        /// <summary>Training activity</summary>
        Training = 6,
        /// <summary>Breeding activity</summary>
        Breeding = 7,
        /// <summary>Foraging activity</summary>
        Foraging = 8,
        /// <summary>Resting activity</summary>
        Resting = 9,
        /// <summary>Custom activity</summary>
        Custom = 255
    }

    /// <summary>
    /// Component for entities participating in activities
    /// </summary>
    public struct ActivityParticipantComponent : IComponentData
    {
        /// <summary>Current activity type</summary>
        public ActivityType CurrentActivity;

        /// <summary>Activity instance ID</summary>
        public uint ActivityInstanceId;

        /// <summary>Participation start time</summary>
        public double StartTime;

        /// <summary>Expected duration of participation</summary>
        public float Duration;

        /// <summary>Performance score in current activity</summary>
        public float Performance;

        /// <summary>Whether participant is actively engaged</summary>
        public bool IsActive;

        /// <summary>Team or group ID (0 = solo)</summary>
        public int TeamId;

        /// <summary>Position within activity (rank, lane, etc.)</summary>
        public int Position;
    }

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