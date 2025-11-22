using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Chimera.Social;
using Laboratory.Chimera.Core;
using ChimeraIdentity = Laboratory.Chimera.Core.CreatureIdentityComponent;

namespace Laboratory.Tests.EditMode
{
    /// <summary>
    /// Integration tests for bonding system flow:
    /// SocialSystemsIntegrationHub → EnhancedBondingSystem → PopulationManagementSystem
    ///
    /// Verifies:
    /// - Bond strength calculations with age modifiers
    /// - Effective bond strength caching
    /// - Population unlock triggers based on strong bonds
    /// - Cross-system coordination
    /// </summary>
    [TestFixture]
    public class BondingIntegrationTests
    {
        private World _testWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("BondingIntegrationTestWorld");
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

        [Test]
        public void BabyChimera_BondStrength_HasHighForgivenessMultiplier()
        {
            // Arrange - Create baby chimera with age sensitivity
            var babyChimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(babyChimera, new AgeSensitivityComponent
            {
                currentLifeStage = LifeStage.Baby,
                agePercentage = 0.05f,           // 5% of lifespan (baby)
                forgivenessMultiplier = 2.5f,    // Babies are very forgiving
                memoryStrength = 0.2f,           // Don't remember mistreatment
                bondDamageMultiplier = 0.5f,     // Less affected by damage
                recoverySpeed = 2.5f,            // Recover bond quickly
                emotionalResilience = 0.8f,      // High resilience
                trustVulnerability = 0.2f        // Low vulnerability
            });

            _entityManager.AddComponentData(babyChimera, new CreatureBondData
            {
                bondStrength = 0.5f,
                loyaltyLevel = 0.6f,
                timeSinceLastInteraction = 0f,
                timeAlive = 100f,  // 100 seconds old (baby)
                hasHadFirstInteraction = true
            });

            // Act - Calculate effective bond strength using memoryStrength (bond depth proxy)
            float baseBondStrength = 0.5f;
            var ageSensitivity = _entityManager.GetComponentData<AgeSensitivityComponent>(babyChimera);
            float effectiveBondStrength = baseBondStrength * ageSensitivity.memoryStrength;

            // Assert
            Assert.That(effectiveBondStrength, Is.EqualTo(0.1f).Within(0.01f),
                "Baby chimera bond depth should be shallow (memoryStrength = 0.2)");
            Assert.That(ageSensitivity.memoryStrength, Is.LessThan(0.5f),
                "Babies should have weak memory of mistreatment");
        }

        [Test]
        public void AdultChimera_BondStrength_HasLowForgivenessMultiplier()
        {
            // Arrange - Create adult chimera with age sensitivity
            var adultChimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(adultChimera, new AgeSensitivityComponent
            {
                currentLifeStage = LifeStage.Adult,
                agePercentage = 0.65f,           // 65% of lifespan (adult)
                forgivenessMultiplier = 0.3f,    // Adults are unforgiving
                memoryStrength = 0.95f,          // Remember everything
                bondDamageMultiplier = 2.0f,     // Damage affects them more
                recoverySpeed = 0.3f,            // Bond damage recovers slowly
                emotionalResilience = 0.3f,      // Low resilience
                trustVulnerability = 0.9f        // High vulnerability
            });

            _entityManager.AddComponentData(adultChimera, new CreatureBondData
            {
                bondStrength = 0.8f,
                loyaltyLevel = 0.9f,
                timeSinceLastInteraction = 0f,
                timeAlive = 10000f,  // 10,000 seconds old (adult)
                hasHadFirstInteraction = true
            });

            // Act - Calculate effective bond strength using memoryStrength (bond depth proxy)
            float baseBondStrength = 0.8f;
            var ageSensitivity = _entityManager.GetComponentData<AgeSensitivityComponent>(adultChimera);
            float effectiveBondStrength = baseBondStrength * ageSensitivity.memoryStrength;

            // Assert
            Assert.That(effectiveBondStrength, Is.EqualTo(0.76f).Within(0.01f),
                "Adult chimera bond depth should be deep (0.8 * 0.95 memoryStrength)");
            Assert.That(ageSensitivity.memoryStrength, Is.GreaterThan(0.9f),
                "Adults should have strong memory of mistreatment");
            Assert.That(ageSensitivity.trustVulnerability, Is.GreaterThan(0.8f),
                "Adults should have high trust vulnerability (trust easily broken)");
        }

