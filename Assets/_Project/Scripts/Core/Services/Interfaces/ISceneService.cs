using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Service interface for scene loading and management.
    /// Provides unified scene operations with progress tracking and preloading support.
    /// </summary>
    public interface ISceneService : IDisposable
    {
        /// <summary>Gets the name of the currently active scene.</summary>
        string? CurrentScene { get; }
        
        /// <summary>Loads a scene asynchronously with progress reporting.</summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="mode">Loading mode (Single or Additive)</param>
        /// <param name="progress">Progress reporter for loading operations</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when scene is loaded</returns>
        UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, 
            IProgress<float>? progress = null, CancellationToken cancellation = default);
        
        /// <summary>Unloads a scene asynchronously.</summary>
        /// <param name="sceneName">Name of the scene to unload</param>
        /// <param name="progress">Progress reporter for unloading operations</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when scene is unloaded</returns>
        UniTask UnloadSceneAsync(string sceneName, IProgress<float>? progress = null, 
            CancellationToken cancellation = default);
        
        /// <summary>Preloads a scene without activating it for faster transitions.</summary>
        /// <param name="sceneName">Name of the scene to preload</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when scene is preloaded</returns>
        UniTask PreloadSceneAsync(string sceneName, CancellationToken cancellation = default);
        
        /// <summary>Activates a previously preloaded scene.</summary>
        /// <param name="sceneName">Name of the preloaded scene to activate</param>
        void ActivatePreloadedScene(string sceneName);
        
        /// <summary>Checks if a scene is currently loaded.</summary>
        /// <param name="sceneName">Name of the scene to check</param>
        /// <returns>True if scene is loaded, false otherwise</returns>
        bool IsSceneLoaded(string sceneName);
        
        /// <summary>Gets a read-only list of all currently loaded scene names.</summary>
        /// <returns>Collection of loaded scene names</returns>
        IReadOnlyList<string> GetLoadedScenes();
    }
}
