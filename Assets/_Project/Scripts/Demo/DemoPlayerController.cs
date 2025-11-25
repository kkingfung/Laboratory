using UnityEngine;

namespace Laboratory.Demo
{
    /// <summary>
    /// Simple player controller for demo scene
    /// WASD movement, mouse look, basic interaction
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DemoPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        // Components
        private CharacterController _controller;

        // State
        private Vector3 _velocity;
        private float _pitch = 0f;
        private float _yaw = 0f;
        private bool _isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Toggle cursor lock with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            HandleMovement();
            HandleMouseLook();
        }

        /// <summary>
        /// Handle WASD movement
        /// </summary>
        private void HandleMovement()
        {
            // Check if grounded
            _isGrounded = _controller.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Calculate movement direction
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

            // Apply sprint
            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= sprintMultiplier;
            }

            // Move character
            _controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Jump
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }

            // Apply gravity
            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        /// <summary>
        /// Handle mouse look rotation
        /// </summary>
        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (invertY)
            {
                mouseY = -mouseY;
            }

            // Update rotation
            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            // Apply rotation (only yaw, camera handles pitch)
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        /// <summary>
        /// Get current velocity
        /// </summary>
        public Vector3 GetVelocity()
        {
            return _velocity;
        }

        /// <summary>
        /// Check if grounded
        /// </summary>
        public bool IsGrounded()
        {
            return _isGrounded;
        }

        /// <summary>
        /// Set mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }

        /// <summary>
        /// Set Y-axis inversion
        /// </summary>
        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }
    }
}
