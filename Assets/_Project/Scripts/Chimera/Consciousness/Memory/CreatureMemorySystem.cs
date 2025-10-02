using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Consciousness.Core;

namespace Laboratory.Chimera.Consciousness.Memory
{
    /// <summary>
    /// Advanced memory system allowing creatures to remember players, events, and relationships
    /// Creates deep emotional connections and realistic behavioral responses
    /// </summary>
    [Serializable]
    public struct CreatureMemory : IComponentData
    {
        // Player relationships
        public PlayerRelationship PrimaryBond;
        public FixedList32Bytes<PlayerRelationship> KnownPlayers;

        // Creature relationships
        public FixedList32Bytes<CreatureRelationship> Friends;
        public FixedList32Bytes<CreatureRelationship> Rivals;

        // Location memories
        public FixedList64Bytes<LocationMemory> ImportantPlaces;

        // Experience memories
        public FixedList128Bytes<ExperienceMemory> SignificantEvents;

        // Learning memories
        public FixedList64Bytes<LearnedBehavior> AcquiredSkills;

        // Memory metadata
        public float OverallMemoryStrength;
        public int DaysAlive;
        public uint LastMemoryUpdate;

        /// <summary>
        /// Remember a player interaction
        /// </summary>
        public void RememberPlayerInteraction(string playerID, InteractionType interaction, float emotionalImpact)
        {
            // Find existing relationship or create new one
            bool foundPlayer = false;
            for (int i = 0; i < KnownPlayers.Length; i++)
            {
                if (KnownPlayers[i].PlayerID.ToString() == playerID)
                {
                    var relationship = KnownPlayers[i];
                    relationship.UpdateRelationship(interaction, emotionalImpact);
                    KnownPlayers[i] = relationship;
                    foundPlayer = true;
                    break;
                }
            }

            if (!foundPlayer && KnownPlayers.Length < KnownPlayers.Capacity)
            {
                var newRelationship = new PlayerRelationship
                {
                    PlayerID = new FixedString64Bytes(playerID),
                    TrustLevel = interaction == InteractionType.Positive ? emotionalImpact * 0.5f : -emotionalImpact * 0.3f,
                    InteractionCount = 1,
                    LastInteractionDay = DaysAlive,
                    RelationshipType = DetermineRelationshipType(emotionalImpact, interaction)
                };

                KnownPlayers.Add(newRelationship);

                // Update primary bond if this is stronger
                if (newRelationship.TrustLevel > PrimaryBond.TrustLevel)
                {
                    PrimaryBond = newRelationship;
                }
            }
        }

        /// <summary>
        /// Remember an important experience
        /// </summary>
        public void RememberExperience(ExperienceType experience, Vector3 location, float emotionalWeight)
        {
            if (SignificantEvents.Length >= SignificantEvents.Capacity)
            {
                // Remove oldest memory to make space
                RemoveOldestMemory();
            }

            var newMemory = new ExperienceMemory
            {
                Experience = experience,
                Location = location,
                EmotionalWeight = emotionalWeight,
                DayOccurred = DaysAlive,
                MemoryStrength = CalculateInitialMemoryStrength(emotionalWeight)
            };

            SignificantEvents.Add(newMemory);
        }

        /// <summary>
        /// Remember a location as important
        /// </summary>
        public void RememberLocation(Vector3 position, LocationType type, float emotionalSignificance)
        {
            // Check if location already exists
            for (int i = 0; i < ImportantPlaces.Length; i++)
            {
                if (Vector3.Distance(ImportantPlaces[i].Position, position) < 5f)
                {
                    var location = ImportantPlaces[i];
                    location.EmotionalSignificance = Mathf.Max(location.EmotionalSignificance, emotionalSignificance);
                    location.VisitCount++;
                    ImportantPlaces[i] = location;
                    return;
                }
            }

            // Add new location if space available
            if (ImportantPlaces.Length < ImportantPlaces.Capacity)
            {
                var newLocation = new LocationMemory
                {
                    Position = position,
                    Type = type,
                    EmotionalSignificance = emotionalSignificance,
                    VisitCount = 1,
                    DayDiscovered = DaysAlive
                };

                ImportantPlaces.Add(newLocation);
            }
        }

