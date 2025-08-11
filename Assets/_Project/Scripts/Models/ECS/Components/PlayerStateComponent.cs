using Unity.Entities;

namespace Models.ECS.Components
{
    /// <summary>
    /// Holds runtime player state: health, stamina, status, etc.
    /// </summary>
    public struct PlayerStateComponent : IComponentData
    {
        /// <summary>Current health points.</summary>
        public int CurrentHP;

        /// <summary>Maximum health points.</summary>
        public int MaxHP;

        /// <summary>Current stamina or energy points.</summary>
        public float Stamina;

        /// <summary>Maximum stamina or energy.</summary>
        public float MaxStamina;

        /// <summary>Is the player currently alive?</summary>
        public bool IsAlive;

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
}
