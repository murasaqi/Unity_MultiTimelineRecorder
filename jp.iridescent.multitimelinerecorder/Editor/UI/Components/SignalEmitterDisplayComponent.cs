using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using Unity.MultiTimelineRecorder.Utilities;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for displaying SignalEmitter information on timelines
    /// </summary>
    public class SignalEmitterDisplayComponent
    {
        private readonly ISignalEmitterService _signalEmitterService;
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private readonly IEventBus _eventBus;
        
        private bool _showAsFrames = false;
        private Dictionary<TimelineAsset, List<SignalTimingInfo>> _cachedSignals = new Dictionary<TimelineAsset, List<SignalTimingInfo>>();
        
        public SignalEmitterDisplayComponent(ISignalEmitterService signalEmitterService, MultiTimelineRecorder.Core.Interfaces.ILogger logger, IEventBus eventBus)
        {
            _signalEmitterService = signalEmitterService ?? throw new ArgumentNullException(nameof(signalEmitterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        
        /// <summary>
        /// Draw signal emitter display for a timeline
        /// </summary>
        public void DrawForTimeline(TimelineAsset timeline, int frameRate, string startSignalName, string endSignalName)
        {
            if (timeline == null) return;
            
            // Get or update cached signals
            if (!_cachedSignals.ContainsKey(timeline))
            {
                RefreshSignalsForTimeline(timeline);
            }
            
            var signals = _cachedSignals[timeline];
            if (signals == null || signals.Count == 0)
            {
                EditorGUILayout.HelpBox("No signal emitters found in this timeline", MessageType.Info);
                return;
            }
            
            // Draw signal list
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Header with refresh button
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Signal Emitters", EditorStyles.boldLabel);
                    
                    GUILayout.FlexibleSpace();
                    
                    // Time display toggle
                    if (GUILayout.Toggle(!_showAsFrames, "Seconds", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                    {
                        _showAsFrames = false;
                    }
                    
                    if (GUILayout.Toggle(_showAsFrames, "Frames", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    {
                        _showAsFrames = true;
                    }
                    
                    if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        RefreshSignalsForTimeline(timeline);
                    }
                }
                
                EditorGUILayout.Space(5);
                
                // Signal list
                var mtrSignals = signals.Where(s => s.trackName.Contains("[MTR]")).ToList();
                var otherSignals = signals.Where(s => !s.trackName.Contains("[MTR]")).ToList();
                
                // Draw [MTR] priority signals first
                if (mtrSignals.Count > 0)
                {
                    EditorGUILayout.LabelField("[MTR] Priority Signals:", EditorStyles.miniBoldLabel);
                    foreach (var signal in mtrSignals)
                    {
                        DrawSignalInfo(signal, frameRate, startSignalName, endSignalName);
                    }
                    
                    if (otherSignals.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                    }
                }
                
                // Draw other signals
                if (otherSignals.Count > 0)
                {
                    EditorGUILayout.LabelField("Other Signals:", EditorStyles.miniBoldLabel);
                    foreach (var signal in otherSignals)
                    {
                        DrawSignalInfo(signal, frameRate, startSignalName, endSignalName);
                    }
                }
                
                // Show recording range if signals are selected
                if (!string.IsNullOrEmpty(startSignalName) && !string.IsNullOrEmpty(endSignalName))
                {
                    EditorGUILayout.Space(10);
                    DrawRecordingRangeInfo(timeline, frameRate, startSignalName, endSignalName);
                }
            }
        }
        
        private void DrawSignalInfo(SignalTimingInfo signal, int frameRate, string startSignalName, string endSignalName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Signal name with highlighting
                bool isStart = signal.displayName == startSignalName;
                bool isEnd = signal.displayName == endSignalName;
                
                var style = EditorStyles.label;
                if (isStart || isEnd)
                {
                    style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = isStart ? Color.green : Color.red;
                    style.fontStyle = FontStyle.Bold;
                }
                
                var label = signal.displayName;
                if (isStart) label += " [START]";
                if (isEnd) label += " [END]";
                
                EditorGUILayout.LabelField(label, style, GUILayout.Width(200));
                
                // Time display
                var timeStr = _signalEmitterService.FormatTimeDisplay(signal.time, frameRate, _showAsFrames);
                EditorGUILayout.LabelField(timeStr, GUILayout.Width(100));
                
                // Track name
                EditorGUILayout.LabelField($"Track: {signal.trackName}", EditorStyles.miniLabel);
            }
        }
        
        private void DrawRecordingRangeInfo(TimelineAsset timeline, int frameRate, string startSignalName, string endSignalName)
        {
            var range = _signalEmitterService.GetRecordingRangeFromSignals(timeline, startSignalName, endSignalName, true);
            
            if (range.isValid)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Recording Range:", EditorStyles.boldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Start:", GUILayout.Width(50));
                        var startStr = _signalEmitterService.FormatTimeDisplay(range.startTime, frameRate, _showAsFrames);
                        EditorGUILayout.LabelField(startStr, EditorStyles.boldLabel);
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("End:", GUILayout.Width(50));
                        var endStr = _signalEmitterService.FormatTimeDisplay(range.endTime, frameRate, _showAsFrames);
                        EditorGUILayout.LabelField(endStr, EditorStyles.boldLabel);
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Duration:", GUILayout.Width(50));
                        var durationStr = _signalEmitterService.FormatTimeDisplay(range.duration, frameRate, _showAsFrames);
                        EditorGUILayout.LabelField(durationStr, EditorStyles.boldLabel);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Could not find valid signals: Start='{startSignalName}', End='{endSignalName}'", MessageType.Warning);
            }
        }
        
        private void RefreshSignalsForTimeline(TimelineAsset timeline)
        {
            if (timeline == null) return;
            
            var signals = _signalEmitterService.GetAllSignalEmitters(timeline);
            _cachedSignals[timeline] = signals;
            
            _logger.LogVerbose($"Refreshed {signals.Count} signals for timeline: {timeline.name}", LogCategory.Timeline);
        }
        
        /// <summary>
        /// Clear cached signals
        /// </summary>
        public void ClearCache()
        {
            _cachedSignals.Clear();
        }
    }
}