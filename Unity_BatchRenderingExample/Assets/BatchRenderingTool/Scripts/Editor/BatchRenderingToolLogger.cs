using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Centralized logging system for Batch Rendering Tool
    /// </summary>
    public static class BatchRenderingToolLogger
    {
        // Set to false to disable verbose logging in production
        private static bool s_EnableVerboseLogging = false;
        
        /// <summary>
        /// Enable or disable verbose logging
        /// </summary>
        public static bool EnableVerboseLogging
        {
            get => s_EnableVerboseLogging;
            set => s_EnableVerboseLogging = value;
        }
        
        /// <summary>
        /// Log verbose debug information (only when verbose logging is enabled)
        /// </summary>
        public static void LogVerbose(string message)
        {
            #if UNITY_EDITOR && BATCH_RENDERING_TOOL_DEBUG
            if (s_EnableVerboseLogging)
            {
                Debug.Log(message);
            }
            #endif
        }
        
        /// <summary>
        /// Log important information (always logged)
        /// </summary>
        public static void Log(string message)
        {
            Debug.Log(message);
        }
        
        /// <summary>
        /// Log warnings (always logged)
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }
        
        /// <summary>
        /// Log errors (always logged)
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}