using NUnit.Framework;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using System;
using System.Collections.Generic;
using UniRx;

#nullable enable

namespace Laboratory.Core.Tests.Unit
{
    /// <summary>
    /// Unit tests for the UnifiedEventBus event system.
    /// </summary>
    [TestFixture]
    public class UnifiedEventBusTests
    {
        private UnifiedEventBus _eventBus = null!;

        [SetUp]
        public void Setup()
        {
            _eventBus = new UnifiedEventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus?.Dispose();
        }

        #region Publishing Tests

        [Test]
        public void Publish_ValidEvent_ShouldNotThrow()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test" };

            // Act & Assert
            Assert.DoesNotThrow(() => _eventBus.Publish(testEvent));
        }

        [Test]
        public void Publish_NullEvent_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _eventBus.Publish<TestEvent>(null!));
        }

        [Test]
        public void Publish_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test" };
            _eventBus.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _eventBus.Publish(testEvent));
        }

        #endregion

        #region Subscription Tests

        [Test]
        public void Subscribe_ValidHandler_ShouldReceiveEvent()
        {
            // Arrange
            TestEvent? receivedEvent = null;
            var subscription = _eventBus.Subscribe<TestEvent>(evt => receivedEvent = evt);

            // Act
            var testEvent = new TestEvent { Message = "Test Message" };
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(receivedEvent.Message, Is.EqualTo("Test Message"));
            
            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Subscribe_MultipleHandlers_ShouldReceiveAllEvents()
        {
            // Arrange
            var receivedEvents = new List<TestEvent>();
            var subscription1 = _eventBus.Subscribe<TestEvent>(evt => receivedEvents.Add(evt));
            var subscription2 = _eventBus.Subscribe<TestEvent>(evt => receivedEvents.Add(evt));

            // Act
            var testEvent = new TestEvent { Message = "Test Message" };
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents[0].Message, Is.EqualTo("Test Message"));
            Assert.That(receivedEvents[1].Message, Is.EqualTo("Test Message"));
            
            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _eventBus.Subscribe<TestEvent>(null!));
        }

        [Test]
        public void Subscribe_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _eventBus.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                _eventBus.Subscribe<TestEvent>(evt => { }));
        }

        #endregion

        #region Advanced Subscription Tests

        [Test]
        public void SubscribeWhere_WithPredicate_ShouldFilterEvents()
        {
            // Arrange
            var receivedEvents = new List<TestEvent>();
            var subscription = _eventBus.SubscribeWhere<TestEvent>(
                evt => evt.Message.StartsWith("Important"), 
                evt => receivedEvents.Add(evt));

            // Act
            _eventBus.Publish(new TestEvent { Message = "Important message" });
            _eventBus.Publish(new TestEvent { Message = "Normal message" });
            _eventBus.Publish(new TestEvent { Message = "Important update" });

            // Assert
            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents[0].Message, Is.EqualTo("Important message"));
            Assert.That(receivedEvents[1].Message, Is.EqualTo("Important update"));
            
            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void SubscribeFirst_ShouldReceiveOnlyFirstEvent()
        {
            // Arrange
            var receivedEvents = new List<TestEvent>();
            var subscription = _eventBus.SubscribeFirst<TestEvent>(evt => receivedEvents.Add(evt));

            // Act
            _eventBus.Publish(new TestEvent { Message = "First" });
            _eventBus.Publish(new TestEvent { Message = "Second" });
            _eventBus.Publish(new TestEvent { Message = "Third" });

            // Assert
            Assert.That(receivedEvents.Count, Is.EqualTo(1));
            Assert.That(receivedEvents[0].Message, Is.EqualTo("First"));
            
            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void SubscribeOnMainThread_ShouldReceiveEvent()
        {
            // Arrange
            TestEvent? receivedEvent = null;
            var subscription = _eventBus.SubscribeOnMainThread<TestEvent>(evt => receivedEvent = evt);

            // Act
            var testEvent = new TestEvent { Message = "Main Thread Test" };
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(receivedEvent.Message, Is.EqualTo("Main Thread Test"));
            
            // Cleanup
            subscription.Dispose();
        }

        #endregion

        #region Observable Tests

        [Test]
        public void Observe_ShouldReturnObservable()
        {
            // Arrange
            var observable = _eventBus.Observe<TestEvent>();

            // Assert
            Assert.That(observable, Is.Not.Null);
            Assert.That(observable, Is.InstanceOf<IObservable<TestEvent>>());
        }

        [Test]
        public void Observe_WithSubscription_ShouldReceiveEvents()
        {
            // Arrange
            TestEvent? receivedEvent = null;
            var observable = _eventBus.Observe<TestEvent>();
            var subscription = observable.Subscribe(evt => receivedEvent = evt);

            // Act
            var testEvent = new TestEvent { Message = "Observable Test" };
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(receivedEvent.Message, Is.EqualTo("Observable Test"));
            
            // Cleanup
            subscription.Dispose();
        }

        #endregion

        #region Subscriber Count Tests

        [Test]
        public void GetSubscriberCount_NoSubscribers_ShouldReturnZero()
        {
            // Act
            var count = _eventBus.GetSubscriberCount<TestEvent>();

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetSubscriberCount_WithSubscribers_ShouldReturnOne()
        {
            // Arrange
            var subscription = _eventBus.Subscribe<TestEvent>(evt => { });

            // Act
            var count = _eventBus.GetSubscriberCount<TestEvent>();

            // Assert
            Assert.That(count, Is.EqualTo(1));
            
            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void HasSubscribers_NoSubscribers_ShouldReturnFalse()
        {
            // Act
            var hasSubscribers = _eventBus.HasSubscribers<TestEvent>();

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void HasSubscribers_WithSubscribers_ShouldReturnTrue()
        {
            // Arrange
            var subscription = _eventBus.Subscribe<TestEvent>(evt => { });

            // Act
            var hasSubscribers = _eventBus.HasSubscribers<TestEvent>();

            // Assert
            Assert.That(hasSubscribers, Is.True);
            
            // Cleanup
            subscription.Dispose();
        }

        #endregion

        #region Clear Subscriptions Tests

        [Test]
        public void ClearSubscriptions_ShouldRemoveAllSubscriptions()
        {
            // Arrange
            var subscription = _eventBus.Subscribe<TestEvent>(evt => { });
            
            // Verify subscription exists
            Assert.That(_eventBus.HasSubscribers<TestEvent>(), Is.True);

            // Act
            _eventBus.ClearSubscriptions<TestEvent>();

            // Assert
            Assert.That(_eventBus.HasSubscribers<TestEvent>(), Is.False);
            
            // Cleanup (subscription should already be disposed)
            subscription.Dispose();
        }

        #endregion

        #region Event Type Count Tests

        [Test]
        public void GetEventTypeCount_NoEvents_ShouldReturnZero()
        {
            // Act
            var count = _eventBus.GetEventTypeCount();

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetEventTypeCount_WithSubscriptions_ShouldReturnCorrectCount()
        {
            // Arrange
            var subscription1 = _eventBus.Subscribe<TestEvent>(evt => { });
            var subscription2 = _eventBus.Subscribe<AnotherTestEvent>(evt => { });

            // Act
            var count = _eventBus.GetEventTypeCount();

            // Assert
            Assert.That(count, Is.EqualTo(2));
            
            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Dispose_ShouldDisposeAllSubscriptions()
        {
            // Arrange
            var subscription = _eventBus.Subscribe<TestEvent>(evt => { });
            
            // Act
            _eventBus.Dispose();

            // Assert - accessing after dispose should throw
            Assert.Throws<ObjectDisposedException>(() => 
                _eventBus.HasSubscribers<TestEvent>());
        }

        [Test]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _eventBus.Dispose();
                _eventBus.Dispose(); // Should not throw on second call
            });
        }

        #endregion

        #region Integration with Standard Events Tests

        [Test]
        public void Publish_StandardLoadingEvent_ShouldWork()
        {
            // Arrange
            LoadingStartedEvent? receivedEvent = null;
            var subscription = _eventBus.Subscribe<LoadingStartedEvent>(evt => receivedEvent = evt);

            // Act
            var loadingEvent = new LoadingStartedEvent("TestOperation", "Testing loading");
            _eventBus.Publish(loadingEvent);

            // Assert
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(receivedEvent.OperationName, Is.EqualTo("TestOperation"));
            Assert.That(receivedEvent.Description, Is.EqualTo("Testing loading"));
            
            // Cleanup
            subscription.Dispose();
        }

        #endregion
    }

    #region Test Event Classes

    public class TestEvent
    {
        public string Message { get; set; } = "";
    }

    public class AnotherTestEvent
    {
        public int Value { get; set; }
    }

    #endregion
}
