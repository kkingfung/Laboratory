using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Character.Configuration;
using Laboratory.Core.Character.Events;
using Laboratory.Core.Character.Systems;

namespace Laboratory.Core.Character.Controllers
{
    /// <summary>
    /// Unified aim controller that consolidates head, chest, and upper body aiming.
    /// Uses IK fallback when Animation Rigging constraints are not available.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class UnifiedAimController : MonoBehaviour, IAimController
    {
        #region Fields

        [Header("Configuration")]
        [SerializeField, Tooltip("Aim settings configuration")]
        private CharacterAimSettings _aimSettings;

        [Header("Target Selection")]
        [SerializeField, Tooltip("Integrated target selector component")]
        private AdvancedTargetSelector _targetSelector;

        [SerializeField, Tooltip("Enable automatic target selection")]
        private bool _autoTargeting = true;

        [Header("IK Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Overall look weight")]
        private float _lookWeight = 1f;
        
        [SerializeField, Range(0f, 1f), Tooltip("Body weight")]
        private float _bodyWeight = 0.3f;
        
        [SerializeField, Range(0f, 1f), Tooltip("Head weight")]
        private float _headWeight = 0.8f;
        
        [SerializeField, Range(0f, 1f), Tooltip("Eyes weight")]
        private float _eyesWeight = 1f;

        // Runtime state
        private bool _isActive = true;
        private bool _isInitialized = false;
        private Transform _currentTarget;
        private float _currentAimWeight = 0f;
        private float _targetAimWeight = 0f;
        
        // Services
        private IServiceContainer _services;
        private IEventBus _eventBus;
        private Animator _animator;
        
        // IK state
        private bool _wasAiming = false;
        
        // Performance tracking
        private float _lastTargetUpdateTime;
        private const float TARGET_UPDATE_INTERVAL = 0.1f;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public Transform CurrentTarget => _currentTarget;
        public bool IsAiming => _currentTarget != null && _currentAimWeight > 0.01f;
        public float AimWeight => _currentAimWeight;
        public float MaxAimDistance 
        { 
            get => _aimSettings?.maxAimDistance ?? 15f;
            set 
            {
                if (_aimSettings != null) _aimSettings.maxAimDistance = value;
                if (_targetSelector != null) _targetSelector.MaxDetectionDistance = value;
            }
        }

        #endregion

        #region Events

        public event Action<Transform> OnTargetAcquired;
        public event Action<Transform> OnTargetLost;
        public event Action<float> OnAimWeightChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError($"[UnifiedAimController] Animator component required on {name}");
                enabled = false;
                return;
            }

