using UnityEngine;
using Unity.Entities;
using Laboratory.Core.Health;
using Laboratory.Core.Events;
using Laboratory.Core.Systems;
using Laboratory.Core.Performance;
using System;

namespace Laboratory.Subsystems.Player
{
    /// <summary>
    /// Main player controller that unifies movement, combat, and interaction systems
    /// for 3D action gameplay. Bridges between traditional MonoBehaviour and ECS systems.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour, IHealthComponent, IOptimizedUpdate
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.4f;
        [SerializeField] private LayerMask groundMask = 1;

        [Header("Combat Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip takeDamageSound;

        // Components
        private CharacterController characterController;
        private Animator animator;
        private UnityEngine.Camera playerCamera;

        // State
        private Vector3 velocity;
        private bool isGrounded;
        private int currentHealth;
        private float lastAttackTime;
        private bool isRunning;

        // Performance optimization: cache ground check to reduce Physics calls
        private float lastGroundCheckTime;

        #region IHealthComponent Implementation

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsDead => currentHealth <= 0;
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        public event Action<Laboratory.Core.Health.HealthChangedEventArgs> OnHealthChanged;
        public event Action<Laboratory.Core.Health.DeathEventArgs> OnDeath;
        public event Action<DamageRequest> OnDamageTaken;

        public bool TakeDamage(DamageRequest damageRequest)
        {
            if (!IsAlive || damageRequest == null) return false;

            int oldHealth = currentHealth;
            int damageAmount = Mathf.RoundToInt(damageRequest.Amount);
            
            currentHealth -= damageAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // Fire events
            OnHealthChanged?.Invoke(new Laboratory.Core.Health.HealthChangedEventArgs(oldHealth, currentHealth, damageRequest.Source));
            OnDamageTaken?.Invoke(damageRequest);

            PlaySound(takeDamageSound);

            if (currentHealth <= 0)
            {
                OnDeath?.Invoke(new Laboratory.Core.Health.DeathEventArgs(damageRequest.Source, damageRequest));
                Die();
            }

            return true;
        }

        public bool Heal(int amount, object source = null)
        {
            if (!IsAlive || amount <= 0) return false;

            int oldHealth = currentHealth;
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            if (oldHealth != currentHealth)
            {
                OnHealthChanged?.Invoke(new Laboratory.Core.Health.HealthChangedEventArgs(oldHealth, currentHealth, source));
                return true;
            }

            return false;
        }

        public void ResetToMaxHealth()
        {
            int oldHealth = currentHealth;
            currentHealth = maxHealth;
            
            if (oldHealth != currentHealth)
            {
                OnHealthChanged?.Invoke(new Laboratory.Core.Health.HealthChangedEventArgs(oldHealth, currentHealth));
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            playerCamera = UnityEngine.Camera.main;
            audioSource = GetComponent<AudioSource>();

            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Register for optimized updates for non-critical systems (ground check, animations)
            OptimizedUpdateManager.Instance.RegisterSystem(this, OptimizedUpdateManager.UpdateFrequency.HighFrequency);
        }

        private void OnDestroy()
        {
            OptimizedUpdateManager.Instance.UnregisterSystem(this);
        }

        private void Update()
        {
            // Keep critical input handling in Update() for responsiveness
            HandleMovement();
            HandleCombat();
            HandleInteraction();
            // Note: Animation and ground check moved to OnOptimizedUpdate
        }

        public void OnOptimizedUpdate(float deltaTime)
        {
            // Non-critical systems can update at lower frequency
            UpdateGroundCheck();
            UpdateAnimations();
        }

        public void OnRegistered(OptimizedUpdateManager.UpdateFrequency frequency)
        {
            // Optional callback when registered
        }

        public void OnUnregistered()
        {
            // Optional callback when unregistered
        }

        #endregion

        #region Movement

        private void UpdateGroundCheck()
        {
            // Ground check moved to optimized update for better performance
            isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);
        }

        private void HandleMovement()
        {
            // Ground check is now handled in OnOptimizedUpdate

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Running
            isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : moveSpeed;

            // Movement direction
            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            if (direction.magnitude >= 0.1f)
            {
                // Calculate movement relative to camera
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                // Move character
                characterController.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

                // Rotate character to face movement direction
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            }

            // Jumping
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                PlaySound(jumpSound);
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        #endregion

        #region Combat

        private void HandleCombat()
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("Attack");
            PlaySound(attackSound);

            // Detect enemies in range
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward * attackRange, attackRange);
            
            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    IHealthComponent enemyHealth = enemy.GetComponent<IHealthComponent>();
                    if (enemyHealth != null)
                    {
                        var damageRequest = new DamageRequest(attackDamage, gameObject);
                        enemyHealth.TakeDamage(damageRequest);
                    }
                }
            }
        }

        #endregion

        #region Interaction

        private void HandleInteraction()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Raycast for interactable objects
                RaycastHit hit;
                if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 3f))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(this);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void Die()
        {
            animator.SetTrigger("Death");
            
            // Disable controls
            enabled = false;
        }

        #endregion

        #region Animation

        private void UpdateAnimations()
        {
            // Movement animations
            float speed = characterController.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsRunning", isRunning && speed > 0.1f);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalVelocity", velocity.y);
        }

        #endregion

        #region Utility

        private void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, attackRange);

            // Draw ground check
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, groundCheckDistance);
        }

        #endregion
    }

    /// <summary>
    /// Interface for objects that can be interacted with
    /// </summary>
    public interface IInteractable
    {
        void Interact(PlayerController player);
        string GetInteractionText();
    }
}