        /// <summary>
        /// Learn a new behavior through experience
        /// </summary>
        public void LearnBehavior(BehaviorType behavior, float proficiency, string context)
        {
            // Check if behavior already known
            for (int i = 0; i < AcquiredSkills.Length; i++)
            {
                if (AcquiredSkills[i].Behavior == behavior)
                {
                    var skill = AcquiredSkills[i];
                    skill.Proficiency = Mathf.Min(1f, skill.Proficiency + proficiency * 0.1f);
                    skill.TimesUsed++;
                    AcquiredSkills[i] = skill;
                    return;
                }
            }

            // Add new skill if space available
            if (AcquiredSkills.Length < AcquiredSkills.Capacity)
            {
                var newSkill = new LearnedBehavior
                {
                    Behavior = behavior,
                    Proficiency = proficiency,
                    TimesUsed = 1,
                    DayLearned = DaysAlive,
                    Context = new FixedString32Bytes(context)
                };

                AcquiredSkills.Add(newSkill);
            }
        }

        /// <summary>
        /// Get emotional response to a player
        /// </summary>
        public EmotionalResponse GetPlayerResponse(string playerID)
        {
            for (int i = 0; i < KnownPlayers.Length; i++)
            {
                if (KnownPlayers[i].PlayerID.ToString() == playerID)
                {
                    return KnownPlayers[i].GetEmotionalResponse();
                }
            }

            return EmotionalResponse.Neutral; // Unknown player
        }

        /// <summary>
        /// Get comfort level at a location
        /// </summary>
        public float GetLocationComfort(Vector3 position)
        {
            float comfort = 0.5f; // Base comfort

            for (int i = 0; i < ImportantPlaces.Length; i++)
            {
                float distance = Vector3.Distance(ImportantPlaces[i].Position, position);
                if (distance < 10f)
                {
                    float influence = 1f - (distance / 10f);
                    comfort += ImportantPlaces[i].EmotionalSignificance * influence;
                }
            }

            return Mathf.Clamp01(comfort);
        }

        /// <summary>
        /// Check if creature knows a behavior
        /// </summary>
        public float GetBehaviorProficiency(BehaviorType behavior)
        {
            for (int i = 0; i < AcquiredSkills.Length; i++)
            {
                if (AcquiredSkills[i].Behavior == behavior)
                {
                    return AcquiredSkills[i].Proficiency;
                }
            }
            return 0f;
        }

        /// <summary>
        /// Update memory strength over time (some memories fade)
        /// </summary>
        public void UpdateMemoryStrength()
        {
            // Fade experience memories over time
            for (int i = SignificantEvents.Length - 1; i >= 0; i--)
            {
                var memory = SignificantEvents[i];
                int daysSince = DaysAlive - memory.DayOccurred;
                memory.MemoryStrength *= CalculateMemoryDecay(daysSince, memory.EmotionalWeight);

                if (memory.MemoryStrength < 0.1f)
                {
                    SignificantEvents.RemoveAt(i); // Forget weak memories
                }
                else
                {
                    SignificantEvents[i] = memory;
                }
            }

            // Update relationship strengths
            for (int i = 0; i < KnownPlayers.Length; i++)
            {
                var relationship = KnownPlayers[i];
                int daysSince = DaysAlive - relationship.LastInteractionDay;
                if (daysSince > 7) // Relationships fade if no interaction
                {
                    relationship.TrustLevel *= 0.98f; // Slow decay
                    KnownPlayers[i] = relationship;
                }
            }
        }

        // Helper methods
        private RelationshipType DetermineRelationshipType(float emotionalImpact, InteractionType interaction)
        {
            if (interaction == InteractionType.Positive && emotionalImpact > 0.7f)
                return RelationshipType.Beloved;
            if (interaction == InteractionType.Positive && emotionalImpact > 0.4f)
                return RelationshipType.Trusted;
            if (interaction == InteractionType.Negative && emotionalImpact > 0.5f)
                return RelationshipType.Feared;

            return RelationshipType.Acquaintance;
        }

        private float CalculateInitialMemoryStrength(float emotionalWeight)
        {
            return Mathf.Clamp01(0.3f + emotionalWeight * 0.7f);
        }

