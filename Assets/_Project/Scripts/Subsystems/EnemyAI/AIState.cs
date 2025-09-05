using System;
using UnityEngine;

namespace Laboratory.EnemyAI
{
    /// <summary>
    /// Base class for all AI states in the Laboratory Unity Project
    /// Version 1.0 - Comprehensive state functionality with lifecycle management
    /// </summary>
    public abstract class AIState
    {
        #region Fields

        protected AIStateMachine stateMachine;
        protected float stateStartTime;
        protected float stateElapsedTime;
        protected bool hasEntered;
        protected bool hasExited;

        private StateDebugInfo _debugInfo;

        #endregion

        #region Properties

        /// <summary>
        /// The state machine that owns this state
        /// </summary>
        public AIStateMachine StateMachine => stateMachine;

        /// <summary>
        /// Time when this state was entered
        /// </summary>
        public float StateStartTime => stateStartTime;

        /// <summary>
        /// Time elapsed since entering this state
        /// </summary>
        public float StateElapsedTime => hasEntered ? Time.time - stateStartTime : 0f;

        /// <summary>
        /// Whether this state has been properly entered
        /// </summary>
        public bool HasEntered => hasEntered;

        /// <summary>
        /// Whether this state has been exited
        /// </summary>
        public bool HasExited => hasExited;

        /// <summary>
        /// State name for debugging
        /// </summary>
        public virtual string StateName => GetType().Name;

        /// <summary>
        /// State priority (higher values take precedence)
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Whether this state can be interrupted by higher priority states
        /// </summary>
        public virtual bool CanBeInterrupted => true;

        /// <summary>
        /// Minimum time this state should remain active
        /// </summary>
        public virtual float MinDuration => 0f;

        /// <summary>
        /// Maximum time this state can remain active (0 = infinite)
        /// </summary>
        public virtual float MaxDuration => 0f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the state with a reference to its state machine
        /// </summary>
        public virtual void Initialize(AIStateMachine owner)
        {
            stateMachine = owner;
            _debugInfo = new StateDebugInfo { StateName = StateName };
            OnInitialize();
        }

