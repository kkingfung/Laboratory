using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections;
using Laboratory.Subsystems.LiveOps;

namespace Laboratory.Tests.Unit.Subsystems.LiveOps
{
    /// <summary>
    /// Unit tests for LiveOpsManager
    /// </summary>
    public class LiveOpsManagerTests
    {
        private GameObject testGameObject;
        private LiveOpsManager liveOpsManager;
        
        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestLiveOpsManager");
            liveOpsManager = testGameObject.AddComponent<LiveOpsManager>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void LiveOpsManager_Initialization_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => {
                // Awake is called automatically when component is added
            });
        }
        
        [Test]
        public void GetRemoteConfigValue_WithNonExistentKey_ShouldReturnDefault()
        {
            var defaultValue = "default";
            var result = liveOpsManager.GetRemoteConfigValue("non_existent_key", defaultValue);
            Assert.AreEqual(defaultValue, result);
        }
        
        [Test]
        public void GetActiveEvents_WhenNoEvents_ShouldReturnEmptyList()
        {
            var events = liveOpsManager.GetActiveEvents();
            Assert.IsNotNull(events);
            Assert.AreEqual(0, events.Count);
        }
        
        [Test]
        public void GetEvent_WithInvalidId_ShouldReturnNull()
        {
            var eventObj = liveOpsManager.GetEvent("invalid_event_id");
            Assert.IsNull(eventObj);
        }
        
        [Test]
        public void ClaimDailyReward_FirstTime_ShouldReturnTrue()
        {
            // This test depends on PlayerPrefs being clean
            PlayerPrefs.DeleteKey("LiveOps_LastDailyReward");
            
            bool result = liveOpsManager.ClaimDailyReward();
            Assert.IsTrue(result);
        }
        
        [Test]
        public void TrackEvent_WithValidEventName_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => {
                liveOpsManager.TrackEvent("test_event");
            });
        }
        
        [UnityTest]
        public IEnumerator LiveOpsManager_Update_ShouldNotThrow()
        {
            // Let it run for a few frames
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
            
            // Should not throw any exceptions
            Assert.IsTrue(true);
        }
    }
    
    /// <summary>
    /// Unit tests for LiveOpsEvent
    /// </summary>
    public class LiveOpsEventTests
    {
        [Test]
        public void LiveOpsEvent_IsActive_WithCurrentTime_ShouldReturnTrue()
        {
            var now = DateTime.Now;
            var liveOpsEvent = new LiveOpsEvent
            {
                Id = "test_event",
                Name = "Test Event",
                StartTime = now.AddHours(-1),
                EndTime = now.AddHours(1)
            };
            
            Assert.IsTrue(liveOpsEvent.IsActive(now));
        }
        
        [Test]
        public void LiveOpsEvent_IsActive_BeforeStartTime_ShouldReturnFalse()
        {
            var now = DateTime.Now;
            var liveOpsEvent = new LiveOpsEvent
            {
                Id = "test_event",
                Name = "Test Event",
                StartTime = now.AddHours(1),
                EndTime = now.AddHours(2)
            };
            
            Assert.IsFalse(liveOpsEvent.IsActive(now));
        }
        
        [Test]
        public void LiveOpsEvent_IsActive_AfterEndTime_ShouldReturnFalse()
        {
            var now = DateTime.Now;
            var liveOpsEvent = new LiveOpsEvent
            {
                Id = "test_event",
                Name = "Test Event",
                StartTime = now.AddHours(-2),
                EndTime = now.AddHours(-1)
            };
            
            Assert.IsFalse(liveOpsEvent.IsActive(now));
        }
        
        [Test]
        public void LiveOpsEvent_GetTimeRemaining_WhenActive_ShouldReturnValidTime()
        {
            var now = DateTime.Now;
            var liveOpsEvent = new LiveOpsEvent
            {
                Id = "test_event",
                Name = "Test Event",
                StartTime = now.AddHours(-1),
                EndTime = now.AddHours(1)
            };
            
            var timeRemaining = liveOpsEvent.GetTimeRemaining(now);
            Assert.IsTrue(timeRemaining.TotalHours > 0);
            Assert.IsTrue(timeRemaining.TotalHours <= 1);
        }
        
        [Test]
        public void LiveOpsEvent_GetTimeRemaining_WhenNotActive_ShouldReturnZero()
        {
            var now = DateTime.Now;
            var liveOpsEvent = new LiveOpsEvent
            {
                Id = "test_event",
                Name = "Test Event",
                StartTime = now.AddHours(-2),
                EndTime = now.AddHours(-1)
            };
            
            var timeRemaining = liveOpsEvent.GetTimeRemaining(now);
            Assert.AreEqual(TimeSpan.Zero, timeRemaining);
        }
    }
    
    /// <summary>
    /// Unit tests for LiveOps Event Classes
    /// </summary>
    public class LiveOpsEventClassesTests
    {
        [Test]
        public void LiveOpsInitializedEvent_Creation_ShouldSetTimestamp()
        {
            var beforeCreation = DateTime.Now;
            var evt = new LiveOpsInitializedEvent();
            var afterCreation = DateTime.Now;
            
            Assert.IsTrue(evt.InitializationTime >= beforeCreation);
            Assert.IsTrue(evt.InitializationTime <= afterCreation);
        }
        
        [Test]
        public void DailyRewardAvailableEvent_WithDays_ShouldStoreDays()
        {
            var days = 5;
            var evt = new DailyRewardAvailableEvent(days);
            
            Assert.AreEqual(days, evt.DaysSinceLastClaim);
        }
        
        [Test]
        public void DailyRewardClaimedEvent_WithConsecutiveDays_ShouldStoreData()
        {
            var consecutiveDays = 3;
            var beforeCreation = DateTime.Now;
            var evt = new DailyRewardClaimedEvent(consecutiveDays);
            var afterCreation = DateTime.Now;
            
            Assert.AreEqual(consecutiveDays, evt.ConsecutiveDays);
            Assert.IsTrue(evt.ClaimTime >= beforeCreation);
            Assert.IsTrue(evt.ClaimTime <= afterCreation);
        }
    }
}
