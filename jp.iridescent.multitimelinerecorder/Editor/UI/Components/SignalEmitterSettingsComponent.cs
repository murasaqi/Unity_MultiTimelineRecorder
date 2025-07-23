using System;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for SignalEmitter settings UI
    /// </summary>
    public class SignalEmitterSettingsComponent
    {
        private readonly ISignalEmitterService _signalEmitterService;
        private readonly IEventBus _eventBus;
        
        private bool _useSignalEmitterTiming = false;
        private string _startTimingName = "";
        private string _endTimingName = "";
        private bool _showAsFrames = false;
        private bool _isExpanded = true;
        
        public bool UseSignalEmitterTiming => _useSignalEmitterTiming;
        public string StartTimingName => _startTimingName;
        public string EndTimingName => _endTimingName;
        
        public SignalEmitterSettingsComponent(ISignalEmitterService signalEmitterService, IEventBus eventBus)
        {
            _signalEmitterService = signalEmitterService ?? throw new ArgumentNullException(nameof(signalEmitterService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        
        /// <summary>
        /// Draw the SignalEmitter settings UI
        /// </summary>
        public void Draw()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    _isExpanded = EditorGUILayout.Foldout(_isExpanded, "SignalEmitter Settings", true, UIStyles.SectionHeader);
                    
                    GUILayout.FlexibleSpace();
                    
                    // Debug mode toggle
                    bool debugMode = EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false);
                    if (GUILayout.Button(debugMode ? "Debug ON" : "Debug OFF", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        EditorPrefs.SetBool("MTR_SignalEmitterDebugMode", !debugMode);
                    }
                }
                
                if (_isExpanded)
                {
                    EditorGUILayout.Space(5);
                    
                    // Use SignalEmitter timing checkbox
                    EditorGUI.BeginChangeCheck();
                    _useSignalEmitterTiming = EditorGUILayout.Toggle("Use SignalEmitter Timing", _useSignalEmitterTiming);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PublishSettingsChanged();
                    }
                    
                    if (_useSignalEmitterTiming)
                    {
                        EditorGUI.indentLevel++;
                        
                        // Start timing name
                        EditorGUI.BeginChangeCheck();
                        _startTimingName = EditorGUILayout.TextField("Start Signal Name", _startTimingName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            PublishSettingsChanged();
                        }
                        
                        // End timing name
                        EditorGUI.BeginChangeCheck();
                        _endTimingName = EditorGUILayout.TextField("End Signal Name", _endTimingName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            PublishSettingsChanged();
                        }
                        
                        // Time display toggle
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel("Time Display");
                            
                            if (GUILayout.Toggle(!_showAsFrames, "Seconds", EditorStyles.miniButtonLeft))
                            {
                                _showAsFrames = false;
                            }
                            
                            if (GUILayout.Toggle(_showAsFrames, "Frames", EditorStyles.miniButtonRight))
                            {
                                _showAsFrames = true;
                            }
                        }
                        
                        EditorGUILayout.Space(5);
                        
                        // Help box
                        EditorGUILayout.HelpBox(
                            "SignalEmitter timing allows you to define recording start/end points using Signal markers in your Timeline.\n\n" +
                            "• Create Signal Track with [MTR] prefix for priority\n" +
                            "• Add SignalEmitter markers at desired times\n" +
                            "• Set marker names matching Start/End Signal Names above", 
                            MessageType.Info);
                        
                        // Show current signal status
                        DrawSignalStatus();
                        
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the current configuration
        /// </summary>
        public void SetConfiguration(RecordingConfiguration config)
        {
            if (config != null && config.GlobalSettings is GlobalSettings globalSettings)
            {
                _useSignalEmitterTiming = globalSettings.UseSignalEmitterTiming;
                _startTimingName = globalSettings.StartSignalName ?? "";
                _endTimingName = globalSettings.EndSignalName ?? "";
            }
        }
        
        /// <summary>
        /// Apply settings to configuration
        /// </summary>
        public void ApplyToConfiguration(RecordingConfiguration config)
        {
            if (config != null && config.GlobalSettings is GlobalSettings globalSettings)
            {
                globalSettings.UseSignalEmitterTiming = _useSignalEmitterTiming;
                globalSettings.StartSignalName = _startTimingName;
                globalSettings.EndSignalName = _endTimingName;
            }
        }
        
        private void DrawSignalStatus()
        {
            // This would show the status of signal detection for selected timelines
            // For now, just show a placeholder
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Signal Detection Status", EditorStyles.boldLabel);
                
                if (string.IsNullOrEmpty(_startTimingName) || string.IsNullOrEmpty(_endTimingName))
                {
                    EditorGUILayout.LabelField("Enter signal names to check detection", EditorStyles.miniLabel);
                }
                else
                {
                    // In a real implementation, this would check the selected timelines
                    EditorGUILayout.LabelField($"Start Signal: '{_startTimingName}'", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"End Signal: '{_endTimingName}'", EditorStyles.miniLabel);
                }
            }
        }
        
        private void PublishSettingsChanged()
        {
            _eventBus.Publish(new SignalEmitterSettingsChangedEvent
            {
                UseSignalEmitterTiming = _useSignalEmitterTiming,
                StartSignalName = _startTimingName,
                EndSignalName = _endTimingName
            });
        }
    }
    
    /// <summary>
    /// Event fired when SignalEmitter settings change
    /// </summary>
    public class SignalEmitterSettingsChangedEvent : IEvent
    {
        public bool UseSignalEmitterTiming { get; set; }
        public string StartSignalName { get; set; }
        public string EndSignalName { get; set; }
    }
}