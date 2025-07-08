using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;

namespace BatchRenderingTool
{
    /// <summary>
    /// Play Mode内でTimeline レンダリングを実行するMonoBehaviour
    /// Runtime用のシンプルな実装
    /// </summary>
    public class PlayModeTimelineRenderer : MonoBehaviour
    {
        // レンダリング状態
        public enum State
        {
            Initializing,
            WaitingForData,
            Rendering,
            Complete,
            Error
        }
        
        [SerializeField] private State currentState = State.Initializing;
        [SerializeField] private string statusMessage = "Initializing...";
        [SerializeField] private float progress = 0f;
        
        // レンダリング用オブジェクト
        private PlayableDirector renderingDirector;
        private PlayableDirector targetDirector;
        
        // Play Mode設定の保存用
        private bool originalRunInBackground;
        private int originalCaptureFramerate;
        
        private void Awake()
        {
            UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Awake called");
            DontDestroyOnLoad(gameObject);
            
            // Play Mode設定を保存して変更
            originalRunInBackground = Application.runInBackground;
            originalCaptureFramerate = Time.captureFramerate;
            Application.runInBackground = true;
        }
        
        private void Start()
        {
            UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Start called");
            StartCoroutine(WaitForRenderingData());
        }
        
        private IEnumerator WaitForRenderingData()
        {
            currentState = State.WaitingForData;
            statusMessage = "Waiting for rendering data...";
            
            // SingleTimelineRendererからのデータ送信を待つ
            float timeout = 5f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                // RenderingDataがセットされているか確認
                var renderingData = GameObject.Find("[RenderingData]");
                if (renderingData != null)
                {
                    UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Found rendering data");
                    var data = renderingData.GetComponent<RenderingData>();
                    if (data != null)
                    {
                        yield return StartCoroutine(StartRendering(data));
                        yield break;
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            SetError("Timeout waiting for rendering data");
        }
        
        private IEnumerator StartRendering(RenderingData data)
        {
            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Starting rendering - Director: {data.directorName}");
            
            currentState = State.Rendering;
            statusMessage = "Rendering in progress...";
            
            // フレームレート設定
            Time.captureFramerate = data.frameRate;
            
            // ターゲットDirectorを探す
            targetDirector = GameObject.Find(data.directorName)?.GetComponent<PlayableDirector>();
            if (targetDirector == null)
            {
                SetError($"Failed to find director: {data.directorName}");
                yield break;
            }
            
            // レンダリング用DirectorをセットアップScriptableObject
            SetupRenderingDirector(data);
            
            if (renderingDirector == null || renderingDirector.playableAsset == null)
            {
                SetError("Failed to setup rendering director");
                yield break;
            }
            
            // FBX Recorderの場合の特別な処理
            if (data.recorderType == RecorderSettingsType.FBX)
            {
                UnityEngine.Debug.Log("[PlayModeTimelineRenderer] FBX Recorder detected - applying special initialization");
                
                // ターゲットDirectorの状態を詳細にログ出力
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director: {targetDirector.gameObject.name}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director playableAsset: {targetDirector.playableAsset}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director time: {targetDirector.time}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director duration: {targetDirector.duration}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director state: {targetDirector.state}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director playOnAwake: {targetDirector.playOnAwake}");
                
                // バインディング情報をログ出力
                if (targetDirector.playableAsset != null)
                {
                    foreach (var output in targetDirector.playableAsset.outputs)
                    {
                        var binding = targetDirector.GetGenericBinding(output.sourceObject);
                        UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target binding: {output.sourceObject} -> {binding}");
                    }
                }
                
                // ターゲットDirectorの初期状態を確保
                targetDirector.time = 0;
                targetDirector.Evaluate();
                UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Target Director evaluated at time 0");
                
                // 1フレーム待機してUnityの初期化を完了させる
                yield return null;
                
                // レンダリングDirectorの状態を詳細にログ出力
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Director: {renderingDirector.gameObject.name}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Director playableAsset: {renderingDirector.playableAsset}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Director time: {renderingDirector.time}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Director duration: {renderingDirector.duration}");
                UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Director state: {renderingDirector.state}");
                
                // レンダリングDirectorも初期化
                renderingDirector.time = 0;
                renderingDirector.Evaluate();
                UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Rendering Director evaluated at time 0");
                
                // もう1フレーム待機
                yield return null;
            }
            
            // レンダリング開始
            renderingDirector.time = 0;
            renderingDirector.Play();
            
            // RenderingDataにDirectorの参照を保存
            data.renderingDirector = renderingDirector;
            
            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Started rendering - Director state: {renderingDirector.state}");
            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Timeline duration: {renderingDirector.duration}");
            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Expected duration from data: {data.duration}");
            
            // レンダリング進行状況を監視 - シンプルにDirectorの状態のみを監視
            float timeout = data.duration + 10f;
            float elapsedTime = 0f;
            int frameCount = 0;
            float lastLogTime = 0f;
            
            while (renderingDirector != null && renderingDirector.state == PlayState.Playing && elapsedTime < timeout)
            {
                // 進捗をRenderingDataに保存（SingleTimelineRendererが参照できるように）
                data.progress = (float)(renderingDirector.time / renderingDirector.duration);
                data.currentTime = (float)renderingDirector.time;
                data.isPlaying = true;
                
                progress = data.progress;
                elapsedTime += Time.deltaTime;
                frameCount++;
                
                // 1秒ごとに詳細なログを出力
                if (elapsedTime - lastLogTime >= 1f)
                {
                    UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Progress Update - Frame: {frameCount}, Time: {renderingDirector.time:F3}/{renderingDirector.duration:F3}, Progress: {data.progress:P1}, State: {renderingDirector.state}");
                    
                    // FBX Recorderの場合、ターゲットDirectorの状態も確認
                    if (data.recorderType == RecorderSettingsType.FBX)
                    {
                        UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target Director - Time: {targetDirector.time:F3}, State: {targetDirector.state}");
                    }
                    
                    lastLogTime = elapsedTime;
                }
                
                yield return null;
            }
            
            // 完了
            if (elapsedTime >= timeout)
            {
                SetError($"Rendering timeout after {elapsedTime:F1} seconds");
                data.isPlaying = false;
                data.hasError = true;
                data.errorMessage = statusMessage;
            }
            else
            {
                currentState = State.Complete;
                statusMessage = "Rendering complete!";
                progress = 1f;
                data.progress = 1f;
                data.isPlaying = false;
                data.isComplete = true;
                UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Rendering completed successfully");
            }
            
            // クリーンアップ
            yield return new WaitForSeconds(1f);
            Cleanup();
        }
        
        private void SetupRenderingDirector(RenderingData data)
        {
            // レンダリング用Directorを作成
            var renderingGO = new GameObject($"{data.directorName}_RenderingDirector");
            renderingGO.transform.SetParent(transform);
            renderingDirector = renderingGO.AddComponent<PlayableDirector>();
            renderingDirector.playableAsset = data.renderTimeline;
            renderingDirector.playOnAwake = false;
            
            // ControlTrackのバインディングを設定
            if (data.renderTimeline != null)
            {
                foreach (var output in data.renderTimeline.outputs)
                {
                    if (output.sourceObject is ControlTrack track)
                    {
                        renderingDirector.SetGenericBinding(track, targetDirector.gameObject);
                        UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Bound ControlTrack to {targetDirector.gameObject.name}");
                        
                        // Log ControlTrack clip details
                        var controlClips = track.GetClips();
                        foreach (var controlClip in controlClips)
                        {
                            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] ControlTrack clip: {controlClip.displayName}, start: {controlClip.start}, duration: {controlClip.duration}");
                        }
                        
                        if (!string.IsNullOrEmpty(data.exposedName))
                        {
                            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Setting exposed reference: {data.exposedName} -> {targetDirector.gameObject.name}");
                            renderingDirector.SetReferenceValue(data.exposedName, targetDirector.gameObject);
                            
                            // Verify the reference was set
                            var resolvedRef = renderingDirector.GetReferenceValue(data.exposedName, out bool isValid);
                            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Exposed reference resolved: {resolvedRef}, isValid: {isValid}");
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("[PlayModeTimelineRenderer] No exposed name provided for ControlTrack binding!");
                        }
                    }
                }
                
                // FBX Recorderの場合、追加のバインディング設定
                if (data.recorderType == RecorderSettingsType.FBX)
                {
                    UnityEngine.Debug.Log("[PlayModeTimelineRenderer] FBX Recorder - Setting up additional bindings");
                    
                    // ターゲットDirectorのPlayOnAwakeを確実にfalseに
                    targetDirector.playOnAwake = false;
                    
                    // レンダリングTimelineの内容を詳細に確認
                    UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Rendering Timeline track count: {data.renderTimeline.GetRootTracks().Count()}");
                    foreach (var track in data.renderTimeline.GetRootTracks())
                    {
                        UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Track: {track.GetType().Name} - {track.name}");
                        
                        // PlayModeではRecorderTrackの詳細情報にアクセスできないため、基本情報のみログ出力
                        var clips = track.GetClips();
                        foreach (var clip in clips)
                        {
                            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Clip: {clip.displayName} - Start: {clip.start:F3}, Duration: {clip.duration:F3}");
                        }
                    }
                    
                    // バインディングを再確認
                    foreach (var output in targetDirector.playableAsset.outputs)
                    {
                        var binding = targetDirector.GetGenericBinding(output.sourceObject);
                        if (binding != null)
                        {
                            UnityEngine.Debug.Log($"[PlayModeTimelineRenderer] Target director binding: {output.sourceObject} -> {binding}");
                        }
                    }
                }
                
                renderingDirector.RebuildGraph();
            }
        }
        
        private void SetError(string message)
        {
            currentState = State.Error;
            statusMessage = message;
            UnityEngine.Debug.LogError($"[PlayModeTimelineRenderer] Error: {message}");
            
            StartCoroutine(ErrorCleanup());
        }
        
        private IEnumerator ErrorCleanup()
        {
            yield return new WaitForSeconds(2f);
            Cleanup();
        }
        
        private void Cleanup()
        {
            UnityEngine.Debug.Log("[PlayModeTimelineRenderer] Starting cleanup");
            
            // 設定を元に戻す
            Application.runInBackground = originalRunInBackground;
            Time.captureFramerate = originalCaptureFramerate;
            
#if UNITY_EDITOR
            // Play Modeを終了
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // ビルドではApplication.Quit()を呼ぶ
            Application.Quit();
#endif
        }
        
        private void OnDestroy()
        {
            UnityEngine.Debug.Log("[PlayModeTimelineRenderer] OnDestroy called");
            
            if (renderingDirector != null)
            {
                renderingDirector.Stop();
            }
            
            // 設定を元に戻す
            Application.runInBackground = originalRunInBackground;
            Time.captureFramerate = originalCaptureFramerate;
        }
        
        /// <summary>
        /// Get the rendering director (for monitoring progress)
        /// </summary>
        public PlayableDirector GetRenderingDirector()
        {
            return renderingDirector;
        }
    }
    
    /// <summary>
    /// レンダリングデータを保持するコンポーネント
    /// </summary>
    public class RenderingData : MonoBehaviour
    {
        // 基本設定
        public string directorName;
        public TimelineAsset renderTimeline;
        public float duration;
        public string exposedName;
        public int frameRate;
        public int preRollFrames;
        public RecorderSettingsType recorderType;
        
        // 進捗状態（SingleTimelineRendererが監視）
        public float progress = 0f;
        public float currentTime = 0f;
        public bool isPlaying = false;
        public bool isComplete = false;
        public bool hasError = false;
        public string errorMessage = "";
        
        // レンダリングDirectorの参照（SingleTimelineRendererが直接監視可能）
        public PlayableDirector renderingDirector;
    }
}