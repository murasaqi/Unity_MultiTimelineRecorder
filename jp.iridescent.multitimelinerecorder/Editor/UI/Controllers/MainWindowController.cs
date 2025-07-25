using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Services;

namespace MultiTimelineRecorder.UI.Controllers
{
    /// <summary>
    /// Main window controller that manages the overall UI logic
    /// </summary>
    public class MainWindowController
    {
        private readonly IRecordingService _recordingService;
        private readonly ITimelineService _timelineService;
        private readonly IConfigurationService _configurationService;
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private readonly IErrorHandlingService _errorHandler;
        private readonly IEventBus _eventBus;

        private IRecordingConfiguration _currentConfiguration;
        private List<PlayableDirector> _availableTimelines;
        private List<PlayableDirector> _selectedTimelines;
        private string _currentJobId;
        private bool _isRecording;

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public IRecordingConfiguration CurrentConfiguration => _currentConfiguration;

        /// <summary>
        /// Gets available timelines
        /// </summary>
        public IReadOnlyList<PlayableDirector> AvailableTimelines => _availableTimelines;

        /// <summary>
        /// Gets selected timelines
        /// </summary>
        public IReadOnlyList<PlayableDirector> SelectedTimelines => _selectedTimelines;

        /// <summary>
        /// Gets whether recording is in progress
        /// </summary>
        public bool IsRecording => _isRecording;

        public MainWindowController(
            IRecordingService recordingService,
            ITimelineService timelineService,
            IConfigurationService configurationService,
            MultiTimelineRecorder.Core.Interfaces.ILogger logger,
            IErrorHandlingService errorHandler,
            IEventBus eventBus)
        {
            _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
            _timelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _availableTimelines = new List<PlayableDirector>();
            _selectedTimelines = new List<PlayableDirector>();

            Initialize();
        }

        /// <summary>
        /// Initializes the controller
        /// </summary>
        private void Initialize()
        {
            // Load configuration
            _currentConfiguration = _configurationService.LoadConfiguration();
            
            // Subscribe to events
            _eventBus.Subscribe<ConfigurationChangedEvent>(OnConfigurationChanged);
            _eventBus.Subscribe<RecordingCompletedEvent>(OnRecordingCompleted);
            _eventBus.Subscribe<RecordingFailedEvent>(OnRecordingFailed);
            _eventBus.Subscribe<RecordingCancelledEvent>(OnRecordingCancelled);
        }

