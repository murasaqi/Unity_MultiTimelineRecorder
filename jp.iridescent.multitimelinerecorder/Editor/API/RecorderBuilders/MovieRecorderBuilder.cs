using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for movie recorder configuration
    /// </summary>
    public class MovieRecorderBuilder : ImageBasedRecorderBuilder<MovieRecorderBuilder>
    {
        private MovieRecorderConfiguration MovieConfig => (MovieRecorderConfiguration)_configuration;
        
        /// <summary>
        /// Creates a new movie recorder builder
        /// </summary>
        public MovieRecorderBuilder()
        {
            _configuration = new MovieRecorderConfiguration
            {
                Name = "Movie Recorder",
                IsEnabled = true,
                Format = VideoFormat.MP4,
                Codec = VideoCodec.H264,
                BitrateMode = BitrateMode.High,
                FrameRate = 30,
                Width = 1920,
                Height = 1080
            };
        }
        
        /// <summary>
        /// Creates a movie recorder builder with parent timeline
        /// </summary>
        /// <param name="timelineBuilder">Parent timeline builder</param>
        internal MovieRecorderBuilder(TimelineConfigurationBuilder timelineBuilder) : this()
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the video format
        /// </summary>
        /// <param name="format">Video format</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithFormat(VideoFormat format)
        {
            MovieConfig.Format = format;
            return this;
        }
        
        /// <summary>
        /// Sets the video codec
        /// </summary>
        /// <param name="codec">Video codec</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithCodec(VideoCodec codec)
        {
            MovieConfig.Codec = codec;
            return this;
        }
        
        /// <summary>
        /// Sets the bitrate mode
        /// </summary>
        /// <param name="bitrateMode">Bitrate mode</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithBitrateMode(BitrateMode bitrateMode)
        {
            MovieConfig.BitrateMode = bitrateMode;
            return this;
        }
        
        /// <summary>
        /// Sets a custom bitrate in kbps
        /// </summary>
        /// <param name="bitrate">Bitrate in kbps</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithBitrate(int bitrate)
        {
            if (bitrate <= 0)
                throw new ArgumentException("Bitrate must be positive", nameof(bitrate));
            
            // Note: Unity Recorder doesn't expose custom bitrate directly
            // We use quality settings to approximate the desired bitrate
            MovieConfig.Quality = Mathf.Clamp01(bitrate / 50000f); // Normalize to 0-1 range
            MovieConfig.BitrateMode = BitrateMode.High;
            return this;
        }
        
        /// <summary>
        /// Sets the quality level (0.0 to 1.0)
        /// </summary>
        /// <param name="quality">Quality level</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithQuality(float quality)
        {
            if (quality < 0f || quality > 1f)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 1");
            
            MovieConfig.Quality = quality;
            return this;
        }
        
        /// <summary>
        /// Sets the target camera
        /// </summary>
        /// <param name="camera">Target camera GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithTargetCamera(GameObject camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            
            MovieConfig.TargetCamera = camera.GetComponent<Camera>();
            return this;
        }
        
        /// <summary>
        /// Sets the target camera by component
        /// </summary>
        /// <param name="camera">Target camera component</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithTargetCamera(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            
            return WithTargetCamera(camera.gameObject);
        }
        
        /// <summary>
        /// Sets the recording source
        /// </summary>
        /// <param name="source">Recording source type</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithSource(ImageRecorderSourceType source)
        {
            MovieConfig.SourceType = source;
            return this;
        }
        
        /// <summary>
        /// Enables or disables audio capture
        /// </summary>
        /// <param name="captureAudio">Enable audio capture</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithAudio(bool captureAudio = true)
        {
            MovieConfig.CaptureAudio = captureAudio;
            return this;
        }
        
        /// <summary>
        /// Sets the file name pattern
        /// </summary>
        /// <param name="pattern">File name pattern with wildcards</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithFileName(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("File name pattern cannot be empty", nameof(pattern));
            
            MovieConfig.FileName = pattern;
            return this;
        }
        
        /// <summary>
        /// Applies a quality preset
        /// </summary>
        /// <param name="preset">Quality preset</param>
        /// <returns>Builder instance for chaining</returns>
        public MovieRecorderBuilder WithPreset(MovieQualityPreset preset)
        {
            switch (preset)
            {
                case MovieQualityPreset.Low:
                    WithResolution(1280, 720)
                        .WithBitrateMode(BitrateMode.Low)
                        .WithFrameRate(30);
                    break;
                    
                case MovieQualityPreset.Medium:
                    WithResolution(1920, 1080)
                        .WithBitrateMode(BitrateMode.Medium)
                        .WithFrameRate(30);
                    break;
                    
                case MovieQualityPreset.High:
                    WithResolution(1920, 1080)
                        .WithBitrateMode(BitrateMode.High)
                        .WithFrameRate(60);
                    break;
                    
                case MovieQualityPreset.Ultra:
                    WithResolution(3840, 2160)
                        .WithBitrateMode(BitrateMode.High)
                        .WithFrameRate(60);
                    break;
                    
                case MovieQualityPreset.ProRes:
                    WithFormat(VideoFormat.MOV)
                        .WithCodec(VideoCodec.ProRes)
                        .WithResolution(1920, 1080)
                        .WithFrameRate(30);
                    break;
            }
            
            return this;
        }
        
        /// <summary>
        /// Validates the movie recorder configuration
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();
            
            if (MovieConfig.Width <= 0 || MovieConfig.Height <= 0)
                throw new InvalidOperationException("Invalid resolution settings");
            
            if (MovieConfig.FrameRate <= 0)
                throw new InvalidOperationException("Invalid frame rate");
            
            // Validate codec compatibility with format
            if (MovieConfig.Format == VideoFormat.MP4 && MovieConfig.Codec == VideoCodec.ProRes)
                throw new InvalidOperationException("ProRes codec is not compatible with MP4 format");
            
            if (string.IsNullOrWhiteSpace(MovieConfig.FileName))
                MovieConfig.FileName = "Movie_<Scene>_<Take>";
        }
    }
    
    /// <summary>
    /// Movie quality presets
    /// </summary>
    public enum MovieQualityPreset
    {
        Low,        // 720p, 30fps, low bitrate
        Medium,     // 1080p, 30fps, medium bitrate
        High,       // 1080p, 60fps, high bitrate
        Ultra,      // 4K, 60fps, high bitrate
        ProRes      // ProRes format for professional use
    }
}