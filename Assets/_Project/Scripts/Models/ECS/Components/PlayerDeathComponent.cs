using Unity.Entities;
using UnityEngine;

// Marks entity as dead, and records time of death
public struct DeadTag : IComponentData { }

public struct DeathTime : IComponentData
{
    public float TimeOfDeath;
}

public struct RespawnTimer : IComponentData
{
    public float TimeRemaining;
}

// Link to Animator or animation trigger
public struct DeathAnimationTrigger : IComponentData
{
    public bool Triggered;
}
