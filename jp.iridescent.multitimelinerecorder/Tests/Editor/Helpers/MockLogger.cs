using System;
using System.Collections.Generic;
using System.Linq;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Tests.Helpers
{
    /// <summary>
    /// Mock implementation of ILogger for testing
    /// </summary>
    public class MockLogger : ILogger
    {
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        
        public void LogInfo(string message, LogCategory category = LogCategory.General)
        {
            _logEntries.Add(new LogEntry
            {
                Level = LogLevel.Info,
                Message = message,
                Category = category,
                Timestamp = DateTime.Now
            });
        }
        
        public void LogWarning(string message, LogCategory category = LogCategory.General)
        {
            _logEntries.Add(new LogEntry
            {
                Level = LogLevel.Warning,
                Message = message,
                Category = category,
                Timestamp = DateTime.Now
            });
        }
        
        public void LogError(string message, LogCategory category = LogCategory.General)
        {
            _logEntries.Add(new LogEntry
            {
                Level = LogLevel.Error,
                Message = message,
                Category = category,
                Timestamp = DateTime.Now
            });
        }
        
        public void LogException(Exception exception, LogCategory category = LogCategory.General)
        {
            _logEntries.Add(new LogEntry
            {
                Level = LogLevel.Error,
                Message = exception.Message,
                Category = category,
                Exception = exception,
                Timestamp = DateTime.Now
            });
        }
        
        /// <summary>
        /// Checks if a log message was recorded
        /// </summary>
        public bool ContainsLog(string message, LogLevel level = LogLevel.Info)
        {
            return _logEntries.Any(e => e.Message.Contains(message) && e.Level == level);
        }
        
        /// <summary>
        /// Gets all log entries
        /// </summary>
        public List<LogEntry> GetLogEntries()
        {
            return new List<LogEntry>(_logEntries);
        }
        
        /// <summary>
        /// Gets log entries by level
        /// </summary>
        public List<LogEntry> GetLogEntriesByLevel(LogLevel level)
        {
            return _logEntries.Where(e => e.Level == level).ToList();
        }
        
        /// <summary>
        /// Gets the count of log entries by level
        /// </summary>
        public int GetLogCount(LogLevel level)
        {
            return _logEntries.Count(e => e.Level == level);
        }
        
        /// <summary>
        /// Clears all log entries
        /// </summary>
        public void Clear()
        {
            _logEntries.Clear();
        }
        
        /// <summary>
        /// Disposes the logger
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
        
        public class LogEntry
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public LogCategory Category { get; set; }
            public Exception Exception { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
    
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}