using Unity.Entities;
using Laboratory.Core.Progression.Components;

namespace Laboratory.Core.Progression.Systems
{
    /// <summary>
    /// Player-level progression system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerProgressionSystem : SystemBase
    {
        private EntityQuery playerQuery;
        private EntityQuery allCreaturesQuery;

        protected override void OnCreate()
        {
            playerQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerProgressionComponent>());
            allCreaturesQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureProgressionComponent>());
        }

        protected override void OnUpdate()
        {
            // Update player progression based on creature achievements
            if (playerQuery.IsEmpty) return;

            var playerProgression = SystemAPI.GetSingletonRW<PlayerProgressionComponent>();

            // Calculate town rating from all creatures
            int totalCreatureLevel = 0;
            int creatureCount = 0;
            float totalAchievementScore = 0f;

            foreach (var (progression, achievements) in
                SystemAPI.Query<RefRO<CreatureProgressionComponent>, RefRO<CreatureAchievementsComponent>>())
            {
                totalCreatureLevel += progression.ValueRO.Level;
                totalAchievementScore += achievements.ValueRO.AchievementScore;
                creatureCount++;
            }

            if (creatureCount > 0)
            {
                int averageCreatureLevel = totalCreatureLevel / creatureCount;
                playerProgression.ValueRW.TownRating = (int)(averageCreatureLevel * 10 + totalAchievementScore / 100);

                // Player level based on town rating
                int newPlayerLevel = playerProgression.ValueRO.TownRating / 1000 + 1;
                if (newPlayerLevel > playerProgression.ValueRO.PlayerLevel)
                {
                    playerProgression.ValueRW.PlayerLevel = newPlayerLevel;
                    playerProgression.ValueRW.ResearchPoints += newPlayerLevel * 10;
                }
            }

            // Update maximum creatures based on player level
            playerProgression.ValueRW.MaxCreatures = 10 + playerProgression.ValueRO.PlayerLevel * 5;
        }
    }
}