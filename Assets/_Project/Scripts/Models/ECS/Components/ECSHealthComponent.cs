using Unity.Entities;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// ECS component representing health values for entities in the simulation.
    /// This component is used for entities that require health tracking in ECS systems.
    /// </summary>
    public struct ECSHealthComponent : IComponentData
    {
        #region Public Fields

        /// <summary>Current health value of the entity</summary>
        public int CurrentHealth;
        
        /// <summary>Maximum health value of the entity</summary>
        public int MaxHealth;
        
        /// <summary>Whether the entity is currently alive</summary>
        public bool IsAlive;
        
        /// <summary>Time stamp of the last damage received</summary>
        public float LastDamageTime;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a health component with specified max health, starting at full health.
        /// </summary>
        /// <param name="maxHealth">Maximum health value</param>
        /// <returns>New health component</returns>
        public static ECSHealthComponent Create(int maxHealth)
        {
            return new ECSHealthComponent
            {
                CurrentHealth = maxHealth,
                MaxHealth = maxHealth,
                IsAlive = true,
                LastDamageTime = 0f
            };
        }

        /// <summary>
        /// Creates a health component with specified current and max health.
        /// </summary>
        /// <param name="currentHealth">Current health value</param>
        /// <param name="maxHealth">Maximum health value</param>
        /// <returns>New health component</returns>
        public static ECSHealthComponent Create(int currentHealth, int maxHealth)
        {
            return new ECSHealthComponent
            {
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth,
                IsAlive = currentHealth > 0,
                LastDamageTime = 0f
            };
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the health percentage (0.0 to 1.0)
        /// </summary>
        public readonly float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        /// <summary>
        /// Gets whether the entity is at full health
        /// </summary>
        public readonly bool IsFullHealth => CurrentHealth == MaxHealth;

        /// <summary>
        /// Gets whether the entity is critically injured (below 25% health)
        /// </summary>
        public readonly bool IsCritical => HealthPercentage < 0.25f && IsAlive;

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies damage to the health component.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="currentTime">Current game time</param>
        /// <returns>True if damage was applied, false if entity was already dead</returns>
        public bool TakeDamage(int damage, float currentTime)
        {
            if (!IsAlive || damage <= 0)
                return false;

            CurrentHealth = UnityEngine.Mathf.Max(0, CurrentHealth - damage);
            LastDamageTime = currentTime;
            
            if (CurrentHealth <= 0)
            {
                IsAlive = false;
            }

            return true;
        }

        /// <summary>
        /// Heals the entity for the specified amount.
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        /// <returns>True if healing was applied, false if entity was dead or at full health</returns>
        public bool Heal(int healAmount)
        {
            if (!IsAlive || healAmount <= 0 || IsFullHealth)
                return false;

            CurrentHealth = UnityEngine.Mathf.Min(MaxHealth, CurrentHealth + healAmount);
            return true;
        }

        /// <summary>
        /// Restores the entity to full health and marks it as alive.
        /// </summary>
        public void RestoreToFullHealth()
        {
            CurrentHealth = MaxHealth;
            IsAlive = true;
            LastDamageTime = 0f;
        }

        /// <summary>
        /// Sets the maximum health and adjusts current health if necessary.
        /// </summary>
        /// <param name="newMaxHealth">New maximum health value</param>
        public void SetMaxHealth(int newMaxHealth)
        {
            if (newMaxHealth <= 0) return;

            MaxHealth = newMaxHealth;
            
            // If current health exceeds new max, clamp it
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates that the health component has consistent values.
        /// </summary>
        /// <returns>True if component is valid, false otherwise</returns>
        public readonly bool IsValid()
        {
            return MaxHealth > 0 &&
                   CurrentHealth >= 0 &&
                   CurrentHealth <= MaxHealth &&
                   IsAlive == (CurrentHealth > 0);
        }

        /// <summary>
        /// Fixes any inconsistencies in the health component values.
        /// </summary>
        public void Validate()
        {
            if (MaxHealth <= 0)
                MaxHealth = 100;

            CurrentHealth = UnityEngine.Mathf.Clamp(CurrentHealth, 0, MaxHealth);
            IsAlive = CurrentHealth > 0;
        }

        #endregion

        #region Equality

        /// <summary>
        /// Compares two health components for equality.
        /// </summary>
        /// <param name="other">Other health component</param>
        /// <returns>True if components are equal</returns>
        public readonly bool Equals(ECSHealthComponent other)
        {
            return CurrentHealth == other.CurrentHealth &&
                   MaxHealth == other.MaxHealth &&
                   IsAlive == other.IsAlive &&
                   UnityEngine.Mathf.Approximately(LastDamageTime, other.LastDamageTime);
        }

        /// <summary>
        /// Generates hash code for the health component.
        /// </summary>
        /// <returns>Hash code</returns>
        public override readonly int GetHashCode()
        {
            return System.HashCode.Combine(CurrentHealth, MaxHealth, IsAlive, LastDamageTime);
        }

        #endregion
    }
}