        private float CalculateMemoryDecay(int daysSince, float emotionalWeight)
        {
            // Strong emotional memories last longer
            float emotionalBonus = emotionalWeight * 0.5f;
            float decayRate = 0.02f - emotionalBonus * 0.01f;
            return Mathf.Max(0.1f, 1f - (daysSince * decayRate));
        }

        private void RemoveOldestMemory()
        {
            if (SignificantEvents.Length == 0) return;

            int oldestIndex = 0;
            int oldestDay = SignificantEvents[0].DayOccurred;

            for (int i = 1; i < SignificantEvents.Length; i++)
            {
                if (SignificantEvents[i].DayOccurred < oldestDay)
                {
                    oldestDay = SignificantEvents[i].DayOccurred;
                    oldestIndex = i;
                }
            }

            SignificantEvents.RemoveAt(oldestIndex);
        }
    }

    /// <summary>
    /// Player relationship data
    /// </summary>
    [Serializable]
    public struct PlayerRelationship
    {
        public FixedString64Bytes PlayerID;
        public float TrustLevel;           // -1.0 to 1.0
        public int InteractionCount;
        public int LastInteractionDay;
        public RelationshipType RelationshipType;

        public void UpdateRelationship(InteractionType interaction, float impact)
        {
            InteractionCount++;

            float change = interaction == InteractionType.Positive ? impact * 0.1f : -impact * 0.15f;
            TrustLevel = Mathf.Clamp(TrustLevel + change, -1f, 1f);

            // Update relationship type based on trust level
            if (TrustLevel > 0.8f) RelationshipType = RelationshipType.Beloved;
            else if (TrustLevel > 0.4f) RelationshipType = RelationshipType.Trusted;
            else if (TrustLevel > 0f) RelationshipType = RelationshipType.Acquaintance;
            else if (TrustLevel > -0.5f) RelationshipType = RelationshipType.Wary;
            else RelationshipType = RelationshipType.Feared;
        }

        public EmotionalResponse GetEmotionalResponse()
        {
            return RelationshipType switch
            {
                RelationshipType.Beloved => EmotionalResponse.Excited,
                RelationshipType.Trusted => EmotionalResponse.Happy,
                RelationshipType.Acquaintance => EmotionalResponse.Curious,
                RelationshipType.Wary => EmotionalResponse.Nervous,
                RelationshipType.Feared => EmotionalResponse.Fearful,
                _ => EmotionalResponse.Neutral
            };
        }
    }

    /// <summary>
    /// Other structs for memory system
    /// </summary>
    [Serializable]
    public struct CreatureRelationship
    {
        public Entity CreatureEntity;
        public RelationshipType Type;
        public float BondStrength;
        public int DayMet;
    }

    [Serializable]
    public struct LocationMemory
    {
        public Vector3 Position;
        public LocationType Type;
        public float EmotionalSignificance; // -1.0 to 1.0
        public int VisitCount;
        public int DayDiscovered;
    }

    [Serializable]
    public struct ExperienceMemory
    {
        public ExperienceType Experience;
        public Vector3 Location;
        public float EmotionalWeight;
        public int DayOccurred;
        public float MemoryStrength;
    }

    [Serializable]
    public struct LearnedBehavior
    {
        public BehaviorType Behavior;
        public float Proficiency;
        public int TimesUsed;
        public int DayLearned;
        public FixedString32Bytes Context;
    }

    // Enums for memory system
    public enum RelationshipType : byte
    {
        Unknown,
        Feared,
        Wary,
        Acquaintance,
        Trusted,
        Beloved
    }

    public enum InteractionType
    {
        Positive,
        Negative,
        Neutral
    }

    public enum EmotionalResponse
    {
        Fearful,
        Nervous,
        Neutral,
        Curious,
        Happy,
        Excited
    }

    public enum LocationType
    {
        Home,
        Feeding,
        Danger,
        Social,
        Exploration,
        Comfort
    }

    public enum BehaviorType
    {
        BasicMovement,
        Foraging,
        SocialInteraction,
        ProblemSolving,
        ToolUse,
        CombatSkills,
        PlayBehavior,
        Grooming
    }
}