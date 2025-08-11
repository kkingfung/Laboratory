using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagingPipe;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infrastructure
{
    /// <summary>
    /// Async scene loader with queue, progress reporting, cancellation,
    /// and MessagingPipe events for scene load/unload lifecycle.
    /// </summary>
    public class AsyncSceneLoader : IDisposable
    {
        #region Events Messages

        public struct SceneLoadStarted
        {
            public string SceneName;
            public LoadSceneMode Mode;
        }

        public struct SceneLoadCompleted
        {
            public string SceneName;
            public LoadSceneMode Mode;
        }

        public struct SceneUnloadStarted
        {
            public string SceneName;
        }

        public struct SceneUnloadCompleted
        {
            public string SceneName;
        }

        #endregion

        #region Fields

        private readonly IMessageBroker _messageBroker;

        private readonly ConcurrentQueue<Func<UniTask>> _queue = new();
        private bool _isProcessing;

        private CancellationTokenSource? _cts;

        #endregion

        #region Constructor

        public AsyncSceneLoader(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enqueue a scene load request.
        /// </summary>
        public void EnqueueLoad(string sceneName,
                                LoadSceneMode loadMode = LoadSceneMode.Single,
                                IProgress<float>? progress = null,
                                CancellationToken cancellationToken = default)
        {
            _queue.Enqueue(() => LoadSceneInternal(sceneName, loadMode, progress, cancellationToken));
            TryProcessQueue();
        }

        /// <summary>
        /// Enqueue a scene unload request.
        /// </summary>
        public void EnqueueUnload(string sceneName,
                                  IProgress<float>? progress = null,
                                  CancellationToken cancellationToken = default)
        {
            _queue.Enqueue(() => UnloadSceneInternal(sceneName, progress, cancellationToken));
            TryProcessQueue();
        }

        /// <summary>
        /// Cancel all pending operations and stop processing.
        /// </summary>
        public void CancelAll()
        {
            _cts?.Cancel();
            _queue.Clear();
            _isProcessing = false;
        }

        public void Dispose()
        {
            CancelAll();
            _cts?.Dispose();
        }

        #endregion

        #region Private Methods

        private async void TryProcessQueue()
        {
            if (_isProcessing) return;

            _isProcessing = true;
            _cts = new CancellationTokenSource();

            while (_queue.TryDequeue(out var taskFunc))
            {
                try
                {
                    await taskFunc.Invoke().AttachExternalCancellation(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning("AsyncSceneLoader: Operation cancelled.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AsyncSceneLoader: Error processing scene task - {ex}");
                }
            }

            _isProcessing = false;
        }

        private async UniTask LoadSceneInternal(string sceneName,
                                                LoadSceneMode loadMode,
                                                IProgress<float>? progress,
                                                CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("sceneName cannot be null or empty.");

            _messageBroker.Publish(new SceneLoadStarted { SceneName = sceneName, Mode = loadMode });

            var operation = SceneManager.LoadSceneAsync(sceneName, loadMode);
            if (operation == null)
                throw new InvalidOperationException($"Failed to start loading scene '{sceneName}'.");

            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float prog = Mathf.Clamp01(operation.progress / 0.9f);
                progress?.Report(prog);

                if (operation.progress >= 0.9f)
                    operation.allowSceneActivation = true;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            progress?.Report(1f);
            _messageBroker.Publish(new SceneLoadCompleted { SceneName = sceneName, Mode = loadMode });
        }

        private async UniTask UnloadSceneInternal(string sceneName,
                                                  IProgress<float>? progress,
                                                  CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("sceneName cannot be null or empty.");

            _messageBroker.Publish(new SceneUnloadStarted { SceneName = sceneName });

            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
                throw new InvalidOperationException($"Failed to start unloading scene '{sceneName}'.");

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float prog = Mathf.Clamp01(operation.progress);
                progress?.Report(prog);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            progress?.Report(1f);
            _messageBroker.Publish(new SceneUnloadCompleted { SceneName = sceneName });
        }

        #endregion
    }
}
