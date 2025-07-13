using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// PlayMode Timeline レンダリング with 進捗監視
    /// </summary>
    public class PlayModeTimelineRenderer : MonoBehaviour
    {
        private PlayableDirector director;
        private RenderingData renderingData;
        private float lastReportedProgress = -1f;
        private bool isRendering = false;
        private float renderStartTime;
        
        void Start()
        {
            Debug.Log("[PlayModeTimelineRenderer] Start - Progress monitoring version");
            
            // RenderingDataを探す
            renderingData = FindObjectOfType<RenderingData>();
            if (renderingData == null)
            {
                Debug.LogError("[PlayModeTimelineRenderer] RenderingData not found!");
                #if UNITY_EDITOR
                EditorPrefs.SetString("STR_Status", "Error: RenderingData not found");
                EditorPrefs.SetFloat("STR_Progress", 0f);
                #endif
                return;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Found RenderingData");
            Debug.Log($"[PlayModeTimelineRenderer] Timeline: {renderingData.renderTimeline?.name ?? "NULL"}");
            Debug.Log($"[PlayModeTimelineRenderer] Duration: {renderingData.renderTimeline?.duration ?? 0}");
            
            // GameObjectを作成
            var directorGO = new GameObject("RenderingDirector");
            
            // PlayableDirectorを追加
            director = directorGO.AddComponent<PlayableDirector>();
            
            // Timelineを設定
            if (renderingData.renderTimeline != null)
            {
                director.playableAsset = renderingData.renderTimeline;
            }
            else
            {
                Debug.LogError("[PlayModeTimelineRenderer] renderTimeline is null!");
                #if UNITY_EDITOR
                EditorPrefs.SetString("STR_Status", "Error: Timeline is null");
                EditorPrefs.SetFloat("STR_Progress", 0f);
                EditorPrefs.SetBool("STR_IsRenderingInProgress", false);
                #endif
                return;
            }
            
            // RenderingDataにdirectorを設定
            renderingData.renderingDirector = director;
            
            // 自動再生を無効化 (手動でPlayを呼ぶので)
            director.playOnAwake = false;
            
            Debug.Log($"[PlayModeTimelineRenderer] Created director with playOnAwake = false");
            Debug.Log($"[PlayModeTimelineRenderer] Director state: {director.state}");
            
            // レンダリング開始
            renderStartTime = Time.time;
            isRendering = true;
            
            #if UNITY_EDITOR
            // 初期ステータスを設定
            EditorPrefs.SetString("STR_Status", "Rendering started...");
            EditorPrefs.SetFloat("STR_Progress", 0f);
            EditorPrefs.SetBool("STR_IsRenderingInProgress", true);
            #endif
            
            // 手動でPlayを呼ぶ
            director.Play();
            
            Debug.Log($"[PlayModeTimelineRenderer] Called Play() - Director state: {director.state}");
        }
        
        void Update()
        {
            if (!isRendering || director == null || renderingData == null)
                return;
            
            // 進捗を計算
            double currentTime = director.time;
            double duration = renderingData.renderTimeline.duration;
            float progress = duration > 0 ? (float)(currentTime / duration) : 0f;
            progress = Mathf.Clamp01(progress);
            
            // RenderingDataを更新
            renderingData.currentTime = (float)currentTime;
            renderingData.progress = progress;
            renderingData.isPlaying = director.state == PlayState.Playing;
            
            #if UNITY_EDITOR
            // 進捗が変化した場合のみ更新（頻繁な更新を避ける）
            if (Mathf.Abs(progress - lastReportedProgress) > 0.01f || progress >= 0.99f)
            {
                lastReportedProgress = progress;
                
                // EditorPrefsで進捗を共有
                EditorPrefs.SetFloat("STR_Progress", progress);
                EditorPrefs.SetFloat("STR_CurrentTime", (float)currentTime);
                EditorPrefs.SetString("STR_Status", $"Rendering... {(progress * 100f):F1}%");
                
                // デバッグ情報
                if (EditorPrefs.GetBool("STR_DebugMode", false))
                {
                    Debug.Log($"[PlayModeTimelineRenderer] Progress: {progress:F3} ({currentTime:F2}/{duration:F2}s)");
                }
            }
            #endif
            
            // レンダリング完了チェック
            if (director.state != PlayState.Playing && progress >= 0.99f)
            {
                OnRenderingComplete();
            }
            
            // タイムアウトチェック（安全対策）
            if (Time.time - renderStartTime > duration + 10f)
            {
                Debug.LogWarning("[PlayModeTimelineRenderer] Rendering timeout detected");
                OnRenderingComplete();
            }
        }
        
        private void OnRenderingComplete()
        {
            if (!isRendering)
                return;
            
            isRendering = false;
            
            Debug.Log("[PlayModeTimelineRenderer] Rendering completed");
            
            // RenderingDataを更新
            renderingData.isComplete = true;
            renderingData.progress = 1f;
            
            #if UNITY_EDITOR
            // 完了ステータスを設定
            EditorPrefs.SetFloat("STR_Progress", 1f);
            EditorPrefs.SetString("STR_Status", "Rendering completed");
            EditorPrefs.SetBool("STR_IsRenderingInProgress", false);
            EditorPrefs.SetBool("STR_IsRenderingComplete", true);
            
            // 1秒後にPlay Mode終了とTake Numberインクリメントを実行
            StartCoroutine(ExitPlayModeAfterDelay(1f));
            #endif
        }
        
        #if UNITY_EDITOR
        private IEnumerator ExitPlayModeAfterDelay(float delay)
        {
            Debug.Log($"[PlayModeTimelineRenderer] Waiting {delay} seconds before exiting Play Mode...");
            
            yield return new WaitForSeconds(delay);
            
            // Take Numberインクリメントのフラグを設定
            EditorPrefs.SetBool("STR_IncrementTakeNumber", true);
            
            // Play Mode終了を予約
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool("STR_AutoExitPlayMode", true))
                {
                    Debug.Log("[PlayModeTimelineRenderer] Exiting Play Mode...");
                    EditorApplication.isPlaying = false;
                }
            };
        }
        #endif
        
        void OnDestroy()
        {
            #if UNITY_EDITOR
            // クリーンアップ
            if (isRendering)
            {
                EditorPrefs.SetBool("STR_IsRenderingInProgress", false);
                EditorPrefs.SetString("STR_Status", "Rendering interrupted");
            }
            #endif
        }
    }
}