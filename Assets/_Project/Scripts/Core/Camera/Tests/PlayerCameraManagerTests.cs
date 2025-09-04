using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.Cinemachine;
using Laboratory.Core.Camera;

namespace Laboratory.Core.Camera.Tests
{
    /// <summary>
    /// Unit tests for PlayerCameraManager functionality
    /// </summary>
    public class PlayerCameraManagerTests
    {
        private GameObject _testPlayer;
        private GameObject _testCameraRig;
        private PlayerCameraManager _cameraManager;

        [SetUp]
        public void Setup()
        {
            // Create test player
            _testPlayer = new GameObject("TestPlayer");
            _testPlayer.transform.position = Vector3.zero;

            // Create camera rig with PlayerCameraManager
            _testCameraRig = new GameObject("CameraRig");
            var mockBrain = _testCameraRig.AddComponent<CinemachineBrain>();
            _cameraManager = _testCameraRig.AddComponent<PlayerCameraManager>();

            // Use reflection to set private fields for testing
            var brainField = typeof(PlayerCameraManager).GetField("_cinemachineBrain", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var playerField = typeof(PlayerCameraManager).GetField("_player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            brainField?.SetValue(_cameraManager, mockBrain);
            playerField?.SetValue(_cameraManager, _testPlayer.transform);

            // Create mock virtual cameras
            CreateMockVirtualCameras();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testPlayer != null)
                Object.DestroyImmediate(_testPlayer);
            if (_testCameraRig != null)
                Object.DestroyImmediate(_testCameraRig);
        }

        private void CreateMockVirtualCameras()
        {
            // Create mock virtual cameras for testing
            var followPlayerVC = new GameObject("FollowPlayerVC").AddComponent<CinemachineCamera>();
            var deathCamVC = new GameObject("DeathCamVC").AddComponent<CinemachineCamera>();
            var teammateVC = new GameObject("TeammateVC").AddComponent<CinemachineCamera>();
            var cinematicVC = new GameObject("CinematicVC").AddComponent<CinemachineCamera>();
            var spectatorVC = new GameObject("SpectatorVC").AddComponent<CinemachineCamera>();

            // Use reflection to set virtual camera fields
            var fields = typeof(PlayerCameraManager).GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.Name == "_followPlayerVC")
                    field.SetValue(_cameraManager, followPlayerVC);
                else if (field.Name == "_deathCamOrbitVC")
                    field.SetValue(_cameraManager, deathCamVC);
                else if (field.Name == "_followTeammateVC")
                    field.SetValue(_cameraManager, teammateVC);
                else if (field.Name == "_cinematicShotVC")
                    field.SetValue(_cameraManager, cinematicVC);
                else if (field.Name == "_freeSpectatorVC")
                    field.SetValue(_cameraManager, spectatorVC);
            }
        }

        [Test]
        public void SetCameraMode_FollowPlayer_SetsCorrectMode()
        {
            // Act
            _cameraManager.SetCameraMode(PlayerCameraManager.CameraMode.FollowPlayer);

            // Assert
            Assert.AreEqual(PlayerCameraManager.CameraMode.FollowPlayer, _cameraManager.CurrentMode);
        }

        [Test]
        public void SetCameraMode_DeathCamOrbit_SetsCorrectMode()
        {
            // Act
            _cameraManager.SetCameraMode(PlayerCameraManager.CameraMode.DeathCamOrbit);

            // Assert
            Assert.AreEqual(PlayerCameraManager.CameraMode.DeathCamOrbit, _cameraManager.CurrentMode);
        }

        [Test]
        public void AddTeammate_WithValidTeammate_AddsToList()
        {
            // Arrange
            var teammate = new GameObject("Teammate").transform;

            // Act
            _cameraManager.AddTeammate(teammate);

            // Assert
            Assert.Contains(teammate, _cameraManager.Teammates as System.Collections.IList);
            Assert.AreEqual(1, _cameraManager.Teammates.Count);

            // Cleanup
            Object.DestroyImmediate(teammate.gameObject);
        }

