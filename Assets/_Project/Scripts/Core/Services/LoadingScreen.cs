using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Laboratory.Core.Timing;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.Infrastructure;

#nullable enable
using Laboratory.Core.Services;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Enhanced loading screen that integrates with the unified service architecture.
    /// Provides progress tracking, event notifications, and smooth loading transitions.
    /// Now supports multiple loading sources and better error handling.
    /// </summary>
    public class LoadingScreen : IDisposable
    {
        #region Fields
        
        private readonly CanvasGroup _loadingCanvasGroup;
        private readonly UnityEngine.UI.Slider? _progressBar;
        private readonly UnityEngine.UI.Text? _statusText;
        private readonly ProgressTimer _progressTimer;
        private readonly List<IDisposable> _disposables = new();
        
        private IEventBus? _eventBus;
        private ISceneService? _sceneService;
        private bool _disposed = false;
        
        #endregion
        
        #region Properties
        
        public float Progress => _progressTimer?.Progress ?? 0f;
        public bool IsVisible => _loadingCanvasGroup.alpha > 0f;
        public string? CurrentStatus { get; private set; }
        
        #endregion
        
        #region Events
        
        public event Action? OnLoadStarted;
        public event Action<string>? OnStatusChanged;
        public event Action? OnLoadCompleted;
        public event Action<Exception>? OnLoadFailed;
        
        #endregion
        
        #region Constructor
        
        public LoadingScreen(CanvasGroup loadingCanvasGroup, 
                           UnityEngine.UI.Slider? progressBar = null,
                           UnityEngine.UI.Text? statusText = null)
        {
            _loadingCanvasGroup = loadingCanvasGroup ?? throw new ArgumentNullException(nameof(loadingCanvasGroup));
            _progressBar = progressBar;
            _statusText = statusText;
            _progressTimer = new ProgressTimer(duration: 1f, autoProgress: false, autoRegister: false);
            
            Initialize();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Shows the loading screen with optional status message.
        /// </summary>
        public void Show(string? statusMessage = null)
        {
            ThrowIfDisposed();
            
            SetCanvasGroupState(true);
            UpdateStatus(statusMessage ?? "Loading...");
            SetProgress(0f);
        }
        
        /// <summary>
        /// Hides the loading screen.
        /// </summary>
        public void Hide()
        {
            if (_disposed) return;
            
            SetCanvasGroupState(false);
            UpdateStatus(null);
        }
        
        /// <summary>
        /// Loads a scene using the unified service architecture.
        /// </summary>
        public async UniTask LoadSceneAsync(string sceneName, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            Show($"Loading {sceneName}...");
            OnLoadStarted?.Invoke();

            try
            {
                if (_sceneService != null)
                {
                    // Use the service-based approach
                    var progress = new Progress<float>(SetProgress);
                    await _sceneService.LoadSceneAsync(sceneName, LoadSceneMode.Single, progress, cancellation);
                }
                else
                {
                    // Fallback to direct Unity API
                    await PerformDirectSceneLoadAsync(sceneName, cancellation);
                }
                
                UpdateStatus("Loading complete!");
                OnLoadCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadingScreen: Failed to load scene '{sceneName}': {ex.Message}");
                UpdateStatus($"Loading failed: {ex.Message}");
                OnLoadFailed?.Invoke(ex);
                throw;
            }
            finally
            {
                // Small delay to show completion message
                await UniTask.Delay(500, cancellationToken: cancellation);
                Hide();
            }
        }
        
        /// <summary>
        /// Loads multiple assets with progress tracking.
        /// </summary>
        public async UniTask LoadAssetsAsync(string[] assetKeys, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            
            if (assetKeys == null || assetKeys.Length == 0)
                return;

            Show("Loading assets...");
            OnLoadStarted?.Invoke();

            try
            {
                var assetService = ServiceContainer.Instance?.ResolveService<IAssetService>();
                if (assetService != null)
                {
                    for (int i = 0; i < assetKeys.Length; i++)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        
                        UpdateStatus($"Loading {assetKeys[i]}...");
                        await assetService.LoadAssetAsync<UnityEngine.Object>(assetKeys[i]);
                        
                        float progress = (float)(i + 1) / assetKeys.Length;
                        SetProgress(progress);
                    }
                }
                
                OnLoadCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadingScreen: Failed to load assets: {ex.Message}");
                OnLoadFailed?.Invoke(ex);
                throw;
            }
            finally
            {
                Hide();
            }
        }
        
        /// <summary>
        /// Updates the loading progress manually.
        /// </summary>
        public void SetProgress(float progress)
        {
            ThrowIfDisposed();
            _progressTimer.SetProgress(Mathf.Clamp01(progress));
        }
        
        /// <summary>
        /// Updates the status text.
        /// </summary>
        public void UpdateStatus(string? status)
        {
            CurrentStatus = status;
            
            if (_statusText != null)
            {
                _statusText.text = status ?? "";
            }
            
            OnStatusChanged?.Invoke(status ?? "");
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            // Dispose all subscriptions
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
            
            _progressTimer?.Dispose();
            _disposed = true;
        }
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            // Get services if available
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
                _sceneService = serviceContainer.ResolveService<ISceneService>();
            }
            
            BindProgressBar();
            SubscribeToLoadingEvents();
            Hide(); // Start hidden
        }
        
        private void BindProgressBar()
        {
            if (_progressBar != null)
            {
                _progressTimer.OnProgressChanged += (progress) => _progressBar.value = progress;
            }
        }
        
        private void SubscribeToLoadingEvents()
        {
            if (_eventBus != null)
            {
                // Subscribe to global loading events
                var sub1 = _eventBus.Subscribe<LoadingStartedEvent>(OnGlobalLoadingStarted);
                var sub2 = _eventBus.Subscribe<LoadingProgressEvent>(OnGlobalLoadingProgress);
                var sub3 = _eventBus.Subscribe<LoadingCompletedEvent>(OnGlobalLoadingCompleted);
                
                if (sub1 != null) _disposables.Add(sub1);
                if (sub2 != null) _disposables.Add(sub2);
                if (sub3 != null) _disposables.Add(sub3);
            }
        }
        
        private void OnGlobalLoadingStarted(LoadingStartedEvent evt)
        {
            if (!IsVisible)
            {
                Show(evt.Description);
            }
            else
            {
                UpdateStatus(evt.Description);
            }
        }
        
        private void OnGlobalLoadingProgress(LoadingProgressEvent evt)
        {
            SetProgress(evt.Progress);
            if (!string.IsNullOrEmpty(evt.StatusText))
            {
                UpdateStatus(evt.StatusText);
            }
        }
        
        private void OnGlobalLoadingCompleted(LoadingCompletedEvent evt)
        {
            if (evt.Success)
            {
                UpdateStatus($"{evt.OperationName} completed successfully");
            }
            else
            {
                UpdateStatus($"{evt.OperationName} failed: {evt.ErrorMessage}");
            }
        }
        
        private void SetCanvasGroupState(bool visible)
        {
            _loadingCanvasGroup.alpha = visible ? 1f : 0f;
            _loadingCanvasGroup.blocksRaycasts = visible;
            _loadingCanvasGroup.interactable = visible;
        }
        
        private async UniTask PerformDirectSceneLoadAsync(string sceneName, CancellationToken cancellation)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOperation == null)
            {
                throw new InvalidOperationException($"Failed to start loading scene '{sceneName}'");
            }
            
            asyncOperation.allowSceneActivation = false;

            while (!asyncOperation.isDone)
            {
                cancellation.ThrowIfCancellationRequested();
                
                float normalizedProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                SetProgress(normalizedProgress);

                if (asyncOperation.progress >= 0.9f)
                {
                    SetProgress(1f);
                    asyncOperation.allowSceneActivation = true;
                }

                await UniTask.Yield();
            }
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LoadingScreen));
        }
        
        #endregion
    }
}
