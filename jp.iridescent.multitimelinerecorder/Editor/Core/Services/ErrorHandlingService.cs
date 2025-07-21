using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service responsible for centralized error handling
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly Dictionary<Type, Delegate> _errorHandlers;
        private readonly Queue<RecordingError> _errorHistory;
        private readonly int _maxErrorHistory = 100;

        public ErrorHandlingService(ILogger logger, INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _errorHandlers = new Dictionary<Type, Delegate>();
            _errorHistory = new Queue<RecordingError>();
        }

        /// <inheritdoc />
        public void HandleError(Exception exception)
        {
            HandleError(exception, null);
        }

        /// <inheritdoc />
        public void HandleError(Exception exception, string context)
        {
            if (exception == null) return;

            // Log the error
            _logger.LogException(exception, context);

            // Create error record
            var error = CreateErrorFromException(exception, context);
            AddToHistory(error);

            // Check for specific error handlers
            var exceptionType = exception.GetType();
            if (_errorHandlers.TryGetValue(exceptionType, out var handler))
            {
                try
                {
                    handler.DynamicInvoke(exception);
                }
                catch (Exception handlerEx)
                {
                    _logger.LogError($"Error handler failed: {handlerEx.Message}");
                }
            }

            // Notify user
            _ = _notificationService.NotifyErrorAsync(error);

            // Handle specific exception types
            switch (exception)
            {
                case RecordingConfigurationException configEx:
                    HandleConfigurationError(configEx);
                    break;
                    
                case RecordingExecutionException execEx:
                    HandleExecutionError(execEx);
                    break;
                    
                case TimelineException timelineEx:
                    HandleTimelineError(timelineEx);
                    break;
                    
                case FileSystemException fsEx:
                    HandleFileSystemError(fsEx);
                    break;
                    
                case ValidationException validEx:
                    HandleValidationError(validEx);
                    break;
            }
        }

        /// <inheritdoc />
        public T ExecuteWithErrorHandling<T>(Func<T> operation, string operationName)
        {
            try
            {
                return operation();
            }
            catch (RecordingException ex)
            {
                _logger.LogError($"Recording error in {operationName}: {ex.Message}", LogCategory.General);
                HandleError(ex, operationName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in {operationName}: {ex.Message}", LogCategory.General);
                var wrappedException = new RecordingExecutionException(
                    $"Unexpected error in {operationName}: {ex.Message}", ex);
                HandleError(wrappedException, operationName);
                throw wrappedException;
            }
        }

        /// <inheritdoc />
        public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName)
        {
            try
            {
                return await operation();
            }
            catch (RecordingException ex)
            {
                _logger.LogError($"Recording error in {operationName}: {ex.Message}", LogCategory.General);
                HandleError(ex, operationName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in {operationName}: {ex.Message}", LogCategory.General);
                var wrappedException = new RecordingExecutionException(
                    $"Unexpected error in {operationName}: {ex.Message}", ex);
                HandleError(wrappedException, operationName);
                throw wrappedException;
            }
        }

        /// <inheritdoc />
        public void ExecuteWithErrorHandling(Action operation, string operationName)
        {
            ExecuteWithErrorHandling(() =>
            {
                operation();
                return true;
            }, operationName);
        }

        /// <inheritdoc />
        public void ReportError(RecordingError error)
        {
            if (error == null) return;

            _logger.LogError(error.Message);
            AddToHistory(error);
            _ = _notificationService.NotifyErrorAsync(error);
        }

        /// <inheritdoc />
        public void ReportWarning(RecordingWarning warning)
        {
            if (warning == null) return;

            _logger.LogWarning(warning.Message);
            _ = _notificationService.NotifyWarningAsync(warning);
        }

        /// <inheritdoc />
        public void SetErrorHandler<TException>(Action<TException> handler) where TException : Exception
        {
            if (handler == null)
            {
                _errorHandlers.Remove(typeof(TException));
            }
            else
            {
                _errorHandlers[typeof(TException)] = handler;
            }
        }

        /// <inheritdoc />
        public RecordingError GetLastError()
        {
            return _errorHistory.Count > 0 ? _errorHistory.Peek() : null;
        }

        /// <inheritdoc />
        public void ClearErrors()
        {
            _errorHistory.Clear();
        }

        /// <summary>
        /// Creates an error record from an exception
        /// </summary>
        private RecordingError CreateErrorFromException(Exception exception, string context)
        {
            var errorCode = "UNKNOWN_ERROR";
            
            if (exception is RecordingException recordingEx)
            {
                errorCode = recordingEx.ErrorCode;
            }

            return new RecordingError(errorCode, exception.Message)
            {
                Context = context,
                Exception = exception
            };
        }

        /// <summary>
        /// Adds an error to history
        /// </summary>
        private void AddToHistory(RecordingError error)
        {
            _errorHistory.Enqueue(error);
            
            // Maintain history size limit
            while (_errorHistory.Count > _maxErrorHistory)
            {
                _errorHistory.Dequeue();
            }
        }

        /// <summary>
        /// Handles configuration errors
        /// </summary>
        private void HandleConfigurationError(RecordingConfigurationException ex)
        {
            _notificationService.ShowInfo(
                "Configuration Error: Please check your recording settings and try again.");
        }

        /// <summary>
        /// Handles execution errors
        /// </summary>
        private void HandleExecutionError(RecordingExecutionException ex)
        {
            _notificationService.ShowInfo(
                "Recording failed. Please check the console for details.");
        }

        /// <summary>
        /// Handles timeline errors
        /// </summary>
        private void HandleTimelineError(TimelineException ex)
        {
            _notificationService.ShowInfo(
                "Timeline Error: Please ensure all timelines are properly configured.");
        }

        /// <summary>
        /// Handles file system errors
        /// </summary>
        private void HandleFileSystemError(FileSystemException ex)
        {
            var message = "File System Error: ";
            if (!string.IsNullOrEmpty(ex.Path))
            {
                message += $"Path: {ex.Path}. ";
            }
            message += "Please check file permissions and available disk space.";
            
            _notificationService.ShowInfo(message);
        }

        /// <summary>
        /// Handles validation errors
        /// </summary>
        private void HandleValidationError(ValidationException ex)
        {
            if (ex.ValidationResult != null && ex.ValidationResult.Issues.Count > 0)
            {
                var errorCount = ex.ValidationResult.Issues.Count(i => i.Severity == ValidationSeverity.Error);
                var warningCount = ex.ValidationResult.Issues.Count(i => i.Severity == ValidationSeverity.Warning);
                
                _notificationService.ShowInfo(
                    $"Validation failed with {errorCount} error(s) and {warningCount} warning(s). Check console for details.");
            }
            else
            {
                _notificationService.ShowInfo("Validation failed. Please check your settings.");
            }
        }
    }
}