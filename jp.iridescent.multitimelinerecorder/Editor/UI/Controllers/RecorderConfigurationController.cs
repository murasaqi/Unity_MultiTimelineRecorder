using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Events;

namespace MultiTimelineRecorder.UI.Controllers
{
    /// <summary>
    /// Controller for managing recorder configurations
    /// </summary>
    public class RecorderConfigurationController
    {
        private readonly ILogger _logger;
        private readonly IErrorHandlingService _errorHandler;
        private readonly IEventBus _eventBus;
        private readonly RecorderConfigurationFactory _factory;

        private TimelineRecorderConfig _currentTimelineConfig;
        private IRecorderConfiguration _selectedRecorder;

        /// <summary>
        /// Gets the current timeline configuration
        /// </summary>
        public TimelineRecorderConfig CurrentTimelineConfig => _currentTimelineConfig;

        /// <summary>
        /// Gets the selected recorder configuration
        /// </summary>
        public IRecorderConfiguration SelectedRecorder => _selectedRecorder;

        /// <summary>
        /// Gets available recorder types
        /// </summary>
        public RecorderSettingsType[] AvailableRecorderTypes => RecorderConfigurationFactory.GetSupportedTypes();

        public RecorderConfigurationController(
            ILogger logger,
            IErrorHandlingService errorHandler,
            IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _factory = new RecorderConfigurationFactory();

            Initialize();
        }

        /// <summary>
        /// Initializes the controller
        /// </summary>
        private void Initialize()
        {
            // Subscribe to events
            _eventBus.Subscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
        }

        /// <summary>
        /// Sets the current timeline configuration
        /// </summary>
        public void SetTimelineConfig(TimelineRecorderConfig config)
        {
            _currentTimelineConfig = config;
            _selectedRecorder = null;

            _eventBus.Publish(new UIRefreshRequestedEvent
            {
                Scope = UIRefreshRequestedEvent.RefreshScope.RecorderList
            });
        }

        /// <summary>
        /// Adds a new recorder to the current timeline
        /// </summary>
        public void AddRecorder(RecorderSettingsType type)
        {
            if (_currentTimelineConfig == null)
            {
                _logger.LogWarning("No timeline selected", LogCategory.UI);
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                // Create new recorder configuration
                var recorderConfig = RecorderConfigurationFactory.CreateDefaultConfiguration(type);
                
                // Add to timeline config
                _currentTimelineConfig.AddRecorderConfig(recorderConfig as RecorderConfigurationBase);

                _eventBus.Publish(new RecorderAddedEvent
                {
                    TimelineConfigId = _currentTimelineConfig.Id,
                    RecorderConfig = recorderConfig
                });

                _logger.LogInfo($"Added {type} recorder to timeline", LogCategory.UI);

                // Select the new recorder
                SelectRecorder(recorderConfig);
            }, "AddRecorder");
        }

