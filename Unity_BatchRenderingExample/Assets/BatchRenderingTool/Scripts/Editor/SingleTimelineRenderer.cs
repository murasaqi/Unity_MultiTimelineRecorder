using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using UnityEditor.Recorder.Encoder;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System.Linq;
using BatchRenderingTool.RecorderEditors;

namespace BatchRenderingTool
{
    /// <summary>
    /// Single Timeline Recorder - Records one timeline at a time with a simple UI
    /// </summary>
    public partial class SingleTimelineRenderer : EditorWindow
    {
        // Static instance tracking
        private static SingleTimelineRenderer instance;
        
        // UI Styles - Unity標準のエディタスタイルに準拠
        private static class Styles
        {
            // Colors
            public static readonly Color SelectionColor = EditorGUIUtility.isProSkin 
                ? new Color(0.22f, 0.44f, 0.69f, 0.5f)  // Pro Skin: 青色
                : new Color(0.31f, 0.57f, 0.87f, 0.5f); // Light Skin: 明るい青
                
            public static readonly Color ActiveSelectionColor = EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.55f, 0.87f, 0.6f)  // Pro Skin: 強調青
                : new Color(0.38f, 0.64f, 0.94f, 0.6f); // Light Skin: 強調青
                
            public static readonly Color HoverColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.05f)  // Pro Skin: 微かな白
                : new Color(0f, 0f, 0f, 0.05f); // Light Skin: 微かな黒
            
            // Consistent dimensions
            public const float CheckboxWidth = 20f;
            public const float IconWidth = 25f;
            public const float DeleteButtonWidth = 20f;
            public const float MinButtonWidth = 200f;
            public const float MinListItemWidth = 200f;
            public const float StandardSpacing = 5f;
            public const float SectionSpacing = 10f;
            public const int HeaderFontSize = 14;
            
            // Background colors for visual hierarchy
            public static readonly Color ColumnBackgroundColor = EditorGUIUtility.isProSkin 
                ? new Color(0.22f, 0.22f, 0.22f, 0.3f)  // Pro Skin: 薄い暗色
                : new Color(0.9f, 0.9f, 0.9f, 0.3f);     // Light Skin: 薄い明色
                
