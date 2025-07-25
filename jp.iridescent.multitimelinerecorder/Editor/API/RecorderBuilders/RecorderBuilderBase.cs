using System;
using UnityEngine;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Base class for all recorder configuration builders
    /// </summary>
    /// <typeparam name="TBuilder">The concrete builder type for fluent API support</typeparam>
    public abstract class RecorderBuilderBase<TBuilder> where TBuilder : RecorderBuilderBase<TBuilder>
    {
        protected IRecorderConfiguration _configuration;
        protected TimelineConfigurationBuilder _timelineBuilder;
        
        /// <summary>
        /// Sets the parent timeline builder
        /// </summary>
        /// <param name="timelineBuilder">Timeline builder</param>
        internal void SetTimeline(TimelineConfigurationBuilder timelineBuilder)
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the recorder name
        /// </summary>
        /// <param name="name">Recorder name</param>
        /// <returns>Builder instance for chaining</returns>
        public TBuilder WithName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Recorder name cannot be empty", nameof(name));
            
            _configuration.Name = name;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether the recorder is enabled
        /// </summary>
        /// <param name="enabled">Enable state</param>
        /// <returns>Builder instance for chaining</returns>
        public TBuilder SetEnabled(bool enabled)
        {
            _configuration.IsEnabled = enabled;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the frame rate
        /// </summary>
        /// <param name="frameRate">Frame rate in FPS</param>
        /// <returns>Builder instance for chaining</returns>
        public TBuilder WithFrameRate(int frameRate)
        {
            if (frameRate <= 0 || frameRate > 120)
                throw new ArgumentOutOfRangeException(nameof(frameRate), "Frame rate must be between 1 and 120");
            
            // Frame rate is typically handled at a global level or on specific recorder types
            // Store it for later use when building the configuration
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the output path pattern
        /// </summary>
        /// <param name="pattern">Output path pattern with wildcards</param>
        /// <returns>Builder instance for chaining</returns>
        public TBuilder WithOutputPath(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Output path cannot be empty", nameof(pattern));
            
            // Output path is managed at recording level, not individual recorder level
            // This method is kept for API compatibility but may need to be reconsidered
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Adds another recorder to the current timeline
        /// </summary>
        /// <returns>Timeline builder for adding more recorders</returns>
        public TimelineConfigurationBuilder And()
        {
            return _timelineBuilder;
        }
        
        /// <summary>
        /// Builds the recorder configuration
        /// </summary>
        /// <returns>The completed recorder configuration</returns>
        public virtual IRecorderConfiguration Build()
        {
            ValidateConfiguration();
            return _configuration;
        }
        
        /// <summary>
        /// Validates the configuration
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            if (_configuration == null)
                throw new InvalidOperationException("Configuration not initialized");
            
            if (string.IsNullOrWhiteSpace(_configuration.Name))
                throw new InvalidOperationException("Recorder name is required");
        }
    }
    
    /// <summary>
    /// Base class for image-based recorder builders
    /// </summary>
    /// <typeparam name="TBuilder">The concrete builder type</typeparam>
    public abstract class ImageBasedRecorderBuilder<TBuilder> : RecorderBuilderBase<TBuilder> 
        where TBuilder : ImageBasedRecorderBuilder<TBuilder>
    {
        /// <summary>
        /// Sets the output resolution
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <returns>Builder instance for chaining</returns>
        public TBuilder WithResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive values");
            
            if (_configuration is MovieRecorderConfiguration movieConfig)
            {
                movieConfig.Width = width;
                movieConfig.Height = height;
            }
            else if (_configuration is ImageRecorderConfiguration imageConfig)
            {
                imageConfig.Width = width;
                imageConfig.Height = height;
            }
            
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the aspect ratio
        /// </summary>
        /// <param name="aspectRatio">Aspect ratio (e.g., 16/9f for 16:9)</param>
        /// <param name="baseWidth">Base width to calculate height from</param>
        /// <returns>Builder instance for chaining</returns>
        public TBuilder WithAspectRatio(float aspectRatio, int baseWidth = 1920)
        {
            if (aspectRatio <= 0)
                throw new ArgumentException("Aspect ratio must be positive", nameof(aspectRatio));
            
            if (baseWidth <= 0)
                throw new ArgumentException("Base width must be positive", nameof(baseWidth));
            
            int height = Mathf.RoundToInt(baseWidth / aspectRatio);
            return WithResolution(baseWidth, height);
        }
    }
}