using Unity.Entities;
using UnityEngine;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Activities.Authoring
{
    /// <summary>
    /// MonoBehaviour authoring component for Activity Centers
    /// Drop this on GameObjects to create activity centers in scenes
    /// </summary>
    public class ActivityCenterAuthoring : MonoBehaviour
    {
        [Header("Activity Configuration")]
        public ActivityType activityType = ActivityType.Racing;
        [Range(1, 50)] public int maxParticipants = 10;
        [Range(30f, 600f)] public float activityDuration = 120f;
        [Range(0.1f, 3.0f)] public float difficultyLevel = 1.0f;
        [Range(0.5f, 2.0f)] public float qualityRating = 1.0f;

        [Header("Center Properties")]
        public bool startActive = true;
        public Transform[] participantSpawnPoints;

        [ContextMenu("Create Activity Center Entity")]
        public void CreateActivityCenterEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = activityType,
                MaxParticipants = maxParticipants,
                CurrentParticipants = 0,
                ActivityDuration = activityDuration,
                DifficultyLevel = difficultyLevel,
                IsActive = startActive,
                QualityRating = qualityRating,
                OwnerCreature = Entity.Null
            });

            // Link to GameObject
            entityManager.AddComponentData(entity, new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            });

            Debug.Log($"✅ Created {activityType} Activity Center with {maxParticipants} max participants");
        }

        private void OnDrawGizmos()
        {
            // Draw activity center visualization
            var color = activityType switch
            {
                ActivityType.Racing => Color.yellow,
                ActivityType.Combat => Color.red,
                ActivityType.Puzzle => Color.blue,
                ActivityType.Strategy => Color.magenta,
                ActivityType.Music => Color.cyan,
                ActivityType.Adventure => Color.green,
                ActivityType.Platforming => Color.orange,
                ActivityType.Crafting => Color.white,
                _ => Color.gray
            };

            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 5f);

            // Draw participant spawn points
            if (participantSpawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in participantSpawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }
        }
    }
}