namespace Laboratory.Core.Abilities.Systems
{
    /// <summary>
    /// Interface for core ability systems in the abilities layer  
    /// </summary>
    public interface ICoreAbilitySystem
    {
        bool ExecuteAbility(string abilityId);
        bool CanExecuteAbility(string abilityId);
        void RegisterAbility(string abilityId, object ability);
        void UnregisterAbility(string abilityId);
    }
}
