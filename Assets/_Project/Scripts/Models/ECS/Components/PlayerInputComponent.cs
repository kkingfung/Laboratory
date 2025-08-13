using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    #region Components

    /// <summary>
    /// Stores player input data for ECS systems.
    /// </summary>
    public struct PlayerInputComponent : IComponentData
    {
        /// <summary>Movement input vector (x,z), normalized.</summary>
        public float2 MoveDirection;

        /// <summary>Look direction vector (x,y,z) or rotation input.</summary>
        public float3 LookDirection;

        /// <summary>Is the attack button pressed this frame?</summary>
        public bool AttackPressed;

        /// <summary>Is the jump button pressed?</summary>
        public bool JumpPressed;

        /// <summary>Additional inputs packed as bits or flags.</summary>
        public byte AdditionalFlags;

        /// <summary>Reset all inputs to default (no input).</summary>
        public void Clear()
        {
            MoveDirection = float2.zero;
            LookDirection = float3.zero;
            AttackPressed = false;
            JumpPressed = false;
            AdditionalFlags = 0;
        }
    }

    #endregion
}
