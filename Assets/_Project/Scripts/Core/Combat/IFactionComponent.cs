using System;

namespace Laboratory.Core.Combat
{
    /// <summary>
    /// Interface for faction-based entities in the combat system.
    /// Enables faction relationships and diplomatic systems.
    /// </summary>
    public interface IFactionComponent
    {
        /// <summary>Unique identifier for the faction</summary>
        string FactionId { get; }

        /// <summary>Display name of the faction</summary>
        string FactionName { get; }

        /// <summary>Faction type for categorization</summary>
        FactionType FactionType { get; }

        /// <summary>Current diplomatic stance</summary>
        DiplomaticStance Stance { get; }

        /// <summary>Whether this entity can engage in diplomacy</summary>
        bool CanChangeDiplomacy { get; }

        /// <summary>Event fired when faction relationship changes</summary>
        event Action<FactionRelationshipChangeEventArgs> OnRelationshipChanged;

        /// <summary>Gets relationship with another faction</summary>
        FactionRelationship GetRelationshipWith(string otherFactionId);

        /// <summary>Sets relationship with another faction</summary>
        bool SetRelationshipWith(string otherFactionId, FactionRelationship relationship);

        /// <summary>Checks if this faction is hostile to another</summary>
        bool IsHostileTo(string otherFactionId);

        /// <summary>Checks if this faction is allied with another</summary>
        bool IsAlliedWith(string otherFactionId);

        /// <summary>Checks if this faction is neutral to another</summary>
        bool IsNeutralTo(string otherFactionId);
    }

    /// <summary>Faction relationship levels</summary>
    public enum FactionRelationship
    {
        Hostile = -2,
        Unfriendly = -1,
        Neutral = 0,
        Friendly = 1,
        Allied = 2
    }

    /// <summary>Faction types for categorization</summary>
    public enum FactionType
    {
        Player,
        Creature,
        NPC,
        Environmental,
        System
    }

    /// <summary>Diplomatic stance options</summary>
    public enum DiplomaticStance
    {
        Peaceful,
        Defensive,
        Aggressive,
        Neutral
    }

    /// <summary>Event arguments for faction relationship changes</summary>
    public class FactionRelationshipChangeEventArgs : EventArgs
    {
        public string FactionId { get; }
        public string OtherFactionId { get; }
        public FactionRelationship OldRelationship { get; }
        public FactionRelationship NewRelationship { get; }
        public DateTime Timestamp { get; }

        public FactionRelationshipChangeEventArgs(string factionId, string otherFactionId,
            FactionRelationship oldRelationship, FactionRelationship newRelationship)
        {
            FactionId = factionId;
            OtherFactionId = otherFactionId;
            OldRelationship = oldRelationship;
            NewRelationship = newRelationship;
            Timestamp = DateTime.Now;
        }
    }
}