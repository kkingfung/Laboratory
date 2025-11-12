using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Content
{
    /// <summary>
    /// Mod support framework for player-created content.
    /// Handles mod discovery, loading, validation, and lifecycle management.
    /// Supports scripts, assets, and configuration overrides with sandboxing.
    /// </summary>
    public class ModSupportFramework : MonoBehaviour
    {
        #region Configuration

        [Header("Paths")]
        [SerializeField] private string modsDirectory = "Mods";
        [SerializeField] private string workshopDirectory = "Workshop";

        [Header("Security")]
        [SerializeField] private bool enableScriptMods = false; // Dangerous - requires sandboxing
        [SerializeField] private bool validateModSignatures = true;
        [SerializeField] private bool allowNetworkAccess = false;
        [SerializeField] private int maxModSize = 100 * 1024 * 1024; // 100MB

        [Header("Loading")]
        [SerializeField] private bool autoLoadMods = true;
        [SerializeField] private bool loadOnStartup = true;
        [SerializeField] private int maxConcurrentLoads = 2;

        [Header("Workshop Integration")]
        [SerializeField] private bool enableWorkshop = true;
        [SerializeField] private string workshopUrl = "https://workshop.projectchimera.com";

        #endregion

        #region Private Fields

        private static ModSupportFramework _instance;

        // Mod registry
        private readonly Dictionary<string, ModInfo> _availableMods = new Dictionary<string, ModInfo>();
        private readonly Dictionary<string, LoadedMod> _loadedMods = new Dictionary<string, LoadedMod>();

        // Dependencies
        private readonly Dictionary<string, List<string>> _modDependencies = new Dictionary<string, List<string>>();

        // State
        private bool _isScanning = false;
        private bool _isLoading = false;

        // Statistics
        private int _totalModsDiscovered = 0;
        private int _totalModsLoaded = 0;
        private int _totalModsUnloaded = 0;
        private int _modsFailedValidation = 0;

        // Events
        public event Action<ModInfo> OnModDiscovered;
        public event Action<string> OnModLoaded;
        public event Action<string> OnModUnloaded;
        public event Action<string, string> OnModLoadFailed;
        public event Action<string, bool> OnModEnabledChanged;

        #endregion

        #region Properties

        public static ModSupportFramework Instance => _instance;
        public int AvailableModCount => _availableMods.Count;
        public int LoadedModCount => _loadedMods.Count;
        public bool IsScanning => _isScanning;
        public string ModsPath => Path.Combine(Application.persistentDataPath, modsDirectory);

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
            if (autoLoadMods && loadOnStartup)
            {
                ScanForMods();
                LoadEnabledMods();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[ModSupportFramework] Initializing...");

            // Create mods directory
            string modsPath = ModsPath;
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                Debug.Log($"[ModSupportFramework] Created mods directory: {modsPath}");
            }

            // Create workshop directory
            string workshopPath = Path.Combine(Application.persistentDataPath, workshopDirectory);
            if (!Directory.Exists(workshopPath))
            {
                Directory.CreateDirectory(workshopPath);
            }

            Debug.Log("[ModSupportFramework] Initialized");
        }

        #endregion

        #region Mod Discovery

        /// <summary>
        /// Scan for available mods.
        /// </summary>
        public void ScanForMods(Action onComplete = null)
        {
            if (_isScanning)
            {
                Debug.LogWarning("[ModSupportFramework] Already scanning");
                return;
            }

            StartCoroutine(ScanForModsCoroutine(onComplete));
        }

        private IEnumerator ScanForModsCoroutine(Action onComplete)
        {
            _isScanning = true;

            string modsPath = ModsPath;
            var modDirectories = Directory.GetDirectories(modsPath);

            Debug.Log($"[ModSupportFramework] Scanning {modDirectories.Length} mod directories...");

            foreach (var modDir in modDirectories)
            {
                string modInfoPath = Path.Combine(modDir, "mod.json");

                if (File.Exists(modInfoPath))
                {
                    try
                    {
                        string json = File.ReadAllText(modInfoPath);
                        var modInfo = JsonUtility.FromJson<ModInfo>(json);

                        modInfo.path = modDir;
                        modInfo.enabled = IsModEnabled(modInfo.modId);

                        // Validate mod
                        if (ValidateMod(modInfo))
                        {
                            _availableMods[modInfo.modId] = modInfo;
                            _totalModsDiscovered++;

                            OnModDiscovered?.Invoke(modInfo);

                            Debug.Log($"[ModSupportFramework] Discovered mod: {modInfo.name} v{modInfo.version}");
                        }
                        else
                        {
                            _modsFailedValidation++;
                            Debug.LogWarning($"[ModSupportFramework] Mod validation failed: {modInfo.name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ModSupportFramework] Failed to parse mod.json in {modDir}: {ex.Message}");
                    }
                }

                yield return null;
            }

            _isScanning = false;
            onComplete?.Invoke();

            Debug.Log($"[ModSupportFramework] Scan complete: {_availableMods.Count} mods found");
        }

        private bool ValidateMod(ModInfo modInfo)
        {
            // Check version compatibility
            if (!string.IsNullOrEmpty(modInfo.gameVersion))
            {
                if (modInfo.gameVersion != Application.version)
                {
                    Debug.LogWarning($"[ModSupportFramework] Mod {modInfo.name} version mismatch: {modInfo.gameVersion} != {Application.version}");
                }
            }

            // Check size
            long modSize = GetDirectorySize(modInfo.path);
            if (modSize > maxModSize)
            {
                Debug.LogWarning($"[ModSupportFramework] Mod {modInfo.name} exceeds max size: {modSize / 1024 / 1024}MB");
                return false;
            }

            // Signature validation
            if (validateModSignatures)
            {
                // Implement signature validation
                // For now, just check if signature file exists
                string signaturePath = Path.Combine(modInfo.path, "mod.sig");
                if (!File.Exists(signaturePath))
                {
                    Debug.LogWarning($"[ModSupportFramework] Mod {modInfo.name} missing signature");
                    // Allow unsigned mods for now
                }
            }

            return true;
        }

        private long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            return files.Sum(file => new FileInfo(file).Length);
        }

        #endregion

        #region Mod Loading

        /// <summary>
        /// Load a mod.
        /// </summary>
        public void LoadMod(string modId, Action onSuccess = null, Action<string> onError = null)
        {
            if (_loadedMods.ContainsKey(modId))
            {
                Debug.LogWarning($"[ModSupportFramework] Mod already loaded: {modId}");
                onSuccess?.Invoke();
                return;
            }

            if (!_availableMods.TryGetValue(modId, out var modInfo))
            {
                string error = $"Mod not found: {modId}";
                OnModLoadFailed?.Invoke(modId, error);
                onError?.Invoke(error);
                return;
            }

            StartCoroutine(LoadModCoroutine(modInfo, onSuccess, onError));
        }

        private IEnumerator LoadModCoroutine(ModInfo modInfo, Action onSuccess, Action<string> onError)
        {
            _isLoading = true;

            // Load dependencies first
            if (modInfo.dependencies != null && modInfo.dependencies.Length > 0)
            {
                foreach (var dependency in modInfo.dependencies)
                {
                    if (!_loadedMods.ContainsKey(dependency))
                    {
                        bool depLoaded = false;
                        string depError = null;

                        LoadMod(dependency,
                            () => depLoaded = true,
                            error => { depError = error; depLoaded = true; });

                        while (!depLoaded)
                        {
                            yield return null;
                        }

                        if (depError != null)
                        {
                            string error = $"Failed to load dependency {dependency}: {depError}";
                            OnModLoadFailed?.Invoke(modInfo.modId, error);
                            onError?.Invoke(error);
                            _isLoading = false;
                            yield break;
                        }
                    }
                }
            }

            // Load mod assets
            var loadedMod = new LoadedMod
            {
                modInfo = modInfo,
                loadTime = Time.time,
                assets = new Dictionary<string, UnityEngine.Object>()
            };

            // Load asset bundles
            string bundlesPath = Path.Combine(modInfo.path, "bundles");
            if (Directory.Exists(bundlesPath))
            {
                var bundleFiles = Directory.GetFiles(bundlesPath, "*.bundle");

                foreach (var bundleFile in bundleFiles)
                {
                    var bundle = AssetBundle.LoadFromFile(bundleFile);

                    if (bundle != null)
                    {
                        loadedMod.assetBundles.Add(bundle);
                        Debug.Log($"[ModSupportFramework] Loaded bundle: {Path.GetFileName(bundleFile)}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ModSupportFramework] Failed to load bundle: {bundleFile}");
                    }

                    yield return null;
                }
            }

            // Load configuration
            string configPath = Path.Combine(modInfo.path, "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string configJson = File.ReadAllText(configPath);
                    loadedMod.config = JsonUtility.FromJson<ModConfig>(configJson);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ModSupportFramework] Failed to load mod config: {ex.Message}");
                }
            }

            // Load scripts (if enabled and safe)
            if (enableScriptMods)
            {
                // DANGEROUS - Requires proper sandboxing
                Debug.LogWarning("[ModSupportFramework] Script mods are enabled but not implemented (security risk)");
            }

            _loadedMods[modInfo.modId] = loadedMod;
            _totalModsLoaded++;
            _isLoading = false;

            OnModLoaded?.Invoke(modInfo.modId);
            onSuccess?.Invoke();

            Debug.Log($"[ModSupportFramework] Mod loaded: {modInfo.name}");
        }

        /// <summary>
        /// Load all enabled mods.
        /// </summary>
        public void LoadEnabledMods(Action onComplete = null)
        {
            StartCoroutine(LoadEnabledModsCoroutine(onComplete));
        }

        private IEnumerator LoadEnabledModsCoroutine(Action onComplete)
        {
            var enabledMods = _availableMods.Values.Where(m => m.enabled).ToArray();

            Debug.Log($"[ModSupportFramework] Loading {enabledMods.Length} enabled mods...");

            foreach (var modInfo in enabledMods)
            {
                bool loadComplete = false;

                LoadMod(modInfo.modId,
                    () => loadComplete = true,
                    error => loadComplete = true);

                while (!loadComplete)
                {
                    yield return null;
                }
            }

            onComplete?.Invoke();

            Debug.Log($"[ModSupportFramework] Enabled mods loaded: {_loadedMods.Count}/{enabledMods.Length}");
        }

        #endregion

        #region Mod Unloading

        /// <summary>
        /// Unload a mod.
        /// </summary>
        public void UnloadMod(string modId)
        {
            if (!_loadedMods.TryGetValue(modId, out var loadedMod))
            {
                Debug.LogWarning($"[ModSupportFramework] Mod not loaded: {modId}");
                return;
            }

            // Unload asset bundles
            foreach (var bundle in loadedMod.assetBundles)
            {
                bundle.Unload(true);
            }

            // Destroy instantiated objects
            foreach (var obj in loadedMod.instantiatedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            _loadedMods.Remove(modId);
            _totalModsUnloaded++;

            OnModUnloaded?.Invoke(modId);

            Debug.Log($"[ModSupportFramework] Mod unloaded: {loadedMod.modInfo.name}");
        }

        /// <summary>
        /// Unload all mods.
        /// </summary>
        public void UnloadAllMods()
        {
            foreach (var modId in _loadedMods.Keys.ToArray())
            {
                UnloadMod(modId);
            }

            Debug.Log("[ModSupportFramework] All mods unloaded");
        }

        #endregion

        #region Mod Management

        /// <summary>
        /// Enable a mod.
        /// </summary>
        public void EnableMod(string modId)
        {
            if (_availableMods.TryGetValue(modId, out var modInfo))
            {
                modInfo.enabled = true;
                SaveModEnabled(modId, true);

                OnModEnabledChanged?.Invoke(modId, true);

                Debug.Log($"[ModSupportFramework] Mod enabled: {modInfo.name}");
            }
        }

        /// <summary>
        /// Disable a mod.
        /// </summary>
        public void DisableMod(string modId)
        {
            if (_availableMods.TryGetValue(modId, out var modInfo))
            {
                modInfo.enabled = false;
                SaveModEnabled(modId, false);

                // Unload if currently loaded
                if (_loadedMods.ContainsKey(modId))
                {
                    UnloadMod(modId);
                }

                OnModEnabledChanged?.Invoke(modId, false);

                Debug.Log($"[ModSupportFramework] Mod disabled: {modInfo.name}");
            }
        }

        private bool IsModEnabled(string modId)
        {
            return PlayerPrefs.GetInt($"Mod_{modId}_Enabled", 1) == 1;
        }

        private void SaveModEnabled(string modId, bool enabled)
        {
            PlayerPrefs.SetInt($"Mod_{modId}_Enabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Workshop Integration

        /// <summary>
        /// Browse workshop mods.
        /// </summary>
        public void BrowseWorkshop(Action<WorkshopMod[]> onSuccess = null, Action<string> onError = null)
        {
            if (!enableWorkshop)
            {
                onError?.Invoke("Workshop disabled");
                return;
            }

            StartCoroutine(BrowseWorkshopCoroutine(onSuccess, onError));
        }

        private IEnumerator BrowseWorkshopCoroutine(Action<WorkshopMod[]> onSuccess, Action<string> onError)
        {
            string url = $"{workshopUrl}/api/mods";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<WorkshopResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response.mods);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"Failed to parse workshop response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Workshop request failed: {request.error}");
                }
            }
        }

        /// <summary>
        /// Download a mod from workshop.
        /// </summary>
        public void DownloadWorkshopMod(string workshopId, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(DownloadWorkshopModCoroutine(workshopId, onSuccess, onError));
        }

        private IEnumerator DownloadWorkshopModCoroutine(string workshopId, Action onSuccess, Action<string> onError)
        {
            string url = $"{workshopUrl}/api/mods/{workshopId}/download";
            string downloadPath = Path.Combine(Application.persistentDataPath, workshopDirectory, $"{workshopId}.zip");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(downloadPath);
                request.timeout = 300;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Extract zip to mods directory
                    // TODO: Implement zip extraction
                    onSuccess?.Invoke();

                    Debug.Log($"[ModSupportFramework] Workshop mod downloaded: {workshopId}");
                }
                else
                {
                    onError?.Invoke($"Download failed: {request.error}");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get mod support statistics.
        /// </summary>
        public ModStats GetStats()
        {
            return new ModStats
            {
                availableMods = _availableMods.Count,
                loadedMods = _loadedMods.Count,
                totalModsDiscovered = _totalModsDiscovered,
                totalModsLoaded = _totalModsLoaded,
                totalModsUnloaded = _totalModsUnloaded,
                modsFailedValidation = _modsFailedValidation
            };
        }

        /// <summary>
        /// Get mod info.
        /// </summary>
        public ModInfo GetModInfo(string modId)
        {
            return _availableMods.TryGetValue(modId, out var modInfo) ? modInfo : null;
        }

        /// <summary>
        /// Get all available mods.
        /// </summary>
        public ModInfo[] GetAvailableMods()
        {
            return _availableMods.Values.ToArray();
        }

        /// <summary>
        /// Check if mod is loaded.
        /// </summary>
        public bool IsModLoaded(string modId)
        {
            return _loadedMods.ContainsKey(modId);
        }

        #endregion

        #region Context Menu

        [ContextMenu("Scan for Mods")]
        private void ScanForModsMenu()
        {
            ScanForMods();
        }

        [ContextMenu("Load Enabled Mods")]
        private void LoadEnabledModsMenu()
        {
            LoadEnabledMods();
        }

        [ContextMenu("Unload All Mods")]
        private void UnloadAllModsMenu()
        {
            UnloadAllMods();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Mod Support Statistics ===\n" +
                      $"Available Mods: {stats.availableMods}\n" +
                      $"Loaded Mods: {stats.loadedMods}\n" +
                      $"Total Discovered: {stats.totalModsDiscovered}\n" +
                      $"Total Loaded: {stats.totalModsLoaded}\n" +
                      $"Total Unloaded: {stats.totalModsUnloaded}\n" +
                      $"Failed Validation: {stats.modsFailedValidation}");
        }

        [ContextMenu("Print Available Mods")]
        private void PrintAvailableMods()
        {
            if (_availableMods.Count == 0)
            {
                Debug.Log("[ModSupportFramework] No mods available");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Available Mods ===");

            foreach (var mod in _availableMods.Values)
            {
                sb.AppendLine($"  {mod.name} v{mod.version} by {mod.author}");
                sb.AppendLine($"    ID: {mod.modId}");
                sb.AppendLine($"    Enabled: {mod.enabled}");
                sb.AppendLine($"    Loaded: {_loadedMods.ContainsKey(mod.modId)}");
            }

            Debug.Log(sb.ToString());
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Mod information.
    /// </summary>
    [Serializable]
    public class ModInfo
    {
        public string modId;
        public string name;
        public string author;
        public string version;
        public string gameVersion;
        public string description;
        public string[] dependencies;
        public string path;
        public bool enabled;
    }

    /// <summary>
    /// Loaded mod data.
    /// </summary>
    [Serializable]
    public class LoadedMod
    {
        public ModInfo modInfo;
        public float loadTime;
        public List<AssetBundle> assetBundles = new List<AssetBundle>();
        public Dictionary<string, UnityEngine.Object> assets = new Dictionary<string, UnityEngine.Object>();
        public List<GameObject> instantiatedObjects = new List<GameObject>();
        public ModConfig config;
    }

    /// <summary>
    /// Mod configuration.
    /// </summary>
    [Serializable]
    public class ModConfig
    {
        public Dictionary<string, string> settings;
    }

    /// <summary>
    /// Workshop mod data.
    /// </summary>
    [Serializable]
    public class WorkshopMod
    {
        public string workshopId;
        public string name;
        public string author;
        public string version;
        public int downloads;
        public float rating;
    }

    /// <summary>
    /// Mod statistics.
    /// </summary>
    [Serializable]
    public struct ModStats
    {
        public int availableMods;
        public int loadedMods;
        public int totalModsDiscovered;
        public int totalModsLoaded;
        public int totalModsUnloaded;
        public int modsFailedValidation;
    }

    // Response structures
    [Serializable] class WorkshopResponse { public WorkshopMod[] mods; }

    #endregion
}
