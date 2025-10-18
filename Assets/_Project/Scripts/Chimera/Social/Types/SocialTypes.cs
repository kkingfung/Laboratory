namespace Laboratory.Chimera.Social.Types
{
    /// <summary>
    /// Enums and type definitions for the social system
    /// </summary>

    public enum InteractionType
    {
        Greeting,
        Conversation,
        Cooperation,
        Competition,
        Conflict
    }

    public enum InteractionOutcome
    {
        Positive,
        Neutral,
        Negative,
        Transformative
    }

    public enum RelationshipType
    {
        Stranger,
        Acquaintance,
        Friend,
        Rival,
        Enemy,
        Family,
        Leader,
        Follower,
        Ally,
        Mentor,
        Student
    }

    public enum SocialStatus
    {
        Outcast,
        Regular,
        Respected,
        Leader,
        Celebrity
    }

    public enum CommunicationStyle
    {
        Direct,
        Diplomatic,
        Aggressive,
        Passive,
        Charismatic,
        Analytical
    }

    public enum LeadershipStyle
    {
        Democratic,
        Authoritarian,
        Charismatic,
        Transformational,
        Servant,
        Laissez_Faire
    }

    public enum EmotionalState
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Fearful,
        Excited,
        Calm,
        Anxious,
        Confident,
        Empathetic
    }

    public enum ConflictIntensity
    {
        Disagreement,
        Tension,
        Argument,
        Fight,
        War
    }
}