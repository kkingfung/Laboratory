using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Advanced
{
    /// <summary>
    /// Advanced AI behavior system with behavior trees and state machines.
    /// Provides goal-oriented behavior, decision-making, and emergent AI.
    /// Optimized for thousands of concurrent AI agents.
    /// </summary>
    public class AdvancedAIBehaviorSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Performance")]
        [SerializeField] private int maxAIUpdatesPerFrame = 50;
        [SerializeField] private float updateInterval = 0.1f; // 10 updates per second
        [SerializeField] private bool useTimeslicing = true;

        [Header("Behavior Settings")]
        [SerializeField] private float decisionChangeThreshold = 0.7f;
        [SerializeField] private bool enableLearning = true;
        [SerializeField] private float learningRate = 0.01f;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;

        #endregion

        #region Private Fields

        private static AdvancedAIBehaviorSystem _instance;

        // AI agents
        private readonly Dictionary<string, AIAgent> _agents = new Dictionary<string, AIAgent>();
        private readonly List<AIAgent> _activeAgents = new List<AIAgent>();

        // Update scheduling
        private int _currentUpdateIndex = 0;
        private float _lastUpdateTime = 0f;

        // Behavior trees
        private readonly Dictionary<string, BehaviorTree> _behaviorTrees = new Dictionary<string, BehaviorTree>();

        // Statistics
        private int _totalAgentsCreated = 0;
        private int _totalDecisionsMade = 0;
        private int _totalBehaviorsExecuted = 0;

        // Events
        public event Action<AIAgent> OnAgentRegistered;
        public event Action<string> OnAgentUnregistered;
        public event Action<AIAgent, AIDecision> OnDecisionMade;
        public event Action<AIAgent, AIBehaviorState> OnStateChanged;

        #endregion

        #region Properties

        public static AdvancedAIBehaviorSystem Instance => _instance;
        public int ActiveAgentCount => _activeAgents.Count;
        public int BehaviorTreeCount => _behaviorTrees.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Time.time - _lastUpdateTime < updateInterval)
                return;

            UpdateAI();
            _lastUpdateTime = Time.time;
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[AdvancedAIBehaviorSystem] Initializing...");

            // Create default behavior trees
            CreateDefaultBehaviorTrees();

            Debug.Log("[AdvancedAIBehaviorSystem] Initialized");
        }

        private void CreateDefaultBehaviorTrees()
        {
            // Creature AI behavior tree
            var creatureTree = new BehaviorTree("creature_ai");

            var root = new SelectorNode();

            // Flee if low health
            var fleeSequence = new SequenceNode();
            fleeSequence.AddChild(new ConditionNode(agent => agent.GetStat("health") < 30));
            fleeSequence.AddChild(new ActionNode(agent => Flee(agent)));
            root.AddChild(fleeSequence);

            // Attack if enemy nearby
            var attackSequence = new SequenceNode();
            attackSequence.AddChild(new ConditionNode(agent => HasEnemyNearby(agent)));
            attackSequence.AddChild(new ActionNode(agent => Attack(agent)));
            root.AddChild(attackSequence);

            // Forage for food
            var forageSequence = new SequenceNode();
            forageSequence.AddChild(new ConditionNode(agent => agent.GetStat("hunger") > 50));
            forageSequence.AddChild(new ActionNode(agent => Forage(agent)));
            root.AddChild(forageSequence);

            // Wander
            root.AddChild(new ActionNode(agent => Wander(agent)));

            creatureTree.SetRoot(root);
            _behaviorTrees["creature_ai"] = creatureTree;

            Log("Default behavior trees created");
        }

        #endregion

        #region Agent Management

        /// <summary>
        /// Register an AI agent.
        /// </summary>
        public void RegisterAgent(AIAgent agent)
        {
            if (_agents.ContainsKey(agent.agentId))
            {
                LogWarning($"Agent already registered: {agent.agentId}");
                return;
            }

            _agents[agent.agentId] = agent;
            _activeAgents.Add(agent);
            _totalAgentsCreated++;

            // Assign default behavior tree
            if (string.IsNullOrEmpty(agent.behaviorTreeId))
            {
                agent.behaviorTreeId = "creature_ai";
            }

            OnAgentRegistered?.Invoke(agent);

            Log($"Agent registered: {agent.agentId}");
        }

        /// <summary>
        /// Unregister an AI agent.
        /// </summary>
        public void UnregisterAgent(string agentId)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                _agents.Remove(agentId);
                _activeAgents.Remove(agent);

                OnAgentUnregistered?.Invoke(agentId);

                Log($"Agent unregistered: {agentId}");
            }
        }

        #endregion

        #region AI Update

        private void UpdateAI()
        {
            if (_activeAgents.Count == 0) return;

            int updatesThisFrame = 0;

            if (useTimeslicing)
            {
                // Update subset of agents per frame
                for (int i = 0; i < maxAIUpdatesPerFrame && updatesThisFrame < _activeAgents.Count; i++)
                {
                    int index = (_currentUpdateIndex + i) % _activeAgents.Count;
                    UpdateAgent(_activeAgents[index]);
                    updatesThisFrame++;
                }

                _currentUpdateIndex = (_currentUpdateIndex + maxAIUpdatesPerFrame) % _activeAgents.Count;
            }
            else
            {
                // Update all agents
                foreach (var agent in _activeAgents)
                {
                    UpdateAgent(agent);
                }
            }
        }

        private void UpdateAgent(AIAgent agent)
        {
            if (!agent.isActive) return;

            // Get behavior tree
            if (!_behaviorTrees.TryGetValue(agent.behaviorTreeId, out var behaviorTree))
            {
                LogWarning($"Behavior tree not found: {agent.behaviorTreeId}");
                return;
            }

            // Execute behavior tree
            var result = behaviorTree.Evaluate(agent);

            _totalBehaviorsExecuted++;

            // Update agent state based on result
            if (result == NodeStatus.Success || result == NodeStatus.Running)
            {
                // Behavior succeeded or continuing
            }
            else
            {
                // Behavior failed - could trigger fallback
                Log($"Behavior failed for agent: {agent.agentId}");
            }

            // Decision-making
            if (Time.time - agent.lastDecisionTime > agent.decisionInterval)
            {
                MakeDecision(agent);
            }

            // Update stats
            UpdateAgentStats(agent);
        }

        #endregion

        #region Decision Making

        private void MakeDecision(AIAgent agent)
        {
            agent.lastDecisionTime = Time.time;
            _totalDecisionsMade++;

            // Evaluate current situation
            float threatLevel = EvaluateThreat(agent);
            float resourceNeed = EvaluateResourceNeed(agent);
            float socialNeed = EvaluateSocialNeed(agent);

            // Choose action based on weighted factors
            AIDecision decision = new AIDecision();

            if (threatLevel > decisionChangeThreshold)
            {
                decision.action = AIAction.Flee;
                decision.priority = threatLevel;
            }
            else if (resourceNeed > decisionChangeThreshold)
            {
                decision.action = AIAction.Forage;
                decision.priority = resourceNeed;
            }
            else if (socialNeed > decisionChangeThreshold)
            {
                decision.action = AIAction.Socialize;
                decision.priority = socialNeed;
            }
            else
            {
                decision.action = AIAction.Wander;
                decision.priority = 0.5f;
            }

            agent.currentDecision = decision;

            OnDecisionMade?.Invoke(agent, decision);

            Log($"Agent {agent.agentId} decision: {decision.action} (priority: {decision.priority:F2})");
        }

        private float EvaluateThreat(AIAgent agent)
        {
            // Check for nearby threats
            float threat = 0f;

            // Low health = higher threat perception
            float healthPercent = agent.GetStat("health") / 100f;
            if (healthPercent < 0.5f)
            {
                threat += (1f - healthPercent) * 0.5f;
            }

            // Enemies nearby (simplified)
            if (HasEnemyNearby(agent))
            {
                threat += 0.7f;
            }

            return Mathf.Clamp01(threat);
        }

        private float EvaluateResourceNeed(AIAgent agent)
        {
            float hunger = agent.GetStat("hunger") / 100f;
            float energy = agent.GetStat("energy") / 100f;

            return Mathf.Max(hunger, 1f - energy);
        }

        private float EvaluateSocialNeed(AIAgent agent)
        {
            // Simplified social need
            return agent.GetStat("sociability") / 100f * 0.3f;
        }

        #endregion

        #region Behaviors

        private NodeStatus Flee(AIAgent agent)
        {
            // Implement flee logic
            agent.state = AIBehaviorState.Fleeing;
            OnStateChanged?.Invoke(agent, agent.state);

            // Move away from threats (simplified)
            if (agent.transform != null)
            {
                agent.transform.Translate(Vector3.back * agent.GetStat("speed") * Time.deltaTime);
            }

            return NodeStatus.Running;
        }

        private NodeStatus Attack(AIAgent agent)
        {
            // Implement attack logic
            agent.state = AIBehaviorState.Attacking;
            OnStateChanged?.Invoke(agent, agent.state);

            // Attack logic (simplified)
            Log($"Agent {agent.agentId} attacking");

            return NodeStatus.Success;
        }

        private NodeStatus Forage(AIAgent agent)
        {
            // Implement foraging logic
            agent.state = AIBehaviorState.Foraging;
            OnStateChanged?.Invoke(agent, agent.state);

            // Decrease hunger
            agent.SetStat("hunger", agent.GetStat("hunger") - 10 * Time.deltaTime);

            return NodeStatus.Running;
        }

        private NodeStatus Wander(AIAgent agent)
        {
            // Implement wander logic
            agent.state = AIBehaviorState.Wandering;
            OnStateChanged?.Invoke(agent, agent.state);

            // Random movement (simplified)
            if (agent.transform != null)
            {
                Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
                randomDir.y = 0;
                agent.transform.Translate(randomDir.normalized * agent.GetStat("speed") * Time.deltaTime * 0.5f);
            }

            return NodeStatus.Running;
        }

        private bool HasEnemyNearby(AIAgent agent)
        {
            // Simplified enemy detection
            // In real implementation, would use spatial partitioning
            return UnityEngine.Random.value < 0.1f;
        }

        #endregion

        #region Agent Stats

        private void UpdateAgentStats(AIAgent agent)
        {
            // Increase hunger over time
            agent.SetStat("hunger", agent.GetStat("hunger") + 5 * Time.deltaTime);

            // Decrease energy over time
            agent.SetStat("energy", agent.GetStat("energy") - 3 * Time.deltaTime);

            // Clamp stats
            agent.SetStat("hunger", Mathf.Clamp(agent.GetStat("hunger"), 0, 100));
            agent.SetStat("energy", Mathf.Clamp(agent.GetStat("energy"), 0, 100));
        }

        #endregion

        #region Behavior Trees

        /// <summary>
        /// Register a custom behavior tree.
        /// </summary>
        public void RegisterBehaviorTree(BehaviorTree tree)
        {
            _behaviorTrees[tree.id] = tree;
            Log($"Behavior tree registered: {tree.id}");
        }

        /// <summary>
        /// Get behavior tree.
        /// </summary>
        public BehaviorTree GetBehaviorTree(string treeId)
        {
            return _behaviorTrees.TryGetValue(treeId, out var tree) ? tree : null;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[AdvancedAIBehaviorSystem] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AdvancedAIBehaviorSystem] {message}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get AI system statistics.
        /// </summary>
        public AISystemStats GetStats()
        {
            return new AISystemStats
            {
                activeAgents = _activeAgents.Count,
                totalAgentsCreated = _totalAgentsCreated,
                behaviorTreeCount = _behaviorTrees.Count,
                totalDecisionsMade = _totalDecisionsMade,
                totalBehaviorsExecuted = _totalBehaviorsExecuted
            };
        }

        /// <summary>
        /// Get agent by ID.
        /// </summary>
        public AIAgent GetAgent(string agentId)
        {
            return _agents.TryGetValue(agentId, out var agent) ? agent : null;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== AI Behavior System Statistics ===\n" +
                      $"Active Agents: {stats.activeAgents}\n" +
                      $"Total Agents Created: {stats.totalAgentsCreated}\n" +
                      $"Behavior Trees: {stats.behaviorTreeCount}\n" +
                      $"Decisions Made: {stats.totalDecisionsMade}\n" +
                      $"Behaviors Executed: {stats.totalBehaviorsExecuted}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// AI agent.
    /// </summary>
    [Serializable]
    public class AIAgent
    {
        public string agentId;
        public string behaviorTreeId;
        public Transform transform;
        public bool isActive = true;

        public AIBehaviorState state = AIBehaviorState.Idle;
        public AIDecision currentDecision;

        public float decisionInterval = 1f;
        public float lastDecisionTime = 0f;

        private Dictionary<string, float> _stats = new Dictionary<string, float>
        {
            { "health", 100f },
            { "hunger", 0f },
            { "energy", 100f },
            { "speed", 5f },
            { "sociability", 50f }
        };

        public float GetStat(string statName)
        {
            return _stats.TryGetValue(statName, out float value) ? value : 0f;
        }

        public void SetStat(string statName, float value)
        {
            _stats[statName] = value;
        }
    }

    /// <summary>
    /// AI decision.
    /// </summary>
    [Serializable]
    public class AIDecision
    {
        public AIAction action;
        public float priority;
        public Vector3 targetPosition;
        public string targetId;
    }

    /// <summary>
    /// AI system statistics.
    /// </summary>
    [Serializable]
    public struct AISystemStats
    {
        public int activeAgents;
        public int totalAgentsCreated;
        public int behaviorTreeCount;
        public int totalDecisionsMade;
        public int totalBehaviorsExecuted;
    }

    /// <summary>
    /// AI actions.
    /// </summary>
    public enum AIAction
    {
        Idle,
        Wander,
        Flee,
        Attack,
        Forage,
        Socialize,
        Rest
    }

    /// <summary>
    /// AI behavior states.
    /// </summary>
    public enum AIBehaviorState
    {
        Idle,
        Wandering,
        Fleeing,
        Attacking,
        Foraging,
        Socializing,
        Resting
    }

    #endregion

    #region Behavior Tree

    /// <summary>
    /// Behavior tree.
    /// </summary>
    public class BehaviorTree
    {
        public string id;
        private BehaviorNode _root;

        public BehaviorTree(string id)
        {
            this.id = id;
        }

        public void SetRoot(BehaviorNode root)
        {
            _root = root;
        }

        public NodeStatus Evaluate(AIAgent agent)
        {
            return _root?.Evaluate(agent) ?? NodeStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior tree node statuses.
    /// </summary>
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// Base behavior node.
    /// </summary>
    public abstract class BehaviorNode
    {
        public abstract NodeStatus Evaluate(AIAgent agent);
    }

    /// <summary>
    /// Selector node (OR).
    /// </summary>
    public class SelectorNode : BehaviorNode
    {
        private List<BehaviorNode> _children = new List<BehaviorNode>();

        public void AddChild(BehaviorNode node)
        {
            _children.Add(node);
        }

        public override NodeStatus Evaluate(AIAgent agent)
        {
            foreach (var child in _children)
            {
                var status = child.Evaluate(agent);
                if (status != NodeStatus.Failure)
                    return status;
            }

            return NodeStatus.Failure;
        }
    }

    /// <summary>
    /// Sequence node (AND).
    /// </summary>
    public class SequenceNode : BehaviorNode
    {
        private List<BehaviorNode> _children = new List<BehaviorNode>();

        public void AddChild(BehaviorNode node)
        {
            _children.Add(node);
        }

        public override NodeStatus Evaluate(AIAgent agent)
        {
            foreach (var child in _children)
            {
                var status = child.Evaluate(agent);
                if (status != NodeStatus.Success)
                    return status;
            }

            return NodeStatus.Success;
        }
    }

    /// <summary>
    /// Condition node.
    /// </summary>
    public class ConditionNode : BehaviorNode
    {
        private Func<AIAgent, bool> _condition;

        public ConditionNode(Func<AIAgent, bool> condition)
        {
            _condition = condition;
        }

        public override NodeStatus Evaluate(AIAgent agent)
        {
            return _condition(agent) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    /// <summary>
    /// Action node.
    /// </summary>
    public class ActionNode : BehaviorNode
    {
        private Func<AIAgent, NodeStatus> _action;

        public ActionNode(Func<AIAgent, NodeStatus> action)
        {
            _action = action;
        }

        public override NodeStatus Evaluate(AIAgent agent)
        {
            return _action(agent);
        }
    }

    #endregion
}
