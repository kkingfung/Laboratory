// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;

// Component to represent health on an entity
public struct Health : IComponentData
{
    public float CurrentHealth;
    public float MaxHealth;
}

// System to handle health logic
public class HealthSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Process entities with a Health component
        Entities.ForEach((ref Health health) =>
        {
            // Ensure current health does not exceed max health
            if (health.CurrentHealth > health.MaxHealth)
            {
                health.CurrentHealth = health.MaxHealth;
            }

            // Check if the entity's health has dropped to zero or below
            if (health.CurrentHealth <= 0)
            {
                // Handle entity death (e.g., mark for destruction or trigger events)
                // For demonstration, we simply log the death
                UnityEngine.Debug.Log("Entity has died.");

                // Optionally, you can destroy the entity
                // EntityManager.DestroyEntity(entity);
            }
        }).ScheduleParallel();
    }
}

// Utility system to apply damage to entities
public class DamageSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Example: Apply periodic damage to all entities with Health
        Entities.ForEach((ref Health health) =>
        {
            float damagePerSecond = 10f; // Example damage value
            health.CurrentHealth -= damagePerSecond * deltaTime;
        }).ScheduleParallel();
    }
}