        /// <summary>
        /// Removes a recorder from the current timeline
        /// </summary>
        public void RemoveRecorder(string recorderId)
        {
            if (_currentTimelineConfig == null || string.IsNullOrEmpty(recorderId))
            {
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var recorder = _currentTimelineConfig.GetRecorderConfig(recorderId);
                if (recorder != null)
                {
                    _currentTimelineConfig.RemoveRecorderConfig(recorderId);

                    _eventBus.Publish(new RecorderRemovedEvent
                    {
                        TimelineConfigId = _currentTimelineConfig.Id,
                        RecorderConfigId = recorderId
                    });

                    _logger.LogInfo($"Removed recorder: {recorder.Name}", LogCategory.UI);

                    // Clear selection if it was the removed recorder
                    if (_selectedRecorder?.Id == recorderId)
                    {
                        _selectedRecorder = null;
                    }
                }
            }, "RemoveRecorder");
        }

        /// <summary>
        /// Updates a recorder configuration
        /// </summary>
        public void UpdateRecorderConfig(IRecorderConfiguration config, string propertyName, object oldValue, object newValue)
        {
            if (config == null || _currentTimelineConfig == null)
            {
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                // Validate the updated configuration
                var validation = config.Validate();
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"Recorder configuration has validation issues: {config.Name}", LogCategory.Configuration);
                }

                _eventBus.Publish(new RecorderUpdatedEvent
                {
                    TimelineConfigId = _currentTimelineConfig.Id,
                    RecorderConfig = config,
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = newValue
                });

                // Request UI refresh for validation state
                _eventBus.Publish(new UIRefreshRequestedEvent
                {
                    Scope = UIRefreshRequestedEvent.RefreshScope.ValidationState
                });

                _logger.LogDebug($"Updated recorder {config.Name}.{propertyName}: {oldValue} -> {newValue}", LogCategory.Configuration);
            }, "UpdateRecorderConfig");
        }

        /// <summary>
        /// Selects a recorder for editing
        /// </summary>
        public void SelectRecorder(IRecorderConfiguration recorder)
        {
            _selectedRecorder = recorder;

            _eventBus.Publish(new UIRefreshRequestedEvent
            {
                Scope = UIRefreshRequestedEvent.RefreshScope.Settings,
                TargetId = recorder?.Id
            });
        }

        /// <summary>
        /// Duplicates a recorder configuration
        /// </summary>
        public void DuplicateRecorder(string recorderId)
        {
            if (_currentTimelineConfig == null || string.IsNullOrEmpty(recorderId))
            {
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var original = _currentTimelineConfig.GetRecorderConfig(recorderId);
                if (original != null)
                {
                    var duplicate = original.Clone() as RecorderConfigurationBase;
                    if (duplicate != null)
                    {
                        duplicate.Name = $"{original.Name} (Copy)";
                        _currentTimelineConfig.AddRecorderConfig(duplicate);

                        _eventBus.Publish(new RecorderAddedEvent
                        {
                            TimelineConfigId = _currentTimelineConfig.Id,
                            RecorderConfig = duplicate
                        });

                        _logger.LogInfo($"Duplicated recorder: {original.Name}", LogCategory.UI);

                        // Select the duplicate
                        SelectRecorder(duplicate);
                    }
                }
            }, "DuplicateRecorder");
        }

        /// <summary>
        /// Toggles the enabled state of a recorder
        /// </summary>
        public void ToggleRecorderEnabled(string recorderId)
        {
            if (_currentTimelineConfig == null || string.IsNullOrEmpty(recorderId))
            {
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var recorder = _currentTimelineConfig.GetRecorderConfig(recorderId);
                if (recorder != null)
                {
                    var oldValue = recorder.IsEnabled;
                    recorder.IsEnabled = !recorder.IsEnabled;

                    UpdateRecorderConfig(recorder, nameof(recorder.IsEnabled), oldValue, recorder.IsEnabled);
                }
            }, "ToggleRecorderEnabled");
        }

        /// <summary>
        /// Gets all recorders for the current timeline
        /// </summary>
        public List<IRecorderConfiguration> GetRecorders()
        {
            if (_currentTimelineConfig == null)
            {
                return new List<IRecorderConfiguration>();
            }

            return _currentTimelineConfig.RecorderConfigs;
        }

        /// <summary>
        /// Gets enabled recorders for the current timeline
        /// </summary>
        public List<IRecorderConfiguration> GetEnabledRecorders()
        {
            if (_currentTimelineConfig == null)
            {
                return new List<IRecorderConfiguration>();
            }

            return _currentTimelineConfig.RecorderConfigs.Where(r => r.IsEnabled).ToList();
        }

        /// <summary>
        /// Validates all recorder configurations
        /// </summary>
        public ValidationResult ValidateRecorders()
        {
            var result = new ValidationResult();

            if (_currentTimelineConfig == null)
            {
                return result;
            }

            foreach (var recorder in _currentTimelineConfig.RecorderConfigs)
            {
                var recorderValidation = recorder.Validate();
                foreach (var issue in recorderValidation.Issues)
                {
                    var message = $"{recorder.Name}: {issue.Message}";
                    if (issue.Severity == ValidationSeverity.Error)
                    {
                        result.AddError(message);
                    }
                    else
                    {
                        result.AddWarning(message);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Reorders recorders in the list
        /// </summary>
        public void ReorderRecorder(string recorderId, int newIndex)
        {
            if (_currentTimelineConfig == null || string.IsNullOrEmpty(recorderId))
            {
                return;
            }

            _errorHandler.ExecuteWithErrorHandling(() =>
            {
                var recorders = _currentTimelineConfig.RecorderConfigs;
                var recorder = recorders.FirstOrDefault(r => r.Id == recorderId);
                
                if (recorder != null)
                {
                    var currentIndex = recorders.IndexOf(recorder);
                    if (currentIndex != newIndex && newIndex >= 0 && newIndex < recorders.Count)
                    {
                        recorders.RemoveAt(currentIndex);
                        recorders.Insert(newIndex, recorder);

                        _eventBus.Publish(new UIRefreshRequestedEvent
                        {
                            Scope = UIRefreshRequestedEvent.RefreshScope.RecorderList
                        });

                        _logger.LogDebug($"Reordered recorder {recorder.Name} from index {currentIndex} to {newIndex}", LogCategory.UI);
                    }
                }
            }, "ReorderRecorder");
        }

        /// <summary>
        /// Handles timeline selection changed event
        /// </summary>
        private void OnTimelineSelectionChanged(TimelineSelectionChangedEvent e)
        {
            // Clear current selection when timeline changes
            _currentTimelineConfig = null;
            _selectedRecorder = null;
        }

        /// <summary>
        /// Disposes the controller
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from events
            _eventBus.Unsubscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
        }
    }
}