using Unity.Entities;
using Unity.Netcode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

// Health component - replicated via NetworkVariable
public struct HealthComponent : IComponentData
{
    public int MaxHealth;
    public int CurrentHealth;
}

// Damage request component - when added, triggers damage application
public struct DamageRequest : IComponentData
{
    public int Amount;
    public DamageType Type;
    public float3 SourcePosition;
    public Entity SourceEntity;
}

// DamageType enum matching previous definition
public enum DamageType
{
    Normal,
    Critical,
    Fire,
    Ice
}
