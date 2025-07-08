using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Recorder;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration for FBX Recorder settings
    /// </summary>
    public class FBXRecorderSettingsConfig
    {
        public GameObject targetGameObject = null;
        public FBXRecordedComponent recordedComponent = FBXRecordedComponent.Camera;
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
            
            // targetGameObject is required for FBX recording
            if (targetGameObject == null)
            {
                errorMessage = "Target GameObject must be set for FBX recording";
                return false;
            }
            
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
                        targetGameObject = null,
                        recordedComponent = FBXRecordedComponent.Camera,
                        recordHierarchy = true,
                        clampedTangents = true,
                        animationCompression = FBXAnimationCompressionLevel.Lossy,
                        exportGeometry = false,
                        transferAnimationSource = null,
                        transferAnimationDest = null,
                        frameRate = 24f
                    };
                    
                case FBXExportPreset.ModelExport:
                    return new FBXRecorderSettingsConfig
                    {
                        targetGameObject = null,
                        recordedComponent = FBXRecordedComponent.Transform,
                        recordHierarchy = true,
                        clampedTangents = true,
                        animationCompression = FBXAnimationCompressionLevel.Lossy,
                        exportGeometry = true,
                        transferAnimationSource = null,
                        transferAnimationDest = null,
                        frameRate = 24f
                    };
                    
                case FBXExportPreset.ModelAndAnimation:
                    return new FBXRecorderSettingsConfig
                    {
                        targetGameObject = null,
                        recordedComponent = FBXRecordedComponent.Camera,
                        recordHierarchy = true,
                        clampedTangents = true,
                        animationCompression = FBXAnimationCompressionLevel.Lossy,
                        exportGeometry = true,
                        transferAnimationSource = null,
                        transferAnimationDest = null,
                        frameRate = 24f
                    };
                    
                default:
                    return new FBXRecorderSettingsConfig
                    {
                        targetGameObject = null,
                        recordedComponent = FBXRecordedComponent.Camera,
                        recordHierarchy = true,
                        clampedTangents = true,
                        animationCompression = FBXAnimationCompressionLevel.Lossy,
                        exportGeometry = true,
                        transferAnimationSource = null,
                        transferAnimationDest = null,
                        frameRate = 24f
                    };
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
                if (settings != null)
                {
                    // Log what type was actually created
                    BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] CreateProperRecorderSettings created type: {settings.GetType().FullName}");
                }
                else
                {
                    BatchRenderingToolLogger.LogWarning("[FBXRecorderSettingsConfig] CreateProperRecorderSettings failed, trying CreateProperFBXRecorderSettings...");
                    settings = RecorderClipUtility.CreateProperFBXRecorderSettings(name);
                    
                    if (settings == null)
                    {
                        BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] Failed to create FBX recorder settings - all methods failed");
                        BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] Please ensure the Unity FBX Exporter package (com.unity.formats.fbx) is installed");
                        return null;
                    }
                }
                
                settings.name = name;
                
                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] FBX Recorder Settings created successfully: {settings.GetType().FullName}");
                
                // IMPORTANT: First, we need to ensure the AnimationInputSettings is properly initialized
                var settingsType = settings.GetType();
                
                // NOTE: Unity's FBX Recorder does not have a direct RecordedComponent property
                // Instead, the component type is determined by what component is set in AnimationInputSettings
                // So we'll skip trying to set RecordedComponent directly and let the AnimationInputSettings handle it
                
                // Apply configuration using reflection
                
                // IMPORTANT: Initialize InputSettings first
                // FBX Recorder requires proper initialization of input settings
                var inputSettingsProperty = settingsType.GetProperty("InputSettings", BindingFlags.Public | BindingFlags.Instance);
                if (inputSettingsProperty != null)
                {
                    var inputSettings = inputSettingsProperty.GetValue(settings);
                    BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] InputSettings type: {(inputSettings != null ? inputSettings.GetType().FullName : "null")}");
                }
                
                // Try to get AnimationInputSettings through property first
                var animInputSettingsProp = settingsType.GetProperty("AnimationInputSettings");
                if (animInputSettingsProp != null && animInputSettingsProp.CanRead)
                {
                    var animInputSettings = animInputSettingsProp.GetValue(settings);
                    if (animInputSettings != null)
                    {
                        BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Found AnimationInputSettings via property: {animInputSettings.GetType().FullName}");
                        ConfigureAnimationInputSettings(animInputSettings, targetGameObject, recordedComponent, recordHierarchy, clampedTangents);
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] AnimationInputSettings property returned null");
                    }
                }
                else
                {
                    // Fallback to field access
                    BatchRenderingToolLogger.Log("[FBXRecorderSettingsConfig] AnimationInputSettings property not found, trying field access...");
                    var animInputSettingsField = settingsType.GetField("m_AnimationInputSettings", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (animInputSettingsField != null)
                    {
                        var animInputSettings = animInputSettingsField.GetValue(settings);
                        
                        // If AnimationInputSettings is null, try to create it
                        if (animInputSettings == null)
                        {
                            BatchRenderingToolLogger.LogVerbose("[FBXRecorderSettingsConfig] AnimationInputSettings is null, attempting to create...");
                            var animInputType = animInputSettingsField.FieldType;
                            try
                            {
                                animInputSettings = System.Activator.CreateInstance(animInputType);
                                animInputSettingsField.SetValue(settings, animInputSettings);
                                BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Created AnimationInputSettings of type: {animInputType.FullName}");
                            }
                            catch (System.Exception ex)
                            {
                                BatchRenderingToolLogger.LogError($"[FBXRecorderSettingsConfig] Failed to create AnimationInputSettings: {ex.Message}");
                            }
                        }
                        
                        if (animInputSettings != null)
                        {
                            BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Found AnimationInputSettings via field: {animInputSettings.GetType().FullName}");
                            
                            // CRITICAL: Configure the AnimationInputSettings with GameObject and components
                            ConfigureAnimationInputSettings(animInputSettings, targetGameObject, recordedComponent, recordHierarchy, clampedTangents);
                            
                            // Set SimplyCurves (Animation Compression) - not handled in ConfigureAnimationInputSettings
                            var animType = animInputSettings.GetType();
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
                            
                            // IMPORTANT: Verify that gameObject was set correctly
                            var goProperty = animType.GetProperty("gameObject");
                            if (goProperty != null && goProperty.CanRead)
                            {
                                var go = goProperty.GetValue(animInputSettings) as GameObject;
                                if (go == null)
                                {
                                    BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] CRITICAL: GameObject is still null after configuration!");
                                }
                                else
                                {
                                    BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] SUCCESS: GameObject is set to {go.name}");
                                }
                            }
                        }
                        else
                        {
                            BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] AnimationInputSettings is null and could not be created");
                        }
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] Could not find m_AnimationInputSettings field");
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
                BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Before setting frame rate: {settings.FrameRate}");
                settings.FrameRate = frameRate;
                BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] After setting frame rate: {settings.FrameRate} (expected: {frameRate})");
                
                BatchRenderingToolLogger.LogVerbose("[FBXRecorderSettingsConfig] FBX recorder settings created successfully");
                
                
                return settings;
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[FBXRecorderSettingsConfig] Exception creating FBX recorder settings: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Configure AnimationInputSettings with proper values
        /// </summary>
        private static void ConfigureAnimationInputSettings(object animInputSettings, GameObject targetGameObject, FBXRecordedComponent recordedComponent, bool recordHierarchy, bool clampedTangents)
        {
            BatchRenderingToolLogger.Log($"[ConfigureAnimationInputSettings] Called with targetGameObject: {(targetGameObject != null ? targetGameObject.name : "NULL")}");
            
            if (animInputSettings == null)
            {
                BatchRenderingToolLogger.LogError("[ConfigureAnimationInputSettings] animInputSettings is null!");
                return;
            }
            
            if (targetGameObject == null)
            {
                BatchRenderingToolLogger.LogError("[ConfigureAnimationInputSettings] targetGameObject is null! FBX recording will fail!");
                return;
            }
                
            var animType = animInputSettings.GetType();
            
            // Set GameObject
            var gameObjectProp = animType.GetProperty("gameObject");
            if (gameObjectProp != null && gameObjectProp.CanWrite)
            {
                gameObjectProp.SetValue(animInputSettings, targetGameObject);
                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Set target GameObject to {targetGameObject.name}");
            }
            
            // Add component to record using AddComponentToRecord method
            var addComponentMethod = animType.GetMethod("AddComponentToRecord");
            if (addComponentMethod != null)
            {
                try
                {
                    // Check if no components are selected
                    if (recordedComponent == FBXRecordedComponent.None)
                    {
                        BatchRenderingToolLogger.LogWarning("[FBXRecorderSettingsConfig] No components selected for recording. Defaulting to Transform.");
                        addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Transform) });
                        BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added Transform to recorded components (default)");
                    }
                    else
                    {
                        // Process each selected component flag
                        int addedComponents = 0;
                        
                        // Transform
                        if ((recordedComponent & FBXRecordedComponent.Transform) != 0)
                        {
                            addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Transform) });
                            BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added Transform to recorded components");
                            addedComponents++;
                        }
                        
                        // Camera
                        if ((recordedComponent & FBXRecordedComponent.Camera) != 0)
                        {
                            var camera = targetGameObject.GetComponent<Camera>();
                            if (camera != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Camera) });
                                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added Camera to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                BatchRenderingToolLogger.LogWarning($"[FBXRecorderSettingsConfig] Camera component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // Light
                        if ((recordedComponent & FBXRecordedComponent.Light) != 0)
                        {
                            var light = targetGameObject.GetComponent<Light>();
                            if (light != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Light) });
                                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added Light to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                BatchRenderingToolLogger.LogWarning($"[FBXRecorderSettingsConfig] Light component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // MeshRenderer
                        if ((recordedComponent & FBXRecordedComponent.MeshRenderer) != 0)
                        {
                            var meshRenderer = targetGameObject.GetComponent<MeshRenderer>();
                            if (meshRenderer != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(MeshRenderer) });
                                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added MeshRenderer to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                BatchRenderingToolLogger.LogWarning($"[FBXRecorderSettingsConfig] MeshRenderer component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // SkinnedMeshRenderer
                        if ((recordedComponent & FBXRecordedComponent.SkinnedMeshRenderer) != 0)
                        {
                            var skinnedMeshRenderer = targetGameObject.GetComponent<SkinnedMeshRenderer>();
                            if (skinnedMeshRenderer != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(SkinnedMeshRenderer) });
                                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added SkinnedMeshRenderer to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                BatchRenderingToolLogger.LogWarning($"[FBXRecorderSettingsConfig] SkinnedMeshRenderer component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // Animator
                        if ((recordedComponent & FBXRecordedComponent.Animator) != 0)
                        {
                            var animator = targetGameObject.GetComponent<Animator>();
                            if (animator != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Animator) });
                                BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added Animator to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                BatchRenderingToolLogger.LogWarning($"[FBXRecorderSettingsConfig] Animator component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // If no components were added (all selected components were missing), add Transform as fallback
                        if (addedComponents == 0)
                        {
                            BatchRenderingToolLogger.LogWarning("[FBXRecorderSettingsConfig] No selected components found on target. Adding Transform as fallback.");
                            addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Transform) });
                            BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Added Transform to recorded components (fallback)");
                        }
                        
                        BatchRenderingToolLogger.Log($"[FBXRecorderSettingsConfig] Total components added: {addedComponents}");
                    }
                }
                catch (Exception ex)
                {
                    BatchRenderingToolLogger.LogError($"[FBXRecorderSettingsConfig] Failed to add component to record: {ex.Message}");
                }
            }
            else
            {
                BatchRenderingToolLogger.LogError("[FBXRecorderSettingsConfig] AddComponentToRecord method not found on AnimationInputSettings");
            }
            
            // Set basic properties
            SetPropertyIfExists(animInputSettings, "Recursive", recordHierarchy);
            SetPropertyIfExists(animInputSettings, "ClampedTangents", clampedTangents);
        }
        
        private static void SetPropertyIfExists(object obj, string propertyName, object value)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    prop.SetValue(obj, value);
                    BatchRenderingToolLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set {propertyName} to {value}");
                }
                catch (Exception ex)
                {
                    BatchRenderingToolLogger.LogWarning($"[FBXRecorderSettingsConfig] Could not set {propertyName}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public FBXRecorderSettingsConfig Clone()
        {
            return new FBXRecorderSettingsConfig
            {
                targetGameObject = this.targetGameObject,
                recordedComponent = this.recordedComponent,
                recordHierarchy = this.recordHierarchy,
                clampedTangents = this.clampedTangents,
                animationCompression = this.animationCompression,
                exportGeometry = this.exportGeometry,
                transferAnimationSource = this.transferAnimationSource,
                transferAnimationDest = this.transferAnimationDest,
                frameRate = this.frameRate
            };
        }
        
    }
}