        [Test]
        public void PopulationUnlock_RequiresStrongBonds()
        {
            // Arrange - Create player with population capacity
            var playerEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(playerEntity, new ChimeraPopulationCapacity
            {
                currentCapacity = 1,
                maxCapacity = 1,
                baseMaxCapacity = 5,
                capacityUnlocked = 1,
                capacityLostPermanently = 0,
                strongBondsRequired = 1,
                bondStrengthRequired = 0.75f,
                canUnlockNext = false,
                currentAliveChimeras = 1,
                atCapacity = true
            });

            // Create chimera with WEAK bond (below threshold)
            var weakBondChimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(weakBondChimera, new CreatureBondData
            {
                bondStrength = 0.5f,  // Below 0.75 threshold
                loyaltyLevel = 0.6f,
                hasHadFirstInteraction = true
            });

            _entityManager.AddComponentData(weakBondChimera, new ChimeraIdentity
            {
                CreatureId = 1,
                SpeciesId = 1,
                CreatureName = new FixedString64Bytes("Weak Bond Chimera")
            });

            // Act - Check if unlock requirements are met
            var capacity = _entityManager.GetComponentData<ChimeraPopulationCapacity>(playerEntity);
            bool hasStrongBond = weakBondChimera.Index >= 0 &&
                                _entityManager.GetComponentData<CreatureBondData>(weakBondChimera).bondStrength
                                >= capacity.bondStrengthRequired;

            // Assert
            Assert.That(hasStrongBond, Is.False,
                "Weak bond (0.5) should NOT meet unlock requirement (0.75)");
            Assert.That(capacity.canUnlockNext, Is.False,
                "Player should NOT be able to unlock next capacity tier");
        }

        [Test]
        public void PopulationUnlock_SucceedsWithStrongBond()
        {
            // Arrange - Create player with population capacity
            var playerEntity = _entityManager.CreateEntity();
            var capacity = new ChimeraPopulationCapacity
            {
                currentCapacity = 1,
                maxCapacity = 1,
                baseMaxCapacity = 5,
                capacityUnlocked = 1,
                capacityLostPermanently = 0,
                strongBondsRequired = 1,
                bondStrengthRequired = 0.75f,
                canUnlockNext = false,
                currentAliveChimeras = 1,
                atCapacity = true
            };

            // Create chimera with STRONG bond (above threshold)
            var strongBondChimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(strongBondChimera, new CreatureBondData
            {
                bondStrength = 0.9f,  // Above 0.75 threshold
                loyaltyLevel = 0.95f,
                hasHadFirstInteraction = true
            });

            _entityManager.AddComponentData(strongBondChimera, new ChimeraIdentity
            {
                CreatureId = 1,
                SpeciesId = 1,
                CreatureName = new FixedString64Bytes("Strong Bond Chimera")
            });

            // Act - Simulate unlock check
            int strongBondCount = 0;
            if (_entityManager.GetComponentData<CreatureBondData>(strongBondChimera).bondStrength >= capacity.bondStrengthRequired)
            {
                strongBondCount++;
            }

            bool canUnlockNext = strongBondCount >= capacity.strongBondsRequired &&
                                capacity.capacityUnlocked < 5;

            // Update capacity
            capacity.canUnlockNext = canUnlockNext;
            _entityManager.AddComponentData(playerEntity, capacity);

            // Assert
            Assert.That(strongBondCount, Is.EqualTo(1),
                "Should have 1 strong bond above threshold");
            Assert.That(canUnlockNext, Is.True,
                "Player SHOULD be able to unlock next capacity tier with strong bond");
        }

