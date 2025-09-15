using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Interface for character aiming controllers.
    /// Handles target acquisition, tracking, and constraint management.
    /// </summary>
    public interface IAimController : ICharacterController
    {
        /// <summary>
        /// Current target being aimed at, null if no target
        /// </summary>
        Transform CurrentTarget { get; }

        /// <summary>
        /// Whether the character is currently aiming at a target
        /// </summary>
        bool IsAiming { get; }

        /// <summary>
        /// Current aim weight being applied (0-1)
        /// </summary>
        float AimWeight { get; }

        /// <summary>
        /// Maximum distance for target acquisition
        /// </summary>
        float MaxAimDistance { get; set; }

        /// <summary>
        /// Sets a specific target to aim at
        /// </summary>
        /// <param name="target">Target transform to aim at</param>
        void SetTarget(Transform target);

        /// <summary>
        /// Clears the current target and stops aiming
        /// </summary>
        void ClearTarget();

        /// <summary>
        /// Sets the overall aim weight multiplier
        /// </summary>
        /// <param name="weight">Aim weight (0-1)</param>
        void SetAimWeight(float weight);

        /// <summary>
        /// Enables or disables automatic target selection
        /// </summary>
        /// <param name="enabled">True to enable automatic targeting</param>
        void SetAutoTargeting(bool enabled);

        /// <summary>
        /// Checks if a target is valid for aiming
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <returns>True if target can be aimed at</returns>
        bool IsValidTarget(Transform target);
    }
}
