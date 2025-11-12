using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Laboratory.Advanced
{
    /// <summary>
    /// Advanced shader management system for performance optimization.
    /// Handles shader loading, material variants, parameter batching, and warmup.
    /// Reduces draw calls and eliminates shader compilation hitches.
    /// </summary>
    public class AdvancedShaderManager : MonoBehaviour
    {
        #region Configuration

        [Header("Shader Settings")]
        [SerializeField] private bool preloadAllShaders = false;
        [SerializeField] private bool warmupShadersOnStart = true;
        [SerializeField] private bool enableShaderVariantStripping = true;

        [Header("Material Pooling")]
        [SerializeField] private bool useMaterialInstancing = true;
        [SerializeField] private int maxMaterialVariants = 1000;

        [Header("Performance")]
        [SerializeField] private bool batchMaterialProperties = true;
        [SerializeField] private int materialBatchSize = 100;
        [SerializeField] private bool useGPUInstancing = true;

        [Header("Quality")]
        [SerializeField] private ShaderQualityLevel currentQualityLevel = ShaderQualityLevel.High;

        #endregion

        #region Private Fields

        private static AdvancedShaderManager _instance;

        // Shader cache
        private readonly Dictionary<string, Shader> _shaderCache = new Dictionary<string, Shader>();
        private readonly Dictionary<string, ShaderVariantCollection> _variantCollections = new Dictionary<string, ShaderVariantCollection>();

        // Material management
        private readonly Dictionary<string, Material> _baseMaterials = new Dictionary<string, Material>();
        private readonly Dictionary<string, List<Material>> _materialInstances = new Dictionary<string, List<Material>>();
        private readonly Dictionary<Material, MaterialPropertyBlock> _propertyBlocks = new Dictionary<Material, MaterialPropertyBlock>();

        // Warmup
        private bool _isWarmedUp = false;
        private readonly HashSet<Shader> _warmedUpShaders = new HashSet<Shader>();

        // Statistics
        private int _totalShadersLoaded = 0;
        private int _totalMaterialsCreated = 0;
        private int _shaderWarmups = 0;
        private int _batchedPropertySets = 0;

        // Events
        public event Action<Shader> OnShaderLoaded;
        public event Action<Material> OnMaterialCreated;
        public event Action OnShadersWarmedUp;
        public event Action<ShaderQualityLevel> OnQualityChanged;

        #endregion

        #region Properties

        public static AdvancedShaderManager Instance => _instance;
        public int LoadedShaderCount => _shaderCache.Count;
        public int MaterialVariantCount => _materialInstances.Sum(kvp => kvp.Value.Count);
        public bool IsShadersWarmedUp => _isWarmedUp;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (warmupShadersOnStart && !_isWarmedUp)
            {
                StartCoroutine(WarmupShadersCoroutine());
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[AdvancedShaderManager] Initializing...");

            if (preloadAllShaders)
            {
                LoadAllShaders();
            }

            Debug.Log("[AdvancedShaderManager] Initialized");
        }

        private void LoadAllShaders()
        {
            var shaders = Resources.FindObjectsOfTypeAll<Shader>();

            foreach (var shader in shaders)
            {
                if (shader.name.StartsWith("Hidden/")) continue;
                if (shader.name.StartsWith("Legacy/")) continue;

                _shaderCache[shader.name] = shader;
                _totalShadersLoaded++;
            }

            Debug.Log($"[AdvancedShaderManager] Preloaded {_totalShadersLoaded} shaders");
        }

        #endregion

        #region Shader Management

        /// <summary>
        /// Get or load a shader by name.
        /// </summary>
        public Shader GetShader(string shaderName)
        {
            if (_shaderCache.TryGetValue(shaderName, out var cachedShader))
            {
                return cachedShader;
            }

            var shader = Shader.Find(shaderName);

            if (shader != null)
            {
                _shaderCache[shaderName] = shader;
                _totalShadersLoaded++;

                OnShaderLoaded?.Invoke(shader);
            }
            else
            {
                Debug.LogWarning($"[AdvancedShaderManager] Shader not found: {shaderName}");
            }

            return shader;
        }

        /// <summary>
        /// Register a shader variant collection.
        /// </summary>
        public void RegisterVariantCollection(string collectionName, ShaderVariantCollection collection)
        {
            _variantCollections[collectionName] = collection;
            Debug.Log($"[AdvancedShaderManager] Variant collection registered: {collectionName}");
        }

        #endregion

        #region Material Management

        /// <summary>
        /// Get or create a material instance.
        /// </summary>
        public Material GetMaterialInstance(string baseMaterialName)
        {
            if (!useMaterialInstancing)
            {
                return GetBaseMaterial(baseMaterialName);
            }

            if (!_materialInstances.TryGetValue(baseMaterialName, out var instances))
            {
                instances = new List<Material>();
                _materialInstances[baseMaterialName] = instances;
            }

            // Check for available instance
            foreach (var instance in instances)
            {
                if (instance != null && !IsInstanceInUse(instance))
                {
                    return instance;
                }
            }

            // Create new instance
            var baseMaterial = GetBaseMaterial(baseMaterialName);

            if (baseMaterial == null)
            {
                Debug.LogError($"[AdvancedShaderManager] Base material not found: {baseMaterialName}");
                return null;
            }

            if (instances.Count >= maxMaterialVariants)
            {
                Debug.LogWarning($"[AdvancedShaderManager] Max material variants reached for: {baseMaterialName}");
                return instances[0]; // Return first instance
            }

            var newInstance = new Material(baseMaterial);
            newInstance.name = $"{baseMaterialName}_Instance_{instances.Count}";

            // Enable GPU instancing if supported
            if (useGPUInstancing)
            {
                newInstance.enableInstancing = true;
            }

            instances.Add(newInstance);
            _totalMaterialsCreated++;

            OnMaterialCreated?.Invoke(newInstance);

            return newInstance;
        }

        /// <summary>
        /// Get base material.
        /// </summary>
        public Material GetBaseMaterial(string materialName)
        {
            if (_baseMaterials.TryGetValue(materialName, out var material))
            {
                return material;
            }

            // Try to load from Resources
            material = Resources.Load<Material>(materialName);

            if (material != null)
            {
                _baseMaterials[materialName] = material;
            }

            return material;
        }

        /// <summary>
        /// Register base material.
        /// </summary>
        public void RegisterBaseMaterial(string materialName, Material material)
        {
            _baseMaterials[materialName] = material;
            Debug.Log($"[AdvancedShaderManager] Base material registered: {materialName}");
        }

        private bool IsInstanceInUse(Material instance)
        {
            // Simple check - in production, would track actual usage
            return false;
        }

        #endregion

        #region Material Property Batching

        /// <summary>
        /// Set material property with batching.
        /// </summary>
        public void SetMaterialProperty(Material material, string propertyName, float value)
        {
            if (!batchMaterialProperties)
            {
                material.SetFloat(propertyName, value);
                return;
            }

            GetPropertyBlock(material).SetFloat(propertyName, value);
            _batchedPropertySets++;
        }

        /// <summary>
        /// Set material color with batching.
        /// </summary>
        public void SetMaterialColor(Material material, string propertyName, Color color)
        {
            if (!batchMaterialProperties)
            {
                material.SetColor(propertyName, color);
                return;
            }

            GetPropertyBlock(material).SetColor(propertyName, color);
            _batchedPropertySets++;
        }

        /// <summary>
        /// Set material texture with batching.
        /// </summary>
        public void SetMaterialTexture(Material material, string propertyName, Texture texture)
        {
            if (!batchMaterialProperties)
            {
                material.SetTexture(propertyName, texture);
                return;
            }

            GetPropertyBlock(material).SetTexture(propertyName, texture);
            _batchedPropertySets++;
        }

        /// <summary>
        /// Apply batched properties to renderer.
        /// </summary>
        public void ApplyPropertiesToRenderer(Renderer renderer, Material material)
        {
            if (_propertyBlocks.TryGetValue(material, out var propertyBlock))
            {
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private MaterialPropertyBlock GetPropertyBlock(Material material)
        {
            if (!_propertyBlocks.TryGetValue(material, out var propertyBlock))
            {
                propertyBlock = new MaterialPropertyBlock();
                _propertyBlocks[material] = propertyBlock;
            }

            return propertyBlock;
        }

        #endregion

        #region Shader Warmup

        /// <summary>
        /// Warm up all shaders to prevent hitches.
        /// </summary>
        public void WarmupShaders(Action onComplete = null)
        {
            StartCoroutine(WarmupShadersCoroutine(onComplete));
        }

        private IEnumerator WarmupShadersCoroutine(Action onComplete = null)
        {
            Debug.Log("[AdvancedShaderManager] Starting shader warmup...");

            var shaders = _shaderCache.Values.ToList();

            foreach (var shader in shaders)
            {
                if (_warmedUpShaders.Contains(shader)) continue;

                // Warmup shader by loading all variant collections
                foreach (var collection in _variantCollections.Values)
                {
                    if (collection != null)
                    {
                        collection.WarmUp();
                    }
                }

                // Manual warmup for shader
                WarmupShader(shader);

                _warmedUpShaders.Add(shader);
                _shaderWarmups++;

                yield return null; // Spread over multiple frames
            }

            _isWarmedUp = true;

            OnShadersWarmedUp?.Invoke();

            Debug.Log($"[AdvancedShaderManager] Shader warmup complete: {_shaderWarmups} shaders");

            onComplete?.Invoke();
        }

        private void WarmupShader(Shader shader)
        {
            // Create temporary material to force compilation
            var tempMaterial = new Material(shader);
            var tempGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempGO.GetComponent<Renderer>().material = tempMaterial;

            // Render off-screen
            tempGO.transform.position = new Vector3(10000, 10000, 10000);

            // Destroy after one frame
            StartCoroutine(DestroyAfterFrame(tempGO, tempMaterial));
        }

        private IEnumerator DestroyAfterFrame(GameObject go, Material material)
        {
            yield return null;
            Destroy(go);
            Destroy(material);
        }

        #endregion

        #region Quality Management

        /// <summary>
        /// Set shader quality level.
        /// </summary>
        public void SetQualityLevel(ShaderQualityLevel level)
        {
            if (currentQualityLevel == level) return;

            currentQualityLevel = level;

            // Update global shader keywords
            UpdateShaderKeywords();

            OnQualityChanged?.Invoke(level);

            Debug.Log($"[AdvancedShaderManager] Quality level set to: {level}");
        }

        private void UpdateShaderKeywords()
        {
            // Disable all quality keywords
            Shader.DisableKeyword("QUALITY_LOW");
            Shader.DisableKeyword("QUALITY_MEDIUM");
            Shader.DisableKeyword("QUALITY_HIGH");
            Shader.DisableKeyword("QUALITY_ULTRA");

            // Enable current quality keyword
            switch (currentQualityLevel)
            {
                case ShaderQualityLevel.Low:
                    Shader.EnableKeyword("QUALITY_LOW");
                    break;
                case ShaderQualityLevel.Medium:
                    Shader.EnableKeyword("QUALITY_MEDIUM");
                    break;
                case ShaderQualityLevel.High:
                    Shader.EnableKeyword("QUALITY_HIGH");
                    break;
                case ShaderQualityLevel.Ultra:
                    Shader.EnableKeyword("QUALITY_ULTRA");
                    break;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Clear all material instances.
        /// </summary>
        public void ClearMaterialInstances()
        {
            foreach (var instances in _materialInstances.Values)
            {
                foreach (var instance in instances)
                {
                    if (instance != null)
                    {
                        Destroy(instance);
                    }
                }
            }

            _materialInstances.Clear();
            _propertyBlocks.Clear();

            Debug.Log("[AdvancedShaderManager] Material instances cleared");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get shader manager statistics.
        /// </summary>
        public ShaderManagerStats GetStats()
        {
            return new ShaderManagerStats
            {
                loadedShaders = _shaderCache.Count,
                totalShadersLoaded = _totalShadersLoaded,
                materialVariants = MaterialVariantCount,
                totalMaterialsCreated = _totalMaterialsCreated,
                shaderWarmups = _shaderWarmups,
                batchedPropertySets = _batchedPropertySets,
                isWarmedUp = _isWarmedUp
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Warmup Shaders")]
        private void WarmupShadersMenu()
        {
            WarmupShaders();
        }

        [ContextMenu("Clear Material Instances")]
        private void ClearMaterialInstancesMenu()
        {
            ClearMaterialInstances();
        }

        [ContextMenu("Set Quality: Low")]
        private void SetQualityLow()
        {
            SetQualityLevel(ShaderQualityLevel.Low);
        }

        [ContextMenu("Set Quality: High")]
        private void SetQualityHigh()
        {
            SetQualityLevel(ShaderQualityLevel.High);
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Shader Manager Statistics ===\n" +
                      $"Loaded Shaders: {stats.loadedShaders}\n" +
                      $"Total Shaders Loaded: {stats.totalShadersLoaded}\n" +
                      $"Material Variants: {stats.materialVariants}\n" +
                      $"Total Materials Created: {stats.totalMaterialsCreated}\n" +
                      $"Shader Warmups: {stats.shaderWarmups}\n" +
                      $"Batched Property Sets: {stats.batchedPropertySets}\n" +
                      $"Warmed Up: {stats.isWarmedUp}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Shader quality levels.
    /// </summary>
    public enum ShaderQualityLevel
    {
        Low,
        Medium,
        High,
        Ultra
    }

    /// <summary>
    /// Shader manager statistics.
    /// </summary>
    [Serializable]
    public struct ShaderManagerStats
    {
        public int loadedShaders;
        public int totalShadersLoaded;
        public int materialVariants;
        public int totalMaterialsCreated;
        public int shaderWarmups;
        public int batchedPropertySets;
        public bool isWarmedUp;
    }

    #endregion
}
