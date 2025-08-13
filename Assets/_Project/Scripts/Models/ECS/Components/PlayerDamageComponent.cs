using Unity.Entities;
using Unity.Netcode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    #region Components

    /// <summary>
    /// Health component - replicated via NetworkVariable.
    /// </summary>
    public struct HealthComponent : IComponentData
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

    #region Enums

    /// <summary>
    /// DamageType enum matching previous definition.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Critical,
        Fire,
        Ice
    }

    #endregion
}
