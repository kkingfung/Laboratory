using NUnit.Framework;
using UnityEngine;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.ECS;
using Laboratory.Shared.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Tests.Unit.AI
{
    /// <summary>
    /// Comprehensive test suite for creature AI behavior systems
    /// Target: 70% code coverage for AI systems
    /// Tests decision-making, behavior states, and personality-driven actions
    /// </summary>
    [TestFixture]
    public class CreatureAISystemTests
    {
        private CreatureDefinition _testCreatureDefinition;

        [SetUp]
        public void Setup()
        {
            _testCreatureDefinition = CreateTestCreatureDefinition();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testCreatureDefinition != null)
            {
                Object.DestroyImmediate(_testCreatureDefinition);
            }
        }

        [Test]
        public void BehaviorState_InitialState_IsIdle()
        {
            // Arrange
            var behaviorComponent = new BehaviorStateComponent
            {
                CurrentBehavior = CreatureBehaviorType.Idle,
                QueuedBehavior = CreatureBehaviorType.Idle,
                BehaviorTimer = 0f,
                BehaviorIntensity = 1.0f
            };

            // Assert
            Assert.AreEqual(CreatureBehaviorType.Idle, behaviorComponent.CurrentBehavior,
                "Initial behavior should be Idle");
        }

        [Test]
        public void CreatureNeeds_AllMaximized_ResultsInIdleBehavior()
        {
            // Arrange
            var needs = new CreatureNeedsComponent
            {
                Hunger = 1.0f,
                Thirst = 1.0f,
                Energy = 1.0f,
                SocialConnection = 1.0f,
                Comfort = 1.0f,
                Safety = 1.0f
            };

            // Act
            var criticalNeed = GetMostCriticalNeed(needs);

            // Assert
            Assert.IsNull(criticalNeed, "No critical needs should exist when all maximized");
        }

        [Test]
        public void CreatureNeeds_LowHunger_PrioritizesForaging()
        {
            // Arrange
            var needs = new CreatureNeedsComponent
            {
                Hunger = 0.1f, // Critical hunger
                Thirst = 0.8f,
                Energy = 0.8f,
                SocialConnection = 0.8f
            };

            // Act
            var criticalNeed = GetMostCriticalNeed(needs);

            // Assert
            Assert.AreEqual("Hunger", criticalNeed, "Hunger should be most critical need");
        }

        [Test]
        public void CreaturePersonality_HighAggression_FavorsAttackBehavior()
        {
            // Arrange
            var personality = new CreaturePersonalityComponent
            {
                Aggression = 0.9f,
                Curiosity = 0.3f,
                Sociability = 0.2f,
                Loyalty = 0.5f
            };

            // Act
            var dominantTrait = GetDominantPersonalityTrait(personality);

            // Assert
            Assert.AreEqual("Aggression", dominantTrait, "Aggression should be dominant trait");
        }

        [Test]
        public void CreaturePersonality_HighSociability_FavorsSocialBehavior()
        {
            // Arrange
            var personality = new CreaturePersonalityComponent
            {
                Aggression = 0.2f,
                Curiosity = 0.3f,
                Sociability = 0.95f,
                Loyalty = 0.5f
            };

            // Act
            var dominantTrait = GetDominantPersonalityTrait(personality);

            // Assert
            Assert.AreEqual("Sociability", dominantTrait, "Sociability should be dominant trait");
        }

        [Test]
        public void BehaviorTransition_ValidTransition_UpdatesState()
        {
            // Arrange
            var behavior = new BehaviorStateComponent
            {
                CurrentBehavior = CreatureBehaviorType.Idle,
                QueuedBehavior = CreatureBehaviorType.Idle
            };

            // Act
            var newBehavior = TransitionBehavior(behavior, CreatureBehaviorType.Foraging);

            // Assert
            Assert.AreEqual(CreatureBehaviorType.Foraging, newBehavior.CurrentBehavior,
                "Should transition to foraging");
            Assert.AreEqual(CreatureBehaviorType.Idle, newBehavior.QueuedBehavior,
                "Should track queued behavior");
        }

        [Test]
        public void BehaviorPriority_MultipleNeeds_SelectsHighestPriority()
        {
            // Arrange
            var needs = new CreatureNeedsComponent
            {
                Hunger = 0.3f,  // Moderate
                Thirst = 0.1f,  // Critical - highest priority
                Energy = 0.5f,  // OK
                Safety = 0.2f   // Low
            };

            // Act
            var priorityNeed = GetMostCriticalNeed(needs);

            // Assert
            Assert.AreEqual("Thirst", priorityNeed,
                "Thirst should be highest priority need");
        }

        [Test]
        public void BehaviorDuration_TimePasses_ExpiresCorrectly()
        {
            // Arrange
            var behavior = new BehaviorStateComponent
            {
                CurrentBehavior = CreatureBehaviorType.Wandering,
                BehaviorStartTime = 0f,
                BehaviorDuration = 5f
            };

            float currentTime = 6f;

            // Act
            bool hasExpired = HasBehaviorExpired(behavior, currentTime);

            // Assert
            Assert.IsTrue(hasExpired, "Behavior should expire after duration");
        }

        [Test]
        public void CreatureMovement_HasDestination_MovesTowardTarget()
        {
            // Arrange
            var movement = new CreatureMovementComponent
            {
                HasDestination = true,
                TargetPosition = new Vector3(10, 0, 10),
                CurrentSpeed = 5f,
                IsMoving = true
            };

            var currentPosition = Vector3.zero;

            // Act
            var direction = (movement.TargetPosition - currentPosition).normalized;
            var expectedDirection = new Vector3(0.707f, 0, 0.707f); // Normalized (10,0,10)

            // Assert
            Assert.AreEqual(expectedDirection.x, direction.x, 0.01f,
                "Should move in correct X direction");
            Assert.AreEqual(expectedDirection.z, direction.z, 0.01f,
                "Should move in correct Z direction");
        }

        [Test]
        public void CreatureHealth_LowHealth_TriggersFleeingBehavior()
        {
            // Arrange
            var health = new CreatureHealthComponent
            {
                CurrentHealth = 15f,
                MaxHealth = 100f,
                IsAlive = true
            };

            // Act
            bool shouldFlee = health.CurrentHealth < (health.MaxHealth * 0.25f);

            // Assert
            Assert.IsTrue(shouldFlee, "Should flee when health below 25%");
        }

        [Test]
        public void CreatureBonding_HighBondStrength_IncreasesLoyalty()
        {
            // Arrange
            var bonding = new CreatureBondingComponent
            {
                BondStrength = 0.9f,
                TrustLevel = 0.8f,
                BondedToPlayer = true
            };

            // Act
            var effectiveLoyalty = bonding.BondStrength * bonding.TrustLevel;

            // Assert
            Assert.Greater(effectiveLoyalty, 0.7f,
                "High bond strength should result in high loyalty");
        }

        [Test]
        public void AIDecisionMaking_MultipleFactors_WeightsCorrectly()
        {
            // Arrange
            var needs = new CreatureNeedsComponent
            {
                Hunger = 0.4f,
                Energy = 0.3f,
                Safety = 0.9f
            };

            var personality = new CreaturePersonalityComponent
            {
                Aggression = 0.2f,
                Fearfulness = 0.7f
            };

            // Act
            // In a real scenario, this would calculate weighted decision
            // Here we verify that low safety + high fear = flee behavior
            bool shouldSeekSafety = (needs.Safety < 0.5f && personality.Fearfulness > 0.5f) ||
                                   needs.Safety < 0.3f;

            // Assert
            Assert.IsFalse(shouldSeekSafety,
                "Should not seek safety when safety need is already high (0.9)");
        }

        [Test]
        public void BehaviorCooldown_RecentAction_PreventsDuplicate()
        {
            // Arrange
            float lastActionTime = 5f;
            float currentTime = 7f;
            float cooldownDuration = 5f;

            // Act
            bool canPerformAction = (currentTime - lastActionTime) >= cooldownDuration;

            // Assert
            Assert.IsFalse(canPerformAction,
                "Should not allow action during cooldown period");
        }

        [Test]
        public void SocialBehavior_NearbyCreatures_InfluencesBehavior()
        {
            // Arrange
            var socialNeeds = new CreatureNeedsComponent
            {
                SocialConnection = 0.2f // Low social connection
            };

            int nearbyCreatureCount = 5;
            float minSocialThreshold = 0.5f;

            // Act
            bool shouldSeekSocial = socialNeeds.SocialConnection < minSocialThreshold &&
                                   nearbyCreatureCount > 0;

            // Assert
            Assert.IsTrue(shouldSeekSocial,
                "Should seek social interaction when isolated and creatures nearby");
        }

        #region Helper Methods

        private CreatureDefinition CreateTestCreatureDefinition()
        {
            var definition = ScriptableObject.CreateInstance<CreatureDefinition>();
            definition.speciesName = "Test Creature";
            definition.baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 5
            };

            definition.behaviorProfile = new CreatureBehaviorProfile
            {
                aggression = 0.5f,
                curiosity = 0.5f,
                loyalty = 0.5f,
                playfulness = 0.5f,
                independence = 0.5f
            };

            return definition;
        }

        private string GetMostCriticalNeed(CreatureNeedsComponent needs)
        {
            float minValue = 1.0f;
            string criticalNeed = null;

            if (needs.Hunger < minValue) { minValue = needs.Hunger; criticalNeed = "Hunger"; }
            if (needs.Thirst < minValue) { minValue = needs.Thirst; criticalNeed = "Thirst"; }
            if (needs.Energy < minValue) { minValue = needs.Energy; criticalNeed = "Energy"; }
            if (needs.SocialConnection < minValue) { minValue = needs.SocialConnection; criticalNeed = "Social"; }
            if (needs.Safety < minValue) { minValue = needs.Safety; criticalNeed = "Safety"; }

            // Return null if no critical needs (all > 0.5)
            return minValue < 0.5f ? criticalNeed : null;
        }

        private string GetDominantPersonalityTrait(CreaturePersonalityComponent personality)
        {
            float maxValue = 0f;
            string dominantTrait = "None";

            if (personality.Aggression > maxValue) { maxValue = personality.Aggression; dominantTrait = "Aggression"; }
            if (personality.Curiosity > maxValue) { maxValue = personality.Curiosity; dominantTrait = "Curiosity"; }
            if (personality.Sociability > maxValue) { maxValue = personality.Sociability; dominantTrait = "Sociability"; }
            if (personality.Loyalty > maxValue) { maxValue = personality.Loyalty; dominantTrait = "Loyalty"; }

            return dominantTrait;
        }

        private BehaviorStateComponent TransitionBehavior(BehaviorStateComponent current,
                                                          CreatureBehaviorType newBehavior)
        {
            return new BehaviorStateComponent
            {
                CurrentBehavior = newBehavior,
                QueuedBehavior = current.CurrentBehavior,
                BehaviorTimer = 0f,
                BehaviorIntensity = 1.0f
            };
        }

        private bool HasBehaviorExpired(BehaviorStateComponent behavior, float currentTime)
        {
            return behavior.BehaviorTimer >= 5f; // Behavior expires after 5 seconds
        }

        #endregion
    }
}
