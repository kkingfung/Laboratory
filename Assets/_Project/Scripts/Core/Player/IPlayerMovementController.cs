using UnityEngine;

namespace Laboratory.Core.Player
{
    /// <summary>
    /// Interface for player movement controllers.
    /// Provides abstraction for different movement systems (FPS, TPS, RTS, etc.)
    /// </summary>
    public interface IPlayerMovementController
    {
        /// <summary>Current movement speed</summary>
        float MovementSpeed { get; set; }

        /// <summary>Whether the player is currently moving</summary>
        bool IsMoving { get; }

        /// <summary>Current velocity vector</summary>
        Vector3 Velocity { get; }

        /// <summary>Whether player can move</summary>
        bool CanMove { get; set; }

        /// <summary>Movement input sensitivity</summary>
        float Sensitivity { get; set; }

        /// <summary>Moves the player in the specified direction</summary>
        void Move(Vector3 direction);

        /// <summary>Stops all movement</summary>
        void Stop();

        /// <summary>Teleports player to specified position</summary>
        void Teleport(Vector3 position);

        /// <summary>Sets movement restrictions</summary>
        void SetMovementRestrictions(MovementRestrictions restrictions);

        /// <summary>Event fired when movement state changes</summary>
        event System.Action<bool> OnMovementStateChanged; // isMoving
    }

    /// <summary>Movement restriction flags</summary>
    [System.Flags]
    public enum MovementRestrictions
    {
        None = 0,
        NoForward = 1 << 0,
        NoBackward = 1 << 1,
        NoLeft = 1 << 2,
        NoRight = 1 << 3,
        NoJump = 1 << 4,
        NoRun = 1 << 5,
        All = ~0
    }
}