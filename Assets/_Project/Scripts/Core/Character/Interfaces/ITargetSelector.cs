using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Interface for target selection systems.
    /// Handles detection, scoring, and validation of potential targets.
    /// </summary>
    public interface ITargetSelector : ICharacterController
    {
        /// <summary>
        /// Currently selected target, null if none
        /// </summary>
        Transform CurrentTarget { get; }

        /// <summary>
        /// All detected targets in range
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
        /// Forces an immediate target detection update
        /// </summary>
        void ForceTargetUpdate();

        /// <summary>
        /// Manually sets the current target
        /// </summary>
        /// <param name="target">Target to select</param>
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
        /// Gets the highest priority target based on scoring
        /// </summary>
        /// <returns>Best target or null if none found</returns>
        Transform GetHighestPriorityTarget();

        /// <summary>
        /// Gets all targets within a specific distance
        /// </summary>
        /// <param name="distance">Maximum distance to check</param>
        /// <returns>List of targets within distance</returns>
        List<Transform> GetTargetsWithinDistance(float distance);

        /// <summary>
        /// Validates if a target meets selection criteria
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <returns>True if target is valid</returns>
        bool ValidateTarget(Transform target);

        /// <summary>
        /// Calculates priority score for a target
        /// </summary>
        /// <param name="target">Target to score</param>
        /// <returns>Priority score (higher = better)</returns>
        float CalculateTargetScore(Transform target);
    }
}
