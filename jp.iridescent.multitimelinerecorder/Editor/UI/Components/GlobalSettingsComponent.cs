using System;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
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
        private SignalEmitterSettingsComponent _signalEmitterSettings;
        
        public GlobalSettingsComponent(MainWindowController controller, IEventBus eventBus)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            // Get SignalEmitterService from ServiceLocator
            var signalEmitterService = ServiceLocator.Instance.Get<ISignalEmitterService>();
            _signalEmitterSettings = new SignalEmitterSettingsComponent(signalEmitterService, _eventBus);
            
            _eventBus.Subscribe<ConfigurationLoadedEvent>(OnConfigurationLoaded);
            _eventBus.Subscribe<ConfigurationChangedEvent>(OnConfigurationChanged);
            _eventBus.Subscribe<SignalEmitterSettingsChangedEvent>(OnSignalEmitterSettingsChanged);
            
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
                var outputPath = _settings.DefaultOutputPath as OutputPathConfiguration;
                if (outputPath != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Base Output Path", GUILayout.Width(UIStyles.FieldLabelWidth));
                    outputPath.BaseDirectory = EditorGUILayout.TextField(outputPath.BaseDirectory);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var newPath = EditorUtility.OpenFolderPanel("Select Output Directory", outputPath.BaseDirectory, "");
                        if (!string.IsNullOrEmpty(newPath))
                        {
                            outputPath.BaseDirectory = newPath;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Output organization
                    outputPath.CreateTimelineSubdirectories = EditorGUILayout.Toggle("Organize by Timeline", outputPath.CreateTimelineSubdirectories);
                    outputPath.CreateRecorderTypeSubdirectories = EditorGUILayout.Toggle("Organize by Recorder Type", outputPath.CreateRecorderTypeSubdirectories);
                    
                    // Filename pattern
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Filename Pattern", GUILayout.Width(UIStyles.FieldLabelWidth));
                    outputPath.FilenamePattern = EditorGUILayout.TextField(outputPath.FilenamePattern);
                    EditorGUILayout.EndHorizontal();
                }
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
                    _settings.DefaultResolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(1920, 1080);
                }
                if (GUILayout.Button("4K (3840x2160)", EditorStyles.miniButtonMid))
                {
                    _settings.DefaultResolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(3840, 2160);
                }
                if (GUILayout.Button("8K (7680x4320)", EditorStyles.miniButtonRight))
                {
                    _settings.DefaultResolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(7680, 4320);
                }
                EditorGUILayout.EndHorizontal();
                
                // Custom resolution
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Resolution", GUILayout.Width(UIStyles.FieldLabelWidth));
                var width = EditorGUILayout.IntField(_settings.DefaultResolution.Width, GUILayout.Width(60));
                EditorGUILayout.LabelField("x", GUILayout.Width(15));
                var height = EditorGUILayout.IntField(_settings.DefaultResolution.Height, GUILayout.Width(60));
                if (width != _settings.DefaultResolution.Width || height != _settings.DefaultResolution.Height)
                {
                    _settings.DefaultResolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(width, height);
                }
                EditorGUILayout.EndHorizontal();
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
            
            // SignalEmitter Settings Section
            _signalEmitterSettings.Draw();
            
            UIStyles.DrawHorizontalLine();
            
            // Advanced Settings Section
            GUILayout.Label("Advanced Settings", UIStyles.SectionHeader);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _settings.MaxConcurrentRecorders = EditorGUILayout.IntSlider("Max Concurrent Recorders", _settings.MaxConcurrentRecorders, 1, 4);
                _settings.UseAsyncRecording = EditorGUILayout.Toggle("Use Async Recording", _settings.UseAsyncRecording);
                _settings.CaptureAudio = EditorGUILayout.Toggle("Capture Audio", _settings.CaptureAudio);
                _settings.LogVerbosity = (LogVerbosity)EditorGUILayout.EnumPopup("Log Verbosity", _settings.LogVerbosity);
                _settings.DebugMode = EditorGUILayout.Toggle("Debug Mode", _settings.DebugMode);
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
                        _settings = GlobalSettings.CreateDefault();
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
                _settings = config.GlobalSettings as GlobalSettings ?? new GlobalSettings();
                // Update SignalEmitter settings
                _signalEmitterSettings.SetConfiguration(config);
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
        
        private void OnSignalEmitterSettingsChanged(SignalEmitterSettingsChangedEvent e)
        {
            // Apply SignalEmitter settings to current configuration
            var config = _controller.CurrentConfiguration as RecordingConfiguration;
            if (config != null)
            {
                _signalEmitterSettings.ApplyToConfiguration(config);
                _controller.SaveConfiguration();
            }
        }
    }
}