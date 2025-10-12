using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.Infrastructure;

namespace Laboratory.EnemyAI
{
    /// <summary>
    /// Advanced AI State Machine for Laboratory Unity Project
    /// Version 1.0 - Complete state management with transitions, conditions, and debugging
    /// </summary>
    public abstract class AIStateMachine : MonoBehaviour
    {
        #region Fields

        [Header("State Machine Configuration")]
        [SerializeField] protected bool enableDebugLogs = true;
        [SerializeField] protected bool enableStateVisualization = true;
        [SerializeField] protected Color debugStateColor = Color.yellow;

        [Header("Performance Settings")]
        [SerializeField] protected float stateUpdateInterval = 0.1f;
        [SerializeField] protected bool useFixedUpdate = false;

        protected AIState currentState;
        protected AIState previousState;
        protected Dictionary<Type, AIState> stateMap = new();
        protected Queue<Type> stateHistory = new();
        protected float lastStateChangeTime;
        protected float nextUpdateTime;

        private IEventBus _eventBus;
        private readonly Dictionary<Type, List<StateTransition>> _transitions = new();
        private readonly Dictionary<Type, StateConfig> _stateConfigs = new();

        // Performance monitoring
        private AIPerformanceMetrics _performanceMetrics = new();

        #endregion

        #region Properties

        public AIState CurrentState => currentState;
        public AIState PreviousState => previousState;
        public Type CurrentStateType => currentState?.GetType();
        public float TimeSinceStateChange => Time.time - lastStateChangeTime;
        public bool IsTransitioning { get; private set; }
        
        public AIPerformanceMetrics PerformanceMetrics => _performanceMetrics;

        #endregion

        #region Events

        public event Action<AIState, AIState> OnStateChanged;
        public event Action<StateTransition> OnTransitionTriggered;
        public event Action<AIState> OnStateEntered;
        public event Action<AIState> OnStateExited;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeStateMachine();
        }

