using UnityEngine;

namespace Laboratory.Movement.Climbing
{
    /// <summary>
    /// Advanced climbing system that handles wall climbing, ledge mantling, and wall jumping mechanics.
    /// Supports stamina management, camera adjustments, and smooth state transitions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ClimbSystem : MonoBehaviour
    {
        #region Fields

        #region Serialized Fields

        [Header("References")]
        [SerializeField, Tooltip("Transform representing the player's chest position for wall detection")]
        private Transform chestPosition;
        
        [SerializeField, Tooltip("Transform representing the hand position for ledge detection")]
        private Transform handPosition;
        
        [SerializeField, Tooltip("Player camera transform for movement direction calculations")]
        private Transform playerCamera;
        
        [SerializeField, Tooltip("Optional animator for IK and climbing animations")]
        private Animator animator;

        [Header("Movement Settings")]
        [SerializeField, Tooltip("Speed of climbing movement along walls"), Range(0.1f, 10f)]
        private float climbSpeed = 3f;
        
        [SerializeField, Tooltip("Speed of mantling over ledges"), Range(0.1f, 10f)]
        private float mantleSpeed = 3f;
        
        [SerializeField, Tooltip("Force applied during wall jump"), Range(1f, 20f)]
        private float wallJumpForce = 5f;

        [Header("Detection Settings")]
        [SerializeField, Tooltip("Maximum reach distance for detecting climbable walls"), Range(0.1f, 3f)]
        private float climbReach = 1f;
        
        [SerializeField, Tooltip("Maximum reach distance for detecting ledges"), Range(0.1f, 3f)]
        private float ledgeReach = 1f;
        
        [SerializeField, Tooltip("Layer mask defining what objects are climbable")]
        private LayerMask climbableLayer;

        [Header("Camera & Stamina")]
        [SerializeField, Tooltip("Camera tilt angle when climbing"), Range(0f, 45f)]
        private float cameraTiltAngle = 15f;
        
        [SerializeField, Tooltip("Speed of camera tilt transition"), Range(0.1f, 10f)]
        private float cameraTiltSpeed = 3f;
        
        [SerializeField, Tooltip("Maximum stamina amount"), Range(10f, 200f)]
        private float staminaMax = 100f;
        
        [SerializeField, Tooltip("Rate at which stamina drains while climbing"), Range(1f, 50f)]
        private float staminaDrainRate = 10f;
        
        [SerializeField, Tooltip("Rate at which stamina recovers when grounded"), Range(1f, 50f)]
        private float staminaRecoverRate = 5f;
        
        [SerializeField, Tooltip("Grace period for leaving wall before falling"), Range(0.1f, 1f)]
        private float coyoteTime = 0.2f;

        #endregion

        #region Public Properties

        /// <summary>
        /// Current stamina value
        /// </summary>
        public float Stamina => stamina;

        /// <summary>
        /// Current climbing state
        /// </summary>
        public ClimbState CurrentState => currentState;

        /// <summary>
        /// Whether the system is currently climbing
        /// </summary>
        public bool IsClimbing => currentState == ClimbState.Climbing;

        /// <summary>
        /// Whether the system is currently mantling
        /// </summary>
        public bool IsMantling => currentState == ClimbState.Mantling;

        #endregion

        #region Private Fields

        private float stamina;
        private float coyoteTimer;
        private Rigidbody rb;
        private Vector3 climbNormal;
        private Vector3 mantleTarget;
        private ClimbState currentState = ClimbState.Grounded;

        #endregion

        #endregion

        #region Enums

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

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize components and set default values
        /// </summary>
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            stamina = staminaMax;
        }

        /// <summary>
        /// Handle input and state transitions
        /// </summary>
        private void Update()
        {
            HandleState();
            RecoverStamina();
            AdjustCamera();
        }

