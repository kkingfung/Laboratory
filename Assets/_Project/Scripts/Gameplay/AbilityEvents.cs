namespace Game.Abilities
{
    public record AbilityStateChangedEvent(int AbilityIndex, bool IsOnCooldown, float CooldownRemaining);
    public record AbilityActivatedEvent(int AbilityIndex);
}
