using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Represents a damage event for an entity.
    /// </summary>
    [System.Serializable]
    public struct DamageTakenEvent
    {
        public int TargetEntityId;  // Using int instead of Entity for buffer compatibility
        public int DamageAmount;
        public DamageType DamageType;
        public float3 SourcePosition;
        public bool IsDead;
    }

    /// <summary>
    /// Buffer element for storing damage events in DOTS.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct DamageTakenEventBufferElement : IBufferElementData
    {
        public DamageTakenEvent Value;
    }
}
