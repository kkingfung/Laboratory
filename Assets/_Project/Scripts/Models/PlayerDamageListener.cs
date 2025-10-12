using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.Infrastructure;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models
{
    /// <summary>
    /// Player damage listener component
    /// </summary>
    public class PlayerDamageListener : MonoBehaviour
    {
        [Header("Health Configuration")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool logDamageEvents = true;

        private ECSHealthComponent healthComponent;
        private IEventBus eventBus;

        private void Awake()
        {
            healthComponent = ECSHealthComponent.Create(maxHealth);
            
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                eventBus = serviceContainer.ResolveService<IEventBus>();
            }
        }

        public void TakeDamage(float damageAmount, DamageType damageType = DamageType.Normal)
        {
            healthComponent = healthComponent.TakeDamage(damageAmount);
            
            if (logDamageEvents)
            {
                Debug.Log($"Player took {damageAmount} {damageType} damage. Health: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}");
            }

            eventBus?.Publish(new PlayerDamagedEvent(damageAmount, damageType, healthComponent.CurrentHealth));
        }

        public void Heal(float healAmount)
        {
            healthComponent = healthComponent.Heal(healAmount);
            Debug.Log($"Player healed {healAmount}. Health: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}");
        }

        public float GetCurrentHealth() => healthComponent.CurrentHealth;
        public float GetMaxHealth() => healthComponent.MaxHealth;
        public bool IsAlive() => healthComponent.IsAlive;
    }

    /// <summary>
    /// Event fired when player takes damage
    /// </summary>
    public class PlayerDamagedEvent
    {
        public float DamageAmount { get; }
        public DamageType DamageType { get; }
        public float CurrentHealth { get; }

        public PlayerDamagedEvent(float damageAmount, DamageType damageType, float currentHealth)
        {
            DamageAmount = damageAmount;
            DamageType = damageType;
            CurrentHealth = currentHealth;
        }
    }
}
