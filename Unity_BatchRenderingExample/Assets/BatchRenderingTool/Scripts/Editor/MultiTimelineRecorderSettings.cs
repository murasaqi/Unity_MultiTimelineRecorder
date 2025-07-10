using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BatchRenderingTool
{
    /// <summary>
    /// MultiTimelineRecorder用の設定データを保存するScriptableObject
    /// </summary>
    public class MultiTimelineRecorderSettings : ScriptableObject
    {
        // 基本録画設定
        public int frameRate = 24;
        public int width = 1920;
        public int height = 1080;
        public string fileName = "<Scene>_<Recorder>_<Take>";
        public OutputPathSettings globalOutputPath = new OutputPathSettings();
        public int takeNumber = 1;
        public int preRollFrames = 0;
        public string cameraTag = "MainCamera";
        public OutputResolution outputResolution = OutputResolution.HD1080p;
        
        // タイムライン選択状態
        public int selectedDirectorIndex = 0;
        public List<int> selectedDirectorIndices = new List<int>();
        public int timelineMarginFrames = 30;
        
        // Multi-recorder設定
        [SerializeField]
        public MultiRecorderConfig multiRecorderConfig = new MultiRecorderConfig();
        
        // タイムライン固有のrecorder設定（Dictionaryは直接シリアライズできないため、別の形式で保存）
        [Serializable]
        public class TimelineRecorderConfigEntry
        {
            public int timelineIndex;
            public MultiRecorderConfig config;
            
            public TimelineRecorderConfigEntry(int index, MultiRecorderConfig cfg)
            {
                timelineIndex = index;
                config = cfg;
            }
        }
        public List<TimelineRecorderConfigEntry> timelineRecorderConfigEntries = new List<TimelineRecorderConfigEntry>();
        
        // タイムライン固有のTake番号管理
        [Serializable]
        public class TimelineTakeNumberEntry
        {
            public int timelineIndex;
            public int takeNumber;
            
            public TimelineTakeNumberEntry(int index, int take)
            {
                timelineIndex = index;
                takeNumber = take;
            }
        }
        public List<TimelineTakeNumberEntry> timelineTakeNumbers = new List<TimelineTakeNumberEntry>();
        
        // UIレイアウト設定
        public float leftColumnWidth = 250f;
        public float centerColumnWidth = 250f;
        
        // デバッグ設定
        public bool debugMode = false;
        public bool showStatusSection = true;
        public bool showDebugSettings = false;
        
        // 設定ファイルのパス
        private const string SETTINGS_PATH = "Assets/BatchRenderingTool/Settings/MultiTimelineRecorderSettings.asset";
        
        /// <summary>
        /// 設定をロードまたは作成
        /// </summary>
        public static MultiTimelineRecorderSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MultiTimelineRecorderSettings>(SETTINGS_PATH);
            
            if (settings == null)
            {
                // ディレクトリの作成
                string directory = System.IO.Path.GetDirectoryName(SETTINGS_PATH);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                // 設定の作成
                settings = CreateInstance<MultiTimelineRecorderSettings>();
                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
            }
            
            return settings;
        }
        
        /// <summary>
        /// 設定を保存
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Dictionaryから変換して保存
        /// </summary>
        public void SetTimelineRecorderConfigs(Dictionary<int, MultiRecorderConfig> configs)
        {
            timelineRecorderConfigEntries.Clear();
            foreach (var kvp in configs)
            {
                timelineRecorderConfigEntries.Add(new TimelineRecorderConfigEntry(kvp.Key, kvp.Value));
            }
        }
        
        /// <summary>
        /// Dictionaryに変換して取得
        /// </summary>
        public Dictionary<int, MultiRecorderConfig> GetTimelineRecorderConfigs()
        {
            var dict = new Dictionary<int, MultiRecorderConfig>();
            foreach (var entry in timelineRecorderConfigEntries)
            {
                dict[entry.timelineIndex] = entry.config;
            }
            return dict;
        }
        
        /// <summary>
        /// 特定のTimelineのTake番号を取得
        /// </summary>
        public int GetTimelineTakeNumber(int timelineIndex)
        {
            var entry = timelineTakeNumbers.Find(e => e.timelineIndex == timelineIndex);
            if (entry != null)
            {
                return entry.takeNumber;
            }
            // エントリーが存在しない場合は、グローバルのtakeNumberを返す
            return takeNumber;
        }
        
        /// <summary>
        /// 特定のTimelineのTake番号を設定
        /// </summary>
        public void SetTimelineTakeNumber(int timelineIndex, int take)
        {
            var entry = timelineTakeNumbers.Find(e => e.timelineIndex == timelineIndex);
            if (entry != null)
            {
                entry.takeNumber = take;
            }
            else
            {
                timelineTakeNumbers.Add(new TimelineTakeNumberEntry(timelineIndex, take));
            }
            Save();
        }
        
        /// <summary>
        /// 特定のTimelineのTake番号をインクリメント
        /// </summary>
        public void IncrementTimelineTakeNumber(int timelineIndex)
        {
            int currentTake = GetTimelineTakeNumber(timelineIndex);
            SetTimelineTakeNumber(timelineIndex, currentTake + 1);
        }
        
        /// <summary>
        /// すべてのTimelineのTake番号をDictionaryとして取得
        /// </summary>
        public Dictionary<int, int> GetAllTimelineTakeNumbers()
        {
            var dict = new Dictionary<int, int>();
            foreach (var entry in timelineTakeNumbers)
            {
                dict[entry.timelineIndex] = entry.takeNumber;
            }
            return dict;
        }
    }
}