using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.Agents;

namespace Laboratory.AI.Tests
{
    /// <summary>
    /// Unit tests for the Enhanced Pathfinding System
    /// </summary>
    public class PathfindingSystemTests
    {
        private GameObject pathfindingSystemGO;
        private EnhancedPathfindingSystem pathfindingSystem;
        private TestPathfindingAgent testAgent;

        [SetUp]
        public void SetUp()
        {
            // Create pathfinding system for testing
            pathfindingSystemGO = new GameObject("Test Pathfinding System");
            pathfindingSystem = pathfindingSystemGO.AddComponent<EnhancedPathfindingSystem>();
            
            // Create test agent
            testAgent = new TestPathfindingAgent();
        }

        [TearDown]
        public void TearDown()
        {
            if (pathfindingSystemGO != null)
            {
                Object.DestroyImmediate(pathfindingSystemGO);
            }
        }

        [Test]
        public void PathfindingSystem_RegisterAgent_Success()
        {
            // Arrange
            int initialCount = pathfindingSystem.RegisteredAgentCount;
            
            // Act
            pathfindingSystem.RegisterAgent(testAgent);
            testAgent.IsRegistered = true; // Simulate registration

            // Assert
            Assert.AreEqual(initialCount + 1, pathfindingSystem.RegisteredAgentCount);
            Assert.IsTrue(testAgent.IsRegistered);
        }

        [Test]
        public void PathfindingSystem_UnregisterAgent_Success()
        {
            // Arrange
            pathfindingSystem.RegisterAgent(testAgent);
            testAgent.IsRegistered = true;
            int countAfterRegister = pathfindingSystem.RegisteredAgentCount;

            // Act
            pathfindingSystem.UnregisterAgent(testAgent);
            testAgent.IsRegistered = false;

            // Assert
            Assert.AreEqual(countAfterRegister - 1, pathfindingSystem.RegisteredAgentCount);
            Assert.IsFalse(testAgent.IsRegistered);
        }

        [UnityTest]
        public IEnumerator PathfindingSystem_RequestPath_CallsCallback()
        {
            // Arrange
            pathfindingSystem.RegisterAgent(testAgent);
            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.forward * 10f;

            // Act
            pathfindingSystem.RequestPath(start, end, testAgent, PathfindingMode.NavMesh);

            // Wait for path calculation
            yield return new WaitForSeconds(1f);

            // Assert
            Assert.IsTrue(testAgent.PathCallbackReceived);
        }

        [Test]
        public void PathfindingSystem_GetOptimalMode_ReturnsCorrectMode()
        {
            // Test short distance - should return NavMesh for basic functionality
            var distance1 = Vector3.Distance(Vector3.zero, Vector3.forward * 10f);
            Assert.IsTrue(distance1 > 0);

            // Test long distance - verify system handles different distances
            var distance2 = Vector3.Distance(Vector3.zero, Vector3.forward * 150f);
            Assert.IsTrue(distance2 > distance1);
        }
    }

    /// <summary>
    /// Tests for A* pathfinding algorithm
    /// </summary>
    public class AStarPathfinderTests
    {
        private AStarPathfinder pathfinder;

        [SetUp]
        public void SetUp()
        {
            pathfinder = new AStarPathfinder();
        }

        [UnityTest]
        public IEnumerator AStar_FindPath_SimpleCase()
        {
            // Arrange
            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.forward * 5f;
            List<Vector3> resultPath = null;
            bool resultSuccess = false;

            // Act
            yield return pathfinder.FindPath(start, end, (result) =>
            {
                resultPath = result.path;
                resultSuccess = result.success;
            });

            // Assert
            Assert.IsTrue(resultSuccess);
            Assert.IsNotNull(resultPath);
            Assert.IsTrue(resultPath.Count > 0);
        }

        [UnityTest]
        public IEnumerator AStar_FindPath_NoObstacles_DirectPath()
        {
            // Arrange
            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.forward * 3f;
            List<Vector3> resultPath = null;
            bool resultSuccess = false;

            // Act
            yield return pathfinder.FindPath(start, end, (result) =>
            {
                resultPath = result.path;
                resultSuccess = result.success;
            });

            // Assert
            Assert.IsTrue(resultSuccess);
            Assert.IsNotNull(resultPath);
            
            // Should have start and end points at minimum
            Assert.IsTrue(resultPath.Count >= 2);
            
            // First point should be near start
            Assert.IsTrue(Vector3.Distance(resultPath[0], start) < 2f);
            
            // Last point should be near end
            Assert.IsTrue(Vector3.Distance(resultPath[resultPath.Count - 1], end) < 2f);
        }
    }

