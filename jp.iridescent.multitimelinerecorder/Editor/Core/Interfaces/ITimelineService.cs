using System.Collections.Generic;
using UnityEngine.Playables;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for timeline management service
    /// Handles timeline discovery, validation, and management
    /// </summary>
    public interface ITimelineService
    {
        /// <summary>
        /// Scans the project for available PlayableDirector instances
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive GameObjects</param>
        /// <returns>List of available PlayableDirector instances</returns>
        List<PlayableDirector> ScanAvailableTimelines(bool includeInactive = false);

        /// <summary>
        /// Scans timelines in a specific scene
        /// </summary>
        /// <param name="scenePath">Path to the scene to scan</param>
        /// <param name="includeInactive">Whether to include inactive GameObjects</param>
        /// <returns>List of PlayableDirector instances in the scene</returns>
        List<PlayableDirector> ScanSceneTimelines(string scenePath, bool includeInactive = false);

        /// <summary>
        /// Validates a timeline for recording
        /// </summary>
        /// <param name="director">The PlayableDirector to validate</param>
        /// <returns>Validation result with any issues found</returns>
        ValidationResult ValidateTimeline(PlayableDirector director);

        /// <summary>
        /// Gets timeline information
        /// </summary>
        /// <param name="director">The PlayableDirector to get information for</param>
        /// <returns>Timeline information</returns>
        TimelineInfo GetTimelineInfo(PlayableDirector director);

        /// <summary>
        /// Prepares a timeline for recording
        /// </summary>
        /// <param name="director">The PlayableDirector to prepare</param>
        void PrepareTimelineForRecording(PlayableDirector director);

        /// <summary>
        /// Restores a timeline after recording
        /// </summary>
        /// <param name="director">The PlayableDirector to restore</param>
        void RestoreTimelineAfterRecording(PlayableDirector director);
    }

    /// <summary>
    /// Validation result for timeline checks
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();

        public void AddError(string message)
        {
            Issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Message = message });
            IsValid = false;
        }

        public void AddWarning(string message)
        {
            Issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Message = message });
        }
        
        public void Merge(ValidationResult other)
        {
            if (other == null) return;
            
            Issues.AddRange(other.Issues);
            if (!other.IsValid)
            {
                IsValid = false;
            }
        }
    }

    /// <summary>
    /// Individual validation issue
    /// </summary>
    public class ValidationIssue
    {
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Severity levels for validation issues
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Information about a timeline
    /// </summary>
    public class TimelineInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public double Duration { get; set; }
        public double FrameRate { get; set; }
        public int FrameCount { get; set; }
        public bool HasSignals { get; set; }
        public List<string> TrackTypes { get; set; } = new List<string>();
    }
}