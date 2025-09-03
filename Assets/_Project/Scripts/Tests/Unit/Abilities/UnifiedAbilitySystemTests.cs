using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Abilities.Systems;
using Laboratory.Core.Systems;

namespace Laboratory.Core.Tests.Unit.Abilities
{
    /// <summary>
    /// Unit tests for the UnifiedAbilitySystem.
    /// </summary>
    [TestFixture]
    public class UnifiedAbilitySystemTests
    {
        private UnifiedAbilitySystem _abilitySystem;
        private MockAbilityManager _mockManager1;
        private MockAbilityManager _mockManager2;
        
        [SetUp]
        public void SetUp()
        {
            _abilitySystem = ScriptableObject.CreateInstance<UnifiedAbilitySystem>();
            _mockManager1 = new MockAbilityManager("Manager1", 3);
            _mockManager2 = new MockAbilityManager("Manager2", 2);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_abilitySystem != null)
            {
                Object.DestroyImmediate(_abilitySystem);
            }
            _mockManager1 = null;
            _mockManager2 = null;
        }
        
        #region Registration Tests
        
        [Test]
        public void RegisterAbilityManager_ValidManager_AddsToList()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            
            var managers = _abilitySystem.GetAllAbilityManagers();
            Assert.AreEqual(1, managers.Count);
            Assert.IsTrue(managers.Contains(_mockManager1));
        }
        
        [Test]
        public void RegisterAbilityManager_NullManager_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[UnifiedAbilitySystem] Cannot register null ability manager");
            
            _abilitySystem.RegisterAbilityManager(null);
            
            var managers = _abilitySystem.GetAllAbilityManagers();
            Assert.AreEqual(0, managers.Count);
        }
        
        [Test]
        public void RegisterAbilityManager_DuplicateManager_LogsWarning()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            
            LogAssert.Expect(LogType.Warning, "[UnifiedAbilitySystem] Manager is already registered");
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            
            var managers = _abilitySystem.GetAllAbilityManagers();
            Assert.AreEqual(1, managers.Count);
        }
        
        [Test]
        public void UnregisterAbilityManager_RegisteredManager_RemovesFromList()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            _abilitySystem.RegisterAbilityManager(_mockManager2);
            
            _abilitySystem.UnregisterAbilityManager(_mockManager1);
            
            var managers = _abilitySystem.GetAllAbilityManagers();
            Assert.AreEqual(1, managers.Count);
            Assert.IsTrue(managers.Contains(_mockManager2));
        }
        
        [Test]
        public void UnregisterAbilityManager_UnregisteredManager_LogsWarning()
        {
            LogAssert.Expect(LogType.Warning, "[UnifiedAbilitySystem] Manager was not registered");
            
            _abilitySystem.UnregisterAbilityManager(_mockManager1);
        }
        
        #endregion
        
        #region Manager ID Tests
        
        [Test]
        public void GetManagerId_RegisteredManager_ReturnsValidId()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            
            int id = _abilitySystem.GetManagerId(_mockManager1);
            Assert.GreaterOrEqual(id, 0);
        }
        
        [Test]
        public void GetManagerId_UnregisteredManager_ReturnsNegativeOne()
        {
            int id = _abilitySystem.GetManagerId(_mockManager1);
            Assert.AreEqual(-1, id);
        }
        
        [Test]
        public void GetManagerId_MultipleManagers_ReturnsUniqueIds()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            _abilitySystem.RegisterAbilityManager(_mockManager2);
            
            int id1 = _abilitySystem.GetManagerId(_mockManager1);
            int id2 = _abilitySystem.GetManagerId(_mockManager2);
            
            Assert.AreNotEqual(id1, id2);
        }
        
        #endregion
        
        #region Ability Activation Tests
        
        [Test]
        public void TryActivateAbility_ValidManagerAndIndex_CallsManagerActivateAbility()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            _mockManager1.SetActivationResult(true);
            
            bool result = _abilitySystem.TryActivateAbility(_mockManager1, 0);
            
            Assert.IsTrue(result);
            Assert.IsTrue(_mockManager1.WasActivateCalled);
            Assert.AreEqual(0, _mockManager1.LastActivationIndex);
        }
        
        [Test]
        public void TryActivateAbility_UnregisteredManager_ReturnsFalse()
        {
            bool result = _abilitySystem.TryActivateAbility(_mockManager1, 0);
            
            Assert.IsFalse(result);
            Assert.IsFalse(_mockManager1.WasActivateCalled);
        }
        
        [Test]
        public void GetAbilityCooldown_ValidManager_ReturnsManagerCooldown()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            _mockManager1.SetCooldown(1, 5.5f);
            
            float cooldown = _abilitySystem.GetAbilityCooldown(_mockManager1, 1);
            
            Assert.AreEqual(5.5f, cooldown, 0.01f);
        }
        
        [Test]
        public void IsAbilityOnCooldown_ValidManager_ReturnsManagerState()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            _mockManager1.SetOnCooldown(1, true);
            
            bool isOnCooldown = _abilitySystem.IsAbilityOnCooldown(_mockManager1, 1);
            
            Assert.IsTrue(isOnCooldown);
        }
        
        #endregion
        
        #region System Operations Tests
        
        [Test]
        public void ResetAllCooldowns_CallsResetOnAllManagers()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1);
            _abilitySystem.RegisterAbilityManager(_mockManager2);
            
            _abilitySystem.ResetAllCooldowns();
            
            Assert.IsTrue(_mockManager1.WasResetCalled);
            Assert.IsTrue(_mockManager2.WasResetCalled);
        }
        
        [Test]
        public void GetSystemStats_ReturnsCorrectStatistics()
        {
            _abilitySystem.RegisterAbilityManager(_mockManager1); // 3 abilities
            _abilitySystem.RegisterAbilityManager(_mockManager2); // 2 abilities
            
            var stats = _abilitySystem.GetSystemStats();
            
            Assert.AreEqual(2, stats.RegisteredManagers);
            Assert.AreEqual(5, stats.TotalAbilities); // 3 + 2
        }
        
        #endregion
        
        #region Mock Class
        
        /// <summary>
        /// Mock implementation of IAbilityManagerCore for testing.
        /// </summary>
        private class MockAbilityManager : IAbilityManagerCore
        {
            public string Name { get; private set; }
            public int AbilityCount { get; private set; }
            public GameObject GameObject => new GameObject(Name);
            public int ManagerId { get; set; }
            public bool IsActive { get; private set; } = true;
            
            // Test tracking properties
            public bool WasActivateCalled { get; private set; }
            public int LastActivationIndex { get; private set; } = -1;
            public bool WasResetCalled { get; private set; }
            
            private bool _activationResult = true;
            private Dictionary<int, float> _cooldowns = new Dictionary<int, float>();
            private Dictionary<int, bool> _onCooldownStates = new Dictionary<int, bool>();
            
#pragma warning disable CS0067 // Events are required by interface but not used in mock
            public event System.Action<int> OnAbilityActivated;
            public event System.Action<int, bool> OnAbilityStateChanged;
            public event System.Action<int> OnAbilityCooldownComplete;
#pragma warning restore CS0067
            
            public MockAbilityManager(string name, int abilityCount)
            {
                Name = name;
                AbilityCount = abilityCount;
            }
            
            public bool ActivateAbility(int abilityIndex)
            {
                WasActivateCalled = true;
                LastActivationIndex = abilityIndex;
                return _activationResult;
            }
            
            public bool TryActivateAbility(int abilityIndex)
            {
                return ActivateAbility(abilityIndex);
            }
            
            public bool IsAbilityOnCooldown(int abilityIndex)
            {
                return _onCooldownStates.GetValueOrDefault(abilityIndex, false);
            }
            
            public float GetAbilityCooldown(int abilityIndex)
            {
                return _cooldowns.GetValueOrDefault(abilityIndex, 0f);
            }
            
            public float GetAbilityCooldownProgress(int abilityIndex)
            {
                // Mock implementation - return 0.5f for testing
                return IsAbilityOnCooldown(abilityIndex) ? 0.5f : 0f;
            }
            
            public void ResetAllCooldowns()
            {
                WasResetCalled = true;
                _cooldowns.Clear();
                _onCooldownStates.Clear();
            }
            
            public object GetAbilityData(int abilityIndex)
            {
                return null;
            }
            
            public void UpdateManager(float deltaTime)
            {
                // Mock implementation
            }
            
            public void SetAbilityCooldown(int abilityIndex, float cooldownTime)
            {
                _cooldowns[abilityIndex] = cooldownTime;
            }
            
            public void SetActive(bool active)
            {
                IsActive = active;
            }
            
            // Test helper methods
            public void SetActivationResult(bool result)
            {
                _activationResult = result;
            }
            
            public void SetCooldown(int index, float cooldown)
            {
                _cooldowns[index] = cooldown;
            }
            
            public void SetOnCooldown(int index, bool onCooldown)
            {
                _onCooldownStates[index] = onCooldown;
            }
        }
        
        #endregion
    }
}
