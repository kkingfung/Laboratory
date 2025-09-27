using Unity.Entities;

namespace Laboratory.Core.ECS.Components
{
    /// <summary>
    /// Tag components for categorizing creatures in the ECS system
    /// </summary>
    
    /// <summary>
    /// Tag for wild creatures that behave naturally and are not owned by players
    /// </summary>
    public struct WildCreatureTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures owned by a player
    /// </summary>
    public struct OwnedCreatureTag : IComponentData
    {
        public Entity owner; // Reference to the player entity that owns this creature
    }
    
    /// <summary>
    /// Tag for creatures that are ready to breed (adult, healthy, etc.)
    /// </summary>
    public struct ReadyToBreedTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures currently in combat
    /// </summary>
    public struct InCombatTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures that are pregnant/carrying offspring
    /// </summary>
    public struct PregnantTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures that are juveniles (not yet adult)
    /// </summary>
    public struct JuvenileTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures that are elders (in final life stages)
    /// </summary>
    public struct ElderTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures that are sick or injured
    /// </summary>
    public struct SickTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag for creatures that are dead (for cleanup purposes)
    /// </summary>
    public struct DeadTag : IComponentData
    {
    }
}
