using System;
using System.Collections.Generic;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models.RecorderSettings;

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
            // TODO: Add other recorder types
            // { RecorderSettingsType.Animation, () => new AnimationRecorderConfiguration() },
            // { RecorderSettingsType.Alembic, () => new AlembicRecorderConfiguration() },
            // { RecorderSettingsType.AOV, () => new AOVRecorderConfiguration() },
            // { RecorderSettingsType.FBX, () => new FBXRecorderConfiguration() }
        };

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

            throw new NotSupportedException($"Recorder type {type} is not supported yet");
        }

        /// <summary>
        /// Creates a recorder configuration with default settings
        /// </summary>
        /// <param name="type">The type of recorder to create</param>
        /// <param name="name">Optional name for the recorder</param>
        /// <returns>A new recorder configuration with default settings</returns>
        public static IRecorderConfiguration CreateDefaultConfiguration(RecorderSettingsType type, string name = null)
        {
            var config = CreateConfiguration(type);
            
            if (!string.IsNullOrEmpty(name))
            {
                config.Name = name;
            }

            // Apply type-specific defaults
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
            }

            return config;
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
    }
}