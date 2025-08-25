using UnityEngine.SceneManagement;

namespace Laboratory.Core.Events.Messages
{
    #region Scene Events
    
    /// <summary>
    /// Event fired when a scene change is requested.
    /// This allows systems to prepare for scene transitions.
    /// </summary>
    public class SceneChangeRequestedEvent
    {
        /// <summary>Name of the scene to load.</summary>
        public string SceneName { get; }
        
        /// <summary>Mode for loading the scene (Single or Additive).</summary>
        public LoadSceneMode LoadMode { get; }
        
        public SceneChangeRequestedEvent(string sceneName, LoadSceneMode loadMode)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
        }
    }
    
    /// <summary>
    /// Event fired when a scene has been successfully loaded.
    /// </summary>
    public class SceneLoadedEvent
    {
        /// <summary>Name of the scene that was loaded.</summary>
        public string SceneName { get; }
        
        /// <summary>Mode that was used for loading.</summary>
        public LoadSceneMode LoadMode { get; }
        
        public SceneLoadedEvent(string sceneName, LoadSceneMode loadMode)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
        }
    }
    
    /// <summary>
    /// Event fired when a scene has been unloaded.
    /// </summary>
    public class SceneUnloadedEvent
    {
        /// <summary>Name of the scene that was unloaded.</summary>
        public string SceneName { get; }
        
        public SceneUnloadedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }
    
    #endregion
}
