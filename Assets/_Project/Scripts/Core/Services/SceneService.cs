using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Implementation of ISceneService that handles scene loading and management.
    /// </summary>
    public class SceneService : ISceneService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, AsyncOperation> _preloadedScenes = new();
        private bool _disposed = false;

        public SceneService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public string? CurrentScene => SceneManager.GetActiveScene().name;

        public async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, 
            IProgress<float>? progress = null, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            try
            {
                _eventBus.Publish(new SceneChangeRequestedEvent(sceneName, mode));
                _eventBus.Publish(new LoadingStartedEvent($"Scene:{sceneName}", $"Loading scene '{sceneName}'"));

                var operation = SceneManager.LoadSceneAsync(sceneName, mode);
                if (operation == null)
                {
                    throw new InvalidOperationException($"Failed to start loading scene '{sceneName}'");
                }

                while (!operation.isDone)
                {
                    cancellation.ThrowIfCancellationRequested();
                    progress?.Report(operation.progress);
                    _eventBus.Publish(new LoadingProgressEvent($"Scene:{sceneName}", operation.progress));
                    await UniTask.Yield();
                }

                progress?.Report(1f);
                _eventBus.Publish(new LoadingCompletedEvent($"Scene:{sceneName}", true));
                Debug.Log($"SceneService: Successfully loaded scene '{sceneName}'");
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"Scene:{sceneName}", false, ex.Message));
                Debug.LogError($"SceneService: Failed to load scene '{sceneName}': {ex.Message}");
                throw;
            }
        }

        public async UniTask UnloadSceneAsync(string sceneName, IProgress<float>? progress = null, 
            CancellationToken cancellation = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            if (!IsSceneLoaded(sceneName))
            {
                Debug.LogWarning($"SceneService: Scene '{sceneName}' is not loaded, cannot unload");
                return;
            }

            try
            {
                _eventBus.Publish(new LoadingStartedEvent($"UnloadScene:{sceneName}", $"Unloading scene '{sceneName}'"));

                var operation = SceneManager.UnloadSceneAsync(sceneName);
                if (operation == null)
                {
                    throw new InvalidOperationException($"Failed to start unloading scene '{sceneName}'");
                }

                while (!operation.isDone)
                {
                    cancellation.ThrowIfCancellationRequested();
                    progress?.Report(operation.progress);
                    _eventBus.Publish(new LoadingProgressEvent($"UnloadScene:{sceneName}", operation.progress));
                    await UniTask.Yield();
                }

                progress?.Report(1f);
                _eventBus.Publish(new LoadingCompletedEvent($"UnloadScene:{sceneName}", true));
                Debug.Log($"SceneService: Successfully unloaded scene '{sceneName}'");
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"UnloadScene:{sceneName}", false, ex.Message));
                Debug.LogError($"SceneService: Failed to unload scene '{sceneName}': {ex.Message}");
                throw;
            }
        }

        public async UniTask PreloadSceneAsync(string sceneName, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            if (_preloadedScenes.ContainsKey(sceneName))
            {
                Debug.LogWarning($"SceneService: Scene '{sceneName}' is already preloaded");
                return;
            }

            try
            {
                _eventBus.Publish(new LoadingStartedEvent($"PreloadScene:{sceneName}", $"Preloading scene '{sceneName}'"));

                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (operation == null)
                {
                    throw new InvalidOperationException($"Failed to start preloading scene '{sceneName}'");
                }

                operation.allowSceneActivation = false; // Don't activate the scene yet
                _preloadedScenes[sceneName] = operation;

                while (operation.progress < 0.9f) // Scene is ready but not activated
                {
                    cancellation.ThrowIfCancellationRequested();
                    _eventBus.Publish(new LoadingProgressEvent($"PreloadScene:{sceneName}", operation.progress));
                    await UniTask.Yield();
                }

                _eventBus.Publish(new LoadingCompletedEvent($"PreloadScene:{sceneName}", true));
                Debug.Log($"SceneService: Successfully preloaded scene '{sceneName}'");
            }
            catch (Exception ex)
            {
                _preloadedScenes.Remove(sceneName);
                _eventBus.Publish(new LoadingCompletedEvent($"PreloadScene:{sceneName}", false, ex.Message));
                Debug.LogError($"SceneService: Failed to preload scene '{sceneName}': {ex.Message}");
                throw;
            }
        }

        public void ActivatePreloadedScene(string sceneName)
        {
            ThrowIfDisposed();

            if (!_preloadedScenes.TryGetValue(sceneName, out var operation))
            {
                Debug.LogError($"SceneService: Scene '{sceneName}' is not preloaded, cannot activate");
                return;
            }

            try
            {
                operation.allowSceneActivation = true;
                _preloadedScenes.Remove(sceneName);
                Debug.Log($"SceneService: Activated preloaded scene '{sceneName}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SceneService: Failed to activate preloaded scene '{sceneName}': {ex.Message}");
            }
        }

        public bool IsSceneLoaded(string sceneName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(sceneName))
                return false;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<string> GetLoadedScenes()
        {
            ThrowIfDisposed();

            var loadedScenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    loadedScenes.Add(scene.name);
                }
            }

            return loadedScenes;
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Cancel any preloaded scenes
            foreach (var kvp in _preloadedScenes)
            {
                try
                {
                    // Clean up preloaded scenes if possible
                    if (IsSceneLoaded(kvp.Key))
                    {
                        SceneManager.UnloadSceneAsync(kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SceneService: Error cleaning up preloaded scene '{kvp.Key}': {ex.Message}");
                }
            }

            _preloadedScenes.Clear();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SceneService));
        }
    }
}
