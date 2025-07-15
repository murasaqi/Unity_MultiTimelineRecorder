using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Recorder;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Configuration for FBX Recorder settings
    /// </summary>
    [Serializable]
    public class FBXRecorderSettingsConfig
    {
        // GameObject参照を保持するためのGameObjectReference
        [SerializeField]
        private GameObjectReference targetGameObjectRef = new GameObjectReference();
        
        public GameObject targetGameObject 
        { 
            get { return targetGameObjectRef?.GameObject; }
            set { if (targetGameObjectRef == null) targetGameObjectRef = new GameObjectReference(); targetGameObjectRef.GameObject = value; }
        }
        
        public FBXRecordedComponent recordedComponent = FBXRecordedComponent.Camera;
        public bool recordHierarchy = true;
        public bool clampedTangents = true;
        public FBXAnimationCompressionLevel animationCompression = FBXAnimationCompressionLevel.Lossy;
        public bool exportGeometry = true;
        
        // Transform参照もGameObjectReferenceで管理
        [SerializeField]
        private GameObjectReference transferAnimationSourceRef = new GameObjectReference();
        [SerializeField]
        private GameObjectReference transferAnimationDestRef = new GameObjectReference();
        
        public Transform transferAnimationSource 
        { 
            get { return transferAnimationSourceRef?.GetTransform(); }
            set { if (transferAnimationSourceRef == null) transferAnimationSourceRef = new GameObjectReference(); transferAnimationSourceRef.GameObject = value != null ? value.gameObject : null; }
        }
        
        public Transform transferAnimationDest
        { 
            get { return transferAnimationDestRef?.GetTransform(); }
            set { if (transferAnimationDestRef == null) transferAnimationDestRef = new GameObjectReference(); transferAnimationDestRef.GameObject = value != null ? value.gameObject : null; }
        }
        
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
            MultiTimelineRecorderLogger.LogVerbose("[FBXRecorderSettingsConfig] Creating FBX recorder settings");
            
            try
            {
                // Use RecorderClipUtility to create the settings properly
                var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
                if (settings != null)
                {
                    // Log what type was actually created
                    MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] CreateProperRecorderSettings created type: {settings.GetType().FullName}");
                }
                else
                {
                    MultiTimelineRecorderLogger.LogWarning("[FBXRecorderSettingsConfig] CreateProperRecorderSettings failed, trying CreateProperFBXRecorderSettings...");
                    settings = RecorderClipUtility.CreateProperFBXRecorderSettings(name);
                    
                    if (settings == null)
                    {
                        MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] Failed to create FBX recorder settings - all methods failed");
                        MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] Please ensure the Unity FBX Exporter package (com.unity.formats.fbx) is installed");
                        return null;
                    }
                }
                
                settings.name = name;
                
                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] FBX Recorder Settings created successfully: {settings.GetType().FullName}");
                
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
                    MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] InputSettings type: {(inputSettings != null ? inputSettings.GetType().FullName : "null")}");
                }
                
                // Try to get AnimationInputSettings through property first
                var animInputSettingsProp = settingsType.GetProperty("AnimationInputSettings");
                if (animInputSettingsProp != null && animInputSettingsProp.CanRead)
                {
                    var animInputSettings = animInputSettingsProp.GetValue(settings);
                    if (animInputSettings != null)
                    {
                        MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Found AnimationInputSettings via property: {animInputSettings.GetType().FullName}");
                        ConfigureAnimationInputSettings(animInputSettings, targetGameObject, recordedComponent, recordHierarchy, clampedTangents);
                    }
                    else
                    {
                        MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] AnimationInputSettings property returned null");
                    }
                }
                else
                {
                    // Fallback to field access
                    MultiTimelineRecorderLogger.Log("[FBXRecorderSettingsConfig] AnimationInputSettings property not found, trying field access...");
                    var animInputSettingsField = settingsType.GetField("m_AnimationInputSettings", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (animInputSettingsField != null)
                    {
                        var animInputSettings = animInputSettingsField.GetValue(settings);
                        
                        // If AnimationInputSettings is null, try to create it
                        if (animInputSettings == null)
                        {
                            MultiTimelineRecorderLogger.LogVerbose("[FBXRecorderSettingsConfig] AnimationInputSettings is null, attempting to create...");
                            var animInputType = animInputSettingsField.FieldType;
                            try
                            {
                                animInputSettings = System.Activator.CreateInstance(animInputType);
                                animInputSettingsField.SetValue(settings, animInputSettings);
                                MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Created AnimationInputSettings of type: {animInputType.FullName}");
                            }
                            catch (System.Exception ex)
                            {
                                MultiTimelineRecorderLogger.LogError($"[FBXRecorderSettingsConfig] Failed to create AnimationInputSettings: {ex.Message}");
                            }
                        }
                        
                        if (animInputSettings != null)
                        {
                            MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Found AnimationInputSettings via field: {animInputSettings.GetType().FullName}");
                            
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
                                    MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set SimplyCurves to {compressionValue}");
                                }
                            }
                            
                            // IMPORTANT: Verify that gameObject was set correctly
                            var goProperty = animType.GetProperty("gameObject");
                            if (goProperty != null && goProperty.CanRead)
                            {
                                var go = goProperty.GetValue(animInputSettings) as GameObject;
                                if (go == null)
                                {
                                    MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] CRITICAL: GameObject is still null after configuration!");
                                }
                                else
                                {
                                    MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] SUCCESS: GameObject is set to {go.name}");
                                }
                            }
                        }
                        else
                        {
                            MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] AnimationInputSettings is null and could not be created");
                        }
                    }
                    else
                    {
                        MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] Could not find m_AnimationInputSettings field");
                    }
                }
                
                // Set ExportGeometry
                var exportGeometryProp = settingsType.GetProperty("ExportGeometry");
                if (exportGeometryProp != null && exportGeometryProp.CanWrite)
                {
                    exportGeometryProp.SetValue(settings, exportGeometry);
                    MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set ExportGeometry to {exportGeometry}");
                }
                
                // Set TransferAnimationSource
                if (transferAnimationSource != null)
                {
                    var transferSourceProp = settingsType.GetProperty("TransferAnimationSource");
                    if (transferSourceProp != null && transferSourceProp.CanWrite)
                    {
                        transferSourceProp.SetValue(settings, transferAnimationSource);
                        MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set TransferAnimationSource to {transferAnimationSource.name}");
                    }
                }
                
                // Set TransferAnimationDest
                if (transferAnimationDest != null)
                {
                    var transferDestProp = settingsType.GetProperty("TransferAnimationDest");
                    if (transferDestProp != null && transferDestProp.CanWrite)
                    {
                        transferDestProp.SetValue(settings, transferAnimationDest);
                        MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set TransferAnimationDest to {transferAnimationDest.name}");
                    }
                }
                
                // Set frame rate
                MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Before setting frame rate: {settings.FrameRate}");
                settings.FrameRate = frameRate;
                MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] After setting frame rate: {settings.FrameRate} (expected: {frameRate})");
                
                MultiTimelineRecorderLogger.LogVerbose("[FBXRecorderSettingsConfig] FBX recorder settings created successfully");
                
                
                return settings;
            }
            catch (System.Exception e)
            {
                MultiTimelineRecorderLogger.LogError($"[FBXRecorderSettingsConfig] Exception creating FBX recorder settings: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Configure AnimationInputSettings with proper values
        /// </summary>
        private static void ConfigureAnimationInputSettings(object animInputSettings, GameObject targetGameObject, FBXRecordedComponent recordedComponent, bool recordHierarchy, bool clampedTangents)
        {
            MultiTimelineRecorderLogger.Log($"[ConfigureAnimationInputSettings] Called with targetGameObject: {(targetGameObject != null ? targetGameObject.name : "NULL")}");
            
            if (animInputSettings == null)
            {
                MultiTimelineRecorderLogger.LogError("[ConfigureAnimationInputSettings] animInputSettings is null!");
                return;
            }
            
            if (targetGameObject == null)
            {
                MultiTimelineRecorderLogger.LogError("[ConfigureAnimationInputSettings] targetGameObject is null! FBX recording will fail!");
                return;
            }
                
            var animType = animInputSettings.GetType();
            
            // Set GameObject
            var gameObjectProp = animType.GetProperty("gameObject");
            if (gameObjectProp != null && gameObjectProp.CanWrite)
            {
                gameObjectProp.SetValue(animInputSettings, targetGameObject);
                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Set target GameObject to {targetGameObject.name}");
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
                        MultiTimelineRecorderLogger.LogWarning("[FBXRecorderSettingsConfig] No components selected for recording. Defaulting to Transform.");
                        addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Transform) });
                        MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added Transform to recorded components (default)");
                    }
                    else
                    {
                        // Process each selected component flag
                        int addedComponents = 0;
                        
                        // Transform
                        if ((recordedComponent & FBXRecordedComponent.Transform) != 0)
                        {
                            addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Transform) });
                            MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added Transform to recorded components");
                            addedComponents++;
                        }
                        
                        // Camera
                        if ((recordedComponent & FBXRecordedComponent.Camera) != 0)
                        {
                            var camera = targetGameObject.GetComponent<Camera>();
                            if (camera != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Camera) });
                                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added Camera to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                MultiTimelineRecorderLogger.LogWarning($"[FBXRecorderSettingsConfig] Camera component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // Light
                        if ((recordedComponent & FBXRecordedComponent.Light) != 0)
                        {
                            var light = targetGameObject.GetComponent<Light>();
                            if (light != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Light) });
                                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added Light to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                MultiTimelineRecorderLogger.LogWarning($"[FBXRecorderSettingsConfig] Light component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // MeshRenderer
                        if ((recordedComponent & FBXRecordedComponent.MeshRenderer) != 0)
                        {
                            var meshRenderer = targetGameObject.GetComponent<MeshRenderer>();
                            if (meshRenderer != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(MeshRenderer) });
                                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added MeshRenderer to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                MultiTimelineRecorderLogger.LogWarning($"[FBXRecorderSettingsConfig] MeshRenderer component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // SkinnedMeshRenderer
                        if ((recordedComponent & FBXRecordedComponent.SkinnedMeshRenderer) != 0)
                        {
                            var skinnedMeshRenderer = targetGameObject.GetComponent<SkinnedMeshRenderer>();
                            if (skinnedMeshRenderer != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(SkinnedMeshRenderer) });
                                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added SkinnedMeshRenderer to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                MultiTimelineRecorderLogger.LogWarning($"[FBXRecorderSettingsConfig] SkinnedMeshRenderer component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // Animator
                        if ((recordedComponent & FBXRecordedComponent.Animator) != 0)
                        {
                            var animator = targetGameObject.GetComponent<Animator>();
                            if (animator != null)
                            {
                                addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Animator) });
                                MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added Animator to recorded components");
                                addedComponents++;
                            }
                            else
                            {
                                MultiTimelineRecorderLogger.LogWarning($"[FBXRecorderSettingsConfig] Animator component not found on {targetGameObject.name}");
                            }
                        }
                        
                        // If no components were added (all selected components were missing), add Transform as fallback
                        if (addedComponents == 0)
                        {
                            MultiTimelineRecorderLogger.LogWarning("[FBXRecorderSettingsConfig] No selected components found on target. Adding Transform as fallback.");
                            addComponentMethod.Invoke(animInputSettings, new object[] { typeof(Transform) });
                            MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Added Transform to recorded components (fallback)");
                        }
                        
                        MultiTimelineRecorderLogger.Log($"[FBXRecorderSettingsConfig] Total components added: {addedComponents}");
                    }
                }
                catch (Exception ex)
                {
                    MultiTimelineRecorderLogger.LogError($"[FBXRecorderSettingsConfig] Failed to add component to record: {ex.Message}");
                }
            }
            else
            {
                MultiTimelineRecorderLogger.LogError("[FBXRecorderSettingsConfig] AddComponentToRecord method not found on AnimationInputSettings");
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
                    MultiTimelineRecorderLogger.LogVerbose($"[FBXRecorderSettingsConfig] Set {propertyName} to {value}");
                }
                catch (Exception ex)
                {
                    MultiTimelineRecorderLogger.LogWarning($"[FBXRecorderSettingsConfig] Could not set {propertyName}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public FBXRecorderSettingsConfig Clone()
        {
            var clone = new FBXRecorderSettingsConfig
            {
                recordedComponent = this.recordedComponent,
                recordHierarchy = this.recordHierarchy,
                clampedTangents = this.clampedTangents,
                animationCompression = this.animationCompression,
                exportGeometry = this.exportGeometry,
                frameRate = this.frameRate
            };
            
            // GameObject参照を適切に深くコピー
            clone.targetGameObject = this.targetGameObject;
            clone.transferAnimationSource = this.transferAnimationSource;
            clone.transferAnimationDest = this.transferAnimationDest;
            
            // GameObjectReferenceオブジェクトを深くコピーして、シリアライズ時に正しく保存されるようにする
            if (this.targetGameObjectRef != null)
            {
                clone.targetGameObjectRef = new GameObjectReference();
                clone.targetGameObjectRef.GameObject = this.targetGameObjectRef.GameObject;
            }
            
            if (this.transferAnimationSourceRef != null)
            {
                clone.transferAnimationSourceRef = new GameObjectReference();
                clone.transferAnimationSourceRef.GameObject = this.transferAnimationSourceRef.GameObject;
            }
            
            if (this.transferAnimationDestRef != null)
            {
                clone.transferAnimationDestRef = new GameObjectReference();
                clone.transferAnimationDestRef.GameObject = this.transferAnimationDestRef.GameObject;
            }
            
            return clone;
        }
        
    }
}