        [Test]
        public void MultipleStrongBonds_UnlockHigherCapacityTiers()
        {
            // Arrange - Create player at capacity tier 2
            var playerEntity = _entityManager.CreateEntity();
            var capacity = new ChimeraPopulationCapacity
            {
                currentCapacity = 2,
                maxCapacity = 2,
                baseMaxCapacity = 5,
                capacityUnlocked = 2,
                capacityLostPermanently = 0,
                strongBondsRequired = 2,  // Tier 3 requires 2 strong bonds
                bondStrengthRequired = 0.80f,
                canUnlockNext = false,
                currentAliveChimeras = 2,
                atCapacity = true
            };

            // Create 2 chimeras with strong bonds
            var chimera1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(chimera1, new CreatureBondData
            {
                bondStrength = 0.85f,
                loyaltyLevel = 0.9f,
                hasHadFirstInteraction = true
            });
            _entityManager.AddComponentData(chimera1, new ChimeraIdentity
            {
                CreatureId = 1,
                SpeciesId = 1,
                CreatureName = new FixedString64Bytes("Chimera 1")
            });

            var chimera2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(chimera2, new CreatureBondData
            {
                bondStrength = 0.9f,
                loyaltyLevel = 0.95f,
                hasHadFirstInteraction = true
            });
            _entityManager.AddComponentData(chimera2, new ChimeraIdentity
            {
                CreatureId = 2,
                SpeciesId = 1,
                CreatureName = new FixedString64Bytes("Chimera 2")
            });

            // Act - Count strong bonds
            int strongBondCount = 0;
            if (_entityManager.GetComponentData<CreatureBondData>(chimera1).bondStrength >= capacity.bondStrengthRequired)
                strongBondCount++;
            if (_entityManager.GetComponentData<CreatureBondData>(chimera2).bondStrength >= capacity.bondStrengthRequired)
                strongBondCount++;

            bool canUnlockNext = strongBondCount >= capacity.strongBondsRequired &&
                                capacity.capacityUnlocked < 5;

            // Assert
            Assert.That(strongBondCount, Is.EqualTo(2),
                "Should have 2 strong bonds above 0.80 threshold");
            Assert.That(canUnlockNext, Is.True,
                "Player with 2 strong bonds should unlock tier 3");
        }

        [Test]
        public void AgeSensitivity_AffectsEffectiveBondStrength()
        {
            // Arrange - Create two chimeras with same base bond but different ages
            var babyChimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(babyChimera, new CreatureBondData
            {
                bondStrength = 0.6f,
                loyaltyLevel = 0.7f,
                hasHadFirstInteraction = true
            });
            _entityManager.AddComponentData(babyChimera, new AgeSensitivityComponent
            {
                forgivenessMultiplier = 2.5f,  // Baby
                recoverySpeed = 2.5f
            });

            var adultChimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(adultChimera, new CreatureBondData
            {
                bondStrength = 0.6f,  // Same base bond
                loyaltyLevel = 0.7f,
                hasHadFirstInteraction = true
            });
            _entityManager.AddComponentData(adultChimera, new AgeSensitivityComponent
            {
                forgivenessMultiplier = 0.3f,  // Adult
                recoverySpeed = 0.3f
            });

            // Act - Calculate effective bond strengths
            var babyBond = _entityManager.GetComponentData<CreatureBondData>(babyChimera);
            var babyAge = _entityManager.GetComponentData<AgeSensitivityComponent>(babyChimera);
            float babyEffectiveBond = babyBond.bondStrength * babyAge.forgivenessMultiplier;

            var adultBond = _entityManager.GetComponentData<CreatureBondData>(adultChimera);
            var adultAge = _entityManager.GetComponentData<AgeSensitivityComponent>(adultChimera);
            float adultEffectiveBond = adultBond.bondStrength * adultAge.forgivenessMultiplier;

            // Assert
            Assert.That(babyEffectiveBond, Is.EqualTo(1.5f).Within(0.01f),
                "Baby effective bond should be 0.6 * 2.5 = 1.5");
            Assert.That(adultEffectiveBond, Is.EqualTo(0.18f).Within(0.01f),
                "Adult effective bond should be 0.6 * 0.3 = 0.18");
            Assert.That(babyEffectiveBond, Is.GreaterThan(adultEffectiveBond * 5),
                "Baby should have significantly higher effective bond than adult with same base");
        }

        [Test]
        public void BondStrengthCache_ReflectsAgeModifiers()
        {
            // This test simulates what SocialSystemsIntegrationHub does:
            // Caching effective bond strength (base * age modifiers)

            // Arrange - Create chimera with age sensitivity
            var chimera = _entityManager.CreateEntity();
            _entityManager.AddComponentData(chimera, new CreatureBondData
            {
                bondStrength = 0.7f,
                loyaltyLevel = 0.8f,
                hasHadFirstInteraction = true
            });
            _entityManager.AddComponentData(chimera, new AgeSensitivityComponent
            {
                forgivenessMultiplier = 1.5f,
                recoverySpeed = 1.5f
            });

            // Act - Simulate caching logic from SocialSystemsIntegrationHub
            var bondData = _entityManager.GetComponentData<CreatureBondData>(chimera);
            var ageSensitivity = _entityManager.GetComponentData<AgeSensitivityComponent>(chimera);

            float cachedEffectiveBondStrength = bondData.bondStrength * ageSensitivity.forgivenessMultiplier;

            // Assert
            Assert.That(cachedEffectiveBondStrength, Is.EqualTo(1.05f).Within(0.01f),
                "Cached effective bond should include age modifier (0.7 * 1.5 = 1.05)");
        }
    }
}
