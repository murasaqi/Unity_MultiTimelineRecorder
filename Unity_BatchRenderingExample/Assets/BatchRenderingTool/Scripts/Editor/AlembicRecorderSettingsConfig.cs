using System;
using System.Collections.Generic;
using System.Reflection;
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
        
        // GameObject参照を保持するためのGameObjectReference
        [SerializeField]
        private GameObjectReference targetGameObjectRef = new GameObjectReference();
        
        public GameObject targetGameObject 
        { 
            get { return targetGameObjectRef?.GameObject; }
            set { if (targetGameObjectRef == null) targetGameObjectRef = new GameObjectReference(); targetGameObjectRef.GameObject = value; }
        }
        
        // カスタム選択用のGameObjectリストの参照も管理
        [SerializeField]
        private List<GameObjectReference> customSelectionRefs = new List<GameObjectReference>();
        
        public List<GameObject> customSelection 
        {
            get 
            {
                var list = new List<GameObject>();
                foreach (var refObj in customSelectionRefs)
                {
                    var go = refObj?.GameObject;
                    if (go != null) list.Add(go);
                }
                return list;
            }
            set 
            {
                customSelectionRefs.Clear();
                if (value != null)
                {
                    foreach (var go in value)
                    {
                        var refObj = new GameObjectReference();
                        refObj.GameObject = go;
                        customSelectionRefs.Add(refObj);
                    }
                }
            }
        }
        
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
        public float worldScale = 1f;
        public bool swapYZ = false;
        public bool flipFaces = false;
        public bool includeChildren = true;
        public AlembicTimeSamplingType timeSamplingType = AlembicTimeSamplingType.Uniform;
        
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
            SetupAlembicInputSettings(settings);
            
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
            
            // Access the internal AlembicRecorderSettings (UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings)
            var settingsProperty = settingsType.GetProperty("Settings");
            if (settingsProperty != null && settingsProperty.CanRead)
            {
                var innerSettings = settingsProperty.GetValue(settings);
                if (innerSettings != null)
                {
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Found inner Settings of type: {innerSettings.GetType().FullName}");
                    var innerType = innerSettings.GetType();
                    
                    // Set Scope on the inner settings
                    var innerScopeProperty = innerType.GetProperty("Scope");
                    if (innerScopeProperty != null && innerScopeProperty.CanWrite)
                    {
                        // Get ExportScope enum type
                        var exportScopeType = innerScopeProperty.PropertyType;
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Found Scope property of type: {exportScopeType.FullName}");
                        
                        // Map our scope to Unity's ExportScope
                        object unityScope = null;
                        if (exportScope == AlembicExportScope.EntireScene)
                        {
                            unityScope = Enum.Parse(exportScopeType, "EntireScene");
                        }
                        else if (exportScope == AlembicExportScope.TargetGameObject || 
                                 exportScope == AlembicExportScope.SelectedHierarchy ||
                                 exportScope == AlembicExportScope.CustomSelection)
                        {
                            unityScope = Enum.Parse(exportScopeType, "TargetBranch");
                        }
                        
                        if (unityScope != null)
                        {
                            innerScopeProperty.SetValue(innerSettings, unityScope);
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {unityScope} ===");
                        }
                    }
                    
                    // Set TargetBranch on the inner settings
                    if ((exportScope == AlembicExportScope.TargetGameObject || 
                         exportScope == AlembicExportScope.SelectedHierarchy ||
                         exportScope == AlembicExportScope.CustomSelection) && 
                        targetGameObject != null)
                    {
                        var targetBranchProperty = innerType.GetProperty("TargetBranch");
                        if (targetBranchProperty != null && targetBranchProperty.CanWrite)
                        {
                            targetBranchProperty.SetValue(innerSettings, targetGameObject);
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set TargetBranch to {targetGameObject.name} ===");
                        }
                    }
                    
                    // Apply other settings to the inner settings
                    ApplySettingsToInnerObject(innerSettings, innerType);
                    return;
                }
            }
            
            // For Unity Alembic, settings might be stored in fields instead of properties
            // Check all fields for Alembic-specific settings
            bool scopeSet = false;
            var fields = settingsType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Field: {field.Name} (Type: {field.FieldType.Name})");
                
                // Look for fields related to scope or target
                if (field.Name.ToLower().Contains("scope") || 
                    field.Name.ToLower().Contains("capturemode") ||
                    field.Name.ToLower().Contains("exportscope"))
                {
                    // Try to set the scope
                    if (field.FieldType.IsEnum)
                    {
                        try
                        {
                            // Get enum values and find matching one
                            var enumValues = System.Enum.GetValues(field.FieldType);
                            foreach (var enumValue in enumValues)
                            {
                                string enumName = enumValue.ToString().ToLower();
                                if (exportScope == AlembicExportScope.TargetGameObject && 
                                    (enumName.Contains("target") || enumName.Contains("branch") || enumName.Contains("gameobject")))
                                {
                                    field.SetValue(settings, enumValue);
                                    scopeSet = true;
                                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Set field {field.Name} to {enumValue}");
                                    break;
                                }
                                else if (exportScope == AlembicExportScope.EntireScene && 
                                         (enumName.Contains("entire") || enumName.Contains("scene") || enumName.Contains("all")))
                                {
                                    field.SetValue(settings, enumValue);
                                    scopeSet = true;
                                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Set field {field.Name} to {enumValue}");
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] Failed to set field {field.Name}: {e.Message}");
                        }
                    }
                }
                else if (field.Name.ToLower().Contains("target") || 
                         field.Name.ToLower().Contains("gameobject") ||
                         field.Name.ToLower().Contains("branch"))
                {
                    // Try to set target GameObject
                    if (field.FieldType == typeof(GameObject) && targetGameObject != null)
                    {
                        field.SetValue(settings, targetGameObject);
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Set field {field.Name} to {targetGameObject.name}");
                    }
                }
            }
            
            // This code path should not be reached if inner Settings were found
            BatchRenderingToolLogger.LogError("[AlembicRecorderSettingsConfig] Failed to access inner Settings object");
            
            // Map our enum values to Unity Alembic's expected values
            // Based on documentation: Scope determines export range (entire Scene or selected branch)
            // We'll try different approaches to set the scope
            object scopeValue = null;
            
            // First, try to find the correct enum type for Scope
            var scopeProperty = settingsType.GetProperty("Scope");
            if (scopeProperty != null && scopeProperty.PropertyType.IsEnum)
            {
                // Get enum values
                var enumValues = System.Enum.GetValues(scopeProperty.PropertyType);
                foreach (var enumValue in enumValues)
                {
                    string enumName = enumValue.ToString().ToLower();
                    BatchRenderingToolLogger.LogVerbose($"[AlembicRecorderSettingsConfig] Found Scope enum value: {enumValue} ({enumName})");
                    
                    // Match our scope to Unity's scope
                    if (exportScope == AlembicExportScope.TargetGameObject && 
                        (enumName.Contains("target") || enumName.Contains("branch") || enumName.Contains("selected")))
                    {
                        scopeValue = enumValue;
                        break;
                    }
                    else if (exportScope == AlembicExportScope.EntireScene && 
                             (enumName.Contains("entire") || enumName.Contains("scene") || enumName.Contains("all")))
                    {
                        scopeValue = enumValue;
                        break;
                    }
                }
            }
            
            // Try setting the scope with the found enum value
            if (scopeValue != null && SetPropertyValue(settings, settingsType, "Scope", scopeValue))
            {
                scopeSet = true;
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {scopeValue} (matched enum) ===");
            }
            else
            {
                // Fallback: try integer values based on common patterns
                // 0 = Target/Branch, 1 = Entire Scene (or vice versa)
                int scopeInt = (exportScope == AlembicExportScope.TargetGameObject) ? 0 : 1;
                if (SetPropertyValue(settings, settingsType, "Scope", scopeInt))
                {
                    scopeSet = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {scopeInt} (int) ===");
                }
                else
                {
                    // Try the reverse mapping
                    scopeInt = (exportScope == AlembicExportScope.TargetGameObject) ? 1 : 0;
                    if (SetPropertyValue(settings, settingsType, "Scope", scopeInt))
                    {
                        scopeSet = true;
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set Scope to {scopeInt} (int reversed) ===");
                    }
                }
            }
            
            if (!scopeSet)
            {
                BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] === FAILED to set scope property ===");
                
                // Log all fields for debugging
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Available fields on {settingsType.Name}: ===");
                var allFields = settingsType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                foreach (var field in allFields)
                {
                    BatchRenderingToolLogger.Log($"  - Field: {field.Name} (Type: {field.FieldType.Name})");
                }
            }
            
            // Set TargetBranch if applicable (According to Unity Alembic documentation, the property is TargetBranch, not GameObject)
            if (exportScope == AlembicExportScope.TargetGameObject && targetGameObject != null)
            {
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Setting TargetBranch to: {targetGameObject.name} ===");
                
                // The correct property name according to Unity Alembic documentation is "TargetBranch"
                bool success = false;
                if (SetPropertyValue(settings, settingsType, "TargetBranch", targetGameObject))
                {
                    success = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'TargetBranch' property ===");
                }
                else if (SetPropertyValue(settings, settingsType, "targetBranch", targetGameObject))
                {
                    success = true;
                    BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'targetBranch' property ===");
                }
                else
                {
                    // Fallback to other possible property names
                    if (SetPropertyValue(settings, settingsType, "TargetGameObject", targetGameObject))
                    {
                        success = true;
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'TargetGameObject' property ===");
                    }
                    else if (SetPropertyValue(settings, settingsType, "GameObject", targetGameObject))
                    {
                        success = true;
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === Successfully set 'GameObject' property ===");
                    }
                }
                
                if (!success)
                {
                    BatchRenderingToolLogger.LogError($"[AlembicRecorderSettingsConfig] === FAILED to set target GameObject on AlembicRecorderSettings ===");
                    LogAvailableProperties(settingsType);
                }
                else
                {
                    // Verify the target was set correctly
                    VerifyTargetGameObjectSetting(settings, settingsType);
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
            
            // Handle export targets according to Unity Alembic documentation
            var exportMeshes = (exportTargets & AlembicExportTargets.MeshRenderer) != 0;
            var exportSkinnedMeshes = (exportTargets & AlembicExportTargets.SkinnedMeshRenderer) != 0;
            var exportCameras = (exportTargets & AlembicExportTargets.Camera) != 0;
            
            // Set capture properties based on documentation
            SetPropertyValue(settings, settingsType, "CaptureMeshRenderer", exportMeshes);
            SetPropertyValue(settings, settingsType, "CaptureSkinnedMeshRenderer", exportSkinnedMeshes);
            SetPropertyValue(settings, settingsType, "CaptureCamera", exportCameras);
            
            // Legacy property names as fallback
            SetPropertyValue(settings, settingsType, "ExportMeshes", exportMeshes || exportSkinnedMeshes);
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
        /// Apply settings to the inner AlembicRecorderSettings object
        /// </summary>
        private void ApplySettingsToInnerObject(object innerSettings, System.Type innerType)
        {
            // Set scale factor
            SetPropertyValue(innerSettings, innerType, "ScaleFactor", scaleFactor);
            
            // Access ExportOptions if available
            var exportOptionsProperty = innerType.GetProperty("ExportOptions");
            if (exportOptionsProperty != null && exportOptionsProperty.CanRead)
            {
                var exportOptions = exportOptionsProperty.GetValue(innerSettings);
                if (exportOptions != null)
                {
                    var optionsType = exportOptions.GetType();
                    
                    // Set handedness
                    SetPropertyValue(exportOptions, optionsType, "SwapHandedness", handedness == AlembicHandedness.Right);
                    
                    // Set capture settings
                    SetPropertyValue(exportOptions, optionsType, "CaptureEveryNthFrame", 1);
                    SetPropertyValue(exportOptions, optionsType, "MotionVectorSamples", samplesPerFrame);
                    
                    // Set what to export
                    var exportMeshes = (exportTargets & (AlembicExportTargets.MeshRenderer | AlembicExportTargets.SkinnedMeshRenderer)) != 0;
                    var exportCameras = (exportTargets & AlembicExportTargets.Camera) != 0;
                    
                    SetPropertyValue(exportOptions, optionsType, "CaptureMeshRenderer", exportMeshes);
                    SetPropertyValue(exportOptions, optionsType, "CaptureSkinnedMeshRenderer", (exportTargets & AlembicExportTargets.SkinnedMeshRenderer) != 0);
                    SetPropertyValue(exportOptions, optionsType, "CaptureCamera", exportCameras);
                    
                    // Set geometry settings
                    SetPropertyValue(exportOptions, optionsType, "MeshNormals", exportNormals);
                    SetPropertyValue(exportOptions, optionsType, "MeshUV0", exportUVs);
                    SetPropertyValue(exportOptions, optionsType, "MeshColors", exportVertexColors);
                }
            }
        }
        
        /// <summary>
        /// Verify that the target GameObject was set correctly
        /// </summary>
        private void VerifyTargetGameObjectSetting(RecorderSettings settings, System.Type settingsType)
        {
            // Try to read back the value to verify it was set
            var properties = new string[] { "TargetBranch", "targetBranch", "TargetGameObject", "GameObject" };
            
            foreach (var propName in properties)
            {
                var prop = settingsType.GetProperty(propName);
                if (prop != null && prop.CanRead)
                {
                    var value = prop.GetValue(settings);
                    if (value != null)
                    {
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === VERIFIED: {propName} = {value} ===");
                        return;
                    }
                }
            }
            
            // Also check the Scope to ensure it's set correctly
            var scopeProp = settingsType.GetProperty("Scope");
            if (scopeProp != null && scopeProp.CanRead)
            {
                var scopeValue = scopeProp.GetValue(settings);
                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] === VERIFIED: Scope = {scopeValue} ===");
            }
        }
        
        /// <summary>
        /// Setup AlembicInputSettings for the recorder
        /// </summary>
        private void SetupAlembicInputSettings(RecorderSettings settings)
        {
            try
            {
                var settingsType = settings.GetType();
                
                // Try to create AlembicInputSettings
                System.Type alembicInputSettingsType = null;
                string[] possibleTypeNames = new string[]
                {
                    "UnityEditor.Recorder.Input.AlembicInputSettings, Unity.Recorder.Editor",
                    "UnityEditor.Formats.Alembic.Importer.AlembicInputSettings, Unity.Formats.Alembic.Editor",
                    "Unity.Formats.Alembic.Runtime.AlembicInputSettings, Unity.Formats.Alembic.Runtime"
                };
                
                foreach (var typeName in possibleTypeNames)
                {
                    alembicInputSettingsType = System.Type.GetType(typeName);
                    if (alembicInputSettingsType != null)
                    {
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Found AlembicInputSettings type: {typeName}");
                        break;
                    }
                }
                
                if (alembicInputSettingsType == null)
                {
                    // Try to get input settings type from the recorder settings
                    var inputSettingsProperty = settingsType.GetProperty("InputSettings");
                    if (inputSettingsProperty != null)
                    {
                        var defaultInputSettings = inputSettingsProperty.GetValue(settings);
                        if (defaultInputSettings != null)
                        {
                            alembicInputSettingsType = defaultInputSettings.GetType();
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Found InputSettings type from settings: {alembicInputSettingsType.FullName}");
                        }
                    }
                }
                
                if (alembicInputSettingsType != null)
                {
                    // Create instance of input settings
                    var inputSettings = ScriptableObject.CreateInstance(alembicInputSettingsType);
                    if (inputSettings != null)
                    {
                        BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Created AlembicInputSettings instance");
                        
                        // Set properties on input settings
                        var inputType = inputSettings.GetType();
                        
                        // Try to set GameObject
                        if (exportScope == AlembicExportScope.TargetGameObject && targetGameObject != null)
                        {
                            SetPropertyValue(inputSettings, inputType, "GameObject", targetGameObject);
                            SetPropertyValue(inputSettings, inputType, "gameObject", targetGameObject);
                            SetPropertyValue(inputSettings, inputType, "TargetGameObject", targetGameObject);
                            SetPropertyValue(inputSettings, inputType, "targetGameObject", targetGameObject);
                            SetPropertyValue(inputSettings, inputType, "TargetBranch", targetGameObject);
                            SetPropertyValue(inputSettings, inputType, "targetBranch", targetGameObject);
                        }
                        
                        // Try to set scope
                        SetPropertyValue(inputSettings, inputType, "CaptureScope", exportScope);
                        SetPropertyValue(inputSettings, inputType, "captureScope", exportScope);
                        SetPropertyValue(inputSettings, inputType, "Scope", exportScope);
                        SetPropertyValue(inputSettings, inputType, "scope", exportScope);
                        
                        // Set the input settings on the recorder settings
                        var inputSettingsProperty = settingsType.GetProperty("InputSettings");
                        if (inputSettingsProperty != null && inputSettingsProperty.CanWrite)
                        {
                            inputSettingsProperty.SetValue(settings, inputSettings);
                            BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Set InputSettings on recorder settings");
                        }
                        else
                        {
                            // Try to set via method
                            var setInputMethod = settingsType.GetMethod("SetInputSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (setInputMethod != null)
                            {
                                setInputMethod.Invoke(settings, new object[] { inputSettings });
                                BatchRenderingToolLogger.Log($"[AlembicRecorderSettingsConfig] Set InputSettings via method");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogWarning($"[AlembicRecorderSettingsConfig] Failed to setup AlembicInputSettings: {e.Message}");
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