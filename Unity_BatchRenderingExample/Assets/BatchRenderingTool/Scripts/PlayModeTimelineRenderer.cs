using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;

namespace BatchRenderingTool
{
    /// <summary>
    /// シンプルなPlayMode Timeline レンダリング
    /// ControlTrackを使用して元のTimelineを破壊せずにレンダリング
    /// </summary>
    public class PlayModeTimelineRenderer : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private bool isRendering = false;
        [SerializeField] private float progress = 0f;
        [SerializeField] private string statusMessage = "Waiting...";
        
        [Header("References")]
        [SerializeField] private PlayableDirector renderingDirector;
        [SerializeField] private PlayableDirector targetDirector;
        
        private float originalCaptureFramerate;
        private bool originalRunInBackground;
        
        void Start()
        {
            Debug.Log("[PlayModeTimelineRenderer] Started - Simple version with playOnAwake");
            InitializeSimple();
        }
        
        void InitializeSimple()
        {
            // RenderingDataを探す
            var renderingData = FindObjectOfType<RenderingData>();
            if (renderingData == null)
            {
                Debug.LogError("[PlayModeTimelineRenderer] RenderingData not found!");
                return;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Found RenderingData - Director: {renderingData.directorName}");
            Debug.Log($"[PlayModeTimelineRenderer] RenderingData.renderTimeline: {renderingData.renderTimeline?.name ?? "NULL"}");
            
            // ターゲットDirectorを名前で探す
            targetDirector = GameObject.Find(renderingData.directorName)?.GetComponent<PlayableDirector>();
            if (targetDirector == null)
            {
                Debug.LogError($"[PlayModeTimelineRenderer] Target director not found: {renderingData.directorName}");
                return;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Found target director: {targetDirector.gameObject.name}");
            
            // 設定を保存
            originalCaptureFramerate = Time.captureFramerate;
            originalRunInBackground = Application.runInBackground;
            
            // レンダリング設定
            Time.captureFramerate = renderingData.frameRate;
            Application.runInBackground = true;
            
            // レンダリング用Directorをセットアップ（シンプル版）
            SetupSimpleRenderingDirector(renderingData);
            
            // ステータス監視を開始
            StartCoroutine(MonitorRendering());
        }
        
        void SetupSimpleRenderingDirector(RenderingData data)
        {
            // レンダリング用Director作成
            var renderingGO = new GameObject("RenderingDirector");
            renderingGO.transform.SetParent(transform);
            renderingDirector = renderingGO.AddComponent<PlayableDirector>();
            
            // タイムラインを設定してplayOnAwakeをtrueにする
            renderingDirector.playableAsset = data.renderTimeline;
            renderingDirector.playOnAwake = true;  // 自動再生を有効にする
            renderingDirector.timeUpdateMode = DirectorUpdateMode.GameTime;
            
            Debug.Log($"[PlayModeTimelineRenderer] Created rendering director with timeline: {data.renderTimeline.name}");
            Debug.Log($"[PlayModeTimelineRenderer] Set playOnAwake = true for automatic playback");
            Debug.Log($"[PlayModeTimelineRenderer] Timeline duration: {data.renderTimeline.duration}");
            
            // ControlTrackのバインディング設定
            foreach (var output in data.renderTimeline.outputs)
            {
                if (output.sourceObject is ControlTrack controlTrack)
                {
                    // ControlTrackを見つけたら、ターゲットDirectorをバインド
                    renderingDirector.SetGenericBinding(controlTrack, targetDirector.gameObject);
                    Debug.Log($"[PlayModeTimelineRenderer] Bound ControlTrack to {targetDirector.gameObject.name}");
                    break;
                }
            }
            
            // データを保存
            data.renderingDirector = renderingDirector;
            data.isPlaying = true;
            
            // 初期化と再生
            renderingDirector.time = 0;
            renderingDirector.RebuildGraph(); // グラフを再構築
            
            Debug.Log("[PlayModeTimelineRenderer] Setup complete - Timeline should start playing automatically");
            Debug.Log($"[PlayModeTimelineRenderer] Initial state: {renderingDirector.state}");
        }
        
        IEnumerator MonitorRendering()
        {
            isRendering = true;
            statusMessage = "Monitoring automatic playback...";
            
            // 少し待機してから監視開始
            yield return new WaitForSeconds(0.1f);
            
            if (renderingDirector == null)
            {
                Debug.LogError("[PlayModeTimelineRenderer] RenderingDirector is null!");
                yield break;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Monitoring started - Director state: {renderingDirector.state}");
            Debug.Log($"[PlayModeTimelineRenderer] Director time: {renderingDirector.time}");
            Debug.Log($"[PlayModeTimelineRenderer] Director duration: {renderingDirector.duration}");
            
            float startTime = Time.time;
            float duration = (float)renderingDirector.duration;
            int frameCount = 0;
            bool hasStartedPlaying = false;
            
            // レンダリング監視ループ
            while (renderingDirector != null)
            {
                frameCount++;
                var currentState = renderingDirector.state;
                var currentTime = renderingDirector.time;
                
                if (currentState == PlayState.Playing)
                {
                    if (!hasStartedPlaying)
                    {
                        hasStartedPlaying = true;
                        Debug.Log("[PlayModeTimelineRenderer] Playback started!");
                    }
                    
                    progress = (float)(currentTime / duration);
                    statusMessage = "Rendering...";
                    
                    // 定期的にステータスをログ
                    if (frameCount % 30 == 0)
                    {
                        Debug.Log($"[PlayModeTimelineRenderer] Frame {frameCount}, Progress: {progress:P}, " +
                                $"Time: {currentTime:F2}/{duration:F2}, State: {currentState}");
                    }
                }
                else if (hasStartedPlaying && currentState == PlayState.Paused && currentTime >= duration - 0.1f)
                {
                    // 完了
                    Debug.Log($"[PlayModeTimelineRenderer] Rendering complete! Total frames: {frameCount}");
                    statusMessage = "Complete!";
                    progress = 1f;
                    isRendering = false;
                    break;
                }
                else if (!hasStartedPlaying && frameCount > 10)
                {
                    // 10フレーム待っても再生が始まらない場合は手動で開始
                    Debug.LogWarning("[PlayModeTimelineRenderer] Timeline not auto-playing, starting manually...");
                    renderingDirector.Play();
                }
                
                // タイムアウトチェック
                if (Time.time - startTime > duration + 30f)
                {
                    Debug.LogError("[PlayModeTimelineRenderer] Rendering timeout!");
                    break;
                }
                
                yield return null;
            }
            
            // 少し待機してからクリーンアップ
            yield return new WaitForSeconds(1f);
            Cleanup();
        }
        
        void Cleanup()
        {
            Debug.Log("[PlayModeTimelineRenderer] Cleaning up...");
            
            // 設定を戻す
            Time.captureFramerate = (int)originalCaptureFramerate;
            Application.runInBackground = originalRunInBackground;
            
            // Directorを停止
            if (renderingDirector != null)
            {
                renderingDirector.Stop();
                Destroy(renderingDirector.gameObject);
            }
            
            if (targetDirector != null)
            {
                targetDirector.Stop();
                targetDirector.time = 0;
                targetDirector.Evaluate();
            }
            
            // RenderingDataも削除
            var renderingData = FindObjectOfType<RenderingData>();
            if (renderingData != null)
            {
                Destroy(renderingData.gameObject);
            }
            
#if UNITY_EDITOR
            // EditorでPlay Modeを終了
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        void OnDestroy()
        {
            // クリーンアップ処理
        }
    }
}