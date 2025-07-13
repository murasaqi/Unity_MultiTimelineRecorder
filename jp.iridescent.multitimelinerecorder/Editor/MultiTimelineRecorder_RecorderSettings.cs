using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;

namespace Unity.MultiTimelineRecorder
{
    // MultiTimelineRecorderクラスのpartial実装
    // 各種RecorderSettings作成メソッドを含む
    public partial class MultiTimelineRecorder
    {
        // ========== Single Recorder Mode Methods ==========
        
        private RecorderSettings CreateImageRecorderSettings(string outputPath, string outputFileName)
        {
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = "ImageRecorderSettings";
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
            settings.CaptureAlpha = false;
            
            // Image format specific settings
            if (settings.OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                settings.JpegQuality = 75;
            }
            else if (settings.OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                settings.EXRCompression = CompressionUtility.EXRCompressionType.None;
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
            
            // Always use default preset for multi-timeline mode
            settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", MovieRecorderPreset.HighQuality1080p);
            
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            
            // Configure output path
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Movie);
            
            return settings;
        }
        
        private List<RecorderSettings> CreateAOVRecorderSettings(string outputPath, string outputFileName)
        {
            // Use default AOV configuration for multi-timeline mode
            var config = AOVRecorderSettingsConfig.Presets.GetCompositing();
            config.width = width;
            config.height = height;
            config.frameRate = frameRate;
            config.capFrameRate = true;
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid AOV configuration: {errorMessage}");
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
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] === CreateAlembicRecorderSettings called with path: {outputPath}, fileName: {outputFileName} ===");
            
            // Use default configuration for multi-timeline mode
            var config = AlembicRecorderSettingsConfig.GetPreset(AlembicExportPreset.AnimationExport);
            config.frameRate = frameRate;
            config.samplesPerFrame = 1;
            config.exportUVs = true;
            config.exportNormals = true;
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid Alembic configuration: {errorMessage}");
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
            // Use default configuration for multi-timeline mode
            var config = AnimationRecorderSettingsConfig.GetPreset(AnimationExportPreset.SimpleTransform);
            config.frameRate = frameRate;
            config.recordInWorldSpace = false;
            config.treatAsHumanoid = false;
            config.optimizeGameObjects = true;
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid Animation configuration: {errorMessage}");
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
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] === CreateFBXRecorderSettings called with path: {outputPath}, fileName: {outputFileName} ===");
            
            // FBX recorder is not supported in single recorder mode for multi-timeline
            MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] FBX Recorder is not supported in single recorder mode. Use per-timeline configuration.");
            return null;
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
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid movie configuration: {errorMessage}");
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
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid AOV configuration: {errorMessage}");
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
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid Animation configuration: {errorMessage}");
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
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] FBX Recorder config is null for recorder '{config.name}'.");
                return null;
            }
            
            if (config.fbxConfig.targetGameObject == null)
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] FBX Recorder requires a target GameObject to be set for recorder '{config.name}'.");
                return null;
            }
            
            var settingsConfig = config.fbxConfig;
            settingsConfig.frameRate = frameRate;
            
            string errorMessage;
            if (!settingsConfig.Validate(out errorMessage))
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid FBX configuration: {errorMessage}");
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
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Invalid Alembic configuration: {errorMessage}");
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
            try
            {
                MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Creating RecorderSettings for {recorderItem.name} on timeline {director.gameObject.name}");
                
                // Get the timeline-specific config to check for global resolution settings
                var timelineConfig = GetTimelineRecorderConfig(timelineIndex);
                
                // Determine which take number to use based on take mode
                int effectiveTakeNumber = recorderItem.takeNumber;
                if (recorderItem.takeMode == RecorderTakeMode.RecordersTake && settings != null)
                {
                    // Find the index of this director in availableDirectors
                    int directorIndex = availableDirectors.IndexOf(director);
                    if (directorIndex >= 0)
                    {
                        effectiveTakeNumber = settings.GetTimelineTakeNumber(directorIndex);
                    }
                }
                
                var context = new WildcardContext(effectiveTakeNumber,
                    timelineConfig.useGlobalResolution ? width : recorderItem.width,
                    timelineConfig.useGlobalResolution ? height : recorderItem.height);
                context.TimelineName = director.gameObject.name;
                context.RecorderName = recorderItem.recorderType.ToString();
                
                // Always set TimelineTakeNumber for <TimelineTake> wildcard
                if (settings != null)
                {
                    // Find the index of this director in availableDirectors
                    int directorIndex = availableDirectors.IndexOf(director);
                    if (directorIndex >= 0)
                    {
                        context.TimelineTakeNumber = settings.GetTimelineTakeNumber(directorIndex);
                    }
                }
                
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
                
                // Determine output path based on recorder's path mode
                string processedFilePath;
                switch (recorderItem.outputPath.pathMode)
                {
                    case RecorderPathMode.UseGlobal:
                        processedFilePath = globalOutputPath.GetResolvedPath(context);
                        break;
                        
                    case RecorderPathMode.RelativeToGlobal:
                        string globalPath = globalOutputPath.GetResolvedPath(context);
                        string relativePath = WildcardProcessor.ProcessWildcards(recorderItem.outputPath.customPath, context);
                        processedFilePath = System.IO.Path.Combine(globalPath, relativePath);
                        break;
                        
                    case RecorderPathMode.Custom:
                        processedFilePath = recorderItem.outputPath.GetResolvedPath(context);
                        break;
                        
                    default:
                        processedFilePath = globalOutputPath.GetResolvedPath(context);
                        break;
                }
                
                MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Output path: {processedFilePath}, Filename: {processedFileName}");
                
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
                        // Only use the first AOV setting to avoid complications with multiple outputs
                        recorderSettings = aovSettingsList[0];
                        
                        if (aovSettingsList.Count > 1)
                        {
                            MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder] Multiple AOV outputs detected ({aovSettingsList.Count}), only using the first one for timeline {director.gameObject.name}");
                            MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder] Consider selecting only one AOV type to avoid this limitation");
                        }
                        
                        MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Using AOV recorder settings: {recorderSettings.name}");
                    }
                    else
                    {
                        MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Failed to create AOV recorder settings for timeline {director.gameObject.name}");
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
                    MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Unsupported recorder type: {recorderItem.recorderType}");
                    break;
            }
            
            return recorderSettings;
            }
            catch (System.Exception e)
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Exception creating RecorderSettings for {recorderItem.name}: {e.Message}");
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Stack trace: {e.StackTrace}");
                return null;
            }
        }
    }
}