        protected virtual void Start()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
            }
            StartStateMachine();
        }

        protected virtual void Update()
        {
            if (!useFixedUpdate)
            {
                UpdateStateMachine();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (useFixedUpdate)
            {
                UpdateStateMachine();
            }
        }

        protected virtual void OnDestroy()
        {
            CleanupStateMachine();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Changes to a specific state type
        /// </summary>
        public virtual bool ChangeState<T>() where T : AIState
        {
            return ChangeState(typeof(T));
        }

        /// <summary>
        /// Changes to a specific state by type
        /// </summary>
        public virtual bool ChangeState(Type stateType)
        {
            if (!stateMap.TryGetValue(stateType, out var newState))
            {
                Debug.LogError($"[AIStateMachine] State not found: {stateType.Name}");
                return false;
            }

            return ChangeState(newState);
        }

        /// <summary>
        /// Changes to a specific state instance
        /// </summary>
        public virtual bool ChangeState(AIState newState)
        {
            if (newState == null || newState == currentState)
                return false;

            // Check if transition is allowed
            if (!CanTransitionTo(newState.GetType()))
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[AIStateMachine] Transition blocked: {CurrentStateType?.Name} -> {newState.GetType().Name}");
                return false;
            }

            PerformStateTransition(newState);
            return true;
        }

        /// <summary>
        /// Forces a state change without checking transitions
        /// </summary>
        public virtual void ForceChangeState<T>() where T : AIState
        {
            if (stateMap.TryGetValue(typeof(T), out var newState))
            {
                PerformStateTransition(newState);
            }
        }

        /// <summary>
        /// Returns to the previous state
        /// </summary>
        public virtual bool ReturnToPreviousState()
        {
            if (previousState != null)
            {
                return ChangeState(previousState);
            }
            return false;
        }

        /// <summary>
        /// Checks if a transition to a specific state is possible
        /// </summary>
        public virtual bool CanTransitionTo<T>() where T : AIState
        {
            return CanTransitionTo(typeof(T));
        }

        /// <summary>
        /// Checks if a transition to a specific state type is possible
        /// </summary>
        public virtual bool CanTransitionTo(Type stateType)
        {
            if (currentState == null) return true;

            var currentStateType = currentState.GetType();
            
            // Check if there are any transitions defined from current state
            if (!_transitions.TryGetValue(currentStateType, out var transitions))
            {
                // If no transitions defined, allow all transitions (open system)
                return true;
            }

            // Check if there's a valid transition to the target state
            foreach (var transition in transitions)
            {
                if (transition.ToState == stateType && transition.CanTransition())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers a state in the state machine
        /// </summary>
        public virtual void RegisterState<T>(T state) where T : AIState
        {
            if (state != null)
            {
                var stateType = typeof(T);
                stateMap[stateType] = state;
                state.Initialize(this);

                if (enableDebugLogs)
                    Debug.Log($"[AIStateMachine] Registered state: {stateType.Name}");
            }
        }

        /// <summary>
        /// Adds a state transition rule
        /// </summary>
        public virtual void AddTransition<TFrom, TTo>(Func<bool> condition = null) 
            where TFrom : AIState 
            where TTo : AIState
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);

            if (!_transitions.ContainsKey(fromType))
            {
                _transitions[fromType] = new List<StateTransition>();
            }

            var transition = new StateTransition
            {
                FromState = fromType,
                ToState = toType,
                Condition = condition,
                Priority = 0
            };

            _transitions[fromType].Add(transition);

            if (enableDebugLogs)
                Debug.Log($"[AIStateMachine] Added transition: {fromType.Name} -> {toType.Name}");
        }

        /// <summary>
        /// Configures a specific state
        /// </summary>
        public virtual void ConfigureState<T>(StateConfig config) where T : AIState
        {
            _stateConfigs[typeof(T)] = config;
        }

        /// <summary>
        /// Gets the current state as a specific type
        /// </summary>
        public virtual T GetCurrentState<T>() where T : AIState
        {
            return currentState as T;
        }

        /// <summary>
        /// Gets a registered state by type
        /// </summary>
        public virtual T GetState<T>() where T : AIState
        {
            return stateMap.TryGetValue(typeof(T), out var state) ? state as T : null;
        }

        /// <summary>
        /// Gets all registered state types
        /// </summary>
        public virtual List<Type> GetAllStateTypes()
        {
            return new List<Type>(stateMap.Keys);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Abstract method for derived classes to initialize their specific states
        /// </summary>
        protected abstract void InitializeStates();

        /// <summary>
        /// Abstract method to define the initial state
        /// </summary>
        protected abstract Type GetInitialStateType();

        /// <summary>
        /// Virtual method for setting up state transitions
        /// </summary>
        protected virtual void SetupTransitions()
        {
            // Override in derived classes to define specific transitions
        }

        /// <summary>
        /// Called when a state is entered
        /// </summary>
        protected virtual void OnStateEnter(AIState state)
        {
            OnStateEntered?.Invoke(state);
            _eventBus?.Publish(new AIStateChangedEvent(this, previousState, state));
        }

        /// <summary>
        /// Called when a state is updated
        /// </summary>
        protected virtual void OnStateUpdate(AIState state)
        {
            // Override for custom update logic
        }

        /// <summary>
        /// Called when a state is exited
        /// </summary>
        protected virtual void OnStateExit(AIState state)
        {
            OnStateExited?.Invoke(state);
        }

        #endregion

        #region Private Methods

        private void InitializeStateMachine()
        {
            try
            {
                // Initialize states defined by derived classes
                InitializeStates();

                // Setup state transitions
                SetupTransitions();

                // Configure performance monitoring
                _performanceMetrics.Initialize();

                if (enableDebugLogs)
                    Debug.Log($"[AIStateMachine] Initialized with {stateMap.Count} states");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIStateMachine] Failed to initialize: {ex.Message}");
            }
        }

        private void StartStateMachine()
        {
            try
            {
                var initialStateType = GetInitialStateType();
                if (initialStateType != null && stateMap.TryGetValue(initialStateType, out var initialState))
                {
                    PerformStateTransition(initialState);
                }
                else
                {
                    Debug.LogError($"[AIStateMachine] Initial state not found: {initialStateType?.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIStateMachine] Failed to start state machine: {ex.Message}");
            }
        }

        private void UpdateStateMachine()
        {
            if (currentState == null) return;

            // Throttle updates based on interval
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + stateUpdateInterval;

            var startTime = Time.realtimeSinceStartup;

            try
            {
                // Update current state
                currentState.Update();
                OnStateUpdate(currentState);

                // Check for automatic transitions
                CheckAutomaticTransitions();

                // Update performance metrics
                _performanceMetrics.RecordStateUpdate(currentState.GetType(), Time.realtimeSinceStartup - startTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIStateMachine] Error during state update: {ex.Message}");
            }
        }

        private void CheckAutomaticTransitions()
        {
            if (currentState == null) return;

            var currentStateType = currentState.GetType();
            
            if (!_transitions.TryGetValue(currentStateType, out var transitions))
                return;

            // Sort transitions by priority (higher priority first)
            transitions.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var transition in transitions)
            {
                if (transition.CanTransition())
                {
                    if (stateMap.TryGetValue(transition.ToState, out var targetState))
                    {
                        OnTransitionTriggered?.Invoke(transition);
                        PerformStateTransition(targetState);
                        break; // Only execute the first valid transition
                    }
                }
            }
        }

        private void PerformStateTransition(AIState newState)
        {
            IsTransitioning = true;
            var oldState = currentState;

            try
            {
                // Exit current state
                if (currentState != null)
                {
                    currentState.Exit();
                    OnStateExit(currentState);
                }

                // Update state references
                previousState = currentState;
                currentState = newState;
                lastStateChangeTime = Time.time;

                // Update state history
                if (previousState != null)
                {
                    stateHistory.Enqueue(previousState.GetType());
                    if (stateHistory.Count > 10) // Keep last 10 states
                    {
                        stateHistory.Dequeue();
                    }
                }

                // Enter new state
                currentState.Enter();
                OnStateEnter(currentState);

                // Fire events
                OnStateChanged?.Invoke(oldState, currentState);

                // Update performance metrics
                _performanceMetrics.RecordStateChange(oldState?.GetType(), currentState.GetType());

                if (enableDebugLogs)
                {
                    string oldStateName = oldState?.GetType().Name ?? "None";
                    string newStateName = currentState.GetType().Name;
                    Debug.Log($"[AIStateMachine] State changed: {oldStateName} -> {newStateName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIStateMachine] Error during state transition: {ex.Message}");
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        private void CleanupStateMachine()
        {
            if (currentState != null)
            {
                currentState.Exit();
            }

            foreach (var state in stateMap.Values)
            {
                state?.Cleanup();
            }

            stateMap.Clear();
            _transitions.Clear();
            _stateConfigs.Clear();
            stateHistory.Clear();
        }

        #endregion

        #region Debug and Visualization

        protected virtual void OnDrawGizmos()
        {
            if (!enableStateVisualization || currentState == null) return;

            Gizmos.color = debugStateColor;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

#if UNITY_EDITOR
            var stateName = currentState.GetType().Name;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, stateName);
#endif
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogStateMachineStatus()
        {
            Debug.Log($"[AIStateMachine] Status Report:\n" +
                     $"Current State: {CurrentStateType?.Name ?? "None"}\n" +
                     $"Previous State: {PreviousState?.GetType().Name ?? "None"}\n" +
                     $"Time in State: {TimeSinceStateChange:F2}s\n" +
                     $"States Registered: {stateMap.Count}\n" +
                     $"Transitions Defined: {_transitions.Values.Sum(t => t.Count)}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents a state transition with conditions
    /// </summary>
    public class StateTransition
    {
        public Type FromState;
        public Type ToState;
        public Func<bool> Condition;
        public int Priority;
        public string Name;
        public float Cooldown;
        public float LastTriggered;

        public bool CanTransition()
        {
            // Check cooldown
            if (Cooldown > 0f && Time.time - LastTriggered < Cooldown)
                return false;

            // Check condition
            return Condition?.Invoke() ?? true;
        }

        public void Trigger()
        {
            LastTriggered = Time.time;
        }
    }

    /// <summary>
    /// Configuration for individual states
    /// </summary>
    [System.Serializable]
    public class StateConfig
    {
        public float MinDuration = 0f;
        public float MaxDuration = float.MaxValue;
        public bool AllowInterruption = true;
        public int Priority = 0;
        public bool LogStateChanges = false;
    }

    /// <summary>
    /// Performance metrics for state machine monitoring
    /// </summary>
    public class AIPerformanceMetrics
    {
        public Dictionary<Type, int> StateChangeCounts = new();
        public Dictionary<Type, float> StateUpdateTimes = new();
        public Dictionary<Type, float> StateDurations = new();
        public float TotalStateChanges;
        public float AverageStateUpdateTime;
        public DateTime SessionStart;

        public void Initialize()
        {
            SessionStart = DateTime.Now;
            StateChangeCounts.Clear();
            StateUpdateTimes.Clear();
            StateDurations.Clear();
            TotalStateChanges = 0f;
            AverageStateUpdateTime = 0f;
        }

        public void RecordStateChange(Type fromState, Type toState)
        {
            TotalStateChanges++;
            
            if (toState != null)
            {
                StateChangeCounts[toState] = StateChangeCounts.GetValueOrDefault(toState, 0) + 1;
            }
        }

        public void RecordStateUpdate(Type stateType, float updateTime)
        {
            if (stateType != null)
            {
                StateUpdateTimes[stateType] = updateTime;
                
                // Update running average
                AverageStateUpdateTime = (AverageStateUpdateTime + updateTime) * 0.5f;
            }
        }
    }

    #endregion

    #region Events

    public class AIStateChangedEvent
    {
        public AIStateMachine StateMachine { get; }
        public AIState PreviousState { get; }
        public AIState NewState { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public AIStateChangedEvent(AIStateMachine stateMachine, AIState previousState, AIState newState)
        {
            StateMachine = stateMachine;
            PreviousState = previousState;
            NewState = newState;
        }
    }

    #endregion
}