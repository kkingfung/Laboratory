using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// Basic logging utility
    /// </summary>
    public static class ToolsUtility
    {
        /// <summary>
        /// Basic logging utility
        /// </summary>
        public static void LogInfo(string message)
        {
            Debug.Log($"[Laboratory.Tools] {message}");
        }
        
        /// <summary>
        /// Basic error logging utility  
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError($"[Laboratory.Tools] {message}");
        }
    }
}
