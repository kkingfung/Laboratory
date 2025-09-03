using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;

namespace Laboratory.Models.ECS.Input.Components
{
    #region Components

    /// <summary>
    /// Enhanced player input component for ECS systems with additional functionality.
    /// Stores player input data with validation and helper methods.
    /// </summary>
    public struct EnhancedPlayerInputComponent : IComponentData
    {
        #region Core Input Data
        
        /// <summary>Movement input vector (x,z), normalized.</summary>
        public float2 MoveDirection;

        /// <summary>Look direction vector (x,y,z) or rotation input.</summary>
        public float3 LookDirection;
        
        /// <summary>Raw movement input before deadzone processing.</summary>
        public float2 RawMoveDirection;
        
        /// <summary>Look input delta for frame-based look processing.</summary>
        public float2 LookDelta;
        
        #endregion

        #region Action Inputs
        
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
        
        /// <summary>Is the pause button pressed?</summary>
        public bool PausePressed;
        
        #endregion

        #region Input State Tracking
        
        /// <summary>Timestamp when input was last updated.</summary>
        public double LastUpdateTime;
        
        /// <summary>Frame number when input was last updated.</summary>
        public uint LastUpdateFrame;
        
        /// <summary>Input validation flags.</summary>
        public InputValidationFlags ValidationFlags;
        
        #endregion

        #region Helper Properties
        