        /// <summary>
        /// Refreshes available timelines
        /// </summary>
        public void RefreshTimelines(bool includeInactive = false)
        {
            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                _logger.LogInfo("Refreshing timeline list", LogCategory.UI);
                
                _availableTimelines = _timelineService.ScanAvailableTimelines(includeInactive);
                
                // Update selected timelines (remove any that are no longer available)
                _selectedTimelines.RemoveAll(t => !_availableTimelines.Contains(t));
                
                _eventBus.Publish(new UIRefreshRequestedEvent 
                { 
                    Scope = UIRefreshRequestedEvent.RefreshScope.TimelineList 
                });
                
                _logger.LogInfo($"Found {_availableTimelines.Count} timelines", LogCategory.UI);
            }, "RefreshTimelines");
        }

        /// <summary>
        /// Adds a timeline to selection
        /// </summary>
        public void AddTimeline(PlayableDirector director)
        {
            if (director == null || _selectedTimelines.Contains(director)) return;

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                // Validate timeline
                var validation = _timelineService.ValidateTimeline(director);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"Timeline validation has issues: {director.name}", LogCategory.Timeline);
                }

                _selectedTimelines.Add(director);
                
                // Add to configuration
                var timelineConfig = TimelineRecorderConfig.CreateDefault(director.name);
                timelineConfig.Director = director;
                
                if (_currentConfiguration is RecordingConfiguration config)
                {
                    config.AddTimelineConfig(timelineConfig);
                }

                _eventBus.Publish(new TimelineSelectionChangedEvent
                {
                    SelectedTimelines = _selectedTimelines.ToList(),
                    AddedTimelines = new List<PlayableDirector> { director },
                    SelectedIndex = _selectedTimelines.Count - 1
                });

                _logger.LogInfo($"Added timeline: {director.name}", LogCategory.UI);
            }, "AddTimeline");
        }

        /// <summary>
        /// Removes a timeline from selection
        /// </summary>
        public void RemoveTimeline(PlayableDirector director)
        {
            if (director == null || !_selectedTimelines.Contains(director)) return;

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                _selectedTimelines.Remove(director);
                
                // Remove from configuration
                if (_currentConfiguration is RecordingConfiguration config)
                {
                    var timelineConfig = config.TimelineConfigs
                        .FirstOrDefault(t => t is TimelineRecorderConfig trc && trc.Director == director);
                    
                    if (timelineConfig != null)
                    {
                        config.RemoveTimelineConfig(timelineConfig.Id);
                    }
                }

                _eventBus.Publish(new TimelineSelectionChangedEvent
                {
                    SelectedTimelines = _selectedTimelines.ToList(),
                    RemovedTimelines = new List<PlayableDirector> { director },
                    SelectedIndex = _selectedTimelines.Count > 0 ? 0 : -1
                });

                _logger.LogInfo($"Removed timeline: {director.name}", LogCategory.UI);
            }, "RemoveTimeline");
        }

        /// <summary>
        /// Starts recording
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording)
            {
                _logger.LogWarning("Recording already in progress", LogCategory.Recording);
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                // Validate configuration
                var validation = _currentConfiguration.Validate();
                if (!validation.IsValid)
                {
                    throw new ValidationException("Configuration validation failed", validation);
                }

                // Validate selected timelines
                if (_selectedTimelines.Count == 0)
                {
                    throw new RecordingConfigurationException("No timelines selected for recording");
                }

                _logger.LogInfo("Starting recording...", LogCategory.Recording);
                _isRecording = true;

                // Prepare timelines
                foreach (var timeline in _selectedTimelines)
                {
                    _timelineService.PrepareTimelineForRecording(timeline);
                }

                // Start recording
                var result = _recordingService.ExecuteRecording(_selectedTimelines, _currentConfiguration);
                
                if (result.IsSuccess)
                {
                    _currentJobId = result.JobId;
                    
                    _eventBus.Publish(new RecordingStartedEvent
                    {
                        JobId = result.JobId,
                        Timelines = _selectedTimelines.ToList(),
                        Configuration = _currentConfiguration
                    });
                }
                else
                {
                    _isRecording = false;
                    throw new RecordingExecutionException($"Failed to start recording: {result.ErrorMessage}");
                }
            }, "StartRecording");
        }

        /// <summary>
        /// Stops recording
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording || string.IsNullOrEmpty(_currentJobId))
            {
                _logger.LogWarning("No recording in progress", LogCategory.Recording);
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                _logger.LogInfo("Stopping recording...", LogCategory.Recording);
                
                _recordingService.CancelRecording(_currentJobId);
                
                // Restore timelines
                foreach (var timeline in _selectedTimelines)
                {
                    _timelineService.RestoreTimelineAfterRecording(timeline);
                }

                _isRecording = false;
                _currentJobId = null;
            }, "StopRecording");
        }

        /// <summary>
        /// Updates configuration
        /// </summary>
        public void UpdateConfiguration(IRecordingConfiguration config)
        {
            if (config == null) return;

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var oldConfig = _currentConfiguration;
                _currentConfiguration = config;
                
                // Save configuration
                _configurationService.SaveConfiguration(config);
                
                _eventBus.Publish(new ConfigurationChangedEvent
                {
                    Configuration = config,
                    OldValue = oldConfig,
                    NewValue = config
                });

                _logger.LogInfo("Configuration updated", LogCategory.Configuration);
            }, "UpdateConfiguration");
        }

        /// <summary>
        /// Gets recording progress
        /// </summary>
        public RecordingProgress GetRecordingProgress()
        {
            if (!_isRecording || string.IsNullOrEmpty(_currentJobId))
            {
                return null;
            }

            return _recordingService.GetProgress(_currentJobId);
        }

        /// <summary>
        /// Validates current state
        /// </summary>
        public ValidationResult ValidateCurrentState()
        {
            var result = new ValidationResult();

            // Validate configuration
            if (_currentConfiguration != null)
            {
                var configValidation = _currentConfiguration.Validate();
                foreach (var issue in configValidation.Issues)
                {
                    result.Issues.Add(issue);
                }
                result.IsValid = result.IsValid && configValidation.IsValid;
            }

            // Validate timeline selection
            if (_selectedTimelines.Count == 0)
            {
                result.AddWarning("No timelines selected for recording");
            }

            return result;
        }

        /// <summary>
        /// Handles configuration changed event
        /// </summary>
        private void OnConfigurationChanged(ConfigurationChangedEvent e)
        {
            _currentConfiguration = e.Configuration;
        }

        /// <summary>
        /// Handles recording completed event
        /// </summary>
        private void OnRecordingCompleted(RecordingCompletedEvent e)
        {
            _isRecording = false;
            _currentJobId = null;

            // Restore timelines
            foreach (var timeline in _selectedTimelines)
            {
                _timelineService.RestoreTimelineAfterRecording(timeline);
            }
        }

        /// <summary>
        /// Handles recording failed event
        /// </summary>
        private void OnRecordingFailed(RecordingFailedEvent e)
        {
            _isRecording = false;
            _currentJobId = null;

            // Restore timelines
            foreach (var timeline in _selectedTimelines)
            {
                _timelineService.RestoreTimelineAfterRecording(timeline);
            }
        }

        /// <summary>
        /// Handles recording cancelled event
        /// </summary>
        private void OnRecordingCancelled(RecordingCancelledEvent e)
        {
            _isRecording = false;
            _currentJobId = null;
        }

        /// <summary>
        /// Saves the current configuration
        /// </summary>
        public void SaveConfiguration()
        {
            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                if (_currentConfiguration == null) return;
                
                _configurationService.SaveConfiguration(_currentConfiguration);
                _logger.LogInfo("Configuration saved", LogCategory.Configuration);
                
                _eventBus.Publish(new ConfigurationSavedEvent
                {
                    Configuration = _currentConfiguration
                });
            }, "SaveConfiguration");
        }

        /// <summary>
        /// Loads a configuration
        /// </summary>
        public void LoadConfiguration()
        {
            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var config = _configurationService.LoadConfiguration();
                UpdateConfiguration(config);
                
                _logger.LogInfo("Configuration loaded", LogCategory.Configuration);
                
                _eventBus.Publish(new ConfigurationLoadedEvent
                {
                    Configuration = config,
                    IsNewConfiguration = false,
                    LoadedFrom = "default"
                });
            }, "LoadConfiguration");
        }

        /// <summary>
        /// Updates global settings
        /// </summary>
        public void UpdateGlobalSettings(GlobalSettings settings)
        {
            if (settings == null) return;
            
            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                if (_currentConfiguration is RecordingConfiguration config)
                {
                    config.GlobalSettings = settings;
                    UpdateConfiguration(config);
                    
                    _logger.LogInfo("Global settings updated", LogCategory.Configuration);
                }
            }, "UpdateGlobalSettings");
        }

        /// <summary>
        /// Applies a recorder configuration to all selected timelines
        /// </summary>
        /// <param name="sourceRecorder">The recorder configuration to apply</param>
        /// <param name="overwriteExisting">Whether to overwrite existing recorders of the same type</param>
        public void ApplyRecorderToSelectedTimelines(IRecorderConfiguration sourceRecorder, bool overwriteExisting)
        {
            if (sourceRecorder == null)
            {
                _logger.LogError("Cannot apply null recorder configuration", LogCategory.Configuration);
                return;
            }
            
            if (_selectedTimelines.Count <= 1)
            {
                _logger.LogWarning("Apply to All requires multiple selected timelines", LogCategory.Configuration);
                return;
            }
            
            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var config = _currentConfiguration as RecordingConfiguration;
                if (config == null) return;
                
                var appliedCount = 0;
                var overwrittenCount = 0;
                
                foreach (var timeline in _selectedTimelines)
                {
                    // Find the timeline configuration
                    var timelineConfig = config.TimelineConfigs
                        .FirstOrDefault(t => t is TimelineRecorderConfig trc && trc.Director == timeline) as TimelineRecorderConfig;
                    
                    if (timelineConfig == null) continue;
                    
                    if (overwriteExisting)
                    {
                        // Remove existing recorders of the same type
                        var existingRecorders = timelineConfig.RecorderConfigs
                            .Where(r => r.Type == sourceRecorder.Type)
                            .ToList();
                        
                        foreach (var existing in existingRecorders)
                        {
                            timelineConfig.RecorderConfigs.Remove(existing);
                            overwrittenCount++;
                        }
                    }
                    
                    // Clone the source recorder and add it
                    var clonedRecorder = sourceRecorder.Clone();
                    timelineConfig.RecorderConfigs.Add(clonedRecorder);
                    appliedCount++;
                }
                
                _logger.LogInfo($"Applied recorder to {appliedCount} timelines" + 
                    (overwrittenCount > 0 ? $" (overwrote {overwrittenCount} existing)" : ""), 
                    LogCategory.Configuration);
                
                // Update configuration
                UpdateConfiguration(config);
                
                // Publish event
                _eventBus.Publish(new RecorderAppliedToTimelinesEvent
                {
                    SourceRecorder = sourceRecorder,
                    TargetTimelines = _selectedTimelines.ToList(),
                    OverwriteExisting = overwriteExisting,
                    AppliedCount = appliedCount
                });
            }, "ApplyRecorderToSelectedTimelines");
        }
        
        /// <summary>
        /// Creates a new configuration
        /// </summary>
        public void CreateNewConfiguration()
        {
            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var config = _configurationService.GetDefaultConfiguration();
                UpdateConfiguration(config);
                
                _logger.LogInfo("New configuration created", LogCategory.Configuration);
                
                _eventBus.Publish(new ConfigurationLoadedEvent
                {
                    Configuration = config,
                    IsNewConfiguration = true,
                    LoadedFrom = "new"
                });
            }, "CreateNewConfiguration");
        }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public ValidationResult ValidateConfiguration()
        {
            if (_currentConfiguration == null)
            {
                var result = new ValidationResult();
                result.AddError("No configuration loaded");
                return result;
            }
            
            return _currentConfiguration.Validate();
        }

        /// <summary>
        /// Disposes the controller
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from events
            _eventBus.Unsubscribe<ConfigurationChangedEvent>(OnConfigurationChanged);
            _eventBus.Unsubscribe<RecordingCompletedEvent>(OnRecordingCompleted);
            _eventBus.Unsubscribe<RecordingFailedEvent>(OnRecordingFailed);
            _eventBus.Unsubscribe<RecordingCancelledEvent>(OnRecordingCancelled);
        }
    }
}