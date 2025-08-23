// This CooldownTimer has been deprecated and replaced with Laboratory.Core.Timing.CooldownTimer
//
// Migration Guide:
// 1. Change using statement from Laboratory.Infrastructure.AsyncUtils to Laboratory.Core.Timing
// 2. The new CooldownTimer has enhanced features:
//    - Events: OnCompleted, OnTick
//    - Auto-registration with TimerService
//    - Better performance and memory management
//    - Progress tracking with Progress property
//
// Example migration:
// OLD: using Laboratory.Infrastructure.AsyncUtils;
// NEW: using Laboratory.Core.Timing;
//
// The API is mostly compatible, with these enhancements:
// - timer.OnCompleted += () => { /* callback */ };
// - timer.OnTick += (elapsed) => { /* update UI */ };
// - float progress = timer.Progress; // 0.0 to 1.0
//
// For full documentation, see:
// Assets/_Project/Scripts/Core/Timing/TimerSystem_Documentation.md

using Laboratory.Core.Timing;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Backward compatibility wrapper for the enhanced CooldownTimer.
    /// Please migrate to Laboratory.Core.Timing.CooldownTimer for new features.
    /// </summary>
    [System.Obsolete("Use Laboratory.Core.Timing.CooldownTimer instead. See migration comments in this file.")]
    public class CooldownTimer : Laboratory.Core.Timing.CooldownTimer
    {
        /// <summary>
        /// Backward compatible constructor.
        /// </summary>
        /// <param name="duration">Duration of cooldown in seconds.</param>
        public CooldownTimer(float duration) : base(duration, autoRegister: false)
        {
            // Note: Auto-registration is disabled for backward compatibility
            // Enable it in the new implementation for automatic timer management
        }
    }
}
