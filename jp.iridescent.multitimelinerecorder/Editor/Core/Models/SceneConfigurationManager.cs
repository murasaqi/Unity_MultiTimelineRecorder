using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Manages scene-specific configurations for Multi Timeline Recorder
    /// </summary>
    public class SceneConfigurationManager
    {
        private const string ConfigurationFolder = "Assets/MultiTimelineRecorder/SceneConfigs";
        private const string ConfigurationExtension = ".mtr-config";
        private const string GlobalConfigName = "GlobalConfiguration";
        
        private readonly ILogger _logger;
        private readonly IGameObjectReferenceService _referenceService;
        private readonly Dictionary<string, SceneConfiguration> _sceneConfigs = new Dictionary<string, SceneConfiguration>();
        private SceneConfiguration _currentSceneConfig;
        private string _currentScenePath;
        private bool _isDirty;
        
        public event Action<SceneConfiguration> ConfigurationLoaded;
        public event Action<SceneConfiguration> ConfigurationSaved;
        public event Action<string> ConfigurationError;

        public SceneConfigurationManager(ILogger logger, IGameObjectReferenceService referenceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _referenceService = referenceService ?? throw new ArgumentNullException(nameof(referenceService));
            
            // Subscribe to scene events
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Load current scene configuration
            LoadCurrentSceneConfiguration();
        }

        /// <summary>
        /// Gets the current scene configuration
        /// </summary>
        public SceneConfiguration CurrentConfiguration => _currentSceneConfig;

        /// <summary>
        /// Gets the global configuration
        /// </summary>
        public SceneConfiguration GlobalConfiguration => GetOrCreateConfiguration(GlobalConfigName);

        /// <summary>
        /// Gets whether the current configuration has unsaved changes
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Saves the current scene configuration
        /// </summary>
        public void SaveCurrentConfiguration()
        {
            if (_currentSceneConfig == null)
            {
                _logger.LogWarning("No current scene configuration to save", LogCategory.Configuration);
                return;
            }

            SaveConfiguration(_currentSceneConfig);
        }

        /// <summary>
        /// Saves a specific configuration
        /// </summary>
        public void SaveConfiguration(SceneConfiguration config)
        {
            if (config == null)
                return;

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(ConfigurationFolder))
                {
                    Directory.CreateDirectory(ConfigurationFolder);
                }

                // Generate file path
                string fileName = SanitizeFileName(config.SceneName) + ConfigurationExtension;
                string filePath = Path.Combine(ConfigurationFolder, fileName);

                // Serialize configuration
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(filePath, json);

                // Import as asset if needed
                AssetDatabase.ImportAsset(filePath);

                _isDirty = false;
                _logger.LogInfo($"Saved configuration for scene: {config.SceneName}", LogCategory.Configuration);
                
                ConfigurationSaved?.Invoke(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save configuration: {ex.Message}", LogCategory.Configuration);
                ConfigurationError?.Invoke($"Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads configuration for a specific scene
        /// </summary>
        public SceneConfiguration LoadConfiguration(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return null;

            // Check cache first
            if (_sceneConfigs.TryGetValue(sceneName, out var cached))
                return cached;

            try
            {
                string fileName = SanitizeFileName(sceneName) + ConfigurationExtension;
                string filePath = Path.Combine(ConfigurationFolder, fileName);

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var config = JsonUtility.FromJson<SceneConfiguration>(json);
                    
                    if (config != null)
                    {
                        config.SceneName = sceneName; // Ensure scene name is correct
                        _sceneConfigs[sceneName] = config;
                        
                        // Restore GameObject references
                        RestoreReferences(config);
                        
                        _logger.LogInfo($"Loaded configuration for scene: {sceneName}", LogCategory.Configuration);
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load configuration: {ex.Message}", LogCategory.Configuration);
                ConfigurationError?.Invoke($"Failed to load configuration: {ex.Message}");
            }

            // Create new configuration if not found
            return GetOrCreateConfiguration(sceneName);
        }

        /// <summary>
        /// Merges two configurations with conflict resolution
        /// </summary>
        public SceneConfiguration MergeConfigurations(SceneConfiguration primary, SceneConfiguration secondary, MergeStrategy strategy)
        {
            if (primary == null)
                return secondary;
            if (secondary == null)
                return primary;

            var merged = new SceneConfiguration
            {
                SceneName = primary.SceneName,
                CreatedDate = primary.CreatedDate,
                LastModifiedDate = DateTime.Now
            };

            switch (strategy)
            {
                case MergeStrategy.PrimaryWins:
                    merged.RecordingConfiguration = primary.RecordingConfiguration.Clone() as RecordingConfiguration;
                    break;
                    
                case MergeStrategy.SecondaryWins:
                    merged.RecordingConfiguration = secondary.RecordingConfiguration.Clone() as RecordingConfiguration;
                    break;
                    
                case MergeStrategy.Merge:
                    merged.RecordingConfiguration = MergeRecordingConfigurations(
                        primary.RecordingConfiguration, 
                        secondary.RecordingConfiguration);
                    break;
            }

            return merged;
        }

        /// <summary>
        /// Exports configuration to JSON
        /// </summary>
        public string ExportConfiguration(SceneConfiguration config)
        {
            if (config == null)
                return null;

            var exportData = new ConfigurationExportData
            {
                Configuration = config,
                ExportDate = DateTime.Now,
                Version = "1.0",
                UnityVersion = Application.unityVersion
            };

            return JsonUtility.ToJson(exportData, true);
        }

        /// <summary>
        /// Imports configuration from JSON
        /// </summary>
        public SceneConfiguration ImportConfiguration(string json, string targetSceneName = null)
        {
            try
            {
                var exportData = JsonUtility.FromJson<ConfigurationExportData>(json);
                if (exportData?.Configuration == null)
                    return null;

                var config = exportData.Configuration;
                
                // Override scene name if specified
                if (!string.IsNullOrEmpty(targetSceneName))
                {
                    config.SceneName = targetSceneName;
                }

                // Validate version compatibility
                if (!IsVersionCompatible(exportData.Version))
                {
                    _logger.LogWarning($"Configuration version mismatch: {exportData.Version}", LogCategory.Configuration);
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import configuration: {ex.Message}", LogCategory.Configuration);
                return null;
            }
        }

        /// <summary>
        /// Deletes configuration for a specific scene
        /// </summary>
        public bool DeleteConfiguration(string sceneName)
        {
            try
            {
                string fileName = SanitizeFileName(sceneName) + ConfigurationExtension;
                string filePath = Path.Combine(ConfigurationFolder, fileName);

                if (File.Exists(filePath))
                {
                    AssetDatabase.DeleteAsset(filePath);
                    _sceneConfigs.Remove(sceneName);
                    
                    _logger.LogInfo($"Deleted configuration for scene: {sceneName}", LogCategory.Configuration);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete configuration: {ex.Message}", LogCategory.Configuration);
            }

            return false;
        }

        /// <summary>
        /// Gets all available scene configurations
        /// </summary>
        public List<SceneConfiguration> GetAllConfigurations()
        {
            var configs = new List<SceneConfiguration>();

            if (Directory.Exists(ConfigurationFolder))
            {
                var files = Directory.GetFiles(ConfigurationFolder, "*" + ConfigurationExtension);
                foreach (var file in files)
                {
                    var sceneName = Path.GetFileNameWithoutExtension(file);
                    if (sceneName.EndsWith(ConfigurationExtension))
                    {
                        sceneName = sceneName.Substring(0, sceneName.Length - ConfigurationExtension.Length);
                    }
                    
                    var config = LoadConfiguration(sceneName);
                    if (config != null)
                    {
                        configs.Add(config);
                    }
                }
            }

            return configs;
        }

        /// <summary>
        /// Validates a configuration
        /// </summary>
        public ValidationResult ValidateConfiguration(SceneConfiguration config)
        {
            var result = new ValidationResult();

            if (config == null)
            {
                result.AddError("Configuration is null");
                return result;
            }

            if (string.IsNullOrEmpty(config.SceneName))
            {
                result.AddError("Scene name is required");
            }

            if (config.RecordingConfiguration != null)
            {
                var recordingResult = config.RecordingConfiguration.Validate();
                result.Merge(recordingResult);
            }

            return result;
        }

        /// <summary>
        /// Marks the current configuration as dirty
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        // Private methods

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single || mode == OpenSceneMode.AdditiveLoad)
            {
                // Save previous scene configuration if dirty
                if (_isDirty && _currentSceneConfig != null)
                {
                    SaveConfiguration(_currentSceneConfig);
                }

                // Load new scene configuration
                LoadCurrentSceneConfiguration();
            }
        }

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (_isDirty && _currentSceneConfig != null && _currentSceneConfig.SceneName == scene.name)
            {
                // Auto-save on scene close
                SaveConfiguration(_currentSceneConfig);
            }
        }

        private void OnSceneSaved(Scene scene)
        {
            if (_currentSceneConfig != null && _currentSceneConfig.SceneName == scene.name)
            {
                // Update scene path
                _currentScenePath = scene.path;
                
                // Auto-save configuration
                if (_isDirty)
                {
                    SaveConfiguration(_currentSceneConfig);
                }
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _isDirty)
            {
                // Save before entering play mode
                SaveCurrentConfiguration();
            }
        }

        private void LoadCurrentSceneConfiguration()
        {
            var activeScene = SceneManager.GetActiveScene();
            _currentScenePath = activeScene.path;
            
            if (!string.IsNullOrEmpty(activeScene.name))
            {
                _currentSceneConfig = LoadConfiguration(activeScene.name);
                ConfigurationLoaded?.Invoke(_currentSceneConfig);
            }
        }

        private SceneConfiguration GetOrCreateConfiguration(string sceneName)
        {
            if (_sceneConfigs.TryGetValue(sceneName, out var existing))
                return existing;

            var config = new SceneConfiguration
            {
                SceneName = sceneName,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                RecordingConfiguration = RecordingConfiguration.CreateDefault()
            };

            _sceneConfigs[sceneName] = config;
            return config;
        }

        private void RestoreReferences(SceneConfiguration config)
        {
            if (config?.RecordingConfiguration?.TimelineConfigs == null)
                return;

            foreach (var timelineConfig in config.RecordingConfiguration.TimelineConfigs)
            {
                // Restore timeline references
                // This would be handled by the TimelineRecorderConfig class
            }
        }

        private RecordingConfiguration MergeRecordingConfigurations(RecordingConfiguration primary, RecordingConfiguration secondary)
        {
            var merged = primary.Clone() as RecordingConfiguration;

            // Merge timeline configurations
            var mergedTimelines = new List<TimelineRecorderConfig>();
            var primaryTimelines = primary.TimelineConfigs.Cast<TimelineRecorderConfig>().ToList();
            var secondaryTimelines = secondary.TimelineConfigs.Cast<TimelineRecorderConfig>().ToList();

            // Add all primary timelines
            mergedTimelines.AddRange(primaryTimelines);

            // Add secondary timelines that don't exist in primary
            foreach (var secondaryTimeline in secondaryTimelines)
            {
                if (!primaryTimelines.Any(t => t.Id == secondaryTimeline.Id))
                {
                    mergedTimelines.Add(secondaryTimeline);
                }
            }

            foreach (var timeline in mergedTimelines)
            {
                merged.AddTimelineConfig(timeline);
            }

            return merged;
        }

        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = fileName;
            
            foreach (var c in invalid)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            return sanitized;
        }

        private bool IsVersionCompatible(string version)
        {
            // Simple version check - could be enhanced
            return version == "1.0";
        }

        public void Dispose()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
    }

    /// <summary>
    /// Scene-specific configuration
    /// </summary>
    [Serializable]
    public class SceneConfiguration
    {
        [SerializeField]
        private string sceneName;
        
        [SerializeField]
        private RecordingConfiguration recordingConfiguration;
        
        [SerializeField]
        private string createdDate;
        
        [SerializeField]
        private string lastModifiedDate;
        
        [SerializeField]
        private Dictionary<string, string> customSettings = new Dictionary<string, string>();

        public string SceneName
        {
            get => sceneName;
            set => sceneName = value;
        }

        public RecordingConfiguration RecordingConfiguration
        {
            get => recordingConfiguration;
            set => recordingConfiguration = value;
        }

        public DateTime CreatedDate
        {
            get => DateTime.TryParse(createdDate, out var date) ? date : DateTime.MinValue;
            set => createdDate = value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public DateTime LastModifiedDate
        {
            get => DateTime.TryParse(lastModifiedDate, out var date) ? date : DateTime.MinValue;
            set => lastModifiedDate = value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public Dictionary<string, string> CustomSettings
        {
            get => customSettings;
            set => customSettings = value ?? new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Configuration export data
    /// </summary>
    [Serializable]
    public class ConfigurationExportData
    {
        public SceneConfiguration Configuration;
        public string ExportDate;
        public string Version;
        public string UnityVersion;
    }

    /// <summary>
    /// Merge strategies for configuration conflicts
    /// </summary>
    public enum MergeStrategy
    {
        PrimaryWins,
        SecondaryWins,
        Merge
    }
}