            public static readonly Color ListBackgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.25f, 0.25f, 0.5f)  // Pro Skin: やや暗い
                : new Color(0.95f, 0.95f, 0.95f, 0.5f); // Light Skin: やや明るい
                
            public static readonly Color AlternateRowColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.02f)          // Pro Skin: 微かに明るい
                : new Color(0f, 0f, 0f, 0.02f);         // Light Skin: 微かに暗い
            
            // Styles
            public static GUIStyle HeaderLabel => new GUIStyle(EditorStyles.boldLabel) 
            { 
                fontSize = HeaderFontSize,
                alignment = TextAnchor.MiddleLeft
            };
            
            public static GUIStyle SelectableLabel => new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 2, 2)
            };
            
            public static GUIStyle SelectedLabel => new GUIStyle(SelectableLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            public static GUIStyle ListItemBackground => new GUIStyle("CN Box")
            {
                padding = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(0, 0, 2, 2)
            };
            
            // Unity標準のリストアイテムスタイル
            public static GUIStyle StandardListItem => new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };
            
            public static GUIStyle ListBackground => new GUIStyle("RL Background")
            {
                padding = new RectOffset(1, 1, 3, 3)
            };
            
            public static GUIStyle ListElementBackground => new GUIStyle("RL Element")
            {
                padding = new RectOffset(2, 2, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            
            // ヘッダースタイル
            public static GUIStyle ColumnHeader
            {
                get
                {
                    var style = new GUIStyle()
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(10, 10, 0, 0),
                        fixedHeight = 26
                    };
                    // Pro/Light Skinに応じた調整
                    if (EditorGUIUtility.isProSkin)
                    {
                        style.normal.textColor = new Color(0.95f, 0.95f, 0.95f);
                    }
                    else
                    {
                        style.normal.textColor = new Color(0.05f, 0.05f, 0.05f);
                    }
                    return style;
                }
            }
            
            // カラムヘッダーの背景色
            public static readonly Color ColumnHeaderBackgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.15f, 0.15f, 1f)  // Pro Skin: 濃い暗色
                : new Color(0.7f, 0.7f, 0.7f, 1f);     // Light Skin: 濃いグレー
        }
        
        public enum RecordState
        {
            Idle,
            Preparing,
            PreparingAssets,      // アセット準備中
            SavingAssets,         // アセット保存中  
            WaitingForPlayMode,
            InitializingInPlayMode, // Play Mode内での初期化中
            Recording,
            Complete,
            Error
        }
        
        // UI State
        private RecordState currentState = RecordState.Idle;
        private string statusMessage = "Ready to record";
        private float renderProgress = 0f;
        
        // Timeline selection
        private List<PlayableDirector> availableDirectors = new List<PlayableDirector>();
        private int selectedDirectorIndex = 0;
        
        // Multiple timeline selection support
        private List<int> selectedDirectorIndices = new List<int>();
        private int timelineMarginFrames = 30; // Margin frames between timelines for safety
        
        // Multi-recorder configuration
        private MultiRecorderConfig multiRecorderConfig = new MultiRecorderConfig();
        private Vector2 multiRecorderScrollPos;
        private int selectedRecorderIndex = -1; // Currently selected recorder for detail editing
        private Vector2 detailPanelScrollPos;
        
        // Timeline-specific recorder configurations
        private Dictionary<int, MultiRecorderConfig> timelineRecorderConfigs = new Dictionary<int, MultiRecorderConfig>();
        private int currentTimelineIndexForRecorder = -1; // Currently selected timeline for recorder configuration
        
        // Recording progress tracking
        private int currentRecordingTimelineIndex = 0;
        private int totalTimelinesToRecord = 0;
        
        // Column width settings for multi-recorder mode
        private float leftColumnWidth = 250f;
        private float centerColumnWidth = 250f;
        private bool isDraggingLeftSplitter = false;
        private bool isDraggingCenterSplitter = false;
        private const float minColumnWidth = 50f;  // Much smaller minimum width
        private const float maxColumnWidth = 600f;  // Larger maximum width
        private const float splitterWidth = 2f;  // Thinner splitter
        
        // Scroll positions for each column
        private Vector2 leftColumnScrollPos;
        private Vector2 centerColumnScrollPos;
        public int frameRate = 24;
        public int width = 1920;
        public int height = 1080;
        public string fileName = "<Scene>_<Recorder>_<Take>"; // File name with wildcard support
        public OutputPathSettings globalOutputPath = new OutputPathSettings(); // Global output path settings
        public int takeNumber = 1;
        public int preRollFrames = 0; // Pre-roll frames for simulation warm-up
        public string cameraTag = "MainCamera";
        public OutputResolution outputResolution = OutputResolution.HD1080p;
        
        // Debug settings
        public bool debugMode = false; // Keep generated assets for debugging
        private string lastGeneratedAssetPath = null; // Track the last generated asset
        
        
        // Recording objects
        private TimelineAsset renderTimeline;
        private GameObject recordingGameObject;
        private PlayableDirector recordingDirector;
        private EditorCoroutine renderCoroutine;
        private string tempAssetPath;
        
        // Progress tracking
        private float renderStartTime;
        private float lastReportedProgress = -1f;
        
        
        // Scroll position for the UI
        private Vector2 scrollPosition;
        
        // Properties for easy access
        public PlayableDirector selectedDirector => 
            availableDirectors != null && selectedDirectorIndex >= 0 && selectedDirectorIndex < availableDirectors.Count 
            ? availableDirectors[selectedDirectorIndex] 
            : null;
        
        [MenuItem("Window/Batch Recording Tool/Timeline Recorder")]
        public static SingleTimelineRenderer ShowWindow()
        {
            var window = GetWindow<SingleTimelineRenderer>();
            window.titleContent = new GUIContent("Timeline Recorder");
            window.minSize = new Vector2(800, 600);  // Larger minimum size for 3-column layout
            instance = window;
            return window;
        }
        
        private void OnEnable()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] OnEnable called");
            instance = this;
            
            // Reset state if not in Play Mode
            if (!EditorApplication.isPlaying)
            {
                currentState = RecordState.Idle;
                renderCoroutine = null;
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Reset to Idle state");
            }
            
            ScanTimelines();
            
            // Initialize selection if empty
            if (selectedDirectorIndices.Count == 0 && availableDirectors.Count > 0)
            {
                // Initialize from selectedDirectorIndex if valid
                if (selectedDirectorIndex >= 0 && selectedDirectorIndex < availableDirectors.Count)
                {
                    selectedDirectorIndices.Add(selectedDirectorIndex);
                }
                else
                {
                    selectedDirectorIndices.Add(0);
                    selectedDirectorIndex = 0;
                }
            }
            
            // Initialize current timeline for recorder if needed
            if (selectedDirectorIndices.Count > 0 && currentTimelineIndexForRecorder < 0)
            {
                currentTimelineIndexForRecorder = selectedDirectorIndices[0];
            }
            
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Initialize file name with default template if empty
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "<Scene>_<Recorder>_<Take>";
            }
            
            // Ensure <Frame> wildcard is present for image sequence types
            if (multiRecorderConfig.RecorderItems.Count > 0)
            {
                var firstRecorderType = multiRecorderConfig.RecorderItems[0].recorderType;
                if ((firstRecorderType == RecorderSettingsType.Image || firstRecorderType == RecorderSettingsType.AOV) 
                    && !fileName.Contains("<Frame>"))
                {
                    // Add <Frame> before extension if present, otherwise at the end
                    if (fileName.Contains("."))
                    {
                        int lastDotIndex = fileName.LastIndexOf('.');
                        fileName = fileName.Substring(0, lastDotIndex) + "_<Frame>" + fileName.Substring(lastDotIndex);
                    }
                    else
                    {
                        fileName += "_<Frame>";
                    }
                }
            }
            
            // Play Mode内でレンダリング中かチェック
            if (EditorApplication.isPlaying && EditorPrefs.GetBool("STR_IsRendering", false))
            {
                // Play Mode内でPlayModeTimelineRendererが処理中
                currentState = RecordState.Recording;
                statusMessage = "Recording in Play Mode...";
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Detected rendering in progress in Play Mode");
                
                // 進捗監視を開始
                MonitorRenderingProgress();
            }
            
            // Restore debug mode setting
            debugMode = EditorPrefs.GetBool("STR_DebugMode", false);
            
            // Restore column widths for multi-recorder mode
            leftColumnWidth = EditorPrefs.GetFloat("STR_LeftColumnWidth", 250f);
            centerColumnWidth = EditorPrefs.GetFloat("STR_CenterColumnWidth", 250f);
            // Validate column widths
            leftColumnWidth = Mathf.Clamp(leftColumnWidth, minColumnWidth, maxColumnWidth);
            centerColumnWidth = Mathf.Clamp(centerColumnWidth, minColumnWidth, maxColumnWidth);
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] OnEnable completed - Directors: {availableDirectors.Count}, State: {currentState}, DebugMode: {debugMode}");
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            // Save column widths for multi-recorder mode
            EditorPrefs.SetFloat("STR_LeftColumnWidth", leftColumnWidth);
            EditorPrefs.SetFloat("STR_CenterColumnWidth", centerColumnWidth);
            
            if (instance == this)
            {
                instance = null;
            }
        }
        
        private void OnGUI()
        {
            // Debug info at the top
            if (Event.current.type == EventType.Layout)
            {
                if (availableDirectors == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] availableDirectors is null!");
                    availableDirectors = new List<PlayableDirector>();
                    ScanTimelines();
                }
            }
            
            // タイトルは既にウィンドウタブに表示されているので削除
            
            // Global settings at the top
            DrawGlobalSettings();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // 3-column layout
            DrawMultiRecorderLayout();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Render controls
            DrawRecordControls();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Status section
            DrawStatusSection();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Debug settings
            DrawDebugSettings();
            
            // Force repaint if GUI changed
            if (GUI.changed)
            {
                Repaint();
            }
        }
        
        private void DrawGlobalSettings()
        {
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
            Rect settingsRect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(settingsRect, Styles.ListBackgroundColor);
            }
            
            // Resolution and Frame Rate on same line
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Resolution:", GUILayout.Width(70));
            width = EditorGUILayout.IntField(width, GUILayout.Width(60));
            EditorGUILayout.LabelField("x", GUILayout.Width(15));
            height = EditorGUILayout.IntField(height, GUILayout.Width(60));
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.LabelField("Frame Rate:", GUILayout.Width(80));
            frameRate = EditorGUILayout.IntField(frameRate, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            
            // Output path using OutputPathSettingsUI
            OutputPathSettingsUI.DrawOutputPathUI(globalOutputPath);
            
            // Timeline settings on same line
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Timeline Margin:", GUILayout.Width(100));
            timelineMarginFrames = EditorGUILayout.IntField(timelineMarginFrames, GUILayout.Width(60));
            EditorGUILayout.LabelField("frames", GUILayout.Width(50));
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.LabelField("Pre-roll:", GUILayout.Width(60));
            preRollFrames = EditorGUILayout.IntField(preRollFrames, GUILayout.Width(60));
            EditorGUILayout.LabelField("frames", GUILayout.Width(50));
            
            EditorGUILayout.EndHorizontal();
            
            // Help info for timeline margin
            if (timelineMarginFrames > 0)
            {
                float marginSeconds = timelineMarginFrames / (float)frameRate;
                EditorGUILayout.LabelField($"Will add {marginSeconds:F2} seconds between each timeline for safe rendering.", EditorStyles.miniLabel);
            }
            
            // Help info for pre-roll
            if (preRollFrames > 0)
            {
                float preRollSeconds = preRollFrames / (float)frameRate;
                EditorGUILayout.LabelField($"Timeline will run at frame 0 for {preRollSeconds:F2} seconds before recording starts.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawMultiRecorderLayout()
        {
            // Three-column layout with draggable splitters
            EditorGUILayout.BeginHorizontal();
            
            // Left column - Timeline selection (①Timeline の追加)
            EditorGUILayout.BeginVertical(GUILayout.Width(leftColumnWidth));
            DrawTimelineSelectionColumn();
            EditorGUILayout.EndVertical();
            
            // Left Splitter
            DrawVerticalSplitter(ref leftColumnWidth, ref isDraggingLeftSplitter);
            
            // Center column - Recorder list (②Recorder の追加)
            EditorGUILayout.BeginVertical(GUILayout.Width(centerColumnWidth));
            DrawRecorderListColumn();
            EditorGUILayout.EndVertical();
            
            // Center Splitter
            DrawVerticalSplitter(ref centerColumnWidth, ref isDraggingCenterSplitter);
            
            // Right column - Recorder details (③各Recorder の設定)
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawRecorderDetailColumn();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Summary section at bottom
            EditorGUILayout.Space(Styles.StandardSpacing);
            EditorGUILayout.LabelField("Timeline Recorder Summary:", EditorStyles.miniBoldLabel);
            int totalRecorders = 0;
            foreach (int idx in selectedDirectorIndices)
            {
                var config = GetTimelineRecorderConfig(idx);
                totalRecorders += config.GetEnabledRecorders().Count;
            }
            EditorGUILayout.LabelField($"Total Active Recorders: {totalRecorders} across {selectedDirectorIndices.Count} timelines", EditorStyles.miniLabel);
        }
        
        private void DrawTimelineSelectionColumn()
        {
            // Column container with background
            Rect columnRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(columnRect, Styles.ColumnBackgroundColor);
            }
            
            // Column header with background
            Rect headerRect = EditorGUILayout.GetControlRect(false, 26);
            if (Event.current.type == EventType.Repaint)
            {
                // カスタム背景を描画
                EditorGUI.DrawRect(headerRect, Styles.ColumnHeaderBackgroundColor);
                
                // 下部に薄い境界線
                Rect bottomBorder = new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1);
                EditorGUI.DrawRect(bottomBorder, new Color(0, 0, 0, 0.3f));
            }
            GUI.Label(headerRect, "Timelines", Styles.ColumnHeader);
            
            // Begin horizontal scroll view for the entire column content
            leftColumnScrollPos = EditorGUILayout.BeginScrollView(leftColumnScrollPos, 
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // Refresh button
            EditorGUILayout.BeginHorizontal();
            // Refresh button with icon
            GUIContent refreshContent = new GUIContent(" Refresh", EditorGUIUtility.IconContent("d_Refresh").image);
            if (GUILayout.Button(refreshContent, GUILayout.Height(20), GUILayout.Width(100)))
            {
                ScanTimelines();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // Timeline list with background
            Rect listBackgroundRect = EditorGUILayout.BeginVertical(GUILayout.MinWidth(Styles.MinListItemWidth));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(listBackgroundRect, Styles.ListBackgroundColor);
            }
            
            if (availableDirectors.Count > 0)
            {
                // Ensure GUI is enabled for timeline selection
                bool previousGUIState = GUI.enabled;
                GUI.enabled = true;
                
                for (int i = 0; i < availableDirectors.Count; i++)
                {
                    bool isSelected = selectedDirectorIndices.Contains(i);
                    bool isCurrentForRecorder = (i == currentTimelineIndexForRecorder);
                    
                    // リストアイテムの背景を描画
                    Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
                    
                    // 交互の行色
                    if (Event.current.type == EventType.Repaint && i % 2 == 1)
                    {
                        EditorGUI.DrawRect(itemRect, Styles.AlternateRowColor);
                    }
                    
                    // マウスホバーとクリックの処理
                    bool isHover = itemRect.Contains(Event.current.mousePosition);
                    // チェックボックスの領域を定義
                    Rect checkboxRect = new Rect(itemRect.x + 4, itemRect.y + 2, 16, 16);
                    
                    // チェックボックス以外の領域でクリックされた場合のみ、タイムラインを選択
                    if (Event.current.type == EventType.MouseDown && isHover && !checkboxRect.Contains(Event.current.mousePosition))
                    {
                        currentTimelineIndexForRecorder = i;
                        selectedRecorderIndex = -1;
                        Event.current.Use();
                    }
                    
                    // 選択状態の背景色
                    if (Event.current.type == EventType.Repaint)
                    {
                        if (isCurrentForRecorder)
                        {
                            // アクティブな選択（レコーダー設定用）- 青色のハイライト
                            var selectionRect = new Rect(itemRect.x + 1, itemRect.y, itemRect.width - 2, itemRect.height);
                            EditorGUI.DrawRect(selectionRect, Styles.SelectionColor);
                        }
                        else if (isHover)
                        {
                            // ホバー
                            EditorGUI.DrawRect(itemRect, Styles.HoverColor);
                        }
                        // チェックボックスの選択状態は背景色を変えない（一般的なUI）
                    }
                    
                    // Checkbox for selection
                    EditorGUI.BeginChangeCheck();
                    bool nowSelected = EditorGUI.Toggle(new Rect(itemRect.x + 4, itemRect.y + 2, 16, 16), isSelected);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (nowSelected)
                        {
                            selectedDirectorIndices.Add(i);
                            selectedDirectorIndices.Sort();
                            // Update current timeline for recorder
                            if (currentTimelineIndexForRecorder < 0 || !selectedDirectorIndices.Contains(currentTimelineIndexForRecorder))
                            {
                                currentTimelineIndexForRecorder = i;
                                selectedRecorderIndex = -1; // Reset recorder selection
                            }
                        }
                        else
                        {
                            selectedDirectorIndices.Remove(i);
                            // If we removed the current timeline, select another one
                            if (currentTimelineIndexForRecorder == i && selectedDirectorIndices.Count > 0)
                            {
                                currentTimelineIndexForRecorder = selectedDirectorIndices[0];
                                selectedRecorderIndex = -1; // Reset recorder selection
                            }
                        }
                    }
                    
                    GUILayout.Space(24); // Checkboxのスペース
                    
                    // Timeline name
                    string timelineName = availableDirectors[i] != null ? availableDirectors[i].gameObject.name : "<Missing>";
                    GUIStyle nameStyle = isCurrentForRecorder ? EditorStyles.boldLabel : Styles.StandardListItem;
                    EditorGUILayout.LabelField(timelineName, nameStyle, GUILayout.ExpandWidth(true));
                    
                    // Show duration
                    var director = availableDirectors[i];
                    if (director != null)
                    {
                        var timeline = director.playableAsset as TimelineAsset;
                        if (timeline != null)
                        {
                            EditorGUILayout.LabelField($"{timeline.duration:F2}s", GUILayout.Width(50));
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                
                // Restore GUI state
                GUI.enabled = previousGUIState;
            }
            else
            {
                EditorGUILayout.LabelField("No timelines found in the scene.", EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRecorderListColumn()
        {
            // Column container with background
            Rect columnRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(columnRect, Styles.ColumnBackgroundColor);
            }
            
            // Column header with background
            Rect headerRect = EditorGUILayout.GetControlRect(false, 26);
            if (Event.current.type == EventType.Repaint)
            {
                // カスタム背景を描画
                EditorGUI.DrawRect(headerRect, Styles.ColumnHeaderBackgroundColor);
                
                // 下部に薄い境界線
                Rect bottomBorder = new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1);
                EditorGUI.DrawRect(bottomBorder, new Color(0, 0, 0, 0.3f));
            }
            GUI.Label(headerRect, "Recorders", Styles.ColumnHeader);
            
            // Begin horizontal scroll view for the entire column content
            centerColumnScrollPos = EditorGUILayout.BeginScrollView(centerColumnScrollPos,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            // Show current timeline name
            if (currentTimelineIndexForRecorder >= 0 && currentTimelineIndexForRecorder < availableDirectors.Count)
            {
                var currentDirector = availableDirectors[currentTimelineIndexForRecorder];
                if (currentDirector != null)
                {
                    EditorGUILayout.Space(Styles.StandardSpacing);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Timeline:", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField(currentDirector.gameObject.name, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.Space(Styles.StandardSpacing);
                EditorGUILayout.LabelField("Select a timeline from the left column to configure its recorders.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            // Get the recorder config for the current timeline
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // Add Recorder button and Copy to All button
            EditorGUILayout.BeginHorizontal();
            
            // Add Recorder button with icon
            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.2f); // Green for add
            GUIContent addRecorderContent = new GUIContent(" Add Recorder", EditorGUIUtility.IconContent("d_Toolbar Plus").image);
            if (GUILayout.Button(addRecorderContent, GUILayout.Height(22), GUILayout.Width(120)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("🎬 Movie"), false, () => AddRecorder(RecorderSettingsType.Movie));
                menu.AddItem(new GUIContent("🖼️ Image Sequence"), false, () => AddRecorder(RecorderSettingsType.Image));
                menu.AddItem(new GUIContent("🌈 AOV Image Sequence"), false, () => AddRecorder(RecorderSettingsType.AOV));
                menu.AddItem(new GUIContent("🎭 Animation Clip"), false, () => AddRecorder(RecorderSettingsType.Animation));
                menu.AddItem(new GUIContent("🗂️ FBX"), false, () => AddRecorder(RecorderSettingsType.FBX));
                menu.AddItem(new GUIContent("📦 Alembic"), false, () => AddRecorder(RecorderSettingsType.Alembic));
                menu.ShowAsContext();
            }
            
            // Copy settings button - only show if there are multiple selected timelines
            if (selectedDirectorIndices.Count > 1)
            {
                GUILayout.Space(5);
                // Copy to All button with icon
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f); // Blue for copy
                GUIContent copyContent = new GUIContent(" Copy to All", EditorGUIUtility.IconContent("d_TreeEditor.Duplicate").image, "Copy these recorder settings to all selected timelines");
                if (GUILayout.Button(copyContent, GUILayout.Height(20), GUILayout.Width(100)))
                {
                    ApplyRecorderSettingsToAllTimelines();
                }
            }
            
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = originalBgColor; // Restore original background color
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // Recorder list with background
            Rect listBackgroundRect = EditorGUILayout.BeginVertical(GUILayout.MinWidth(Styles.MinListItemWidth));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(listBackgroundRect, Styles.ListBackgroundColor);
            }
            
            for (int i = 0; i < currentConfig.RecorderItems.Count; i++)
            {
                var item = currentConfig.RecorderItems[i];
                
                bool isSelected = (i == selectedRecorderIndex);
                
                // リストアイテムの背景を描画
                Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
                
                // 交互の行色
                if (Event.current.type == EventType.Repaint && i % 2 == 1)
                {
                    EditorGUI.DrawRect(itemRect, Styles.AlternateRowColor);
                }
                
                // マウスホバーとクリックの処理
                bool isHover = itemRect.Contains(Event.current.mousePosition);
                if (Event.current.type == EventType.MouseDown && isHover)
                {
                    selectedRecorderIndex = i;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
                
                // 選択状態の背景色
                if (Event.current.type == EventType.Repaint)
                {
                    if (isSelected)
                    {
                        // 選択状態
                        var selectionRect = new Rect(itemRect.x + 1, itemRect.y, itemRect.width - 2, itemRect.height);
                        EditorGUI.DrawRect(selectionRect, Styles.SelectionColor);
                    }
                    else if (isHover)
                    {
                        // ホバー
                        EditorGUI.DrawRect(itemRect, Styles.HoverColor);
                    }
                }
                
                // Enable checkbox
                EditorGUI.BeginChangeCheck();
                item.enabled = EditorGUI.Toggle(new Rect(itemRect.x + 4, itemRect.y + 2, 16, 16), item.enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    // 変更を反映
                }
                
                GUILayout.Space(24); // Checkboxのスペース
                
                // Icon based on recorder type
                GUIContent iconContent = GetRecorderIconContent(item.recorderType);
                GUI.Label(new Rect(itemRect.x + 26, itemRect.y + 2, 16, 16), iconContent);
                GUILayout.Space(Styles.IconWidth);
                
                // Recorder name
                EditorGUILayout.LabelField(item.name, Styles.StandardListItem, GUILayout.ExpandWidth(true));
                
                // Delete button
                if (GUI.Button(new Rect(itemRect.x + itemRect.width - 20, itemRect.y + 2, 16, 16), "×", EditorStyles.miniButton))
                {
                    currentConfig.RecorderItems.RemoveAt(i);
                    if (selectedRecorderIndex >= i) selectedRecorderIndex--;
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRecorderDetailColumn()
        {
            // Column container with background
            Rect columnRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(columnRect, Styles.ColumnBackgroundColor);
            }
            
            // Column header with background
            Rect headerRect = EditorGUILayout.GetControlRect(false, 26);
            if (Event.current.type == EventType.Repaint)
            {
                // カスタム背景を描画
                EditorGUI.DrawRect(headerRect, Styles.ColumnHeaderBackgroundColor);
                
                // 下部に薄い境界線
                Rect bottomBorder = new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1);
                EditorGUI.DrawRect(bottomBorder, new Color(0, 0, 0, 0.3f));
            }
            GUI.Label(headerRect, "Details", Styles.ColumnHeader);
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // Get the current timeline's recorder config
            if (currentTimelineIndexForRecorder < 0)
            {
                EditorGUILayout.LabelField("Select a timeline from the left column first.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical(); // End column
                return;
            }
            
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            
            if (selectedRecorderIndex < 0 || selectedRecorderIndex >= currentConfig.RecorderItems.Count)
            {
                EditorGUILayout.LabelField("Select a recorder from the list to edit its settings.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical(); // End column
                return;
            }
            
            var item = currentConfig.RecorderItems[selectedRecorderIndex];
            
            // Create adapter for the selected recorder item
            var itemHost = new MultiRecorderConfigItemHost(item, this);
            
            // Create recorder editor for the specific type
            var editor = CreateRecorderEditor(item.recorderType, itemHost);
            
            detailPanelScrollPos = EditorGUILayout.BeginScrollView(detailPanelScrollPos,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            // Content wrapper with minimum width to ensure horizontal scrolling when needed
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));
            
            // Recorder Type header with background
            EditorGUILayout.Space(5);
            Rect typeHeaderRect = EditorGUILayout.GetControlRect(false, 22);
            if (Event.current.type == EventType.Repaint)
            {
                // 背景を描画
                Color headerBg = EditorGUIUtility.isProSkin 
                    ? new Color(0.18f, 0.18f, 0.18f, 1f)
                    : new Color(0.8f, 0.8f, 0.8f, 1f);
                EditorGUI.DrawRect(typeHeaderRect, headerBg);
                
                // 下線を描画
                Color lineColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.3f, 0.3f, 0.3f, 1f)
                    : new Color(0.6f, 0.6f, 0.6f, 1f);
                Rect lineRect = new Rect(typeHeaderRect.x, typeHeaderRect.yMax - 1, typeHeaderRect.width, 1);
                EditorGUI.DrawRect(lineRect, lineColor);
            }
            
            // ラベルを描画
            typeHeaderRect.x += 8;
            string recorderTypeName = GetRecorderTypeName(item.recorderType);
            GUI.Label(typeHeaderRect, $"Recorder Type: {recorderTypeName}", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            // Check if recorder type is supported
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(item.recorderType))
            {
                string reason = GetUnsupportedReason(item.recorderType);
                var errorStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                errorStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField(reason, errorStyle);
                EditorGUILayout.EndVertical(); // End content wrapper
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical(); // End column
                return;
            }
            
            // Use RecorderEditor to draw all settings
            if (editor != null)
            {
                editor.DrawRecorderSettings();
            }
            else
            {
                EditorGUILayout.LabelField("Recorder editor not available for this type.", EditorStyles.wordWrappedMiniLabel);
            }
            
            // Output Path settings (moved to bottom as requested)
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Output Path header with background
            Rect outputHeaderRect = EditorGUILayout.GetControlRect(false, 22);
            if (Event.current.type == EventType.Repaint)
            {
                // 背景を描画
                Color headerBg = EditorGUIUtility.isProSkin 
                    ? new Color(0.18f, 0.18f, 0.18f, 1f)
                    : new Color(0.8f, 0.8f, 0.8f, 1f);
                EditorGUI.DrawRect(outputHeaderRect, headerBg);
                
                // 下線を描画
                Color lineColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.3f, 0.3f, 0.3f, 1f)
                    : new Color(0.6f, 0.6f, 0.6f, 1f);
                Rect lineRect = new Rect(outputHeaderRect.x, outputHeaderRect.yMax - 1, outputHeaderRect.width, 1);
                EditorGUI.DrawRect(lineRect, lineColor);
            }
            
            // ラベルを描画
            outputHeaderRect.x += 8;
            GUI.Label(outputHeaderRect, "Output Path Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            // Get current timeline name for wildcard context
            string timelineName = "Timeline";
            if (currentTimelineIndexForRecorder >= 0 && currentTimelineIndexForRecorder < availableDirectors.Count)
            {
                var director = availableDirectors[currentTimelineIndexForRecorder];
                if (director != null && director.playableAsset != null)
                {
                    timelineName = director.playableAsset.name;
                }
            }
            
            OutputPathSettingsUI.DrawRecorderPathUI(globalOutputPath, item.outputPath, "Output Path", timelineName, item.name);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.EndVertical(); // End content wrapper
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical(); // End column
        }
        
        /// <summary>
        /// Draws a vertical splitter that can be dragged to resize columns
        /// </summary>
        private void DrawVerticalSplitter(ref float columnWidth, ref bool isDragging)
        {
            // Get the rect for the splitter
            Rect splitterRect = GUILayoutUtility.GetRect(splitterWidth, 1, GUILayout.ExpandHeight(true));
            
            // Determine color based on state - Unity standard colors
            Color splitterColor;
            if (isDragging)
            {
                splitterColor = Styles.ActiveSelectionColor; // Active state
            }
            else if (splitterRect.Contains(Event.current.mousePosition))
            {
                splitterColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.7f, 0.7f, 0.7f, 0.5f)  // Pro Skin hover
                    : new Color(0.4f, 0.4f, 0.4f, 0.5f); // Light Skin hover
            }
            else
            {
                splitterColor = EditorGUIUtility.isProSkin
                    ? new Color(0.15f, 0.15f, 0.15f, 1f)  // Pro Skin default
                    : new Color(0.6f, 0.6f, 0.6f, 1f);    // Light Skin default
            }
            
            // Draw the splitter as a thin line
            Rect centerLine = new Rect(splitterRect.x + splitterRect.width * 0.5f - 0.5f, splitterRect.y, 1f, splitterRect.height);
            EditorGUI.DrawRect(centerLine, splitterColor);
            
            // Change cursor when hovering over splitter
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            
            // Handle mouse events
            Event e = Event.current;
            
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(e.mousePosition))
                    {
                        isDragging = true;
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        columnWidth += e.delta.x;
                        columnWidth = Mathf.Clamp(columnWidth, minColumnWidth, maxColumnWidth);
                        Repaint();
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (isDragging)
                    {
                        isDragging = false;
                        e.Use();
                    }
                    break;
            }
        }
        
        private GUIContent GetRecorderIconContent(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Movie: 
                    return EditorGUIUtility.IconContent("Animation.Record");
                case RecorderSettingsType.Image: 
                    return EditorGUIUtility.IconContent("RawImage Icon");
                case RecorderSettingsType.AOV: 
                    return EditorGUIUtility.IconContent("Texture Icon");
                case RecorderSettingsType.Animation: 
                    return EditorGUIUtility.IconContent("AnimationClip Icon");
                case RecorderSettingsType.FBX: 
                    return EditorGUIUtility.IconContent("PrefabModel Icon");
                case RecorderSettingsType.Alembic: 
                    return EditorGUIUtility.IconContent("Mesh Icon");
                default: 
                    return EditorGUIUtility.IconContent("Camera Icon");
            }
        }
        
        private string GetRecorderTypeName(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Movie: return "Movie Recorder Settings";
                case RecorderSettingsType.Image: return "Image Sequence Recorder Settings";
                case RecorderSettingsType.AOV: return "AOV Recorder Settings";
                case RecorderSettingsType.Animation: return "Animation Recorder Settings";
                case RecorderSettingsType.FBX: return "FBX Recorder Settings";
                case RecorderSettingsType.Alembic: return "Alembic Recorder Settings";
                default: return "Recorder Settings";
            }
        }
        
        private string GetUnsupportedReason(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.AOV:
                    return "AOV Recorder requires HDRP package to be installed";
                case RecorderSettingsType.Alembic:
                    return "Alembic Recorder requires Unity Alembic package to be installed";
                case RecorderSettingsType.FBX:
                    return "FBX Recorder requires Unity FBX Exporter package to be installed";
                default:
                    return $"{type} recorder is not available";
            }
        }
        
        
        
        private void ShowWildcardsMenu(MultiRecorderConfig.RecorderConfigItem item)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("<Scene>"), false, () => item.fileName += "<Scene>");
            menu.AddItem(new GUIContent("<Recorder>"), false, () => item.fileName += "<Recorder>");
            menu.AddItem(new GUIContent("<Take>"), false, () => item.fileName += "<Take>");
            menu.AddItem(new GUIContent("<Frame>"), false, () => item.fileName += "<Frame>");
            menu.AddItem(new GUIContent("<Time>"), false, () => item.fileName += "<Time>");
            menu.AddItem(new GUIContent("<Resolution>"), false, () => item.fileName += "<Resolution>");
            
            if (item.recorderType == RecorderSettingsType.Animation || 
                item.recorderType == RecorderSettingsType.Alembic ||
                item.recorderType == RecorderSettingsType.FBX)
            {
                menu.AddItem(new GUIContent("<GameObject>"), false, () => item.fileName += "<GameObject>");
            }
            
            menu.ShowAsContext();
        }
        
        
        private void AddRecorder(RecorderSettingsType type)
        {
            // Make sure we have a timeline selected
            if (currentTimelineIndexForRecorder < 0)
            {
                BatchRenderingToolLogger.LogWarning("[SingleTimelineRenderer] No timeline selected for recorder configuration");
                return;
            }
            
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            var item = MultiRecorderConfig.CreateDefaultRecorder(type);
            
            // Apply global settings
            if (currentConfig.useGlobalResolution)
            {
                item.width = currentConfig.globalWidth;
                item.height = currentConfig.globalHeight;
            }
            item.frameRate = frameRate;
            
            currentConfig.AddRecorder(item);
            
            // Auto-select the newly added recorder
            selectedRecorderIndex = currentConfig.RecorderItems.Count - 1;
        }
        
        
        
        
        
        
        
        // ========== 欠落していたメソッドの復元 ==========
        
        private void ScanTimelines()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] ScanTimelines called");
            availableDirectors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Found {allDirectors.Length} total PlayableDirectors");
            
            foreach (var director in allDirectors)
            {
                if (director != null && director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    availableDirectors.Add(director);
                    BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Added director: {director.name}");
                }
                else if (director != null)
                {
                    BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Skipped director: {director.name} (asset: {director.playableAsset?.GetType().Name ?? "null"})");
                }
            }
            
            // Remove any null entries that might have been destroyed
            availableDirectors.RemoveAll(d => d == null || d.gameObject == null);
            
            availableDirectors.Sort((a, b) => {
                if (a == null || a.gameObject == null) return 1;
                if (b == null || b.gameObject == null) return -1;
                return a.gameObject.name.CompareTo(b.gameObject.name);
            });
            
            if (selectedDirectorIndex >= availableDirectors.Count)
            {
                selectedDirectorIndex = 0;
            }
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] ScanTimelines completed - Found {availableDirectors.Count} valid directors");
        }
        
        private void OnEditorUpdate()
        {
            // OnPlayModeStateChanged handles Play Mode transitions now
            // This method can be used for other update tasks if needed
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Play Mode state changed: {state}");
            
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Entered Play Mode");
                
                // レンダリングが進行中の場合、PlayModeTimelineRendererを作成
                bool isRendering = EditorPrefs.GetBool("STR_IsRendering", false);
                
                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] STR_IsRendering: {isRendering}");
                
                if (isRendering)
                {
                    BatchRenderingToolLogger.Log("[SingleTimelineRenderer] Creating PlayModeTimelineRenderer GameObject");
                    currentState = RecordState.Recording;
                    statusMessage = "Recording in Play Mode...";
                    
                    // レンダリングデータを準備
                    string directorName = EditorPrefs.GetString("STR_DirectorName", "");
                    string tempAssetPath = EditorPrefs.GetString("STR_TempAssetPath", "");
                    float duration = EditorPrefs.GetFloat("STR_Duration", 0f);
                    int frameRate = EditorPrefs.GetInt("STR_FrameRate", 24);
                    int preRollFrames = EditorPrefs.GetInt("STR_PreRollFrames", 0);
                    
                    // 診断情報をログ出力
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Play Mode diagnostic info:");
                    BatchRenderingToolLogger.Log($"  - DirectorName: {directorName}");
                    BatchRenderingToolLogger.Log($"  - TempAssetPath: {tempAssetPath}");
                    BatchRenderingToolLogger.Log($"  - Duration: {duration}");
                    BatchRenderingToolLogger.Log($"  - FrameRate: {frameRate}");
                    BatchRenderingToolLogger.Log($"  - PreRollFrames: {preRollFrames}");
                    
                    // Render Timelineをロード
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Attempting to load timeline from: {tempAssetPath}");
                    
                    // AssetDatabase refresh to ensure latest state
                    AssetDatabase.Refresh();
                    
                    var renderTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
                    if (renderTimeline == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to load timeline from: {tempAssetPath}");
                        
                        // Check if file exists
                        if (System.IO.File.Exists(tempAssetPath))
                        {
                            BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] File exists but couldn't load as TimelineAsset");
                        }
                        else
                        {
                            BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] File does not exist at path: {tempAssetPath}");
                        }
                        
                        currentState = RecordState.Error;
                        statusMessage = "Failed to load recording timeline";
                        
                        // Clear rendering flag
                        EditorPrefs.SetBool("STR_IsRendering", false);
                        EditorPrefs.SetBool("STR_IsRenderingInProgress", false);
                        EditorPrefs.SetString("STR_Status", "Error: Timeline load failed");
                        
                        return;
                    }
                    
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Successfully loaded timeline: {renderTimeline.name}");
                    
                    // レンダリングデータを持つGameObjectを作成
                    var dataGO = new GameObject("[RenderingData]");
                    var renderingData = dataGO.AddComponent<RenderingData>();
                    renderingData.directorName = directorName;
                    renderingData.renderTimeline = renderTimeline;
                    
                    renderingData.duration = duration;
                    renderingData.frameRate = frameRate;
                    renderingData.preRollFrames = preRollFrames;
                    renderingData.recorderType = (RecorderSettingsType)EditorPrefs.GetInt("STR_RecorderType", 0);
                    
                    // PlayModeTimelineRenderer GameObjectを作成
                    var rendererGO = new GameObject("[PlayModeTimelineRenderer]");
                    var renderer = rendererGO.AddComponent<PlayModeTimelineRenderer>();
                    
                    // 作成確認
                    if (renderer != null)
                    {
                        BatchRenderingToolLogger.Log("[SingleTimelineRenderer] PlayModeTimelineRenderer successfully created");
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create PlayModeTimelineRenderer");
                    }
                    
                    // EditorPrefsをクリア
                    EditorPrefs.SetBool("STR_IsRendering", false);
                    
                    // このEditorWindowでは進行状況の監視のみ行う
                    MonitorRenderingProgress();
                }
                else
                {
                    BatchRenderingToolLogger.LogWarning("[SingleTimelineRenderer] STR_IsRendering is false - PlayModeTimelineRenderer will not be created");
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Exiting Play Mode");
                
                // 状態はすでにOnRecordingProgressUpdateで更新されているため、
                // ここでは最終的なクリーンアップのみ行う
                
                // 監視を停止
                EditorApplication.update -= OnRecordingProgressUpdate;
                
                // クリーンアップ
                if (renderCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                    renderCoroutine = null;
                }
                
                CleanupRendering();
                
                // EditorPrefsのクリーンアップ
                EditorPrefs.SetBool("STR_IsRendering", false);
            }
        }
        
        
        /// <summary>
        /// Creates recorder editor for specific recorder type with given host
        /// </summary>
        private RecorderSettingsEditorBase CreateRecorderEditor(RecorderSettingsType type, IRecorderSettingsHost host)
        {
            return type switch
            {
                RecorderSettingsType.Image => new ImageRecorderEditor(host),
                RecorderSettingsType.Movie => new MovieRecorderEditor(host),
                RecorderSettingsType.AOV => new AOVRecorderEditor(host),
                RecorderSettingsType.Alembic => new AlembicRecorderEditor(host),
                RecorderSettingsType.Animation => new AnimationRecorderEditor(host),
                RecorderSettingsType.FBX => new FBXRecorderEditor(host),
                _ => null
            };
        }
        
        private void MonitorRenderingProgress()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Starting rendering progress monitoring");
            
            // EditorWindowではコルーチンは使用できないため、
            // EditorApplication.updateを使用して進行状況を監視
            EditorApplication.update += OnRecordingProgressUpdate;
        }
        
        private void DrawRecordControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool canRecord = currentState == RecordState.Idle && availableDirectors.Count > 0 && !EditorApplication.isPlaying;
            
            // Validate timeline selection
            canRecord = canRecord && selectedDirectorIndices.Count > 0;
            
            // Validate recorder configurations for selected timelines
            if (canRecord)
            {
                int validTimelineCount = 0;
                foreach (int idx in selectedDirectorIndices)
                {
                    var config = GetTimelineRecorderConfig(idx);
                    if (config.GetEnabledRecorders().Count > 0)
                    {
                        validTimelineCount++;
                    }
                }
                canRecord = validTimelineCount > 0;
            }
            
            // Add Reset button if stuck in WaitingForPlayMode
            if (currentState == RecordState.WaitingForPlayMode && !EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Recorder is stuck in WaitingForPlayMode state. Click Reset to fix.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Reset State", GUILayout.Height(25)))
                {
                    currentState = RecordState.Idle;
                    renderCoroutine = null;
                    statusMessage = "State reset to Idle";
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] State manually reset to Idle");
                }
            }
            
            // Record button with icon and color
            GUI.enabled = canRecord;
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red for recording
            
            GUIContent recordContent = new GUIContent(" Start Recording", EditorGUIUtility.IconContent("d_Record").image);
            if (GUILayout.Button(recordContent, GUILayout.Height(30), GUILayout.MinWidth(150)))
            {
                StartRecording();
            }
            
            // Stop button with icon
            GUI.enabled = currentState == RecordState.Recording || EditorApplication.isPlaying;
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Gray for stop
            
            GUIContent stopContent = new GUIContent(" Stop Recording", EditorGUIUtility.IconContent("d_PreMatQuad").image);
            if (GUILayout.Button(stopContent, GUILayout.Height(30), GUILayout.MinWidth(150)))
            {
                StopRecording();
            }
            
            GUI.backgroundColor = originalColor;
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();
            
            // Status message
            Color originalColor = GUI.color;
            switch (currentState)
            {
                case RecordState.Error:
                    GUI.color = Color.red;
                    break;
                case RecordState.Complete:
                    GUI.color = Color.green;
                    break;
                case RecordState.Recording:
                    GUI.color = new Color(1f, 0.3f, 0.3f); // Recording red
                    break;
            }
            
            EditorGUILayout.LabelField($"State: {currentState}");
            GUI.color = originalColor;
            
            // Show timeline progress
            if (currentState == RecordState.Recording && totalTimelinesToRecord > 0)
            {
                string timelineName = "";
                if (currentRecordingTimelineIndex < selectedDirectorIndices.Count)
                {
                    int directorIdx = selectedDirectorIndices[currentRecordingTimelineIndex];
                    if (directorIdx < availableDirectors.Count)
                    {
                        timelineName = availableDirectors[directorIdx].gameObject.name;
                    }
                }
                
                EditorGUILayout.LabelField($"Message: Processing {timelineName} ({currentRecordingTimelineIndex + 1}/{totalTimelinesToRecord})");
            }
            else if (currentState == RecordState.Complete && totalTimelinesToRecord > 0)
            {
                EditorGUILayout.LabelField($"Message: Completed all {totalTimelinesToRecord} timelines");
            }
            else
            {
                EditorGUILayout.LabelField($"Message: {statusMessage}");
            }
            
            // Progress bar with recording color
            if (currentState == RecordState.Recording || currentState == RecordState.PreparingAssets || currentState == RecordState.WaitingForPlayMode)
            {
                // Recording-themed progress bar
                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
                
                // Draw custom progress bar with recording colors
                Color barColor = currentState == RecordState.Recording 
                    ? new Color(0.8f, 0.2f, 0.2f, 0.7f) // Red for active recording
                    : new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray for preparing
                    
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * renderProgress, rect.height), barColor);
                EditorGUI.ProgressBar(rect, renderProgress, $"{(int)(renderProgress * 100)}%");
                
                // 詳細な時間情報
                if (EditorApplication.isPlaying)
                {
                    float currentTime = EditorPrefs.GetFloat("STR_CurrentTime", 0f);
                    float duration = EditorPrefs.GetFloat("STR_Duration", 0f);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Time: {currentTime:F2}s / {duration:F2}s", EditorStyles.miniBoldLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDebugSettings()
        {
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();
            
            // Debug mode toggle
            EditorGUI.BeginChangeCheck();
            debugMode = EditorGUILayout.Toggle("Keep Generated Assets", debugMode);
            if (EditorGUI.EndChangeCheck())
            {
                // Save preference
                EditorPrefs.SetBool("STR_DebugMode", debugMode);
            }
            
            if (debugMode)
            {
                EditorGUILayout.LabelField("Debug Mode Active: Generated Timeline assets and GameObjects will not be deleted after rendering.", EditorStyles.miniLabel);
                
                // Show last generated asset if available
                if (!string.IsNullOrEmpty(lastGeneratedAssetPath))
                {
                    EditorGUILayout.Space(Styles.StandardSpacing);
                    EditorGUILayout.LabelField("Last Generated Asset:", EditorStyles.miniBoldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(lastGeneratedAssetPath);
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lastGeneratedAssetPath);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                        else
                        {
                            BatchRenderingToolLogger.LogWarning($"[SingleTimelineRenderer] Could not find asset at: {lastGeneratedAssetPath}");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Clean up debug assets button
                EditorGUILayout.Space(Styles.SectionSpacing);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Clean Debug Assets", GUILayout.Height(25)))
                {
                    CleanDebugAssets();
                }
                
                if (GUILayout.Button("Open Temp Folder", GUILayout.Height(25)))
                {
                    string tempPath = Application.dataPath + "/BatchRenderingTool/Temp";
                    if (Directory.Exists(tempPath))
                    {
                        EditorUtility.RevealInFinder(tempPath);
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogWarning("[SingleTimelineRenderer] Temp folder does not exist.");
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // ========== その他の必要なメソッド ==========
        
        private void CleanDebugAssets()
        {
            if (EditorUtility.DisplayDialog("Clean Debug Assets",
                "This will delete all [DEBUG] prefixed assets in the Temp folder. Continue?",
                "Yes", "Cancel"))
            {
                string tempDir = "Assets/BatchRenderingTool/Temp";
                if (AssetDatabase.IsValidFolder(tempDir))
                {
                    var guids = AssetDatabase.FindAssets("t:Object", new[] { tempDir });
                    int deletedCount = 0;
                    
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                        
                        if (asset != null && asset.name.StartsWith("[DEBUG]"))
                        {
                            AssetDatabase.DeleteAsset(path);
                            deletedCount++;
                        }
                    }
                    
                    // Also clean up debug GameObjects in the scene
                    var debugObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                        .Where(go => go.name.StartsWith("[DEBUG]"))
                        .ToArray();
                    
                    foreach (var obj in debugObjects)
                    {
                        DestroyImmediate(obj);
                        deletedCount++;
                    }
                    
                    AssetDatabase.Refresh();
                    
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Cleaned {deletedCount} debug assets and objects.");
                    
                    // Clear the last generated asset path if it was deleted
                    if (!string.IsNullOrEmpty(lastGeneratedAssetPath) && 
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lastGeneratedAssetPath) == null)
                    {
                        lastGeneratedAssetPath = null;
                    }
                }
            }
        }
        
        private void StartRecording()
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === StartRecording called ===");
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Current state: {currentState}");
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Available directors: {availableDirectors.Count}");
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Selected index: {selectedDirectorIndex}");
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Is Playing: {EditorApplication.isPlaying}");
            
            if (renderCoroutine != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Stopping existing coroutine");
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
            }
            
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Starting new coroutine");
            renderCoroutine = EditorCoroutineUtility.StartCoroutine(RenderTimelineCoroutine(), this);
        }
        
        private void StopRecording()
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] StopRecording called");
            
            if (renderCoroutine != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Stopping render coroutine");
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                renderCoroutine = null;
            }
            
            if (recordingDirector != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Stopping rendering director");
                try
                {
                    recordingDirector.Stop();
                }
                catch (System.Exception e)
                {
                    BatchRenderingToolLogger.LogWarning($"[SingleTimelineRenderer] Error stopping director: {e.Message}");
                }
            }
            
            // Exit Play Mode if active
            if (EditorApplication.isPlaying)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Exiting play mode");
                EditorApplication.isPlaying = false;
            }
            
            // CleanupRenderingを安全に実行
            try
            {
                CleanupRendering();
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Error during cleanup: {e.Message}");
            }
            
            currentState = RecordState.Idle;
            statusMessage = "Recording stopped by user";
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] StopRecording completed");
        }
        
        private void OnRecordingProgressUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Rendering progress monitoring ended");
                EditorApplication.update -= OnRecordingProgressUpdate;
                
                // Play Mode終了時の最終状態チェック
                if (EditorPrefs.GetBool("STR_IsRenderingComplete", false))
                {
                    currentState = RecordState.Complete;
                    statusMessage = "Recording complete!";
                    renderProgress = 1f;
                    EditorPrefs.DeleteKey("STR_IsRenderingComplete");
                }
                
                return;
            }
            
            // EditorPrefsから進捗情報を取得（PlayModeTimelineRendererから送信される）
            if (EditorPrefs.GetBool("STR_IsRenderingInProgress", false))
            {
                float progress = EditorPrefs.GetFloat("STR_Progress", 0f);
                string status = EditorPrefs.GetString("STR_Status", "Rendering...");
                float currentTime = EditorPrefs.GetFloat("STR_CurrentTime", 0f);
                
                // 進捗を更新
                renderProgress = progress;
                statusMessage = status;
                
                // デバッグ情報
                if (debugMode && Mathf.Abs(progress - lastReportedProgress) > 0.01f)
                {
                    lastReportedProgress = progress;
                    BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Progress update: {progress:F3} - {status}");
                }
                
                // UIを更新
                Repaint();
            }
            else if (EditorPrefs.GetBool("STR_IsRenderingComplete", false))
            {
                // レンダリング完了
                currentState = RecordState.Complete;
                statusMessage = "Recording complete!";
                renderProgress = 1f;
                
                // UIを更新
                Repaint();
                
                // 監視を停止
                EditorApplication.update -= OnRecordingProgressUpdate;
            }
            
            // フォールバック: RenderingDataオブジェクトを直接監視
            var renderingDataGO = GameObject.Find("[RenderingData]");
            if (renderingDataGO != null)
            {
                var renderingData = renderingDataGO.GetComponent<RenderingData>();
                if (renderingData != null && renderingData.renderingDirector != null)
                {
                    // PlayableDirectorの状態も確認
                    var director = renderingData.renderingDirector;
                    if (director.time > 0)
                    {
                        float duration = renderingData.duration;
                        float progress = duration > 0 ? (float)(director.time / duration) : 0f;
                        renderProgress = Mathf.Clamp01(progress);
                        
                        // UIを更新
                        Repaint();
                    }
                }
            }
        }
        
        private void CleanupRendering()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] CleanupRendering started");
            
            // renderTimelineのクリーンアップ
            if (renderTimeline != null && !debugMode)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Deleting render timeline asset");
                string path = AssetDatabase.GetAssetPath(renderTimeline);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
                renderTimeline = null;
            }
            
            // recordingDirectorのクリーンアップ
            if (recordingDirector != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Destroying rendering director");
                if (recordingDirector.gameObject != null)
                {
                    DestroyImmediate(recordingDirector.gameObject);
                }
                recordingDirector = null;
            }
            
            // クリーンアップ: RenderingDataオブジェクトを削除
            var renderingDataGO = GameObject.Find("[RenderingData]");
            if (renderingDataGO != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Destroying RenderingData GameObject");
                DestroyImmediate(renderingDataGO);
            }
            
            // クリーンアップ: PlayModeTimelineRendererオブジェクトを削除
            var rendererGO = GameObject.Find("[PlayModeTimelineRenderer]");
            if (rendererGO != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Destroying PlayModeTimelineRenderer GameObject");
                DestroyImmediate(rendererGO);
            }
            
            // EditorPrefsのクリーンアップ
            EditorPrefs.DeleteKey("STR_DirectorName");
            EditorPrefs.DeleteKey("STR_TempAssetPath");
            EditorPrefs.DeleteKey("STR_Duration");
            EditorPrefs.DeleteKey("STR_IsRendering");
            EditorPrefs.DeleteKey("STR_IsRenderingInProgress");
            EditorPrefs.DeleteKey("STR_IsRenderingComplete");
            EditorPrefs.DeleteKey("STR_Progress");
            EditorPrefs.DeleteKey("STR_Status");
            EditorPrefs.DeleteKey("STR_CurrentTime");
            
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] CleanupRendering completed");
        }
        
        private IEnumerator RenderTimelineCoroutine()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] RenderTimelineCoroutine started");
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Available directors count: {availableDirectors.Count}");
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Selected index: {selectedDirectorIndex}");
            
            currentState = RecordState.Preparing;
            statusMessage = "Preparing...";
            renderProgress = 0f;
            
            // Reset progress tracking
            currentRecordingTimelineIndex = 0;
            totalTimelinesToRecord = selectedDirectorIndices.Count;
            
            // Validate selection
            if (availableDirectors.Count == 0)
            {
                currentState = RecordState.Error;
                statusMessage = "No timelines available";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] No timelines available");
                yield break;
            }
            
            List<PlayableDirector> directorsToRender = new List<PlayableDirector>();
            float totalTimelineDuration = 0f;
            
            // Collect selected directors based on selectedDirectorIndices
            if (selectedDirectorIndices.Count == 0)
            {
                currentState = RecordState.Error;
                statusMessage = "No timelines selected";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] No timelines selected");
                yield break;
            }
            
            // Collect selected directors
            foreach (int idx in selectedDirectorIndices)
            {
                if (idx >= 0 && idx < availableDirectors.Count)
                {
                    var director = availableDirectors[idx];
                    if (director != null && director.gameObject != null && director.playableAsset is TimelineAsset)
                    {
                        directorsToRender.Add(director);
                        var timeline = director.playableAsset as TimelineAsset;
                        totalTimelineDuration += (float)timeline.duration;
                    }
                }
            }
            
            if (directorsToRender.Count == 0)
            {
                currentState = RecordState.Error;
                statusMessage = "No valid timelines selected";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] No valid timelines in selection");
                yield break;
            }
            
            // Add margins to total duration
            if (directorsToRender.Count > 1)
            {
                float marginTime = (directorsToRender.Count - 1) * timelineMarginFrames / (float)frameRate;
                totalTimelineDuration += marginTime;
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Starting multi-timeline render: {directorsToRender.Count} timelines, total duration: {totalTimelineDuration:F2}s ===");
            }
            else
            {
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Starting render for: {directorsToRender[0].gameObject.name} ===");
                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Timeline duration: {totalTimelineDuration}");
            }
            
            // Store original playOnAwake values and disable them
            Dictionary<PlayableDirector, bool> originalPlayOnAwakeValues = new Dictionary<PlayableDirector, bool>();
            foreach (var director in directorsToRender)
            {
                originalPlayOnAwakeValues[director] = director.playOnAwake;
                director.playOnAwake = false;
            }
            
            // Create render timeline
            currentState = RecordState.PreparingAssets;
            statusMessage = "Creating recording timeline...";
            yield return null; // Allow UI to update
            
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating recording timeline...");
            renderTimeline = null;
            try
            {
                if (directorsToRender.Count > 1)
                {
                    renderTimeline = CreateRenderTimelineMultiple(directorsToRender);
                }
                else
                {
                    var selectedDirector = directorsToRender[0];
                    var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
                    renderTimeline = CreateRenderTimeline(selectedDirector, originalTimeline);
                }
            }
            catch (System.Exception e)
            {
                currentState = RecordState.Error;
                statusMessage = $"Failed to create timeline: {e.Message}";
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create recording timeline: {e}");
                
                // Restore original playOnAwake values
                foreach (var kvp in originalPlayOnAwakeValues)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
                yield break;
            }
            
            if (renderTimeline == null)
            {
                currentState = RecordState.Error;
                statusMessage = "Failed to create recording timeline";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] CreateRenderTimeline returned null");
                
                // Restore original playOnAwake values
                foreach (var kvp in originalPlayOnAwakeValues)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
                yield break;
            }
            
            // Save assets and verify
            currentState = RecordState.SavingAssets;
            statusMessage = "Saving timeline asset...";
            yield return null; // Allow UI to update
            
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Saving assets...");
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(tempAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            // Verify asset was saved
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Verifying saved asset...");
            var verifyAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
            if (verifyAsset == null)
            {
                currentState = RecordState.Error;
                statusMessage = "Failed to save Timeline asset";
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to verify saved asset at: {tempAssetPath}");
                
                // Restore original playOnAwake values
                foreach (var kvp in originalPlayOnAwakeValues)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
                yield break;
            }
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Asset verified successfully: {verifyAsset.name}");
            
            // Wait to ensure asset is fully saved
            yield return new WaitForSeconds(0.5f);
            
            // Enter Play Mode
            currentState = RecordState.WaitingForPlayMode;
            statusMessage = "Starting Unity Play Mode...";
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Current Play Mode state: {EditorApplication.isPlaying} ===");
            
            if (!EditorApplication.isPlaying)
            {
                BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === Entering Play Mode... ===");
                
                // アセットパスが有効か確認
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Saving tempAssetPath to EditorPrefs: {tempAssetPath}");
                if (string.IsNullOrEmpty(tempAssetPath))
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] tempAssetPath is null or empty!");
                    currentState = RecordState.Error;
                    statusMessage = "Timeline asset path is invalid";
                    
                    // Restore original playOnAwake values
                    foreach (var kvp in originalPlayOnAwakeValues)
                    {
                        kvp.Key.playOnAwake = kvp.Value;
                    }
                    yield break;
                }
                
                // Store necessary data for Play Mode
                string directorName = directorsToRender.Count > 1 ? 
                    $"MultiTimeline_{directorsToRender.Count}" : 
                    directorsToRender[0].gameObject.name;
                    
                EditorPrefs.SetString("STR_DirectorName", directorName);
                EditorPrefs.SetString("STR_TempAssetPath", tempAssetPath);
                EditorPrefs.SetFloat("STR_Duration", totalTimelineDuration);
                EditorPrefs.SetBool("STR_IsRendering", true);
                EditorPrefs.SetInt("STR_TakeNumber", takeNumber);
                EditorPrefs.SetString("STR_OutputFile", fileName);
                // Store first recorder type for single recorder mode
                var firstRecorderType = multiRecorderConfig.RecorderItems.Count > 0 ? 
                    multiRecorderConfig.RecorderItems[0].recorderType : RecorderSettingsType.Image;
                EditorPrefs.SetInt("STR_RecorderType", (int)firstRecorderType);
                EditorPrefs.SetInt("STR_FrameRate", frameRate);
                EditorPrefs.SetInt("STR_PreRollFrames", preRollFrames);
                // exposedNameも保存（CreateRenderTimelineで生成されたもの）
                if (renderTimeline != null)
                {
                    // ControlTrackのexposedNameを探す
                    foreach (var output in renderTimeline.outputs)
                    {
                        if (output.sourceObject is ControlTrack track)
                        {
                            var clips = track.GetClips();
                            foreach (var clip in clips)
                            {
                                // ExposedReferenceは使用しない
                                break;
                            }
                            break;
                        }
                    }
                }
                
                // AssetDatabaseがPlay Mode移行前に最新の状態になるようにする
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // EditorPrefsの値を再確認
                string verifyPath = EditorPrefs.GetString("STR_TempAssetPath", "");
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Verified EditorPrefs STR_TempAssetPath before Play Mode: {verifyPath}");
                
                EditorApplication.isPlaying = true;
                // Play Modeに入ると、PlayModeTimelineRendererが自動的に処理を引き継ぐ
            }
        }
        
        /// <summary>
        /// Get or create recorder config for a specific timeline
        /// </summary>
        private MultiRecorderConfig GetTimelineRecorderConfig(int timelineIndex)
        {
            if (!timelineRecorderConfigs.ContainsKey(timelineIndex))
            {
                var newConfig = new MultiRecorderConfig();
                // Note: Do NOT copy globalOutputPath to maintain independence
                // Only copy resolution and frame rate settings
                newConfig.useGlobalResolution = multiRecorderConfig.useGlobalResolution;
                newConfig.globalWidth = multiRecorderConfig.globalWidth;
                newConfig.globalHeight = multiRecorderConfig.globalHeight;
                newConfig.useGlobalFrameRate = multiRecorderConfig.useGlobalFrameRate;
                newConfig.globalFrameRate = multiRecorderConfig.globalFrameRate;
                
                timelineRecorderConfigs[timelineIndex] = newConfig;
            }
            return timelineRecorderConfigs[timelineIndex];
        }
        
        /// <summary>
        /// Apply current recorder settings to all selected timelines
        /// </summary>
        private void ApplyRecorderSettingsToAllTimelines()
        {
            if (currentTimelineIndexForRecorder < 0 || !timelineRecorderConfigs.ContainsKey(currentTimelineIndexForRecorder))
            {
                BatchRenderingToolLogger.LogWarning("[SingleTimelineRenderer] No recorder configuration to apply");
                return;
            }
            
            var sourceConfig = timelineRecorderConfigs[currentTimelineIndexForRecorder];
            
            foreach (int timelineIndex in selectedDirectorIndices)
            {
                if (timelineIndex != currentTimelineIndexForRecorder)
                {
                    // Clone the configuration
                    var targetConfig = GetTimelineRecorderConfig(timelineIndex);
                    targetConfig.RecorderItems.Clear();
                    
                    // Deep copy each recorder item
                    foreach (var sourceItem in sourceConfig.RecorderItems)
                    {
                        var clonedItem = MultiRecorderConfig.CloneRecorderItem(sourceItem);
                        targetConfig.AddRecorder(clonedItem);
                    }
                    
                    // Copy global settings
                    targetConfig.useGlobalResolution = sourceConfig.useGlobalResolution;
                    targetConfig.globalWidth = sourceConfig.globalWidth;
                    targetConfig.globalHeight = sourceConfig.globalHeight;
                    targetConfig.globalOutputPath = sourceConfig.globalOutputPath;
                }
            }
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Copied recorder settings to {selectedDirectorIndices.Count - 1} other timelines");
            EditorUtility.DisplayDialog("Copy Complete", 
                $"Recorder settings have been copied to {selectedDirectorIndices.Count - 1} other timeline{(selectedDirectorIndices.Count - 1 > 1 ? "s" : "")}.", 
                "OK");
        }
        
        /// <summary>
        /// Adapter class to bridge MultiRecorderConfig.RecorderConfigItem with IRecorderSettingsHost
        /// </summary>
        private class MultiRecorderConfigItemHost : IRecorderSettingsHost
        {
            private MultiRecorderConfig.RecorderConfigItem item;
            private SingleTimelineRenderer renderer;
            
            public MultiRecorderConfigItemHost(MultiRecorderConfig.RecorderConfigItem item, SingleTimelineRenderer renderer)
            {
                this.item = item;
                this.renderer = renderer;
            }
            
            // IRecorderSettingsHost implementation
            public PlayableDirector selectedDirector => renderer.selectedDirector;
            
            // Use global settings where applicable, item-specific otherwise
            public int frameRate { get => renderer.frameRate; set => renderer.frameRate = value; }
            public int width 
            { 
                get => item.width; 
                set => item.width = value; 
            }
            public int height 
            { 
                get => item.height; 
                set => item.height = value; 
            }
            public string fileName { get => item.fileName; set => item.fileName = value; }
            public string filePath 
            { 
                get => OutputPathManager.ResolveRecorderPath(renderer.globalOutputPath, item.outputPath); 
                set 
                { 
                    // This property should not modify global settings
                    // Instead, update the recorder's specific output path
                    if (item.outputPath.pathMode == RecorderPathMode.UseGlobal)
                    {
                        // If using global path, switch to custom mode to allow individual configuration
                        item.outputPath.pathMode = RecorderPathMode.Custom;
                    }
                    item.outputPath.path = value;
                } 
            }
            public int takeNumber { get => item.takeNumber; set => item.takeNumber = value; }
            public string cameraTag { get => renderer.cameraTag; set => renderer.cameraTag = value; }
            public OutputResolution outputResolution { get => renderer.outputResolution; set => renderer.outputResolution = value; }
            
            // Image settings
            public ImageRecorderSettings.ImageRecorderOutputFormat imageOutputFormat { get => item.imageFormat; set => item.imageFormat = value; }
            public bool imageCaptureAlpha { get => item.captureAlpha; set => item.captureAlpha = value; }
            public int jpegQuality { get => item.jpegQuality; set => item.jpegQuality = value; }
            public CompressionUtility.EXRCompressionType exrCompression { get => item.exrCompression; set => item.exrCompression = value; }
            
            // Movie settings
            public MovieRecorderSettings.VideoRecorderOutputFormat movieOutputFormat 
            { 
                get => item.movieConfig?.outputFormat ?? MovieRecorderSettings.VideoRecorderOutputFormat.MP4; 
                set 
                { 
                    if (item.movieConfig == null) item.movieConfig = new MovieRecorderSettingsConfig();
                    item.movieConfig.outputFormat = value; 
                }
            }
            public VideoBitrateMode movieQuality 
            { 
                get => item.movieConfig?.videoBitrateMode ?? VideoBitrateMode.High; 
                set 
                { 
                    if (item.movieConfig == null) item.movieConfig = new MovieRecorderSettingsConfig();
                    item.movieConfig.videoBitrateMode = value; 
                }
            }
            public bool movieCaptureAudio 
            { 
                get => item.movieConfig?.captureAudio ?? false; 
                set 
                { 
                    if (item.movieConfig == null) item.movieConfig = new MovieRecorderSettingsConfig();
                    item.movieConfig.captureAudio = value; 
                }
            }
            public bool movieCaptureAlpha 
            { 
                get => item.movieConfig?.captureAlpha ?? false; 
                set 
                { 
                    if (item.movieConfig == null) item.movieConfig = new MovieRecorderSettingsConfig();
                    item.movieConfig.captureAlpha = value; 
                }
            }
            public int movieBitrate 
            { 
                get => item.movieConfig?.customBitrate ?? 15; 
                set 
                { 
                    if (item.movieConfig == null) item.movieConfig = new MovieRecorderSettingsConfig();
                    item.movieConfig.customBitrate = value; 
                }
            }
            public AudioBitRateMode audioBitrate 
            { 
                get => item.movieConfig?.audioBitrate ?? AudioBitRateMode.High; 
                set 
                { 
                    if (item.movieConfig == null) item.movieConfig = new MovieRecorderSettingsConfig();
                    item.movieConfig.audioBitrate = value; 
                }
            }
            public MovieRecorderPreset moviePreset { get => MovieRecorderPreset.Custom; set { } }
            public bool useMoviePreset { get => false; set { } }
            
            // AOV settings
            public AOVType selectedAOVTypes 
            { 
                get => item.aovConfig?.selectedAOVs ?? AOVType.Beauty; 
                set 
                { 
                    if (item.aovConfig == null) item.aovConfig = new AOVRecorderSettingsConfig();
                    item.aovConfig.selectedAOVs = value; 
                }
            }
            public AOVOutputFormat aovOutputFormat 
            { 
                get => item.aovConfig?.outputFormat ?? AOVOutputFormat.PNG; 
                set 
                { 
                    if (item.aovConfig == null) item.aovConfig = new AOVRecorderSettingsConfig();
                    item.aovConfig.outputFormat = value; 
                }
            }
            public AOVPreset aovPreset { get => AOVPreset.Custom; set { } }
            public bool useAOVPreset { get => false; set { } }
            public bool useMultiPartEXR { get => false; set { } }
            public AOVColorSpace aovColorSpace { get => AOVColorSpace.sRGB; set { } }
            public AOVCompression aovCompression { get => AOVCompression.None; set { } }
            
            // Alembic settings
            public AlembicExportTargets alembicExportTargets 
            { 
                get => item.alembicConfig?.exportTargets ?? (AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform); 
                set 
                { 
                    if (item.alembicConfig == null) item.alembicConfig = new AlembicRecorderSettingsConfig();
                    item.alembicConfig.exportTargets = value; 
                }
            }
            public AlembicExportScope alembicExportScope 
            { 
                get => item.alembicConfig?.exportScope ?? AlembicExportScope.TargetGameObject; 
                set 
                { 
                    if (item.alembicConfig == null) item.alembicConfig = new AlembicRecorderSettingsConfig();
                    item.alembicConfig.exportScope = value; 
                }
            }
            public GameObject alembicTargetGameObject 
            { 
                get => item.alembicConfig?.targetGameObject; 
                set 
                { 
                    if (item.alembicConfig == null) item.alembicConfig = new AlembicRecorderSettingsConfig();
                    item.alembicConfig.targetGameObject = value; 
                }
            }
            public AlembicHandedness alembicHandedness 
            { 
                get => item.alembicConfig?.handedness ?? AlembicHandedness.Right; 
                set 
                { 
                    if (item.alembicConfig == null) item.alembicConfig = new AlembicRecorderSettingsConfig();
                    item.alembicConfig.handedness = value; 
                }
            }
            public float alembicWorldScale { get => 1.0f; set { } }
            public float alembicFrameRate { get => renderer.frameRate; set { } }
            public AlembicTimeSamplingType alembicTimeSamplingType { get => AlembicTimeSamplingType.Uniform; set { } }
            public bool alembicIncludeChildren { get => true; set { } }
            public bool alembicFlattenHierarchy { get => false; set { } }
            public AlembicExportPreset alembicPreset { get => AlembicExportPreset.Custom; set { } }
            public bool useAlembicPreset { get => false; set { } }
            public float alembicScaleFactor 
            { 
                get => item.alembicConfig?.scaleFactor ?? 1.0f; 
                set 
                { 
                    if (item.alembicConfig == null) item.alembicConfig = new AlembicRecorderSettingsConfig();
                    item.alembicConfig.scaleFactor = value; 
                }
            }
            
            // Animation settings
            public GameObject animationTargetGameObject 
            { 
                get => item.animationConfig?.targetGameObject; 
                set 
                { 
                    if (item.animationConfig == null) item.animationConfig = new AnimationRecorderSettingsConfig();
                    item.animationConfig.targetGameObject = value; 
                }
            }
            public AnimationRecordingScope animationRecordingScope 
            { 
                get => item.animationConfig?.recordingScope ?? AnimationRecordingScope.SingleGameObject; 
                set 
                { 
                    if (item.animationConfig == null) item.animationConfig = new AnimationRecorderSettingsConfig();
                    item.animationConfig.recordingScope = value; 
                }
            }
            public bool animationIncludeChildren { get => true; set { } }
            public bool animationClampedTangents { get => true; set { } }
            public bool animationRecordBlendShapes { get => true; set { } }
            public float animationPositionError { get => 0.5f; set { } }
            public float animationRotationError { get => 0.5f; set { } }
            public float animationScaleError { get => 0.5f; set { } }
            public AnimationExportPreset animationPreset { get => AnimationExportPreset.Custom; set { } }
            public bool useAnimationPreset { get => false; set { } }
            public AnimationRecordingProperties animationRecordingProperties 
            { 
                get => item.animationConfig?.recordingProperties ?? AnimationRecordingProperties.AllProperties; 
                set 
                { 
                    if (item.animationConfig == null) item.animationConfig = new AnimationRecorderSettingsConfig();
                    item.animationConfig.recordingProperties = value; 
                }
            }
            public AnimationInterpolationMode animationInterpolationMode 
            { 
                get => item.animationConfig?.interpolationMode ?? AnimationInterpolationMode.Linear; 
                set 
                { 
                    if (item.animationConfig == null) item.animationConfig = new AnimationRecorderSettingsConfig();
                    item.animationConfig.interpolationMode = value; 
                }
            }
            public AnimationCompressionLevel animationCompressionLevel 
            { 
                get => item.animationConfig?.compressionLevel ?? AnimationCompressionLevel.Medium; 
                set 
                { 
                    if (item.animationConfig == null) item.animationConfig = new AnimationRecorderSettingsConfig();
                    item.animationConfig.compressionLevel = value; 
                }
            }
            
            // FBX settings
            public GameObject fbxTargetGameObject 
            { 
                get => item.fbxConfig?.targetGameObject; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.targetGameObject = value; 
                }
            }
            public FBXRecordedComponent fbxRecordedComponent 
            { 
                get => item.fbxConfig?.recordedComponent ?? FBXRecordedComponent.Transform; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.recordedComponent = value; 
                }
            }
            public bool fbxRecordHierarchy 
            { 
                get => item.fbxConfig?.recordHierarchy ?? true; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.recordHierarchy = value; 
                }
            }
            public bool fbxClampedTangents 
            { 
                get => item.fbxConfig?.clampedTangents ?? true; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.clampedTangents = value; 
                }
            }
            public FBXAnimationCompressionLevel fbxAnimationCompression 
            { 
                get => item.fbxConfig?.animationCompression ?? FBXAnimationCompressionLevel.Lossy; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.animationCompression = value; 
                }
            }
            public bool fbxExportGeometry 
            { 
                get => item.fbxConfig?.exportGeometry ?? false; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.exportGeometry = value; 
                }
            }
            public Transform fbxTransferAnimationSource 
            { 
                get => item.fbxConfig?.transferAnimationSource; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.transferAnimationSource = value; 
                }
            }
            public Transform fbxTransferAnimationDest 
            { 
                get => item.fbxConfig?.transferAnimationDest; 
                set 
                { 
                    if (item.fbxConfig == null) item.fbxConfig = new FBXRecorderSettingsConfig();
                    item.fbxConfig.transferAnimationDest = value; 
                }
            }
            public FBXExportPreset fbxPreset { get => FBXExportPreset.Custom; set { } }
            public bool useFBXPreset { get => false; set { } }
        }
        
    }
}
