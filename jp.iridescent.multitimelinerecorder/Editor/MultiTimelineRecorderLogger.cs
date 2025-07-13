using UnityEngine;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Simple logging for Multi Timeline Recorder
    /// </summary>
    public static class MultiTimelineRecorderLogger
    {
        private const string PREFIX = "[MultiTimelineRecorder] ";
        
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