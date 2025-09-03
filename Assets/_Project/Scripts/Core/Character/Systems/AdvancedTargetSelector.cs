using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Character.Events;

namespace Laboratory.Core.Character.Systems
{
    /// <summary>
    /// Advanced target selection system with multiple detection modes and intelligent scoring.
    /// Replaces the old TargetSelector with enhanced features and event integration.
    /// </summary>
    public class AdvancedTargetSelector : MonoBehaviour, ITargetSelector
    {
        #region Nested Types

        private class TargetInfo
        {
            public float Distance { get; set; }
            public float Angle { get; set; }
            public Vector3 LastKnownPosition { get; set; }
            public float LastUpdateTime { get; set; }
        }

        #endregion

        #region Fields

        [Header("Detection Settings")]
        [SerializeField, Range(0.5f, 20f)]
        private float _detectionRadius = 10f;

        [SerializeField, Range(1f, 50f)]
        private float _maxDetectionDistance = 15f;

        [SerializeField]
        private LayerMask _targetLayers = -1;

        [SerializeField]
        private bool _useProximityDetection = true;

        [SerializeField]
        private bool _useRaycastDetection = true;

        [SerializeField]
        private bool _validateLineOfSight = true;

        [Header("Target Prioritization")]
        [SerializeField, Range(0f, 2f)]
        private float _distanceWeight = 1f;

        [SerializeField, Range(0f, 2f)]
        private float _angleWeight = 0.5f;

        [SerializeField, Range(0f, 2f)]
        private float _visibilityWeight = 0.8f;

        [SerializeField]
        private bool _prioritizeClosest = true;

        [Header("Camera Integration")]
        [SerializeField]
        private UnityEngine.Camera _playerCamera;

        [SerializeField, Range(0f, 1f)]
        private float _screenCenterWeight = 0.3f;

        [SerializeField]
        private bool _useCameraCenterBias = true;

        [Header("Performance")]
        [SerializeField, Range(0.05f, 1f)]
        private float _updateInterval = 0.1f;

        [SerializeField, Range(1, 50)]
        private int _maxTargetsPerFrame = 10;

        [SerializeField]
        private LayerMask _obstacleLayers = -1;

        [SerializeField, Range(0.1f, 2f)]
        private float _minTargetSize = 0.5f;

        // Runtime state
        private bool _isActive = true;
        private bool _isInitialized = false;
        private Transform _currentTarget;
        private List<Transform> _detectedTargets = new List<Transform>();
        private Dictionary<Transform, float> _targetScores = new Dictionary<Transform, float>();
        private Dictionary<Transform, TargetInfo> _targetInfoCache = new Dictionary<Transform, TargetInfo>();

        // Services
        private IServiceContainer _services;
        private IEventBus _eventBus;

        // Performance tracking
        private float _lastUpdateTime;
        private List<Transform> _targetsToRemove = new List<Transform>();

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public Transform CurrentTarget => _currentTarget;
        public IReadOnlyList<Transform> DetectedTargets => _detectedTargets.AsReadOnly();
        public int TargetCount => _detectedTargets.Count;
        public bool HasTargets => _detectedTargets.Count > 0;

        public float DetectionRadius
        {
            get => _detectionRadius;
            set => _detectionRadius = Mathf.Max(0.1f, value);
        }

        public float MaxDetectionDistance
        {
            get => _maxDetectionDistance;
            set => _maxDetectionDistance = Mathf.Max(0.1f, value);
        }

        public LayerMask TargetLayers
        {
            get => _targetLayers;
            set => _targetLayers = value;
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

            _detectedTargets = new List<Transform>();
            _targetScores = new Dictionary<Transform, float>();
            _targetInfoCache = new Dictionary<Transform, TargetInfo>();
            _targetsToRemove = new List<Transform>();
        }

        private void Update()
        {
            if (!_isActive || !_isInitialized) return;

            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateTargetDetection();
                UpdateTargetSelection();
                _lastUpdateTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region ICharacterController Implementation

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
                // Successfully resolved event bus
            }

            _isInitialized = true;
        }

        public void UpdateController()
        {
            // Update logic is handled in Update()
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

        #region ITargetSelector Implementation

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
                Debug.LogWarning($"[AdvancedTargetSelector] Attempted to set target {target.name} that is not detected");
                return;
            }

            var previousTarget = _currentTarget;
            _currentTarget = target;

            OnTargetChanged?.Invoke(_currentTarget);
            
            if (previousTarget != null && target != null)
            {
                PublishTargetSelectedEvent(target, previousTarget);
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

            if (_prioritizeClosest)
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
            if (distance > _maxDetectionDistance) return false;

            // Check layer
            if ((_targetLayers.value & (1 << target.gameObject.layer)) == 0) return false;

            // Check minimum size
            if (_minTargetSize > 0f)
            {
                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    float size = renderer.bounds.size.magnitude;
                    if (size < _minTargetSize) return false;
                }
            }

