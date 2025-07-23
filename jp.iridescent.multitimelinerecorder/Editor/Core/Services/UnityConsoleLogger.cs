using System;
using System.Collections.Generic;
using UnityEngine;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Logger implementation that outputs to Unity console
    /// </summary>
    public class UnityConsoleLogger : MultiTimelineRecorder.Core.Interfaces.ILogger
    {
        private LogLevel _minimumLevel = LogLevel.Info;
        private readonly Dictionary<LogCategory, bool> _categoryEnabled = new Dictionary<LogCategory, bool>();
        private readonly string _prefix = "[MultiTimelineRecorder]";

        public UnityConsoleLogger()
        {
            // Enable all categories by default
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                _categoryEnabled[category] = true;
            }
        }

        /// <inheritdoc />
        public void Log(LogLevel level, LogCategory category, string message)
        {
            // Check if level meets minimum threshold
            if (level < _minimumLevel)
            {
                return;
            }

            // Check if category is enabled
            if (!_categoryEnabled.TryGetValue(category, out var enabled) || !enabled)
            {
                return;
            }

            var formattedMessage = FormatMessage(level, category, message);

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }

        /// <inheritdoc />
        public void LogVerbose(string message, LogCategory category = LogCategory.General)
        {
            Log(LogLevel.Verbose, category, message);
        }

        /// <inheritdoc />
        public void LogDebug(string message, LogCategory category = LogCategory.General)
        {
            Log(LogLevel.Debug, category, message);
        }

        /// <inheritdoc />
        public void LogInfo(string message, LogCategory category = LogCategory.General)
        {
            Log(LogLevel.Info, category, message);
        }

        /// <inheritdoc />
        public void LogWarning(string message, LogCategory category = LogCategory.General)
        {
            Log(LogLevel.Warning, category, message);
        }

        /// <inheritdoc />
        public void LogError(string message, LogCategory category = LogCategory.General)
        {
            Log(LogLevel.Error, category, message);
        }

        /// <inheritdoc />
        public void LogException(Exception exception, string message = null, LogCategory category = LogCategory.General)
        {
            if (exception == null) return;

            var fullMessage = string.IsNullOrEmpty(message) 
                ? $"Exception: {exception.Message}" 
                : $"{message} - Exception: {exception.Message}";

            LogError(fullMessage, category);

            if (_minimumLevel <= LogLevel.Debug)
            {
                LogDebug($"Stack trace: {exception.StackTrace}", category);
            }
        }

        /// <inheritdoc />
        public void SetMinimumLogLevel(LogLevel level)
        {
            _minimumLevel = level;
        }

        /// <inheritdoc />
        public void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            _categoryEnabled[category] = enabled;
        }

        /// <inheritdoc />
        public void Clear()
        {
            // Unity doesn't provide a way to clear console programmatically in runtime
            #if UNITY_EDITOR
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(new object(), null);
            #endif
        }

        /// <summary>
        /// Formats a log message
        /// </summary>
        private string FormatMessage(LogLevel level, LogCategory category, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var levelStr = GetLevelString(level);
            var categoryStr = category != LogCategory.General ? $"[{category}]" : "";
            
            return $"{_prefix} [{timestamp}] {levelStr} {categoryStr} {message}";
        }

        /// <summary>
        /// Gets a formatted string for the log level
        /// </summary>
        private string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    return "[VERBOSE]";
                case LogLevel.Debug:
                    return "[DEBUG]";
                case LogLevel.Info:
                    return "[INFO]";
                case LogLevel.Warning:
                    return "[WARNING]";
                case LogLevel.Error:
                    return "[ERROR]";
                default:
                    return "[UNKNOWN]";
            }
        }
    }
}