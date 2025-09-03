using System;
using UnityEngine;
using NUnit.Framework;
using Laboratory.Core.Health;
using Laboratory.Core.Systems;
using Laboratory.Core.Health.Components;

#nullable enable

namespace Laboratory.Tests.Unit.Core
{
    /// <summary>
    /// Unit tests for Health System functionality.
    /// Tests health component registration, damage/healing application, and events.
    /// </summary>
    public class HealthSystemTests
    {
        private MockHealthSystem? _healthSystem;
        private MockHealthComponent? _healthComponent;

        [SetUp]
        public void SetUp()
        {
            _healthSystem = new MockHealthSystem();
            _healthComponent = new MockHealthComponent();
        }

        [TearDown]
        public void TearDown()
        {
            _healthSystem?.Dispose();
            _healthSystem = null;
            _healthComponent = null;
        }

        #region Registration Tests

        [Test]
        public void RegisterHealthComponent_AddsToSystem()
        {
            // Act
            _healthSystem!.RegisterHealthComponent(_healthComponent!);

            // Assert
            var components = _healthSystem.GetAllHealthComponents();
            Assert.AreEqual(1, components.Count);
            Assert.Contains(_healthComponent, components);
        }

        [Test]
        public void UnregisterHealthComponent_RemovesFromSystem()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);

            // Act
            _healthSystem.UnregisterHealthComponent(_healthComponent);

