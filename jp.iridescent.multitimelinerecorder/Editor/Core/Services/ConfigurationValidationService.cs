using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service for validating and auto-repairing recorder configurations
    /// </summary>
    public class ConfigurationValidationService : IConfigurationValidationService
    {
        private readonly ILogger _logger;
        private readonly IGameObjectReferenceService _referenceService;
        private readonly Dictionary<Type, IConfigurationValidator> _validators;
        private readonly List<IAutoRepairStrategy> _repairStrategies;

        public ConfigurationValidationService(ILogger logger, IGameObjectReferenceService referenceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _referenceService = referenceService ?? throw new ArgumentNullException(nameof(referenceService));
            
            _validators = new Dictionary<Type, IConfigurationValidator>();
            _repairStrategies = new List<IAutoRepairStrategy>();
            
            InitializeValidators();
            InitializeRepairStrategies();
        }

        /// <summary>
        /// Validates a recording configuration with detailed checks
        /// </summary>
        public ValidationResult ValidateConfiguration(IRecordingConfiguration configuration)
        {
            var result = new ValidationResult();

            if (configuration == null)
            {
                result.AddError("Configuration is null");
                return result;
            }

            // Basic validation
            var basicResult = configuration.Validate();
            result.Merge(basicResult);

            // Enhanced validation
            result.Merge(ValidateFrameRateConsistency(configuration));
            result.Merge(ValidateResourceUsage(configuration));
            result.Merge(ValidateRecorderCompatibility(configuration));
            result.Merge(ValidateUnityVersion(configuration));

            return result;
        }

        /// <summary>
        /// Validates a specific recorder configuration
        /// </summary>
        public ValidationResult ValidateRecorderConfiguration(IRecorderConfiguration recorderConfig)
        {
            var result = new ValidationResult();

            if (recorderConfig == null)
            {
                result.AddError("Recorder configuration is null");
                return result;
            }

            // Basic validation
            result.Merge(recorderConfig.Validate());

            // Type-specific validation
            var configType = recorderConfig.GetType();
            if (_validators.TryGetValue(configType, out var validator))
            {
                result.Merge(validator.Validate(recorderConfig));
            }

            return result;
        }

        /// <summary>
        /// Attempts to auto-repair configuration issues
        /// </summary>
        public RepairResult AutoRepairConfiguration(IRecordingConfiguration configuration)
        {
            var repairResult = new RepairResult();
            
            if (configuration == null)
            {
                repairResult.Success = false;
                repairResult.Message = "Configuration is null";
                return repairResult;
            }

            var validationResult = ValidateConfiguration(configuration);
            if (validationResult.IsValid)
            {
                repairResult.Success = true;
                repairResult.Message = "Configuration is already valid";
                return repairResult;
            }

            // Apply repair strategies
            foreach (var issue in validationResult.Issues)
            {
                var repaired = false;
                
                foreach (var strategy in _repairStrategies)
                {
                    if (strategy.CanRepair(issue))
                    {
                        try
                        {
                            repaired = strategy.Repair(configuration, issue);
                            if (repaired)
                            {
                                repairResult.RepairedIssues.Add(issue);
                                _logger.LogInfo($"Repaired issue: {issue.Message}", LogCategory.Configuration);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to repair issue: {ex.Message}", LogCategory.Configuration);
                        }
                    }
                }

                if (!repaired)
                {
                    repairResult.UnrepairedIssues.Add(issue);
                }
            }

            repairResult.Success = repairResult.UnrepairedIssues.Count == 0;
            repairResult.Message = repairResult.Success 
                ? "All issues repaired successfully" 
                : $"{repairResult.UnrepairedIssues.Count} issues could not be repaired";

            return repairResult;
        }

        /// <summary>
        /// Gets repair suggestions for validation issues
        /// </summary>
        public List<RepairSuggestion> GetRepairSuggestions(ValidationResult validationResult)
        {
            var suggestions = new List<RepairSuggestion>();

            foreach (var issue in validationResult.Issues)
            {
                var suggestion = GetSuggestionForIssue(issue);
                if (suggestion != null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Validates Unity version compatibility
        /// </summary>
        public ValidationResult ValidateUnityVersion(IRecordingConfiguration configuration)
        {
            var result = new ValidationResult();
            var currentVersion = Application.unityVersion;

            // Check minimum Unity version
            if (!IsUnityVersionSupported(currentVersion))
            {
                result.AddWarning($"Unity version {currentVersion} may have compatibility issues. Recommended: 2021.3 or later");
            }

            // Check recorder package version
            var recorderPackageVersion = GetRecorderPackageVersion();
            if (!string.IsNullOrEmpty(recorderPackageVersion))
            {
                if (!IsRecorderPackageVersionSupported(recorderPackageVersion))
                {
                    result.AddWarning($"Unity Recorder package version {recorderPackageVersion} may have compatibility issues");
                }
            }

            return result;
        }

        /// <summary>
        /// Predicts resource usage for a configuration
        /// </summary>
        public ResourceUsagePrediction PredictResourceUsage(IRecordingConfiguration configuration)
        {
            var prediction = new ResourceUsagePrediction();

            if (configuration == null)
                return prediction;

            // Calculate based on resolution and frame rate
            var resolution = configuration.Resolution;
            var frameRate = configuration.FrameRate;
            var timelineCount = configuration.TimelineConfigs?.Count ?? 0;

            // Memory usage prediction (MB)
            prediction.EstimatedMemoryUsageMB = CalculateMemoryUsage(resolution, frameRate, timelineCount);

            // Disk space prediction (MB per minute)
            prediction.EstimatedDiskUsageMBPerMinute = CalculateDiskUsage(configuration);

            // CPU usage prediction (percentage)
            prediction.EstimatedCPUUsage = CalculateCPUUsage(resolution, frameRate, timelineCount);

            // Performance impact
            if (prediction.EstimatedMemoryUsageMB > 4096)
            {
                prediction.PerformanceImpact = PerformanceImpact.High;
                prediction.Warnings.Add("High memory usage predicted. Consider reducing resolution or timeline count.");
            }
            else if (prediction.EstimatedMemoryUsageMB > 2048)
            {
                prediction.PerformanceImpact = PerformanceImpact.Medium;
            }
            else
            {
                prediction.PerformanceImpact = PerformanceImpact.Low;
            }

            return prediction;
        }

        // Private validation methods

        private ValidationResult ValidateFrameRateConsistency(IRecordingConfiguration configuration)
        {
            var result = new ValidationResult();
            var globalFrameRate = configuration.FrameRate;

            foreach (var timelineConfig in configuration.TimelineConfigs)
            {
                // Check if all recorders use consistent frame rate
                var timelineRecorderConfig = timelineConfig as TimelineRecorderConfig;
                if (timelineRecorderConfig?.RecorderConfigurations != null)
                {
                    foreach (var recorderConfig in timelineRecorderConfig.RecorderConfigurations)
                    {
                        if (recorderConfig is AnimationRecorderConfiguration animConfig)
                        {
                            if (animConfig.FrameRate != globalFrameRate)
                            {
                                result.AddWarning($"Animation recorder frame rate ({animConfig.FrameRate}) differs from global frame rate ({globalFrameRate})");
                            }
                        }
                    }
                }
            }

            return result;
        }

        private ValidationResult ValidateResourceUsage(IRecordingConfiguration configuration)
        {
            var result = new ValidationResult();
            var prediction = PredictResourceUsage(configuration);

            if (prediction.PerformanceImpact == PerformanceImpact.High)
            {
                foreach (var warning in prediction.Warnings)
                {
                    result.AddWarning(warning);
                }
            }

            return result;
        }

        private ValidationResult ValidateRecorderCompatibility(IRecordingConfiguration configuration)
        {
            var result = new ValidationResult();

            // Check for incompatible recorder combinations
            var hasAlembic = false;
            var hasFBX = false;

            foreach (var timelineConfig in configuration.TimelineConfigs)
            {
                var recorderConfig = timelineConfig as TimelineRecorderConfig;
                if (recorderConfig?.RecorderConfigurations != null)
                {
                    foreach (var recorder in recorderConfig.RecorderConfigurations)
                    {
                        if (recorder.Type == RecorderSettingsType.Alembic)
                            hasAlembic = true;
                        if (recorder.Type == RecorderSettingsType.FBX)
                            hasFBX = true;
                    }
                }
            }

            if (hasAlembic && hasFBX)
            {
                result.AddWarning("Using both Alembic and FBX recorders may cause performance issues");
            }

            return result;
        }

        // Helper methods

        private void InitializeValidators()
        {
            _validators[typeof(ImageRecorderConfiguration)] = new ImageRecorderValidator(_logger);
            _validators[typeof(MovieRecorderConfiguration)] = new MovieRecorderValidator(_logger);
            _validators[typeof(AnimationRecorderConfiguration)] = new AnimationRecorderValidator(_logger);
            _validators[typeof(AlembicRecorderConfiguration)] = new AlembicRecorderValidator(_logger);
            _validators[typeof(FBXRecorderConfiguration)] = new FBXRecorderValidator(_logger);
            _validators[typeof(AOVRecorderConfiguration)] = new AOVRecorderValidator(_logger);
        }

        private void InitializeRepairStrategies()
        {
            _repairStrategies.Add(new FrameRateRepairStrategy(_logger));
            _repairStrategies.Add(new ResolutionRepairStrategy(_logger));
            _repairStrategies.Add(new OutputPathRepairStrategy(_logger));
            _repairStrategies.Add(new GameObjectReferenceRepairStrategy(_logger, _referenceService));
        }

        private RepairSuggestion GetSuggestionForIssue(ValidationIssue issue)
        {
            var suggestion = new RepairSuggestion
            {
                Issue = issue,
                Description = GetSuggestionDescription(issue.Message)
            };

            // Add specific suggestions based on issue type
            if (issue.Message.Contains("frame rate"))
            {
                suggestion.Steps.Add("Check Timeline settings for frame rate configuration");
                suggestion.Steps.Add("Ensure all recorders use the same frame rate");
                suggestion.Steps.Add("Use the global frame rate setting for consistency");
            }
            else if (issue.Message.Contains("GameObject"))
            {
                suggestion.Steps.Add("Verify the GameObject exists in the scene");
                suggestion.Steps.Add("Check if the GameObject name has changed");
                suggestion.Steps.Add("Re-select the GameObject in the recorder settings");
            }
            else if (issue.Message.Contains("output path"))
            {
                suggestion.Steps.Add("Ensure the output directory exists");
                suggestion.Steps.Add("Check folder permissions");
                suggestion.Steps.Add("Use relative paths for better portability");
            }

            return suggestion;
        }

        private string GetSuggestionDescription(string issueMessage)
        {
            if (issueMessage.Contains("frame rate"))
                return "Frame rate inconsistency can cause synchronization issues";
            if (issueMessage.Contains("GameObject"))
                return "Missing GameObject references will cause recording failures";
            if (issueMessage.Contains("output path"))
                return "Invalid output paths will prevent recording";
            
            return "This issue should be resolved before recording";
        }

        private bool IsUnityVersionSupported(string version)
        {
            // Parse version and check minimum requirements
            var parts = version.Split('.');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            {
                return major > 2021 || (major == 2021 && minor >= 3);
            }
            return false;
        }

        private bool IsRecorderPackageVersionSupported(string version)
        {
            // Simple version check - could be enhanced
            return !string.IsNullOrEmpty(version);
        }

        private string GetRecorderPackageVersion()
        {
            // This would query the Package Manager for the Unity Recorder version
            // For now, return a placeholder
            return "2.0.0";
        }

        private int CalculateMemoryUsage(Resolution resolution, int frameRate, int timelineCount)
        {
            // Basic calculation: resolution * bytes per pixel * frame buffer count * timeline count
            var bytesPerPixel = 4; // RGBA
            var frameBufferCount = 3; // Triple buffering estimate
            var bytesPerFrame = resolution.Width * resolution.Height * bytesPerPixel;
            var totalBytes = bytesPerFrame * frameBufferCount * Math.Max(1, timelineCount);
            
            return totalBytes / (1024 * 1024); // Convert to MB
        }

        private float CalculateDiskUsage(IRecordingConfiguration configuration)
        {
            var totalUsage = 0f;
            
            foreach (var timelineConfig in configuration.TimelineConfigs)
            {
                var recorderConfig = timelineConfig as TimelineRecorderConfig;
                if (recorderConfig?.RecorderConfigurations != null)
                {
                    foreach (var recorder in recorderConfig.RecorderConfigurations)
                    {
                        totalUsage += EstimateRecorderDiskUsage(recorder, configuration);
                    }
                }
            }
            
            return totalUsage;
        }

        private float EstimateRecorderDiskUsage(IRecorderConfiguration recorder, IRecordingConfiguration configuration)
        {
            var resolution = configuration.Resolution;
            var frameRate = configuration.FrameRate;
            
            switch (recorder.Type)
            {
                case RecorderSettingsType.Image:
                    // PNG: ~1-3 MB per frame
                    return frameRate * 60 * 2; // MB per minute
                    
                case RecorderSettingsType.Movie:
                    // H.264: ~5-20 Mbps
                    return 10 * 60 / 8; // Convert Mbps to MB per minute
                    
                case RecorderSettingsType.Animation:
                    // Animation clips: relatively small
                    return 10; // MB per minute estimate
                    
                case RecorderSettingsType.Alembic:
                case RecorderSettingsType.FBX:
                    // Geometry cache: can be large
                    return 50; // MB per minute estimate
                    
                default:
                    return 20; // Default estimate
            }
        }

        private float CalculateCPUUsage(Resolution resolution, int frameRate, int timelineCount)
        {
            // Basic estimation based on resolution and frame rate
            var pixelCount = resolution.Width * resolution.Height;
            var pixelOperationsPerSecond = pixelCount * frameRate * timelineCount;
            
            // Normalize to percentage (rough estimate)
            var baselineOperations = 1920 * 1080 * 30; // 1080p at 30fps
            var cpuUsage = (pixelOperationsPerSecond / (float)baselineOperations) * 50; // 50% for baseline
            
            return Math.Min(100, cpuUsage);
        }
    }

    /// <summary>
    /// Resource usage prediction result
    /// </summary>
    public class ResourceUsagePrediction
    {
        public int EstimatedMemoryUsageMB { get; set; }
        public float EstimatedDiskUsageMBPerMinute { get; set; }
        public float EstimatedCPUUsage { get; set; }
        public PerformanceImpact PerformanceImpact { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Performance impact levels
    /// </summary>
    public enum PerformanceImpact
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Repair result
    /// </summary>
    public class RepairResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ValidationIssue> RepairedIssues { get; set; } = new List<ValidationIssue>();
        public List<ValidationIssue> UnrepairedIssues { get; set; } = new List<ValidationIssue>();
    }

    /// <summary>
    /// Repair suggestion
    /// </summary>
    public class RepairSuggestion
    {
        public ValidationIssue Issue { get; set; }
        public string Description { get; set; }
        public List<string> Steps { get; set; } = new List<string>();
    }

    /// <summary>
    /// Interface for configuration validators
    /// </summary>
    public interface IConfigurationValidator
    {
        ValidationResult Validate(IRecorderConfiguration configuration);
    }

    /// <summary>
    /// Interface for auto-repair strategies
    /// </summary>
    public interface IAutoRepairStrategy
    {
        bool CanRepair(ValidationIssue issue);
        bool Repair(IRecordingConfiguration configuration, ValidationIssue issue);
    }

    /// <summary>
    /// Interface for configuration validation service
    /// </summary>
    public interface IConfigurationValidationService
    {
        ValidationResult ValidateConfiguration(IRecordingConfiguration configuration);
        ValidationResult ValidateRecorderConfiguration(IRecorderConfiguration recorderConfig);
        RepairResult AutoRepairConfiguration(IRecordingConfiguration configuration);
        List<RepairSuggestion> GetRepairSuggestions(ValidationResult validationResult);
        ResourceUsagePrediction PredictResourceUsage(IRecordingConfiguration configuration);
    }
}