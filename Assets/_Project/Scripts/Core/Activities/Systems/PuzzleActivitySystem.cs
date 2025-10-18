using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Activities.Systems
{
    /// <summary>
    /// Puzzle Academy System - Intelligence-based problem solving
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class PuzzleActivitySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (participant, genetics) in
                SystemAPI.Query<RefRW<ActivityParticipantComponent>, RefRO<GeneticDataComponent>>())
            {
                if (participant.ValueRO.CurrentActivity != ActivityType.Puzzle ||
                    participant.ValueRO.Status != ActivityStatus.Active)
                    continue;

                // Puzzle solving is primarily intelligence-based
                float intelligenceFactor = genetics.ValueRO.Intelligence;
                float curiosityBonus = genetics.ValueRO.Curiosity * 0.5f;
                float learningSpeed = intelligenceFactor + curiosityBonus;

                // Puzzles get easier over time as the creature learns
                float learningProgress = participant.ValueRO.TimeInActivity / 60f; // 1 minute to learn
                float difficultyReduction = math.saturate(learningProgress) * 0.3f;

                float puzzleScore = learningSpeed * (1f + difficultyReduction);
                participant.ValueRW.PerformanceScore = math.clamp(puzzleScore, 0.1f, 2.0f);
            }
        }
    }
}