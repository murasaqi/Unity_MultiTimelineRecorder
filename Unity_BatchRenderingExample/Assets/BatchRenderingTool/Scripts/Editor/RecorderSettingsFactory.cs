using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Factory class for creating various RecorderSettings instances
    /// </summary>
    public static class RecorderSettingsFactory
    {
        /// <summary>
        /// Create a RecorderSettings instance based on the specified type
        /// </summary>
        public static RecorderSettings CreateRecorderSettings(RecorderSettingsType type, string name)
        {
            RecorderSettings settings = null;
            
            switch (type)
            {
                case RecorderSettingsType.Image:
                    settings = CreateImageRecorderSettings(name);
                    break;
                    
                case RecorderSettingsType.Movie:
                    settings = CreateMovieRecorderSettings(name);
                    break;
                    
                case RecorderSettingsType.Animation:
                    settings = CreateAnimationRecorderSettings(name);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    settings = CreateAlembicRecorderSettings(name);
                    break;
                    
                case RecorderSettingsType.AOV:
                    settings = CreateAOVRecorderSettings(name);
                    break;
                    
                case RecorderSettingsType.FBX:
                    settings = CreateFBXRecorderSettings(name);
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown recorder type: {type}");
            }
            
            if (settings != null)
            {
                ConfigureCommonSettings(settings);
            }
            
            return settings;
        }
        
        /// <summary>
        /// Create ImageRecorderSettings with default configuration
        /// </summary>
        public static ImageRecorderSettings CreateImageRecorderSettings(string name)
        {
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = name;
            
            // Default image settings
            settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
            settings.CaptureAlpha = false;
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };
            
            return settings;
        }
        
        /// <summary>
        /// Create ImageRecorderSettings with specific configuration
        /// </summary>
        public static ImageRecorderSettings CreateImageRecorderSettings(string name, ImageRecorderSettingsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                throw new ArgumentException($"Invalid configuration: {errorMessage}");
            }
            
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = name;
            
            // Apply configuration
            config.ApplyToSettings(settings);
            
            // Apply common settings
            ConfigureCommonSettings(settings);
            
            return settings;
        }
        
        /// <summary>
        /// Create MovieRecorderSettings with default configuration
        /// </summary>
        public static MovieRecorderSettings CreateMovieRecorderSettings(string name)
        {
            var settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            settings.name = name;
            
            // Default movie settings
            settings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            // Note: Unity Recorder API doesn't expose VideoBitRateMode directly
            settings.CaptureAudio = false;
            settings.CaptureAlpha = false;
            
            settings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };
            
            // Audio settings (when enabled)
            settings.AudioInputSettings.PreserveAudio = true;
            
            return settings;
        }
        
        /// <summary>
        /// Create MovieRecorderSettings with specific configuration
        /// </summary>
        public static MovieRecorderSettings CreateMovieRecorderSettings(string name, MovieRecorderSettingsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                throw new ArgumentException($"Invalid configuration: {errorMessage}");
            }
            
            var settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            settings.name = name;
            
            // Apply configuration
            config.ApplyToSettings(settings);
            
            // Apply common settings
            ConfigureCommonSettings(settings);
            
            return settings;
        }
        
        /// <summary>
        /// Create MovieRecorderSettings with a preset configuration
        /// </summary>
        public static MovieRecorderSettings CreateMovieRecorderSettings(string name, MovieRecorderPreset preset)
        {
            var config = MovieRecorderSettingsConfig.GetPreset(preset);
            return CreateMovieRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create AOVRecorderSettings with default configuration
        /// </summary>
        public static RecorderSettings CreateAOVRecorderSettings(string name)
        {
            // Check if HDRP is available
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                BatchRenderingToolLogger.LogError("AOV Recorder requires HDRP package to be installed");
                return null;
            }
            
            // Create default AOV configuration
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.Depth | AOVType.Normal | AOVType.Albedo,
                outputFormat = AOVOutputFormat.EXR16,
                width = 1920,
                height = 1080,
                frameRate = 24
            };
            
            // For single AOV creation, return the first setting or a placeholder
            var settingsList = CreateAOVRecorderSettings(name, config);
            return settingsList.Count > 0 ? settingsList[0] : null;
        }
        
        /// <summary>
        /// Create AOVRecorderSettings with specific configuration
        /// </summary>
        public static List<RecorderSettings> CreateAOVRecorderSettings(string name, AOVRecorderSettingsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                throw new ArgumentException($"Invalid AOV configuration: {errorMessage}");
            }
            
            BatchRenderingToolLogger.LogVerbose($"[RecorderSettingsFactory] Creating AOV recorder settings for: {name}");
            
            // Unity Recorder 5.1.2では専用のAOVRecorderSettingsが存在しないため、
            // ImageRecorderSettingsを使用した暫定実装を使用
            BatchRenderingToolLogger.LogWarning("[RecorderSettingsFactory] Using fallback AOV implementation with ImageRecorderSettings");
            
            var settingsList = AOVRecorderImplementation.AOVRecorderFallback.CreateAOVRecorderSettingsFallback(name, config);
            
            // Apply common settings to each
            foreach (var settings in settingsList)
            {
                ConfigureCommonSettings(settings);
            }
            
            return settingsList;
        }
        
        /// <summary>
        /// Create AOVRecorderSettings with a preset configuration
        /// </summary>
        public static List<RecorderSettings> CreateAOVRecorderSettings(string name, AOVPreset preset)
        {
            AOVRecorderSettingsConfig config = null;
            
            switch (preset)
            {
                case AOVPreset.Compositing:
                    config = AOVRecorderSettingsConfig.Presets.GetCompositing();
                    break;
                case AOVPreset.GeometryOnly:
                    config = AOVRecorderSettingsConfig.Presets.GetGeometryOnly();
                    break;
                case AOVPreset.LightingOnly:
                    config = AOVRecorderSettingsConfig.Presets.GetLightingOnly();
                    break;
                case AOVPreset.MaterialProperties:
                    config = AOVRecorderSettingsConfig.Presets.GetMaterialProperties();
                    break;
                default:
                    throw new ArgumentException($"Unknown AOV preset: {preset}");
            }
            
            return CreateAOVRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create AlembicRecorderSettings with default configuration
        /// </summary>
        public static RecorderSettings CreateAlembicRecorderSettings(string name)
        {
            // Check if Alembic package is available
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                BatchRenderingToolLogger.LogError("Alembic Recorder requires Unity Alembic package to be installed");
                return null;
            }
            
            // Create default Alembic configuration
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform,
                exportScope = AlembicExportScope.EntireScene,
                frameRate = 24f,
                scaleFactor = 1f,
                handedness = AlembicHandedness.Left
            };
            
            return CreateAlembicRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create AlembicRecorderSettings with specific configuration
        /// </summary>
        public static RecorderSettings CreateAlembicRecorderSettings(string name, AlembicRecorderSettingsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                throw new ArgumentException($"Invalid Alembic configuration: {errorMessage}");
            }
            
            BatchRenderingToolLogger.LogVerbose($"[RecorderSettingsFactory] Creating Alembic recorder settings for: {name}");
            
            // Create recorder settings
            var settings = config.CreateAlembicRecorderSettings(name);
            
            // Apply common settings
            if (settings != null)
            {
                ConfigureCommonSettings(settings);
            }
            
            return settings;
        }
        
        /// <summary>
        /// Create AlembicRecorderSettings with a preset configuration
        /// </summary>
        public static RecorderSettings CreateAlembicRecorderSettings(string name, AlembicExportPreset preset)
        {
            var config = AlembicRecorderSettingsConfig.GetPreset(preset);
            return CreateAlembicRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create AnimationRecorderSettings with default configuration
        /// </summary>
        public static RecorderSettings CreateAnimationRecorderSettings(string name)
        {
            // Create default Animation configuration
            var config = new AnimationRecorderSettingsConfig
            {
                recordingProperties = AnimationRecordingProperties.TransformOnly,
                recordingScope = AnimationRecordingScope.SingleGameObject,
                frameRate = 30f,
                compressionLevel = AnimationCompressionLevel.Medium,
                interpolationMode = AnimationInterpolationMode.Linear
            };
            
            return CreateAnimationRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create AnimationRecorderSettings with specific configuration
        /// </summary>
        public static RecorderSettings CreateAnimationRecorderSettings(string name, AnimationRecorderSettingsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                throw new ArgumentException($"Invalid Animation configuration: {errorMessage}");
            }
            
            BatchRenderingToolLogger.LogVerbose($"[RecorderSettingsFactory] Creating Animation recorder settings for: {name}");
            
            // Create recorder settings
            var settings = config.CreateAnimationRecorderSettings(name);
            
            // Apply common settings
            if (settings != null)
            {
                ConfigureCommonSettings(settings);
            }
            
            return settings;
        }
        
        /// <summary>
        /// Create AnimationRecorderSettings with a preset configuration
        /// </summary>
        public static RecorderSettings CreateAnimationRecorderSettings(string name, AnimationExportPreset preset)
        {
            var config = AnimationRecorderSettingsConfig.GetPreset(preset);
            return CreateAnimationRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create FBXRecorderSettings with default configuration
        /// </summary>
        public static RecorderSettings CreateFBXRecorderSettings(string name)
        {
            // Check if FBX package is available
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                BatchRenderingToolLogger.LogError("FBX Recorder requires Unity FBX package to be installed");
                return null;
            }
            
            // Create default FBX configuration
            var config = new FBXRecorderSettingsConfig
            {
                targetGameObject = null,
                recordHierarchy = true,
                clampedTangents = true,
                animationCompression = FBXAnimationCompressionLevel.Lossy,
                exportGeometry = true,
                transferAnimationSource = null,
                transferAnimationDest = null,
                frameRate = 24f
            };
            
            return CreateFBXRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Create FBXRecorderSettings with specific configuration
        /// </summary>
        public static RecorderSettings CreateFBXRecorderSettings(string name, FBXRecorderSettingsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                throw new ArgumentException($"Invalid FBX configuration: {errorMessage}");
            }
            
            BatchRenderingToolLogger.LogVerbose($"[RecorderSettingsFactory] Creating FBX recorder settings for: {name}");
            
            // Create recorder settings
            var settings = config.CreateFBXRecorderSettings(name);
            
            // Apply common settings
            if (settings != null)
            {
                ConfigureCommonSettings(settings);
                BatchRenderingToolLogger.Log($"[RecorderSettingsFactory] Created FBX recorder settings of actual type: {settings.GetType().FullName}");
            }
            else
            {
                BatchRenderingToolLogger.LogError("[RecorderSettingsFactory] Failed to create FBX recorder settings - returned null");
            }
            
            return settings;
        }
        
        /// <summary>
        /// Create FBXRecorderSettings with a preset configuration
        /// </summary>
        public static RecorderSettings CreateFBXRecorderSettings(string name, FBXExportPreset preset)
        {
            var config = FBXRecorderSettingsConfig.GetPreset(preset);
            return CreateFBXRecorderSettings(name, config);
        }
        
        /// <summary>
        /// Configure common settings for all recorder types
        /// </summary>
        private static void ConfigureCommonSettings(RecorderSettings settings)
        {
            settings.Enabled = true;
            settings.RecordMode = RecordMode.Manual;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            // Don't override FrameRate here - it should be set by the specific config
            // settings.FrameRate = 24;
            settings.CapFrameRate = true;
        }
        
        /// <summary>
        /// Detect the type of an existing RecorderSettings instance
        /// </summary>
        public static RecorderSettingsType DetectRecorderType(RecorderSettings settings)
        {
            if (settings is ImageRecorderSettings)
                return RecorderSettingsType.Image;
            else if (settings is MovieRecorderSettings)
                return RecorderSettingsType.Movie;
            else if (settings.GetType().Name == "AnimationRecorderSettings")
                return RecorderSettingsType.Animation;
            else if (settings.GetType().Name == "AlembicRecorderSettings")
                return RecorderSettingsType.Alembic;
            else if (settings.GetType().Name == "AOVRecorderSettings")
                return RecorderSettingsType.AOV;
            else if (settings.GetType().Name == "FbxRecorderSettings")
                return RecorderSettingsType.FBX;
            else
                throw new NotSupportedException($"Unknown recorder settings type: {settings.GetType().Name}");
        }
        
        /// <summary>
        /// Validate if a recorder type is currently supported
        /// </summary>
        public static bool IsRecorderTypeSupported(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Image:
                case RecorderSettingsType.Movie:
                    return true;
                    
                case RecorderSettingsType.AOV:
                    // AOV is supported only if HDRP is available
                    return AOVTypeInfo.IsHDRPAvailable();
                    
                case RecorderSettingsType.Alembic:
                    // Alembic is supported only if Alembic package is available
                    return AlembicExportInfo.IsAlembicPackageAvailable();
                    
                case RecorderSettingsType.Animation:
                    return true;
                    
                case RecorderSettingsType.FBX:
                    // FBX is supported only if FBX package is available
                    return FBXExportInfo.IsFBXPackageAvailable();
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get display name for recorder type
        /// </summary>
        public static string GetRecorderTypeDisplayName(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Image:
                    return "Image Sequence";
                case RecorderSettingsType.Movie:
                    return "Movie";
                case RecorderSettingsType.Animation:
                    return "Animation Clip";
                case RecorderSettingsType.Alembic:
                    return "Alembic";
                case RecorderSettingsType.AOV:
                    return "AOV (HDRP)";
                case RecorderSettingsType.FBX:
                    return "FBX";
                default:
                    return type.ToString();
            }
        }
    }
}