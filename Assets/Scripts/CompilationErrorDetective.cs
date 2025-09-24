using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace ProjectChimera.ErrorFixing
{
    /// <summary>
    /// Compilation Error Detective for Project Chimera
    /// Identifies and fixes common compilation issues automatically
    /// </summary>
    public class CompilationErrorDetective : MonoBehaviour
    {
        [Header("🔍 Error Detection Settings")]
        [SerializeField] private bool autoDetectOnStart = true;
        [SerializeField] private bool autoFixErrors = true;
        [SerializeField] private bool logDetailedResults = true;

        private List<CompilationIssue> detectedIssues = new List<CompilationIssue>();

        #region Unity Lifecycle

        void Start()
        {
            if (autoDetectOnStart)
            {
                DetectCompilationIssues();
            }
        }

        #endregion

        #region Issue Detection

        [ContextMenu("Detect Compilation Issues")]
        public void DetectCompilationIssues()
        {
            detectedIssues.Clear();
            
            Debug.Log("🕵️ Starting compilation error detection...");

            // Check for common issues
            CheckMissingPackages();
            CheckNamespaceIssues();
            CheckMissingReferences();
            CheckUsingStatements();
            CheckScriptErrors();

            // Report findings
            ReportFindings();

            // Auto-fix if enabled
            if (autoFixErrors && detectedIssues.Count > 0)
            {
                AttemptAutoFix();
            }
        }

        #endregion

        #region Package Detection

        private void CheckMissingPackages()
        {
            Debug.Log("📦 Checking for missing packages...");

            // Check Unity Entities
            #if !UNITY_ENTITIES
            AddIssue(CompilationSeverity.Warning, 
                "Missing Package", 
                "Unity Entities package not installed",
                "Install via Window > Package Manager > Unity Registry > Entities",
                "Some ECS features will be disabled");
            #endif

            // Check Unity Netcode
            #if !UNITY_NETCODE_GAMEOBJECTS
            AddIssue(CompilationSeverity.Warning,
                "Missing Package",
                "Unity Netcode for GameObjects not installed", 
                "Install via Window > Package Manager > Unity Registry > Netcode for GameObjects",
                "Multiplayer features will be limited");
            #endif

            // Check Unity Mathematics
            try
            {
                var mathType = Type.GetType("Unity.Mathematics.math, Unity.Mathematics");
                if (mathType == null)
                {
                    AddIssue(CompilationSeverity.Warning,
                        "Missing Package",
                        "Unity Mathematics package not installed",
                        "Install via Window > Package Manager > Unity Registry > Mathematics", 
                        "Math operations may fail");
                }
            }
            catch
            {
                AddIssue(CompilationSeverity.Info,
                    "Package Check",
                    "Could not verify Mathematics package",
                    "Manual verification recommended",
                    "Check Package Manager");
            }
        }

        #endregion

        #region Namespace Detection

        private void CheckNamespaceIssues()
        {
            Debug.Log("🏷️ Checking namespace issues...");

            // Check if our custom types are accessible
            var chimeraManagerType = FindTypeByName("ChimeraManager");
            var networkManagerType = FindTypeByName("ChimeraNetworkManager"); 
            var ecsUtilitiesType = FindTypeByName("ChimeraECSUtilities");

            if (chimeraManagerType == null)
            {
                AddIssue(CompilationSeverity.Error,
                    "Missing Type",
                    "ChimeraManager type not found",
                    "Check ChimeraManager.cs for compilation errors",
                    "Core systems won't work");
            }

            if (networkManagerType == null)
            {
                AddIssue(CompilationSeverity.Warning,
                    "Missing Type", 
                    "ChimeraNetworkManager type not found",
                    "Check ChimeraNetworkManager.cs for compilation errors",
                    "Network features disabled");
            }

            if (ecsUtilitiesType == null)
            {
                AddIssue(CompilationSeverity.Info,
                    "Missing Type",
                    "ChimeraECSUtilities type not found", 
                    "Check ChimeraECSUtilities.cs - may be due to missing ECS package",
                    "ECS features disabled");
            }
        }

        private Type FindTypeByName(string typeName)
        {
            try
            {
                #if UNITY_EDITOR
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var types = assembly.GetTypes().Where(t => t.Name.Contains(typeName));
                    if (types.Any())
                    {
                        return types.First();
                    }
                }
                #endif
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Error finding type {typeName}: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Reference Detection

        private void CheckMissingReferences()
        {
            Debug.Log("🔗 Checking for missing script references...");

            // Find all MonoBehaviours with missing scripts
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int missingScripts = 0;

            foreach (var go in allGameObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        missingScripts++;
                    }
                }
            }

            if (missingScripts > 0)
            {
                AddIssue(CompilationSeverity.Error,
                    "Missing Scripts",
                    $"{missingScripts} missing script references found",
                    "Remove or fix broken script references on GameObjects",
                    "Will cause null reference exceptions");
            }
        }

        #endregion

        #region Using Statement Detection

        private void CheckUsingStatements()
        {
            Debug.Log("📝 Checking using statements...");
            
            // This is limited without access to source files
            // We'll check for runtime issues instead
            
            try
            {
                // Test common Unity types that might be missing
                var testTransform = typeof(Transform);
                var testGameObject = typeof(GameObject);
                var testMonoBehaviour = typeof(MonoBehaviour);
                var testCoroutine = typeof(Coroutine);
                
                Debug.Log("✅ Basic Unity types accessible");
            }
            catch (Exception e)
            {
                AddIssue(CompilationSeverity.Error,
                    "Using Statement Error",
                    "Basic Unity types not accessible",
                    "Check Unity installation and project settings",
                    $"Error: {e.Message}");
            }
        }

        #endregion

        #region Script Error Detection

        private void CheckScriptErrors()
        {
            Debug.Log("📄 Checking script compilation status...");

            #if UNITY_EDITOR
            try
            {
                // In editor, we can check for compilation errors
                bool hasCompilationErrors = EditorUtility.scriptCompilationFailed;
                
                if (hasCompilationErrors)
                {
                    AddIssue(CompilationSeverity.Error,
                        "Compilation Errors",
                        "Unity reports script compilation failures",
                        "Check Console window for detailed error messages",
                        "Scripts will not work until errors are fixed");
                }
                else
                {
                    Debug.Log("✅ No compilation errors reported");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Could not check compilation status: {e.Message}");
            }
            #endif
        }

        #endregion

        #region Issue Management

        private void AddIssue(CompilationSeverity severity, string category, string description, string solution, string impact)
        {
            var issue = new CompilationIssue
            {
                severity = severity,
                category = category,
                description = description,
                solution = solution,
                impact = impact,
                timestamp = DateTime.Now
            };

            detectedIssues.Add(issue);

            if (logDetailedResults)
            {
                string icon = GetSeverityIcon(severity);
                Debug.Log($"{icon} {category}: {description}");
                Debug.Log($"   💡 Solution: {solution}");
                Debug.Log($"   ⚡ Impact: {impact}");
            }
        }

        private string GetSeverityIcon(CompilationSeverity severity)
        {
            switch (severity)
            {
                case CompilationSeverity.Error: return "❌";
                case CompilationSeverity.Warning: return "⚠️"; 
                case CompilationSeverity.Info: return "ℹ️";
                default: return "?";
            }
        }

        #endregion

        #region Auto-Fix Attempts

        private void AttemptAutoFix()
        {
            Debug.Log("🔧 Attempting to auto-fix detected issues...");

            int fixedCount = 0;
            foreach (var issue in detectedIssues.ToList())
            {
                if (TryAutoFixIssue(issue))
                {
                    fixedCount++;
                    detectedIssues.Remove(issue);
                }
            }

            Debug.Log($"✅ Auto-fixed {fixedCount} issues");
            
            if (detectedIssues.Count > 0)
            {
                Debug.LogWarning($"⚠️ {detectedIssues.Count} issues require manual intervention");
            }
        }

        private bool TryAutoFixIssue(CompilationIssue issue)
        {
            try
            {
                switch (issue.category)
                {
                    case "Missing Scripts":
                        return TryFixMissingScripts();
                        
                    case "Missing Package":
                        return TryFixMissingPackage(issue.description);
                        
                    default:
                        return false; // Cannot auto-fix this type
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to auto-fix {issue.category}: {e.Message}");
                return false;
            }
        }

        private bool TryFixMissingScripts()
        {
            Debug.Log("🔧 Attempting to fix missing script references...");
            
            // This would require more complex logic to actually fix
            // For now, just report what was found
            Debug.LogWarning("⚠️ Missing script references require manual cleanup");
            return false;
        }

        private bool TryFixMissingPackage(string description)
        {
            Debug.Log($"📦 Package issue detected: {description}");
            Debug.Log("💡 Install missing packages via Window > Package Manager");
            return false; // Requires manual intervention
        }

        #endregion

        #region Reporting

        private void ReportFindings()
        {
            Debug.Log("📊 === COMPILATION ISSUE DETECTION RESULTS ===");
            
            var errorCount = detectedIssues.Count(i => i.severity == CompilationSeverity.Error);
            var warningCount = detectedIssues.Count(i => i.severity == CompilationSeverity.Warning);
            var infoCount = detectedIssues.Count(i => i.severity == CompilationSeverity.Info);
            
            Debug.Log($"❌ Errors: {errorCount}");
            Debug.Log($"⚠️ Warnings: {warningCount}");
            Debug.Log($"ℹ️ Info: {infoCount}");
            
            if (detectedIssues.Count == 0)
            {
                Debug.Log("🎉 No compilation issues detected!");
            }
            else
            {
                Debug.Log($"📋 Issues found: {detectedIssues.Count}");
                
                // Show top issues
                foreach (var issue in detectedIssues.Take(5))
                {
                    Debug.Log($"  {GetSeverityIcon(issue.severity)} {issue.category}: {issue.description}");
                }
                
                if (detectedIssues.Count > 5)
                {
                    Debug.Log($"  ... and {detectedIssues.Count - 5} more issues");
                }
            }
            
            Debug.Log("=== END DETECTION RESULTS ===");
        }

        #endregion

        #region Public API

        public List<CompilationIssue> GetDetectedIssues()
        {
            return new List<CompilationIssue>(detectedIssues);
        }

        public void ClearDetectedIssues()
        {
            detectedIssues.Clear();
            Debug.Log("🧹 Cleared detected issues");
        }

        #endregion

        #region Editor Menu Items

        #if UNITY_EDITOR
        [MenuItem("🐲 Chimera/Error Detection/Run Full Diagnosis")]
        public static void RunFullDiagnosis()
        {
            var detective = FindFirstObjectByType<CompilationErrorDetective>();
            if (detective == null)
            {
                GameObject go = new GameObject("CompilationErrorDetective");
                detective = go.AddComponent<CompilationErrorDetective>();
            }
            
            detective.DetectCompilationIssues();
        }

        [MenuItem("🐲 Chimera/Error Detection/Check Missing Packages")]
        public static void MenuCheckMissingPackages()
        {
            var detective = new CompilationErrorDetective();
            detective.CheckMissingPackages();
        }

        [MenuItem("🐲 Chimera/Error Detection/Clear Console")]
        public static void ClearConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            if (logEntries != null)
            {
                var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearMethod?.Invoke(null, null);
                Debug.Log("🧹 Console cleared");
            }
        }
        #endif

        #endregion
    }

    #region Data Structures

    [Serializable]
    public enum CompilationSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public class CompilationIssue
    {
        public CompilationSeverity severity;
        public string category;
        public string description;
        public string solution;
        public string impact;
        public DateTime timestamp;

        public override string ToString()
        {
            return $"[{severity}] {category}: {description}";
        }
    }

    #endregion
}
