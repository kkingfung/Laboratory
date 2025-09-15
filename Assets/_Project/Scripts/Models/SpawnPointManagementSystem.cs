using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Models
{
    /// <summary>
    /// System for managing spawn point placement and management
    /// </summary>
    public class SpawnPointManagementSystem : MonoBehaviour
    {
        [SerializeField] private SpawnPointService spawnPointService;
        [SerializeField] private float spawnRadius = 2f;
        [SerializeField] private LayerMask obstacleLayerMask = -1;
        
        private void Awake()
        {
            if (spawnPointService == null)
                spawnPointService = FindFirstObjectByType<SpawnPointService>();
        }
        
        public bool IsSpawnPointValid(Vector3 position)
        {
            return !Physics.CheckSphere(position, spawnRadius, obstacleLayerMask);
        }
        
        public Vector3 GetValidSpawnPosition(Vector3 desiredPosition, float searchRadius = 10f)
        {
            if (IsSpawnPointValid(desiredPosition))
                return desiredPosition;
                
            // Search for a valid position in a spiral pattern
            for (float radius = 1f; radius <= searchRadius; radius += 1f)
            {
                for (int angle = 0; angle < 360; angle += 30)
                {
                    Vector3 testPosition = desiredPosition + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                    if (IsSpawnPointValid(testPosition))
                        return testPosition;
                }
            }
            
            return desiredPosition; // Return original if no valid position found
        }
    }
}
