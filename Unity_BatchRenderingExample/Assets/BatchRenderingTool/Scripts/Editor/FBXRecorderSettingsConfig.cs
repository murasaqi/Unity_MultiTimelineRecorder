using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration for FBX Recorder settings
    /// </summary>
    public class FBXRecorderSettingsConfig
    {
        public bool exportGeometry = true;
        public Transform transferAnimationSource = null;
        public Transform transferAnimationDest = null;
        public float frameRate = 24f;
        
        /// <summary>
        /// Validate the configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (frameRate <= 0)
            {
                errorMessage = "Frame rate must be greater than 0";
                return false;
            }
            
            // If transfer animation is enabled, validate the transforms
            if (transferAnimationSource != null && transferAnimationDest == null)
            {
                errorMessage = "Animation destination transform must be set when source is specified";
                return false;
            }
            
            if (transferAnimationSource == null && transferAnimationDest != null)
            {
                errorMessage = "Animation source transform must be set when destination is specified";
                return false;
            }
            
            // Validate that source and destination are not the same
            if (transferAnimationSource != null && transferAnimationDest != null)
            {
                if (transferAnimationSource == transferAnimationDest)
                {
                    errorMessage = "Animation source and destination cannot be the same transform";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get preset configuration
        /// </summary>
        public static FBXRecorderSettingsConfig GetPreset(FBXExportPreset preset)
        {
            switch (preset)
            {
                case FBXExportPreset.AnimationExport:
                    return new FBXRecorderSettingsConfig
                    {
                        exportGeometry = false,
                        frameRate = 24f
                    };
                    
                case FBXExportPreset.ModelExport:
                    return new FBXRecorderSettingsConfig
                    {
                        exportGeometry = true,
                        frameRate = 24f
                    };
                    
                case FBXExportPreset.ModelAndAnimation:
                    return new FBXRecorderSettingsConfig
                    {
                        exportGeometry = true,
                        frameRate = 24f
                    };
                    
                default:
                    return new FBXRecorderSettingsConfig();
            }
        }
        
        /// <summary>
        /// Create FBXRecorderSettings based on this configuration
        /// </summary>
        public UnityEditor.Recorder.RecorderSettings CreateFBXRecorderSettings(string name)
        {
            BatchRenderingToolLogger.LogVerbose("[FBXRecorderSettingsConfig] Creating FBX recorder settings");
            
            try
            {
                // Use RecorderClipUtility to create the settings properly
                var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
                if (settings == null)
                {
                    BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] Failed to create FBX recorder settings");
                    return null;
                }
                
                settings.name = name;
                
                // Apply configuration using reflection
                var settingsType = settings.GetType();
                
                // Set ExportGeometry
                var exportGeometryProp = settingsType.GetProperty("ExportGeometry");
                if (exportGeometryProp != null && exportGeometryProp.CanWrite)
                {
                    exportGeometryProp.SetValue(settings, exportGeometry);
                    BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set ExportGeometry to {exportGeometry}");
                }
                
                // Set TransferAnimationSource
                if (transferAnimationSource != null)
                {
                    var transferSourceProp = settingsType.GetProperty("TransferAnimationSource");
                    if (transferSourceProp != null && transferSourceProp.CanWrite)
                    {
                        transferSourceProp.SetValue(settings, transferAnimationSource);
                        BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set TransferAnimationSource to {transferAnimationSource.name}");
                    }
                }
                
                // Set TransferAnimationDest
                if (transferAnimationDest != null)
                {
                    var transferDestProp = settingsType.GetProperty("TransferAnimationDest");
                    if (transferDestProp != null && transferDestProp.CanWrite)
                    {
                        transferDestProp.SetValue(settings, transferAnimationDest);
                        BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set TransferAnimationDest to {transferAnimationDest.name}");
                    }
                }
                
                // Set frame rate
                settings.FrameRate = frameRate;
                
                BatchRenderingToolLogger.LogVerbose("[FBXRecorderSettingsConfig] FBX recorder settings created successfully");
                
                return settings;
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[FBXRecorderSettingsConfig] Exception creating FBX recorder settings: {e.Message}");
                return null;
            }
        }
    }
}