using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration class for AOVRecorderSettings
    /// </summary>
    [Serializable]
    public class AOVRecorderSettingsConfig
    {
        // Selected AOV types (using flags for multiple selection)
        public AOVType selectedAOVs = AOVType.None;
        
        // Output settings
        public AOVOutputFormat outputFormat = AOVOutputFormat.EXR16;
        public bool compressionEnabled = true;
        
        // Resolution settings
        public int width = 1920;
        public int height = 1080;
        
        // Frame rate settings
        public int frameRate = 24;
        public bool capFrameRate = true;
        
        // Advanced settings
        public bool flipVertical = false;
        public string customPassName = "";
        
        /// <summary>
        /// Get list of selected AOV types
        /// </summary>
        public List<AOVType> GetSelectedAOVsList()
        {
            var result = new List<AOVType>();
            
            foreach (AOVType aov in Enum.GetValues(typeof(AOVType)))
            {
                if (aov != AOVType.None && (selectedAOVs & aov) == aov)
                {
                    result.Add(aov);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check if HDRP is available
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                errorMessage = "AOV Recorder requires HDRP (High Definition Render Pipeline) package";
                return false;
            }
            
            // Check if any AOV is selected
            if (selectedAOVs == AOVType.None)
            {
                errorMessage = "At least one AOV type must be selected";
                return false;
            }
            
            // Validate resolution
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
            
            // Validate frame rate
            if (frameRate <= 0 || frameRate > 120)
            {
                errorMessage = "Frame rate must be between 1 and 120";
                return false;
            }
            
            // Validate custom pass name if CustomPass is selected
            if ((selectedAOVs & AOVType.CustomPass) == AOVType.CustomPass && 
                string.IsNullOrEmpty(customPassName))
            {
                errorMessage = "Custom pass name is required when Custom Pass AOV is selected";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Create AOVRecorderSettings for each selected AOV type
        /// Note: Unity Recorder API might require separate recorder instances for each AOV
        /// </summary>
        public List<RecorderSettings> CreateAOVRecorderSettings(string baseName)
        {
            var settings = new List<RecorderSettings>();
            var selectedList = GetSelectedAOVsList();
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsConfig] Creating AOV settings for {selectedList.Count} selected AOVs");
            
            // Note: The actual implementation depends on Unity Recorder's AOV API
            // This is a placeholder structure that would need to be adapted to the actual API
            foreach (var aovType in selectedList)
            {
                var aovSettings = CreateSingleAOVRecorderSettings(baseName, aovType);
                if (aovSettings != null)
                {
                    settings.Add(aovSettings);
                }
            }
            
            return settings;
        }
        
        /// <summary>
        /// Create RecorderSettings for a single AOV type
        /// </summary>
        private RecorderSettings CreateSingleAOVRecorderSettings(string baseName, AOVType aovType)
        {
            // Note: This is a conceptual implementation
            // The actual Unity Recorder AOV API might be different
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsConfig] Creating settings for AOV: {aovType}");
            
            // For now, we'll create ImageRecorderSettings as a placeholder
            // In actual implementation, this would use AOVRecorderSettings class
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = $"{baseName}_AOV_{aovType}";
            
            // Configure basic settings
            settings.Enabled = true;
            settings.RecordMode = RecordMode.Manual;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = frameRate;
            settings.CapFrameRate = capFrameRate;
            
            // Configure output format based on AOV output format
            switch (outputFormat)
            {
                case AOVOutputFormat.EXR16:
                case AOVOutputFormat.EXR32:
                    settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                    break;
                case AOVOutputFormat.PNG16:
                    // PNG is not available in ImageRecorderSettings, use JPEG as fallback
                    settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.JPEG;
                    break;
                case AOVOutputFormat.TGA:
                    settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.JPEG;
                    break;
            }
            
            // Configure resolution
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height,
                FlipFinalOutput = flipVertical
            };
            
            // Note: In actual implementation, we would set AOV-specific properties here
            // For example:
            // settings.AOVType = ConvertToUnityAOVType(aovType);
            // settings.BitDepth = (outputFormat == AOVOutputFormat.EXR32) ? 32 : 16;
            
            return settings;
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public AOVRecorderSettingsConfig Clone()
        {
            return new AOVRecorderSettingsConfig
            {
                selectedAOVs = this.selectedAOVs,
                outputFormat = this.outputFormat,
                compressionEnabled = this.compressionEnabled,
                width = this.width,
                height = this.height,
                frameRate = this.frameRate,
                capFrameRate = this.capFrameRate,
                flipVertical = this.flipVertical,
                customPassName = this.customPassName
            };
        }
        
        /// <summary>
        /// Get recommended presets for common AOV workflows
        /// </summary>
        public static class Presets
        {
            public static AOVRecorderSettingsConfig GetCompositing()
            {
                return new AOVRecorderSettingsConfig
                {
                    selectedAOVs = AOVType.Albedo | AOVType.DirectDiffuse | AOVType.DirectSpecular | 
                                  AOVType.IndirectDiffuse | AOVType.IndirectSpecular | AOVType.Depth | 
                                  AOVType.Normal | AOVType.MotionVectors,
                    outputFormat = AOVOutputFormat.EXR16,
                    compressionEnabled = true,
                    width = 1920,
                    height = 1080,
                    frameRate = 24
                };
            }
            
            public static AOVRecorderSettingsConfig GetGeometryOnly()
            {
                return new AOVRecorderSettingsConfig
                {
                    selectedAOVs = AOVType.Depth | AOVType.Normal | AOVType.MotionVectors,
                    outputFormat = AOVOutputFormat.EXR32,
                    compressionEnabled = false,
                    width = 1920,
                    height = 1080,
                    frameRate = 24
                };
            }
            
            public static AOVRecorderSettingsConfig GetLightingOnly()
            {
                return new AOVRecorderSettingsConfig
                {
                    selectedAOVs = AOVType.DirectDiffuse | AOVType.DirectSpecular | 
                                  AOVType.IndirectDiffuse | AOVType.IndirectSpecular | 
                                  AOVType.Emissive | AOVType.Shadow,
                    outputFormat = AOVOutputFormat.EXR16,
                    compressionEnabled = true,
                    width = 1920,
                    height = 1080,
                    frameRate = 24
                };
            }
            
            public static AOVRecorderSettingsConfig GetMaterialProperties()
            {
                return new AOVRecorderSettingsConfig
                {
                    selectedAOVs = AOVType.Albedo | AOVType.Specular | AOVType.Smoothness | 
                                  AOVType.Metal | AOVType.AmbientOcclusion,
                    outputFormat = AOVOutputFormat.EXR16,
                    compressionEnabled = true,
                    width = 1920,
                    height = 1080,
                    frameRate = 24
                };
            }
        }
    }
    
    /// <summary>
    /// AOV preset types for quick configuration
    /// </summary>
    public enum AOVPreset
    {
        Custom,
        Compositing,
        GeometryOnly,
        LightingOnly,
        MaterialProperties
    }
}