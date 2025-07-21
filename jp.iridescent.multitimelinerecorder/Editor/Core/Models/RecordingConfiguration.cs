using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Implementation of recording configuration
    /// Contains all settings needed for a recording session
    /// </summary>
    [Serializable]
    public class RecordingConfiguration : IRecordingConfiguration
    {
        [SerializeField]
        private int frameRate = 24;
        
        [SerializeField]
        private Resolution resolution = new Resolution(1920, 1080);
        
        [SerializeField]
        private string outputPath = "Recordings";
        
        [SerializeField]
        private List<TimelineRecorderConfig> timelineConfigs = new List<TimelineRecorderConfig>();
        
        [SerializeField]
        private GlobalSettings globalSettings = new GlobalSettings();

        /// <inheritdoc />
        public int FrameRate
        {
            get => frameRate;
            set => frameRate = Mathf.Clamp(value, 1, 120);
        }

        /// <inheritdoc />
        public Resolution Resolution
        {
            get => resolution;
            set => resolution = value;
        }

        /// <inheritdoc />
        public string OutputPath
        {
            get => outputPath;
            set => outputPath = value;
        }

        /// <inheritdoc />
        public List<ITimelineRecorderConfig> TimelineConfigs
        {
            get => timelineConfigs.Cast<ITimelineRecorderConfig>().ToList();
            set => timelineConfigs = value?.Cast<TimelineRecorderConfig>().ToList() ?? new List<TimelineRecorderConfig>();
        }

        /// <inheritdoc />
        public IGlobalSettings GlobalSettings
        {
            get => globalSettings;
            set => globalSettings = value as GlobalSettings ?? new GlobalSettings();
        }

        /// <inheritdoc />
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate frame rate
            if (frameRate < 1 || frameRate > 120)
            {
                result.AddError($"Frame rate must be between 1 and 120. Current value: {frameRate}");
            }

            // Validate resolution
            if (resolution.Width < 1 || resolution.Height < 1)
            {
                result.AddError($"Resolution must be positive. Current value: {resolution}");
            }

            if (resolution.Width > 16384 || resolution.Height > 16384)
            {
                result.AddWarning($"Resolution is very high and may cause performance issues: {resolution}");
            }

            // Validate output path
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                result.AddError("Output path cannot be empty");
            }

            // Validate timeline configs
            if (timelineConfigs == null || timelineConfigs.Count == 0)
            {
                result.AddWarning("No timelines configured for recording");
            }
            else
            {
                var enabledCount = timelineConfigs.Count(t => t.IsEnabled);
                if (enabledCount == 0)
                {
                    result.AddWarning("No timelines are enabled for recording");
                }

                // Validate each timeline config
                foreach (var timelineConfig in timelineConfigs)
                {
                    var timelineResult = timelineConfig.Validate();
                    if (!timelineResult.IsValid)
                    {
                        foreach (var issue in timelineResult.Issues)
                        {
                            var message = $"Timeline '{timelineConfig.TimelineName}': {issue.Message}";
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

            // Validate global settings
            if (globalSettings != null)
            {
                var globalResult = globalSettings.Validate();
                if (!globalResult.IsValid)
                {
                    foreach (var issue in globalResult.Issues)
                    {
                        if (issue.Severity == ValidationSeverity.Error)
                        {
                            result.AddError($"Global settings: {issue.Message}");
                        }
                        else
                        {
                            result.AddWarning($"Global settings: {issue.Message}");
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public IRecordingConfiguration Clone()
        {
            var clone = new RecordingConfiguration
            {
                frameRate = this.frameRate,
                resolution = this.resolution,
                outputPath = this.outputPath,
                globalSettings = this.globalSettings?.Clone() as GlobalSettings ?? new GlobalSettings()
            };

            // Deep clone timeline configs
            clone.timelineConfigs = new List<TimelineRecorderConfig>();
            foreach (var config in timelineConfigs)
            {
                clone.timelineConfigs.Add(config.Clone() as TimelineRecorderConfig);
            }

            return clone;
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        public static RecordingConfiguration CreateDefault()
        {
            var config = new RecordingConfiguration
            {
                frameRate = 24,
                resolution = new Resolution(1920, 1080),
                outputPath = "Recordings",
                globalSettings = GlobalSettings.CreateDefault()
            };

            return config;
        }

        /// <summary>
        /// Adds a timeline configuration
        /// </summary>
        public void AddTimelineConfig(TimelineRecorderConfig config)
        {
            if (config != null)
            {
                timelineConfigs.Add(config);
            }
        }

        /// <summary>
        /// Removes a timeline configuration
        /// </summary>
        public void RemoveTimelineConfig(string id)
        {
            timelineConfigs.RemoveAll(t => t.Id == id);
        }

        /// <summary>
        /// Gets a timeline configuration by ID
        /// </summary>
        public TimelineRecorderConfig GetTimelineConfig(string id)
        {
            return timelineConfigs.FirstOrDefault(t => t.Id == id);
        }
    }
}