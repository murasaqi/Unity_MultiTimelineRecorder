using UnityEngine;
using UnityEditor;
using System.Linq;

namespace BatchRenderingTool
{
    /// <summary>
    /// バッチレンダリングのデバッグヘルパー
    /// </summary>
    public static class BatchRenderingDebugHelper
    {
        [MenuItem("Window/Batch Rendering Tool/Debug/Clear Temp Assets")]
        public static void ClearTempAssets()
        {
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (AssetDatabase.IsValidFolder(tempDir))
            {
                AssetDatabase.DeleteAsset(tempDir);
                Debug.Log("[Debug] Cleared temp assets");
            }
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Force Exit Play Mode")]
        public static void ForceExitPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[Debug] Forced exit from Play Mode");
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/List Temp Timeline Assets")]
        public static void ListTempTimelineAssets()
        {
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (AssetDatabase.IsValidFolder(tempDir))
            {
                string[] guids = AssetDatabase.FindAssets("t:TimelineAsset", new[] { tempDir });
                Debug.Log($"[Debug] Found {guids.Length} temporary timeline assets:");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var timeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Timeline.TimelineAsset>(path);
                    if (timeline != null)
                    {
                        Debug.Log($"  - {path} (Tracks: {timeline.GetOutputTracks().Count()})");
                    }
                }
            }
            else
            {
                Debug.Log("[Debug] No temp directory found");
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Check Editor Prefs")]
        public static void CheckEditorPrefs()
        {
            Debug.Log("[Debug] Checking Editor Prefs:");
            Debug.Log($"  BR_DirectorName: {EditorPrefs.GetString("BR_DirectorName", "not set")}");
            Debug.Log($"  BR_TempAssetPath: {EditorPrefs.GetString("BR_TempAssetPath", "not set")}");
            Debug.Log($"  BR_Duration: {EditorPrefs.GetFloat("BR_Duration", -1f)}");
            
            // SingleTimelineRenderer prefs
            Debug.Log($"  STR_DirectorName: {EditorPrefs.GetString("STR_DirectorName", "not set")}");
            Debug.Log($"  STR_IsRendering: {EditorPrefs.GetBool("STR_IsRendering", false)}");
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Clear Editor Prefs")]
        public static void ClearEditorPrefs()
        {
            EditorPrefs.DeleteKey("BR_DirectorName");
            EditorPrefs.DeleteKey("BR_TempAssetPath");
            EditorPrefs.DeleteKey("BR_Duration");
            EditorPrefs.DeleteKey("STR_DirectorName");
            EditorPrefs.DeleteKey("STR_IsRendering");
            Debug.Log("[Debug] Cleared all batch rendering editor prefs");
        }
    }
}