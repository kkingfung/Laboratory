using UnityEngine;
using Laboratory.Core.Health;
using CoreDamageRequest = Laboratory.Core.Health.DamageRequest;

namespace Laboratory.Subsystems.Combat
{
    /// <summary>
    /// Base weapon class for all weapons in the 3D action game.
    /// Supports melee and ranged weapons with different attack patterns.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] protected float damage = 25f;
        [SerializeField] protected float range = 2f;
        [SerializeField] protected float attackCooldown = 1f;
        [SerializeField] protected int maxAmmo = -1; // -1 for infinite ammo
        [SerializeField] protected float reloadTime = 2f;

        [Header("Audio")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip attackSound;
        [SerializeField] protected AudioClip reloadSound;
        [SerializeField] protected AudioClip emptySound;

        [Header("Effects")]
        [SerializeField] protected GameObject muzzleFlash;
        [SerializeField] protected GameObject impactEffect;
        [SerializeField] protected Transform firePoint;

        // State
        protected float lastAttackTime;
        protected int currentAmmo;
        protected bool isReloading = false;
        protected bool isEquipped = false;

        // Events
        public System.Action<int, int> OnAmmoChanged; // current, max
        public System.Action OnReloadStart;
        public System.Action OnReloadComplete;
        public System.Action OnAttack;

        #region Properties

        public float Damage => damage;
        public float Range => range;
        public bool CanAttack => Time.time >= lastAttackTime + attackCooldown && !isReloading && HasAmmo();
        public bool HasAmmo() => maxAmmo == -1 || currentAmmo > 0;
        public bool IsReloading => isReloading;
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            currentAmmo = maxAmmo;
        }

        protected virtual void Start()
        {
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        #endregion

        #region Weapon Control

        public virtual void Equip()
        {
            isEquipped = true;
            gameObject.SetActive(true);
        }

        public virtual void Unequip()
        {
            isEquipped = false;
            gameObject.SetActive(false);
        }

        public virtual bool TryAttack(Vector3 attackDirection)
        {
            if (!CanAttack) return false;

            if (!HasAmmo())
            {
                PlaySound(emptySound);
                return false;
            }

            PerformAttack(attackDirection);
            return true;
        }

        protected virtual void PerformAttack(Vector3 attackDirection)
        {
            lastAttackTime = Time.time;
            
            if (maxAmmo > 0)
            {
                currentAmmo--;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            }

            PlaySound(attackSound);
            OnAttack?.Invoke();

            // Show muzzle flash
            if (muzzleFlash && firePoint)
            {
                var flash = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.1f);
            }

            // Perform weapon-specific attack
            ExecuteAttack(attackDirection);
        }

        protected abstract void ExecuteAttack(Vector3 attackDirection);

        public virtual void Reload()
        {
            if (isReloading || currentAmmo == maxAmmo || maxAmmo == -1) return;

            StartCoroutine(ReloadCoroutine());
        }

        private System.Collections.IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReloadStart?.Invoke();
            PlaySound(reloadSound);

            yield return new UnityEngine.WaitForSeconds(reloadTime);

            currentAmmo = maxAmmo;
            isReloading = false;
            OnReloadComplete?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        #endregion

        #region Utility

        protected virtual void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        protected virtual void DealDamage(Collider target, float damageAmount)
        {
            IHealthComponent health = target.GetComponent<IHealthComponent>();
            if (health != null)
            {
                var damageRequest = new CoreDamageRequest(damageAmount, gameObject);
                health.TakeDamage(damageRequest);

                // Show impact effect
                if (impactEffect)
                {
                    Instantiate(impactEffect, target.transform.position, Quaternion.identity);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Melee weapon implementation (sword, axe, etc.)
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        [Header("Melee Settings")]
        [SerializeField] private float attackAngle = 60f;
        [SerializeField] private LayerMask targetLayers = -1;

        protected override void ExecuteAttack(Vector3 attackDirection)
        {
            // Detect enemies in attack range and angle
            Collider[] potentialTargets = Physics.OverlapSphere(transform.position, range);

            foreach (Collider target in potentialTargets)
            {
                if (IsValidTarget(target, attackDirection))
                {
                    DealDamage(target, damage);
                }
            }
        }

        private bool IsValidTarget(Collider target, Vector3 attackDirection)
        {
            // Check if target is in the correct layer
            if ((targetLayers.value & (1 << target.gameObject.layer)) == 0)
                return false;

            // Check if target is within attack angle
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(attackDirection, directionToTarget);

            return angle <= attackAngle / 2f;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, range);

            // Draw attack angle
            Gizmos.color = Color.yellow;
            Vector3 forward = transform.forward;
            Vector3 leftBound = Quaternion.Euler(0, -attackAngle / 2f, 0) * forward;
            Vector3 rightBound = Quaternion.Euler(0, attackAngle / 2f, 0) * forward;
            
            Gizmos.DrawRay(transform.position, leftBound * range);
            Gizmos.DrawRay(transform.position, rightBound * range);
        }
    }

    /// <summary>
    /// Ranged weapon implementation (gun, bow, etc.)
    /// </summary>
    public class RangedWeapon : Weapon
    {
        [Header("Ranged Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private bool useRaycast = false;
        [SerializeField] private LayerMask targetLayers = -1;

        protected override void ExecuteAttack(Vector3 attackDirection)
        {
            if (useRaycast)
            {
                PerformRaycastAttack(attackDirection);
            }
            else
            {
                FireProjectile(attackDirection);
            }
        }

        private void PerformRaycastAttack(Vector3 attackDirection)
        {
            RaycastHit hit;
            Vector3 startPosition = firePoint ? firePoint.position : transform.position;

            if (Physics.Raycast(startPosition, attackDirection, out hit, range, targetLayers))
            {
                DealDamage(hit.collider, damage);

                // Draw debug line
                Debug.DrawRay(startPosition, attackDirection * hit.distance, Color.red, 0.5f);
            }
            else
            {
                Debug.DrawRay(startPosition, attackDirection * range, Color.white, 0.5f);
            }
        }

        private void FireProjectile(Vector3 attackDirection)
        {
            if (projectilePrefab && firePoint)
            {
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.linearVelocity = attackDirection * projectileSpeed;
                }

                // Set up projectile component
                Projectile proj = projectile.GetComponent<Projectile>();
                if (proj)
                {
                    proj.Initialize(damage, range, targetLayers);
                }

                // Destroy projectile after range time
                Destroy(projectile, range / projectileSpeed);
            }
        }
    }

    /// <summary>
    /// Projectile component for ranged weapons
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private float damage;
        private float maxDistance;
        private LayerMask targetLayers;
        private Vector3 startPosition;
        private bool hasHit = false;

        [SerializeField] private GameObject hitEffect;

        public void Initialize(float projDamage, float projRange, LayerMask layers)
        {
            damage = projDamage;
            maxDistance = projRange;
            targetLayers = layers;
            startPosition = transform.position;
        }

        private void Update()
        {
            // Check if projectile has traveled too far
            if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Check if target is in the correct layer
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            hasHit = true;

            // Deal damage
            IHealthComponent health = other.GetComponent<IHealthComponent>();
            if (health != null)
            {
                var damageRequest = new CoreDamageRequest(damage, gameObject);
                health.TakeDamage(damageRequest);
            }

            // Show hit effect
            if (hitEffect)
            {
                Instantiate(hitEffect, transform.position, transform.rotation);
            }

            DestroyProjectile();
        }

        private void DestroyProjectile()
        {
            Destroy(gameObject);
        }
    }
}
