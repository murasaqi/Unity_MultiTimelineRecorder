using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Notification service implementation for Unity Editor
    /// </summary>
    public class EditorNotificationService : INotificationService
    {
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private bool _isShowingProgress;
        private float _currentProgress;

        public EditorNotificationService(MultiTimelineRecorder.Core.Interfaces.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task NotifyErrorAsync(RecordingError error)
        {
            if (error == null) return;

            // Log to console
            _logger.LogError($"[{error.ErrorCode}] {error.Message}");

            // Show dialog if not in batch mode
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Recording Error",
                    error.Message,
                    "OK");
            }

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task NotifyWarningAsync(RecordingWarning warning)
        {
            if (warning == null) return;

            // Log to console
            _logger.LogWarning($"[{warning.WarningCode}] {warning.Message}");

            // Show notification in status bar
            ShowNotification($"Warning: {warning.Message}", 3f);

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public void ShowInfo(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _logger.LogInfo(message);

            // Show notification
            ShowNotification(message, 2f);
        }

        /// <inheritdoc />
        public void ShowProgress(string title, string info, float progress)
        {
            if (Application.isBatchMode) return;

            progress = Mathf.Clamp01(progress);
            
            // Only update if progress has changed significantly
            if (!_isShowingProgress || Math.Abs(progress - _currentProgress) > 0.01f)
            {
                _currentProgress = progress;
                _isShowingProgress = true;

                if (progress < 1.0f)
                {
                    EditorUtility.DisplayProgressBar(title, info, progress);
                }
                else
                {
                    ClearProgress();
                }
            }
        }

        /// <inheritdoc />
        public void ClearProgress()
        {
            if (_isShowingProgress)
            {
                EditorUtility.ClearProgressBar();
                _isShowingProgress = false;
                _currentProgress = 0f;
            }
        }

        /// <summary>
        /// Shows a notification in the editor
        /// </summary>
        private void ShowNotification(string message, float duration)
        {
            if (Application.isBatchMode) return;

            // Try to show notification in active editor window
            var window = EditorWindow.focusedWindow;
            if (window != null)
            {
                window.ShowNotification(new GUIContent(message), duration);
            }
            else
            {
                // Fallback to console message
                Debug.Log($"[Notification] {message}");
            }
        }
    }
}