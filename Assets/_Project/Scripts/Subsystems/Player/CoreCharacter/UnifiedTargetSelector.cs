using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Character.Events;
using Laboratory.Core.Character.Interfaces;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Unified target selection system combining the best features from both TargetSelector and AdvancedTargetSelector.
    /// Provides intelligent target detection, prioritization, and selection with performance optimization.
    /// </summary>
    public class UnifiedTargetSelector : MonoBehaviour, ITargetSelector
    {
        #region Nested Types

        [System.Serializable]
        public class DetectionSettings
        {
            [Range(0.5f, 20f)]
            public float detectionRadius = 10f;
            [Range(1f, 50f)]
            public float maxDetectionDistance = 15f;
            public LayerMask targetLayers = -1;
            public LayerMask obstacleLayers = -1;
            public bool useProximityDetection = true;
            public bool useRaycastDetection = true;
            public bool validateLineOfSight = true;
            [Range(0.1f, 2f)]
            public float minTargetSize = 0.5f;
        }

        [System.Serializable]
        public class PrioritizationSettings
        {
            [Range(0f, 2f)]
            public float distanceWeight = 1f;
            [Range(0f, 2f)]
            public float angleWeight = 0.5f;
            [Range(0f, 2f)]
            public float visibilityWeight = 0.8f;
            [Range(0f, 1f)]
            public float screenCenterWeight = 0.3f;
            public bool prioritizeClosest = true;
            public bool useCameraCenterBias = true;
        }

        [System.Serializable]
        public class PerformanceSettings
        {
            [Range(0.05f, 1f)]
            public float updateInterval = 0.1f;
            [Range(1, 50)]
            public int maxTargetsPerFrame = 10;
            public bool enablePerformanceMetrics = false;
        }

        private class TargetInfo
        {
            public float Distance { get; set; }
            public float Angle { get; set; }
            public Vector3 LastKnownPosition { get; set; }
            public float LastUpdateTime { get; set; }
            public float Score { get; set; }
        }

        #endregion

        #region Fields

        [Header("Detection")]
        [SerializeField] private DetectionSettings _detection = new DetectionSettings();

        [Header("Prioritization")]
        [SerializeField] private PrioritizationSettings _prioritization = new PrioritizationSettings();

        [Header("Performance")]
        [SerializeField] private PerformanceSettings _performance = new PerformanceSettings();

        [Header("Camera Integration")]
        [SerializeField] private UnityEngine.Camera _playerCamera;

        // Runtime state
        private bool _isActive = true;
        private bool _isInitialized = false;
        private Transform _currentTarget;
        private readonly List<Transform> _detectedTargets = new List<Transform>();
        private readonly Dictionary<Transform, float> _targetScores = new Dictionary<Transform, float>();
        private readonly Dictionary<Transform, TargetInfo> _targetInfoCache = new Dictionary<Transform, TargetInfo>();
        private readonly List<Transform> _targetsToRemove = new List<Transform>();

        // Services
        private IServiceContainer _services;
        private IEventBus _eventBus;

        // Performance tracking
        private float _lastUpdateTime;
        private float _frameTime;
        private int _targetsProcessedThisFrame;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public Transform CurrentTarget => _currentTarget;
        public IReadOnlyList<Transform> DetectedTargets => _detectedTargets.AsReadOnly();
        public int TargetCount => _detectedTargets.Count;
        public bool HasTargets => _detectedTargets.Count > 0;

        public float DetectionRadius
        {
            get => _detection.detectionRadius;
            set => _detection.detectionRadius = Mathf.Max(0.1f, value);
        }

        public float MaxDetectionDistance
        {
            get => _detection.maxDetectionDistance;
            set => _detection.maxDetectionDistance = Mathf.Max(0.1f, value);
        }

        public LayerMask TargetLayers
        {
            get => _detection.targetLayers;
            set => _detection.targetLayers = value;
        }

        public LayerMask ObstacleLayers
        {
            get => _detection.obstacleLayers;
            set => _detection.obstacleLayers = value;
        }

        public bool PrioritizeClosest
        {
            get => _prioritization.prioritizeClosest;
            set => _prioritization.prioritizeClosest = value;
        }

        #endregion

        #region Events

        public event Action<Transform> OnTargetChanged;
        public event Action<Transform> OnTargetDetected;
        public event Action<Transform> OnTargetLost;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_playerCamera == null)
                _playerCamera = UnityEngine.Camera.main;

            InitializeCollections();
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                // Try to auto-resolve services if not manually initialized
                var serviceProvider = GlobalServiceProvider.Instance;
                if (serviceProvider != null)
                {
                    Initialize(serviceProvider);
                }
            }
        }

        private void Update()
        {
            if (!_isActive || !_isInitialized) return;

            if (Time.time - _lastUpdateTime >= _performance.updateInterval)
            {
                _frameTime = Time.realtimeSinceStartup;
                _targetsProcessedThisFrame = 0;

                UpdateTargetDetection();
                UpdateTargetSelection();
                
                _lastUpdateTime = Time.time;

                if (_performance.enablePerformanceMetrics)
                {
                    LogPerformanceMetrics();
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region ITargetSelector Implementation

        public void SetActive(bool active)
        {
            if (_isActive == active) return;

            _isActive = active;

            if (!_isActive)
            {
                ClearAllTargets();
            }
        }

        public void Initialize(IServiceContainer services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            if (_services.TryResolve<IEventBus>(out _eventBus))
            {
                Debug.Log("[UnifiedTargetSelector] Event bus resolved successfully");
            }

            _isInitialized = true;
        }

        public void ForceTargetUpdate()
        {
            _lastUpdateTime = 0f;
            UpdateTargetDetection();
            UpdateTargetSelection();
        }

        public void SetCurrentTarget(Transform target)
        {
            if (_currentTarget == target) return;

            if (target != null && !_detectedTargets.Contains(target))
            {
                Debug.LogWarning($"[UnifiedTargetSelector] Attempted to set target {target.name} that is not detected");
                return;
            }

            var previousTarget = _currentTarget;
            _currentTarget = target;

            OnTargetChanged?.Invoke(_currentTarget);
            
            if (_eventBus != null && target != null)
            {
                var selectedEvent = new TargetSelectedEvent(transform, target, previousTarget, 
                    Vector3.Distance(transform.position, target.position));
                _eventBus.Publish(selectedEvent);
            }
        }

        public void ClearCurrentTarget()
        {
            SetCurrentTarget(null);
        }

        public Transform GetClosestTarget()
        {
            if (_detectedTargets.Count == 0) return null;

            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var target in _detectedTargets)
            {
                if (target == null) continue;

                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        public Transform GetHighestPriorityTarget()
        {
            if (_detectedTargets.Count == 0) return null;

            if (_prioritization.prioritizeClosest)
            {
                return GetClosestTarget();
            }

            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var target in _detectedTargets)
            {
                if (target == null) continue;

                if (_targetScores.TryGetValue(target, out float score) && score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        public List<Transform> GetTargetsWithinDistance(float distance)
        {
            var targets = new List<Transform>();
            
            foreach (var target in _detectedTargets)
            {
                if (target == null) continue;

                if (Vector3.Distance(transform.position, target.position) <= distance)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        public bool ValidateTarget(Transform target)
        {
            if (target == null || target == transform) return false;

            // Check distance
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > _detection.maxDetectionDistance) return false;

            // Check layer
            if ((_detection.targetLayers.value & (1 << target.gameObject.layer)) == 0) return false;

            // Check minimum size
            if (_detection.minTargetSize > 0f)
            {
                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    float size = renderer.bounds.size.magnitude;
                    if (size < _detection.minTargetSize) return false;
                }
            }

            // Check line of sight
            if (_detection.validateLineOfSight)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                
                if (Physics.Raycast(transform.position, direction, distance, _detection.obstacleLayers))
                {
                    return false;
                }
            }

            return true;
        }

        public float CalculateTargetScore(Transform target)
        {
            if (target == null) return 0f;

            var targetInfo = GetOrCreateTargetInfo(target);
            float score = 0f;

            // Distance score (closer is better)
            float distanceScore = 1f - (targetInfo.Distance / _detection.maxDetectionDistance);
            score += distanceScore * _prioritization.distanceWeight;

            // Angle score (more forward-facing is better)
            float angleScore = 1f - (targetInfo.Angle / 180f);
            score += angleScore * _prioritization.angleWeight;

            // Visibility/camera score
            if (_playerCamera != null && _prioritization.useCameraCenterBias)
            {
                Vector3 screenPoint = _playerCamera.WorldToViewportPoint(target.position);
                float screenCenterDistance = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), Vector2.one * 0.5f);
                float visibilityScore = 1f - screenCenterDistance;
                score += visibilityScore * _prioritization.visibilityWeight * _prioritization.screenCenterWeight;
            }

            targetInfo.Score = score;
            return score;
        }

        public void Dispose()
        {
            ClearAllTargets();
            
            OnTargetChanged = null;
            OnTargetDetected = null;
            OnTargetLost = null;

            _detectedTargets?.Clear();
            _targetScores?.Clear();
            _targetInfoCache?.Clear();
            _targetsToRemove?.Clear();
        }

        #endregion

        #region Public Configuration Methods

        /// <summary>
        /// Updates detection settings at runtime
        /// </summary>
        public void UpdateDetectionSettings(DetectionSettings newSettings)
        {
            _detection = newSettings;
            ForceTargetUpdate();
        }

        /// <summary>
        /// Updates prioritization settings at runtime
        /// </summary>
        public void UpdatePrioritizationSettings(PrioritizationSettings newSettings)
        {
            _prioritization = newSettings;
            UpdateTargetScores();
        }

        /// <summary>
        /// Updates performance settings at runtime
        /// </summary>
        public void UpdatePerformanceSettings(PerformanceSettings newSettings)
        {
            _performance = newSettings;
        }

        /// <summary>
        /// Gets current detection settings
        /// </summary>
        public DetectionSettings GetDetectionSettings() => _detection;

        /// <summary>
        /// Gets current prioritization settings
        /// </summary>
        public PrioritizationSettings GetPrioritizationSettings() => _prioritization;

        /// <summary>
        /// Gets current performance settings
        /// </summary>
        public PerformanceSettings GetPerformanceSettings() => _performance;

        #endregion

        #region Private Methods

        private void InitializeCollections()
        {
            _detectedTargets.Clear();
            _targetScores.Clear();
            _targetInfoCache.Clear();
            _targetsToRemove.Clear();
        }

        private void UpdateTargetDetection()
        {
            _targetsToRemove.Clear();

            // Remove invalid targets
            for (int i = _detectedTargets.Count - 1; i >= 0; i--)
            {
                var target = _detectedTargets[i];
                if (target == null || !ValidateTarget(target))
                {
                    _targetsToRemove.Add(target);
                }
            }

            foreach (var target in _targetsToRemove)
            {
                RemoveTarget(target);
            }

            // Detect new targets
            if (_detection.useProximityDetection)
            {
                DetectTargetsByProximity();
            }

            if (_detection.useRaycastDetection && _playerCamera != null)
            {
                DetectTargetsByRaycast();
            }

            // Update target scores
            UpdateTargetScores();
        }

        private void DetectTargetsByProximity()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _detection.detectionRadius, _detection.targetLayers);

            foreach (var hit in hits)
            {
                if (_targetsProcessedThisFrame >= _performance.maxTargetsPerFrame) break;

                if (hit.transform == transform) continue;
                if (_detectedTargets.Contains(hit.transform)) continue;

                if (ValidateTarget(hit.transform))
                {
                    AddTarget(hit.transform);
                    _targetsProcessedThisFrame++;
                }
            }
        }

        private void DetectTargetsByRaycast()
        {
            // Cast rays from camera center and nearby points for better coverage
            Vector3[] rayDirections = {
                _playerCamera.transform.forward,
                _playerCamera.ViewportPointToRay(new Vector3(0.3f, 0.5f, 0f)).direction,
                _playerCamera.ViewportPointToRay(new Vector3(0.7f, 0.5f, 0f)).direction,
                _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.3f, 0f)).direction,
                _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.7f, 0f)).direction,
            };

            foreach (var direction in rayDirections)
            {
                if (_targetsProcessedThisFrame >= _performance.maxTargetsPerFrame) break;

                RaycastHit[] hits = Physics.RaycastAll(_playerCamera.transform.position, 
                    direction, _detection.maxDetectionDistance, _detection.targetLayers);

                foreach (var hit in hits)
                {
                    if (_targetsProcessedThisFrame >= _performance.maxTargetsPerFrame) break;

                    if (hit.transform == transform) continue;
                    if (_detectedTargets.Contains(hit.transform)) continue;

                    if (ValidateTarget(hit.transform))
                    {
                        AddTarget(hit.transform);
                        _targetsProcessedThisFrame++;
                    }
                }
            }
        }

        private void UpdateTargetScores()
        {
            foreach (var target in _detectedTargets)
            {
                if (target == null) continue;

                float score = CalculateTargetScore(target);
                _targetScores[target] = score;
            }
        }

        private void UpdateTargetSelection()
        {
            if (_detectedTargets.Count == 0)
            {
                if (_currentTarget != null)
                {
                    SetCurrentTarget(null);
                }
                return;
            }

            // Auto-select best target if none is currently selected or current target is no longer valid
            if (_currentTarget == null || !_detectedTargets.Contains(_currentTarget))
            {
                var bestTarget = GetHighestPriorityTarget();
                SetCurrentTarget(bestTarget);
            }
        }

        private void AddTarget(Transform target)
        {
            if (target == null || _detectedTargets.Contains(target)) return;

            _detectedTargets.Add(target);
            _targetInfoCache[target] = CreateTargetInfo(target);

            OnTargetDetected?.Invoke(target);
            
            if (_eventBus != null)
            {
                var detectedEvent = new TargetDetectedEvent(transform, target, 
                    Vector3.Distance(transform.position, target.position), 
                    CalculateTargetScore(target));
                _eventBus.Publish(detectedEvent);
            }
        }

        private void RemoveTarget(Transform target)
        {
            if (target == null || !_detectedTargets.Contains(target)) return;

            _detectedTargets.Remove(target);
            _targetScores.Remove(target);
            _targetInfoCache.Remove(target);

            if (_currentTarget == target)
            {
                SetCurrentTarget(null);
            }

            OnTargetLost?.Invoke(target);

            if (_eventBus != null)
            {
                var lostEvent = new TargetLostEvent(transform, target, "Validation failed or out of range");
                _eventBus.Publish(lostEvent);
            }
        }

        private void ClearAllTargets()
        {
            var targetsToRemove = new List<Transform>(_detectedTargets);
            foreach (var target in targetsToRemove)
            {
                RemoveTarget(target);
            }
        }

        private TargetInfo CreateTargetInfo(Transform target)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            
            return new TargetInfo
            {
                Distance = Vector3.Distance(transform.position, target.position),
                Angle = Vector3.Angle(transform.forward, directionToTarget),
                LastKnownPosition = target.position,
                LastUpdateTime = Time.time,
                Score = 0f
            };
        }

        private TargetInfo GetOrCreateTargetInfo(Transform target)
        {
            if (_targetInfoCache.TryGetValue(target, out TargetInfo info))
            {
                // Update info if it's stale
                if (Time.time - info.LastUpdateTime > _performance.updateInterval)
                {
                    Vector3 directionToTarget = (target.position - transform.position).normalized;
                    info.Distance = Vector3.Distance(transform.position, target.position);
                    info.Angle = Vector3.Angle(transform.forward, directionToTarget);
                    info.LastKnownPosition = target.position;
                    info.LastUpdateTime = Time.time;
                }
                return info;
            }

            var newInfo = CreateTargetInfo(target);
            _targetInfoCache[target] = newInfo;
            return newInfo;
        }

        private void LogPerformanceMetrics()
        {
            float frameProcessingTime = (Time.realtimeSinceStartup - _frameTime) * 1000f; // Convert to milliseconds
            
            if (frameProcessingTime > 1f) // Log if processing takes more than 1ms
            {
                Debug.Log($"[UnifiedTargetSelector] Frame processing: {frameProcessingTime:F2}ms, " +
                         $"Targets: {_detectedTargets.Count}, Processed: {_targetsProcessedThisFrame}");
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detection.detectionRadius);

            // Draw max detection distance
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _detection.maxDetectionDistance);

            // Draw current target line
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);

                // Draw target score as text
                if (_targetScores.TryGetValue(_currentTarget, out float score))
                {
                    UnityEditor.Handles.Label(_currentTarget.position + Vector3.up, $"Score: {score:F2}");
                }
            }

            // Draw detected targets
            Gizmos.color = Color.cyan;
            foreach (var target in _detectedTargets)
            {
                if (target != _currentTarget && target != null)
                {
                    Gizmos.DrawWireSphere(target.position, 0.2f);
                    
                    // Draw target scores
                    if (_targetScores.TryGetValue(target, out float score))
                    {
                        UnityEditor.Handles.Label(target.position + Vector3.up * 0.5f, $"{score:F1}");
                    }
                }
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            _detection.detectionRadius = Mathf.Max(0.1f, _detection.detectionRadius);
            _detection.maxDetectionDistance = Mathf.Max(0.1f, _detection.maxDetectionDistance);
            _detection.minTargetSize = Mathf.Max(0.1f, _detection.minTargetSize);
            
            _performance.updateInterval = Mathf.Max(0.05f, _performance.updateInterval);
            _performance.maxTargetsPerFrame = Mathf.Max(1, _performance.maxTargetsPerFrame);
        }
#endif

        #endregion
    }
}
