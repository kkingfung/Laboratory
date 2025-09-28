using UnityEngine;
using UnityEditor;
using System.IO;
using Laboratory.Chimera.Configuration;

namespace Laboratory.Chimera.Editor
{
    /// <summary>
    /// Editor utility to generate default Chimera configurations
    /// FEATURES: One-click asset generation, proper folder structure, validation
    /// USAGE: Window -> Project Chimera -> Generate Configuration Assets
    /// </summary>
    public class ChimeraConfigurationGenerator : EditorWindow
    {
        private const string CONFIG_PATH = "Assets/Resources/Configs";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/Chimera";

        private bool _createConfigDirectory = true;
        private bool _overwriteExisting = false;
        private string _configAssetName = "ChimeraUniverse";

        [MenuItem("🧪 Laboratory/Project Chimera/Windows/Generate Configuration Assets")]
        public static void ShowWindow()
        {
            GetWindow<ChimeraConfigurationGenerator>("Chimera Config Generator");
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Assets/Default Universe Configuration")]
        public static void CreateDefaultConfiguration()
        {
            CreateChimeraUniverseConfiguration("ChimeraUniverse", true);
        }

        private void OnGUI()
        {
            GUILayout.Label("Chimera Configuration Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("This tool generates the default configuration assets needed for Project Chimera. " +
                                   "These assets control all aspects of creature behavior, genetics, and world settings.", MessageType.Info);

            EditorGUILayout.Space();

            // Configuration options
            GUILayout.Label("Generation Options", EditorStyles.boldLabel);
            _createConfigDirectory = EditorGUILayout.Toggle("Create Config Directory", _createConfigDirectory);
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", _overwriteExisting);
            _configAssetName = EditorGUILayout.TextField("Configuration Name", _configAssetName);

            EditorGUILayout.Space();

            // Status information
            GUILayout.Label("Status", EditorStyles.boldLabel);
            string configPath = Path.Combine(CONFIG_PATH, _configAssetName + ".asset");
            bool configExists = AssetDatabase.LoadAssetAtPath<ChimeraUniverseConfiguration>(configPath) != null;

            if (configExists)
            {
                EditorGUILayout.HelpBox($"Configuration already exists at: {configPath}", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"Configuration will be created at: {configPath}", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Generation buttons
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(configExists && !_overwriteExisting))
            {
                if (GUILayout.Button("Generate Default Configuration", GUILayout.Height(30)))
                {
                    GenerateDefaultConfiguration();
                }
            }

            if (GUILayout.Button("Generate All Assets", GUILayout.Height(30)))
            {
                GenerateAllAssets();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Configuration Folder"))
            {
                if (Directory.Exists(CONFIG_PATH))
                {
                    EditorUtility.RevealInFinder(CONFIG_PATH);
                }
                else
                {
                    EditorUtility.DisplayDialog("Folder Not Found",
                        $"Configuration folder doesn't exist yet: {CONFIG_PATH}\n\nGenerate assets first to create the folder.",
                        "OK");
                }
            }
        }

        private void GenerateDefaultConfiguration()
        {
            CreateChimeraUniverseConfiguration(_configAssetName, _overwriteExisting);
        }

        private void GenerateAllAssets()
        {
            EditorUtility.DisplayProgressBar("Generating Chimera Assets", "Creating configuration...", 0.2f);
            CreateChimeraUniverseConfiguration(_configAssetName, _overwriteExisting);

            EditorUtility.DisplayProgressBar("Generating Chimera Assets", "Creating demo scene setup...", 0.4f);
            CreateDemoSceneSetup();

            EditorUtility.DisplayProgressBar("Generating Chimera Assets", "Creating example prefabs...", 0.6f);
            CreateExamplePrefabs();

            EditorUtility.DisplayProgressBar("Generating Chimera Assets", "Finalizing...", 0.8f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Generation Complete",
                $"Successfully generated all Chimera configuration assets!\n\n" +
                $"Configuration: {CONFIG_PATH}/{_configAssetName}.asset\n" +
                $"Demo Scene: Available in Project window\n" +
                $"Example Prefabs: {PREFAB_PATH}/",
                "OK");
        }

        public static void CreateChimeraUniverseConfiguration(string assetName, bool overwrite = false)
        {
            // Ensure config directory exists
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                AssetDatabase.ImportAsset(CONFIG_PATH);
            }

            string assetPath = Path.Combine(CONFIG_PATH, assetName + ".asset");

            // Check if asset already exists
            if (!overwrite && AssetDatabase.LoadAssetAtPath<ChimeraUniverseConfiguration>(assetPath) != null)
            {
                Debug.LogWarning($"Configuration already exists at {assetPath}. Use overwrite option to replace it.");
                return;
            }

            // Create the configuration
            var config = ChimeraUniverseConfiguration.CreateDefault();

            // Customize some settings for a good demo experience
            config.World.maxCreatures = 500;
            config.World.simulationSpeed = 1.2f;
            config.Performance.maxBehaviorUpdatesPerFrame = 200;
            config.Behavior.enableLearning = true;
            config.Ecosystem.enableRandomEvents = true;

            // Create the asset
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();

            // Select the asset in the project
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"✅ Created Chimera Universe Configuration at: {assetPath}");
        }

        private void CreateDemoSceneSetup()
        {
            // Create a demo scene GameObject with bootstrap
            var demoSetup = new GameObject("Chimera World Demo");
            var bootstrap = demoSetup.AddComponent<Laboratory.Core.ECS.ChimeraWorldBootstrap>();

            // Load the configuration we just created
            string configPath = Path.Combine(CONFIG_PATH, _configAssetName + ".asset");
            var config = AssetDatabase.LoadAssetAtPath<ChimeraUniverseConfiguration>(configPath);

            if (config != null)
            {
                // Use reflection to set the private universeConfig field
                var field = typeof(Laboratory.Core.ECS.ChimeraWorldBootstrap).GetField("universeConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(bootstrap, config);
            }

            // Create as prefab
            string prefabPath = Path.Combine(PREFAB_PATH, "ChimeraWorldDemo.prefab");
            if (!Directory.Exists(PREFAB_PATH))
            {
                Directory.CreateDirectory(PREFAB_PATH);
                AssetDatabase.ImportAsset(PREFAB_PATH);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(demoSetup, prefabPath);
            DestroyImmediate(demoSetup);

            Debug.Log($"✅ Created Chimera World Demo prefab at: {prefabPath}");
        }

        private void CreateExamplePrefabs()
        {
            if (!Directory.Exists(PREFAB_PATH))
            {
                Directory.CreateDirectory(PREFAB_PATH);
                AssetDatabase.ImportAsset(PREFAB_PATH);
            }

            // Create a simple creature visualization prefab
            var creatureViz = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            creatureViz.name = "CreatureVisualization";

            // Add a simple material
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.7f, 0.4f, 0.2f); // Brown color for creatures
            creatureViz.GetComponent<Renderer>().material = material;

            // Scale it down
            creatureViz.transform.localScale = Vector3.one * 0.5f;

            string creaturePrefabPath = Path.Combine(PREFAB_PATH, "CreatureVisualization.prefab");
            PrefabUtility.SaveAsPrefabAsset(creatureViz, creaturePrefabPath);
            DestroyImmediate(creatureViz);

            Debug.Log($"✅ Created creature visualization prefab at: {creaturePrefabPath}");
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Assets/Testing Scene")]
        public static void CreateTestingScene()
        {
            // Create a new scene for testing
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene("ChimeraTest");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

            // Add basic lighting
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Add camera
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            cameraGO.transform.position = new Vector3(0, 10, -10);
            cameraGO.transform.LookAt(Vector3.zero);
            cameraGO.tag = "MainCamera";

            // Add Chimera World Bootstrap
            var chimeraWorldGO = new GameObject("Chimera World");
            chimeraWorldGO.AddComponent<Laboratory.Core.ECS.ChimeraWorldBootstrap>();

            // Add ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = Vector3.one * 20;
            var groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = new Color(0.3f, 0.7f, 0.3f);
            ground.GetComponent<Renderer>().material = groundMaterial;

            Debug.Log("✅ Created Chimera testing scene with world bootstrap!");
        }
    }

    /// <summary>
    /// Asset processor to automatically setup configuration when imported
    /// </summary>
    public class ChimeraAssetProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                         string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.Contains("ChimeraUniverseConfiguration") && assetPath.EndsWith(".asset"))
                {
                    var config = AssetDatabase.LoadAssetAtPath<ChimeraUniverseConfiguration>(assetPath);
                    if (config != null)
                    {
                        // Validate the configuration on import
                        EditorUtility.SetDirty(config);
                    }
                }
            }
        }
    }
}