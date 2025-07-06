using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;

namespace BatchRenderingTool.DebugTools
{
    /// <summary>
    /// FBXレコーダーのデバッグツール
    /// </summary>
    public class FBXDebugTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> debugLogs = new List<string>();
        private GameObject testGameObject;
        
        [MenuItem("Window/Batch Rendering Tool/Debug/FBX Debug Tool")]
        public static void ShowWindow()
        {
            GetWindow<FBXDebugTool>("FBX Debug Tool");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("FBX Recorder Debug Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Test GameObject selection
            testGameObject = (GameObject)EditorGUILayout.ObjectField("Test GameObject", testGameObject, typeof(GameObject), true);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Test FBX Recorder Creation"))
            {
                TestFBXRecorderCreation();
            }
            
            if (GUILayout.Button("Analyze FBX Recorder Structure"))
            {
                AnalyzeFBXRecorderStructure();
            }
            
            if (GUILayout.Button("Test AnimationInputSettings"))
            {
                TestAnimationInputSettings();
            }
            
            if (GUILayout.Button("Test FBX Package Detection"))
            {
                TestFBXPackageDetection();
            }
            
            if (GUILayout.Button("Clear Logs"))
            {
                debugLogs.Clear();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Logs:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var log in debugLogs)
            {
                EditorGUILayout.TextArea(log, EditorStyles.textArea);
            }
            EditorGUILayout.EndScrollView();
        }
        
        private void TestFBXRecorderCreation()
        {
            debugLogs.Clear();
            AddLog("=== Testing FBX Recorder Creation ===");
            
            try
            {
                // Test with configuration
                var config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = testGameObject,
                    recordHierarchy = true,
                    clampedTangents = true,
                    animationCompression = FBXAnimationCompressionLevel.Lossy,
                    exportGeometry = true,
                    frameRate = 24f
                };
                
                AddLog($"Created config: targetGameObject={config.targetGameObject?.name ?? "null"}");
                
                // Validate configuration
                string errorMessage;
                if (!config.Validate(out errorMessage))
                {
                    AddLog($"ERROR: Config validation failed: {errorMessage}");
                    return;
                }
                
                AddLog("Config validation passed");
                
                // Create recorder settings
                var settings = config.CreateFBXRecorderSettings("TestFBX");
                
                if (settings == null)
                {
                    AddLog("ERROR: Failed to create FBX recorder settings (null result)");
                    return;
                }
                
                AddLog($"Created FBX recorder settings: {settings.GetType().FullName}");
                
                // Analyze the created settings
                AnalyzeRecorderSettings(settings);
            }
            catch (Exception e)
            {
                AddLog($"EXCEPTION: {e.GetType().Name}: {e.Message}");
                AddLog($"Stack trace: {e.StackTrace}");
            }
        }
        
        private void AnalyzeFBXRecorderStructure()
        {
            debugLogs.Clear();
            AddLog("=== Analyzing FBX Recorder Structure ===");
            
            try
            {
                // Create a dummy FBX recorder using reflection
                var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
                if (settings == null)
                {
                    AddLog("ERROR: Failed to create FbxRecorderSettings");
                    return;
                }
                
                var settingsType = settings.GetType();
                AddLog($"FBX Recorder Type: {settingsType.FullName}");
                
                // List all properties
                AddLog("\n--- Properties ---");
                var properties = settingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    AddLog($"Property: {prop.Name} ({prop.PropertyType.Name})");
                }
                
                // List all fields
                AddLog("\n--- Fields (including private) ---");
                var fields = settingsType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    AddLog($"Field: {field.Name} ({field.FieldType.Name}) [Private: {field.IsPrivate}]");
                }
                
