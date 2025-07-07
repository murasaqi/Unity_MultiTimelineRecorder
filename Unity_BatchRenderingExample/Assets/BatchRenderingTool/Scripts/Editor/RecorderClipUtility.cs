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
                
                // Simplified: Just try to apply settings without excessive logging
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogWarning($"[RecorderClipUtility] Failed to apply Alembic-specific settings: {e.Message}");
            }
        }
        
        /// <summary>
        /// Creates RecorderSettings based on type name
        /// </summary>
        public static RecorderSettings CreateProperRecorderSettings(string typeName)
        {
            BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Creating recorder settings for type: {typeName}");
            
            // Use RecordersInventory to create default settings if available
            var inventoryType = System.Type.GetType("UnityEditor.Recorder.RecordersInventory, Unity.Recorder.Editor");
            if (inventoryType != null)
            {
                var createMethod = inventoryType.GetMethod("CreateDefaultRecorderSettings", BindingFlags.Public | BindingFlags.Static);
                if (createMethod != null)
                {
                    try
                    {
                        BatchRenderingToolLogger.Log($"[RecorderClipUtility] Using RecordersInventory to create: {typeName}");
                        
                        // Try to find the recorder settings type
                        System.Type settingsType = null;
                        
                        switch (typeName.ToLower())
                        {
                            case "imagerecordersettings":
                            case "imagerecorder":
                                settingsType = typeof(ImageRecorderSettings);
                                break;
                                
                            case "movierecordersettings":
                            case "movierecorder":
                                settingsType = typeof(MovieRecorderSettings);
                                break;
                                
                            case "animationrecordersettings":
                            case "animationrecorder":
                                settingsType = typeof(AnimationRecorderSettings);
                                break;
                                
                            case "alembicrecordersettings":
                            case "alembicrecorder":
                                settingsType = System.Type.GetType("UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor");
                                break;
                                
                            case "fbxrecordersettings":
                            case "fbxrecorder":
                                BatchRenderingToolLogger.Log("[RecorderClipUtility] Looking for FBX recorder type...");
                                
                                // Search for FBX recorder type in assemblies
                                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    if (assembly.FullName.Contains("Fbx"))
                                    {
                                        try
                                        {
                                            var types = assembly.GetTypes();
                                            foreach (var type in types)
                                            {
                                                if (type.Name == "FbxRecorderSettings" && typeof(RecorderSettings).IsAssignableFrom(type))
                                                {
                                                    settingsType = type;
                                                    BatchRenderingToolLogger.Log($"[RecorderClipUtility] Found FBX type: {type.FullName}");
                                                    break;
                                                }
                                            }
                                        }
                                        catch { }
                                        if (settingsType != null) break;
                                    }
                                }
                                
                                if (settingsType == null)
                                {
                                    BatchRenderingToolLogger.LogError("[RecorderClipUtility] FbxRecorderSettings not found. Install Unity FBX Exporter package.");
                                    return null;
                                }
                                break;
                                
                            case "aovrecordersettings":
                            case "aovrecorder":
                                // AOV is typically an ImageRecorderSettings with special configuration
                                settingsType = typeof(ImageRecorderSettings);
                                break;
                        }
                        
                        if (settingsType != null)
                        {
                            BatchRenderingToolLogger.Log($"[RecorderClipUtility] About to create {settingsType.FullName} using RecordersInventory");
                            var settings = createMethod.Invoke(null, new object[] { settingsType }) as RecorderSettings;
                            if (settings != null)
                            {
                                settings.name = typeName;
                                BatchRenderingToolLogger.LogVerbose($"[RecorderClipUtility] Created {settingsType.Name} using RecordersInventory");
                                BatchRenderingToolLogger.Log($"[RecorderClipUtility] IMPORTANT: Actual created type is {settings.GetType().FullName}");
                                
                                // CRITICAL CHECK: If we requested FBX but got Animation, reject it
                                if (typeName.ToLower().Contains("fbx") && settings.GetType().Name.Contains("Animation"))
                                {
                                    BatchRenderingToolLogger.LogError($"[RecorderClipUtility] ERROR: Requested FBX recorder but got Animation recorder! Rejecting.");
                                    UnityEngine.Object.DestroyImmediate(settings);
                                    return null;
                                }
                                
                                return settings;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        BatchRenderingToolLogger.LogWarning($"[RecorderClipUtility] Failed to use RecordersInventory: {e.Message}");
                    }
                }
            }
            
            // Fallback to direct creation
            System.Type recorderType = null;
            
            switch (typeName.ToLower())
            {
                case "imagerecordersettings":
                case "imagerecorder":
                    return CreateProperImageRecorderSettings(typeName);
                    
                case "movierecordersettings":
                case "movierecorder":
                    var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                    movieSettings.name = typeName;
                    return movieSettings;
                    
                case "animationrecordersettings":
                case "animationrecorder":
                    var animSettings = ScriptableObject.CreateInstance<AnimationRecorderSettings>();
                    animSettings.name = typeName;
                    return animSettings;
                    
                case "alembicrecordersettings":
                case "alembicrecorder":
                    return CreateProperAlembicRecorderSettings(typeName);
                    
                case "fbxrecordersettings":
                case "fbxrecorder":
                    // Search for FBX recorder type in assemblies
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.FullName.Contains("Fbx"))
                        {
                            try
                            {
                                var types = assembly.GetTypes();
                                foreach (var type in types)
                                {
                                    if (type.Name == "FbxRecorderSettings" && typeof(RecorderSettings).IsAssignableFrom(type))
                                    {
                                        recorderType = type;
                                        break;
                                    }
                                }
                            }
                            catch { }
                            if (recorderType != null) break;
                        }
                    }
                    
                    if (recorderType != null)
                    {
                        var fbxSettings = ScriptableObject.CreateInstance(recorderType) as RecorderSettings;
                        if (fbxSettings != null)
                        {
                            fbxSettings.name = typeName;
                            fbxSettings.RecordMode = RecordMode.Manual;
                            fbxSettings.FrameRatePlayback = FrameRatePlayback.Constant;
                            fbxSettings.CapFrameRate = true;
                            EditorUtility.SetDirty(fbxSettings);
                            return fbxSettings;
                        }
                    }
                    
                    BatchRenderingToolLogger.LogError("[RecorderClipUtility] FbxRecorderSettings not found. Install Unity FBX Exporter package.");
                    return null;
                    
                case "aovrecordersettings":
                case "aovrecorder":
                    return CreateProperImageRecorderSettings(typeName);
            }
            
            BatchRenderingToolLogger.LogError($"[RecorderClipUtility] Unknown recorder type: {typeName}");
            return null;
        }
        
        /// <summary>
        /// Ensures RecorderSettings is properly configured for FBX recording
        /// </summary>
        public static RecorderSettings CreateProperFBXRecorderSettings(string name)
        {
            // Search for FBX recorder type in assemblies
            System.Type fbxRecorderType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Fbx"))
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.Name == "FbxRecorderSettings" && typeof(RecorderSettings).IsAssignableFrom(type))
                            {
                                fbxRecorderType = type;
                                break;
                            }
                        }
                    }
                    catch { }
                    if (fbxRecorderType != null) break;
                }
            }
            
            if (fbxRecorderType != null)
            {
                var fbxSettings = ScriptableObject.CreateInstance(fbxRecorderType) as RecorderSettings;
                if (fbxSettings != null)
                {
                    fbxSettings.name = name;
                    fbxSettings.RecordMode = RecordMode.Manual;
                    fbxSettings.FrameRatePlayback = FrameRatePlayback.Constant;
                    fbxSettings.CapFrameRate = true;
                    EditorUtility.SetDirty(fbxSettings);
                    return fbxSettings;
                }
            }
            
            BatchRenderingToolLogger.LogError("[RecorderClipUtility] FbxRecorderSettings not found. Install Unity FBX Exporter package.");
            return null;
        }
    }
}