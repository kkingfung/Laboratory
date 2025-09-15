using System;

namespace Laboratory.Models.ECS.Input.Components
{
    /// <summary>
    /// Flags for input validation
    /// </summary>
    [Flags]
    public enum InputValidationFlags : uint
    {
        None = 0,
        Valid = 1,
        WithinDeadzone = 2,
        Bounded = 4,
        Filtered = 8,
        Smoothed = 16,
        TimestampValid = 32,
        All = Valid | WithinDeadzone | Bounded | Filtered | Smoothed | TimestampValid
    }
    
    /// <summary>
    /// Action types for player input
    /// </summary>
    [Flags]
    public enum ActionType : uint
    {
        None = 0,
        Move = 1,
        Look = 2,
        Jump = 4,
        Attack = 8,
        Roll = 16,
        CharSkill = 32,
        WeaponSkill = 64,
        Pause = 128,
        Interact = 256,
        Reload = 512,
        Aim = 1024
    }
}
