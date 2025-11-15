using UnityEngine;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// Pathfinding operation modes
    /// </summary>
    public enum PathfindingMode : byte
    {
        /// <summary>Automatic mode selection based on conditions</summary>
        Auto = 0,
        /// <summary>Standard Unity NavMesh pathfinding</summary>
        NavMesh = 1,
        /// <summary>A* grid-based pathfinding</summary>
        AStar = 2,
        /// <summary>Flow field pathfinding for groups</summary>
        FlowField = 3,
        /// <summary>Hierarchical pathfinding for large distances</summary>
        Hierarchical = 4,
        /// <summary>Direct movement without pathfinding</summary>
        Direct = 5
    }

    /// <summary>
    /// Current status of a pathfinding operation
    /// </summary>
    public enum PathfindingStatus : byte
    {
        /// <summary>No active pathfinding request</summary>
        Idle = 0,
        /// <summary>Computing path</summary>
        Computing = 1,
        /// <summary>Path found and ready</summary>
        PathReady = 2,
        /// <summary>Following path</summary>
        Following = 3,
        /// <summary>Path blocked or invalid</summary>
        Blocked = 4,
        /// <summary>No path to destination exists</summary>
        NoPath = 5,
        /// <summary>Pathfinding failed due to error</summary>
        Failed = 6,
        /// <summary>Request was cancelled</summary>
        Cancelled = 7
    }

    /// <summary>
    /// Type of AI agent for pathfinding optimization
    /// </summary>
    public enum AgentType : byte
    {
        /// <summary>Small, fast agent (mouse, small creature)</summary>
        Small = 0,
        /// <summary>Medium agent (human-sized)</summary>
        Medium = 1,
        /// <summary>Large agent (vehicle, large creature)</summary>
        Large = 2,
        /// <summary>Aerial agent (flying creatures)</summary>
        Flying = 3,
        /// <summary>Aquatic agent (swimming creatures)</summary>
        Aquatic = 4
    }

    /// <summary>
    /// Interface for objects that can use pathfinding
    /// </summary>
    public interface IPathfindingAgent
    {
        /// <summary>Current agent type</summary>
        AgentType AgentType { get; }

        /// <summary>Current pathfinding mode</summary>
        PathfindingMode PathfindingMode { get; }

        /// <summary>Current pathfinding status</summary>
        PathfindingStatus Status { get; }

        /// <summary>Current position</summary>
        Vector3 Position { get; }

        /// <summary>Current destination</summary>
        Vector3 Destination { get; }

        /// <summary>Request pathfinding to a destination</summary>
        void RequestPath(Vector3 destination, PathfindingMode mode = PathfindingMode.Auto);

        /// <summary>Cancel current pathfinding request</summary>
        void CancelPath();

        /// <summary>Check if agent has reached destination</summary>
        bool HasReachedDestination();
    }
}