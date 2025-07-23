using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using Unity.MultiTimelineRecorder.Utilities;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service for managing SignalEmitter functionality
    /// </summary>
    public class SignalEmitterService : ISignalEmitterService
    {
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private bool _debugMode => EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false);

        public SignalEmitterService(MultiTimelineRecorder.Core.Interfaces.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public List<SignalTimingInfo> GetAllSignalEmitters(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                _logger.LogWarning("Timeline asset is null", LogCategory.Timeline);
                return new List<SignalTimingInfo>();
            }

            _logger.LogVerbose($"Getting all signal emitters from timeline: {timelineAsset.name}", LogCategory.Timeline);
            return SignalEmitterRecordControl.GetAllSignalEmitters(timelineAsset);
        }

        /// <inheritdoc />
        public SignalTimingInfo? FindSignalEmitterByName(TimelineAsset timelineAsset, string signalName)
        {
            if (timelineAsset == null || string.IsNullOrEmpty(signalName))
            {
                return null;
            }

            _logger.LogVerbose($"Finding signal emitter by name: {signalName} in timeline: {timelineAsset.name}", LogCategory.Timeline);
            return SignalEmitterRecordControl.FindSignalEmitterByName(timelineAsset, signalName);
        }

        /// <inheritdoc />
        public RecordingRange GetRecordingRangeFromSignals(TimelineAsset timelineAsset, string startTimingName, string endTimingName, bool allowFallback = true)
        {
            if (timelineAsset == null)
            {
                _logger.LogError("Timeline asset is null when getting recording range", LogCategory.Timeline);
                return RecordingRange.Invalid;
            }

            _logger.LogInfo($"Getting recording range from signals: Start='{startTimingName}', End='{endTimingName}'", LogCategory.Timeline);
            
            var range = SignalEmitterRecordControl.GetRecordingRangeFromSignalsWithFallback(
                timelineAsset, startTimingName, endTimingName, allowFallback);
            
            if (range.isValid)
            {
                _logger.LogInfo($"Recording range: {range.startTime:F2}s - {range.endTime:F2}s (Duration: {range.duration:F2}s)", LogCategory.Timeline);
            }
            else
            {
                _logger.LogWarning("Invalid recording range returned", LogCategory.Timeline);
            }
            
            return range;
        }

        /// <inheritdoc />
        public bool HasValidSignalEmitters(TimelineAsset timelineAsset, string startTimingName, string endTimingName)
        {
            if (timelineAsset == null || string.IsNullOrEmpty(startTimingName) || string.IsNullOrEmpty(endTimingName))
            {
                return false;
            }

            var hasValid = SignalEmitterRecordControl.HasValidSignalEmitters(timelineAsset, startTimingName, endTimingName);
            
            if (_debugMode)
            {
                _logger.LogDebug($"HasValidSignalEmitters: {hasValid} for timeline: {timelineAsset.name}", LogCategory.Timeline);
            }
            
            return hasValid;
        }

        /// <inheritdoc />
        public bool HasSignalTrack(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                return false;
            }

            return SignalEmitterRecordControl.HasSignalTrack(timelineAsset);
        }

        /// <inheritdoc />
        public bool HasSignalTrackWithValidEmitters(TimelineAsset timelineAsset, string startTimingName, string endTimingName)
        {
            if (timelineAsset == null)
            {
                return false;
            }

            return SignalEmitterRecordControl.HasSignalTrackWithValidEmitters(timelineAsset, startTimingName, endTimingName);
        }

        /// <inheritdoc />
        public string FormatTimeDisplay(double timeInSeconds, int frameRate, bool showAsFrames)
        {
            if (showAsFrames)
            {
                int frames = Mathf.RoundToInt((float)(timeInSeconds * frameRate));
                return $"{frames} frames";
            }
            else
            {
                return $"{timeInSeconds:F2}s";
            }
        }

        /// <inheritdoc />
        public List<TrackAsset> GetMTRPriorityTracks(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                return new List<TrackAsset>();
            }

            var allTracks = timelineAsset.GetOutputTracks().ToList();
            var mtrTracks = allTracks.Where(t => t.name.Contains("[MTR]")).ToList();
            
            if (_debugMode && mtrTracks.Count > 0)
            {
                _logger.LogDebug($"Found {mtrTracks.Count} [MTR] priority tracks in timeline: {timelineAsset.name}", LogCategory.Timeline);
                foreach (var track in mtrTracks)
                {
                    _logger.LogDebug($"  - {track.name}", LogCategory.Timeline);
                }
            }
            
            return mtrTracks;
        }
    }
}