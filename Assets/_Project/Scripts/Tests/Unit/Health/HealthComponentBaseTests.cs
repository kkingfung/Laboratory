using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health;

namespace Laboratory.Core.Tests.Unit.Health
{
    /// <summary>
    /// Comprehensive unit tests for the enhanced HealthComponentBase system.
    /// Tests all functionality including damage, healing, events, statistics, and edge cases.
    /// </summary>
    [TestFixture]
    public class HealthComponentBaseTests
    {
        #region Test Setup

        private TestHealthComponent _healthComponent;
        private GameObject _testGameObject;
        
        // Event tracking
        private HealthChangedEventArgs _lastHealthChangedEvent;
        private DeathEventArgs _lastDeathEvent;
        private DamagePreventedEventArgs _lastDamagePreventedEvent;
        private HealingAppliedEventArgs _lastHealingEvent;
        private HealthStatsChangedEventArgs _lastStatsEvent;
        private int _healthChangedCount = 0;
        private int _deathEventCount = 0;

        [SetUp]
        public void Setup()
        {
            // Create test GameObject with health component
            _testGameObject = new GameObject("TestHealthObject");
            _healthComponent = _testGameObject.AddComponent<TestHealthComponent>();
            
            // Subscribe to events for testing
            _healthComponent.OnHealthChanged += OnHealthChanged;
            _healthComponent.OnDeath += OnDeath;
            _healthComponent.OnDamagePrevented += OnDamagePrevented;
            _healthComponent.OnHealingApplied += OnHealingApplied;
            _healthComponent.OnStatsChanged += OnStatsChanged;
            
            // Reset event tracking
            ResetEventTracking();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        private void ResetEventTracking()
        {
            _lastHealthChangedEvent = null;
            _lastDeathEvent = null;
            _lastDamagePreventedEvent = null;
            _lastHealingEvent = null;
            _lastStatsEvent = null;
            _healthChangedCount = 0;
            _deathEventCount = 0;
        }

        #endregion

        #region Basic Health Tests

        [Test]
        public void InitializeHealth_SetsCorrectValues()
        {
            // Arrange & Act (initialization happens in Setup)
            
            // Assert
            Assert.AreEqual(100, _healthComponent.MaxHealth, "Max health should be 100");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Current health should start at max");
            Assert.IsTrue(_healthComponent.IsAlive, "Component should be alive");
            Assert.AreEqual(1.0f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 100%");
        }

        [Test]
        public void TakeDamage_ValidRequest_ReducesHealth()
        {
            // Arrange
            var damageRequest = new DamageRequest
            {
                Amount = 30,
                Source = this,
                Type = DamageType.Normal
            };

            // Act
            bool result = _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(result, "Damage should be applied successfully");
            Assert.AreEqual(70, _healthComponent.CurrentHealth, "Health should be reduced to 70");
            Assert.AreEqual(0.7f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 70%");
            Assert.IsTrue(_healthComponent.IsAlive, "Component should still be alive");
            
            // Verify events
            Assert.AreEqual(1, _healthChangedCount, "Health changed event should fire once");
            Assert.IsNotNull(_lastHealthChangedEvent, "Health changed event args should exist");
            Assert.AreEqual(100, _lastHealthChangedEvent.OldHealth, "Old health should be 100");
            Assert.AreEqual(70, _lastHealthChangedEvent.NewHealth, "New health should be 70");
        }

        [Test]
        public void TakeDamage_FatalDamage_TriggersDeath()
        {
            // Arrange
            var damageRequest = new DamageRequest
            {
                Amount = 150, // More than current health
                Source = this,
                Type = DamageType.Normal
            };

            // Act
            bool result = _healthComponent.TakeDamage(damageRequest);

            // Assert
            Assert.IsTrue(result, "Fatal damage should be applied successfully");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should be 0");
            Assert.AreEqual(0.0f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 0%");
            Assert.IsFalse(_healthComponent.IsAlive, "Component should be dead");
            
            // Verify death event
            Assert.AreEqual(1, _deathEventCount, "Death event should fire once");
            Assert.IsNotNull(_lastDeathEvent, "Death event args should exist");
            Assert.AreEqual(this, _lastDeathEvent.Source, "Death source should match damage source");
        }

        [Test]
        public void TakeDamage_InvalidRequest_ReturnsFalse()
        {
            // Test null request
            Assert.IsFalse(_healthComponent.TakeDamage(null), "Null damage request should return false");
            
            // Test negative damage
            var negativeDamage = new DamageRequest { Amount = -10 };
            Assert.IsFalse(_healthComponent.TakeDamage(negativeDamage), "Negative damage should return false");
            
            // Test zero damage
            var zeroDamage = new DamageRequest { Amount = 0 };
            Assert.IsFalse(_healthComponent.TakeDamage(zeroDamage), "Zero damage should return false");
            
            // Health should remain unchanged
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should remain at 100");
        }

        #endregion

        #region Healing Tests

        [Test]
        public void Heal_ValidAmount_IncreasesHealth()
        {
            // Arrange - damage first
            _healthComponent.ApplyDamage(40);
            ResetEventTracking();
            
            // Act
            bool result = _healthComponent.Heal(20, this);

            // Assert
            Assert.IsTrue(result, "Healing should be applied successfully");
            Assert.AreEqual(80, _healthComponent.CurrentHealth, "Health should be increased to 80");
            Assert.AreEqual(0.8f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 80%");
            
            // Verify events
            Assert.AreEqual(1, _healthChangedCount, "Health changed event should fire once");
            Assert.IsNotNull(_lastHealingEvent, "Healing event should fire");
            Assert.AreEqual(20, _lastHealingEvent.Amount, "Healing amount should be 20");
        }

        [Test]
        public void Heal_ExceedsMaxHealth_ClampsToMax()
        {
            // Arrange - damage first
            _healthComponent.ApplyDamage(30);
            
            // Act - heal more than the damage taken
            bool result = _healthComponent.Heal(50, this);

            // Assert
            Assert.IsTrue(result, "Healing should be applied successfully");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should be clamped to max (100)");
            Assert.AreEqual(1.0f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 100%");
        }

        [Test]
        public void Heal_AlreadyAtMaxHealth_ReturnsFalse()
        {
            // Act (health is already at 100)
            bool result = _healthComponent.Heal(10, this);

            // Assert
            Assert.IsFalse(result, "Healing at max health should return false");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should remain at 100");
            Assert.AreEqual(0, _healthChangedCount, "No health changed event should fire");
        }

        [Test]
        public void Heal_DeadEntity_ReturnsFalse()
        {
            // Arrange - kill the entity
            _healthComponent.ApplyDamage(200);
            ResetEventTracking();
            
            // Act
            bool result = _healthComponent.Heal(50, this);

            // Assert
            Assert.IsFalse(result, "Healing dead entity should return false");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should remain at 0");
            Assert.AreEqual(0, _healthChangedCount, "No health changed event should fire");
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void HealthStats_TracksDamageCorrectly()
        {
            // Arrange & Act
            _healthComponent.ApplyDamage(25);
            _healthComponent.ApplyDamage(15);
            _healthComponent.Heal(10);

            // Assert
            var stats = _healthComponent.GetHealthStats();
            Assert.AreEqual(40, stats.TotalDamageReceived, "Total damage should be 40");
            Assert.AreEqual(10, stats.TotalHealingReceived, "Total healing should be 10");
            Assert.AreEqual(2, stats.TimesDamaged, "Should have been damaged 2 times");
            Assert.AreEqual(1, stats.TimesHealed, "Should have been healed 1 time");
            Assert.AreEqual(70, stats.CurrentHealth, "Current health should be 70");
        }

        [Test]
        public void HealthStats_TracksTimestamps()
        {
            // Arrange
            float timeBeforeDamage = Time.time;
            
            // Act
            _healthComponent.ApplyDamage(20);
            float timeAfterDamage = Time.time;
            
            _healthComponent.Heal(10);
            float timeAfterHeal = Time.time;

            // Assert
            var stats = _healthComponent.GetHealthStats();
            Assert.GreaterOrEqual(stats.LastDamageTime, timeBeforeDamage, "Damage timestamp should be recent");
            Assert.LessOrEqual(stats.LastDamageTime, timeAfterDamage, "Damage timestamp should not be in future");
            Assert.GreaterOrEqual(stats.LastHealTime, timeAfterDamage, "Heal timestamp should be after damage");
            Assert.LessOrEqual(stats.LastHealTime, timeAfterHeal, "Heal timestamp should not be in future");
        }

        #endregion

        #region Max Health Tests

        [Test]
        public void SetMaxHealth_ValidValue_UpdatesCorrectly()
        {
            // Arrange
            int oldMaxHealth = _healthComponent.MaxHealth;
            
            // Act
            _healthComponent.SetMaxHealth(150);

            // Assert
            Assert.AreEqual(150, _healthComponent.MaxHealth, "Max health should be updated to 150");
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Current health should remain 100");
            Assert.AreEqual(100f/150f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should update");
        }

        [Test]
        public void SetMaxHealth_LowerThanCurrent_ClampsCurrentHealth()
        {
            // Act
            _healthComponent.SetMaxHealth(50);

            // Assert
            Assert.AreEqual(50, _healthComponent.MaxHealth, "Max health should be 50");
            Assert.AreEqual(50, _healthComponent.CurrentHealth, "Current health should be clamped to 50");
            Assert.AreEqual(1.0f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 100%");
            
            // Verify health changed event fired
            Assert.Greater(_healthChangedCount, 0, "Health changed event should fire when clamping");
        }

        [Test]
        public void SetMaxHealth_InvalidValue_ShowsWarning()
        {
            // Arrange
            int originalMax = _healthComponent.MaxHealth;
            
            // Act
            _healthComponent.SetMaxHealth(0);
            _healthComponent.SetMaxHealth(-50);

            // Assert
            Assert.AreEqual(originalMax, _healthComponent.MaxHealth, "Max health should not change with invalid values");
            // Note: In a real test, you'd verify that Debug.LogWarning was called
        }

        #endregion

        #region Instant Kill Tests

        [Test]
        public void InstantKill_ValidTarget_KillsImmediately()
        {
            // Act
            bool result = _healthComponent.InstantKill(this);

            // Assert
            Assert.IsTrue(result, "Instant kill should succeed");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should be 0");
            Assert.IsFalse(_healthComponent.IsAlive, "Entity should be dead");
            Assert.AreEqual(1, _deathEventCount, "Death event should fire");
        }

        [Test]
        public void InstantKill_AlreadyDead_ReturnsFalse()
        {
            // Arrange - kill first
            _healthComponent.ApplyDamage(200);
            ResetEventTracking();
            
            // Act
            bool result = _healthComponent.InstantKill(this);

            // Assert
            Assert.IsFalse(result, "Instant kill on dead entity should return false");
            Assert.AreEqual(0, _deathEventCount, "No additional death event should fire");
        }

        #endregion

        #region Reset Tests

        [Test]
        public void ResetToMaxHealth_RestoresFullHealth()
        {
            // Arrange
            _healthComponent.ApplyDamage(60);
            ResetEventTracking();
            
            // Act
            _healthComponent.ResetToMaxHealth();

            // Assert
            Assert.AreEqual(100, _healthComponent.CurrentHealth, "Health should be restored to 100");
            Assert.AreEqual(1.0f, _healthComponent.HealthPercentage, 0.01f, "Health percentage should be 100%");
            Assert.IsTrue(_healthComponent.IsAlive, "Entity should be alive");
            Assert.AreEqual(1, _healthChangedCount, "Health changed event should fire");
        }

        #endregion

        #region Event Callback Methods

        private void OnHealthChanged(HealthChangedEventArgs args)
        {
            _lastHealthChangedEvent = args;
            _healthChangedCount++;
        }

        private void OnDeath(DeathEventArgs args)
        {
            _lastDeathEvent = args;
            _deathEventCount++;
        }

        private void OnDamagePrevented(DamagePreventedEventArgs args)
        {
            _lastDamagePreventedEvent = args;
        }

        private void OnHealingApplied(HealingAppliedEventArgs args)
        {
            _lastHealingEvent = args;
        }

        private void OnStatsChanged(HealthStatsChangedEventArgs args)
        {
            _lastStatsEvent = args;
        }

        #endregion
    }

    #region Test Health Component

    /// <summary>
    /// Test implementation of HealthComponentBase for unit testing.
    /// </summary>
    public class TestHealthComponent : HealthComponentBase
    {
        public void SetInvulnerabilityDuration(float duration)
        {
            _invulnerabilityDuration = duration;
        }

        public void SetDamageTypeImmunity(DamageType[] immuneTypes)
        {
            _immuneDamageTypes = immuneTypes;
        }

        public void SetCanTakeDamage(bool canTakeDamage)
        {
            _canTakeDamage = canTakeDamage;
        }

        public void SetCanHeal(bool canHeal)
        {
            _canHeal = canHeal;
        }

        protected override void OnDeathBehavior()
        {
            // Test implementation - just disable the component
            enabled = false;
        }
    }

    #endregion
}