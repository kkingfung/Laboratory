using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Service for managing emotional intelligence, empathy, and emotional contagion.
    /// Handles emotional state updates, empathy development, and emotional synchronization.
    /// Extracted from AdvancedSocialSystem for single responsibility.
    /// </summary>
    public class EmotionalIntelligenceService
    {
        private readonly bool _enableEmotionalContagion;
        private readonly float _empathyRange;
        private readonly float _empathyDevelopmentRate;
        private readonly float _emotionalSynchronizationRate;
        private readonly EmotionalContagionEngine _emotionalContagion;
        private readonly EmpathyNetworkManager _empathyNetwork;
        private readonly Action<uint, uint, EmotionalState> _onEmotionalContagion;

        public EmotionalIntelligenceService(
            bool enableEmotionalContagion,
            float empathyRange,
            float empathyDevelopmentRate,
            float emotionalSynchronizationRate,
            Action<uint, uint, EmotionalState> onEmotionalContagion)
        {
            _enableEmotionalContagion = enableEmotionalContagion;
            _empathyRange = empathyRange;
            _empathyDevelopmentRate = empathyDevelopmentRate;
            _emotionalSynchronizationRate = emotionalSynchronizationRate;
            _onEmotionalContagion = onEmotionalContagion;
            _emotionalContagion = new EmotionalContagionEngine(empathyRange);
            _empathyNetwork = new EmpathyNetworkManager();
        }

        /// <summary>
        /// Calculates empathy gain from a positive interaction
        /// </summary>
        public float CalculateEmpathyGain(SocialAgent agentA, SocialAgent agentB, SocialInteraction interaction)
        {
            if (interaction.outcome != InteractionOutcome.Positive)
                return 0f;

            float empathyGain = 0.01f;

            // Empathic individuals gain more empathy from positive interactions
            empathyGain *= (agentA.empathyLevel + 0.5f);

            // Certain interaction types promote empathy development
            if (interaction.interactionType == InteractionType.Grooming ||
                interaction.interactionType == InteractionType.Teaching ||
                interaction.interactionType == InteractionType.Cooperation)
            {
                empathyGain *= 1.5f;
            }

            return empathyGain;
        }

        /// <summary>
        /// Applies empathy gain to agents after successful interaction
        /// </summary>
        public void ApplyEmpathyGain(SocialAgent agentA, SocialAgent agentB, float empathyGain)
        {
            agentA.empathyLevel = math.clamp(agentA.empathyLevel + empathyGain * _empathyDevelopmentRate, 0f, 1f);
            agentB.empathyLevel = math.clamp(agentB.empathyLevel + empathyGain * _empathyDevelopmentRate, 0f, 1f);
        }

        /// <summary>
        /// Processes emotional contagion between two agents during interaction
        /// </summary>
        public void ProcessEmotionalContagion(SocialAgent agentA, SocialAgent agentB, SocialInteraction interaction)
        {
            if (!_enableEmotionalContagion) return;
            if (interaction.outcome != InteractionOutcome.Positive) return;

            // More empathic agents are more susceptible to emotional contagion
            float contagionStrength = (agentA.empathyLevel + agentB.empathyLevel) * 0.5f * _emotionalSynchronizationRate;

            if (UnityEngine.Random.value < contagionStrength)
            {
                // Agent A adopts some of Agent B's emotional state
                var previousStateA = agentA.emotionalState;

                if (agentB.emotionalState != agentA.emotionalState)
                {
                    // Gradual emotional convergence
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        agentA.emotionalState = agentB.emotionalState;
                        _onEmotionalContagion?.Invoke(agentA.agentId, agentB.agentId, agentB.emotionalState);
                    }
                }
            }
        }

        /// <summary>
        /// Updates emotional states for all agents over time
        /// </summary>
        public void UpdateEmotionalStates(
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, SocialGroup> activeGroups,
            float deltaTime)
        {
            _emotionalContagion.UpdateEmotionalStates(socialAgents.Values, activeGroups.Values, deltaTime);
        }

        /// <summary>
        /// Analyzes empathy network connections
        /// </summary>
        public EmpathyNetworkAnalysis AnalyzeEmpathyNetwork(Dictionary<uint, SocialAgent> socialAgents)
        {
            return _empathyNetwork.AnalyzeEmpathyConnections(socialAgents.Values);
        }

        /// <summary>
        /// Calculates average empathy across all agents
        /// </summary>
        public float CalculateAverageEmpathy(Dictionary<uint, SocialAgent> socialAgents)
        {
            if (socialAgents.Count == 0) return 0f;
            return socialAgents.Values.Average(a => a.empathyLevel);
        }

        /// <summary>
        /// Identifies agents with exceptionally high empathy (empathy hubs)
        /// </summary>
        public List<uint> IdentifyEmpathyHubs(Dictionary<uint, SocialAgent> socialAgents, float threshold = 0.8f)
        {
            return socialAgents.Values
                .Where(a => a.empathyLevel >= threshold)
                .Select(a => a.agentId)
                .ToList();
        }

        /// <summary>
        /// Calculates emotional synchronization rate across groups
        /// </summary>
        public float CalculateEmotionalSyncRate(Dictionary<uint, SocialGroup> activeGroups, Dictionary<uint, SocialAgent> socialAgents)
        {
            if (activeGroups.Count == 0) return 0f;

            float totalSync = 0f;
            int groupCount = 0;

            foreach (var group in activeGroups.Values)
            {
                if (group.memberIds.Count < 2) continue;

                // Calculate how synchronized the group's emotional states are
                var emotionalStates = group.memberIds
                    .Where(id => socialAgents.ContainsKey(id))
                    .Select(id => socialAgents[id].emotionalState)
                    .ToList();

                if (emotionalStates.Count > 0)
                {
                    // Count most common emotional state
                    var mostCommonState = emotionalStates
                        .GroupBy(e => e)
                        .OrderByDescending(g => g.Count())
                        .First();

                    float syncRate = (float)mostCommonState.Count() / emotionalStates.Count;
                    totalSync += syncRate;
                    groupCount++;
                }
            }

            return groupCount > 0 ? totalSync / groupCount : 0f;
        }
    }
}
