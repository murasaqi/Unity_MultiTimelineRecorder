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
                        Debug.Log($"[RecorderClipUtility] Set recorder type from {currentType} to {expectedType}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RecorderClipUtility] Failed to set recorder type via reflection: {e.Message}");
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
                            Debug.Log("[RecorderClipUtility] Created ImageRecorderSettings using RecordersInventory");
                            return settings;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[RecorderClipUtility] Failed to use RecordersInventory: {e.Message}");
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
            
            Debug.Log($"[RecorderClipUtility] Created ImageRecorderSettings directly: {imageSettings.GetType().FullName}");
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
                        var alembicType = System.Type.GetType("UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor");
                        if (alembicType != null)
                        {
                            var settings = createMethod.Invoke(null, new object[] { alembicType }) as RecorderSettings;
                            if (settings != null)
                            {
                                settings.name = name;
                                Debug.Log("[RecorderClipUtility] Created AlembicRecorderSettings using RecordersInventory");
                                return settings;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[RecorderClipUtility] Failed to use RecordersInventory for Alembic: {e.Message}");
                    }
                }
            }
            
            // Fallback to direct creation
            var alembicRecorderType = System.Type.GetType("UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor");
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
                    
                    Debug.Log($"[RecorderClipUtility] Created AlembicRecorderSettings directly: {alembicSettings.GetType().FullName}");
                    return alembicSettings;
                }
            }
            
            Debug.LogError("[RecorderClipUtility] Failed to create AlembicRecorderSettings. Make sure Alembic package is installed.");
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
            
            // Force the clip to recognize the settings type
            EditorUtility.SetDirty(recorderClip);
            
            return recorderClip;
        }
    }
}