using Unity.Entities;
using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    #region Components

    /// <summary>
    /// Marks entity as dead.
    /// </summary>
    public struct DeadTag : IComponentData { }

    /// <summary>
    /// Stores the time of death for an entity.
    /// </summary>
    public struct DeathTime : IComponentData
    {
        public float TimeOfDeath;
    }

    /// <summary>
    /// Tracks respawn timer for an entity.
    /// </summary>
    public struct RespawnTimer : IComponentData
    {
        public float TimeRemaining;
    }

    /// <summary>
    /// Triggers death animation for an entity.
    /// </summary>
    public struct DeathAnimationTrigger : IComponentData
    {
        public bool Triggered;
    }

    #endregion
}
