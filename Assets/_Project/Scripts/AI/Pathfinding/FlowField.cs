using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.AI.Pathfinding
{
    /// <summary>
    /// Flow field for efficient pathfinding with many agents to the same destination
    /// </summary>
    public class FlowField
    {
        public Vector3 destination;
        public float radius;
        public float creationTime;
        public Vector3[,] directions;
        public int gridWidth;
        public int gridHeight;
        public float cellSize;
        
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
        
        public Vector3[] GetPathToDestination(Vector3 start)
        {
            List<Vector3> path = new List<Vector3>();
            Vector3 current = start;
            
            for (int i = 0; i < 50; i++) // Max 50 steps to prevent infinite loops
            {
                Vector3 direction = GetDirectionAtPosition(current);
                if (direction == Vector3.zero) break;
                
                current += direction * cellSize;
                path.Add(current);
                
                if (Vector3.Distance(current, destination) < cellSize * 2)
                {
                    path.Add(destination);
                    break;
                }
            }
            
            return path.ToArray();
        }
        
        private Vector3 GetDirectionAtPosition(Vector3 worldPos)
        {
            // Convert world position to grid coordinates
            Vector3 relativePos = worldPos - (destination - Vector3.one * radius);
            int x = Mathf.FloorToInt(relativePos.x / cellSize);
            int z = Mathf.FloorToInt(relativePos.z / cellSize);
            
            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            {
                return directions[x, z];
            }
            
            return Vector3.zero;
        }
        
        public void DrawDebug()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 worldPos = destination - Vector3.one * radius + 
                                     new Vector3(x * cellSize, 0, z * cellSize);
                    Vector3 direction = directions[x, z];
                    
                    if (direction != Vector3.zero)
                    {
                        Gizmos.DrawRay(worldPos, direction * cellSize * 0.8f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generator for creating flow fields
    /// </summary>
    public class FlowFieldGenerator
    {
        public FlowField GenerateFlowField(Vector3 destination, float radius)
        {
            int gridSize = Mathf.RoundToInt(radius / 2f);
            float cellSize = radius / gridSize;
            
            FlowField flowField = new FlowField(destination, radius, gridSize, gridSize, cellSize);
            
            // Simple flow field generation - points towards destination
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 cellWorldPos = destination - Vector3.one * radius + 
                                         new Vector3(x * cellSize, 0, z * cellSize);
                    Vector3 directionToTarget = (destination - cellWorldPos).normalized;
                    directionToTarget.y = 0; // Keep it on the horizontal plane
                    
                    flowField.directions[x, z] = directionToTarget;
                }
            }
            
            return flowField;
        }
    }
}
