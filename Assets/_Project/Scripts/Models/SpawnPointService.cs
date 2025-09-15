using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Models
{
    /// <summary>
    /// Service for managing spawn points in the game
    /// </summary>
    public class SpawnPointService : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool useRandomSpawning = true;
        
        private int lastUsedSpawnIndex = 0;
        
        public Vector3 GetSpawnPosition(int? preferredIndex = null)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return Vector3.zero;
                
            int index = preferredIndex ?? GetNextSpawnIndex();
            index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
            
            return spawnPoints[index]?.position ?? Vector3.zero;
        }
        
        public Quaternion GetSpawnRotation(int? preferredIndex = null)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return Quaternion.identity;
                
            int index = preferredIndex ?? lastUsedSpawnIndex;
            index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
            
            return spawnPoints[index]?.rotation ?? Quaternion.identity;
        }
        
        private int GetNextSpawnIndex()
        {
            if (useRandomSpawning)
            {
                return UnityEngine.Random.Range(0, spawnPoints.Length);
            }
            else
            {
                lastUsedSpawnIndex = (lastUsedSpawnIndex + 1) % spawnPoints.Length;
                return lastUsedSpawnIndex;
            }
        }
    }
}
