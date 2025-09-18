using UnityEngine;

namespace Laboratory.Subsystems.Spawning
{
    /// <summary>
    /// Defines a spawn point location and properties
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Spawn Point Configuration")]
        [SerializeField] private string spawnTag = "default";
        [SerializeField] private bool isActive = true;
        [SerializeField] private float spawnRadius = 1f;
        
        [Header("Spawn Constraints")]
        [SerializeField] private bool checkForObstacles = true;
        [SerializeField] private LayerMask obstacleLayerMask = -1;
        
        [Header("Visual Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.green;

        public string SpawnTag 
        { 
            get => spawnTag; 
            set => spawnTag = value; 
        }
        
        public bool IsActive 
        { 
            get => isActive; 
            set => isActive = value; 
        }
        
        public float SpawnRadius 
        { 
            get => spawnRadius; 
            set => spawnRadius = Mathf.Max(0.1f, value); 
        }

        /// <summary>
        /// Check if this spawn point is available for spawning
        /// </summary>
        public bool IsAvailable()
        {
            if (!isActive) return false;
            
            if (checkForObstacles)
            {
                // Check for colliders in spawn area
                Collider[] colliders = Physics.OverlapSphere(transform.position, spawnRadius, obstacleLayerMask);
                return colliders.Length == 0;
            }
            
            return true;
        }

        /// <summary>
        /// Get a random position within the spawn radius
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Raycast down to find ground level
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                spawnPosition.y = hit.point.y;
            }
            
            return spawnPosition;
        }

        /// <summary>
        /// Get spawn rotation with optional random variation
        /// </summary>
        public Quaternion GetSpawnRotation(bool randomizeY = false)
        {
            if (randomizeY)
            {
                float randomYRotation = Random.Range(0f, 360f);
                return Quaternion.Euler(transform.eulerAngles.x, randomYRotation, transform.eulerAngles.z);
            }
            
            return transform.rotation;
        }

        private void OnDrawGizmos()
        {
            if (showGizmos)
            {
                Gizmos.color = isActive ? gizmoColor : Color.red;
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
                
                // Draw spawn direction
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (showGizmos)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position, 0.2f);
                
                // Draw spawn area
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
                Gizmos.DrawSphere(transform.position, spawnRadius);
            }
        }
    }
}
