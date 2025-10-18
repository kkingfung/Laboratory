namespace Laboratory.Core.Activities.Types
{
    public enum ActivityType : byte
    {
        None,
        Racing,
        Combat,
        Puzzle,
        Strategy,
        Music,
        Adventure,
        Platforming,
        Crafting
    }

    public enum ActivityStatus : byte
    {
        NotParticipating,
        Queued,
        Warming_Up,
        Active,
        Completed,
        Failed,
        Rewarded
    }
}