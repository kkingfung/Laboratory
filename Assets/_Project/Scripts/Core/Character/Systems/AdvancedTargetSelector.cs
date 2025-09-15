using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Health;

namespace Laboratory.Core.Character.Systems
{
    /// <summary>
    /// Advanced target selection system for AI and player targeting
    /// </summary>
    public class AdvancedTargetSelector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float fieldOfViewAngle = 120f;
        [SerializeField] private LayerMask targetLayerMask = -1;
        [SerializeField] private LayerMask obstacleLayerMask = -1;
        [SerializeField] private bool requireLineOfSight = true;

        [Header("Targeting Priorities")]
        [SerializeField] private TargetingPriority primaryPriority = TargetingPriority.Closest;
        [SerializeField] private TargetingPriority secondaryPriority = TargetingPriority.LowestHealth;

        private Transform currentTarget;
        private List<Transform> availableTargets = new List<Transform>();
        private float lastScanTime;
        private float scanInterval = 0.1f;

        public Transform CurrentTarget => currentTarget;
        public List<Transform> AvailableTargets => availableTargets;
        public float DetectionRange => detectionRange;
        public float MaxDetectionDistance => detectionRange;
        public List<Transform> DetectedTargets => availableTargets;
        public int TargetCount => availableTargets.Count;
        public bool HasTargets => availableTargets.Count > 0;
        public bool IsActive { get; private set; } = true;
        
        // Property for testing support
        public float DetectionRadius 
        { 
            get => detectionRange; 
            set => detectionRange = value; 
        }

        public System.Action<Transform> OnTargetAcquired;
        public System.Action<Transform> OnTargetLost;

        /// <summary>
        /// Initializes the target selector with service dependencies
        /// </summary>
        public void Initialize(object services = null)
        {
            IsActive = true;
            // Additional initialization logic can be added here
        }

        private void Update()
        {
            if (Time.time - lastScanTime >= scanInterval)
            {
                ScanForTargets();
                UpdateCurrentTarget();
                lastScanTime = Time.time;
            }
        }

        /// <summary>
        /// Scans for available targets within range
        /// </summary>
        private void ScanForTargets()
        {
            availableTargets.Clear();
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, targetLayerMask);
            
