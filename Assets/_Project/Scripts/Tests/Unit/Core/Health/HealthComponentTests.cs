using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.Health;
using Laboratory.Core.Health.Components;

namespace Laboratory.Tests.Unit.Core.Health
{
    /// <summary>
    /// Comprehensive unit tests for the Health Component system.
    /// Tests core health functionality including damage, healing, death, and event integration.
    /// </summary>
    public class HealthComponentTests
    {
        #region Test Setup

        private GameObject _testGameObject;
        private LocalHealthComponent _healthComponent;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("Test Health Entity");
            _healthComponent = _testGameObject.AddComponent<LocalHealthComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        #endregion

        #region Initialization Tests

        [Test]
        public void Health_InitializesWithCorrectValues()
        {
            // Arrange & Act
            _healthComponent.Initialize(100);

            // Assert
            Assert.AreEqual(100, _healthComponent.MaxHealth, "Max health should be set correctly");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Current health should start at max health");
            Assert.IsTrue(_healthComponent.IsAlive, "Entity should be alive after initialization");
        }

        [Test]
        public void Health_InitializesWithDefaultValues()
        {
            // Arrange & Act
            _healthComponent.Initialize();

            // Assert
            Assert.IsTrue(_healthComponent.MaxHealth > 0, "Max health should have a positive default value");
            Assert.AreEqual(_healthComponent.MaxHealth, _healthComponent.CurrentHealth, "Current health should start at max health");
            Assert.IsTrue(_healthComponent.IsAlive, "Entity should be alive after initialization");
        }

        #endregion

        #region Damage Tests

        [Test]
        public void TakeDamage_ValidDamage_ReducesHealth()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(25f, _testGameObject, DamageType.Physical);

            // Act
            bool result = _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(result, "Damage should be applied successfully");
            Assert.AreEqual(75, _healthComponent.CurrentHealth, "Health should be reduced by damage amount");
            Assert.IsTrue(_healthComponent.IsAlive, "Entity should still be alive");
        }

        [Test]
        public void TakeDamage_LethalDamage_KillsEntity()
        {
            // Arrange
            _healthComponent.Initialize(50);
            var damageRequest = new DamageRequest(60f, _testGameObject, DamageType.Physical);

            // Act
            bool result = _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(result, "Damage should be applied successfully");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should be reduced to zero");
            Assert.IsFalse(_healthComponent.IsAlive, "Entity should be dead");
        }

        [Test]
        public void TakeDamage_NullRequest_ReturnsFalse()
        {
            // Arrange
            _healthComponent.Initialize(100);

            // Act
            bool result = _healthComponent.TakeDamage(null);

            // Assert
            Assert.IsFalse(result, "Null damage request should be rejected");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should remain unchanged");
        }

        [Test]
        public void TakeDamage_NegativeDamage_ReturnsFalse()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(-10f, _testGameObject, DamageType.Physical);

