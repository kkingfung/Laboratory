using System;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Discovery.Core;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Shared types and enums for the social and bonding systems
    /// </summary>

    public enum BondingEmotionalState : byte
    {
        Neutral,
        Happy,
        Sad,
        Excited,
        Anxious,
        Content,
        Nostalgic,
        Playful,
        Protective,
        Proud
    }

    public enum TriggerConditionType : byte
    {
        TimeElapsed,
        BondStrength,
        EmotionalState,
        Interaction,
        Location,
        BondStrengthThreshold,
        BondingEmotionalState,
        TimeOfDay,
        InteractionType
    }

    public enum BondingInteractionType : byte
    {
        None,
        Feeding,
        Playing,
        Training,
        Exploring,
        Resting,
        Combat,
        Discovery
    }

    public enum EmotionalMemoryType : byte
    {
        Joy,
        Trust,
        Fear,
        Anger,
        Sadness,
        Surprise,
        Disgust,
        Anticipation,
        AncestralMemory,
        Nostalgia,
        BondingMoment,
        TraumaticEvent
    }

    public enum LegacyConnectionType : byte
    {
        Ancestor,
        Descendant,
        Sibling,
        Cousin,
        LineageFounder,
        LegendaryLine,
        DirectDescendant,
        CloseRelative,
        DistantRelative,
        SharedAncestor
    }

    public enum GenerationalPatternType : byte
    {
        TrustPattern,
        PlayPattern,
        LoyaltyPattern,
        IndependencePattern,
        CuriosityPattern,
        TraitInheritance
    }

    public enum BondingMomentType
    {
        FirstMeeting,
        TrustBreakthrough,
        Adolescence,
        Maturity,
        SharedDiscovery,
        Rescue,
        Loyalty,
        Sacrifice,
        Reunion,
        Legacy
    }

    [System.Serializable]
    public struct MemoryTriggerCondition
    {
        public TriggerConditionType type;
        public float threshold;
        public BondingEmotionalState requiredState;
        public BondingInteractionType requiredInteraction;
        public float timeRange;
    }
}