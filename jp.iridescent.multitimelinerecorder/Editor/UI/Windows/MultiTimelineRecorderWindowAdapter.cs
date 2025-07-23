using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.UI.Controllers;

namespace MultiTimelineRecorder.UI.Windows
{
    /// <summary>
    /// Adapter class to integrate existing MultiTimelineRecorder UI with new architecture
    /// </summary>
    public class MultiTimelineRecorderWindowAdapter
    {
        private readonly MultiTimelineRecorder _multiTimelineRecorder;
        private readonly MainWindowController _mainController;
        private readonly RecorderConfigurationController _recorderController;
        private readonly IEventBus _eventBus;
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        
        // State synchronization
        private bool _isSynchronizing = false;
        
        public MultiTimelineRecorderWindowAdapter(
            MultiTimelineRecorder multiTimelineRecorder,
            MainWindowController mainController,
            RecorderConfigurationController recorderController,
            IEventBus eventBus,
            MultiTimelineRecorder.Core.Interfaces.ILogger logger)
        {
            _multiTimelineRecorder = multiTimelineRecorder ?? throw new ArgumentNullException(nameof(multiTimelineRecorder));
            _mainController = mainController ?? throw new ArgumentNullException(nameof(mainController));
            _recorderController = recorderController ?? throw new ArgumentNullException(nameof(recorderController));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            Initialize();
        }
        
        private void Initialize()
        {
            // Subscribe to UI events from MultiTimelineRecorder
            SubscribeToLegacyEvents();
            
            // Subscribe to new architecture events
            SubscribeToNewEvents();
            
            // Initial synchronization
            SynchronizeState();
        }
        
        /// <summary>
        /// Synchronizes state from legacy UI to new architecture
        /// </summary>
        public void SynchronizeState()
        {
            if (_isSynchronizing) return;
            
            _isSynchronizing = true;
            try
            {
                // Sync selected timelines
                SynchronizeTimelines();
                
                // Sync global settings
                SynchronizeGlobalSettings();
                
                // Sync recorder configurations
                SynchronizeRecorderConfigurations();
                
                _logger.LogInfo("State synchronized from legacy UI", LogCategory.UI);
            }
            finally
            {
                _isSynchronizing = false;
            }
        }
        
        /// <summary>
        /// Handles timeline addition from legacy UI
        /// </summary>
        public void HandleTimelineAdded(PlayableDirector director)
        {
            if (_isSynchronizing) return;
            
            _mainController.AddTimeline(director);
            _logger.LogInfo($"Timeline added through legacy UI: {director.name}", LogCategory.UI);
        }
        
        /// <summary>
        /// Handles timeline removal from legacy UI
        /// </summary>
        public void HandleTimelineRemoved(PlayableDirector director)
        {
            if (_isSynchronizing) return;
            
            _mainController.RemoveTimeline(director);
            _logger.LogInfo($"Timeline removed through legacy UI: {director.name}", LogCategory.UI);
        }
        
        /// <summary>
        /// Handles recorder addition from legacy UI
        /// </summary>
        public void HandleRecorderAdded(int timelineIndex, RecorderSettingsType recorderType)
        {
            if (_isSynchronizing) return;
            
            // Get timeline configuration
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            if (config == null || timelineIndex < 0 || timelineIndex >= config.TimelineConfigs.Count)
                return;
            
            var timelineConfig = config.TimelineConfigs[timelineIndex] as TimelineRecorderConfig;
            if (timelineConfig == null) return;
            
            // Set current timeline in recorder controller
            _recorderController.SetTimelineConfig(timelineConfig);
            
            // Add recorder
            _recorderController.AddRecorder(recorderType);
            
            _logger.LogInfo($"Recorder added through legacy UI: {recorderType}", LogCategory.UI);
        }
        
        /// <summary>
        /// Handles recorder removal from legacy UI
        /// </summary>
        public void HandleRecorderRemoved(int timelineIndex, string recorderId)
        {
            if (_isSynchronizing) return;
            
            // Get timeline configuration
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            if (config == null || timelineIndex < 0 || timelineIndex >= config.TimelineConfigs.Count)
                return;
            
            var timelineConfig = config.TimelineConfigs[timelineIndex] as TimelineRecorderConfig;
            if (timelineConfig == null) return;
            
            // Set current timeline in recorder controller
            _recorderController.SetTimelineConfig(timelineConfig);
            
            // Remove recorder
            _recorderController.RemoveRecorder(recorderId);
            
            _logger.LogInfo($"Recorder removed through legacy UI: {recorderId}", LogCategory.UI);
        }
        
        /// <summary>
        /// Handles recording start from legacy UI
        /// </summary>
        public void HandleStartRecording()
        {
            if (_isSynchronizing) return;
            
            _mainController.StartRecording();
            _logger.LogInfo("Recording started from legacy UI", LogCategory.Recording);
        }
        
