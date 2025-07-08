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
        
        private Coroutine renderingCoroutine;
        private float originalCaptureFramerate;
        private bool originalRunInBackground;
        
        void Start()
        {
            Debug.Log("[PlayModeTimelineRenderer] Started");
            StartCoroutine(InitializeAndRender());
        }
        
        IEnumerator InitializeAndRender()
        {
            // 1フレーム待機して初期化
            yield return null;
            
            // RenderingDataを探す
            var renderingData = FindObjectOfType<RenderingData>();
            if (renderingData == null)
            {
                Debug.LogError("[PlayModeTimelineRenderer] RenderingData not found!");
                yield break;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Found RenderingData - Director: {renderingData.directorName}");
            Debug.Log($"[PlayModeTimelineRenderer] RenderingData.renderTimeline: {renderingData.renderTimeline?.name ?? "NULL"}");
            Debug.Log($"[PlayModeTimelineRenderer] RenderingData.frameRate: {renderingData.frameRate}");
            Debug.Log($"[PlayModeTimelineRenderer] RenderingData.duration: {renderingData.duration}");
            
            // ターゲットDirectorを名前で探す
            targetDirector = GameObject.Find(renderingData.directorName)?.GetComponent<PlayableDirector>();
            if (targetDirector == null)
            {
                Debug.LogError($"[PlayModeTimelineRenderer] Target director not found: {renderingData.directorName}");
                yield break;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Found target director: {targetDirector.gameObject.name}");
            
            // ターゲットDirectorを初期化
            if (targetDirector.playableAsset != null)
            {
                targetDirector.time = 0;
                targetDirector.Evaluate();
                Debug.Log($"[PlayModeTimelineRenderer] Initialized target director - time: {targetDirector.time}, asset: {targetDirector.playableAsset.name}");
            }
            
            // 設定を保存
            originalCaptureFramerate = Time.captureFramerate;
            originalRunInBackground = Application.runInBackground;
            
            // レンダリング設定
            Time.captureFramerate = renderingData.frameRate;
            Application.runInBackground = true;
            
            // レンダリング用Directorをセットアップ
            SetupRenderingDirector(renderingData);
            
            // レンダリング開始
            yield return StartCoroutine(RenderTimeline(renderingData));
            
            // クリーンアップ
            Cleanup();
        }
        
        void SetupRenderingDirector(RenderingData data)
        {
            // レンダリング用Director作成
            var renderingGO = new GameObject("RenderingDirector");
            renderingGO.transform.SetParent(transform);
            renderingDirector = renderingGO.AddComponent<PlayableDirector>();
            renderingDirector.playableAsset = data.renderTimeline;
            renderingDirector.playOnAwake = false;
            
            Debug.Log($"[PlayModeTimelineRenderer] Created rendering director with timeline: {data.renderTimeline.name}");
            Debug.Log($"[PlayModeTimelineRenderer] Timeline asset is null?: {data.renderTimeline == null}");
            Debug.Log($"[PlayModeTimelineRenderer] Timeline duration: {data.renderTimeline?.duration ?? 0}");
            Debug.Log($"[PlayModeTimelineRenderer] Timeline output count: {data.renderTimeline?.outputs.Count() ?? 0}");
            
            // Timelineの詳細をログ
            if (data.renderTimeline != null)
            {
                var tracks = data.renderTimeline.GetRootTracks();
                Debug.Log($"[PlayModeTimelineRenderer] Timeline track count: {tracks.Count()}");
                foreach (var track in tracks)
                {
                    Debug.Log($"[PlayModeTimelineRenderer] Track: {track.name} - Type: {track.GetType().Name}");
                }
            }
            
            // ControlTrackのバインディング設定
            bool foundControlTrack = false;
            foreach (var output in data.renderTimeline.outputs)
            {
                if (output.sourceObject is ControlTrack controlTrack)
                {
                    // ControlTrackを見つけたら、ターゲットDirectorをバインド
                    renderingDirector.SetGenericBinding(controlTrack, targetDirector.gameObject);
                    foundControlTrack = true;
                    Debug.Log($"[PlayModeTimelineRenderer] Bound ControlTrack to {targetDirector.gameObject.name}");
                    Debug.Log($"[PlayModeTimelineRenderer] Target Director playOnAwake: {targetDirector.playOnAwake}");
                    Debug.Log($"[PlayModeTimelineRenderer] Target Director state: {targetDirector.state}");
                    Debug.Log($"[PlayModeTimelineRenderer] Target Director time: {targetDirector.time}");
                    Debug.Log($"[PlayModeTimelineRenderer] Target Director playableAsset: {targetDirector.playableAsset?.name ?? "NULL"}");
                    
                    // クリップ情報をログ
                    var clips = controlTrack.GetClips();
                    foreach (var clip in clips)
                    {
                        Debug.Log($"[PlayModeTimelineRenderer] ControlClip: {clip.displayName}, Start: {clip.start}, Duration: {clip.duration}");
                    }
                }
            }
            
            if (!foundControlTrack)
            {
                Debug.LogWarning("[PlayModeTimelineRenderer] No ControlTrack found in timeline!");
            }
            
            // ターゲットDirectorの準備
            targetDirector.time = 0;
            targetDirector.playOnAwake = false;
            targetDirector.Stop();
            targetDirector.Evaluate();
            
            Debug.Log($"[PlayModeTimelineRenderer] Target director prepared - Timeline: {targetDirector.playableAsset?.name ?? "null"}");
        }
        
        IEnumerator RenderTimeline(RenderingData data)
        {
            isRendering = true;
            statusMessage = "Starting render...";
            
            // DirectorをEvaluate
            renderingDirector.time = 0;
            renderingDirector.Evaluate();
            
            // 初期化のために数フレーム待機
            Debug.Log("[PlayModeTimelineRenderer] Waiting for initialization...");
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                renderingDirector.Evaluate();
                Debug.Log($"[PlayModeTimelineRenderer] Init frame {i} - Director state: {renderingDirector.state}, time: {renderingDirector.time}");
            }
            
            // 再生開始
            Debug.Log("[PlayModeTimelineRenderer] Starting playback...");
            Debug.Log($"[PlayModeTimelineRenderer] Before Play - Director state: {renderingDirector.state}");
            Debug.Log($"[PlayModeTimelineRenderer] Before Play - Director time: {renderingDirector.time}");
            Debug.Log($"[PlayModeTimelineRenderer] Before Play - Timeline asset: {renderingDirector.playableAsset?.name ?? "NULL"}");
            
            renderingDirector.Play();
            
            Debug.Log($"[PlayModeTimelineRenderer] After Play - Director state: {renderingDirector.state}");
            Debug.Log($"[PlayModeTimelineRenderer] After Play - Director time: {renderingDirector.time}");
            Debug.Log($"[PlayModeTimelineRenderer] After Play - playableGraph valid: {renderingDirector.playableGraph.IsValid()}");
            
            // もう一度Evaluateを呼んでみる
            yield return null;
            renderingDirector.Evaluate();
            Debug.Log($"[PlayModeTimelineRenderer] After Evaluate - Director state: {renderingDirector.state}");
            
            // ターゲットDirectorの状態を確認
            Debug.Log($"[PlayModeTimelineRenderer] Rendering director state: {renderingDirector.state}");
            Debug.Log($"[PlayModeTimelineRenderer] Target director state: {targetDirector.state}");
            Debug.Log($"[PlayModeTimelineRenderer] Target director time: {targetDirector.time}");
            
            float startTime = Time.time;
            float duration = (float)renderingDirector.duration;
            int frameCount = 0;
            
            statusMessage = "Rendering...";
            
            // レンダリングループ
            Debug.Log($"[PlayModeTimelineRenderer] Entering render loop - Director state: {renderingDirector.state}");
            
            // Director が Playing 状態になるまで少し待つ
            int waitFrames = 0;
            while (renderingDirector != null && renderingDirector.state != PlayState.Playing && waitFrames < 10)
            {
                Debug.Log($"[PlayModeTimelineRenderer] Waiting for Playing state... Frame {waitFrames}, State: {renderingDirector.state}");
                yield return null;
                waitFrames++;
            }
            
            if (renderingDirector.state != PlayState.Playing)
            {
                Debug.LogError($"[PlayModeTimelineRenderer] Director failed to start playing! Final state: {renderingDirector.state}");
                Debug.LogError($"[PlayModeTimelineRenderer] Director time: {renderingDirector.time}");
                Debug.LogError($"[PlayModeTimelineRenderer] Director duration: {renderingDirector.duration}");
                Debug.LogError($"[PlayModeTimelineRenderer] Director playableAsset: {renderingDirector.playableAsset?.name ?? "NULL"}");
            }
            
            while (renderingDirector != null && renderingDirector.state == PlayState.Playing)
            {
                frameCount++;
                progress = (float)(renderingDirector.time / renderingDirector.duration);
                
                // 定期的にステータスをログ
                if (frameCount % 30 == 0)
                {
                    Debug.Log($"[PlayModeTimelineRenderer] Frame {frameCount}, Progress: {progress:P}, " +
                            $"Rendering time: {renderingDirector.time:F2}/{duration:F2}, " +
                            $"Target time: {targetDirector.time:F2}");
                }
                
                // タイムアウトチェック
                if (Time.time - startTime > duration + 30f)
                {
                    Debug.LogError("[PlayModeTimelineRenderer] Rendering timeout!");
                    break;
                }
                
                yield return null;
            }
            
            // 完了
            Debug.Log($"[PlayModeTimelineRenderer] Rendering complete! Total frames: {frameCount}");
            statusMessage = "Complete!";
            progress = 1f;
            isRendering = false;
            
            // 少し待機
            yield return new WaitForSeconds(1f);
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
            if (renderingCoroutine != null)
            {
                StopCoroutine(renderingCoroutine);
            }
        }
    }
}