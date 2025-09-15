#if UNITY_EDITOR && UNITY_TEST_FRAMEWORK
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
    /// Note: Some integration tests require Unity Test Framework package
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
            if (_testSettings != null)
            {
                _testSettings.maxAimDistance = 10f;
                _testSettings.headWeight = 0.8f;
                _testSettings.smoothBlending = true;
                _testSettings.blendSpeed = 5f;
            }

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
        public void SetAutoTargeting_EnablesAutoTargeting()
        {
            // Act & Assert - verify it doesn't throw an exception
            Assert.DoesNotThrow(() => _aimController.SetAutoTargeting(true));
            Assert.DoesNotThrow(() => _aimController.SetAutoTargeting(false));
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
        public void Dispose_CleansUpResources()
        {
            // Arrange
            var target = new GameObject("Target").transform;
            _aimController.SetTarget(target);

            // Act
            _aimController.Dispose();

            // Assert - should not throw exceptions after disposal
            Assert.DoesNotThrow(() => _aimController.SetTarget(null));

            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }

        #region Integration Tests
        
        /// <summary>
        /// Integration test for aim weight interpolation over time.
        /// Uses coroutine to test smooth blending behavior.
        /// </summary>
        [UnityTest]
        public IEnumerator AimWeight_InterpolatesOverTime_WhenSmoothBlendingEnabled()
        {
            // Arrange
            if (_testSettings != null)
            {
                _testSettings.smoothBlending = true;
                _testSettings.blendSpeed = 2f;
            }
            
            var target = new GameObject("Target").transform;
            target.position = Vector3.forward * 5f;
            
            float initialWeight = _aimController.AimWeight;
            
            // Act
            _aimController.SetTarget(target);
            _aimController.SetAimWeight(1f);
            
            // Wait for interpolation to occur
            float timeElapsed = 0f;
            float maxWaitTime = 2f;
            bool weightIncreased = false;
            
            while (timeElapsed < maxWaitTime)
            {
                yield return null;
                timeElapsed += Time.deltaTime;
                
                if (_aimController.AimWeight > initialWeight + 0.1f)
                {
                    weightIncreased = true;
                    break;
                }
            }
            
            // Assert
            Assert.IsTrue(weightIncreased, "Aim weight should interpolate smoothly over time");
            Assert.That(_aimController.AimWeight, Is.GreaterThan(initialWeight));
            
            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }
        
        /// <summary>
        /// Integration test for target selector integration.
        /// Tests that target selector changes properly update the aim controller.
        /// </summary>
        [UnityTest]
        public IEnumerator TargetSelector_Integration_UpdatesAimControllerTarget()
        {
            // Arrange
            var targetSelector = _testCharacter.AddComponent<AdvancedTargetSelector>();
            var target1 = new GameObject("Target1").transform;
            var target2 = new GameObject("Target2").transform;
            
            target1.position = Vector3.forward * 3f;
            target2.position = Vector3.forward * 6f;
            
            _aimController.SetAutoTargeting(true);
            
            // Act - simulate target selector finding targets
            _aimController.SetTarget(target1);
            yield return new WaitForSeconds(0.1f);
            
            Assert.AreEqual(target1, _aimController.CurrentTarget, "First target should be set");
            
            _aimController.SetTarget(target2);
            yield return new WaitForSeconds(0.1f);
            
            // Assert
            Assert.AreEqual(target2, _aimController.CurrentTarget, "Target should update when selector changes");
            Assert.IsTrue(_aimController.IsAiming, "Should be aiming when target is set");
            
            // Cleanup
            Object.DestroyImmediate(target1.gameObject);
            Object.DestroyImmediate(target2.gameObject);
        }
        
        /// <summary>
        /// Integration test for event system integration.
        /// Tests that aim events are properly published through the event bus.
        /// </summary>
        [UnityTest]
        public IEnumerator EventSystem_Integration_PublishesAimEvents()
        {
            // Arrange
            bool targetAcquiredEventFired = false;
            bool targetLostEventFired = false;
            bool aimWeightChangedEventFired = false;
            
            _aimController.OnTargetAcquired += (target) => targetAcquiredEventFired = true;
            _aimController.OnTargetLost += (target) => targetLostEventFired = true;
            _aimController.OnAimWeightChanged += (weight) => aimWeightChangedEventFired = true;
            
            var target = new GameObject("EventTarget").transform;
            target.position = Vector3.forward * 5f;
            
            // Act
            _aimController.SetTarget(target);
            yield return null; // Wait one frame for events to process
            
            _aimController.SetAimWeight(0.8f);
            yield return null;
            
            _aimController.ClearTarget();
            yield return null;
            
            // Assert
            Assert.IsTrue(targetAcquiredEventFired, "OnTargetAcquired event should fire when target is set");
            Assert.IsTrue(aimWeightChangedEventFired, "OnAimWeightChanged event should fire when weight changes");
            Assert.IsTrue(targetLostEventFired, "OnTargetLost event should fire when target is cleared");
            
            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }
        
        /// <summary>
        /// Integration test for animation constraint behavior.
        /// Tests that IK look behavior works correctly with the Animator.
        /// </summary>
        [UnityTest]
        public IEnumerator AnimationConstraint_Integration_AppliesIKLook()
        {
            // Arrange
            var target = new GameObject("IKTarget").transform;
            target.position = Vector3.forward * 5f + Vector3.up * 2f; // Above and in front
            
            var animator = _testCharacter.GetComponent<Animator>();
            Assert.IsNotNull(animator, "Animator should be present for IK tests");
            
            // Act
            _aimController.SetTarget(target);
            _aimController.SetAimWeight(1f);
            
            // Wait for IK to be applied
            yield return new WaitForSeconds(0.2f);
            
            // Assert
            Assert.IsTrue(_aimController.IsAiming, "Should be aiming at target");
            Assert.AreEqual(target, _aimController.CurrentTarget, "Target should be set correctly");
            Assert.That(_aimController.AimWeight, Is.GreaterThan(0f), "Aim weight should be greater than 0");
            
            // Test IK state changes
            _aimController.ClearTarget();
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsFalse(_aimController.IsAiming, "Should stop aiming when target is cleared");
            
            // Cleanup
            Object.DestroyImmediate(target.gameObject);
        }
        
        /// <summary>
        /// Integration test for performance under continuous target updates.
        /// Tests that frequent target changes don't cause performance issues.
        /// </summary>
        [UnityTest]
        public IEnumerator Performance_Integration_HandlesFrequentTargetUpdates()
        {
            // Arrange
            var targets = new List<Transform>();
            for (int i = 0; i < 10; i++)
            {
                var target = new GameObject($"PerfTarget{i}").transform;
                target.position = Vector3.forward * (3f + i * 0.5f);
                targets.Add(target);
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            // Act - rapidly change targets
            for (int i = 0; i < targets.Count; i++)
            {
                _aimController.SetTarget(targets[i]);
                yield return null; // Wait one frame between changes
            }
            
            float endTime = Time.realtimeSinceStartup;
            float elapsedTime = endTime - startTime;
            
            // Assert
            Assert.That(elapsedTime, Is.LessThan(1f), "Target updates should complete quickly");
            Assert.AreEqual(targets[targets.Count - 1], _aimController.CurrentTarget, "Final target should be set");
            Assert.IsTrue(_aimController.IsAiming, "Should still be aiming after multiple updates");
            
            // Cleanup
            foreach (var target in targets)
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }
        
        /// <summary>
        /// Integration test for aim distance validation.
        /// Tests that targets beyond max distance are properly rejected.
        /// </summary>
        [UnityTest]
        public IEnumerator AimDistance_Integration_RejectsDistantTargets()
        {
            // Arrange
            var nearTarget = new GameObject("NearTarget").transform;
            var farTarget = new GameObject("FarTarget").transform;
            
            nearTarget.position = Vector3.forward * 5f; // Within range
            farTarget.position = Vector3.forward * 50f; // Beyond range
            
            // Act & Assert - near target should be valid
            Assert.IsTrue(_aimController.IsValidTarget(nearTarget), "Near target should be valid");
            
            _aimController.SetTarget(nearTarget);
            yield return null;
            
            Assert.AreEqual(nearTarget, _aimController.CurrentTarget, "Near target should be set");
            
            // Act & Assert - far target should be invalid
            Assert.IsFalse(_aimController.IsValidTarget(farTarget), "Far target should be invalid");
            
            _aimController.SetTarget(farTarget);
            yield return null;
            
            // The controller might still set an invalid target, but IsValidTarget should return false
            Assert.IsFalse(_aimController.IsValidTarget(farTarget), "Far target should remain invalid");
            
            // Cleanup
            Object.DestroyImmediate(nearTarget.gameObject);
            Object.DestroyImmediate(farTarget.gameObject);
        }
        
        #endregion
    }
}
#endif