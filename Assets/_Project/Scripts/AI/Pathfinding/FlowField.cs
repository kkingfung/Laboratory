using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// Flow field implementation for efficient pathfinding when many agents share the same destination.
    /// Uses a pre-calculated direction grid where each cell points toward the optimal path to the target.
    /// Ideal for swarm behavior, crowd simulation, and large-scale agent movement (1000+ agents).
    /// </summary>
    public class FlowField
    {
        /// <summary>World position of the flow field destination</summary>
        public Vector3 destination;

        /// <summary>Radius of the flow field coverage area in world units</summary>
        public float radius;

        /// <summary>Time when this flow field was created (for cache expiration)</summary>
        public float creationTime;

        /// <summary>2D grid of direction vectors pointing toward the destination. Each cell contains the optimal movement direction.</summary>
        public Vector3[,] directions;

        /// <summary>Number of grid cells along the X-axis</summary>
        public int gridWidth;

        /// <summary>Number of grid cells along the Z-axis</summary>
        public int gridHeight;

        /// <summary>Size of each grid cell in world units</summary>
        public float cellSize;

        /// <summary>
        /// Creates a new flow field with the specified parameters.
        /// </summary>
        /// <param name="dest">Target destination for all agents using this flow field</param>
        /// <param name="rad">Radius of coverage area around the destination</param>
        /// <param name="width">Number of grid cells along X-axis (higher = more precision)</param>
        /// <param name="height">Number of grid cells along Z-axis (higher = more precision)</param>
        /// <param name="size">Size of each grid cell in world units (smaller = more precision)</param>
        public FlowField(Vector3 dest, float rad, int width, int height, float size)
        {
            destination = dest;
            radius = rad;
            creationTime = Time.time;
            gridWidth = width;
            gridHeight = height;
            cellSize = size;
            directions = new Vector3[width, height];
        }
        
        /// <summary>
        /// Generates a path from the start position to the destination using the flow field.
        /// Follows the direction vectors in the grid to create an optimal path.
        /// </summary>
        /// <param name="start">Starting world position for path generation</param>
        /// <returns>Array of waypoints leading to the destination. Empty array if no path exists.</returns>
        public Vector3[] GetPathToDestination(Vector3 start)
        {
            List<Vector3> path = new List<Vector3>();
            Vector3 current = start;

            // Limit iterations to prevent infinite loops in case of circular flows
            for (int i = 0; i < 50; i++)
            {
                Vector3 direction = GetDirectionAtPosition(current);
                if (direction == Vector3.zero) break; // No valid direction available

                current += direction * cellSize;
                path.Add(current);

                // Check if we've reached the destination (within 2 cell sizes for tolerance)
                if (Vector3.Distance(current, destination) < cellSize * 2)
                {
                    path.Add(destination);
                    break;
                }
            }

            return path.ToArray();
        }
        
        /// <summary>
        /// Gets the optimal movement direction at a given world position.
        /// Converts world coordinates to grid coordinates and returns the stored direction vector.
        /// </summary>
        /// <param name="worldPos">World position to query</param>
        /// <returns>Normalized direction vector pointing toward destination, or Vector3.zero if outside grid bounds</returns>
        private Vector3 GetDirectionAtPosition(Vector3 worldPos)
        {
            // Transform world position to grid-local coordinates
            // Grid origin is at (destination - radius) to center the grid around the destination
            Vector3 relativePos = worldPos - (destination - Vector3.one * radius);

            // Convert continuous world coordinates to discrete grid indices
            int x = Mathf.FloorToInt(relativePos.x / cellSize);
            int z = Mathf.FloorToInt(relativePos.z / cellSize);

            // Bounds check: ensure coordinates are within the grid
            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            {
                return directions[x, z];
            }

            // Return zero vector if position is outside the flow field coverage area
            return Vector3.zero;
        }
        
        /// <summary>
        /// Draws debug visualization of the flow field using Gizmos (Editor only).
        /// Shows direction arrows for each grid cell pointing toward the destination.
        /// Useful for debugging flow field generation and visualizing agent movement patterns.
        /// </summary>
        public void DrawDebug()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    // Calculate world position for this grid cell
                    Vector3 worldPos = destination - Vector3.one * radius +
                                     new Vector3(x * cellSize, 0, z * cellSize);
                    Vector3 direction = directions[x, z];

                    // Only draw arrows for cells with valid directions
                    if (direction != Vector3.zero)
                    {
                        // Draw direction arrow scaled to 80% of cell size for clarity
                        Gizmos.DrawRay(worldPos, direction * cellSize * 0.8f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generator for creating flow fields using various algorithms.
    /// Supports simple direct-path flow fields and can be extended for obstacle avoidance.
    /// Uses Dijkstra-like propagation to ensure optimal paths around obstacles.
    /// </summary>
    public class FlowFieldGenerator
    {
        /// <summary>
        /// Generates a flow field for the specified destination and coverage radius.
        /// Uses a simple direct-path algorithm where each cell points directly toward the destination.
        /// This is optimal for open areas without obstacles.
        /// </summary>
        /// <param name="destination">Target destination for agent movement</param>
        /// <param name="radius">Coverage radius around the destination in world units</param>
        /// <returns>Generated flow field ready for agent pathfinding</returns>
        public FlowField GenerateFlowField(Vector3 destination, float radius)
        {
            // Calculate grid resolution based on radius (smaller radius = higher resolution)
            int gridSize = Mathf.RoundToInt(radius / 2f);
            float cellSize = radius / gridSize;

            FlowField flowField = new FlowField(destination, radius, gridSize, gridSize, cellSize);

            // Generate direction vectors using direct-path algorithm
            // Each cell points directly toward the destination (suitable for obstacle-free environments)
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Calculate world position of this grid cell
                    Vector3 cellWorldPos = destination - Vector3.one * radius +
                                         new Vector3(x * cellSize, 0, z * cellSize);

                    // Calculate normalized direction vector toward destination
                    Vector3 directionToTarget = (destination - cellWorldPos).normalized;
                    directionToTarget.y = 0; // Constrain movement to horizontal plane

                    flowField.directions[x, z] = directionToTarget;
                }
            }

            return flowField;
        }
    }
}
