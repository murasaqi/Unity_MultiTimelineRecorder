using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace BatchRenderingTool
{
    /// <summary>
    /// Cut information for timeline rendering
    /// </summary>
    [Serializable]
    public class CutData
    {
        public string name = "New Cut";
        public double startTime = 0.0;
        public double endTime = 10.0;
        public bool enabled = true;
        
        public CutData()
        {
        }
        
        public CutData(string name, double start, double end)
        {
            this.name = name;
            this.startTime = start;
            this.endTime = end;
            this.enabled = true;
        }
        
        public double Duration => endTime - startTime;
        
        public bool IsValid()
        {
            return startTime >= 0 && endTime > startTime;
        }
    }
    
    /// <summary>
    /// Cut list manager for timeline rendering
    /// </summary>
    [Serializable]
    public class CutListData
    {
        [SerializeField]
        private List<CutData> cuts = new List<CutData>();
        
        public List<CutData> Cuts => cuts;
        
        public void AddCut(CutData cut)
        {
            if (cut != null && cut.IsValid())
            {
                cuts.Add(cut);
                SortCuts();
            }
        }
        
        public void AddCutAtEnd(TimelineAsset timeline)
        {
            if (timeline == null) return;
            
            double lastEndTime = 0;
            if (cuts.Count > 0)
            {
                lastEndTime = cuts[cuts.Count - 1].endTime;
            }
            
            double duration = Math.Min(10.0, timeline.duration - lastEndTime);
            if (duration <= 0) duration = 10.0;
            
            var cut = new CutData($"Cut {cuts.Count + 1}", lastEndTime, lastEndTime + duration);
            AddCut(cut);
        }
        
        public void AddCutAtCurrent(double currentTime, double defaultDuration = 10.0)
        {
            var cut = new CutData($"Cut {cuts.Count + 1}", currentTime, currentTime + defaultDuration);
            AddCut(cut);
        }
        
        public void RemoveCut(int index)
        {
            if (index >= 0 && index < cuts.Count)
            {
                cuts.RemoveAt(index);
            }
        }
        
        public void RemoveCut(CutData cut)
        {
            cuts.Remove(cut);
        }
        
        public void Clear()
        {
            cuts.Clear();
        }
        
        public void SortCuts()
        {
            cuts.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        }
        
        public double GetTotalDuration()
        {
            double total = 0;
            foreach (var cut in cuts)
            {
                if (cut.enabled)
                {
                    total += cut.Duration;
                }
            }
            return total;
        }
        
        public int GetEnabledCutCount()
        {
            int count = 0;
            foreach (var cut in cuts)
            {
                if (cut.enabled) count++;
            }
            return count;
        }
    }
}