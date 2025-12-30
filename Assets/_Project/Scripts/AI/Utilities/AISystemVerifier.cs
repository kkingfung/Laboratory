using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace ProjectChimera.AI.Utilities
{
    /// <summary>
    /// Verifies that all AI system components are properly compiled
    /// </summary>
    public static class AISystemVerifier
    {
        [MenuItem("Tools/AI System/Verify Compilation")]
        public static void VerifyCompilation()
        {
            Debug.Log("=== AI System Compilation Verification ===");
            
            // Check if all types can be found
            System.Type[] requiredTypes = new System.Type[]
            {
                System.Type.GetType("ProjectChimera.AI.Pathfinding.IPathfindingAgent, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Pathfinding.PathfindingMode, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Pathfinding.PathRequest, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Pathfinding.CachedPath, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Pathfinding.FlowField, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Pathfinding.FlowFieldGenerator, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Pathfinding.EnhancedPathfindingSystem, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Agents.EnhancedAIAgent, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.PathfindingSystemSetup, Assembly-CSharp"),
                System.Type.GetType("ProjectChimera.AI.Utilities.PathfindingProfiler, Assembly-CSharp")
            };
            
            bool allTypesFound = true;
            foreach (var type in requiredTypes)
            {
                if (type != null)
                {
                    Debug.Log($"[AI] Found: {type.FullName}");
                }
                else
                {
                    Debug.LogError("[AI] Missing type!");
                    allTypesFound = false;
                }
            }
            
            if (allTypesFound)
            {
                Debug.Log("\n[AI] All AI System components compiled successfully!");
            }
            else
            {
                Debug.LogError("\n[AI] Some components failed to compile. Check the Console for errors.");
            }
        }
        
        [MenuItem("Tools/AI System/Force Reimport")]
        public static void ForceReimport()
        {
            Debug.Log("Force reimporting AI system scripts...");
            
            // Reimport the scripts in the correct order
            AssetDatabase.ImportAsset("Assets/_Project/Scripts/AI/Pathfinding/PathfindingInterfaces.cs", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/Scripts/AI/Pathfinding/FlowField.cs", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/Scripts/AI/Pathfinding/EnhancedPathfindingSystem.cs", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/Scripts/AI/Agents/EnhancedAIAgent.cs", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/Scripts/AI/PathfindingSystemSetup.cs", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/Scripts/AI/Utilities/PathfindingProfiler.cs", ImportAssetOptions.ForceUpdate);
            
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            
            Debug.Log("Reimport complete! Check if compilation errors are resolved.");
        }
    }
}
#endif
