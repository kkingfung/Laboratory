using UnityEngine;
using Laboratory.Subsystems.Player;
using Laboratory.Core.Management;

namespace Laboratory.Gameplay
{
    /// <summary>
    /// Base class for all collectible items in the 3D action game.
    /// Supports health potions, score items, power-ups, and quest items.
    /// </summary>
    public abstract class Pickup : MonoBehaviour, IInteractable
    {
        [Header("Pickup Settings")]
        [SerializeField] protected float rotationSpeed = 50f;
        [SerializeField] protected float bobHeight = 0.5f;
        [SerializeField] protected float bobSpeed = 2f;
        [SerializeField] protected bool autoPickup = true;
        [SerializeField] protected float pickupRange = 2f;

        [Header("Effects")]
        [SerializeField] protected GameObject pickupEffect;
        [SerializeField] protected AudioClip pickupSound;
        [SerializeField] protected float effectDuration = 1f;

        protected Vector3 startPosition;
        protected AudioSource audioSource;
        protected bool isCollected = false;

        // Performance optimization: cache player detection to reduce Physics calls
        private float lastPlayerCheckTime;
        private float playerCheckInterval = 0.2f; // Check for players 5 times per second instead of every frame

        protected virtual void Awake()
        {
            startPosition = transform.position;
            audioSource = GetComponent<AudioSource>();
        }

        protected virtual void Update()
        {
            if (isCollected) return;

            AnimatePickup();

            if (autoPickup)
            {
                CheckForPlayerInRange();
            }
        }

        protected virtual void AnimatePickup()
        {
            // Rotate the pickup
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

            // Bob up and down
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        protected virtual void CheckForPlayerInRange()
        {
            // Performance optimized player check - only check every playerCheckInterval seconds
            if (Time.time - lastPlayerCheckTime < playerCheckInterval)
                return;

            lastPlayerCheckTime = Time.time;

            Collider[] playersInRange = Physics.OverlapSphere(transform.position, pickupRange, LayerMask.GetMask("Player"));

            if (playersInRange.Length > 0)
            {
                PlayerController player = playersInRange[0].GetComponent<PlayerController>();
                if (player != null)
                {
                    CollectItem(player);
                }
            }
        }

        public virtual void Interact(PlayerController player)
        {
            if (!autoPickup)
            {
                CollectItem(player);
            }
        }

        public abstract string GetInteractionPrompt();
        
        public virtual bool CanInteract(PlayerController player)
        {
            return !isCollected;
        }

        protected virtual void CollectItem(PlayerController player)
        {
            if (isCollected) return;

            isCollected = true;

            // Apply pickup effect
            ApplyPickupEffect(player);

            // Play sound and effects
            PlayPickupEffects();

            // Destroy the pickup
            Destroy(gameObject, effectDuration);
        }

        protected abstract void ApplyPickupEffect(PlayerController player);

        protected virtual void PlayPickupEffects()
        {
            if (pickupSound && audioSource)
            {
                audioSource.PlayOneShot(pickupSound);
            }

            if (pickupEffect)
            {
                Instantiate(pickupEffect, transform.position, transform.rotation);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }

    /// <summary>
    /// Health pickup that restores player health
    /// </summary>
    public class HealthPickup : Pickup
    {
        [Header("Health Settings")]
        [SerializeField] private int healAmount = 25;

        public override string GetInteractionPrompt()
        {
            return $"Pick up Health Potion (+{healAmount} HP)";
        }

        protected override void ApplyPickupEffect(PlayerController player)
        {
            player.Heal(healAmount);
            
            if (Laboratory.Core.GameManager.Instance)
            {
                Laboratory.Core.GameManager.Instance.AddScore(25);
            }
        }
    }

    /// <summary>
    /// Score pickup that adds points to player score
    /// </summary>
    public class ScorePickup : Pickup
    {
        [Header("Score Settings")]
        [SerializeField] private int scoreValue = 100;

        public override string GetInteractionPrompt()
        {
            return $"Collect ({scoreValue} points)";
        }

        protected override void ApplyPickupEffect(PlayerController player)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
            }
        }
    }

    /// <summary>
    /// Power-up pickup that gives temporary abilities
    /// </summary>
    public class PowerUpPickup : Pickup
    {
        [Header("Power-up Settings")]
        [SerializeField] private PowerUpType powerUpType;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float multiplier = 2f;

        public enum PowerUpType
        {
            SpeedBoost,
            DamageBoost,
            Invincibility
        }

        public override string GetInteractionPrompt()
        {
            return $"Power-up: {powerUpType}";
        }

        protected override void ApplyPickupEffect(PlayerController player)
        {
            // Apply power-up effect based on type
            switch (powerUpType)
            {
                case PowerUpType.SpeedBoost:
                    ApplySpeedBoost(player);
                    break;
                case PowerUpType.DamageBoost:
                    ApplyDamageBoost(player);
                    break;
                case PowerUpType.Invincibility:
                    ApplyInvincibility(player);
                    break;
            }

            if (Laboratory.Core.GameManager.Instance)
            {
                Laboratory.Core.GameManager.Instance.AddScore(50);
            }
        }

        private void ApplySpeedBoost(PlayerController player)
        {
            // This would require modifying PlayerController to support temporary speed changes
            Debug.Log($"Speed boost applied for {duration} seconds with {multiplier}x multiplier!");
        }

        private void ApplyDamageBoost(PlayerController player)
        {
            // This would require modifying PlayerController to support damage multipliers
            Debug.Log($"Damage boost applied for {duration} seconds with {multiplier}x multiplier!");
        }

        private void ApplyInvincibility(PlayerController player)
        {
            // This would require modifying PlayerController to support invincibility
            Debug.Log($"Invincibility applied for {duration} seconds!");
        }
    }

    /// <summary>
    /// Key pickup for unlocking doors or areas
    /// </summary>
    public class KeyPickup : Pickup
    {
        [Header("Key Settings")]
        [SerializeField] private string keyId = "RedKey";
        [SerializeField] private KeyType keyType = KeyType.Red;

        public enum KeyType
        {
            Red,
            Blue,
            Yellow,
            Master
        }

        public override string GetInteractionPrompt()
        {
            return $"Pick up {keyType} Key";
        }

        protected override void ApplyPickupEffect(PlayerController player)
        {
            // Add key to player inventory
            // This would require an inventory system on the player
            Debug.Log($"Collected {keyType} key with ID: {keyId}");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(200);
            }
        }
    }
}
