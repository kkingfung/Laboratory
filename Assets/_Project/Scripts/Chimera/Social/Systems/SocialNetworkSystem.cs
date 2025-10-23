using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Social.Data;
using Laboratory.Chimera.Social.Types;

namespace Laboratory.Chimera.Social.Systems
{
    /// <summary>
    /// Core social network management system
    /// </summary>
    public class SocialNetworkSystem : MonoBehaviour
    {
        [Header("Network Configuration")]
        [SerializeField] private float relationshipDecayRate = 0.001f;
        [SerializeField] private float maxRelationshipDistance = 100f;
        [SerializeField] private int maxRelationshipsPerAgent = 50;

        private Dictionary<uint, Data.SocialAgent> socialAgents = new();
        private Dictionary<(uint, uint), Data.SocialRelationship> relationships = new();
        private SocialNetworkGraph networkGraph;

        public event Action<uint, uint, Laboratory.Chimera.Social.Types.RelationshipType> OnRelationshipFormed;
        public event Action<uint, uint> OnRelationshipDecayed;

        private void Awake()
        {
            networkGraph = new SocialNetworkGraph();
        }

        public void RegisterAgent(Data.SocialAgent agent)
        {
            if (!socialAgents.ContainsKey(agent.AgentId))
            {
                socialAgents[agent.AgentId] = agent;
                networkGraph.AddNode(agent.AgentId);
                UnityEngine.Debug.Log($"Registered social agent: {agent.Name}");
            }
        }

        public void UpdateRelationship(uint agent1Id, uint agent2Id, InteractionOutcome outcome, float intensity)
        {
            var relationshipKey = GetRelationshipKey(agent1Id, agent2Id);

            if (!relationships.TryGetValue(relationshipKey, out var relationship))
            {
                relationship = new Data.SocialRelationship
                {
                    Agent1Id = agent1Id,
                    Agent2Id = agent2Id,
                    Type = Types.RelationshipType.Stranger,
                    Strength = 0f,
                    Trust = 0f,
                    FormationDate = DateTime.UtcNow
                };
                relationships[relationshipKey] = relationship;
            }

            ApplySocialInteractionOutcome(relationship, outcome, intensity);
            UpdateSocialRelationshipType(relationship);
            networkGraph.UpdateEdge(agent1Id, agent2Id, relationship.Strength);
        }

        public Data.SocialRelationship GetRelationship(uint agent1Id, uint agent2Id)
        {
            var key = GetRelationshipKey(agent1Id, agent2Id);
            return relationships.TryGetValue(key, out var relationship) ? relationship : null;
        }

        public List<Data.SocialRelationship> GetAgentRelationships(uint agentId)
        {
            return relationships.Values.Where(r =>
                r.Agent1Id == agentId || r.Agent2Id == agentId).ToList();
        }

        public void UpdateNetworkDecay()
        {
            var decayingRelationships = new List<(uint, uint)>();

            foreach (var kvp in relationships)
            {
                var relationship = kvp.Value;
                relationship.Strength -= relationshipDecayRate * Time.deltaTime;
                relationship.Trust -= relationshipDecayRate * 0.5f * Time.deltaTime;

                if (relationship.Strength <= 0f)
                {
                    decayingRelationships.Add(kvp.Key);
                }
            }

            foreach (var key in decayingRelationships)
            {
                var relationship = relationships[key];
                relationships.Remove(key);
                networkGraph.RemoveEdge(relationship.Agent1Id, relationship.Agent2Id);
                OnRelationshipDecayed?.Invoke(relationship.Agent1Id, relationship.Agent2Id);
            }
        }

        private (uint, uint) GetRelationshipKey(uint agent1Id, uint agent2Id)
        {
            return agent1Id < agent2Id ? (agent1Id, agent2Id) : (agent2Id, agent1Id);
        }

        private void ApplySocialInteractionOutcome(Data.SocialRelationship relationship, InteractionOutcome outcome, float intensity)
        {
            switch (outcome)
            {
                case InteractionOutcome.Positive:
                    relationship.Strength += intensity * 0.1f;
                    relationship.Trust += intensity * 0.05f;
                    break;
                case InteractionOutcome.Negative:
                    relationship.Strength -= intensity * 0.15f;
                    relationship.Trust -= intensity * 0.1f;
                    break;
                default:
                    // Handle any other outcome types
                    relationship.Strength += intensity * 0.05f;
                    break;
            }

            relationship.Strength = Mathf.Clamp(relationship.Strength, -1f, 1f);
            relationship.Trust = Mathf.Clamp(relationship.Trust, -1f, 1f);
        }

        private void UpdateSocialRelationshipType(Data.SocialRelationship relationship)
        {
            var oldType = relationship.Type;

            if (relationship.Strength > 0.7f && relationship.Trust > 0.6f)
                relationship.Type = Types.RelationshipType.Friend;
            else if (relationship.Strength < -0.5f)
                relationship.Type = Types.RelationshipType.Enemy;
            else if (relationship.Strength < -0.2f)
                relationship.Type = Types.RelationshipType.Rival;
            else if (relationship.Strength > 0.3f)
                relationship.Type = Types.RelationshipType.Acquaintance;
            else
                relationship.Type = Types.RelationshipType.Stranger;

            if (oldType != relationship.Type)
            {
                OnRelationshipFormed?.Invoke(relationship.Agent1Id, relationship.Agent2Id, relationship.Type);
            }
        }
    }

    /// <summary>
    /// Social network graph representation
    /// </summary>
    public class SocialNetworkGraph
    {
        private Dictionary<uint, List<uint>> adjacencyList = new();
        private Dictionary<(uint, uint), float> edgeWeights = new();

        public void AddNode(uint nodeId)
        {
            if (!adjacencyList.ContainsKey(nodeId))
            {
                adjacencyList[nodeId] = new List<uint>();
            }
        }

        public void UpdateEdge(uint node1, uint node2, float weight)
        {
            if (!adjacencyList.ContainsKey(node1)) AddNode(node1);
            if (!adjacencyList.ContainsKey(node2)) AddNode(node2);

            var edgeKey = node1 < node2 ? (node1, node2) : (node2, node1);
            edgeWeights[edgeKey] = weight;

            if (!adjacencyList[node1].Contains(node2))
                adjacencyList[node1].Add(node2);
            if (!adjacencyList[node2].Contains(node1))
                adjacencyList[node2].Add(node1);
        }

        public void RemoveEdge(uint node1, uint node2)
        {
            var edgeKey = node1 < node2 ? (node1, node2) : (node2, node1);
            edgeWeights.Remove(edgeKey);

            adjacencyList[node1]?.Remove(node2);
            adjacencyList[node2]?.Remove(node1);
        }

        public List<uint> GetNeighbors(uint nodeId)
        {
            return adjacencyList.TryGetValue(nodeId, out var neighbors) ?
                new List<uint>(neighbors) : new List<uint>();
        }

        public float GetEdgeWeight(uint node1, uint node2)
        {
            var edgeKey = node1 < node2 ? (node1, node2) : (node2, node1);
            return edgeWeights.TryGetValue(edgeKey, out var weight) ? weight : 0f;
        }
    }
}