        [Test]
        public void AddTeammate_WithDuplicateTeammate_DoesNotDuplicate()
        {
            // Arrange
            var teammate = new GameObject("Teammate").transform;

            // Act
            _cameraManager.AddTeammate(teammate);
            _cameraManager.AddTeammate(teammate); // Add same teammate again

            // Assert
            Assert.AreEqual(1, _cameraManager.Teammates.Count);

            // Cleanup
            Object.DestroyImmediate(teammate.gameObject);
        }

        [Test]
        public void AddTeammate_WithNullTeammate_DoesNotAdd()
        {
            // Act
            _cameraManager.AddTeammate(null);

            // Assert
            Assert.AreEqual(0, _cameraManager.Teammates.Count);
        }

        [Test]
        public void RemoveTeammate_WithExistingTeammate_RemovesFromList()
        {
            // Arrange
            var teammate = new GameObject("Teammate").transform;
            _cameraManager.AddTeammate(teammate);

            // Act
            _cameraManager.RemoveTeammate(teammate);

            // Assert
            Assert.AreEqual(0, _cameraManager.Teammates.Count);

            // Cleanup
            Object.DestroyImmediate(teammate.gameObject);
        }

        [Test]
        public void SetTeammates_WithValidList_ReplacesTeammatesList()
        {
            // Arrange
            var teammate1 = new GameObject("Teammate1").transform;
            var teammate2 = new GameObject("Teammate2").transform;
            var newTeammates = new List<Transform> { teammate1, teammate2 };

            // Act
            _cameraManager.SetTeammates(newTeammates);

            // Assert
            Assert.AreEqual(2, _cameraManager.Teammates.Count);
            Assert.Contains(teammate1, _cameraManager.Teammates as System.Collections.IList);
            Assert.Contains(teammate2, _cameraManager.Teammates as System.Collections.IList);

            // Cleanup
            Object.DestroyImmediate(teammate1.gameObject);
            Object.DestroyImmediate(teammate2.gameObject);
        }

        [Test]
        public void CycleToNextTeammate_WithMultipleTeammates_AdvancesIndex()
        {
            // Arrange
            var teammate1 = new GameObject("Teammate1").transform;
            var teammate2 = new GameObject("Teammate2").transform;
            _cameraManager.AddTeammate(teammate1);
            _cameraManager.AddTeammate(teammate2);

            int initialIndex = _cameraManager.CurrentTeammateIndex;

            // Act
            _cameraManager.CycleToNextTeammate();

            // Assert
            Assert.AreNotEqual(initialIndex, _cameraManager.CurrentTeammateIndex);
            Assert.AreEqual((initialIndex + 1) % 2, _cameraManager.CurrentTeammateIndex);

            // Cleanup
            Object.DestroyImmediate(teammate1.gameObject);
            Object.DestroyImmediate(teammate2.gameObject);
        }

        [Test]
        public void SetTeammateIndex_WithValidIndex_SetsIndex()
        {
            // Arrange
            var teammate1 = new GameObject("Teammate1").transform;
            var teammate2 = new GameObject("Teammate2").transform;
            _cameraManager.AddTeammate(teammate1);
            _cameraManager.AddTeammate(teammate2);

            // Act
            _cameraManager.SetTeammateIndex(1);

            // Assert
            Assert.AreEqual(1, _cameraManager.CurrentTeammateIndex);
            Assert.AreEqual(teammate2, _cameraManager.CurrentTeammate);

            // Cleanup
            Object.DestroyImmediate(teammate1.gameObject);
            Object.DestroyImmediate(teammate2.gameObject);
        }

        [Test]
        public void CurrentTeammate_WithValidIndex_ReturnsCorrectTeammate()
        {
            // Arrange
            var teammate1 = new GameObject("Teammate1").transform;
            var teammate2 = new GameObject("Teammate2").transform;
            _cameraManager.AddTeammate(teammate1);
            _cameraManager.AddTeammate(teammate2);
            _cameraManager.SetTeammateIndex(1);

            // Act & Assert
            Assert.AreEqual(teammate2, _cameraManager.CurrentTeammate);

            // Cleanup
            Object.DestroyImmediate(teammate1.gameObject);
            Object.DestroyImmediate(teammate2.gameObject);
        }

        [Test]
        public void CurrentTeammate_WithNoTeammates_ReturnsNull()
        {
            // Act & Assert
            Assert.IsNull(_cameraManager.CurrentTeammate);
        }
    }
}