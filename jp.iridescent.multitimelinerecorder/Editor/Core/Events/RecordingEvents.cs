using System;
using System.Collections.Generic;
using UnityEngine.Playables;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;

namespace MultiTimelineRecorder.Core.Events
{
    /// <summary>
    /// Base class for all recording-related events
    /// </summary>
    public abstract class RecordingEvent : EventArgs
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    /// <summary>
    /// Event raised when recording starts
    /// </summary>
    public class RecordingStartedEvent : RecordingEvent
    {
        public string JobId { get; set; }
        public List<PlayableDirector> Timelines { get; set; }
        public IRecordingConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// Event raised when recording completes
    /// </summary>
    public class RecordingCompletedEvent : RecordingEvent
    {
        public string JobId { get; set; }
        public RecordingResult Result { get; set; }
    }

    /// <summary>
    /// Event raised when recording progress updates
    /// </summary>
    public class RecordingProgressEvent : RecordingEvent
    {
        public string JobId { get; set; }
        public float Progress { get; set; }
        public int CurrentFrame { get; set; }
        public int TotalFrames { get; set; }
        public string CurrentTimeline { get; set; }
    }

    /// <summary>
    /// Event raised when recording is cancelled
    /// </summary>
    public class RecordingCancelledEvent : RecordingEvent
    {
        public string JobId { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Event raised when recording fails
    /// </summary>
    public class RecordingFailedEvent : RecordingEvent
    {
        public string JobId { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event raised when configuration changes
    /// </summary>
    public class ConfigurationChangedEvent : RecordingEvent
    {
        public IRecordingConfiguration Configuration { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    /// <summary>
    /// Event raised when timeline selection changes
    /// </summary>
    public class TimelineSelectionChangedEvent : RecordingEvent
    {
        public List<PlayableDirector> SelectedTimelines { get; set; }
        public List<PlayableDirector> AddedTimelines { get; set; }
        public List<PlayableDirector> RemovedTimelines { get; set; }
    }

    /// <summary>
    /// Event raised when a recorder is added
    /// </summary>
    public class RecorderAddedEvent : RecordingEvent
    {
        public string TimelineConfigId { get; set; }
        public IRecorderConfiguration RecorderConfig { get; set; }
    }

    /// <summary>
    /// Event raised when a recorder is removed
    /// </summary>
    public class RecorderRemovedEvent : RecordingEvent
    {
        public string TimelineConfigId { get; set; }
        public string RecorderConfigId { get; set; }
    }

    /// <summary>
    /// Event raised when a recorder configuration is updated
    /// </summary>
    public class RecorderUpdatedEvent : RecordingEvent
    {
        public string TimelineConfigId { get; set; }
        public IRecorderConfiguration RecorderConfig { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    /// <summary>
    /// Event raised when validation state changes
    /// </summary>
    public class ValidationStateChangedEvent : RecordingEvent
    {
        public ValidationResult ValidationResult { get; set; }
        public bool WasValid { get; set; }
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Event raised when UI needs to refresh
    /// </summary>
    public class UIRefreshRequestedEvent : RecordingEvent
    {
        public enum RefreshScope
        {
            All,
            TimelineList,
            RecorderList,
            Settings,
            ValidationState
        }

        public RefreshScope Scope { get; set; }
        public string TargetId { get; set; }
    }

    /// <summary>
    /// Event raised when a recording error occurs
    /// </summary>
    public class RecordingErrorEvent : RecordingEvent
    {
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public LogCategory Category { get; set; }
    }

    /// <summary>
    /// Event raised when configuration is loaded
    /// </summary>
    public class ConfigurationLoadedEvent : RecordingEvent
    {
        public IRecordingConfiguration Configuration { get; set; }
        public bool IsNewConfiguration { get; set; }
        public string LoadedFrom { get; set; }
    }

    /// <summary>
    /// Event raised when configuration is saved
    /// </summary>
    public class ConfigurationSavedEvent : RecordingEvent
    {
        public IRecordingConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// Event raised when recorder configuration changes
    /// </summary>
    public class RecorderConfigurationChangedEvent : RecordingEvent
    {
        public IRecorderConfiguration RecorderConfig { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }
}