            foreach (var collider in colliders)
            {
                if (collider.transform == transform) continue;
                
                if (IsValidTarget(collider.transform))
                {
                    availableTargets.Add(collider.transform);
                }
            }
        }

        /// <summary>
        /// Validates if a target is selectable
        /// </summary>
        private bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angle > fieldOfViewAngle / 2f) return false;

            if (requireLineOfSight)
            {
                if (Physics.Linecast(transform.position, target.position, obstacleLayerMask))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the current target based on priority
        /// </summary>
        private void UpdateCurrentTarget()
        {
            Transform previousTarget = currentTarget;
            
            if (availableTargets.Count == 0)
            {
                currentTarget = null;
            }
            else
            {
                currentTarget = SelectBestTarget(availableTargets);
            }

            if (previousTarget != currentTarget)
            {
                if (previousTarget != null)
                    OnTargetLost?.Invoke(previousTarget);
                    
                if (currentTarget != null)
                    OnTargetAcquired?.Invoke(currentTarget);
            }
        }

        /// <summary>
        /// Selects the best target based on priority settings
        /// </summary>
        private Transform SelectBestTarget(List<Transform> targets)
        {
            if (targets.Count == 0) return null;
            if (targets.Count == 1) return targets[0];

            var sortedTargets = SortTargetsByPriority(targets, primaryPriority);
            
            // Use secondary priority if multiple targets have same primary priority score
            if (sortedTargets.Count > 1)
            {
                var topPriorityTargets = GetTopPriorityTargets(sortedTargets, primaryPriority);
                if (topPriorityTargets.Count > 1)
                {
                    sortedTargets = SortTargetsByPriority(topPriorityTargets, secondaryPriority);
                }
            }
            
            return sortedTargets[0];
        }

        /// <summary>
        /// Sorts targets by the specified priority
        /// </summary>
        private List<Transform> SortTargetsByPriority(List<Transform> targets, TargetingPriority priority)
        {
            switch (priority)
            {
                case TargetingPriority.Closest:
                    return targets.OrderBy(t => Vector3.Distance(transform.position, t.position)).ToList();
                    
                case TargetingPriority.Furthest:
                    return targets.OrderByDescending(t => Vector3.Distance(transform.position, t.position)).ToList();
                    
                case TargetingPriority.LowestHealth:
                    return targets.OrderBy(GetTargetHealth).ToList();
                    
                case TargetingPriority.HighestHealth:
                    return targets.OrderByDescending(GetTargetHealth).ToList();
                    
                default:
                    return targets;
            }
        }

        /// <summary>
        /// Gets targets that share the top priority value
        /// </summary>
        private List<Transform> GetTopPriorityTargets(List<Transform> sortedTargets, TargetingPriority priority)
        {
            var result = new List<Transform> { sortedTargets[0] };
            float topValue = GetPriorityValue(sortedTargets[0], priority);

            for (int i = 1; i < sortedTargets.Count; i++)
            {
                float value = GetPriorityValue(sortedTargets[i], priority);
                if (Mathf.Approximately(value, topValue))
                {
                    result.Add(sortedTargets[i]);
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the priority value for a target
        /// </summary>
        private float GetPriorityValue(Transform target, TargetingPriority priority)
        {
            switch (priority)
            {
                case TargetingPriority.Closest:
                case TargetingPriority.Furthest:
                    return Vector3.Distance(transform.position, target.position);
                    
                case TargetingPriority.LowestHealth:
                case TargetingPriority.HighestHealth:
                    return GetTargetHealth(target);
                    
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Gets the health value of a target
        /// </summary>
        private float GetTargetHealth(Transform target)
        {
            var healthComponent = target.GetComponent<IHealthComponent>();
            if (healthComponent != null)
                return healthComponent.CurrentHealth;

            return 100f;
        }

        /// <summary>
        /// Forces a target selection update
        /// </summary>
        public void ForceTargetUpdate()
        {
            ScanForTargets();
            UpdateCurrentTarget();
        }

        /// <summary>
        /// Manually sets a specific target
        /// </summary>
        public void SetTarget(Transform target)
        {
            Transform previousTarget = currentTarget;
            currentTarget = target;

            if (previousTarget != currentTarget)
            {
                if (previousTarget != null)
                    OnTargetLost?.Invoke(previousTarget);
                    
                if (currentTarget != null)
                    OnTargetAcquired?.Invoke(currentTarget);
            }
        }
        
        /// <summary>
        /// Initialize the target selector with settings
        /// </summary>
        public void Initialize(float range, bool lineOfSight = true)
        {
            detectionRange = range;
            requireLineOfSight = lineOfSight;
            IsActive = true;
        }
        
        /// <summary>
        /// Validates a specific target
        /// </summary>
        public bool ValidateTarget(Transform target)
        {
            return IsValidTarget(target);
        }
        
        /// <summary>
        /// Calculate target score for prioritization
        /// </summary>
        public float CalculateTargetScore(Transform target)
        {
            if (target == null) return 0f;
            
            float distance = Vector3.Distance(transform.position, target.position);
            float health = GetTargetHealth(target);
            
            // Simple scoring: closer targets with lower health get higher scores
            return (detectionRange - distance) + (100f - health);
        }
        
        /// <summary>
        /// Set the active state of the selector
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            if (!active)
            {
                ClearCurrentTarget();
            }
        }
        
        /// <summary>
        /// Clear the current target
        /// </summary>
        public void ClearCurrentTarget()
        {
            if (currentTarget != null)
            {
                Transform previousTarget = currentTarget;
                currentTarget = null;
                OnTargetLost?.Invoke(previousTarget);
            }
        }
        
        /// <summary>
        /// Get the closest target from available targets
        /// </summary>
        public Transform GetClosestTarget()
        {
            if (availableTargets.Count == 0) return null;
            
            Transform closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var target in availableTargets)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = target;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// Event subscription for target changes
        /// </summary>
        public System.Action<Transform> OnTargetChanged
        {
            get => OnTargetAcquired;
            set => OnTargetAcquired = value;
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            availableTargets.Clear();
            currentTarget = null;
            OnTargetAcquired = null;
            OnTargetLost = null;
            IsActive = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
    }

    /// <summary>
    /// Targeting priority options
    /// </summary>
    public enum TargetingPriority
    {
        Closest,
        Furthest,
        LowestHealth,
        HighestHealth,
        Random
    }
}