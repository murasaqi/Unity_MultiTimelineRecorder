using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BatchRenderingTool
{
    /// <summary>
    /// 最もシンプルなPlayMode Timeline レンダリング
    /// PlayableDirectorとTimelineの生成のみ
    /// </summary>
    public class PlayModeTimelineRenderer : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("[PlayModeTimelineRenderer] Start - Minimal version");
            
            // RenderingDataを探す
            var renderingData = FindObjectOfType<RenderingData>();
            if (renderingData == null)
            {
                Debug.LogError("[PlayModeTimelineRenderer] RenderingData not found!");
                return;
            }
            
            Debug.Log($"[PlayModeTimelineRenderer] Found RenderingData");
            Debug.Log($"[PlayModeTimelineRenderer] Timeline: {renderingData.renderTimeline?.name ?? "NULL"}");
            Debug.Log($"[PlayModeTimelineRenderer] Duration: {renderingData.renderTimeline?.duration ?? 0}");
            
            // GameObjectを作成
            var directorGO = new GameObject("SimpleDirector");
            
            // PlayableDirectorを追加
            var director = directorGO.AddComponent<PlayableDirector>();
            
            // Timelineを設定
            director.playableAsset = renderingData.renderTimeline;
            
            // 自動再生を有効化
            director.playOnAwake = true;
            
            Debug.Log($"[PlayModeTimelineRenderer] Created director with playOnAwake = true");
            Debug.Log($"[PlayModeTimelineRenderer] Director state: {director.state}");
            
            // 手動でPlayを呼ぶ
            director.Play();
            
            Debug.Log($"[PlayModeTimelineRenderer] Called Play() - Director state: {director.state}");
        }
    }
}