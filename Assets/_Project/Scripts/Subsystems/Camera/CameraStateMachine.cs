using UnityEngine;
using System.Collections;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// State machine for managing camera mode transitions
    /// Smooth blending between different camera perspectives
    /// </summary>
    public class CameraStateMachine : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraConfig config;

        // State
        private CameraMode _currentMode;
        private CameraMode _previousMode;
        private bool _isTransitioning = false;
        private Coroutine _transitionCoroutine;

        // Transition data
        private struct CameraState
        {
            public Vector3 position;
            public Quaternion rotation;
            public float fieldOfView;
        }

        private CameraState _currentState;
        private CameraState _targetState;

        // References
        private Transform _cameraTransform;
        private UnityEngine.Camera _camera;

        // Events
        public event System.Action<CameraMode> OnModeChanged;
        public event System.Action<CameraMode, CameraMode> OnTransitionStarted;
        public event System.Action OnTransitionCompleted;

        private void Awake()
        {
            _cameraTransform = transform;
            _camera = GetComponent<UnityEngine.Camera>();

            if (config != null)
            {
                _currentMode = config.DefaultMode;
            }

            CaptureCurrentState();
        }

        /// <summary>
        /// Transition to a new camera mode
        /// </summary>
        public void TransitionTo(CameraMode newMode, float? customDuration = null)
        {
            if (newMode == _currentMode && !_isTransitioning)
            {
                return;
            }

            _previousMode = _currentMode;
            _currentMode = newMode;

            float duration = customDuration ?? (config != null ? config.TransitionDuration : 0.5f);

            OnTransitionStarted?.Invoke(_previousMode, newMode);

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _transitionCoroutine = StartCoroutine(TransitionCoroutine(newMode, duration));
        }

        /// <summary>
        /// Instantly switch to a new camera mode without transition
        /// </summary>
        public void SwitchToImmediate(CameraMode newMode)
        {
            _previousMode = _currentMode;
            _currentMode = newMode;

            OnModeChanged?.Invoke(newMode);
        }

        /// <summary>
        /// Transition coroutine with smooth blending
        /// </summary>
        private IEnumerator TransitionCoroutine(CameraMode targetMode, float duration)
        {
            _isTransitioning = true;

            CaptureCurrentState();
            CameraState startState = _currentState;

            float elapsed = 0f;
            AnimationCurve curve = config != null ? config.TransitionCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(elapsed / duration);

                // Note: Actual target state would be calculated by CameraController
                // This is a placeholder blend
                _cameraTransform.position = Vector3.Lerp(startState.position, _targetState.position, t);
                _cameraTransform.rotation = Quaternion.Slerp(startState.rotation, _targetState.rotation, t);
                _camera.fieldOfView = Mathf.Lerp(startState.fieldOfView, _targetState.fieldOfView, t);

                yield return null;
            }

            _cameraTransform.position = _targetState.position;
            _cameraTransform.rotation = _targetState.rotation;
            _camera.fieldOfView = _targetState.fieldOfView;

            _isTransitioning = false;
            OnTransitionCompleted?.Invoke();
            OnModeChanged?.Invoke(targetMode);
        }

        /// <summary>
        /// Set target state for transition (called by CameraController)
        /// </summary>
        public void SetTargetState(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            _targetState = new CameraState
            {
                position = position,
                rotation = rotation,
                fieldOfView = fieldOfView
            };
        }

        /// <summary>
        /// Capture current camera state
        /// </summary>
        private void CaptureCurrentState()
        {
            _currentState = new CameraState
            {
                position = _cameraTransform.position,
                rotation = _cameraTransform.rotation,
                fieldOfView = _camera.fieldOfView
            };
        }

        /// <summary>
        /// Get current camera mode
        /// </summary>
        public CameraMode GetCurrentMode()
        {
            return _currentMode;
        }

        /// <summary>
        /// Get previous camera mode
        /// </summary>
        public CameraMode GetPreviousMode()
        {
            return _previousMode;
        }

        /// <summary>
        /// Check if camera is currently transitioning
        /// </summary>
        public bool IsTransitioning()
        {
            return _isTransitioning;
        }

        /// <summary>
        /// Revert to previous camera mode
        /// </summary>
        public void RevertToPrevious(float? customDuration = null)
        {
            TransitionTo(_previousMode, customDuration);
        }

        private void OnDestroy()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
        }
    }
}
