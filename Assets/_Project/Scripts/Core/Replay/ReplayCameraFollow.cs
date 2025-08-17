using UnityEngine;
using Unity.Cinemachine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Controls camera behavior during replay playback.
    /// Provides smooth camera following and cinematic shots for replay analysis.
    /// </summary>
    public class ReplayCameraFollow : MonoBehaviour
    {
        #region Fields

        [Header("Camera References")]
        [SerializeField] private CinemachineBrain _cinemachineBrain;
        [SerializeField] private CinemachineCamera _followCamera;
        [SerializeField] private CinemachineCamera _cinematicCamera;
        [SerializeField] private CinemachineCamera _overviewCamera;

        [Header("Follow Settings")]
        [SerializeField] private Transform _targetActor;
        [SerializeField] private float _followDistance = 5f;
        [SerializeField] private float _followHeight = 2f;
        [SerializeField] private float _followSpeed = 3f;
        [SerializeField] private bool _smoothFollow = true;

        [Header("Cinematic Settings")]
        [SerializeField] private float _cinematicBlendTime = 1f;
        [SerializeField] private bool _autoCinematicMode = false;
        [SerializeField] private float _cinematicTriggerDistance = 10f;
        [SerializeField] private float _cinematicDuration = 3f;

        [Header("Overview Settings")]
        [SerializeField] private float _overviewHeight = 15f;
        [SerializeField] private float _overviewDistance = 20f;
        [SerializeField] private bool _showOverviewOnStart = false;

        // Runtime state
        private CameraMode _currentMode = CameraMode.Follow;
        private float _modeBlendTimer = 0f;
        private Vector3 _lastTargetPosition;
        private bool _isTransitioning = false;

        #endregion

        #region Enums

        /// <summary>
        /// Available camera modes for replay viewing
        /// </summary>
        public enum CameraMode
        {
            /// <summary>Follow target actor closely</summary>
            Follow,
            /// <summary>Cinematic wide shots</summary>
            Cinematic,
            /// <summary>High overview of the scene</summary>
            Overview,
            /// <summary>Free camera movement</summary>
            Free
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current camera mode
        /// </summary>
        public CameraMode CurrentMode => _currentMode;

        /// <summary>
        /// Target actor being followed
        /// </summary>
        public Transform TargetActor => _targetActor;

        /// <summary>
        /// Whether camera is currently transitioning between modes
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Follow distance from target
        /// </summary>
        public float FollowDistance => _followDistance;

        /// <summary>
        /// Follow height above target
        /// </summary>
        public float FollowHeight => _followHeight;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCamera();
        }

        private void Start()
        {
            if (_showOverviewOnStart)
            {
                SetCameraMode(CameraMode.Overview);
            }
        }

        private void Update()
        {
            UpdateCameraMode();
            UpdateFollowBehavior();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the target actor for camera following
        /// </summary>
        /// <param name="target">Transform to follow</param>
        public void SetTargetActor(Transform target)
        {
            _targetActor = target;
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// Changes the camera mode
        /// </summary>
        /// <param name="mode">New camera mode</param>
        public void SetCameraMode(CameraMode mode)
        {
            if (_currentMode == mode) return;

            _currentMode = mode;
            _modeBlendTimer = 0f;
            _isTransitioning = true;

            ActivateCameraForMode(mode);
        }

        /// <summary>
        /// Sets the follow distance
        /// </summary>
        /// <param name="distance">Distance from target</param>
        public void SetFollowDistance(float distance)
        {
            _followDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// Sets the follow height
        /// </summary>
        /// <param name="height">Height above target</param>
        public void SetFollowHeight(float height)
        {
            _followHeight = Mathf.Max(0f, height);
        }

        /// <summary>
        /// Sets the follow speed
        /// </summary>
        /// <param name="speed">Follow speed multiplier</param>
        public void SetFollowSpeed(float speed)
        {
            _followSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Enables or disables smooth following
        /// </summary>
        /// <param name="enabled">Whether smooth following should be enabled</param>
        public void SetSmoothFollow(bool enabled)
        {
            _smoothFollow = enabled;
        }

        /// <summary>
        /// Sets the cinematic blend time
        /// </summary>
        /// <param name="blendTime">Blend duration in seconds</param>
        public void SetCinematicBlendTime(float blendTime)
        {
            _cinematicBlendTime = Mathf.Max(0.1f, blendTime);
        }

        /// <summary>
        /// Enables or disables automatic cinematic mode
        /// </summary>
        /// <param name="enabled">Whether automatic cinematic mode should be enabled</param>
        public void SetAutoCinematicMode(bool enabled)
        {
            _autoCinematicMode = enabled;
        }

        /// <summary>
        /// Sets the overview camera height
        /// </summary>
        /// <param name="height">Height above the scene</param>
        public void SetOverviewHeight(float height)
        {
            _overviewHeight = Mathf.Max(5f, height);
        }

        /// <summary>
        /// Sets the overview camera distance
        /// </summary>
        /// <param name="distance">Distance from scene center</param>
        public void SetOverviewDistance(float distance)
        {
            _overviewDistance = Mathf.Max(5f, distance);
        }

        /// <summary>
        /// Cycles to the next camera mode
        /// </summary>
        public void CycleToNextMode()
        {
            int currentIndex = (int)_currentMode;
            int nextIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(CameraMode)).Length;
            SetCameraMode((CameraMode)nextIndex);
        }

        /// <summary>
        /// Cycles to the previous camera mode
        /// </summary>
        public void CycleToPreviousMode()
        {
            int currentIndex = (int)_currentMode;
            int previousIndex = (currentIndex - 1 + System.Enum.GetValues(typeof(CameraMode)).Length) % System.Enum.GetValues(typeof(CameraMode)).Length;
            SetCameraMode((CameraMode)previousIndex);
        }

        #endregion

        #region Private Methods

        private void InitializeCamera()
        {
            if (_cinemachineBrain == null)
                _cinemachineBrain = GetComponent<CinemachineBrain>();

            if (_cinemachineBrain == null)
                _cinemachineBrain = FindObjectOfType<CinemachineBrain>();

            if (_cinemachineBrain == null)
            {
                Debug.LogError("ReplayCameraFollow: No CinemachineBrain found");
                enabled = false;
                return;
            }

            // Set initial camera mode
            SetCameraMode(_currentMode);
        }

        private void UpdateCameraMode()
        {
            if (_isTransitioning)
            {
                _modeBlendTimer += Time.deltaTime;
                if (_modeBlendTimer >= _cinematicBlendTime)
                {
                    _isTransitioning = false;
                    _modeBlendTimer = 0f;
                }
            }

            // Auto cinematic mode logic
            if (_autoCinematicMode && _targetActor != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _targetActor.position);
                if (distanceToTarget > _cinematicTriggerDistance && _currentMode == CameraMode.Follow)
                {
                    SetCameraMode(CameraMode.Cinematic);
                    StartCoroutine(ReturnToFollowAfterDelay(_cinematicDuration));
                }
            }
        }

        private void UpdateFollowBehavior()
        {
            if (_currentMode != CameraMode.Follow || _targetActor == null) return;

            Vector3 targetPosition = _targetActor.position;
            Vector3 desiredPosition = CalculateFollowPosition(targetPosition);

            if (_smoothFollow)
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * _followSpeed);
            }
            else
            {
                transform.position = desiredPosition;
            }

            // Look at target
            Vector3 lookDirection = (targetPosition - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * _followSpeed);
            }

            _lastTargetPosition = targetPosition;
        }

        private Vector3 CalculateFollowPosition(Vector3 targetPosition)
        {
            Vector3 offset = new Vector3(0, _followHeight, -_followDistance);
            return targetPosition + offset;
        }

        private void ActivateCameraForMode(CameraMode mode)
        {
            // Disable all cameras first
            if (_followCamera != null) _followCamera.gameObject.SetActive(false);
            if (_cinematicCamera != null) _cinematicCamera.gameObject.SetActive(false);
            if (_overviewCamera != null) _overviewCamera.gameObject.SetActive(false);

            // Enable the appropriate camera
            switch (mode)
            {
                case CameraMode.Follow:
                    if (_followCamera != null)
                    {
                        _followCamera.gameObject.SetActive(true);
                        SetupFollowCamera();
                    }
                    break;

                case CameraMode.Cinematic:
                    if (_cinematicCamera != null)
                    {
                        _cinematicCamera.gameObject.SetActive(true);
                        SetupCinematicCamera();
                    }
                    break;

                case CameraMode.Overview:
                    if (_overviewCamera != null)
                    {
                        _overviewCamera.gameObject.SetActive(true);
                        SetupOverviewCamera();
                    }
                    break;

                case CameraMode.Free:
                    // Free camera mode - no specific camera needed
                    break;
            }
        }

        private void SetupFollowCamera()
        {
            if (_followCamera == null || _targetActor == null) return;

            // Configure follow camera settings
            var follow = _followCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (follow != null)
            {
                follow.m_CameraDistance = _followDistance;
                follow.m_CameraHeight = _followHeight;
            }
        }

        private void SetupCinematicCamera()
        {
            if (_cinematicCamera == null) return;

            // Configure cinematic camera for wide shots
            var composer = _cinematicCamera.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_TrackedObjectOffset = new Vector3(0, 2f, 0);
            }
        }

        private void SetupOverviewCamera()
        {
            if (_overviewCamera == null) return;

            // Position overview camera above the scene
            Vector3 overviewPosition = Vector3.up * _overviewHeight;
            if (_targetActor != null)
            {
                overviewPosition += _targetActor.position;
            }

            _overviewCamera.transform.position = overviewPosition;
            _overviewCamera.transform.LookAt(_targetActor != null ? _targetActor.position : Vector3.zero);
        }

        private System.Collections.IEnumerator ReturnToFollowAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_currentMode == CameraMode.Cinematic)
            {
                SetCameraMode(CameraMode.Follow);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_targetActor != null)
            {
                // Draw follow area
                Gizmos.color = Color.blue;
                Vector3 followPosition = CalculateFollowPosition(_targetActor.position);
                Gizmos.DrawWireSphere(followPosition, 0.5f);
                Gizmos.DrawLine(_targetActor.position, followPosition);

                // Draw cinematic trigger area
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_targetActor.position, _cinematicTriggerDistance);

                // Draw overview area
                Gizmos.color = Color.green;
                Vector3 overviewPosition = _targetActor.position + Vector3.up * _overviewHeight;
                Gizmos.DrawWireSphere(overviewPosition, 1f);
                Gizmos.DrawLine(_targetActor.position, overviewPosition);
            }
        }

        #endregion
    }
}
