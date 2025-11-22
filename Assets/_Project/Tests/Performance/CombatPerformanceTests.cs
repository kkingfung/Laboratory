using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Diagnostics;
using UnityEngine;
using Laboratory.Chimera.Activities;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Core;
using ChimeraIdentity = Laboratory.Chimera.Core.CreatureIdentityComponent;

namespace Laboratory.Tests.Performance
{
    /// <summary>
    /// Combat system performance tests for Project Chimera
    ///
    /// Performance Targets (60 FPS = 16.67ms frame budget):
    /// - 1000 creatures in combat: <3ms per frame for damage calculation
    /// - Combat resolution (winners/losers): <2ms per frame
    /// - Health updates (1000 creatures): <1ms per frame
    /// - Combat matchmaking (500 pairs): <2ms per frame
    ///
    /// Total combat budget: 8ms / 16.67ms = 48% of frame
    /// (Leaves room for rendering, AI, social systems)
    /// </summary>
    [TestFixture]
    public class CombatPerformanceTests
    {
        private World _testWorld;
        private EntityManager _entityManager;
        private const int LARGE_POPULATION = 1000;
        private const int MEDIUM_POPULATION = 500;
        private const int SMALL_POPULATION = 100;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("CombatPerformanceTestWorld");
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

        #region Combat Initialization Performance

