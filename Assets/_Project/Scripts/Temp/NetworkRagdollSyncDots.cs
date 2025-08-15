using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// DOTS-friendly ragdoll network sync for Unity.Entities.
/// Syncs hit triggers and optionally bone transforms for multiplayer.
/// </summary>
public class NetworkRagdollSyncDots : NetworkBehaviour
{
    [Header("DOTS References")]
    [Tooltip("Assign the root entity of the character manually.")]
    [SerializeField] private GameObject characterRoot; // Root GameObject with entity conversion

    [Tooltip("Assign delay before sending bone transforms.")]
    [SerializeField] private float transformSyncDelay = 0.1f;

    [Tooltip("Send bone transform snapshots after ragdoll trigger.")]
    [SerializeField] private bool syncBoneTransforms = true;

    private EntityManager entityManager;
    private Entity characterEntity;

    private void Awake()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        characterEntity = characterRoot != null ? characterRoot.GetComponent<GameObjectEntity>().Entity : Entity.Null;
    }

    /// <summary>
    /// Trigger a ragdoll hit on a specific bone/entity.
    /// </summary>
    public void NetworkedHit(Entity boneEntity, float3 hitForce)
    {
        if (!IsServer) return;

        // Apply local ECS ragdoll force
        ApplyHitToEntity(boneEntity, hitForce);

        // Notify clients
        TriggerRagdollClientRpc(boneEntity.Index, hitForce);
    }

    #region ECS Physics Application

    private void ApplyHitToEntity(Entity boneEntity, float3 force)
    {
        if (entityManager.HasComponent<Unity.Physics.PhysicsVelocity>(boneEntity))
        {
            var velocity = entityManager.GetComponentData<Unity.Physics.PhysicsVelocity>(boneEntity);
            velocity.Linear += force;
            entityManager.SetComponentData(boneEntity, velocity);
        }

        // Optional: add angular impulse or custom ragdoll state component
    }

    #endregion

    #region Netcode RPCs

    [ClientRpc]
    private void TriggerRagdollClientRpc(int boneEntityIndex, float3 hitForce)
    {
        if (IsOwner) return;

        Entity boneEntity = new Entity { Index = boneEntityIndex };
        ApplyHitToEntity(boneEntity, hitForce);

        if (syncBoneTransforms)
            StartCoroutine(DelayedBoneSync());
    }

    #endregion

    #region Optional Transform Sync

    private IEnumerator DelayedBoneSync()
    {
        yield return new WaitForSeconds(transformSyncDelay);

        using (var boneQuery = entityManager.CreateEntityQuery(typeof(LocalTransform)))
        {
            var transforms = boneQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var entities = boneQuery.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < transforms.Length; i++)
            {
                SendBoneTransformClientRpc(entities[i].Index, transforms[i].Position, transforms[i].Rotation);
            }

            transforms.Dispose();
            entities.Dispose();
        }
    }

    [ClientRpc]
    private void SendBoneTransformClientRpc(int boneEntityIndex, float3 pos, quaternion rot)
    {
        if (IsOwner) return;

        Entity boneEntity = new Entity { Index = boneEntityIndex };
        if (entityManager.HasComponent<LocalTransform>(boneEntity))
        {
            var transformData = entityManager.GetComponentData<LocalTransform>(boneEntity);
            transformData.Position = pos;
            transformData.Rotation = rot;
            entityManager.SetComponentData(boneEntity, transformData);
        }
    }

    #endregion
}
