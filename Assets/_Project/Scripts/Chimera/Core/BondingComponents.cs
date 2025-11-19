using System;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// Bonding component data moved to Core to break circular dependencies
    /// Used by both ECS bonding system and Social systems
    /// </summary>

    /// <summary>
    /// Component data for creature bonding
    /// </summary>
    [Serializable]
    public struct CreatureBondData : IComponentData
    {
        public int playerId;
        public int creatureId;
        public float bondStrength;
        public float loyaltyLevel;
        public float timeSinceLastInteraction;
        public float timeAlive;

        // Bonding milestones
        public bool hasHadFirstInteraction;
        public bool hasTrustBreakthrough;
        public bool hasAdolescenceMoment;
        public bool hasMaturityMoment;
        public bool hasSharedDiscoveryMoment;
        public bool participatedInDiscovery;
        public bool hasLegacyConnection;

        // Experience tracking
        public int positiveExperiences;
        public int negativeExperiences;
        public int recentPositiveInteractions;
        public BondingInteractionType lastInteractionType;

        // Emotional state
        public BondingEmotionalState currentBondingEmotionalState;
        public float emotionalStateTimer;

        // Memory system
        public FixedList128Bytes<EmotionalMemory> emotionalMemories;
    }

    /// <summary>
    /// Player's bonding history across all creatures
    /// </summary>
    [Serializable]
    public struct PlayerBondingHistory : IComponentData
    {
        public int playerId;
        public int totalCreaturesBonded;
        public int totalGenerations;
        public float overallBondingExpertise;

        // Legacy connections
        public FixedList128Bytes<LegacyConnection> legacyConnections;
        public FixedList128Bytes<PastBondData> pastBonds;
        public FixedList128Bytes<GenerationalPattern> generationalPatterns;
    }

    /// <summary>
    /// Significant bonding moments that strengthen relationships
    /// </summary>
    [Serializable]
    public struct BondingMoment
    {
        public BondingMomentType type;
        public float intensity;
        public float timestamp;
        public string description;
        public float memoryStrength;
    }

    /// <summary>
    /// Connection between current creature and past bonded creatures
    /// </summary>
    [Serializable]
    public struct LegacyConnection
    {
        public int currentCreatureId;
        public int ancestorCreatureId;
        public LegacyConnectionType connectionType;
        public float memoryStrength;
        public float discoveredAt;
        public bool isActive;
        public FixedString64Bytes description;
    }

    /// <summary>
    /// Emotional memory stored by creatures
    /// </summary>
    [Serializable]
    public struct EmotionalMemory
    {
        public EmotionalMemoryType type;
        public float strength;
        public float timestamp;
        public FixedList64Bytes<MemoryTriggerCondition> triggerConditions;
        public bool isActive;
        public FixedString64Bytes description;
    }

    /// <summary>
    /// Data about past bonded creatures
    /// </summary>
    [Serializable]
    public struct PastBondData
    {
        public int creatureId;
        public FixedString32Bytes creatureName;
        public uint geneticHash; // Simplified genetics reference
        public float finalBondStrength;
        public float bondDurationDays;
        public bool isActive;
        public float endedAt;
    }

    /// <summary>
    /// Pattern recognition across generations
    /// </summary>
    [Serializable]
    public struct GenerationalPattern
    {
        public GenerationalPatternType patternType;
        public float strength;
        public FixedList32Bytes<LegacyConnectionType> connectionTypes;
        public bool isActive;
        public float discoveredAt;
    }
}
