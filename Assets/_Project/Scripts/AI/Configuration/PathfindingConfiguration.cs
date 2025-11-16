using UnityEngine;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.Agents;

namespace Laboratory.AI.Configuration
{
    /// <summary>
    /// ScriptableObject configuration for the Enhanced Pathfinding System
    /// Allows for easy tuning of pathfinding parameters across different environments
    /// </summary>
    [CreateAssetMenu(fileName = "PathfindingConfig", menuName = "Laboratory/AI/Pathfinding Configuration")]
    public class PathfindingConfiguration : ScriptableObject
    {
        [Header("Algorithm Selection")]
        [SerializeField] private PathfindingMode defaultMode = PathfindingMode.Hybrid;
        [SerializeField] private float aStarMaxDistance = 20f;
        [SerializeField] private float flowFieldMinAgents = 3;
        [SerializeField] private float hierarchicalMinDistance = 100f;

        [Header("Performance Settings")]
        [SerializeField] private int maxAgentsPerFrame = 10;
        [SerializeField] private float pathUpdateInterval = 0.2f;
        [SerializeField] private int maxPathRequestsPerFrame = 5;
        [SerializeField] private float pathCacheLifetime = 5f;
        [SerializeField] private int maxCachedPaths = 100;

        [Header("Flow Field Settings")]
        [SerializeField] private bool enableFlowFields = true;
        [SerializeField] private float flowFieldCellSize = 2f;
        [SerializeField] private float flowFieldRadius = 50f;
        [SerializeField] private float flowFieldLifetime = 30f;

        [Header("A* Settings")]
        [SerializeField] private float aStarCellSize = 1f;
        [SerializeField] private int aStarMaxIterations = 1000;
        [SerializeField] private bool enablePathSmoothing = true;

