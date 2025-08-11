using System;
using System.Collections.Generic;

namespace Infrastructure
{
    /// <summary>
    /// Generic game state machine supporting enter/exit actions and guarded transitions.
    /// </summary>
    public class GameStateMachine<TState> where TState : Enum
    {
        private TState _currentState;

        private readonly Dictionary<TState, Action> _onEnter = new();
        private readonly Dictionary<TState, Action> _onExit = new();

        private readonly Dictionary<TState, List<Transition>> _transitions = new();

        public TState CurrentState => _currentState;

        public GameStateMachine(TState initialState)
        {
            _currentState = initialState;
        }

        public void AddTransition(TState from, TState to, Func<bool>? condition = null)
        {
            if (!_transitions.TryGetValue(from, out var list))
            {
                list = new List<Transition>();
                _transitions[from] = list;
            }
            list.Add(new Transition(to, condition ?? (() => true)));
        }

        public void SetOnEnter(TState state, Action onEnter)
        {
            _onEnter[state] = onEnter;
        }

        public void SetOnExit(TState state, Action onExit)
        {
            _onExit[state] = onExit;
        }

        /// <summary>
        /// Attempts to transition to a new state if conditions pass.
        /// </summary>
        public bool TryTransition(TState newState)
        {
            if (_currentState.Equals(newState))
                return false;

            if (!_transitions.TryGetValue(_currentState, out var possibleTransitions))
                return false;

            foreach (var transition in possibleTransitions)
            {
                if (transition.To.Equals(newState) && transition.Condition())
                {
                    ChangeState(newState);
                    return true;
                }
            }

            return false;
        }

        private void ChangeState(TState newState)
        {
            if (_onExit.TryGetValue(_currentState, out var exitAction))
                exitAction();

            _currentState = newState;

            if (_onEnter.TryGetValue(newState, out var enterAction))
                enterAction();
        }

        private class Transition
        {
            public TState To { get; }
            public Func<bool> Condition { get; }

            public Transition(TState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }
    }
}
