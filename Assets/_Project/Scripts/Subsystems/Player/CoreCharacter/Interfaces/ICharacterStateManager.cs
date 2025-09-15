using System;
using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Character state enumeration covering all possible character states
    /// </summary>
    public enum CharacterState
    {
        None,
        Idle,
        Moving,
        Aiming,
        Climbing, 
        Customizing,
        Dead,
        Stunned,
        Interacting
    }

    /// <summary>
    /// Interface for character state management system.
    /// Handles state transitions, validation, and event publishing.
    /// </summary>
    public interface ICharacterStateManager : ICharacterController
    {
        /// <summary>
        /// Current character state
        /// </summary>
        CharacterState CurrentState { get; }

        /// <summary>
        /// Previous character state
        /// </summary>
        CharacterState PreviousState { get; }

        /// <summary>
        /// How long the character has been in the current state
        /// </summary>
        float TimeInCurrentState { get; }

        /// <summary>
        /// Event fired when character state changes
        /// </summary>
        event Action<CharacterState, CharacterState> OnStateChanged;

        /// <summary>
        /// Event fired before state transition (can be cancelled)
        /// </summary>
        event Func<CharacterState, CharacterState, bool> OnStateChanging;

        /// <summary>
        /// Checks if transition to target state is valid
        /// </summary>
        /// <param name="targetState">State to transition to</param>
        /// <returns>True if transition is valid</returns>
        bool CanTransitionTo(CharacterState targetState);

        /// <summary>
        /// Requests a state transition
        /// </summary>
        /// <param name="targetState">State to transition to</param>
        /// <param name="force">Whether to force the transition ignoring validation</param>
        /// <returns>True if transition was successful</returns>
        bool TryTransitionTo(CharacterState targetState, bool force = false);

        /// <summary>
        /// Forces an immediate state change without validation
        /// </summary>
        /// <param name="targetState">State to change to</param>
        void ForceState(CharacterState targetState);

        /// <summary>
        /// Registers a validator for state transitions
        /// </summary>
        /// <param name="from">Source state (None for any)</param>
        /// <param name="to">Target state (None for any)</param>
        /// <param name="validator">Validation function</param>
        void RegisterStateValidator(CharacterState from, CharacterState to, Func<bool> validator);

        /// <summary>
        /// Removes a registered state validator
        /// </summary>
        /// <param name="from">Source state</param>
        /// <param name="to">Target state</param>
        void UnregisterStateValidator(CharacterState from, CharacterState to);

        /// <summary>
        /// Checks if the character is in any of the specified states
        /// </summary>
        /// <param name="states">States to check</param>
        /// <returns>True if in any of the specified states</returns>
        bool IsInState(params CharacterState[] states);

        /// <summary>
        /// Gets a readable description of the current state
        /// </summary>
        /// <returns>Human-readable state description</returns>
        string GetStateDescription();
    }
}
