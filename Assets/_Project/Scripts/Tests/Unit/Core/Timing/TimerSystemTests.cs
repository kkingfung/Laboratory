using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.Timing;

namespace Laboratory.Tests.Core.Timing
{
    /// <summary>
    /// Comprehensive unit tests for the Laboratory Timer System.
    /// Tests all timer implementations and their integration points.
    /// </summary>
    [TestFixture]
    public class TimerSystemTests
    {
        #region CooldownTimer Tests

        [Test]
        public void CooldownTimer_Constructor_ValidParameters_SetsCorrectValues()
        {
            // Arrange & Act
            var timer = new CooldownTimer(5f, autoRegister: false);
            
            // Assert
            Assert.AreEqual(5f, timer.Duration);
            Assert.AreEqual(0f, timer.Remaining);
            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(1f, timer.Progress, 0.001f); // Progress is 1 when not active
            
            timer.Dispose();
        }

        [Test]
        public void CooldownTimer_Constructor_NegativeDuration_ThrowsException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new CooldownTimer(-1f, autoRegister: false));
        }

        [Test]
        public void CooldownTimer_Start_SetsCorrectState()
        {
            // Arrange
            var timer = new CooldownTimer(3f, autoRegister: false);
            
            // Act
            timer.Start();
            
            // Assert
            Assert.AreEqual(3f, timer.Remaining, 0.001f);
            Assert.AreEqual(0f, timer.Elapsed, 0.001f);
            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(0f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void CooldownTimer_TryStart_WhenNotActive_ReturnsTrue()
        {
            // Arrange
            var timer = new CooldownTimer(2f, autoRegister: false);
            
            // Act
            bool result = timer.TryStart();
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void CooldownTimer_TryStart_WhenActive_ReturnsFalse()
        {
            // Arrange
            var timer = new CooldownTimer(2f, autoRegister: false);
            timer.Start();
            
            // Act
            bool result = timer.TryStart();
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsTrue(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void CooldownTimer_Tick_ReducesRemainingTime()
        {
            // Arrange
            var timer = new CooldownTimer(5f, autoRegister: false);
            timer.Start();
            
            // Act
            timer.Tick(2f);
            
            // Assert
            Assert.AreEqual(3f, timer.Remaining, 0.001f);
            Assert.AreEqual(2f, timer.Elapsed, 0.001f);
            Assert.AreEqual(0.4f, timer.Progress, 0.001f);
            Assert.IsTrue(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void CooldownTimer_Tick_CompletesTimer_FiresOnCompleted()
        {
            // Arrange
            var timer = new CooldownTimer(1f, autoRegister: false);
            bool completedFired = false;
            timer.OnCompleted += () => completedFired = true;
            timer.Start();
            
            // Act
            timer.Tick(1.5f); // Tick longer than duration
            
            // Assert
            Assert.IsTrue(completedFired);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.Remaining, 0.001f);
            Assert.AreEqual(1f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void CooldownTimer_Tick_FiresOnTickEvent()
        {
            // Arrange
            var timer = new CooldownTimer(2f, autoRegister: false);
            float lastElapsed = -1f;
            timer.OnTick += (elapsed) => lastElapsed = elapsed;
            timer.Start();
            
            // Act
            timer.Tick(0.5f);
            
            // Assert
            Assert.AreEqual(0.5f, lastElapsed, 0.001f);
            
            timer.Dispose();
        }

        #endregion

        #region CountdownTimer Tests

        [Test]
        public void CountdownTimer_Constructor_SetsCorrectValues()
        {
            // Arrange & Act
            var timer = new CountdownTimer(10f, autoRegister: false);
            
            // Assert
            Assert.AreEqual(10f, timer.Duration);
            Assert.AreEqual(10f, timer.Remaining);
            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Start_ActivatesTimer()
        {
            // Arrange
            var timer = new CountdownTimer(5f, autoRegister: false);
            
            // Act
            timer.Start();
            
            // Assert
            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(5f, timer.Remaining);
            
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Stop_DeactivatesTimer()
        {
            // Arrange
            var timer = new CountdownTimer(5f, autoRegister: false);
            timer.Start();
            
            // Act
            timer.Stop();
            
            // Assert
            Assert.IsFalse(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Tick_CountsDownCorrectly()
        {
            // Arrange
            var timer = new CountdownTimer(10f, autoRegister: false);
            timer.Start();
            
            // Act
            timer.Tick(3f);
            
            // Assert
            Assert.AreEqual(7f, timer.Remaining, 0.001f);
            Assert.AreEqual(3f, timer.Elapsed, 0.001f);
            Assert.AreEqual(0.3f, timer.Progress, 0.001f);
            Assert.IsTrue(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_SetRemainingTime_UpdatesCorrectly()
        {
            // Arrange
            var timer = new CountdownTimer(10f, autoRegister: false);
            
            // Act
            timer.SetRemainingTime(3f);
            
            // Assert
            Assert.AreEqual(3f, timer.Remaining, 0.001f);
            Assert.AreEqual(7f, timer.Elapsed, 0.001f);
            Assert.AreEqual(0.7f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Reset_RestoresInitialState()
        {
            // Arrange
            var timer = new CountdownTimer(8f, autoRegister: false);
            timer.Start();
            timer.Tick(2f);
            
            // Act
            timer.Reset();
            
            // Assert
            Assert.AreEqual(8f, timer.Remaining);
            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsFalse(timer.IsActive);
            
            timer.Dispose();
        }

        #endregion

        #region ProgressTimer Tests

        [Test]
        public void ProgressTimer_Constructor_DefaultValues_SetsCorrectState()
        {
            // Arrange & Act
            var timer = new ProgressTimer(autoRegister: false);
            
            // Assert
            Assert.AreEqual(0f, timer.Duration);
            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(1f, timer.Progress, 0.001f); // Default progress is 1 for zero duration
            
            timer.Dispose();
        }

        [Test]
        public void ProgressTimer_Constructor_WithDuration_SetsCorrectState()
        {
            // Arrange & Act
            var timer = new ProgressTimer(duration: 5f, autoProgress: true, autoRegister: false);
            
            // Assert
            Assert.AreEqual(5f, timer.Duration);
            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void ProgressTimer_SetProgress_UpdatesCorrectly()
        {
            // Arrange
            var timer = new ProgressTimer(autoRegister: false);
            
            // Act
            timer.SetProgress(0.7f);
            
            // Assert
            Assert.AreEqual(0.7f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void ProgressTimer_SetProgress_FiresProgressChangedEvent()
        {
            // Arrange
            var timer = new ProgressTimer(autoRegister: false);
            float lastProgress = -1f;
            timer.OnProgressChanged += (progress) => lastProgress = progress;
            
            // Act
            timer.SetProgress(0.4f);
            
            // Assert
            Assert.AreEqual(0.4f, lastProgress, 0.001f);
            
            timer.Dispose();
        }

        [Test]
        public void ProgressTimer_SetProgress_ToOne_FiresCompletedEvent()
        {
            // Arrange
            var timer = new ProgressTimer(autoRegister: false);
            bool completedFired = false;
            timer.OnCompleted += () => completedFired = true;
            
            // Act
            timer.SetProgress(1f);
            
            // Assert
            Assert.IsTrue(completedFired);
            Assert.IsFalse(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void ProgressTimer_AutoProgress_UpdatesOverTime()
        {
            // Arrange
            var timer = new ProgressTimer(duration: 4f, autoProgress: true, autoRegister: false);
            timer.Start();
            
            // Act
            timer.Tick(1f);
            
            // Assert
            Assert.AreEqual(0.25f, timer.Progress, 0.001f);
            Assert.AreEqual(1f, timer.Elapsed, 0.001f);
            Assert.IsTrue(timer.IsActive);
            
            timer.Dispose();
        }

        [Test]
        public void ProgressTimer_SetDuration_UpdatesCorrectly()
        {
            // Arrange
            var timer = new ProgressTimer(autoRegister: false);
            
            // Act
            timer.SetDuration(10f);
            
            // Assert
            Assert.AreEqual(10f, timer.Duration);
            
            timer.Dispose();
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void AllTimers_ZeroDuration_HandlesCorrectly()
        {
            // Arrange & Act
            var cooldown = new CooldownTimer(0f, autoRegister: false);
            var countdown = new CountdownTimer(0f, autoRegister: false);
            var progress = new ProgressTimer(0f, autoRegister: false);
            
            // Assert
            Assert.AreEqual(1f, cooldown.Progress, 0.001f);
            Assert.AreEqual(1f, countdown.Progress, 0.001f);
            Assert.AreEqual(1f, progress.Progress, 0.001f);
            
            cooldown.Dispose();
            countdown.Dispose();
            progress.Dispose();
        }

        [Test]
        public void ProgressTimer_SetProgress_OutOfRange_ClampsCorrectly()
        {
            // Arrange
            var timer = new ProgressTimer(autoRegister: false);
            
            // Act & Assert - Test values above 1
            timer.SetProgress(1.5f);
            Assert.AreEqual(1f, timer.Progress, 0.001f);
            
            // Act & Assert - Test negative values
            timer.SetProgress(-0.5f);
            Assert.AreEqual(0f, timer.Progress, 0.001f);
            
            timer.Dispose();
        }

        #endregion

        #region Performance Tests

        [Test]
        public void Timer_CreateAndDispose_ManyTimes_PerformsWell()
        {
            // Arrange
            const int timerCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < timerCount; i++)
            {
                var timer = new CooldownTimer(1f, autoRegister: false);
                timer.Start();
                timer.Tick(0.5f);
                timer.Dispose();
            }
            
            stopwatch.Stop();
            
            // Assert - Should complete in reasonable time (less than 100ms)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, 
                $"Creating and disposing {timerCount} timers took {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Teardown

        [TearDown]
        public void TearDown()
        {
            // Ensure any test timers are cleaned up
            // In a real implementation, this might involve clearing the TimerService
        }

        #endregion
    }

    /// <summary>
    /// Integration tests for TimerService and timer coordination.
    /// These tests require the Unity environment and test scene setup.
    /// </summary>
    [TestFixture]
    public class TimerServiceIntegrationTests
    {
        private GameObject _testObject;
        private TimerService _timerService;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestTimerService");
            _timerService = _testObject.AddComponent<TimerService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        [UnityTest]
        public IEnumerator TimerService_RegisteredTimer_UpdatesAutomatically()
        {
            // Arrange
            var timer = new CooldownTimer(1f, autoRegister: true);
            bool completed = false;
            timer.OnCompleted += () => completed = true;
            timer.Start();

            // Act - Wait for timer to complete
            yield return new WaitForSeconds(1.2f);

            // Assert
            Assert.IsTrue(completed);
            Assert.IsFalse(timer.IsActive);

            timer.Dispose();
        }

        [Test]
        public void TimerService_GetActiveTimerCount_ReturnsCorrectCount()
        {
            // Arrange
            Assert.AreEqual(0, _timerService.GetActiveTimerCount());

            var timer1 = new CooldownTimer(5f, autoRegister: true);
            var timer2 = new CooldownTimer(5f, autoRegister: true);
            timer1.Start();
            timer2.Start();

            // Act & Assert
            Assert.AreEqual(2, _timerService.GetActiveTimerCount());

            timer1.Stop();
            timer1.Dispose();
            timer2.Dispose();
        }
    }
}