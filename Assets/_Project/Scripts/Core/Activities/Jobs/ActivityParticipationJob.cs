using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Activities.Jobs
{

    public partial struct ActivityParticipationJob : IJobEntity
    {
        public float DeltaTime;
        public float CurrentTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            ref ActivityParticipantComponent participant,
            in GeneticDataComponent genetics,
            in CreatureIdentityComponent identity,
            in ActivityPerformanceComponent performance)
        {
            if (participant.Status == ActivityStatus.NotParticipating)
                return;

            participant.TimeInActivity += DeltaTime;

            switch (participant.Status)
            {
                case ActivityStatus.Queued:
                    ProcessQueuedState(ref participant);
                    break;

                case ActivityStatus.Warming_Up:
                    ProcessWarmingUpState(ref participant);
                    break;

                case ActivityStatus.Active:
                    ProcessActiveState(ref participant, genetics);
                    break;

                case ActivityStatus.Completed:
                    ProcessCompletedState(ref participant);
                    break;
            }
        }


        private void ProcessQueuedState(ref ActivityParticipantComponent participant)
        {
            if (participant.TimeInActivity > 2f) // 2 second queue time
            {
                participant.Status = ActivityStatus.Warming_Up;
                participant.TimeInActivity = 0f;
            }
        }


        private void ProcessWarmingUpState(ref ActivityParticipantComponent participant)
        {
            if (participant.TimeInActivity > 3f) // 3 second warm-up
            {
                participant.Status = ActivityStatus.Active;
                participant.TimeInActivity = 0f;
                participant.ActivityProgress = 0f;
            }
        }


        private void ProcessActiveState(ref ActivityParticipantComponent participant, GeneticDataComponent genetics)
        {
            float performanceMultiplier = CalculatePerformanceMultiplier(participant.CurrentActivity, genetics);
            float progressRate = performanceMultiplier * DeltaTime;

            participant.ActivityProgress += progressRate;
            participant.PerformanceScore = performanceMultiplier;

            if (participant.ActivityProgress >= 1f)
            {
                participant.Status = ActivityStatus.Completed;
                participant.ExperienceGained = CalculateExperienceGained(performanceMultiplier, participant.CurrentActivity);
                participant.HasRewards = true;
            }
        }


        private void ProcessCompletedState(ref ActivityParticipantComponent participant)
        {
            if (!participant.HasRewards)
            {
                participant.Status = ActivityStatus.Rewarded;
            }
        }


        private float CalculatePerformanceMultiplier(ActivityType activity, GeneticDataComponent genetics)
        {
            return activity switch
            {
                ActivityType.Racing => (genetics.Speed * 0.4f + genetics.Stamina * 0.3f + genetics.Agility * 0.3f),
                ActivityType.Combat => (genetics.Aggression * 0.5f + genetics.Size * 0.3f + genetics.Dominance * 0.2f),
                ActivityType.Puzzle => (genetics.Intelligence * 0.7f + genetics.Curiosity * 0.3f),
                ActivityType.Strategy => (genetics.Intelligence * 0.6f + genetics.Caution * 0.4f),
                ActivityType.Music => (genetics.Intelligence * 0.4f + genetics.Sociability * 0.6f),
                ActivityType.Adventure => (genetics.Curiosity * 0.4f + genetics.Adaptability * 0.3f + genetics.Stamina * 0.3f),
                ActivityType.Platforming => (genetics.Agility * 0.6f + genetics.Intelligence * 0.4f),
                ActivityType.Crafting => (genetics.Intelligence * 0.5f + genetics.Adaptability * 0.5f),
                _ => 0.5f
            };
        }


        private int CalculateExperienceGained(float performance, ActivityType activity)
        {
            float baseExp = activity switch
            {
                ActivityType.Racing => 10f,
                ActivityType.Combat => 15f,
                ActivityType.Puzzle => 12f,
                ActivityType.Strategy => 18f,
                ActivityType.Music => 8f,
                ActivityType.Adventure => 20f,
                ActivityType.Platforming => 10f,
                ActivityType.Crafting => 14f,
                _ => 10f
            };

            return (int)(baseExp * performance * UnityEngine.Random.Range(0.8f, 1.2f));
        }
    }
}