        /// <summary>
        /// Handles recording stop from legacy UI
        /// </summary>
        public void HandleStopRecording()
        {
            if (_isSynchronizing) return;
            
            _mainController.StopRecording();
            _logger.LogInfo("Recording stopped from legacy UI", LogCategory.Recording);
        }
        
        /// <summary>
        /// Handles global settings update from legacy UI
        /// </summary>
        public void HandleGlobalSettingsChanged()
        {
            if (_isSynchronizing) return;
            
            // Extract global settings from legacy UI
            var globalSettings = ExtractGlobalSettingsFromLegacyUI();
            _mainController.UpdateGlobalSettings(globalSettings);
            
            _logger.LogInfo("Global settings updated from legacy UI", LogCategory.Configuration);
        }
        
        /// <summary>
        /// Handles recorder configuration update from legacy UI
        /// </summary>
        public void HandleRecorderConfigurationChanged(int timelineIndex, int recorderIndex)
        {
            if (_isSynchronizing) return;
            
            // Get the recorder configuration from legacy UI
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            if (config == null || timelineIndex < 0 || timelineIndex >= config.TimelineConfigs.Count)
                return;
            
            var timelineConfig = config.TimelineConfigs[timelineIndex] as TimelineRecorderConfig;
            if (timelineConfig == null || recorderIndex < 0 || recorderIndex >= timelineConfig.RecorderConfigs.Count)
                return;
            
            var recorderConfig = timelineConfig.RecorderConfigs[recorderIndex];
            
            // Update through recorder controller
            _recorderController.SetTimelineConfig(timelineConfig);
            _recorderController.UpdateRecorderConfig(recorderConfig);
            
            _logger.LogInfo($"Recorder configuration updated from legacy UI", LogCategory.Configuration);
        }
        
