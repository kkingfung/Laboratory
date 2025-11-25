using UnityEngine;
using UnityEngine.AI;

namespace Laboratory.Genres.Strategy
{
    /// <summary>
    /// RTS unit that can be selected, moved, and given commands
    /// Supports player and AI control
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class RTSUnit : MonoBehaviour
    {
        [Header("Unit Stats")]
        [SerializeField] private string unitName = "Unit";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer unitRenderer;

        // Components
        private NavMeshAgent _agent;

        // State
        private int _currentHealth;
        private bool _isSelected;
        private int _teamId;
        private RTSUnit _currentTarget;
        private float _attackCooldownTimer;

        // Events
        public event System.Action<RTSUnit> OnDeath;
        public event System.Action<RTSUnit> OnSelected;
        public event System.Action<RTSUnit> OnDeselected;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _currentHealth = maxHealth;

            if (_agent != null)
            {
                _agent.speed = moveSpeed;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }

        private void Update()
        {
            UpdateAttack();
        }

        /// <summary>
        /// Update attack logic
        /// </summary>
        private void UpdateAttack()
        {
            // Update attack cooldown
            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= Time.deltaTime;
            }

            // Attack target if in range
            if (_currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

                if (distanceToTarget <= attackRange)
                {
                    // Stop moving
                    if (_agent != null)
                    {
                        _agent.isStopped = true;
                    }

                    // Attack if cooldown ready
                    if (_attackCooldownTimer <= 0f)
                    {
                        AttackTarget();
                    }

                    // Face target
                    Vector3 lookDir = _currentTarget.transform.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                    }
                }
                else
                {
                    // Move towards target
                    MoveToPosition(_currentTarget.transform.position);
                }
            }
        }

        /// <summary>
        /// Move to position
        /// </summary>
        public void MoveToPosition(Vector3 position)
        {
            if (_agent != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(position);
            }
        }

        /// <summary>
        /// Attack target
        /// </summary>
        public void AttackUnit(RTSUnit target)
        {
            _currentTarget = target;
        }

        /// <summary>
        /// Perform attack
        /// </summary>
        private void AttackTarget()
        {
            if (_currentTarget == null) return;

            _currentTarget.TakeDamage(attackDamage);
            _attackCooldownTimer = attackCooldown;

            Debug.Log($"[RTSUnit] {unitName} attacked {_currentTarget.unitName} for {attackDamage} damage");
        }

        /// <summary>
        /// Take damage
        /// </summary>
        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Unit dies
        /// </summary>
        private void Die()
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Select unit
        /// </summary>
        public void Select()
        {
            if (_isSelected) return;

            _isSelected = true;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }

            OnSelected?.Invoke(this);
        }

        /// <summary>
        /// Deselect unit
        /// </summary>
        public void Deselect()
        {
            if (!_isSelected) return;

            _isSelected = false;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            OnDeselected?.Invoke(this);
        }

        /// <summary>
        /// Set team
        /// </summary>
        public void SetTeam(int teamId)
        {
            _teamId = teamId;

            // Change color based on team
            if (unitRenderer != null)
            {
                Color teamColor = teamId == 0 ? Color.blue : Color.red;
                unitRenderer.material.color = teamColor;
            }
        }

        // Getters
        public string GetUnitName() => unitName;
        public int GetCurrentHealth() => _currentHealth;
        public int GetMaxHealth() => maxHealth;
        public bool IsSelected() => _isSelected;
        public int GetTeamId() => _teamId;
        public float GetAttackRange() => attackRange;
    }
}
