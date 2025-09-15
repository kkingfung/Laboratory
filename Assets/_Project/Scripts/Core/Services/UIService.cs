using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Implementation of IUIService that provides UI system management and operations.
    /// Integrates with the unified event system and service architecture.
    /// </summary>
    public class UIService : IUIService, IDisposable
    {
        #region Fields
        
        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, GameObject> _openScreens = new();
        private readonly Dictionary<string, GameObject> _cachedPrefabs = new();
        private UIConfiguration _configuration = new();
        private Canvas? _mainCanvas;
        private UIStatistics _statistics = new();
        private bool _isInitialized = false;
        private bool _disposed = false;
        
        #endregion
        
        #region Properties
        
        public bool IsInitialized => _isInitialized;
        public Canvas? MainCanvas => _mainCanvas;
        
        #endregion
        
        #region Events
        
        public event Action? OnUIServiceInitialized;
        public event Action<string>? OnScreenOpened;
        public event Action<string>? OnScreenClosed;
        
        #endregion
        
        #region Constructor
        
        public UIService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        
        #endregion
        
        #region IUIService Implementation
        
        public async UniTask InitializeAsync(UIConfiguration config, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            
            _configuration = config ?? new UIConfiguration();
            
            Debug.Log("[UIService] Initializing UI service");
            
            // Setup main canvas
            await SetupMainCanvasAsync();
            
            // Setup event system
            await SetupEventSystemAsync();
            
            // Preload common prefabs if specified
            if (_configuration.CommonPrefabPaths.Length > 0)
            {
                await PreloadCommonUIPrefabsAsync(cancellation);
            }
            
            _isInitialized = true;
            
            Debug.Log("[UIService] UI service initialized successfully");
            OnUIServiceInitialized?.Invoke();
            _eventBus.Publish(new SystemInitializedEvent("UIService"));
        }
        
        public async UniTask PreloadCommonUIPrefabsAsync(CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            
            Debug.Log("[UIService] Preloading common UI prefabs");
            
            for (int i = 0; i < _configuration.CommonPrefabPaths.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                
                var prefabPath = _configuration.CommonPrefabPaths[i];
                
                try
                {
                    var prefab = await LoadPrefabAsync(prefabPath);
                    if (prefab != null)
                    {
                        _cachedPrefabs[prefabPath] = prefab;
                        Debug.Log($"[UIService] Preloaded prefab: {prefabPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[UIService] Failed to preload prefab: {prefabPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UIService] Error preloading prefab '{prefabPath}': {ex.Message}");
                }
            }
            
            _statistics.CachedPrefabCount = _cachedPrefabs.Count;
            Debug.Log($"[UIService] Preloaded {_cachedPrefabs.Count} UI prefabs");
        }
        
        public async UniTask<GameObject?> OpenScreenAsync(string screenName, object? data = null)
        {
            ThrowIfDisposed();
            
            if (_openScreens.ContainsKey(screenName))
            {
                Debug.LogWarning($"[UIService] Screen '{screenName}' is already open");
                return _openScreens[screenName];
            }
            
            try
            {
                var startTime = Time.realtimeSinceStartup;
                
                // Try to get cached prefab first
                GameObject? prefab = null;
                if (_cachedPrefabs.TryGetValue($"UI/{screenName}", out var cachedPrefab))
                {
                    prefab = cachedPrefab;
                }
                else
                {
                    // Load prefab from resources
                    prefab = await LoadPrefabAsync($"UI/{screenName}");
                }
                
                if (prefab == null)
                {
                    Debug.LogError($"[UIService] Failed to load screen prefab: {screenName}");
                    return null;
                }
                
                // Instantiate the screen
                var screenInstance = UnityEngine.Object.Instantiate(prefab, _mainCanvas?.transform);
                screenInstance.name = screenName;
                
                // Setup screen if it has initialization interface
                if (screenInstance.TryGetComponent<IUIScreen>(out var uiScreen))
                {
                    await uiScreen.InitializeAsync(data);
                }
                
                _openScreens[screenName] = screenInstance;
                
                var openTime = Time.realtimeSinceStartup - startTime;
                UpdateAverageOpenTime(openTime);
                _statistics.TotalScreensOpened++;
                _statistics.OpenScreenCount = _openScreens.Count;
                
                Debug.Log($"[UIService] Opened screen '{screenName}' in {openTime:F2}s");
                OnScreenOpened?.Invoke(screenName);
                _eventBus.Publish(new UIScreenOpenedEvent(screenName));
                
                return screenInstance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error opening screen '{screenName}': {ex.Message}");
                return null;
            }
        }
        
        public async UniTask CloseScreenAsync(string screenName)
        {
            ThrowIfDisposed();
            
            if (!_openScreens.TryGetValue(screenName, out var screenInstance))
            {
                Debug.LogWarning($"[UIService] Screen '{screenName}' is not open");
                return;
            }
            
            try
            {
                // Cleanup screen if it has cleanup interface
                if (screenInstance.TryGetComponent<IUIScreen>(out var uiScreen))
                {
                    await uiScreen.CleanupAsync();
                }
                
                UnityEngine.Object.Destroy(screenInstance);
                _openScreens.Remove(screenName);
                
                _statistics.OpenScreenCount = _openScreens.Count;
                
                Debug.Log($"[UIService] Closed screen '{screenName}'");
                OnScreenClosed?.Invoke(screenName);
                _eventBus.Publish(new UIScreenClosedEvent(screenName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error closing screen '{screenName}': {ex.Message}");
            }
        }
        
        public async UniTask CloseAllScreensAsync()
        {
            ThrowIfDisposed();
            
            var screenNames = new List<string>(_openScreens.Keys);
            
            foreach (var screenName in screenNames)
            {
                await CloseScreenAsync(screenName);
            }
            
            Debug.Log("[UIService] Closed all open screens");
        }
        
        public bool IsScreenOpen(string screenName)
        {
            ThrowIfDisposed();
            return _openScreens.ContainsKey(screenName);
        }
        
        public GameObject? GetOpenScreen(string screenName)
        {
            ThrowIfDisposed();
            return _openScreens.TryGetValue(screenName, out var screen) ? screen : null;
        }
        
        public void SetMainCanvas(Canvas canvas)
        {
            ThrowIfDisposed();
            
            _mainCanvas = canvas;
            Debug.Log($"[UIService] Main canvas set: {canvas?.name}");
        }
        
        public UIStatistics GetStatistics()
        {
            ThrowIfDisposed();
            
            // Update memory usage estimate
            long memoryEstimate = 0;
            foreach (var screen in _openScreens.Values)
            {
                if (screen != null)
                {
                    memoryEstimate += EstimateGameObjectMemory(screen);
                }
            }
            _statistics.MemoryUsage = memoryEstimate;
            
            return _statistics;
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask SetupMainCanvasAsync()
        {
            // Try to find existing main canvas
            var existingCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            
            if (existingCanvas != null)
            {
                _mainCanvas = existingCanvas;
                Debug.Log($"[UIService] Using existing canvas: {existingCanvas.name}");
            }
            else if (_configuration.CreateCanvasIfMissing)
            {
                // Create new main canvas
                var canvasGO = new GameObject(_configuration.MainCanvasName);
                _mainCanvas = canvasGO.AddComponent<Canvas>();
                _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _mainCanvas.sortingOrder = 0;
                
                // Add canvas scaler
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                // Add graphic raycaster
                canvasGO.AddComponent<GraphicRaycaster>();
                
                if (_configuration.DontDestroyOnLoad)
                {
                    UnityEngine.Object.DontDestroyOnLoad(canvasGO);
                }
                
                Debug.Log($"[UIService] Created main canvas: {_configuration.MainCanvasName}");
            }
            
            await UniTask.Yield();
        }
        
        private async UniTask SetupEventSystemAsync()
        {
            // Ensure event system exists
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                
                if (_configuration.DontDestroyOnLoad)
                {
                    UnityEngine.Object.DontDestroyOnLoad(eventSystemGO);
                }
                
                Debug.Log("[UIService] Created EventSystem");
            }
            
            await UniTask.Yield();
        }
        
        private async UniTask<GameObject?> LoadPrefabAsync(string prefabPath)
        {
            try
            {
                // Try to load from Resources
                var request = Resources.LoadAsync<GameObject>(prefabPath);
                await request;
                
                return request.asset as GameObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Failed to load prefab '{prefabPath}': {ex.Message}");
                return null;
            }
        }
        
        private void UpdateAverageOpenTime(float newTime)
        {
            if (_statistics.TotalScreensOpened == 0)
            {
                _statistics.AverageOpenTime = newTime;
            }
            else
            {
                _statistics.AverageOpenTime = (_statistics.AverageOpenTime * (_statistics.TotalScreensOpened - 1) + newTime) / _statistics.TotalScreensOpened;
            }
        }
        
        private long EstimateGameObjectMemory(GameObject obj)
        {
            // Rough estimate based on components
            long estimate = 1024; // Base object overhead
            
            var components = obj.GetComponentsInChildren<Component>();
            estimate += components.Length * 512; // Rough component overhead
            
            return estimate;
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UIService));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                CloseAllScreensAsync().Forget();
                _cachedPrefabs.Clear();
                _openScreens.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error during disposal: {ex.Message}");
            }
            
            _disposed = true;
        }
        
        #endregion
    }
    
    #region Supporting Interfaces and Classes
    
    /// <summary>
    /// Interface for UI screens that need initialization and cleanup.
    /// </summary>
    public interface IUIScreen
    {
        UniTask InitializeAsync(object? data = null);
        UniTask CleanupAsync();
    }
    
    /// <summary>Event fired when a UI screen is opened.</summary>
    public class UIScreenOpenedEvent
    {
        public string ScreenName { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public UIScreenOpenedEvent(string screenName)
        {
            ScreenName = screenName;
        }
    }
    
    /// <summary>Event fired when a UI screen is closed.</summary>
    public class UIScreenClosedEvent
    {
        public string ScreenName { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public UIScreenClosedEvent(string screenName)
        {
            ScreenName = screenName;
        }
    }
    
    #endregion
}