        /// <summary>Inputs packed as action type flags.</summary>
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
                if (PausePressed) actions |= ActionType.Pause;
                return actions;
            }
        }

        /// <summary>True if any movement input is active.</summary>
        public bool IsMoving => math.lengthsq(MoveDirection) > 0.01f;

        /// <summary>True if any action button is pressed.</summary>
        public bool HasAnyAction => CurrentActions != ActionType.Default;

        /// <summary>Movement input magnitude.</summary>
        public float MovementMagnitude => math.length(MoveDirection);

        /// <summary>True if input data is valid.</summary>
        public bool IsValid => (ValidationFlags & InputValidationFlags.Invalid) == 0;

        #endregion

        #region Methods

        /// <summary>Reset all inputs to default (no input).</summary>
        public void Clear()
        {
            MoveDirection = float2.zero;
            LookDirection = float3.zero;
            RawMoveDirection = float2.zero;
            LookDelta = float2.zero;
            
            AttackPressed = false;
            ActionPressed = false;
            JumpPressed = false;
            RollPressed = false;
            CharSkillPressed = false;
            WeaponSkillPressed = false;
            PausePressed = false;
            
            LastUpdateTime = 0;
            LastUpdateFrame = 0;
            ValidationFlags = InputValidationFlags.None;
        }

        /// <summary>Updates the timestamp and frame information.</summary>
        public void UpdateTimestamp(double currentTime, uint currentFrame)
        {
            LastUpdateTime = currentTime;
            LastUpdateFrame = currentFrame;
        }

        /// <summary>Validates the input data and sets validation flags.</summary>
        public void ValidateInput()
        {
            ValidationFlags = InputValidationFlags.None;

            // Validate movement input
            if (math.any(math.isnan(MoveDirection)) || math.any(math.isinf(MoveDirection)))
                ValidationFlags |= InputValidationFlags.InvalidMovement;

            // Validate look input
            if (math.any(math.isnan(LookDirection)) || math.any(math.isinf(LookDirection)))
                ValidationFlags |= InputValidationFlags.InvalidLook;

            // Check for reasonable input ranges
            if (math.length(MoveDirection) > 1.1f)
                ValidationFlags |= InputValidationFlags.ExcessiveMovement;

            // Set overall invalid flag if any issues found
            if (ValidationFlags != InputValidationFlags.None)
                ValidationFlags |= InputValidationFlags.Invalid;
        }

        /// <summary>Copies input data from another component.</summary>
        public void CopyFrom(EnhancedPlayerInputComponent other)
        {
            MoveDirection = other.MoveDirection;
            LookDirection = other.LookDirection;
            RawMoveDirection = other.RawMoveDirection;
            LookDelta = other.LookDelta;
            
            AttackPressed = other.AttackPressed;
            ActionPressed = other.ActionPressed;
            JumpPressed = other.JumpPressed;
            RollPressed = other.RollPressed;
            CharSkillPressed = other.CharSkillPressed;
            WeaponSkillPressed = other.WeaponSkillPressed;
            PausePressed = other.PausePressed;
            
            LastUpdateTime = other.LastUpdateTime;
            LastUpdateFrame = other.LastUpdateFrame;
            ValidationFlags = other.ValidationFlags;
        }

        #endregion
    }

    /// <summary>
    /// Input buffer component for storing historical input data.
    /// </summary>
    public struct InputBufferComponent : IComponentData
    {
        /// <summary>Maximum number of frames to buffer.</summary>
        public const int MAX_BUFFER_SIZE = 8;
        
        /// <summary>Current buffer write index.</summary>
        public int WriteIndex;
        
        /// <summary>Number of valid entries in buffer.</summary>
        public int Count;
        
        /// <summary>Last buffer update time.</summary>
        public double LastUpdateTime;
        
        /// <summary>Adds input data to the buffer.</summary>
        public void AddInput(EnhancedPlayerInputComponent input)
        {
            // For now, just track the metadata since we can't store the full component
            // In a full implementation, you'd want to use a DynamicBuffer<> for this
            Count = Unity.Mathematics.math.min(Count + 1, MAX_BUFFER_SIZE);
            WriteIndex = (WriteIndex + 1) % MAX_BUFFER_SIZE;
            LastUpdateTime = UnityEngine.Time.unscaledTimeAsDouble;
        }
        
        /// <summary>Gets the most recent input from the buffer.</summary>
        public EnhancedPlayerInputComponent GetLatestInput()
        {
            // Return empty input for now - in full implementation would retrieve from buffer
            return new EnhancedPlayerInputComponent();
        }
        
        /// <summary>Clears the input buffer.</summary>
        public void Clear()
        {
            WriteIndex = 0;
            Count = 0;
            LastUpdateTime = 0;
        }
    }

    #endregion

    #region Enums and Flags

    /// <summary>
    /// Enhanced action type flags with additional actions.
    /// </summary>
    [Flags]
    public enum ActionType : byte
    {
        Default = 0,           // 0b0000000
        Jump = 1 << 0,         // 0b0000001
        Attack = 1 << 1,       // 0b0000010
        Roll = 1 << 2,         // 0b0000100
        Craft = 1 << 3,        // 0b0001000
        CharSkill = 1 << 4,    // 0b0010000
        WeaponSkill = 1 << 5,  // 0b0100000
        Pause = 1 << 6         // 0b1000000
    }

    /// <summary>
    /// Input validation flags.
    /// </summary>
    [Flags]
    public enum InputValidationFlags : byte
    {
        None = 0,
        InvalidMovement = 1 << 0,
        InvalidLook = 1 << 1,
        ExcessiveMovement = 1 << 2,
        Invalid = 1 << 7  // Overall invalid flag
    }

    #endregion

    #region Tag Components

    /// <summary>
    /// Tag component to identify the local player entity.
    /// </summary>
    public struct LocalPlayerTag : IComponentData { }

    /// <summary>
    /// Tag component to identify entities that should receive input.
    /// </summary>
    public struct InputEnabledTag : IComponentData { }

    /// <summary>
    /// Component to store input configuration for specific entities.
    /// </summary>
    public struct InputConfigurationComponent : IComponentData
    {
        /// <summary>Input sensitivity multiplier.</summary>
        public float Sensitivity;
        
        /// <summary>Deadzone threshold.</summary>
        public float Deadzone;
        
        /// <summary>Whether input buffering is enabled.</summary>
        public bool BufferingEnabled;
        
        /// <summary>Input buffer time in seconds.</summary>
        public float BufferTime;
        
        /// <summary>Creates default configuration.</summary>
        public static InputConfigurationComponent Default => new()
        {
            Sensitivity = 1.0f,
            Deadzone = 0.1f,
            BufferingEnabled = true,
            BufferTime = 0.5f
        };
    }

    #endregion
}
