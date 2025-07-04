using System;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    // Unity Recorder doesn't expose VideoBitrateMode in the namespace, so we define our own
    public enum VideoBitrateMode
    {
        Low,
        Medium,
        High
    }
    /// <summary>
    /// Configuration class for MovieRecorderSettings
    /// </summary>
    [Serializable]
    public class MovieRecorderSettingsConfig
    {
        // Video settings
        public MovieRecorderSettings.VideoRecorderOutputFormat outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        public VideoBitrateMode videoBitrateMode = VideoBitrateMode.High;
        
        // Custom bitrate settings (when using Low mode)
        public int customBitrate = 15000; // in kbps
        
        // Resolution settings
        public int width = 1920;
        public int height = 1080;
        
        // Frame rate settings
        public int frameRate = 24;
        public bool capFrameRate = true;
        
        // Audio settings
        public bool captureAudio = false;
        // Note: Unity Recorder API doesn't expose detailed audio settings
        public int audioBitrate = 192; // in kbps
        
        // Alpha channel
        public bool captureAlpha = false;
        
        // Advanced settings
        public bool flipVertical = false;
        
        /// <summary>
        /// Apply configuration to MovieRecorderSettings
        /// </summary>
        public void ApplyToSettings(MovieRecorderSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            
            // Video settings
            settings.OutputFormat = outputFormat;
            
            // Note: Unity Recorder API doesn't expose direct quality/bitrate control in Editor
            // The videoBitrateMode is used for our internal logic only
            // Actual encoding quality is controlled by the system's media encoder
            if (videoBitrateMode == VideoBitrateMode.Low)
            {
                Debug.Log($"Low quality mode selected (target bitrate: {customBitrate} kbps)");
            }
            
            // Resolution
            settings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height,
                FlipFinalOutput = flipVertical
            };
            
            // Frame rate
            settings.FrameRate = frameRate;
            settings.CapFrameRate = capFrameRate;
            
            // Audio settings
            settings.CaptureAudio = captureAudio;
            if (captureAudio && settings.AudioInputSettings != null)
            {
                settings.AudioInputSettings.PreserveAudio = true;
                // Additional audio configuration if exposed by API
            }
            
            // Alpha channel
            settings.CaptureAlpha = captureAlpha;
            
            // Common settings
            settings.RecordMode = RecordMode.Manual;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
        }
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Validate resolution
            if (width <= 0 || height <= 0)
            {
                errorMessage = "Invalid resolution: width and height must be positive";
                return false;
            }
            
            if (width > 4096 || height > 4096)
            {
                errorMessage = "Resolution exceeds maximum supported (4096x4096)";
                return false;
            }
            
            // Validate frame rate
            if (frameRate <= 0 || frameRate > 120)
            {
                errorMessage = "Frame rate must be between 1 and 120";
                return false;
            }
            
            // Validate custom bitrate
            if (videoBitrateMode == VideoBitrateMode.Low && customBitrate <= 0)
            {
                errorMessage = "Custom bitrate must be positive when using Low quality mode";
                return false;
            }
            
            // Platform-specific validation
            if (outputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MOV)
            {
                #if !UNITY_EDITOR_OSX
                errorMessage = "MOV format is only supported on macOS";
                return false;
                #endif
            }
            
            // Alpha channel validation
            if (captureAlpha)
            {
                if (outputFormat != MovieRecorderSettings.VideoRecorderOutputFormat.MOV &&
                    outputFormat != MovieRecorderSettings.VideoRecorderOutputFormat.WebM)
                {
                    errorMessage = "Alpha channel is only supported with MOV (ProRes 4444) or WebM formats";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get recommended settings for common use cases
        /// </summary>
        public static MovieRecorderSettingsConfig GetPreset(MovieRecorderPreset preset)
        {
            var config = new MovieRecorderSettingsConfig();
            
            switch (preset)
            {
                case MovieRecorderPreset.HighQuality1080p:
                    config.width = 1920;
                    config.height = 1080;
                    config.frameRate = 30;
                    config.videoBitrateMode = VideoBitrateMode.High;
                    config.outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                    config.captureAudio = true;
                    break;
                    
                case MovieRecorderPreset.HighQuality4K:
                    config.width = 3840;
                    config.height = 2160;
                    config.frameRate = 30;
                    config.videoBitrateMode = VideoBitrateMode.High;
                    config.outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                    config.captureAudio = true;
                    break;
                    
                case MovieRecorderPreset.WebOptimized:
                    config.width = 1280;
                    config.height = 720;
                    config.frameRate = 30;
                    config.videoBitrateMode = VideoBitrateMode.Medium;
                    config.outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.WebM;
                    config.captureAudio = true;
                    break;
                    
                case MovieRecorderPreset.ProResWithAlpha:
                    config.width = 1920;
                    config.height = 1080;
                    config.frameRate = 24;
                    config.videoBitrateMode = VideoBitrateMode.High;
                    config.outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MOV;
                    config.captureAlpha = true;
                    config.captureAudio = false;
                    break;
                    
                case MovieRecorderPreset.LowFileSize:
                    config.width = 1280;
                    config.height = 720;
                    config.frameRate = 24;
                    config.videoBitrateMode = VideoBitrateMode.Low;
                    config.customBitrate = 5000;
                    config.outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                    config.captureAudio = false;
                    break;
            }
            
            return config;
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public MovieRecorderSettingsConfig Clone()
        {
            return new MovieRecorderSettingsConfig
            {
                outputFormat = this.outputFormat,
                videoBitrateMode = this.videoBitrateMode,
                customBitrate = this.customBitrate,
                width = this.width,
                height = this.height,
                frameRate = this.frameRate,
                capFrameRate = this.capFrameRate,
                captureAudio = this.captureAudio,
                audioBitrate = this.audioBitrate,
                captureAlpha = this.captureAlpha,
                flipVertical = this.flipVertical
            };
        }
    }
    
    /// <summary>
    /// Preset configurations for MovieRecorderSettings
    /// </summary>
    public enum MovieRecorderPreset
    {
        HighQuality1080p,
        HighQuality4K,
        WebOptimized,
        ProResWithAlpha,
        LowFileSize,
        Custom
    }
}