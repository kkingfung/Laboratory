using UnityEngine;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// Pathfinding algorithm selection for AI agents.
    /// Different modes offer trade-offs between performance, accuracy, and scalability.
    /// </summary>
    public enum PathfindingMode
    {
        /// <summary>Automatically selects the best pathfinding method based on agent count and terrain complexity.</summary>
        Auto,

        /// <summary>Uses Unity's built-in NavMesh system. Best for single agents with complex geometry.</summary>
        NavMesh,

        /// <summary>A* grid-based pathfinding. Good balance of accuracy and performance for small agent counts.</summary>
        AStar,

        /// <summary>Flow field pathfinding for massive agent counts (1000+). Excellent for swarm behavior.</summary>
        FlowField,

        /// <summary>Combines multiple pathfinding methods dynamically. Best overall performance but higher complexity.</summary>
        Hybrid
    }

    /// <summary>
    /// Interface for AI agents that use the Enhanced Pathfinding System.
    /// Implement this interface to enable advanced pathfinding capabilities for your agents.
    /// </summary>
    public interface IPathfindingAgent
    {
        /// <summary>Gets the current world position of this agent.</summary>
        /// <returns>Current position in world coordinates</returns>
        Vector3 GetPosition();

        /// <summary>Gets the target destination this agent is trying to reach.</summary>
        /// <returns>Target position in world coordinates</returns>
        Vector3 GetDestination();

        /// <summary>Determines if this agent needs a new path calculation.</summary>
        /// <returns>True if path should be recalculated due to movement or destination changes</returns>
        bool NeedsPathUpdate();

        /// <summary>Determines if this agent should force an immediate path update regardless of normal conditions.</summary>
        /// <returns>True if path must be recalculated immediately (e.g., due to obstacles)</returns>
        bool ShouldForcePathUpdate();

        /// <summary>Callback invoked when pathfinding calculation completes.</summary>
        /// <param name="path">Array of waypoints forming the calculated path</param>
        /// <param name="success">Whether pathfinding succeeded or failed</param>
        void OnPathCalculated(Vector3[] path, bool success);

        /// <summary>Draws debug visualization of the agent's current path (Editor only).</summary>
        void DrawDebugPath();
    }

    /// <summary>
    /// Data structure representing a pathfinding calculation request.
    /// Contains all information needed to calculate a path for an AI agent.
    /// </summary>
    [System.Serializable]
    public class PathRequest
    {
        /// <summary>Starting position for path calculation</summary>
        public Vector3 start;

        /// <summary>Target destination for path calculation</summary>
        public Vector3 destination;

        /// <summary>Agent requesting the path (receives callback when complete)</summary>
        public IPathfindingAgent agent;

        /// <summary>Preferred pathfinding algorithm to use for this request</summary>
        public PathfindingMode mode;

        /// <summary>Request priority (higher values processed first). Range: 0-10</summary>
        public int priority;

        /// <summary>Time when this request was created (for timeout handling)</summary>
        public float timestamp;
    }

    /// <summary>
    /// Cached path data to avoid redundant pathfinding calculations.
    /// Paths are cached based on start/destination pairs and expire after a timeout.
    /// </summary>
    [System.Serializable]
    public class CachedPath
    {
        /// <summary>Array of waypoints forming the cached path</summary>
        public Vector3[] path;

        /// <summary>Time when this path was cached (for expiration)</summary>
        public float timestamp;
    }
    
    /// <summary>
    /// Core pathfinding system interface
    /// </summary>
    public interface IPathfindingSystem
    {
        void RequestPath(PathRequest request);
        void CancelPathRequest(IPathfindingAgent agent);
        bool HasCachedPath(Vector3 start, Vector3 destination, float maxAge = 5f);
        Vector3[] GetCachedPath(Vector3 start, Vector3 destination);
        void InvalidatePathsFromPosition(Vector3 position, float radius);
        void SetPathfindingMode(PathfindingMode mode);
        PathfindingMode GetCurrentMode();
    }
}
