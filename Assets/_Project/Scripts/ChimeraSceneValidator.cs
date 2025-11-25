using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ProjectChimera.Validation
{
    /// <summary>
    /// Scene validation system for Project Chimera
    /// Catches setup errors before they crash our monster breeding paradise!
    /// </summary>
    public class ChimeraSceneValidator : MonoBehaviour
    {
        [Header("🔍 Validation Settings")]
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool validatePeriodically = true;
        [SerializeField] private float validationInterval = 60f;
        [SerializeField] private bool autoFixIssues = false;
        [SerializeField] private bool showDetailedLogs = true;

        [Header("🎯 Validation Rules")]
        [SerializeField] private bool checkEssentialObjects = true;
        [SerializeField] private bool checkCameraSetup = true;
        [SerializeField] private bool checkAudioSetup = true;
        [SerializeField] private bool checkLightingSetup = true;
        [SerializeField] private bool checkNetworkSetup = true;
        [SerializeField] private bool checkMonsterSetup = true;

        private List<ValidationIssue> issues = new List<ValidationIssue>();
        private Coroutine validationCoroutine;

        // Validation results
        public static event Action<List<ValidationIssue>> OnValidationComplete;
        public static event Action<ValidationIssue> OnIssueFound;
        public static event Action<ValidationIssue> OnIssueFixed;

        #region Unity Lifecycle

        void Start()
        {
            if (validateOnStart)
            {
                ValidateScene();
            }

            if (validatePeriodically)
            {
                StartPeriodicValidation();
            }
        }

        void OnDestroy()
        {
            if (validationCoroutine != null)
            {
                StopCoroutine(validationCoroutine);
            }
        }

        #endregion

        #region Scene Validation

        [ContextMenu("Validate Scene")]
        public void ValidateScene()
        {
            try
            {
                Debug.Log("🔍 Starting Project Chimera scene validation...");
                issues.Clear();

                Scene currentScene = SceneManager.GetActiveScene();
                Debug.Log($"🎬 Validating scene: {currentScene.name}");

                // Run validation checks
                if (checkEssentialObjects) ValidateEssentialObjects();
                if (checkCameraSetup) ValidateCameraSetup();
                if (checkAudioSetup) ValidateAudioSetup();
                if (checkLightingSetup) ValidateLightingSetup();
                if (checkNetworkSetup) ValidateNetworkSetup();
                if (checkMonsterSetup) ValidateMonsterSetup();

                // Additional checks
                ValidateSceneReferences();
                ValidatePerformanceSettings();

                // Report results
                ReportValidationResults();

                // Auto-fix if enabled
                if (autoFixIssues && issues.Any(i => i.canAutoFix))
                {
                    AutoFixIssues();
                }

                OnValidationComplete?.Invoke(new List<ValidationIssue>(issues));
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Scene validation failed: {e.Message}");
            }
        }

        #endregion

        #region Validation Checks

        private void ValidateEssentialObjects()
        {
            Debug.Log("🔍 Checking essential objects...");

            // Check for EventSystem (required for UI)
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                AddIssue(ValidationSeverity.Warning, 
                    "EventSystem", 
                    "No EventSystem found - UI interactions won't work",
                    "UI will not respond to input",
                    true);
            }

            // Check for Canvas if UI elements exist
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length == 0 && FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Length > 0)
            {
                AddIssue(ValidationSeverity.Error,
                    "Canvas",
                    "UI elements found but no Canvas present",
                    "UI elements won't render",
                    true);
            }
        }

        private void ValidateCameraSetup()
        {
            Debug.Log("🔍 Checking camera setup...");

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            
            if (cameras.Length == 0)
            {
                AddIssue(ValidationSeverity.Error,
                    "Camera",
                    "No cameras found in scene",
                    "Nothing will be visible to players",
                    true);
            }
            else if (cameras.Length == 1)
            {
                Camera cam = cameras[0];
                
                // Check camera tag
                if (!cam.CompareTag("MainCamera"))
                {
                    AddIssue(ValidationSeverity.Warning,
                        "Camera Tag",
                        "Camera is not tagged as MainCamera",
                        "Camera.main will return null",
                        true);
                }

                // Check camera clear flags for performance
                if (cam.clearFlags == CameraClearFlags.Nothing)
                {
                    AddIssue(ValidationSeverity.Info,
                        "Camera Clear Flags", 
                        "Camera set to Don't Clear - ensure this is intentional",
                        "May cause rendering artifacts",
                        false);
                }
            }
            else
            {
                // Multiple cameras - check for conflicts
                Camera[] mainCameras = cameras.Where(c => c.CompareTag("MainCamera")).ToArray();
                if (mainCameras.Length > 1)
                {
                    AddIssue(ValidationSeverity.Warning,
                        "Multiple Main Cameras",
                        $"{mainCameras.Length} cameras tagged as MainCamera",
                        "Camera.main behavior undefined",
                        false);
                }
            }
        }

        private void ValidateAudioSetup()
        {
            Debug.Log("🔍 Checking audio setup...");

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            
            if (listeners.Length == 0)
            {
                AddIssue(ValidationSeverity.Error,
                    "AudioListener",
                    "No AudioListener found in scene",
                    "No audio will be heard",
                    true);
            }
            else if (listeners.Length > 1)
            {
                AddIssue(ValidationSeverity.Error,
                    "Multiple AudioListeners", 
                    $"{listeners.Length} AudioListeners found",
                    "Audio will not work correctly",
                    true);
            }

            // Check for AudioSource components with missing clips
            AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource source in audioSources)
            {
                if (source.playOnAwake && source.clip == null)
                {
                    AddIssue(ValidationSeverity.Warning,
                        "Missing Audio Clip",
                        $"AudioSource on {source.gameObject.name} has Play On Awake but no clip",
                        "Will cause errors at runtime",
                        false);
                }
            }
        }

        private void ValidateLightingSetup()
        {
            Debug.Log("🔍 Checking lighting setup...");

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            
            if (lights.Length == 0)
            {
                // Check if we have environment lighting
                if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat && 
                    RenderSettings.ambientLight == Color.black)
                {
                    AddIssue(ValidationSeverity.Warning,
                        "No Lighting",
                        "No lights and no ambient lighting",
                        "Scene will be completely dark",
                        true);
                }
            }
            else
            {
                // Check for excessive lights (performance)
                Light[] realtimeLights = lights.Where(l => l.lightmapBakeType == LightmapBakeType.Realtime).ToArray();
                if (realtimeLights.Length > 8)
                {
                    AddIssue(ValidationSeverity.Warning,
                        "Too Many Realtime Lights",
                        $"{realtimeLights.Length} realtime lights found",
                        "May impact performance",
                        false);
                }
            }
        }

        private void ValidateNetworkSetup()
        {
            Debug.Log("🔍 Checking network setup...");

            // Check for NetworkManager
            var networkManagers = FindObjectsByType<ProjectChimera.Networking.ChimeraNetworkManager>(FindObjectsSortMode.None);
            if (networkManagers.Length == 0)
            {
                AddIssue(ValidationSeverity.Info,
                    "Network Manager",
                    "No ChimeraNetworkManager found",
                    "Multiplayer features disabled",
                    true);
            }
            else if (networkManagers.Length > 1)
            {
                AddIssue(ValidationSeverity.Error,
                    "Multiple Network Managers",
                    $"{networkManagers.Length} ChimeraNetworkManagers found",
                    "Network conflicts will occur",
                    false);
            }

            // Check for Unity Netcode
            #if !UNITY_NETCODE_GAMEOBJECTS
            AddIssue(ValidationSeverity.Warning,
                "Netcode Package",
                "Unity Netcode for GameObjects not installed",
                "Multiplayer functionality limited",
                false);
            #endif
        }

        private void ValidateMonsterSetup()
        {
            Debug.Log("🔍 Checking monster setup...");

            // Check for ChimeraManager
            var chimeraManagers = UnityEngine.Object.FindObjectsByType<ProjectChimera.Core.ChimeraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (chimeraManagers == null || chimeraManagers.Length == 0)
            {
                chimeraManagers = FindObjectsByType<ProjectChimera.Core.ChimeraManager>(FindObjectsSortMode.None);
            }
            if (chimeraManagers.Length == 0)
            {
                AddIssue(ValidationSeverity.Warning,
                    "Chimera Manager",
                    "No ChimeraManager found in scene",
                    "Core systems may not initialize",
                    true);
            }

            // Check for monster objects
            GameObject[] potentialMonsters = GameObject.FindGameObjectsWithTag("Monster");
            if (potentialMonsters.Length == 0)
            {
                // Check by name
                GameObject[] namedMonsters = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                    .Where(go => go.name.ToLower().Contains("monster") || go.name.ToLower().Contains("creature"))
                    .ToArray();
                
                if (namedMonsters.Length == 0)
                {
                    AddIssue(ValidationSeverity.Info,
                        "No Monsters",
                        "No monster objects found in scene",
                        "This may be intentional for menu scenes",
                        false);
                }
            }

            // Check monster components
            foreach (GameObject monster in potentialMonsters)
            {
                ValidateMonsterObject(monster);
            }
        }

        private void ValidateMonsterObject(GameObject monster)
        {
            if (monster.GetComponent<Collider>() == null)
            {
                AddIssue(ValidationSeverity.Warning,
                    "Monster Missing Collider",
                    $"Monster {monster.name} has no Collider",
                    "Monster won't interact with world",
                    false);
            }

            if (monster.GetComponent<Renderer>() == null)
            {
                AddIssue(ValidationSeverity.Warning,
                    "Monster Missing Renderer", 
                    $"Monster {monster.name} has no Renderer",
                    "Monster will be invisible",
                    false);
            }
        }

        private void ValidateSceneReferences()
        {
            Debug.Log("🔍 Checking scene references...");

            // Find all MonoBehaviours and check for missing script references
            MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour mb in allMonoBehaviours)
            {
                if (mb == null)
                {
                    AddIssue(ValidationSeverity.Error,
                        "Missing Script",
                        "Missing script reference found",
                        "Will cause null reference errors",
                        false);
                }
            }
        }

        private void ValidatePerformanceSettings()
        {
            Debug.Log("🔍 Checking performance settings...");

            // Check for excessive GameObject count
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            if (allObjects.Length > 5000)
            {
                AddIssue(ValidationSeverity.Warning,
                    "High GameObject Count",
                    $"{allObjects.Length} GameObjects in scene",
                    "May impact performance",
                    false);
            }

            // Check texture settings on renderers
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                foreach (Material mat in materials)
                {
                    if (mat != null && mat.mainTexture != null)
                    {
                        Texture2D tex = mat.mainTexture as Texture2D;
                        if (tex != null && (tex.width > 2048 || tex.height > 2048))
                        {
                            AddIssue(ValidationSeverity.Info,
                                "Large Texture",
                                $"Large texture ({tex.width}x{tex.height}) on {renderer.gameObject.name}",
                                "Consider optimizing texture size",
                                false);
                        }
                    }
                }
            }
        }

        #endregion

        #region Issue Management

        private void AddIssue(ValidationSeverity severity, string category, string description, string impact, bool canAutoFix)
        {
            var issue = new ValidationIssue
            {
                severity = severity,
                category = category,
                description = description,
                impact = impact,
                canAutoFix = canAutoFix,
                sceneName = SceneManager.GetActiveScene().name,
                timestamp = DateTime.Now
            };

            issues.Add(issue);
            OnIssueFound?.Invoke(issue);

            if (showDetailedLogs)
            {
                string logMessage = $"{GetSeverityIcon(severity)} {category}: {description}";
                
                switch (severity)
                {
                    case ValidationSeverity.Error:
                        Debug.LogError(logMessage);
                        break;
                    case ValidationSeverity.Warning:
                        Debug.LogWarning(logMessage);
                        break;
                    case ValidationSeverity.Info:
                        Debug.Log(logMessage);
                        break;
                }
            }
        }

        private string GetSeverityIcon(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Error: return "❌";
                case ValidationSeverity.Warning: return "⚠️";
                case ValidationSeverity.Info: return "ℹ️";
                default: return "?";
            }
        }

        private void AutoFixIssues()
        {
            Debug.Log("🔧 Auto-fixing issues...");
            
            int fixedCount = 0;
            foreach (var issue in issues.Where(i => i.canAutoFix).ToList())
            {
                if (TryAutoFix(issue))
                {
                    fixedCount++;
                    OnIssueFixed?.Invoke(issue);
                    issues.Remove(issue);
                }
            }
            
            Debug.Log($"✅ Auto-fixed {fixedCount} issues");
        }

        private bool TryAutoFix(ValidationIssue issue)
        {
            try
            {
                switch (issue.category)
                {
                    case "EventSystem":
                        CreateEventSystem();
                        return true;
                        
                    case "Canvas":
                        CreateCanvas();
                        return true;
                        
                    case "Camera":
                        CreateMainCamera();
                        return true;
                        
                    case "Camera Tag":
                        FixCameraTag();
                        return true;
                        
                    case "AudioListener":
                        CreateAudioListener();
                        return true;
                        
                    case "Multiple AudioListeners":
                        FixMultipleAudioListeners();
                        return true;
                        
                    case "No Lighting":
                        CreateBasicLighting();
                        return true;
                        
                    case "Network Manager":
                        CreateNetworkManager();
                        return true;
                        
                    case "Chimera Manager":
                        CreateChimeraManager();
                        return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to auto-fix {issue.category}: {e.Message}");
            }
            
            return false;
        }

        #endregion

        #region Auto-Fix Methods

        private void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("➕ Created EventSystem");
        }

        private void CreateCanvas()
        {
            GameObject canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("➕ Created Canvas");
        }

        private void CreateMainCamera()
        {
            GameObject camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            Camera cam = camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();
            
            // Position camera sensibly
            camera.transform.position = new Vector3(0, 1, -10);
            cam.clearFlags = CameraClearFlags.Skybox;
            
            Debug.Log("➕ Created Main Camera with AudioListener");
        }

        private void FixCameraTag()
        {
            Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.tag = "MainCamera";
                Debug.Log("🔧 Fixed camera tag");
            }
        }

        private void CreateAudioListener()
        {
            Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
                Debug.Log("➕ Added AudioListener to camera");
            }
            else
            {
                GameObject audioListenerGO = new GameObject("AudioListener");
                audioListenerGO.AddComponent<AudioListener>();
                Debug.Log("➕ Created standalone AudioListener");
            }
        }

        private void FixMultipleAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 1; i < listeners.Length; i++)
            {
                DestroyImmediate(listeners[i]);
            }
            Debug.Log($"🔧 Removed {listeners.Length - 1} extra AudioListeners");
        }

        private void CreateBasicLighting()
        {
            GameObject light = new GameObject("Directional Light");
            Light lightComponent = light.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.color = Color.white;
            lightComponent.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            
            Debug.Log("➕ Created basic directional light");
        }

        private void CreateNetworkManager()
        {
            GameObject networkManager = new GameObject("ChimeraNetworkManager");
            networkManager.AddComponent<ProjectChimera.Networking.ChimeraNetworkManager>();
            Debug.Log("➕ Created ChimeraNetworkManager");
        }

        private void CreateChimeraManager()
        {
            GameObject chimeraManager = new GameObject("ChimeraManager");
            chimeraManager.AddComponent<ProjectChimera.Core.ChimeraManager>();
            Debug.Log("➕ Created ChimeraManager");
        }

        #endregion

        #region Reporting

        private void ReportValidationResults()
        {
            Debug.Log("📊 === SCENE VALIDATION RESULTS ===");
            
            var errorCount = issues.Count(i => i.severity == ValidationSeverity.Error);
            var warningCount = issues.Count(i => i.severity == ValidationSeverity.Warning);
            var infoCount = issues.Count(i => i.severity == ValidationSeverity.Info);
            
            Debug.Log($"❌ Errors: {errorCount}");
            Debug.Log($"⚠️ Warnings: {warningCount}");  
            Debug.Log($"ℹ️ Info: {infoCount}");
            
            if (issues.Count == 0)
            {
                Debug.Log("🎉 Scene validation passed! No issues found.");
            }
            else
            {
                Debug.Log($"📋 Found {issues.Count} total issues:");
                foreach (var issue in issues)
                {
                    Debug.Log($"  {GetSeverityIcon(issue.severity)} {issue.category}: {issue.description}");
                }
                
                var fixableCount = issues.Count(i => i.canAutoFix);
                if (fixableCount > 0)
                {
                    Debug.Log($"🔧 {fixableCount} issues can be auto-fixed");
                }
            }
            
            Debug.Log("=== END VALIDATION RESULTS ===");
        }

        #endregion

        #region Periodic Validation

        private void StartPeriodicValidation()
        {
            validationCoroutine = StartCoroutine(PeriodicValidationRoutine());
        }

        private System.Collections.IEnumerator PeriodicValidationRoutine()
        {
            while (this != null)
            {
                yield return new WaitForSeconds(validationInterval);
                
                Debug.Log("🔄 Running periodic scene validation...");
                ValidateScene();
            }
        }

        #endregion

        #region Public API

        public List<ValidationIssue> GetCurrentIssues()
        {
            return new List<ValidationIssue>(issues);
        }

        public void ForceAutoFix()
        {
            if (issues.Any(i => i.canAutoFix))
            {
                AutoFixIssues();
                ValidateScene(); // Re-validate after fixes
            }
            else
            {
                Debug.Log("⚠️ No auto-fixable issues found");
            }
        }

        public void ClearIssues()
        {
            issues.Clear();
            Debug.Log("🧹 Cleared validation issues");
        }

        #endregion

        #region Editor Integration

        #if UNITY_EDITOR
        [MenuItem("🧪 Laboratory/Project Chimera/Scene Validation/Validate Current Scene")]
        public static void ValidateCurrentScene()
        {
            ChimeraSceneValidator validator = FindFirstObjectByType<ChimeraSceneValidator>();
            if (validator == null)
            {
                // Create temporary validator
                GameObject temp = new GameObject("TempValidator");
                validator = temp.AddComponent<ChimeraSceneValidator>();
                validator.ValidateScene();
                DestroyImmediate(temp);
            }
            else
            {
                validator.ValidateScene();
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Scene Validation/Auto-Fix Issues")]
        public static void AutoFixCurrentScene()
        {
            ChimeraSceneValidator validator = FindFirstObjectByType<ChimeraSceneValidator>();
            if (validator != null)
            {
                validator.ForceAutoFix();
            }
            else
            {
                Debug.LogWarning("⚠️ No ChimeraSceneValidator found in scene");
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Scene Validation/Add Validator to Scene")]
        public static void AddValidatorToScene()
        {
            if (FindFirstObjectByType<ChimeraSceneValidator>() == null)
            {
                GameObject validator = new GameObject("ChimeraSceneValidator");
                validator.AddComponent<ChimeraSceneValidator>();
                Debug.Log("➕ Added ChimeraSceneValidator to scene");
                
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            else
            {
                Debug.Log("ℹ️ ChimeraSceneValidator already exists in scene");
            }
        }
        #endif

        #endregion
    }

    #region Data Structures

    [Serializable]
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public class ValidationIssue
    {
        public ValidationSeverity severity;
        public string category;
        public string description;
        public string impact;
        public bool canAutoFix;
        public string sceneName;
        public DateTime timestamp;

        public override string ToString()
        {
            return $"[{severity}] {category}: {description} (Impact: {impact})";
        }
    }

    #endregion
}
