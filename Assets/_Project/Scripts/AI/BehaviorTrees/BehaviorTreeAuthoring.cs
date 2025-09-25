using Unity.Entities;
using UnityEngine;
using System;

namespace Laboratory.AI.BehaviorTrees
{
    /// <summary>
    /// BEHAVIOR TREE AUTHORING - Unity Inspector integration for behavior tree creation
    /// PURPOSE: Enable designers to create and edit behavior trees directly in Unity Inspector
    /// FEATURES: Visual tree editing, parameter configuration, runtime modification support
    /// WORKFLOW: Design in Inspector → Convert to ECS → Execute at runtime
    /// </summary>
    public class BehaviorTreeAuthoring : MonoBehaviour
    {
        [Header("Behavior Tree Configuration")]
        public string treeName = "New Behavior Tree";

        [Range(0.01f, 1f)]
        public float executionInterval = 0.1f;

        public bool allowInterrupts = true;

        [Range(1, 10)]
        public byte priority = 1;

        [Header("Tree Structure")]
        public BehaviorTreeNode rootNode;

        [Header("Runtime Debugging")]
        public bool enableDebugLogging = false;
        public bool showGizmos = true;

        private Entity _behaviorTreeEntity;
        private BehaviorTreeSystem _behaviorTreeSystem;

        private void Start()
        {
            InitializeBehaviorTree();
        }

        private void OnValidate()
        {
            // Validate tree structure
            if (rootNode != null)
            {
                ValidateTreeStructure(rootNode, 0);
            }
        }

        private void InitializeBehaviorTree()
        {
            if (rootNode == null)
            {
                Debug.LogWarning($"No root node assigned to behavior tree '{treeName}' on {gameObject.name}");
                return;
            }

            // Get the behavior tree system
            var world = World.DefaultGameObjectInjectionWorld;
            _behaviorTreeSystem = world?.GetExistingSystemManaged<BehaviorTreeSystem>();

            if (_behaviorTreeSystem == null)
            {
                Debug.LogError("BehaviorTreeSystem not found in world");
                return;
            }

            // Create behavior tree definition
            var definition = CreateBehaviorTreeDefinition();

            // Create ECS behavior tree entity
            _behaviorTreeEntity = _behaviorTreeSystem.CreateBehaviorTree(definition);

            // Link the entity to this GameObject for debugging
            var entityManager = world.EntityManager;
            if (entityManager.HasComponent<BehaviorTreeComponent>(_behaviorTreeEntity))
            {
                var component = entityManager.GetComponentData<BehaviorTreeComponent>(_behaviorTreeEntity);
                entityManager.SetComponentData(_behaviorTreeEntity, component);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"Initialized behavior tree '{treeName}' with entity {_behaviorTreeEntity}");
            }
        }

        private BehaviorTreeDefinition CreateBehaviorTreeDefinition()
        {
            var nodes = CollectAllNodes(rootNode);
            var nodeDefinitions = new BehaviorNodeDefinition[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                nodeDefinitions[i] = nodes[i].CreateDefinition(i);
            }

            return new BehaviorTreeDefinition
            {
                Name = treeName,
                ExecutionInterval = executionInterval,
                AllowInterrupts = allowInterrupts,
                Priority = priority,
                Nodes = nodeDefinitions
            };
        }

        private System.Collections.Generic.List<BehaviorTreeNode> CollectAllNodes(BehaviorTreeNode node)
        {
            var allNodes = new System.Collections.Generic.List<BehaviorTreeNode>();
            CollectNodesRecursive(node, allNodes);
            return allNodes;
        }

