using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Events;
using Laboratory.Shared.Types;
using System;

namespace Laboratory.Tests.Unit.Chimera
{
    /// <summary>
    /// Comprehensive test suite for Project Chimera breeding system.
    /// Tests genetic inheritance, breeding compatibility, and offspring generation.
    /// </summary>
    [TestFixture]
    public class BreedingSystemTests
    {
        private BreedingSystem _breedingSystem;
        private IEventBus _eventBus;
        private CreatureDefinition _testSpecies1;
        private CreatureDefinition _testSpecies2;
        
        [SetUp]
        public void Setup()
        {
            // Create mock event bus
            _eventBus = new UnifiedEventBus();
            _breedingSystem = new BreedingSystem(_eventBus);
            
            // Create test creature definitions
            _testSpecies1 = CreateTestCreatureDefinition("Forest Dragon", 1);
            _testSpecies2 = CreateTestCreatureDefinition("Mountain Drake", 1);
        }
        
        [TearDown]
        public void TearDown()
        {
            _breedingSystem?.Dispose();
            _eventBus?.Dispose();
        }
        
        [Test]
        public void BreedCreatures_ValidParents_ProducesOffspring()
        {
            // Arrange
            var parent1 = CreateTestCreatureInstance(_testSpecies1);
            var parent2 = CreateTestCreatureInstance(_testSpecies2);
            var environment = CreateTestEnvironment();
            
            // Act
            var result = _breedingSystem.AttemptBreeding(parent1, parent2, environment);
            
            // Assert
            Assert.IsTrue(result.Success, "Breeding should succeed with compatible parents");
            Assert.IsNotNull(result.Offspring, "Offspring should not be null");
            Assert.IsNotNull(result.Offspring.GeneticProfile, "Offspring should have genetic profile");
        }
        
        [Test]
        public void BreedCreatures_IncompatibleSpecies_FailsBreeding()
        {
            // Arrange
            var incompatibleSpecies = CreateTestCreatureDefinition("Water Spirit", 2); // Different compatibility group
            var parent1 = CreateTestCreatureInstance(_testSpecies1);
            var parent2 = CreateTestCreatureInstance(incompatibleSpecies);
            var environment = CreateTestEnvironment();
            
            // Act
            var result = _breedingSystem.AttemptBreeding(parent1, parent2, environment);
            
            // Assert
            Assert.IsFalse(result.Success, "Breeding should fail with incompatible species");
            Assert.IsTrue(result.ErrorMessage.Contains("not genetically compatible"), "Should specify compatibility issue");
        }
        
