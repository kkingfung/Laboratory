using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Laboratory.Network.Ragdoll
{
    /// <summary>
    /// DOTS-compatible network synchronization system for ragdoll physics.
    /// Handles networked hit events and optional bone transform synchronization using Unity.Entities.
    /// </summary>
    public class NetworkRagdollSyncDots : NetworkBehaviour
    {
        #region Fields
        
        [Header("DOTS Integration")]
        [Tooltip("Root GameObject containing the character entity for DOTS integration.")]
        [SerializeField] private GameObject characterRoot;
        
        [Header("Network Settings")]
        [Tooltip("Delay before synchronizing bone transforms across the network.")]
        [SerializeField] private float transformSyncDelay = 0.1f;
        
        [Tooltip("Enable bone transform snapshot transmission after ragdoll activation.")]
        [SerializeField] private bool syncBoneTransforms = true;
        
        /// <summary>
        /// Cached reference to the ECS EntityManager.
        /// </summary>
        private EntityManager entityManager;
        
        /// <summary>
        /// Entity representation of the character root GameObject.
        /// </summary>
        private Entity characterEntity;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes ECS integration and caches entity references.
        /// </summary>
        private void Awake()
        {
            InitializeECSIntegration();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Triggers a networked ragdoll hit on the specified bone entity.
        /// Only processes on server for authoritative physics simulation.
        /// </summary>
        /// <param name="boneEntity">ECS entity representing the bone to hit</param>
        /// <param name="hitForce">Force vector to apply to the bone</param>
        public void NetworkedHit(Entity boneEntity, float3 hitForce)
        {
            if (!IsServer) return;
            
            ProcessLocalECSHit(boneEntity, hitForce);
            NotifyClientsOfHit(boneEntity, hitForce);
        }
        
        #endregion
        
        #region Private Methods - Initialization
        
        /// <summary>
        /// Sets up ECS integration by obtaining EntityManager and character entity references.
        /// </summary>
        private void InitializeECSIntegration()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            entityManager = world?.EntityManager ?? default;
            
            characterEntity = GetCharacterEntity();
        }
        
        /// <summary>
        /// Retrieves the entity representation of the character root GameObject.
        /// </summary>
        /// <returns>Entity associated with the character root, or Entity.Null if not found</returns>
        private Entity GetCharacterEntity()
        {
            if (characterRoot?.GetComponent<GameObjectEntity>() != null)
            {
                return characterRoot.GetComponent<GameObjectEntity>().Entity;
            }
            return Entity.Null;
        }
        
        #endregion
        
        #region Private Methods - ECS Physics
        
        /// <summary>
        /// Applies hit force directly to an ECS entity using the physics system.
        /// </summary>
        /// <param name="boneEntity">Target bone entity</param>
        /// <param name="hitForce">Force vector to apply</param>
        private void ProcessLocalECSHit(Entity boneEntity, float3 hitForce)
        {
            ApplyForceToEntity(boneEntity, hitForce);
        }
        
        /// <summary>
        /// Applies force to an entity by modifying its PhysicsVelocity component.
        /// </summary>
        /// <param name="boneEntity">Target entity with physics components</param>
        /// <param name="force">Force vector to add to current velocity</param>
        private void ApplyForceToEntity(Entity boneEntity, float3 force)
        {
            if (!entityManager.HasComponent<Unity.Physics.PhysicsVelocity>(boneEntity)) return;
            
            var velocity = entityManager.GetComponentData<Unity.Physics.PhysicsVelocity>(boneEntity);
            velocity.Linear += force;
            entityManager.SetComponentData(boneEntity, velocity);
        }
        
        /// <summary>
        /// Notifies all clients about the hit event.
        /// </summary>
        /// <param name="boneEntity">Entity that was hit</param>
        /// <param name="hitForce">Force that was applied</param>
        private void NotifyClientsOfHit(Entity boneEntity, float3 hitForce)
        {
            TriggerRagdollClientRpc(boneEntity.Index, hitForce);
        }
        
        #endregion
        
        #region Network RPCs
        
        /// <summary>
        /// Client RPC for replicating ragdoll hit effects on all clients.
        /// </summary>
        /// <param name="boneEntityIndex">Index of the bone entity that was hit</param>
        /// <param name="hitForce">Force vector applied to the bone</param>
        [ClientRpc]
        private void TriggerRagdollClientRpc(int boneEntityIndex, float3 hitForce)
        {
            if (IsOwner) return; // Skip owner to avoid duplicate processing
            
            Entity boneEntity = new Entity { Index = boneEntityIndex };
            ProcessLocalECSHit(boneEntity, hitForce);
            
            if (syncBoneTransforms)
            {
                StartCoroutine(DelayedBoneSyncCoroutine());
            }
        }
        
        /// <summary>
        /// Client RPC for synchronizing individual bone transform data.
        /// </summary>
        /// <param name="boneEntityIndex">Index of the bone entity to update</param>
        /// <param name="position">New world position</param>
        /// <param name="rotation">New world rotation</param>
        [ClientRpc]
        private void SendBoneTransformClientRpc(int boneEntityIndex, float3 position, quaternion rotation)
        {
            if (IsOwner) return; // Skip owner to prevent local override
            
            ApplyEntityTransform(boneEntityIndex, position, rotation);
        }
        
        #endregion
        
        #region Private Methods - Transform Synchronization
        
        /// <summary>
        /// Coroutine that delays bone transform synchronization to allow physics settling.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator DelayedBoneSyncCoroutine()
        {
            yield return new WaitForSeconds(transformSyncDelay);
            SynchronizeAllEntityTransforms();
        }
        
        /// <summary>
        /// Queries all entities with LocalTransform and sends their data to clients.
        /// </summary>
        private void SynchronizeAllEntityTransforms()
        {
            using (var boneQuery = entityManager.CreateEntityQuery(typeof(LocalTransform)))
            {
                var transforms = boneQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
                var entities = boneQuery.ToEntityArray(Allocator.TempJob);
                
                TransmitTransformData(entities, transforms);
                
                transforms.Dispose();
                entities.Dispose();
            }
        }
        
        /// <summary>
        /// Transmits transform data for all specified entities to clients.
        /// </summary>
        /// <param name="entities">Array of entities to synchronize</param>
        /// <param name="transforms">Corresponding transform data</param>
        private void TransmitTransformData(NativeArray<Entity> entities, NativeArray<LocalTransform> transforms)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                SendBoneTransformClientRpc(
                    entities[i].Index,
                    transforms[i].Position,
                    transforms[i].Rotation
                );
            }
        }
        
        /// <summary>
        /// Applies received transform data to the specified entity.
        /// </summary>
        /// <param name="boneEntityIndex">Index of the target entity</param>
        /// <param name="position">New world position</param>
        /// <param name="rotation">New world rotation</param>
        private void ApplyEntityTransform(int boneEntityIndex, float3 position, quaternion rotation)
        {
            Entity boneEntity = new Entity { Index = boneEntityIndex };
            
            if (!entityManager.HasComponent<LocalTransform>(boneEntity)) return;
            
            var transformData = entityManager.GetComponentData<LocalTransform>(boneEntity);
            transformData.Position = position;
            transformData.Rotation = rotation;
            entityManager.SetComponentData(boneEntity, transformData);
        }
        
        #endregion
    }
}
