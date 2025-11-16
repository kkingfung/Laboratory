namespace Laboratory.Core.Activities.Types
{
    /// <summary>
    /// Types of activities that creatures can participate in
    /// </summary>
    public enum ActivityType : byte
    {
        /// <summary>No activity</summary>
        None = 0,
        /// <summary>Combat activity</summary>
        Combat = 1,
        /// <summary>Racing activity</summary>
        Racing = 2,
        /// <summary>Puzzle-solving activity</summary>
        Puzzle = 3,
        /// <summary>Exploration activity</summary>
        Exploration = 4,
        /// <summary>Social activity</summary>
        Social = 5,
        /// <summary>Training activity</summary>
        Training = 6,
        /// <summary>Breeding activity</summary>
        Breeding = 7,
        /// <summary>Foraging activity</summary>
        Foraging = 8,
        /// <summary>Resting activity</summary>
        Resting = 9,
        /// <summary>Strategy activity</summary>
        Strategy = 10,
        /// <summary>Music activity</summary>
        Music = 11,
        /// <summary>Adventure activity</summary>
        Adventure = 12,
        /// <summary>Platforming activity</summary>
        Platforming = 13,
        /// <summary>Crafting activity</summary>
        Crafting = 14,
        /// <summary>Custom activity</summary>
        Custom = 255
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