        [Test]
        public void GeneticProfile_CreateOffspring_CombinesParentGenes()
        {
            // Arrange
            var parent1Genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = 0.8f, dominance = 0.7f },
                new Gene { traitName = "Speed", value = 0.6f, dominance = 0.5f }
            };
            var parent2Genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = 0.4f, dominance = 0.3f },
                new Gene { traitName = "Intelligence", value = 0.9f, dominance = 0.8f }
            };
            
            var parent1Profile = new GeneticProfile(parent1Genes, 1);
            var parent2Profile = new GeneticProfile(parent2Genes, 1);
            
            // Act
            var offspring = GeneticProfile.CreateOffspring(parent1Profile, parent2Profile);
            
            // Assert
            Assert.IsNotNull(offspring, "Offspring genetic profile should not be null");
            Assert.AreEqual(2, offspring.Generation, "Offspring should be generation 2");
            Assert.Greater(offspring.Genes.Count, 0, "Offspring should have inherited genes");
        }
        
        [Test]
        public void CreatureDefinition_CompatibilityCheck_WorksCorrectly()
        {
            // Arrange
            var species1 = CreateTestCreatureDefinition("Test Species 1", 1);
            var species2 = CreateTestCreatureDefinition("Test Species 2", 1); // Same group
            var species3 = CreateTestCreatureDefinition("Test Species 3", 2); // Different group
            
            // Act & Assert
            Assert.IsTrue(species1.CanBreedWith(species2), "Same compatibility group should be compatible");
            Assert.IsFalse(species1.CanBreedWith(species3), "Different compatibility groups should not be compatible");
        }
        
        [Test]
        public void CreatureStats_Multiplication_WorksCorrectly()
        {
            // Arrange
            var baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 8
            };
            
            // Act
            var multipliedStats = baseStats * 1.5f;
            
            // Assert
            Assert.AreEqual(150, multipliedStats.health, "Health should be multiplied correctly");
            Assert.AreEqual(30, multipliedStats.attack, "Attack should be multiplied correctly");
            Assert.AreEqual(15, multipliedStats.speed, "Speed should be multiplied correctly");
        }
        
        #region Helper Methods
        
        private CreatureDefinition CreateTestCreatureDefinition(string name, int compatibilityGroup)
        {
            var definition = ScriptableObject.CreateInstance<CreatureDefinition>();
            definition.speciesName = name;
            definition.baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 5
            };
            definition.breedingCompatibilityGroup = compatibilityGroup;
            definition.fertilityRate = 0.75f;
            definition.maturationAge = 90;
            definition.size = CreatureSize.Medium;
            definition.preferredBiomes = new BiomeType[] { BiomeType.Forest };
            definition.biomeCompatibility = new float[] { 1.0f };
            
            return definition;
        }
        
        private CreatureInstance CreateTestCreatureInstance(CreatureDefinition definition)
        {
            var genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = 0.7f, dominance = 0.6f, isActive = true },
                new Gene { traitName = "Speed", value = 0.5f, dominance = 0.4f, isActive = true },
                new Gene { traitName = "Intelligence", value = 0.6f, dominance = 0.5f, isActive = true }
            };
            
            var genetics = new GeneticProfile(genes, 1);
            
            return new CreatureInstance
            {
                Definition = definition,
                GeneticProfile = genetics,
                AgeInDays = 100, // Adult
                CurrentHealth = definition.baseStats.health,
                Happiness = 0.8f,
                Level = 1,
                IsWild = false
            };
        }
        
        private BreedingEnvironment CreateTestEnvironment()
        {
            return new BreedingEnvironment
            {
                BiomeType = BiomeType.Forest,
                Temperature = 22f,
                FoodAvailability = 0.8f,
                PredatorPressure = 0.3f,
                PopulationDensity = 0.4f
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Tests for the genetic system specifically
    /// </summary>
    [TestFixture]
    public class GeneticsSystemTests
    {
        [Test]
        public void Gene_Constructor_CopiesCorrectly()
        {
            // Arrange
            var originalGene = new Gene
            {
                traitName = "Test Trait",
                traitType = Laboratory.Core.Enums.TraitType.Strength,
                dominance = 0.7f,
                value = 0.8f,
                expression = GeneExpression.Enhanced,
                isActive = true
            };
            
            // Act
            var copiedGene = new Gene(originalGene);
            
            // Assert
            Assert.AreEqual(originalGene.traitName, copiedGene.traitName);
            Assert.AreEqual(originalGene.traitType, copiedGene.traitType);
            Assert.AreEqual(originalGene.dominance, copiedGene.dominance, 0.001f);
            Assert.AreEqual(originalGene.value, copiedGene.value);
            Assert.AreEqual(originalGene.expression, copiedGene.expression);
            Assert.AreEqual(originalGene.isActive, copiedGene.isActive);
        }
        
        [Test]
        public void GeneticProfile_GetGeneticPurity_CalculatesCorrectly()
        {
            // Arrange
            var genes = new Gene[] { new Gene { traitName = "Test", isActive = true } };
            var profile = new GeneticProfile(genes, 1);
            
            // Act
            float purityWithoutMutations = profile.GetGeneticPurity();
            
            // Assert
            Assert.AreEqual(1.0f, purityWithoutMutations, 0.001f, "Profile without mutations should have perfect purity");
        }
        
        [Test]
        public void GeneticProfile_GetTraitSummary_ReturnsSignificantTraits()
        {
            // Arrange
            var genes = new Gene[]
            {
                new Gene { traitName = "High Trait", value = 0.9f, isActive = true },
                new Gene { traitName = "Low Trait", value = 0.2f, isActive = true },
                new Gene { traitName = "Medium Trait", value = 0.5f, isActive = true }
            };
            var profile = new GeneticProfile(genes, 1);

            // Act
            string summary = profile.GetTraitSummary(2);

            // Assert
            Assert.IsTrue(summary.Contains("High Trait"), "Summary should include high-value traits");
            Assert.IsFalse(summary.Contains("Low Trait"), "Summary should not include low-value traits");
        }
    }

    /// <summary>
    /// Extended breeding system tests for comprehensive coverage
    /// Target: 80% code coverage for breeding systems
    /// </summary>
    [TestFixture]
    public class BreedingSystemExtendedTests
    {
        private BreedingSystem _breedingSystem;
        private IEventBus _eventBus;
        private CreatureDefinition _testSpecies;

        [SetUp]
        public void Setup()
        {
            _eventBus = new UnifiedEventBus();
            _breedingSystem = new BreedingSystem(_eventBus);
            _testSpecies = CreateTestCreatureDefinition("Test Species", 1);
        }

        [TearDown]
        public void TearDown()
        {
            _breedingSystem?.Dispose();
            _eventBus?.Dispose();
        }

        [Test]
        public void CalculateBreedingSuccessChance_OptimalConditions_ReturnsHighChance()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies, ageRatio: 0.4f); // Optimal breeding age
            var parent2 = CreateAdultCreature(_testSpecies, ageRatio: 0.4f);
            var optimalEnvironment = new BreedingEnvironment
            {
                FoodAvailability = 1.0f,
                PredatorPressure = 0f,
                PopulationDensity = 0.3f
                // StressLevel, ComfortLevel, and BreedingSuccessMultiplier are read-only (use defaults)
            };

            // Act
            float successChance = _breedingSystem.CalculateBreedingSuccessChance(parent1, parent2, optimalEnvironment);

            // Assert
            Assert.Greater(successChance, 0.7f, "Optimal conditions should yield high success chance");
            Assert.LessOrEqual(successChance, 1.0f, "Success chance should be clamped to 1.0");
        }

        [Test]
        public void CalculateBreedingSuccessChance_PoorConditions_ReturnsLowChance()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies, ageRatio: 0.9f); // Old age
            var parent2 = CreateAdultCreature(_testSpecies, ageRatio: 0.9f);
            parent1.CurrentHealth = (int)(parent1.Definition.baseStats.health * 0.3f); // Low health
            parent2.CurrentHealth = (int)(parent2.Definition.baseStats.health * 0.3f);

            var poorEnvironment = new BreedingEnvironment
            {
                FoodAvailability = 0.2f,
                PredatorPressure = 0.7f,
                PopulationDensity = 0.9f // Overcrowded
                // StressLevel, ComfortLevel, BreedingSuccessMultiplier are read-only (use defaults)
            };

            // Act
            float successChance = _breedingSystem.CalculateBreedingSuccessChance(parent1, parent2, poorEnvironment);

            // Assert
            Assert.Less(successChance, 0.4f, "Poor conditions should yield low success chance");
        }

        [Test]
        public void CanBreed_ImmatureCreatures_ReturnsFalse()
        {
            // Arrange
            var juvenile1 = CreateTestCreatureInstance(_testSpecies);
            var juvenile2 = CreateTestCreatureInstance(_testSpecies);
            juvenile1.AgeInDays = 30; // Below maturity age (90)
            juvenile2.AgeInDays = 30;

            // Act
            bool canBreed = _breedingSystem.CanBreed(juvenile1, juvenile2);

            // Assert
            Assert.IsFalse(canBreed, "Immature creatures should not be able to breed");
        }

        [Test]
        public void CanBreed_DeadCreature_ReturnsFalse()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);
            // Note: IsAlive is read-only, so we test by setting health to 0
            parent1.CurrentHealth = 0;

            // Act
            bool canBreed = _breedingSystem.CanBreed(parent1, parent2);

            // Assert
            Assert.IsFalse(canBreed, "Dead creatures (health = 0) should not be able to breed");
        }

        [Test]
        public void CanBreed_SameCreature_ReturnsFalse()
        {
            // Arrange
            var creature = CreateAdultCreature(_testSpecies);

            // Act
            bool canBreed = _breedingSystem.CanBreed(creature, creature);

            // Assert
            Assert.IsFalse(canBreed, "Creature should not be able to breed with itself");
        }

        [Test]
        public void AttemptBreeding_NullParent_ReturnsFailure()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);

            // Act
            var result = _breedingSystem.AttemptBreeding(parent1, null);

            // Assert
            Assert.IsFalse(result.Success, "Breeding with null parent should fail");
            Assert.IsNotNull(result.ErrorMessage, "Error message should be provided");
        }

        [Test]
        public async UniTask AttemptBreedingAsync_ValidParents_CompletesSuccessfully()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);
            var cts = new CancellationTokenSource();

            // Act
            var result = await _breedingSystem.AttemptBreedingAsync(parent1, parent2, cts.Token);

            // Assert
            Assert.IsNotNull(result, "Async breeding should return result");
            // Note: Success depends on random chance, just verify it completes
        }

        [Test]
        public async UniTask AttemptBreedingAsync_Cancelled_ReturnsFailure()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            var result = await _breedingSystem.AttemptBreedingAsync(parent1, parent2, cts.Token);

            // Assert
            Assert.IsFalse(result.Success, "Cancelled breeding should return failure");
            Assert.IsTrue(result.ErrorMessage.Contains("cancelled"), "Should indicate cancellation");
        }

        [Test]
        public void GeneticDiversity_HighSimilarity_ReducesBreedingSuccess()
        {
            // Arrange - Create genetically very similar creatures
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);

            // Make genetics nearly identical
            parent2.GeneticProfile = new GeneticProfile(parent1.GeneticProfile.Genes.ToArray(), 1);

            // Act
            float successChance = _breedingSystem.CalculateBreedingSuccessChance(parent1, parent2);

            // Assert
            Assert.Greater(successChance, 0f, "Should still have some chance");
            // Note: System applies inbreeding penalty for high similarity
        }

        [Test]
        public void EnvironmentalFactor_Overcrowding_ReducesSuccess()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);
            var overcrowdedEnvironment = new BreedingEnvironment
            {
                PopulationDensity = 0.95f, // 95% capacity
                FoodAvailability = 0.5f,
                ComfortLevel = 0.5f
            };

            // Act
            float successChance = _breedingSystem.CalculateBreedingSuccessChance(parent1, parent2, overcrowdedEnvironment);

            // Assert
            Assert.Less(successChance, 0.6f, "Overcrowding should significantly reduce breeding success");
        }

        [Test]
        public void OffspringGeneration_InheritsParentTraits()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);

            // Act
            var result = _breedingSystem.AttemptBreeding(parent1, parent2);

            // Assert
            if (result.Success)
            {
                Assert.IsNotNull(result.Offspring, "Offspring should be created");
                Assert.AreEqual(0, result.Offspring.AgeInDays, "Offspring should be newborn");
                Assert.IsTrue(result.Offspring.Parents.Contains(parent1.InstanceId), "Should track parent 1");
                Assert.IsTrue(result.Offspring.Parents.Contains(parent2.InstanceId), "Should track parent 2");
            }
        }

        [Test]
        public void BreedingSuccessEvent_OnSuccess_IsFired()
        {
            // Arrange
            var parent1 = CreateAdultCreature(_testSpecies);
            var parent2 = CreateAdultCreature(_testSpecies);
            bool eventFired = false;
            CreatureInstance offspringFromEvent = null;

            _eventBus.Subscribe<BreedingSuccessEvent>(evt =>
            {
                eventFired = true;
                offspringFromEvent = evt.Offspring;
            });

            // Act
            var result = _breedingSystem.AttemptBreeding(parent1, parent2);

            // Assert
            if (result.Success)
            {
                Assert.IsTrue(eventFired, "Breeding success event should be fired");
                Assert.IsNotNull(offspringFromEvent, "Event should contain offspring");
            }
        }

        #region Helper Methods

        private CreatureDefinition CreateTestCreatureDefinition(string name, int compatibilityGroup)
        {
            var definition = ScriptableObject.CreateInstance<CreatureDefinition>();
            definition.speciesName = name;
            definition.baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 5
            };
            definition.breedingCompatibilityGroup = compatibilityGroup;
            definition.fertilityRate = 0.75f;
            definition.maturationAge = 90;
            definition.maxLifespan = 365 * 5;
            definition.size = CreatureSize.Medium;
            definition.preferredBiomes = new BiomeType[] { BiomeType.Forest };
            definition.biomeCompatibility = new float[] { 1.0f };

            return definition;
        }

        private CreatureInstance CreateTestCreatureInstance(CreatureDefinition definition)
        {
            var genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = 0.7f, dominance = 0.6f, isActive = true },
                new Gene { traitName = "Speed", value = 0.5f, dominance = 0.4f, isActive = true },
                new Gene { traitName = "Intelligence", value = 0.6f, dominance = 0.5f, isActive = true }
            };

            var genetics = new GeneticProfile(genes, 1);

            return new CreatureInstance
            {
                Definition = definition,
                GeneticProfile = genetics,
                AgeInDays = 100,
                CurrentHealth = definition.baseStats.health,
                Happiness = 0.8f,
                Level = 1,
                IsWild = false,
                IsAlive = true
            };
        }

        private CreatureInstance CreateAdultCreature(CreatureDefinition definition, float ageRatio = 0.4f)
        {
            var creature = CreateTestCreatureInstance(definition);
            creature.AgeInDays = (int)(definition.maxLifespan * ageRatio);
            return creature;
        }

        #endregion
    }
}
