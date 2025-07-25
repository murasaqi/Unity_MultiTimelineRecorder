using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for configuring timeline-specific settings
    /// </summary>
    public class TimelineConfigurationBuilder
    {
        private readonly RecordingConfigurationBuilder _parentBuilder;
        private readonly TimelineRecorderConfig _timelineConfig;
        
        /// <summary>
        /// Creates a new timeline configuration builder
        /// </summary>
        /// <param name="parentBuilder">Parent recording configuration builder</param>
        /// <param name="director">PlayableDirector for this timeline</param>
        internal TimelineConfigurationBuilder(RecordingConfigurationBuilder parentBuilder, PlayableDirector director)
        {
            _parentBuilder = parentBuilder ?? throw new ArgumentNullException(nameof(parentBuilder));
            
            if (director == null)
                throw new ArgumentNullException(nameof(director));
            
            _timelineConfig = new TimelineRecorderConfig
            {
                Director = director,
                TimelineName = director.name,
                IsEnabled = true,
                RecorderConfigs = new List<IRecorderConfiguration>()
            };
        }
        
        /// <summary>
        /// Sets the timeline name
        /// </summary>
        /// <param name="name">Timeline name</param>
        /// <returns>Builder instance for chaining</returns>
        public TimelineConfigurationBuilder WithName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Timeline name cannot be empty", nameof(name));
            
            _timelineConfig.TimelineName = name;
            return this;
        }
        
        /// <summary>
        /// Sets whether the timeline is enabled for recording
        /// </summary>
        /// <param name="enabled">Enable state</param>
        /// <returns>Builder instance for chaining</returns>
        public TimelineConfigurationBuilder SetEnabled(bool enabled)
        {
            _timelineConfig.IsEnabled = enabled;
            return this;
        }
        
        /// <summary>
        /// Sets the recording time range
        /// </summary>
        /// <param name="startTime">Start time in seconds</param>
        /// <param name="endTime">End time in seconds</param>
        /// <returns>Builder instance for chaining</returns>
        public TimelineConfigurationBuilder WithTimeRange(double startTime, double endTime)
        {
            if (startTime < 0)
                throw new ArgumentException("Start time cannot be negative", nameof(startTime));
            
            if (endTime <= startTime)
                throw new ArgumentException("End time must be greater than start time", nameof(endTime));
            
            // Time range is typically managed by the PlayableDirector
            // This configuration could be stored elsewhere or passed to recorders
            return this;
        }
        
        /// <summary>
        /// Adds a movie recorder to this timeline
        /// </summary>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>Movie recorder builder for chaining</returns>
        public MovieRecorderBuilder WithMovieRecorder(Action<MovieRecorderBuilder> configure = null)
        {
            var builder = new MovieRecorderBuilder(this);
            configure?.Invoke(builder);
            
            var recorder = builder.Build();
            _timelineConfig.RecorderConfigs.Add(recorder);
            
            return builder;
        }
        
        /// <summary>
        /// Adds an image sequence recorder to this timeline
        /// </summary>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>Image recorder builder for chaining</returns>
        public ImageRecorderBuilder WithImageSequenceRecorder(Action<ImageRecorderBuilder> configure = null)
        {
            var builder = new ImageRecorderBuilder(this);
            configure?.Invoke(builder);
            
            var recorder = builder.Build();
            _timelineConfig.RecorderConfigs.Add(recorder);
            
            return builder;
        }
        
        /// <summary>
        /// Adds an animation recorder to this timeline
        /// </summary>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>Animation recorder builder for chaining</returns>
        public AnimationRecorderBuilder WithAnimationRecorder(Action<AnimationRecorderBuilder> configure = null)
        {
            var builder = new AnimationRecorderBuilder(this);
            configure?.Invoke(builder);
            
            var recorder = builder.Build();
            _timelineConfig.RecorderConfigs.Add(recorder);
            
            return builder;
        }
        
        /// <summary>
        /// Adds an FBX recorder to this timeline
        /// </summary>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>FBX recorder builder for chaining</returns>
        public FBXRecorderBuilder WithFBXRecorder(Action<FBXRecorderBuilder> configure = null)
        {
            var builder = new FBXRecorderBuilder(this);
            configure?.Invoke(builder);
            
            var recorder = builder.Build();
            _timelineConfig.RecorderConfigs.Add(recorder);
            
            return builder;
        }
        
        /// <summary>
        /// Adds an Alembic recorder to this timeline
        /// </summary>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>Alembic recorder builder for chaining</returns>
        public AlembicRecorderBuilder WithAlembicRecorder(Action<AlembicRecorderBuilder> configure = null)
        {
            var builder = new AlembicRecorderBuilder(this);
            configure?.Invoke(builder);
            
            var recorder = builder.Build();
            _timelineConfig.RecorderConfigs.Add(recorder);
            
            return builder;
        }
        
        /// <summary>
        /// Adds an AOV recorder to this timeline
        /// </summary>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>AOV recorder builder for chaining</returns>
        public AOVRecorderBuilder WithAOVRecorder(Action<AOVRecorderBuilder> configure = null)
        {
            var builder = new AOVRecorderBuilder(this);
            configure?.Invoke(builder);
            
            var recorder = builder.Build();
            _timelineConfig.RecorderConfigs.Add(recorder);
            
            return builder;
        }
        
        /// <summary>
        /// Adds multiple recorders of the same type
        /// </summary>
        /// <typeparam name="TBuilder">Recorder builder type</typeparam>
        /// <param name="count">Number of recorders to add</param>
        /// <param name="configure">Configuration action for each recorder</param>
        /// <returns>Builder instance for chaining</returns>
        public TimelineConfigurationBuilder WithMultipleRecorders<TBuilder>(int count, Action<TBuilder, int> configure) 
            where TBuilder : RecorderBuilderBase<TBuilder>, new()
        {
            if (count <= 0)
                throw new ArgumentException("Count must be positive", nameof(count));
            
            for (int i = 0; i < count; i++)
            {
                var builder = new TBuilder();
                builder.SetTimeline(this);
                configure?.Invoke(builder, i);
                
                var recorder = builder.Build();
                _timelineConfig.RecorderConfigs.Add(recorder);
            }
            
            return this;
        }
        
        /// <summary>
        /// Returns to the parent recording configuration builder
        /// </summary>
        /// <returns>Parent recording configuration builder</returns>
        public RecordingConfigurationBuilder And()
        {
            return _parentBuilder;
        }
        
        /// <summary>
        /// Adds another timeline to the configuration
        /// </summary>
        /// <param name="director">PlayableDirector to add</param>
        /// <returns>New timeline builder for chaining</returns>
        public TimelineConfigurationBuilder AddTimeline(PlayableDirector director)
        {
            // Finish current timeline and create new one using the extension method
            return RecordingConfigurationBuilderTimelineExtensions.AddTimeline(_parentBuilder, director);
        }
        
        /// <summary>
        /// Builds the complete recording configuration
        /// </summary>
        /// <returns>The completed recording configuration</returns>
        public RecordingConfiguration Build()
        {
            return _parentBuilder.Build();
        }
        
        /// <summary>
        /// Gets the timeline configuration being built
        /// </summary>
        internal TimelineRecorderConfig GetConfiguration()
        {
            return _timelineConfig;
        }
    }
    
    /// <summary>
    /// Extension methods for RecordingConfigurationBuilder to support timeline building
    /// </summary>
    public static class RecordingConfigurationBuilderTimelineExtensions
    {
        /// <summary>
        /// Adds a timeline with fluent configuration
        /// </summary>
        /// <param name="builder">Recording configuration builder</param>
        /// <param name="director">PlayableDirector to add</param>
        /// <returns>Timeline configuration builder</returns>
        public static TimelineConfigurationBuilder AddTimeline(this RecordingConfigurationBuilder builder, PlayableDirector director)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            
            if (director == null)
                throw new ArgumentNullException(nameof(director));
            
            var timelineBuilder = new TimelineConfigurationBuilder(builder, director);
            
            // Add the timeline config to the parent builder
            var config = timelineBuilder.GetConfiguration();
            
            // Use reflection to call the internal method or make it public
            var method = typeof(RecordingConfigurationBuilder).GetMethod("AddTimelineConfig",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(builder, new object[] { config });
            
            return timelineBuilder;
        }
    }
}