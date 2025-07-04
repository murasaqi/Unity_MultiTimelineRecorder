using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using System.Linq;

namespace BatchRenderingTool
{
    public static class SimpleRenderTest
    {
        [MenuItem("Window/Batch Rendering Tool/Debug/Simple Render Test")]
        public static void RunSimpleRenderTest()
        {
            Debug.Log("===== Simple Render Test Started =====");
            
            // 1. Find PlayableDirectors
            var directors = GameObject.FindObjectsOfType<PlayableDirector>();
            Debug.Log($"[TEST] Found {directors.Length} PlayableDirectors in scene");
            
            foreach (var director in directors)
            {
                Debug.Log($"[TEST] Director: {director.name}, PlayOnAwake: {director.playOnAwake}, Asset: {director.playableAsset?.name}");
            }
            
            // 2. Open SingleTimelineRenderer window
            var window = SingleTimelineRenderer.ShowWindow();
            if (window == null)
            {
                Debug.LogError("[TEST] Failed to open SingleTimelineRenderer window");
                return;
            }
            
            Debug.Log("[TEST] SingleTimelineRenderer window opened successfully");
            
            // 3. Check if directors were scanned
            var availableDirectors = window.GetAllPlayableDirectors();
            Debug.Log($"[TEST] Window has {availableDirectors.Count} directors");
            
            if (availableDirectors.Count == 0)
            {
                Debug.LogError("[TEST] No directors found in SingleTimelineRenderer window");
                return;
            }
            
            // 4. Validate settings
            string errorMessage;
            bool isValid = window.ValidateSettings(out errorMessage);
            Debug.Log($"[TEST] Settings valid: {isValid}");
            if (!isValid)
            {
                Debug.LogError($"[TEST] Validation error: {errorMessage}");
            }
            
            Debug.Log("===== Simple Render Test Completed =====");
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Force Refresh Timeline List")]
        public static void ForceRefreshTimelineList()
        {
            var window = EditorWindow.GetWindow<SingleTimelineRenderer>();
            if (window != null)
            {
                // Use reflection to call ScanTimelines
                var scanMethod = typeof(SingleTimelineRenderer).GetMethod("ScanTimelines", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (scanMethod != null)
                {
                    scanMethod.Invoke(window, null);
                    Debug.Log("[TEST] Forced timeline scan completed");
                }
                else
                {
                    Debug.LogError("[TEST] Could not find ScanTimelines method");
                }
            }
            else
            {
                Debug.LogError("[TEST] SingleTimelineRenderer window not found");
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Create Test Timeline")]
        public static void CreateTestTimeline()
        {
            Debug.Log("[TEST] Creating test Timeline setup...");
            
            // Create a GameObject with PlayableDirector
            var testGO = new GameObject("Test Timeline Object");
            var director = testGO.AddComponent<PlayableDirector>();
            
            // Create a Timeline asset
            var timeline = ScriptableObject.CreateInstance<UnityEngine.Timeline.TimelineAsset>();
            timeline.name = "Test Timeline";
            
            // Create a simple animation track
            var track = timeline.CreateTrack<UnityEngine.Timeline.AnimationTrack>(null, "Test Track");
            
            // Assign timeline to director
            director.playableAsset = timeline;
            director.playOnAwake = false;
            
            Debug.Log($"[TEST] Created test timeline: {testGO.name} with timeline asset: {timeline.name}");
            
            // Save as temporary asset
            string path = "Assets/BatchRenderingTool/Temp/TestTimeline.playable";
            if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool/Temp"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                {
                    AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                }
                AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
            }
            
            AssetDatabase.CreateAsset(timeline, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[TEST] Saved timeline asset at: {path}");
            
            // Select the GameObject
            Selection.activeGameObject = testGO;
        }
    }
}