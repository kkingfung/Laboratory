using NUnit.Framework;
using Laboratory.Core.Timing;
using UnityEngine;

namespace Laboratory.Tests
{
    /// <summary>
    /// Tests for the enhanced timer system to ensure proper functionality.
    /// </summary>
    public class TimerSystemTests
    {
        [Test]
        public void CooldownTimer_StartsInactive_IsCorrect()
        {
            var timer = new CooldownTimer(5f, autoRegister: false);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.Remaining);
            Assert.AreEqual(5f, timer.Duration);
            timer.Dispose();
        }
        
        [Test]
        public void CooldownTimer_AfterStart_IsActive()
        {
            var timer = new CooldownTimer(5f, autoRegister: false);
            timer.Start();
            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(5f, timer.Remaining, 0.1f);
            timer.Dispose();
        }
        
        [Test]
        public void CooldownTimer_AfterTick_ReducesRemaining()
        {
            var timer = new CooldownTimer(5f, autoRegister: false);
            timer.Start();
            timer.Tick(1f);
            Assert.AreEqual(4f, timer.Remaining, 0.1f);
            Assert.AreEqual(1f, timer.Elapsed, 0.1f);
            timer.Dispose();
        }
        
        [Test]
        public void CountdownTimer_CountsDownCorrectly()
        {
            var timer = new CountdownTimer(10f, autoRegister: false);
            timer.Start();
            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(10f, timer.Remaining, 0.1f);
            
            timer.Tick(3f);
            Assert.AreEqual(7f, timer.Remaining, 0.1f);
            timer.Dispose();
        }
        
        [Test]
        public void ProgressTimer_SetProgressWorks()
        {
            var timer = new ProgressTimer(autoRegister: false);
            timer.SetProgress(0.5f);
            Assert.AreEqual(0.5f, timer.Progress, 0.01f);
            timer.Dispose();
        }
        
        [Test]
        public void CooldownTimer_TryStart_PreventsDoubleStart()
        {
            var timer = new CooldownTimer(5f, autoRegister: false);
            
            // First start should succeed
            Assert.IsTrue(timer.TryStart());
            Assert.IsTrue(timer.IsActive);
            
            // Second start should fail
            Assert.IsFalse(timer.TryStart());
            
            timer.Dispose();
        }
    }
}