                // Check for AnimationInputSettings
                AddLog("\n--- AnimationInputSettings Analysis ---");
                var animInputField = settingsType.GetField("m_AnimationInputSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                if (animInputField != null)
                {
                    AddLog($"Found m_AnimationInputSettings: {animInputField.FieldType.FullName}");
                    
                    var animInputSettings = animInputField.GetValue(settings);
                    if (animInputSettings != null)
                    {
                        AnalyzeAnimationInputSettings(animInputSettings);
                    }
                    else
                    {
                        AddLog("m_AnimationInputSettings is null - need to initialize");
                    }
                }
                else
                {
                    AddLog("m_AnimationInputSettings field not found!");
                }
            }
            catch (Exception e)
            {
                AddLog($"EXCEPTION: {e.GetType().Name}: {e.Message}");
                AddLog($"Stack trace: {e.StackTrace}");
            }
        }
        
        private void TestAnimationInputSettings()
        {
            debugLogs.Clear();
            AddLog("=== Testing AnimationInputSettings ===");
            
            try
            {
                var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
                if (settings == null)
                {
                    AddLog("ERROR: Failed to create FbxRecorderSettings");
                    return;
                }
                
                var settingsType = settings.GetType();
                
                // Get AnimationInputSettings
                var animInputField = settingsType.GetField("m_AnimationInputSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                if (animInputField == null)
                {
                    AddLog("ERROR: m_AnimationInputSettings field not found");
                    return;
                }
                
                var animInputSettings = animInputField.GetValue(settings);
                if (animInputSettings == null)
                {
                    AddLog("AnimationInputSettings is null, attempting to create...");
                    
                    // Try to create AnimationInputSettings
                    var animInputType = animInputField.FieldType;
                    animInputSettings = Activator.CreateInstance(animInputType);
                    animInputField.SetValue(settings, animInputSettings);
                    
                    AddLog($"Created AnimationInputSettings: {animInputType.FullName}");
                }
                
                // Try to set GameObject
                if (testGameObject != null)
                {
                    var animType = animInputSettings.GetType();
                    var gameObjectProp = animType.GetProperty("gameObject");
                    
                    if (gameObjectProp != null && gameObjectProp.CanWrite)
                    {
                        gameObjectProp.SetValue(animInputSettings, testGameObject);
                        AddLog($"Set GameObject to: {testGameObject.name}");
                        
                        // Verify it was set
                        var retrievedGO = gameObjectProp.GetValue(animInputSettings) as GameObject;
                        AddLog($"Retrieved GameObject: {retrievedGO?.name ?? "null"}");
                    }
                    else
                    {
                        AddLog("ERROR: gameObject property not found or not writable");
                    }
                }
                else
                {
                    AddLog("No test GameObject selected");
                }
            }
            catch (Exception e)
            {
                AddLog($"EXCEPTION: {e.GetType().Name}: {e.Message}");
                AddLog($"Stack trace: {e.StackTrace}");
            }
        }
        
        private void AnalyzeRecorderSettings(RecorderSettings settings)
        {
            AddLog("\n--- Analyzing Created Settings ---");
            
            var type = settings.GetType();
            
            // Check common properties
            AddLog($"Name: {settings.name}");
            AddLog($"Enabled: {settings.Enabled}");
            AddLog($"RecordMode: {settings.RecordMode}");
            AddLog($"FrameRate: {settings.FrameRate}");
            
            // Check for specific FBX properties
            var exportGeometryProp = type.GetProperty("ExportGeometry");
            if (exportGeometryProp != null)
            {
                var value = exportGeometryProp.GetValue(settings);
                AddLog($"ExportGeometry: {value}");
            }
            
            var transferSourceProp = type.GetProperty("TransferAnimationSource");
            if (transferSourceProp != null)
            {
                var value = transferSourceProp.GetValue(settings) as Transform;
                AddLog($"TransferAnimationSource: {value?.name ?? "null"}");
            }
        }
        
        private void AnalyzeAnimationInputSettings(object animInputSettings)
        {
            var animType = animInputSettings.GetType();
            AddLog($"AnimationInputSettings Type: {animType.FullName}");
            
            // List properties
            var properties = animType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(animInputSettings);
                    AddLog($"  {prop.Name}: {value?.ToString() ?? "null"}");
                }
                catch
                {
                    AddLog($"  {prop.Name}: (unable to read)");
                }
            }
        }
        
        private void TestFBXPackageDetection()
        {
            debugLogs.Clear();
            AddLog("=== Testing FBX Package Detection ===");
            
            // Check if FBX package is installed
            AddLog("Checking FBX Package installation...");
            bool isFBXInstalled = FBXExportInfo.IsFBXPackageAvailable();
            AddLog($"FBX Package Available: {isFBXInstalled}");
            
            // List all FBX-related assemblies
            AddLog("\n--- FBX-related Assemblies ---");
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Fbx") || assembly.FullName.Contains("FBX"))
                {
                    AddLog($"Assembly: {assembly.FullName}");
                    
                    // Try to list recorder-related types
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.Name.Contains("Recorder") || type.Name.Contains("Settings"))
                            {
                                AddLog($"  Type: {type.FullName}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        AddLog($"  Error listing types: {e.Message}");
                    }
                }
            }
            
            // Try each known FBX recorder type name
            AddLog("\n--- Testing FBX Recorder Type Names ---");
            var typeNames = new[]
            {
                "UnityEditor.Formats.Fbx.Exporter.FbxRecorderSettings, Unity.Formats.Fbx.Runtime.Editor",
                "UnityEditor.Formats.Fbx.Exporter.FbxRecorderSettings, Unity.Formats.Fbx.Editor",
                "UnityEditor.Recorder.FbxRecorderSettings, Unity.Formats.Fbx.Editor",
                "Unity.Formats.Fbx.Runtime.FbxRecorderSettings, Unity.Formats.Fbx.Runtime",
                "UnityEditor.Formats.Fbx.FbxRecorderSettings, Unity.Formats.Fbx.Editor",
                "UnityEditor.Formats.Fbx.Recorder.FbxRecorderSettings, Unity.Formats.Fbx.Editor",
                "UnityEditor.Recorder.FbxRecorderSettings, Unity.Recorder.Editor"
            };
            
            foreach (var typeName in typeNames)
            {
                var type = System.Type.GetType(typeName);
                if (type != null)
                {
                    AddLog($"✓ Found: {typeName}");
                    AddLog($"  Assembly: {type.Assembly.FullName}");
                }
                else
                {
                    AddLog($"✗ Not found: {typeName}");
                }
            }
            
            // Test direct creation
            AddLog("\n--- Testing Direct FBX Recorder Creation ---");
            try
            {
                var settings = RecorderClipUtility.CreateProperFBXRecorderSettings("TestFBX");
                if (settings != null)
                {
                    AddLog($"✓ Successfully created FBX recorder: {settings.GetType().FullName}");
                }
                else
                {
                    AddLog("✗ Failed to create FBX recorder settings");
                }
            }
            catch (Exception e)
            {
                AddLog($"✗ Exception during creation: {e.Message}");
            }
        }
        
        private void AddLog(string message)
        {
            debugLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[FBXDebugTool] {message}");
        }
    }
}