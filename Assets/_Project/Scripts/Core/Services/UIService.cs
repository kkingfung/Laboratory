using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Basic implementation of the UI service for managing UI screens and canvases.
    /// </summary>
    public class UIService : IUIService
    {
        #region Fields
        
        private UIConfiguration? _config;
        private Canvas? _mainCanvas;
        private readonly Dictionary<string, GameObject> _openScreens = new();
        private readonly Dictionary<string, GameObject> _prefabCache = new();
        private bool _isInitialized = false;
        private int _totalScreensOpened = 0;
        private float _totalOpenTime = 0f;
        
        #endregion
        
        #region Events
        
        public event Action? OnUIServiceInitialized;
        public event Action<string>? OnScreenOpened;
        public event Action<string>? OnScreenClosed;
        
        #endregion
        
        #region Properties
        
        public bool IsInitialized => _isInitialized;
        public Canvas? MainCanvas => _mainCanvas;
        
        #endregion
        
        #region Public Methods
        
        public async UniTask InitializeAsync(UIConfiguration config, CancellationToken cancellation = default)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[UIService] Already initialized");
                return;
            }
            
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            Debug.Log("[UIService] Initializing UI service...");
            
            // Find or create main canvas
            await SetupMainCanvas();
            
            // Setup UI hierarchy if needed
            SetupUIHierarchy();
            
            _isInitialized = true;
            
            Debug.Log("[UIService] UI service initialized successfully");
            OnUIServiceInitialized?.Invoke();
        }
        
        public async UniTask PreloadCommonUIPrefabsAsync(CancellationToken cancellation = default)
        {
            if (_config?.CommonPrefabPaths == null) return;
            
            Debug.Log($"[UIService] Preloading {_config.CommonPrefabPaths.Length} UI prefabs...");
            
            foreach (var prefabPath in _config.CommonPrefabPaths)
            {
                if (cancellation.IsCancellationRequested) break;
                
                try
                {
                    var prefab = await LoadPrefabAsync(prefabPath);
                    if (prefab != null)
                    {
                        _prefabCache[prefabPath] = prefab;
                        Debug.Log($"[UIService] Preloaded prefab: {prefabPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UIService] Failed to preload prefab {prefabPath}: {ex.Message}");
                }
                
                await UniTask.Yield();
            }
            
            Debug.Log($"[UIService] Prefab preloading complete. Cached: {_prefabCache.Count}");
        }
        
        public async UniTask<GameObject?> OpenScreenAsync(string screenName, object? data = null)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[UIService] Cannot open screen - service not initialized");
                return null;
            }
            
            if (_openScreens.ContainsKey(screenName))
            {
                Debug.LogWarning($"[UIService] Screen '{screenName}' is already open");
                return _openScreens[screenName];
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Try to find prefab in cache first
                GameObject? prefab = null;
                if (_prefabCache.ContainsKey($"UI/{screenName}"))
                {
                    prefab = _prefabCache[$"UI/{screenName}"];
                }
                else
                {
                    prefab = await LoadPrefabAsync($"UI/{screenName}");
                }
                
                if (prefab == null)
                {
                    Debug.LogError($"[UIService] Failed to load prefab for screen '{screenName}'");
                    return null;
                }
                
                // Instantiate the screen
                var screenInstance = UnityEngine.Object.Instantiate(prefab, GetScreenParent(screenName));
                screenInstance.name = screenName;
                
                // Pass data to screen if it has a data receiver
                if (data != null)
                {
                    var dataReceiver = screenInstance.GetComponent<IUIDataReceiver>();
                    dataReceiver?.ReceiveData(data);
                }
                
                _openScreens[screenName] = screenInstance;
                _totalScreensOpened++;
                
                var openTime = Time.realtimeSinceStartup - startTime;
                _totalOpenTime += openTime;
                
                Debug.Log($"[UIService] Opened screen '{screenName}' in {openTime:F3}s");
                OnScreenOpened?.Invoke(screenName);
                
                return screenInstance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Failed to open screen '{screenName}': {ex}");
                return null;
            }
        }
        
        public async UniTask CloseScreenAsync(string screenName)
        {
            if (!_openScreens.TryGetValue(screenName, out var screen))
            {
                Debug.LogWarning($"[UIService] Screen '{screenName}' is not open");
                return;
            }
            
            try
            {
                // Notify screen it's closing
                var closable = screen.GetComponent<IUIClosable>();
                if (closable != null)
                {
                    await closable.OnCloseAsync();
                }
                
                UnityEngine.Object.Destroy(screen);
                _openScreens.Remove(screenName);
                
                Debug.Log($"[UIService] Closed screen '{screenName}'");
                OnScreenClosed?.Invoke(screenName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Failed to close screen '{screenName}': {ex}");
            }
        }
        
        public async UniTask CloseAllScreensAsync()
        {
            var screenNames = new List<string>(_openScreens.Keys);
            
            foreach (var screenName in screenNames)
            {
                await CloseScreenAsync(screenName);
            }
        }
        
        public bool IsScreenOpen(string screenName)
        {
            return _openScreens.ContainsKey(screenName);
        }
        
        public GameObject? GetOpenScreen(string screenName)
        {
            return _openScreens.TryGetValue(screenName, out var screen) ? screen : null;
        }
        
        public void SetMainCanvas(Canvas canvas)
        {
            _mainCanvas = canvas;
            Debug.Log($"[UIService] Main canvas set to '{canvas.name}'");
        }
        
        public UIStatistics GetStatistics()
        {
            return new UIStatistics
            {
                OpenScreenCount = _openScreens.Count,
                CachedPrefabCount = _prefabCache.Count,
                TotalScreensOpened = _totalScreensOpened,
                AverageOpenTime = _totalScreensOpened > 0 ? _totalOpenTime / _totalScreensOpened : 0f,
                MemoryUsage = GC.GetTotalMemory(false)
            };
        }
        
        public void Dispose()
        {
            if (!_isInitialized) return;
            
            Debug.Log("[UIService] Disposing UI service...");
            
            // Close all screens
            CloseAllScreensAsync().Forget();
            
            // Clear caches
            _prefabCache.Clear();
            _openScreens.Clear();
            
            // Clear events
            OnUIServiceInitialized = null;
            OnScreenOpened = null;
            OnScreenClosed = null;
            
            _isInitialized = false;
            
            Debug.Log("[UIService] UI service disposed");
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask SetupMainCanvas()
        {
            // Try to find existing canvas
            _mainCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            
            if (_mainCanvas == null && _config!.CreateCanvasIfMissing)
            {
                Debug.Log("[UIService] Creating main canvas...");
                
                var canvasGO = new GameObject(_config.MainCanvasName);
                _mainCanvas = canvasGO.AddComponent<Canvas>();
                _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _mainCanvas.sortingOrder = 0;
                
                // Add canvas scaler
                var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                // Add graphic raycaster
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                if (_config.DontDestroyOnLoad)
                {
                    UnityEngine.Object.DontDestroyOnLoad(canvasGO);
                }
                
                Debug.Log("[UIService] Main canvas created successfully");
            }
            else if (_mainCanvas != null)
            {
                Debug.Log($"[UIService] Using existing canvas '{_mainCanvas.name}'");
            }
            else
            {
                Debug.LogError("[UIService] No main canvas found and creation is disabled");
            }
            
            await UniTask.Yield();
        }
        
        private void SetupUIHierarchy()
        {
            if (_mainCanvas == null) return;
            
            var layers = new string[]
            {
                "BackgroundLayer",
                "GameplayLayer",
                "MenuLayer",
                "OverlayLayer",
                "ModalLayer"
            };
            
            foreach (var layerName in layers)
            {
                if (_mainCanvas.transform.Find(layerName) == null)
                {
                    var layerGO = new GameObject(layerName);
                    layerGO.transform.SetParent(_mainCanvas.transform, false);
                    
                    var rectTransform = layerGO.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }
        
        private Transform GetScreenParent(string screenName)
        {
            if (_mainCanvas == null) return null!;
            
            // Determine appropriate layer based on screen name
            string layerName = screenName.ToLower() switch
            {
                var name when name.Contains("background") => "BackgroundLayer",
                var name when name.Contains("gameplay") || name.Contains("hud") => "GameplayLayer",
                var name when name.Contains("menu") => "MenuLayer",
                var name when name.Contains("modal") || name.Contains("dialog") => "ModalLayer",
                _ => "OverlayLayer"
            };
            
            var layer = _mainCanvas.transform.Find(layerName);
            return layer ?? _mainCanvas.transform;
        }
        
        private async UniTask<GameObject?> LoadPrefabAsync(string prefabPath)
        {
            try
            {
                // Try Resources.Load first
                var prefab = Resources.Load<GameObject>(prefabPath);
                if (prefab != null)
                {
                    return prefab;
                }
                
                // Try Addressables if available (placeholder for now)
                Debug.LogWarning($"[UIService] Could not load prefab at path '{prefabPath}' - implement Addressable loading if needed");
                
                await UniTask.Yield();
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error loading prefab '{prefabPath}': {ex}");
                return null;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Interface for UI components that can receive data when opened.
    /// </summary>
    public interface IUIDataReceiver
    {
        void ReceiveData(object data);
    }
    
    /// <summary>
    /// Interface for UI components that need custom close handling.
    /// </summary>
    public interface IUIClosable
    {
        UniTask OnCloseAsync();
    }
}
