using System;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

namespace Infrastructure.UI
{
    /// <summary>
    /// Manages the loading screen UI and async scene loading process.
    /// Provides progress updates and show/hide controls.
    /// </summary>
    public class LoadingScreen : IDisposable
    {
        private readonly CanvasGroup _loadingCanvasGroup;
        private readonly UnityEngine.UI.Slider _progressBar;

        private readonly ReactiveProperty<float> _progress = new(0f);

        /// <summary>
        /// Progress of current loading operation (0 to 1).
        /// </summary>
        public IReadOnlyReactiveProperty<float> Progress => _progress;

        /// <summary>
        /// Is loading screen currently visible?
        /// </summary>
        public bool IsVisible => _loadingCanvasGroup.alpha > 0f;

        /// <summary>
        /// Event fired when loading starts.
        /// </summary>
        public event Action? OnLoadStarted;

        /// <summary>
        /// Event fired when loading completes.
        /// </summary>
        public event Action? OnLoadCompleted;

        /// <summary>
        /// Creates a new LoadingScreen controller.
        /// </summary>
        /// <param name="loadingCanvasGroup">CanvasGroup controlling loading UI visibility.</param>
        /// <param name="progressBar">UI Slider used as progress bar (optional).</param>
        public LoadingScreen(CanvasGroup loadingCanvasGroup, UnityEngine.UI.Slider? progressBar = null)
        {
            _loadingCanvasGroup = loadingCanvasGroup ?? throw new ArgumentNullException(nameof(loadingCanvasGroup));
            _progressBar = progressBar;
            _progress.Subscribe(value =>
            {
                if (_progressBar != null)
                    _progressBar.value = value;
            });
        }

        /// <summary>
        /// Show the loading screen UI.
        /// </summary>
        public void Show()
        {
            _loadingCanvasGroup.alpha = 1f;
            _loadingCanvasGroup.blocksRaycasts = true;
            _loadingCanvasGroup.interactable = true;
        }

        /// <summary>
        /// Hide the loading screen UI.
        /// </summary>
        public void Hide()
        {
            _loadingCanvasGroup.alpha = 0f;
            _loadingCanvasGroup.blocksRaycasts = false;
            _loadingCanvasGroup.interactable = false;
        }

        /// <summary>
        /// Loads the scene asynchronously by name, updating progress and showing the loading UI.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        public async Task LoadSceneAsync(string sceneName)
        {
            Show();
            OnLoadStarted?.Invoke();

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                // Unity loads to 0.9 progress before activation
                float progressValue = Mathf.Clamp01(asyncOp.progress / 0.9f);
                _progress.Value = progressValue;

                if (asyncOp.progress >= 0.9f)
                {
                    // Ready to activate scene
                    _progress.Value = 1f;
                    asyncOp.allowSceneActivation = true;
                }

                await Task.Yield();
            }

            OnLoadCompleted?.Invoke();
            Hide();
        }

        public void Dispose()
        {
            _progress?.Dispose();
        }
    }
}
