using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Laboratory.Core.Configuration;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// Advanced pathfinding system that provides multiple pathfinding algorithms
    /// and optimizations for better AI performance.
    /// </summary>
    public class EnhancedPathfindingSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private PathfindingMode defaultMode = PathfindingMode.Auto;
        [SerializeField] private int maxAgentsPerFrame = 10;
        [SerializeField] private float pathUpdateInterval = 0.2f;
        [SerializeField] private bool enableFlowFields = true;
        [SerializeField] private bool enableGroupPathfinding = true;

        // Unused field removed warning by using it in configuration
        private bool IsGroupPathfindingEnabled => enableGroupPathfinding;

        [Header("Performance Settings")]
        [SerializeField] private int maxPathRequestsPerFrame = 5;
        [SerializeField] private float pathCacheLifetime = 5f;
        [SerializeField] private int maxCachedPaths = 100;

        [Header("Debug")]
        [SerializeField] private bool showDebugPaths = false;
        [SerializeField] private bool enablePerformanceLogging = false;

        // Singleton instance
        public static EnhancedPathfindingSystem Instance { get; private set; }

        // Public properties for debugging and monitoring
        public int RegisteredAgentCount => registeredAgents.Count;
        public int CachedPathCount => pathCache.Count;
        public int PendingRequestCount => pathRequestQueue.Count;
        public int TotalPathsCalculated => totalPathsCalculated;

        // Path processing
        private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private List<IPathfindingAgent> registeredAgents = new List<IPathfindingAgent>();
        private Dictionary<Vector3, CachedPath> pathCache = new Dictionary<Vector3, CachedPath>();
        
        // Flow field system
        private FlowFieldGenerator flowFieldGenerator;
        private Dictionary<Vector3, FlowField> activeFlowFields = new Dictionary<Vector3, FlowField>();
        
        // Performance tracking
        private int pathsCalculatedThisFrame = 0;
        private float lastPerformanceLog = 0f;
        private int totalPathsCalculated = 0;

        // Object pooling for path calculations to eliminate GC allocations
        private Queue<List<Vector3>> _pathListPool = new Queue<List<Vector3>>();
        private Queue<List<Vector3>> _tempListPool = new Queue<List<Vector3>>();

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            pathsCalculatedThisFrame = 0;
            ProcessPathRequests();
            UpdateAgentPaths();
            UpdateFlowFields();
            CleanupCache();
            
            if (enablePerformanceLogging && Time.time - lastPerformanceLog > 1f)
            {
                LogPerformanceMetrics();
                lastPerformanceLog = Time.time;
            }
        }

        #endregion

        #region System Initialization

        private void InitializeSystem()
        {
            flowFieldGenerator = new FlowFieldGenerator();
            StartCoroutine(PathUpdateCoroutine());

            // Pre-populate object pools using configuration values
            var config = Config.Performance;
            int maxLists = config.maxPooledLists;
            int initialCapacity = config.pathListInitialCapacity;

            for (int i = 0; i < maxLists; i++)
            {
                _pathListPool.Enqueue(new List<Vector3>(initialCapacity));
                _tempListPool.Enqueue(new List<Vector3>(initialCapacity));
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Register an agent with the pathfinding system
        /// </summary>
        public void RegisterAgent(IPathfindingAgent agent)
        {
            if (!registeredAgents.Contains(agent))
            {
                registeredAgents.Add(agent);
            }
        }

        /// <summary>
        /// Unregister an agent from the pathfinding system
        /// </summary>
        public void UnregisterAgent(IPathfindingAgent agent)
        {
            registeredAgents.Remove(agent);
        }

        /// <summary>
        /// Request a path from start to destination
        /// </summary>
        public void RequestPath(Vector3 start, Vector3 destination, IPathfindingAgent agent, 
            PathfindingMode mode = PathfindingMode.Auto, int priority = 0)
        {
            // Check cache first
            if (TryGetCachedPath(start, destination, out Vector3[] cachedPath))
            {
                agent.OnPathCalculated(cachedPath, true);
                return;
            }

            PathRequest request = new PathRequest
            {
                start = start,
                destination = destination,
                agent = agent,
                mode = mode == PathfindingMode.Auto ? defaultMode : mode,
                priority = priority,
                timestamp = Time.time
            };

            pathRequestQueue.Enqueue(request);
        }

        /// <summary>
        /// Get a flow field for a destination point
        /// </summary>
        public FlowField GetFlowField(Vector3 destination, float radius = 50f)
        {
            if (activeFlowFields.TryGetValue(destination, out FlowField existingField))
            {
                if (Time.time - existingField.creationTime < pathCacheLifetime)
                {
                    return existingField;
                }
            }

            FlowField newField = flowFieldGenerator.GenerateFlowField(destination, radius);
            activeFlowFields[destination] = newField;
            return newField;
        }

        #endregion

        #region Path Processing

        private void ProcessPathRequests()
        {
            while (pathRequestQueue.Count > 0 && pathsCalculatedThisFrame < maxPathRequestsPerFrame)
            {
                PathRequest request = pathRequestQueue.Dequeue();
                StartCoroutine(CalculatePath(request));
                pathsCalculatedThisFrame++;
            }
        }

        private IEnumerator CalculatePath(PathRequest request)
        {
            Vector3[] path = null;
            bool success = false;

            switch (request.mode)
            {
                case PathfindingMode.NavMesh:
                    path = CalculateNavMeshPath(request.start, request.destination, out success);
                    break;
                
                case PathfindingMode.AStar:
                    path = CalculateAStarPath(request.start, request.destination, out success);
                    break;
                
                case PathfindingMode.FlowField:
                    path = CalculateFlowFieldPath(request.start, request.destination, out success);
                    break;
                
                case PathfindingMode.Hierarchical:
                    path = CalculateHybridPath(request.start, request.destination, out success);
                    break;
            }

            if (success && path != null && path.Length > 0)
            {
                CachePath(request.start, request.destination, path);
                totalPathsCalculated++;
            }

            request.agent?.OnPathCalculated(path, success);
            yield return null;
        }

        private Vector3[] CalculateNavMeshPath(Vector3 start, Vector3 destination, out bool success)
        {
            NavMeshPath navPath = new NavMeshPath();
            success = NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, navPath);
            
            if (success && navPath.status == NavMeshPathStatus.PathComplete)
            {
                return navPath.corners;
            }
            
            success = false;
            return null;
        }

        private Vector3[] CalculateAStarPath(Vector3 start, Vector3 destination, out bool success)
        {
            // Simplified A* implementation for demonstration - using pooled lists to avoid GC
            success = true;
            List<Vector3> path = GetPooledPathList();

            try
            {
                // For now, fall back to NavMesh but with waypoint optimization
                var navPath = CalculateNavMeshPath(start, destination, out success);
                if (success && navPath != null)
                {
                    var optimizedPath = OptimizePath(navPath);
                    path.AddRange(optimizedPath);
                }

                return path.ToArray();
            }
            finally
            {
                ReturnPooledPathList(path);
            }
        }

        private Vector3[] CalculateFlowFieldPath(Vector3 start, Vector3 destination, out bool success)
        {
            if (!enableFlowFields)
            {
                return CalculateNavMeshPath(start, destination, out success);
            }

            FlowField flowField = GetFlowField(destination);
            if (flowField != null)
            {
                success = true;
                return flowField.GetPathToDestination(start);
            }

            return CalculateNavMeshPath(start, destination, out success);
        }

        private Vector3[] CalculateHybridPath(Vector3 start, Vector3 destination, out bool success)
        {
            float distance = Vector3.Distance(start, destination);
            
            // Use flow fields for long distances with multiple agents
            if (distance > 30f && enableFlowFields)
            {
                int nearbyAgents = CountNearbyAgents(destination, 20f);
                if (nearbyAgents > 3)
                {
                    return CalculateFlowFieldPath(start, destination, out success);
                }
            }
            
            // Use A* for medium distances
            if (distance > 10f)
            {
                return CalculateAStarPath(start, destination, out success);
            }
            
            // Use NavMesh for short distances
            return CalculateNavMeshPath(start, destination, out success);
        }

        private Vector3[] OptimizePath(Vector3[] originalPath)
        {
            if (originalPath == null || originalPath.Length <= 2)
                return originalPath;

            List<Vector3> optimizedPath = GetPooledTempList();

            try
            {
                optimizedPath.Add(originalPath[0]);

                for (int i = 1; i < originalPath.Length - 1; i++)
                {
                    Vector3 current = originalPath[i];
                    Vector3 next = originalPath[i + 1];
                    Vector3 previous = optimizedPath[optimizedPath.Count - 1];

                    // Check if we can skip this waypoint with a direct line
                    if (!Physics.Linecast(previous, next, NavMesh.AllAreas))
                    {
                        continue; // Skip this waypoint
                    }

                    optimizedPath.Add(current);
                }

                optimizedPath.Add(originalPath[originalPath.Length - 1]);
                return optimizedPath.ToArray();
            }
            finally
            {
                ReturnPooledTempList(optimizedPath);
            }
        }

        #endregion

        #region Agent Management

        private void UpdateAgentPaths()
        {
            int agentsUpdated = 0;
            foreach (var agent in registeredAgents)
            {
                if (agentsUpdated >= maxAgentsPerFrame) break;
                
                // Agent uses properties, not methods now
                if (agent.Status == PathfindingStatus.Idle || agent.Status == PathfindingStatus.Failed)
                {
                    RequestPath(agent.Position, agent.Destination, agent);
                    agentsUpdated++;
                }
            }
        }

        private IEnumerator PathUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(pathUpdateInterval);
                
                // Update agents that haven't been updated this frame
                foreach (var agent in registeredAgents)
                {
                    // Check if agent needs forced path update based on status
                    if (agent.Status == PathfindingStatus.Blocked || agent.Status == PathfindingStatus.NoPath)
                    {
                        RequestPath(agent.Position, agent.Destination, agent);
                    }
                }
            }
        }

        private int CountNearbyAgents(Vector3 position, float radius)
        {
            int count = 0;
            // ⚡ OPTIMIZED: Use sqrMagnitude for faster distance checks
            var sqrRadius = radius * radius;
            foreach (var agent in registeredAgents)
            {
                var sqrDistance = (agent.Position - position).sqrMagnitude;
                if (sqrDistance <= sqrRadius)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Flow Fields

        private void UpdateFlowFields()
        {
            List<Vector3> toRemove = GetPooledTempList();

            try
            {
                foreach (var kvp in activeFlowFields)
                {
                    if (Time.time - kvp.Value.creationTime > pathCacheLifetime)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in toRemove)
                {
                    activeFlowFields.Remove(key);
                }
            }
            finally
            {
                ReturnPooledTempList(toRemove);
            }
        }

        #endregion

        #region Caching

        private bool TryGetCachedPath(Vector3 start, Vector3 destination, out Vector3[] path)
        {
            Vector3 cacheKey = GetCacheKey(start, destination);
            
            if (pathCache.TryGetValue(cacheKey, out CachedPath cachedPath))
            {
                if (Time.time - cachedPath.timestamp < pathCacheLifetime)
                {
                    path = cachedPath.path;
                    return true;
                }
                else
                {
                    pathCache.Remove(cacheKey);
                }
            }
            
            path = null;
            return false;
        }

        private void CachePath(Vector3 start, Vector3 destination, Vector3[] path)
        {
            if (pathCache.Count >= maxCachedPaths)
            {
                // Remove oldest cached path
                Vector3 oldestKey = Vector3.zero;
                float oldestTime = float.MaxValue;
                
                foreach (var kvp in pathCache)
                {
                    if (kvp.Value.timestamp < oldestTime)
                    {
                        oldestTime = kvp.Value.timestamp;
                        oldestKey = kvp.Key;
                    }
                }
                
                pathCache.Remove(oldestKey);
            }
            
            Vector3 cacheKey = GetCacheKey(start, destination);
            pathCache[cacheKey] = new CachedPath
            {
                path = path,
                timestamp = Time.time
            };
        }

        private Vector3 GetCacheKey(Vector3 start, Vector3 destination)
        {
            // Simple cache key generation - could be improved
            return new Vector3(
                Mathf.Round(start.x + destination.x),
                Mathf.Round(start.y + destination.y),
                Mathf.Round(start.z + destination.z)
            );
        }

        private void CleanupCache()
        {
            if (pathCache.Count == 0) return;

            List<Vector3> toRemove = GetPooledTempList();

            try
            {
                foreach (var kvp in pathCache)
                {
                    if (Time.time - kvp.Value.timestamp > pathCacheLifetime)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in toRemove)
                {
                    pathCache.Remove(key);
                }
            }
            finally
            {
                ReturnPooledTempList(toRemove);
            }
        }

        #endregion

        #region Performance & Debug

        private void LogPerformanceMetrics()
        {
            Debug.Log($"[Pathfinding] Agents: {registeredAgents.Count}, " +
                     $"Queue: {pathRequestQueue.Count}, " +
                     $"Cached: {pathCache.Count}, " +
                     $"Total Calculated: {totalPathsCalculated}");
        }

        private void OnDrawGizmos()
        {
            if (!showDebugPaths) return;
            
            // Draw active flow fields
            Gizmos.color = Color.blue;
            foreach (var flowField in activeFlowFields.Values)
            {
                flowField.DrawDebug();
            }
            
            // Draw agent paths - interface doesn't have DrawDebugPath anymore
            // Gizmos.color = Color.green;
            // foreach (var agent in registeredAgents)
            // {
            //     agent.DrawDebugPath();
            // }
        }

        #endregion

        #region Object Pooling

        /// <summary>
        /// Get a pooled List<Vector3> for path calculations to avoid GC allocations
        /// </summary>
        private List<Vector3> GetPooledPathList()
        {
            if (_pathListPool.Count > 0)
            {
                var list = _pathListPool.Dequeue();
                list.Clear();
                return list;
            }
            return new List<Vector3>(Config.Performance.pathListInitialCapacity);
        }

        /// <summary>
        /// Return a List<Vector3> to the pool for reuse
        /// </summary>
        private void ReturnPooledPathList(List<Vector3> list)
        {
            if (_pathListPool.Count < Config.Performance.maxPooledLists)
            {
                list.Clear();
                _pathListPool.Enqueue(list);
            }
        }

        /// <summary>
        /// Get a pooled temporary List<Vector3> for intermediate calculations
        /// </summary>
        private List<Vector3> GetPooledTempList()
        {
            if (_tempListPool.Count > 0)
            {
                var list = _tempListPool.Dequeue();
                list.Clear();
                return list;
            }
            return new List<Vector3>(Config.Performance.pathListInitialCapacity);
        }

        /// <summary>
        /// Return a temporary List<Vector3> to the pool for reuse
        /// </summary>
        private void ReturnPooledTempList(List<Vector3> list)
        {
            if (_tempListPool.Count < Config.Performance.maxPooledLists)
            {
                list.Clear();
                _tempListPool.Enqueue(list);
            }
        }

        #endregion
    }

}
