using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;

namespace BatchRenderingTool
{
    // SingleTimelineRendererクラスのpartial実装
    // 各種RecorderSettings作成メソッドを含む
    public partial class SingleTimelineRenderer
    {
        // ========== Single Recorder Mode Methods ==========
        
        private RecorderSettings CreateImageRecorderSettings(string outputPath, string outputFileName)
        {
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = "ImageRecorderSettings";
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.OutputFormat = imageOutputFormat;
            settings.CaptureAlpha = imageCaptureAlpha;
            
            // Image format specific settings
            if (imageOutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                settings.JpegQuality = jpegQuality;
            }
            else if (imageOutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                settings.EXRCompression = exrCompression;
            }
            
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
            
            // Configure output path
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Image);
            
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
            };
            
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettings(string outputPath, string outputFileName)
        {
            MovieRecorderSettings settings = null;
            
            if (useMoviePreset && moviePreset != MovieRecorderPreset.Custom)
            {
                // Create with preset
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", moviePreset);
            }
            else
            {
                // Create with custom settings
                var config = new MovieRecorderSettingsConfig
                {
                    outputFormat = movieOutputFormat,
                    videoBitrateMode = movieQuality,
                    captureAudio = movieCaptureAudio,
                    captureAlpha = movieCaptureAlpha,
                    width = width,
                    height = height,
                    frameRate = frameRate,
                    capFrameRate = true
                };
                
                string errorMessage;
                if (!config.Validate(out errorMessage))
                {
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid movie configuration: {errorMessage}");
                    return null;
                }
                
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", config);
            }
            
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            
            // Configure output path
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Movie);
            
