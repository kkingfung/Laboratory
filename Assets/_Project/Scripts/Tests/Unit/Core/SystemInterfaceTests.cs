using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NUnit.Framework;
using Laboratory.Core.Systems;
using Laboratory.Gameplay.Abilities;

#nullable enable

namespace Laboratory.Tests.Unit.Core
{
    /// <summary>
    /// Unit tests for System interfaces and their implementations.
    /// Tests IAbilitySystem and IHealthSystem integration patterns.
    /// </summary>
    public class SystemInterfaceTests
    {
        private MockAbilitySystem? _abilitySystem;
        private MockAbilityManager? _abilityManager;

        [SetUp]
        public void SetUp()
        {
            _abilitySystem = new MockAbilitySystem();
            _abilityManager = new MockAbilityManager();
        }

        [TearDown]
        public void TearDown()
        {
            _abilitySystem?.Dispose();
            _abilitySystem = null;
            _abilityManager = null;
        }

        #region IAbilitySystem Tests

        [Test]
        public void RegisterAbilityManager_AddsToSystem()
        {
            // Act
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);

            // Assert
            var managers = _abilitySystem.GetAllAbilityManagers();
            Assert.AreEqual(1, managers.Count);
            Assert.IsTrue(managers.Any(m => m == _abilityManager));
        }

        [Test]
        public void UnregisterAbilityManager_RemovesFromSystem()
        {
            // Arrange
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);

            // Act
            _abilitySystem.UnregisterAbilityManager(_abilityManager!);

