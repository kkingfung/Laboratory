using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Laboratory.Core.Character.Systems;
using Laboratory.Core.DI;

namespace Laboratory.Core.Character.Tests
{
    /// <summary>
    /// Comprehensive unit tests for AdvancedTargetSelector functionality
    /// </summary>
    public class AdvancedTargetSelectorTests
    {
        private GameObject _testSelector;
        private AdvancedTargetSelector _targetSelector;
        private ServiceContainer _mockServices;
        private Camera _testCamera;
        private List<GameObject> _testTargets;

        [SetUp]
        public void Setup()
        {
            // Create test selector
            _testSelector = new GameObject("TestSelector");
            _targetSelector = _testSelector.AddComponent<AdvancedTargetSelector>();

            // Create test camera
            var cameraGO = new GameObject("TestCamera");
            _testCamera = cameraGO.AddComponent<Camera>();

            // Set camera reference using reflection
            var cameraField = typeof(AdvancedTargetSelector).GetField("_playerCamera", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cameraField?.SetValue(_targetSelector, _testCamera);

            // Create mock services
            _mockServices = new ServiceContainer();

            // Initialize test targets list
            _testTargets = new List<GameObject>();

            // Initialize target selector
            _targetSelector.Initialize(_mockServices);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup test targets
            foreach (var target in _testTargets)
            {
                if (target != null)
                    Object.DestroyImmediate(target);
            }
            _testTargets.Clear();

            // Cleanup main objects
            if (_testSelector != null)
                Object.DestroyImmediate(_testSelector);
            if (_testCamera != null)
                Object.DestroyImmediate(_testCamera.gameObject);

            _mockServices?.Dispose();
        }

        private GameObject CreateTestTarget(string name, Vector3 position, int layer = 0)
        {
            var target = new GameObject(name);
            target.transform.position = position;
            target.layer = layer;
            
            // Add collider for detection
            target.AddComponent<BoxCollider>();
            
            _testTargets.Add(target);
            return target;
        }

        [Test]
        public void Initialize_WithValidServices_SetsInitializedState()
        {
            // Arrange
            var newSelector = _testSelector.AddComponent<AdvancedTargetSelector>();

            // Act
            newSelector.Initialize(_mockServices);

            // Assert
            Assert.IsTrue(newSelector.IsActive);
        }

        [Test]
        public void DetectionRadius_WhenSet_UpdatesProperty()
        {
            // Arrange
            float newRadius = 5f;

            // Act
            _targetSelector.DetectionRadius = newRadius;

            // Assert
            Assert.AreEqual(newRadius, _targetSelector.DetectionRadius);
        }

        [Test]
        public void ValidateTarget_WithNullTarget_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_targetSelector.ValidateTarget(null));
        }

        [Test]
        public void ValidateTarget_WithSelfTransform_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_targetSelector.ValidateTarget(_testSelector.transform));
        }

        [Test]
        public void ValidateTarget_WithValidTarget_ReturnsTrue()
        {
            // Arrange
            var target = CreateTestTarget("ValidTarget", Vector3.forward * 5f);

            // Act
            bool isValid = _targetSelector.ValidateTarget(target.transform);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void CalculateTargetScore_WithNullTarget_ReturnsZero()
        {
            // Act & Assert
            Assert.AreEqual(0f, _targetSelector.CalculateTargetScore(null));
        }

        [Test]
        public void HasTargets_WithNoTargets_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_targetSelector.HasTargets);
            Assert.AreEqual(0, _targetSelector.TargetCount);
        }

        [Test]
        public void ForceTargetUpdate_DoesNotThrowException()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _targetSelector.ForceTargetUpdate());
        }

        [Test]
        public void SetActive_WithFalse_DeactivatesSelector()
        {
            // Act
            _targetSelector.SetActive(false);

            // Assert
            Assert.IsFalse(_targetSelector.IsActive);
        }

        [Test]
        public void ClearCurrentTarget_WithExistingTarget_ClearsTarget()
        {
            // Act
            _targetSelector.ClearCurrentTarget();

            // Assert
            Assert.IsNull(_targetSelector.CurrentTarget);
        }

        [Test]
        public void GetClosestTarget_WithNoTargets_ReturnsNull()
        {
            // Act & Assert
            Assert.IsNull(_targetSelector.GetClosestTarget());
        }

        [Test]
        public void DetectedTargets_ReadOnlyProperty_IsNotNull()
        {
            // Act & Assert
            Assert.IsNotNull(_targetSelector.DetectedTargets);
            Assert.AreEqual(0, _targetSelector.DetectedTargets.Count);
        }

        [Test]
        public void Dispose_CleansUpResources()
        {
            // Act & Assert - should not throw exceptions
            Assert.DoesNotThrow(() => _targetSelector.Dispose());
            Assert.AreEqual(0, _targetSelector.TargetCount);
        }
    }
}