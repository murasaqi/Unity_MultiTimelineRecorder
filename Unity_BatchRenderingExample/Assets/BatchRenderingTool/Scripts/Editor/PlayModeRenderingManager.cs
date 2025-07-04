using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Collections;

namespace BatchRenderingTool
{
    /// <summary>
    /// Manages rendering process across Play Mode transitions
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeRenderingManager
    {
        private static EditorCoroutine activeCoroutine;
        
        static PlayModeRenderingManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Debug.Log($"[PlayModeRenderingManager] Play Mode state changed: {state}");
            
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Check if we have rendering data stored
                if (EditorPrefs.GetBool("STR_IsRendering", false))
                {
                    Debug.Log("[PlayModeRenderingManager] Detected rendering in progress, starting continuation process");
                    
                    // Clear the flag immediately
                    EditorPrefs.SetBool("STR_IsRendering", false);
                    
                    // Start the rendering process
                    if (activeCoroutine == null)
                    {
                        Debug.Log("[PlayModeRenderingManager] Starting coroutine...");
                        activeCoroutine = EditorCoroutineUtility.StartCoroutine(ContinueRenderingInPlayMode(), null);
                        Debug.Log($"[PlayModeRenderingManager] Coroutine started: {activeCoroutine != null}");
                    }
                    else
                    {
                        Debug.Log("[PlayModeRenderingManager] Coroutine already active");
                    }
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Clean up
                if (activeCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(activeCoroutine);
                    activeCoroutine = null;
                }
            }
        }
        
        private static IEnumerator ContinueRenderingInPlayMode()
        {
            Debug.Log("[PlayModeRenderingManager] Starting Play Mode rendering continuation");
            
            // Wait a few frames for Play Mode to fully initialize
            yield return null;
            yield return null;
            yield return null;
            
            // Find or create the SingleTimelineRenderer window
            var window = EditorWindow.GetWindow<SingleTimelineRenderer>(false, "Single Timeline Renderer", false);
            if (window == null)
            {
                Debug.LogError("[PlayModeRenderingManager] Failed to get SingleTimelineRenderer window");
                yield break;
            }
            
            Debug.Log("[PlayModeRenderingManager] Found SingleTimelineRenderer window, triggering continuation");
            
            // Use reflection to call the continuation method
            var continueMethod = typeof(SingleTimelineRenderer).GetMethod("ContinueRenderingInPlayMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (continueMethod != null)
            {
                var coroutine = continueMethod.Invoke(window, null) as IEnumerator;
                if (coroutine != null)
                {
                    Debug.Log("[PlayModeRenderingManager] Starting ContinueRenderingInPlayMode coroutine");
                    yield return EditorCoroutineUtility.StartCoroutine(coroutine, window);
                }
                else
                {
                    Debug.LogError("[PlayModeRenderingManager] ContinueRenderingInPlayMode returned null");
                }
            }
            else
            {
                Debug.LogError("[PlayModeRenderingManager] Could not find ContinueRenderingInPlayMode method");
            }
            
            activeCoroutine = null;
        }
    }
}