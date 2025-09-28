using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// High-performance A* pathfinder optimized for Unity with coroutine support.
    /// Uses grid-based navigation with obstacle detection and frame-spreading for smooth gameplay.
    /// Implements classic A* algorithm with Manhattan+Diagonal heuristic for optimal pathfinding.
    /// </summary>
    public class AStarPathfinder
    {
        /// <summary>Maximum pathfinding iterations per request to prevent infinite loops</summary>
        private const int MAX_ITERATIONS = 1000;

        /// <summary>Size of each grid cell in world units (smaller = more precise, slower)</summary>
        private readonly float cellSize = 1f;

        /// <summary>LayerMask for obstacle detection (-1 = all layers)</summary>
        private readonly LayerMask obstacleLayer = -1;

        /// <summary>
        /// Asynchronously finds an optimal path between two world positions using A* algorithm.
        /// Spreads computation across multiple frames to maintain stable framerate.
        /// </summary>
        /// <param name="start">Starting world position</param>
        /// <param name="end">Target destination world position</param>
        /// <param name="callback">Callback invoked with (path waypoints, success) when complete</param>
        /// <returns>Coroutine enumerator for Unity StartCoroutine</returns>
        public IEnumerator FindPath(Vector3 start, Vector3 end, Action<(List<Vector3> path, bool success)> callback)
        {
            var openSet = new PriorityQueue<AStarNode>();
            var closedSet = new HashSet<Vector3>();
            var cameFrom = new Dictionary<Vector3, Vector3>();
            var gScore = new Dictionary<Vector3, float>();
            var fScore = new Dictionary<Vector3, float>();

            Vector3 startGrid = WorldToGrid(start);
            Vector3 endGrid = WorldToGrid(end);

            gScore[startGrid] = 0;
            fScore[startGrid] = Heuristic(startGrid, endGrid);
            openSet.Enqueue(new AStarNode { Position = startGrid, FScore = fScore[startGrid] });

            int iterations = 0;
            while (openSet.Count > 0 && iterations < MAX_ITERATIONS)
            {
                iterations++;
                
                // Yield every 50 iterations to prevent frame drops
                if (iterations % 50 == 0)
                {
                    yield return null;
                }

                var current = openSet.Dequeue();
                
                if (Vector3.Distance(current.Position, endGrid) < cellSize)
                {
                    // Path found, reconstruct it
                    var path = ReconstructPath(cameFrom, current.Position, startGrid, start, end);
                    callback((path, true));
                    yield break;
                }

                closedSet.Add(current.Position);

                // Check all neighbors
                foreach (var neighbor in GetNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighbor) || IsObstacle(neighbor))
                        continue;

                    float tentativeGScore = gScore.GetValueOrDefault(current.Position, float.MaxValue) + 
                                          Vector3.Distance(current.Position, neighbor);

                    if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current.Position;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, endGrid);

                        if (!ContainsPosition(openSet, neighbor))
                        {
                            openSet.Enqueue(new AStarNode { Position = neighbor, FScore = fScore[neighbor] });
                        }
                    }
                }
            }

            // No path found
            callback((null, false));
        }

        private Vector3 WorldToGrid(Vector3 worldPos)
        {
            return new Vector3(
                Mathf.Round(worldPos.x / cellSize) * cellSize,
                worldPos.y,
                Mathf.Round(worldPos.z / cellSize) * cellSize
            );
        }

        private List<Vector3> GetNeighbors(Vector3 position)
        {
            var neighbors = new List<Vector3>();
            
            // 8-directional movement
            Vector3[] directions = {
                new Vector3(cellSize, 0, 0),      // Right
                new Vector3(-cellSize, 0, 0),     // Left
                new Vector3(0, 0, cellSize),      // Forward
                new Vector3(0, 0, -cellSize),     // Back
                new Vector3(cellSize, 0, cellSize),   // Right-Forward
                new Vector3(-cellSize, 0, cellSize),  // Left-Forward
                new Vector3(cellSize, 0, -cellSize),  // Right-Back
                new Vector3(-cellSize, 0, -cellSize)  // Left-Back
            };

            foreach (var dir in directions)
            {
                neighbors.Add(position + dir);
            }

            return neighbors;
        }

        /// <summary>
        /// Checks if a grid position contains an obstacle.
        /// Uses NavMesh sampling for accurate walkability detection.
        /// </summary>
        /// <param name="position">Grid position to check</param>
        /// <returns>True if position is blocked, false if walkable</returns>
        private bool IsObstacle(Vector3 position)
        {
            // Use NavMesh sampling for reliable obstacle detection
            // More accurate than raycasting for complex geometry
            return !NavMesh.SamplePosition(position, out NavMeshHit hit, cellSize * 0.5f, NavMesh.AllAreas);
        }

        /// <summary>
        /// Calculates heuristic distance estimate between two grid positions.
        /// Uses Chebyshev distance (diagonal movement allowed) for optimal A* performance.
        /// </summary>
        /// <param name="a">Start position</param>
        /// <param name="b">Goal position</param>
        /// <returns>Estimated distance cost (never overestimates for A* optimality)</returns>
        private float Heuristic(Vector3 a, Vector3 b)
        {
            // Chebyshev distance with diagonal movement cost adjustment
            float dx = Mathf.Abs(a.x - b.x);
            float dz = Mathf.Abs(a.z - b.z);
            // Formula: D * (dx + dz) + (D2 - 2 * D) * min(dx, dz)
            // Where D = 1 (straight move cost), D2 = √2 ≈ 1.414 (diagonal move cost)
            return (dx + dz) + (1.414f - 2) * Mathf.Min(dx, dz);
        }

        /// <summary>
        /// Reconstructs the final path from A* search results.
        /// Works backwards from goal using the cameFrom map, then smooths the path.
        /// </summary>
        /// <param name="cameFrom">Dictionary mapping each node to its parent in the optimal path</param>
        /// <param name="current">Goal position reached by A*</param>
        /// <param name="startGrid">Grid-aligned start position</param>
        /// <param name="actualStart">Original world start position</param>
        /// <param name="actualEnd">Original world end position</param>
        /// <returns>Smoothed path waypoints from start to end</returns>
        private List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current,
            Vector3 startGrid, Vector3 actualStart, Vector3 actualEnd)
        {
            var path = new List<Vector3>();

            // Trace backwards through the optimal path using parent links
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Add(startGrid);
            path.Reverse(); // Convert from goal→start to start→goal

            // Convert grid positions to world positions, preserving exact start/end
            var worldPath = new List<Vector3> { actualStart };

            // Add intermediate grid positions
            for (int i = 1; i < path.Count - 1; i++)
            {
                worldPath.Add(path[i]);
            }

            worldPath.Add(actualEnd);

            // Apply path smoothing to reduce waypoint count and create natural movement
            return SmoothPath(worldPath);
        }

        private List<Vector3> SmoothPath(List<Vector3> path)
        {
            if (path.Count <= 2) return path;

            var smoothed = new List<Vector3> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                // Check if we can skip this waypoint
                if (!HasLineOfSight(smoothed[smoothed.Count - 1], path[i + 1]))
                {
                    smoothed.Add(path[i]);
                }
            }

            smoothed.Add(path[path.Count - 1]);
            return smoothed;
        }

        private bool HasLineOfSight(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            
            return !Physics.Raycast(start + Vector3.up * 0.5f, direction.normalized, 
                distance, obstacleLayer);
        }

        private bool ContainsPosition(PriorityQueue<AStarNode> openSet, Vector3 position)
        {
            foreach (var node in openSet)
            {
                if (Vector3.Distance(node.Position, position) < 0.1f)
                    return true;
            }
            return false;
        }

        private class AStarNode : IComparable<AStarNode>
        {
            public Vector3 Position;
            public float FScore;

            public int CompareTo(AStarNode other)
            {
                return FScore.CompareTo(other.FScore);
            }
        }
    }

    /// <summary>
    /// Hierarchical pathfinder for long-distance navigation
    /// </summary>
    public class HierarchicalPathfinder
    {
        private const float REGION_SIZE = 50f;
        private Dictionary<Vector2Int, NavMeshRegion> regions = new Dictionary<Vector2Int, NavMeshRegion>();

        public IEnumerator FindPath(Vector3 start, Vector3 end, Action<(List<Vector3> path, bool success)> callback)
        {
            // Step 1: Find high-level path between regions
            var startRegion = GetRegion(start);
            var endRegion = GetRegion(end);

            var regionPath = new List<Vector2Int>();
            yield return StartCoroutine(FindRegionPath(startRegion, endRegion, path => regionPath = path));

            if (regionPath.Count == 0)
            {
                callback((null, false));
                yield break;
            }

            // Step 2: Find detailed path within regions
            var detailedPath = new List<Vector3>();
            Vector3 currentPos = start;

            foreach (var regionCoord in regionPath)
            {
                Vector3 regionCenter = RegionToWorldCenter(regionCoord);
                Vector3 targetInRegion = (regionCoord == endRegion) ? end : regionCenter;

                // Use NavMesh for detailed pathfinding within region
                var navPath = new NavMeshPath();
                if (NavMesh.CalculatePath(currentPos, targetInRegion, NavMesh.AllAreas, navPath))
                {
                    for (int i = (detailedPath.Count > 0 ? 1 : 0); i < navPath.corners.Length; i++)
                    {
                        detailedPath.Add(navPath.corners[i]);
                    }
                    currentPos = targetInRegion;
                }

                yield return null; // Yield between regions
            }

            callback((detailedPath, detailedPath.Count > 0));
        }

        private IEnumerator StartCoroutine(IEnumerator routine)
        {
            yield return routine;
        }

        private Vector2Int GetRegion(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / REGION_SIZE),
                Mathf.FloorToInt(worldPos.z / REGION_SIZE)
            );
        }

        private Vector3 RegionToWorldCenter(Vector2Int regionCoord)
        {
            return new Vector3(
                regionCoord.x * REGION_SIZE + REGION_SIZE * 0.5f,
                0,
                regionCoord.y * REGION_SIZE + REGION_SIZE * 0.5f
            );
        }

        private IEnumerator FindRegionPath(Vector2Int start, Vector2Int end, Action<List<Vector2Int>> callback)
        {
            // Simple BFS for region-level pathfinding
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            int iterations = 0;
            while (queue.Count > 0 && iterations < 1000)
            {
                iterations++;
                if (iterations % 50 == 0) yield return null;

                var current = queue.Dequeue();
                
                if (current == end)
                {
                    // Reconstruct region path
                    var path = new List<Vector2Int>();
                    var node = end;
                    
                    while (parent.ContainsKey(node))
                    {
                        path.Add(node);
                        node = parent[node];
                    }
                    path.Add(start);
                    path.Reverse();
                    
                    callback(path);
                    yield break;
                }

                // Check adjacent regions
                Vector2Int[] neighbors = {
                    new Vector2Int(current.x + 1, current.y),
                    new Vector2Int(current.x - 1, current.y),
                    new Vector2Int(current.x, current.y + 1),
                    new Vector2Int(current.x, current.y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor) && IsRegionNavigable(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            callback(new List<Vector2Int>()); // No path found
        }

        private bool IsRegionNavigable(Vector2Int regionCoord)
        {
            Vector3 regionCenter = RegionToWorldCenter(regionCoord);
            return NavMesh.SamplePosition(regionCenter, out NavMeshHit hit, REGION_SIZE * 0.5f, NavMesh.AllAreas);
        }

        private class NavMeshRegion
        {
            public Vector2Int Coordinate;
            public List<Vector3> Connections;
            public bool IsNavigable;
        }
    }

    /// <summary>
    /// Simple priority queue implementation for pathfinding algorithms
    /// </summary>
    public class PriorityQueue<T> : IEnumerable<T> where T : IComparable<T>
    {
        private List<T> data;

        public PriorityQueue()
        {
            data = new List<T>();
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;

            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;

                if (data[childIndex].CompareTo(data[parentIndex]) >= 0)
                    break;

                var tmp = data[childIndex];
                data[childIndex] = data[parentIndex];
                data[parentIndex] = tmp;

                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);

            lastIndex--;
            int parentIndex = 0;

            while (true)
            {
                int childIndex = parentIndex * 2 + 1;
                if (childIndex > lastIndex)
                    break;

                int rightChild = childIndex + 1;
                if (rightChild <= lastIndex && data[rightChild].CompareTo(data[childIndex]) < 0)
                    childIndex = rightChild;

                if (data[parentIndex].CompareTo(data[childIndex]) <= 0)
                    break;

                var tmp = data[parentIndex];
                data[parentIndex] = data[childIndex];
                data[childIndex] = tmp;

                parentIndex = childIndex;
            }

            return frontItem;
        }

        public T Peek()
        {
            return data[0];
        }

        public int Count => data.Count;

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}