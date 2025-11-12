using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Service for managing social relationships between agents.
    /// Handles relationship formation, updates, decay, and lifecycle management.
    /// Extracted from AdvancedSocialSystem for single responsibility.
    /// </summary>
    public class RelationshipManagementService
    {
        private readonly float _relationshipDecayRate;
        private readonly SocialNetworkGraph _socialNetwork;
        private readonly Action<uint, uint, RelationshipType> _onRelationshipFormed;

        public RelationshipManagementService(
            float relationshipDecayRate,
            SocialNetworkGraph socialNetwork,
            Action<uint, uint, RelationshipType> onRelationshipFormed)
        {
            _relationshipDecayRate = relationshipDecayRate;
            _socialNetwork = socialNetwork;
            _onRelationshipFormed = onRelationshipFormed;
        }

        /// <summary>
        /// Updates relationship between two agents based on interaction result
        /// </summary>
        public void UpdateRelationship(
            uint agentA,
            uint agentB,
            SocialInteraction interaction,
            SocialInteractionResult result,
            Dictionary<uint, SocialAgent> socialAgents)
        {
            var agentAData = socialAgents[agentA];
            var agentBData = socialAgents[agentB];

            // Update or create relationship for agent A
            if (agentAData.relationships.TryGetValue(agentB, out var relationshipA))
            {
                relationshipA.strength = math.clamp(relationshipA.strength + result.relationshipChange, -1f, 1f);
                relationshipA.interactionCount++;
                relationshipA.lastInteraction = Time.time;
                relationshipA.relationshipHistory.Add(interaction);

                // Keep history manageable
                if (relationshipA.relationshipHistory.Count > 20)
                {
                    relationshipA.relationshipHistory.RemoveAt(0);
                }
            }
            else
            {
                relationshipA = new SocialRelationship
                {
                    targetAgent = agentB,
                    strength = result.relationshipChange,
                    relationshipType = DetermineRelationshipType(result.relationshipChange),
                    formationTime = Time.time,
                    lastInteraction = Time.time,
                    interactionCount = 1,
                    relationshipHistory = new List<SocialInteraction> { interaction }
                };
                agentAData.relationships[agentB] = relationshipA;
            }

            // Mirror for agent B
            if (agentBData.relationships.TryGetValue(agentA, out var relationshipB))
            {
                relationshipB.strength = math.clamp(relationshipB.strength + result.relationshipChange, -1f, 1f);
                relationshipB.interactionCount++;
                relationshipB.lastInteraction = Time.time;
                relationshipB.relationshipHistory.Add(interaction);

                if (relationshipB.relationshipHistory.Count > 20)
                {
                    relationshipB.relationshipHistory.RemoveAt(0);
                }
            }
            else
            {
                relationshipB = new SocialRelationship
                {
                    targetAgent = agentA,
                    strength = result.relationshipChange,
                    relationshipType = DetermineRelationshipType(result.relationshipChange),
                    formationTime = Time.time,
                    lastInteraction = Time.time,
                    interactionCount = 1,
                    relationshipHistory = new List<SocialInteraction> { interaction }
                };
                agentBData.relationships[agentA] = relationshipB;
            }

            // Update social network
            _socialNetwork.UpdateEdge(agentA, agentB, relationshipA.strength);

            // Check for relationship milestones
            if (relationshipA.strength > 0.7f && relationshipA.relationshipType != RelationshipType.Bond)
            {
                relationshipA.relationshipType = RelationshipType.Bond;
                relationshipB.relationshipType = RelationshipType.Bond;
                _onRelationshipFormed?.Invoke(agentA, agentB, RelationshipType.Bond);
            }
        }

        /// <summary>
        /// Determines relationship type based on strength
        /// </summary>
        public RelationshipType DetermineRelationshipType(float strength)
        {
            return strength switch
            {
                > 0.7f => RelationshipType.Bond,
                > 0.3f => RelationshipType.Friendly,
                > -0.3f => RelationshipType.Neutral,
                > -0.7f => RelationshipType.Antagonistic,
                _ => RelationshipType.Hostile
            };
        }

        /// <summary>
        /// Updates relationship decay for all agents over time
        /// </summary>
        public void UpdateRelationshipDecay(Dictionary<uint, SocialAgent> socialAgents, float deltaTime)
        {
            foreach (var agent in socialAgents.Values)
            {
                var expiredRelationships = new List<uint>();

                foreach (var relationship in agent.relationships.Values)
                {
                    // Relationships decay over time without interaction
                    float timeSinceInteraction = Time.time - relationship.lastInteraction;
                    float decayAmount = _relationshipDecayRate * timeSinceInteraction * deltaTime;

                    relationship.strength = math.max(-1f, relationship.strength - decayAmount);

                    // Remove very weak relationships
                    if (relationship.strength < -0.8f && relationship.interactionCount < 3)
                    {
                        expiredRelationships.Add(relationship.targetAgent);
                    }
                }

                // Clean up expired relationships
                foreach (var expiredId in expiredRelationships)
                {
                    agent.relationships.Remove(expiredId);
                    _socialNetwork.RemoveEdge(agent.agentId, expiredId);
                }
            }
        }

        /// <summary>
        /// Calculates relationship change based on interaction outcome
        /// </summary>
        public float CalculateRelationshipChange(SocialInteraction interaction, float interactionTypeModifier)
        {
            float change = 0f;

            switch (interaction.outcome)
            {
                case InteractionOutcome.Positive:
                    change = 0.1f + (interaction.success * 0.2f);
                    break;
                case InteractionOutcome.Neutral:
                    change = 0.02f;
                    break;
                case InteractionOutcome.Negative:
                    change = -0.05f - ((1f - interaction.success) * 0.1f);
                    break;
            }

            // Interaction type modifiers
            change *= interactionTypeModifier;

            return change;
        }
    }
}
