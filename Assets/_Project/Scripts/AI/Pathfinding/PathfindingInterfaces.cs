using UnityEngine;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// Pathfinding mode selection for agents
    /// </summary>
    public enum PathfindingMode
    {
        Auto,
        NavMesh,
        AStar,
        FlowField,
        Hybrid
    }

    /// <summary>
    /// Interface for agents that use the Enhanced Pathfinding System
    /// </summary>
    public interface IPathfindingAgent
    {
        Vector3 GetPosition();
        Vector3 GetDestination();
        bool NeedsPathUpdate();
        bool ShouldForcePathUpdate();
        void OnPathCalculated(Vector3[] path, bool success);
        void DrawDebugPath();
    }

    /// <summary>
    /// Path request data structure
    /// </summary>
    [System.Serializable]
    public class PathRequest
    {
        public Vector3 start;
        public Vector3 destination;
        public IPathfindingAgent agent;
        public PathfindingMode mode;
        public int priority;
        public float timestamp;
    }

    /// <summary>
    /// Cached path data structure
    /// </summary>
    [System.Serializable]
    public class CachedPath
    {
        public Vector3[] path;
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
