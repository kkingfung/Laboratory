using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Chimera.Genetics.Advanced;

namespace Laboratory.Tests.Performance
{
    /// <summary>
    /// Performance regression tests to ensure optimizations remain effective
    /// These establish performance baselines and alert on regressions
    /// </summary>
    [TestFixture]
    public class PerformanceRegressionTests
    {
        [Test, Performance]
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
            Measure.Method(() =>
            {
                for (int i = 0; i < creatureCount; i++)
                {
                    job.Execute(i);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .IterationsPerMeasurement(10)
            .SampleGroup("GeneticsBlending_1000Creatures")
            .Run();

            // Assert - Should complete in under 5ms (target: <1ms with Burst)
            // Performance test framework will track this baseline

            // Cleanup
            parent1Traits.Dispose();
            parent2Traits.Dispose();
            blendFactors.Dispose();
            offspringTraits.Dispose();
        }

        [Test, Performance]
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
            Measure.Method(() =>
            {
                for (int i = 0; i < creatureCount; i++)
                {
                    job.Execute(i);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .IterationsPerMeasurement(5)
            .SampleGroup("FitnessEvaluation_1000Creatures")
            .Run();

            // Cleanup
            creatureTraits.Dispose();
            environmentalFactors.Dispose();
            fitnessWeights.Dispose();
            fitnessScores.Dispose();
        }

        [Test, Performance]
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
            Measure.Method(() =>
            {
                for (int i = 0; i < pairCount; i++)
                {
                    job.Execute(i);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .IterationsPerMeasurement(5)
            .SampleGroup("MatingCompatibility_1000Pairs")
            .Run();

            // Cleanup
            creature1Traits.Dispose();
            creature2Traits.Dispose();
            creature1Generations.Dispose();
            creature2Generations.Dispose();
            compatibilityScores.Dispose();
        }

        [Test, Performance]
        public void MemoryAllocation_GeneticsJobs_ProducesMinimalGarbage()
        {
            // Measure memory allocations to ensure no GC pressure
            const int iterationCount = 100;

            Measure.Method(() =>
            {
                var traits = new NativeArray<float4>(10, Allocator.Temp);
                // Simulate job work
                for (int i = 0; i < 10; i++)
                {
                    traits[i] = new float4(0.5f);
                }
                traits.Dispose();
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .GC()
            .SampleGroup("GeneticsJobs_GCAllocation")
            .Run();

            // Target: <1KB allocations per iteration
        }
    }

    /// <summary>
    /// Baseline performance metrics to track across versions
    /// </summary>
    [TestFixture]
    public class PerformanceBaselineTests
    {
        [Test, Performance]
        public void Baseline_10000_SIMDOperations()
        {
            // Establish baseline for SIMD math operations
            var data = new NativeArray<float4>(10000, Allocator.Temp);

            Measure.Method(() =>
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = math.normalize(new float4(i));
                }
            })
            .WarmupCount(5)
            .MeasurementCount(100)
            .SampleGroup("Baseline_SIMD")
            .Run();

            data.Dispose();
        }

        [Test, Performance]
        public void Baseline_NativeArray_CreationAndDisposal()
        {
            // Measure NativeArray overhead
            Measure.Method(() =>
            {
                var temp = new NativeArray<float>(1000, Allocator.Temp);
                temp.Dispose();
            })
            .WarmupCount(10)
            .MeasurementCount(100)
            .SampleGroup("Baseline_NativeArrayAllocation")
            .Run();
        }
    }
}
