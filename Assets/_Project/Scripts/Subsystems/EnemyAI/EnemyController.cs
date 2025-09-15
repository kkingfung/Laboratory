using UnityEngine;
using UnityEngine.AI;
using Laboratory.Core.Health;
using Laboratory.Subsystems.Player;
using System;
using Random = UnityEngine.Random;

namespace Laboratory.Subsystems.EnemyAI
{
    /// <summary>
    /// Basic enemy controller with AI behavior, combat, and health system integration.
    /// Provides foundation for different enemy types in 3D action game.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : MonoBehaviour, IHealthComponent
    {
        [Header("AI Settings")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float patrolRadius = 5f;
        [SerializeField] private float waitTime = 2f;

        [Header("Combat")]
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private AudioClip alertSound;

        // Components
        private NavMeshAgent agent;
        private Animator animator;
        private PlayerController targetPlayer;

        // State
        private Vector3 startPosition;
        private int currentHealth;
        private float lastAttackTime;
        private float lastPatrolTime;
        private Vector3 patrolTarget;
        private EnemyState currentState = EnemyState.Patrolling;

        #region IHealthComponent Implementation

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsDead => currentHealth <= 0;
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        public event Action<HealthChangedEventArgs> OnHealthChanged;
        public event Action<DeathEventArgs> OnDeath;
        public event Action<DamageRequest> OnDamageTaken;

        public bool TakeDamage(DamageRequest damageRequest)
        {
            if (!IsAlive || damageRequest == null) return false;

            int oldHealth = currentHealth;
            int damageAmount = Mathf.RoundToInt(damageRequest.Amount);
            
            currentHealth -= damageAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // Fire events
            OnHealthChanged?.Invoke(new HealthChangedEventArgs(oldHealth, currentHealth, damageRequest.Source));
            OnDamageTaken?.Invoke(damageRequest);

            if (currentHealth <= 0)
            {
                OnDeath?.Invoke(new DeathEventArgs(damageRequest.Source, damageRequest));
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
                OnHealthChanged?.Invoke(new HealthChangedEventArgs(oldHealth, currentHealth, source));
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
                OnHealthChanged?.Invoke(new HealthChangedEventArgs(oldHealth, currentHealth));
            }
        }

        #endregion

        public enum EnemyState
        {
            Patrolling,
            Chasing,
            Attacking,
            Dead
        }

        #region Unity Lifecycle

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            
            startPosition = transform.position;
            currentHealth = maxHealth;
            SetNewPatrolTarget();
        }

        private void Update()
        {
            if (currentState == EnemyState.Dead) return;

            DetectPlayer();
            UpdateBehavior();
            UpdateAnimations();
        }

        #endregion

        #region AI Behavior

        private void DetectPlayer()
        {
            // Find player within detection range
            Collider[] playersInRange = Physics.OverlapSphere(transform.position, detectionRange, LayerMask.GetMask("Player"));
            
            if (playersInRange.Length > 0)
            {
                PlayerController nearestPlayer = playersInRange[0].GetComponent<PlayerController>();
                if (nearestPlayer != null && nearestPlayer.IsAlive)
                {
                    // Check line of sight
                    Vector3 directionToPlayer = (nearestPlayer.transform.position - transform.position).normalized;
                    RaycastHit hit;
                    
                    if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange))
                    {
                        if (hit.collider.GetComponent<PlayerController>() != null)
                        {
                            if (targetPlayer == null)
                            {
                                PlaySound(alertSound);
                            }
                            targetPlayer = nearestPlayer;
                            return;
                        }
                    }
                }
            }

            // Lose target if too far away
            if (targetPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
                if (distanceToPlayer > detectionRange * 1.5f)
                {
                    targetPlayer = null;
                }
            }
        }

        private void UpdateBehavior()
        {
            switch (currentState)
            {
                case EnemyState.Patrolling:
                    HandlePatrolling();
                    break;
                case EnemyState.Chasing:
                    HandleChasing();
                    break;
                case EnemyState.Attacking:
                    HandleAttacking();
                    break;
            }
        }

        private void HandlePatrolling()
        {
            if (targetPlayer != null)
            {
                currentState = EnemyState.Chasing;
                return;
            }

            // Move to patrol target
            agent.SetDestination(patrolTarget);

            // Check if reached patrol target
            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                if (Time.time >= lastPatrolTime + waitTime)
                {
                    SetNewPatrolTarget();
                }
            }
        }

        private void HandleChasing()
        {
            if (targetPlayer == null || !targetPlayer.IsAlive)
            {
                currentState = EnemyState.Patrolling;
                SetNewPatrolTarget();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

            if (distanceToPlayer <= attackRange)
            {
                currentState = EnemyState.Attacking;
                agent.SetDestination(transform.position); // Stop moving
            }
            else
            {
                agent.SetDestination(targetPlayer.transform.position);
            }
        }

        private void HandleAttacking()
        {
            if (targetPlayer == null || !targetPlayer.IsAlive)
            {
                currentState = EnemyState.Patrolling;
                SetNewPatrolTarget();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

            if (distanceToPlayer > attackRange * 1.2f)
            {
                currentState = EnemyState.Chasing;
                return;
            }

            // Face the player
            Vector3 lookDirection = (targetPlayer.transform.position - transform.position).normalized;
            lookDirection.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDirection);

            // Attack if cooldown is ready
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }

        private void SetNewPatrolTarget()
        {
            lastPatrolTime = Time.time;
            
            // Find random point within patrol radius
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection.y = 0;
            patrolTarget = startPosition + randomDirection;

            // Make sure target is on navmesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(patrolTarget, out hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolTarget = hit.position;
            }
            else
            {
                patrolTarget = startPosition;
            }
        }

        #endregion

        #region Combat

        private void PerformAttack()
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("Attack");
            PlaySound(attackSound);

            // Damage player if in range
            if (targetPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
                if (distanceToPlayer <= attackRange)
                {
                    var damageRequest = new DamageRequest(attackDamage, gameObject);
                    targetPlayer.TakeDamage(damageRequest);
                }
            }
        }

        #endregion

        #region Private Methods

        private void Die()
        {
            currentState = EnemyState.Dead;
            animator.SetTrigger("Death");
            PlaySound(deathSound);
            
            agent.enabled = false;
            enabled = false;

            // Destroy after animation
            Destroy(gameObject, 3f);
        }

        #endregion

        #region Animation

        private void UpdateAnimations()
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsChasing", currentState == EnemyState.Chasing);
            animator.SetBool("IsAttacking", currentState == EnemyState.Attacking);
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
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw patrol radius
            Gizmos.color = Color.blue;
            Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawWireSphere(startPos, patrolRadius);

            // Draw patrol target
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(patrolTarget, 0.5f);
            }
        }

        #endregion
    }
}
