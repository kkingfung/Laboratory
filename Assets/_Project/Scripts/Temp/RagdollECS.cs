using Unity.Entities;
using Unity.Mathematics;

namespace RagdollECS
{
    // Triggered when a bone is hit
    public struct HitEvent : IComponentData
    {
        public Entity BoneEntity;
        public float3 Force;
        public float DelayBeforeBlend;
    }

    // Triggered when a bone collides with environment
    public struct CollisionEvent : IComponentData
    {
        public Entity BoneEntity;
        public float3 CollisionForce;
    }

    // Triggered when character lands on the ground
    public struct LandingEvent : IComponentData
    {
        public Entity CharacterEntity;
    }

    // Stores blend-back interpolation data
    public struct BlendData : IComponentData
    {
        public float3 StartPosition;
        public quaternion StartRotation;
        public float Timer;
        public float Duration;
    }

    // Tag component to mark bones for blend-back
    public struct BlendBackTag : IComponentData { }

    // Tag component to mark bones/entities participating in partial ragdoll
    public struct PartialRagdollTag : IComponentData { }
}
