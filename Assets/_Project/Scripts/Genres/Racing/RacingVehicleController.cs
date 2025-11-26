using UnityEngine;

namespace Laboratory.Genres.Racing
{
    /// <summary>
    /// Arcade-style racing vehicle controller
    /// Supports player and AI control
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RacingVehicleController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RacingConfig config;

        [Header("Visual")]
        [SerializeField] private Transform vehicleModel;
        [SerializeField] private Transform[] wheels;

        // Components
        private Rigidbody _rigidbody;

        // Input
        private float _throttleInput;
        private float _steerInput;
        private bool _brakeInput;
        private bool _boostInput;

        // State
        private float _currentSpeed;
        private bool _isBoosting;
        private float _boostTimer;
        private float _boostCooldownTimer;

        // AI
        private bool _isAIControlled;
        private Transform _currentWaypoint;

        // Events
        public event System.Action OnBoostActivated;
        public event System.Action OnBoostEnded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.centerOfMass = Vector3.down * 0.5f; // Lower center of mass for stability
        }

        private void FixedUpdate()
        {
            UpdateVehiclePhysics();
            UpdateBoost();
        }

        /// <summary>
        /// Update vehicle physics
        /// </summary>
        private void UpdateVehiclePhysics()
        {
            if (config == null) return;

            // Calculate speed
            _currentSpeed = Vector3.Dot(_rigidbody.velocity, transform.forward);

            // Apply acceleration/braking
            float targetSpeed = _throttleInput * (  _isBoosting ? config.BoostSpeed : config.MaxSpeed);

            if (_brakeInput)
            {
                targetSpeed = 0f;
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, Vector3.zero, config.BrakeForce * Time.fixedDeltaTime);
            }
            else
            {
                Vector3 forwardForce = transform.forward * (_throttleInput * config.Acceleration);
                _rigidbody.AddForce(forwardForce, ForceMode.Acceleration);

                // Limit speed
                float maxSpeed = _isBoosting ? config.BoostSpeed : config.MaxSpeed;
                if (_rigidbody.velocity.magnitude > maxSpeed)
                {
                    _rigidbody.velocity = _rigidbody.velocity.normalized * maxSpeed;
                }
            }

            // Apply steering
            if (Mathf.Abs(_steerInput) > 0.01f && _currentSpeed > 1f)
            {
                float turnAmount = _steerInput * config.TurnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
                _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
            }

            // Apply drift physics
            Vector3 lateralVelocity = transform.right * Vector3.Dot(_rigidbody.velocity, transform.right);
            _rigidbody.velocity -= lateralVelocity * (1f - config.DriftFactor);

            // Visual rotation for model
            if (vehicleModel != null && Mathf.Abs(_steerInput) > 0.01f)
            {
                float tiltAngle = -_steerInput * 15f;
                vehicleModel.localRotation = Quaternion.Lerp(
                    vehicleModel.localRotation,
                    Quaternion.Euler(0f, 0f, tiltAngle),
                    Time.fixedDeltaTime * 5f
                );
            }

            // Rotate wheels visual
            if (wheels != null && wheels.Length > 0)
            {
                float wheelRotation = _currentSpeed * 360f * Time.fixedDeltaTime / (2f * Mathf.PI * 0.3f);
                foreach (Transform wheel in wheels)
                {
                    if (wheel != null)
                    {
                        wheel.Rotate(wheelRotation, 0f, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Update boost system
        /// </summary>
        private void UpdateBoost()
        {
            if (config == null || !config.EnableBoost) return;

            // Update boost timer
            if (_isBoosting)
            {
                _boostTimer -= Time.fixedDeltaTime;
                if (_boostTimer <= 0f)
                {
                    EndBoost();
                }
            }

            // Update cooldown
            if (_boostCooldownTimer > 0f)
            {
                _boostCooldownTimer -= Time.fixedDeltaTime;
            }

            // Activate boost from input
            if (_boostInput && !_isBoosting && _boostCooldownTimer <= 0f)
            {
                ActivateBoost();
            }
        }

        /// <summary>
        /// Set player input
        /// </summary>
        public void SetInput(float throttle, float steer, bool brake, bool boost)
        {
            _throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            _steerInput = Mathf.Clamp(steer, -1f, 1f);
            _brakeInput = brake;
            _boostInput = boost;
        }

        /// <summary>
        /// Activate boost
        /// </summary>
        public void ActivateBoost()
        {
            if (config == null || !config.EnableBoost || _isBoosting) return;

            _isBoosting = true;
            _boostTimer = config.BoostDuration;
            _boostCooldownTimer = config.BoostCooldown;

            OnBoostActivated?.Invoke();
        }

        /// <summary>
        /// End boost
        /// </summary>
        private void EndBoost()
        {
            _isBoosting = false;
            OnBoostEnded?.Invoke();
        }

        /// <summary>
        /// Reset vehicle to position
        /// </summary>
        public void ResetToPosition(Vector3 position, Quaternion rotation)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = position;
            transform.rotation = rotation;
        }

        /// <summary>
        /// Set AI control mode
        /// </summary>
        public void SetAIControlled(bool isAI)
        {
            _isAIControlled = isAI;
        }

        /// <summary>
        /// Simple AI steering towards waypoint
        /// </summary>
        public void SetAIWaypoint(Transform waypoint)
        {
            _currentWaypoint = waypoint;

            if (_isAIControlled && _currentWaypoint != null)
            {
                // Calculate steering direction
                Vector3 targetDir = (_currentWaypoint.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, targetDir);
                float cross = Vector3.Cross(transform.forward, targetDir).y;

                // Set AI input
                _throttleInput = Mathf.Clamp01(dot);
                _steerInput = Mathf.Clamp(cross * 2f, -1f, 1f);
                _brakeInput = dot < -0.5f;
            }
        }

        // Getters
        public float GetCurrentSpeed() => _currentSpeed;
        public bool IsBoosting() => _isBoosting;
        public float GetBoostCooldown() => _boostCooldownTimer;
        public float GetBoostCharge() => config != null && config.EnableBoost ? Mathf.Clamp01(1f - (_boostCooldownTimer / config.BoostCooldown)) : 0f;
        public bool CanBoost() => config != null && config.EnableBoost && !_isBoosting && _boostCooldownTimer <= 0f;
    }
}
