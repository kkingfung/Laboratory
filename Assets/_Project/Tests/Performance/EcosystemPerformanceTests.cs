using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using System.Diagnostics;
using UnityEngine;
using Laboratory.Chimera.Social;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.ECS;
using ChimeraIdentity = Laboratory.Chimera.Core.CreatureIdentityComponent;

namespace Laboratory.Tests.Performance
{
    /// <summary>
    /// Ecosystem simulation performance tests for Project Chimera
    ///
    /// Performance Targets (60 FPS = 16.67ms frame budget):
    /// - 1000 creatures with full simulation: <12ms per frame
    /// - Social bonding system (1000 creatures): <2ms per frame
    /// - Genetics processing (1000 creatures): <1ms per frame
    /// - AI behavior decisions (1000 creatures): <3ms per frame
    /// - Ecosystem simulation overhead: <1ms per frame
    ///
    /// Total ecosystem budget: 12ms / 16.67ms = 72% of frame
    /// </summary>
    [TestFixture]
    public class EcosystemPerformanceTests
    {
        private World _testWorld;
        private EntityManager _entityManager;
        private const int LARGE_POPULATION = 1000;
        private const int MEDIUM_POPULATION = 500;
        private const int SMALL_POPULATION = 100;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("EcosystemPerformanceTestWorld");
            _entityManager = _testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testWorld != null && _testWorld.IsCreated)
            {
                _testWorld.Dispose();
            }
        }

        #region Creature Spawning Performance

        [Test]
        public void SpawnCreatures_1000Creatures_CompletesWithin50ms()
        {
            // Arrange
            var archetype = _entityManager.CreateArchetype(
                typeof(ChimeraGeneticDataComponent),
                typeof(CreatureBondData),
                typeof(ChimeraIdentity),
                typeof(LocalTransform)
            );

            var random = new Unity.Mathematics.Random(12345);
            var stopwatch = Stopwatch.StartNew();

            // Act - Spawn 1000 creatures
            var entities = new NativeArray<Entity>(LARGE_POPULATION, Allocator.Temp);
            _entityManager.CreateEntity(archetype, entities);

            // Initialize data for all creatures
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                _entityManager.SetComponentData(entities[i], new ChimeraGeneticDataComponent
                {
                    Aggression = random.NextFloat(0f, 1f),
                    Sociability = random.NextFloat(0f, 1f),
                    Curiosity = random.NextFloat(0f, 1f),
                    Intelligence = random.NextFloat(0f, 1f),
                    Speed = random.NextFloat(0f, 1f),
                    Stamina = random.NextFloat(0f, 1f),
                    Size = random.NextFloat(0f, 1f),
                    GeneticHash = (uint)random.NextInt()
                });

                _entityManager.SetComponentData(entities[i], new CreatureBondData
                {
                    bondStrength = 0f,
                    loyaltyLevel = 0.5f,
                    playerId = 0,
                    creatureId = i,
                    hasHadFirstInteraction = false
                });

                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureID = new FixedString64Bytes($"creature_{i}"),
                    SpeciesID = random.NextInt(1, 10),
                    CreatureName = new FixedString32Bytes($"Creature_{i}")
                });

                _entityManager.SetComponentData(entities[i], LocalTransform.FromPosition(
                    new float3(random.NextFloat(-100f, 100f), 0f, random.NextFloat(-100f, 100f))
                ));
            }

            stopwatch.Stop();
            entities.Dispose();

            // Assert - Should complete in under 50ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"SpawnCreatures_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 50.0,
                $"Performance regression: Spawning 1000 creatures took {elapsedMs:F2}ms, expected <50ms");
        }

        #endregion

        #region Social System Performance

        [Test]
        public void SocialBonding_1000Creatures_CompletesWithin5ms()
        {
            // Arrange - Create 1000 creatures with bonding data
            SpawnCreaturesWithBonding(LARGE_POPULATION, out var entities);
            var random = new Unity.Mathematics.Random(54321);

            // Create job to update bond strengths
            var bondData = new NativeArray<CreatureBondData>(LARGE_POPULATION, Allocator.TempJob);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                bondData[i] = _entityManager.GetComponentData<CreatureBondData>(entities[i]);
            }

            var job = new UpdateBondStrengthJob
            {
                bondData = bondData,
                deltaTime = 0.016f, // 60 FPS frame
                random = random
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup
            job.Run(LARGE_POPULATION);

            // Actual measurement
            stopwatch.Restart();
            job.Run(LARGE_POPULATION);
            stopwatch.Stop();

            // Cleanup
            bondData.Dispose();
            entities.Dispose();

            // Assert - Should complete in under 5ms (2ms per frame budget)
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"SocialBonding_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 5.0,
                $"Performance regression: Social bonding took {elapsedMs:F2}ms, expected <5ms");
        }

        [Test]
        public void AgeSensitivity_1000Creatures_CompletesWithin3ms()
        {
            // Arrange - Create creatures with age sensitivity
            SpawnCreaturesWithAgeSensitivity(LARGE_POPULATION, out var entities);

            var bondData = new NativeArray<CreatureBondData>(LARGE_POPULATION, Allocator.TempJob);
            var ageData = new NativeArray<AgeSensitivityComponent>(LARGE_POPULATION, Allocator.TempJob);

            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                bondData[i] = _entityManager.GetComponentData<CreatureBondData>(entities[i]);
                ageData[i] = _entityManager.GetComponentData<AgeSensitivityComponent>(entities[i]);
            }

            var job = new CalculateEffectiveBondStrengthJob
            {
                bondData = bondData,
                ageSensitivity = ageData,
                effectiveBondStrength = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob)
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup
            job.Run(LARGE_POPULATION);

            // Actual measurement
            stopwatch.Restart();
            job.Run(LARGE_POPULATION);
            stopwatch.Stop();

            // Cleanup
            bondData.Dispose();
            ageData.Dispose();
            job.effectiveBondStrength.Dispose();
            entities.Dispose();

            // Assert - Should complete in under 3ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"AgeSensitivity_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 3.0,
                $"Performance regression: Age sensitivity took {elapsedMs:F2}ms, expected <3ms");
        }

        [Test]
        public void PopulationUnlockCheck_1000Creatures_CompletesWithin2ms()
        {
            // Arrange - Create creatures with various bond strengths
            SpawnCreaturesWithBonding(LARGE_POPULATION, out var entities);
            var random = new Unity.Mathematics.Random(11111);

            var bondStrengths = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                bondStrengths[i] = random.NextFloat(0f, 1f);
            }

            var job = new CountStrongBondsJob
            {
                bondStrengths = bondStrengths,
                threshold = 0.75f,
                strongBondCount = new NativeArray<int>(1, Allocator.TempJob)
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup
            job.Run();

            // Actual measurement
            stopwatch.Restart();
            job.Run();
            stopwatch.Stop();

            int strongBonds = job.strongBondCount[0];

            // Cleanup
            bondStrengths.Dispose();
            job.strongBondCount.Dispose();
            entities.Dispose();

            // Assert
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"PopulationUnlockCheck_1000Creatures found {strongBonds} strong bonds in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 2.0,
                $"Performance regression: Population unlock check took {elapsedMs:F2}ms, expected <2ms");
        }

        #endregion

        #region Genetics Performance

        [Test]
        public void GeneticsProcessing_1000Creatures_CompletesWithin2ms()
        {
            // Arrange - Create creatures with genetic data
            SpawnCreaturesWithGenetics(LARGE_POPULATION, out var entities);

            var geneticsData = new NativeArray<ChimeraGeneticDataComponent>(LARGE_POPULATION, Allocator.TempJob);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                geneticsData[i] = _entityManager.GetComponentData<ChimeraGeneticDataComponent>(entities[i]);
            }

            var job = new CalculateFitnessJob
            {
                genetics = geneticsData,
                environmentalFactors = new float4(0.5f, 0.6f, 0.7f, 0.5f),
                fitnessScores = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob)
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup
            job.Run(LARGE_POPULATION);

            // Actual measurement
            stopwatch.Restart();
            job.Run(LARGE_POPULATION);
            stopwatch.Stop();

            // Cleanup
            geneticsData.Dispose();
            job.fitnessScores.Dispose();
            entities.Dispose();

            // Assert - Should complete in under 2ms (1ms budget)
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"GeneticsProcessing_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 2.0,
                $"Performance regression: Genetics processing took {elapsedMs:F2}ms, expected <2ms");
        }

        #endregion

        #region Full Ecosystem Simulation

        [Test]
        public void FullEcosystemUpdate_1000Creatures_CompletesWithin12ms()
        {
            // Arrange - Create full ecosystem with all systems
            SpawnFullEcosystem(LARGE_POPULATION, out var entities);

            // Simulate one full update cycle: Genetics → Behavior → Social → Population
            var stopwatch = Stopwatch.StartNew();

            // 1. Genetics Processing (~1ms)
            var geneticsData = new NativeArray<ChimeraGeneticDataComponent>(LARGE_POPULATION, Allocator.TempJob);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                geneticsData[i] = _entityManager.GetComponentData<ChimeraGeneticDataComponent>(entities[i]);
            }

            var fitnessJob = new CalculateFitnessJob
            {
                genetics = geneticsData,
                environmentalFactors = new float4(0.5f, 0.6f, 0.7f, 0.5f),
                fitnessScores = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob)
            };
            fitnessJob.Run(LARGE_POPULATION);

            // 2. Social Bonding (~2ms)
            var bondData = new NativeArray<CreatureBondData>(LARGE_POPULATION, Allocator.TempJob);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                bondData[i] = _entityManager.GetComponentData<CreatureBondData>(entities[i]);
            }

            var bondJob = new UpdateBondStrengthJob
            {
                bondData = bondData,
                deltaTime = 0.016f,
                random = new Unity.Mathematics.Random(99999)
            };
            bondJob.Run(LARGE_POPULATION);

            // 3. Age Sensitivity (~1ms)
            var ageData = new NativeArray<AgeSensitivityComponent>(LARGE_POPULATION, Allocator.TempJob);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                ageData[i] = _entityManager.GetComponentData<AgeSensitivityComponent>(entities[i]);
            }

            var effectiveJob = new CalculateEffectiveBondStrengthJob
            {
                bondData = bondData,
                ageSensitivity = ageData,
                effectiveBondStrength = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob)
            };
            effectiveJob.Run(LARGE_POPULATION);

            // 4. Population Management (~1ms)
            var unlockJob = new CountStrongBondsJob
            {
                bondStrengths = effectiveJob.effectiveBondStrength,
                threshold = 0.75f,
                strongBondCount = new NativeArray<int>(1, Allocator.TempJob)
            };
            unlockJob.Run();

            stopwatch.Stop();

            // Cleanup
            geneticsData.Dispose();
            fitnessJob.fitnessScores.Dispose();
            bondData.Dispose();
            ageData.Dispose();
            effectiveJob.effectiveBondStrength.Dispose();
            unlockJob.strongBondCount.Dispose();
            entities.Dispose();

            // Assert - Total budget: 12ms (72% of 16.67ms frame)
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"FullEcosystemUpdate_1000Creatures completed in {elapsedMs:F2}ms");
            UnityEngine.Debug.Log($"  - Frame budget usage: {elapsedMs / 16.67 * 100:F1}% (target: <72%)");
            Assert.IsTrue(elapsedMs < 12.0,
                $"Performance regression: Full ecosystem update took {elapsedMs:F2}ms, expected <12ms");
        }

        [Test]
        public void ScalabilityTest_100_500_1000Creatures_LinearScaling()
        {
            // Test that performance scales linearly with creature count
            var results = new (int count, double timeMs)[3];

            // Test with 100 creatures
            results[0] = MeasureEcosystemPerformance(SMALL_POPULATION);

            // Test with 500 creatures
            results[1] = MeasureEcosystemPerformance(MEDIUM_POPULATION);

            // Test with 1000 creatures
            results[2] = MeasureEcosystemPerformance(LARGE_POPULATION);

            // Analyze scaling
            double timePerCreature100 = results[0].timeMs / results[0].count;
            double timePerCreature500 = results[1].timeMs / results[1].count;
            double timePerCreature1000 = results[2].timeMs / results[2].count;

            UnityEngine.Debug.Log($"Scalability Analysis:");
            UnityEngine.Debug.Log($"  100 creatures: {results[0].timeMs:F2}ms ({timePerCreature100:F4}ms per creature)");
            UnityEngine.Debug.Log($"  500 creatures: {results[1].timeMs:F2}ms ({timePerCreature500:F4}ms per creature)");
            UnityEngine.Debug.Log($"  1000 creatures: {results[2].timeMs:F2}ms ({timePerCreature1000:F4}ms per creature)");

            // Assert - Time per creature should remain relatively constant (within 50%)
            // This indicates good parallelization and linear scaling
            double maxTimePerCreature = math.max(math.max(timePerCreature100, timePerCreature500), timePerCreature1000);
            double minTimePerCreature = math.min(math.min(timePerCreature100, timePerCreature500), timePerCreature1000);
            double variance = (maxTimePerCreature - minTimePerCreature) / minTimePerCreature;

            UnityEngine.Debug.Log($"  Scaling variance: {variance * 100:F1}% (target: <50%)");
            Assert.IsTrue(variance < 0.5,
                $"Performance scaling issue: {variance * 100:F1}% variance, expected <50%");
        }

        #endregion

        #region Helper Methods

        private void SpawnCreaturesWithBonding(int count, out NativeArray<Entity> entities)
        {
            var archetype = _entityManager.CreateArchetype(
                typeof(CreatureBondData),
                typeof(ChimeraIdentity)
            );

            entities = new NativeArray<Entity>(count, Allocator.Temp);
            _entityManager.CreateEntity(archetype, entities);

            var random = new Unity.Mathematics.Random(12345);
            for (int i = 0; i < count; i++)
            {
                _entityManager.SetComponentData(entities[i], new CreatureBondData
                {
                    bondStrength = random.NextFloat(0f, 1f),
                    loyaltyLevel = random.NextFloat(0f, 1f),
                    playerId = 0,
                    creatureId = i,
                    hasHadFirstInteraction = random.NextBool()
                });

                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureID = new FixedString64Bytes($"creature_{i}"),
                    SpeciesID = random.NextInt(1, 10),
                    CreatureName = new FixedString32Bytes($"Creature_{i}")
                });
            }
        }

        private void SpawnCreaturesWithAgeSensitivity(int count, out NativeArray<Entity> entities)
        {
            var archetype = _entityManager.CreateArchetype(
                typeof(CreatureBondData),
                typeof(AgeSensitivityComponent),
                typeof(ChimeraIdentity)
            );

            entities = new NativeArray<Entity>(count, Allocator.Temp);
            _entityManager.CreateEntity(archetype, entities);

            var random = new Unity.Mathematics.Random(54321);
            for (int i = 0; i < count; i++)
            {
                _entityManager.SetComponentData(entities[i], new CreatureBondData
                {
                    bondStrength = random.NextFloat(0f, 1f),
                    loyaltyLevel = random.NextFloat(0f, 1f),
                    timeAlive = random.NextFloat(0f, 10000f)
                });

                // Mix of babies and adults
                bool isBaby = i < count / 2;
                _entityManager.SetComponentData(entities[i], new AgeSensitivityComponent
                {
                    forgivenessMultiplier = isBaby ? 2.5f : 0.3f,
                    memoryStrength = isBaby ? 0.2f : 0.95f,
                    recoverySpeed = isBaby ? 2.5f : 0.3f,
                    bondDepthMultiplier = isBaby ? 0.5f : 2.0f,
                    scarPermanence = isBaby ? 0.1f : 0.9f
                });

                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureID = new FixedString64Bytes($"creature_{i}"),
                    SpeciesID = random.NextInt(1, 10),
                    CreatureName = new FixedString32Bytes($"Creature_{i}")
                });
            }
        }

        private void SpawnCreaturesWithGenetics(int count, out NativeArray<Entity> entities)
        {
            var archetype = _entityManager.CreateArchetype(
                typeof(ChimeraGeneticDataComponent),
                typeof(ChimeraIdentity)
            );

            entities = new NativeArray<Entity>(count, Allocator.Temp);
            _entityManager.CreateEntity(archetype, entities);

            var random = new Unity.Mathematics.Random(11111);
            for (int i = 0; i < count; i++)
            {
                _entityManager.SetComponentData(entities[i], new ChimeraGeneticDataComponent
                {
                    Aggression = random.NextFloat(0f, 1f),
                    Sociability = random.NextFloat(0f, 1f),
                    Curiosity = random.NextFloat(0f, 1f),
                    Intelligence = random.NextFloat(0f, 1f),
                    Caution = random.NextFloat(0f, 1f),
                    Playfulness = random.NextFloat(0f, 1f),
                    Loyalty = random.NextFloat(0f, 1f),
                    Dominance = random.NextFloat(0f, 1f),
                    Adaptability = random.NextFloat(0f, 1f),
                    Speed = random.NextFloat(0f, 1f),
                    Stamina = random.NextFloat(0f, 1f),
                    Size = random.NextFloat(0f, 1f),
                    GeneticHash = (uint)random.NextInt()
                });

                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureID = new FixedString64Bytes($"creature_{i}"),
                    SpeciesID = random.NextInt(1, 10),
                    CreatureName = new FixedString32Bytes($"Creature_{i}")
                });
            }
        }

        private void SpawnFullEcosystem(int count, out NativeArray<Entity> entities)
        {
            var archetype = _entityManager.CreateArchetype(
                typeof(ChimeraGeneticDataComponent),
                typeof(CreatureBondData),
                typeof(AgeSensitivityComponent),
                typeof(ChimeraIdentity),
                typeof(LocalTransform)
            );

            entities = new NativeArray<Entity>(count, Allocator.Temp);
            _entityManager.CreateEntity(archetype, entities);

            var random = new Unity.Mathematics.Random(99999);
            for (int i = 0; i < count; i++)
            {
                _entityManager.SetComponentData(entities[i], new ChimeraGeneticDataComponent
                {
                    Aggression = random.NextFloat(0f, 1f),
                    Sociability = random.NextFloat(0f, 1f),
                    Curiosity = random.NextFloat(0f, 1f),
                    Intelligence = random.NextFloat(0f, 1f),
                    Speed = random.NextFloat(0f, 1f),
                    Stamina = random.NextFloat(0f, 1f),
                    Size = random.NextFloat(0f, 1f),
                    GeneticHash = (uint)random.NextInt()
                });

                _entityManager.SetComponentData(entities[i], new CreatureBondData
                {
                    bondStrength = random.NextFloat(0f, 1f),
                    loyaltyLevel = random.NextFloat(0f, 1f),
                    timeAlive = random.NextFloat(0f, 10000f)
                });

                bool isBaby = random.NextBool();
                _entityManager.SetComponentData(entities[i], new AgeSensitivityComponent
                {
                    forgivenessMultiplier = isBaby ? 2.5f : 0.3f,
                    memoryStrength = isBaby ? 0.2f : 0.95f,
                    recoverySpeed = isBaby ? 2.5f : 0.3f
                });

                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureID = new FixedString64Bytes($"creature_{i}"),
                    SpeciesID = random.NextInt(1, 10),
                    CreatureName = new FixedString32Bytes($"Creature_{i}")
                });

                _entityManager.SetComponentData(entities[i], LocalTransform.FromPosition(
                    new float3(random.NextFloat(-100f, 100f), 0f, random.NextFloat(-100f, 100f))
                ));
            }
        }

        private (int count, double timeMs) MeasureEcosystemPerformance(int creatureCount)
        {
            SpawnFullEcosystem(creatureCount, out var entities);

            var stopwatch = Stopwatch.StartNew();

            // Run simplified ecosystem update
            var geneticsData = new NativeArray<ChimeraGeneticDataComponent>(creatureCount, Allocator.TempJob);
            var bondData = new NativeArray<CreatureBondData>(creatureCount, Allocator.TempJob);

            for (int i = 0; i < creatureCount; i++)
            {
                geneticsData[i] = _entityManager.GetComponentData<ChimeraGeneticDataComponent>(entities[i]);
                bondData[i] = _entityManager.GetComponentData<CreatureBondData>(entities[i]);
            }

            var fitnessJob = new CalculateFitnessJob
            {
                genetics = geneticsData,
                environmentalFactors = new float4(0.5f),
                fitnessScores = new NativeArray<float>(creatureCount, Allocator.TempJob)
            };
            fitnessJob.Run(creatureCount);

            var bondJob = new UpdateBondStrengthJob
            {
                bondData = bondData,
                deltaTime = 0.016f,
                random = new Unity.Mathematics.Random(11111)
            };
            bondJob.Run(creatureCount);

            stopwatch.Stop();

            // Cleanup
            geneticsData.Dispose();
            bondData.Dispose();
            fitnessJob.fitnessScores.Dispose();
            entities.Dispose();

            return (creatureCount, stopwatch.Elapsed.TotalMilliseconds);
        }

        #endregion

        #region Performance Jobs

        struct UpdateBondStrengthJob : IJobParallelFor
        {
            public NativeArray<CreatureBondData> bondData;
            public float deltaTime;
            public Unity.Mathematics.Random random;

            public void Execute(int index)
            {
                var bond = bondData[index];
                // Simulate bond strength decay over time
                bond.timeSinceLastInteraction += deltaTime;
                bond.bondStrength = math.max(0f, bond.bondStrength - deltaTime * 0.01f);
                bondData[index] = bond;
            }
        }

        struct CalculateEffectiveBondStrengthJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<CreatureBondData> bondData;
            [ReadOnly] public NativeArray<AgeSensitivityComponent> ageSensitivity;
            public NativeArray<float> effectiveBondStrength;

            public void Execute(int index)
            {
                float baseBond = bondData[index].bondStrength;
                float ageMultiplier = ageSensitivity[index].forgivenessMultiplier;
                effectiveBondStrength[index] = baseBond * ageMultiplier;
            }
        }

        struct CountStrongBondsJob : IJob
        {
            [ReadOnly] public NativeArray<float> bondStrengths;
            public float threshold;
            public NativeArray<int> strongBondCount;

            public void Execute()
            {
                int count = 0;
                for (int i = 0; i < bondStrengths.Length; i++)
                {
                    if (bondStrengths[i] >= threshold)
                        count++;
                }
                strongBondCount[0] = count;
            }
        }

        struct CalculateFitnessJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ChimeraGeneticDataComponent> genetics;
            public float4 environmentalFactors;
            public NativeArray<float> fitnessScores;

            public void Execute(int index)
            {
                var g = genetics[index];
                // Simplified fitness calculation
                float4 traits = new float4(g.Aggression, g.Sociability, g.Curiosity, g.Intelligence);
                float4 compatibility = 1.0f - math.abs(traits - environmentalFactors);
                fitnessScores[index] = math.csum(compatibility) / 4.0f;
            }
        }

        #endregion
    }
}
