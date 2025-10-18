using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using Laboratory.Chimera.Social.Data;
using Laboratory.Chimera.Social.Types;

namespace Laboratory.Chimera.Social.Systems
{
    /// <summary>
    /// Emotional contagion and empathy network system
    /// </summary>
    public class EmotionalContagionSystem : MonoBehaviour
    {
        [Header("Emotional Configuration")]
        [SerializeField] private float empathyRange = 50f;
        [SerializeField] private bool enableEmotionalContagion = true;
        [SerializeField] private float emotionalSynchronizationRate = 0.1f;
        [SerializeField] private float empathyDevelopmentRate = 0.02f;

        private EmotionalContagionEngine emotionalContagion;
        private EmpathyNetworkManager empathyNetwork;
        private Dictionary<uint, EmotionalState> agentEmotionalStates = new();
        private Dictionary<uint, float> agentEmpathyLevels = new();

        public event Action<uint, uint, EmotionalState> OnEmotionalContagion;
        public event Action<uint, EmotionalState> OnEmotionalStateChanged;
        public event Action<uint, float> OnEmpathyDeveloped;

        private void Awake()
        {
            emotionalContagion = new EmotionalContagionEngine(empathyRange);
            empathyNetwork = new EmpathyNetworkManager();
        }

        public void RegisterAgent(uint agentId, EmotionalState initialState = EmotionalState.Neutral, float empathyLevel = 0.5f)
        {
            agentEmotionalStates[agentId] = initialState;
            agentEmpathyLevels[agentId] = empathyLevel;
            empathyNetwork.AddAgent(agentId, empathyLevel);

            Debug.Log($"Registered emotional agent {agentId} with state {initialState} and empathy {empathyLevel:F2}");
        }

        public void UpdateEmotionalState(uint agentId, EmotionalState newState, float intensity = 1.0f)
        {
            if (!agentEmotionalStates.ContainsKey(agentId)) return;

            var previousState = agentEmotionalStates[agentId];
            agentEmotionalStates[agentId] = newState;

            OnEmotionalStateChanged?.Invoke(agentId, newState);

            // Trigger emotional contagion if state changed significantly
            if (previousState != newState && enableEmotionalContagion)
            {
                ProcessEmotionalContagion(agentId, newState, intensity);
            }
        }

        private void ProcessEmotionalContagion(uint sourceAgentId, EmotionalState emotion, float intensity)
        {
            var nearbyAgents = GetNearbyAgents(sourceAgentId, empathyRange);

            foreach (var nearbyAgentId in nearbyAgents)
            {
                if (nearbyAgentId == sourceAgentId) continue;

                var empathyLevel = agentEmpathyLevels.GetValueOrDefault(nearbyAgentId, 0.5f);
                var contagionProbability = CalculateContagionProbability(empathyLevel, intensity, GetDistance(sourceAgentId, nearbyAgentId));

                if (UnityEngine.Random.value < contagionProbability)
                {
                    ApplyEmotionalContagion(sourceAgentId, nearbyAgentId, emotion, intensity * empathyLevel);
                }
            }
        }

        private float CalculateContagionProbability(float empathyLevel, float intensity, float distance)
        {
            float distanceFactor = Mathf.Clamp01(1f - (distance / empathyRange));
            float baseProbability = empathyLevel * intensity * emotionalSynchronizationRate;
            return baseProbability * distanceFactor;
        }

        private void ApplyEmotionalContagion(uint sourceId, uint targetId, EmotionalState emotion, float strength)
        {
            var currentState = agentEmotionalStates[targetId];
            var newState = BlendEmotionalStates(currentState, emotion, strength);

            agentEmotionalStates[targetId] = newState;

            // Develop empathy through emotional sharing
            DevelopEmpathy(targetId, strength * empathyDevelopmentRate);

            OnEmotionalContagion?.Invoke(sourceId, targetId, newState);
            Debug.Log($"Emotional contagion: {emotion} spread from {sourceId} to {targetId} (strength: {strength:F2})");
        }

        private EmotionalState BlendEmotionalStates(EmotionalState current, EmotionalState incoming, float blendStrength)
        {
            // Simplified blending - in reality this would be more sophisticated
            if (blendStrength > 0.7f)
            {
                return incoming; // Strong influence overwrites current state
            }
            else if (blendStrength > 0.3f)
            {
                return GetBlendedEmotion(current, incoming);
            }
            else
            {
                return current; // Weak influence doesn't change state
            }
        }

        private EmotionalState GetBlendedEmotion(EmotionalState emotion1, EmotionalState emotion2)
        {
            // Emotional blending rules
            return (emotion1, emotion2) switch
            {
                (EmotionalState.Happy, EmotionalState.Excited) => EmotionalState.Excited,
                (EmotionalState.Excited, EmotionalState.Happy) => EmotionalState.Excited,
                (EmotionalState.Sad, EmotionalState.Fearful) => EmotionalState.Anxious,
                (EmotionalState.Fearful, EmotionalState.Sad) => EmotionalState.Anxious,
                (EmotionalState.Angry, EmotionalState.Fearful) => EmotionalState.Anxious,
                (EmotionalState.Happy, EmotionalState.Calm) => EmotionalState.Confident,
                (EmotionalState.Calm, EmotionalState.Happy) => EmotionalState.Confident,
                _ => emotion1 // Default to first emotion if no blending rule exists
            };
        }

        private void DevelopEmpathy(uint agentId, float developmentAmount)
        {
            if (agentEmpathyLevels.ContainsKey(agentId))
            {
                agentEmpathyLevels[agentId] += developmentAmount;
                agentEmpathyLevels[agentId] = Mathf.Clamp01(agentEmpathyLevels[agentId]);

                empathyNetwork.UpdateEmpathyLevel(agentId, agentEmpathyLevels[agentId]);
                OnEmpathyDeveloped?.Invoke(agentId, agentEmpathyLevels[agentId]);
            }
        }

