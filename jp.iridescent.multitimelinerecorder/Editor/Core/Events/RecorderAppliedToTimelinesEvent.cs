using System.Collections.Generic;
using UnityEngine.Playables;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Events
{
    /// <summary>
    /// Event raised when a recorder is applied to multiple timelines
    /// </summary>
    public class RecorderAppliedToTimelinesEvent : IEvent
    {
        /// <summary>
        /// The source recorder configuration that was applied
        /// </summary>
        public IRecorderConfiguration SourceRecorder { get; set; }
        
        /// <summary>
        /// The timelines the recorder was applied to
        /// </summary>
        public List<PlayableDirector> TargetTimelines { get; set; }
        
        /// <summary>
        /// Whether existing recorders were overwritten
        /// </summary>
        public bool OverwriteExisting { get; set; }
        
        /// <summary>
        /// Number of timelines the recorder was applied to
        /// </summary>
        public int AppliedCount { get; set; }
    }
}