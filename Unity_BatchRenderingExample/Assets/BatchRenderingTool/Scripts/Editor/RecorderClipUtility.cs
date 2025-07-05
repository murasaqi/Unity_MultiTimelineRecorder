using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;
using System.Reflection;

namespace BatchRenderingTool
{
    /// <summary>
    /// Utility class for properly configuring RecorderClip with ImageRecorderSettings
    /// </summary>
    public static class RecorderClipUtility
    {
        private static MethodInfo s_GetRecorderTypeMethod;
        private static MethodInfo s_SetRecorderTypeMethod;
        /// <summary>
        /// Creates and configures a RecorderClip with ImageRecorderSettings
        /// </summary>
        public static RecorderClip CreateImageRecorderClip(TimelineAsset timeline, ImageRecorderSettings settings)
        {
            // Create RecorderClip instance
            var recorderClip = ScriptableObject.CreateInstance<RecorderClip>();
            
            // CRITICAL: Set the settings immediately before the clip is added to timeline
            recorderClip.settings = settings;
            
            // Use reflection to ensure the recorder type is properly initialized
            EnsureRecorderTypeIsSet(recorderClip, settings);
            
            // Force the clip to recognize the settings type
            EditorUtility.SetDirty(recorderClip);
            
            return recorderClip;
        }
        
