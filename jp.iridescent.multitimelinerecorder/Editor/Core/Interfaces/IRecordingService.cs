using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Playables;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for the recording execution service
    /// Handles the core recording logic and Unity Recorder integration
    /// </summary>
    public interface IRecordingService
    {
        /// <summary>
        /// Executes recording for multiple timelines with the specified configuration
        /// </summary>
        /// <param name="timelines">List of PlayableDirector instances to record</param>
        /// <param name="config">Recording configuration settings</param>
        /// <returns>Result of the recording operation</returns>
        RecordingResult ExecuteRecording(List<PlayableDirector> timelines, IRecordingConfiguration config);

        /// <summary>
        /// Executes recording asynchronously
        /// </summary>
        /// <param name="timelines">List of PlayableDirector instances to record</param>
        /// <param name="config">Recording configuration settings</param>
        /// <returns>Task representing the asynchronous recording operation</returns>
        Task<RecordingResult> ExecuteRecordingAsync(List<PlayableDirector> timelines, IRecordingConfiguration config);

        /// <summary>
        /// Cancels an ongoing recording
        /// </summary>
        /// <param name="jobId">The ID of the recording job to cancel</param>
        void CancelRecording(string jobId);

        /// <summary>
        /// Gets the progress of a recording job
        /// </summary>
        /// <param name="jobId">The ID of the recording job</param>
        /// <returns>Current progress of the recording</returns>
        RecordingProgress GetProgress(string jobId);

        /// <summary>
        /// Checks if a recording is currently in progress
        /// </summary>
        bool IsRecording { get; }
    }

    /// <summary>
    /// Result of a recording operation
    /// </summary>
    public class RecordingResult
    {
        public bool IsSuccess { get; set; }
        public string JobId { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> OutputFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Progress information for an ongoing recording
    /// </summary>
    public class RecordingProgress
    {
        public string JobId { get; set; }
        public float Progress { get; set; } // 0.0 to 1.0
        public string CurrentTimeline { get; set; }
        public int CurrentFrame { get; set; }
        public int TotalFrames { get; set; }
        public RecordingState State { get; set; }
    }

    /// <summary>
    /// State of a recording job
    /// </summary>
    public enum RecordingState
    {
        Pending,
        Preparing,
        Recording,
        PostProcessing,
        Completed,
        Cancelled,
        Failed
    }
}