using Unity.Netcode;
using Unity.Collections;

namespace Laboratory.Infrastructure.Networking
{
    #region Enums

    /// <summary>
    /// Represents the current life state of a network entity.
    /// </summary>
    public enum LifeState : byte
    {
        /// <summary>Entity is alive and active.</summary>
        Alive,
        /// <summary>Entity is dead and inactive.</summary>
        Dead
    }

    #endregion

    /// <summary>
    /// Network-synchronized component that manages entity life state and respawn timing.
    /// Provides server-authoritative life state management with client visibility.
    /// </summary>
    public class NetworkLifeState : NetworkBehaviour
    {
        #region Fields

        /// <summary>Current life state of the entity (Alive/Dead).</summary>
        public NetworkVariable<LifeState> CurrentState = new NetworkVariable<LifeState>(
            LifeState.Alive,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Time remaining until respawn (in seconds). Only relevant when dead.</summary>
        public NetworkVariable<float> RespawnTimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        #endregion

        #region Properties

        /// <summary>Whether the entity is currently alive.</summary>
        public bool IsAlive => CurrentState.Value == LifeState.Alive;

        /// <summary>Whether the entity is currently dead.</summary>
        public bool IsDead => CurrentState.Value == LifeState.Dead;

        /// <summary>Whether the entity is waiting to respawn.</summary>
        public bool IsWaitingToRespawn => IsDead && RespawnTimeRemaining.Value > 0f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the entity to dead state and starts respawn countdown. Server only.
        /// </summary>
        /// <param name="respawnDelay">Time in seconds until respawn becomes available.</param>
        public void SetDead(float respawnDelay = 0f)
        {
            if (!IsServer) return;

            CurrentState.Value = LifeState.Dead;
            RespawnTimeRemaining.Value = respawnDelay;
        }

        /// <summary>
        /// Sets the entity to alive state and clears respawn timer. Server only.
        /// </summary>
        public void SetAlive()
        {
            if (!IsServer) return;

            CurrentState.Value = LifeState.Alive;
            RespawnTimeRemaining.Value = 0f;
        }

        /// <summary>
        /// Updates the respawn countdown timer. Server only.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        public void UpdateRespawnTimer(float deltaTime)
        {
            if (!IsServer || CurrentState.Value != LifeState.Dead) return;

            if (RespawnTimeRemaining.Value > 0f)
            {
                RespawnTimeRemaining.Value = UnityEngine.Mathf.Max(0f, RespawnTimeRemaining.Value - deltaTime);
            }
        }

        #endregion
    }
}
