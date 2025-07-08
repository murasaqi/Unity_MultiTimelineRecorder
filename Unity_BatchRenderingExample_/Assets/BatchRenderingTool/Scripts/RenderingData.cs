using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace BatchRenderingTool
{
    /// <summary>
    /// PlayMode内でレンダリングに必要なデータを保持するコンポーネント
    /// </summary>
    public class RenderingData : MonoBehaviour
    {
        [Header("Target")]
        public string directorName;
        public TimelineAsset renderTimeline;
        
        [Header("Settings")]
        public float duration;
        public int frameRate = 24;
        public int preRollFrames = 0;
        public RecorderSettingsType recorderType;
        
        [Header("Runtime Status")]
        public PlayableDirector renderingDirector;
        public float progress = 0f;
        public float currentTime = 0f;
        public bool isPlaying = false;
        public bool isComplete = false;
    }
}