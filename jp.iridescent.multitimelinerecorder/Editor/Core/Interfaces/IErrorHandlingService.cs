using System;
using System.Threading.Tasks;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for error handling service
    /// Provides centralized error handling and notification
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Handles an exception
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        void HandleError(Exception exception);

        /// <summary>
        /// Handles an exception with additional context
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">Additional context information</param>
        void HandleError(Exception exception, string context);

        /// <summary>
        /// Executes an operation with error handling
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <returns>Result of the operation</returns>
        T ExecuteWithErrorHandling<T>(Func<T> operation, string operationName);

        /// <summary>
        /// Executes an asynchronous operation with error handling
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <returns>Task representing the async operation</returns>
        Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName);

        /// <summary>
        /// Executes an operation with error handling (no return value)
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        void ExecuteWithErrorHandling(Action operation, string operationName);

        /// <summary>
        /// Reports a recoverable error
        /// </summary>
        /// <param name="error">The error to report</param>
        void ReportError(RecordingError error);

        /// <summary>
        /// Reports a warning
        /// </summary>
        /// <param name="warning">The warning to report</param>
        void ReportWarning(RecordingWarning warning);

        /// <summary>
        /// Sets the error handler for specific error types
        /// </summary>
        /// <typeparam name="TException">Type of exception to handle</typeparam>
        /// <param name="handler">Handler for the exception type</param>
        void SetErrorHandler<TException>(Action<TException> handler) where TException : Exception;

        /// <summary>
        /// Gets the last error that occurred
        /// </summary>
        /// <returns>Last error or null if none</returns>
        RecordingError GetLastError();

        /// <summary>
        /// Clears all error history
        /// </summary>
        void ClearErrors();
    }

    /// <summary>
    /// Interface for notification service
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Notifies about an error
        /// </summary>
        /// <param name="error">The error to notify about</param>
        /// <returns>Task representing the notification operation</returns>
        Task NotifyErrorAsync(RecordingError error);

        /// <summary>
        /// Notifies about a warning
        /// </summary>
        /// <param name="warning">The warning to notify about</param>
        /// <returns>Task representing the notification operation</returns>
        Task NotifyWarningAsync(RecordingWarning warning);

        /// <summary>
        /// Shows an information message
        /// </summary>
        /// <param name="message">The message to show</param>
        void ShowInfo(string message);

        /// <summary>
        /// Shows a progress notification
        /// </summary>
        /// <param name="title">Progress title</param>
        /// <param name="info">Progress information</param>
        /// <param name="progress">Progress value (0.0 to 1.0)</param>
        void ShowProgress(string title, string info, float progress);

        /// <summary>
        /// Clears the progress notification
        /// </summary>
        void ClearProgress();
    }

    /// <summary>
    /// Represents a recording error
    /// </summary>
    public class RecordingError
    {
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Context { get; set; }
        public Exception Exception { get; set; }

        public RecordingError(string errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }
    }

    /// <summary>
    /// Represents a recording warning
    /// </summary>
    public class RecordingWarning
    {
        public string WarningCode { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Context { get; set; }

        public RecordingWarning(string warningCode, string message)
        {
            WarningCode = warningCode;
            Message = message;
        }
    }
}