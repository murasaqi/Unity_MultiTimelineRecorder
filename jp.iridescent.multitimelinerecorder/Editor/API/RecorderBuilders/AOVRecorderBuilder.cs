using System;
using UnityEngine;
using UnityEngine.Rendering;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for AOV (Arbitrary Output Variables) recorder configuration
    /// </summary>
    public class AOVRecorderBuilder : RecorderBuilderBase<AOVRecorderBuilder>
    {
        private AOVRecorderConfiguration AOVConfig => (AOVRecorderConfiguration)_configuration;
        
        /// <summary>
        /// Creates a new AOV recorder builder
        /// </summary>
        public AOVRecorderBuilder()
        {
            _configuration = new AOVRecorderConfiguration
            {
                Name = "AOV Recorder",
                IsEnabled = true,
                AOVType = MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Beauty,
                OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR,
                CaptureAlpha = true
            };
        }
        
        /// <summary>
        /// Creates an AOV recorder builder with parent timeline
        /// </summary>
        /// <param name="timelineBuilder">Parent timeline builder</param>
        internal AOVRecorderBuilder(TimelineConfigurationBuilder timelineBuilder) : this()
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the AOV type
        /// </summary>
        /// <param name="aovType">AOV type</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType aovType)
        {
            AOVConfig.AOVType = aovType;
            
            // Auto-configure format based on AOV type
            if (aovType == MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Depth || 
                aovType == MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Normal || 
                aovType == MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Motion)
            {
                AOVConfig.OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                // HDR capture is configured based on format
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the output format
        /// </summary>
        /// <param name="format">Output format</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat format)
        {
            AOVConfig.OutputFormat = format;
            return this;
        }
        
        /// <summary>
        /// Sets the super sampling level
        /// </summary>
        /// <param name="level">Super sampling level (1, 2, 4, 8, or 16)</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithSuperSampling(int level)
        {
            if (level != 1 && level != 2 && level != 4 && level != 8 && level != 16)
                throw new ArgumentException("Super sampling must be 1, 2, 4, 8, or 16", nameof(level));
            
            AOVConfig.SuperSampling = level;
            return this;
        }
        
        /// <summary>
        /// Sets the target camera
        /// </summary>
        /// <param name="camera">Target camera GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithTargetCamera(GameObject camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            
            // TargetCamera is set via CameraTag instead
            var cameraComponent = camera.GetComponent<Camera>();
            if (cameraComponent != null)
            {
                AOVConfig.CameraTag = cameraComponent.tag;
            }
            return this;
        }
        
        /// <summary>
        /// Sets the target camera by component
        /// </summary>
        /// <param name="camera">Target camera component</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithTargetCamera(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            
            return WithTargetCamera(camera.gameObject);
        }
        
        /// <summary>
        /// Sets the file name pattern
        /// </summary>
        /// <param name="fileName">File name pattern</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            
            AOVConfig.FileName = fileName;
            return this;
        }
        
        /// <summary>
        /// Enables or disables alpha capture
        /// </summary>
        /// <param name="captureAlpha">Capture alpha channel</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithAlpha(bool captureAlpha = true)
        {
            AOVConfig.CaptureAlpha = captureAlpha;
            return this;
        }
        
        /// <summary>
        /// Enables or disables transparency recording
        /// </summary>
        /// <param name="recordTransparency">Record transparency</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithTransparency(bool recordTransparency = true)
        {
            AOVConfig.RecordTransparency = recordTransparency;
            return this;
        }
        
        /// <summary>
        /// Sets the camera tag for tagged camera source
        /// </summary>
        /// <param name="tag">Camera tag</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithCameraTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Camera tag cannot be empty", nameof(tag));
            
            AOVConfig.CameraTag = tag;
            AOVConfig.SourceType = Unity.MultiTimelineRecorder.ImageRecorderSourceType.TargetCamera;
            return this;
        }
        
        /// <summary>
        /// Sets whether to flip the output vertically
        /// </summary>
        /// <param name="flip">Flip vertical</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithFlipVertical(bool flip = true)
        {
            AOVConfig.FlipVertical = flip;
            return this;
        }
        
        /// <summary>
        /// Sets the custom AOV name when using Custom type
        /// </summary>
        /// <param name="name">Custom AOV name</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithCustomAOVName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Custom AOV name cannot be empty", nameof(name));
            
            AOVConfig.CustomAOVName = name;
            AOVConfig.AOVType = MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Custom;
            return this;
        }
        
        /// <summary>
        /// Applies an AOV preset
        /// </summary>
        /// <param name="preset">AOV preset</param>
        /// <returns>Builder instance for chaining</returns>
        public AOVRecorderBuilder WithPreset(AOVPreset preset)
        {
            switch (preset)
            {
                case AOVPreset.Beauty:
                    WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Beauty)
                        .WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG)
                        .WithAlpha(false);
                    break;
                    
                case AOVPreset.Depth:
                    WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Depth)
                        .WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
                        .WithAlpha(true);
                    break;
                    
                case AOVPreset.Normal:
                    WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Normal)
                        .WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
                        .WithAlpha(true);
                    break;
                    
                case AOVPreset.Motion:
                    WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Motion)
                        .WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
                        .WithAlpha(true);
                    break;
                    
                case AOVPreset.Albedo:
                    WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Albedo)
                        .WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG)
                        .WithAlpha(false);
                    break;
                    
                case AOVPreset.AllPasses:
                    // Note: This would typically require creating multiple recorders
                    WithAOVType(MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Beauty)
                        .WithOutputFormat(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
                        .WithAlpha(true);
                    break;
            }
            
            return this;
        }
        
        /// <summary>
        /// Validates the AOV recorder configuration
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();
            
            // Validate super sampling
            var validSamples = new[] { 1, 2, 4, 8, 16 };
            if (AOVConfig.SuperSampling > 0 && !System.Linq.Enumerable.Contains(validSamples, AOVConfig.SuperSampling))
                throw new InvalidOperationException($"Invalid super sampling value: {AOVConfig.SuperSampling}. Must be 1, 2, 4, 8, or 16.");
            
            // Validate custom AOV name when using custom type
            if (AOVConfig.AOVType == MultiTimelineRecorder.Core.Models.RecorderSettings.AOVType.Custom && 
                string.IsNullOrWhiteSpace(AOVConfig.CustomAOVName))
                throw new InvalidOperationException("Custom AOV name is required when AOV type is Custom");
            
            if (string.IsNullOrWhiteSpace(AOVConfig.FileName))
                AOVConfig.FileName = $"AOV_{AOVConfig.AOVType}_<Scene>_<Take>";
        }
    }
    
    /// <summary>
    /// AOV presets
    /// </summary>
    public enum AOVPreset
    {
        Beauty,     // Standard beauty pass
        Depth,      // Depth pass
        Normal,     // World normal pass
        Motion,     // Motion vectors
        Albedo,     // Base color/albedo
        AllPasses   // All available passes
    }
}