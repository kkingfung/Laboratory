using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health;

namespace Laboratory.Core.Tests.Unit.Health
{
    /// <summary>
    /// Unit tests for the Health System components and functionality.
    /// </summary>
    [TestFixture]
    public class HealthSystemTests
    {
        private GameObject _testObject;
        private LocalHealthComponent _healthComponent;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestObject");
            _healthComponent = _testObject.AddComponent<LocalHealthComponent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }
        
        #region Basic Health Tests
        
        [Test]
        public void HealthComponent_InitialValues_AreCorrect()
        {
            // Assuming default max health is 100
            Assert.AreEqual(100, _healthComponent.MaxHealth);
            Assert.AreEqual(100, _healthComponent.CurrentHealth);
            Assert.IsTrue(_healthComponent.IsAlive);
            Assert.AreEqual(1f, _healthComponent.HealthPercentage, 0.01f);
        }
        
        [Test]
        public void TakeDamage_ValidAmount_ReducesHealth()
        {
            float damageAmount = 25f;
            var damageRequest = new DamageRequest
            {
                Amount = damageAmount,
                Source = _testObject
            };
            
            _healthComponent.TakeDamage(damageRequest);
            
            Assert.AreEqual(75, _healthComponent.CurrentHealth);
            Assert.AreEqual(0.75f, _healthComponent.HealthPercentage, 0.01f);
            Assert.IsTrue(_healthComponent.IsAlive);
        }
        
        [Test]
        public void TakeDamage_MoreThanCurrentHealth_SetsHealthToZero()
        {
            var damageRequest = new DamageRequest
            {
                Amount = 150f,
                Source = _testObject
            };
            
            _healthComponent.TakeDamage(damageRequest);
            
            Assert.AreEqual(0, _healthComponent.CurrentHealth);
            Assert.AreEqual(0f, _healthComponent.HealthPercentage, 0.01f);
            Assert.IsFalse(_healthComponent.IsAlive);
        }
        
        [Test]
        public void TakeDamage_NegativeAmount_DoesNothing()
        {
            int initialHealth = _healthComponent.CurrentHealth;
            var damageRequest = new DamageRequest
            {
                Amount = -10f,
                Source = _testObject
            };
            
            bool result = _healthComponent.TakeDamage(damageRequest);
            
            Assert.IsFalse(result);
            Assert.AreEqual(initialHealth, _healthComponent.CurrentHealth);
        }
        
        [Test]
        public void Heal_ValidAmount_IncreasesHealth()
        {
            // First damage the object
            var damageRequest = new DamageRequest
            {
                Amount = 50f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            // Then heal
            _healthComponent.Heal(30);
            
            Assert.AreEqual(80, _healthComponent.CurrentHealth);
            Assert.AreEqual(0.8f, _healthComponent.HealthPercentage, 0.01f);
            Assert.IsTrue(_healthComponent.IsAlive);
        }
        
        [Test]
        public void Heal_MoreThanMaxHealth_ClampsToMaxHealth()
        {
            // Damage first
            var damageRequest = new DamageRequest
            {
                Amount = 25f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            // Overheal
            _healthComponent.Heal(50);
            
            Assert.AreEqual(100, _healthComponent.CurrentHealth);
            Assert.AreEqual(1f, _healthComponent.HealthPercentage, 0.01f);
        }
        
        [Test]
        public void Heal_WhenDead_DoesNothing()
        {
            // Kill the object
            var damageRequest = new DamageRequest
            {
                Amount = 150f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            // Try to heal
            bool result = _healthComponent.Heal(50);
            
            Assert.IsFalse(result);
            Assert.AreEqual(0, _healthComponent.CurrentHealth);
            Assert.IsFalse(_healthComponent.IsAlive);
        }
        
        [Test]
        public void ApplyDamage_ValidValue_UpdatesHealth()
        {
            _healthComponent.ApplyDamage(60);
            
            Assert.AreEqual(40, _healthComponent.CurrentHealth);
            Assert.AreEqual(0.4f, _healthComponent.HealthPercentage, 0.01f);
            Assert.IsTrue(_healthComponent.IsAlive);
        }
        
        [Test]
        public void ApplyDamage_NegativeValue_DoesNothing()
        {
            int initialHealth = _healthComponent.CurrentHealth;
            
            bool result = _healthComponent.ApplyDamage(-10);
            
            Assert.IsFalse(result);
            Assert.AreEqual(initialHealth, _healthComponent.CurrentHealth);
        }
        
        [Test]
        public void ResetToMaxHealth_RestoresFullHealth()
        {
            // Damage first
            var damageRequest = new DamageRequest
            {
                Amount = 40f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            // Reset to max
            _healthComponent.ResetToMaxHealth();
            
            Assert.AreEqual(100, _healthComponent.CurrentHealth);
            Assert.AreEqual(1f, _healthComponent.HealthPercentage, 0.01f);
            Assert.IsTrue(_healthComponent.IsAlive);
        }
        
        [Test]
        public void SetMaxHealth_ValidValue_UpdatesMaxHealth()
        {
            _healthComponent.SetMaxHealth(150);
            
            Assert.AreEqual(150, _healthComponent.MaxHealth);
            Assert.AreEqual(100, _healthComponent.CurrentHealth); // Should stay the same
        }
        
        [Test]
        public void SetMaxHealth_LowerThanCurrent_ClampsCurrentHealth()
        {
            _healthComponent.SetMaxHealth(50);
            
            Assert.AreEqual(50, _healthComponent.MaxHealth);
            Assert.AreEqual(50, _healthComponent.CurrentHealth);
            Assert.AreEqual(1f, _healthComponent.HealthPercentage, 0.01f);
        }
        
        #endregion
        
        #region Death Tests
        
        [Test]
        public void Death_TriggersDeathEvents()
        {
            bool deathEventFired = false;
            _healthComponent.OnDeath += (args) => deathEventFired = true;
            
            var damageRequest = new DamageRequest
            {
                Amount = 150f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            Assert.IsTrue(deathEventFired);
        }
        
        #endregion
        
        #region Health Change Events Tests
        
        [Test]
        public void HealthChange_TriggersHealthChangedEvent()
        {
            bool eventFired = false;
            int receivedOldHealth = 0;
            int receivedNewHealth = 0;
            
            _healthComponent.OnHealthChanged += (args) => {
                eventFired = true;
                receivedOldHealth = args.OldHealth;
                receivedNewHealth = args.NewHealth;
            };
            
            var damageRequest = new DamageRequest
            {
                Amount = 25f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual(100, receivedOldHealth);
            Assert.AreEqual(75, receivedNewHealth);
        }
        
        [Test]
        public void Heal_TriggersHealthChangedEvent()
        {
            // Damage first
            var damageRequest = new DamageRequest
            {
                Amount = 30f,
                Source = _testObject
            };
            _healthComponent.TakeDamage(damageRequest);
            
            bool eventFired = false;
            int receivedOldHealth = 0;
            int receivedNewHealth = 0;
            
            _healthComponent.OnHealthChanged += (args) => {
                eventFired = true;
                receivedOldHealth = args.OldHealth;
                receivedNewHealth = args.NewHealth;
            };
            
            _healthComponent.Heal(20);
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual(70, receivedOldHealth);
            Assert.AreEqual(90, receivedNewHealth);
        }
        
        #endregion
    }
}
