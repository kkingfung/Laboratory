using UnityEngine;
using Laboratory.Infrastructure.Core;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.Character.Events;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Enumeration of possible climbing states
    /// </summary>
    public enum ClimbState
    {
        /// <summary>Player is on the ground</summary>
        Grounded,
        /// <summary>Player is climbing a wall</summary>
        Climbing,
        /// <summary>Player is mantling over a ledge</summary>
        Mantling,
        /// <summary>Player is performing a wall jump</summary>
        WallJumping
    }

    /// <summary>
    /// Advanced climbing system that handles wall climbing, ledge mantling, and wall jumping mechanics.
    /// Supports stamina management, camera adjustments, and smooth state transitions.
    /// Updated with modern Unity APIs and proper event integration.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ClimbingController : MonoBehaviour, ICharacterController
    {
        #region Fields

        [Header("References")]
        [SerializeField, Tooltip("Transform representing the player's chest position for wall detection")]
        private Transform _chestPosition;
        
        [SerializeField, Tooltip("Transform representing the hand position for ledge detection")]
        private Transform _handPosition;
        
        [SerializeField, Tooltip("Player camera transform for movement direction calculations")]
        private Transform _playerCamera;
        
        [SerializeField, Tooltip("Optional animator for IK and climbing animations")]
        private Animator _animator;

        [Header("Detection Settings")]
        [SerializeField, Tooltip("Maximum reach distance for detecting climbable walls")]
        [Range(0.1f, 3f)]
        private float _climbReach = 1f;
        
        [SerializeField, Tooltip("Maximum reach distance for detecting ledges")]
        [Range(0.1f, 3f)]
        private float _ledgeReach = 1f;
        
        [SerializeField, Tooltip("Layer mask defining what objects are climbable")]
        private LayerMask _climbableLayer = -1;

        [Header("Movement")]
        [SerializeField, Tooltip("Speed of climbing movement along walls")]
        [Range(0.1f, 10f)]
        private float _climbSpeed = 3f;
        
        [SerializeField, Tooltip("Speed of mantling over ledges")]
        [Range(0.1f, 10f)]
        private float _mantleSpeed = 3f;
        
        [SerializeField, Tooltip("Force applied during wall jump")]
        [Range(1f, 20f)]
        private float _wallJumpForce = 5f;

        [Header("Stamina System")]
        [SerializeField, Tooltip("Maximum stamina amount")]
        [Range(10f, 200f)]
        private float _staminaMax = 100f;
        
        [SerializeField, Tooltip("Rate at which stamina drains while climbing")]
        [Range(1f, 50f)]
        private float _staminaDrainRate = 10f;
        
        [SerializeField, Tooltip("Rate at which stamina recovers when grounded")]
        [Range(1f, 50f)]
        private float _staminaRecoverRate = 5f;

        [Header("Camera & Animation")]
        [SerializeField, Tooltip("Camera tilt angle when climbing")]
        [Range(0f, 45f)]
        private float _cameraTiltAngle = 15f;
        
        [SerializeField, Tooltip("Speed of camera tilt transition")]
        [Range(0.1f, 10f)]
        private float _cameraTiltSpeed = 3f;
        
        [SerializeField, Tooltip("Grace period for leaving wall before falling")]
        [Range(0.1f, 1f)]
        private float _coyoteTime = 0.2f;

        // Runtime state
        private bool _isActive = true;
        private bool _isInitialized = false;
        private float _stamina;
        private float _coyoteTimer;
        private ClimbState _currentState = ClimbState.Grounded;
        private Vector3 _climbNormal;
        private Vector3 _mantleTarget;
        
        // Components
        private Rigidbody _rigidbody;
        private ServiceContainer _services;
        private IEventBus _eventBus;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public float Stamina => _stamina;
        public ClimbState CurrentState => _currentState;
        public bool IsClimbing => _currentState == ClimbState.Climbing;
        public bool IsMantling => _currentState == ClimbState.Mantling;

        #endregion

        #region Events

        public event System.Action<ClimbState> OnClimbStateChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError($"[ClimbingController] Rigidbody component required on {name}");
                enabled = false;
                return;
            }

            ValidateReferences();
        }

        private void Start()
        {
            _stamina = _staminaMax;
        }

        private void Update()
        {
            if (!_isActive || !_isInitialized) return;

            HandleState();
            RecoverStamina();
            AdjustCamera();
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            switch (_currentState)
            {
                case ClimbState.Climbing:
                    ClimbMovement();
                    break;
                case ClimbState.Mantling:
                    MantleMovement();
                    break;
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
                ForceExitClimb();
            }
        }

        public void Initialize(ServiceContainer services)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));

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
            OnClimbStateChanged = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the climbing system to exit climbing state
        /// </summary>
        public void ForceExitClimb()
        {
            if (_currentState != ClimbState.Grounded)
            {
                ExitClimb();
            }
        }

        /// <summary>
        /// Sets the current stamina value
        /// </summary>
        /// <param name="newStamina">New stamina value (will be clamped between 0 and max)</param>
        public void SetStamina(float newStamina)
        {
            _stamina = Mathf.Clamp(newStamina, 0f, _staminaMax);
        }

        /// <summary>
        /// Adds stamina to the current amount
        /// </summary>
        /// <param name="amount">Amount to add</param>
        public void AddStamina(float amount)
        {
            SetStamina(_stamina + amount);
        }

        /// <summary>
        /// Checks if climbing is possible at current position
        /// </summary>
        /// <returns>True if can start climbing</returns>
        public bool CanStartClimbing()
        {
            return DetectClimbable() && _stamina > 0f;
        }

        #endregion

        #region Private Methods

        #region State Handling

        /// <summary>
        /// Handles state transitions based on input and conditions
        /// </summary>
        private void HandleState()
        {
            bool canClimbWall = DetectClimbable();
            bool wantsClimb = UnityEngine.Input.GetKey(KeyCode.Space) && _stamina > 0;

            switch (_currentState)
            {
                case ClimbState.Grounded:
                    if (canClimbWall && wantsClimb)
                        EnterClimb();
                    break;

                case ClimbState.Climbing:
                    DrainStamina();
                    
                    if (!canClimbWall) 
                        _coyoteTimer += Time.deltaTime;
                    else 
                        _coyoteTimer = 0;

                    if (_coyoteTimer > _coyoteTime || _stamina <= 0)
                        ExitClimb();
                    else if (DetectLedge() && wantsClimb)
                        EnterMantle();
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                        WallJump();
                    break;
                    
                case ClimbState.WallJumping:
                    // Wall jump is temporary state, will auto-transition to grounded
                    if (_rigidbody.linearVelocity.magnitude < 0.1f)
                        ChangeState(ClimbState.Grounded);
                    break;
            }
        }

        private void ChangeState(ClimbState newState)
        {
            if (_currentState == newState) return;

            ClimbState previousState = _currentState;
            _currentState = newState;

            OnClimbStateChanged?.Invoke(_currentState);
            PublishClimbStateChangedEvent(previousState, _currentState);
        }

        #endregion

        #region Climb Logic

        /// <summary>
        /// Detects if there's a climbable surface in front of the player
        /// </summary>
        /// <returns>True if a climbable surface is detected</returns>
        private bool DetectClimbable()
        {
            if (_chestPosition == null) return false;

            if (Physics.Raycast(_chestPosition.position, transform.forward, out RaycastHit hit, _climbReach, _climbableLayer))
            {
                _climbNormal = hit.normal;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enters climbing state and configures physics
        /// </summary>
        private void EnterClimb()
        {
            ChangeState(ClimbState.Climbing);
            _rigidbody.useGravity = false;
            _rigidbody.linearVelocity = Vector3.zero;
            
            if (_animator) _animator.SetBool("Climbing", true);
        }

        /// <summary>
        /// Exits climbing state and restores normal physics
        /// </summary>
        private void ExitClimb()
        {
            ChangeState(ClimbState.Grounded);
            _rigidbody.useGravity = true;
            
            if (_animator) _animator.SetBool("Climbing", false);
        }

        /// <summary>
        /// Handles movement while climbing on walls
        /// </summary>
        private void ClimbMovement()
        {
            float vertical = UnityEngine.Input.GetAxis("Vertical");
            float horizontal = UnityEngine.Input.GetAxis("Horizontal");

            if (_playerCamera == null) return;

            Vector3 moveDir = (_playerCamera.right * horizontal + _playerCamera.up * vertical);
            moveDir = Vector3.ProjectOnPlane(moveDir, _climbNormal).normalized;

            _rigidbody.linearVelocity = moveDir * _climbSpeed;

            // Face wall
            Vector3 lookDir = -_climbNormal;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        #endregion

        #region Ledge & Mantle

        /// <summary>
        /// Detects if there's a ledge above the player for mantling
        /// </summary>
        /// <returns>True if a ledge is detected</returns>
        private bool DetectLedge()
        {
            if (_handPosition == null) return false;

            if (Physics.Raycast(_handPosition.position, Vector3.up, out RaycastHit hit, _ledgeReach, _climbableLayer))
            {
                _mantleTarget = hit.point + Vector3.up * 1f; // adjust for top of ledge
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enters mantling state
        /// </summary>
        private void EnterMantle()
        {
            ChangeState(ClimbState.Mantling);
            _rigidbody.useGravity = false;
            _rigidbody.linearVelocity = Vector3.zero;
            
            if (_animator) _animator.SetTrigger("Mantle");
        }

        /// <summary>
        /// Handles movement during mantling over ledges
        /// </summary>
        private void MantleMovement()
        {
            _rigidbody.linearVelocity = (_mantleTarget - transform.position) * _mantleSpeed;
            
            if (Vector3.Distance(transform.position, _mantleTarget) < 0.05f)
            {
                ChangeState(ClimbState.Grounded);
                _rigidbody.useGravity = true;
                _rigidbody.linearVelocity = Vector3.zero;
                
                if (_animator) _animator.SetBool("Climbing", false);
            }
        }

        #endregion

        #region Wall Jump

        /// <summary>
        /// Performs a wall jump away from the current wall
        /// </summary>
        private void WallJump()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce((-_climbNormal + Vector3.up) * _wallJumpForce, ForceMode.Impulse);
            
            ChangeState(ClimbState.WallJumping);
            _rigidbody.useGravity = true;
            
            if (_animator) _animator.SetTrigger("WallJump");
        }

        #endregion

        #region Stamina

        /// <summary>
        /// Drains stamina while climbing
        /// </summary>
        private void DrainStamina()
        {
            _stamina -= _staminaDrainRate * Time.deltaTime;
            if (_stamina < 0) _stamina = 0;
        }

        /// <summary>
        /// Recovers stamina when grounded
        /// </summary>
        private void RecoverStamina()
        {
            if (_currentState == ClimbState.Grounded && _stamina < _staminaMax)
                _stamina += _staminaRecoverRate * Time.deltaTime;
        }

        #endregion

        #region Camera

        /// <summary>
        /// Adjusts camera tilt based on climbing state
        /// </summary>
        private void AdjustCamera()
        {
            if (_playerCamera == null) return;

            float targetTilt = (IsClimbing || IsMantling) ? _cameraTiltAngle : 0f;
            Vector3 localEuler = _playerCamera.localEulerAngles;
            localEuler.x = Mathf.LerpAngle(localEuler.x, targetTilt, Time.deltaTime * _cameraTiltSpeed);
            _playerCamera.localEulerAngles = localEuler;
        }

        #endregion

        #region Validation & Events

        private void ValidateReferences()
        {
            if (_chestPosition == null)
                Debug.LogWarning($"[ClimbingController] ChestPosition not assigned on {name}");
            
            if (_handPosition == null)
                Debug.LogWarning($"[ClimbingController] HandPosition not assigned on {name}");
            
            if (_playerCamera == null)
                Debug.LogWarning($"[ClimbingController] PlayerCamera not assigned on {name}");
        }

        private void PublishClimbStateChangedEvent(ClimbState previousState, ClimbState newState)
        {
            var stateEvent = new CharacterClimbStateChangedEvent(transform, IsClimbing, null);
            _eventBus?.Publish(stateEvent);
        }

        #endregion

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw climb reach
            if (_chestPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(_chestPosition.position, transform.forward * _climbReach);
            }

            // Draw ledge reach
            if (_handPosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(_handPosition.position, Vector3.up * _ledgeReach);
            }

            // Draw mantle target
            if (IsMantling)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_mantleTarget, 0.3f);
                Gizmos.DrawLine(transform.position, _mantleTarget);
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            _climbReach = Mathf.Max(0.1f, _climbReach);
            _ledgeReach = Mathf.Max(0.1f, _ledgeReach);
            _climbSpeed = Mathf.Max(0.1f, _climbSpeed);
            _mantleSpeed = Mathf.Max(0.1f, _mantleSpeed);
            _wallJumpForce = Mathf.Max(1f, _wallJumpForce);
            _staminaMax = Mathf.Max(10f, _staminaMax);
            _staminaDrainRate = Mathf.Max(1f, _staminaDrainRate);
            _staminaRecoverRate = Mathf.Max(1f, _staminaRecoverRate);
        }
#endif

        #endregion
    }
}
