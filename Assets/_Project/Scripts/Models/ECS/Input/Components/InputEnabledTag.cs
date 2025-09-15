using Unity.Entities;

namespace Laboratory.Models.ECS.Input.Components
{
    /// <summary>
    /// Tag component to mark entities that should receive input processing
    /// </summary>
    public struct InputEnabledTag : IComponentData
    {
        // Empty tag component - Unity ECS will handle it
    }
}
