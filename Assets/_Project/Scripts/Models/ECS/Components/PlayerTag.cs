using Unity.Entities;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Tag component used to identify player entities in ECS systems.
    /// This is an empty struct used purely for filtering and identification.
    /// </summary>
    public struct PlayerTag : IComponentData
    {
        // Empty tag component - no data needed
    }
}
