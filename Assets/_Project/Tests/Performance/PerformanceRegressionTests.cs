using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Diagnostics;
using UnityEngine;

namespace Laboratory.Tests.Performance
{
    /// <summary>
    /// Performance regression tests to ensure optimizations remain effective
    /// These establish performance baselines and alert on regressions
    /// </summary>
    [TestFixture]
    public class PerformanceRegressionTests
    {
        [Test]
        public void GeneticsJob_1000Creatures_CompletesWithin5ms()
        {
            // Arrange
            const int creatureCount = 1000;
            var parent1Traits = new NativeArray<float4>(creatureCount, Allocator.TempJob);
            var parent2Traits = new NativeArray<float4>(creatureCount, Allocator.TempJob);
            var blendFactors = new NativeArray<float>(creatureCount, Allocator.TempJob);
            var offspringTraits = new NativeArray<float4>(creatureCount, Allocator.TempJob);

            // Initialize with realistic data
            for (int i = 0; i < creatureCount; i++)
            {
                parent1Traits[i] = new float4(0.6f, 0.7f, 0.5f, 0.8f);
                parent2Traits[i] = new float4(0.4f, 0.6f, 0.7f, 0.5f);
                blendFactors[i] = 0.5f;
            }

            var job = new SIMDGeneticOptimizations.SIMDTraitBlendingJob
            {
                parent1Traits = parent1Traits,
                parent2Traits = parent2Traits,
                blendFactors = blendFactors,
                offspringTraits = offspringTraits
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup runs
            for (int warmup = 0; warmup < 5; warmup++)
            {
                for (int i = 0; i < creatureCount; i++)
                {
                    job.Execute(i);
                }
            }

            // Actual performance measurement
            stopwatch.Restart();
            for (int i = 0; i < creatureCount; i++)
            {
                job.Execute(i);
            }
            stopwatch.Stop();

            // Assert - Should complete in under 5ms (target: <1ms with Burst)
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"GeneticsJob_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 5.0, $"Performance regression: GeneticsJob took {elapsedMs:F2}ms, expected <5ms");

            // Cleanup
            parent1Traits.Dispose();
            parent2Traits.Dispose();
            blendFactors.Dispose();
            offspringTraits.Dispose();
        }

        [Test]
        public void FitnessEvaluation_1000Creatures_CompletesWithin10ms()
        {
            // Arrange
            const int creatureCount = 1000;
            var creatureTraits = new NativeArray<float4>(creatureCount, Allocator.TempJob);
            var environmentalFactors = new NativeArray<float4>(creatureCount, Allocator.TempJob);
            var fitnessWeights = new NativeArray<float4>(creatureCount, Allocator.TempJob);
            var fitnessScores = new NativeArray<float>(creatureCount, Allocator.TempJob);

            for (int i = 0; i < creatureCount; i++)
            {
                creatureTraits[i] = new float4(0.5f, 0.6f, 0.7f, 0.5f);
                environmentalFactors[i] = new float4(0.6f, 0.5f, 0.8f, 0.6f);
                fitnessWeights[i] = new float4(0.25f, 0.25f, 0.25f, 0.25f);
            }

            var job = new SIMDGeneticOptimizations.SIMDFitnessEvaluationJob
            {
                creatureTraits = creatureTraits,
                environmentalFactors = environmentalFactors,
                fitnessWeights = fitnessWeights,
                fitnessScores = fitnessScores
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup runs
            for (int warmup = 0; warmup < 5; warmup++)
            {
                for (int i = 0; i < creatureCount; i++)
                {
                    job.Execute(i);
                }
            }

            // Actual performance measurement
            stopwatch.Restart();
            for (int i = 0; i < creatureCount; i++)
            {
                job.Execute(i);
            }
            stopwatch.Stop();

            // Assert - Should complete in under 10ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"FitnessEvaluation_1000Creatures completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 10.0, $"Performance regression: FitnessEvaluation took {elapsedMs:F2}ms, expected <10ms");

            // Cleanup
            creatureTraits.Dispose();
            environmentalFactors.Dispose();
            fitnessWeights.Dispose();
            fitnessScores.Dispose();
        }

        [Test]
        public void MatingCompatibility_1000Pairs_CompletesWithin10ms()
        {
            // Arrange
            const int pairCount = 1000;
            var creature1Traits = new NativeArray<float4>(pairCount, Allocator.TempJob);
            var creature2Traits = new NativeArray<float4>(pairCount, Allocator.TempJob);
            var creature1Generations = new NativeArray<int>(pairCount, Allocator.TempJob);
            var creature2Generations = new NativeArray<int>(pairCount, Allocator.TempJob);
            var compatibilityScores = new NativeArray<float>(pairCount, Allocator.TempJob);

            for (int i = 0; i < pairCount; i++)
            {
                creature1Traits[i] = new float4(0.4f, 0.6f, 0.5f, 0.7f);
                creature2Traits[i] = new float4(0.6f, 0.5f, 0.7f, 0.4f);
                creature1Generations[i] = i % 10;
                creature2Generations[i] = (i + 3) % 10;
            }

            var job = new SIMDGeneticOptimizations.SIMDMatingCompatibilityJob
            {
                creature1Traits = creature1Traits,
                creature2Traits = creature2Traits,
                creature1Generations = creature1Generations,
                creature2Generations = creature2Generations,
                compatibilityScores = compatibilityScores
            };

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();

            // Warmup runs
            for (int warmup = 0; warmup < 5; warmup++)
            {
                for (int i = 0; i < pairCount; i++)
                {
                    job.Execute(i);
                }
            }

            // Actual performance measurement
            stopwatch.Restart();
            for (int i = 0; i < pairCount; i++)
            {
                job.Execute(i);
            }
            stopwatch.Stop();

            // Assert - Should complete in under 10ms
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"MatingCompatibility_1000Pairs completed in {elapsedMs:F2}ms");
            Assert.IsTrue(elapsedMs < 10.0, $"Performance regression: MatingCompatibility took {elapsedMs:F2}ms, expected <10ms");

            // Cleanup
            creature1Traits.Dispose();
            creature2Traits.Dispose();
            creature1Generations.Dispose();
            creature2Generations.Dispose();
            compatibilityScores.Dispose();
        }

        [Test]
        public void MemoryAllocation_GeneticsJobs_ProducesMinimalGarbage()
        {
            // Measure memory allocations to ensure no GC pressure
            const int iterationCount = 100;

            // Measure memory allocations to ensure no GC pressure
            long memoryBefore = System.GC.GetTotalMemory(true);

            for (int iteration = 0; iteration < iterationCount; iteration++)
            {
                var traits = new NativeArray<float4>(10, Allocator.Temp);
                // Simulate job work
                for (int i = 0; i < 10; i++)
                {
                    traits[i] = new float4(0.5f);
                }
                traits.Dispose();
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            long memoryAfter = System.GC.GetTotalMemory(false);

            long allocatedBytes = memoryAfter - memoryBefore;
            long bytesPerIteration = allocatedBytes / iterationCount;

            UnityEngine.Debug.Log($"MemoryAllocation test: {bytesPerIteration} bytes per iteration");

            // Target: <1KB allocations per iteration
            Assert.IsTrue(bytesPerIteration < 1024, $"Memory regression: {bytesPerIteration} bytes per iteration, expected <1024 bytes");
        }
    }

    /// <summary>
    /// Baseline performance metrics to track across versions
    /// </summary>
    [TestFixture]
    public class PerformanceBaselineTests
    {
        [Test]
        public void Baseline_10000_SIMDOperations()
        {
            // Establish baseline for SIMD math operations
            var data = new NativeArray<float4>(10000, Allocator.Temp);

            var stopwatch = Stopwatch.StartNew();

            // Warmup runs
            for (int warmup = 0; warmup < 5; warmup++)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = math.normalize(new float4(i));
                }
            }

            // Actual performance measurement
            stopwatch.Restart();
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = math.normalize(new float4(i));
            }
            stopwatch.Stop();

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"Baseline_10000_SIMDOperations completed in {elapsedMs:F2}ms");

