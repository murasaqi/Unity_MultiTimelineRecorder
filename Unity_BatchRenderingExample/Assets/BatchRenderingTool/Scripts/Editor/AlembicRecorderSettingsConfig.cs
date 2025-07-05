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
            BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Creating Alembic settings: {name}");
            
            // Try to find AlembicRecorderSettings type in Unity Recorder
            System.Type alembicRecorderSettingsType = null;
            
            // Check for Unity Recorder's AlembicRecorderSettings first (most common)
            alembicRecorderSettingsType = System.Type.GetType("UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor");
            
            if (alembicRecorderSettingsType == null)
            {
                // Try Unity Alembic package types
                alembicRecorderSettingsType = System.Type.GetType("UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor");
            }
            
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
                BatchRenderingToolLogger.LogError("[AlembicRecorderSettingsConfig] AlembicRecorderSettings type not found. Make sure Unity Recorder and Alembic packages are installed.");
                return null;
            }
            
            // Create AlembicRecorderSettings instance
            var settings = ScriptableObject.CreateInstance(alembicRecorderSettingsType) as RecorderSettings;
            if (settings == null)
            {
                BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] Failed to create instance of {alembicRecorderSettingsType.Name}");
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
            
            // Try to create and set AlembicInputSettings if needed
            TrySetAlembicInputSettings(settings);
            
            BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Export targets: {exportTargets}");
            BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Frame rate: {frameRate}, Samples per frame: {samplesPerFrame}");
            BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Scale: {scaleFactor}, Handedness: {handedness}");
            
            return settings;
        }
        
        /// <summary>
        /// Apply Alembic-specific settings using reflection
        /// </summary>
        private void ApplyAlembicSettings(RecorderSettings settings)
        {
            var settingsType = settings.GetType();
            
            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Applying settings to type: {settingsType.FullName} ===");
            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Export scope: {exportScope}, Target GameObject: {targetGameObject?.name ?? "null"} ===");
            
            // Log all available properties first for debugging
            LogAvailableProperties(settingsType);
            
            // Set scope - try both int and enum
            bool scopeSet = false;
            
            // Map our enum values to Unity Recorder's expected values
            // Unity Recorder might use different enum values:
            // 0 = Game Object
            // 1 = Targeted Camera  
            // 2 = Entire Scene
            int unityRecorderScope = 2; // Default to entire scene
            switch (exportScope)
            {
                case AlembicExportScope.TargetGameObject:
                    unityRecorderScope = 0; // Game Object
                    break;
                case AlembicExportScope.EntireScene:
                    unityRecorderScope = 2; // Entire Scene
                    break;
                case AlembicExportScope.SelectedHierarchy:
                case AlembicExportScope.CustomSelection:
                    unityRecorderScope = 0; // Treat as Game Object
                    break;
            }
            
            // Try setting with Unity Recorder's expected values
            if (SetPropertyValue(settings, settingsType, "Scope", unityRecorderScope))
            {
                scopeSet = true;
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {unityRecorderScope} (Unity Recorder int value) ===");
            }
            else if (SetPropertyValue(settings, settingsType, "Scope", exportScope))
            {
                scopeSet = true;
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {exportScope} (enum) ===");
            }
            else if (SetPropertyValue(settings, settingsType, "Scope", (int)exportScope))
            {
                scopeSet = true;
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {(int)exportScope} (int) ===");
            }
            else if (SetPropertyValue(settings, settingsType, "ExportScope", exportScope))
            {
                scopeSet = true;
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set ExportScope to {exportScope} ===");
            }
            else if (SetPropertyValue(settings, settingsType, "exportScope", exportScope))
            {
                scopeSet = true;
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set exportScope to {exportScope} ===");
            }
            
            if (!scopeSet)
            {
                BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] === FAILED to set scope property ===");
            }
            
            // Try to set input settings if it exists
            var inputSettingsProperty = settingsType.GetProperty("InputSettings");
            if (inputSettingsProperty != null)
            {
                var inputSettings = inputSettingsProperty.GetValue(settings);
                if (inputSettings != null)
                {
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Found InputSettings of type: {inputSettings.GetType().FullName} ===");
                    
                    // Log InputSettings properties
                    var inputType = inputSettings.GetType();
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === InputSettings properties: ===");
                    LogAvailableProperties(inputType);
                    
                    // Try to set GameObject on InputSettings
                    if (exportScope == AlembicExportScope.TargetGameObject && targetGameObject != null)
                    {
                        bool inputGameObjectSet = false;
                        if (SetPropertyValue(inputSettings, inputType, "GameObject", targetGameObject))
                        {
                            inputGameObjectSet = true;
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set InputSettings.GameObject ===");
                        }
                        else if (SetPropertyValue(inputSettings, inputType, "gameObject", targetGameObject))
                        {
                            inputGameObjectSet = true;
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set InputSettings.gameObject ===");
                        }
                        else if (SetPropertyValue(inputSettings, inputType, "TargetGameObject", targetGameObject))
                        {
                            inputGameObjectSet = true;
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set InputSettings.TargetGameObject ===");
                        }
                        
                        if (!inputGameObjectSet)
                        {
                            BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] === FAILED to set GameObject on InputSettings ===");
                        }
                    }
                }
                else
                {
                    BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] === InputSettings is null ===");
                }
            }
            else
            {
                BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] === InputSettings property not found on {settingsType.Name} ===");
            }
            
            // Set target GameObject if applicable
            if (exportScope == AlembicExportScope.TargetGameObject && targetGameObject != null)
            {
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Setting TargetGameObject to: {targetGameObject.name} ===");
                
                // Try different property names that might be used in AlembicRecorderSettings
                bool success = false;
                if (SetPropertyValue(settings, settingsType, "TargetGameObject", targetGameObject))
                {
                    success = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'TargetGameObject' property ===");
                }
                else if (SetPropertyValue(settings, settingsType, "targetGameObject", targetGameObject))
                {
                    success = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'targetGameObject' property ===");
                }
                else if (SetPropertyValue(settings, settingsType, "GameObject", targetGameObject))
                {
                    success = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'GameObject' property ===");
                }
                else if (SetPropertyValue(settings, settingsType, "gameObject", targetGameObject))
                {
                    success = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'gameObject' property ===");
                }
                
                if (!success)
                {
                    BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] === FAILED to set target GameObject on AlembicRecorderSettings ===");
                    LogAvailableProperties(settingsType);
                }
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
        private bool SetPropertyValue(object obj, System.Type type, string propertyName, object value)
        {
            try
            {
                var property = type.GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    // Log property type information
                    BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Found property {propertyName} of type {property.PropertyType.Name}");
                    
                    // Try to convert value if needed
                    object convertedValue = value;
                    if (value != null && property.PropertyType.IsEnum && value.GetType() != property.PropertyType)
                    {
                        // Try to convert to the correct enum type
                        try
                        {
                            if (value is int intValue)
                            {
                                convertedValue = System.Enum.ToObject(property.PropertyType, intValue);
                            }
                            else if (value is System.Enum)
                            {
                                // Try to convert enum by name
                                string enumName = value.ToString();
                                convertedValue = System.Enum.Parse(property.PropertyType, enumName);
                            }
                            BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Converted value from {value.GetType().Name} to {property.PropertyType.Name}");
                        }
                        catch (System.Exception convEx)
                        {
                            BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] Failed to convert value: {convEx.Message}");
                            return false;
                        }
                    }
                    
                    property.SetValue(obj, convertedValue);
                    
                    // Verify the value was set correctly
                    var verifyValue = property.GetValue(obj);
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Set property {propertyName} to {convertedValue}, verified: {verifyValue} ===");
                    return true;
                }
                else
                {
                    // Try field if property not found
                    var field = type.GetField(propertyName);
                    if (field != null)
                    {
                        field.SetValue(obj, value);
                        var verifyValue = field.GetValue(obj);
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Set field {propertyName} to {value}, verified: {verifyValue} ===");
                        return true;
                    }
                }
                
                // Property/field not found
                BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Property/field {propertyName} not found on type {type.Name}");
                return false;
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] Failed to set {propertyName}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Try to set AlembicInputSettings if available
        /// </summary>
        private void TrySetAlembicInputSettings(RecorderSettings settings)
        {
            var settingsType = settings.GetType();
            
            // Try to find AlembicInputSettings type
            System.Type alembicInputSettingsType = null;
            string[] possibleTypeNames = new string[]
            {
                "UnityEditor.Recorder.Input.AlembicInputSettings, Unity.Recorder.Editor",
                "UnityEditor.Recorder.AlembicInputSettings, Unity.Recorder.Editor",
                "UnityEditor.Formats.Alembic.Recorder.AlembicInputSettings, Unity.Formats.Alembic.Editor",
                "AlembicInputSettings, Unity.Recorder.Editor"
            };
            
            foreach (var typeName in possibleTypeNames)
            {
                alembicInputSettingsType = System.Type.GetType(typeName);
                if (alembicInputSettingsType != null)
                {
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Found AlembicInputSettings type: {typeName} ===");
                    break;
                }
            }
            
            if (alembicInputSettingsType == null)
            {
                BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] === AlembicInputSettings type not found ===");
                return;
            }
            
            // Check if settings already has AlembicInputSettings
            var inputSettingsProperty = settingsType.GetProperty("AlembicInputSettings");
            if (inputSettingsProperty == null)
            {
                inputSettingsProperty = settingsType.GetProperty("alembicInputSettings");
            }
            
            if (inputSettingsProperty != null)
            {
                // Create new AlembicInputSettings instance
                var inputSettings = ScriptableObject.CreateInstance(alembicInputSettingsType);
                if (inputSettings != null)
                {
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Created AlembicInputSettings instance ===");
                    
                    // Set properties on AlembicInputSettings
                    var inputType = inputSettings.GetType();
                    
                    // Set scope using Unity Recorder's expected values
                    int unityRecorderScope = 2; // Default to entire scene
                    switch (exportScope)
                    {
                        case AlembicExportScope.TargetGameObject:
                            unityRecorderScope = 0; // Game Object
                            break;
                        case AlembicExportScope.EntireScene:
                            unityRecorderScope = 2; // Entire Scene
                            break;
                        case AlembicExportScope.SelectedHierarchy:
                        case AlembicExportScope.CustomSelection:
                            unityRecorderScope = 0; // Treat as Game Object
                            break;
                    }
                    
                    if (!SetPropertyValue(inputSettings, inputType, "CaptureScope", unityRecorderScope))
                    {
                        if (!SetPropertyValue(inputSettings, inputType, "captureScope", unityRecorderScope))
                        {
                            if (!SetPropertyValue(inputSettings, inputType, "Scope", unityRecorderScope))
                            {
                                // Try with our original enum as fallback
                                SetPropertyValue(inputSettings, inputType, "Scope", exportScope);
                            }
                        }
                    }
                    
                    // Set target GameObject
                    if (exportScope == AlembicExportScope.TargetGameObject && targetGameObject != null)
                    {
                        if (!SetPropertyValue(inputSettings, inputType, "TargetGameObject", targetGameObject))
                        {
                            if (!SetPropertyValue(inputSettings, inputType, "targetGameObject", targetGameObject))
                            {
                                SetPropertyValue(inputSettings, inputType, "GameObject", targetGameObject);
                            }
                        }
                    }
                    
                    // Set the input settings on the recorder settings
                    inputSettingsProperty.SetValue(settings, inputSettings);
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Set AlembicInputSettings on recorder settings ===");
                }
            }
            else
            {
                BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] === AlembicInputSettings property not found on recorder settings ===");
            }
        }
        
        /// <summary>
        /// Log available properties and fields for debugging
        /// </summary>
        private void LogAvailableProperties(System.Type type)
        {
            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Available properties on {type.Name}: ===");
            
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                BatchRenderingToolLogger.Log($"  - Property: {prop.Name} (Type: {prop.PropertyType.Name}, CanWrite: {prop.CanWrite})");
            }
            
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                BatchRenderingToolLogger.Log($"  - Field: {field.Name} (Type: {field.FieldType.Name})");
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