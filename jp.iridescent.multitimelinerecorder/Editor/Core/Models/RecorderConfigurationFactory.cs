using System;
using System.Collections.Generic;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using UnityEngine;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Factory for creating recorder configurations
    /// </summary>
    public class RecorderConfigurationFactory
    {
        private static readonly Dictionary<RecorderSettingsType, Func<IRecorderConfiguration>> _factories = 
            new Dictionary<RecorderSettingsType, Func<IRecorderConfiguration>>
        {
            { RecorderSettingsType.Image, () => new ImageRecorderConfiguration() },
            { RecorderSettingsType.Movie, () => new MovieRecorderConfiguration() },
            { RecorderSettingsType.Animation, () => new AnimationRecorderConfiguration() },
            { RecorderSettingsType.Alembic, () => new AlembicRecorderConfiguration() },
            { RecorderSettingsType.AOV, () => new AOVRecorderConfiguration() },
            { RecorderSettingsType.FBX, () => new FBXRecorderConfiguration() }
        };

        private static IGameObjectReferenceService _gameObjectReferenceService;
        private static int? _globalFrameRate;

        /// <summary>
        /// Sets the GameObject reference service for automatic reference resolution
        /// </summary>
        public static void SetGameObjectReferenceService(IGameObjectReferenceService service)
        {
            _gameObjectReferenceService = service;
        }

        /// <summary>
        /// Sets the global frame rate for unified frame rate application
        /// </summary>
        public static void SetGlobalFrameRate(int frameRate)
        {
            _globalFrameRate = Mathf.Clamp(frameRate, 1, 120);
        }

        /// <summary>
        /// Creates a recorder configuration of the specified type
        /// </summary>
        /// <param name="type">The type of recorder to create</param>
        /// <returns>A new recorder configuration instance</returns>
        public static IRecorderConfiguration CreateConfiguration(RecorderSettingsType type)
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory();
            }

            throw new NotSupportedException($"Recorder type {type} is not supported");
        }

        /// <summary>
        /// Creates a recorder configuration with default settings
        /// </summary>
        /// <param name="type">The type of recorder to create</param>
        /// <param name="name">Optional name for the recorder</param>
        /// <param name="targetGameObject">Optional target GameObject for animation/alembic recorders</param>
        /// <returns>A new recorder configuration with default settings</returns>
        public static IRecorderConfiguration CreateDefaultConfiguration(
            RecorderSettingsType type, 
            string name = null,
            GameObject targetGameObject = null)
        {
            var config = CreateConfiguration(type);
            
            if (!string.IsNullOrEmpty(name))
            {
                config.Name = name;
            }

            // Apply global frame rate if available
            if (_globalFrameRate.HasValue)
            {
                ApplyFrameRate(config, _globalFrameRate.Value);
            }

            // Apply type-specific defaults and GameObject references
            switch (type)
            {
                case RecorderSettingsType.Image:
                    if (config is ImageRecorderConfiguration imageConfig)
                    {
                        imageConfig.OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                        imageConfig.CaptureAlpha = false;
                        imageConfig.SourceType = ImageRecorderSourceType.GameView;
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    if (config is MovieRecorderConfiguration movieConfig)
                    {
                        movieConfig.OutputFormat = UnityEditor.Recorder.MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                        movieConfig.Quality = 0.75f;
                        movieConfig.CaptureAudio = true;
                        movieConfig.SourceType = ImageRecorderSourceType.GameView;
                    }
                    break;

                case RecorderSettingsType.Animation:
                    if (config is AnimationRecorderConfiguration animConfig)
                    {
                        if (targetGameObject != null)
                        {
                            animConfig.TargetGameObject = targetGameObject;
                            
                            // Create reference if service is available
                            if (_gameObjectReferenceService != null)
                            {
                                var reference = _gameObjectReferenceService.CreateReference(targetGameObject);
                                // Store reference for later restoration if needed
                            }
                        }
                        animConfig.RecordHierarchy = true;
                        animConfig.RecordBlendShapes = true;
                        animConfig.RecordTransform = true;
                        animConfig.ClampedTangents = false;
                    }
                    break;

                case RecorderSettingsType.Alembic:
                    if (config is AlembicRecorderConfiguration alembicConfig)
                    {
                        if (targetGameObject != null)
                        {
                            alembicConfig.TargetGameObject = targetGameObject;
                            
                            // Create reference if service is available
                            if (_gameObjectReferenceService != null)
                            {
                                var reference = _gameObjectReferenceService.CreateReference(targetGameObject);
                                // Store reference for later restoration if needed
                            }
                        }
                        alembicConfig.RecordHierarchy = true;
                        alembicConfig.CaptureTransform = true;
                        alembicConfig.CaptureMeshRenderer = true;
                        alembicConfig.CaptureSkinnedMeshRenderer = true;
                        alembicConfig.ScaleFactor = 1.0f;
                    }
                    break;

                case RecorderSettingsType.AOV:
                    if (config is AOVRecorderConfiguration aovConfig)
                    {
                        aovConfig.OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                        aovConfig.CaptureAlpha = true;
                        aovConfig.SourceType = ImageRecorderSourceType.TargetCamera;
                        aovConfig.AOVType = MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Beauty;
                    }
                    break;

                case RecorderSettingsType.FBX:
                    if (config is FBXRecorderConfiguration fbxConfig)
                    {
                        if (targetGameObject != null)
                        {
                            fbxConfig.TargetGameObject = targetGameObject;
                            
                            // Create reference if service is available
                            if (_gameObjectReferenceService != null)
                            {
                                var reference = _gameObjectReferenceService.CreateReference(targetGameObject);
                                // Store reference for later restoration if needed
                            }
                        }
                        fbxConfig.RecordHierarchy = true;
                        fbxConfig.ExportSkinnedMesh = true;
                        fbxConfig.ExportMeshes = true;
                        fbxConfig.ExportAnimation = true;
                    }
                    break;
            }

            return config;
        }

        /// <summary>
        /// Applies frame rate to a recorder configuration
        /// </summary>
        /// <param name="config">The configuration to apply frame rate to</param>
        /// <param name="frameRate">The frame rate to apply</param>
        private static void ApplyFrameRate(IRecorderConfiguration config, int frameRate)
        {
            // Apply frame rate based on recorder type
            switch (config)
            {
                case AnimationRecorderConfiguration animConfig:
                    animConfig.FrameRate = frameRate;
                    break;
                    
                case MovieRecorderConfiguration movieConfig:
                    // Movie recorder frame rate is handled by Timeline
                    // but we can set it as a hint
                    break;
                    
                case AlembicRecorderConfiguration alembicConfig:
                    // Alembic uses time sampling settings
                    break;
                    
                case FBXRecorderConfiguration fbxConfig:
                    // FBX frame rate is handled differently
                    break;
                    
                // Image and AOV recorders don't have frame rate settings
            }
        }

        /// <summary>
        /// Creates a recorder configuration with automatic GameObject resolution
        /// </summary>
        /// <param name="type">The type of recorder to create</param>
        /// <param name="scenePath">Scene path to search for GameObjects</param>
        /// <param name="objectName">Name of the GameObject to find</param>
        /// <returns>A new recorder configuration with resolved GameObject</returns>
        public static IRecorderConfiguration CreateConfigurationWithAutoResolve(
            RecorderSettingsType type,
            string scenePath,
            string objectName)
        {
            var config = CreateConfiguration(type);
            
            // Apply global frame rate if available
            if (_globalFrameRate.HasValue)
            {
                ApplyFrameRate(config, _globalFrameRate.Value);
            }

            // Auto-resolve GameObject for types that need it
            if (type == RecorderSettingsType.Animation || 
                type == RecorderSettingsType.Alembic || 
                type == RecorderSettingsType.FBX)
            {
                GameObject targetObject = null;
                
                // Try to find the GameObject in the scene
                if (!string.IsNullOrEmpty(objectName))
                {
                    targetObject = GameObject.Find(objectName);
                    
                    if (targetObject == null)
                    {
                        // Try to find in all root objects
                        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                        foreach (var root in rootObjects)
                        {
                            targetObject = FindChildByName(root.transform, objectName)?.gameObject;
                            if (targetObject != null)
                                break;
                        }
                    }
                }

                if (targetObject != null)
                {
                    // Apply the found GameObject to the configuration
                    switch (config)
                    {
                        case AnimationRecorderConfiguration animConfig:
                            animConfig.TargetGameObject = targetObject;
                            break;
                        case AlembicRecorderConfiguration alembicConfig:
                            alembicConfig.TargetGameObject = targetObject;
                            break;
                        case FBXRecorderConfiguration fbxConfig:
                            fbxConfig.TargetGameObject = targetObject;
                            break;
                    }

                    // Create reference if service is available
                    if (_gameObjectReferenceService != null)
                    {
                        _gameObjectReferenceService.CreateReference(targetObject);
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Finds a child transform by name recursively
        /// </summary>
        private static Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                var result = FindChildByName(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Registers a custom recorder configuration factory
        /// </summary>
        /// <param name="type">The recorder type</param>
        /// <param name="factory">Factory function to create the configuration</param>
        public static void RegisterFactory(RecorderSettingsType type, Func<IRecorderConfiguration> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factories[type] = factory;
        }

        /// <summary>
        /// Unregisters a custom recorder configuration factory
        /// </summary>
        /// <param name="type">The recorder type to unregister</param>
        /// <returns>True if the factory was unregistered</returns>
        public static bool UnregisterFactory(RecorderSettingsType type)
        {
            return _factories.Remove(type);
        }

        /// <summary>
        /// Checks if a recorder type is supported
        /// </summary>
        /// <param name="type">The recorder type to check</param>
        /// <returns>True if the type is supported</returns>
        public static bool IsTypeSupported(RecorderSettingsType type)
        {
            return _factories.ContainsKey(type);
        }

        /// <summary>
        /// Gets all supported recorder types
        /// </summary>
        /// <returns>Array of supported recorder types</returns>
        public static RecorderSettingsType[] GetSupportedTypes()
        {
            var types = new List<RecorderSettingsType>();
            foreach (var key in _factories.Keys)
            {
                types.Add(key);
            }
            return types.ToArray();
        }

        /// <summary>
        /// Resets the factory to default state
        /// </summary>
        public static void Reset()
        {
            _gameObjectReferenceService = null;
            _globalFrameRate = null;
            
            // Reset to default factories only
            _factories.Clear();
            _factories[RecorderSettingsType.Image] = () => new ImageRecorderConfiguration();
            _factories[RecorderSettingsType.Movie] = () => new MovieRecorderConfiguration();
            _factories[RecorderSettingsType.Animation] = () => new AnimationRecorderConfiguration();
            _factories[RecorderSettingsType.Alembic] = () => new AlembicRecorderConfiguration();
            _factories[RecorderSettingsType.AOV] = () => new AOVRecorderConfiguration();
            _factories[RecorderSettingsType.FBX] = () => new FBXRecorderConfiguration();
        }
    }
}