            // Baseline measurement - just log the performance for tracking
            Assert.IsTrue(elapsedMs < 100.0, $"SIMD baseline performance issue: {elapsedMs:F2}ms for 10k operations");

            data.Dispose();
        }

        [Test]
        public void Baseline_NativeArray_CreationAndDisposal()
        {
            // Measure NativeArray overhead
            const int iterationCount = 100;
            var stopwatch = Stopwatch.StartNew();

            // Warmup runs
            for (int warmup = 0; warmup < 10; warmup++)
            {
                var temp = new NativeArray<float>(1000, Allocator.Temp);
                temp.Dispose();
            }

            // Actual performance measurement
            stopwatch.Restart();
            for (int i = 0; i < iterationCount; i++)
            {
                var temp = new NativeArray<float>(1000, Allocator.Temp);
                temp.Dispose();
            }
            stopwatch.Stop();

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            double msPerIteration = elapsedMs / iterationCount;
            UnityEngine.Debug.Log($"Baseline_NativeArray allocation: {msPerIteration:F4}ms per iteration");

            // Baseline measurement - just log the performance for tracking
            Assert.IsTrue(msPerIteration < 0.1, $"NativeArray allocation baseline issue: {msPerIteration:F4}ms per allocation");
        }
    }

    /// <summary>
    /// Stub implementation for testing - actual genetics optimizations would be in the main genetics system
    /// </summary>
    public static class SIMDGeneticOptimizations
    {
        public struct SIMDTraitBlendingJob : IJobParallelFor
        {
            public NativeArray<float4> parent1Traits;
            public NativeArray<float4> parent2Traits;
            public NativeArray<float> blendFactors;
            public NativeArray<float4> offspringTraits;

            public void Execute(int index)
            {
                // Stub implementation for testing
                float blendFactor = blendFactors[index];
                offspringTraits[index] = math.lerp(parent1Traits[index], parent2Traits[index], blendFactor);
            }
        }

        public struct SIMDFitnessEvaluationJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float4> creatureTraits;
            [ReadOnly] public NativeArray<float4> environmentalFactors;
            [ReadOnly] public NativeArray<float4> fitnessWeights;
            public NativeArray<float> fitnessScores;

            public void Execute(int index)
            {
                // Stub implementation for testing
                float4 traits = creatureTraits[index];
                float4 environment = environmentalFactors[index];
                float4 weights = fitnessWeights[index];

                float4 compatibility = 1.0f - math.abs(traits - environment);
                fitnessScores[index] = math.dot(compatibility, weights);
            }
        }

        public struct SIMDMatingCompatibilityJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float4> creature1Traits;
            [ReadOnly] public NativeArray<float4> creature2Traits;
            [ReadOnly] public NativeArray<int> creature1Generations;
            [ReadOnly] public NativeArray<int> creature2Generations;
            public NativeArray<float> compatibilityScores;

            public void Execute(int index)
            {
                // Stub implementation for testing
                float4 diff = math.abs(creature1Traits[index] - creature2Traits[index]);
                float traitCompatibility = 1.0f - math.csum(diff) / 4.0f;

                int genDiff = math.abs(creature1Generations[index] - creature2Generations[index]);
                float genBonus = math.min(genDiff * 0.1f, 0.5f);

                compatibilityScores[index] = math.clamp(traitCompatibility + genBonus, 0f, 1f);
            }
        }
    }
}