        private List<uint> GetNearbyAgents(uint agentId, float range)
        {
            // This would use actual spatial data in a full implementation
            // For now, returning a subset of all agents
            var allAgents = agentEmotionalStates.Keys.ToList();
            return allAgents.Where(id => id != agentId && UnityEngine.Random.value < 0.3f).Take(5).ToList();
        }

        private float GetDistance(uint agent1Id, uint agent2Id)
        {
            // This would calculate actual distance in a full implementation
            return UnityEngine.Random.Range(1f, empathyRange);
        }

        public void UpdateEmotionalDecay()
        {
            // Gradually return emotions to neutral state
            var decayRate = 0.01f;

            foreach (var agentId in agentEmotionalStates.Keys.ToList())
            {
                var currentState = agentEmotionalStates[agentId];

                if (currentState != EmotionalState.Neutral && UnityEngine.Random.value < decayRate)
                {
                    // Gradually move toward neutral
                    var neutralizingStates = new[] { EmotionalState.Neutral, EmotionalState.Calm };
                    var newState = neutralizingStates[UnityEngine.Random.Range(0, neutralizingStates.Length)];

                    agentEmotionalStates[agentId] = newState;
                    OnEmotionalStateChanged?.Invoke(agentId, newState);
                }
            }
        }

        public EmotionalState GetAgentEmotionalState(uint agentId)
        {
            return agentEmotionalStates.GetValueOrDefault(agentId, EmotionalState.Neutral);
        }

        public float GetAgentEmpathyLevel(uint agentId)
        {
            return agentEmpathyLevels.GetValueOrDefault(agentId, 0.5f);
        }

        public Dictionary<uint, EmotionalState> GetAllEmotionalStates()
        {
            return new Dictionary<uint, EmotionalState>(agentEmotionalStates);
        }

        public EmpathyNetworkAnalysis GetEmpathyNetworkAnalysis()
        {
            return empathyNetwork.GenerateAnalysis();
        }
    }

    /// <summary>
    /// Emotional contagion processing engine
    /// </summary>
    public class EmotionalContagionEngine
    {
        private readonly float maxRange;
        private Dictionary<uint, List<(uint, DateTime)>> contagionHistory = new();

        public EmotionalContagionEngine(float maxRange)
        {
            this.maxRange = maxRange;
        }

        public void RecordContagion(uint sourceId, uint targetId)
        {
            if (!contagionHistory.ContainsKey(sourceId))
            {
                contagionHistory[sourceId] = new List<(uint, DateTime)>();
            }

            contagionHistory[sourceId].Add((targetId, DateTime.UtcNow));

            // Clean old records
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
            contagionHistory[sourceId] = contagionHistory[sourceId]
                .Where(record => record.Item2 > cutoffTime).ToList();
        }

        public float GetContagionInfluence(uint agentId)
        {
            if (!contagionHistory.TryGetValue(agentId, out var history))
                return 0f;

            return history.Count * 0.1f; // Each contagion event increases influence
        }
    }

    /// <summary>
    /// Empathy network management
    /// </summary>
    public class EmpathyNetworkManager
    {
        private Dictionary<uint, float> agentEmpathyLevels = new();
        private Dictionary<uint, List<uint>> empathyConnections = new();

        public void AddAgent(uint agentId, float empathyLevel)
        {
            agentEmpathyLevels[agentId] = empathyLevel;
            empathyConnections[agentId] = new List<uint>();
        }

        public void UpdateEmpathyLevel(uint agentId, float newLevel)
        {
            if (agentEmpathyLevels.ContainsKey(agentId))
            {
                agentEmpathyLevels[agentId] = newLevel;
            }
        }

        public void RecordEmpathyConnection(uint agent1Id, uint agent2Id)
        {
            if (empathyConnections.ContainsKey(agent1Id) && !empathyConnections[agent1Id].Contains(agent2Id))
            {
                empathyConnections[agent1Id].Add(agent2Id);
            }

            if (empathyConnections.ContainsKey(agent2Id) && !empathyConnections[agent2Id].Contains(agent1Id))
            {
                empathyConnections[agent2Id].Add(agent1Id);
            }
        }

        public EmpathyNetworkAnalysis GenerateAnalysis()
        {
            return new EmpathyNetworkAnalysis
            {
                TotalAgents = agentEmpathyLevels.Count,
                AverageEmpathyLevel = agentEmpathyLevels.Values.Average(),
                HighestEmpathyAgent = agentEmpathyLevels.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                TotalConnections = empathyConnections.Values.Sum(list => list.Count) / 2,
                NetworkDensity = CalculateNetworkDensity()
            };
        }

        private float CalculateNetworkDensity()
        {
            if (agentEmpathyLevels.Count < 2) return 0f;

            int totalPossibleConnections = agentEmpathyLevels.Count * (agentEmpathyLevels.Count - 1) / 2;
            int actualConnections = empathyConnections.Values.Sum(list => list.Count) / 2;

            return totalPossibleConnections > 0 ? (float)actualConnections / totalPossibleConnections : 0f;
        }
    }

    /// <summary>
    /// Empathy network analysis results
    /// </summary>
    [Serializable]
    public class EmpathyNetworkAnalysis
    {
        public int TotalAgents;
        public float AverageEmpathyLevel;
        public uint HighestEmpathyAgent;
        public int TotalConnections;
        public float NetworkDensity;
        public DateTime AnalysisDate = DateTime.UtcNow;
    }
}