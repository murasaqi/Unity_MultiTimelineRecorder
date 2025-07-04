using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;

namespace BatchRenderingTool.Editor
{
    public class RecorderQuickTest : EditorWindow
    {
        [MenuItem("Window/Batch Rendering Tool/Recorder Quick Test")]
        public static void ShowWindow()
        {
            GetWindow<RecorderQuickTest>("Recorder Quick Test");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity Recorder Quick Test", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Test 1: Create Timeline Asset"))
            {
                Debug.Log("[RecorderQuickTest] === Test 1: Create Timeline Asset ===");
                
                try
                {
                    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                    if (timeline != null)
                    {
                        Debug.Log($"[RecorderQuickTest] ✓ Successfully created TimelineAsset: {timeline.GetType().FullName}");
                        DestroyImmediate(timeline);
                    }
                    else
                    {
                        Debug.LogError("[RecorderQuickTest] ✗ Failed to create TimelineAsset");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RecorderQuickTest] ✗ Exception creating TimelineAsset: {e}");
                }
            }
            
            if (GUILayout.Button("Test 2: Create RecorderTrack"))
            {
                Debug.Log("[RecorderQuickTest] === Test 2: Create RecorderTrack ===");
                
                try
                {
                    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                    if (timeline != null)
                    {
                        var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Test Recorder Track");
                        if (recorderTrack != null)
                        {
                            Debug.Log($"[RecorderQuickTest] ✓ Successfully created RecorderTrack: {recorderTrack.GetType().FullName}");
                        }
                        else
                        {
                            Debug.LogError("[RecorderQuickTest] ✗ Failed to create RecorderTrack");
                        }
                        DestroyImmediate(timeline);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RecorderQuickTest] ✗ Exception creating RecorderTrack: {e}");
                }
            }
            
            if (GUILayout.Button("Test 3: Create RecorderClip"))
            {
                Debug.Log("[RecorderQuickTest] === Test 3: Create RecorderClip ===");
                
                try
                {
                    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                    if (timeline != null)
                    {
                        var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Test Recorder Track");
                        if (recorderTrack != null)
                        {
                            var recorderClip = recorderTrack.CreateClip<RecorderClip>();
                            if (recorderClip != null)
                            {
                                Debug.Log($"[RecorderQuickTest] ✓ Successfully created RecorderClip");
                                Debug.Log($"[RecorderQuickTest]   - Clip type: {recorderClip.GetType().FullName}");
                                Debug.Log($"[RecorderQuickTest]   - Asset type: {recorderClip.asset?.GetType().FullName ?? "null"}");
                            }
                            else
                            {
                                Debug.LogError("[RecorderQuickTest] ✗ Failed to create RecorderClip");
                            }
                        }
                        DestroyImmediate(timeline);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RecorderQuickTest] ✗ Exception creating RecorderClip: {e}");
                }
            }
            
            if (GUILayout.Button("Test 4: Create ImageRecorderSettings"))
            {
                Debug.Log("[RecorderQuickTest] === Test 4: Create ImageRecorderSettings ===");
                
                try
                {
                    // Method 1: Direct creation
                    var settings1 = ScriptableObject.CreateInstance<ImageRecorderSettings>();
                    if (settings1 != null)
                    {
                        Debug.Log($"[RecorderQuickTest] ✓ Method 1 (Direct): Created {settings1.GetType().FullName}");
                        DestroyImmediate(settings1);
                    }
                    else
                    {
                        Debug.LogError("[RecorderQuickTest] ✗ Method 1 (Direct): Failed");
                    }
                    
                    // Method 2: Using RecorderClipUtility
                    var settings2 = RecorderClipUtility.CreateProperImageRecorderSettings("Test");
                    if (settings2 != null)
                    {
                        Debug.Log($"[RecorderQuickTest] ✓ Method 2 (RecorderClipUtility): Created {settings2.GetType().FullName}");
                        DestroyImmediate(settings2);
                    }
                    else
                    {
                        Debug.LogError("[RecorderQuickTest] ✗ Method 2 (RecorderClipUtility): Failed");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RecorderQuickTest] ✗ Exception creating ImageRecorderSettings: {e}");
                }
            }
            
            if (GUILayout.Button("Test 5: Full Timeline Creation"))
            {
                Debug.Log("[RecorderQuickTest] === Test 5: Full Timeline Creation ===");
                
                try
                {
                    // Create timeline
                    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                    timeline.name = "TestTimeline";
                    Debug.Log("[RecorderQuickTest] ✓ Created timeline");
                    
                    // Create temporary asset
                    string tempPath = "Assets/TestTimeline_temp.playable";
                    AssetDatabase.CreateAsset(timeline, tempPath);
                    Debug.Log($"[RecorderQuickTest] ✓ Saved timeline to: {tempPath}");
                    
                    // Create recorder track
                    var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Recorder Track");
                    Debug.Log("[RecorderQuickTest] ✓ Created RecorderTrack");
                    
                    // Create recorder clip
                    var recorderClip = recorderTrack.CreateClip<RecorderClip>();
                    Debug.Log("[RecorderQuickTest] ✓ Created RecorderClip");
                    
                    // Create and assign settings
                    var settings = RecorderClipUtility.CreateProperImageRecorderSettings("Test");
                    if (settings != null)
                    {
                        var recorderAsset = recorderClip.asset as RecorderClip;
                        if (recorderAsset != null)
                        {
                            recorderAsset.settings = settings;
                            Debug.Log("[RecorderQuickTest] ✓ Assigned RecorderSettings");
                            
                            // Save as sub-asset
                            AssetDatabase.AddObjectToAsset(settings, timeline);
                            EditorUtility.SetDirty(timeline);
                            AssetDatabase.SaveAssets();
                            Debug.Log("[RecorderQuickTest] ✓ Saved settings as sub-asset");
                        }
                    }
                    
                    // Cleanup
                    AssetDatabase.DeleteAsset(tempPath);
                    Debug.Log("[RecorderQuickTest] ✓ Cleaned up temporary asset");
                    
                    Debug.Log("[RecorderQuickTest] === Test completed successfully ===");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RecorderQuickTest] ✗ Exception: {e}");
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Check the Console for test results", MessageType.Info);
        }
    }
}