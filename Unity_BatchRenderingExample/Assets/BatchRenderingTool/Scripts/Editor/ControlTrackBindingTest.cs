using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;

namespace BatchRenderingTool
{
    public class ControlTrackBindingTest : EditorWindow
    {
        private PlayableDirector director;
        private TimelineAsset timeline;
        private ControlTrack controlTrack;
        private GameObject targetObject;
        
        [MenuItem("Window/Batch Rendering Tool/Control Track Binding Test")]
        public static void ShowWindow()
        {
            GetWindow<ControlTrackBindingTest>().Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Control Track Binding Test", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Test Setup"))
            {
                CreateTestSetup();
            }
            
            EditorGUILayout.Space();
            
            if (director != null && timeline != null)
            {
                EditorGUILayout.LabelField("Timeline Info:");
                EditorGUILayout.ObjectField("Timeline:", timeline, typeof(TimelineAsset), false);
                EditorGUILayout.ObjectField("Director:", director, typeof(PlayableDirector), true);
                EditorGUILayout.ObjectField("Target Object:", targetObject, typeof(GameObject), true);
                
                if (controlTrack != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Control Track Info:");
                    EditorGUILayout.ObjectField("Control Track:", controlTrack, typeof(ControlTrack), false);
                    
                    // Check binding
                    var binding = director.GetGenericBinding(controlTrack);
                    EditorGUILayout.ObjectField("Current Binding:", binding, typeof(Object), true);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Binding Methods:");
                    
                    if (GUILayout.Button("Method 1: SetGenericBinding on Track"))
                    {
                        director.SetGenericBinding(controlTrack, targetObject);
                        Debug.Log("Set binding via SetGenericBinding on track");
                    }
                    
                    if (GUILayout.Button("Method 2: Get Track Output and Bind"))
                    {
                        foreach (var output in timeline.outputs)
                        {
                            if (output.sourceObject == controlTrack)
                            {
                                director.SetGenericBinding(output.sourceObject, targetObject);
                                Debug.Log($"Set binding via output: {output.streamName}");
                            }
                        }
                    }
                    
                    if (GUILayout.Button("Method 3: Force Rebuild Graph"))
                    {
                        director.RebuildGraph();
                        Debug.Log("Rebuilt playable graph");
                    }
                    
                    if (GUILayout.Button("Check All Bindings"))
                    {
                        CheckAllBindings();
                    }
                }
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Cleanup"))
            {
                Cleanup();
            }
        }
        
        private void CreateTestSetup()
        {
            // Create target object
            targetObject = GameObject.Find("TestTarget");
            if (targetObject == null)
            {
                targetObject = new GameObject("TestTarget");
            }
            
            // Create timeline
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "TestTimeline";
            
            // Create control track
            controlTrack = timeline.CreateTrack<ControlTrack>(null, "Test Control Track");
            var clip = controlTrack.CreateClip<ControlPlayableAsset>();
            clip.displayName = "Test Control Clip";
            clip.duration = 5.0;
            
            var controlAsset = clip.asset as ControlPlayableAsset;
            controlAsset.prefabGameObject = targetObject;
            
            // Create director
            var directorGO = GameObject.Find("TestDirector");
            if (directorGO == null)
            {
                directorGO = new GameObject("TestDirector");
            }
            director = directorGO.GetComponent<PlayableDirector>();
            if (director == null)
            {
                director = directorGO.AddComponent<PlayableDirector>();
            }
            
            director.playableAsset = timeline;
            
            Debug.Log("Test setup created");
        }
        
        private void CheckAllBindings()
        {
            Debug.Log("=== Checking All Bindings ===");
            
            foreach (var output in timeline.outputs)
            {
                var binding = director.GetGenericBinding(output.sourceObject);
                Debug.Log($"Output: {output.streamName} ({output.sourceObject?.GetType().Name}) -> Binding: {binding}");
            }
            
            // Check if timeline is saved
            string path = AssetDatabase.GetAssetPath(timeline);
            Debug.Log($"Timeline asset path: {(string.IsNullOrEmpty(path) ? "NOT SAVED" : path)}");
            
            Debug.Log("=== End Binding Check ===");
        }
        
        private void Cleanup()
        {
            if (director != null && director.gameObject != null)
            {
                DestroyImmediate(director.gameObject);
            }
            
            if (targetObject != null)
            {
                DestroyImmediate(targetObject);
            }
            
            if (timeline != null)
            {
                DestroyImmediate(timeline);
            }
            
            director = null;
            timeline = null;
            controlTrack = null;
            targetObject = null;
        }
    }
}