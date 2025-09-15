using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// ECS compatible damage request component
    /// </summary>
    public struct DamageRequest : IComponentData
    {
        public float Amount;
        public int DamageType; // Using int instead of enum for ECS compatibility
        public bool CanBeBlocked;
        public bool CanBeDodged;
        public bool IsCritical;
        public Entity Source;
        public float3 HitPoint;
        public float3 Direction;
        public float KnockbackForce;
        public float Timestamp;
        public int DamageId;

        public static DamageRequest Create(float amount, Entity source = default)
        {
            return new DamageRequest
            {
                Amount = amount,
                DamageType = 0, // Physical
                CanBeBlocked = true,
                CanBeDodged = true,
                IsCritical = false,
                Source = source,
                HitPoint = float3.zero,
                Direction = float3.zero,
                KnockbackForce = 0f,
                Timestamp = Time.time,
                DamageId = UnityEngine.Random.Range(1, int.MaxValue)
            };
        }
    }
}
