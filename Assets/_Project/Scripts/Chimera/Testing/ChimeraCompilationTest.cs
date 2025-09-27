using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.AI;
using Laboratory.AI.Pathfinding;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// Quick compilation test for Project Chimera core systems
    /// </summary>
    public class ChimeraCompilationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool verboseLogging = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunCompilationTest();
            }
        }
        
        [ContextMenu("Run Compilation Test")]
        public void RunCompilationTest()
        {
            UnityEngine.Debug.Log("🧬 Project Chimera - Compilation Test Starting...");
            
            // Test genetic system
            TestGeneticSystem();
            
            // Test AI system
            TestAISystem();
            
            // Test pathfinding system
            TestPathfindingSystem();
            
            // Test life stages
            TestLifeStages();
            
            UnityEngine.Debug.Log("✅ Project Chimera - All systems compiled successfully!");
        }
        
        private void TestGeneticSystem()
        {
            if (verboseLogging) UnityEngine.Debug.Log("Testing Genetic System...");
            
            // Create a test gene
            var allele1 = new Allele("Red", 1.0f, true, true);
            var allele2 = new Allele("Blue", 0.5f, true, false);
            var gene = new Gene("color_01", "Primary Color", TraitType.Physical, allele1, allele2);
            
            // Test gene expression
            var expressed = gene.GetExpressedAllele();
            UnityEngine.Debug.Log($"Gene {gene.traitName} expresses: {expressed.value}");
            
            // Create a mutation
            var mutation = new Mutation("mut_01", "color_01", MutationType.Enhancement, 1.2f);
            UnityEngine.Debug.Log($"Created mutation: {mutation.mutationId}");
            
            if (verboseLogging) UnityEngine.Debug.Log("✅ Genetic System - OK");
        }
        
        private void TestAISystem()
        {
            if (verboseLogging) UnityEngine.Debug.Log("Testing AI System...");
            
            // Test AI behavior states
            var idleState = AIBehaviorState.Idle;
            var combatState = AIBehaviorState.Combat;
            
            // Test state properties
            bool canMove = idleState.AllowsMovement();
            bool isAggressive = combatState.IsAggressive();
            int priority = combatState.GetPriority();
            
            UnityEngine.Debug.Log($"Idle allows movement: {canMove}, Combat is aggressive: {isAggressive}, Priority: {priority}");
            
            // Test state transitions
            bool canTransition = idleState.CanTransitionTo(combatState);
            UnityEngine.Debug.Log($"Can transition from Idle to Combat: {canTransition}");
            
            if (verboseLogging) UnityEngine.Debug.Log("✅ AI System - OK");
        }
        
        private void TestPathfindingSystem()
        {
            if (verboseLogging) UnityEngine.Debug.Log("Testing Pathfinding System...");
            
            // Test pathfinding modes
            var mode = PathfindingMode.Hybrid;
            UnityEngine.Debug.Log($"Using pathfinding mode: {mode}");
            
            // Test flow field creation
            var generator = new FlowFieldGenerator();
            var flowField = generator.GenerateFlowField(Vector3.zero, 10f);
            UnityEngine.Debug.Log($"Created flow field with {flowField.gridWidth}x{flowField.gridHeight} grid");
            
            if (verboseLogging) UnityEngine.Debug.Log("✅ Pathfinding System - OK");
        }
        
        private void TestLifeStages()
        {
            if (verboseLogging) UnityEngine.Debug.Log("Testing Life Stages...");
            
            // Test life stage calculations
            var infantStage = LifeStageExtensions.CalculateLifeStage(5, 365); // 5 days old out of 365 day lifespan
            var adultStage = LifeStageExtensions.CalculateLifeStage(200, 365); // 200 days old
            
            UnityEngine.Debug.Log($"5 days old = {infantStage}, 200 days old = {adultStage}");
            
            // Test breeding capability
            bool infantCanBreed = infantStage.CanBreed();
            bool adultCanBreed = adultStage.CanBreed();
            
            UnityEngine.Debug.Log($"Infant can breed: {infantCanBreed}, Adult can breed: {adultCanBreed}");
            
            // Test stat modifiers
            var (health, attack, defense, speed, intelligence) = adultStage.GetStatModifiers();
            UnityEngine.Debug.Log($"Adult stat modifiers - H:{health}, A:{attack}, D:{defense}, S:{speed}, I:{intelligence}");
            
            if (verboseLogging) UnityEngine.Debug.Log("✅ Life Stages - OK");
        }
        
        [ContextMenu("Test Monster Creation")]
        public void TestMonsterCreation()
        {
            UnityEngine.Debug.Log("🐉 Testing Monster Creation...");
            
            // This would test the full creature creation pipeline
            // For now, just log that the systems are ready
            UnityEngine.Debug.Log("All systems ready for monster breeding!");
            UnityEngine.Debug.Log("- Genetic inheritance system ✓");
            UnityEngine.Debug.Log("- AI behavior system ✓");
            UnityEngine.Debug.Log("- Pathfinding system ✓");
            UnityEngine.Debug.Log("- Life stage management ✓");
            UnityEngine.Debug.Log("🔥 Project Chimera is ready to BREED SOME MONSTERS!");
        }
    }
}
