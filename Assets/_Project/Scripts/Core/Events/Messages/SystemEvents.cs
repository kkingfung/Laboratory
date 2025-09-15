using System;

#nullable enable

namespace Laboratory.Core.Events.Messages
{
    #region System Events
    
    /// <summary>
    /// Event fired when a system is successfully initialized.
    /// </summary>
    public class SystemInitializedEvent
    {
        /// <summary>Name of the system that was initialized.</summary>
        public string SystemName { get; }
        
        /// <summary>Timestamp when initialization completed.</summary>
        public DateTime InitializedAt { get; }
        
        public SystemInitializedEvent(string systemName)
        {
            SystemName = systemName;
            InitializedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when a system is shutting down.
    /// </summary>
    public class SystemShutdownEvent
    {
        /// <summary>Name of the system that is shutting down.</summary>
        public string SystemName { get; }
        
        /// <summary>Timestamp when shutdown started.</summary>
        public DateTime ShutdownAt { get; }
        
        public SystemShutdownEvent(string systemName)
        {
            SystemName = systemName;
            ShutdownAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when a system encounters an error.
    /// </summary>
    public class SystemErrorEvent
    {
        /// <summary>Name of the system that encountered an error.</summary>
        public string SystemName { get; }
        
        /// <summary>Error message describing what went wrong.</summary>
        public string ErrorMessage { get; }
        
        /// <summary>Exception that caused the error (if any).</summary>
        public Exception? Exception { get; }
        
        /// <summary>Timestamp when the error occurred.</summary>
        public DateTime ErrorAt { get; }
        
        public SystemErrorEvent(string systemName, string errorMessage, Exception? exception = null)
        {
            SystemName = systemName;
            ErrorMessage = errorMessage;
            Exception = exception;
            ErrorAt = DateTime.UtcNow;
        }
    }
    
    #endregion
}
