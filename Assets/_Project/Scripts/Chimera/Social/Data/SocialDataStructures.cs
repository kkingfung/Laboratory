using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Chimera.Social.Data
{
    /// <summary>
    /// Core social data structures for the advanced social system
    /// </summary>

    [Serializable]
    public class SocialAgent
    {
        public uint AgentId;
        public string Name;
        public float3 Position;
        public EmotionalState CurrentEmotionalState;
        public List<uint> Relationships = new();
        public uint CurrentGroup;
        public float Charisma;
        public float Empathy;
        public float SocialIntelligence;
        public CommunicationProfile CommunicationProfile;
        public Dictionary<string, float> CulturalValues = new();
        public float SocialStanding;
    }

    [Serializable]
    public class SocialRelationship
    {
        public uint Agent1Id;
        public uint Agent2Id;
        public RelationshipType Type;
        public float Strength;
        public float Trust;
        public DateTime FormationDate;
        public List<SocialInteraction> SharedHistory = new();
    }

    [Serializable]
    public class SocialGroup
    {
        public uint GroupId;
        public string GroupName;
        public List<uint> Members = new();
        public uint LeaderId;
        public float Cohesion;
        public float3 CenterPosition;
        public Dictionary<string, float> GroupValues = new();
        public List<CulturalTrait> SharedCulture = new();
        public SocialStatus GroupStatus;
        public DateTime FormationDate;
    }

    [Serializable]
    public class SocialInteraction
    {
        public uint InitiatorId;
        public uint TargetId;
        public InteractionType Type;
        public InteractionOutcome Outcome;
        public float Intensity;
        public DateTime Timestamp;
        public Vector3 Location;
        public Dictionary<string, object> Context = new();
    }

    [Serializable]
    public class SocialInteractionResult
    {
        public uint InteractionId;
        public List<uint> AffectedAgents = new();
        public Dictionary<uint, float> RelationshipChanges = new();
        public List<CulturalTrait> CulturalTransmissions = new();
        public EmotionalState ResultingEmotion;
        public float GroupCohesionChange;
    }

    [Serializable]
    public class CulturalTrait
    {
        public string TraitName;
        public string Description;
        public float Prevalence;
        public float TransmissionRate;
        public Dictionary<string, float> AssociatedValues = new();
        public DateTime EmergenceDate;
    }

    [Serializable]
    public class CommunicationProfile
    {
        public CommunicationStyle Style;
        public float Expressiveness;
        public float Receptiveness;
        public List<string> PreferredTopics = new();
        public Dictionary<string, float> LanguageProficiency = new();
        public float NonVerbalCommunication;
    }

    [Serializable]
    public class SocialRank
    {
        public uint AgentId;
        public int HierarchyLevel;
        public float Influence;
        public List<uint> Subordinates = new();
        public uint Superior;
        public DateTime RankDate;
    }

    [Serializable]
    public class Leadership
    {
        public uint LeaderId;
        public uint GroupId;
        public LeadershipStyle Style;
        public float Effectiveness;
        public float PopularityRating;
        public List<uint> Supporters = new();
        public List<uint> Opposition = new();
        public DateTime LeadershipStart;
    }

    [Serializable]
    public class IndividualSocialProfile
    {
        public uint AgentId;
        public int TotalInteractions;
        public float AverageRelationshipStrength;
        public int GroupMemberships;
        public int LeadershipRoles;
        public float CulturalInfluence;
        public Dictionary<InteractionType, int> InteractionHistory = new();
        public List<string> PersonalValues = new();
    }
}