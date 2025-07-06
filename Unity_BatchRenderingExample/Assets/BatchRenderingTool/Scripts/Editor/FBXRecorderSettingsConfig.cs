using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration for FBX Recorder settings
    /// </summary>
    public class FBXRecorderSettingsConfig
    {
        public GameObject targetGameObject = null;
        public bool recordHierarchy = true;
        public bool clampedTangents = true;
        public FBXAnimationCompressionLevel animationCompression = FBXAnimationCompressionLevel.Lossy;
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
                
                // Configure AnimationInputSettings if targetGameObject is set
                if (targetGameObject != null)
                {
                    var animInputSettingsField = settingsType.GetField("m_AnimationInputSettings", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (animInputSettingsField != null)
                    {
                        var animInputSettings = animInputSettingsField.GetValue(settings);
                        if (animInputSettings != null)
                        {
                            var animType = animInputSettings.GetType();
                            
                            // Set GameObject
                            var gameObjectProp = animType.GetProperty("gameObject");
                            if (gameObjectProp != null && gameObjectProp.CanWrite)
                            {
                                gameObjectProp.SetValue(animInputSettings, targetGameObject);
                                BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set target GameObject to {targetGameObject.name}");
                            }
                            
                            // Set Recursive (Record Hierarchy)
                            var recursiveProp = animType.GetProperty("Recursive");
                            if (recursiveProp != null && recursiveProp.CanWrite)
                            {
                                recursiveProp.SetValue(animInputSettings, recordHierarchy);
                                BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set Recursive to {recordHierarchy}");
                            }
                            
                            // Set ClampedTangents
                            var clampedTangentsProp = animType.GetProperty("ClampedTangents");
                            if (clampedTangentsProp != null && clampedTangentsProp.CanWrite)
                            {
                                clampedTangentsProp.SetValue(animInputSettings, clampedTangents);
                                BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set ClampedTangents to {clampedTangents}");
                            }
                            
                            // Set SimplyCurves (Animation Compression)
                            var simplyCurvesField = animType.GetField("m_SimplyCurves", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (simplyCurvesField != null)
                            {
                                // Map AnimationCompressionLevel to CurveSimplificationOptions
                                var curveSimplificationOptionsType = simplyCurvesField.FieldType;
                                object compressionValue = null;
                                
                                switch (animationCompression)
                                {
                                    case FBXAnimationCompressionLevel.Lossy:
                                        compressionValue = System.Enum.Parse(curveSimplificationOptionsType, "Lossy");
                                        break;
                                    case FBXAnimationCompressionLevel.Lossless:
                                        compressionValue = System.Enum.Parse(curveSimplificationOptionsType, "Lossless");
                                        break;
                                    case FBXAnimationCompressionLevel.Disabled:
                                        compressionValue = System.Enum.Parse(curveSimplificationOptionsType, "Disabled");
                                        break;
                                }
                                
                                if (compressionValue != null)
                                {
                                    simplyCurvesField.SetValue(animInputSettings, compressionValue);
                                    BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set SimplyCurves to {compressionValue}");
                                }
                            }
                        }
                    }
                }
                
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