            // Check line of sight
            if (_validateLineOfSight)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                
                if (Physics.Raycast(transform.position, direction, distance, _obstacleLayers))
                {
                    return false;
                }
            }

            return true;
        }

        public float CalculateTargetScore(Transform target)
        {
            if (target == null) return 0f;

            float score = 0f;
            var targetInfo = GetOrCreateTargetInfo(target);

            // Distance score (closer is better)
            float distanceScore = 1f - (targetInfo.Distance / _maxDetectionDistance);
            score += distanceScore * _distanceWeight;

            // Angle score (more forward-facing is better)
            float angleScore = 1f - (targetInfo.Angle / 180f);
            score += angleScore * _angleWeight;

            // Visibility score (more visible is better)
            if (_playerCamera != null && _useCameraCenterBias)
            {
                Vector3 screenPoint = _playerCamera.WorldToViewportPoint(target.position);
                float screenCenterDistance = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), Vector2.one * 0.5f);
                float visibilityScore = 1f - screenCenterDistance;
                score += visibilityScore * _visibilityWeight * _screenCenterWeight;
            }

            return score;
        }

        #endregion

        #region Private Methods

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
            if (_useProximityDetection)
            {
                DetectTargetsByProximity();
            }

            if (_useRaycastDetection)
            {
                DetectTargetsByRaycast();
            }

            // Update target scores
            UpdateTargetScores();
        }

        private void DetectTargetsByProximity()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _targetLayers);
            int processed = 0;

            foreach (var hit in hits)
            {
                if (processed >= _maxTargetsPerFrame) break;

                if (hit.transform == transform) continue;
                if (_detectedTargets.Contains(hit.transform)) continue;

                if (ValidateTarget(hit.transform))
                {
                    AddTarget(hit.transform);
                    processed++;
                }
            }
        }

        private void DetectTargetsByRaycast()
        {
            if (_playerCamera == null) return;

            // Cast rays from camera center and nearby points
            Vector3[] rayDirections = {
                _playerCamera.transform.forward,
                _playerCamera.ViewportPointToRay(new Vector3(0.3f, 0.5f, 0f)).direction,
                _playerCamera.ViewportPointToRay(new Vector3(0.7f, 0.5f, 0f)).direction,
                _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.3f, 0f)).direction,
                _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.7f, 0f)).direction,
            };

            int processed = 0;
            foreach (var direction in rayDirections)
            {
                if (processed >= _maxTargetsPerFrame) break;

                RaycastHit[] hits = Physics.RaycastAll(_playerCamera.transform.position, 
                    direction, _maxDetectionDistance, _targetLayers);

                foreach (var hit in hits)
                {
                    if (processed >= _maxTargetsPerFrame) break;

                    if (hit.transform == transform) continue;
                    if (_detectedTargets.Contains(hit.transform)) continue;

                    if (ValidateTarget(hit.transform))
                    {
                        AddTarget(hit.transform);
                        processed++;
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

            // Auto-select best target if none is currently selected
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
            
            var detectedEvent = new TargetDetectedEvent(transform, target, 
                Vector3.Distance(transform.position, target.position), 
                CalculateTargetScore(target));
            _eventBus?.Publish(detectedEvent);
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

            var lostEvent = new TargetLostEvent(transform, target, "Out of range or occluded");
            _eventBus?.Publish(lostEvent);
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
            return new TargetInfo
            {
                Distance = Vector3.Distance(transform.position, target.position),
                Angle = Vector3.Angle(transform.forward, (target.position - transform.position).normalized),
                LastKnownPosition = target.position,
                LastUpdateTime = Time.time
            };
        }

        private TargetInfo GetOrCreateTargetInfo(Transform target)
        {
            if (_targetInfoCache.TryGetValue(target, out TargetInfo info))
            {
                // Update info if it's stale
                if (Time.time - info.LastUpdateTime > _updateInterval)
                {
                    info.Distance = Vector3.Distance(transform.position, target.position);
                    info.Angle = Vector3.Angle(transform.forward, (target.position - transform.position).normalized);
                    info.LastKnownPosition = target.position;
                    info.LastUpdateTime = Time.time;
                }
                return info;
            }

            return CreateTargetInfo(target);
        }

        private void PublishTargetSelectedEvent(Transform newTarget, Transform previousTarget)
        {
            float distance = newTarget != null ? Vector3.Distance(transform.position, newTarget.position) : 0f;
            var selectedEvent = new TargetSelectedEvent(transform, newTarget, previousTarget, distance);
            _eventBus?.Publish(selectedEvent);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Draw max detection distance
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _maxDetectionDistance);

            // Draw current target line
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);
            }

            // Draw detected targets
            Gizmos.color = Color.cyan;
            foreach (var target in _detectedTargets)
            {
                if (target != _currentTarget && target != null)
                {
                    Gizmos.DrawWireSphere(target.position, 0.2f);
                }
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            _detectionRadius = Mathf.Max(0.1f, _detectionRadius);
            _maxDetectionDistance = Mathf.Max(0.1f, _maxDetectionDistance);
            _updateInterval = Mathf.Max(0.05f, _updateInterval);
            _maxTargetsPerFrame = Mathf.Max(1, _maxTargetsPerFrame);
            _minTargetSize = Mathf.Max(0.1f, _minTargetSize);
        }
#endif

        #endregion
    }
}
