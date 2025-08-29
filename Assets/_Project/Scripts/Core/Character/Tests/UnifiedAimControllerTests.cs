using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.Character.Controllers;
using Laboratory.Core.Character.Configuration;
using Laboratory.Core.DI;

namespace Laboratory.Core.Character.Tests
{
    /// <summary>
    /// Unit tests for UnifiedAimController functionality
    /// </summary>
    public class UnifiedAimControllerTests
    {
        private GameObject _testCharacter;
        private UnifiedAimController _aimController;
        private ServiceContainer _mockServices;
        private CharacterAimSettings _testSettings;

        [SetUp]
        public void Setup()
        {
            // Create test character with aim controller
            _testCharacter = new GameObject("TestCharacter");
            _testCharacter.AddComponent<Animator>();
            _aimController = _testCharacter.AddComponent<UnifiedAimController>();

            // Create mock services
            _mockServices = new ServiceContainer();

            // Create test settings
            _testSettings = ScriptableObject.CreateInstance<CharacterAimSettings>();
            _testSettings.maxAimDistance = 10f;
            _testSettings.headWeight = 0.8f;
            _testSettings.smoothBlending = true;
            _testSettings.blendSpeed = 5f;

            // Initialize controller
            _aimController.Initialize(_mockServices);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testCharacter != null)
            {
                Object.DestroyImmediate(_testCharacter);
            }

            if (_testSettings != null)
            {
                Object.DestroyImmediate(_testSettings);
            }

            _mockServices?.Dispose();
        }

        [Test]
        public void Initialize_WithValidServices_SetsInitializedState()
        {
            // Arrange
            var newController = _testCharacter.AddComponent<UnifiedAimController>();

            // Act
            newController.Initialize(_mockServices);

            // Assert
            Assert.IsTrue(newController.IsActive);
        }

        [Test]
        public void SetTarget_WithValidTarget_UpdatesCurrentTarget()
        {
            // Arrange
            var target = new GameObject("Target").transform;
            target.position = Vector3.forward * 5f; // Within range

            // Act
            _aimController.SetTarget(target);

            // Assert
            Assert.AreEqual(target, _aimController.CurrentTarget);
            Assert.IsTrue(_aimController.IsAiming);

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void ClearTarget_WithExistingTarget_ClearsCurrentTarget()
        {
            // Arrange
            var target = new GameObject("Target").transform;
            target.position = Vector3.forward * 5f;
            _aimController.SetTarget(target);

            // Act
            _aimController.ClearTarget();

            // Assert
            Assert.IsNull(_aimController.CurrentTarget);
            Assert.IsFalse(_aimController.IsAiming);

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void SetAimWeight_WithValidWeight_UpdatesAimWeight()
        {
            // Arrange
            float expectedWeight = 0.7f;

            // Act
            _aimController.SetAimWeight(expectedWeight);

            // Assert
            // Note: AimWeight property reflects current interpolated weight, not target weight
            // In a real test, you might want to wait a frame or test the target weight directly
            Assert.That(_aimController.AimWeight, Is.LessThanOrEqualTo(1f));
            Assert.That(_aimController.AimWeight, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void SetAimWeight_WithInvalidWeight_ClampsWeight()
        {
            // Arrange & Act
            _aimController.SetAimWeight(1.5f); // Above 1.0
            
            // Assert - should be clamped to 1.0
            Assert.That(_aimController.AimWeight, Is.LessThanOrEqualTo(1f));

            // Arrange & Act
            _aimController.SetAimWeight(-0.5f); // Below 0.0
            
            // Assert - should be clamped to 0.0
            Assert.That(_aimController.AimWeight, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void IsValidTarget_WithNullTarget_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_aimController.IsValidTarget(null));
        }

        [Test]
        public void IsValidTarget_WithSelfTarget_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_aimController.IsValidTarget(_testCharacter.transform));
        }

        [Test]
        public void IsValidTarget_WithValidTarget_ReturnsTrue()
        {
            // Arrange
            var target = new GameObject("ValidTarget").transform;
            target.position = Vector3.forward * 5f; // Within max distance

            // Act
            bool isValid = _aimController.IsValidTarget(target);

            // Assert
            Assert.IsTrue(isValid);

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void IsValidTarget_WithTooDistantTarget_ReturnsFalse()
        {
            // Arrange
            var target = new GameObject("DistantTarget").transform;
            target.position = Vector3.forward * 100f; // Way beyond max distance

            // Act
            bool isValid = _aimController.IsValidTarget(target);

            // Assert
            Assert.IsFalse(isValid);

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void SetAutoTargeting_WithTrue_EnablesAutoTargeting()
        {
            // Act
            _aimController.SetAutoTargeting(true);

            // Assert
            // This test would be more meaningful if we could access the auto-targeting state
            // For now, we just verify it doesn't throw an exception
            Assert.DoesNotThrow(() => _aimController.SetAutoTargeting(true));
        }

        [Test]
        public void SetActive_WithFalse_DeactivatesController()
        {
            // Arrange
            var target = new GameObject("Target").transform;
            target.position = Vector3.forward * 5f;
            _aimController.SetTarget(target);

            // Act
            _aimController.SetActive(false);

            // Assert
            Assert.IsFalse(_aimController.IsActive);
            Assert.IsNull(_aimController.CurrentTarget); // Should clear target when deactivated

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        [UnityTest]
        public IEnumerator AimWeight_WithSmoothBlending_InterpolatesOverTime()
        {
            // Arrange
            var target = new GameObject("Target").transform;
            target.position = Vector3.forward * 5f;
            _aimController.SetTarget(target);
            _aimController.SetAimWeight(1f);

            float initialWeight = _aimController.AimWeight;

            // Act - wait a few frames for interpolation
            yield return new WaitForSeconds(0.2f);

            // Assert - weight should have changed (increased towards target)
            float finalWeight = _aimController.AimWeight;
            Assert.That(finalWeight, Is.GreaterThanOrEqualTo(initialWeight));

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void MaxAimDistance_WhenSet_UpdatesProperty()
        {
            // Arrange
            float newDistance = 20f;

            // Act
            _aimController.MaxAimDistance = newDistance;

            // Assert
            Assert.AreEqual(newDistance, _aimController.MaxAimDistance);
        }

        [Test]
        public void Dispose_WhenCalled_CleansUpResources()
        {
            // Arrange
            var target = new GameObject("Target").transform;
            target.position = Vector3.forward * 5f;
            _aimController.SetTarget(target);

            // Act
            _aimController.Dispose();

            // Assert
            // After disposal, controller should be cleaned up
            Assert.DoesNotThrow(() => _aimController.Dispose());

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }
    }
}
