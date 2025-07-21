using System;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for managing global settings
    /// </summary>
    public class GlobalSettingsComponent
    {
        private readonly MainWindowController _controller;
        private readonly IEventBus _eventBus;
        private GlobalSettings _settings;
        
        public GlobalSettingsComponent(MainWindowController controller, IEventBus eventBus)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus.Subscribe<ConfigurationLoadedEvent>(OnConfigurationLoaded);
            _eventBus.Subscribe<ConfigurationChangedEvent>(OnConfigurationChanged);
            
            LoadSettings();
        }
        
        public void Draw()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("No configuration loaded", MessageType.Info);
                if (GUILayout.Button("Create New Configuration"))
                {
                    _controller.CreateNewConfiguration();
                }
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            
            // Output Settings Section
            GUILayout.Label("Output Settings", UIStyles.SectionHeader);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Base output path
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Base Output Path", GUILayout.Width(UIStyles.FieldLabelWidth));
                _settings.BaseOutputPath = EditorGUILayout.TextField(_settings.BaseOutputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var newPath = EditorUtility.OpenFolderPanel("Select Output Directory", _settings.BaseOutputPath, "");
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        _settings.BaseOutputPath = newPath;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // Use scene directory
                _settings.UseSceneDirectory = EditorGUILayout.Toggle("Use Scene Directory", _settings.UseSceneDirectory);
                
                // Output organization
                _settings.OrganizeByTimeline = EditorGUILayout.Toggle("Organize by Timeline", _settings.OrganizeByTimeline);
                _settings.OrganizeByRecorderType = EditorGUILayout.Toggle("Organize by Recorder Type", _settings.OrganizeByRecorderType);
                
                // Auto-create directories
                _settings.AutoCreateDirectories = EditorGUILayout.Toggle("Auto-create Directories", _settings.AutoCreateDirectories);
            }
            
            UIStyles.DrawHorizontalLine();
            
            // Recording Settings Section
            GUILayout.Label("Recording Settings", UIStyles.SectionHeader);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Frame rate
                _settings.DefaultFrameRate = EditorGUILayout.IntSlider("Default Frame Rate", _settings.DefaultFrameRate, 24, 120);
                
                // Resolution presets
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Resolution Preset", GUILayout.Width(UIStyles.FieldLabelWidth));
                if (GUILayout.Button("HD (1920x1080)", EditorStyles.miniButtonLeft))
                {
                    _settings.DefaultWidth = 1920;
                    _settings.DefaultHeight = 1080;
                }
                if (GUILayout.Button("4K (3840x2160)", EditorStyles.miniButtonMid))
                {
                    _settings.DefaultWidth = 3840;
                    _settings.DefaultHeight = 2160;
                }
                if (GUILayout.Button("8K (7680x4320)", EditorStyles.miniButtonRight))
                {
                    _settings.DefaultWidth = 7680;
                    _settings.DefaultHeight = 4320;
                }
                EditorGUILayout.EndHorizontal();
                
                // Custom resolution
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Resolution", GUILayout.Width(UIStyles.FieldLabelWidth));
                _settings.DefaultWidth = EditorGUILayout.IntField(_settings.DefaultWidth, GUILayout.Width(60));
                EditorGUILayout.LabelField("x", GUILayout.Width(15));
                _settings.DefaultHeight = EditorGUILayout.IntField(_settings.DefaultHeight, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // Quality settings
                _settings.DefaultQuality = EditorGUILayout.IntSlider("Default Quality", _settings.DefaultQuality, 0, 100);
                _settings.UseMotionBlur = EditorGUILayout.Toggle("Use Motion Blur", _settings.UseMotionBlur);
            }
            
            UIStyles.DrawHorizontalLine();
            
            // Workflow Settings Section
            GUILayout.Label("Workflow Settings", UIStyles.SectionHeader);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _settings.AutoSaveBeforeRecording = EditorGUILayout.Toggle("Auto-save Before Recording", _settings.AutoSaveBeforeRecording);
                _settings.ShowPreviewWindow = EditorGUILayout.Toggle("Show Preview Window", _settings.ShowPreviewWindow);
                _settings.ValidateBeforeRecording = EditorGUILayout.Toggle("Validate Before Recording", _settings.ValidateBeforeRecording);
                _settings.OpenOutputFolderAfterRecording = EditorGUILayout.Toggle("Open Output Folder After Recording", _settings.OpenOutputFolderAfterRecording);
            }
            
            UIStyles.DrawHorizontalLine();
            
            // Advanced Settings Section
            GUILayout.Label("Advanced Settings", UIStyles.SectionHeader);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _settings.MaxConcurrentRecorders = EditorGUILayout.IntSlider("Max Concurrent Recorders", _settings.MaxConcurrentRecorders, 1, 4);
                _settings.UseAsyncRecording = EditorGUILayout.Toggle("Use Async Recording", _settings.UseAsyncRecording);
                _settings.CaptureAudio = EditorGUILayout.Toggle("Capture Audio", _settings.CaptureAudio);
                _settings.LogVerbosity = (Unity.MultiTimelineRecorder.LogVerbosity)EditorGUILayout.EnumPopup("Log Verbosity", _settings.LogVerbosity);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                // Notify about changes
                _eventBus.Publish(new ConfigurationChangedEvent 
                { 
                    Configuration = _controller.CurrentConfiguration,
                    PropertyName = "GlobalSettings"
                });
                
                // Save configuration
                _controller.SaveConfiguration();
            }
            
            UIStyles.DrawHorizontalLine();
            
            // Configuration Management
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Configuration", GUILayout.Height(25)))
                {
                    _controller.SaveConfiguration();
                }
                
                if (GUILayout.Button("Load Configuration", GUILayout.Height(25)))
                {
                    _controller.LoadConfiguration();
                }
                
                if (GUILayout.Button("Reset to Defaults", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Reset Settings", 
                        "Are you sure you want to reset all settings to defaults?", 
                        "Reset", "Cancel"))
                    {
                        _settings = new GlobalSettings();
                        _controller.UpdateGlobalSettings(_settings);
                    }
                }
            }
        }
        
        private void LoadSettings()
        {
            var config = _controller.CurrentConfiguration as RecordingConfiguration;
            if (config != null)
            {
                _settings = config.GlobalSettings ?? new GlobalSettings();
            }
            else
            {
                _settings = new GlobalSettings();
            }
        }
        
        private void OnConfigurationLoaded(ConfigurationLoadedEvent e)
        {
            LoadSettings();
            _eventBus.Publish(new UIRefreshRequestedEvent 
            { 
                Scope = UIRefreshRequestedEvent.RefreshScope.All 
            });
        }
        
        private void OnConfigurationChanged(ConfigurationChangedEvent e)
        {
            if (e.PropertyName == "GlobalSettings")
            {
                LoadSettings();
            }
        }
    }
}