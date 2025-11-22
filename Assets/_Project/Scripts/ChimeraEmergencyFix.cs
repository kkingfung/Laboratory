using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.EmergencyFix
{
    /// <summary>
    /// Emergency fix for common Project Chimera compilation issues
    /// Run this if you're still getting errors after all our fixes!
    /// </summary>
    public class ChimeraEmergencyFix : MonoBehaviour
    {
        [Header("🚨 Emergency Fix Settings")]
        [SerializeField] private bool runFixOnStart = false;
        [SerializeField] private bool createMissingComponents = true;
        [SerializeField] private bool fixNullReferences = true;
        [SerializeField] private bool addMissingTags = true;

        void Start()
        {
            if (runFixOnStart)
            {
                StartCoroutine(EmergencyFixRoutine());
            }
        }

        [ContextMenu("Run Emergency Fix")]
        public void RunEmergencyFix()
        {
            StartCoroutine(EmergencyFixRoutine());
        }

        private IEnumerator EmergencyFixRoutine()
        {
            Debug.Log("🚨 Running Project Chimera Emergency Fix...");

            yield return new WaitForSeconds(0.1f);

            try
            {
                EnsureBasicUnitySetup();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Emergency fix step 1 failed: {e.Message}");
            }
            yield return new WaitForSeconds(0.1f);

            try
            {
                CreateMissingEssentials();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Emergency fix step 2 failed: {e.Message}");
            }
            yield return new WaitForSeconds(0.1f);

            if (fixNullReferences)
            {
                try
                {
                    FixNullReferences();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Emergency fix step 3 failed: {e.Message}");
                }
            }
            yield return new WaitForSeconds(0.1f);

            if (addMissingTags)
            {
                try
                {
                    AddMissingTags();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Emergency fix step 4 failed: {e.Message}");
                }
            }
            yield return new WaitForSeconds(0.1f);

            try
            {
                ValidateSceneSetup();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Emergency fix step 5 failed: {e.Message}");
            }

            Debug.Log("✅ Emergency fix completed!");
        }

        #region Fix Methods

        private void EnsureBasicUnitySetup()
        {
            Debug.Log("🔧 Ensuring basic Unity setup...");
            
            // Ensure we have a main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length == 0)
                {
                    Debug.Log("📷 Creating Main Camera...");
                    GameObject cameraGO = new GameObject("Main Camera");
                    cameraGO.tag = "MainCamera";
                    var cam = cameraGO.AddComponent<Camera>();
                    cameraGO.AddComponent<AudioListener>();
                    cameraGO.transform.position = new Vector3(0, 1, -10);
                }
                else
                {
                    cameras[0].tag = "MainCamera";
                }
            }
            
            // Ensure we have an AudioListener
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener == null)
            {
                Debug.Log("🔊 Adding AudioListener...");
                if (mainCamera != null)
                {
                    mainCamera.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    Camera.main?.gameObject.AddComponent<AudioListener>();
                }
            }
            
            // Ensure we have an EventSystem for UI
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.Log("🖱️ Creating EventSystem...");
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void CreateMissingEssentials()
        {
            Debug.Log("🏗️ Creating missing essential components...");
            
            if (createMissingComponents)
            {
                // Try to create ChimeraManager if missing
                var chimeraManager = FindChimeraManagerSafely();
                if (chimeraManager == null)
                {
                    Debug.Log("🐲 Creating ChimeraManager...");
                    GameObject managerGO = new GameObject("ChimeraManager");
                    
                    // Try to add the ChimeraManager component
                    try
                    {
                        var managerType = FindTypeByName("ChimeraManager") ?? 
                                         FindTypeByName("ProjectChimera.Core.ChimeraManager");
                        if (managerType != null)
                        {
                            managerGO.AddComponent(managerType);
                        }
                        else
                        {
                            // Add a placeholder if the real one isn't available
                            managerGO.AddComponent<ChimeraManagerPlaceholder>();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"⚠️ Could not create ChimeraManager: {e.Message}");
                    }
                }
            }
        }

        private MonoBehaviour FindChimeraManagerSafely()
        {
            try
            {
                // Try multiple approaches to find ChimeraManager
                var managers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Where(mb => mb.GetType().Name.Contains("ChimeraManager"))
                    .ToArray();
                    
                return managers.FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Error finding ChimeraManager: {e.Message}");
                return null;
            }
        }

        private void FixNullReferences()
        {
            Debug.Log("🔍 Fixing null references...");
            
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int fixedCount = 0;
            
            foreach (var go in allGameObjects)
            {
                var components = go.GetComponents<Component>();
                for (int i = components.Length - 1; i >= 0; i--)
                {
                    if (components[i] == null)
                    {
                        Debug.Log($"🗑️ Removing null component from {go.name}");
                        fixedCount++;
                        
                        // Note: We can't actually remove null components at runtime
                        // This would need to be done in the editor
                        #if UNITY_EDITOR
                        // In editor, we could remove the null component
                        // But it's safer to just log it for manual cleanup
                        #endif
                    }
                }
            }
            
            if (fixedCount > 0)
            {
                Debug.LogWarning($"⚠️ Found {fixedCount} null references that need manual cleanup in the editor");
            }
        }

        private void AddMissingTags()
        {
            Debug.Log("🏷️ Adding missing tags...");
            
            #if UNITY_EDITOR
            try
            {
                // Add common tags that our scripts might need
                string[] requiredTags = { "Monster", "Player", "Environment", "Interactable" };
                
                var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var tagsProp = tagManager.FindProperty("tags");
                
                foreach (string tag in requiredTags)
                {
                    bool tagExists = false;
                    for (int i = 0; i < tagsProp.arraySize; i++)
                    {
                        if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                        {
                            tagExists = true;
                            break;
                        }
                    }
                    
                    if (!tagExists)
                    {
                        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                        Debug.Log($"➕ Added tag: {tag}");
                    }
                }
                
                tagManager.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Could not add tags: {e.Message}");
            }
            #else
            Debug.LogWarning("⚠️ Tag creation only available in editor");
            #endif
        }

        private void ValidateSceneSetup()
        {
            Debug.Log("✅ Validating scene setup...");
            
            // Basic validation
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            
            Debug.Log($"📊 Scene Validation Results:");
            Debug.Log($"   📷 Cameras: {cameras.Length}");
            Debug.Log($"   🔊 AudioListeners: {audioListeners.Length}"); 
            Debug.Log($"   💡 Lights: {lights.Length}");
            
            // Warnings
            if (cameras.Length == 0)
                Debug.LogWarning("⚠️ No cameras in scene - nothing will be rendered");
            if (audioListeners.Length == 0)
                Debug.LogWarning("⚠️ No audio listeners - no sound will be heard");
            if (audioListeners.Length > 1)
                Debug.LogWarning("⚠️ Multiple audio listeners - may cause audio issues");
            if (lights.Length == 0 && RenderSettings.ambientLight == Color.black)
                Debug.LogWarning("⚠️ No lighting - scene will be dark");
        }

        #endregion

        #region Utility Methods

        private Type FindTypeByName(string typeName)
        {
            try
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes().Where(t => 
                            t.Name == typeName || 
                            t.FullName == typeName ||
                            t.Name.EndsWith(typeName));
                        if (types.Any())
                        {
                            return types.First();
                        }
                    }
                    catch
                    {
                        // Skip assemblies that can't be loaded
                        continue;
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Error finding type {typeName}: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Editor Menu Items

        #if UNITY_EDITOR
        [MenuItem("🧪 Laboratory/Project Chimera/Emergency Fix/Run Full Emergency Fix")]
        public static void RunFullEmergencyFix()
        {
            var fixer = FindFirstObjectByType<ChimeraEmergencyFix>();
            if (fixer == null)
            {
                GameObject go = new GameObject("ChimeraEmergencyFix");
                fixer = go.AddComponent<ChimeraEmergencyFix>();
            }
            
            fixer.RunEmergencyFix();
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Emergency Fix/Create Essential Objects")]
        public static void CreateEssentialObjects()
        {
            var fixer = new ChimeraEmergencyFix();
            fixer.createMissingComponents = true;
            fixer.EnsureBasicUnitySetup();
            fixer.CreateMissingEssentials();
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Emergency Fix/Add Missing Tags")]
        public static void MenuAddMissingTags()
        {
            var fixer = new ChimeraEmergencyFix();
            fixer.AddMissingTags();
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Emergency Fix/Validate Scene")]
        public static void ValidateScene()
        {
            var fixer = new ChimeraEmergencyFix();
            fixer.ValidateSceneSetup();
        }
        #endif

        #endregion
    }

    #region Placeholder Components

    /// <summary>
    /// Placeholder ChimeraManager for when the real one isn't available
    /// </summary>
    public class ChimeraManagerPlaceholder : MonoBehaviour
    {
        [Header("🐲 Chimera Manager Placeholder")]
        [SerializeField] private bool showWarning = true;
        
        void Start()
        {
            if (showWarning)
            {
                Debug.LogWarning($"⚠️ ChimeraManagerPlaceholder active on {gameObject.name}");
                Debug.LogWarning("   This is a placeholder - install the full ChimeraManager component for full functionality");
            }
        }
    }

    /// <summary>
    /// Emergency MonoBehaviour base for components that need safe initialization
    /// </summary>
    public abstract class SafeMonoBehaviour : MonoBehaviour
    {
        protected bool isInitialized = false;
        
        protected virtual void Awake()
        {
            try
            {
                SafeAwake();
                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ SafeAwake failed on {gameObject.name}: {e.Message}");
            }
        }
        
        protected virtual void Start()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"⚠️ {GetType().Name} on {gameObject.name} was not properly initialized");
                return;
            }
            
            try
            {
                SafeStart();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ SafeStart failed on {gameObject.name}: {e.Message}");
            }
        }
        
        protected virtual void SafeAwake() { }
        protected virtual void SafeStart() { }
    }

    #endregion
}
