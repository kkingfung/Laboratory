using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Laboratory.Core.Health;
using Laboratory.Core.Health.Services;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health.Managers;

namespace Laboratory.Core.Health.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for the Health & Combat subsystem.
    /// Tests all major components, services, and integration points.
    /// </summary>
    public class HealthSystemTests
    {
        #region Fields

        private HealthSystemService _healthSystemService;
        private GameObject _testGameObject;
        private LocalHealthComponent _testHealthComponent;

        #endregion

        #region Setup and Teardown

        [SetUp]
        public void SetUp()
        {
            // Create test objects
            _testGameObject = new GameObject("TestHealthObject");
            _testHealthComponent = _testGameObject.AddComponent<LocalHealthComponent>();
            
            // Create health system service
            _healthSystemService = new HealthSystemService();
        }

        [TearDown]
        public void TearDown()
        {
            _healthSystemService?.Dispose();
            
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        #endregion

        #region Health System Service Tests

        [Test]
        public void HealthSystemService_RegisterComponent_ComponentIsRegistered()
        {
            // Arrange & Act
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            
            // Assert
            Assert.AreEqual(1, _healthSystemService.TotalComponents);
            Assert.Contains(_testHealthComponent, _healthSystemService.GetAllHealthComponents().ToList());
        }

        [Test]
        public void HealthSystemService_RegisterComponent_Twice_OnlyRegisteredOnce()
        {
            // Arrange & Act
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            
            // Assert
            Assert.AreEqual(1, _healthSystemService.TotalComponents);
        }

        [Test]
        public void HealthSystemService_UnregisterComponent_ComponentIsUnregistered()
        {
            // Arrange
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            
            // Act
            _healthSystemService.UnregisterHealthComponent(_testHealthComponent);
            
            // Assert
            Assert.AreEqual(0, _healthSystemService.TotalComponents);
            Assert.IsFalse(_healthSystemService.GetAllHealthComponents().Contains(_testHealthComponent));
        }

        [Test]
        public void HealthSystemService_ApplyDamage_DamageIsApplied()
        {
            // Arrange
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            var damageRequest = new DamageRequest(25f, DamageType.Normal);
            var initialHealth = _testHealthComponent.CurrentHealth;
            
            // Act
            bool result = _healthSystemService.ApplyDamage(_testHealthComponent, damageRequest);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(initialHealth - 25, _testHealthComponent.CurrentHealth);
            Assert.AreEqual(1, _healthSystemService.Statistics.TotalDamageApplications);
        }

        [Test]
        public void HealthSystemService_ApplyHealing_HealingIsApplied()
        {
            // Arrange
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            _testHealthComponent.TakeDamage(new DamageRequest(30f));
            var damagedHealth = _testHealthComponent.CurrentHealth;
            
            // Act
            bool result = _healthSystemService.ApplyHealing(_testHealthComponent, 15);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(damagedHealth + 15, _testHealthComponent.CurrentHealth);
            Assert.AreEqual(1, _healthSystemService.Statistics.TotalHealingApplications);
        }

        [Test]
        public void HealthSystemService_GetHealthComponentsInRange_ReturnsCorrectComponents()
        {
            // Arrange
            var healthyComponent = _testHealthComponent;
            
            var damagedObject = new GameObject("DamagedObject");
            var damagedComponent = damagedObject.AddComponent<LocalHealthComponent>();
            damagedComponent.TakeDamage(new DamageRequest(60f)); // 40% health
            
            _healthSystemService.RegisterHealthComponent(healthyComponent);
            _healthSystemService.RegisterHealthComponent(damagedComponent);
            
            try
            {
                // Act - Get components with 30-60% health
                var components = _healthSystemService.GetHealthComponentsInRange(0.3f, 0.6f).ToList();
                
                // Assert
                Assert.AreEqual(1, components.Count);
                Assert.Contains(damagedComponent, components);
                Assert.IsFalse(components.Contains(healthyComponent)); // Healthy component should be excluded
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damagedObject);
            }
        }

        #endregion

        #region Damage Request Tests

        [Test]
        public void DamageRequest_DefaultConstructor_HasCorrectDefaults()
        {
            // Act
            var damageRequest = new DamageRequest();
            
            // Assert
            Assert.AreEqual(0f, damageRequest.Amount);
            Assert.AreEqual(DamageType.Normal, damageRequest.Type);
            Assert.IsTrue(damageRequest.CanBeBlocked);
            Assert.IsTrue(damageRequest.TriggerInvulnerability);
            Assert.IsNotNull(damageRequest.Metadata);
        }

        [Test]
        public void DamageRequest_FactoryMethods_CreateCorrectRequests()
        {
            // Act
            var basicRequest = DamageRequest.Create(50f, "TestSource");
            var typedRequest = DamageRequest.CreateTyped(75f, DamageType.Fire, "TestSource");
            var directionalRequest = DamageRequest.CreateDirectional(100f, Vector3.up, "TestSource");
            
            // Assert
            Assert.AreEqual(50f, basicRequest.Amount);
            Assert.AreEqual(DamageType.Normal, basicRequest.Type);
            
            Assert.AreEqual(75f, typedRequest.Amount);
            Assert.AreEqual(DamageType.Fire, typedRequest.Type);
            
            Assert.AreEqual(100f, directionalRequest.Amount);
            Assert.AreEqual(Vector3.up, directionalRequest.Direction);
        }

        #endregion

        #region Health Component Base Tests

        [Test]
        public void HealthComponentBase_TakeDamage_ReducesHealth()
        {
            // Arrange
            var initialHealth = _testHealthComponent.CurrentHealth;
            var damageRequest = new DamageRequest(30f, DamageType.Normal);
            
            // Act
            bool result = _testHealthComponent.TakeDamage(damageRequest);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(initialHealth - 30, _testHealthComponent.CurrentHealth);
            Assert.IsTrue(_testHealthComponent.IsAlive);
        }

        [Test]
        public void HealthComponentBase_TakeDamage_KillsWhenHealthReachesZero()
        {
            // Arrange
            var damageRequest = new DamageRequest(_testHealthComponent.MaxHealth, DamageType.Normal);
            bool deathEventFired = false;
            _testHealthComponent.OnDeath += _ => deathEventFired = true;
            
            // Act
            _testHealthComponent.TakeDamage(damageRequest);
            
            // Assert
            Assert.AreEqual(0, _testHealthComponent.CurrentHealth);
            Assert.IsFalse(_testHealthComponent.IsAlive);
            Assert.IsTrue(deathEventFired);
        }

        [Test]
        public void HealthComponentBase_Heal_IncreasesHealth()
        {
            // Arrange
            _testHealthComponent.TakeDamage(new DamageRequest(40f));
            var damagedHealth = _testHealthComponent.CurrentHealth;
            
            // Act
            bool result = _testHealthComponent.Heal(20);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(damagedHealth + 20, _testHealthComponent.CurrentHealth);
        }

        [Test]
        public void HealthComponentBase_Heal_CannotExceedMaxHealth()
        {
            // Arrange
            var maxHealth = _testHealthComponent.MaxHealth;
            
            // Act
            bool result = _testHealthComponent.Heal(50); // Try to overheal
            
            // Assert
            Assert.IsFalse(result); // Should return false when already at max health
            Assert.AreEqual(maxHealth, _testHealthComponent.CurrentHealth);
        }

        [Test]
        public void HealthComponentBase_ResetToMaxHealth_RestoresFullHealth()
        {
            // Arrange
            _testHealthComponent.TakeDamage(new DamageRequest(60f));
            var maxHealth = _testHealthComponent.MaxHealth;
            
            // Act
            _testHealthComponent.ResetToMaxHealth();
            
            // Assert
            Assert.AreEqual(maxHealth, _testHealthComponent.CurrentHealth);
            Assert.IsTrue(_testHealthComponent.IsAlive);
        }

        [Test]
        public void HealthComponentBase_SetMaxHealth_UpdatesMaxHealth()
        {
            // Arrange
            var newMaxHealth = 150;
            
            // Act
            _testHealthComponent.SetMaxHealth(newMaxHealth);
            
            // Assert
            Assert.AreEqual(newMaxHealth, _testHealthComponent.MaxHealth);
        }

        [Test]
        public void HealthComponentBase_SetMaxHealth_ClampsCurrentHealth()
        {
            // Arrange
            var newMaxHealth = 50; // Less than current health
            
            // Act
            _testHealthComponent.SetMaxHealth(newMaxHealth);
            
            // Assert
            Assert.AreEqual(newMaxHealth, _testHealthComponent.MaxHealth);
            Assert.AreEqual(newMaxHealth, _testHealthComponent.CurrentHealth);
        }

        [Test]
        public void HealthComponentBase_HealthPercentage_CalculatedCorrectly()
        {
            // Arrange
            _testHealthComponent.TakeDamage(new DamageRequest(25f)); // 75% health remaining
            
            // Act
            float percentage = _testHealthComponent.HealthPercentage;
            
            // Assert
            Assert.AreEqual(0.75f, percentage, 0.01f);
        }

        [Test]
        public void HealthComponentBase_InvulnerabilityFrames_PreventDamage()
        {
            // Arrange
            var damageRequest1 = new DamageRequest(20f) { TriggerInvulnerability = true };
            var damageRequest2 = new DamageRequest(20f) { TriggerInvulnerability = true };
            
            // Act
            _testHealthComponent.TakeDamage(damageRequest1);
            var healthAfterFirst = _testHealthComponent.CurrentHealth;
            bool secondDamageApplied = _testHealthComponent.TakeDamage(damageRequest2); // Immediate second damage
            
            // Assert
            Assert.IsFalse(secondDamageApplied);
            Assert.AreEqual(healthAfterFirst, _testHealthComponent.CurrentHealth);
        }

        #endregion

        #region Event Tests

        [Test]
        public void HealthComponentBase_OnHealthChanged_FiresWhenHealthChanges()
        {
            // Arrange
            bool eventFired = false;
            HealthChangedEventArgs capturedArgs = null;
            _testHealthComponent.OnHealthChanged += (args) =>
            {
                eventFired = true;
                capturedArgs = args;
            };
            
            // Act
            _testHealthComponent.TakeDamage(new DamageRequest(25f));
            
            // Assert
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(capturedArgs);
            Assert.AreEqual(100, capturedArgs.OldHealth);
            Assert.AreEqual(75, capturedArgs.NewHealth);
            Assert.AreEqual(-25, capturedArgs.HealthDelta);
        }

        [Test]
        public void HealthComponentBase_OnDeath_FiresWhenHealthReachesZero()
        {
            // Arrange
            bool eventFired = false;
            DeathEventArgs capturedArgs = null;
            _testHealthComponent.OnDeath += (args) =>
            {
                eventFired = true;
                capturedArgs = args;
            };
            
            // Act
            _testHealthComponent.TakeDamage(new DamageRequest(_testHealthComponent.MaxHealth));
            
            // Assert
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(capturedArgs);
        }

        #endregion

        #region Damage Processing Tests

        [Test]
        public void DamageManager_ApplyDamage_ValidTarget_AppliesDamage()
        {
            // Arrange
            var damageManager = DamageManager.Instance;
            Assert.IsNotNull(damageManager, "DamageManager instance should exist");
            
            var damageRequest = new DamageRequest(30f, DamageType.Normal);
            var initialHealth = _testHealthComponent.CurrentHealth;
            
            // Act
            bool result = damageManager.ApplyDamage(_testGameObject, damageRequest);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(initialHealth - 30, _testHealthComponent.CurrentHealth);
        }

        [Test]
        public void DamageManager_ApplyDamage_InvalidTarget_ReturnsFalse()
        {
            // Arrange
            var damageManager = DamageManager.Instance;
            var emptyGameObject = new GameObject("EmptyObject"); // No health component
            var damageRequest = new DamageRequest(30f, DamageType.Normal);
            
            try
            {
                // Act
                bool result = damageManager.ApplyDamage(emptyGameObject, damageRequest);
                
                // Assert
                Assert.IsFalse(result);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(emptyGameObject);
            }
        }

        #endregion

        #region AI Health Component Tests

        [Test]
        public void AIHealthComponent_SetHealthPercentage_UpdatesHealth()
        {
            // Arrange
            var aiObject = new GameObject("AIObject");
            var aiHealthComponent = aiObject.AddComponent<AIHealthComponent>();
            
            try
            {
                // Act
                aiHealthComponent.SetHealthPercentage(0.5f); // 50% health
                
                // Assert
                Assert.AreEqual(50, aiHealthComponent.CurrentHealth);
                Assert.AreEqual(0.5f, aiHealthComponent.HealthPercentage, 0.01f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aiObject);
            }
        }

        [Test]
        public void AIHealthComponent_IsHealthBelowPercentage_ReturnsCorrectResult()
        {
            // Arrange
            var aiObject = new GameObject("AIObject");
            var aiHealthComponent = aiObject.AddComponent<AIHealthComponent>();
            aiHealthComponent.TakeDamage(new DamageRequest(70f)); // 30% health remaining
            
            try
            {
                // Act & Assert
                Assert.IsTrue(aiHealthComponent.IsHealthBelowPercentage(0.5f)); // Below 50%
                Assert.IsFalse(aiHealthComponent.IsHealthBelowPercentage(0.2f)); // Above 20%
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aiObject);
            }
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void HealthSystemService_Statistics_TrackDamageCorrectly()
        {
            // Arrange
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            var damageRequest1 = new DamageRequest(25f);
            var damageRequest2 = new DamageRequest(30f);
            
            // Act
            _healthSystemService.ApplyDamage(_testHealthComponent, damageRequest1);
            _healthSystemService.ApplyDamage(_testHealthComponent, damageRequest2);
            
            // Assert
            Assert.AreEqual(2, _healthSystemService.Statistics.TotalDamageApplications);
            Assert.AreEqual(55f, _healthSystemService.Statistics.TotalDamageDealt);
        }

        [Test]
        public void HealthSystemService_Statistics_TrackHealingCorrectly()
        {
            // Arrange
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            _testHealthComponent.TakeDamage(new DamageRequest(60f)); // Damage first
            
            // Act
            _healthSystemService.ApplyHealing(_testHealthComponent, 20);
            _healthSystemService.ApplyHealing(_testHealthComponent, 15);
            
            // Assert
            Assert.AreEqual(2, _healthSystemService.Statistics.TotalHealingApplications);
            Assert.AreEqual(35, _healthSystemService.Statistics.TotalHealingApplied);
        }

        [Test]
        public void HealthSystemService_ResetStatistics_ClearsStats()
        {
            // Arrange
            _healthSystemService.RegisterHealthComponent(_testHealthComponent);
            _healthSystemService.ApplyDamage(_testHealthComponent, new DamageRequest(25f));
            
            // Act
            _healthSystemService.ResetStatistics();
            
            // Assert
            Assert.AreEqual(0, _healthSystemService.Statistics.TotalDamageApplications);
            Assert.AreEqual(0f, _healthSystemService.Statistics.TotalDamageDealt);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void HealthSystem_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange - Create multiple components
            var player = new GameObject("Player");
            var playerHealth = player.AddComponent<LocalHealthComponent>();
            
            var enemy = new GameObject("Enemy");  
            var enemyHealth = enemy.AddComponent<AIHealthComponent>();
            
            try
            {
                // Act - Register components
                _healthSystemService.RegisterHealthComponent(playerHealth);
                _healthSystemService.RegisterHealthComponent(enemyHealth);
                
                // Act - Apply damage and healing
                _healthSystemService.ApplyDamage(playerHealth, new DamageRequest(40f, DamageType.Fire));
                _healthSystemService.ApplyDamage(enemyHealth, new DamageRequest(60f, DamageType.Lightning));
                _healthSystemService.ApplyHealing(playerHealth, 20);
                
                // Assert - Check final state
                Assert.AreEqual(2, _healthSystemService.AliveComponents);
                Assert.AreEqual(80, playerHealth.CurrentHealth); // 100 - 40 + 20
                Assert.AreEqual(40, enemyHealth.CurrentHealth); // 100 - 60
                Assert.AreEqual(2, _healthSystemService.Statistics.TotalDamageApplications);
                Assert.AreEqual(1, _healthSystemService.Statistics.TotalHealingApplications);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        #endregion
    }
}