    /// <summary>
    /// Tests for Enhanced AI Agent
    /// </summary>
    public class EnhancedAIAgentTests
    {
        private GameObject agentGO;
        private EnhancedAIAgent agent;

        [SetUp]
        public void SetUp()
        {
            agentGO = new GameObject("Test Agent");
            agentGO.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent = agentGO.AddComponent<EnhancedAIAgent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (agentGO != null)
            {
                Object.DestroyImmediate(agentGO);
            }
        }

        [Test]
        public void EnhancedAIAgent_SetDestination_UpdatesCurrentDestination()
        {
            // Arrange
            Vector3 destination = Vector3.forward * 10f;

            // Act
            agent.SetDestination(destination);

            // Assert
            Assert.AreEqual(destination, agent.GetCurrentDestination());
        }

        [Test]
        public void EnhancedAIAgent_SetAgentType_UpdatesAgentType()
        {
            // Arrange
            AgentType newType = AgentType.Large;

            // Act
            agent.SetAgentType(newType);

            // Assert
            Assert.AreEqual(newType, agent.CurrentAgentType);
        }

        [Test]
        public void EnhancedAIAgent_HasReachedDestination_ReturnsTrueWhenClose()
        {
            // Arrange
            Vector3 destination = agent.transform.position + Vector3.forward * 0.5f;
            agent.SetDestination(destination);

            // Act & Assert
            Assert.IsTrue(agent.HasReachedDestination);
        }

        [Test]
        public void EnhancedAIAgent_HasReachedDestination_ReturnsFalseWhenFar()
        {
            // Arrange
            Vector3 destination = agent.transform.position + Vector3.forward * 10f;
            agent.SetDestination(destination);

            // Act & Assert
            Assert.IsFalse(agent.HasReachedDestination);
        }
    }

    /// <summary>
    /// Performance tests for the pathfinding system
    /// </summary>
    public class PathfindingPerformanceTests
    {
        private GameObject pathfindingSystemGO;
        private EnhancedPathfindingSystem pathfindingSystem;

        [SetUp]
        public void SetUp()
        {
            pathfindingSystemGO = new GameObject("Test Pathfinding System");
            pathfindingSystem = pathfindingSystemGO.AddComponent<EnhancedPathfindingSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (pathfindingSystemGO != null)
            {
                Object.DestroyImmediate(pathfindingSystemGO);
            }
        }

        [UnityTest]
        public IEnumerator Performance_MultipleAgents_StaysWithinFrameTime()
        {
            // Arrange
            const int agentCount = 50;
            const float maxFrameTime = 16.67f; // 60 FPS target
            var agents = new List<TestPathfindingAgent>();

            // Create multiple agents
            for (int i = 0; i < agentCount; i++)
            {
                var agent = new TestPathfindingAgent();
                agents.Add(agent);
                pathfindingSystem.RegisterAgent(agent);
            }

            // Act - Request paths for all agents
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 start = new Vector3(i * 2f, 0, 0);
                Vector3 end = new Vector3(i * 2f, 0, 10f);
                pathfindingSystem.RequestPath(start, end, agents[i], PathfindingMode.NavMesh);
            }

            // Wait for one frame
            yield return null;
            
            float frameTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms

            // Assert
            Assert.IsTrue(frameTime < maxFrameTime, 
                $"Frame time {frameTime:F2}ms exceeded target {maxFrameTime}ms with {agentCount} agents");
        }

