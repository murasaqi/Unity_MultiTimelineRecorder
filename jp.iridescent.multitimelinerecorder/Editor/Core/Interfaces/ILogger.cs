namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Log levels for categorizing log messages
    /// </summary>
    public enum LogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Log categories for organizing log messages
    /// </summary>
    public enum LogCategory
    {
        General,
        Recording,
        Configuration,
        UI,
        FileSystem,
        Timeline,
        Validation,
        Performance,
        Reference
    }

    /// <summary>
    /// Interface for logging service
    /// Provides consistent logging across the application
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with specified level and category
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="category">Log category</param>
        /// <param name="message">Message to log</param>
        void Log(LogLevel level, LogCategory category, string message);

        /// <summary>
        /// Logs a verbose message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Log category</param>
        void LogVerbose(string message, LogCategory category = LogCategory.General);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Log category</param>
        void LogDebug(string message, LogCategory category = LogCategory.General);

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Log category</param>
        void LogInfo(string message, LogCategory category = LogCategory.General);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Log category</param>
        void LogWarning(string message, LogCategory category = LogCategory.General);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Log category</param>
        void LogError(string message, LogCategory category = LogCategory.General);

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="exception">Exception to log</param>
        /// <param name="message">Additional message</param>
        /// <param name="category">Log category</param>
        void LogException(System.Exception exception, string message = null, LogCategory category = LogCategory.General);

        /// <summary>
        /// Sets the minimum log level
        /// </summary>
        /// <param name="level">Minimum log level to display</param>
        void SetMinimumLogLevel(LogLevel level);

        /// <summary>
        /// Enables or disables logging for a specific category
        /// </summary>
        /// <param name="category">Category to configure</param>
        /// <param name="enabled">Whether to enable logging for this category</param>
        void SetCategoryEnabled(LogCategory category, bool enabled);

        /// <summary>
        /// Clears all log entries
        /// </summary>
        void Clear();
    }
}