using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for displaying validation messages and errors
    /// </summary>
    public class ValidationPanelComponent
    {
        private readonly MainWindowController _controller;
        private readonly IEventBus _eventBus;
        private List<ValidationMessage> _messages = new List<ValidationMessage>();
        private Vector2 _scrollPosition;
        private bool _autoScroll = true;
        
        public ValidationPanelComponent(MainWindowController controller, IEventBus eventBus)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus.Subscribe<ValidationEvent>(OnValidationEvent);
            _eventBus.Subscribe<RecordingStartedEvent>(OnRecordingStarted);
            _eventBus.Subscribe<RecordingCompletedEvent>(OnRecordingCompleted);
            _eventBus.Subscribe<RecordingErrorEvent>(OnRecordingError);
        }
        
        public void Draw()
        {
            // Header with controls
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Validation Messages", UIStyles.SectionHeader);
                
                GUILayout.FlexibleSpace();
                
                _autoScroll = GUILayout.Toggle(_autoScroll, "Auto-scroll", EditorStyles.miniButton, GUILayout.Width(70));
                
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    _messages.Clear();
                }
                
                if (GUILayout.Button("Validate", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    RunValidation();
                }
            }
            
            UIStyles.DrawHorizontalLine();
            
            // Message list
            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;
                
                if (_messages.Count == 0)
                {
                    EditorGUILayout.HelpBox("No validation messages. Click 'Validate' to check configuration.", MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < _messages.Count; i++)
                    {
                        DrawValidationMessage(_messages[i], i);
                    }
                }
            }
            
            // Auto-scroll to bottom
            if (_autoScroll && Event.current.type == EventType.Layout)
            {
                _scrollPosition.y = float.MaxValue;
            }
            
            // Summary bar
            DrawSummaryBar();
        }
        
        private void DrawValidationMessage(ValidationMessage message, int index)
        {
            var messageType = GetMessageType(message.Severity);
            var icon = GetSeverityIcon(message.Severity);
            var color = GetSeverityColor(message.Severity);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // Timestamp
                var timeString = message.Timestamp.ToString("HH:mm:ss");
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label(timeString, EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = Color.white;
                
                // Icon
                GUI.color = color;
                GUILayout.Label(icon, EditorStyles.label, GUILayout.Width(20));
                GUI.color = Color.white;
                
                // Message
                var style = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    richText = true
                };
                
                EditorGUILayout.LabelField(message.Message, style);
                
                // Context button
                if (!string.IsNullOrEmpty(message.Context) && GUILayout.Button("?", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    EditorUtility.DisplayDialog("Validation Context", message.Context, "OK");
                }
            }
            
            // Draw separator between messages
            if (index < _messages.Count - 1)
            {
                var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.3f));
            }
        }
        
        private void DrawSummaryBar()
        {
            var errorCount = _messages.Count(m => m.Severity == ValidationSeverity.Error);
            var warningCount = _messages.Count(m => m.Severity == ValidationSeverity.Warning);
            var infoCount = _messages.Count(m => m.Severity == ValidationSeverity.Info);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (errorCount > 0)
                {
                    GUI.color = UIStyles.ErrorColor;
                    GUILayout.Label($"⊗ {errorCount} Error{(errorCount != 1 ? "s" : "")}", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                
                if (warningCount > 0)
                {
                    GUI.color = UIStyles.WarningColor;
                    GUILayout.Label($"⚠ {warningCount} Warning{(warningCount != 1 ? "s" : "")}", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                
                if (infoCount > 0)
                {
                    GUILayout.Label($"ⓘ {infoCount} Info", EditorStyles.miniLabel);
                }
                
                GUILayout.FlexibleSpace();
                
                var status = errorCount > 0 ? "Failed" : warningCount > 0 ? "Warning" : "Ready";
                var statusColor = errorCount > 0 ? UIStyles.ErrorColor : warningCount > 0 ? UIStyles.WarningColor : UIStyles.SuccessColor;
                
                GUI.color = statusColor;
                GUILayout.Label($"Status: {status}", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
        }
        
        private void RunValidation()
        {
            _messages.Clear();
            AddMessage(ValidationSeverity.Info, "Starting validation...");
            
            // Validate configuration
            var config = _controller.CurrentConfiguration;
            if (config == null)
            {
                AddMessage(ValidationSeverity.Error, "No configuration loaded");
                return;
            }
            
            var result = _controller.ValidateConfiguration();
            
            if (result.IsValid)
            {
                AddMessage(ValidationSeverity.Info, "✓ Configuration is valid");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    AddMessage(ValidationSeverity.Error, error);
                }
            }
            
            // Validate timelines
            var selectedTimelines = _controller.SelectedTimelines;
            if (selectedTimelines.Count == 0)
            {
                AddMessage(ValidationSeverity.Warning, "No timelines selected");
            }
            else
            {
                AddMessage(ValidationSeverity.Info, $"Found {selectedTimelines.Count} timeline(s)");
                
                foreach (var timeline in selectedTimelines)
                {
                    if (timeline == null)
                    {
                        AddMessage(ValidationSeverity.Error, "Null timeline reference detected");
                    }
                    else if (timeline.playableAsset == null)
                    {
                        AddMessage(ValidationSeverity.Error, $"Timeline '{timeline.name}' has no playable asset");
                    }
                    else
                    {
                        AddMessage(ValidationSeverity.Info, $"✓ Timeline '{timeline.name}' is valid");
                    }
                }
            }
            
            // Validate recorders
            var recorderCount = 0;
            if (config is RecordingConfiguration recordingConfig)
            {
                foreach (var timelineConfig in recordingConfig.TimelineConfigs)
                {
                    if (timelineConfig.IsEnabled)
                    {
                        foreach (var recorder in timelineConfig.RecorderConfigs)
                        {
                            if (recorder.IsEnabled)
                            {
                                recorderCount++;
                                var recorderResult = recorder.Validate();
                                if (!recorderResult.IsValid)
                                {
                                    foreach (var error in recorderResult.Errors)
                                    {
                                        AddMessage(ValidationSeverity.Error, $"Recorder '{recorder.Name}': {error}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            if (recorderCount == 0)
            {
                AddMessage(ValidationSeverity.Warning, "No recorders enabled");
            }
            else
            {
                AddMessage(ValidationSeverity.Info, $"Found {recorderCount} enabled recorder(s)");
            }
            
            // Check output paths
            if (config is RecordingConfiguration rc && rc.GlobalSettings != null)
            {
                if (string.IsNullOrEmpty(rc.GlobalSettings.BaseOutputPath))
                {
                    AddMessage(ValidationSeverity.Warning, "No base output path specified");
                }
                else if (!System.IO.Directory.Exists(rc.GlobalSettings.BaseOutputPath))
                {
                    if (rc.GlobalSettings.AutoCreateDirectories)
                    {
                        AddMessage(ValidationSeverity.Info, "Output directory will be created automatically");
                    }
                    else
                    {
                        AddMessage(ValidationSeverity.Warning, "Output directory does not exist");
                    }
                }
            }
            
            AddMessage(ValidationSeverity.Info, "Validation complete");
        }
        
        private void AddMessage(ValidationSeverity severity, string message, string context = null)
        {
            _messages.Add(new ValidationMessage
            {
                Severity = severity,
                Message = message,
                Context = context,
                Timestamp = DateTime.Now
            });
            
            // Limit message count
            if (_messages.Count > 1000)
            {
                _messages.RemoveRange(0, 100);
            }
        }
        
        private MessageType GetMessageType(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Error:
                    return MessageType.Error;
                case ValidationSeverity.Warning:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }
        
        private string GetSeverityIcon(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Error:
                    return "⊗";
                case ValidationSeverity.Warning:
                    return "⚠";
                default:
                    return "ⓘ";
            }
        }
        
        private Color GetSeverityColor(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Error:
                    return UIStyles.ErrorColor;
                case ValidationSeverity.Warning:
                    return UIStyles.WarningColor;
                default:
                    return Color.white;
            }
        }
        
        // Event handlers
        private void OnValidationEvent(ValidationEvent e)
        {
            AddMessage(e.Severity, e.Message, e.Context);
        }
        
        private void OnRecordingStarted(RecordingStartedEvent e)
        {
            AddMessage(ValidationSeverity.Info, $"Recording started (Job ID: {e.JobId})");
        }
        
        private void OnRecordingCompleted(RecordingCompletedEvent e)
        {
            if (e.Result.IsSuccess)
            {
                AddMessage(ValidationSeverity.Info, $"✓ Recording completed successfully", 
                    $"Output: {e.Result.OutputPath}\nDuration: {e.Result.Duration:g}");
            }
            else
            {
                AddMessage(ValidationSeverity.Error, $"Recording failed: {e.Result.ErrorMessage}");
            }
        }
        
        private void OnRecordingError(RecordingErrorEvent e)
        {
            AddMessage(ValidationSeverity.Error, $"Recording error: {e.Error}", e.StackTrace);
        }
        
        private class ValidationMessage
        {
            public ValidationSeverity Severity { get; set; }
            public string Message { get; set; }
            public string Context { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        private enum ValidationSeverity
        {
            Info,
            Warning,
            Error
        }
    }
}