        /// <summary>
        /// Called when entering this state
        /// </summary>
        public void Enter()
        {
            if (hasEntered)
            {
                Debug.LogWarning($"[AIState] State {StateName} entered multiple times without exit!");
                return;
            }

            stateStartTime = Time.time;
            hasEntered = true;
            hasExited = false;

            _debugInfo.EnterTime = stateStartTime;
            _debugInfo.EnterCount++;

            try
            {
                OnEnter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIState] Error entering state {StateName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Called every frame while this state is active
        /// </summary>
        public void Update()
        {
            if (!hasEntered || hasExited)
                return;

            stateElapsedTime = Time.time - stateStartTime;

            try
            {
                // Check for automatic timeout
                if (MaxDuration > 0f && stateElapsedTime >= MaxDuration)
                {
                    OnTimeout();
                    return;
                }

                OnUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIState] Error updating state {StateName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        public void Exit()
        {
            if (!hasEntered || hasExited)
                return;

            hasExited = true;

            _debugInfo.ExitTime = Time.time;
            _debugInfo.TotalDuration += stateElapsedTime;

            try
            {
                OnExit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIState] Error exiting state {StateName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the state machine is being destroyed
        /// </summary>
        public virtual void Cleanup()
        {
            OnCleanup();
        }

        /// <summary>
        /// Checks if this state can be entered from the given state
        /// </summary>
        public virtual bool CanEnterFrom(AIState fromState)
        {
            return true; // Override for specific transition rules
        }

        /// <summary>
        /// Checks if this state can be exited to the given state
        /// </summary>
        public virtual bool CanExitTo(AIState toState)
        {
            // Check minimum duration
            if (MinDuration > 0f && StateElapsedTime < MinDuration)
                return false;

            // Check if can be interrupted
            if (!CanBeInterrupted && toState != null && toState.Priority > Priority)
                return false;

            return true;
        }

        /// <summary>
        /// Forces the state machine to transition to another state
        /// </summary>
        protected bool RequestStateChange<T>() where T : AIState
        {
            return stateMachine?.ChangeState<T>() ?? false;
        }

        /// <summary>
        /// Forces the state machine to transition to another state
        /// </summary>
        protected bool RequestStateChange(Type stateType)
        {
            return stateMachine?.ChangeState(stateType) ?? false;
        }

        /// <summary>
        /// Gets debug information about this state
        /// </summary>
        public StateDebugInfo GetDebugInfo()
        {
            return _debugInfo;
        }

        #endregion

        #region Abstract/Virtual Methods

        /// <summary>
        /// Called during state initialization
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Called when the state is entered
        /// </summary>
        protected abstract void OnEnter();

        /// <summary>
        /// Called every frame while the state is active
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// Called when the state is exited
        /// </summary>
        protected virtual void OnExit() { }

        /// <summary>
        /// Called when the state times out (exceeds MaxDuration)
        /// </summary>
        protected virtual void OnTimeout()
        {
            // Default behavior is to do nothing - override for timeout handling
        }

        /// <summary>
        /// Called during cleanup
        /// </summary>
        protected virtual void OnCleanup() { }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the GameObject that owns the state machine
        /// </summary>
        protected GameObject GetOwnerGameObject()
        {
            return stateMachine?.gameObject;
        }

        /// <summary>
        /// Gets the Transform of the owner GameObject
        /// </summary>
        protected Transform GetOwnerTransform()
        {
            return stateMachine?.transform;
        }

        /// <summary>
        /// Gets a component from the owner GameObject
        /// </summary>
        protected T GetOwnerComponent<T>() where T : Component
        {
            return stateMachine?.GetComponent<T>();
        }

        /// <summary>
        /// Logs a message with state context
        /// </summary>
        protected void LogState(string message, LogType logType = LogType.Log)
        {
            string fullMessage = $"[{StateName}] {message}";
            
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(fullMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(fullMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(fullMessage);
                    break;
            }
        }

        /// <summary>
        /// Checks if enough time has passed since entering the state
        /// </summary>
        protected bool HasBeenActiveFor(float duration)
        {
            return StateElapsedTime >= duration;
        }

        /// <summary>
        /// Checks if the state should timeout soon
        /// </summary>
        protected bool WillTimeoutSoon(float threshold = 1f)
        {
            return MaxDuration > 0f && (MaxDuration - StateElapsedTime) <= threshold;
        }

        #endregion

        #region Operator Overrides

        public override string ToString()
        {
            return $"{StateName} (Active: {StateElapsedTime:F1}s)";
        }

        public override bool Equals(object obj)
        {
            return obj is AIState other && other.GetType() == GetType();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        #endregion
    }

    #region Specialized State Base Classes

    /// <summary>
    /// Base class for states that need to perform actions over time
    /// </summary>
    public abstract class TimedState : AIState
    {
        protected float actionDuration;
        protected float actionStartTime;
        protected bool actionCompleted;

        protected TimedState(float duration)
        {
            actionDuration = duration;
        }

        protected override void OnEnter()
        {
            actionStartTime = Time.time;
            actionCompleted = false;
            OnActionStart();
        }

        protected override void OnUpdate()
        {
            float actionProgress = (Time.time - actionStartTime) / actionDuration;
            
            if (actionProgress >= 1f && !actionCompleted)
            {
                actionCompleted = true;
                OnActionComplete();
            }
            else
            {
                OnActionUpdate(actionProgress);
            }
        }

        protected abstract void OnActionStart();
        protected abstract void OnActionUpdate(float progress);
        protected abstract void OnActionComplete();
    }

    /// <summary>
    /// Base class for states that wait for external conditions
    /// </summary>
    public abstract class ConditionalState : AIState
    {
        protected bool conditionMet;

        protected override void OnEnter()
        {
            conditionMet = false;
            OnConditionCheckStart();
        }

        protected override void OnUpdate()
        {
            if (!conditionMet)
            {
                conditionMet = CheckCondition();
                if (conditionMet)
                {
                    OnConditionMet();
                }
            }

            OnConditionalUpdate();
        }

        protected abstract void OnConditionCheckStart();
        protected abstract bool CheckCondition();
        protected abstract void OnConditionMet();
        protected abstract void OnConditionalUpdate();
    }

    /// <summary>
    /// Base class for movement-based states
    /// </summary>
    public abstract class MovementState : AIState
    {
        protected Vector3 targetPosition;
        protected float movementSpeed;
        protected float stoppingDistance = 0.1f;

        protected MovementState(Vector3 target, float speed)
        {
            targetPosition = target;
            movementSpeed = speed;
        }

        protected bool HasReachedTarget()
        {
            var ownerTransform = GetOwnerTransform();
            return ownerTransform != null && 
                   Vector3.Distance(ownerTransform.position, targetPosition) <= stoppingDistance;
        }

        protected void MoveTowardsTarget()
        {
            var ownerTransform = GetOwnerTransform();
            if (ownerTransform == null) return;

            Vector3 direction = (targetPosition - ownerTransform.position).normalized;
            Vector3 movement = direction * movementSpeed * Time.deltaTime;
            ownerTransform.position += movement;
        }
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Debug information for state monitoring
    /// </summary>
    [System.Serializable]
    public class StateDebugInfo
    {
        public string StateName;
        public float EnterTime;
        public float ExitTime;
        public float TotalDuration;
        public int EnterCount;
        public bool IsCurrentState;
    }

    #endregion
}