        /// <summary>
        /// Updates legacy UI when new architecture state changes
        /// </summary>
        public void UpdateLegacyUI()
        {
            if (_isSynchronizing) return;
            
            _isSynchronizing = true;
            try
            {
                // Update timeline list in legacy UI
                UpdateLegacyTimelineList();
                
                // Update recorder configurations in legacy UI
                UpdateLegacyRecorderConfigurations();
                
                // Update global settings in legacy UI
                UpdateLegacyGlobalSettings();
                
                // Trigger UI repaint
                if (_multiTimelineRecorder != null)
                {
                    _multiTimelineRecorder.Repaint();
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }
        
        private void SynchronizeTimelines()
        {
            var legacyTimelines = GetLegacySelectedTimelines();
            var currentTimelines = _mainController.SelectedTimelines.ToList();
            
            // Add timelines that exist in legacy but not in new
            foreach (var timeline in legacyTimelines)
            {
                if (!currentTimelines.Contains(timeline))
                {
                    _mainController.AddTimeline(timeline);
                }
            }
            
            // Remove timelines that exist in new but not in legacy
            foreach (var timeline in currentTimelines)
            {
                if (!legacyTimelines.Contains(timeline))
                {
                    _mainController.RemoveTimeline(timeline);
                }
            }
        }
        
        private void SynchronizeGlobalSettings()
        {
            var globalSettings = ExtractGlobalSettingsFromLegacyUI();
            _mainController.UpdateGlobalSettings(globalSettings);
        }
        
        private void SynchronizeRecorderConfigurations()
        {
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            if (config == null) return;
            
            // Synchronize each timeline's recorder configurations
            var legacyConfigs = GetLegacyRecorderConfigurations();
            
            for (int i = 0; i < config.TimelineConfigs.Count && i < legacyConfigs.Count; i++)
            {
                var timelineConfig = config.TimelineConfigs[i] as TimelineRecorderConfig;
                var legacyTimelineConfig = legacyConfigs[i];
                
                if (timelineConfig != null && legacyTimelineConfig != null)
                {
                    // Sync recorder configurations
                    SynchronizeTimelineRecorders(timelineConfig, legacyTimelineConfig);
                }
            }
        }
        
        private void SynchronizeTimelineRecorders(TimelineRecorderConfig timelineConfig, MultiTimelineRecorderSettings.TimelineSettingEntry legacyConfig)
        {
            // Clear existing recorders
            timelineConfig.RecorderConfigs.Clear();
            
            // Add recorders from legacy config
            if (legacyConfig.multiRecorderConfig != null && legacyConfig.multiRecorderConfig.RecorderItems != null)
            {
                foreach (var legacyRecorder in legacyConfig.multiRecorderConfig.RecorderItems)
                {
                    var recorderConfig = ConvertLegacyRecorderConfig(legacyRecorder);
                    if (recorderConfig != null)
                    {
                        timelineConfig.RecorderConfigs.Add(recorderConfig);
                    }
                }
            }
        }
        
        private IRecorderConfiguration ConvertLegacyRecorderConfig(RecorderConfig legacyConfig)
        {
            // Create appropriate recorder configuration based on type
            IRecorderConfiguration recorderConfig = null;
            
            switch (legacyConfig.selectedRecorderType)
            {
                case RecorderSettingsType.Image:
                    recorderConfig = new ImageRecorderConfiguration
                    {
                        Name = legacyConfig.name,
                        IsEnabled = legacyConfig.enabled,
                        TakeNumber = legacyConfig.Take
                    };
                    break;
                    
                // Add other recorder types as needed
                default:
                    _logger.LogWarning($"Unknown recorder type: {legacyConfig.selectedRecorderType}", LogCategory.Configuration);
                    break;
            }
            
            return recorderConfig;
        }
        
        private GlobalSettings ExtractGlobalSettingsFromLegacyUI()
        {
            // Extract global settings from legacy UI fields
            var globalSettings = new GlobalSettings
            {
                GlobalFrameRate = GetLegacyFrameRate(),
                DefaultResolution = GetLegacyResolution(),
                DebugMode = GetLegacyDebugMode()
            };
            
            return globalSettings;
        }
        
        private void UpdateLegacyTimelineList()
        {
            // Update timeline list in legacy UI based on new architecture state
            var selectedTimelines = _mainController.SelectedTimelines.ToList();
            UpdateLegacyTimelineSelection(selectedTimelines);
        }
        
        private void UpdateLegacyRecorderConfigurations()
        {
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            if (config == null) return;
            
            // Update each timeline's recorder configurations in legacy UI
            for (int i = 0; i < config.TimelineConfigs.Count; i++)
            {
                var timelineConfig = config.TimelineConfigs[i] as TimelineRecorderConfig;
                if (timelineConfig != null)
                {
                    UpdateLegacyTimelineRecorders(i, timelineConfig);
                }
            }
        }
        
        private void UpdateLegacyGlobalSettings()
        {
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            if (config?.GlobalSettings == null) return;
            
            // Update global settings in legacy UI
            SetLegacyFrameRate(config.GlobalSettings.GlobalFrameRate);
            SetLegacyResolution(config.GlobalSettings.DefaultResolution);
            SetLegacyDebugMode(config.GlobalSettings.DebugMode);
        }
        
        // Helper methods to access legacy UI fields
        private List<PlayableDirector> GetLegacySelectedTimelines()
        {
            // Access legacy UI's selected timelines
            // This would need to access private fields through reflection or public properties
            return new List<PlayableDirector>();
        }
        
        private List<MultiTimelineRecorderSettings.TimelineSettingEntry> GetLegacyRecorderConfigurations()
        {
            // Access legacy UI's recorder configurations
            return new List<MultiTimelineRecorderSettings.TimelineSettingEntry>();
        }
        
        private int GetLegacyFrameRate()
        {
            // Access legacy UI's frame rate setting
            return 30;
        }
        
        private Resolution GetLegacyResolution()
        {
            // Access legacy UI's resolution setting
            return new Resolution(1920, 1080);
        }
        
        private bool GetLegacyDebugMode()
        {
            // Access legacy UI's debug mode setting
            return false;
        }
        
        private void UpdateLegacyTimelineSelection(List<PlayableDirector> timelines)
        {
            // Update timeline selection in legacy UI
        }
        
        private void UpdateLegacyTimelineRecorders(int timelineIndex, TimelineRecorderConfig config)
        {
            // Update recorder configurations for a specific timeline in legacy UI
        }
        
        private void SetLegacyFrameRate(int frameRate)
        {
            // Set frame rate in legacy UI
        }
        
        private void SetLegacyResolution(Resolution resolution)
        {
            // Set resolution in legacy UI
        }
        
        private void SetLegacyDebugMode(bool debugMode)
        {
            // Set debug mode in legacy UI
        }
        
        private void SubscribeToLegacyEvents()
        {
            // Subscribe to events from legacy UI
            // This would typically be done through callbacks or delegates
        }
        
        private void SubscribeToNewEvents()
        {
            // Subscribe to events from new architecture
            _eventBus.Subscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
            _eventBus.Subscribe<RecorderConfigurationChangedEvent>(OnRecorderConfigurationChanged);
            _eventBus.Subscribe<ConfigurationChangedEvent>(OnConfigurationChanged);
        }
        
        private void OnTimelineSelectionChanged(TimelineSelectionChangedEvent e)
        {
            UpdateLegacyUI();
        }
        
        private void OnRecorderConfigurationChanged(RecorderConfigurationChangedEvent e)
        {
            UpdateLegacyUI();
        }
        
        private void OnConfigurationChanged(ConfigurationChangedEvent e)
        {
            UpdateLegacyUI();
        }
        
        public void Dispose()
        {
            // Unsubscribe from events
            _eventBus.Unsubscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
            _eventBus.Unsubscribe<RecorderConfigurationChangedEvent>(OnRecorderConfigurationChanged);
            _eventBus.Unsubscribe<ConfigurationChangedEvent>(OnConfigurationChanged);
        }
    }
}