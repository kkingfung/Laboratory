using NUnit.Framework;
using Laboratory.Core.State;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.State.States;
using Cysharp.Threading.Tasks;
using UniRx;
using System;
using System.Collections.Generic;

#nullable enable

namespace Laboratory.Core.Tests.Unit
{
    /// <summary>
    /// Unit tests for the GameStateService state management system.
    /// </summary>
    [TestFixture]
    public class GameStateServiceTests
    {
        private UnifiedEventBus _eventBus = null!;
        private GameStateService _stateService = null!;

        [SetUp]
        public void Setup()
        {
            _eventBus = new UnifiedEventBus();
            _stateService = new GameStateService(_eventBus);
        }

        [TearDown]
        public void TearDown()
        {
            _stateService?.Dispose();
            _eventBus?.Dispose();
        }

        #region Initialization Tests

        [Test]
        public void Constructor_WithValidEventBus_ShouldInitialize()
        {
            // Assert
            Assert.That(_stateService.Current, Is.EqualTo(GameState.None));
        }

        [Test]
        public void Constructor_WithNullEventBus_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GameStateService(null!));
        }

        #endregion

        #region State Registration Tests

        [Test]
        public void RegisterState_ValidState_ShouldRegisterSuccessfully()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _stateService.RegisterState<MockMainMenuState>());
        }

        [Test]
        public void RegisterState_WithFactory_ShouldRegisterSuccessfully()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _stateService.RegisterState(() => new MockMainMenuState()));
        }

        [Test]
        public void RegisterState_WithNullFactory_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _stateService.RegisterState<MockMainMenuState>(null!));
        }

        #endregion

        #region State Transition Tests

        [Test]
        public async UniTask RequestTransitionAsync_ValidTransition_ShouldTransitionSuccessfully()
        {
            // Arrange
            _stateService.RegisterState<MockMainMenuState>();

            // Act
            var result = await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_stateService.Current, Is.EqualTo(GameState.MainMenu));
        }

        [Test]
        public async UniTask RequestTransitionAsync_ToSameState_ShouldReturnTrue()
        {
            // Arrange
            _stateService.RegisterState<MockMainMenuState>();
            await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Act
            var result = await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_stateService.Current, Is.EqualTo(GameState.MainMenu));
        }

        [Test]
        public async UniTask RequestTransitionAsync_ShouldPublishEvents()
        {
            // Arrange
            _stateService.RegisterState<MockMainMenuState>();
            
            var requestedEvents = new List<GameStateChangeRequestedEvent>();
            var changedEvents = new List<GameStateChangedEvent>();
            
            _eventBus.Subscribe<GameStateChangeRequestedEvent>(evt => requestedEvents.Add(evt));
            _eventBus.Subscribe<GameStateChangedEvent>(evt => changedEvents.Add(evt));

            // Act
            await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Assert
            Assert.That(requestedEvents.Count, Is.EqualTo(1));
            Assert.That(changedEvents.Count, Is.EqualTo(1));
            Assert.That(requestedEvents[0].ToState, Is.EqualTo(GameState.MainMenu));
            Assert.That(changedEvents[0].CurrentState, Is.EqualTo(GameState.MainMenu));
        }

        [Test]
        public void RequestTransitionAsync_WithoutRegisteredState_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw, but may not transition properly
            Assert.DoesNotThrow(async () => 
                await _stateService.RequestTransitionAsync(GameState.MainMenu));
        }

        #endregion

        #region State Change Observable Tests

        [Test]
        public async UniTask StateChanges_ShouldEmitOnTransition()
        {
            // Arrange
            _stateService.RegisterState<MockMainMenuState>();
            _stateService.RegisterState<MockPlayingState>();
            
            var stateChanges = new List<GameStateChangedEvent>();
            _stateService.StateChanges.Subscribe(evt => stateChanges.Add(evt));

            // Act
            await _stateService.RequestTransitionAsync(GameState.MainMenu);
            await _stateService.RequestTransitionAsync(GameState.Playing);

            // Assert
            Assert.That(stateChanges.Count, Is.EqualTo(2));
            Assert.That(stateChanges[0].PreviousState, Is.EqualTo(GameState.None));
            Assert.That(stateChanges[0].CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(stateChanges[1].PreviousState, Is.EqualTo(GameState.MainMenu));
            Assert.That(stateChanges[1].CurrentState, Is.EqualTo(GameState.Playing));
        }

        #endregion

        #region Remote State Change Tests

        [Test]
        public void ApplyRemoteStateChange_ShouldChangeStateWithoutEvents()
        {
            // Arrange
            var stateChanges = new List<GameStateChangedEvent>();
            _stateService.StateChanges.Subscribe(evt => stateChanges.Add(evt));

            // Act
            _stateService.ApplyRemoteStateChange(GameState.Playing, suppressEvents: true);

            // Assert
            Assert.That(_stateService.Current, Is.EqualTo(GameState.Playing));
            Assert.That(stateChanges.Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyRemoteStateChange_WithEvents_ShouldPublishEvents()
        {
            // Arrange
            var stateChanges = new List<GameStateChangedEvent>();
            _stateService.StateChanges.Subscribe(evt => stateChanges.Add(evt));

            // Act
            _stateService.ApplyRemoteStateChange(GameState.Playing, suppressEvents: false);

            // Assert
            Assert.That(_stateService.Current, Is.EqualTo(GameState.Playing));
            Assert.That(stateChanges.Count, Is.EqualTo(1));
            Assert.That(stateChanges[0].CurrentState, Is.EqualTo(GameState.Playing));
        }

        #endregion

        #region Transition Validation Tests

        [Test]
        public void CanTransitionTo_FromNone_ShouldAllowAnyTransition()
        {
            // Act & Assert
            Assert.That(_stateService.CanTransitionTo(GameState.MainMenu), Is.True);
            Assert.That(_stateService.CanTransitionTo(GameState.Playing), Is.True);
            Assert.That(_stateService.CanTransitionTo(GameState.Loading), Is.True);
        }

        [Test]
        public void CanTransitionTo_ToNone_ShouldNotAllow()
        {
            // Act & Assert
            Assert.That(_stateService.CanTransitionTo(GameState.None), Is.False);
        }

        [Test]
        public async UniTask CanTransitionTo_WithStateImplementation_ShouldRespectStateRules()
        {
            // Arrange
            _stateService.RegisterState<MockRestrictiveState>();
            await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Act & Assert
            Assert.That(_stateService.CanTransitionTo(GameState.Playing), Is.False);
            Assert.That(_stateService.CanTransitionTo(GameState.Loading), Is.True);
        }

        #endregion

        #region Current State Implementation Tests

        [Test]
        public void GetCurrentStateImplementation_InitialState_ShouldReturnNull()
        {
            // Act
            var implementation = _stateService.GetCurrentStateImplementation();

            // Assert
            Assert.That(implementation, Is.Null);
        }

        [Test]
        public async UniTask GetCurrentStateImplementation_AfterTransition_ShouldReturnImplementation()
        {
            // Arrange
            _stateService.RegisterState<MockMainMenuState>();
            await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Act
            var implementation = _stateService.GetCurrentStateImplementation();

            // Assert
            Assert.That(implementation, Is.Not.Null);
            Assert.That(implementation, Is.InstanceOf<MockMainMenuState>());
        }

        #endregion

        #region Update Tests

        [Test]
        public async UniTask Update_WithCurrentState_ShouldCallStateUpdate()
        {
            // Arrange
            _stateService.RegisterState<MockMainMenuState>();
            await _stateService.RequestTransitionAsync(GameState.MainMenu);
            var state = _stateService.GetCurrentStateImplementation() as MockMainMenuState;

            // Act
            _stateService.Update();

            // Assert
            Assert.That(state?.UpdateCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Update_WithoutCurrentState_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _stateService.Update());
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Dispose_ShouldDisposeCorrectly()
        {
            // Act
            _stateService.Dispose();

            // Assert - accessing after dispose should throw
            Assert.Throws<ObjectDisposedException>(() => 
                _stateService.GetCurrentStateImplementation());
        }

        [Test]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _stateService.Dispose();
                _stateService.Dispose(); // Should not throw on second call
            });
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async UniTask RequestTransitionAsync_StateThrowsOnEnter_ShouldHandleGracefully()
        {
            // Arrange
            _stateService.RegisterState<MockFailingState>();

            // Act
            var result = await _stateService.RequestTransitionAsync(GameState.MainMenu);

            // Assert - Should handle error gracefully and return false
            Assert.That(result, Is.False);
            Assert.That(_stateService.Current, Is.EqualTo(GameState.None)); // Should revert
        }

        #endregion
    }

    #region Mock State Classes

    public class MockMainMenuState : IGameState
    {
        public GameState StateType => GameState.MainMenu;
        public int UpdateCallCount { get; private set; }
        public int EnterCallCount { get; private set; }
        public int ExitCallCount { get; private set; }

        public async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            EnterCallCount++;
            await UniTask.CompletedTask;
        }

        public async UniTask OnExitAsync(GameState toState)
        {
            ExitCallCount++;
            await UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
            UpdateCallCount++;
        }

        public bool CanTransitionTo(GameState targetState)
        {
            return targetState != GameState.None;
        }
    }

    public class MockPlayingState : IGameState
    {
        public GameState StateType => GameState.Playing;

        public async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await UniTask.CompletedTask;
        }

        public async UniTask OnExitAsync(GameState toState)
        {
            await UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
        }

        public bool CanTransitionTo(GameState targetState)
        {
            return targetState != GameState.None;
        }
    }

    public class MockRestrictiveState : IGameState
    {
        public GameState StateType => GameState.MainMenu;

        public async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await UniTask.CompletedTask;
        }

        public async UniTask OnExitAsync(GameState toState)
        {
            await UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
        }

        public bool CanTransitionTo(GameState targetState)
        {
            // Only allow transition to Loading state
            return targetState == GameState.Loading;
        }
    }

    public class MockFailingState : IGameState
    {
        public GameState StateType => GameState.MainMenu;

        public async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await UniTask.CompletedTask;
            throw new InvalidOperationException("Mock failure on enter");
        }

        public async UniTask OnExitAsync(GameState toState)
        {
            await UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
        }

        public bool CanTransitionTo(GameState targetState)
        {
            return true;
        }
    }

    #endregion
}
