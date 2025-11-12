using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core.ECS;
using Laboratory.Core.Progression.Components;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Progression.Authoring
{
    /// <summary>
    /// MonoBehaviour for creating progression-enabled creatures
    /// </summary>
    public class CreatureProgressionAuthoring : MonoBehaviour
    {
        [Header("Initial Progression")]
        [Range(1, 100)] public int startingLevel = 1;
        public int bonusExperience = 0;
        public bool unlockAllActivities = false;

        [Header("Skill Preferences")]
        public bool autoAllocateSkills = true;
        public SkillFocus primaryFocus = SkillFocus.Balanced;

        public enum SkillFocus
        {
            Balanced,
            Racing,
            Combat,
            Puzzle,
            Strategy,
            Social
        }

        [ContextMenu("Add Progression Components")]
        public void AddProgressionComponents()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;

            // Find creature entity (would need proper entity linking)
            var entities = entityManager.GetAllEntities(Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<CreatureData>(entity))
                {
                    // Add progression components
                    entityManager.AddComponentData(entity, new CreatureProgressionComponent
                    {
                        Level = startingLevel,
                        Experience = 0,
                        ExperienceToNextLevel = CalculateExperienceForLevel(startingLevel + 1),
                        TotalExperience = bonusExperience,
                        AvailableSkillPoints = startingLevel,
                        HighestPerformanceScore = 0f
                    });

                    entityManager.AddComponentData(entity, new CreatureSkillsComponent
                    {
                        MasteryLevel = 0,
                        OverallMasteryBonus = 0f,
                        HasSpecialization = false
                    });

                    entityManager.AddComponentData(entity, new CreatureAchievementsComponent
                    {
                        TotalAchievements = 0,
                        AchievementScore = 0f
                    });

                    UnityEngine.Debug.Log($"✅ Added progression components to creature (Level {startingLevel})");
                    break;
                }
            }

            entities.Dispose();
        }

        private int CalculateExperienceForLevel(int level)
        {
            return (int)(100f * math.pow(1.5f, level));
        }

        private void OnDrawGizmos()
        {
            // Draw progression visualization
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

            // Draw level indicator
            for (int i = 0; i < startingLevel && i < 10; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(
                    transform.position + Vector3.up * (2.5f + i * 0.2f),
                    transform.position + Vector3.up * (2.5f + i * 0.2f) + Vector3.right * 0.5f
                );
            }
        }
    }
}