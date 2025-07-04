using UnityEngine;
using UnityEditor;

namespace BatchRenderingTool
{
    public static class SingleTimelineRendererDebugTest
    {
        [MenuItem("Window/Batch Rendering Tool/Debug/Test Single Timeline Renderer")]
        public static void TestSingleTimelineRenderer()
        {
            Debug.Log("[DEBUG TEST] Starting Single Timeline Renderer debug test...");
            
            // Check if STR_IsRendering flag is set
            bool isRenderingFlag = EditorPrefs.GetBool("STR_IsRendering", false);
            Debug.Log($"[DEBUG TEST] STR_IsRendering flag: {isRenderingFlag}");
            
            // Check other stored values
            string directorName = EditorPrefs.GetString("STR_DirectorName", "");
            string tempAssetPath = EditorPrefs.GetString("STR_TempAssetPath", "");
            
            Debug.Log($"[DEBUG TEST] STR_DirectorName: {directorName}");
            Debug.Log($"[DEBUG TEST] STR_TempAssetPath: {tempAssetPath}");
            Debug.Log($"[DEBUG TEST] Current Play Mode: {EditorApplication.isPlaying}");
            
            // Clear any stuck flags
            if (!EditorApplication.isPlaying && isRenderingFlag)
            {
                Debug.LogWarning("[DEBUG TEST] Found stuck STR_IsRendering flag while not in Play Mode. Clearing...");
                EditorPrefs.SetBool("STR_IsRendering", false);
            }
            
            // Open the Single Timeline Renderer window
            var window = SingleTimelineRenderer.ShowWindow();
            Debug.Log($"[DEBUG TEST] Single Timeline Renderer window opened: {window != null}");
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Clear Rendering Flags")]
        public static void ClearRenderingFlags()
        {
            Debug.Log("[DEBUG TEST] Clearing all rendering flags...");
            
            EditorPrefs.DeleteKey("STR_DirectorName");
            EditorPrefs.DeleteKey("STR_TempAssetPath");
            EditorPrefs.DeleteKey("STR_Duration");
            EditorPrefs.DeleteKey("STR_ExposedName");
            EditorPrefs.DeleteKey("STR_TakeNumber");
            EditorPrefs.DeleteKey("STR_OutputFile");
            EditorPrefs.DeleteKey("STR_RecorderType");
            EditorPrefs.SetBool("STR_IsRendering", false);
            
            Debug.Log("[DEBUG TEST] All rendering flags cleared.");
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Check Temp Assets")]
        public static void CheckTempAssets()
        {
            string tempDir = "Assets/BatchRenderingTool/Temp";
            Debug.Log($"[DEBUG TEST] Checking temp assets in: {tempDir}");
            
            if (AssetDatabase.IsValidFolder(tempDir))
            {
                string[] guids = AssetDatabase.FindAssets("", new[] { tempDir });
                Debug.Log($"[DEBUG TEST] Found {guids.Length} assets in temp folder:");
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    Debug.Log($"[DEBUG TEST]   - {path} (Type: {asset?.GetType().Name ?? "null"})");
                }
            }
            else
            {
                Debug.Log("[DEBUG TEST] Temp folder does not exist.");
            }
        }
    }
}