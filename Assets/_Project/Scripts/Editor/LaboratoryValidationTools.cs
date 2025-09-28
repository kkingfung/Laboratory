#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Laboratory.Core.DI;

namespace Laboratory.Editor
{
    /// <summary>
    /// Collection of validation and diagnostic tools for the Laboratory architecture.
    /// Replaces the old migration utilities with consolidated functionality.
    /// </summary>
    public static class LaboratoryValidationTools
    {
        private const string MenuRoot = "🧪 Laboratory/Validation/";

        #region Validation Menu Items

        /// <summary>
        /// Comprehensive validation of the entire Laboratory system architecture.
        /// </summary>
        [MenuItem(MenuRoot + "Complete System Validation")]
        public static void ValidateCompleteSystem()
        {
            Debug.Log("=== LABORATORY SYSTEM VALIDATION ===");
            
            var issues = new List<string>();
            bool allPassed = true;

            // Test GlobalServiceProvider
            if (!ValidateServiceProvider(issues))
                allPassed = false;

            // Test core services
            if (!ValidateCoreServices(issues))
                allPassed = false;

            // Test event system
            if (!ValidateEventSystem(issues))
                allPassed = false;

            // Check for deprecated components
            if (!ValidateDeprecatedComponents(issues))
                allPassed = false;

            // Check timer system
            if (!ValidateTimerSystem(issues))
                allPassed = false;

            // Report results
            ReportValidationResults(allPassed, issues);
        }

        /// <summary>
        /// Quick health check of critical systems.
        /// </summary>
        [MenuItem(MenuRoot + "Quick Health Check")]
        public static void QuickHealthCheck()
        {
            Debug.Log("=== QUICK SYSTEM HEALTH CHECK ===");
            
            bool healthy = true;
            
            // Check GlobalServiceProvider
            if (!GlobalServiceProvider.IsInitialized)
            {
                Debug.LogError("❌ GlobalServiceProvider not initialized");
                healthy = false;
            }
            else
            {
                Debug.Log("✅ GlobalServiceProvider initialized");
            }

            // Check for deprecated components in current scene
            var deprecatedFound = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Count(obj => obj.GetType().GetCustomAttribute<System.ObsoleteAttribute>() != null);

            if (deprecatedFound > 0)
            {
                Debug.LogWarning($"⚠️ Found {deprecatedFound} deprecated components");
                healthy = false;
            }
            else
            {
                Debug.Log("✅ No deprecated components in current scene");
            }

            // Test event system quickly
            if (GlobalServiceProvider.IsInitialized)
            {
                if (!GlobalServiceProvider.TestEventSystem())
                {
                    healthy = false;
                }
            }

            if (healthy)
            {
                Debug.Log("🎉 QUICK HEALTH CHECK PASSED!");
            }
            else
            {
                Debug.LogWarning("⚠️ Issues found - run complete validation for details");
            }
        }

        /// <summary>
        /// Show diagnostic information about the current system state.
        /// </summary>
        [MenuItem(MenuRoot + "Show System Diagnostics")]
        public static void ShowSystemDiagnostics()
        {
            Debug.Log("=== LABORATORY SYSTEM DIAGNOSTICS ===");
            
            // Service Provider diagnostics
            Debug.Log(GlobalServiceProvider.GetDiagnosticInfo());
            
            // Scene analysis
            var monoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            Debug.Log($"Scene Analysis:");
            Debug.Log($"- Total MonoBehaviours: {monoBehaviours.Length}");
            Debug.Log($"- Health Components: {monoBehaviours.Count(mb => mb.GetType().Name.Contains("Health"))}");
            Debug.Log($"- UI Components: {monoBehaviours.Count(mb => mb.GetType().Namespace?.Contains("UI") == true)}");
            
            // Timer system diagnostics
            if (Laboratory.Core.Timing.TimerService.Instance != null)
            {
                Debug.Log($"Timer System:");
                Debug.Log($"- Active Timers: {Laboratory.Core.Timing.TimerService.Instance.GetActiveTimerCount()}");
            }
            else
            {
                Debug.Log("Timer System: TimerService not found in scene");
            }
        }

        #endregion

        #region Validation Implementation

        private static bool ValidateServiceProvider(List<string> issues)
        {
            Debug.Log("Validating GlobalServiceProvider...");
            
            if (!GlobalServiceProvider.IsInitialized)
            {
                issues.Add("CRITICAL: GlobalServiceProvider is not initialized");
                return false;
            }

            Debug.Log("✅ GlobalServiceProvider is properly initialized");
            return true;
        }

