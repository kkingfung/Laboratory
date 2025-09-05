using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.EnemyAI
{
    #region Composite Nodes

    /// <summary>
    /// Sequence node - executes children in order until one fails
    /// </summary>
    public class SequenceNode : CompositeNode
    {
        protected override BehaviorTreeStatus OnExecute()
        {
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                var status = children[i].Execute();

                switch (status)
                {
                    case BehaviorTreeStatus.Running:
                        currentChildIndex = i;
                        return BehaviorTreeStatus.Running;

                    case BehaviorTreeStatus.Failure:
                        currentChildIndex = 0;
                        return BehaviorTreeStatus.Failure;

                    case BehaviorTreeStatus.Success:
                        continue; // Move to next child
                }
            }

            // All children succeeded
            currentChildIndex = 0;
            return BehaviorTreeStatus.Success;
        }
    }

    /// <summary>
    /// Selector node - executes children until one succeeds
    /// </summary>
    public class SelectorNode : CompositeNode
    {
        protected override BehaviorTreeStatus OnExecute()
        {
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                var status = children[i].Execute();

                switch (status)
                {
                    case BehaviorTreeStatus.Running:
                        currentChildIndex = i;
                        return BehaviorTreeStatus.Running;

                    case BehaviorTreeStatus.Success:
                        currentChildIndex = 0;
                        return BehaviorTreeStatus.Success;

                    case BehaviorTreeStatus.Failure:
                        continue; // Try next child
                }
            }

            // All children failed
            currentChildIndex = 0;
            return BehaviorTreeStatus.Failure;
        }
    }

    /// <summary>
    /// Parallel node - executes all children simultaneously
    /// </summary>
    public class ParallelNode : CompositeNode
    {
        public enum ParallelPolicy
        {
            RequireAll,     // All children must succeed
            RequireOne,     // At least one child must succeed
            RequireCount    // Specific number of children must succeed
        }

        private ParallelPolicy successPolicy = ParallelPolicy.RequireAll;
        private ParallelPolicy failurePolicy = ParallelPolicy.RequireOne;
        private int requiredSuccessCount = 1;
        private int requiredFailureCount = 1;

        public ParallelNode(ParallelPolicy success = ParallelPolicy.RequireAll, 
                           ParallelPolicy failure = ParallelPolicy.RequireOne)
        {
            successPolicy = success;
            failurePolicy = failure;
        }

        public ParallelNode(int successCount, int failureCount)
        {
            successPolicy = ParallelPolicy.RequireCount;
            failurePolicy = ParallelPolicy.RequireCount;
            requiredSuccessCount = successCount;
            requiredFailureCount = failureCount;
        }

        protected override BehaviorTreeStatus OnExecute()
        {
            int successCount = 0;
            int failureCount = 0;
            int runningCount = 0;

            foreach (var child in children)
            {
                var status = child.Execute();

                switch (status)
                {
                    case BehaviorTreeStatus.Success:
                        successCount++;
                        break;
                    case BehaviorTreeStatus.Failure:
                        failureCount++;
                        break;
                    case BehaviorTreeStatus.Running:
                        runningCount++;
                        break;
                }
            }

            // Check failure conditions first
            if (ShouldFail(failureCount, children.Count))
            {
                return BehaviorTreeStatus.Failure;
            }

            // Check success conditions
            if (ShouldSucceed(successCount, children.Count))
            {
                return BehaviorTreeStatus.Success;
            }

            // If we have running children, continue running
            if (runningCount > 0)
            {
                return BehaviorTreeStatus.Running;
            }

            // Default to failure if no specific condition is met
            return BehaviorTreeStatus.Failure;
        }

        private bool ShouldSucceed(int successCount, int totalCount)
        {
            return successPolicy switch
            {
                ParallelPolicy.RequireAll => successCount == totalCount,
                ParallelPolicy.RequireOne => successCount >= 1,
                ParallelPolicy.RequireCount => successCount >= requiredSuccessCount,
                _ => false
            };
        }

        private bool ShouldFail(int failureCount, int totalCount)
        {
            return failurePolicy switch
            {
                ParallelPolicy.RequireAll => failureCount == totalCount,
                ParallelPolicy.RequireOne => failureCount >= 1,
                ParallelPolicy.RequireCount => failureCount >= requiredFailureCount,
                _ => false
            };
        }
    }

    /// <summary>
    /// Random selector - randomly picks a child to execute
    /// </summary>
    public class RandomSelectorNode : CompositeNode
    {
        private int selectedChildIndex = -1;
        private bool hasSelectedChild = false;

        protected override BehaviorTreeStatus OnExecute()
        {
            if (children.Count == 0)
                return BehaviorTreeStatus.Failure;

            // Select a random child if we haven't already
            if (!hasSelectedChild)
            {
                selectedChildIndex = Random.Range(0, children.Count);
                hasSelectedChild = true;
            }

            var status = children[selectedChildIndex].Execute();

            if (status != BehaviorTreeStatus.Running)
            {
                hasSelectedChild = false;
            }

            return status;
        }

        protected override void OnReset()
        {
            base.OnReset();
            hasSelectedChild = false;
            selectedChildIndex = -1;
        }
    }

    #endregion

    #region Decorator Nodes

    /// <summary>
    /// Inverter node - inverts the result of its child
    /// </summary>
    public class InverterNode : DecoratorNode
    {
        protected override BehaviorTreeStatus OnExecute()
        {
            if (child == null)
                return BehaviorTreeStatus.Failure;

            var status = child.Execute();

            return status switch
            {
                BehaviorTreeStatus.Success => BehaviorTreeStatus.Failure,
                BehaviorTreeStatus.Failure => BehaviorTreeStatus.Success,
                BehaviorTreeStatus.Running => BehaviorTreeStatus.Running,
                _ => BehaviorTreeStatus.Failure
            };
        }
    }

    /// <summary>
    /// Repeater node - repeats its child a specified number of times
    /// </summary>
    public class RepeaterNode : DecoratorNode
    {
        private int repeatCount;
        private int currentRepeat = 0;
        private bool infiniteRepeat;

        public RepeaterNode(int count = -1)
        {
            if (count < 0)
            {
                infiniteRepeat = true;
            }
            else
            {
                repeatCount = count;
                infiniteRepeat = false;
            }
        }

        protected override BehaviorTreeStatus OnExecute()
        {
            if (child == null)
                return BehaviorTreeStatus.Failure;

            while (infiniteRepeat || currentRepeat < repeatCount)
            {
                var status = child.Execute();

                if (status == BehaviorTreeStatus.Running)
                {
                    return BehaviorTreeStatus.Running;
                }

                currentRepeat++;
                child.Reset();

                if (!infiniteRepeat && currentRepeat >= repeatCount)
                {
                    break;
                }
            }

            currentRepeat = 0;
            return BehaviorTreeStatus.Success;
        }

        protected override void OnReset()
        {
            base.OnReset();
            currentRepeat = 0;
        }
    }

    /// <summary>
    /// Succeeder node - always returns success regardless of child result
    /// </summary>
    public class SucceederNode : DecoratorNode
    {
        protected override BehaviorTreeStatus OnExecute()
        {
            if (child == null)
                return BehaviorTreeStatus.Success;

            child.Execute();
            return BehaviorTreeStatus.Success;
        }
    }

    /// <summary>
    /// Until fail node - repeats child until it fails, then returns success
    /// </summary>
    public class UntilFailNode : DecoratorNode
    {
        protected override BehaviorTreeStatus OnExecute()
        {
            if (child == null)
                return BehaviorTreeStatus.Failure;

            var status = child.Execute();

            return status switch
            {
                BehaviorTreeStatus.Failure => BehaviorTreeStatus.Success,
                BehaviorTreeStatus.Success => BehaviorTreeStatus.Running,
                BehaviorTreeStatus.Running => BehaviorTreeStatus.Running,
                _ => BehaviorTreeStatus.Failure
            };
        }
    }

    /// <summary>
    /// Cooldown node - prevents child execution until cooldown expires
    /// </summary>
    public class CooldownNode : DecoratorNode
    {
        private float cooldownTime;
        private float lastCooldownTime = -1f;

        public CooldownNode(float cooldown)
        {
            cooldownTime = cooldown;
        }

        protected override BehaviorTreeStatus OnExecute()
        {
            if (child == null)
                return BehaviorTreeStatus.Failure;

            // Check if we're still in cooldown
            if (lastCooldownTime >= 0f && Time.time - lastCooldownTime < cooldownTime)
            {
                return BehaviorTreeStatus.Failure;
            }

            var status = child.Execute();

            // Record execution time when child completes
            if (status != BehaviorTreeStatus.Running)
            {
                lastCooldownTime = Time.time;
            }

            return status;
        }

        protected override void OnReset()
        {
            base.OnReset();
            lastCooldownTime = -1f;
        }
    }

    /// <summary>
    /// Timeout node - fails if child takes too long to complete
    /// </summary>
    public class TimeoutNode : DecoratorNode
    {
        private float timeoutDuration;
        private float startTime = -1f;

        public TimeoutNode(float timeout)
        {
            timeoutDuration = timeout;
        }

        protected override BehaviorTreeStatus OnExecute()
        {
            if (child == null)
                return BehaviorTreeStatus.Failure;

            // Start timer on first execution
            if (startTime < 0f)
            {
                startTime = Time.time;
            }

            // Check for timeout
            if (Time.time - startTime >= timeoutDuration)
            {
                startTime = -1f;
                return BehaviorTreeStatus.Failure;
            }

            var status = child.Execute();

            // Reset timer when child completes
            if (status != BehaviorTreeStatus.Running)
            {
                startTime = -1f;
            }

            return status;
        }

        protected override void OnReset()
        {
            base.OnReset();
            startTime = -1f;
        }
    }

    #endregion

    #region Condition Nodes

    /// <summary>
    /// Base class for condition checking nodes
    /// </summary>
    public abstract class ConditionNode : BehaviorNode
    {
        protected override BehaviorTreeStatus OnExecute()
        {
            return CheckCondition() ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }

        protected abstract bool CheckCondition();
    }

    /// <summary>
    /// Checks if a blackboard value meets a condition
    /// </summary>
    public class BlackboardConditionNode : ConditionNode
    {
        private string key;
        private object expectedValue;
        private System.Func<object, object, bool> comparisonFunc;

        public BlackboardConditionNode(string blackboardKey, object value, System.Func<object, object, bool> comparison = null)
        {
            key = blackboardKey;
            expectedValue = value;
            comparisonFunc = comparison ?? ((a, b) => Equals(a, b));
        }

        protected override bool CheckCondition()
        {
            var actualValue = GetBlackboardValue<object>(key);
            return comparisonFunc(actualValue, expectedValue);
        }
    }

    /// <summary>
    /// Checks distance to a target
    /// </summary>
    public class DistanceConditionNode : ConditionNode
    {
        private string targetKey;
        private float requiredDistance;
        private bool lessThan;

        public DistanceConditionNode(string targetBlackboardKey, float distance, bool lessThanDistance = true)
        {
            targetKey = targetBlackboardKey;
            requiredDistance = distance;
            lessThan = lessThanDistance;
        }

        protected override bool CheckCondition()
        {
            var target = GetBlackboardValue<Transform>(targetKey);
            if (target == null) return false;

            var selfTransform = GetTransform();
            if (selfTransform == null) return false;

            float actualDistance = Vector3.Distance(selfTransform.position, target.position);

            return lessThan ? actualDistance <= requiredDistance : actualDistance >= requiredDistance;
        }
    }

    /// <summary>
    /// Checks if target is in line of sight
    /// </summary>
    public class LineOfSightConditionNode : ConditionNode
    {
        private string targetKey;
        private LayerMask obstacleMask;
        private float maxDistance;

        public LineOfSightConditionNode(string targetBlackboardKey, LayerMask obstacles, float maxDist = float.MaxValue)
        {
            targetKey = targetBlackboardKey;
            obstacleMask = obstacles;
            maxDistance = maxDist;
        }

        protected override bool CheckCondition()
        {
            var target = GetBlackboardValue<Transform>(targetKey);
            var selfTransform = GetTransform();

            if (target == null || selfTransform == null) return false;

            Vector3 direction = target.position - selfTransform.position;
            float distance = direction.magnitude;

            if (distance > maxDistance) return false;

            return !Physics.Raycast(selfTransform.position, direction.normalized, distance, obstacleMask);
        }
    }

    #endregion

    #region Action Nodes

    /// <summary>
    /// Base class for action nodes
    /// </summary>
    public abstract class ActionNode : BehaviorNode
    {
        protected bool isExecuting = false;
        protected float actionStartTime;

        protected override BehaviorTreeStatus OnExecute()
        {
            if (!isExecuting)
            {
                isExecuting = true;
                actionStartTime = Time.time;
                OnActionStart();
            }

            var status = OnActionUpdate();

            if (status != BehaviorTreeStatus.Running)
            {
                isExecuting = false;
                OnActionComplete(status);
            }

            return status;
        }

        protected override void OnReset()
        {
            if (isExecuting)
            {
                isExecuting = false;
                OnActionInterrupted();
            }
        }

        protected abstract void OnActionStart();
        protected abstract BehaviorTreeStatus OnActionUpdate();
        protected virtual void OnActionComplete(BehaviorTreeStatus finalStatus) { }
        protected virtual void OnActionInterrupted() { }
    }

    /// <summary>
    /// Wait for a specified duration
    /// </summary>
    public class WaitNode : ActionNode
    {
        private float waitDuration;

        public WaitNode(float duration)
        {
            waitDuration = duration;
        }

        protected override void OnActionStart()
        {
            // Nothing to do on start
        }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            float elapsed = Time.time - actionStartTime;
            return elapsed >= waitDuration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
        }
    }

    /// <summary>
    /// Move to a position
    /// </summary>
    public class MoveToPositionNode : ActionNode
    {
        private Vector3 targetPosition;
        private float moveSpeed;
        private float stoppingDistance;

        public MoveToPositionNode(Vector3 position, float speed = 5f, float stoppingDist = 0.1f)
        {
            targetPosition = position;
            moveSpeed = speed;
            stoppingDistance = stoppingDist;
        }

        protected override void OnActionStart()
        {
            // Could play movement animation here
        }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            var selfTransform = GetTransform();
            if (selfTransform == null) return BehaviorTreeStatus.Failure;

            float distance = Vector3.Distance(selfTransform.position, targetPosition);
            
            if (distance <= stoppingDistance)
            {
                return BehaviorTreeStatus.Success;
            }

            // Move towards target
            Vector3 direction = (targetPosition - selfTransform.position).normalized;
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            selfTransform.position += movement;

            // Face movement direction
            if (movement.magnitude > 0.01f)
            {
                selfTransform.rotation = Quaternion.LookRotation(direction);
            }

            return BehaviorTreeStatus.Running;
        }
    }

    /// <summary>
    /// Move to a target from blackboard
    /// </summary>
    public class MoveToTargetNode : ActionNode
    {
        private string targetKey;
        private float moveSpeed;
        private float stoppingDistance;

        public MoveToTargetNode(string targetBlackboardKey, float speed = 5f, float stoppingDist = 0.1f)
        {
            targetKey = targetBlackboardKey;
            moveSpeed = speed;
            stoppingDistance = stoppingDist;
        }

        protected override void OnActionStart()
        {
            // Could play movement animation here
        }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            var target = GetBlackboardValue<Transform>(targetKey);
            var selfTransform = GetTransform();

            if (target == null || selfTransform == null)
                return BehaviorTreeStatus.Failure;

            float distance = Vector3.Distance(selfTransform.position, target.position);
            
            if (distance <= stoppingDistance)
            {
                return BehaviorTreeStatus.Success;
            }

            // Move towards target
            Vector3 direction = (target.position - selfTransform.position).normalized;
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            selfTransform.position += movement;

            // Face movement direction
            if (movement.magnitude > 0.01f)
            {
                selfTransform.rotation = Quaternion.LookRotation(direction);
            }

            return BehaviorTreeStatus.Running;
        }
    }

    /// <summary>
    /// Set a blackboard value
    /// </summary>
    public class SetBlackboardValueNode : ActionNode
    {
        private string key;
        private object value;

        public SetBlackboardValueNode(string blackboardKey, object val)
        {
            key = blackboardKey;
            value = val;
        }

        protected override void OnActionStart() { }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            SetBlackboardValue(key, value);
            return BehaviorTreeStatus.Success;
        }
    }

    /// <summary>
    /// Debug log node - outputs a message to console
    /// </summary>
    public class DebugLogNode : ActionNode
    {
        private string message;
        private LogType logType;

        public DebugLogNode(string msg, LogType type = LogType.Log)
        {
            message = msg;
            logType = type;
        }

        protected override void OnActionStart() { }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log($"[BehaviorTree] {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[BehaviorTree] {message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[BehaviorTree] {message}");
                    break;
            }

            return BehaviorTreeStatus.Success;
        }
    }

    #endregion
}