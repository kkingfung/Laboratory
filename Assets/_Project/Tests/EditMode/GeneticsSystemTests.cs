using NUnit.Framework;
using Unity.Mathematics;
using Unity.Collections;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Tests.EditMode
{
    /// <summary>
    /// Tests for the genetics system - trait inheritance, breeding, mutations
    /// </summary>
    public class GeneticsSystemTests
    {
        [Test]
        public void TraitBlending_WithEqualWeights_ProducesAverageTraits()
        {
            // Arrange
            var parent1Traits = new NativeArray<float4>(1, Allocator.Temp);
            var parent2Traits = new NativeArray<float4>(1, Allocator.Temp);
            var blendFactors = new NativeArray<float>(1, Allocator.Temp);
            var offspringTraits = new NativeArray<float4>(1, Allocator.Temp);

            parent1Traits[0] = new float4(1.0f, 0.8f, 0.6f, 0.4f);
            parent2Traits[0] = new float4(0.0f, 0.2f, 0.4f, 0.6f);
            blendFactors[0] = 0.5f; // 50/50 blend

            var job = new SIMDGeneticOptimizations.SIMDTraitBlendingJob
            {
                parent1Traits = parent1Traits,
                parent2Traits = parent2Traits,
                blendFactors = blendFactors,
                offspringTraits = offspringTraits
            };

            // Act
            job.Execute(0);

            // Assert
            var result = offspringTraits[0];
            // Allow small variance due to mutation (±0.05)
            Assert.That(result.x, Is.InRange(0.45f, 0.55f), "Strength trait should average");
            Assert.That(result.y, Is.InRange(0.45f, 0.55f), "Vitality trait should average");
            Assert.That(result.z, Is.InRange(0.45f, 0.55f), "Agility trait should average");
            Assert.That(result.w, Is.InRange(0.45f, 0.55f), "Resilience trait should average");

            // Cleanup
            parent1Traits.Dispose();
            parent2Traits.Dispose();
            blendFactors.Dispose();
            offspringTraits.Dispose();
        }

        [Test]
        public void TraitBlending_WithParent1Dominant_FavorsParent1()
        {
            // Arrange
            var parent1Traits = new NativeArray<float4>(1, Allocator.Temp);
            var parent2Traits = new NativeArray<float4>(1, Allocator.Temp);
            var blendFactors = new NativeArray<float>(1, Allocator.Temp);
            var offspringTraits = new NativeArray<float4>(1, Allocator.Temp);

            parent1Traits[0] = new float4(1.0f, 1.0f, 1.0f, 1.0f);
            parent2Traits[0] = new float4(0.0f, 0.0f, 0.0f, 0.0f);
            blendFactors[0] = 0.2f; // 80% parent1, 20% parent2

            var job = new SIMDGeneticOptimizations.SIMDTraitBlendingJob
            {
                parent1Traits = parent1Traits,
                parent2Traits = parent2Traits,
                blendFactors = blendFactors,
                offspringTraits = offspringTraits
            };

            // Act
            job.Execute(0);

            // Assert
            var result = offspringTraits[0];
            // Should be closer to parent1 (1.0) than parent2 (0.0)
            Assert.That(result.x, Is.GreaterThan(0.6f), "Should favor parent1");
            Assert.That(result.y, Is.GreaterThan(0.6f), "Should favor parent1");

            // Cleanup
            parent1Traits.Dispose();
            parent2Traits.Dispose();
            blendFactors.Dispose();
            offspringTraits.Dispose();
        }

        [Test]
        public void TraitBlending_ClampsResultsToValidRange()
        {
            // Arrange
            var parent1Traits = new NativeArray<float4>(1, Allocator.Temp);
            var parent2Traits = new NativeArray<float4>(1, Allocator.Temp);
            var blendFactors = new NativeArray<float>(1, Allocator.Temp);
            var offspringTraits = new NativeArray<float4>(1, Allocator.Temp);

            parent1Traits[0] = new float4(1.0f, 1.0f, 1.0f, 1.0f);
            parent2Traits[0] = new float4(1.0f, 1.0f, 1.0f, 1.0f);
            blendFactors[0] = 1.0f;

            var job = new SIMDGeneticOptimizations.SIMDTraitBlendingJob
            {
                parent1Traits = parent1Traits,
                parent2Traits = parent2Traits,
                blendFactors = blendFactors,
                offspringTraits = offspringTraits
            };

            // Act
            job.Execute(0);

            // Assert - Even with mutation, should never exceed 1.0
            var result = offspringTraits[0];
            Assert.That(result.x, Is.LessThanOrEqualTo(1.0f), "Should clamp to max 1.0");
            Assert.That(result.y, Is.LessThanOrEqualTo(1.0f), "Should clamp to max 1.0");
            Assert.That(result.z, Is.LessThanOrEqualTo(1.0f), "Should clamp to max 1.0");
            Assert.That(result.w, Is.LessThanOrEqualTo(1.0f), "Should clamp to max 1.0");

            Assert.That(result.x, Is.GreaterThanOrEqualTo(0.0f), "Should clamp to min 0.0");
            Assert.That(result.y, Is.GreaterThanOrEqualTo(0.0f), "Should clamp to min 0.0");

            // Cleanup
            parent1Traits.Dispose();
            parent2Traits.Dispose();
            blendFactors.Dispose();
            offspringTraits.Dispose();
        }

        [Test]
        public void FitnessEvaluation_HighCompatibility_ProducesHighScore()
        {
            // Arrange
            var creatureTraits = new NativeArray<float4>(1, Allocator.Temp);
            var environmentalFactors = new NativeArray<float4>(1, Allocator.Temp);
            var fitnessWeights = new NativeArray<float4>(1, Allocator.Temp);
            var fitnessScores = new NativeArray<float>(1, Allocator.Temp);

            // Creature perfectly adapted to environment
            creatureTraits[0] = new float4(1.0f, 1.0f, 1.0f, 1.0f);
            environmentalFactors[0] = new float4(1.0f, 1.0f, 1.0f, 1.0f);
            fitnessWeights[0] = new float4(0.25f, 0.25f, 0.25f, 0.25f); // Equal importance

            var job = new SIMDGeneticOptimizations.SIMDFitnessEvaluationJob
            {
                creatureTraits = creatureTraits,
                environmentalFactors = environmentalFactors,
                fitnessWeights = fitnessWeights,
                fitnessScores = fitnessScores
            };

            // Act
            job.Execute(0);

            // Assert
            Assert.That(fitnessScores[0], Is.GreaterThan(0.5f), "High compatibility should produce high fitness");

            // Cleanup
            creatureTraits.Dispose();
            environmentalFactors.Dispose();
            fitnessWeights.Dispose();
            fitnessScores.Dispose();
        }

        [Test]
        public void FitnessEvaluation_LowCompatibility_ProducesLowScore()
        {
            // Arrange
            var creatureTraits = new NativeArray<float4>(1, Allocator.Temp);
            var environmentalFactors = new NativeArray<float4>(1, Allocator.Temp);
            var fitnessWeights = new NativeArray<float4>(1, Allocator.Temp);
            var fitnessScores = new NativeArray<float>(1, Allocator.Temp);

            // Creature poorly adapted to environment
            creatureTraits[0] = new float4(1.0f, 1.0f, 1.0f, 1.0f);
            environmentalFactors[0] = new float4(0.0f, 0.0f, 0.0f, 0.0f);
            fitnessWeights[0] = new float4(0.25f, 0.25f, 0.25f, 0.25f);

            var job = new SIMDGeneticOptimizations.SIMDFitnessEvaluationJob
            {
                creatureTraits = creatureTraits,
                environmentalFactors = environmentalFactors,
                fitnessWeights = fitnessWeights,
                fitnessScores = fitnessScores
            };

            // Act
            job.Execute(0);

            // Assert
            Assert.That(fitnessScores[0], Is.LessThan(0.3f), "Low compatibility should produce low fitness");

            // Cleanup
            creatureTraits.Dispose();
            environmentalFactors.Dispose();
            fitnessWeights.Dispose();
            fitnessScores.Dispose();
        }

        [Test]
        public void MatingCompatibility_SimilarTraits_ProducesLowScore()
        {
            // Arrange
            var creature1Traits = new NativeArray<float4>(1, Allocator.Temp);
            var creature2Traits = new NativeArray<float4>(1, Allocator.Temp);
            var creature1Generations = new NativeArray<int>(1, Allocator.Temp);
            var creature2Generations = new NativeArray<int>(1, Allocator.Temp);
            var compatibilityScores = new NativeArray<float>(1, Allocator.Temp);

            // Very similar creatures (less genetic diversity)
            creature1Traits[0] = new float4(0.5f, 0.5f, 0.5f, 0.5f);
            creature2Traits[0] = new float4(0.5f, 0.5f, 0.5f, 0.5f);
            creature1Generations[0] = 1;
            creature2Generations[0] = 1;

            var job = new SIMDGeneticOptimizations.SIMDMatingCompatibilityJob
            {
                creature1Traits = creature1Traits,
                creature2Traits = creature2Traits,
                creature1Generations = creature1Generations,
                creature2Generations = creature2Generations,
                compatibilityScores = compatibilityScores
            };

            // Act
            job.Execute(0);

            // Assert
            Assert.That(compatibilityScores[0], Is.LessThan(0.5f), "Similar traits should reduce compatibility");

            // Cleanup
            creature1Traits.Dispose();
            creature2Traits.Dispose();
            creature1Generations.Dispose();
            creature2Generations.Dispose();
            compatibilityScores.Dispose();
        }

        [Test]
        public void MatingCompatibility_DiverseTraits_ProducesHighScore()
        {
            // Arrange
            var creature1Traits = new NativeArray<float4>(1, Allocator.Temp);
            var creature2Traits = new NativeArray<float4>(1, Allocator.Temp);
            var creature1Generations = new NativeArray<int>(1, Allocator.Temp);
            var creature2Generations = new NativeArray<int>(1, Allocator.Temp);
            var compatibilityScores = new NativeArray<float>(1, Allocator.Temp);

            // Diverse creatures (good genetic diversity)
            creature1Traits[0] = new float4(0.2f, 0.3f, 0.4f, 0.5f);
            creature2Traits[0] = new float4(0.8f, 0.7f, 0.6f, 0.5f);
            creature1Generations[0] = 1;
            creature2Generations[0] = 5; // Different generations also boost score

            var job = new SIMDGeneticOptimizations.SIMDMatingCompatibilityJob
            {
                creature1Traits = creature1Traits,
                creature2Traits = creature2Traits,
                creature1Generations = creature1Generations,
                creature2Generations = creature2Generations,
                compatibilityScores = compatibilityScores
            };

            // Act
            job.Execute(0);

            // Assert
            Assert.That(compatibilityScores[0], Is.GreaterThan(0.3f), "Diverse traits should increase compatibility");

            // Cleanup
            creature1Traits.Dispose();
            creature2Traits.Dispose();
            creature1Generations.Dispose();
            creature2Generations.Dispose();
            compatibilityScores.Dispose();
        }
    }
}
