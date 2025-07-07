using UnityEditor;
using UnityEngine;
using BatchRenderingTool;

namespace BatchRenderingTool.DebugTools
{
    /// <summary>
    /// Simple test to debug FBX Recorder Component issue
    /// </summary>
    public class FBXRecorderTest
    {
        [MenuItem("Tools/Batch Rendering Tool/Test FBX Recorder")]
        public static void TestFBXRecorder()
        {
            // Create a test GameObject with Camera
            var testGO = new GameObject("FBX Test Camera");
            var camera = testGO.AddComponent<Camera>();
            
            try
            {
                // Create FBX settings
                var config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = testGO,
                    recordedComponent = FBXRecordedComponent.Camera,
                    recordHierarchy = true,
                    frameRate = 24f
                };
                
                var settings = config.CreateFBXRecorderSettings("TestFBX");
                
                if (settings != null)
                {
                    UnityEngine.Debug.Log($"✓ FBX Settings created: {settings.GetType().Name}");
                    
                    // Check AnimationInputSettings
                    var settingsType = settings.GetType();
                    var animInputProp = settingsType.GetProperty("AnimationInputSettings");
                    if (animInputProp != null)
                    {
                        var animInput = animInputProp.GetValue(settings);
                        if (animInput != null)
                        {
                            UnityEngine.Debug.Log($"✓ AnimationInputSettings found: {animInput.GetType().Name}");
                            
                            // Check GameObject property
                            var goProperty = animInput.GetType().GetProperty("gameObject");
                            if (goProperty != null)
                            {
                                var go = goProperty.GetValue(animInput) as GameObject;
                                UnityEngine.Debug.Log($"GameObject in AnimationInputSettings: {(go != null ? go.name : "NULL")}");
                            }
                            
                            // Check component property
                            var compProperty = animInput.GetType().GetProperty("component");
                            if (compProperty != null)
                            {
                                var comp = compProperty.GetValue(animInput) as Component;
                                UnityEngine.Debug.Log($"Component in AnimationInputSettings: {(comp != null ? comp.GetType().Name : "NULL")}");
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("✗ AnimationInputSettings is NULL");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("✗ AnimationInputSettings property not found");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("✗ Failed to create FBX settings");
                }
            }
            finally
            {
                // Cleanup
                Object.DestroyImmediate(testGO);
            }
        }
    }
}