            ValidateComponents();
        }

        private void Start()
        {
            // Initialize with default settings if none provided
            if (_aimSettings == null)
            {
                _aimSettings = CharacterAimSettings.CreateDefault();
                Debug.LogWarning($"[UnifiedAimController] No aim settings assigned to {name}, using defaults");
            }

            if (_aimSettings != null)
            {
                _aimSettings.ValidateSettings();
            }
        }

        private void Update()
        {
            if (!_isActive || !_isInitialized) return;

            UpdateTargeting();
            UpdateAimWeight();
            UpdateIKState();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || !IsAiming) return;

            ApplyIKLook();
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
                ClearTarget();
            }
        }

        public void Initialize(IServiceContainer services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            
            if (_services.TryResolve<IEventBus>(out _eventBus))
            {
                // Successfully resolved event bus
            }

            if (_targetSelector != null)
            {
                _targetSelector.Initialize(services);
                _targetSelector.OnTargetChanged += OnTargetSelectorTargetChanged;
            }

            _isInitialized = true;
        }

        public void UpdateController()
        {
            if (!_isActive) return;
            
            // This is called from Update(), actual logic is there
        }

        public void Dispose()
        {
            if (_targetSelector != null)
            {
                _targetSelector.OnTargetChanged -= OnTargetSelectorTargetChanged;
                _targetSelector.Dispose();
            }

            OnTargetAcquired = null;
            OnTargetLost = null;
            OnAimWeightChanged = null;
        }

        #endregion

        #region IAimController Implementation

        public void SetTarget(Transform target)
        {
            if (_currentTarget == target) return;

            var previousTarget = _currentTarget;
            _currentTarget = target;

            if (target != null)
            {
                _targetAimWeight = _aimSettings?.headWeight ?? 1f;
                PublishAimStartedEvent(target);
                OnTargetAcquired?.Invoke(target);
            }
            else
            {
                _targetAimWeight = 0f;
                PublishAimStoppedEvent(previousTarget);
                OnTargetLost?.Invoke(previousTarget);
            }
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }

        public void SetAimWeight(float weight)
        {
            float clampedWeight = Mathf.Clamp01(weight);
            if (Mathf.Approximately(_targetAimWeight, clampedWeight)) return;

            float previousWeight = _targetAimWeight;
            _targetAimWeight = clampedWeight;

            PublishAimWeightChangedEvent(previousWeight, clampedWeight);
            OnAimWeightChanged?.Invoke(clampedWeight);
        }

        public void SetAutoTargeting(bool enabled)
        {
            _autoTargeting = enabled;
            
            if (_targetSelector != null)
            {
                _targetSelector.SetActive(enabled);
            }
        }

        public bool IsValidTarget(Transform target)
        {
            if (target == null || target == transform) return false;
            
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > MaxAimDistance) return false;

            if (_aimSettings?.clampRotation == true)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToTarget);
                if (angle > _aimSettings.maxHeadAngle) return false;
            }

            return true;
        }

        #endregion

        #region Private Methods

        private void ValidateComponents()
        {
            if (_targetSelector == null)
            {
                _targetSelector = GetComponent<AdvancedTargetSelector>();
                if (_targetSelector == null)
                {
                    Debug.LogWarning($"[UnifiedAimController] No target selector found on {name}. Auto-targeting disabled.");
                    _autoTargeting = false;
                }
            }
        }

        private void UpdateTargeting()
        {
            if (!_autoTargeting || _targetSelector == null) return;
            
            // Throttle target updates for performance
            if (Time.time - _lastTargetUpdateTime < TARGET_UPDATE_INTERVAL) return;
            _lastTargetUpdateTime = Time.time;

            // Target selector handles its own updates
        }

        private void UpdateAimWeight()
        {
            if (_aimSettings == null) return;

            // Smooth weight transitions
            float targetWeight = _currentTarget != null ? _targetAimWeight : 0f;
            
            if (_aimSettings.smoothBlending)
            {
                _currentAimWeight = Mathf.Lerp(_currentAimWeight, targetWeight, 
                    Time.deltaTime * _aimSettings.blendSpeed);
            }
            else
            {
                _currentAimWeight = targetWeight;
            }
        }

        private void UpdateIKState()
        {
            // Track aiming state changes for IK
            bool isAimingNow = IsAiming;
            if (_wasAiming != isAimingNow)
            {
                _wasAiming = isAimingNow;
            }
        }

        private void ApplyIKLook()
        {
            if (_currentTarget == null) return;

            float finalWeight = _currentAimWeight * _lookWeight;
            
            _animator.SetLookAtWeight(
                finalWeight,
                _bodyWeight,
                _headWeight,
                _eyesWeight,
                0.5f
            );
            
            _animator.SetLookAtPosition(_currentTarget.position);
        }

        private void OnTargetSelectorTargetChanged(Transform newTarget)
        {
            if (_autoTargeting)
            {
                SetTarget(newTarget);
            }
        }

        private void PublishAimStartedEvent(Transform target)
        {
            var aimEvent = new CharacterAimStartedEvent(transform, target);
            _eventBus?.Publish(aimEvent);
        }

        private void PublishAimStoppedEvent(Transform previousTarget)
        {
            var aimEvent = new CharacterAimStoppedEvent(transform, previousTarget);
            _eventBus?.Publish(aimEvent);
        }

        private void PublishAimWeightChangedEvent(float previousWeight, float newWeight)
        {
            var weightEvent = new CharacterAimWeightChangedEvent(transform, previousWeight, newWeight);
            _eventBus?.Publish(weightEvent);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);
            }

            // Draw aim distance
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, MaxAimDistance);

            // Draw rotation limits if enabled
            if (_aimSettings?.clampRotation == true)
            {
                Gizmos.color = Color.green;
                DrawRotationCone(transform.position, transform.forward, _aimSettings.maxHeadAngle, MaxAimDistance);
            }
        }

        private void DrawRotationCone(Vector3 position, Vector3 direction, float angle, float distance)
        {
            int segments = 16;
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float currentAngle = Mathf.Lerp(-angle, angle, t);
                Vector3 rotatedDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * direction;
                points[i] = position + rotatedDirection * distance;
            }
            
            for (int i = 0; i < segments; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [Header("Debug Info")]
        [SerializeField, Tooltip("Show debug information in inspector")]
        #pragma warning disable CS0414 // Field assigned but never used - reserved for future debug UI feature
        private bool _showDebugInfo = true;
        #pragma warning restore CS0414

        private void OnValidate()
        {
            if (_aimSettings != null)
            {
                _aimSettings.ValidateSettings();
            }
        }
#endif

        #endregion
    }
}