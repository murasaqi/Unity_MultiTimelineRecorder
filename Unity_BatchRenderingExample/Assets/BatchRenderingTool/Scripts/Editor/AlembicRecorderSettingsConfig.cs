using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Configuration class for AlembicRecorderSettings
    /// </summary>
    [Serializable]
    public class AlembicRecorderSettingsConfig
    {
        // Export targets
        public AlembicExportTargets exportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform;
        public AlembicExportScope exportScope = AlembicExportScope.EntireScene;
        public GameObject targetGameObject = null;
        public List<GameObject> customSelection = new List<GameObject>();
        
        // Time sampling settings
        public AlembicTimeSamplingMode timeSamplingMode = AlembicTimeSamplingMode.Uniform;
        public float frameRate = 24f;
        public int frameRateNumerator = 24000;
        public int frameRateDenominator = 1001; // For 23.976 fps
        public float motionBlurShutterAngle = 180f;
        public int samplesPerFrame = 1;
        
        // Transform settings
        public AlembicHandedness handedness = AlembicHandedness.Left;
        public float scaleFactor = 1f;
        public bool swapYZ = false;
        public bool flipFaces = false;
        
        // Geometry settings
        public bool exportUVs = true;
        public bool exportNormals = true;
        public bool exportTangents = false;
        public bool exportVertexColors = true;
        public bool exportVisibility = true;
        public bool assumeUnchangedTopology = true;
        
        // Advanced settings
        public bool flattenHierarchy = false;
        public string customAttributes = "";
        public bool includeInactiveMeshes = false;
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check if Alembic package is available
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                errorMessage = "Alembic package is not installed. Please install com.unity.formats.alembic package";
                return false;
            }
            
            // Check if any export target is selected
            if (exportTargets == AlembicExportTargets.None)
            {
                errorMessage = "At least one export target must be selected";
                return false;
            }
            
            // Validate frame rate
            if (frameRate <= 0 || frameRate > 120)
            {
                errorMessage = "Frame rate must be between 1 and 120";
                return false;
            }
            
            // Validate samples per frame
            if (samplesPerFrame < 1 || samplesPerFrame > 10)
            {
                errorMessage = "Samples per frame must be between 1 and 10";
                return false;
            }
            
            // Validate scale factor
            if (scaleFactor <= 0)
            {
                errorMessage = "Scale factor must be positive";
                return false;
            }
            
            // Validate export scope
            switch (exportScope)
            {
                case AlembicExportScope.TargetGameObject:
                    if (targetGameObject == null)
                    {
                        errorMessage = "Target GameObject is required when using TargetGameObject scope";
                        return false;
                    }
                    break;
                    
                case AlembicExportScope.CustomSelection:
                    if (customSelection == null || customSelection.Count == 0)
                    {
                        errorMessage = "Custom selection is empty";
                        return false;
                    }
                    break;
            }
            
            return true;
        }
        
        /// <summary>
        /// Apply configuration to AlembicRecorderSettings
        /// </summary>
        public RecorderSettings CreateAlembicRecorderSettings(string name)
        {
            Debug.Log($"[AlembicRecorderSettingsConfig] Creating Alembic settings: {name}");
            
            // Try to find AlembicRecorderSettings type in Unity Recorder
            System.Type alembicRecorderSettingsType = null;
            
            // Check for the correct Alembic recorder type
            alembicRecorderSettingsType = System.Type.GetType("UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor");
            
            if (alembicRecorderSettingsType == null)
            {
                // Try alternative type names
                alembicRecorderSettingsType = System.Type.GetType("UnityEditor.Recorder.Formats.Alembic.AlembicRecorderSettings, Unity.Recorder.Editor");
            }
            
            if (alembicRecorderSettingsType == null)
            {
                // Try without namespace
                alembicRecorderSettingsType = System.Type.GetType("AlembicRecorderSettings, Unity.Formats.Alembic.Editor");
            }
            
            if (alembicRecorderSettingsType == null)
            {
                Debug.LogError("[AlembicRecorderSettingsConfig] AlembicRecorderSettings type not found. Make sure Unity Recorder and Alembic packages are installed.");
                return null;
            }
            
            // Create AlembicRecorderSettings instance
            var settings = ScriptableObject.CreateInstance(alembicRecorderSettingsType) as RecorderSettings;
            if (settings == null)
            {
                Debug.LogError($"[AlembicRecorderSettingsConfig] Failed to create instance of {alembicRecorderSettingsType.Name}");
                return null;
            }
            
            settings.name = name;
            
            // Configure basic settings
            settings.Enabled = true;
            settings.RecordMode = RecordMode.Manual;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
            
            // Apply Alembic-specific settings using reflection
            ApplyAlembicSettings(settings);
            
            Debug.Log($"[AlembicRecorderSettingsConfig] Export targets: {exportTargets}");
            Debug.Log($"[AlembicRecorderSettingsConfig] Frame rate: {frameRate}, Samples per frame: {samplesPerFrame}");
            Debug.Log($"[AlembicRecorderSettingsConfig] Scale: {scaleFactor}, Handedness: {handedness}");
            
            return settings;
        }
        
        /// <summary>
        /// Apply Alembic-specific settings using reflection
        /// </summary>
        private void ApplyAlembicSettings(RecorderSettings settings)
        {
            var settingsType = settings.GetType();
            
            // Set scope
            SetPropertyValue(settings, settingsType, "Scope", (int)exportScope);
            
            // Set target GameObject if applicable
            if (exportScope == AlembicExportScope.TargetGameObject && targetGameObject != null)
            {
                SetPropertyValue(settings, settingsType, "TargetGameObject", targetGameObject);
            }
            
            // Set scale factor
            SetPropertyValue(settings, settingsType, "ScaleFactor", scaleFactor);
            
            // Set handedness (might be an enum or bool depending on Unity version)
            SetPropertyValue(settings, settingsType, "SwapHandedness", handedness == AlembicHandedness.Right);
            
            // Set samples per frame
            SetPropertyValue(settings, settingsType, "CaptureEveryNthFrame", 1);
            SetPropertyValue(settings, settingsType, "MotionVectorSamples", samplesPerFrame);
            
            // Set geometry settings
            SetPropertyValue(settings, settingsType, "ExportVisibility", exportVisibility);
            SetPropertyValue(settings, settingsType, "AssumeNonSkinnedMeshesAreConstant", assumeUnchangedTopology);
            
            // Set what to export
            SetPropertyValue(settings, settingsType, "MeshNormals", exportNormals);
            SetPropertyValue(settings, settingsType, "MeshUV0", exportUVs);
            SetPropertyValue(settings, settingsType, "MeshColors", exportVertexColors);
            
            // Handle export targets if property exists
            var exportMeshes = (exportTargets & (AlembicExportTargets.MeshRenderer | AlembicExportTargets.SkinnedMeshRenderer)) != 0;
            var exportCameras = (exportTargets & AlembicExportTargets.Camera) != 0;
            
            SetPropertyValue(settings, settingsType, "ExportMeshes", exportMeshes);
            SetPropertyValue(settings, settingsType, "ExportCameras", exportCameras);
        }
        
        /// <summary>
        /// Set property value using reflection
        /// </summary>
        private void SetPropertyValue(object obj, System.Type type, string propertyName, object value)
        {
            try
            {
                var property = type.GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(obj, value);
                }
                else
                {
                    // Try field if property not found
                    var field = type.GetField(propertyName);
                    if (field != null)
                    {
                        field.SetValue(obj, value);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AlembicRecorderSettingsConfig] Failed to set {propertyName}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get objects to export based on scope
        /// </summary>
        public List<GameObject> GetExportObjects()
        {
            var objects = new List<GameObject>();
            
            switch (exportScope)
            {
                case AlembicExportScope.EntireScene:
                    // Get all root objects in scene
                    var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    objects.AddRange(rootObjects);
                    break;
                    
                case AlembicExportScope.SelectedHierarchy:
                    // Get selected objects and their children
                    foreach (var go in Selection.gameObjects)
                    {
                        objects.Add(go);
                    }
                    break;
                    
                case AlembicExportScope.TargetGameObject:
                    if (targetGameObject != null)
                    {
                        objects.Add(targetGameObject);
                    }
                    break;
                    
                case AlembicExportScope.CustomSelection:
                    objects.AddRange(customSelection);
                    break;
            }
            
            return objects;
        }
        
        /// <summary>
        /// Clone this configuration
        /// </summary>
        public AlembicRecorderSettingsConfig Clone()
        {
            return new AlembicRecorderSettingsConfig
            {
                exportTargets = this.exportTargets,
                exportScope = this.exportScope,
                targetGameObject = this.targetGameObject,
                customSelection = new List<GameObject>(this.customSelection),
                timeSamplingMode = this.timeSamplingMode,
                frameRate = this.frameRate,
                frameRateNumerator = this.frameRateNumerator,
                frameRateDenominator = this.frameRateDenominator,
                motionBlurShutterAngle = this.motionBlurShutterAngle,
                samplesPerFrame = this.samplesPerFrame,
                handedness = this.handedness,
                scaleFactor = this.scaleFactor,
                swapYZ = this.swapYZ,
                flipFaces = this.flipFaces,
                exportUVs = this.exportUVs,
                exportNormals = this.exportNormals,
                exportTangents = this.exportTangents,
                exportVertexColors = this.exportVertexColors,
                exportVisibility = this.exportVisibility,
                assumeUnchangedTopology = this.assumeUnchangedTopology,
                flattenHierarchy = this.flattenHierarchy,
                customAttributes = this.customAttributes,
                includeInactiveMeshes = this.includeInactiveMeshes
            };
        }
        
        /// <summary>
        /// Get preset configuration
        /// </summary>
        public static AlembicRecorderSettingsConfig GetPreset(AlembicExportPreset preset)
        {
            var config = new AlembicRecorderSettingsConfig();
            
            switch (preset)
            {
                case AlembicExportPreset.AnimationExport:
                    config.exportTargets = AlembicExportInfo.Presets.GetAnimationExport();
                    config.exportScope = AlembicExportScope.SelectedHierarchy;
                    config.samplesPerFrame = 1;
                    config.assumeUnchangedTopology = false;
                    break;
                    
                case AlembicExportPreset.CameraExport:
                    config.exportTargets = AlembicExportInfo.Presets.GetCameraExport();
                    config.exportScope = AlembicExportScope.EntireScene;
                    config.samplesPerFrame = 1;
                    break;
                    
                case AlembicExportPreset.FullSceneExport:
                    config.exportTargets = AlembicExportInfo.Presets.GetFullSceneExport();
                    config.exportScope = AlembicExportScope.EntireScene;
                    config.includeInactiveMeshes = true;
                    break;
                    
                case AlembicExportPreset.EffectsExport:
                    config.exportTargets = AlembicExportInfo.Presets.GetEffectsExport();
                    config.exportScope = AlembicExportScope.SelectedHierarchy;
                    config.samplesPerFrame = 2; // Higher sampling for particles
                    break;
            }
            
            return config;
        }
    }
}