        private static bool ValidateCoreServices(List<string> issues)
        {
            Debug.Log("Validating core services...");
            
            if (!GlobalServiceProvider.IsInitialized)
            {
                issues.Add("Cannot validate services: GlobalServiceProvider not initialized");
                return false;
            }

            return GlobalServiceProvider.ValidateCoreServices();
        }

        private static bool ValidateEventSystem(List<string> issues)
        {
            Debug.Log("Validating event system...");
            
            if (!GlobalServiceProvider.IsInitialized)
            {
                issues.Add("Cannot validate event system: GlobalServiceProvider not initialized");
                return false;
            }

            return GlobalServiceProvider.TestEventSystem();
        }

        private static bool ValidateDeprecatedComponents(List<string> issues)
        {
            Debug.Log("Checking for deprecated components...");
            
            var sceneObjects = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int deprecatedFound = 0;
            
            foreach (var obj in sceneObjects)
            {
                var type = obj.GetType();
                var obsoleteAttribute = type.GetCustomAttribute<System.ObsoleteAttribute>();
                
                if (obsoleteAttribute != null)
                {
                    issues.Add($"DEPRECATED COMPONENT: {obj.name} has obsolete component {type.Name}");
                    deprecatedFound++;
                }
            }

            if (deprecatedFound == 0)
            {
                Debug.Log("✅ No deprecated components found in scene");
                return true;
            }
            else
            {
                Debug.LogWarning($"⚠️ Found {deprecatedFound} deprecated components");
                return false;
            }
        }

        private static bool ValidateTimerSystem(List<string> issues)
        {
            Debug.Log("Validating timer system...");
            
            var timerService = Laboratory.Core.Timing.TimerService.Instance;
            if (timerService == null)
            {
                issues.Add("TIMER SYSTEM: TimerService not found in scene. Add TimerService component to ensure proper timer management.");
                return false;
            }

            Debug.Log($"✅ TimerService is active with {timerService.GetActiveTimerCount()} active timers");
            return true;
        }

        private static void ReportValidationResults(bool allPassed, List<string> issues)
        {
            Debug.Log("=== VALIDATION RESULTS ===");
            
            if (allPassed)
            {
                Debug.Log("🎉 ALL VALIDATION CHECKS PASSED!");
                Debug.Log("Your Laboratory architecture is healthy and properly configured.");
                
                EditorUtility.DisplayDialog("System Validation", 
                    "✅ All validation checks passed!\n\nYour Laboratory architecture is healthy and properly configured.", 
                    "Excellent!");
            }
            else
            {
                Debug.LogWarning($"⚠️ VALIDATION FOUND {issues.Count} ISSUES:");
                
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"• {issue}");
                }
                
                var issueText = string.Join("\n• ", issues);
                EditorUtility.DisplayDialog("System Validation Issues", 
                    $"❌ Found {issues.Count} issues:\n\n• {issueText}\n\nSee Console for details. Fix these issues for optimal system health.", 
                    "Fix Issues");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Opens the Laboratory architecture documentation.
        /// </summary>
        [MenuItem(MenuRoot + "Open Documentation")]
        public static void OpenDocumentation()
        {
            var architectureSummaryPath = Application.dataPath + "/_Project/Scripts/ARCHITECTURE_SUMMARY.md";
            if (System.IO.File.Exists(architectureSummaryPath))
            {
                Application.OpenURL("file://" + architectureSummaryPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation Not Found", 
                    "ARCHITECTURE_SUMMARY.md not found at expected location.", 
                    "OK");
            }
        }

        /// <summary>
        /// Scans for TODO items in scripts.
        /// </summary>
        [MenuItem(MenuRoot + "Scan for TODOs")]
        public static void ScanForTodos()
        {
            var todoItems = new List<string>();
            var scriptPaths = AssetDatabase.FindAssets("t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/_Project/Scripts/"));

            foreach (var scriptPath in scriptPaths)
            {
                var content = System.IO.File.ReadAllText(scriptPath);
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Contains("TODO", StringComparison.OrdinalIgnoreCase))
                    {
                        todoItems.Add($"{scriptPath}:{i + 1} - {line.Trim()}");
                    }
                }
            }

            Debug.Log($"=== TODO SCAN RESULTS ({todoItems.Count} items found) ===");
            foreach (var todo in todoItems)
            {
                Debug.Log($"📝 {todo}");
            }

            if (todoItems.Count == 0)
            {
                Debug.Log("🎉 No TODO items found in project scripts!");
            }
        }

        #endregion
    }
}
#endif