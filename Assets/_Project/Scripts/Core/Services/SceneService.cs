using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

#nullable enable

namespace Laboratory.Core.Services
{
    #region Scene Service

    /// <summary>
    /// Implementation of ISceneService with improved async operations and event publishing.
    /// </summary>
    public class SceneService : ISceneService
    {
        #region Fields

        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, AsyncOperation> _preloadedScenes = new();

        #endregion

        #region Constructor

        public SceneService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        #endregion

        #region Properties

        public string? CurrentScene => SceneManager.GetActiveScene().name;

        #endregion

        #region ISceneService Implementation

        public async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, 
            IProgress<float>? progress = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            _eventBus.Publish(new SceneChangeRequestedEvent(sceneName, mode));
            _eventBus.Publish(new LoadingStartedEvent($"Scene:{sceneName}", $"Loading scene: {sceneName}"));

            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneName, mode);
                if (operation == null)
                    throw new InvalidOperationException($"Failed to start loading scene '{sceneName}'");

                operation.allowSceneActivation = false;

                while (!operation.isDone)
                {
                    cancellation.ThrowIfCancellationRequested();

                    float prog = Mathf.Clamp01(operation.progress / 0.9f);
                    progress?.Report(prog);
                    _eventBus.Publish(new LoadingProgressEvent($"Scene:{sceneName}", prog));

                    if (operation.progress >= 0.9f)
                        operation.allowSceneActivation = true;

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
                }

                progress?.Report(1f);
                _eventBus.Publish(new LoadingCompletedEvent($"Scene:{sceneName}", true));
                
                Debug.Log($"SceneService: Successfully loaded scene '{sceneName}'");
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"Scene:{sceneName}", false, ex.Message));
                Debug.LogError($"SceneService: Failed to load scene '{sceneName}': {ex}");
                throw;
            }
        }

        public async UniTask UnloadSceneAsync(string sceneName, IProgress<float>? progress = null, 
            CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            if (!IsSceneLoaded(sceneName))
            {
                Debug.LogWarning($"SceneService: Scene '{sceneName}' is not loaded");
                return;
            }

            _eventBus.Publish(new LoadingStartedEvent($"UnloadScene:{sceneName}", $"Unloading scene: {sceneName}"));

            try
            {
                var operation = SceneManager.UnloadSceneAsync(sceneName);
                if (operation == null)
                    throw new InvalidOperationException($"Failed to start unloading scene '{sceneName}'");

                while (!operation.isDone)
                {
                    cancellation.ThrowIfCancellationRequested();

                    float prog = Mathf.Clamp01(operation.progress);
                    progress?.Report(prog);
                    _eventBus.Publish(new LoadingProgressEvent($"UnloadScene:{sceneName}", prog));

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
                }

                progress?.Report(1f);
                _eventBus.Publish(new LoadingCompletedEvent($"UnloadScene:{sceneName}", true));
                
                Debug.Log($"SceneService: Successfully unloaded scene '{sceneName}'");
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"UnloadScene:{sceneName}", false, ex.Message));
                Debug.LogError($"SceneService: Failed to unload scene '{sceneName}': {ex}");
                throw;
            }
        }

        public async UniTask PreloadSceneAsync(string sceneName, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));

            if (_preloadedScenes.ContainsKey(sceneName))
            {
                Debug.LogWarning($"SceneService: Scene '{sceneName}' is already preloaded");
                return;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
                throw new InvalidOperationException($"Failed to start preloading scene '{sceneName}'");

            operation.allowSceneActivation = false;
            _preloadedScenes[sceneName] = operation;

            while (operation.progress < 0.9f)
            {
                cancellation.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }

            Debug.Log($"SceneService: Successfully preloaded scene '{sceneName}'");
        }

        public void ActivatePreloadedScene(string sceneName)
        {
            if (!_preloadedScenes.TryGetValue(sceneName, out var operation))
            {
                Debug.LogError($"SceneService: Scene '{sceneName}' is not preloaded");
                return;
            }

            operation.allowSceneActivation = true;
            _preloadedScenes.Remove(sceneName);
            
            Debug.Log($"SceneService: Activated preloaded scene '{sceneName}'");
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }

        public IReadOnlyList<string> GetLoadedScenes()
        {
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

        #endregion
    }

    #endregion
}