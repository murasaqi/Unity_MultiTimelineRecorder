using System;
using System.Collections.Generic;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Tests.Helpers
{
    /// <summary>
    /// Mock implementation of INotificationService for testing
    /// </summary>
    public class MockNotificationService : INotificationService
    {
        private readonly List<NotificationEntry> _notifications = new List<NotificationEntry>();
        
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            _notifications.Add(new NotificationEntry
            {
                Message = message,
                Type = type,
                Duration = duration,
                Timestamp = DateTime.Now
            });
        }
        
        public void ShowProgress(string title, string info, float progress)
        {
            _notifications.Add(new NotificationEntry
            {
                Message = $"{title}: {info}",
                Type = NotificationType.Progress,
                Progress = progress,
                Timestamp = DateTime.Now
            });
        }
        
        public void ClearProgress()
        {
            // Mock implementation - do nothing
        }
        
        public bool DisplayDialog(string title, string message, string ok, string cancel = "")
        {
            _notifications.Add(new NotificationEntry
            {
                Message = $"Dialog: {title} - {message}",
                Type = NotificationType.Dialog,
                Timestamp = DateTime.Now
            });
            
            // Always return true in tests unless overridden
            return true;
        }
        
        /// <summary>
        /// Gets all notifications
        /// </summary>
        public List<NotificationEntry> GetNotifications()
        {
            return new List<NotificationEntry>(_notifications);
        }
        
        /// <summary>
        /// Gets notifications by type
        /// </summary>
        public List<NotificationEntry> GetNotificationsByType(NotificationType type)
        {
            return _notifications.FindAll(n => n.Type == type);
        }
        
        /// <summary>
        /// Checks if a notification was shown
        /// </summary>
        public bool WasNotificationShown(string message, NotificationType type = NotificationType.Info)
        {
            return _notifications.Exists(n => n.Message.Contains(message) && n.Type == type);
        }
        
        /// <summary>
        /// Clears all notifications
        /// </summary>
        public void Clear()
        {
            _notifications.Clear();
        }
        
        public class NotificationEntry
        {
            public string Message { get; set; }
            public NotificationType Type { get; set; }
            public float Duration { get; set; }
            public float Progress { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}