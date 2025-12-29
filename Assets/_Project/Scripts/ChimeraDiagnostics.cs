using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Core.Diagnostics
{
    /// <summary>
    /// Project Chimeraの統合診断システム
    /// コンパイルエラー検出、緊急修復、シーン検証を一元管理
    /// </summary>
    public class ChimeraDiagnostics : MonoBehaviour
    {
        [Header("Diagnostics Settings")]
        [SerializeField] private bool autoRunOnStart = false;
        [SerializeField] private bool logDetailedResults = true;
        [SerializeField] private bool autoFixIssues = false;

        [Header("Detection Options")]
        [SerializeField] private bool checkMissingPackages = true;
        [SerializeField] private bool checkMissingScripts = true;
        [SerializeField] private bool checkSceneSetup = true;
        [SerializeField] private bool checkBackupFiles = true;

        private List<DiagnosticIssue> detectedIssues = new List<DiagnosticIssue>();
        private List<string> fixedIssues = new List<string>();

        // Events
        public static event Action<List<DiagnosticIssue>> OnDiagnosticsComplete;
        public static event Action<DiagnosticIssue> OnIssueDetected;
        public static event Action<string> OnIssueFix;

        #region Unity Lifecycle

        void Start()
        {
            if (autoRunOnStart)
            {
                RunFullDiagnostics();
            }
        }

        #endregion

        #region Main Diagnostics

        [ContextMenu("Run Full Diagnostics")]
        public void RunFullDiagnostics()
        {
            StartCoroutine(DiagnosticsRoutine());
        }

        private IEnumerator DiagnosticsRoutine()
        {
            Debug.Log("[ChimeraDiagnostics] Starting comprehensive diagnostics...");

            detectedIssues.Clear();
            fixedIssues.Clear();

            yield return new WaitForSeconds(0.1f);

            // Package checks
            if (checkMissingPackages)
            {
                CheckPackages();
            }
            yield return new WaitForSeconds(0.1f);

            // Missing script checks
            if (checkMissingScripts)
            {
                CheckMissingScripts();
            }
            yield return new WaitForSeconds(0.1f);

            // Scene setup checks
            if (checkSceneSetup)
            {
                CheckSceneSetup();
            }
            yield return new WaitForSeconds(0.1f);

            // Backup file checks
            if (checkBackupFiles)
            {
                CheckBackupFiles();
            }
            yield return new WaitForSeconds(0.1f);

            // Report results
            ReportResults();

            // Auto-fix if enabled
            if (autoFixIssues)
            {
                AttemptAutoFix();
            }

            OnDiagnosticsComplete?.Invoke(new List<DiagnosticIssue>(detectedIssues));
        }

        #endregion

        #region Package Detection

        private void CheckPackages()
        {
            Debug.Log("[ChimeraDiagnostics] Checking packages...");

            // Unity Entities
            #if !UNITY_ENTITIES
            AddIssue(DiagnosticSeverity.Warning,
                "Missing Package",
                "Unity Entities package not installed",
                "Install via Package Manager > Unity Registry > Entities",
                false);
            #endif

            // Unity Netcode
            #if !UNITY_NETCODE_GAMEOBJECTS
            AddIssue(DiagnosticSeverity.Warning,
                "Missing Package",
                "Unity Netcode for GameObjects not installed",
                "Install via Package Manager > Unity Registry > Netcode for GameObjects",
                false);
            #endif

            // Unity Mathematics
            try
            {
                var mathType = Type.GetType("Unity.Mathematics.math, Unity.Mathematics");
                if (mathType == null)
                {
                    AddIssue(DiagnosticSeverity.Warning,
                        "Missing Package",
                        "Unity Mathematics package not installed",
                        "Install via Package Manager > Unity Registry > Mathematics",
                        false);
                }
            }
            catch
            {
                // Package check failed, not critical
            }
        }

        #endregion

        #region Missing Script Detection

        private void CheckMissingScripts()
        {
            Debug.Log("[ChimeraDiagnostics] Checking for missing scripts...");

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
                AddIssue(DiagnosticSeverity.Error,
                    "Missing Scripts",
                    $"{missingScripts} missing script references found in scene",
                    "Remove or fix broken script references on GameObjects",
                    false);
            }

            // Check for core types
            CheckCoreTypes();
        }

        private void CheckCoreTypes()
        {
            var chimeraManagerType = FindTypeByName("ChimeraManager");
            var networkManagerType = FindTypeByName("ChimeraNetworkManager");

            if (chimeraManagerType == null)
            {
                AddIssue(DiagnosticSeverity.Error,
                    "Missing Type",
                    "ChimeraManager type not found",
                    "Check ChimeraManager.cs for compilation errors",
                    false);
            }

            if (networkManagerType == null)
            {
                AddIssue(DiagnosticSeverity.Warning,
                    "Missing Type",
                    "ChimeraNetworkManager type not found",
                    "Check ChimeraNetworkManager.cs for compilation errors",
                    false);
            }
        }

        private Type FindTypeByName(string typeName)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes().Where(t => t.Name.Contains(typeName));
                        if (types.Any())
                        {
                            return types.First();
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Scene Setup Checks

        private void CheckSceneSetup()
        {
            Debug.Log("[ChimeraDiagnostics] Checking scene setup...");

            // Camera check
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length == 0)
            {
                AddIssue(DiagnosticSeverity.Error,
                    "No Camera",
                    "No cameras found in scene",
                    "Add a Camera to the scene",
                    true);
            }
            else if (Camera.main == null)
            {
                AddIssue(DiagnosticSeverity.Warning,
                    "No Main Camera",
                    "No camera tagged as MainCamera",
                    "Tag a camera as MainCamera",
                    true);
            }

            // AudioListener check
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length == 0)
            {
                AddIssue(DiagnosticSeverity.Warning,
                    "No AudioListener",
                    "No AudioListener found in scene",
                    "Add AudioListener to main camera",
                    true);
            }
            else if (listeners.Length > 1)
            {
                AddIssue(DiagnosticSeverity.Error,
                    "Multiple AudioListeners",
                    $"{listeners.Length} AudioListeners found",
                    "Remove extra AudioListeners",
                    true);
            }

            // EventSystem check
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                AddIssue(DiagnosticSeverity.Warning,
                    "No EventSystem",
                    "No EventSystem found - UI interactions won't work",
                    "Add EventSystem to scene",
                    true);
            }

            // Light check
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (lights.Length == 0 && RenderSettings.ambientLight == Color.black)
            {
                AddIssue(DiagnosticSeverity.Warning,
                    "No Lighting",
                    "No lights and no ambient lighting",
                    "Add a Directional Light or configure ambient lighting",
                    true);
            }
        }

        #endregion

        #region Backup File Detection

        private void CheckBackupFiles()
        {
            Debug.Log("[ChimeraDiagnostics] Checking for backup files...");

            #if UNITY_EDITOR
            string assetsPath = Application.dataPath;
            string[] backupPatterns = { "*.bak", "*.meta.bak" };

            int totalFound = 0;
            foreach (string pattern in backupPatterns)
            {
                try
                {
                    string[] files = Directory.GetFiles(assetsPath, pattern, SearchOption.AllDirectories);
                    totalFound += files.Length;

                    foreach (string file in files)
                    {
                        string relativePath = file.Replace(assetsPath, "Assets").Replace("\\", "/");
                        if (logDetailedResults)
                        {
                            Debug.LogWarning($"[ChimeraDiagnostics] Backup file found: {relativePath}");
                        }
                    }
                }
                catch
                {
                    // Directory access issue, skip
                }
            }

            if (totalFound > 0)
            {
                AddIssue(DiagnosticSeverity.Error,
                    "Backup Files",
                    $"{totalFound} backup (.bak) files found - may cause duplicate definition errors",
                    "Delete all .bak files from the project",
                    false);
            }
            #endif
        }

        #endregion

        #region Issue Management

        private void AddIssue(DiagnosticSeverity severity, string category, string description, string solution, bool canAutoFix)
        {
            var issue = new DiagnosticIssue
            {
                severity = severity,
                category = category,
                description = description,
                solution = solution,
                canAutoFix = canAutoFix,
                timestamp = DateTime.Now
            };

            detectedIssues.Add(issue);
            OnIssueDetected?.Invoke(issue);

            if (logDetailedResults)
            {
                string prefix = severity == DiagnosticSeverity.Error ? "[ERROR]" :
                               severity == DiagnosticSeverity.Warning ? "[WARNING]" : "[INFO]";
                Debug.Log($"{prefix} {category}: {description}");
                Debug.Log($"  Solution: {solution}");
            }
        }

        #endregion

        #region Auto-Fix

        private void AttemptAutoFix()
        {
            Debug.Log("[ChimeraDiagnostics] Attempting auto-fix...");

            int fixedCount = 0;
            foreach (var issue in detectedIssues.Where(i => i.canAutoFix).ToList())
            {
                if (TryAutoFix(issue))
                {
                    fixedCount++;
                    fixedIssues.Add(issue.description);
                    detectedIssues.Remove(issue);
                    OnIssueFix?.Invoke(issue.description);
                }
            }

            Debug.Log($"[ChimeraDiagnostics] Auto-fixed {fixedCount} issues");
        }

        private bool TryAutoFix(DiagnosticIssue issue)
        {
            try
            {
                switch (issue.category)
                {
                    case "No Camera":
                        CreateMainCamera();
                        return true;

                    case "No Main Camera":
                        FixMainCameraTag();
                        return true;

                    case "No AudioListener":
                        CreateAudioListener();
                        return true;

                    case "Multiple AudioListeners":
                        FixMultipleAudioListeners();
                        return true;

                    case "No EventSystem":
                        CreateEventSystem();
                        return true;

                    case "No Lighting":
                        CreateBasicLighting();
                        return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChimeraDiagnostics] Auto-fix failed for {issue.category}: {e.Message}");
            }

            return false;
        }

        private void CreateMainCamera()
        {
            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();
            cameraGO.transform.position = new Vector3(0, 1, -10);
            Debug.Log("[ChimeraDiagnostics] Created Main Camera");
        }

        private void FixMainCameraTag()
        {
            var camera = FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.tag = "MainCamera";
                Debug.Log("[ChimeraDiagnostics] Fixed Main Camera tag");
            }
        }

        private void CreateAudioListener()
        {
            var camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
                Debug.Log("[ChimeraDiagnostics] Added AudioListener to camera");
            }
        }

        private void FixMultipleAudioListeners()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 1; i < listeners.Length; i++)
            {
                DestroyImmediate(listeners[i]);
            }
            Debug.Log($"[ChimeraDiagnostics] Removed {listeners.Length - 1} extra AudioListeners");
        }

        private void CreateEventSystem()
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[ChimeraDiagnostics] Created EventSystem");
        }

        private void CreateBasicLighting()
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            Debug.Log("[ChimeraDiagnostics] Created Directional Light");
        }

        #endregion

        #region Reporting

        private void ReportResults()
        {
            Debug.Log("=== CHIMERA DIAGNOSTICS RESULTS ===");

            var errorCount = detectedIssues.Count(i => i.severity == DiagnosticSeverity.Error);
            var warningCount = detectedIssues.Count(i => i.severity == DiagnosticSeverity.Warning);
            var infoCount = detectedIssues.Count(i => i.severity == DiagnosticSeverity.Info);

            Debug.Log($"Errors: {errorCount}");
            Debug.Log($"Warnings: {warningCount}");
            Debug.Log($"Info: {infoCount}");

            if (detectedIssues.Count == 0)
            {
                Debug.Log("No issues detected!");
            }
            else
            {
                Debug.Log($"Total issues: {detectedIssues.Count}");

                var fixableCount = detectedIssues.Count(i => i.canAutoFix);
                if (fixableCount > 0)
                {
                    Debug.Log($"{fixableCount} issues can be auto-fixed");
                }
            }

            Debug.Log("=== END DIAGNOSTICS ===");
        }

        #endregion

        #region Public API

        public List<DiagnosticIssue> GetDetectedIssues()
        {
            return new List<DiagnosticIssue>(detectedIssues);
        }

        public void ClearIssues()
        {
            detectedIssues.Clear();
            fixedIssues.Clear();
            Debug.Log("[ChimeraDiagnostics] Cleared all issues");
        }

        public void ForceAutoFix()
        {
            AttemptAutoFix();
        }

        #endregion

        #region Editor Menu Items

        #if UNITY_EDITOR
        [MenuItem("Laboratory/Project Chimera/Diagnostics/Run Full Diagnostics")]
        public static void MenuRunFullDiagnostics()
        {
            var diagnostics = FindFirstObjectByType<ChimeraDiagnostics>();
            if (diagnostics == null)
            {
                GameObject go = new GameObject("ChimeraDiagnostics");
                diagnostics = go.AddComponent<ChimeraDiagnostics>();
            }

            diagnostics.RunFullDiagnostics();
        }

        [MenuItem("Laboratory/Project Chimera/Diagnostics/Check Missing Packages")]
        public static void MenuCheckPackages()
        {
            var diagnostics = new ChimeraDiagnostics();
            diagnostics.CheckPackages();
        }

        [MenuItem("Laboratory/Project Chimera/Diagnostics/Check Scene Setup")]
        public static void MenuCheckSceneSetup()
        {
            var diagnostics = new ChimeraDiagnostics();
            diagnostics.CheckSceneSetup();
        }

        [MenuItem("Laboratory/Project Chimera/Diagnostics/List Backup Files")]
        public static void MenuListBackupFiles()
        {
            Debug.Log("[ChimeraDiagnostics] Searching for .bak files...");

            string assetsPath = Application.dataPath;
            string[] backupPatterns = { "*.bak", "*.meta.bak" };

            int totalFound = 0;
            foreach (string pattern in backupPatterns)
            {
                try
                {
                    string[] files = Directory.GetFiles(assetsPath, pattern, SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        string relativePath = file.Replace(assetsPath, "Assets").Replace("\\", "/");
                        Debug.LogWarning($"Backup file: {relativePath}");
                        totalFound++;
                    }
                }
                catch
                {
                    // Skip inaccessible directories
                }
            }

            if (totalFound == 0)
            {
                Debug.Log("[ChimeraDiagnostics] No backup files found");
            }
            else
            {
                Debug.LogError($"[ChimeraDiagnostics] Found {totalFound} backup files that should be deleted!");
            }
        }

        [MenuItem("Laboratory/Project Chimera/Diagnostics/Auto-Fix Scene Issues")]
        public static void MenuAutoFixScene()
        {
            var diagnostics = FindFirstObjectByType<ChimeraDiagnostics>();
            if (diagnostics == null)
            {
                GameObject go = new GameObject("ChimeraDiagnostics");
                diagnostics = go.AddComponent<ChimeraDiagnostics>();
            }

            diagnostics.autoFixIssues = true;
            diagnostics.RunFullDiagnostics();
        }
        #endif

        #endregion
    }

    #region Data Structures

    [Serializable]
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public class DiagnosticIssue
    {
        public DiagnosticSeverity severity;
        public string category;
        public string description;
        public string solution;
        public bool canAutoFix;
        public DateTime timestamp;

        public override string ToString()
        {
            return $"[{severity}] {category}: {description}";
        }
    }

    #endregion
}
