using System;
using System.Collections.Generic;
using UnityEngine;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder.Input;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for animation recorder configuration
    /// </summary>
    public class AnimationRecorderBuilder : RecorderBuilderBase<AnimationRecorderBuilder>
    {
        private AnimationRecorderConfiguration AnimConfig => (AnimationRecorderConfiguration)_configuration;
        
        /// <summary>
        /// Creates a new animation recorder builder
        /// </summary>
        public AnimationRecorderBuilder()
        {
            _configuration = new AnimationRecorderConfiguration
            {
                Name = "Animation Recorder",
                IsEnabled = true,
                RecordTransform = true,
                RecordComponents = true,
                RecordHierarchy = false,
                FrameRate = 30
            };
        }
        
        /// <summary>
        /// Creates an animation recorder builder with parent timeline
        /// </summary>
        /// <param name="timelineBuilder">Parent timeline builder</param>
        internal AnimationRecorderBuilder(TimelineConfigurationBuilder timelineBuilder) : this()
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the target GameObject to record
        /// </summary>
        /// <param name="target">Target GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithTarget(GameObject target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            
            AnimConfig.TargetGameObject = target;
            
            return this;
        }
        
        /// <summary>
        /// Sets the target Transform to record
        /// </summary>
        /// <param name="target">Target Transform</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithTarget(Transform target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            
            return WithTarget(target.gameObject);
        }
        
        /// <summary>
        /// Sets whether to record transform properties
        /// </summary>
        /// <param name="recordTransform">Record transform</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithTransform(bool recordTransform = true)
        {
            AnimConfig.RecordTransform = recordTransform;
            return this;
        }
        
        /// <summary>
        /// Sets whether to record component properties
        /// </summary>
        /// <param name="recordComponents">Record components</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithComponents(bool recordComponents = true)
        {
            AnimConfig.RecordComponents = recordComponents;
            return this;
        }
        
        /// <summary>
        /// Sets whether to record the entire hierarchy
        /// </summary>
        /// <param name="recordHierarchy">Record hierarchy</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithHierarchy(bool recordHierarchy = true)
        {
            AnimConfig.RecordHierarchy = recordHierarchy;
            return this;
        }
        
        /// <summary>
        /// Sets specific components to record
        /// </summary>
        /// <param name="componentTypes">Component types to record</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithSpecificComponents(params Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
                throw new ArgumentException("At least one component type must be specified", nameof(componentTypes));
            
            // Component filtering is typically done differently in Unity Recorder
            // This could be stored in custom configuration or passed through other means
            
            AnimConfig.RecordComponents = true;
            return this;
        }
        
        /// <summary>
        /// Sets the file name for the animation clip
        /// </summary>
        /// <param name="fileName">File name pattern</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            
            AnimConfig.FileName = fileName;
            return this;
        }
        
        /// <summary>
        /// Sets the curve simplification options
        /// </summary>
        /// <param name="options">Simplification options</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithSimplification(AnimationInputSettings.CurveSimplificationOptions options)
        {
            AnimConfig.SimplificationOptions = options;
            return this;
        }
        
        /// <summary>
        /// Sets animation compression mode
        /// </summary>
        /// <param name="mode">Compression mode</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithCompressionMode(AnimationCompressionMode mode)
        {
            AnimConfig.CompressionMode = mode;
            return this;
        }
        
        /// <summary>
        /// Enables or disables keyframe reduction
        /// </summary>
        /// <param name="reduce">Enable keyframe reduction</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithKeyframeReduction(bool reduce = true)
        {
            AnimConfig.KeyframeReduction = reduce;
            return this;
        }
        
        /// <summary>
        /// Enables or disables clamped tangents
        /// </summary>
        /// <param name="clamp">Use clamped tangents</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithClampedTangents(bool clamp = true)
        {
            AnimConfig.ClampedTangents = clamp;
            return this;
        }
        
        /// <summary>
        /// Applies a recording preset
        /// </summary>
        /// <param name="preset">Recording preset</param>
        /// <returns>Builder instance for chaining</returns>
        public AnimationRecorderBuilder WithPreset(AnimationRecordingPreset preset)
        {
            switch (preset)
            {
                case AnimationRecordingPreset.TransformOnly:
                    WithTransform(true)
                        .WithComponents(false)
                        .WithHierarchy(false);
                    break;
                    
                case AnimationRecordingPreset.FullObject:
                    WithTransform(true)
                        .WithComponents(true)
                        .WithHierarchy(false);
                    break;
                    
                case AnimationRecordingPreset.FullHierarchy:
                    WithTransform(true)
                        .WithComponents(true)
                        .WithHierarchy(true);
                    break;
                    
                case AnimationRecordingPreset.Optimized:
                    WithTransform(true)
                        .WithComponents(true)
                        .WithHierarchy(false)
                        .WithSimplification(AnimationInputSettings.CurveSimplificationOptions.Lossy);
                    break;
                    
                case AnimationRecordingPreset.HighPrecision:
                    WithTransform(true)
                        .WithComponents(true)
                        .WithHierarchy(false)
                        .WithFrameRate(60)
                        .WithSimplification(AnimationInputSettings.CurveSimplificationOptions.Lossless);
                    break;
            }
            
            return this;
        }
        
        /// <summary>
        /// Validates the animation recorder configuration
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();
            
            if (AnimConfig.TargetGameObject == null)
                throw new InvalidOperationException("Target GameObject is required for animation recording");
            
            if (!AnimConfig.RecordTransform && !AnimConfig.RecordComponents)
                throw new InvalidOperationException("At least one recording option (transform or components) must be enabled");
            
            if (string.IsNullOrWhiteSpace(AnimConfig.FileName))
                AnimConfig.FileName = "Animation_<Scene>_<Take>";
        }
    }
    
    /// <summary>
    /// Animation recording presets
    /// </summary>
    public enum AnimationRecordingPreset
    {
        TransformOnly,      // Record only transform properties
        FullObject,         // Record transform and all components
        FullHierarchy,      // Record entire hierarchy
        Optimized,          // Full object with simplification
        HighPrecision       // High frame rate, minimal simplification
    }
    
}