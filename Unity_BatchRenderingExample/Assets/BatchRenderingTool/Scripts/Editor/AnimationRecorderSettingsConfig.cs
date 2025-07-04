using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration class for AnimationRecorderSettings
    /// </summary>
    [Serializable]
    public class AnimationRecorderSettingsConfig
    {
        // Recording targets
        public AnimationRecordingProperties recordingProperties = AnimationRecordingProperties.TransformOnly;
        public AnimationRecordingScope recordingScope = AnimationRecordingScope.SingleGameObject;
        public GameObject targetGameObject = null;
        public List<GameObject> customSelection = new List<GameObject>();
        
        // Sampling settings
        public float frameRate = 30f;
        public AnimationInterpolationMode interpolationMode = AnimationInterpolationMode.Linear;
        public bool recordInWorldSpace = false;
        
        // Compression settings
        public AnimationCompressionLevel compressionLevel = AnimationCompressionLevel.Medium;
        public float positionError = 0.01f;
        public float rotationError = 0.5f;
        public float scaleError = 0.01f;
        public bool optimizeGameObjects = true;
        
        // Humanoid settings
        public bool treatAsHumanoid = false;
        public Avatar avatarMask = null;
        public bool recordRootMotion = true;
        
        // Advanced settings
        public bool recordHierarchyChanges = false;
        public bool recordComponentEnableStates = false;
        public string customPropertyPaths = "";
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check if any recording property is selected
            if (recordingProperties == AnimationRecordingProperties.None)
            {
                errorMessage = "At least one recording property must be selected";
                return false;
            }
            
            // Validate frame rate
            if (frameRate <= 0 || frameRate > 120)
            {
                errorMessage = "Frame rate must be between 1 and 120";
                return false;
            }
            
            // Validate error tolerances
            if (positionError < 0 || rotationError < 0 || scaleError < 0)
            {
                errorMessage = "Error tolerances must be non-negative";
                return false;
            }
            
            // Validate recording scope
            switch (recordingScope)
            {
                case AnimationRecordingScope.SingleGameObject:
                case AnimationRecordingScope.GameObjectAndChildren:
                    if (targetGameObject == null)
                    {
                        errorMessage = "Target GameObject is required";
                        return false;
                    }
                    break;
                    
                case AnimationRecordingScope.CustomSelection:
                    if (customSelection == null || customSelection.Count == 0)
                    {
                        errorMessage = "Custom selection is empty";
                        return false;
                    }
                    break;
            }
            
            // Validate humanoid settings
            if (treatAsHumanoid && targetGameObject != null)
            {
                var animator = targetGameObject.GetComponent<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    errorMessage = "Target GameObject must have a Humanoid Animator for humanoid recording";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Create AnimationRecorderSettings
        /// </summary>
        public RecorderSettings CreateAnimationRecorderSettings(string name)
        {
            Debug.Log($"[AnimationRecorderSettingsConfig] Creating Animation settings: {name}");
            
            // Create actual AnimationRecorderSettings
            var settings = ScriptableObject.CreateInstance<AnimationRecorderSettings>();
            settings.name = name;
            
            // Configure basic settings
            settings.Enabled = true;
            settings.RecordMode = RecordMode.Manual;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
            
            // Configure AnimationInputSettings
            if (settings.AnimationInputSettings != null)
            {
                // Set target GameObject
                settings.AnimationInputSettings.gameObject = targetGameObject;
                
                // Set recursive recording based on scope
                settings.AnimationInputSettings.Recursive = (recordingScope == AnimationRecordingScope.GameObjectAndChildren);
                
                // Configure curve settings
                settings.AnimationInputSettings.ClampedTangents = true;
                
                // Set compression based on compression level
                switch (compressionLevel)
                {
                    case AnimationCompressionLevel.None:
                        settings.AnimationInputSettings.SimplyCurves = AnimationInputSettings.CurveSimplificationOptions.Disabled;
                        break;
                    case AnimationCompressionLevel.Low:
                    case AnimationCompressionLevel.Medium:
                        settings.AnimationInputSettings.SimplyCurves = AnimationInputSettings.CurveSimplificationOptions.Lossless;
                        break;
                    case AnimationCompressionLevel.High:
                        settings.AnimationInputSettings.SimplyCurves = AnimationInputSettings.CurveSimplificationOptions.Lossy;
                        break;
                }
                
                // Add components to record based on recording properties
                if ((recordingProperties & AnimationRecordingProperties.Position) != 0 ||
                    (recordingProperties & AnimationRecordingProperties.Rotation) != 0 ||
                    (recordingProperties & AnimationRecordingProperties.Scale) != 0)
                {
                    settings.AnimationInputSettings.AddComponentToRecord(typeof(Transform));
                }
                
                if ((recordingProperties & AnimationRecordingProperties.BlendShapes) != 0)
                {
                    settings.AnimationInputSettings.AddComponentToRecord(typeof(SkinnedMeshRenderer));
                }
                
                if ((recordingProperties & AnimationRecordingProperties.MaterialProperties) != 0)
                {
                    settings.AnimationInputSettings.AddComponentToRecord(typeof(MeshRenderer));
                    settings.AnimationInputSettings.AddComponentToRecord(typeof(SkinnedMeshRenderer));
                }
                
                if ((recordingProperties & AnimationRecordingProperties.CustomProperties) != 0)
                {
                    // Add Animator component for custom properties
                    settings.AnimationInputSettings.AddComponentToRecord(typeof(Animator));
                }
            }
            
            Debug.Log($"[AnimationRecorderSettingsConfig] Recording properties: {recordingProperties}");
            Debug.Log($"[AnimationRecorderSettingsConfig] Frame rate: {frameRate}, Interpolation: {interpolationMode}");
            Debug.Log($"[AnimationRecorderSettingsConfig] Compression: {compressionLevel}");
            
            return settings;
        }
        
        /// <summary>
        /// Get objects to record based on scope
        /// </summary>
        public List<GameObject> GetRecordingObjects()
        {
            var objects = new List<GameObject>();
            
            switch (recordingScope)
            {
                case AnimationRecordingScope.SingleGameObject:
                    if (targetGameObject != null)
                    {
                        objects.Add(targetGameObject);
                    }
                    break;
                    
                case AnimationRecordingScope.GameObjectAndChildren:
                    if (targetGameObject != null)
                    {
                        objects.Add(targetGameObject);
                        // Add all children recursively
                        AddChildrenRecursive(targetGameObject.transform, objects);
                    }
                    break;
                    
                case AnimationRecordingScope.SelectedHierarchy:
                    // Get selected objects
                    foreach (var go in Selection.gameObjects)
                    {
                        objects.Add(go);
                    }
                    break;
                    
                case AnimationRecordingScope.CustomSelection:
                    objects.AddRange(customSelection);
                    break;
            }
            
            return objects;
        }
        
        private void AddChildrenRecursive(Transform parent, List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                list.Add(child.gameObject);
                AddChildrenRecursive(child, list);
            }
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public AnimationRecorderSettingsConfig Clone()
        {
            return new AnimationRecorderSettingsConfig
            {
                recordingProperties = this.recordingProperties,
                recordingScope = this.recordingScope,
                targetGameObject = this.targetGameObject,
                customSelection = new List<GameObject>(this.customSelection),
                frameRate = this.frameRate,
                interpolationMode = this.interpolationMode,
                recordInWorldSpace = this.recordInWorldSpace,
                compressionLevel = this.compressionLevel,
                positionError = this.positionError,
                rotationError = this.rotationError,
                scaleError = this.scaleError,
                optimizeGameObjects = this.optimizeGameObjects,
                treatAsHumanoid = this.treatAsHumanoid,
                avatarMask = this.avatarMask,
                recordRootMotion = this.recordRootMotion,
                recordHierarchyChanges = this.recordHierarchyChanges,
                recordComponentEnableStates = this.recordComponentEnableStates,
                customPropertyPaths = this.customPropertyPaths
            };
        }
        
        /// <summary>
        /// Get preset configuration
        /// </summary>
        public static AnimationRecorderSettingsConfig GetPreset(AnimationExportPreset preset)
        {
            var config = new AnimationRecorderSettingsConfig();
            
            switch (preset)
            {
                case AnimationExportPreset.CharacterAnimation:
                    config.recordingProperties = AnimationRecordingProperties.TransformAndBlendShapes;
                    config.recordingScope = AnimationRecordingScope.GameObjectAndChildren;
                    config.treatAsHumanoid = true;
                    config.recordRootMotion = true;
                    config.compressionLevel = AnimationCompressionLevel.Medium;
                    config.interpolationMode = AnimationInterpolationMode.Smooth;
                    break;
                    
                case AnimationExportPreset.CameraAnimation:
                    config.recordingProperties = AnimationRecordingProperties.Position | AnimationRecordingProperties.Rotation | AnimationRecordingProperties.CameraProperties;
                    config.recordingScope = AnimationRecordingScope.SingleGameObject;
                    config.compressionLevel = AnimationCompressionLevel.Low;
                    config.interpolationMode = AnimationInterpolationMode.Linear;
                    break;
                    
                case AnimationExportPreset.SimpleTransform:
                    config.recordingProperties = AnimationRecordingProperties.TransformOnly;
                    config.recordingScope = AnimationRecordingScope.SingleGameObject;
                    config.compressionLevel = AnimationCompressionLevel.High;
                    config.interpolationMode = AnimationInterpolationMode.Linear;
                    break;
                    
                case AnimationExportPreset.ComplexAnimation:
                    config.recordingProperties = AnimationRecordingProperties.AllProperties;
                    config.recordingScope = AnimationRecordingScope.GameObjectAndChildren;
                    config.compressionLevel = AnimationCompressionLevel.Optimal;
                    config.interpolationMode = AnimationInterpolationMode.Smooth;
                    config.recordHierarchyChanges = true;
                    config.recordComponentEnableStates = true;
                    break;
            }
            
            // Apply compression presets
            config.positionError = AnimationRecordingInfo.CompressionPresets.GetPositionErrorTolerance(config.compressionLevel);
            config.rotationError = AnimationRecordingInfo.CompressionPresets.GetRotationErrorTolerance(config.compressionLevel);
            config.scaleError = AnimationRecordingInfo.CompressionPresets.GetScaleErrorTolerance(config.compressionLevel);
            
            return config;
        }
    }
}