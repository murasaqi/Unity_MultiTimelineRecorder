using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Simple logging for Batch Rendering Tool
    /// </summary>
    public static class BatchRenderingToolLogger
    {
        private const string PREFIX = "[BatchRenderingTool] ";
        
        public static void LogVerbose(string message) 
        { 
            /* Disabled for production */ 
        }
        
        public static void Log(string message) 
        {
            if (!string.IsNullOrEmpty(message))
                Debug.Log(PREFIX + message);
        }
        
        public static void LogWarning(string message) 
        {
            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning(PREFIX + message);
        }
        
        public static void LogError(string message) 
        {
            if (!string.IsNullOrEmpty(message))
                Debug.LogError(PREFIX + message);
        }
    }
}