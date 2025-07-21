using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Configuration for recording a specific timeline
    /// </summary>
    [Serializable]
    public class TimelineRecorderConfig : ITimelineRecorderConfig
    {
        [SerializeField]
        private string id;
        
        [SerializeField]
        private string timelineName;
        
        [SerializeField]
        private bool isEnabled = true;
        
        [SerializeField]
        private List<RecorderConfigurationBase> recorderConfigs = new List<RecorderConfigurationBase>();

        /// <summary>
        /// Reference to the PlayableDirector (not serialized)
        /// </summary>
        [NonSerialized]
        private PlayableDirector director;

        /// <inheritdoc />
        public string Id
        {
            get => string.IsNullOrEmpty(id) ? (id = Guid.NewGuid().ToString()) : id;
            set => id = value;
        }

        /// <inheritdoc />
        public string TimelineName
        {
            get => timelineName;
            set => timelineName = value;
        }

        /// <inheritdoc />
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <inheritdoc />
        public List<IRecorderConfiguration> RecorderConfigs
        {
            get => recorderConfigs.Cast<IRecorderConfiguration>().ToList();
            set => recorderConfigs = value?.Cast<RecorderConfigurationBase>().ToList() ?? new List<RecorderConfigurationBase>();
        }

        /// <summary>
        /// Gets or sets the PlayableDirector reference
        /// </summary>
        public PlayableDirector Director
        {
            get => director;
            set
            {
                director = value;
                if (director != null && string.IsNullOrEmpty(timelineName))
                {
                    timelineName = director.name;
                }
            }
        }

        /// <inheritdoc />
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate timeline name
            if (string.IsNullOrWhiteSpace(timelineName))
            {
                result.AddError("Timeline name cannot be empty");
            }

            // Validate recorder configs
            if (recorderConfigs == null || recorderConfigs.Count == 0)
            {
                result.AddWarning("No recorders configured for this timeline");
            }
            else
            {
                var enabledCount = recorderConfigs.Count(r => r.IsEnabled);
                if (enabledCount == 0 && isEnabled)
                {
                    result.AddWarning("Timeline is enabled but no recorders are enabled");
                }

                // Check for duplicate recorder types
                var recorderTypes = recorderConfigs
                    .Where(r => r.IsEnabled)
                    .GroupBy(r => r.Type)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var type in recorderTypes)
                {
                    result.AddWarning($"Multiple {type} recorders are enabled for this timeline");
                }

                // Validate each recorder config
                foreach (var recorderConfig in recorderConfigs)
                {
                    var recorderResult = recorderConfig.Validate();
                    if (!recorderResult.IsValid)
                    {
                        foreach (var issue in recorderResult.Issues)
                        {
                            var message = $"Recorder '{recorderConfig.Name}': {issue.Message}";
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
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a clone of this configuration
        /// </summary>
        public ITimelineRecorderConfig Clone()
        {
            var clone = new TimelineRecorderConfig
            {
                id = Guid.NewGuid().ToString(), // Generate new ID for clone
                timelineName = this.timelineName,
                isEnabled = this.isEnabled,
                director = this.director
            };

            // Deep clone recorder configs
            clone.recorderConfigs = new List<RecorderConfigurationBase>();
            foreach (var config in recorderConfigs)
            {
                clone.recorderConfigs.Add(config.Clone() as RecorderConfigurationBase);
            }

            return clone;
        }

        /// <summary>
        /// Creates a default timeline recorder configuration
        /// </summary>
        public static TimelineRecorderConfig CreateDefault(string timelineName = "Timeline")
        {
            var config = new TimelineRecorderConfig
            {
                id = Guid.NewGuid().ToString(),
                timelineName = timelineName,
                isEnabled = true
            };

            return config;
        }

        /// <summary>
        /// Adds a recorder configuration
        /// </summary>
        public void AddRecorderConfig(RecorderConfigurationBase config)
        {
            if (config != null)
            {
                recorderConfigs.Add(config);
            }
        }

        /// <summary>
        /// Removes a recorder configuration
        /// </summary>
        public void RemoveRecorderConfig(string id)
        {
            recorderConfigs.RemoveAll(r => r.Id == id);
        }

        /// <summary>
        /// Gets a recorder configuration by ID
        /// </summary>
        public RecorderConfigurationBase GetRecorderConfig(string id)
        {
            return recorderConfigs.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>
        /// Gets all enabled recorder configurations
        /// </summary>
        public List<RecorderConfigurationBase> GetEnabledRecorderConfigs()
        {
            return recorderConfigs.Where(r => r.IsEnabled).ToList();
        }
    }
}