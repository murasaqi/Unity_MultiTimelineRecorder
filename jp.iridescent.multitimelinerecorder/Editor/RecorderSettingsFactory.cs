using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace Unity.MultiTimelineRecorder
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
                MultiTimelineRecorderLogger.LogError("AOV Recorder requires HDRP package to be installed");
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
            
            MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsFactory] Creating AOV recorder settings for: {name}");
            
            var settingsList = new List<RecorderSettings>();
            
            // Unity Recorder has AOVRecorderSettings, use it directly
            try
            {
                // Create single AOVRecorderSettings instance
                var aovSettings = ScriptableObject.CreateInstance<UnityEditor.Recorder.AOVRecorderSettings>();
                aovSettings.name = name;
                
                // Configure AOV types
                var aovTypeList = new List<UnityEditor.Recorder.AOVType>();
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Beauty) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Beauty);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Albedo) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Albedo);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Normal) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Normal);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Smoothness) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Smoothness);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.AmbientOcclusion) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.AmbientOcclusion);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Metal) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Metal);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Specular) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Specular);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Alpha) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Alpha);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.DirectDiffuse) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.DirectDiffuse);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.DirectSpecular) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.DirectSpecular);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.IndirectDiffuse) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.IndirectDiffuse);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Reflection) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Reflection);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Refraction) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Refraction);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Emissive) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Emissive);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.MotionVectors) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.MotionVectors);
                if ((config.selectedAOVs & Unity.MultiTimelineRecorder.AOVType.Depth) != 0)
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Depth);
                
                // Add Beauty pass if none selected
                if (aovTypeList.Count == 0)
                {
                    aovTypeList.Add(UnityEditor.Recorder.AOVType.Beauty);
                }
                
                // Set AOV types
                aovSettings.SetAOVSelection(aovTypeList.ToArray());
                
                // Configure output format
                switch (config.outputFormat)
                {
                    case AOVOutputFormat.PNG:
                        aovSettings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                        MultiTimelineRecorderLogger.Log($"[RecorderSettingsFactory] Set AOV output format to PNG");
                        break;
                    case AOVOutputFormat.JPEG:
                        aovSettings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.JPEG;
                        MultiTimelineRecorderLogger.Log($"[RecorderSettingsFactory] Set AOV output format to JPEG");
                        break;
                    case AOVOutputFormat.EXR16:
                    case AOVOutputFormat.EXR32:
                        aovSettings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                        MultiTimelineRecorderLogger.Log($"[RecorderSettingsFactory] Set AOV output format to EXR");
                        break;
                }
                
                // Configure multi-part EXR
                aovSettings.IsMultiPartEXR = aovTypeList.Count > 1;
                
                // Configure resolution using AOVCameraInputSettings
                var aovInputSettings = new UnityEditor.Recorder.Input.AOVCameraInputSettings
                {
                    OutputWidth = config.width,
                    OutputHeight = config.height,
                    FlipFinalOutput = config.flipVertical,
                    RecordTransparency = false
                };
                aovSettings.imageInputSettings = aovInputSettings;
                
                // Configure frame rate
                aovSettings.FrameRate = config.frameRate;
                aovSettings.CapFrameRate = config.capFrameRate;
                
                settingsList.Add(aovSettings);
                
                // Log the actual output format after configuration
                MultiTimelineRecorderLogger.Log($"[RecorderSettingsFactory] Created AOVRecorderSettings with {aovTypeList.Count} AOV types");
                MultiTimelineRecorderLogger.Log($"[RecorderSettingsFactory] Final AOV output format: {aovSettings.OutputFormat}");
            }
            catch (Exception ex)
            {
                MultiTimelineRecorderLogger.LogWarning($"[RecorderSettingsFactory] Failed to create AOVRecorderSettings: {ex.Message}");
                MultiTimelineRecorderLogger.LogWarning("[RecorderSettingsFactory] Falling back to ImageRecorderSettings implementation");
                
                // Fallback to ImageRecorderSettings implementation
                settingsList = AOVRecorderImplementation.AOVRecorderFallback.CreateAOVRecorderSettingsFallback(name, config);
            }
            
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
                MultiTimelineRecorderLogger.LogError("Alembic Recorder requires Unity Alembic package to be installed");
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
            
            MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsFactory] Creating Alembic recorder settings for: {name}");
            
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
            
            MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsFactory] Creating Animation recorder settings for: {name}");
            
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
                MultiTimelineRecorderLogger.LogError("FBX Recorder requires Unity FBX package to be installed");
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
            
            MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsFactory] Creating FBX recorder settings for: {name}");
            
            // Create recorder settings
            var settings = config.CreateFBXRecorderSettings(name);
            
            // Apply common settings
            if (settings != null)
            {
                ConfigureCommonSettings(settings);
                // Reapply frame rate after common settings to ensure it's not overwritten
                MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsFactory] Before reapplying frame rate: {settings.FrameRate}");
                settings.FrameRate = config.frameRate;
                MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsFactory] After reapplying frame rate: {settings.FrameRate} (expected: {config.frameRate})");
                MultiTimelineRecorderLogger.Log($"[RecorderSettingsFactory] Created FBX recorder settings of actual type: {settings.GetType().FullName}");
            }
            else
            {
                MultiTimelineRecorderLogger.LogError("[RecorderSettingsFactory] Failed to create FBX recorder settings - returned null");
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
            
            // Log current frame rate to debug
            MultiTimelineRecorderLogger.LogVerbose($"[ConfigureCommonSettings] Current FrameRate after common settings: {settings.FrameRate}");
        }
        
        /// <summary>
        /// Detect the type of an existing RecorderSettings instance
        /// </summary>
        public static RecorderSettingsType DetectRecorderType(RecorderSettings settings)
        {
            var typeName = settings.GetType().Name;
            
            // Check specific types first
            if (typeName == "AOVRecorderSettings")
                return RecorderSettingsType.AOV;
            else if (typeName == "AnimationRecorderSettings")
                return RecorderSettingsType.Animation;
            else if (typeName == "AlembicRecorderSettings")
                return RecorderSettingsType.Alembic;
            else if (typeName == "FbxRecorderSettings")
                return RecorderSettingsType.FBX;
            else if (settings is MovieRecorderSettings)
                return RecorderSettingsType.Movie;
            else if (settings is ImageRecorderSettings)
            {
                // Check if it's a fallback AOV implementation
                if (settings.name.Contains("AOV"))
                    return RecorderSettingsType.AOV;
                return RecorderSettingsType.Image;
            }
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