            return settings;
        }
        
        private List<RecorderSettings> CreateAOVRecorderSettings(string outputPath, string outputFileName)
        {
            AOVRecorderSettingsConfig config = null;
            
            if (useAOVPreset && aovPreset != AOVPreset.Custom)
            {
                // Use preset configuration
                switch (aovPreset)
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
                }
            }
            else
            {
                // Create custom configuration
                config = new AOVRecorderSettingsConfig
                {
                    selectedAOVs = selectedAOVTypes,
                    outputFormat = aovOutputFormat,
                    width = width,
                    height = height,
                    frameRate = frameRate,
                    capFrameRate = true
                };
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid AOV configuration: {errorMessage}");
                return null;
            }
            
            var settingsList = RecorderSettingsFactory.CreateAOVRecorderSettings("AOVRecorder", config);
            
            // Configure output path for each AOV setting
            foreach (var settings in settingsList)
            {
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.AOV);
            }
            
            return settingsList;
        }
        
        private RecorderSettings CreateAlembicRecorderSettings(string outputPath, string outputFileName)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateAlembicRecorderSettings called with path: {outputPath}, fileName: {outputFileName} ===");
            
            AlembicRecorderSettingsConfig config = null;
            
            if (useAlembicPreset && alembicPreset != AlembicExportPreset.Custom)
            {
                config = AlembicRecorderSettingsConfig.GetPreset(alembicPreset);
            }
            else
            {
                // Create custom configuration
                config = new AlembicRecorderSettingsConfig
                {
                    exportTargets = alembicExportTargets,
                    exportScope = alembicExportScope,
                    targetGameObject = alembicTargetGameObject,
                    handedness = alembicHandedness,
                    scaleFactor = alembicScaleFactor,
                    frameRate = frameRate,
                    samplesPerFrame = 1, // Default to 1 sample per frame
                    exportUVs = true,
                    exportNormals = true
                };
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Alembic config: scope={alembicExportScope}, targetGameObject={alembicTargetGameObject?.name ?? "null"} ===");
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid Alembic configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings("AlembicRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Alembic);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAnimationRecorderSettings(string outputPath, string outputFileName)
        {
            AnimationRecorderSettingsConfig config = null;
            
            if (useAnimationPreset && animationPreset != AnimationExportPreset.Custom)
            {
                config = AnimationRecorderSettingsConfig.GetPreset(animationPreset);
                // Override target GameObject if needed
                if ((animationRecordingScope == AnimationRecordingScope.SingleGameObject ||
                     animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren) &&
                    animationTargetGameObject != null)
                {
                    config.targetGameObject = animationTargetGameObject;
                }
            }
            else
            {
                // Create custom configuration
                config = new AnimationRecorderSettingsConfig
                {
                    recordingProperties = animationRecordingProperties,
                    recordingScope = animationRecordingScope,
                    targetGameObject = animationTargetGameObject,
                    interpolationMode = animationInterpolationMode,
                    compressionLevel = animationCompressionLevel,
                    frameRate = frameRate,
                    recordInWorldSpace = false,
                    treatAsHumanoid = false,
                    optimizeGameObjects = true
                };
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid Animation configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings("AnimationRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Animation);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateFBXRecorderSettings(string outputPath, string outputFileName)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateFBXRecorderSettings called with path: {outputPath}, fileName: {outputFileName} ===");
            
            // FBXレコーダーにはターゲットGameObjectが必要
            if (fbxTargetGameObject == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] FBX Recorder requires a target GameObject to be set.");
                return null;
            }
            
            FBXRecorderSettingsConfig config = null;
            
            if (useFBXPreset && fbxPreset != FBXExportPreset.Custom)
            {
                config = FBXRecorderSettingsConfig.GetPreset(fbxPreset);
                // Presetを使用する場合もtargetGameObjectを設定
                config.targetGameObject = fbxTargetGameObject;
            }
            else
            {
                // Create custom configuration
                config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = fbxTargetGameObject,
                    recordedComponent = fbxRecordedComponent,
                    recordHierarchy = fbxRecordHierarchy,
                    clampedTangents = fbxClampedTangents,
                    animationCompression = fbxAnimationCompression,
                    exportGeometry = fbxExportGeometry,
                    transferAnimationSource = fbxTransferAnimationSource,
                    transferAnimationDest = fbxTransferAnimationDest,
                    frameRate = frameRate
                };
                
                // Safely log FBX configuration
                string sourceStr = "null";
                string destStr = "null";
                
                try 
                {
                    if (fbxTransferAnimationSource != null)
                        sourceStr = fbxTransferAnimationSource.name;
                } 
                catch (Exception) 
                {
                    sourceStr = "null (invalid reference)";
                }
                
                try 
                {
                    if (fbxTransferAnimationDest != null)
                        destStr = fbxTransferAnimationDest.name;
                } 
                catch (Exception) 
                {
                    destStr = "null (invalid reference)";
                }
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === FBX config: exportGeometry={fbxExportGeometry}, transferSource={sourceStr}, transferDest={destStr} ===");
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid FBX configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("FBXRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.FBX);
            }
            
            return settings;
        }
        
        // ========== Multi Recorder Mode Methods ==========
        
        private RecorderSettings CreateImageRecorderSettingsFromConfig(string outputPath, string outputFileName, MultiRecorderConfig.RecorderConfigItem config)
        {
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = "ImageRecorderSettings";
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.OutputFormat = config.imageFormat;
            settings.CaptureAlpha = config.captureAlpha;
            
            if (config.imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                settings.JpegQuality = config.jpegQuality;
            }
            else if (config.imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                settings.EXRCompression = config.exrCompression;
            }
            
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
            
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Image);
            
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = config.width,
                OutputHeight = config.height
            };
            
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettingsFromConfig(string outputPath, string outputFileName, MultiRecorderConfig.RecorderConfigItem config)
        {
            var settingsConfig = config.movieConfig;
            settingsConfig.width = config.width;
            settingsConfig.height = config.height;
            settingsConfig.frameRate = frameRate;
            settingsConfig.capFrameRate = true;
            
            string errorMessage;
            if (!settingsConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid movie configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", settingsConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Movie);
            }
            
            return settings;
        }
        
        private List<RecorderSettings> CreateAOVRecorderSettingsFromConfig(string outputPath, string outputFileName, MultiRecorderConfig.RecorderConfigItem config)
        {
            var settingsConfig = config.aovConfig;
            settingsConfig.width = config.width;
            settingsConfig.height = config.height;
            settingsConfig.frameRate = frameRate;
            settingsConfig.capFrameRate = true;
            
            string errorMessage;
            if (!settingsConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid AOV configuration: {errorMessage}");
                return null;
            }
            
            var settingsList = RecorderSettingsFactory.CreateAOVRecorderSettings("AOVRecorder", settingsConfig);
            
            foreach (var settings in settingsList)
            {
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.AOV);
            }
            
            return settingsList;
        }
        
        private RecorderSettings CreateAnimationRecorderSettingsFromConfig(string outputPath, string outputFileName, MultiRecorderConfig.RecorderConfigItem config)
        {
            var settingsConfig = config.animationConfig;
            settingsConfig.frameRate = frameRate;
            
            string errorMessage;
            if (!settingsConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid Animation configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings("AnimationRecorder", settingsConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Animation);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateFBXRecorderSettingsFromConfig(string outputPath, string outputFileName, MultiRecorderConfig.RecorderConfigItem config)
        {
            // FBX configがnullの場合の処理
            if (config.fbxConfig == null)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] FBX Recorder config is null for recorder '{config.name}'.");
                return null;
            }
            
            if (config.fbxConfig.targetGameObject == null)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] FBX Recorder requires a target GameObject to be set for recorder '{config.name}'.");
                return null;
            }
            
            var settingsConfig = config.fbxConfig;
            settingsConfig.frameRate = frameRate;
            
            string errorMessage;
            if (!settingsConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid FBX configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("FBXRecorder", settingsConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.FBX);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAlembicRecorderSettingsFromConfig(string outputPath, string outputFileName, MultiRecorderConfig.RecorderConfigItem config)
        {
            var settingsConfig = config.alembicConfig;
            settingsConfig.frameRate = frameRate;
            settingsConfig.samplesPerFrame = 1;
            settingsConfig.exportUVs = true;
            settingsConfig.exportNormals = true;
            
            string errorMessage;
            if (!settingsConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid Alembic configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings("AlembicRecorder", settingsConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Alembic);
            }
            
            return settings;
        }
        
        // ========== Per-Timeline Recorder Configuration Methods ==========
        
        /// <summary>
        /// Creates RecorderSettings for a specific timeline and recorder item
        /// </summary>
        private RecorderSettings CreateRecorderSettingsForItem(MultiRecorderConfig.RecorderConfigItem recorderItem, PlayableDirector director, int timelineIndex)
        {
            // Get the timeline-specific config to check for global resolution settings
            var timelineConfig = GetTimelineRecorderConfig(timelineIndex);
            
            var context = new WildcardContext(recorderItem.takeNumber,
                timelineConfig.useGlobalResolution ? timelineConfig.globalWidth : recorderItem.width,
                timelineConfig.useGlobalResolution ? timelineConfig.globalHeight : recorderItem.height);
            context.TimelineName = director.gameObject.name;
            context.RecorderName = recorderItem.recorderType.ToString();
            
            // Set GameObject name based on recorder type
            if (recorderItem.recorderType == RecorderSettingsType.Alembic && recorderItem.alembicConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.alembicConfig.targetGameObject.name;
            }
            else if (recorderItem.recorderType == RecorderSettingsType.Animation && recorderItem.animationConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.animationConfig.targetGameObject.name;
            }
            else if (recorderItem.recorderType == RecorderSettingsType.FBX && recorderItem.fbxConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.fbxConfig.targetGameObject.name;
            }
            
            var processedFileName = WildcardProcessor.ProcessWildcards(recorderItem.fileName, context);
            var processedFilePath = timelineConfig.globalOutputPath;
            
            // Create recorder settings based on type
            RecorderSettings recorderSettings = null;
            
            switch (recorderItem.recorderType)
            {
                case RecorderSettingsType.Image:
                    recorderSettings = CreateImageRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    break;
                    
                case RecorderSettingsType.Movie:
                    recorderSettings = CreateMovieRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    break;
                    
                case RecorderSettingsType.AOV:
                    var aovSettingsList = CreateAOVRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (aovSettingsList != null && aovSettingsList.Count > 0)
                    {
                        recorderSettings = aovSettingsList[0];
                        // Additional AOV settings need to be handled separately
                        BatchRenderingToolLogger.LogWarning($"[SingleTimelineRenderer] Multiple AOV outputs detected, only using first one for timeline {director.gameObject.name}");
                    }
                    break;
                    
                case RecorderSettingsType.Animation:
                    recorderSettings = CreateAnimationRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    break;
                    
                case RecorderSettingsType.FBX:
                    recorderSettings = CreateFBXRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    recorderSettings = CreateAlembicRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    break;
                    
                default:
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Unsupported recorder type: {recorderItem.recorderType}");
                    break;
            }
            
            return recorderSettings;
        }
    }
}