using UnityEngine;

namespace Laboratory.Core.Events.Messages
{
    #region Loading Events
    
    /// <summary>
    /// Event fired when a loading operation begins.
    /// </summary>
    public class LoadingStartedEvent
    {
        /// <summary>Name/identifier of the loading operation.</summary>
        public string OperationName { get; }
        
        /// <summary>Human-readable description of what is being loaded.</summary>
        public string Description { get; }
        
        public LoadingStartedEvent(string operationName, string description = "")
        {
            OperationName = operationName;
            Description = description;
        }
    }
    
    /// <summary>
    /// Event fired during loading operations to report progress.
    /// </summary>
    public class LoadingProgressEvent
    {
        /// <summary>Name/identifier of the loading operation.</summary>
        public string OperationName { get; }
        
        /// <summary>Progress value from 0.0 to 1.0.</summary>
        public float Progress { get; }
        
        /// <summary>Optional status text describing current loading step.</summary>
        public string? StatusText { get; }
        
        public LoadingProgressEvent(string operationName, float progress, string? statusText = null)
        {
            OperationName = operationName;
            Progress = Mathf.Clamp01(progress);
            StatusText = statusText;
        }
    }
    
    /// <summary>
    /// Event fired when a loading operation completes (successfully or with error).
    /// </summary>
    public class LoadingCompletedEvent
    {
        /// <summary>Name/identifier of the loading operation.</summary>
        public string OperationName { get; }
        
        /// <summary>Whether the loading operation succeeded.</summary>
        public bool Success { get; }
        
        /// <summary>Error message if loading failed.</summary>
        public string? ErrorMessage { get; }
        
        public LoadingCompletedEvent(string operationName, bool success, string? errorMessage = null)
        {
            OperationName = operationName;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
    
    #endregion
}
