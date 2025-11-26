using UnityEngine;

namespace Laboratory.Genres.Platforming
{
    /// <summary>
    /// 2D platformer character controller
    /// Supports jump, double jump, wall slide, wall jump, and dash
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlatformerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlatformingConfig config;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [SerializeField] private Transform wallCheck;
        [SerializeField] private float wallCheckDistance = 0.5f;

        // Components
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;

        // Input
        private float _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _dashPressed;

        // State
        private bool _isGrounded;
        private bool _isTouchingWall;
        private int _wallDirection;
        private int _jumpsRemaining;
        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;

        // Dash
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector2 _dashDirection;

        // Events
        public event System.Action OnJump;
        public event System.Action OnDoubleJump;
        public event System.Action OnWallJump;
        public event System.Action OnDash;
        public event System.Action OnLanded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            CheckGround();
            CheckWall();
            UpdateTimers();
        }

        private void FixedUpdate()
        {
            if (_isDashing)
            {
                PerformDash();
            }
            else
            {
                ApplyMovement();
                ApplyGravity();
            }
        }

        /// <summary>
        /// Check if grounded
        /// </summary>
        private void CheckGround()
        {
            bool wasGrounded = _isGrounded;

            Vector2 checkPosition = groundCheck != null ? groundCheck.position : transform.position;
            _isGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);

            // Reset jumps when grounded
            if (_isGrounded)
            {
                if (config != null)
                {
                    _jumpsRemaining = config.MaxJumps;
                }
                _coyoteTimeCounter = config != null ? config.CoyoteTime : 0.15f;

                if (!wasGrounded)
                {
                    OnLanded?.Invoke();
                }
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Check if touching wall
        /// </summary>
        private void CheckWall()
        {
            if (config == null || !config.EnableWallSlide) return;

            Vector2 checkPosition = wallCheck != null ? wallCheck.position : transform.position;
            RaycastHit2D hitRight = Physics2D.Raycast(checkPosition, Vector2.right, wallCheckDistance, groundLayer);
            RaycastHit2D hitLeft = Physics2D.Raycast(checkPosition, Vector2.left, wallCheckDistance, groundLayer);

            _isTouchingWall = hitRight.collider != null || hitLeft.collider != null;
            _wallDirection = hitRight.collider != null ? 1 : (hitLeft.collider != null ? -1 : 0);
        }

        /// <summary>
        /// Update timers
        /// </summary>
        private void UpdateTimers()
        {
            // Jump buffer
            if (_jumpPressed)
            {
                _jumpBufferCounter = config != null ? config.JumpBufferTime : 0.1f;
            }
            else
            {
                _jumpBufferCounter -= Time.deltaTime;
            }

            // Dash cooldown
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }

            // Dash duration
            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                }
            }
        }

        /// <summary>
        /// Apply horizontal movement
        /// </summary>
        private void ApplyMovement()
        {
            if (config == null) return;

            float targetSpeed = _moveInput * config.MoveSpeed;
            float currentSpeed = _rigidbody.linearVelocity.x;

            // Apply acceleration/deceleration
            float speedDiff = targetSpeed - currentSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? config.Acceleration : config.Deceleration;

            // Reduce control in air
            if (!_isGrounded && config.AirControl < 1f)
            {
                accelRate *= config.AirControl;
            }

            float movement = speedDiff * accelRate * Time.fixedDeltaTime;
            _rigidbody.AddForce(Vector2.right * movement);

            // Wall slide
            if (_isTouchingWall && !_isGrounded && _rigidbody.linearVelocity.y < 0 && config.EnableWallSlide)
            {
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, Mathf.Max(_rigidbody.linearVelocity.y, config.WallSlideSpeed));
            }
        }

        /// <summary>
        /// Apply gravity
        /// </summary>
        private void ApplyGravity()
        {
            if (config == null) return;

            float gravityMultiplier = 1f;

            // Faster falling
            if (_rigidbody.linearVelocity.y < 0)
            {
                gravityMultiplier = config.FallMultiplier;
            }
            // Variable jump height
            else if (_rigidbody.linearVelocity.y > 0 && !_jumpHeld && config.VariableJumpHeight)
            {
                gravityMultiplier = config.FallMultiplier;
            }

            _rigidbody.linearVelocity += Vector2.up * config.Gravity * gravityMultiplier * Time.fixedDeltaTime;

            // Clamp fall speed
            if (_rigidbody.linearVelocity.y < config.MaxFallSpeed)
            {
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, config.MaxFallSpeed);
            }
        }

        /// <summary>
        /// Set input
        /// </summary>
        public void SetInput(float move, bool jump, bool jumpHeld, bool dash)
        {
            _moveInput = move;
            _jumpPressed = jump;
            _jumpHeld = jumpHeld;
            _dashPressed = dash;

            // Process jump
            if (_jumpPressed && _jumpBufferCounter > 0f)
            {
                TryJump();
            }

            // Process dash
            if (_dashPressed && CanDash())
            {
                StartDash();
            }
        }

        /// <summary>
        /// Try to jump
        /// </summary>
        private void TryJump()
        {
            // Wall jump
            if (_isTouchingWall && !_isGrounded && config != null && config.EnableWallJump)
            {
                PerformWallJump();
                return;
            }

            // Regular/double jump
            if (_isGrounded || _coyoteTimeCounter > 0f || _jumpsRemaining > 0)
            {
                PerformJump();
            }
        }

        /// <summary>
        /// Perform jump
        /// </summary>
        private void PerformJump()
        {
            if (config == null) return;

            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 0f);
            _rigidbody.AddForce(Vector2.up * config.JumpForce, ForceMode2D.Impulse);

            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;

            bool isDoubleJump = !_isGrounded && _jumpsRemaining < (config.MaxJumps);
            _jumpsRemaining--;

            if (isDoubleJump)
            {
                OnDoubleJump?.Invoke();
            }
            else
            {
                OnJump?.Invoke();
            }
        }

        /// <summary>
        /// Perform wall jump
        /// </summary>
        private void PerformWallJump()
        {
            if (config == null) return;

            Vector2 force = new Vector2(config.WallJumpForce.x * -_wallDirection, config.WallJumpForce.y);
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.AddForce(force, ForceMode2D.Impulse);

            _jumpBufferCounter = 0f;
            _jumpsRemaining = config.MaxJumps;

            OnWallJump?.Invoke();
        }

        /// <summary>
        /// Start dash
        /// </summary>
        private void StartDash()
        {
            if (config == null) return;

            _isDashing = true;
            _dashTimer = config.DashDuration;
            _dashCooldownTimer = config.DashCooldown;

            // Determine dash direction
            float dashDir = _moveInput != 0 ? Mathf.Sign(_moveInput) : transform.localScale.x;
            _dashDirection = new Vector2(dashDir, 0f);

            OnDash?.Invoke();
        }

        /// <summary>
        /// Perform dash
        /// </summary>
        private void PerformDash()
        {
            if (config == null) return;

            _rigidbody.linearVelocity = _dashDirection * config.DashSpeed;
        }

        /// <summary>
        /// Check if can dash
        /// </summary>
        private bool CanDash()
        {
            return config != null && config.EnableDash && !_isDashing && _dashCooldownTimer <= 0f;
        }

        /// <summary>
        /// Reset to position
        /// </summary>
        public void ResetToPosition(Vector3 position)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            transform.position = position;
            _isDashing = false;
        }

        // Getters
        public bool IsGrounded() => _isGrounded;
        public bool IsTouchingWall() => _isTouchingWall;
        public bool IsDashing() => _isDashing;
        public float GetDashCooldown() => _dashCooldownTimer;

        private void OnDrawGizmosSelected()
        {
            // Draw ground check
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            // Draw wall check
            if (wallCheck != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
                Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.left * wallCheckDistance);
            }
        }
    }
}
