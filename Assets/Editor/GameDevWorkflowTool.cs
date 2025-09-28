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
    /// GameDev Workflow Tool for Unity projects
    /// Streamlines common development tasks and provides quick utilities
    /// </summary>
    public class GameDevWorkflowTool : EditorWindow
    {
        #region Fields

        private Vector2 scrollPosition;
        private string searchFilter = "";
        private ScriptTemplate selectedTemplate = ScriptTemplate.MonoBehaviour;

        // Quick Scene Management
        private string newSceneName = "NewScene";
        private bool autoAddToBuild = true;

        // Asset Organization
        private string folderName = "NewFolder";
        private DefaultAsset selectedFolder;

        // Debugging Tools
        private bool showDebugOptions = true;
        private bool showSceneTools = true;
        private bool showAssetTools = true;
        private bool showUtilities = true;

        #endregion

        #region Unity Editor Window

        [MenuItem("🧪 Laboratory/Workflow/GameDev Tools")]
        public static void ShowWindow()
        {
            GetWindow<GameDevWorkflowTool>("GameDev Workflow");
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("⚡ GameDev Workflow", "Development workflow utilities");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GameDev Workflow Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                DrawQuickActions();
                EditorGUILayout.Space();
                DrawSceneTools();
                EditorGUILayout.Space();
                DrawAssetTools();
                EditorGUILayout.Space();
                DrawDebugTools();
                EditorGUILayout.Space();
                DrawUtilities();
            }
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI Sections

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("🚀 Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("💾 Save All", GUILayout.Height(30)))
                {
                    SaveAll();
                }

                if (GUILayout.Button("🔄 Refresh Assets", GUILayout.Height(30)))
                {
                    AssetDatabase.Refresh();
                    Debug.Log("✅ Asset database refreshed");
                }

                if (GUILayout.Button("🧹 Clear Console", GUILayout.Height(30)))
                {
                    ClearConsole();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("▶️ Play from Scene 0"))
                {
                    PlayFromFirstScene();
                }

                if (GUILayout.Button("⏹️ Stop Play Mode"))
                {
                    EditorApplication.isPlaying = false;
                }

                if (GUILayout.Button("⏸️ Pause"))
                {
                    EditorApplication.isPaused = !EditorApplication.isPaused;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSceneTools()
        {
            showSceneTools = EditorGUILayout.Foldout(showSceneTools, "🎬 Scene Management");
            if (!showSceneTools) return;

            EditorGUI.indentLevel++;

            // Quick scene switcher
            EditorGUILayout.LabelField("Scene Switcher", EditorStyles.boldLabel);
            DrawSceneSwitcher();

            EditorGUILayout.Space();

            // New scene creation
            EditorGUILayout.LabelField("Create New Scene", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                newSceneName = EditorGUILayout.TextField("Scene Name:", newSceneName);
                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    CreateNewScene();
                }
            }
            EditorGUILayout.EndHorizontal();

            autoAddToBuild = EditorGUILayout.Toggle("Auto-add to Build Settings", autoAddToBuild);

            EditorGUILayout.Space();

            // Scene validation
            EditorGUILayout.LabelField("Scene Validation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("🔍 Analyze Current Scene"))
                {
                    AnalyzeCurrentScene();
                }

                if (GUILayout.Button("🛠️ Fix Common Issues"))
                {
                    FixCommonSceneIssues();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawAssetTools()
        {
            showAssetTools = EditorGUILayout.Foldout(showAssetTools, "📦 Asset Management");
            if (!showAssetTools) return;

            EditorGUI.indentLevel++;

            // Asset search
            EditorGUILayout.LabelField("Asset Search", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
                if (GUILayout.Button("🔍 Find", GUILayout.Width(50)))
                {
                    SearchAssets();
                }
                if (GUILayout.Button("🎯 Find Missing", GUILayout.Width(80)))
                {
                    FindMissingReferences();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Script generation
            EditorGUILayout.LabelField("Script Generation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                selectedTemplate = (ScriptTemplate)EditorGUILayout.EnumPopup("Template:", selectedTemplate);
                if (GUILayout.Button("📝 Generate", GUILayout.Width(70)))
                {
                    GenerateScript();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Folder management
            EditorGUILayout.LabelField("Folder Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                folderName = EditorGUILayout.TextField("Folder Name:", folderName);
                if (GUILayout.Button("📁 Create", GUILayout.Width(60)))
                {
                    CreateProjectFolder();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("🗂️ Organize Assets"))
                {
                    OrganizeAssets();
                }

                if (GUILayout.Button("🧹 Cleanup Unused"))
                {
                    CleanupUnusedAssets();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawDebugTools()
        {
            showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "🐛 Debug Tools");
            if (!showDebugOptions) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("🔍 Find All Scripts"))
                {
                    FindAllScriptsInScene();
                }

                if (GUILayout.Button("💊 Health Check"))
                {
                    PerformHealthCheck();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("📊 Performance Report"))
                {
                    GeneratePerformanceReport();
                }

                if (GUILayout.Button("🔧 Auto Fix Issues"))
                {
                    AutoFixCommonIssues();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("📝 Generate Debug Log"))
                {
                    GenerateDebugLog();
                }

                if (GUILayout.Button("🏷️ Tag All Untagged"))
                {
                    TagAllUntagged();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawUtilities()
        {
            showUtilities = EditorGUILayout.Foldout(showUtilities, "🔧 Utilities");
            if (!showUtilities) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("📸 Screenshot"))
                {
                    TakeScreenshot();
                }

                if (GUILayout.Button("📊 Stats Report"))
                {
                    GenerateStatsReport();
                }

                if (GUILayout.Button("🎯 Select All Cameras"))
                {
                    SelectAllCameras();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("💡 Optimize Lighting"))
                {
                    OptimizeLighting();
                }

                if (GUILayout.Button("🔊 Check Audio"))
                {
                    CheckAudioSources();
                }

                if (GUILayout.Button("🎨 Material Audit"))
                {
                    AuditMaterials();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawSceneSwitcher()
        {
            var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();

            if (buildScenes.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes in build settings", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                for (int i = 0; i < Mathf.Min(buildScenes.Length, 4); i++)
                {
                    var scene = buildScenes[i];
                    var sceneName = Path.GetFileNameWithoutExtension(scene.path);

                    if (GUILayout.Button($"{i}: {sceneName}"))
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(scene.path);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (buildScenes.Length > 4)
            {
                if (GUILayout.Button($"... and {buildScenes.Length - 4} more scenes"))
                {
                    ShowSceneSelectionMenu(buildScenes);
                }
            }
        }

        #endregion

        #region Tool Implementations

        private void SaveAll()
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("✅ All assets and scenes saved");
        }

        private void ClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
            Debug.Log("🧹 Console cleared");
        }

        private void PlayFromFirstScene()
        {
            if (EditorBuildSettings.scenes.Length > 0)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            }
            EditorApplication.isPlaying = true;
        }

        private void CreateNewScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            var scenePath = $"Assets/Scenes/{newSceneName}.unity";

            // Create Scenes folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(newScene, scenePath);

            if (autoAddToBuild)
            {
                AddSceneToBuildSettings(scenePath);
            }

            Debug.Log($"✅ Created new scene: {scenePath}");
        }

        private void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private void AnalyzeCurrentScene()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("🔍 Scene Analysis Report");
            report.AppendLine("========================");

            // Count objects
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            report.AppendLine($"Total GameObjects: {allObjects.Length}");

            // Count by type
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
            var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length;

            report.AppendLine($"Cameras: {cameras}");
            report.AppendLine($"Lights: {lights}");
            report.AppendLine($"Renderers: {renderers}");
            report.AppendLine($"Colliders: {colliders}");
            report.AppendLine($"AudioSources: {audioSources}");

            // Performance indicators
            var highPolyObjects = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Count(mr => {
                    // Use TryGetComponent and combine checks for better performance
                    if (mr.TryGetComponent<MeshFilter>(out var meshFilter))
                        return meshFilter.sharedMesh != null && meshFilter.sharedMesh.triangles.Length > 3000;
                    return false;
                });

            report.AppendLine($"High Poly Objects (>1000 tris): {highPolyObjects}");

            // Issues
            if (cameras == 0) report.AppendLine("⚠️ WARNING: No cameras in scene");
            if (lights == 0) report.AppendLine("⚠️ WARNING: No lights in scene");

            Debug.Log(report.ToString());
        }

        private void FixCommonSceneIssues()
        {
            int fixes = 0;

            // Ensure there's a camera
            if (FindObjectsByType<Camera>(FindObjectsSortMode.None).Length == 0)
            {
                var cameraGO = new GameObject("Main Camera");
                cameraGO.AddComponent<Camera>();
                cameraGO.AddComponent<AudioListener>();
                cameraGO.tag = "MainCamera";
                fixes++;
            }

            // Ensure there's lighting
            if (FindObjectsByType<Light>(FindObjectsSortMode.None).Length == 0)
            {
                var lightGO = new GameObject("Directional Light");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                fixes++;
            }

            // Check for multiple AudioListeners
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length > 1)
            {
                for (int i = 1; i < listeners.Length; i++)
                {
                    DestroyImmediate(listeners[i]);
                }
                fixes++;
            }

            Debug.Log($"✅ Fixed {fixes} common scene issues");
        }

        private void SearchAssets()
        {
            if (string.IsNullOrEmpty(searchFilter)) return;

            var assets = AssetDatabase.FindAssets(searchFilter);
            Debug.Log($"🔍 Found {assets.Length} assets matching '{searchFilter}':");

            foreach (var guid in assets.Take(20)) // Limit to first 20
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"  📄 {path}");
            }

            if (assets.Length > 20)
            {
                Debug.Log($"  ... and {assets.Length - 20} more");
            }
        }

        private void FindMissingReferences()
        {
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            int missingCount = 0;

            foreach (var obj in objects)
            {
                var components = obj.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        Debug.LogWarning($"❌ Missing script on: {GetGameObjectPath(obj)}", obj);
                        missingCount++;
                    }
                }
            }

            Debug.Log($"🔍 Found {missingCount} missing script references");
        }

        private void GenerateScript()
        {
            var template = GetScriptTemplate(selectedTemplate);
            var path = EditorUtility.SaveFilePanel("Save Script", "Assets/Scripts", "NewScript", "cs");

            if (!string.IsNullOrEmpty(path))
            {
                var relativePath = FileUtil.GetProjectRelativePath(path);
                File.WriteAllText(path, template);
                AssetDatabase.Refresh();
                Debug.Log($"✅ Created script: {relativePath}");
            }
        }

        private void CreateProjectFolder()
        {
            var path = EditorUtility.SaveFolderPanel("Create Folder", "Assets", folderName);
            if (!string.IsNullOrEmpty(path))
            {
                var relativePath = FileUtil.GetProjectRelativePath(path);
                AssetDatabase.CreateFolder(Path.GetDirectoryName(relativePath), Path.GetFileName(relativePath));
                AssetDatabase.Refresh();
                Debug.Log($"📁 Created folder: {relativePath}");
            }
        }

        private void OrganizeAssets()
        {
            // Create standard folder structure
            var folders = new[] { "Scripts", "Materials", "Textures", "Audio", "Prefabs", "Scenes", "Animations" };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder($"Assets/{folder}"))
                {
                    AssetDatabase.CreateFolder("Assets", folder);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("📂 Created standard folder structure");
        }

        private void CleanupUnusedAssets()
        {
            Debug.Log("🧹 Cleanup unused assets feature would require dependency analysis");
            Debug.Log("💡 Consider using Unity's Addressable Asset System for better asset management");
        }

        private void FindAllScriptsInScene()
        {
            var scripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var scriptTypes = scripts.Select(s => s.GetType().Name).Distinct().OrderBy(name => name);

            Debug.Log($"📜 Found {scripts.Length} script instances of {scriptTypes.Count()} different types:");
            foreach (var scriptType in scriptTypes)
            {
                var count = scripts.Count(s => s.GetType().Name == scriptType);
                Debug.Log($"  🔹 {scriptType}: {count} instances");
            }
        }

        private void PerformHealthCheck()
        {
            var issues = new List<string>();

            // Check cameras
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length == 0) issues.Add("No cameras in scene");

            // Check lights
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (lights.Length == 0) issues.Add("No lights in scene");

            // Check audio listeners
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length == 0) issues.Add("No AudioListener in scene");
            if (listeners.Length > 1) issues.Add($"Multiple AudioListeners ({listeners.Length})");

            if (issues.Count == 0)
            {
                Debug.Log("✅ Health check passed - no issues found");
            }
            else
            {
                Debug.LogWarning($"⚠️ Health check found {issues.Count} issues:");
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"  🔸 {issue}");
                }
            }
        }

        private void GeneratePerformanceReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("📊 Performance Report");
            report.AppendLine("===================");

            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var triangleCount = renderers.Sum(r => {
                // Use TryGetComponent for better performance
                if (r.TryGetComponent<MeshFilter>(out var meshFilter))
                    return meshFilter.sharedMesh?.triangles.Length / 3 ?? 0;
                return 0;
            });

            report.AppendLine($"Total Renderers: {renderers.Length}");
            report.AppendLine($"Total Triangles: {triangleCount:N0}");

            var materials = Resources.FindObjectsOfTypeAll<Material>().Length;
            var textures = Resources.FindObjectsOfTypeAll<Texture>().Length;

            report.AppendLine($"Materials: {materials}");
            report.AppendLine($"Textures: {textures}");

            // Performance recommendations
            if (renderers.Length > 500)
            {
                report.AppendLine("⚠️ High renderer count may impact performance");
            }

            if (triangleCount > 100000)
            {
                report.AppendLine("⚠️ High triangle count may impact performance");
            }

            Debug.Log(report.ToString());
        }

        private void AutoFixCommonIssues()
        {
            FixCommonSceneIssues();
            // Add more automated fixes here
        }

        private void GenerateDebugLog()
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logPath = $"Assets/DebugLog_{timestamp}.txt";

            var log = new System.Text.StringBuilder();
            log.AppendLine($"Debug Log Generated: {System.DateTime.Now}");
            log.AppendLine($"Unity Version: {Application.unityVersion}");
            log.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
            log.AppendLine($"Platform: {Application.platform}");

            File.WriteAllText(logPath, log.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"📝 Debug log saved to: {logPath}");
        }

        private void TagAllUntagged()
        {
            var untaggedObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.tag == "Untagged")
                .ToArray();

            foreach (var obj in untaggedObjects)
            {
                obj.tag = "Default"; // or whatever default tag you prefer
            }

            Debug.Log($"🏷️ Tagged {untaggedObjects.Length} previously untagged objects");
        }

        private void TakeScreenshot()
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var path = $"Screenshots/Screenshot_{timestamp}.png";

            Directory.CreateDirectory("Screenshots");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"📸 Screenshot saved: {path}");
        }

        private void GenerateStatsReport()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("📊 Project Statistics");
            stats.AppendLine("===================");

            var scriptFiles = AssetDatabase.FindAssets("t:MonoScript").Length;
            var scenes = AssetDatabase.FindAssets("t:Scene").Length;
            var materials = AssetDatabase.FindAssets("t:Material").Length;
            var textures = AssetDatabase.FindAssets("t:Texture2D").Length;
            var prefabs = AssetDatabase.FindAssets("t:Prefab").Length;

            stats.AppendLine($"Scripts: {scriptFiles}");
            stats.AppendLine($"Scenes: {scenes}");
            stats.AppendLine($"Materials: {materials}");
            stats.AppendLine($"Textures: {textures}");
            stats.AppendLine($"Prefabs: {prefabs}");

            Debug.Log(stats.ToString());
        }

        private void SelectAllCameras()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Selection.objects = cameras.Select(c => c.gameObject).ToArray();
            Debug.Log($"🎯 Selected {cameras.Length} cameras");
        }

        private void OptimizeLighting()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int optimized = 0;

            foreach (var light in lights)
            {
                if (light.shadows == LightShadows.Hard && light.intensity > 1f)
                {
                    light.shadows = LightShadows.Soft;
                    optimized++;
                }
            }

            Debug.Log($"💡 Optimized {optimized} light sources");
        }

        private void CheckAudioSources()
        {
            var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            var issues = 0;

            foreach (var source in audioSources)
            {
                if (source.clip == null)
                {
                    Debug.LogWarning($"🔊 AudioSource without clip: {GetGameObjectPath(source.gameObject)}", source);
                    issues++;
                }
            }

            Debug.Log($"🔊 Checked {audioSources.Length} audio sources, found {issues} issues");
        }

        private void AuditMaterials()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            var issues = 0;

            foreach (var material in materials)
            {
                if (material.mainTexture == null)
                {
                    Debug.LogWarning($"🎨 Material without main texture: {material.name}");
                    issues++;
                }
            }

            Debug.Log($"🎨 Audited {materials.Length} materials, found {issues} issues");
        }

        #endregion

        #region Helper Methods

        private void ShowSceneSelectionMenu(EditorBuildSettingsScene[] scenes)
        {
            var menu = new GenericMenu();

            foreach (var scene in scenes)
            {
                var sceneName = Path.GetFileNameWithoutExtension(scene.path);
                menu.AddItem(new GUIContent(sceneName), false, () =>
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.path);
                    }
                });
            }

            menu.ShowAsContext();
        }

        private string GetGameObjectPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private string GetScriptTemplate(ScriptTemplate template)
        {
            return template switch
            {
                ScriptTemplate.MonoBehaviour => GetMonoBehaviourTemplate(),
                ScriptTemplate.ScriptableObject => GetScriptableObjectTemplate(),
                ScriptTemplate.Editor => GetEditorTemplate(),
                ScriptTemplate.Interface => GetInterfaceTemplate(),
                _ => GetMonoBehaviourTemplate()
            };
        }

        private string GetMonoBehaviourTemplate()
        {
            return @"using UnityEngine;

namespace Laboratory.Scripts
{
    /// <summary>
    /// Add description here
    /// </summary>
    public class NewScript : MonoBehaviour
    {
        [Header(""Settings"")]
        [SerializeField] private float exampleValue = 1f;

        private void Awake()
        {

        }

        private void Start()
        {

        }

        private void Update()
        {

        }
    }
}";
        }

        private string GetScriptableObjectTemplate()
        {
            return @"using UnityEngine;

namespace Laboratory.Scripts
{
    /// <summary>
    /// Add description here
    /// </summary>
    [CreateAssetMenu(fileName = ""NewScriptableObject"", menuName = ""Laboratory/NewScriptableObject"")]
    public class NewScriptableObject : ScriptableObject
    {
        [Header(""Settings"")]
        [SerializeField] private float exampleValue = 1f;

        public float ExampleValue => exampleValue;
    }
}";
        }

        private string GetEditorTemplate()
        {
            return @"using UnityEngine;
using UnityEditor;

namespace Laboratory.Editor
{
    /// <summary>
    /// Custom editor for NewScript
    /// </summary>
    [CustomEditor(typeof(NewScript))]
    public class NewScriptEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add custom inspector code here
        }
    }
}";
        }

        private string GetInterfaceTemplate()
        {
            return @"namespace Laboratory.Scripts
{
    /// <summary>
    /// Add description here
    /// </summary>
    public interface INewInterface
    {
        void ExampleMethod();
        bool ExampleProperty { get; }
    }
}";
        }

        #endregion
    }

    #region Enums

    public enum ScriptTemplate
    {
        MonoBehaviour,
        ScriptableObject,
        Editor,
        Interface
    }

    #endregion
}