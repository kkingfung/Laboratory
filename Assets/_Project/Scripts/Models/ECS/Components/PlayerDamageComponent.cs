using Unity.Entities;
using Unity.Netcode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Laboratory.Core.Health;

namespace Laboratory.Models.ECS.Components
{
    #region Components

    /// <summary>
    /// ECS Health component - replicated via NetworkVariable.
    /// </summary>
    public struct ECSHealthComponent : IComponentData
    {
        public int MaxHealth;
        public int CurrentHealth;
    }

    /// <summary>
    /// Damage request component - when added, triggers damage application.
    /// </summary>
    public struct DamageRequest : IComponentData
    {
        public int Amount;
        public DamageType Type;
        public float3 SourcePosition;
        public Entity SourceEntity;
    }

    #endregion
}
