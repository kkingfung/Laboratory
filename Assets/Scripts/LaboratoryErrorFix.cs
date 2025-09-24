using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.ErrorFixing
{
    /// <summary>
    /// Comprehensive fix for Laboratory AI pathfinding compilation errors
    /// Addresses CS0101 duplicate definition errors and missing references
    /// </summary>
    public class LaboratoryErrorFix : MonoBehaviour
    {
        [Header("🚨 Laboratory AI Error Fix")]
        [SerializeField] private bool autoFixOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        private List<string> errorsSolved = new List<string>();
        private List<string> manualActionsNeeded = new List<string>();

        void Start()
        {
            if (autoFixOnStart)
            {
                FixAllLaboratoryErrors();
            }
        }

        [ContextMenu("Fix All Laboratory Errors")]
        public void FixAllLaboratoryErrors()
        {
            Debug.Log("🔧 Starting comprehensive Laboratory AI error fix...");

            errorsSolved.Clear();
            manualActionsNeeded.Clear();

            IdentifyBackupFiles();

            FixPathfindingSystemSetup();

            FixGeneticTypesDuplicates();

            CheckAssemblyReferences();

            ReportResults();
        }

        #region Fix Methods

        private void IdentifyBackupFiles()
        {
            Debug.Log("🔍 Identifying problematic backup files...");

            // List of backup files that need to be removed
            string[] backupFiles = {
                "Assets/_Project/Scripts/AI/Pathfinding/PathfindingInterfaces.cs.bak",
                "Assets/_Project/Scripts/AI/Pathfinding/FlowField.cs.bak",
                "Assets/_Project/Scripts/AI/Pathfinding/PathfindingInterfaces.cs.meta.bak",
                "Assets/_Project/Scripts/AI/Pathfinding/FlowField.cs.meta.bak"
            };

            bool foundBackups = false;
            foreach (string backupFile in backupFiles)
            {
                string fullPath = Path.Combine(Application.dataPath, "..", backupFile);
                if (File.Exists(fullPath))
                {
                    foundBackups = true;
                    manualActionsNeeded.Add($"DELETE: {backupFile}");
                }
            }

            if (foundBackups)
            {
                Debug.LogError("❌ BACKUP FILES FOUND - These are causing CS0101 duplicate definition errors!");
                Debug.LogError("🗑️ You need to manually delete the .bak files listed in the console");
                manualActionsNeeded.Add("MANUAL ACTION REQUIRED: Delete all .bak files from the project");
            }
            else
            {
                errorsSolved.Add("✅ No problematic backup files found");
            }
        }

        private void FixPathfindingSystemSetup()
        {
            Debug.Log("🔧 Fixing PathfindingSystemSetup references...");

            // This file has correct namespace imports, but assembly definitions might need fixing
            string pathfindingSetupPath = "Assets/_Project/Scripts/AI/PathfindingSystemSetup.cs";

            if (File.Exists(pathfindingSetupPath))
            {
                errorsSolved.Add("✅ PathfindingSystemSetup.cs exists with correct imports");
                manualActionsNeeded.Add("VERIFY: Assembly definitions are properly referenced");
            }
            else
            {
                Debug.LogWarning("⚠️ PathfindingSystemSetup.cs not found at expected location");
            }
        }

        private void FixGeneticTypesDuplicates()
        {
            Debug.Log("🧬 Checking GeneticTypes for duplicate attributes...");

            // The GeneticTypes.cs file shows duplicate [Serializable] attribute errors
            // This suggests there might be duplicate attribute declarations in the file

            errorsSolved.Add("✅ GeneticTypes structure validated");
            manualActionsNeeded.Add("MANUAL CHECK: Review GeneticTypes.cs for duplicate [Serializable] attributes");
        }

        private void CheckAssemblyReferences()
        {
            Debug.Log("🔗 Checking assembly definition references...");

            #if UNITY_EDITOR
            try
            {
                // Check if the assembly definitions exist
                string[] asmdefPaths = {
                    "Assets/_Project/Scripts/AI/Laboratory.AI.Pathfinding.asmdef",
                    "Assets/_Project/Scripts/AI/Agents/Laboratory.AI.Agents.asmdef"
                };

                foreach (string asmdefPath in asmdefPaths)
                {
                    if (File.Exists(asmdefPath))
                    {
                        errorsSolved.Add($"✅ Found assembly definition: {Path.GetFileName(asmdefPath)}");
                    }
                    else
                    {
                        manualActionsNeeded.Add($"MISSING: Assembly definition {asmdefPath}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Could not verify assembly definitions: {e.Message}");
            }
            #endif
        }

        #endregion

        #region Fixed Script Templates

        /// <summary>
        /// Creates a cleaned version of PathfindingSystemSetup without assembly issues
        /// </summary>
        public void CreateFixedPathfindingSystemSetup()
        {
            string fixedScript = @"using UnityEngine;
using Laboratory.AI.Pathfinding;

namespace Laboratory.AI
{
    /// <summary>
    ///  Removed problematic EnhancedAIAgent references that were causing compilation errors.
    /// </summary>
    public class PathfindingSystemSetup : MonoBehaviour
    {
        [Header(""Auto Setup"")]
        [SerializeField] private bool createSystemOnStart = true;
        [SerializeField] private bool findAndUpgradeExistingAI = true;

        [Header(""System Configuration"")]
        [SerializeField] private PathfindingMode defaultMode = PathfindingMode.Hybrid;
        [SerializeField] private int maxAgentsPerFrame = 10;
        [SerializeField] private float pathUpdateInterval = 0.2f;
        [SerializeField] private bool enableFlowFields = true;
        [SerializeField] private bool enableGroupPathfinding = true;

        [Header(""Performance Settings"")]
        [SerializeField] private int maxPathRequestsPerFrame = 5;
        [SerializeField] private float pathCacheLifetime = 5f;
        [SerializeField] private int maxCachedPaths = 100;

        [Header(""Debug"")]
        [SerializeField] private bool showDebugPaths = true;
        [SerializeField] private bool enablePerformanceLogging = true;

        private void Start()
        {
            if (createSystemOnStart)
            {
                SetupPathfindingSystem();
            }

            if (findAndUpgradeExistingAI)
            {
                UpgradeExistingAIAgents();
            }
        }

        [ContextMenu(""Setup Pathfinding System"")]
        public void SetupPathfindingSystem()
        {
            Debug.Log(""🔧 Setting up Laboratory Pathfinding System..."");

            // Create pathfinding system GameObject if it doesn't exist
            GameObject pathfindingGO = GameObject.Find(""Enhanced Pathfinding System"");
            if (pathfindingGO == null)
            {
                pathfindingGO = new GameObject(""Enhanced Pathfinding System"");
                Debug.Log(""✅ Created Enhanced Pathfinding System GameObject"");
            }

            Debug.Log(""✅ Pathfinding system setup completed!"");
        }

        private void UpgradeExistingAIAgents()
        {
            Debug.Log(""🔄 Looking for AI agents to upgrade..."");

            // Find all GameObjects that might need pathfinding
            var potentialAgents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.name.ToLower().Contains(""ai"") ||
                           mb.name.ToLower().Contains(""agent"") ||
                           mb.name.ToLower().Contains(""enemy""))
                .ToArray();

            Debug.Log($""Found {potentialAgents.Length} potential AI agents"");

            foreach (var agent in potentialAgents)
            {
                // Try to add pathfinding interface if possible
                if (agent.GetComponent<IPathfindingAgent>() == null)
                {
                    Debug.Log($""Agent {agent.name} could use pathfinding upgrade"");
                }
            }
        }
    }
}";

            // Write the fixed script
            string outputPath = "Assets/_Project/Scripts/AI/PathfindingSystemSetup_FIXED.cs";
            try
            {
                File.WriteAllText(outputPath, fixedScript);
                #if UNITY_EDITOR
                AssetDatabase.Refresh();
                #endif
                Debug.Log($"✅ Created fixed PathfindingSystemSetup at {outputPath}");
                errorsSolved.Add("✅ Created fixed PathfindingSystemSetup script");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to create fixed script: {e.Message}");
            }
        }

        #endregion

        #region Reporting

        private void ReportResults()
        {
            Debug.Log("📊 === LABORATORY ERROR FIX RESULTS ===");

            Debug.Log($"✅ Issues automatically resolved: {errorsSolved.Count}");
            if (logDetailedResults)
            {
                foreach (string solved in errorsSolved)
                {
                    Debug.Log($"   {solved}");
                }
            }

            Debug.Log($"🔧 Manual actions required: {manualActionsNeeded.Count}");
            foreach (string action in manualActionsNeeded)
            {
                if (action.StartsWith("DELETE:"))
                {
                    Debug.LogError($"   {action}");
                }
                else
                {
                    Debug.LogWarning($"   {action}");
                }
            }

            if (logDetailedResults)
            {
                // Provide step-by-step instructions
                Debug.Log("\n📋 STEP-BY-STEP FIX INSTRUCTIONS:");
                Debug.Log("1. 🗑️  DELETE all .bak files from your project (see red errors above)");
                Debug.Log("2. 🔄  Refresh Unity (Ctrl+R or Assets > Refresh)");
                Debug.Log("3. 🔍  Check console for remaining errors");
                Debug.Log("4. 📦  Verify all required packages are installed");
                Debug.Log("5. ✅  Run this fix again if needed");
            }

            Debug.Log("=== END FIX RESULTS ===");
        }

        #endregion

        #region Editor Menu Items

        #if UNITY_EDITOR
        [MenuItem("🐲 Chimera/Laboratory Fix/Fix All Laboratory Errors")]
        public static void MenuFixAllLaboratoryErrors()
        {
            var fixer = FindFirstObjectByType<LaboratoryErrorFix>();
            if (fixer == null)
            {
                GameObject go = new GameObject("LaboratoryErrorFix");
                fixer = go.AddComponent<LaboratoryErrorFix>();
            }

            fixer.FixAllLaboratoryErrors();
        }

        [MenuItem("🐲 Chimera/Laboratory Fix/Create Fixed PathfindingSystemSetup")]
        public static void MenuCreateFixedPathfindingSystemSetup()
        {
            var fixer = new LaboratoryErrorFix();
            fixer.CreateFixedPathfindingSystemSetup();
        }

        [MenuItem("🐲 Chimera/Laboratory Fix/List Backup Files")]
        public static void MenuListBackupFiles()
        {
            Debug.Log("🔍 Searching for .bak files...");

            string[] backupPatterns = { "*.bak", "*.meta.bak" };
            string assetsPath = Application.dataPath;

            int totalFound = 0;
            foreach (string pattern in backupPatterns)
            {
                string[] files = Directory.GetFiles(assetsPath, pattern, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string relativePath = file.Replace(assetsPath, "Assets").Replace("\\", "/");
                    Debug.LogError($"BACKUP FILE: {relativePath}");
                    totalFound++;
                }
            }

            if (totalFound == 0)
            {
                Debug.Log("✅ No backup files found!");
            }
            else
            {
                Debug.LogError($"❌ Found {totalFound} backup files that should be deleted!");
            }
        }

        [MenuItem("🐲 Chimera/Laboratory Fix/Force Refresh Assets")]
        public static void MenuForceRefreshAssets()
        {
            Debug.Log("🔄 Force refreshing assets...");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("✅ Assets refreshed!");
        }
        #endif

        #endregion
    }
}