        private void CollectNodesRecursive(BehaviorTreeNode node, System.Collections.Generic.List<BehaviorTreeNode> allNodes)
        {
            if (node == null) return;

            allNodes.Add(node);

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    CollectNodesRecursive(child, allNodes);
                }
            }
        }

        private void ValidateTreeStructure(BehaviorTreeNode node, int depth)
        {
            if (depth > 10)
            {
                Debug.LogWarning($"Behavior tree depth exceeds 10 levels - potential infinite recursion in '{treeName}'");
                return;
            }

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child != null)
                    {
                        ValidateTreeStructure(child, depth + 1);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || rootNode == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

            if (enableDebugLogging && Application.isPlaying && _behaviorTreeEntity != Entity.Null)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager.Exists(_behaviorTreeEntity) == true)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
                }
            }
        }

        public void SetBehaviorTreeActive(bool active)
        {
            if (_behaviorTreeSystem != null && _behaviorTreeEntity != Entity.Null)
            {
                _behaviorTreeSystem.SetBehaviorTreeActive(_behaviorTreeEntity, active);
            }
        }

        public Entity GetBehaviorTreeEntity()
        {
            return _behaviorTreeEntity;
        }
    }

    /// <summary>
    /// Base class for behavior tree nodes in the authoring system
    /// </summary>
    [Serializable]
    public abstract class BehaviorTreeNode
    {
        [Header("Node Configuration")]
        public string nodeName = "Node";
        public NodeType nodeType;

        [Range(0f, 10f)]
        public float weight = 1f;

        [Range(0f, 5f)]
        public float cooldown = 0f;

        [Header("Child Nodes")]
        public BehaviorTreeNode[] children;

        public abstract BehaviorNodeDefinition CreateDefinition(int nodeId);

        protected virtual BehaviorNodeParameters CreateParameters()
        {
            return new BehaviorNodeParameters();
        }

        public virtual void ValidateNode()
        {
            // Override in derived classes for custom validation
        }
    }

    /// <summary>
    /// Composite nodes
    /// </summary>
    [Serializable]
    public class SequenceNode : BehaviorTreeNode
    {
        public SequenceNode()
        {
            nodeType = NodeType.Sequence;
            nodeName = "Sequence";
        }

        public override BehaviorNodeDefinition CreateDefinition(int nodeId)
        {
            return new BehaviorNodeDefinition
            {
                Id = nodeId,
                Type = nodeType,
                ParentId = -1,
                Weight = weight,
                Cooldown = cooldown,
                Parameters = CreateParameters()
            };
        }
    }

    [Serializable]
    public class SelectorNode : BehaviorTreeNode
    {
        public SelectorNode()
        {
            nodeType = NodeType.Selector;
            nodeName = "Selector";
        }

        public override BehaviorNodeDefinition CreateDefinition(int nodeId)
        {
            return new BehaviorNodeDefinition
            {
                Id = nodeId,
                Type = nodeType,
                ParentId = -1,
                Weight = weight,
                Cooldown = cooldown,
                Parameters = CreateParameters()
            };
        }
    }

    /// <summary>
    /// Action nodes
    /// </summary>
    [Serializable]
    public class MoveToNode : BehaviorTreeNode
    {
        [Header("Movement Parameters")]
        public Vector3 targetPosition;
        public float movementSpeed = 5f;
        public float stoppingDistance = 1f;

        public MoveToNode()
        {
            nodeType = NodeType.MoveTo;
            nodeName = "Move To";
        }

        public override BehaviorNodeDefinition CreateDefinition(int nodeId)
        {
            return new BehaviorNodeDefinition
            {
                Id = nodeId,
                Type = nodeType,
                ParentId = -1,
                Weight = weight,
                Cooldown = cooldown,
                Parameters = CreateParameters()
            };
        }

        protected override BehaviorNodeParameters CreateParameters()
        {
            return new BehaviorNodeParameters
            {
                targetPosition = targetPosition,
                movementSpeed = movementSpeed,
                stoppingDistance = stoppingDistance
            };
        }
    }

    [Serializable]
    public class WaitNode : BehaviorTreeNode
    {
        [Header("Wait Parameters")]
        [Range(0.1f, 10f)]
        public float waitDuration = 1f;

        public WaitNode()
        {
            nodeType = NodeType.Wait;
            nodeName = "Wait";
        }

        public override BehaviorNodeDefinition CreateDefinition(int nodeId)
        {
            return new BehaviorNodeDefinition
            {
                Id = nodeId,
                Type = nodeType,
                ParentId = -1,
                Weight = weight,
                Cooldown = cooldown,
                Parameters = CreateParameters()
            };
        }

        protected override BehaviorNodeParameters CreateParameters()
        {
            return new BehaviorNodeParameters
            {
                duration = waitDuration
            };
        }
    }

    /// <summary>
    /// Condition nodes
    /// </summary>
    [Serializable]
    public class IsTargetInRangeNode : BehaviorTreeNode
    {
        [Header("Range Parameters")]
        public GameObject targetObject;
        public float range = 5f;

        public IsTargetInRangeNode()
        {
            nodeType = NodeType.IsTargetInRange;
            nodeName = "Is Target In Range";
        }

        public override BehaviorNodeDefinition CreateDefinition(int nodeId)
        {
            return new BehaviorNodeDefinition
            {
                Id = nodeId,
                Type = nodeType,
                ParentId = -1,
                Weight = weight,
                Cooldown = cooldown,
                Parameters = CreateParameters()
            };
        }

        protected override BehaviorNodeParameters CreateParameters()
        {
            // Note: In a real implementation, you'd need to convert GameObject to Entity
            return new BehaviorNodeParameters
            {
                threshold = range,
                target = Entity.Null // Would need proper GameObject to Entity conversion
            };
        }
    }
}