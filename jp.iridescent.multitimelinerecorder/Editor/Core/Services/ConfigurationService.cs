using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service responsible for configuration management
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger _logger;
        private readonly string _defaultConfigPath;
        private readonly string _sceneSettingsPath;

        // Default paths
        private const string DEFAULT_CONFIG_FOLDER = "Assets/MultiTimelineRecorder/Settings";
        private const string DEFAULT_CONFIG_FILE = "DefaultRecordingConfig.asset";
        private const string SCENE_SETTINGS_FOLDER = "SceneSettings";

        public ConfigurationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultConfigPath = Path.Combine(DEFAULT_CONFIG_FOLDER, DEFAULT_CONFIG_FILE);
            _sceneSettingsPath = Path.Combine(DEFAULT_CONFIG_FOLDER, SCENE_SETTINGS_FOLDER);
            
            EnsureDirectoriesExist();
        }

        /// <inheritdoc />
        public void SaveConfiguration(IRecordingConfiguration config, string path = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            path = path ?? _defaultConfigPath;
            _logger.LogInfo($"Saving configuration to: {path}", LogCategory.Configuration);

            try
            {
                // Convert to serializable format
                var serializableConfig = config as RecordingConfiguration;
                if (serializableConfig == null && config != null)
                {
                    // If it's not already a RecordingConfiguration, create one
                    serializableConfig = new RecordingConfiguration
                    {
                        FrameRate = config.FrameRate,
                        Resolution = config.Resolution,
                        OutputPath = config.OutputPath,
                        GlobalSettings = config.GlobalSettings as GlobalSettings ?? GlobalSettings.CreateDefault()
                    };

                    // Copy timeline configs
                    foreach (var timelineConfig in config.TimelineConfigs)
                    {
                        if (timelineConfig is TimelineRecorderConfig trc)
                        {
                            serializableConfig.AddTimelineConfig(trc);
                        }
                    }
                }

                // Create or update ScriptableObject
                var existingAsset = AssetDatabase.LoadAssetAtPath<RecordingConfigurationAsset>(path);
                if (existingAsset != null)
                {
                    existingAsset.Configuration = serializableConfig;
                    EditorUtility.SetDirty(existingAsset);
                }
                else
                {
                    var asset = ScriptableObject.CreateInstance<RecordingConfigurationAsset>();
                    asset.Configuration = serializableConfig;
                    AssetDatabase.CreateAsset(asset, path);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                _logger.LogInfo("Configuration saved successfully", LogCategory.Configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save configuration: {ex.Message}", LogCategory.Configuration);
                throw new FileSystemException($"Failed to save configuration to {path}", path, ex);
            }
        }

        /// <inheritdoc />
        public IRecordingConfiguration LoadConfiguration(string path = null)
        {
            path = path ?? _defaultConfigPath;
            _logger.LogInfo($"Loading configuration from: {path}", LogCategory.Configuration);

            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<RecordingConfigurationAsset>(path);
                if (asset != null && asset.Configuration != null)
                {
                    _logger.LogInfo("Configuration loaded successfully", LogCategory.Configuration);
                    return asset.Configuration;
                }

                _logger.LogWarning("Configuration not found, returning default", LogCategory.Configuration);
                return GetDefaultConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load configuration: {ex.Message}", LogCategory.Configuration);
                return GetDefaultConfiguration();
            }
        }

        /// <inheritdoc />
        public void SaveSceneSettings(string scenePath, SceneSettings settings)
        {
            if (string.IsNullOrEmpty(scenePath) || settings == null)
            {
                return;
            }

            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var settingsPath = Path.Combine(_sceneSettingsPath, $"{sceneName}_Settings.asset");
            
            _logger.LogDebug($"Saving scene settings for: {sceneName}", LogCategory.Configuration);

            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneSettingsAsset>(settingsPath);
                if (asset != null)
                {
                    asset.Settings = settings;
                    EditorUtility.SetDirty(asset);
                }
                else
                {
                    asset = ScriptableObject.CreateInstance<SceneSettingsAsset>();
                    asset.Settings = settings;
                    AssetDatabase.CreateAsset(asset, settingsPath);
                }

                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save scene settings: {ex.Message}", LogCategory.Configuration);
            }
        }

        /// <inheritdoc />
        public SceneSettings LoadSceneSettings(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return new SceneSettings { ScenePath = scenePath };
            }

            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var settingsPath = Path.Combine(_sceneSettingsPath, $"{sceneName}_Settings.asset");
            
            _logger.LogDebug($"Loading scene settings for: {sceneName}", LogCategory.Configuration);

            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneSettingsAsset>(settingsPath);
                if (asset != null && asset.Settings != null)
                {
                    return asset.Settings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load scene settings: {ex.Message}", LogCategory.Configuration);
            }

            return new SceneSettings { ScenePath = scenePath };
        }

        /// <inheritdoc />
        public IRecordingConfiguration GetDefaultConfiguration()
        {
            return RecordingConfiguration.CreateDefault();
        }

        /// <inheritdoc />
        public string ExportConfiguration(IRecordingConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            try
            {
                return JsonUtility.ToJson(config, true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to export configuration: {ex.Message}", LogCategory.Configuration);
                throw new RecordingConfigurationException("Failed to export configuration", ex);
            }
        }

        /// <inheritdoc />
        public IRecordingConfiguration ImportConfiguration(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("JSON string cannot be empty", nameof(json));
            }

            try
            {
                var config = new RecordingConfiguration();
                JsonUtility.FromJsonOverwrite(json, config);
                
                var validationResult = config.Validate();
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Imported configuration has validation issues", LogCategory.Configuration);
                }
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import configuration: {ex.Message}", LogCategory.Configuration);
                throw new RecordingConfigurationException("Failed to import configuration", ex);
            }
        }

        /// <inheritdoc />
        public List<ConfigurationInfo> ListSavedConfigurations()
        {
            var configurations = new List<ConfigurationInfo>();

            try
            {
                var guids = AssetDatabase.FindAssets("t:RecordingConfigurationAsset", new[] { DEFAULT_CONFIG_FOLDER });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<RecordingConfigurationAsset>(path);
                    
                    if (asset != null)
                    {
                        var fileInfo = new FileInfo(path);
                        configurations.Add(new ConfigurationInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(path),
                            Path = path,
                            CreatedDate = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            ModifiedDate = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to list configurations: {ex.Message}", LogCategory.Configuration);
            }

            return configurations;
        }

        /// <summary>
        /// Ensures required directories exist
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            EnsureDirectoryExists(DEFAULT_CONFIG_FOLDER);
            EnsureDirectoryExists(_sceneSettingsPath);
        }

        /// <summary>
        /// Ensures a directory exists
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var currentPath = parts[0];
                
                for (int i = 1; i < parts.Length; i++)
                {
                    var nextPath = $"{currentPath}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = nextPath;
                }
            }
        }
    }

    /// <summary>
    /// ScriptableObject wrapper for RecordingConfiguration
    /// </summary>
    [System.Serializable]
    public class RecordingConfigurationAsset : ScriptableObject
    {
        [SerializeField]
        private RecordingConfiguration configuration;

        public RecordingConfiguration Configuration
        {
            get => configuration;
            set => configuration = value;
        }
    }

    /// <summary>
    /// ScriptableObject wrapper for SceneSettings
    /// </summary>
    [System.Serializable]
    public class SceneSettingsAsset : ScriptableObject
    {
        [SerializeField]
        private SceneSettings settings;

        public SceneSettings Settings
        {
            get => settings;
            set => settings = value;
        }
    }
}