            // Assert
            var components = _healthSystem.GetAllHealthComponents();
            Assert.AreEqual(0, components.Count);
        }

        [Test]
        public void RegisterHealthComponent_DuplicateComponent_DoesNotAddTwice()
        {
            // Act
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            _healthSystem.RegisterHealthComponent(_healthComponent); // Add same component twice

            // Assert
            var components = _healthSystem.GetAllHealthComponents();
            Assert.AreEqual(1, components.Count);
        }

        #endregion

        #region Damage System Tests

        [Test]
        public void ApplyDamage_ValidTarget_ReturnsTrueAndAppliesDamage()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            var damageRequest = new DamageRequest
            {
                Amount = 25,
                Type = DamageType.Normal,
                Source = null
            };

            // Act
            var result = _healthSystem.ApplyDamage(_healthComponent, damageRequest);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(75, _healthComponent.CurrentHealth); // 100 - 25 = 75
        }

        [Test]
        public void ApplyDamage_UnregisteredTarget_ReturnsFalse()
        {
            // Arrange
            var damageRequest = new DamageRequest
            {
                Amount = 25,
                Type = DamageType.Normal,
                Source = null
            };

            // Act
            var result = _healthSystem!.ApplyDamage(_healthComponent!, damageRequest);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(100, _healthComponent.CurrentHealth); // No damage applied
        }

        [Test]
        public void ApplyDamage_TriggersOnDamageAppliedEvent()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            var damageRequest = new DamageRequest
            {
                Amount = 25,
                Type = DamageType.Normal,
                Source = null
            };

            var eventTriggered = false;
            IHealthComponent? eventTarget = null;
            DamageRequest? eventRequest = null;

            _healthSystem.OnDamageApplied += (target, request) =>
            {
                eventTriggered = true;
                eventTarget = target;
                eventRequest = request;
            };

            // Act
            _healthSystem.ApplyDamage(_healthComponent, damageRequest);

            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(_healthComponent, eventTarget);
            Assert.AreEqual(25, eventRequest?.Amount);
        }

        #endregion

        #region Healing System Tests

        [Test]
        public void ApplyHealing_ValidTarget_ReturnsTrueAndAppliesHealing()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            _healthComponent.SetCurrentHealth(50); // Set to damaged state

            // Act
            var result = _healthSystem.ApplyHealing(_healthComponent, 25);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(75, _healthComponent.CurrentHealth); // 50 + 25 = 75
        }

        [Test]
        public void ApplyHealing_ExceedsMaxHealth_ClampsToMax()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            _healthComponent.SetCurrentHealth(90);

            // Act
            var result = _healthSystem.ApplyHealing(_healthComponent, 25);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(100, _healthComponent.CurrentHealth); // Clamped to max
        }

        [Test]
        public void ApplyHealing_TriggersOnHealingAppliedEvent()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            _healthComponent.SetCurrentHealth(50);

            var eventTriggered = false;
            IHealthComponent? eventTarget = null;
            int eventAmount = 0;

            _healthSystem.OnHealingApplied += (target, amount) =>
            {
                eventTriggered = true;
                eventTarget = target;
                eventAmount = amount;
            };

            // Act
            _healthSystem.ApplyHealing(_healthComponent, 25);

            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(_healthComponent, eventTarget);
            Assert.AreEqual(25, eventAmount);
        }

        #endregion

        #region Death Tests

        [Test]
        public void ApplyDamage_CausesDeath_TriggersOnComponentDeathEvent()
        {
            // Arrange
            _healthSystem!.RegisterHealthComponent(_healthComponent!);
            var damageRequest = new DamageRequest
            {
                Amount = 150, // More than current health
                Type = DamageType.Normal,
                Source = null
            };

            var eventTriggered = false;
            IHealthComponent? eventTarget = null;

            _healthSystem.OnComponentDeath += (target) =>
            {
                eventTriggered = true;
                eventTarget = target;
            };

            // Act
            _healthSystem.ApplyDamage(_healthComponent, damageRequest);

            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(_healthComponent, eventTarget);
            Assert.IsFalse(_healthComponent.IsAlive);
        }

        #endregion
    }

    #region Mock Implementations

    /// <summary>
    /// Mock implementation of IHealthSystem for testing.
    /// </summary>
    public class MockHealthSystem : IHealthSystem
    {
        private readonly System.Collections.Generic.List<IHealthComponent> _components = new();

        public event Action<IHealthComponent, DamageRequest>? OnDamageApplied;
        public event Action<IHealthComponent, int>? OnHealingApplied;
        public event Action<IHealthComponent>? OnComponentDeath;

        public void RegisterHealthComponent(IHealthComponent healthComponent)
        {
            if (!_components.Contains(healthComponent))
            {
                _components.Add(healthComponent);
            }
        }

        public void UnregisterHealthComponent(IHealthComponent healthComponent)
        {
            _components.Remove(healthComponent);
        }

        public bool ApplyDamage(IHealthComponent target, DamageRequest damageRequest)
        {
            if (!_components.Contains(target))
                return false;

            var success = target.TakeDamage(damageRequest);
            if (success)
            {
                OnDamageApplied?.Invoke(target, damageRequest);
                
                if (!target.IsAlive)
                {
                    OnComponentDeath?.Invoke(target);
                }
            }

            return success;
        }

        public bool ApplyHealing(IHealthComponent target, int amount, object? source = null)
        {
            if (!_components.Contains(target))
                return false;

            var success = target.Heal(amount, source);
            if (success)
            {
                OnHealingApplied?.Invoke(target, amount);
            }

            return success;
        }

        public System.Collections.Generic.IReadOnlyList<IHealthComponent> GetAllHealthComponents()
        {
            return _components.AsReadOnly();
        }

        public void Dispose()
        {
            _components.Clear();
            OnDamageApplied = null;
            OnHealingApplied = null;
            OnComponentDeath = null;
        }
    }

    /// <summary>
    /// Mock implementation of IHealthComponent for testing.
    /// </summary>
    public class MockHealthComponent : IHealthComponent
    {
        public int CurrentHealth { get; private set; } = 100;
        public int MaxHealth { get; private set; } = 100;
        public bool IsAlive => CurrentHealth > 0;
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
        public GameObject? GameObject => null;

        public event Action<HealthChangedEventArgs>? OnHealthChanged;
        public event Action<DeathEventArgs>? OnDeath;

        public void SetCurrentHealth(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        }

        public void SetMaxHealth(int maxHealth)
        {
            MaxHealth = maxHealth;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }

        public void ResetToMaxHealth()
        {
            int oldHealth = CurrentHealth;
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(new HealthChangedEventArgs(oldHealth, CurrentHealth, this));
        }

        public bool TakeDamage(DamageRequest damageRequest)
        {
            if (!IsAlive) return false;

            int oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damageRequest.Amount);
            
            OnHealthChanged?.Invoke(new HealthChangedEventArgs(oldHealth, CurrentHealth, this));

            if (!IsAlive)
            {
                OnDeath?.Invoke(new DeathEventArgs(this, damageRequest.Source));
            }

            return true;
        }

        public bool Heal(int amount, object? source = null)
        {
            if (!IsAlive) return false;

            int oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            
            OnHealthChanged?.Invoke(new HealthChangedEventArgs(oldHealth, CurrentHealth, source));

            return CurrentHealth != oldHealth; // Return true if healing was applied
        }

        public bool CanTakeDamage(DamageRequest damageRequest)
        {
            return IsAlive && damageRequest.Amount > 0;
        }

        public bool CanHeal(int amount)
        {
            return IsAlive && CurrentHealth < MaxHealth && amount > 0;
        }
    }

    #endregion
}