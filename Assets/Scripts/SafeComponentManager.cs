using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Components
{
    /// <summary>
    /// Safe component manager for Project Chimera - prevents null reference hell!
    /// Every monster breeding game needs bulletproof component handling
    /// </summary>
    public class SafeComponentManager : MonoBehaviour
    {
        [Header("🛡️ Component Safety Settings")]
        [SerializeField] private bool autoValidateOnStart = true;
        [SerializeField] private bool logMissingComponents = true;
        [SerializeField] private bool createMissingComponents = false;
        [SerializeField] private bool runPeriodicValidation = true;
        [SerializeField] private float validationInterval = 30f;

        // Component caching system
        private Dictionary<Type, Component> componentCache = new Dictionary<Type, Component>();
        private Dictionary<string, GameObject> gameObjectCache = new Dictionary<string, GameObject>();
        
        // Validation tracking
        private List<string> validationErrors = new List<string>();
        private Coroutine validationCoroutine;
        
        // Events
        public static event Action<string> OnComponentMissing;
        public static event Action<string> OnComponentAdded;

        #region Unity Lifecycle

        void Awake()
        {
            InitializeComponentManager();
        }

        void Start()
        {
            if (autoValidateOnStart)
            {
                ValidateAllComponents();
            }

            if (runPeriodicValidation)
            {
                StartPeriodicValidation();
            }
        }

        void OnDestroy()
        {
            CleanupComponentManager();
        }

        #endregion

        #region Initialization

        private void InitializeComponentManager()
        {
            try
            {
                Debug.Log($"🛡️ Initializing Safe Component Manager for {gameObject.name}");
                
                // Pre-cache essential components
                CacheEssentialComponents();
                
                Debug.Log("✅ Component Manager initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Component Manager initialization failed: {e.Message}");
            }
        }

        private void CacheEssentialComponents()
        {
            // Cache commonly used components
            TryCacheComponent<Transform>();
            TryCacheComponent<Rigidbody>();
            TryCacheComponent<Collider>();
            TryCacheComponent<Renderer>();
            TryCacheComponent<AudioSource>();
            TryCacheComponent<Animator>();
        }

        private void TryCacheComponent<T>() where T : Component
        {
            try
            {
                T component = GetComponent<T>();
                if (component != null)
                {
                    componentCache[typeof(T)] = component;
                    Debug.Log($"✅ Cached {typeof(T).Name} component");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to cache {typeof(T).Name}: {e.Message}");
            }
        }

        #endregion

        #region Safe Component Access

        /// <summary>
        /// Safely get a component with null checking and caching
        /// </summary>
        public T SafeGetComponent<T>(bool useCache = true) where T : Component
        {
            try
            {
                Type componentType = typeof(T);
                
                // Check cache first if enabled
                if (useCache && componentCache.ContainsKey(componentType))
                {
                    T cachedComponent = componentCache[componentType] as T;
                    if (cachedComponent != null)
                    {
                        return cachedComponent;
                    }
                    else
                    {
                        // Remove null entry from cache
                        componentCache.Remove(componentType);
                    }
                }

                // Get component normally
                T component = GetComponent<T>();
                
                if (component != null)
                {
                    // Cache the component
                    if (useCache)
                    {
                        componentCache[componentType] = component;
                    }
                    return component;
                }
                else
                {
                    if (logMissingComponents)
                    {
                        string errorMessage = $"Component {typeof(T).Name} not found on {gameObject.name}";
                        LogComponentMissing(errorMessage);
                    }

                    // Optionally create missing component
                    if (createMissingComponents && CanCreateComponent<T>())
                    {
                        return CreateMissingComponent<T>();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ SafeGetComponent<{typeof(T).Name}> failed: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Safely get a component from a child object
        /// </summary>
        public T SafeGetComponentInChildren<T>(string childName = "", bool includeInactive = false) where T : Component
        {
            try
            {
                if (string.IsNullOrEmpty(childName))
                {
                    T component = GetComponentInChildren<T>(includeInactive);
                    if (component == null && logMissingComponents)
                    {
                        LogComponentMissing($"Component {typeof(T).Name} not found in children of {gameObject.name}");
                    }
                    return component;
                }
                else
                {
                    // Find specific child first
                    GameObject child = SafeFindChild(childName);
                    if (child != null)
                    {
                        return child.GetComponent<T>();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ SafeGetComponentInChildren<{typeof(T).Name}> failed: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Safely find a child GameObject by name
        /// </summary>
        public GameObject SafeFindChild(string childName)
        {
            try
            {
                // Check cache first
                string cacheKey = $"{gameObject.name}_{childName}";
                if (gameObjectCache.ContainsKey(cacheKey))
                {
                    GameObject cachedChild = gameObjectCache[cacheKey];
                    if (cachedChild != null)
                    {
                        return cachedChild;
                    }
                    else
                    {
                        gameObjectCache.Remove(cacheKey);
                    }
                }

                // Search for child
                Transform childTransform = transform.Find(childName);
                if (childTransform != null)
                {
                    GameObject child = childTransform.gameObject;
                    gameObjectCache[cacheKey] = child;
                    return child;
                }
                else
                {
                    // Try recursive search
                    childTransform = FindChildRecursive(transform, childName);
                    if (childTransform != null)
                    {
                        GameObject child = childTransform.gameObject;
                        gameObjectCache[cacheKey] = child;
                        return child;
                    }
                }

                if (logMissingComponents)
                {
                    LogComponentMissing($"Child '{childName}' not found under {gameObject.name}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ SafeFindChild({childName}) failed: {e.Message}");
            }

            return null;
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        #endregion

        #region Component Creation

        private bool CanCreateComponent<T>() where T : Component
        {
            // Only create certain safe components automatically
            Type componentType = typeof(T);
            
            return componentType == typeof(AudioSource) ||
                   componentType == typeof(Rigidbody) ||
                   componentType == typeof(BoxCollider) ||
                   componentType == typeof(SphereCollider) ||
                   componentType == typeof(CapsuleCollider);
        }

        private T CreateMissingComponent<T>() where T : Component
        {
            try
            {
                T component = gameObject.AddComponent<T>();
                
                // Configure the component with safe defaults
                ConfigureCreatedComponent(component);
                
                // Cache the new component
                componentCache[typeof(T)] = component;
                
                string message = $"Created missing {typeof(T).Name} component on {gameObject.name}";
                Debug.Log($"➕ {message}");
                OnComponentAdded?.Invoke(message);
                
                return component;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to create {typeof(T).Name} component: {e.Message}");
                return null;
            }
        }

        private void ConfigureCreatedComponent<T>(T component) where T : Component
        {
            try
            {
                switch (component)
                {
                    case AudioSource audioSource:
                        audioSource.playOnAwake = false;
                        audioSource.volume = 0.5f;
                        break;
                        
                    case Rigidbody rigidbody:
                        rigidbody.mass = 1f;
                        rigidbody.linearDamping = 1f;
                        rigidbody.angularDamping = 5f;
                        break;
                        
                    case BoxCollider boxCollider:
                        boxCollider.isTrigger = false;
                        break;
                        
                    case SphereCollider sphereCollider:
                        sphereCollider.isTrigger = false;
                        sphereCollider.radius = 0.5f;
                        break;
                        
                    case CapsuleCollider capsuleCollider:
                        capsuleCollider.isTrigger = false;
                        capsuleCollider.radius = 0.5f;
                        capsuleCollider.height = 2f;
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to configure {typeof(T).Name}: {e.Message}");
            }
        }

        #endregion

        #region Validation System

        private void StartPeriodicValidation()
        {
            if (validationCoroutine != null)
            {
                StopCoroutine(validationCoroutine);
            }
            
            validationCoroutine = StartCoroutine(PeriodicValidationRoutine());
        }

        private IEnumerator PeriodicValidationRoutine()
        {
            while (this != null)
            {
                yield return new WaitForSeconds(validationInterval);
                
                try
                {
                    ValidateAllComponents();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Periodic validation failed: {e.Message}");
                }
            }
        }

        public void ValidateAllComponents()
        {
            validationErrors.Clear();
            
            Debug.Log($"🔍 Validating components on {gameObject.name}...");
            
            // Validate cached components
            ValidateCachedComponents();
            
            // Validate essential components for monster objects
            ValidateMonsterComponents();
            
            // Report results
            if (validationErrors.Count == 0)
            {
                Debug.Log($"✅ Component validation passed for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Component validation found {validationErrors.Count} issues:");
                foreach (string error in validationErrors)
                {
                    Debug.LogWarning($"  • {error}");
                }
            }
        }

        private void ValidateCachedComponents()
        {
            List<Type> toRemove = new List<Type>();
            
            foreach (var kvp in componentCache)
            {
                if (kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                    validationErrors.Add($"Cached component {kvp.Key.Name} is null");
                }
            }
            
            // Remove null entries
            foreach (Type type in toRemove)
            {
                componentCache.Remove(type);
            }
        }

        private void ValidateMonsterComponents()
        {
            // Check if this looks like a monster object
            if (IsMonsterObject())
            {
                ValidateRequiredMonsterComponents();
            }
        }

        private bool IsMonsterObject()
        {
            return gameObject.name.ToLower().Contains("monster") ||
                   gameObject.name.ToLower().Contains("creature") ||
                   gameObject.name.ToLower().Contains("beast") ||
                   gameObject.tag == "Monster";
        }

        private void ValidateRequiredMonsterComponents()
        {
            // Essential components for monster objects in Project Chimera
            ValidateComponent<Transform>("Monsters need Transform for positioning");
            ValidateComponent<Collider>("Monsters need Collider for interaction");
            
            // Nice-to-have components
            if (GetComponent<Renderer>() == null)
            {
                validationErrors.Add("Monster missing Renderer - won't be visible");
            }
            
            if (GetComponent<AudioSource>() == null)
            {
                validationErrors.Add("Monster missing AudioSource - can't make sounds");
            }
        }

        private void ValidateComponent<T>(string errorMessage) where T : Component
        {
            if (GetComponent<T>() == null)
            {
                validationErrors.Add(errorMessage);
            }
        }

        #endregion

        #region Error Handling

        private void LogComponentMissing(string message)
        {
            Debug.LogWarning($"⚠️ {message}");
            OnComponentMissing?.Invoke(message);
            
            if (!validationErrors.Contains(message))
            {
                validationErrors.Add(message);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Clear all cached components and force refresh
        /// </summary>
        public void RefreshComponentCache()
        {
            componentCache.Clear();
            gameObjectCache.Clear();
            CacheEssentialComponents();
            Debug.Log($"🔄 Refreshed component cache for {gameObject.name}");
        }

        /// <summary>
        /// Get validation errors from last check
        /// </summary>
        public List<string> GetValidationErrors()
        {
            return new List<string>(validationErrors);
        }

        /// <summary>
        /// Check if object has all required components for its type
        /// </summary>
        public bool IsObjectValid()
        {
            ValidateAllComponents();
            return validationErrors.Count == 0;
        }

        /// <summary>
        /// Force validate and fix common issues
        /// </summary>
        public void AutoFixComponents()
        {
            Debug.Log($"🔧 Auto-fixing components on {gameObject.name}");
            
            bool originalCreateMissing = createMissingComponents;
            createMissingComponents = true;
            
            // Try to get essential components (will create if missing)
            SafeGetComponent<Transform>();
            
            if (IsMonsterObject())
            {
                SafeGetComponent<Collider>();
                SafeGetComponent<AudioSource>();
            }
            
            createMissingComponents = originalCreateMissing;
            
            Debug.Log($"✅ Auto-fix completed for {gameObject.name}");
        }

        #endregion

        #region Cleanup

        private void CleanupComponentManager()
        {
            try
            {
                if (validationCoroutine != null)
                {
                    StopCoroutine(validationCoroutine);
                    validationCoroutine = null;
                }
                
                componentCache.Clear();
                gameObjectCache.Clear();
                validationErrors.Clear();
                
                Debug.Log($"🧹 Component Manager cleaned up for {gameObject.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Component Manager cleanup failed: {e.Message}");
            }
        }

        #endregion

        #region Editor Utilities

        #if UNITY_EDITOR
        [MenuItem("🐲 Chimera/Component Tools/Validate Selected Objects")]
        public static void ValidateSelectedObjects()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                SafeComponentManager manager = obj.GetComponent<SafeComponentManager>();
                if (manager != null)
                {
                    manager.ValidateAllComponents();
                }
                else
                {
                    Debug.LogWarning($"⚠️ {obj.name} doesn't have SafeComponentManager");
                }
            }
        }

        [MenuItem("🐲 Chimera/Component Tools/Auto-Fix Selected Objects")]
        public static void AutoFixSelectedObjects()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                SafeComponentManager manager = obj.GetComponent<SafeComponentManager>();
                if (manager == null)
                {
                    manager = obj.AddComponent<SafeComponentManager>();
                }
                manager.AutoFixComponents();
            }
        }

        [MenuItem("🐲 Chimera/Component Tools/Add to Selected Monsters")]
        public static void AddToSelectedMonsters()
        {
            int count = 0;
            foreach (GameObject obj in Selection.gameObjects)
            {
                if (obj.name.ToLower().Contains("monster") || 
                    obj.name.ToLower().Contains("creature") ||
                    obj.tag == "Monster")
                {
                    if (obj.GetComponent<SafeComponentManager>() == null)
                    {
                        obj.AddComponent<SafeComponentManager>();
                        count++;
                    }
                }
            }
            Debug.Log($"➕ Added SafeComponentManager to {count} monster objects");
        }
        #endif

        #endregion
    }

    #region Helper Components

    /// <summary>
    /// Simple component to mark objects as monsters for validation
    /// </summary>
    public class MonsterTag : MonoBehaviour
    {
        [Header("🐲 Monster Info")]
        public string monsterName = "Unknown Monster";
        public string species = "Generic";
        public int level = 1;
        
        void Awake()
        {
            // Ensure this object is tagged as Monster
            if (gameObject.tag != "Monster")
            {
                try
                {
                    gameObject.tag = "Monster";
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Could not set Monster tag: {e.Message}");
                }
            }
        }
    }

    #endregion
}
