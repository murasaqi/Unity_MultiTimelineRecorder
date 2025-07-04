using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace BatchRenderingTool
{
    public class SimpleTimelineTest : EditorWindow
    {
        private PlayableDirector testDirector;
        private TimelineAsset testTimeline;
        
        [MenuItem("Window/Batch Rendering Tool/Simple Timeline Test")]
        public static void ShowWindow()
        {
            var window = GetWindow<SimpleTimelineTest>();
            window.titleContent = new GUIContent("Simple Timeline Test");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Simple Timeline Test", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Step 1: Create test timeline
            if (GUILayout.Button("1. Create Test Timeline", GUILayout.Height(30)))
            {
                CreateTestTimeline();
            }
            
            if (testDirector != null)
            {
                EditorGUILayout.HelpBox($"Test Timeline Created: {testDirector.name}", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Step 2: Check directors in scene
            if (GUILayout.Button("2. List All PlayableDirectors", GUILayout.Height(30)))
            {
                ListAllDirectors();
            }
            
            EditorGUILayout.Space();
            
            // Step 3: Open SingleTimelineRenderer
            if (GUILayout.Button("3. Open SingleTimelineRenderer", GUILayout.Height(30)))
            {
                var renderer = SingleTimelineRenderer.ShowWindow();
                if (renderer != null)
                {
                    Debug.Log("[TEST] SingleTimelineRenderer opened");
                    
                    // Force refresh
                    var scanMethod = typeof(SingleTimelineRenderer).GetMethod("ScanTimelines", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (scanMethod != null)
                    {
                        scanMethod.Invoke(renderer, null);
                        Debug.Log("[TEST] Forced timeline scan");
                    }
                }
            }
            
            EditorGUILayout.Space();
            
            // Step 4: Test rendering
            if (GUILayout.Button("4. Check Renderer State", GUILayout.Height(30)))
            {
                CheckRendererState();
            }
            
            EditorGUILayout.Space();
            
            // Cleanup
            if (GUILayout.Button("Clean Up Test Objects", GUILayout.Height(30)))
            {
                CleanUpTestObjects();
            }
        }
        
        private void CreateTestTimeline()
        {
            Debug.Log("[TEST] Creating test timeline...");
            
            // Clean up any existing test objects
            CleanUpTestObjects();
            
            // Create GameObject
            testDirector = new GameObject("Test Timeline Director").AddComponent<PlayableDirector>();
            
            // Create Timeline asset
            testTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            testTimeline.name = "Test Timeline Asset";
            
            // Create a simple animation track
            var track = testTimeline.CreateTrack<AnimationTrack>(null, "Test Animation Track");
            
            // Assign to director
            testDirector.playableAsset = testTimeline;
            testDirector.playOnAwake = false;
            
            // Save timeline asset
            string path = "Assets/BatchRenderingTool/Temp/TestTimeline.playable";
            AssetDatabase.CreateAsset(testTimeline, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[TEST] Created timeline at: {path}");
            Debug.Log($"[TEST] Director: {testDirector.name}, Timeline: {testTimeline.name}");
            
            // Select the GameObject
            Selection.activeGameObject = testDirector.gameObject;
        }
        
        private void ListAllDirectors()
        {
            var directors = GameObject.FindObjectsOfType<PlayableDirector>();
            Debug.Log($"[TEST] Found {directors.Length} PlayableDirectors:");
            
            foreach (var dir in directors)
            {
                var timeline = dir.playableAsset as TimelineAsset;
                Debug.Log($"[TEST] - {dir.name}:");
                Debug.Log($"[TEST]   PlayableAsset: {dir.playableAsset?.name ?? "null"}");
                Debug.Log($"[TEST]   Is Timeline: {timeline != null}");
                Debug.Log($"[TEST]   Asset Type: {dir.playableAsset?.GetType().Name ?? "null"}");
                Debug.Log($"[TEST]   PlayOnAwake: {dir.playOnAwake}");
            }
        }
        
        private void CheckRendererState()
        {
            var renderer = EditorWindow.GetWindow<SingleTimelineRenderer>(false);
            if (renderer == null)
            {
                Debug.LogError("[TEST] SingleTimelineRenderer window not found");
                return;
            }
            
            Debug.Log("[TEST] Checking SingleTimelineRenderer state...");
            
            // Get available directors
            var directors = renderer.GetAllPlayableDirectors();
            Debug.Log($"[TEST] Renderer has {directors.Count} directors");
            
            foreach (var dir in directors)
            {
                Debug.Log($"[TEST] - {dir?.name ?? "null"}");
            }
            
            // Check validation
            string error;
            bool valid = renderer.ValidateSettings(out error);
            Debug.Log($"[TEST] Validation: {valid} - {error}");
        }
        
        private void CleanUpTestObjects()
        {
            if (testDirector != null)
            {
                DestroyImmediate(testDirector.gameObject);
                testDirector = null;
            }
            
            // Delete test timeline asset
            string path = "Assets/BatchRenderingTool/Temp/TestTimeline.playable";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[TEST] Cleaned up test objects");
        }
    }
}