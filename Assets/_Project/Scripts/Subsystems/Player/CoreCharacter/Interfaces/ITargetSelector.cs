using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;

namespace Laboratory.Core.Character.Interfaces
{
    /// <summary>
    /// Interface for target selection systems.
    /// Provides standardized target detection, validation, and selection functionality.
    /// </summary>
    public interface ITargetSelector
    {
        #region Properties

        /// <summary>
        /// Whether the target selector is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Currently selected target transform
        /// </summary>
        Transform CurrentTarget { get; }

        /// <summary>
        /// List of all detected targets
        /// </summary>
        IReadOnlyList<Transform> DetectedTargets { get; }

        /// <summary>
        /// Number of currently detected targets
        /// </summary>
        int TargetCount { get; }

        /// <summary>
        /// Whether any targets are currently detected
        /// </summary>
        bool HasTargets { get; }

        /// <summary>
        /// Detection radius for target finding
        /// </summary>
        float DetectionRadius { get; set; }

        /// <summary>
        /// Maximum detection distance
        /// </summary>
        float MaxDetectionDistance { get; set; }

        /// <summary>
        /// Layer mask for valid targets
        /// </summary>
        LayerMask TargetLayers { get; set; }

        /// <summary>
        /// Layer mask for obstacles that block detection
        /// </summary>
        LayerMask ObstacleLayers { get; set; }

        /// <summary>
        /// Whether to prioritize closest target over scoring system
        /// </summary>
        bool PrioritizeClosest { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the current target changes
        /// </summary>
        event Action<Transform> OnTargetChanged;

        /// <summary>
        /// Event fired when a new target is detected
        /// </summary>
        event Action<Transform> OnTargetDetected;

        /// <summary>
        /// Event fired when a target is lost
        /// </summary>
        event Action<Transform> OnTargetLost;

        #endregion

        #region Methods

        /// <summary>
        /// Activates or deactivates the target selector
        /// </summary>
        /// <param name="active">Whether the selector should be active</param>
        void SetActive(bool active);

        /// <summary>
        /// Initializes the target selector with required services
        /// </summary>
        /// <param name="services">Service container for dependency injection</param>
        void Initialize(IServiceContainer services);

        /// <summary>
        /// Forces an immediate target detection update
        /// </summary>
        void ForceTargetUpdate();

        /// <summary>
        /// Manually sets the current target
        /// </summary>
        /// <param name="target">Target transform to set</param>
        void SetCurrentTarget(Transform target);

        /// <summary>
        /// Clears the current target selection
        /// </summary>
        void ClearCurrentTarget();

        /// <summary>
        /// Gets the closest target from detected targets
        /// </summary>
        /// <returns>Closest target or null if none found</returns>
        Transform GetClosestTarget();

        /// <summary>
        /// Gets the target with the highest priority score
        /// </summary>
        /// <returns>Highest priority target or null if none found</returns>
        Transform GetHighestPriorityTarget();

        /// <summary>
        /// Gets all targets within a specific distance
        /// </summary>
        /// <param name="distance">Maximum distance to check</param>
        /// <returns>List of targets within the specified distance</returns>
        List<Transform> GetTargetsWithinDistance(float distance);

        /// <summary>
        /// Validates whether a target is suitable for selection
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <returns>True if the target is valid</returns>
        bool ValidateTarget(Transform target);

        /// <summary>
        /// Calculates a priority score for a target
        /// </summary>
        /// <param name="target">Target to score</param>
        /// <returns>Priority score (higher is better)</returns>
        float CalculateTargetScore(Transform target);

        /// <summary>
        /// Disposes of the target selector and cleans up resources
        /// </summary>
        void Dispose();

        #endregion
    }

    /// <summary>
    /// Interface for character controllers that can be managed by systems
    /// </summary>
    public interface ICharacterController
    {
        /// <summary>
        /// Whether the controller is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Activates or deactivates the controller
        /// </summary>
        /// <param name="active">Whether the controller should be active</param>
        void SetActive(bool active);

        /// <summary>
        /// Initializes the controller with required services
        /// </summary>
        /// <param name="services">Service container for dependency injection</param>
        void Initialize(IServiceContainer services);

        /// <summary>
        /// Updates the controller (called per frame if active)
        /// </summary>
        void UpdateController();

        /// <summary>
        /// Disposes of the controller and cleans up resources
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Interface for character state management
    /// </summary>
    public interface ICharacterStateManager
    {
        /// <summary>
        /// Current state of the character
        /// </summary>
        string CurrentState { get; }

        /// <summary>
        /// Previous state of the character
        /// </summary>
        string PreviousState { get; }

        /// <summary>
        /// Whether the character can transition to the specified state
        /// </summary>
        /// <param name="newState">State to check</param>
        /// <returns>True if the transition is valid</returns>
        bool CanTransitionTo(string newState);

        /// <summary>
        /// Attempts to transition to a new state
        /// </summary>
        /// <param name="newState">State to transition to</param>
        /// <returns>True if the transition was successful</returns>
        bool TryTransitionTo(string newState);

        /// <summary>
        /// Forces a state transition without validation
        /// </summary>
        /// <param name="newState">State to transition to</param>
        void ForceTransitionTo(string newState);

        /// <summary>
        /// Event fired when character state changes
        /// </summary>
        event Action<string, string> OnStateChanged;
    }

    /// <summary>
    /// Interface for aim control systems
    /// </summary>
    public interface IAimController
    {
        /// <summary>
        /// Current aim target
        /// </summary>
        Transform AimTarget { get; }

        /// <summary>
        /// Current aim direction
        /// </summary>
        Vector3 AimDirection { get; }

        /// <summary>
        /// Whether the controller is currently aiming
        /// </summary>
        bool IsAiming { get; }

        /// <summary>
        /// Sets the aim target
        /// </summary>
        /// <param name="target">Target to aim at</param>
        void SetAimTarget(Transform target);

        /// <summary>
        /// Sets the aim direction manually
        /// </summary>
        /// <param name="direction">Direction to aim</param>
        void SetAimDirection(Vector3 direction);

        /// <summary>
        /// Starts aiming
        /// </summary>
        void StartAiming();

        /// <summary>
        /// Stops aiming
        /// </summary>
        void StopAiming();

        /// <summary>
        /// Event fired when aim target changes
        /// </summary>
        event Action<Transform> OnAimTargetChanged;

        /// <summary>
        /// Event fired when aiming starts or stops
        /// </summary>
        event Action<bool> OnAimingStateChanged;
    }
}
