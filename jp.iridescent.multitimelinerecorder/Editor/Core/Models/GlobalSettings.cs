using System;
using UnityEngine;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Global settings that apply to all recordings
    /// </summary>
    [Serializable]
    public class GlobalSettings : IGlobalSettings
    {
        [SerializeField]
        private int defaultFrameRate = 24;
        
        [SerializeField]
        private Resolution defaultResolution = new Resolution(1920, 1080);
        
        [SerializeField]
        private OutputPathConfiguration defaultOutputPath = new OutputPathConfiguration();
        
        [SerializeField]
        private bool debugMode = false;

        /// <inheritdoc />
        public int DefaultFrameRate
        {
            get => defaultFrameRate;
            set => defaultFrameRate = Mathf.Clamp(value, 1, 120);
        }

        /// <inheritdoc />
        public Resolution DefaultResolution
        {
            get => defaultResolution;
            set => defaultResolution = value;
        }

        /// <inheritdoc />
        public IOutputPathConfiguration DefaultOutputPath
        {
            get => defaultOutputPath;
            set => defaultOutputPath = value as OutputPathConfiguration ?? new OutputPathConfiguration();
        }

        /// <inheritdoc />
        public bool DebugMode
        {
            get => debugMode;
            set => debugMode = value;
        }

        /// <summary>
        /// Validates the global settings
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate frame rate
            if (defaultFrameRate < 1 || defaultFrameRate > 120)
            {
                result.AddError($"Default frame rate must be between 1 and 120. Current value: {defaultFrameRate}");
            }

            // Validate resolution
            if (defaultResolution.Width < 1 || defaultResolution.Height < 1)
            {
                result.AddError($"Default resolution must be positive. Current value: {defaultResolution}");
            }

            // Validate output path
            if (defaultOutputPath != null)
            {
                var pathResult = defaultOutputPath.Validate();
                if (!pathResult.IsValid)
                {
                    foreach (var issue in pathResult.Issues)
                    {
                        if (issue.Severity == ValidationSeverity.Error)
                        {
                            result.AddError($"Output path: {issue.Message}");
                        }
                        else
                        {
                            result.AddWarning($"Output path: {issue.Message}");
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a clone of the global settings
        /// </summary>
        public IGlobalSettings Clone()
        {
            return new GlobalSettings
            {
                defaultFrameRate = this.defaultFrameRate,
                defaultResolution = this.defaultResolution,
                defaultOutputPath = this.defaultOutputPath?.Clone() as OutputPathConfiguration ?? new OutputPathConfiguration(),
                debugMode = this.debugMode
            };
        }

        /// <summary>
        /// Creates default global settings
        /// </summary>
        public static GlobalSettings CreateDefault()
        {
            return new GlobalSettings
            {
                defaultFrameRate = 24,
                defaultResolution = new Resolution(1920, 1080),
                defaultOutputPath = OutputPathConfiguration.CreateDefault(),
                debugMode = false
            };
        }
    }

    /// <summary>
    /// Configuration for output path handling
    /// </summary>
    [Serializable]
    public class OutputPathConfiguration : IOutputPathConfiguration
    {
        [SerializeField]
        private string baseDirectory = "Recordings";
        
        [SerializeField]
        private bool createTimelineSubdirectories = true;
        
        [SerializeField]
        private bool createRecorderTypeSubdirectories = true;
        
        [SerializeField]
        private string filenamePattern = "<Scene>_<Timeline>_<RecorderType>_Take<Take>";

        /// <inheritdoc />
        public string BaseDirectory
        {
            get => baseDirectory;
            set => baseDirectory = value;
        }

        /// <inheritdoc />
        public bool CreateTimelineSubdirectories
        {
            get => createTimelineSubdirectories;
            set => createTimelineSubdirectories = value;
        }

        /// <inheritdoc />
        public bool CreateRecorderTypeSubdirectories
        {
            get => createRecorderTypeSubdirectories;
            set => createRecorderTypeSubdirectories = value;
        }

        /// <inheritdoc />
        public string FilenamePattern
        {
            get => filenamePattern;
            set => filenamePattern = value;
        }

        /// <summary>
        /// Validates the output path configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate base directory
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                result.AddError("Base directory cannot be empty");
            }

            // Validate filename pattern
            if (string.IsNullOrWhiteSpace(filenamePattern))
            {
                result.AddError("Filename pattern cannot be empty");
            }
            else
            {
                // Check for at least one wildcard
                if (!filenamePattern.Contains("<") || !filenamePattern.Contains(">"))
                {
                    result.AddWarning("Filename pattern should contain wildcards (e.g., <Scene>, <Timeline>)");
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a clone of the output path configuration
        /// </summary>
        public IOutputPathConfiguration Clone()
        {
            return new OutputPathConfiguration
            {
                baseDirectory = this.baseDirectory,
                createTimelineSubdirectories = this.createTimelineSubdirectories,
                createRecorderTypeSubdirectories = this.createRecorderTypeSubdirectories,
                filenamePattern = this.filenamePattern
            };
        }

        /// <summary>
        /// Creates default output path configuration
        /// </summary>
        public static OutputPathConfiguration CreateDefault()
        {
            return new OutputPathConfiguration
            {
                baseDirectory = "Recordings",
                createTimelineSubdirectories = true,
                createRecorderTypeSubdirectories = true,
                filenamePattern = "<Scene>_<Timeline>_<RecorderType>_Take<Take>"
            };
        }
    }
}