using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for creating recording configurations
    /// </summary>
    public class RecordingConfigurationBuilder
    {
        private RecordingConfiguration _configuration;
        private TimelineRecorderConfig _currentTimeline;
        private IRecorderConfiguration _currentRecorder;
        
        /// <summary>
        /// Creates a new configuration builder
        /// </summary>
        public RecordingConfigurationBuilder()
        {
            _configuration = RecordingConfiguration.CreateDefault();
        }
        
        /// <summary>
        /// Creates a new configuration builder with an existing configuration
        /// </summary>
        /// <param name="configuration">Existing configuration to modify</param>
        public RecordingConfigurationBuilder(RecordingConfiguration configuration)
        {
            _configuration = configuration ?? RecordingConfiguration.CreateDefault();
        }
        
        #region Global Settings
        
        /// <summary>
        /// Sets the global frame rate
        /// </summary>
        /// <param name="frameRate">Frame rate in FPS</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithFrameRate(int frameRate)
        {
            if (frameRate <= 0 || frameRate > 120)
            {
                throw new ArgumentOutOfRangeException(nameof(frameRate), "Frame rate must be between 1 and 120");
            }
            
            _configuration.FrameRate = frameRate;
            return this;
        }
        
        /// <summary>
        /// Sets the global resolution
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Width and height must be positive values");
            }
            
            _configuration.Resolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(width, height);
            return this;
        }
        
        /// <summary>
        /// Sets the output path for recordings
        /// </summary>
        /// <param name="path">Output directory path</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithOutputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Output path cannot be empty", nameof(path));
            }
            
            _configuration.OutputPath = path;
            return this;
        }
        
        #endregion
        
        #region Timeline Management
        
        /// <summary>
        /// Adds a timeline to the configuration
        /// </summary>
        /// <param name="director">PlayableDirector to add</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder AddTimeline(PlayableDirector director)
        {
            if (director == null)
            {
                throw new ArgumentNullException(nameof(director));
            }
            
            _currentTimeline = new TimelineRecorderConfig
            {
                Director = director,
                TimelineName = director.name,
                IsEnabled = true
            };
            
            _configuration.AddTimelineConfig(_currentTimeline);
            return this;
        }
        
        /// <summary>
        /// Adds a timeline with custom settings
        /// </summary>
        /// <param name="director">PlayableDirector to add</param>
        /// <param name="name">Custom timeline name</param>
        /// <param name="enabled">Whether the timeline is enabled</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder AddTimeline(PlayableDirector director, string name, bool enabled = true)
        {
            if (director == null)
            {
                throw new ArgumentNullException(nameof(director));
            }
            
            _currentTimeline = new TimelineRecorderConfig
            {
                Director = director,
                TimelineName = name ?? director.name,
                IsEnabled = enabled
            };
            
            _configuration.AddTimelineConfig(_currentTimeline);
            return this;
        }
        
        /// <summary>
        /// Adds a timeline configuration created by a timeline builder
        /// </summary>
        /// <param name="timelineConfig">Timeline configuration</param>
        /// <returns>Builder instance for chaining</returns>
        internal RecordingConfigurationBuilder AddTimelineConfig(TimelineRecorderConfig timelineConfig)
        {
            if (timelineConfig == null)
            {
                throw new ArgumentNullException(nameof(timelineConfig));
            }
            
            _configuration.AddTimelineConfig(timelineConfig);
            _currentTimeline = timelineConfig;
            return this;
        }
        
        #endregion
        
        #region Recorder Management
        
        /// <summary>
        /// Adds a movie recorder to the current timeline
        /// </summary>
        /// <param name="name">Recorder name</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithMovieRecorder(string name = "Movie Recorder")
        {
            ValidateCurrentTimeline();
            
            _currentRecorder = new MovieRecorderConfiguration
            {
                Name = name,
                IsEnabled = true,
                Format = VideoFormat.MP4,
                FrameRate = _configuration.FrameRate,
                Width = _configuration.Resolution.Width,
                Height = _configuration.Resolution.Height,
                Codec = VideoCodec.H264,
                BitrateMode = BitrateMode.High
            };
            
            _currentTimeline.RecorderConfigs.Add(_currentRecorder);
            return this;
        }
        
        /// <summary>
        /// Adds an image sequence recorder to the current timeline
        /// </summary>
        /// <param name="name">Recorder name</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithImageRecorder(string name = "Image Sequence")
        {
            ValidateCurrentTimeline();
            
            _currentRecorder = new ImageRecorderConfiguration
            {
                Name = name,
                IsEnabled = true,
                Format = ImageFormat.PNG,
                FrameRate = _configuration.FrameRate,
                Width = _configuration.Resolution.Width,
                Height = _configuration.Resolution.Height,
                CaptureAlpha = false
            };
            
            _currentTimeline.RecorderConfigs.Add(_currentRecorder);
            return this;
        }
        
        /// <summary>
        /// Adds an animation recorder to the current timeline
        /// </summary>
        /// <param name="name">Recorder name</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithAnimationRecorder(string name = "Animation Recorder")
        {
            ValidateCurrentTimeline();
            
            _currentRecorder = new AnimationRecorderConfiguration
            {
                Name = name,
                IsEnabled = true,
                RecordTransform = true,
                RecordComponents = true,
                FrameRate = _configuration.FrameRate
            };
            
            _currentTimeline.RecorderConfigs.Add(_currentRecorder);
            return this;
        }
        
        /// <summary>
        /// Adds an AOV recorder to the current timeline
        /// </summary>
        /// <param name="name">Recorder name</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithAOVRecorder(string name = "AOV Recorder")
        {
            ValidateCurrentTimeline();
            
            _currentRecorder = new AOVRecorderConfiguration
            {
                Name = name,
                IsEnabled = true,
                AOVType = AOVType.Beauty,
                OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR
            };
            
            _currentTimeline.RecorderConfigs.Add(_currentRecorder);
            return this;
        }
        
        /// <summary>
        /// Adds a generic recorder with a configuration action
        /// </summary>
        /// <typeparam name="T">Recorder configuration type</typeparam>
        /// <param name="configure">Configuration action</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder WithRecorder<T>(Action<T> configure = null) where T : IRecorderConfiguration, new()
        {
            ValidateCurrentTimeline();
            
            var recorder = new T();
            // Set frame rate if supported by recorder type
            if (recorder is MovieRecorderConfiguration movieRec)
            {
                movieRec.FrameRate = _configuration.FrameRate;
            }
            else if (recorder is ImageRecorderConfiguration imageRec)
            {
                imageRec.FrameRate = _configuration.FrameRate;
            }
            else if (recorder is AnimationRecorderConfiguration animRec)
            {
                animRec.FrameRate = _configuration.FrameRate;
            }
            
            configure?.Invoke(recorder);
            
            _currentRecorder = recorder;
            _currentTimeline.RecorderConfigs.Add(_currentRecorder);
            
            return this;
        }
        
        #endregion
        
        #region Recorder Configuration
        
        /// <summary>
        /// Sets the output format for the current movie recorder
        /// </summary>
        /// <param name="format">Video format</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetOutputFormat(VideoFormat format)
        {
            if (_currentRecorder is MovieRecorderConfiguration movieRecorder)
            {
                movieRecorder.Format = format;
            }
            else
            {
                throw new InvalidOperationException("Current recorder is not a movie recorder");
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the output format for the current image recorder
        /// </summary>
        /// <param name="format">Image format</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetOutputFormat(ImageFormat format)
        {
            if (_currentRecorder is ImageRecorderConfiguration imageRecorder)
            {
                imageRecorder.Format = format;
            }
            else
            {
                throw new InvalidOperationException("Current recorder is not an image recorder");
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the quality for the current movie recorder
        /// </summary>
        /// <param name="bitrateMode">Bitrate mode</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetQuality(BitrateMode bitrateMode)
        {
            if (_currentRecorder is MovieRecorderConfiguration movieRecorder)
            {
                movieRecorder.BitrateMode = bitrateMode;
            }
            else
            {
                throw new InvalidOperationException("Current recorder is not a movie recorder");
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the file name pattern for the current recorder
        /// </summary>
        /// <param name="pattern">File name pattern with wildcards</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetFileName(string pattern)
        {
            if (_currentRecorder == null)
            {
                throw new InvalidOperationException("No recorder selected");
            }
            
            // File name pattern is set based on recorder type
            // This is handled in the individual recorder configurations
            if (_currentRecorder is MovieRecorderConfiguration movieRec)
            {
                movieRec.FileName = pattern;
            }
            else if (_currentRecorder is ImageRecorderConfiguration imageRec)
            {
                imageRec.FileNamePattern = pattern;
            }
            else if (_currentRecorder is AnimationRecorderConfiguration animRec)
            {
                animRec.FileName = pattern;
            }
            else if (_currentRecorder is AOVRecorderConfiguration aovRec)
            {
                aovRec.FileName = pattern;
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the target GameObject for animation recorder
        /// </summary>
        /// <param name="target">Target GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetTargetGameObject(GameObject target)
        {
            if (_currentRecorder is AnimationRecorderConfiguration animRecorder)
            {
                animRecorder.TargetGameObject = target;
            }
            else
            {
                throw new InvalidOperationException("Current recorder is not an animation recorder");
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the recording scope for animation recorder
        /// </summary>
        /// <param name="includeChildren">Include children in recording</param>
        /// <param name="recordComponents">Record component properties</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetRecordingScope(bool includeChildren, bool recordComponents = true)
        {
            if (_currentRecorder is AnimationRecorderConfiguration animRecorder)
            {
                animRecorder.RecordHierarchy = includeChildren;
                animRecorder.RecordComponents = recordComponents;
            }
            else
            {
                throw new InvalidOperationException("Current recorder is not an animation recorder");
            }
            
            return this;
        }
        
        /// <summary>
        /// Enables alpha channel capture for image recorder
        /// </summary>
        /// <param name="captureAlpha">Enable alpha capture</param>
        /// <returns>Builder instance for chaining</returns>
        public RecordingConfigurationBuilder SetCaptureAlpha(bool captureAlpha)
        {
            if (_currentRecorder is ImageRecorderConfiguration imageRecorder)
            {
                imageRecorder.CaptureAlpha = captureAlpha;
            }
            else
            {
                throw new InvalidOperationException("Current recorder is not an image recorder");
            }
            
            return this;
        }
        
        #endregion
        
        #region Build and Validation
        
        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The completed recording configuration</returns>
        public RecordingConfiguration Build()
        {
            // Apply global frame rate to all recorders
            foreach (var timeline in _configuration.TimelineConfigs)
            {
                foreach (var recorder in timeline.RecorderConfigs)
                {
                    // Set frame rate if supported by recorder type
                    if (recorder is MovieRecorderConfiguration movieRec)
                    {
                        movieRec.FrameRate = _configuration.FrameRate;
                    }
                    else if (recorder is ImageRecorderConfiguration imageRec)
                    {
                        imageRec.FrameRate = _configuration.FrameRate;
                    }
                    else if (recorder is AnimationRecorderConfiguration animRec)
                    {
                        animRec.FrameRate = _configuration.FrameRate;
                    }
                }
            }
            
            // Validate configuration
            var validationResult = _configuration.Validate();
            if (!validationResult.IsValid)
            {
                var errors = string.Join("\n", validationResult.Issues
                    .Where(i => i.Severity == ValidationSeverity.Error)
                    .Select(i => i.Message));
                throw new InvalidOperationException($"Configuration validation failed:\n{errors}");
            }
            
            return _configuration;
        }
        
        /// <summary>
        /// Builds the configuration without validation
        /// </summary>
        /// <returns>The recording configuration</returns>
        public RecordingConfiguration BuildWithoutValidation()
        {
            // Apply global frame rate to all recorders
            foreach (var timeline in _configuration.TimelineConfigs)
            {
                foreach (var recorder in timeline.RecorderConfigs)
                {
                    // Set frame rate if supported by recorder type
                    if (recorder is MovieRecorderConfiguration movieRec)
                    {
                        movieRec.FrameRate = _configuration.FrameRate;
                    }
                    else if (recorder is ImageRecorderConfiguration imageRec)
                    {
                        imageRec.FrameRate = _configuration.FrameRate;
                    }
                    else if (recorder is AnimationRecorderConfiguration animRec)
                    {
                        animRec.FrameRate = _configuration.FrameRate;
                    }
                }
            }
            
            return _configuration;
        }
        
        #endregion
        
        #region Private Methods
        
        private void ValidateCurrentTimeline()
        {
            if (_currentTimeline == null)
            {
                throw new InvalidOperationException("No timeline selected. Call AddTimeline first.");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for fluent API
    /// </summary>
    public static class RecordingConfigurationBuilderExtensions
    {
        /// <summary>
        /// Applies a preset to the builder
        /// </summary>
        /// <param name="builder">Builder instance</param>
        /// <param name="preset">Preset to apply</param>
        /// <returns>Builder instance for chaining</returns>
        public static RecordingConfigurationBuilder WithPreset(this RecordingConfigurationBuilder builder, RecordingPreset preset)
        {
            switch (preset)
            {
                case RecordingPreset.FullHD_30fps:
                    builder.WithFrameRate(30).WithResolution(1920, 1080);
                    break;
                    
                case RecordingPreset.FullHD_60fps:
                    builder.WithFrameRate(60).WithResolution(1920, 1080);
                    break;
                    
                case RecordingPreset.UHD_4K_30fps:
                    builder.WithFrameRate(30).WithResolution(3840, 2160);
                    break;
                    
                case RecordingPreset.UHD_4K_60fps:
                    builder.WithFrameRate(60).WithResolution(3840, 2160);
                    break;
                    
                case RecordingPreset.HD_30fps:
                    builder.WithFrameRate(30).WithResolution(1280, 720);
                    break;
            }
            
            return builder;
        }
    }
    
    /// <summary>
    /// Common recording presets
    /// </summary>
    public enum RecordingPreset
    {
        FullHD_30fps,
        FullHD_60fps,
        UHD_4K_30fps,
        UHD_4K_60fps,
        HD_30fps
    }
}