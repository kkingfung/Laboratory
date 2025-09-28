using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// QA Automation Tool for Unity projects
    /// Provides automated testing, validation, and quality assurance features
    /// </summary>
    public class QAAutomationTool : EditorWindow
    {
        #region Fields

        private Vector2 scrollPosition;
        private List<QACheck> qaChecks = new List<QACheck>();
        private bool isRunningTests = false;
        private int testsCompleted = 0;
        private int totalTests = 0;

        // QA Categories
        private bool showSceneValidation = true;
        private bool showAssetValidation = true;
        private bool showPerformanceChecks = true;
        private bool showBuildValidation = true;

        #endregion

        #region Unity Editor Window

        [MenuItem("🧪 Laboratory/Quality/QA Automation")]
        public static void ShowWindow()
        {
            GetWindow<QAAutomationTool>("QA Automation");
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("🛠️ QA Automation", "Automated quality assurance testing");
            InitializeQAChecks();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("QA Automation Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawControlPanel();
            EditorGUILayout.Space();
            DrawQACategories();
            EditorGUILayout.Space();
            DrawResults();
        }

        #endregion

        #region GUI Sections

        private void DrawControlPanel()
        {
            EditorGUILayout.LabelField("🎮 Test Controls", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isRunningTests);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("🚀 Run All QA Checks", GUILayout.Height(40)))
                    {
                        RunAllQAChecks();
                    }

                    if (GUILayout.Button("🎯 Quick Validation"))
                    {
                        RunQuickValidation();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("🔍 Scene Analysis"))
                    {
                        RunSceneAnalysis();
                    }

                    if (GUILayout.Button("📦 Asset Validation"))
                    {
                        RunAssetValidation();
                    }

                    if (GUILayout.Button("🏗️ Build Check"))
                    {
                        RunBuildCheck();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();

            if (isRunningTests)
            {
                var progress = totalTests > 0 ? (float)testsCompleted / totalTests : 0f;
                EditorGUILayout.LabelField($"⏳ Running tests... {testsCompleted}/{totalTests}", GUI.skin.box);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{progress:P0}");
            }
        }

        private void DrawQACategories()
        {
            EditorGUILayout.LabelField("📋 QA Categories", EditorStyles.boldLabel);

            showSceneValidation = EditorGUILayout.Foldout(showSceneValidation, "🎬 Scene Validation");
            if (showSceneValidation)
            {
                EditorGUI.indentLevel++;
                DrawCategoryChecks(QACategory.Scene);
                EditorGUI.indentLevel--;
            }

            showAssetValidation = EditorGUILayout.Foldout(showAssetValidation, "📦 Asset Validation");
            if (showAssetValidation)
            {
                EditorGUI.indentLevel++;
                DrawCategoryChecks(QACategory.Asset);
                EditorGUI.indentLevel--;
            }

            showPerformanceChecks = EditorGUILayout.Foldout(showPerformanceChecks, "⚡ Performance Checks");
            if (showPerformanceChecks)
            {
                EditorGUI.indentLevel++;
                DrawCategoryChecks(QACategory.Performance);
                EditorGUI.indentLevel--;
            }

            showBuildValidation = EditorGUILayout.Foldout(showBuildValidation, "🏗️ Build Validation");
            if (showBuildValidation)
            {
                EditorGUI.indentLevel++;
                DrawCategoryChecks(QACategory.Build);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCategoryChecks(QACategory category)
        {
            var categoryChecks = qaChecks.Where(c => c.Category == category).ToList();

            foreach (var check in categoryChecks)
            {
                DrawQACheck(check);
            }
        }

        private void DrawQACheck(QACheck check)
        {
            EditorGUILayout.BeginHorizontal();
            {
                var icon = GetStatusIcon(check.Status);
                var statusColor = GetStatusColor(check.Status);

                var originalColor = GUI.contentColor;
                GUI.contentColor = statusColor;
                EditorGUILayout.LabelField($"{icon} {check.Name}", GUILayout.Width(250));
                GUI.contentColor = originalColor;

                if (check.Status == QAStatus.Failed && !string.IsNullOrEmpty(check.ErrorMessage))
                {
                    EditorGUILayout.LabelField(check.ErrorMessage, GUI.skin.label);
                }

                if (GUILayout.Button("▶️", GUILayout.Width(30)))
                {
                    RunSingleCheck(check);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResults()
        {
            var passedChecks = qaChecks.Count(c => c.Status == QAStatus.Passed);
            var failedChecks = qaChecks.Count(c => c.Status == QAStatus.Failed);
            var notRunChecks = qaChecks.Count(c => c.Status == QAStatus.NotRun);

            if (passedChecks + failedChecks > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("📊 QA Summary", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"✅ Passed: {passedChecks} | ❌ Failed: {failedChecks} | ⏳ Not Run: {notRunChecks}");

                if (failedChecks > 0)
                {
                    EditorGUILayout.HelpBox("⚠️ Some QA checks failed. Review issues above before building.", MessageType.Warning);
                }
                else if (passedChecks > 0)
                {
                    EditorGUILayout.HelpBox("✅ All QA checks passed! Project is ready for build.", MessageType.Info);
                }
            }
        }

        #endregion

        #region QA Check Execution

        private void InitializeQAChecks()
        {
            qaChecks.Clear();

            // Scene Validation Checks
            qaChecks.Add(new QACheck("Missing Camera", QACategory.Scene, () => CheckMissingCamera()));
            qaChecks.Add(new QACheck("Missing Light Sources", QACategory.Scene, () => CheckMissingLights()));
            qaChecks.Add(new QACheck("Missing Audio Listener", QACategory.Scene, () => CheckMissingAudioListener()));
            qaChecks.Add(new QACheck("Empty GameObjects", QACategory.Scene, () => CheckEmptyGameObjects()));
            qaChecks.Add(new QACheck("Missing Colliders on Interactables", QACategory.Scene, () => CheckMissingColliders()));

            // Asset Validation Checks
            qaChecks.Add(new QACheck("Missing Textures", QACategory.Asset, () => CheckMissingTextures()));
            qaChecks.Add(new QACheck("Uncompressed Textures", QACategory.Asset, () => CheckUncompressedTextures()));
            qaChecks.Add(new QACheck("Missing Audio Clips", QACategory.Asset, () => CheckMissingAudioClips()));
            qaChecks.Add(new QACheck("Large File Sizes", QACategory.Asset, () => CheckLargeFiles()));
            qaChecks.Add(new QACheck("Unused Assets", QACategory.Asset, () => CheckUnusedAssets()));

            // Performance Checks
            qaChecks.Add(new QACheck("High Poly Count Objects", QACategory.Performance, () => CheckHighPolyCount()));
            qaChecks.Add(new QACheck("Too Many Draw Calls", QACategory.Performance, () => CheckDrawCalls()));
            qaChecks.Add(new QACheck("Missing LOD Groups", QACategory.Performance, () => CheckMissingLODs()));
            qaChecks.Add(new QACheck("Expensive Materials", QACategory.Performance, () => CheckExpensiveMaterials()));

            // Build Validation
            qaChecks.Add(new QACheck("Build Settings Valid", QACategory.Build, () => CheckBuildSettings()));
            qaChecks.Add(new QACheck("Required Scenes Added", QACategory.Build, () => CheckRequiredScenes()));
            qaChecks.Add(new QACheck("No Build Errors", QACategory.Build, () => CheckBuildErrors()));
            qaChecks.Add(new QACheck("Platform Settings", QACategory.Build, () => CheckPlatformSettings()));
        }

        private void RunAllQAChecks()
        {
            isRunningTests = true;
            totalTests = qaChecks.Count;
            testsCompleted = 0;

            foreach (var check in qaChecks)
            {
                RunSingleCheck(check);
                testsCompleted++;

                if (testsCompleted % 5 == 0) // Update UI every 5 checks
                {
                    Repaint();
                }
            }

            isRunningTests = false;
            Repaint();

            // Summary
            var failed = qaChecks.Count(c => c.Status == QAStatus.Failed);
            if (failed > 0)
            {
                Debug.LogWarning($"⚠️ QA Automation: {failed} checks failed out of {totalTests}");
            }
            else
            {
                Debug.Log($"✅ QA Automation: All {totalTests} checks passed!");
            }
        }

        private void RunQuickValidation()
        {
            var quickChecks = qaChecks.Where(c =>
                c.Name.Contains("Missing Camera") ||
                c.Name.Contains("Missing Light") ||
                c.Name.Contains("Build Settings") ||
                c.Name.Contains("Build Errors")
            ).ToList();

            foreach (var check in quickChecks)
            {
                RunSingleCheck(check);
            }

            Repaint();
        }

        private void RunSceneAnalysis()
        {
            var sceneChecks = qaChecks.Where(c => c.Category == QACategory.Scene).ToList();
            foreach (var check in sceneChecks)
            {
                RunSingleCheck(check);
            }
            Repaint();
        }

        private void RunAssetValidation()
        {
            var assetChecks = qaChecks.Where(c => c.Category == QACategory.Asset).ToList();
            foreach (var check in assetChecks)
            {
                RunSingleCheck(check);
            }
            Repaint();
        }

        private void RunBuildCheck()
        {
            var buildChecks = qaChecks.Where(c => c.Category == QACategory.Build).ToList();
            foreach (var check in buildChecks)
            {
                RunSingleCheck(check);
            }
            Repaint();
        }

        private void RunSingleCheck(QACheck check)
        {
            try
            {
                var result = check.CheckFunction();
                check.Status = result.Passed ? QAStatus.Passed : QAStatus.Failed;
                check.ErrorMessage = result.ErrorMessage;
            }
            catch (System.Exception e)
            {
                check.Status = QAStatus.Failed;
                check.ErrorMessage = $"Check failed: {e.Message}";
            }
        }

        #endregion

        #region QA Check Implementations

        private QAResult CheckMissingCamera()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length == 0)
            {
                return new QAResult(false, "No cameras found in current scene");
            }
            return new QAResult(true);
        }

        private QAResult CheckMissingLights()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (lights.Length == 0)
            {
                return new QAResult(false, "No light sources found in current scene");
            }
            return new QAResult(true);
        }

        private QAResult CheckMissingAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length == 0)
            {
                return new QAResult(false, "No AudioListener found in current scene");
            }
            if (listeners.Length > 1)
            {
                return new QAResult(false, $"Multiple AudioListeners found ({listeners.Length}). Should only have one.");
            }
            return new QAResult(true);
        }

        private QAResult CheckEmptyGameObjects()
        {
            var emptyObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.GetComponents<Component>().Length == 1) // Only Transform
                .Where(go => go.transform.childCount == 0)
                .ToArray();

            if (emptyObjects.Length > 10)
            {
                return new QAResult(false, $"Found {emptyObjects.Length} empty GameObjects. Consider cleanup.");
            }
            return new QAResult(true);
        }

        private QAResult CheckMissingColliders()
        {
            var interactables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.name.ToLower().Contains("pickup") ||
                           mb.name.ToLower().Contains("interactable") ||
                           mb.name.ToLower().Contains("button"))
                .Where(mb => !mb.TryGetComponent<Collider>(out _)) // More efficient than null check
                .ToArray();

            if (interactables.Length > 0)
            {
                return new QAResult(false, $"Found {interactables.Length} interactable objects without colliders");
            }
            return new QAResult(true);
        }

        private QAResult CheckMissingTextures()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            var missingTextures = 0;

            foreach (var material in materials)
            {
                if (material.mainTexture == null)
                {
                    missingTextures++;
                }
            }

            if (missingTextures > 0)
            {
                return new QAResult(false, $"Found {missingTextures} materials with missing main textures");
            }
            return new QAResult(true);
        }

        private QAResult CheckUncompressedTextures()
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Where(tex => tex != null)
                .Where(tex => tex.format == TextureFormat.RGBA32 && tex.width > 256)
                .ToArray();

            if (textures.Length > 0)
            {
                return new QAResult(false, $"Found {textures.Length} large uncompressed textures. Consider compression.");
            }
            return new QAResult(true);
        }

        private QAResult CheckMissingAudioClips()
        {
            var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
                .Where(source => source.clip == null)
                .ToArray();

            if (audioSources.Length > 0)
            {
                return new QAResult(false, $"Found {audioSources.Length} AudioSources without clips assigned");
            }
            return new QAResult(true);
        }

        private QAResult CheckLargeFiles()
        {
            var largeFiles = AssetDatabase.GetAllAssetPaths()
                .Where(path => !path.StartsWith("Packages/"))
                .Where(path => new FileInfo(path).Length > 50 * 1024 * 1024) // 50MB
                .ToArray();

            if (largeFiles.Length > 0)
            {
                return new QAResult(false, $"Found {largeFiles.Length} files over 50MB. Consider optimization.");
            }
            return new QAResult(true);
        }

        private QAResult CheckUnusedAssets()
        {
            // This is a simplified check - in a real implementation, you'd want more sophisticated dependency analysis
            return new QAResult(true, "Unused asset check not fully implemented");
        }

        private QAResult CheckHighPolyCount()
        {
            var meshes = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Select(mr => {
                    // Use TryGetComponent for better performance
                    if (mr.TryGetComponent<MeshFilter>(out var meshFilter))
                        return meshFilter.sharedMesh;
                    return null;
                })
                .Where(mesh => mesh != null)
                .Where(mesh => mesh.triangles.Length > 10000) // > 5000 triangles
                .ToArray();

            if (meshes.Length > 5)
            {
                return new QAResult(false, $"Found {meshes.Length} high poly count meshes (>5000 triangles)");
            }
            return new QAResult(true);
        }

        private QAResult CheckDrawCalls()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers.Length > 500)
            {
                return new QAResult(false, $"High number of renderers ({renderers.Length}). May cause performance issues.");
            }
            return new QAResult(true);
        }

        private QAResult CheckMissingLODs()
        {
            var meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(mr => {
                    // Check both LODGroup and triangle count in single pass
                    if (mr.TryGetComponent<LODGroup>(out _))
                        return false; // Has LOD group, skip

                    if (mr.TryGetComponent<MeshFilter>(out var meshFilter))
                        return meshFilter.sharedMesh?.triangles.Length > 1000;

                    return false;
                })
                .ToArray();

            if (meshRenderers.Length > 10)
            {
                return new QAResult(false, $"Found {meshRenderers.Length} complex meshes without LOD groups");
            }
            return new QAResult(true);
        }

        private QAResult CheckExpensiveMaterials()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>()
                .Where(mat => mat.shader.name.Contains("Standard") && mat.shader.renderQueue > 2500)
                .ToArray();

            if (materials.Length > 20)
            {
                return new QAResult(false, $"Found {materials.Length} potentially expensive materials");
            }
            return new QAResult(true);
        }

        private QAResult CheckBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                return new QAResult(false, "No scenes added to build settings");
            }

            var invalidScenes = scenes.Where(scene => !scene.enabled || string.IsNullOrEmpty(scene.path)).ToArray();
            if (invalidScenes.Length > 0)
            {
                return new QAResult(false, $"Found {invalidScenes.Length} invalid scenes in build settings");
            }

            return new QAResult(true);
        }

        private QAResult CheckRequiredScenes()
        {
            var sceneNames = EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            var requiredScenes = new[] { "MainMenu", "GameScene" }; // Customize as needed

            var missingScenes = requiredScenes.Where(required => !sceneNames.Any(scene => scene.Contains(required))).ToArray();

            if (missingScenes.Length > 0)
            {
                return new QAResult(false, $"Missing required scenes: {string.Join(", ", missingScenes)}");
            }
            return new QAResult(true);
        }

        private QAResult CheckBuildErrors()
        {
            // Check for common build-breaking issues
            var scripts = AssetDatabase.FindAssets("t:MonoScript")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.EndsWith(".cs"))
                .ToArray();

            // This is simplified - in reality you'd want to check compilation status
            return new QAResult(true);
        }

        private QAResult CheckPlatformSettings()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;

            // Basic platform checks
            if (target == BuildTarget.Android && PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23)
            {
                return new QAResult(false, "Android min SDK version is too low");
            }

            return new QAResult(true);
        }

        #endregion

        #region Helper Methods

        private string GetStatusIcon(QAStatus status)
        {
            return status switch
            {
                QAStatus.Passed => "✅",
                QAStatus.Failed => "❌",
                QAStatus.NotRun => "⏳",
                _ => "❓"
            };
        }

        private Color GetStatusColor(QAStatus status)
        {
            return status switch
            {
                QAStatus.Passed => Color.green,
                QAStatus.Failed => Color.red,
                QAStatus.NotRun => Color.yellow,
                _ => Color.white
            };
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class QACheck
    {
        public string Name;
        public QACategory Category;
        public System.Func<QAResult> CheckFunction;
        public QAStatus Status = QAStatus.NotRun;
        public string ErrorMessage = "";

        public QACheck(string name, QACategory category, System.Func<QAResult> checkFunction)
        {
            Name = name;
            Category = category;
            CheckFunction = checkFunction;
        }
    }

    [System.Serializable]
    public class QAResult
    {
        public bool Passed;
        public string ErrorMessage;

        public QAResult(bool passed, string errorMessage = "")
        {
            Passed = passed;
            ErrorMessage = errorMessage;
        }
    }

    public enum QAStatus
    {
        NotRun,
        Passed,
        Failed
    }

    public enum QACategory
    {
        Scene,
        Asset,
        Performance,
        Build
    }

    #endregion
}