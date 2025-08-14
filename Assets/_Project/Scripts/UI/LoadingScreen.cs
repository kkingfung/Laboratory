using System;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Manages loading screen UI and asynchronous scene loading operations.
    /// Provides progress tracking, event notifications, and smooth loading transitions.
    /// Supports both automatic and manual scene activation control.
    /// </summary>
    public class LoadingScreen : IDisposable
    {
        #region Fields
        
        /// <summary>
        /// Canvas group controlling the loading screen visibility
        /// </summary>
        private readonly CanvasGroup _loadingCanvasGroup;
        
        /// <summary>
        /// UI slider component for displaying loading progress
        /// </summary>
        private readonly UnityEngine.UI.Slider _progressBar;
        
        /// <summary>
        /// Reactive property tracking loading progress from 0 to 1
        /// </summary>
        private readonly ReactiveProperty<float> _progress = new(0f);
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the current loading progress as a reactive property (0 to 1).
        /// </summary>
        public IReadOnlyReactiveProperty<float> Progress => _progress;
        
        /// <summary>
        /// Gets a value indicating whether the loading screen is currently visible.
        /// </summary>
        public bool IsVisible => _loadingCanvasGroup.alpha > 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event fired when a loading operation begins.
        /// </summary>
        public event Action OnLoadStarted;
        
        /// <summary>
        /// Event fired when a loading operation completes successfully.
        /// </summary>
        public event Action OnLoadCompleted;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the LoadingScreen class.
        /// </summary>
        /// <param name="loadingCanvasGroup">Canvas group controlling loading UI visibility</param>
        /// <param name="progressBar">Optional UI slider for progress display</param>
        /// <exception cref="ArgumentNullException">Thrown when loadingCanvasGroup is null</exception>
        public LoadingScreen(CanvasGroup loadingCanvasGroup, UnityEngine.UI.Slider progressBar = null)
        {
            _loadingCanvasGroup = loadingCanvasGroup ?? throw new ArgumentNullException(nameof(loadingCanvasGroup));
            _progressBar = progressBar;
            
            Initialize();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Shows the loading screen UI with full opacity and interaction enabled.
        /// </summary>
        public void Show()
        {
            SetCanvasGroupState(true);
        }
        
        /// <summary>
        /// Hides the loading screen UI with zero opacity and interaction disabled.
        /// </summary>
        public void Hide()
        {
            SetCanvasGroupState(false);
        }
        
        /// <summary>
        /// Loads a scene asynchronously with progress tracking and UI management.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <returns>Task representing the asynchronous loading operation</returns>
        /// <exception cref="ArgumentException">Thrown when sceneName is null or empty</exception>
        public async Task LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            Show();
            OnLoadStarted?.Invoke();

            try
            {
                await PerformSceneLoadAsync(sceneName);
            }
            finally
            {
                OnLoadCompleted?.Invoke();
                Hide();
            }
        }
        
        /// <summary>
        /// Updates the loading progress manually.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1</param>
        public void SetProgress(float progress)
        {
            _progress.Value = Mathf.Clamp01(progress);
        }
        
        /// <summary>
        /// Releases all resources used by the LoadingScreen instance.
        /// </summary>
        public void Dispose()
        {
            _progress?.Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes the loading screen components and bindings.
        /// </summary>
        private void Initialize()
        {
            BindProgressBar();
            Hide(); // Start hidden
        }
        
        /// <summary>
        /// Binds the progress reactive property to the UI progress bar.
        /// </summary>
        private void BindProgressBar()
        {
            if (_progressBar != null)
            {
                _progress.Subscribe(value => _progressBar.value = value);
            }
        }
        
        /// <summary>
        /// Sets the canvas group visibility and interaction state.
        /// </summary>
        /// <param name="visible">Whether the canvas group should be visible and interactive</param>
        private void SetCanvasGroupState(bool visible)
        {
            _loadingCanvasGroup.alpha = visible ? 1f : 0f;
            _loadingCanvasGroup.blocksRaycasts = visible;
            _loadingCanvasGroup.interactable = visible;
        }
        
        /// <summary>
        /// Performs the actual asynchronous scene loading with progress updates.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <returns>Task representing the loading operation</returns>
        private async Task PerformSceneLoadAsync(string sceneName)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;

            // Track loading progress
            while (!asyncOperation.isDone)
            {
                // Unity reports progress up to 0.9 before scene activation
                float normalizedProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                _progress.Value = normalizedProgress;

                // Check if scene is ready for activation
                if (asyncOperation.progress >= 0.9f)
                {
                    _progress.Value = 1f;
                    
                    // Allow scene activation
                    asyncOperation.allowSceneActivation = true;
                }

                await Task.Yield();
            }
        }
        
        #endregion
    }
}
