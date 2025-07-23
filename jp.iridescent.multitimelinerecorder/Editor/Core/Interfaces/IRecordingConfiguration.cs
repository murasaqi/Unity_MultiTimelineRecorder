using System.Collections.Generic;
using UnityEngine;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for recording configuration
    /// Defines the contract for all recording configurations
    /// </summary>
    public interface IRecordingConfiguration
    {
        /// <summary>
        /// Frame rate for recording
        /// </summary>
        int FrameRate { get; set; }

        /// <summary>
        /// Resolution for recording
        /// </summary>
        Resolution Resolution { get; set; }

        /// <summary>
        /// Output path for recorded files
        /// </summary>
        string OutputPath { get; set; }

        /// <summary>
        /// List of timeline-specific configurations
        /// </summary>
        List<ITimelineRecorderConfig> TimelineConfigs { get; set; }

        /// <summary>
        /// Global settings that apply to all recordings
        /// </summary>
        IGlobalSettings GlobalSettings { get; set; }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>Validation result</returns>
        ValidationResult Validate();

        /// <summary>
        /// Creates a deep copy of the configuration
        /// </summary>
        /// <returns>Cloned configuration</returns>
        IRecordingConfiguration Clone();
    }

    /// <summary>
    /// Interface for timeline-specific recorder configuration
    /// </summary>
    public interface ITimelineRecorderConfig
    {
        /// <summary>
        /// Unique identifier for this configuration
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Name of the timeline
        /// </summary>
        string TimelineName { get; set; }

        /// <summary>
        /// Whether this timeline is enabled for recording
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// List of recorder configurations for this timeline
        /// </summary>
        List<IRecorderConfiguration> RecorderConfigs { get; set; }

        /// <summary>
        /// Validates the timeline configuration
        /// </summary>
        /// <returns>Validation result</returns>
        ValidationResult Validate();
    }

    /// <summary>
    /// Interface for individual recorder configuration
    /// </summary>
    public interface IRecorderConfiguration
    {
        /// <summary>
        /// Unique identifier for this recorder
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Display name for this recorder
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Type of recorder
        /// </summary>
        RecorderSettingsType Type { get; }

        /// <summary>
        /// Whether this recorder is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Current take number
        /// </summary>
        int TakeNumber { get; set; }

        /// <summary>
        /// Validates the recorder configuration
        /// </summary>
        /// <returns>Validation result</returns>
        ValidationResult Validate();

        /// <summary>
        /// Creates Unity Recorder settings from this configuration
        /// </summary>
        /// <param name="context">Wildcard context for path resolution</param>
        /// <returns>Unity Recorder settings</returns>
        UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(WildcardContext context);
    }

    /// <summary>
    /// Interface for global recording settings
    /// </summary>
    public interface IGlobalSettings
    {
        /// <summary>
        /// Default frame rate for new recordings
        /// </summary>
        int DefaultFrameRate { get; set; }

        /// <summary>
        /// Default resolution for new recordings
        /// </summary>
        Resolution DefaultResolution { get; set; }

        /// <summary>
        /// Default output path configuration
        /// </summary>
        IOutputPathConfiguration DefaultOutputPath { get; set; }

        /// <summary>
        /// Whether debug mode is enabled
        /// </summary>
        bool DebugMode { get; set; }
    }

    /// <summary>
    /// Interface for output path configuration
    /// </summary>
    public interface IOutputPathConfiguration
    {
        /// <summary>
        /// Base output directory
        /// </summary>
        string BaseDirectory { get; set; }

        /// <summary>
        /// Whether to create subdirectories for each timeline
        /// </summary>
        bool CreateTimelineSubdirectories { get; set; }

        /// <summary>
        /// Whether to create subdirectories for each recorder type
        /// </summary>
        bool CreateRecorderTypeSubdirectories { get; set; }

        /// <summary>
        /// Custom filename pattern
        /// </summary>
        string FilenamePattern { get; set; }
    }

    /// <summary>
    /// Standard resolution settings
    /// </summary>
    [System.Serializable]
    public struct Resolution
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Resolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }

    /// <summary>
    /// Context for wildcard replacement in paths
    /// </summary>
    public class WildcardContext
    {
        public string TimelineName { get; set; }
        public string SceneName { get; set; }
        public int TakeNumber { get; set; }
        public string RecorderType { get; set; }
        public System.DateTime RecordingDate { get; set; }
        public Dictionary<string, string> CustomWildcards { get; set; } = new Dictionary<string, string>();
        public int GlobalFrameRate { get; set; }
        public string RecorderName { get; set; }
        public int? TimelineTakeNumber { get; set; }
    }
}