        public static void EnsureRecorderTypeIsSet(RecorderClip clip, RecorderSettings settings)
        {
            try
            {
                // Get reflection methods if not cached
                if (s_GetRecorderTypeMethod == null || s_SetRecorderTypeMethod == null)
                {
                    var recorderClipType = typeof(RecorderClip);
                    var recorderTypeProperty = recorderClipType.GetProperty("recorderType", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (recorderTypeProperty != null)
                    {
                        s_GetRecorderTypeMethod = recorderTypeProperty.GetGetMethod(true);
                        s_SetRecorderTypeMethod = recorderTypeProperty.GetSetMethod(true);
                    }
                }
                
                if (s_GetRecorderTypeMethod != null && s_SetRecorderTypeMethod != null)
                {
                    var currentType = s_GetRecorderTypeMethod.Invoke(clip, null) as System.Type;
                    var expectedType = settings.GetType();
                    
                    if (currentType != expectedType)
                    {
                        s_SetRecorderTypeMethod.Invoke(clip, new object[] { expectedType });
                        BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Set recorder type from {currentType} to {expectedType}");
                    }
                }
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogWarning($"[RecorderClipUtility] Failed to set recorder type via reflection: {e.Message}");
            }
        }
        
        /// <summary>
        /// Ensures RecorderSettings is properly configured for image recording
        /// </summary>
        public static ImageRecorderSettings CreateProperImageRecorderSettings(string name)
        {
            // Use RecordersInventory to create default settings if available
            var inventoryType = System.Type.GetType("UnityEditor.Recorder.RecordersInventory, Unity.Recorder.Editor");
            if (inventoryType != null)
            {
                var createMethod = inventoryType.GetMethod("CreateDefaultRecorderSettings", BindingFlags.Public | BindingFlags.Static);
                if (createMethod != null)
                {
                    try
                    {
                        var settings = createMethod.Invoke(null, new object[] { typeof(ImageRecorderSettings) }) as ImageRecorderSettings;
                        if (settings != null)
                        {
                            settings.name = name;
                            BatchRenderingToolLogger.LogVerbose("[RecorderClipUtility] Created ImageRecorderSettings using RecordersInventory");
                            return settings;
                        }
                    }
                    catch (System.Exception e)
                    {
                        BatchRenderingToolLogger.LogWarning($"[RecorderClipUtility] Failed to use RecordersInventory: {e.Message}");
                    }
                }
            }
            
            // Fallback to standard creation with explicit type setting
            var imageSettings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            imageSettings.name = name;
            
            // Force initialize the recorder settings type
            // This ensures Unity Recorder recognizes it as an Image Recorder
            imageSettings.hideFlags = HideFlags.None;
            
            // Initialize default values to ensure proper setup
            imageSettings.RecordMode = RecordMode.Manual;
            imageSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            imageSettings.FrameRate = 24;
            imageSettings.CapFrameRate = true;
            
            EditorUtility.SetDirty(imageSettings);
            
            BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Created ImageRecorderSettings directly: {imageSettings.GetType().FullName}");
            return imageSettings;
        }
        
        /// <summary>
        /// Ensures RecorderSettings is properly configured for Alembic recording
        /// </summary>
        public static RecorderSettings CreateProperAlembicRecorderSettings(string name)
        {
            // Use RecordersInventory to create default settings if available
            var inventoryType = System.Type.GetType("UnityEditor.Recorder.RecordersInventory, Unity.Recorder.Editor");
            if (inventoryType != null)
            {
                var createMethod = inventoryType.GetMethod("CreateDefaultRecorderSettings", BindingFlags.Public | BindingFlags.Static);
                if (createMethod != null)
                {
                    try
                    {
                        // Try to find AlembicRecorderSettings type
                        var alembicType = System.Type.GetType("UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor");
                        if (alembicType != null)
                        {
                            var settings = createMethod.Invoke(null, new object[] { alembicType }) as RecorderSettings;
                            if (settings != null)
                            {
                                settings.name = name;
                                BatchRenderingToolLogger.LogVerbose("[RecorderClipUtility] Created AlembicRecorderSettings using RecordersInventory");
                                return settings;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        BatchRenderingToolLogger.LogWarning($"[RecorderClipUtility] Failed to use RecordersInventory for Alembic: {e.Message}");
                    }
                }
            }
            
            // Fallback to direct creation
            var alembicRecorderType = System.Type.GetType("UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor");
            if (alembicRecorderType != null)
            {
                var alembicSettings = ScriptableObject.CreateInstance(alembicRecorderType) as RecorderSettings;
                if (alembicSettings != null)
                {
                    alembicSettings.name = name;
                    alembicSettings.hideFlags = HideFlags.None;
                    
                    // Initialize default values
                    alembicSettings.RecordMode = RecordMode.Manual;
                    alembicSettings.FrameRatePlayback = FrameRatePlayback.Constant;
                    alembicSettings.FrameRate = 24;
                    alembicSettings.CapFrameRate = true;
                    
                    EditorUtility.SetDirty(alembicSettings);
                    
                    BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Created AlembicRecorderSettings directly: {alembicSettings.GetType().FullName}");
                    return alembicSettings;
                }
            }
            
            BatchRenderingToolLogger.LogError("[RecorderClipUtility] Failed to create AlembicRecorderSettings. Make sure Alembic package is installed.");
            return null;
        }
        
        /// <summary>
        /// Creates and configures a RecorderClip with AlembicRecorderSettings
        /// </summary>
        public static RecorderClip CreateAlembicRecorderClip(TimelineAsset timeline, RecorderSettings settings)
        {
            // Create RecorderClip instance
            var recorderClip = ScriptableObject.CreateInstance<RecorderClip>();
            
            // CRITICAL: Set the settings immediately before the clip is added to timeline
            recorderClip.settings = settings;
            
            // Use reflection to ensure the recorder type is properly initialized
            EnsureRecorderTypeIsSet(recorderClip, settings);
            
            // For Alembic, try to set additional fields via reflection
            ApplyAlembicSpecificSettings(recorderClip, settings);
            
            // Force the clip to recognize the settings type
            EditorUtility.SetDirty(recorderClip);
            
            return recorderClip;
        }
        
        /// <summary>
        /// Apply Alembic-specific settings to RecorderClip
        /// </summary>
        private static void ApplyAlembicSpecificSettings(RecorderClip clip, RecorderSettings settings)
        {
            try
            {
                var clipType = clip.GetType();
                var settingsType = settings.GetType();
                
                // Log available fields on RecorderClip
                BatchRenderingToolLogger.LogVerbose("[RecorderClipUtility] RecorderClip fields:");
                var clipFields = clipType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                foreach (var field in clipFields)
                {
                    BatchRenderingToolLogger.LogVerbose($"  - {field.Name} ({field.FieldType.Name})");
                }
                
                // Check if settings has any GameObject reference
                var settingsFields = settingsType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                GameObject targetGameObject = null;
                foreach (var field in settingsFields)
                {
                    if (field.FieldType == typeof(GameObject))
                    {
                        var value = field.GetValue(settings) as GameObject;
                        if (value != null)
                        {
                            targetGameObject = value;
                            BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Found GameObject in settings field {field.Name}: {targetGameObject.name}");
                            break;
                        }
                    }
                }
                
                // If we found a target GameObject, try to set it on the clip
                if (targetGameObject != null)
                {
                    foreach (var field in clipFields)
                    {
                        if (field.FieldType == typeof(GameObject) || 
                            field.Name.ToLower().Contains("target") || 
                            field.Name.ToLower().Contains("gameobject"))
                        {
                            field.SetValue(clip, targetGameObject);
                            BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Set RecorderClip field {field.Name} to {targetGameObject.name}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogWarning($"[RecorderClipUtility] Failed to apply Alembic-specific settings: {e.Message}");
            }
        }
    }
}