using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Core.Timing;

namespace Laboratory.Tests.AsyncUtils
{
    /// <summary>
    /// Integration tests for AsyncUtils components with the Timer System.
    /// </summary>
    [TestFixture]
    public class AsyncUtilsIntegrationTests
    {
        private GameObject _testCanvas;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            // Create test UI components
            _testCanvas = new GameObject("TestCanvas");
            var canvas = _testCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _canvasGroup = _testCanvas.AddComponent<CanvasGroup>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testCanvas != null)
            {
                UnityEngine.Object.DestroyImmediate(_testCanvas);
            }
        }

        [Test]
        public void LoadingScreen_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var loadingScreen = new LoadingScreen(_canvasGroup);

            // Assert
            Assert.IsNotNull(loadingScreen);
            Assert.AreEqual(0f, loadingScreen.Progress);
            Assert.IsFalse(loadingScreen.IsVisible);
            Assert.IsNull(loadingScreen.CurrentStatus);

            loadingScreen.Dispose();
        }

        [Test]
        public void LoadingScreen_Show_MakesVisible()
        {
            // Arrange
            var loadingScreen = new LoadingScreen(_canvasGroup);
            
            // Act
            loadingScreen.Show("Test Loading");
            
            // Assert
            Assert.IsTrue(loadingScreen.IsVisible);
            Assert.AreEqual("Test Loading", loadingScreen.CurrentStatus);
            
            loadingScreen.Dispose();
        }

        [Test]
        public void LoadingScreen_Hide_MakesInvisible()
        {
            // Arrange
            var loadingScreen = new LoadingScreen(_canvasGroup);
            loadingScreen.Show("Test");
            
            // Act
            loadingScreen.Hide();
            
            // Assert
            Assert.IsFalse(loadingScreen.IsVisible);
            
            loadingScreen.Dispose();
        }

        [Test]
        public void LoadingScreen_SetProgress_UpdatesCorrectly()
        {
            // Arrange
            var loadingScreen = new LoadingScreen(_canvasGroup);
            
            // Act
            loadingScreen.SetProgress(0.75f);
            
            // Assert
            Assert.AreEqual(0.75f, loadingScreen.Progress, 0.001f);
            
            loadingScreen.Dispose();
        }

        [Test]
        public void LoadingScreen_UpdateStatus_ChangesStatus()
        {
            // Arrange
            var loadingScreen = new LoadingScreen(_canvasGroup);
            string statusChanged = null;
            loadingScreen.OnStatusChanged += (status) => statusChanged = status;
            
            // Act
            loadingScreen.UpdateStatus("New Status");
            
            // Assert
            Assert.AreEqual("New Status", loadingScreen.CurrentStatus);
            Assert.AreEqual("New Status", statusChanged);
            
            loadingScreen.Dispose();
        }
    }
}