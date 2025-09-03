using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Gameplay.Abilities;
using AbilityActivatedEvent = Laboratory.Core.Abilities.Events.AbilityActivatedEvent;
using AbilityStateChangedEvent = Laboratory.Core.Abilities.Events.AbilityStateChangedEvent;
using AbilityCooldownCompleteEvent = Laboratory.Core.Abilities.Events.AbilityCooldownCompleteEvent;
using Laboratory.Core.Abilities.Events;

namespace Laboratory.Tests.Unit.Abilities
{
    /// <summary>
    /// Comprehensive unit tests for the AbilityManager component.
    /// Tests core functionality including ability activation, cooldowns, and event integration.
    /// </summary>
    [TestFixture]
    public class AbilityManagerTests
    {
        #region Fields

        private AbilityManager _abilityManager;
        private GameObject _testGameObject;
        
        #pragma warning disable CS0414 // Field assigned but never used - used in event testing
        private bool _activatedEventReceived;
        private bool _stateChangedEventReceived;
        private bool _cooldownCompleteEventReceived;
        #pragma warning restore CS0414
        
        private AbilityActivatedEvent _lastActivatedEvent;
        private AbilityStateChangedEvent _lastStateChangedEvent;
        private AbilityCooldownCompleteEvent _lastCooldownCompleteEvent;

        #endregion

        #region Setup & Teardown

        [SetUp]
        public void Setup()
        {
            // Create test GameObject with AbilityManager
            _testGameObject = new GameObject("TestAbilityManager");
            _abilityManager = _testGameObject.AddComponent<AbilityManager>();
            
            // Reset event flags
            ResetEventFlags();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Allow one frame for initialization
            _abilityManager.SendMessage("Awake");
        }

        [TearDown]
        public void Teardown()
        {
            UnsubscribeFromEvents();
            
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }

        private void ResetEventFlags()
        {
            _activatedEventReceived = false;
            _stateChangedEventReceived = false;
            _cooldownCompleteEventReceived = false;
            
            _lastActivatedEvent = null;
            _lastStateChangedEvent = null;
            _lastCooldownCompleteEvent = null;
        }

        #endregion

        #region Basic Functionality Tests

        [Test]
        public void AbilityManager_InitializesCorrectly()
        {
            // Assert
            Assert.IsNotNull(_abilityManager);
            Assert.AreEqual(3, _abilityManager.AbilityCount); // Default count
            Assert.IsNotNull(_abilityManager.Abilities);
            Assert.AreEqual(3, _abilityManager.Abilities.Count);
            
            // Check that all abilities have proper data
            for (int i = 0; i < _abilityManager.AbilityCount; i++)
            {
                var abilityData = _abilityManager.GetAbilityData(i);
                Assert.IsNotNull(abilityData, $"Ability {i} data should not be null");
                Assert.AreEqual(i, abilityData.index, $"Ability {i} should have correct index");
                Assert.IsNotEmpty(abilityData.name, $"Ability {i} should have a name");
            }
        }

        [Test]
        public void ActivateAbility_WithValidIndex_ShouldActivateSuccessfully()
        {
            // Arrange
            const int abilityIndex = 0;
            
            // Act
            bool result = _abilityManager.ActivateAbility(abilityIndex);
            
            // Assert
            Assert.IsTrue(result, "Ability activation should succeed");
            Assert.IsTrue(_abilityManager.IsAbilityOnCooldown(abilityIndex), "Ability should be on cooldown after activation");
            Assert.Greater(_abilityManager.GetAbilityCooldown(abilityIndex), 0f, "Cooldown should be greater than 0");
        }

        [Test]
        public void ActivateAbility_WithInvalidIndex_ShouldReturnFalse()
        {
            // Arrange
            int[] invalidIndices = { -1, 999, 100 };
            
            foreach (int invalidIndex in invalidIndices)
            {
                // Act
                bool result = _abilityManager.ActivateAbility(invalidIndex);
                
                // Assert
                Assert.IsFalse(result, $"Activation with invalid index {invalidIndex} should fail");
            }
        }

