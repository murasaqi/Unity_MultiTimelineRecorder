using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for image sequence recorder configuration
    /// </summary>
    public class ImageRecorderBuilder : ImageBasedRecorderBuilder<ImageRecorderBuilder>
    {
        private ImageRecorderConfiguration ImageConfig => (ImageRecorderConfiguration)_configuration;
        
        /// <summary>
        /// Creates a new image recorder builder
        /// </summary>
        public ImageRecorderBuilder()
        {
            _configuration = new ImageRecorderConfiguration
            {
                Name = "Image Sequence",
                IsEnabled = true,
                Format = ImageFormat.PNG,
                FrameRate = 30,
                Width = 1920,
                Height = 1080,
                CaptureAlpha = false
            };
        }
        
        /// <summary>
        /// Creates an image recorder builder with parent timeline
        /// </summary>
        /// <param name="timelineBuilder">Parent timeline builder</param>
        internal ImageRecorderBuilder(TimelineConfigurationBuilder timelineBuilder) : this()
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the image format
        /// </summary>
        /// <param name="format">Image format</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithFormat(ImageFormat format)
        {
            ImageConfig.Format = format;
            return this;
        }
        
        /// <summary>
        /// Enables or disables alpha channel capture
        /// </summary>
        /// <param name="captureAlpha">Enable alpha capture</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithAlpha(bool captureAlpha = true)
        {
            ImageConfig.CaptureAlpha = captureAlpha;
            
            // Automatically switch to PNG if alpha is enabled and format doesn't support it
            if (captureAlpha && ImageConfig.Format == ImageFormat.JPEG)
            {
                ImageConfig.Format = ImageFormat.PNG;
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets the JPEG quality (0-100)
        /// </summary>
        /// <param name="quality">JPEG quality</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithJpegQuality(int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "JPEG quality must be between 0 and 100");
            
            ImageConfig.JpegQuality = quality;
            return this;
        }
        
        /// <summary>
        /// Sets the file name pattern
        /// </summary>
        /// <param name="pattern">File name pattern with wildcards</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithFileNamePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("File name pattern cannot be empty", nameof(pattern));
            
            ImageConfig.FileNamePattern = pattern;
            return this;
        }
        
        /// <summary>
        /// Sets the image source
        /// </summary>
        /// <param name="source">Image source type</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithSource(ImageRecorderSourceType source)
        {
            ImageConfig.SourceType = source;
            return this;
        }
        
        /// <summary>
        /// Sets the target camera
        /// </summary>
        /// <param name="camera">Target camera GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithTargetCamera(GameObject camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            
            ImageConfig.TargetCamera = camera.GetComponent<Camera>();
            return this;
        }
        
        /// <summary>
        /// Sets the target camera by component
        /// </summary>
        /// <param name="camera">Target camera component</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithTargetCamera(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            
            return WithTargetCamera(camera.gameObject);
        }
        
        /// <summary>
        /// Sets the target texture
        /// </summary>
        /// <param name="renderTexture">Target render texture</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithTargetTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
                throw new ArgumentNullException(nameof(renderTexture));
            
            ImageConfig.RenderTexture = renderTexture;
            ImageConfig.SourceType = ImageRecorderSourceType.RenderTexture;
            return this;
        }
        
        /// <summary>
        /// Sets the EXR compression type
        /// </summary>
        /// <param name="compression">EXR compression type</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithEXRCompression(CompressionUtility.EXRCompressionType compression)
        {
            ImageConfig.ExrCompression = compression;
            return this;
        }
        
        /// <summary>
        /// Sets the frame padding for sequential numbering
        /// </summary>
        /// <param name="padding">Number of digits for frame numbers</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithFramePadding(int padding)
        {
            if (padding < 1 || padding > 10)
                throw new ArgumentOutOfRangeException(nameof(padding), "Frame padding must be between 1 and 10");
            
            ImageConfig.FramePadding = padding;
            return this;
        }
        
        /// <summary>
        /// Applies a quality preset
        /// </summary>
        /// <param name="preset">Quality preset</param>
        /// <returns>Builder instance for chaining</returns>
        public ImageRecorderBuilder WithPreset(ImageQualityPreset preset)
        {
            switch (preset)
            {
                case ImageQualityPreset.Preview:
                    WithResolution(1280, 720)
                        .WithFormat(ImageFormat.JPEG)
                        .WithJpegQuality(75);
                    break;
                    
                case ImageQualityPreset.Standard:
                    WithResolution(1920, 1080)
                        .WithFormat(ImageFormat.PNG);
                    break;
                    
                case ImageQualityPreset.High:
                    WithResolution(3840, 2160)
                        .WithFormat(ImageFormat.PNG);
                    break;
                    
                case ImageQualityPreset.HDR:
                    WithResolution(1920, 1080)
                        .WithFormat(ImageFormat.EXR)
                        .WithEXRCompression(CompressionUtility.EXRCompressionType.Zip);
                    break;
                    
                case ImageQualityPreset.Alpha:
                    WithResolution(1920, 1080)
                        .WithFormat(ImageFormat.PNG)
                        .WithAlpha(true);
                    break;
            }
            
            return this;
        }
        
        /// <summary>
        /// Validates the image recorder configuration
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();
            
            if (ImageConfig.Width <= 0 || ImageConfig.Height <= 0)
                throw new InvalidOperationException("Invalid resolution settings");
            
            if (ImageConfig.FrameRate <= 0)
                throw new InvalidOperationException("Invalid frame rate");
            
            // Validate format compatibility
            if (ImageConfig.CaptureAlpha && ImageConfig.Format == ImageFormat.JPEG)
                throw new InvalidOperationException("JPEG format does not support alpha channel");
            
            if (ImageConfig.Format == ImageFormat.EXR && ImageConfig.CaptureAlpha)
                ImageConfig.ColorSpace = ColorSpace.Linear;
            
            if (string.IsNullOrWhiteSpace(ImageConfig.FileNamePattern))
                ImageConfig.FileNamePattern = "Image_<Take>_<Frame>";
        }
    }
    
    /// <summary>
    /// Image quality presets
    /// </summary>
    public enum ImageQualityPreset
    {
        Preview,    // 720p JPEG for quick previews
        Standard,   // 1080p PNG
        High,       // 4K PNG
        HDR,        // 1080p EXR with HDR
        Alpha       // 1080p PNG with alpha
    }
}