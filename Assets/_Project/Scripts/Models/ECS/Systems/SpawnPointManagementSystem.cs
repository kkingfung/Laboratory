using Unity.Entities;
using UnityEngine;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// ECS System for managing spawn point entities and operations
    /// </summary>
    public class ECSSpawnPointManagementSystem : MonoBehaviour
    {
        private EntityManager entityManager;
        
        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        
        public Entity CreateSpawnPointEntity(Vector3 position, Quaternion rotation)
        {
            var entity = entityManager.CreateEntity();
            return entity;
        }
        
        public void RemoveSpawnPoint(Entity spawnPointEntity)
        {
            if (entityManager.Exists(spawnPointEntity))
            {
                entityManager.DestroyEntity(spawnPointEntity);
            }
        }
    }
}