        [Test]
        public void TryActivateAbility_WhenNotOnCooldown_ShouldActivate()
        {
            // Arrange
            const int abilityIndex = 1;
            
            // Act
            bool result = _abilityManager.TryActivateAbility(abilityIndex);
            
            // Assert
            Assert.IsTrue(result, "TryActivateAbility should succeed when not on cooldown");
            Assert.IsTrue(_abilityManager.IsAbilityOnCooldown(abilityIndex), "Ability should be on cooldown after activation");
        }

        [Test]
        public void ResetAllCooldowns_ShouldResetAllAbilities()
        {
            // Arrange - Activate all abilities
            for (int i = 0; i < _abilityManager.AbilityCount; i++)
            {
                _abilityManager.ActivateAbility(i);
                Assert.IsTrue(_abilityManager.IsAbilityOnCooldown(i), $"Ability {i} should be on cooldown after activation");
            }
            
            // Act
            _abilityManager.ResetAllCooldowns();
            
            // Assert - All abilities should be ready
            for (int i = 0; i < _abilityManager.AbilityCount; i++)
            {
                Assert.IsFalse(_abilityManager.IsAbilityOnCooldown(i), $"Ability {i} should not be on cooldown after reset");
                Assert.AreEqual(0f, _abilityManager.GetAbilityCooldown(i), $"Ability {i} should have 0 remaining cooldown");
            }
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator AbilityCooldown_CompletesAfterDuration()
        {
            // Arrange
            const int abilityIndex = 0;
            var abilityData = _abilityManager.GetAbilityData(abilityIndex);
            float cooldownDuration = abilityData.cooldownDuration;
            
            // Act
            _abilityManager.ActivateAbility(abilityIndex);
            Assert.IsTrue(_abilityManager.IsAbilityOnCooldown(abilityIndex), "Ability should be on cooldown immediately after activation");
            
            // Wait for cooldown to complete (with small buffer)
            yield return new WaitForSeconds(cooldownDuration + 0.1f);
            
            // Assert
            Assert.IsFalse(_abilityManager.IsAbilityOnCooldown(abilityIndex), "Ability should not be on cooldown after duration");
            Assert.AreEqual(0f, _abilityManager.GetAbilityCooldown(abilityIndex), "Ability should have 0 remaining cooldown");
        }

        #endregion

        #region Helper Methods

        private void SubscribeToEvents()
        {
            // Subscribe to events through AbilityEventBus
            AbilityEventBus.OnAbilityActivated.AddListener(OnAbilityActivated);
            AbilityEventBus.OnAbilityStateChanged.AddListener(OnAbilityStateChanged);
            AbilityEventBus.OnAbilityCooldownComplete.AddListener(OnAbilityCooldownComplete);
        }

        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from events
            AbilityEventBus.OnAbilityActivated.RemoveListener(OnAbilityActivated);
            AbilityEventBus.OnAbilityStateChanged.RemoveListener(OnAbilityStateChanged);
            AbilityEventBus.OnAbilityCooldownComplete.RemoveListener(OnAbilityCooldownComplete);
        }

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            _activatedEventReceived = true;
            _lastActivatedEvent = evt;
        }

        private void OnAbilityStateChanged(AbilityStateChangedEvent evt)
        {
            _stateChangedEventReceived = true;
            _lastStateChangedEvent = evt;
        }

        private void OnAbilityCooldownComplete(AbilityCooldownCompleteEvent evt)
        {
            _cooldownCompleteEventReceived = true;
            _lastCooldownCompleteEvent = evt;
        }

        #endregion
    }

    /// <summary>
    /// Custom ability data for testing specific scenarios.
    /// </summary>
    [System.Serializable]
    public class TestAbilityData : AbilityManager.AbilityData
    {
        public bool wasActivated;
        public int activationCount;
        public bool blockActivation;

        public override bool CanActivate(AbilityManager manager)
        {
            if (blockActivation) return false;
            return !manager.IsAbilityOnCooldown(index) && activationCount < 5;
        }

        public override void OnActivate(AbilityManager manager)
        {
            wasActivated = true;
            activationCount++;
            Debug.Log($"[TestAbilityData] Activated {name} (Count: {activationCount})");
        }
    }
}