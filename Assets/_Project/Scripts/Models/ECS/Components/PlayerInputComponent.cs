using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Basic player input component with essential input properties
    /// </summary>
    public struct PlayerInputComponent : IComponentData
    {
        public float2 MoveDirection;
        public float2 LookDirection;
        public bool ActionPressed;
        public bool RollPressed;
        public bool CharSkillPressed;
        public bool WeaponSkillPressed;
        public bool PausePressed;
        public bool JumpPressed;
        public bool AttackPressed;
        public uint CurrentActions;
        public bool IsMoving;
        public float Timestamp;
        
        public static PlayerInputComponent CreateEmpty()
        {
            return new PlayerInputComponent
            {
                MoveDirection = float2.zero,
                LookDirection = float2.zero,
                ActionPressed = false,
                RollPressed = false,
                CharSkillPressed = false,
                WeaponSkillPressed = false,
                PausePressed = false,
                JumpPressed = false,
                AttackPressed = false,
                CurrentActions = 0,
                IsMoving = false,
                Timestamp = UnityEngine.Time.time
            };
        }
    }
}
