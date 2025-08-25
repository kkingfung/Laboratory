using System;
using Laboratory.Core.State;

namespace Laboratory.Core.Events.Messages
{
    #region Game State Events
    
    /// <summary>
    /// Event fired when a game state change is requested.
    /// This allows systems to prepare for or validate state transitions.
    /// </summary>
    public class GameStateChangeRequestedEvent
    {
        /// <summary>The current state before transition.</summary>
        public GameState FromState { get; }
        
        /// <summary>The target state to transition to.</summary>
        public GameState ToState { get; }
        
        /// <summary>Optional context data for the transition.</summary>
        public object? Context { get; }
        
        public GameStateChangeRequestedEvent(
            GameState fromState, 
            GameState toState, 
            object? context = null)
        {
            FromState = fromState;
            ToState = toState;
            Context = context;
        }
    }
    
    /// <summary>
    /// Event fired when a game state change has completed successfully.
    /// </summary>
    public class GameStateChangedEvent
    {
        /// <summary>The previous game state.</summary>
        public GameState PreviousState { get; }
        
        /// <summary>The new current game state.</summary>
        public GameState CurrentState { get; }
        
        /// <summary>Timestamp when the state change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public GameStateChangedEvent(GameState previousState, GameState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    #endregion
}
