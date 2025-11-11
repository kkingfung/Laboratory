using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Laboratory.Core.ECS;
using Laboratory.Chimera.ECS;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// High-performance ECS systems for creature simulation.
    /// Optimized for 1000+ creatures with burst compilation and job system.
    /// </summary>


    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CreatureAgingSystem : ISystem
    {
        private EntityQuery creatureQuery;


        public void OnCreate(ref SystemState state)
        {
            creatureQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureData, CreatureSimulationTag>()
                .Build();
        }


        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var agingJob = new CreatureAgingJob
            {
                deltaTime = deltaTime
            };

            state.Dependency = agingJob.ScheduleParallel(creatureQuery, state.Dependency);
        }
    }


    [BurstCompile]
    public partial struct CreatureAgingJob : IJobEntity
    {
        public float deltaTime;


        public void Execute(ref CreatureData creatureData)
        {
            if (!creatureData.isAlive) return;

            // Age creatures by seconds (convert to days in a real system)
            creatureData.age += (int)(deltaTime * 100); // Accelerated aging for demo

            // Simple death condition - in real system would be more sophisticated
            if (creatureData.age > 10000) // Demo death age
            {
                creatureData.isAlive = false;
            }
        }
    }


    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CreatureAgingSystem))]
    public partial struct CreatureAISystem : ISystem
    {
        private EntityQuery aiQuery;


        public void OnCreate(ref SystemState state)
        {
            aiQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureAIComponent, CreatureData, LocalTransform>()
                .WithAll<CreatureSimulationTag>()
                .Build();
        }


        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            var aiJob = new CreatureAIJob
            {
                time = time,
                deltaTime = deltaTime,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)(time * 1000))
            };

            state.Dependency = aiJob.ScheduleParallel(aiQuery, state.Dependency);
        }
    }


    [BurstCompile]
    public partial struct CreatureAIJob : IJobEntity
    {
        public float time;
        public float deltaTime;
        public Unity.Mathematics.Random random;


        public void Execute(ref CreatureAIComponent ai, in CreatureData data, ref LocalTransform transform)
        {
            if (!data.isAlive) return;

            // Update AI behavior based on time
            if (time > ai.StateTimer)
            {
                UpdateBehaviorState(ref ai, in data);
                ai.StateTimer = time + random.NextFloat(1f, 3f);
            }

            // Execute current behavior
            ExecuteBehavior(ref ai, in data, ref transform);
        }


        private void UpdateBehaviorState(ref CreatureAIComponent ai, in CreatureData data)
        {
            // Simple state machine - in real system would be more sophisticated
            switch (ai.CurrentState)
            {
                case AIState.Idle:
                    if (random.NextFloat() < 0.3f)
                        ai.CurrentState = AIState.Patrol;
                    break;

                case AIState.Patrol:
                    if (random.NextFloat() < 0.2f)
                        ai.CurrentState = AIState.Idle;
                    break;
            }
        }


        private void ExecuteBehavior(ref CreatureAIComponent ai, in CreatureData data, ref LocalTransform transform)
        {
            switch (ai.CurrentState)
            {
                case AIState.Patrol:
                    // Simple random movement
                    var direction = new float3(
                        random.NextFloat(-1f, 1f),
                        0f,
                        random.NextFloat(-1f, 1f)
                    );
                    direction = math.normalize(direction);

                    var movement = direction * 5f * deltaTime;
                    transform.Position += movement;
                    break;

                case AIState.Idle:
                    // Idle behavior - maybe rotate slowly
                    var rotation = quaternion.RotateY(deltaTime * 0.1f);
                    transform.Rotation = math.mul(transform.Rotation, rotation);
                    break;
            }
        }
    }


    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CreatureGeneticsSystem : ISystem
    {
        private EntityQuery geneticsQuery;


        public void OnCreate(ref SystemState state)
        {
            geneticsQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureData, CreatureStats>()
                .WithAll<CreatureGeneticTrait>()
                .WithAll<CreatureSimulationTag>()
                .Build();
        }


        public void OnUpdate(ref SystemState state)
        {
            var geneticsJob = new CreatureGeneticsJob();
            state.Dependency = geneticsJob.ScheduleParallel(geneticsQuery, state.Dependency);
        }
    }


    [BurstCompile]
    public partial struct CreatureGeneticsJob : IJobEntity
    {

        public void Execute(in CreatureData data, ref CreatureStats stats,
                          in DynamicBuffer<CreatureGeneticTrait> traits)
        {
            if (!data.isAlive) return;

            // Apply genetic modifiers to stats over time
            // This is a simplified example - real system would be more complex

            float strengthMod = GetTraitValue(traits, "Strength".GetHashCode());
            float vitalityMod = GetTraitValue(traits, "Vitality".GetHashCode());

            // Gradually apply genetic influence to stats
            float influence = 0.01f; // Small influence per frame
            stats.attack = math.lerp(stats.attack, stats.attack * (1f + strengthMod * 0.5f), influence);
            stats.health = math.lerp(stats.health, stats.maxHealth * (1f + vitalityMod * 0.3f), influence);
        }


        private float GetTraitValue(in DynamicBuffer<CreatureGeneticTrait> traits, int traitNameHash)
        {
            for (int i = 0; i < traits.Length; i++)
            {
                if (traits[i].traitName == traitNameHash)
                    return traits[i].value;
            }
            return 0.5f; // Default neutral value
        }
    }


    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct CreatureVisualizationSystem : ISystem
    {
        private EntityQuery visualQuery;

        public void OnCreate(ref SystemState state)
        {
            visualQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureVisualData, CreatureData, LocalTransform>()
                .WithAll<CreatureSimulationTag>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            // This system would handle visual updates, LOD, culling, etc.
            // For now, just a placeholder that updates visual scale based on age

            foreach (var (visualData, creatureData, transform) in
                     SystemAPI.Query<RefRW<CreatureVisualData>, RefRO<CreatureData>, RefRW<LocalTransform>>()
                     .WithAll<CreatureSimulationTag>())
            {
                if (!creatureData.ValueRO.isAlive) continue;

                // Scale creatures slightly based on age (growth simulation)
                float ageScale = math.lerp(0.8f, 1.2f, math.min(creatureData.ValueRO.age / 5000f, 1f));
                float finalScale = visualData.ValueRO.baseScale * ageScale;

                transform.ValueRW.Scale = finalScale;
            }
        }
    }

    /// <summary>
    /// System for cleaning up dead creatures
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CreatureAgingSystem))]
    public partial struct CreatureCleanupSystem : ISystem
    {
        private EntityQuery deadCreatureQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        public void OnCreate(ref SystemState state)
        {
            deadCreatureQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureData, CreatureSimulationTag>()
                .Build();

            ecbSystem = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            var cleanupJob = new CreatureCleanupJob
            {
                ecb = ecb
            };

            state.Dependency = cleanupJob.ScheduleParallel(deadCreatureQuery, state.Dependency);
            ecbSystem.AddJobHandleForProducer(state.Dependency);
        }
    }


    [BurstCompile]
    public partial struct CreatureCleanupJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;


        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in CreatureData data)
        {
            if (!data.isAlive)
            {
                // Remove dead creatures from simulation
                ecb.DestroyEntity(chunkIndex, entity);
            }
        }
    }

    /// <summary>
    /// Debug system for performance monitoring
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CreatureDebugSystem : SystemBase
    {
        private EntityQuery allCreaturesQuery;

        protected override void OnCreate()
        {
            allCreaturesQuery = GetEntityQuery(
                ComponentType.ReadOnly<CreatureData>(),
                ComponentType.ReadOnly<CreatureSimulationTag>()
            );
        }

        protected override void OnUpdate()
        {
            // Update debug info every second
            if (SystemAPI.Time.ElapsedTime % 1.0 < SystemAPI.Time.DeltaTime)
            {
                int totalCreatures = allCreaturesQuery.CalculateEntityCount();
                int aliveCreatures = 0;

                foreach (var data in SystemAPI.Query<RefRO<CreatureData>>().WithAll<CreatureSimulationTag>())
                {
                    if (data.ValueRO.isAlive) aliveCreatures++;
                }

                // Set debug data for Unity's debug system
                UnityEngine.Debug.Log($"ECS Creatures - Total: {totalCreatures}, Alive: {aliveCreatures}, Dead: {totalCreatures - aliveCreatures}");
            }
        }
    }
}