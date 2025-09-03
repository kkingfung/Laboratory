using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.Abilities.Components;
using Laboratory.Core.Abilities.Interfaces;
using Laboratory.Core.Abilities.Events;
using Laboratory.Core.Systems;

namespace Laboratory.Core.Tests.Unit.Abilities
{
    /// <summary>
    /// Unit tests for the BaseAbility class and ability system functionality.
    /// </summary>
    [TestFixture]
    public class BaseAbilityTests
    {
        private TestAbility _testAbility;
        
        [SetUp]
        public void SetUp()
        {
            _testAbility = new TestAbility();
            _testAbility.Initialize("test_ability", "Test Ability", "A test ability", 2f, 0f, 10, 5f);
        }
        
        [TearDown]
        public void TearDown()
        {
            _testAbility = null;
            AbilityEventBus.ClearAllSubscriptions();
        }
        
        #region Basic Property Tests
        
        [Test]
        public void AbilityProperties_ReturnsCorrectValues()
        {
            Assert.AreEqual("test_ability", _testAbility.AbilityId);
            Assert.AreEqual("Test Ability", _testAbility.DisplayName);
            Assert.AreEqual("A test ability", _testAbility.Description);
            Assert.AreEqual(2f, _testAbility.CooldownTime, 0.01f);
            Assert.AreEqual(0f, _testAbility.CastTime, 0.01f);
            Assert.AreEqual(10, _testAbility.ResourceCost);
            Assert.AreEqual(5f, _testAbility.Range, 0.01f);
        }
        
        [Test]
        public void IsUsable_WhenNotOnCooldown_ReturnsTrue()
        {
            Assert.IsTrue(_testAbility.IsUsable);
        }
        
        [Test]
        public void IsOnCooldown_WhenNotActivated_ReturnsFalse()
        {
            Assert.IsFalse(_testAbility.IsOnCooldown);
        }
        
        [Test]
        public void CooldownRemaining_WhenNotOnCooldown_ReturnsZero()
        {
            Assert.AreEqual(0f, _testAbility.CooldownRemaining, 0.01f);
        }
        
        #endregion
        
        #region Activation Tests
        
        [Test]
        public void TryActivate_WhenUsable_ReturnsTrue()
        {
            _testAbility.SetCanPayCost(true);
            bool result = _testAbility.TryActivate();
            Assert.IsTrue(result);
            Assert.IsTrue(_testAbility.WasExecuted);
        }
        
        [Test]
        public void TryActivate_WhenCannotPayCost_ReturnsFalse()
        {
            _testAbility.SetCanPayCost(false);
            bool result = _testAbility.TryActivate();
            Assert.IsFalse(result);
            Assert.IsFalse(_testAbility.WasExecuted);
        }
        
        [Test]
        public void TryActivate_WhenOnCooldown_ReturnsFalse()
        {
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate(); // First activation
            
            bool result = _testAbility.TryActivate(); // Second activation
            Assert.IsFalse(result);
        }
        
        [Test]
        public void ForceActivate_Always_ExecutesAbility()
        {
            _testAbility.SetCanPayCost(false);
            _testAbility.ForceActivate();
            Assert.IsTrue(_testAbility.WasExecuted);
        }
        
        #endregion
        
        #region Cooldown Tests
        
        [Test]
        public void Activation_StartsCooldown()
        {
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate();
            
            Assert.IsTrue(_testAbility.IsOnCooldown);
            Assert.AreEqual(_testAbility.CooldownTime, _testAbility.CooldownRemaining, 0.01f);
        }
        
        [Test]
        public void UpdateAbility_ReducesCooldown()
        {
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate();
            
            float deltaTime = 1f;
            _testAbility.UpdateAbility(deltaTime);
            
            Assert.AreEqual(_testAbility.CooldownTime - deltaTime, _testAbility.CooldownRemaining, 0.01f);
        }
        
        [Test]
        public void UpdateAbility_CompleteCooldown_MakesAbilityUsable()
        {
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate();
            
            _testAbility.UpdateAbility(_testAbility.CooldownTime + 0.1f);
            
            Assert.IsFalse(_testAbility.IsOnCooldown);
            Assert.AreEqual(0f, _testAbility.CooldownRemaining, 0.01f);
            Assert.IsTrue(_testAbility.IsUsable);
        }
        
        [Test]
        public void ResetCooldown_MakesAbilityUsableImmediately()
        {
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate();
            
            _testAbility.ResetCooldown();
            
            Assert.IsFalse(_testAbility.IsOnCooldown);
            Assert.AreEqual(0f, _testAbility.CooldownRemaining, 0.01f);
            Assert.IsTrue(_testAbility.IsUsable);
        }
        
        #endregion
        
        #region Event Tests
        
        [Test]
        public void Activation_PublishesAbilityActivatedEvent()
        {
            bool eventReceived = false;
            AbilityActivatedEvent receivedEvent = null;
            
            AbilityEventBus.OnAbilityActivated.AddListener((evt) => {
                eventReceived = true;
                receivedEvent = evt;
            });
            
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate();
            
            Assert.IsTrue(eventReceived);
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(_testAbility.TestAbilityIndex, receivedEvent.AbilityIndex);
        }
        
        [Test]
        public void CooldownComplete_PublishesCooldownCompleteEvent()
        {
            bool eventReceived = false;
            AbilityCooldownCompleteEvent receivedEvent = null;
            
            AbilityEventBus.OnAbilityCooldownComplete.AddListener((evt) => {
                eventReceived = true;
                receivedEvent = evt;
            });
            
            _testAbility.SetCanPayCost(true);
            _testAbility.TryActivate();
            _testAbility.UpdateAbility(_testAbility.CooldownTime + 0.1f);
            
            Assert.IsTrue(eventReceived);
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(_testAbility.TestAbilityIndex, receivedEvent.AbilityIndex);
        }
        
        #endregion
        
        #region Helper Class
        
        /// <summary>
        /// Test implementation of BaseAbility for unit testing.
        /// </summary>
        private class TestAbility : BaseAbility
        {
            public bool WasExecuted { get; private set; }
            public int TestAbilityIndex { get; private set; } = 42;
            private bool _canPayCost = true;
            
            public void Initialize(string id, string name, string desc, float cooldown, float cast, int cost, float abilityRange)
            {
                abilityId = id;
                displayName = name;
                description = desc;
                cooldownTime = cooldown;
                castTime = cast;
                resourceCost = cost;
                range = abilityRange;
                enableDebugLogs = false; // Disable for tests
            }
            
            public void SetCanPayCost(bool canPay)
            {
                _canPayCost = canPay;
            }
            
            protected override void OnAbilityExecuted()
            {
                WasExecuted = true;
            }
            
            protected override bool CanPayCost()
            {
                return _canPayCost;
            }
            
            protected override int GetAbilityIndex()
            {
                return TestAbilityIndex;
            }
        }
        
        #endregion
    }
}