        [Test]
        public void CombatInitialization_1000Creatures_CompletesWithin10ms()
        {
            // Arrange - Create creatures with combat stats
            var archetype = _entityManager.CreateArchetype(
                typeof(ChimeraGeneticDataComponent),
                typeof(CombatStatsComponent),
                typeof(ChimeraIdentity)
            );

            var entities = new NativeArray<Entity>(LARGE_POPULATION, Allocator.Temp);
            var random = new Unity.Mathematics.Random(12345);

            // Act & Measure - Initialize combat for 1000 creatures
            var stopwatch = Stopwatch.StartNew();

            _entityManager.CreateEntity(archetype, entities);

            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                var genetics = new ChimeraGeneticDataComponent
                {
                    Aggression = random.NextFloat(0f, 1f),
                    Dominance = random.NextFloat(0f, 1f),
                    Size = random.NextFloat(0f, 1f),
                    Speed = random.NextFloat(0f, 1f),
                    Stamina = random.NextFloat(0f, 1f)
                };

                _entityManager.SetComponentData(entities[i], genetics);
                _entityManager.SetComponentData(entities[i], CalculateCombatStats(genetics));
                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureId = i,
                    SpeciesId = random.NextInt(1, 10),
                    CreatureName = new FixedString64Bytes($"Fighter_{i}")
                });
            }

            stopwatch.Stop();
            entities.Dispose();

            // Assert
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"CombatInitialization_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 10.0,
                $"Performance regression: Combat initialization took {elapsedMs:F2}ms, expected <10ms");
        }

        #endregion

        #region Damage Calculation Performance

        [Test]
        public void DamageCalculation_1000Creatures_CompletesWithin3ms()
        {
            // Arrange - Create combatants
            SpawnCombatants(LARGE_POPULATION, out var attackers, out var defenders);

            var attackerStats = new NativeArray<CombatStatsComponent>(LARGE_POPULATION, Allocator.TempJob);
            var defenderStats = new NativeArray<CombatStatsComponent>(LARGE_POPULATION, Allocator.TempJob);
            var damageResults = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob);

            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                attackerStats[i] = _entityManager.GetComponentData<CombatStatsComponent>(attackers[i]);
                defenderStats[i] = _entityManager.GetComponentData<CombatStatsComponent>(defenders[i]);
            }

            var job = new CalculateDamageJob
            {
                attackerStats = attackerStats,
                defenderStats = defenderStats,
                damageResults = damageResults,
                random = new Unity.Mathematics.Random(54321)
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
            attackerStats.Dispose();
            defenderStats.Dispose();
            damageResults.Dispose();
            attackers.Dispose();
            defenders.Dispose();

            // Assert - Should complete in under 3ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"DamageCalculation_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 3.0,
                $"Performance regression: Damage calculation took {elapsedMs:F2}ms, expected <3ms");
        }

        [Test]
        public void HealthUpdates_1000Creatures_CompletesWithin1ms()
        {
            // Arrange - Create creatures with health
            SpawnCombatants(LARGE_POPULATION, out var entities, out _);

            var healthData = new NativeArray<HealthComponent>(LARGE_POPULATION, Allocator.TempJob);
            var damageAmounts = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob);

            var random = new Unity.Mathematics.Random(11111);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                healthData[i] = new HealthComponent
                {
                    currentHealth = 100f,
                    maxHealth = 100f,
                    isAlive = true
                };
                damageAmounts[i] = random.NextFloat(10f, 30f);
            }

            var job = new ApplyDamageJob
            {
                healthData = healthData,
                damageAmounts = damageAmounts
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup
            job.Run(LARGE_POPULATION);

            // Reset health for actual measurement
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                var health = healthData[i];
                health.currentHealth = 100f;
                health.isAlive = true;
                healthData[i] = health;
            }

            // Actual measurement
            stopwatch.Restart();
            job.Run(LARGE_POPULATION);
            stopwatch.Stop();

            // Cleanup
            healthData.Dispose();
            damageAmounts.Dispose();
            entities.Dispose();

            // Assert - Should complete in under 1ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"HealthUpdates_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 1.0,
                $"Performance regression: Health updates took {elapsedMs:F2}ms, expected <1ms");
        }

        #endregion

        #region Combat Resolution Performance

        [Test]
        public void CombatResolution_500Matches_CompletesWithin5ms()
        {
            // Arrange - Create 500 combat matches (1000 combatants)
            SpawnCombatants(LARGE_POPULATION, out var entities, out _);

            var combatStats = new NativeArray<CombatStatsComponent>(LARGE_POPULATION, Allocator.TempJob);
            var health = new NativeArray<HealthComponent>(LARGE_POPULATION, Allocator.TempJob);
            var random = new Unity.Mathematics.Random(99999);

            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                combatStats[i] = _entityManager.GetComponentData<CombatStatsComponent>(entities[i]);
                health[i] = new HealthComponent
                {
                    currentHealth = random.NextFloat(50f, 100f),
                    maxHealth = 100f,
                    isAlive = true
                };
            }

            var job = new ResolveCombatMatchesJob
            {
                combatStats = combatStats,
                health = health,
                matchResults = new NativeArray<CombatResult>(MEDIUM_POPULATION, Allocator.TempJob),
                random = random
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup
            job.Run(MEDIUM_POPULATION);

            // Actual measurement
            stopwatch.Restart();
            job.Run(MEDIUM_POPULATION);
            stopwatch.Stop();

            // Cleanup
            combatStats.Dispose();
            health.Dispose();
            job.matchResults.Dispose();
            entities.Dispose();

            // Assert - Should complete in under 5ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"CombatResolution_500Matches completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 5.0,
                $"Performance regression: Combat resolution took {elapsedMs:F2}ms, expected <5ms");
        }

        #endregion

        #region Full Combat Simulation

        [Test]
        public void FullCombatTick_1000Creatures_CompletesWithin8ms()
        {
            // Arrange - Full combat simulation cycle
            SpawnCombatants(LARGE_POPULATION, out var entities, out _);

            var combatStats = new NativeArray<CombatStatsComponent>(LARGE_POPULATION, Allocator.TempJob);
            var health = new NativeArray<HealthComponent>(LARGE_POPULATION, Allocator.TempJob);
            var damageResults = new NativeArray<float>(LARGE_POPULATION, Allocator.TempJob);

            var random = new Unity.Mathematics.Random(77777);
            for (int i = 0; i < LARGE_POPULATION; i++)
            {
                combatStats[i] = _entityManager.GetComponentData<CombatStatsComponent>(entities[i]);
                health[i] = new HealthComponent
                {
                    currentHealth = 100f,
                    maxHealth = 100f,
                    isAlive = true
                };
            }

            // Act & Measure - Full combat tick
            var stopwatch = Stopwatch.StartNew();

            // 1. Damage calculation (~3ms)
            var damageJob = new CalculateDamageJob
            {
                attackerStats = combatStats,
                defenderStats = combatStats,
                damageResults = damageResults,
                random = random
            };
            damageJob.Run(LARGE_POPULATION);

            // 2. Apply damage (~1ms)
            var applyJob = new ApplyDamageJob
            {
                healthData = health,
                damageAmounts = damageResults
            };
            applyJob.Run(LARGE_POPULATION);

            // 3. Resolve matches (~2ms)
            var resolveJob = new ResolveCombatMatchesJob
            {
                combatStats = combatStats,
                health = health,
                matchResults = new NativeArray<CombatResult>(MEDIUM_POPULATION, Allocator.TempJob),
                random = random
            };
            resolveJob.Run(MEDIUM_POPULATION);

            stopwatch.Stop();

            // Cleanup
            combatStats.Dispose();
            health.Dispose();
            damageResults.Dispose();
            resolveJob.matchResults.Dispose();
            entities.Dispose();

            // Assert - Total budget: 8ms (48% of 16.67ms frame)
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"FullCombatTick_1000Creatures completed in {elapsedMs:F2}ms");
            UnityEngine.Debug.Log($"  - Frame budget usage: {elapsedMs / 16.67 * 100:F1}% (target: <48%)");
            Assert.IsTrue(elapsedMs < 8.0,
                $"Performance regression: Full combat tick took {elapsedMs:F2}ms, expected <8ms");
        }

        [Test]
        public void CombatScalability_100_500_1000Creatures_LinearScaling()
        {
            // Test that combat performance scales linearly with creature count
            var results = new (int count, double timeMs)[3];

            // Test with 100 creatures
            results[0] = MeasureCombatPerformance(SMALL_POPULATION);

            // Test with 500 creatures
            results[1] = MeasureCombatPerformance(MEDIUM_POPULATION);

            // Test with 1000 creatures
            results[2] = MeasureCombatPerformance(LARGE_POPULATION);

            // Analyze scaling
            double timePerCreature100 = results[0].timeMs / results[0].count;
            double timePerCreature500 = results[1].timeMs / results[1].count;
            double timePerCreature1000 = results[2].timeMs / results[2].count;

            UnityEngine.Debug.Log($"Combat Scalability Analysis:");
            UnityEngine.Debug.Log($"  100 creatures: {results[0].timeMs:F2}ms ({timePerCreature100:F4}ms per creature)");
            UnityEngine.Debug.Log($"  500 creatures: {results[1].timeMs:F2}ms ({timePerCreature500:F4}ms per creature)");
            UnityEngine.Debug.Log($"  1000 creatures: {results[2].timeMs:F2}ms ({timePerCreature1000:F4}ms per creature)");

            // Assert - Time per creature should remain relatively constant (within 50%)
            double maxTimePerCreature = math.max(math.max(timePerCreature100, timePerCreature500), timePerCreature1000);
            double minTimePerCreature = math.min(math.min(timePerCreature100, timePerCreature500), timePerCreature1000);
            double variance = (maxTimePerCreature - minTimePerCreature) / minTimePerCreature;

            UnityEngine.Debug.Log($"  Scaling variance: {variance * 100:F1}% (target: <50%)");
            Assert.IsTrue(variance < 0.5,
                $"Combat scaling issue: {variance * 100:F1}% variance, expected <50%");
        }

        #endregion

        #region Helper Methods

        private void SpawnCombatants(int count, out NativeArray<Entity> entities, out NativeArray<Entity> dummyEntities)
        {
            var archetype = _entityManager.CreateArchetype(
                typeof(CombatStatsComponent),
                typeof(ChimeraGeneticDataComponent),
                typeof(ChimeraIdentity)
            );

            entities = new NativeArray<Entity>(count, Allocator.Temp);
            _entityManager.CreateEntity(archetype, entities);

            var random = new Unity.Mathematics.Random(12345);
            for (int i = 0; i < count; i++)
            {
                var genetics = new ChimeraGeneticDataComponent
                {
                    Aggression = random.NextFloat(0f, 1f),
                    Dominance = random.NextFloat(0f, 1f),
                    Size = random.NextFloat(0f, 1f),
                    Speed = random.NextFloat(0f, 1f),
                    Stamina = random.NextFloat(0f, 1f)
                };

                _entityManager.SetComponentData(entities[i], genetics);
                _entityManager.SetComponentData(entities[i], CalculateCombatStats(genetics));
                _entityManager.SetComponentData(entities[i], new ChimeraIdentity
                {
                    CreatureId = i,
                    SpeciesId = random.NextInt(1, 10),
                    CreatureName = new FixedString64Bytes($"Fighter_{i}")
                });
            }

            dummyEntities = new NativeArray<Entity>(0, Allocator.Temp);
        }

        private CombatStatsComponent CalculateCombatStats(ChimeraGeneticDataComponent genetics)
        {
            return new CombatStatsComponent
            {
                attack = genetics.Aggression * 50f + genetics.Dominance * 30f + genetics.Size * 20f,
                defense = genetics.Size * 40f + genetics.Stamina * 30f,
                speed = genetics.Speed * 100f,
                criticalChance = genetics.Aggression * 0.2f,
                criticalMultiplier = 1.5f + genetics.Dominance * 0.5f
            };
        }

        private (int count, double timeMs) MeasureCombatPerformance(int creatureCount)
        {
            SpawnCombatants(creatureCount, out var entities, out _);

            var combatStats = new NativeArray<CombatStatsComponent>(creatureCount, Allocator.TempJob);
            var damageResults = new NativeArray<float>(creatureCount, Allocator.TempJob);
            var random = new Unity.Mathematics.Random(88888);

            for (int i = 0; i < creatureCount; i++)
            {
                combatStats[i] = _entityManager.GetComponentData<CombatStatsComponent>(entities[i]);
            }

            var stopwatch = Stopwatch.StartNew();

            var damageJob = new CalculateDamageJob
            {
                attackerStats = combatStats,
                defenderStats = combatStats,
                damageResults = damageResults,
                random = random
            };
            damageJob.Run(creatureCount);

            stopwatch.Stop();

            // Cleanup
            combatStats.Dispose();
            damageResults.Dispose();
            entities.Dispose();

            return (creatureCount, stopwatch.Elapsed.TotalMilliseconds);
        }

        #endregion

        #region Combat Components

        public struct CombatStatsComponent : IComponentData
        {
            public float attack;
            public float defense;
            public float speed;
            public float criticalChance;
            public float criticalMultiplier;
        }

        public struct HealthComponent : IComponentData
        {
            public float currentHealth;
            public float maxHealth;
            public bool isAlive;
        }

        public struct CombatResult
        {
            public Entity winner;
            public Entity loser;
            public float damageDealt;
            public bool wasCritical;
        }

        #endregion

        #region Combat Jobs

        struct CalculateDamageJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<CombatStatsComponent> attackerStats;
            [ReadOnly] public NativeArray<CombatStatsComponent> defenderStats;
            public NativeArray<float> damageResults;
            public Unity.Mathematics.Random random;

            public void Execute(int index)
            {
                var attacker = attackerStats[index];
                var defender = defenderStats[(index + 1) % defenderStats.Length]; // Circular opponent

                // Calculate base damage
                float baseDamage = math.max(0f, attacker.attack - defender.defense * 0.5f);

                // Critical hit check
                bool isCritical = random.NextFloat() < attacker.criticalChance;
                float damage = isCritical ? baseDamage * attacker.criticalMultiplier : baseDamage;

                damageResults[index] = damage;
            }
        }

        struct ApplyDamageJob : IJobParallelFor
        {
            public NativeArray<HealthComponent> healthData;
            [ReadOnly] public NativeArray<float> damageAmounts;

            public void Execute(int index)
            {
                var health = healthData[index];
                health.currentHealth = math.max(0f, health.currentHealth - damageAmounts[index]);
                health.isAlive = health.currentHealth > 0f;
                healthData[index] = health;
            }
        }

        struct ResolveCombatMatchesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<CombatStatsComponent> combatStats;
            [ReadOnly] public NativeArray<HealthComponent> health;
            public NativeArray<CombatResult> matchResults;
            public Unity.Mathematics.Random random;

            public void Execute(int index)
            {
                // Match index i vs index (i + matchOffset)
                int fighter1Index = index * 2;
                int fighter2Index = math.min(index * 2 + 1, health.Length - 1);

                var health1 = health[fighter1Index];
                var health2 = health[fighter2Index];
                var stats1 = combatStats[fighter1Index];
                var stats2 = combatStats[fighter2Index];

                // Determine winner based on health and stats
                Entity winner = default;
                Entity loser = default;
                float damageDealt = 0f;
                bool wasCritical = false;

                if (health1.currentHealth > health2.currentHealth)
                {
                    winner = new Entity { Index = fighter1Index, Version = 1 };
                    loser = new Entity { Index = fighter2Index, Version = 1 };
                    damageDealt = health2.maxHealth - health2.currentHealth;
                }
                else
                {
                    winner = new Entity { Index = fighter2Index, Version = 1 };
                    loser = new Entity { Index = fighter1Index, Version = 1 };
                    damageDealt = health1.maxHealth - health1.currentHealth;
                }

                wasCritical = random.NextFloat() < 0.2f;

                matchResults[index] = new CombatResult
                {
                    winner = winner,
                    loser = loser,
                    damageDealt = damageDealt,
                    wasCritical = wasCritical
                };
            }
        }

        #endregion
    }
}