            // Assert
            var managers = _abilitySystem.GetAllAbilityManagers();
            Assert.AreEqual(0, managers.Count);
        }

        [Test]
        public void TryActivateAbility_ValidAbility_ReturnsTrue()
        {
            // Arrange
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);
            _abilityManager!.AddAbility(0); // Add ability at index 0

            // Act
            var result = _abilitySystem.TryActivateAbility(_abilityManager!, 0);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(_abilityManager!.IsAbilityActivated(0));
        }

        [Test]
        public void TryActivateAbility_InvalidAbility_ReturnsFalse()
        {
            // Arrange
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);

            // Act
            var result = _abilitySystem.TryActivateAbility(_abilityManager!, 0);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TryActivateAbility_TriggersOnAbilityActivatedEvent()
        {
            // Arrange
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);
            _abilityManager!.AddAbility(0);

            var eventTriggered = false;
            IAbilityManagerCore? eventManager = null;
            int eventAbilityIndex = -1;

            _abilitySystem.OnAbilityActivated += (manager, abilityIndex) =>
            {
                eventTriggered = true;
                eventManager = manager;
                eventAbilityIndex = abilityIndex;
            };

            // Act
            _abilitySystem.TryActivateAbility(_abilityManager!, 0);

            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(_abilityManager!, eventManager);
            Assert.AreEqual(0, eventAbilityIndex);
        }

        [Test]
        public void GetAbilityCooldown_ValidAbility_ReturnsCorrectValue()
        {
            // Arrange
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);
            _abilityManager!.AddAbility(0, 5.0f); // 5 second cooldown

            // Act
            var cooldown = _abilitySystem.GetAbilityCooldown(_abilityManager!, 0);

            // Assert
            Assert.AreEqual(5.0f, cooldown, 0.01f);
        }

        [Test]
        public void IsAbilityOnCooldown_ActiveCooldown_ReturnsTrue()
        {
            // Arrange
            _abilitySystem!.RegisterAbilityManager(_abilityManager!);
            _abilityManager!.AddAbility(0, 5.0f);
            _abilitySystem.TryActivateAbility(_abilityManager!, 0); // Trigger cooldown

            // Act
            var isOnCooldown = _abilitySystem.IsAbilityOnCooldown(_abilityManager!, 0);

            // Assert
            Assert.IsTrue(isOnCooldown);
        }

        #endregion
    }

    #region Mock Implementations

    /// <summary>
    /// Mock implementation of IAbilitySystem for testing.
    /// </summary>
    public class MockAbilitySystem : IAbilitySystem
    {
        private readonly List<IAbilityManagerCore> _abilityManagers = new();

        public event Action<IAbilityManagerCore, int>? OnAbilityActivated;
#pragma warning disable 0414
        public event Action<IAbilityManagerCore, int>? OnAbilityCooldownComplete;
#pragma warning restore 0414
        public event Action<IAbilityManagerCore, int, bool, float>? OnAbilityStateChanged;

        public void RegisterAbilityManager(IAbilityManagerCore abilityManager)
        {
            if (!_abilityManagers.Contains(abilityManager))
            {
                _abilityManagers.Add(abilityManager);
            }
        }

        public void UnregisterAbilityManager(IAbilityManagerCore abilityManager)
        {
            _abilityManagers.Remove(abilityManager);
        }

        public bool TryActivateAbility(IAbilityManagerCore manager, int abilityIndex)
        {
            if (!_abilityManagers.Contains(manager))
                return false;

            var success = manager.ActivateAbility(abilityIndex);
            if (success)
            {
                OnAbilityActivated?.Invoke(manager, abilityIndex);
                OnAbilityStateChanged?.Invoke(manager, abilityIndex, true, GetAbilityCooldown(manager, abilityIndex));
            }
            return success;
        }

        public float GetAbilityCooldown(IAbilityManagerCore manager, int abilityIndex)
        {
            return manager.GetAbilityCooldown(abilityIndex);
        }

        public bool IsAbilityOnCooldown(IAbilityManagerCore manager, int abilityIndex)
        {
            return manager.IsAbilityOnCooldown(abilityIndex);
        }

        public IReadOnlyList<IAbilityManagerCore> GetAllAbilityManagers()
        {
            return _abilityManagers.AsReadOnly();
        }

        public void Dispose()
        {
            _abilityManagers.Clear();
            OnAbilityActivated = null;
            OnAbilityCooldownComplete = null;
            OnAbilityStateChanged = null;
        }
    }

    /// <summary>
    /// Mock implementation of IAbilityManagerCore for testing.
    /// </summary>
    public class MockAbilityManager : IAbilityManagerCore
    {
        private readonly Dictionary<int, MockAbility> _abilities = new();
        private GameObject? _gameObject;

        public GameObject GameObject => _gameObject ?? (_gameObject = new GameObject("MockAbilityManager"));
        public int AbilityCount => _abilities.Count;

        public void AddAbility(int index, float cooldown = 0f)
        {
            _abilities[index] = new MockAbility(cooldown);
        }

        public bool ActivateAbility(int index)
        {
            if (_abilities.TryGetValue(index, out var ability))
            {
                var result = ability.TryActivate();
                return result;
            }
            return false;
        }

        public float GetAbilityCooldown(int index)
        {
            if (_abilities.TryGetValue(index, out var ability))
            {
                return ability.CooldownDuration;
            }
            return 0f;
        }

        public bool IsAbilityOnCooldown(int index)
        {
            if (_abilities.TryGetValue(index, out var ability))
            {
                return ability.IsOnCooldown;
            }
            return false;
        }

        public float GetAbilityCooldownProgress(int index)
        {
            if (_abilities.TryGetValue(index, out var ability))
            {
                return ability.IsOnCooldown ? 0.5f : 0f; // Mock progress
            }
            return 0f;
        }

        public bool IsAbilityActivated(int abilityIndex)
        {
            if (_abilities.TryGetValue(abilityIndex, out var ability))
            {
                return ability.IsActivated;
            }
            return false;
        }

        public void ResetAllCooldowns()
        {
            foreach (var ability in _abilities.Values)
            {
                ability.Reset();
            }
        }
    }

    /// <summary>
    /// Mock ability for testing purposes.
    /// </summary>
    public class MockAbility
    {
        public float CooldownDuration { get; }
        public bool IsOnCooldown { get; private set; }
        public bool IsActivated { get; private set; }

        public MockAbility(float cooldownDuration)
        {
            CooldownDuration = cooldownDuration;
        }

        public bool TryActivate()
        {
            if (IsOnCooldown) return false;

            IsActivated = true;
            IsOnCooldown = CooldownDuration > 0f;
            return true;
        }

        public void Reset()
        {
            IsOnCooldown = false;
            IsActivated = false;
        }
    }

    #endregion
}
