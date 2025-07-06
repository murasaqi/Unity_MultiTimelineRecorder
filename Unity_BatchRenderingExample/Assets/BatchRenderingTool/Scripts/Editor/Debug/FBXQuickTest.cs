using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;

namespace BatchRenderingTool.DebugTools
{
    /// <summary>
    /// FBXレコーダーの簡易テストツール
    /// </summary>
    public class FBXQuickTest : EditorWindow
    {
        private GameObject targetGameObject;
        private string testResult = "Click 'Run FBX Test' to start";
        
        [MenuItem("Window/Batch Rendering Tool/Debug/FBX Quick Test")]
        public static void ShowWindow()
        {
            GetWindow<FBXQuickTest>("FBX Quick Test");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("FBX Recorder Quick Test", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("This tool tests FBX recorder creation and configuration.", MessageType.Info);
            
            targetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetGameObject, typeof(GameObject), true);
            
            if (GUILayout.Button("Run FBX Test", GUILayout.Height(30)))
            {
                RunFBXTest();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Result:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(testResult, GUILayout.MinHeight(200));
        }
        
        private void RunFBXTest()
        {
            testResult = "";
            
            try
            {
                // Step 1: Check FBX package
                AddLog("=== Step 1: Checking FBX Package ===");
                var fbxPackage = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    .FirstOrDefault(p => p.name == "com.unity.formats.fbx");
                
                if (fbxPackage != null)
                {
                    AddLog($"✓ FBX Package found: {fbxPackage.version}");
                }
                else
                {
                    AddLog("✗ FBX Package NOT found!");
                    AddLog("Please install 'Unity FBX Exporter' package from Package Manager");
                    return;
                }
                
                // Step 2: Try to create FBX recorder settings
                AddLog("\n=== Step 2: Creating FBX Recorder Settings ===");
                
                var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
                if (settings != null)
                {
                    AddLog($"✓ Created settings: {settings.GetType().FullName}");
                    
                    // Check if it's actually FBX settings
                    if (settings.GetType().Name.Contains("FBX") || settings.GetType().Name.Contains("Fbx"))
                    {
                        AddLog("✓ Settings type is FBX recorder");
                    }
                    else
                    {
                        AddLog($"✗ WARNING: Settings type is {settings.GetType().Name}, not FBX!");
                    }
                }
                else
                {
                    AddLog("✗ Failed to create FBX recorder settings");
                    
                    // Try direct creation
                    AddLog("\n=== Step 3: Trying Direct Creation ===");
                    TestDirectCreation();
                }
                
                // Step 4: Test with configuration
                if (targetGameObject != null)
                {
                    AddLog("\n=== Step 4: Testing with Target GameObject ===");
                    TestWithConfiguration();
                }
                else
                {
                    AddLog("\n=== Step 4: Skipped (no target GameObject selected) ===");
                }
            }
            catch (Exception e)
            {
                AddLog($"\n✗ EXCEPTION: {e.GetType().Name}");
                AddLog($"Message: {e.Message}");
                AddLog($"Stack trace:\n{e.StackTrace}");
            }
        }
        
        private void TestDirectCreation()
        {
            // Look for FBX recorder type
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.Contains("Fbx") || assembly.FullName.Contains("FBX"))
                {
                    AddLog($"Found FBX assembly: {assembly.FullName}");
                    
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name.Contains("FbxRecorderSettings") && typeof(RecorderSettings).IsAssignableFrom(type))
                        {
                            AddLog($"Found FBX recorder type: {type.FullName}");
                            
                            try
                            {
                                var instance = ScriptableObject.CreateInstance(type) as RecorderSettings;
                                if (instance != null)
                                {
                                    AddLog($"✓ Successfully created instance of {type.Name}");
                                    UnityEngine.Object.DestroyImmediate(instance);
                                }
                            }
                            catch (Exception e)
                            {
                                AddLog($"✗ Failed to create instance: {e.Message}");
                            }
                        }
                    }
                }
            }
        }
        
        private void TestWithConfiguration()
        {
            try
            {
                var config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = targetGameObject,
                    recordHierarchy = true,
                    clampedTangents = true,
                    animationCompression = FBXAnimationCompressionLevel.Lossy,
                    exportGeometry = true,
                    frameRate = 24f
                };
                
                AddLog($"Created config for: {targetGameObject.name}");
                
                string errorMessage;
                if (config.Validate(out errorMessage))
                {
                    AddLog("✓ Config validation passed");
                    
                    var settings = config.CreateFBXRecorderSettings("TestFBX");
                    if (settings != null)
                    {
                        AddLog($"✓ Successfully created FBX settings with config");
                        AddLog($"  Type: {settings.GetType().FullName}");
                        UnityEngine.Object.DestroyImmediate(settings);
                    }
                    else
                    {
                        AddLog("✗ Failed to create FBX settings with config");
                    }
                }
                else
                {
                    AddLog($"✗ Config validation failed: {errorMessage}");
                }
            }
            catch (Exception e)
            {
                AddLog($"✗ Exception in config test: {e.Message}");
            }
        }
        
        private void AddLog(string message)
        {
            testResult += message + "\n";
        }
    }
}