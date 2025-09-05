using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.EnemyAI
{
    /// <summary>
    /// Behavior Tree Implementation for Laboratory Unity Project
    /// Version 1.0 - Complete behavior tree system with all node types and debugging
    /// </summary>
    public class BehaviorTree : MonoBehaviour
    {
        #region Fields

        [Header("Behavior Tree Configuration")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool enableVisualDebugging = true;
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private int maxExecutionsPerFrame = 10;

        private BehaviorNode rootNode;
        private BehaviorTreeContext context;
        private float lastUpdateTime;
        private int executionsThisFrame;

        // Performance monitoring
        private BehaviorTreeStatistics _statistics = new();

        #endregion

        #region Properties

        public BehaviorNode RootNode => rootNode;
        public BehaviorTreeContext Context => context;
        public BehaviorTreeStatistics Statistics => _statistics;
        public bool IsRunning { get; private set; }

        #endregion

        #region Events

        public event Action<BehaviorNode, BehaviorTreeStatus> OnNodeExecuted;
        public event Action<BehaviorTree> OnTreeExecutionComplete;
        public event Action<BehaviorTree> OnTreeReset;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeBehaviorTree();
        }

        protected virtual void Start()
        {
            StartBehaviorTree();
        }

        protected virtual void Update()
        {
            UpdateBehaviorTree();
        }

        protected virtual void OnDestroy()
        {
            CleanupBehaviorTree();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the root node of the behavior tree
        /// </summary>
        public void SetRootNode(BehaviorNode node)
        {
            rootNode = node;
            if (node != null)
            {
                node.SetContext(context);
            }
        }

        /// <summary>
        /// Executes the behavior tree once
        /// </summary>
        public BehaviorTreeStatus Execute()
        {
            if (rootNode == null || context == null)
            {
                return BehaviorTreeStatus.Failure;
            }

            executionsThisFrame++;
            var startTime = Time.realtimeSinceStartup;

            try
            {
                var status = rootNode.Execute();
                
                // Update statistics
                var executionTime = Time.realtimeSinceStartup - startTime;
                _statistics.RecordExecution(status, executionTime);

                // Trigger node execution event
                OnNodeExecuted?.Invoke(rootNode, status);
                OnTreeExecutionComplete?.Invoke(this);

                return status;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BehaviorTree] Error during execution: {ex.Message}");
                return BehaviorTreeStatus.Failure;
            }
        }

        /// <summary>
        /// Starts the behavior tree
        /// </summary>
        public void StartTree()
        {
            IsRunning = true;
            
            if (enableDebugLogs)
                Debug.Log("[BehaviorTree] Tree started");
        }

        /// <summary>
        /// Stops the behavior tree
        /// </summary>
        public void StopTree()
        {
            IsRunning = false;
            ResetTree();
            
            if (enableDebugLogs)
                Debug.Log("[BehaviorTree] Tree stopped");
        }

        /// <summary>
        /// Resets the behavior tree to initial state
        /// </summary>
        public void ResetTree()
        {
            rootNode?.Reset();
            executionsThisFrame = 0;
            
            OnTreeReset?.Invoke(this);
            
            if (enableDebugLogs)
                Debug.Log("[BehaviorTree] Tree reset");
        }

        /// <summary>
        /// Sets a blackboard value
        /// </summary>
        public void SetBlackboardValue<T>(string key, T value)
        {
            context?.Blackboard.SetValue(key, value);
        }

        /// <summary>
        /// Gets a blackboard value
        /// </summary>
        public T GetBlackboardValue<T>(string key, T defaultValue = default(T))
        {
            return context != null ? context.Blackboard.GetValue(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// Clears the blackboard
        /// </summary>
        public void ClearBlackboard()
        {
            context?.Blackboard.Clear();
        }

        #endregion

        #region Private Methods

        private void InitializeBehaviorTree()
        {
            context = new BehaviorTreeContext(this);
            _statistics.Initialize();
            
            // Initialize blackboard with common values
            SetupDefaultBlackboardValues();
        }

        private void StartBehaviorTree()
        {
            StartTree();
        }

        private void UpdateBehaviorTree()
        {
            if (!IsRunning || rootNode == null) return;

            // Throttle updates based on interval
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;

            // Reset execution counter each frame
            if (executionsThisFrame >= maxExecutionsPerFrame)
            {
                executionsThisFrame = 0;
                return;
            }

            Execute();
        }

        private void CleanupBehaviorTree()
        {
            StopTree();
            rootNode?.Cleanup();
            context?.Cleanup();
        }

        private void SetupDefaultBlackboardValues()
        {
            if (context?.Blackboard == null) return;

            // Add common AI values
            context.Blackboard.SetValue("SelfTransform", transform);
            context.Blackboard.SetValue("SelfGameObject", gameObject);
            context.Blackboard.SetValue("StartPosition", transform.position);
            context.Blackboard.SetValue("StartRotation", transform.rotation);
            context.Blackboard.SetValue("CurrentTarget", (Transform)null);
            context.Blackboard.SetValue("LastSeenPosition", Vector3.zero);
            context.Blackboard.SetValue("IsAlert", false);
            context.Blackboard.SetValue("MovementSpeed", 5f);
            context.Blackboard.SetValue("DetectionRange", 10f);
            context.Blackboard.SetValue("AttackRange", 2f);
        }

        #endregion

        #region Debug and Visualization

        protected virtual void OnDrawGizmos()
        {
            if (!enableVisualDebugging || rootNode == null) return;

            DrawBehaviorTreeGizmos();
        }

        private void DrawBehaviorTreeGizmos()
        {
            Gizmos.color = IsRunning ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);

#if UNITY_EDITOR
            var status = IsRunning ? "Running" : "Stopped";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3.5f, $"BT: {status}");
#endif
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogTreeStatus()
        {
            Debug.Log($"[BehaviorTree] Status Report:\n" +
                     $"Is Running: {IsRunning}\n" +
                     $"Root Node: {rootNode?.GetType().Name ?? "None"}\n" +
                     $"Executions This Frame: {executionsThisFrame}\n" +
                     $"Total Executions: {_statistics.TotalExecutions}\n" +
                     $"Success Rate: {_statistics.SuccessRate:F2}%");
        }

        #endregion
    }

    #region Behavior Tree Status

    /// <summary>
    /// Status returned by behavior tree nodes
    /// </summary>
    public enum BehaviorTreeStatus
    {
        Success,    // The node completed successfully
        Failure,    // The node failed to complete
        Running     // The node is still executing
    }

    #endregion

    #region Behavior Tree Context

    /// <summary>
    /// Context shared between all nodes in the behavior tree
    /// </summary>
    public class BehaviorTreeContext
    {
        public BehaviorTree Tree { get; private set; }
        public Blackboard Blackboard { get; private set; }
        public GameObject GameObject => Tree.gameObject;
        public Transform Transform => Tree.transform;

        public BehaviorTreeContext(BehaviorTree tree)
        {
            Tree = tree;
            Blackboard = new Blackboard();
        }

        public void Cleanup()
        {
            Blackboard?.Clear();
        }
    }

    #endregion

    #region Blackboard

    /// <summary>
    /// Shared data storage for behavior tree
    /// </summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new();

        public void SetValue<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (_data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public bool HasValue(string key)
        {
            return _data.ContainsKey(key);
        }

        public void RemoveValue(string key)
        {
            _data.Remove(key);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public Dictionary<string, object> GetAllValues()
        {
            return new Dictionary<string, object>(_data);
        }
    }

    #endregion

    #region Base Node Classes

    /// <summary>
    /// Base class for all behavior tree nodes
    /// </summary>
    public abstract class BehaviorNode
    {
        protected BehaviorTreeContext context;
        protected BehaviorTreeStatus lastStatus = BehaviorTreeStatus.Failure;
        protected float lastExecutionTime;
        protected int executionCount;

        public BehaviorTreeStatus LastStatus => lastStatus;
        public float LastExecutionTime => lastExecutionTime;
        public int ExecutionCount => executionCount;

        public virtual void SetContext(BehaviorTreeContext newContext)
        {
            context = newContext;
            OnContextSet();
        }

        public virtual BehaviorTreeStatus Execute()
        {
            var startTime = Time.realtimeSinceStartup;
            executionCount++;

            try
            {
                lastStatus = OnExecute();
                return lastStatus;
            }
            finally
            {
                lastExecutionTime = Time.realtimeSinceStartup - startTime;
            }
        }

        public virtual void Reset()
        {
            OnReset();
        }

        public virtual void Cleanup()
        {
            OnCleanup();
        }

        protected abstract BehaviorTreeStatus OnExecute();
        protected virtual void OnContextSet() { }
        protected virtual void OnReset() { }
        protected virtual void OnCleanup() { }

        // Helper methods
        protected T GetBlackboardValue<T>(string key, T defaultValue = default(T))
        {
            if (context?.Blackboard == null)
                return defaultValue;
            return context.Blackboard.GetValue(key, defaultValue);
        }

        protected void SetBlackboardValue<T>(string key, T value)
        {
            context?.Blackboard.SetValue(key, value);
        }

        protected GameObject GetGameObject()
        {
            return context?.GameObject;
        }

        protected Transform GetTransform()
        {
            return context?.Transform;
        }
    }

    /// <summary>
    /// Base class for composite nodes (nodes with children)
    /// </summary>
    public abstract class CompositeNode : BehaviorNode
    {
        protected List<BehaviorNode> children = new();
        protected int currentChildIndex = 0;

        public void AddChild(BehaviorNode child)
        {
            children.Add(child);
            child.SetContext(context);
        }

        public void RemoveChild(BehaviorNode child)
        {
            children.Remove(child);
        }

        public void ClearChildren()
        {
            children.Clear();
        }

        public override void SetContext(BehaviorTreeContext newContext)
        {
            base.SetContext(newContext);
            foreach (var child in children)
            {
                child.SetContext(newContext);
            }
        }

        protected override void OnReset()
        {
            currentChildIndex = 0;
            foreach (var child in children)
            {
                child.Reset();
            }
        }

        protected override void OnCleanup()
        {
            foreach (var child in children)
            {
                child.Cleanup();
            }
        }
    }

    /// <summary>
    /// Base class for decorator nodes (nodes with one child)
    /// </summary>
    public abstract class DecoratorNode : BehaviorNode
    {
        protected BehaviorNode child;

        public void SetChild(BehaviorNode newChild)
        {
            child = newChild;
            child?.SetContext(context);
        }

        public override void SetContext(BehaviorTreeContext newContext)
        {
            base.SetContext(newContext);
            child?.SetContext(newContext);
        }

        protected override void OnReset()
        {
            child?.Reset();
        }

        protected override void OnCleanup()
        {
            child?.Cleanup();
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Performance statistics for behavior tree monitoring
    /// </summary>
    public class BehaviorTreeStatistics
    {
        public int TotalExecutions { get; private set; }
        public int SuccessfulExecutions { get; private set; }
        public int FailedExecutions { get; private set; }
        public int RunningExecutions { get; private set; }
        public float TotalExecutionTime { get; private set; }
        public float AverageExecutionTime => TotalExecutions > 0 ? TotalExecutionTime / TotalExecutions : 0f;
        public float SuccessRate => TotalExecutions > 0 ? (float)SuccessfulExecutions / TotalExecutions * 100f : 0f;
        public DateTime SessionStart { get; private set; }

        public void Initialize()
        {
            SessionStart = DateTime.Now;
            Reset();
        }

        public void Reset()
        {
            TotalExecutions = 0;
            SuccessfulExecutions = 0;
            FailedExecutions = 0;
            RunningExecutions = 0;
            TotalExecutionTime = 0f;
        }

        public void RecordExecution(BehaviorTreeStatus status, float executionTime)
        {
            TotalExecutions++;
            TotalExecutionTime += executionTime;

            switch (status)
            {
                case BehaviorTreeStatus.Success:
                    SuccessfulExecutions++;
                    break;
                case BehaviorTreeStatus.Failure:
                    FailedExecutions++;
                    break;
                case BehaviorTreeStatus.Running:
                    RunningExecutions++;
                    break;
            }
        }
    }

    #endregion
}