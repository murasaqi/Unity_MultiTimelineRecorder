using System;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Base exception for all recording-related exceptions
    /// </summary>
    public abstract class RecordingException : Exception
    {
        /// <summary>
        /// Error code for categorizing the exception
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Additional context information
        /// </summary>
        public string Context { get; set; }

        protected RecordingException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected RecordingException(string errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when configuration is invalid
    /// </summary>
    public class RecordingConfigurationException : RecordingException
    {
        public RecordingConfigurationException(string message) 
            : base("RECORDING_CONFIG_ERROR", message)
        {
        }

        public RecordingConfigurationException(string message, Exception innerException) 
            : base("RECORDING_CONFIG_ERROR", message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown during recording execution
    /// </summary>
    public class RecordingExecutionException : RecordingException
    {
        public RecordingExecutionException(string message) 
            : base("RECORDING_EXECUTION_ERROR", message)
        {
        }

        public RecordingExecutionException(string message, Exception innerException) 
            : base("RECORDING_EXECUTION_ERROR", message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when timeline operations fail
    /// </summary>
    public class TimelineException : RecordingException
    {
        public TimelineException(string message) 
            : base("TIMELINE_ERROR", message)
        {
        }

        public TimelineException(string message, Exception innerException) 
            : base("TIMELINE_ERROR", message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when file system operations fail
    /// </summary>
    public class FileSystemException : RecordingException
    {
        public string Path { get; set; }

        public FileSystemException(string message, string path = null) 
            : base("FILE_SYSTEM_ERROR", message)
        {
            Path = path;
        }

        public FileSystemException(string message, string path, Exception innerException) 
            : base("FILE_SYSTEM_ERROR", message, innerException)
        {
            Path = path;
        }
    }

    /// <summary>
    /// Exception thrown when recorder initialization fails
    /// </summary>
    public class RecorderInitializationException : RecordingException
    {
        public string RecorderType { get; set; }

        public RecorderInitializationException(string message, string recorderType = null) 
            : base("RECORDER_INIT_ERROR", message)
        {
            RecorderType = recorderType;
        }

        public RecorderInitializationException(string message, string recorderType, Exception innerException) 
            : base("RECORDER_INIT_ERROR", message, innerException)
        {
            RecorderType = recorderType;
        }
    }

    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : RecordingException
    {
        public Interfaces.ValidationResult ValidationResult { get; set; }

        public ValidationException(string message, Interfaces.ValidationResult validationResult = null) 
            : base("VALIDATION_ERROR", message)
        {
            ValidationResult = validationResult;
        }
    }
}