using System.Collections.Generic;
using UnityEngine.Timeline;
using Unity.MultiTimelineRecorder.Utilities;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for SignalEmitter management service
    /// </summary>
    public interface ISignalEmitterService
    {
        /// <summary>
        /// Get all signal emitters from a timeline
        /// </summary>
        /// <param name="timelineAsset">The timeline asset to scan</param>
        /// <returns>List of signal timing information</returns>
        List<SignalTimingInfo> GetAllSignalEmitters(TimelineAsset timelineAsset);
        
        /// <summary>
        /// Find a signal emitter by its display name
        /// </summary>
        /// <param name="timelineAsset">The timeline asset to search in</param>
        /// <param name="signalName">The display name of the signal</param>
        /// <returns>Signal timing information if found</returns>
        SignalTimingInfo? FindSignalEmitterByName(TimelineAsset timelineAsset, string signalName);
        
        /// <summary>
        /// Get recording range from signal emitters with fallback
        /// </summary>
        /// <param name="timelineAsset">The timeline asset</param>
        /// <param name="startTimingName">Start signal name</param>
        /// <param name="endTimingName">End signal name</param>
        /// <param name="allowFallback">Whether to fallback to full timeline range</param>
        /// <returns>Recording range</returns>
        RecordingRange GetRecordingRangeFromSignals(TimelineAsset timelineAsset, string startTimingName, string endTimingName, bool allowFallback = true);
        
        /// <summary>
        /// Check if timeline has valid signal emitters
        /// </summary>
        /// <param name="timelineAsset">The timeline asset</param>
        /// <param name="startTimingName">Start signal name</param>
        /// <param name="endTimingName">End signal name</param>
        /// <returns>True if both signals are found</returns>
        bool HasValidSignalEmitters(TimelineAsset timelineAsset, string startTimingName, string endTimingName);
        
        /// <summary>
        /// Check if timeline has signal track
        /// </summary>
        /// <param name="timelineAsset">The timeline asset</param>
        /// <returns>True if signal track exists</returns>
        bool HasSignalTrack(TimelineAsset timelineAsset);
        
        /// <summary>
        /// Check if timeline has signal track with valid emitters
        /// </summary>
        /// <param name="timelineAsset">The timeline asset</param>
        /// <param name="startTimingName">Start signal name</param>
        /// <param name="endTimingName">End signal name</param>
        /// <returns>True if signal track exists and has valid emitters</returns>
        bool HasSignalTrackWithValidEmitters(TimelineAsset timelineAsset, string startTimingName, string endTimingName);
        
        /// <summary>
        /// Format time display (seconds/frames toggle)
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds</param>
        /// <param name="frameRate">Frame rate</param>
        /// <param name="showAsFrames">Whether to show as frames</param>
        /// <returns>Formatted time string</returns>
        string FormatTimeDisplay(double timeInSeconds, int frameRate, bool showAsFrames);
        
        /// <summary>
        /// Get [MTR] priority tracks
        /// </summary>
        /// <param name="timelineAsset">The timeline asset</param>
        /// <returns>List of tracks with [MTR] prefix</returns>
        List<TrackAsset> GetMTRPriorityTracks(TimelineAsset timelineAsset);
    }
}