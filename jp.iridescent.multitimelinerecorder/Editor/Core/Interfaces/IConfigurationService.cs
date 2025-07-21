using System.Collections.Generic;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for configuration management service
    /// Handles saving, loading, and managing recording configurations
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Saves the recording configuration
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <param name="path">Optional path to save to. If null, uses default location</param>
        void SaveConfiguration(IRecordingConfiguration config, string path = null);

        /// <summary>
        /// Loads the recording configuration
        /// </summary>
        /// <param name="path">Optional path to load from. If null, uses default location</param>
        /// <returns>The loaded configuration</returns>
        IRecordingConfiguration LoadConfiguration(string path = null);

        /// <summary>
        /// Saves scene-specific settings
        /// </summary>
        /// <param name="scenePath">Path to the scene</param>
        /// <param name="settings">Scene-specific settings to save</param>
        void SaveSceneSettings(string scenePath, SceneSettings settings);

        /// <summary>
        /// Loads scene-specific settings
        /// </summary>
        /// <param name="scenePath">Path to the scene</param>
        /// <returns>Scene-specific settings</returns>
        SceneSettings LoadSceneSettings(string scenePath);

        /// <summary>
        /// Gets the default configuration
        /// </summary>
        /// <returns>Default recording configuration</returns>
        IRecordingConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Exports configuration to JSON
        /// </summary>
        /// <param name="config">Configuration to export</param>
        /// <returns>JSON string representation</returns>
        string ExportConfiguration(IRecordingConfiguration config);

        /// <summary>
        /// Imports configuration from JSON
        /// </summary>
        /// <param name="json">JSON string to import</param>
        /// <returns>Imported configuration</returns>
        IRecordingConfiguration ImportConfiguration(string json);

        /// <summary>
        /// Lists all saved configurations
        /// </summary>
        /// <returns>List of saved configuration info</returns>
        List<ConfigurationInfo> ListSavedConfigurations();
    }

    /// <summary>
    /// Scene-specific settings
    /// </summary>
    public class SceneSettings
    {
        public string ScenePath { get; set; }
        public List<string> SavedJobIds { get; set; } = new List<string>();
        public Dictionary<string, int> TimelineTakeNumbers { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Information about a saved configuration
    /// </summary>
    public class ConfigurationInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string CreatedDate { get; set; }
        public string ModifiedDate { get; set; }
    }
}