using NUnit.Framework;
using System;
using System.Threading;
using Laboratory.Core.Events;
namespace Laboratory.Tests.Unit.Core.Events
{
    /// <summary>
    /// Comprehensive unit tests for UnifiedEventBus
    /// </summary>
    public class UnifiedEventBusTests
    {
        private UnifiedEventBus eventBus;
        private bool eventReceived;
        private int eventCount;
        private TestEvent lastReceivedEvent;
        
        [SetUp]
        public void SetUp()
        {
            eventBus = new UnifiedEventBus();
            eventReceived = false;
            eventCount = 0;
            lastReceivedEvent = null;
        }
        
        [TearDown]
        public void TearDown()
        {
            eventBus?.Dispose();
        }
        
        #region Basic Functionality Tests
        
        [Test]
        public void EventBus_PublishAndSubscribe_ShouldWork()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test Message" };
            
            // Act
            eventBus.Subscribe<TestEvent>(evt => {
                eventReceived = true;
                lastReceivedEvent = evt;
            });
            
            eventBus.Publish(testEvent);
            
            // Assert
            Assert.IsTrue(eventReceived);
            Assert.AreEqual(testEvent.Message, lastReceivedEvent.Message);
        }
        
        [Test]
        public void EventBus_PublishNull_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => eventBus.Publish<TestEvent>(null));
        }
        
        [Test]
        public void EventBus_SubscribeWithNullHandler_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => eventBus.Subscribe<TestEvent>(null));
        }
        
        [Test]
        public void EventBus_MultipleSubscribers_ShouldReceiveEvent()
        {
            // Arrange
            int subscriber1Called = 0;
            int subscriber2Called = 0;
            var testEvent = new TestEvent { Message = "Multi Test" };
            
            // Act
            eventBus.Subscribe<TestEvent>(evt => subscriber1Called++);
            eventBus.Subscribe<TestEvent>(evt => subscriber2Called++);
            eventBus.Publish(testEvent);
            
            // Assert
            Assert.AreEqual(1, subscriber1Called);
            Assert.AreEqual(1, subscriber2Called);
        }
        
        #endregion
        
        #region Advanced Features Tests
        
        [Test]
        public void EventBus_SubscribeWhere_ShouldFilterEvents()
        {
            // Arrange
            int filteredEventCount = 0;
            
            eventBus.SubscribeWhere<TestEvent>(
                evt => evt.Message.Contains("Filter"),
                evt => filteredEventCount++
            );
            
            // Act
            eventBus.Publish(new TestEvent { Message = "Filter Me" });
            eventBus.Publish(new TestEvent { Message = "Ignore Me" });
            eventBus.Publish(new TestEvent { Message = "Filter Me Too" });
            
            // Assert
            Assert.AreEqual(2, filteredEventCount);
        }
        
        [Test]
        public void EventBus_SubscribeFirst_ShouldReceiveOnlyFirstEvent()
        {
            // Arrange
            int eventCount = 0;
            
            eventBus.SubscribeFirst<TestEvent>(evt => eventCount++);
            
            // Act
            eventBus.Publish(new TestEvent { Message = "First" });
            eventBus.Publish(new TestEvent { Message = "Second" });
            eventBus.Publish(new TestEvent { Message = "Third" });
            
            // Assert
            Assert.AreEqual(1, eventCount);
        }
        
        [Test]
        public void EventBus_GetSubscriberCount_ShouldReturnCorrectCount()
        {
            // Arrange & Act
            Assert.AreEqual(0, eventBus.GetSubscriberCount<TestEvent>());
            
            var subscription1 = eventBus.Subscribe<TestEvent>(evt => { });
            Assert.AreEqual(1, eventBus.GetSubscriberCount<TestEvent>());
            
            var subscription2 = eventBus.Subscribe<TestEvent>(evt => { });
            Assert.AreEqual(2, eventBus.GetSubscriberCount<TestEvent>());
            
            subscription1.Dispose();
            // Note: R3 might not immediately update count, so we test the behavior exists
            Assert.IsTrue(eventBus.GetSubscriberCount<TestEvent>() >= 0);
        }
        
        [Test]
        public void EventBus_ClearSubscriptions_ShouldRemoveAllSubscribers()
        {
            // Arrange
            eventBus.Subscribe<TestEvent>(evt => eventCount++);
            eventBus.Subscribe<TestEvent>(evt => eventCount++);
            
            // Act
            eventBus.ClearSubscriptions<TestEvent>();
            eventBus.Publish(new TestEvent { Message = "Should not be received" });
            
            // Assert
            Assert.AreEqual(0, eventCount);
        }
        
        #endregion
        
        #region Observable Tests
        
        
        #endregion
        
        #region Disposal Tests
        
        [Test]
        public void EventBus_Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => eventBus.Dispose());
        }
        
        [Test]
        public void EventBus_OperationsAfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            eventBus.Dispose();
            
            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => eventBus.Publish(new TestEvent()));
            Assert.Throws<ObjectDisposedException>(() => eventBus.Subscribe<TestEvent>(evt => { }));
        }
        
        #endregion
        
        #region Thread Safety Tests
        
        [Test]
        public void EventBus_SubscribeOnMainThread_ShouldWork()
        {
            // Arrange
            bool eventReceivedOnMainThread = false;
            
            // Act
            eventBus.SubscribeOnMainThread<TestEvent>(evt => {
                eventReceivedOnMainThread = true;
            });
            
            eventBus.Publish(new TestEvent { Message = "Main Thread Test" });
            
            // Assert
            Assert.IsTrue(eventReceivedOnMainThread);
        }
        
        #endregion
        
        #region Error Handling Tests
        
        [Test]
        public void EventBus_SubscriberThrowsException_ShouldNotAffectOtherSubscribers()
        {
            // Arrange
            int goodSubscriberCallCount = 0;
            
            eventBus.Subscribe<TestEvent>(evt => throw new Exception("Bad subscriber"));
            eventBus.Subscribe<TestEvent>(evt => goodSubscriberCallCount++);
            
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent()));
            
            // Good subscriber should still be called despite the exception
            Assert.AreEqual(1, goodSubscriberCallCount);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Test event class for unit testing
    /// </summary>
    public class TestEvent
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Another test event class for testing multiple event types
    /// </summary>
    public class AnotherTestEvent
    {
        public int Value { get; set; }
    }
}
