using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Service interface for UI system management and operations.
    /// Provides unified UI functionality with canvas management, prefab loading, and state handling.
    /// </summary>
    public interface IUIService : IDisposable
    {
        /// <summary>Gets whether the UI service is initialized and ready.</summary>
        bool IsInitialized { get; }
        
        /// <summary>Gets the main game canvas.</summary>
        Canvas? MainCanvas { get; }
        
        /// <summary>Event fired when the UI service is initialized.</summary>
        event Action OnUIServiceInitialized;
        
        /// <summary>Event fired when a UI screen is opened.</summary>
        event Action<string> OnScreenOpened;
        
        /// <summary>Event fired when a UI screen is closed.</summary>
        event Action<string> OnScreenClosed;
        
        /// <summary>Initializes the UI service with the specified configuration.</summary>
        /// <param name="config">UI configuration settings</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when initialization is finished</returns>
        UniTask InitializeAsync(UIConfiguration config, CancellationToken cancellation = default);
        
        /// <summary>Preloads common UI prefabs for faster instantiation.</summary>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when prefabs are preloaded</returns>
        UniTask PreloadCommonUIPrefabsAsync(CancellationToken cancellation = default);
        
        /// <summary>Opens a UI screen by name.</summary>
        /// <param name="screenName">Name of the screen to open</param>
        /// <param name="data">Optional data to pass to the screen</param>
        /// <returns>Task that completes when the screen is opened</returns>
        UniTask<GameObject?> OpenScreenAsync(string screenName, object? data = null);
        
        /// <summary>Closes a UI screen by name.</summary>
        /// <param name="screenName">Name of the screen to close</param>
        /// <returns>Task that completes when the screen is closed</returns>
        UniTask CloseScreenAsync(string screenName);
        
        /// <summary>Closes all open UI screens.</summary>
        /// <returns>Task that completes when all screens are closed</returns>
        UniTask CloseAllScreensAsync();
        
        /// <summary>Checks if a UI screen is currently open.</summary>
        /// <param name="screenName">Name of the screen to check</param>
        /// <returns>True if the screen is open, false otherwise</returns>
        bool IsScreenOpen(string screenName);
        
        /// <summary>Gets a reference to an open UI screen.</summary>
        /// <param name="screenName">Name of the screen</param>
        /// <returns>GameObject reference or null if not found</returns>
        GameObject? GetOpenScreen(string screenName);
        
        /// <summary>Sets the main canvas for UI operations.</summary>
        /// <param name="canvas">Main canvas to use</param>
        void SetMainCanvas(Canvas canvas);
        
        /// <summary>Gets UI system statistics and information.</summary>
        /// <returns>Current UI statistics</returns>
        UIStatistics GetStatistics();
    }
    
    /// <summary>
    /// UI configuration settings.
    /// </summary>
    [System.Serializable]
    public class UIConfiguration
    {
        [Header("Canvas Settings")]
        public string MainCanvasName { get; set; } = "MainCanvas";
        public bool CreateCanvasIfMissing { get; set; } = true;
        public bool DontDestroyOnLoad { get; set; } = true;
        
        [Header("Prefab Settings")]
        public string[] CommonPrefabPaths { get; set; } = {
            "UI/LoadingScreen",
            "UI/MessageBox",
            "UI/Notification"
        };
        
        [Header("Performance Settings")]
        public int MaxCachedPrefabs { get; set; } = 50;
        public bool EnableUIPooling { get; set; } = true;
        public float ScreenTransitionDuration { get; set; } = 0.3f;
    }
    
    /// <summary>
    /// UI system statistics and metrics.
    /// </summary>
    public struct UIStatistics
    {
        public int OpenScreenCount { get; set; }
        public int CachedPrefabCount { get; set; }
        public int TotalScreensOpened { get; set; }
        public float AverageOpenTime { get; set; }
        public long MemoryUsage { get; set; }
    }
}