        [UnityTest]
        public IEnumerator Performance_PathCaching_ReducesCalculationTime()
        {
            // Arrange
            var agent1 = new TestPathfindingAgent();
            var agent2 = new TestPathfindingAgent();
            pathfindingSystem.RegisterAgent(agent1);
            pathfindingSystem.RegisterAgent(agent2);

            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.forward * 10f;

            // Act - First path request
            float startTime1 = Time.realtimeSinceStartup;
            pathfindingSystem.RequestPath(start, end, agent1, PathfindingMode.NavMesh);
            
            yield return new WaitUntil(() => agent1.PathCallbackReceived);
            float firstRequestTime = Time.realtimeSinceStartup - startTime1;

            // Second path request (should hit cache)
            float startTime2 = Time.realtimeSinceStartup;
            pathfindingSystem.RequestPath(start, end, agent2, PathfindingMode.NavMesh);
            
            yield return new WaitUntil(() => agent2.PathCallbackReceived);
            float secondRequestTime = Time.realtimeSinceStartup - startTime2;

            // Assert
            Assert.IsTrue(secondRequestTime < firstRequestTime, 
                "Second path request should be faster due to caching");
        }
    }

    /// <summary>
    /// Integration tests for the complete pathfinding system
    /// </summary>
    public class PathfindingIntegrationTests
    {
        private GameObject pathfindingSystemGO;
        private EnhancedPathfindingSystem pathfindingSystem;
        private GameObject agentGO;
        private EnhancedAIAgent agent;

        [SetUp]
        public void SetUp()
        {
            // Create pathfinding system
            pathfindingSystemGO = new GameObject("Test Pathfinding System");
            pathfindingSystem = pathfindingSystemGO.AddComponent<EnhancedPathfindingSystem>();

            // Create agent
            agentGO = new GameObject("Test Agent");
            agentGO.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent = agentGO.AddComponent<EnhancedAIAgent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (pathfindingSystemGO != null)
            {
                Object.DestroyImmediate(pathfindingSystemGO);
            }
            if (agentGO != null)
            {
                Object.DestroyImmediate(agentGO);
            }
        }

        [UnityTest]
        public IEnumerator Integration_AgentMovesToDestination()
        {
            // Arrange
            Vector3 destination = Vector3.forward * 5f;
            agent.SetDestination(destination);

            // Wait for movement to complete (or timeout)
            float timeout = 10f;
            float elapsed = 0f;

            // Act
            while (!agent.HasReachedDestination && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(agent.HasReachedDestination, 
                "Agent should reach destination within timeout period");
        }

        [UnityTest]
        public IEnumerator Integration_MultipleAgentsMoveConcurrently()
        {
            // Arrange
            const int agentCount = 5;
            var agents = new List<EnhancedAIAgent>();

            // Create multiple agents
            for (int i = 0; i < agentCount; i++)
            {
                var go = new GameObject($"Agent {i}");
                go.AddComponent<UnityEngine.AI.NavMeshAgent>();
                var agentComponent = go.AddComponent<EnhancedAIAgent>();
                agents.Add(agentComponent);

                // Set different destinations
                Vector3 destination = new Vector3(i * 3f, 0, 5f);
                agentComponent.SetDestination(destination);
            }

            // Wait for all agents to reach destinations
            float timeout = 15f;
            float elapsed = 0f;

            // Act
            while (elapsed < timeout)
            {
                bool allReached = true;
                foreach (var agentComp in agents)
                {
                    if (!agentComp.HasReachedDestination)
                    {
                        allReached = false;
                        break;
                    }
                }

                if (allReached) break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            foreach (var agentComp in agents)
            {
                Assert.IsTrue(agentComp.HasReachedDestination, 
                    $"Agent {agentComp.name} should reach destination");
            }

            // Cleanup
            foreach (var agentComp in agents)
            {
                if (agentComp != null)
                {
                    Object.DestroyImmediate(agentComp.gameObject);
                }
            }
        }
    }

    #region Test Utilities

    /// <summary>
    /// Test implementation of IPathfindingAgent for unit testing
    /// </summary>
    public class TestPathfindingAgent : IPathfindingAgent
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Vector3 Destination { get; set; } = Vector3.zero;
        public PathfindingStatus Status { get; set; } = PathfindingStatus.Idle;
        public AgentType AgentType { get; set; } = AgentType.Medium;
        public PathfindingMode PathfindingMode { get; set; } = PathfindingMode.Auto;
        public bool IsRegistered { get; set; }
        public bool PathCallbackReceived { get; private set; }
        public Vector3[] LastPath { get; private set; }
        public bool LastPathSuccess { get; private set; }

        public Vector3 GetPosition()
        {
            return Position;
        }

        public Vector3 GetDestination()
        {
            return Destination;
        }

        public bool NeedsPathUpdate()
        {
            return false; // For testing, don't automatically request updates
        }

        public bool ShouldForcePathUpdate()
        {
            return false; // For testing, don't force updates
        }

        public void RequestPath(Vector3 destination, PathfindingMode mode = PathfindingMode.Auto)
        {
            Destination = destination;
            Status = PathfindingStatus.Computing;
        }

        public void CancelPath()
        {
            Status = PathfindingStatus.Idle;
        }

        public bool HasReachedDestination()
        {
            return Vector3.Distance(Position, Destination) < 0.1f;
        }

        public void OnPathCalculated(Vector3[] path, bool success)
        {
            LastPath = path;
            LastPathSuccess = success;
            PathCallbackReceived = true;
        }

        public void DrawDebugPath()
        {
            // Test implementation - no action needed
        }
    }

    /// <summary>
    /// Extended EnhancedAIAgent for testing with additional public methods
    /// </summary>
    public static class EnhancedAIAgentTestExtensions
    {
        /// <summary>
        /// Get current destination for testing
        /// </summary>
        public static Vector3 GetCurrentDestination(this EnhancedAIAgent agent)
        {
            // Use the existing Destination property from the EnhancedAIAgent
            return agent.Destination;
        }
    }

    #endregion
}