            // Act
            bool result = _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsFalse(result, "Negative damage should be rejected");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should remain unchanged");
        }

        [Test]
        public void TakeDamage_DeadEntity_ReturnsFalse()
        {
            // Arrange
            _healthComponent.Initialize(50);
            var killDamage = new DamageRequest(50f, _testGameObject, DamageType.Physical);
            var additionalDamage = new DamageRequest(25f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(killDamage); // Kill the entity

            // Act
            bool result = _healthComponent.TakeDamage(additionalDamage);

            // Assert
            Assert.IsFalse(result, "Dead entity should not take additional damage");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should remain at zero");
        }

        #endregion

        #region Healing Tests

        [Test]
        public void Heal_ValidAmount_IncreasesHealth()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(30f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(damageRequest);

            // Act
            bool result = _healthComponent.Heal(20);

            // Assert
            Assert.IsTrue(result, "Healing should be applied successfully");
            Assert.AreEqual(90, _healthComponent.CurrentHealth, "Health should be increased by heal amount");
        }

        [Test]
        public void Heal_OverMaxHealth_ClampsToMaxHealth()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(20f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(damageRequest);

            // Act
            bool result = _healthComponent.Heal(50); // Heal more than damage taken

            // Assert
            Assert.IsTrue(result, "Healing should be applied successfully");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should be clamped to max health");
        }

        [Test]
        public void Heal_FullHealth_ReturnsFalse()
        {
            // Arrange
            _healthComponent.Initialize(100);

            // Act
            bool result = _healthComponent.Heal(20);

            // Assert
            Assert.IsFalse(result, "Healing at full health should be rejected");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should remain at max");
        }

        [Test]
        public void Heal_NegativeAmount_ReturnsFalse()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(20f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(damageRequest);

            // Act
            bool result = _healthComponent.Heal(-10);

            // Assert
            Assert.IsFalse(result, "Negative healing should be rejected");
            Assert.AreEqual(80, _healthComponent.CurrentHealth, "Health should remain unchanged");
        }

        [Test]
        public void Heal_DeadEntity_ReturnsFalse()
        {
            // Arrange
            _healthComponent.Initialize(50);
            var killDamage = new DamageRequest(50f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(killDamage);

            // Act
            bool result = _healthComponent.Heal(25);

            // Assert
            Assert.IsFalse(result, "Dead entity should not be healable");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should remain at zero");
        }

        #endregion

        #region Death and Kill Tests

        [Test]
        public void Kill_LivingEntity_KillsEntity()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var killer = new GameObject("Killer");

            // Act
            _healthComponent.Kill(killer, "Test kill reason");

            // Assert
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should be set to zero");
            Assert.IsFalse(_healthComponent.IsAlive, "Entity should be dead");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(killer);
        }

        [Test]
        public void ResetToMaxHealth_DeadEntity_RevivesEntity()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var killDamage = new DamageRequest(100f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(killDamage);

            // Act
            _healthComponent.ResetToMaxHealth();

            // Assert
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should be restored to max");
            Assert.IsTrue(_healthComponent.IsAlive, "Entity should be alive again");
        }

        #endregion

        #region Event Tests

        [Test]
        public void TakeDamage_TriggersHealthChangedEvent()
        {
            // Arrange
            _healthComponent.Initialize(100);
            bool eventTriggered = false;
            HealthChangedEventArgs capturedArgs = null;

            _healthComponent.OnHealthChanged += (args) => {
                eventTriggered = true;
                capturedArgs = args;
            };

            var damageRequest = new DamageRequest(25f, _testGameObject, DamageType.Physical);

            // Act
            _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(eventTriggered, "HealthChanged event should be triggered");
            Assert.IsNotNull(capturedArgs, "Event args should not be null");
            Assert.AreEqual(100, capturedArgs.OldHealth, "Old health should match initial value");
            Assert.AreEqual(75, capturedArgs.NewHealth, "New health should match damage result");
        }

        [Test]
        public void Heal_TriggersHealthChangedEvent()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(30f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(damageRequest);

            bool eventTriggered = false;
            HealthChangedEventArgs capturedArgs = null;

            _healthComponent.OnHealthChanged += (args) => {
                eventTriggered = true;
                capturedArgs = args;
            };

            // Act
            _healthComponent.Heal(20);

            // Assert
            Assert.IsTrue(eventTriggered, "HealthChanged event should be triggered");
            Assert.IsNotNull(capturedArgs, "Event args should not be null");
            Assert.AreEqual(70, capturedArgs.OldHealth, "Old health should match pre-heal value");
            Assert.AreEqual(90, capturedArgs.NewHealth, "New health should match heal result");
        }

        [Test]
        public void TakeDamage_LethalDamage_TriggersDeathEvent()
        {
            // Arrange
            _healthComponent.Initialize(50);
            bool deathEventTriggered = false;
            DeathEventArgs capturedArgs = null;

            _healthComponent.OnDeath += (args) => {
                deathEventTriggered = true;
                capturedArgs = args;
            };

            var damageRequest = new DamageRequest(60f, _testGameObject, DamageType.Physical);

            // Act
            _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(deathEventTriggered, "Death event should be triggered");
            Assert.IsNotNull(capturedArgs, "Death event args should not be null");
            Assert.AreEqual(_healthComponent, capturedArgs.Source, "Death event source should match health component");
        }

        [Test]
        public void TakeDamage_TriggersOnDamageTakenEvent()
        {
            // Arrange
            _healthComponent.Initialize(100);
            bool eventTriggered = false;
            DamageRequest capturedRequest = null;

            _healthComponent.OnDamageTaken += (request) => {
                eventTriggered = true;
                capturedRequest = request;
            };

            var damageRequest = new DamageRequest(25f, _testGameObject, DamageType.Physical);

            // Act
            _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(eventTriggered, "OnDamageTaken event should be triggered");
            Assert.IsNotNull(capturedRequest, "Captured damage request should not be null");
            Assert.AreEqual(damageRequest.Amount, capturedRequest.Amount, "Damage amounts should match");
            Assert.AreEqual(damageRequest.Type, capturedRequest.Type, "Damage types should match");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void CurrentHealth_NeverGoesNegative()
        {
            // Arrange
            _healthComponent.Initialize(10);
            var massiveDamage = new DamageRequest(1000f, _testGameObject, DamageType.Physical);

            // Act
            _healthComponent.TakeDamage(massiveDamage);

            // Assert
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should not go below zero");
            Assert.IsFalse(_healthComponent.IsAlive, "Entity should be dead");
        }

        [Test]
        public void HealthPercentage_CalculatesCorrectly()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var damageRequest = new DamageRequest(25f, _testGameObject, DamageType.Physical);

            // Act
            _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.AreEqual(0.75f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be calculated correctly");
        }

        [Test]
        public void HealthPercentage_DeadEntity_ReturnsZero()
        {
            // Arrange
            _healthComponent.Initialize(100);
            var killDamage = new DamageRequest(100f, _testGameObject, DamageType.Physical);
            _healthComponent.TakeDamage(killDamage);

            // Act & Assert
            Assert.AreEqual(0f, _healthComponent.HealthPercentage, "Dead entity health percentage should be zero");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Performance]
        public void TakeDamage_Performance_HandlesMultipleDamageQuickly()
        {
            // Arrange
            _healthComponent.Initialize(1000);
            const int damageCount = 100;
            var damageRequest = new DamageRequest(1f, _testGameObject, DamageType.Physical);

            // Act
            var startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < damageCount; i++)
            {
                _healthComponent.TakeDamage(damageRequest);
            }
            var endTime = Time.realtimeSinceStartup;

            // Assert
            var duration = endTime - startTime;
            Assert.Less(duration, 0.1f, "100 damage operations should complete in under 100ms");
            Assert.AreEqual(900, _healthComponent.CurrentHealth, "Health should be reduced correctly");
        }

        #endregion
    }
}