using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Repairs frame rate inconsistencies
    /// </summary>
    public class FrameRateRepairStrategy : IAutoRepairStrategy
    {
        private readonly ILogger _logger;

        public FrameRateRepairStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public bool CanRepair(ValidationIssue issue)
        {
            return issue.Message.ToLower().Contains("frame rate");
        }

        public bool Repair(IRecordingConfiguration configuration, ValidationIssue issue)
        {
            try
            {
                var globalFrameRate = configuration.FrameRate;
                
                // Apply global frame rate to all recorders that support it
                foreach (var timelineConfig in configuration.TimelineConfigs)
                {
                    var recorderConfig = timelineConfig as TimelineRecorderConfig;
                    if (recorderConfig?.RecorderConfigurations != null)
                    {
                        foreach (var recorder in recorderConfig.RecorderConfigurations)
                        {
                            if (recorder is AnimationRecorderConfiguration animConfig)
                            {
                                animConfig.FrameRate = globalFrameRate;
                                _logger.LogInfo($"Set animation recorder frame rate to {globalFrameRate}", LogCategory.Configuration);
                            }
                            // Add other recorder types that have frame rate settings
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to repair frame rate: {ex.Message}", LogCategory.Configuration);
                return false;
            }
        }
    }

    /// <summary>
    /// Repairs resolution issues
    /// </summary>
    public class ResolutionRepairStrategy : IAutoRepairStrategy
    {
        private readonly ILogger _logger;
        private readonly Resolution[] _standardResolutions = new[]
        {
            new Resolution(1920, 1080), // 1080p
            new Resolution(1280, 720),  // 720p
            new Resolution(3840, 2160), // 4K
            new Resolution(2560, 1440), // 1440p
        };

        public ResolutionRepairStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public bool CanRepair(ValidationIssue issue)
        {
            return issue.Message.ToLower().Contains("resolution");
        }

        public bool Repair(IRecordingConfiguration configuration, ValidationIssue issue)
        {
            try
            {
                var currentResolution = configuration.Resolution;
                
                // Check if resolution is invalid
                if (currentResolution.Width <= 0 || currentResolution.Height <= 0)
                {
                    // Set to default 1080p
                    configuration.Resolution = _standardResolutions[0];
                    _logger.LogInfo("Set resolution to default 1920x1080", LogCategory.Configuration);
                    return true;
                }
                
                // Check if resolution is too high
                if (currentResolution.Width > 8192 || currentResolution.Height > 8192)
                {
                    // Find the closest standard resolution
                    var closestResolution = FindClosestStandardResolution(currentResolution);
                    configuration.Resolution = closestResolution;
                    _logger.LogInfo($"Reduced resolution to {closestResolution}", LogCategory.Configuration);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to repair resolution: {ex.Message}", LogCategory.Configuration);
                return false;
            }
        }

        private Resolution FindClosestStandardResolution(Resolution current)
        {
            var currentPixels = current.Width * current.Height;
            return _standardResolutions
                .OrderBy(r => Math.Abs(r.Width * r.Height - currentPixels))
                .First();
        }
    }

    /// <summary>
    /// Repairs output path issues
    /// </summary>
    public class OutputPathRepairStrategy : IAutoRepairStrategy
    {
        private readonly ILogger _logger;

        public OutputPathRepairStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public bool CanRepair(ValidationIssue issue)
        {
            return issue.Message.ToLower().Contains("output path") || 
                   issue.Message.ToLower().Contains("directory");
        }

        public bool Repair(IRecordingConfiguration configuration, ValidationIssue issue)
        {
            try
            {
                var outputPath = configuration.OutputPath;
                
                // Check if path is empty
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    configuration.OutputPath = "Recordings";
                    _logger.LogInfo("Set output path to default 'Recordings'", LogCategory.Configuration);
                    return true;
                }
                
                // Convert absolute path to relative if needed
                if (Path.IsPathRooted(outputPath))
                {
                    // Try to make it relative to project
                    var projectPath = Application.dataPath;
                    if (outputPath.StartsWith(projectPath))
                    {
                        var relativePath = outputPath.Substring(projectPath.Length).TrimStart('/', '\\');
                        configuration.OutputPath = Path.Combine("Assets", relativePath);
                        _logger.LogInfo($"Converted to relative path: {configuration.OutputPath}", LogCategory.Configuration);
                        return true;
                    }
                }
                
                // Ensure directory exists
                var fullPath = Path.Combine(Application.dataPath, outputPath);
                if (!Directory.Exists(fullPath))
                {
                    try
                    {
                        Directory.CreateDirectory(fullPath);
                        _logger.LogInfo($"Created output directory: {fullPath}", LogCategory.Configuration);
                        return true;
                    }
                    catch
                    {
                        // If we can't create it, use default
                        configuration.OutputPath = "Recordings";
                        _logger.LogWarning("Could not create output directory, using default", LogCategory.Configuration);
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to repair output path: {ex.Message}", LogCategory.Configuration);
                return false;
            }
        }
    }

    /// <summary>
    /// Repairs GameObject reference issues
    /// </summary>
    public class GameObjectReferenceRepairStrategy : IAutoRepairStrategy
    {
        private readonly ILogger _logger;
        private readonly IGameObjectReferenceService _referenceService;

        public GameObjectReferenceRepairStrategy(ILogger logger, IGameObjectReferenceService referenceService)
        {
            _logger = logger;
            _referenceService = referenceService;
        }

        public bool CanRepair(ValidationIssue issue)
        {
            return issue.Message.ToLower().Contains("gameobject") || 
                   issue.Message.ToLower().Contains("target") ||
                   issue.Message.ToLower().Contains("reference");
        }

        public bool Repair(IRecordingConfiguration configuration, ValidationIssue issue)
        {
            try
            {
                var repaired = false;
                
                foreach (var timelineConfig in configuration.TimelineConfigs)
                {
                    var recorderConfig = timelineConfig as TimelineRecorderConfig;
                    if (recorderConfig?.RecorderConfigurations != null)
                    {
                        foreach (var recorder in recorderConfig.RecorderConfigurations)
                        {
                            if (RepairRecorderReferences(recorder))
                            {
                                repaired = true;
                            }
                        }
                    }
                }
                
                return repaired;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to repair GameObject references: {ex.Message}", LogCategory.Configuration);
                return false;
            }
        }

        private bool RepairRecorderReferences(IRecorderConfiguration recorder)
        {
            switch (recorder)
            {
                case AnimationRecorderConfiguration animConfig:
                    return RepairAnimationReferences(animConfig);
                    
                case AlembicRecorderConfiguration alembicConfig:
                    return RepairAlembicReferences(alembicConfig);
                    
                case FBXRecorderConfiguration fbxConfig:
                    return RepairFBXReferences(fbxConfig);
                    
                default:
                    return false;
            }
        }

        private bool RepairAnimationReferences(AnimationRecorderConfiguration config)
        {
            if (config.TargetGameObject != null)
                return false; // Already valid
            
            // Try to find a suitable GameObject
            var potentialTargets = GameObject.FindObjectsOfType<Animator>();
            if (potentialTargets.Length > 0)
            {
                config.TargetGameObject = potentialTargets[0].gameObject;
                _logger.LogInfo($"Auto-assigned animation target: {config.TargetGameObject.name}", LogCategory.Configuration);
                
                // Create reference for persistence
                _referenceService.CreateReference(config.TargetGameObject);
                return true;
            }
            
            // Try to find any GameObject with animation-worthy components
            var skinnedMeshes = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();
            if (skinnedMeshes.Length > 0)
            {
                config.TargetGameObject = skinnedMeshes[0].gameObject;
                _logger.LogInfo($"Auto-assigned animation target with skinned mesh: {config.TargetGameObject.name}", LogCategory.Configuration);
                
                _referenceService.CreateReference(config.TargetGameObject);
                return true;
            }
            
            return false;
        }

        private bool RepairAlembicReferences(AlembicRecorderConfiguration config)
        {
            if (config.TargetGameObject != null)
                return false; // Already valid
            
            // Try to find root GameObject with meshes
            var meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
            if (meshRenderers.Length > 0)
            {
                // Find the topmost parent
                var topParent = FindTopmostParent(meshRenderers[0].transform);
                config.TargetGameObject = topParent.gameObject;
                _logger.LogInfo($"Auto-assigned Alembic target: {config.TargetGameObject.name}", LogCategory.Configuration);
                
                _referenceService.CreateReference(config.TargetGameObject);
                return true;
            }
            
            return false;
        }

        private bool RepairFBXReferences(FBXRecorderConfiguration config)
        {
            if (config.TargetGameObject != null)
                return false; // Already valid
            
            // Similar to Alembic, find suitable geometry
            var meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
            if (meshFilters.Length > 0)
            {
                var topParent = FindTopmostParent(meshFilters[0].transform);
                config.TargetGameObject = topParent.gameObject;
                _logger.LogInfo($"Auto-assigned FBX target: {config.TargetGameObject.name}", LogCategory.Configuration);
                
                _referenceService.CreateReference(config.TargetGameObject);
                return true;
            }
            
            return false;
        }

        private Transform FindTopmostParent(Transform transform)
        {
            var current = transform;
            while (current.parent != null)
            {
                current = current.parent;
            }
            return current;
        }
    }

    /// <summary>
    /// Repairs filename pattern issues
    /// </summary>
    public class FilenamePatternRepairStrategy : IAutoRepairStrategy
    {
        private readonly ILogger _logger;
        private readonly string[] _defaultPatterns = new[]
        {
            "<Scene>_<Timeline>_<RecorderType>_<Take>",
            "<Scene>_<Timeline>_<Take>",
            "<Timeline>_<RecorderType>_<Take>"
        };

        public FilenamePatternRepairStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public bool CanRepair(ValidationIssue issue)
        {
            return issue.Message.ToLower().Contains("filename") || 
                   issue.Message.ToLower().Contains("pattern");
        }

        public bool Repair(IRecordingConfiguration configuration, ValidationIssue issue)
        {
            try
            {
                var repaired = false;
                
                foreach (var timelineConfig in configuration.TimelineConfigs)
                {
                    var recorderConfig = timelineConfig as TimelineRecorderConfig;
                    if (recorderConfig?.RecorderConfigurations != null)
                    {
                        foreach (var recorder in recorderConfig.RecorderConfigurations)
                        {
                            if (RepairFilenamePattern(recorder))
                            {
                                repaired = true;
                            }
                        }
                    }
                }
                
                return repaired;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to repair filename patterns: {ex.Message}", LogCategory.Configuration);
                return false;
            }
        }

        private bool RepairFilenamePattern(IRecorderConfiguration recorder)
        {
            string currentPattern = null;
            string propertyName = null;
            
            // Get current pattern based on recorder type
            switch (recorder)
            {
                case ImageRecorderConfiguration imageConfig:
                    currentPattern = imageConfig.FileNamePattern;
                    propertyName = "FileNamePattern";
                    break;
                    
                case AnimationRecorderConfiguration animConfig:
                    currentPattern = animConfig.FileName;
                    propertyName = "FileName";
                    break;
                    
                case AlembicRecorderConfiguration alembicConfig:
                    currentPattern = alembicConfig.FileName;
                    propertyName = "FileName";
                    break;
                    
                case FBXRecorderConfiguration fbxConfig:
                    currentPattern = fbxConfig.FileName;
                    propertyName = "FileName";
                    break;
                    
                case AOVRecorderConfiguration aovConfig:
                    currentPattern = aovConfig.FileName;
                    propertyName = "FileName";
                    break;
            }
            
            if (string.IsNullOrWhiteSpace(currentPattern))
            {
                // Set default pattern based on recorder type
                var defaultPattern = GetDefaultPatternForRecorder(recorder);
                SetFilenamePattern(recorder, defaultPattern);
                
                _logger.LogInfo($"Set default filename pattern: {defaultPattern}", LogCategory.Configuration);
                return true;
            }
            
            // Check for invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var patternWithoutWildcards = System.Text.RegularExpressions.Regex.Replace(currentPattern, @"<[^>]+>", "");
            
            if (patternWithoutWildcards.Any(c => invalidChars.Contains(c)))
            {
                // Remove invalid characters
                foreach (var c in invalidChars)
                {
                    currentPattern = currentPattern.Replace(c.ToString(), "_");
                }
                
                SetFilenamePattern(recorder, currentPattern);
                _logger.LogInfo($"Removed invalid characters from filename pattern", LogCategory.Configuration);
                return true;
            }
            
            return false;
        }

        private string GetDefaultPatternForRecorder(IRecorderConfiguration recorder)
        {
            return recorder.Type switch
            {
                RecorderSettingsType.Animation => "<Scene>_<Timeline>_Animation_<Take>",
                RecorderSettingsType.Alembic => "<Scene>_<Timeline>_Alembic_<Take>",
                RecorderSettingsType.FBX => "<Scene>_<Timeline>_FBX_<Take>",
                RecorderSettingsType.AOV => "<Scene>_<Timeline>_<AOVType>_<Take>",
                _ => _defaultPatterns[0]
            };
        }

        private void SetFilenamePattern(IRecorderConfiguration recorder, string pattern)
        {
            switch (recorder)
            {
                case ImageRecorderConfiguration imageConfig:
                    imageConfig.FileNamePattern = pattern;
                    break;
                    
                case AnimationRecorderConfiguration animConfig:
                    animConfig.FileName = pattern;
                    break;
                    
                case AlembicRecorderConfiguration alembicConfig:
                    alembicConfig.FileName = pattern;
                    break;
                    
                case FBXRecorderConfiguration fbxConfig:
                    fbxConfig.FileName = pattern;
                    break;
                    
                case AOVRecorderConfiguration aovConfig:
                    aovConfig.FileName = pattern;
                    break;
            }
        }
    }
}