        /// <summary>
        /// Handle physics-based movement during climbing states
        /// </summary>
        private void FixedUpdate()
        {
            switch (currentState)
            {
                case ClimbState.Climbing:
                    ClimbMovement();
                    break;
                case ClimbState.Mantling:
                    MantleMovement();
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the climbing system to exit climbing state
        /// </summary>
        public void ForceExitClimb()
        {
            ExitClimb();
        }

        /// <summary>
        /// Sets the current stamina value
        /// </summary>
        /// <param name="newStamina">New stamina value (will be clamped between 0 and max)</param>
        public void SetStamina(float newStamina)
        {
            stamina = Mathf.Clamp(newStamina, 0f, staminaMax);
        }

        /// <summary>
        /// Adds stamina to the current amount
        /// </summary>
        /// <param name="amount">Amount to add</param>
        public void AddStamina(float amount)
        {
            SetStamina(stamina + amount);
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
            bool wantsClimb = Input.GetKey(KeyCode.Space) && stamina > 0;

            switch (currentState)
            {
                case ClimbState.Grounded:
                    if (canClimbWall && wantsClimb)
                        EnterClimb();
                    break;

                case ClimbState.Climbing:
                    DrainStamina();
                    if (!canClimbWall) coyoteTimer += Time.deltaTime;
                    else coyoteTimer = 0;

                    if (coyoteTimer > coyoteTime || stamina <= 0)
                        ExitClimb();
                    else if (DetectLedge() && wantsClimb)
                        EnterMantle();
                    else if (Input.GetKeyDown(KeyCode.Space))
                        WallJump();
                    break;
            }
        }

        #endregion

        #region Climb Logic

        /// <summary>
        /// Detects if there's a climbable surface in front of the player
        /// </summary>
        /// <returns>True if a climbable surface is detected</returns>
        private bool DetectClimbable()
        {
            RaycastHit hit;
            if (Physics.Raycast(chestPosition.position, transform.forward, out hit, climbReach, climbableLayer))
            {
                climbNormal = hit.normal;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enters climbing state and configures physics
        /// </summary>
        private void EnterClimb()
        {
            currentState = ClimbState.Climbing;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            if (animator) animator.SetBool("Climbing", true);
        }

        /// <summary>
        /// Exits climbing state and restores normal physics
        /// </summary>
        private void ExitClimb()
        {
            currentState = ClimbState.Grounded;
            rb.useGravity = true;
            if (animator) animator.SetBool("Climbing", false);
        }

        /// <summary>
        /// Handles movement while climbing on walls
        /// </summary>
        private void ClimbMovement()
        {
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            Vector3 moveDir = (playerCamera.right * horizontal + playerCamera.up * vertical);
            moveDir = Vector3.ProjectOnPlane(moveDir, climbNormal).normalized;

            rb.linearVelocity = moveDir * climbSpeed;

            // Face wall
            Vector3 lookDir = -climbNormal;
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
            RaycastHit hit;
            if (Physics.Raycast(handPosition.position, Vector3.up, out hit, ledgeReach, climbableLayer))
            {
                mantleTarget = hit.point + Vector3.up * 1f; // adjust for top of ledge
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enters mantling state
        /// </summary>
        private void EnterMantle()
        {
            currentState = ClimbState.Mantling;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            if (animator) animator.SetTrigger("Mantle");
        }

        /// <summary>
        /// Handles movement during mantling over ledges
        /// </summary>
        private void MantleMovement()
        {
            rb.linearVelocity = (mantleTarget - transform.position) * mantleSpeed;
            if (Vector3.Distance(transform.position, mantleTarget) < 0.05f)
            {
                currentState = ClimbState.Grounded;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                if (animator) animator.SetBool("Climbing", false);
            }
        }

        #endregion

        #region Wall Jump

        /// <summary>
        /// Performs a wall jump away from the current wall
        /// </summary>
        private void WallJump()
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce((-climbNormal + Vector3.up) * wallJumpForce, ForceMode.Impulse);
            currentState = ClimbState.WallJumping;
            rb.useGravity = true;
            if (animator) animator.SetTrigger("WallJump");
        }

        #endregion

        #region Stamina

        /// <summary>
        /// Drains stamina while climbing
        /// </summary>
        private void DrainStamina()
        {
            stamina -= staminaDrainRate * Time.deltaTime;
            if (stamina < 0) stamina = 0;
        }

        /// <summary>
        /// Recovers stamina when grounded
        /// </summary>
        private void RecoverStamina()
        {
            if (currentState == ClimbState.Grounded && stamina < staminaMax)
                stamina += staminaRecoverRate * Time.deltaTime;
        }

        #endregion

        #region Camera

        /// <summary>
        /// Adjusts camera tilt based on climbing state
        /// </summary>
        private void AdjustCamera()
        {
            float targetTilt = (currentState == ClimbState.Climbing || currentState == ClimbState.Mantling) ? cameraTiltAngle : 0f;
            Vector3 localEuler = playerCamera.localEulerAngles;
            localEuler.x = Mathf.LerpAngle(localEuler.x, targetTilt, Time.deltaTime * cameraTiltSpeed);
            playerCamera.localEulerAngles = localEuler;
        }

        #endregion

        #endregion
    }
}
