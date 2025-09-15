using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Input.Components
{
    /// <summary>
    /// Enhanced input component with additional features
    /// </summary>
    public struct EnhancedPlayerInputComponent : IComponentData
    {
        public float2 MoveInput;
        public float2 LookInput;
        public float2 RawMoveInput;
        public float2 SmoothedMoveInput;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool AttackPressed;
        public bool AttackHeld;
        public bool AimPressed;
        public bool AimHeld;
        public bool CrouchPressed;
        public bool CrouchHeld;
        public bool RunPressed;
        public bool RunHeld;
        public bool ReloadPressed;
        public bool InteractPressed;
        public bool WeaponSwitch1Pressed;
        public bool WeaponSwitch2Pressed;
        public float ScrollWheelDelta;
        public float InputSensitivity;
        public bool InputEnabled;
        public float InputTimestamp;
        public uint FrameNumber;
        
        // Additional properties referenced by systems
        public float2 RawMoveDirection;
        public float2 MoveDirection;
        public float2 LookDirection;
        public float2 LookDelta;
        public bool ActionPressed;
        public bool RollPressed;
        public bool CharSkillPressed;
        public bool WeaponSkillPressed;
        public bool PausePressed;
        public bool HasAnyAction;
        public bool IsMoving;
        public bool IsValid;
        public uint ValidationFlags;
        
        public static EnhancedPlayerInputComponent CreateEmpty()
        {
            return new EnhancedPlayerInputComponent
            {
                MoveInput = float2.zero,
                LookInput = float2.zero,
                RawMoveInput = float2.zero,
                SmoothedMoveInput = float2.zero,
                JumpPressed = false,
                JumpHeld = false,
                AttackPressed = false,
                AttackHeld = false,
                AimPressed = false,
                AimHeld = false,
                CrouchPressed = false,
                CrouchHeld = false,
                RunPressed = false,
                RunHeld = false,
                ReloadPressed = false,
                InteractPressed = false,
                WeaponSwitch1Pressed = false,
                WeaponSwitch2Pressed = false,
                ScrollWheelDelta = 0f,
                InputSensitivity = 1f,
                InputEnabled = true,
                InputTimestamp = UnityEngine.Time.time,
                FrameNumber = 0,
                RawMoveDirection = float2.zero,
                MoveDirection = float2.zero,
                LookDirection = float2.zero,
                LookDelta = float2.zero,
                ActionPressed = false,
                RollPressed = false,
                CharSkillPressed = false,
                WeaponSkillPressed = false,
                PausePressed = false,
                HasAnyAction = false,
                IsMoving = false,
                IsValid = true,
                ValidationFlags = 0
            };
        }
        
        public void Clear()
        {
            this = CreateEmpty();
        }
        
        public void UpdateTimestamp()
        {
            InputTimestamp = UnityEngine.Time.time;
        }
        
        public void ValidateInput()
        {
            IsValid = InputEnabled;
            ValidationFlags = IsValid ? 1u : 0u;
        }
    }
    
    /// <summary>
    /// Input configuration component
    /// </summary>
    public struct InputConfigurationComponent : IComponentData
    {
        public float MouseSensitivity;
        public float GamepadSensitivity;
        public float DeadZone;
        public float Deadzone; // Alternative spelling referenced in errors
        public float Sensitivity;
        public bool InvertYAxis;
        public bool AutoRun;
        public float HoldToRunThreshold;
        public bool EnableInputSmoothing;
        public float SmoothingFactor;
        public bool UseRawInput;
        public float InputBufferSize;
        
        public static InputConfigurationComponent CreateDefault()
        {
            return new InputConfigurationComponent
            {
                MouseSensitivity = 1.0f,
                GamepadSensitivity = 1.0f,
                DeadZone = 0.2f,
                Deadzone = 0.2f,
                Sensitivity = 1.0f,
                InvertYAxis = false,
                AutoRun = false,
                HoldToRunThreshold = 0.1f,
                EnableInputSmoothing = true,
                SmoothingFactor = 0.1f,
                UseRawInput = false,
                InputBufferSize = 8f
            };
        }
        
        public static InputConfigurationComponent Default => CreateDefault();
    }
    
    /// <summary>
    /// Input buffer component for storing input frames
    /// </summary>
    public struct InputBufferComponent : IComponentData
    {
        public int WriteIndex;
        public int ReadIndex;
        public int Count;
        public int MaxBufferSize;
        public double LastUpdateTime;
        public bool IsBuffering;
        
        public static InputBufferComponent Create(int bufferSize = 8)
        {
            return new InputBufferComponent
            {
                WriteIndex = 0,
                ReadIndex = 0,
                Count = 0,
                MaxBufferSize = bufferSize,
                LastUpdateTime = UnityEngine.Time.unscaledTimeAsDouble,
                IsBuffering = true
            };
        }
        
        public void AddFrame()
        {
            Count = math.min(Count + 1, MaxBufferSize);
            WriteIndex = (WriteIndex + 1) % MaxBufferSize;
            LastUpdateTime = UnityEngine.Time.unscaledTimeAsDouble;
        }
        
        public bool HasFrames()
        {
            return Count > 0;
        }
        
        public void Clear()
        {
            WriteIndex = 0;
            ReadIndex = 0;
            Count = 0;
            LastUpdateTime = 0;
        }
        
        public void AddInput(EnhancedPlayerInputComponent input)
        {
            // This method is referenced by the errors but missing implementation
            AddFrame();
            // Store the input data somewhere - this would need actual buffer array implementation
        }
    }
}
