using Unity.Entities;
using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    #region Components

    /// <summary>
    /// Stores player state data for ECS systems.
    /// </summary>
    public struct PlayerStateComponent : IComponentData
    {
        /// <summary>Is the player currently alive?</summary>
        public bool IsAlive;

        /// <summary>Is the player currently stunned?</summary>
        public bool IsStunned;

        /// <summary>Is the player currently invulnerable?</summary>
        public bool IsInvulnerable;

        /// <summary>Current team index (if applicable).</summary>
        public int TeamIndex;

        /// <summary>Current score.</summary>
        public int Score;

        /// <summary>Player's current health points.</summary>
        public int CurrentHP;

        /// <summary>Player's maximum health points.</summary>
        public int MaxHP;

        /// <summary>Player's current stamina or energy points.</summary>
        public float Stamina;

        /// <summary>Player's maximum stamina or energy.</summary>
        public float MaxStamina;

        /// <summary>Current status flags bitmask (e.g. stunned, poisoned).</summary>
        public uint StatusFlags;

        /// <summary>Resets to default alive state with full HP and stamina.</summary>
        public void ResetState(int maxHp, float maxStamina)
        {
            MaxHP = maxHp;
            CurrentHP = maxHp;
            MaxStamina = maxStamina;
            Stamina = maxStamina;
            IsAlive = true;
            StatusFlags = 0;
        }
    }

    #endregion
}
