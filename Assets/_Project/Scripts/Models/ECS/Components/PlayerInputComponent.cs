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

        /// <summary>Is the action button pressed?</summary>
        public bool ActionPressed;

        /// <summary>Is the jump button pressed?</summary>
        public bool JumpPressed;

        /// <summary>Is the roll button pressed?</summary>
        public bool RollPressed;

        /// <summary>Is the character skill button pressed?</summary>
        public bool CharSkillPressed;

        /// <summary>Is the weapon skill button pressed?</summary>
        public bool WeaponSkillPressed;

        /// <summary>Inputs packed as bits or flags.</summary>
        public ActionType CurrentActions
        {
            get
            {
                ActionType actions = ActionType.Default;
                if (JumpPressed) actions |= ActionType.Jump;
                if (AttackPressed) actions |= ActionType.Attack;
                if (RollPressed) actions |= ActionType.Roll;
                if (ActionPressed) actions |= ActionType.Craft;
                if (CharSkillPressed) actions |= ActionType.CharSkill;
                if (WeaponSkillPressed) actions |= ActionType.WeaponSkill;
                return actions;
            }
        }

        /// <summary>Reset all inputs to default (no input).</summary>
        public void Clear()
        {
            MoveDirection = float2.zero;
            LookDirection = float3.zero;
            AttackPressed = false;
            JumpPressed = false;
            RollPressed = false;
            ActionPressed = false;
            CharSkillPressed = false;
            WeaponSkillPressed = false;
        }
    }

    [System.Flags]
    public enum ActionType : byte  
    {
        Default = 0,        // 0b000000
        Jump = 1 << 0,      // 0b000001
        Attack = 1 << 1,    // 0b000010
        Roll = 1 << 2,      // 0b000100
        Craft = 1 << 3,     // 0b001000
        CharSkill = 1 << 4, // 0b010000
        WeaponSkill = 1 << 5 // 0b100000
    }

    #endregion
}