        [Header("LOD Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float highDetailRange = 20f;
        [SerializeField] private float mediumDetailRange = 50f;
        [SerializeField] private float lowDetailUpdateMultiplier = 4f;

        [Header("Debug Settings")]
        [SerializeField] private bool enablePerformanceLogging = false;
        [SerializeField] private bool showDebugPaths = false;
        [SerializeField] private Color pathDebugColor = Color.green;
        [SerializeField] private Color flowFieldDebugColor = Color.blue;

        // Properties for easy access
        public PathfindingMode DefaultMode => defaultMode;
        public float AStarMaxDistance => aStarMaxDistance;
        public float FlowFieldMinAgents => flowFieldMinAgents;
        public float HierarchicalMinDistance => hierarchicalMinDistance;
        public int MaxAgentsPerFrame => maxAgentsPerFrame;
        public float PathUpdateInterval => pathUpdateInterval;
        public int MaxPathRequestsPerFrame => maxPathRequestsPerFrame;
        public float PathCacheLifetime => pathCacheLifetime;
        public int MaxCachedPaths => maxCachedPaths;
        public bool EnableFlowFields => enableFlowFields;
        public float FlowFieldCellSize => flowFieldCellSize;
        public float FlowFieldRadius => flowFieldRadius;
        public float FlowFieldLifetime => flowFieldLifetime;
        public float AStarCellSize => aStarCellSize;
        public int AStarMaxIterations => aStarMaxIterations;
        public bool EnablePathSmoothing => enablePathSmoothing;
        public bool EnableLOD => enableLOD;
        public float HighDetailRange => highDetailRange;
        public float MediumDetailRange => mediumDetailRange;
        public float LowDetailUpdateMultiplier => lowDetailUpdateMultiplier;
        public bool EnablePerformanceLogging => enablePerformanceLogging;
        public bool ShowDebugPaths => showDebugPaths;
        public Color PathDebugColor => pathDebugColor;
        public Color FlowFieldDebugColor => flowFieldDebugColor;

        /// <summary>
        /// Get the optimal pathfinding mode based on current configuration and parameters
        /// </summary>
        public PathfindingMode GetOptimalMode(float distance, int nearbyAgents, Laboratory.AI.Pathfinding.AgentType agentType)
        {
            // Short distances - use A*
            if (distance <= aStarMaxDistance)
                return PathfindingMode.AStar;
            
            // Multiple agents going to similar destination - use flow fields
            if (enableFlowFields && nearbyAgents >= flowFieldMinAgents && distance <= flowFieldRadius)
                return PathfindingMode.FlowField;
            
            // Long distances or complex scenarios - use hybrid approach
            if (distance >= hierarchicalMinDistance)
                return PathfindingMode.Hybrid;
            
            // Default fallback
            return PathfindingMode.NavMesh;
        }

        /// <summary>
        /// Create a default configuration
        /// </summary>
        public static PathfindingConfiguration CreateDefault()
        {
            var config = CreateInstance<PathfindingConfiguration>();
            config.name = "Default Pathfinding Config";
            return config;
        }

        /// <summary>
        /// Create a performance-optimized configuration for mobile/low-end devices
        /// </summary>
        public static PathfindingConfiguration CreateMobileOptimized()
        {
            var config = CreateInstance<PathfindingConfiguration>();
            config.name = "Mobile Optimized Config";
            
            // Reduce performance demands
            config.maxAgentsPerFrame = 5;
            config.pathUpdateInterval = 0.5f;
            config.maxPathRequestsPerFrame = 3;
            config.enableFlowFields = false;
            config.highDetailRange = 10f;
            config.mediumDetailRange = 20f;
            config.lowDetailUpdateMultiplier = 6f;
            
            return config;
        }

        /// <summary>
        /// Create a high-performance configuration for powerful devices
        /// </summary>
        public static PathfindingConfiguration CreateHighPerformance()
        {
            var config = CreateInstance<PathfindingConfiguration>();
            config.name = "High Performance Config";
            
            // Enable all features with high limits
            config.maxAgentsPerFrame = 20;
            config.pathUpdateInterval = 0.1f;
            config.maxPathRequestsPerFrame = 10;
            config.enableFlowFields = true;
            config.maxCachedPaths = 200;
            config.highDetailRange = 30f;
            config.mediumDetailRange = 60f;
            
            return config;
        }

        /// <summary>
        /// Validate configuration values and clamp to reasonable ranges
        /// </summary>
        private void OnValidate()
        {
            // Clamp performance values to reasonable ranges
            maxAgentsPerFrame = Mathf.Clamp(maxAgentsPerFrame, 1, 50);
            pathUpdateInterval = Mathf.Clamp(pathUpdateInterval, 0.05f, 2f);
            maxPathRequestsPerFrame = Mathf.Clamp(maxPathRequestsPerFrame, 1, 20);
            pathCacheLifetime = Mathf.Clamp(pathCacheLifetime, 1f, 60f);
            maxCachedPaths = Mathf.Clamp(maxCachedPaths, 10, 1000);
            
            // Clamp distance values
            aStarMaxDistance = Mathf.Clamp(aStarMaxDistance, 5f, 100f);
            hierarchicalMinDistance = Mathf.Clamp(hierarchicalMinDistance, 50f, 500f);
            
            // Clamp LOD ranges
            highDetailRange = Mathf.Clamp(highDetailRange, 5f, 100f);
            mediumDetailRange = Mathf.Clamp(mediumDetailRange, highDetailRange, 200f);
            lowDetailUpdateMultiplier = Mathf.Clamp(lowDetailUpdateMultiplier, 1f, 10f);
            
            // Ensure flow field settings are reasonable
            flowFieldCellSize = Mathf.Clamp(flowFieldCellSize, 0.5f, 5f);
            flowFieldRadius = Mathf.Clamp(flowFieldRadius, 10f, 200f);
            flowFieldLifetime = Mathf.Clamp(flowFieldLifetime, 5f, 300f);
            
            // A* settings
            aStarCellSize = Mathf.Clamp(aStarCellSize, 0.25f, 2f);
            aStarMaxIterations = Mathf.Clamp(aStarMaxIterations, 100, 5000);
        }
    }

    /// <summary>
    /// Agent-specific configuration for different types of AI
    /// </summary>
    [CreateAssetMenu(fileName = "AgentConfig", menuName = "Laboratory/AI/Agent Configuration")]
    public class AgentConfiguration : ScriptableObject
    {
        [Header("Movement Settings")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float angularSpeed = 120f;
        [SerializeField] private float stoppingDistance = 1f;
        [SerializeField] private float rotationSmoothing = 5f;

        [Header("Pathfinding Preferences")]
        [SerializeField] private PathfindingMode preferredMode = PathfindingMode.Auto;
        [SerializeField] private bool enablePathSmoothing = true;
        [SerializeField] private bool enableLookAhead = true;
        [SerializeField] private float lookAheadDistance = 3f;
        [SerializeField] private int maxPathNodes = 50;

        [Header("Performance Settings")]
        [SerializeField] private float pathUpdateFrequency = 0.5f;
        [SerializeField] private bool enableStuckDetection = true;
        [SerializeField] private float stuckThreshold = 0.1f;
        [SerializeField] private int stuckFrames = 30;

        [Header("Agent Type Specific")]
        [SerializeField] private Laboratory.AI.Pathfinding.AgentType agentType = Laboratory.AI.Pathfinding.AgentType.Medium;
        [SerializeField] private float agentRadius = 0.5f;
        [SerializeField] private float agentHeight = 2f;

        // Properties
        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float AngularSpeed => angularSpeed;
        public float StoppingDistance => stoppingDistance;
        public float RotationSmoothing => rotationSmoothing;
        public PathfindingMode PreferredMode => preferredMode;
        public bool EnablePathSmoothing => enablePathSmoothing;
        public bool EnableLookAhead => enableLookAhead;
        public float LookAheadDistance => lookAheadDistance;
        public int MaxPathNodes => maxPathNodes;
        public float PathUpdateFrequency => pathUpdateFrequency;
        public bool EnableStuckDetection => enableStuckDetection;
        public float StuckThreshold => stuckThreshold;
        public int StuckFrames => stuckFrames;
        public Laboratory.AI.Pathfinding.AgentType CurrentAgentType => agentType;
        public float AgentRadius => agentRadius;
        public float AgentHeight => agentHeight;

        /// <summary>
        /// Apply this configuration to an Enhanced AI Agent
        /// </summary>
        public void ApplyToAgent(EnhancedAIAgent agent)
        {
            if (agent == null) return;

            agent.SetAgentType(agentType);
            // Additional configuration application would go here
            // This would require public setters on the EnhancedAIAgent
        }

        /// <summary>
        /// Create configuration for different agent types
        /// </summary>
        public static AgentConfiguration CreateForType(Laboratory.AI.Pathfinding.AgentType type)
        {
            var config = CreateInstance<AgentConfiguration>();
            config.agentType = type;
            config.name = $"{type} Agent Config";

            switch (type)
            {
                case EnhancedAIAgent.AgentType.Small:
                    config.maxSpeed = 4f;
                    config.agentRadius = 0.3f;
                    config.agentHeight = 1f;
                    config.pathUpdateFrequency = 0.3f;
                    break;

                case EnhancedAIAgent.AgentType.Medium:
                    config.maxSpeed = 5f;
                    config.agentRadius = 0.5f;
                    config.agentHeight = 2f;
                    config.pathUpdateFrequency = 0.5f;
                    break;

                case EnhancedAIAgent.AgentType.Large:
                    config.maxSpeed = 3f;
                    config.agentRadius = 1f;
                    config.agentHeight = 3f;
                    config.pathUpdateFrequency = 0.7f;
                    config.acceleration = 4f;
                    break;

                case EnhancedAIAgent.AgentType.Flying:
                    config.maxSpeed = 8f;
                    config.agentRadius = 0.5f;
                    config.agentHeight = 2f;
                    config.pathUpdateFrequency = 0.3f;
                    config.enableLookAhead = true;
                    config.lookAheadDistance = 5f;
                    break;
            }

            return config;
        }

        private void OnValidate()
        {
            maxSpeed = Mathf.Clamp(maxSpeed, 0.1f, 20f);
            acceleration = Mathf.Clamp(acceleration, 1f, 20f);
            angularSpeed = Mathf.Clamp(angularSpeed, 30f, 360f);
            stoppingDistance = Mathf.Clamp(stoppingDistance, 0.1f, 5f);
            rotationSmoothing = Mathf.Clamp(rotationSmoothing, 1f, 20f);
            lookAheadDistance = Mathf.Clamp(lookAheadDistance, 1f, 10f);
            maxPathNodes = Mathf.Clamp(maxPathNodes, 10, 200);
            pathUpdateFrequency = Mathf.Clamp(pathUpdateFrequency, 0.1f, 2f);
            stuckThreshold = Mathf.Clamp(stuckThreshold, 0.01f, 1f);
            stuckFrames = Mathf.Clamp(stuckFrames, 10, 120);
            agentRadius = Mathf.Clamp(agentRadius, 0.1f, 3f);
            agentHeight = Mathf.Clamp(agentHeight, 0.5f, 10f);
        }
    }
}