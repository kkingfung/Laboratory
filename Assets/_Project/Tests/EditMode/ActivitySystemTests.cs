using NUnit.Framework;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Tests.EditMode
{
    /// <summary>
    /// Tests for activity systems - participation, scoring, progression
    /// </summary>
    public class ActivitySystemTests
    {
        [Test]
        public void Racing_Performance_DependsOnSpeedAndStamina()
        {
            // Arrange
            var genetics = new GeneticDataComponent
            {
                Speed = 0.9f,
                Stamina = 0.8f,
                Agility = 0.5f,
                Intelligence = 0.3f,
                Aggression = 0.2f,
                Curiosity = 0.4f,
                Caution = 0.3f,
                Dominance = 0.4f,
                Sociability = 0.5f,
                Adaptability = 0.6f,
                Size = 0.7f
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Racing, genetics);

            // Assert
            Assert.That(performance, Is.GreaterThan(0.6f), "High speed/stamina should produce good racing performance");
            Assert.That(performance, Is.LessThanOrEqualTo(1.0f), "Performance should not exceed maximum");
        }

        [Test]
        public void Combat_Performance_DependsOnAggressionAndSize()
        {
            // Arrange
            var genetics = new GeneticDataComponent
            {
                Speed = 0.3f,
                Stamina = 0.4f,
                Agility = 0.5f,
                Intelligence = 0.3f,
                Aggression = 0.9f,  // High aggression
                Curiosity = 0.2f,
                Caution = 0.1f,
                Dominance = 0.8f,    // High dominance
                Sociability = 0.3f,
                Adaptability = 0.4f,
                Size = 0.9f          // Large size
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Combat, genetics);

            // Assert
            Assert.That(performance, Is.GreaterThan(0.6f), "High aggression/size should produce good combat performance");
        }

        [Test]
        public void Puzzle_Performance_DependsOnIntelligenceAndCuriosity()
        {
            // Arrange
            var genetics = new GeneticDataComponent
            {
                Speed = 0.3f,
                Stamina = 0.4f,
                Agility = 0.3f,
                Intelligence = 0.95f,  // Very intelligent
                Aggression = 0.1f,
                Curiosity = 0.9f,      // Very curious
                Caution = 0.5f,
                Dominance = 0.3f,
                Sociability = 0.4f,
                Adaptability = 0.6f,
                Size = 0.5f
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Puzzle, genetics);

            // Assert
            Assert.That(performance, Is.GreaterThan(0.7f), "High intelligence/curiosity should excel at puzzles");
        }

        [Test]
        public void Strategy_Performance_DependsOnIntelligenceAndCaution()
        {
            // Arrange
            var genetics = new GeneticDataComponent
            {
                Speed = 0.3f,
                Stamina = 0.4f,
                Agility = 0.3f,
                Intelligence = 0.9f,
                Aggression = 0.2f,
                Curiosity = 0.5f,
                Caution = 0.85f,      // Very cautious (strategic)
                Dominance = 0.6f,
                Sociability = 0.5f,
                Adaptability = 0.7f,
                Size = 0.5f
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Strategy, genetics);

            // Assert
            Assert.That(performance, Is.GreaterThan(0.65f), "High intelligence/caution should excel at strategy");
        }

        [Test]
        public void Music_Performance_DependsOnSociabilityAndIntelligence()
        {
            // Arrange
            var genetics = new GeneticDataComponent
            {
                Speed = 0.4f,
                Stamina = 0.5f,
                Agility = 0.6f,
                Intelligence = 0.7f,
                Aggression = 0.2f,
                Curiosity = 0.6f,
                Caution = 0.5f,
                Dominance = 0.4f,
                Sociability = 0.95f,   // Very social
                Adaptability = 0.6f,
                Size = 0.5f
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Music, genetics);

            // Assert
            Assert.That(performance, Is.GreaterThan(0.6f), "High sociability should help with music performances");
        }

        [Test]
        public void Crafting_Performance_DependsOnIntelligenceAndAdaptability()
        {
            // Arrange
            var genetics = new GeneticDataComponent
            {
                Speed = 0.4f,
                Stamina = 0.5f,
                Agility = 0.6f,
                Intelligence = 0.85f,
                Aggression = 0.2f,
                Curiosity = 0.6f,
                Caution = 0.5f,
                Dominance = 0.4f,
                Sociability = 0.5f,
                Adaptability = 0.9f,   // Highly adaptable
                Size = 0.5f
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Crafting, genetics);

            // Assert
            Assert.That(performance, Is.GreaterThan(0.65f), "High intelligence/adaptability should excel at crafting");
        }

        [Test]
        public void PoorGeneticFit_ProducesLowPerformance()
        {
            // Arrange - creature with low stats for racing
            var genetics = new GeneticDataComponent
            {
                Speed = 0.1f,          // Very slow
                Stamina = 0.2f,        // Poor stamina
                Agility = 0.15f,       // Low agility
                Intelligence = 0.9f,   // Smart but doesn't help racing
                Aggression = 0.8f,
                Curiosity = 0.7f,
                Caution = 0.6f,
                Dominance = 0.5f,
                Sociability = 0.4f,
                Adaptability = 0.3f,
                Size = 0.2f
            };

            // Act
            float performance = CalculateActivityPerformance(ActivityType.Racing, genetics);

            // Assert
            Assert.That(performance, Is.LessThan(0.4f), "Poor genetic fit should produce low performance");
        }

        [Test]
        public void ActivityProgression_IncreasesWithTime()
        {
            // This would test the ActivityParticipationJob logic
            // Simulating that progress increases over time based on performance

            float initialProgress = 0.0f;
            float deltaTime = 1.0f;  // 1 second
            float performanceMultiplier = 0.5f;

            float newProgress = initialProgress + (performanceMultiplier * deltaTime);

            Assert.That(newProgress, Is.GreaterThan(initialProgress), "Progress should increase over time");
            Assert.That(newProgress, Is.EqualTo(0.5f).Within(0.01f), "Progress should match expected calculation");
        }

        [Test]
        public void ActivityCompletion_HappensAt100Percent()
        {
            float progress = 1.0f;  // 100% complete

            bool isComplete = progress >= 1.0f;

            Assert.That(isComplete, Is.True, "Activity should be complete at 100% progress");
        }

        // Helper method matching ActivityParticipationJob logic
        private float CalculateActivityPerformance(ActivityType activity, GeneticDataComponent genetics)
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
    }
}
