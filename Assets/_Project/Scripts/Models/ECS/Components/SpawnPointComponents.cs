using Unity.Entities;

#nullable enable

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Tag component to identify spawn point entities.
    /// </summary>
    public struct SpawnPointTag : IComponentData
    {
    }

    /// <summary>
    /// Core data for spawn point entities.
    /// </summary>
    public struct SpawnPointData : IComponentData
    {
        public int SpawnPointId;
        public int TeamId;
        public int Priority;
        public bool IsActive;
        public bool IsSafe;
        public float LastUsedTime;
        public float CooldownDuration;
    }

    /// <summary>
    /// Safety check configuration for spawn points.
    /// </summary>
    public struct SpawnPointSafety : IComponentData
    {
        public float SafetyRadius;
        public float CheckInterval;
        public float LastCheckedTime;
    }


}
