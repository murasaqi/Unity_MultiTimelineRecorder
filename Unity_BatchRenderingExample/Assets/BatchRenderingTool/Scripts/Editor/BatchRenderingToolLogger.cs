using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Simple logging for Batch Rendering Tool
    /// </summary>
    public static class BatchRenderingToolLogger
    {
        public static void LogVerbose(string message) { /* Disabled for production */ }
        public static void Log(string message) => Debug.Log(message);
        public static void LogWarning(string message) => Debug.LogWarning(message);
        public static void LogError(string message) => Debug.LogError(message);
    }
}