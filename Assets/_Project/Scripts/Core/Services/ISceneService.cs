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
    /// Service interface for scene loading and management.
    /// Replaces AsyncSceneLoader with improved functionality.
    /// </summary>
    public interface ISceneService
    {
        /// <summary>Current active scene name.</summary>
        string? CurrentScene { get; }
        
        /// <summary>Loads a scene asynchronously with progress reporting.</summary>
        UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, 
            IProgress<float>? progress = null, CancellationToken cancellation = default);
        
        /// <summary>Unloads a scene asynchronously.</summary>
        UniTask UnloadSceneAsync(string sceneName, IProgress<float>? progress = null, 
            CancellationToken cancellation = default);
        
        /// <summary>Preloads a scene without activating it.</summary>
        UniTask PreloadSceneAsync(string sceneName, CancellationToken cancellation = default);
        
        /// <summary>Activates a preloaded scene.</summary>
        void ActivatePreloadedScene(string sceneName);
        
        /// <summary>Checks if a scene is currently loaded.</summary>
        bool IsSceneLoaded(string sceneName);
        
        /// <summary>Gets all currently loaded scenes.</summary>
        IReadOnlyList<string> GetLoadedScenes();
    }
    #endregion
}