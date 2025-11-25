using UnityEngine;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// Main camera controller supporting all 47 game genres
    /// Orchestrates camera modes, effects, and state transitions
    /// </summary>
    [RequireComponent(typeof(CameraStateMachine))]
    [RequireComponent(typeof(CameraEffects))]
    public class CameraController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraConfig config;

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = Vector3.zero;

        [Header("Input (Optional)")]
        [SerializeField] private bool enableMouseInput = true;
        [SerializeField] private bool enableKeyboardInput = true;

        // Components
        private CameraStateMachine _stateMachine;
        private CameraEffects _effects;
        private UnityEngine.Camera _camera;
        private Transform _cameraTransform;

        // Input state
        private Vector2 _mouseInput;
        private Vector2 _keyboardInput;
        private float _zoomInput;

        // Camera state per mode
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        private float _currentFOV;

        // Third person state
        private float _thirdPersonYaw = 0f;
        private float _thirdPersonPitch = 20f;

        // First person state
        private float _firstPersonYaw = 0f;
        private float _firstPersonPitch = 0f;

        // Top down state
        private Vector3 _topDownOffset = Vector3.zero;
        private float _topDownZoom = 15f;

        // Strategy state
        private Vector3 _strategyPanOffset = Vector3.zero;
        private float _strategyZoom = 20f;
        private float _strategyRotation = 0f;

        private void Awake()
        {
            _stateMachine = GetComponent<CameraStateMachine>();
            _effects = GetComponent<CameraEffects>();
            _camera = GetComponent<UnityEngine.Camera>();
            _cameraTransform = transform;

            if (config != null)
            {
                _currentFOV = _camera.fieldOfView;
                _topDownZoom = config.TopDownDistance;
                _strategyZoom = config.StrategyZoomLimits.x + (config.StrategyZoomLimits.y - config.StrategyZoomLimits.x) * 0.5f;
            }
        }

        private void Start()
        {
            // Subscribe to state machine events
            _stateMachine.OnTransitionStarted += OnTransitionStarted;
            _stateMachine.OnModeChanged += OnModeChanged;
        }

        private void LateUpdate()
        {
            if (target == null || config == null) return;

            // Update input
            UpdateInput();

            // Update camera based on current mode
            CameraMode currentMode = _stateMachine.GetCurrentMode();
            UpdateCameraForMode(currentMode);

            // Apply camera state (if not transitioning, state machine handles it)
            if (!_stateMachine.IsTransitioning())
            {
                _cameraTransform.position = _currentPosition;
                _cameraTransform.rotation = _currentRotation;
                _camera.fieldOfView = _currentFOV;
            }
            else
            {
                // Update state machine with target state
                _stateMachine.SetTargetState(_currentPosition, _currentRotation, _currentFOV);
            }
        }

        /// <summary>
        /// Update input from mouse and keyboard
        /// </summary>
        private void UpdateInput()
        {
            if (enableMouseInput)
            {
                _mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }

            if (enableKeyboardInput)
            {
                _keyboardInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            }

            _zoomInput = Input.GetAxis("Mouse ScrollWheel");
        }

        /// <summary>
        /// Update camera position and rotation based on current mode
        /// </summary>
        private void UpdateCameraForMode(CameraMode mode)
        {
            Vector3 targetPosition = target.position + targetOffset;

            switch (mode)
            {
                case CameraMode.ThirdPerson:
                case CameraMode.ThirdPersonOrbit:
                    UpdateThirdPerson(targetPosition);
                    break;

                case CameraMode.FirstPerson:
                    UpdateFirstPerson(targetPosition);
                    break;

                case CameraMode.TopDown:
                case CameraMode.TopDownIsometric:
                    UpdateTopDown(targetPosition);
                    break;

                case CameraMode.RacingThirdPerson:
                    UpdateRacing(targetPosition);
                    break;

                case CameraMode.StrategyRTS:
                case CameraMode.StrategyTurnBased:
                    UpdateStrategy(targetPosition);
                    break;

                case CameraMode.SideScroller:
                case CameraMode.SideScrollerFixed:
                    UpdateSideScroller(targetPosition);
                    break;

                case CameraMode.FightingGame:
                    UpdateFightingGame(targetPosition);
                    break;

                case CameraMode.Free:
                    UpdateFreeCamera();
                    break;

                default:
                    UpdateThirdPerson(targetPosition);
                    break;
            }
        }

        /// <summary>
        /// Third person camera update
        /// </summary>
        private void UpdateThirdPerson(Vector3 targetPosition)
        {
            _thirdPersonYaw += _mouseInput.x * config.ThirdPersonRotationSpeed;
            _thirdPersonPitch -= _mouseInput.y * config.ThirdPersonRotationSpeed;
            _thirdPersonPitch = Mathf.Clamp(_thirdPersonPitch, config.ThirdPersonPitchLimits.x, config.ThirdPersonPitchLimits.y);

            Quaternion rotation = Quaternion.Euler(_thirdPersonPitch, _thirdPersonYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, config.ThirdPersonHeight, -config.ThirdPersonDistance);

            _currentPosition = Vector3.Lerp(_cameraTransform.position, targetPosition + offset, Time.deltaTime * config.ThirdPersonFollowSpeed);
            _currentRotation = Quaternion.Lerp(_cameraTransform.rotation, rotation, Time.deltaTime * config.ThirdPersonRotationSpeed);
            _currentFOV = _camera.fieldOfView;
        }

        /// <summary>
        /// First person camera update
        /// </summary>
        private void UpdateFirstPerson(Vector3 targetPosition)
        {
            float sensitivity = config.FirstPersonMouseSensitivity;
            _firstPersonYaw += _mouseInput.x * sensitivity;
            _firstPersonPitch -= _mouseInput.y * sensitivity;
            _firstPersonPitch = Mathf.Clamp(_firstPersonPitch, config.FirstPersonPitchLimits.x, config.FirstPersonPitchLimits.y);

            _currentPosition = targetPosition + Vector3.up * config.FirstPersonHeight;
            _currentRotation = Quaternion.Euler(_firstPersonPitch, _firstPersonYaw, 0f);
            _currentFOV = config.FirstPersonFOV;
        }

        /// <summary>
        /// Top down camera update
        /// </summary>
        private void UpdateTopDown(Vector3 targetPosition)
        {
            // Pan with keyboard
            _topDownOffset += new Vector3(_keyboardInput.x, 0f, _keyboardInput.y) * config.TopDownPanSpeed * Time.deltaTime;

            // Zoom with mouse wheel
            _topDownZoom -= _zoomInput * config.ZoomSpeed;
            _topDownZoom = Mathf.Clamp(_topDownZoom, config.TopDownZoomLimits.x, config.TopDownZoomLimits.y);

            Vector3 offset = Quaternion.Euler(config.TopDownAngle, 0f, 0f) * new Vector3(0f, 0f, -_topDownZoom);
            _currentPosition = targetPosition + _topDownOffset + offset;
            _currentRotation = Quaternion.Euler(config.TopDownAngle, 0f, 0f);
            _currentFOV = 60f;
        }

        /// <summary>
        /// Racing camera update
        /// </summary>
        private void UpdateRacing(Vector3 targetPosition)
        {
            Vector3 forward = target.forward;
            Vector3 lookAheadPoint = targetPosition + forward * config.RacingLookAheadDistance;

            Vector3 offset = -forward * config.RacingDistance + Vector3.up * config.RacingHeight;
            _currentPosition = Vector3.Lerp(_cameraTransform.position, targetPosition + offset, Time.deltaTime * config.RacingFollowSpeed);

            _currentRotation = Quaternion.Lerp(_cameraTransform.rotation, Quaternion.LookRotation(lookAheadPoint - _currentPosition), Time.deltaTime * config.RacingFollowSpeed);
            _currentFOV = _camera.fieldOfView;
        }

        /// <summary>
        /// Strategy RTS camera update
        /// </summary>
        private void UpdateStrategy(Vector3 targetPosition)
        {
            // Pan
            _strategyPanOffset += new Vector3(_keyboardInput.x, 0f, _keyboardInput.y) * config.StrategyPanSpeed * Time.deltaTime;

            // Rotate (Q/E keys)
            if (Input.GetKey(KeyCode.Q))
                _strategyRotation -= config.StrategyRotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E))
                _strategyRotation += config.StrategyRotationSpeed * Time.deltaTime;

            // Zoom
            _strategyZoom -= _zoomInput * config.StrategyZoomSpeed;
            _strategyZoom = Mathf.Clamp(_strategyZoom, config.StrategyZoomLimits.x, config.StrategyZoomLimits.y);

            Quaternion rotation = Quaternion.Euler(45f, _strategyRotation, 0f);
            Vector3 offset = rotation * new Vector3(0f, _strategyZoom * 0.7f, -_strategyZoom * 0.7f);

            _currentPosition = targetPosition + _strategyPanOffset + offset;
            _currentRotation = rotation;
            _currentFOV = 60f;
        }

        /// <summary>
        /// Side scroller camera update
        /// </summary>
        private void UpdateSideScroller(Vector3 targetPosition)
        {
            Vector3 offset = new Vector3(0f, 2f, -10f);
            _currentPosition = new Vector3(targetPosition.x, targetPosition.y + offset.y, offset.z);
            _currentRotation = Quaternion.identity;
            _currentFOV = 60f;
        }

        /// <summary>
        /// Fighting game camera update
        /// </summary>
        private void UpdateFightingGame(Vector3 targetPosition)
        {
            // Fixed side view like Street Fighter
            Vector3 offset = new Vector3(-15f, 2f, 0f);
            _currentPosition = targetPosition + offset;
            _currentRotation = Quaternion.Euler(0f, 90f, 0f);
            _currentFOV = 60f;
        }

        /// <summary>
        /// Free camera update (debug mode)
        /// </summary>
        private void UpdateFreeCamera()
        {
            // WASD movement
            Vector3 movement = _cameraTransform.right * _keyboardInput.x + _cameraTransform.forward * _keyboardInput.y;
            _currentPosition += movement * 10f * Time.deltaTime;

            // Mouse look
            _firstPersonYaw += _mouseInput.x * 2f;
            _firstPersonPitch -= _mouseInput.y * 2f;
            _currentRotation = Quaternion.Euler(_firstPersonPitch, _firstPersonYaw, 0f);
            _currentFOV = _camera.fieldOfView;
        }

        /// <summary>
        /// Set target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Change camera mode
        /// </summary>
        public void SetMode(CameraMode mode, float? transitionDuration = null)
        {
            _stateMachine.TransitionTo(mode, transitionDuration);
        }

        /// <summary>
        /// Get camera effects component
        /// </summary>
        public CameraEffects GetEffects()
        {
            return _effects;
        }

        /// <summary>
        /// Get camera state machine
        /// </summary>
        public CameraStateMachine GetStateMachine()
        {
            return _stateMachine;
        }

        private void OnTransitionStarted(CameraMode from, CameraMode to)
        {
            // Can add custom logic when transition starts
        }

        private void OnModeChanged(CameraMode newMode)
        {
            // Reset mode-specific state when mode changes
            switch (newMode)
            {
                case CameraMode.TopDown:
                    _topDownOffset = Vector3.zero;
                    break;
                case CameraMode.StrategyRTS:
                    _strategyPanOffset = Vector3.zero;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnTransitionStarted -= OnTransitionStarted;
                _stateMachine.OnModeChanged -= OnModeChanged;
            }
        }
    }
}
