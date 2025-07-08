using System;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration class for ImageRecorderSettings
    /// </summary>
    [Serializable]
    public class ImageRecorderSettingsConfig
    {
        public ImageRecorderSettings.ImageRecorderOutputFormat imageFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        public int jpegQuality = 75;
        public int width = 1920;
        public int height = 1080;
        public int frameRate = 24;
        public bool captureAlpha = false;
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (width <= 0 || height <= 0)
            {
                errorMessage = "Invalid resolution: width and height must be positive";
                return false;
            }
            
            if (width > 8192 || height > 8192)
            {
                errorMessage = "Resolution exceeds maximum supported (8192x8192)";
                return false;
            }
            
            if (frameRate <= 0 || frameRate > 120)
            {
                errorMessage = "Frame rate must be between 1 and 120";
                return false;
            }
            
            if (imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG && 
                (jpegQuality < 1 || jpegQuality > 100))
            {
                errorMessage = "JPEG quality must be between 1 and 100";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Apply settings to ImageRecorderSettings
        /// </summary>
        public void ApplyToSettings(ImageRecorderSettings settings)
        {
            settings.OutputFormat = imageFormat;
            settings.CaptureAlpha = captureAlpha;
            
            // Configure input settings
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height,
                FlipFinalOutput = false
            };
            
            // JPEG specific settings
            if (imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                // Note: JPEG quality is set through reflection or other means in Unity Recorder
                // as there's no direct API for it in some versions
            }
            
            // Frame rate settings
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public ImageRecorderSettingsConfig Clone()
        {
            return new ImageRecorderSettingsConfig
            {
                imageFormat = this.imageFormat,
                jpegQuality = this.jpegQuality,
                width = this.width,
                height = this.height,
                frameRate = this.frameRate,
                captureAlpha = this.captureAlpha
            };
        }
    }
}