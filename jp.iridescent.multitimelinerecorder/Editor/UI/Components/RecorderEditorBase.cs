using System;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Base class for recorder-specific editor components
    /// </summary>
    public abstract class RecorderEditorBase : IRecorderEditor
    {
        protected readonly IRecorderConfiguration _config;
        protected readonly RecorderConfigurationController _controller;
        protected readonly IEventBus _eventBus;
        
        protected RecorderEditorBase(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        
        public virtual void Draw()
        {
            EditorGUI.BeginChangeCheck();
            
            // Common settings
            DrawCommonSettings();
            
            UIStyles.DrawHorizontalLine();
            
            // Recorder-specific settings
            DrawRecorderSpecificSettings();
            
            if (EditorGUI.EndChangeCheck())
            {
                OnSettingsChanged();
            }
            
            UIStyles.DrawHorizontalLine();
            
            // Actions
            DrawActions();
        }
        
        protected virtual void DrawCommonSettings()
        {
            // Name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newName = EditorGUILayout.TextField(_config.Name);
            if (newName != _config.Name)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_config.Name), _config.Name, newName);
                _config.Name = newName;
            }
            EditorGUILayout.EndHorizontal();
            
            // Enabled
            var wasEnabled = _config.IsEnabled;
            _config.IsEnabled = EditorGUILayout.Toggle("Enabled", _config.IsEnabled);
            if (wasEnabled != _config.IsEnabled)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_config.IsEnabled), wasEnabled, _config.IsEnabled);
            }
            
            // Type (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Type", _config.Type);
            EditorGUI.EndDisabledGroup();
        }
        
        protected abstract void DrawRecorderSpecificSettings();
        
        protected virtual void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to Defaults", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Reset Settings", 
                        "Are you sure you want to reset this recorder to default settings?", 
                        "Reset", "Cancel"))
                    {
                        ResetToDefaults();
                    }
                }
                
                if (GUILayout.Button("Duplicate", GUILayout.Height(25)))
                {
                    _controller.DuplicateRecorder(_config.Id);
                }
                
                GUI.color = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Recorder", 
                        $"Are you sure you want to delete '{_config.Name}'?", 
                        "Delete", "Cancel"))
                    {
                        _controller.RemoveRecorder(_config.Id);
                    }
                }
                GUI.color = Color.white;
            }
        }
        
        protected virtual void OnSettingsChanged()
        {
            // Validate and update
            string errorMessage;
            if (!Validate(out errorMessage))
            {
                _eventBus.Publish(new ValidationEvent
                {
                    Severity = MultiTimelineRecorder.Core.Interfaces.ValidationSeverity.Error,
                    Message = $"Recorder '{_config.Name}': {errorMessage}"
                });
            }
            
            // Notify about changes
            _eventBus.Publish(new RecorderConfigurationChangedEvent
            {
                RecorderConfig = _config
            });
        }
        
        public abstract bool Validate(out string errorMessage);
        
        public abstract void ResetToDefaults();
        
        // Helper methods for common UI patterns
        protected void DrawPathField(string label, string currentPath, Action<string> onPathChanged, string dialogTitle = "Select Path")
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(UIStyles.FieldLabelWidth));
            
            var newPath = EditorGUILayout.TextField(currentPath);
            if (newPath != currentPath)
            {
                onPathChanged(newPath);
            }
            
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel(dialogTitle, currentPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    onPathChanged(selectedPath);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        protected void DrawEnumField<T>(string label, T currentValue, Action<T> onValueChanged) where T : Enum
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(UIStyles.FieldLabelWidth));
            
            var newValue = (T)EditorGUILayout.EnumPopup(currentValue);
            if (!newValue.Equals(currentValue))
            {
                onValueChanged(newValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        protected void DrawIntSlider(string label, int currentValue, int min, int max, Action<int> onValueChanged)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(UIStyles.FieldLabelWidth));
            
            var newValue = EditorGUILayout.IntSlider(currentValue, min, max);
            if (newValue != currentValue)
            {
                onValueChanged(newValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        protected void DrawFloatSlider(string label, float currentValue, float min, float max, Action<float> onValueChanged)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(UIStyles.FieldLabelWidth));
            
            var newValue = EditorGUILayout.Slider(currentValue, min, max);
            if (!Mathf.Approximately(newValue, currentValue))
            {
                onValueChanged(newValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    /// <summary>
    /// Validation event severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
    
    /// <summary>
    /// Validation event
    /// </summary>
    public class ValidationEvent : EventArgs
    {
        public MultiTimelineRecorder.Core.Interfaces.ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Context { get; set; }
    }
}