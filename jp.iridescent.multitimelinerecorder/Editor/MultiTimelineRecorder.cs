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
using Unity.MultiTimelineRecorder.RecorderEditors;
using Unity.MultiTimelineRecorder.Utilities;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Multi Timeline Recorder - Records multiple timelines with various recorder types
    /// </summary>
    public partial class MultiTimelineRecorder : EditorWindow
    {
        // Static instance tracking
        private static MultiTimelineRecorder instance;
        
        // Settings management
        private MultiTimelineRecorderSettings settings;
        
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
                
            // Button-like header colors
            public static readonly Color HeaderButtonColor = EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.25f, 0.25f, 1f)  // Pro Skin: Dark button
                : new Color(0.85f, 0.85f, 0.85f, 1f); // Light Skin: Light button
                
            public static readonly Color HeaderButtonHoverColor = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 1f)     // Pro Skin: Lighter on hover
                : new Color(0.8f, 0.8f, 0.8f, 1f);    // Light Skin: Darker on hover
                
            public static readonly Color HeaderButtonPressedColor = EditorGUIUtility.isProSkin
                ? new Color(0.2f, 0.2f, 0.2f, 1f)     // Pro Skin: Darker when pressed
                : new Color(0.9f, 0.9f, 0.9f, 1f);    // Light Skin: Lighter when pressed
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
        private List<PlayableDirector> sceneTimelineDirectors = new List<PlayableDirector>(); // All timelines found in the scene
        private List<PlayableDirector> recordingQueueDirectors = new List<PlayableDirector>(); // Timelines added to recording queue by user
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
        private Dictionary<int, int> timelineSelectedRecorderIndices = new Dictionary<int, int>(); // Store selected recorder index per timeline
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
        
        // SignalEmitter設定 (TODO-282)
        public bool useSignalEmitterTiming = false;
        public string startTimingName = "pre";
        public string endTimingName = "post";
        public bool showTimingInFrames = false; // false=秒数表示, true=フレーム数表示
        
        // UI折りたたみ用フラグ
        private bool showStatusSection = true;
        private bool showDebugSettings = false;
        
        
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
            recordingQueueDirectors != null && selectedDirectorIndex >= 0 && selectedDirectorIndex < recordingQueueDirectors.Count 
            ? recordingQueueDirectors[selectedDirectorIndex] 
            : null;
        
        [MenuItem("Window/Multi Timeline Recorder")]
        public static MultiTimelineRecorder ShowWindow()
        {
            var window = GetWindow<MultiTimelineRecorder>();
            window.titleContent = new GUIContent("Multi Timeline Recorder");
            window.minSize = new Vector2(200, 150);  // 自由にリサイズ可能な最小サイズ
            instance = window;
            return window;
        }
        
        private void OnEnable()
        {
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] OnEnable called");
            instance = this;
            
            // Load settings
            LoadSettings();
            
            // Reset state if not in Play Mode
            if (!EditorApplication.isPlaying)
            {
                currentState = RecordState.Idle;
                renderCoroutine = null;
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Reset to Idle state");
            }
            
            // For backward compatibility, scan timelines if we have none selected
            if (recordingQueueDirectors.Count == 0)
            {
                ScanTimelines();
            }
            
            // Initialize selection if empty
            if (selectedDirectorIndices.Count == 0 && recordingQueueDirectors.Count > 0)
            {
                // Initialize from selectedDirectorIndex if valid
                if (selectedDirectorIndex >= 0 && selectedDirectorIndex < recordingQueueDirectors.Count)
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
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Detected rendering in progress in Play Mode");
                
                // 進捗監視を開始
                MonitorRenderingProgress();
            }
            
            // Column widths are now loaded from settings in LoadSettings()
            // Validate column widths
            leftColumnWidth = Mathf.Clamp(leftColumnWidth, minColumnWidth, maxColumnWidth);
            centerColumnWidth = Mathf.Clamp(centerColumnWidth, minColumnWidth, maxColumnWidth);
            
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] OnEnable completed - Directors: {sceneTimelineDirectors.Count}, State: {currentState}, DebugMode: {debugMode}");
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            // Save settings
            SaveSettings();
            
            // Save column widths for multi-recorder mode (keep for backward compatibility)
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
                if (recordingQueueDirectors == null)
                {
                    MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] recordingQueueDirectors is null!");
                    recordingQueueDirectors = new List<PlayableDirector>();
                }
                
                if (sceneTimelineDirectors == null)
                {
                    sceneTimelineDirectors = new List<PlayableDirector>();
                }
            }
            
            // Add padding around the entire window content
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            EditorGUILayout.Space(8); // Top padding
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(8); // Left padding
            
            EditorGUILayout.BeginVertical();
            
            // Begin checking for changes
            EditorGUI.BeginChangeCheck();
            
            // Global settings
            DrawGlobalSettings();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // 3-column layout
            DrawMultiRecorderLayout();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Render controls after the main content
            DrawRecordControls();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Status section
            DrawStatusSection();
            EditorGUILayout.Space(Styles.SectionSpacing);
            
            // Debug settings
            DrawDebugSettings();
            
            // Bottom spacing to prevent content from being too close to window edge
            EditorGUILayout.Space(15);
            
            // Save settings if any changes were made
            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(8); // Right padding
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(8); // Bottom padding
            EditorGUILayout.EndVertical();
            
            // Force repaint if GUI changed
            if (GUI.changed)
            {
                Repaint();
            }
        }
        
        private void DrawGlobalSettings()
        {
            // モダンなヘッダーデザイン
            EditorGUILayout.BeginVertical("HelpBox");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Recording Settings", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Resolution and Frame Rate on same line
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Resolution:", GUILayout.Width(70));
            width = EditorGUILayout.IntField(width, GUILayout.Width(60));
            EditorGUILayout.LabelField("×", GUILayout.Width(15));
            height = EditorGUILayout.IntField(height, GUILayout.Width(60));
            
            GUILayout.Space(30);
            
            EditorGUILayout.LabelField("Frame Rate:", GUILayout.Width(80));
            frameRate = EditorGUILayout.IntField(frameRate, GUILayout.Width(60));
            EditorGUILayout.LabelField("fps", GUILayout.Width(30));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // Output path using OutputPathSettingsUI
            OutputPathSettingsUI.DrawOutputPathUI(globalOutputPath);
            
            // Timeline settings on same line, left-aligned
            EditorGUILayout.BeginHorizontal();
            
            // Pre-roll
            EditorGUILayout.LabelField("Pre-roll:", GUILayout.Width(60));
            preRollFrames = EditorGUILayout.IntField(preRollFrames, GUILayout.Width(60));
            EditorGUILayout.LabelField("frames", GUILayout.Width(50));
            
            EditorGUILayout.Space(20);
            
            // Timeline Margin
            EditorGUILayout.LabelField("Timeline Margin:", GUILayout.Width(100));
            timelineMarginFrames = EditorGUILayout.IntField(timelineMarginFrames, GUILayout.Width(60));
            EditorGUILayout.LabelField("frames", GUILayout.Width(50));
            
            GUILayout.FlexibleSpace(); // Push everything to the left
            
            EditorGUILayout.EndHorizontal();
            
            // SignalEmitter設定 (TODO-282)
            EditorGUILayout.Space(5);
            
            // セパレーター線を描画
            Rect separatorRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(5);
            
            DrawSignalEmitterSettings();
            
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
            
            // Draw separator line
            EditorGUILayout.Space(2);
            Rect separatorRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(2);
        }
        
        private void DrawTimelineSelectionColumn()
        {
            // Column container with background
            Rect columnRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(columnRect, Styles.ColumnBackgroundColor);
            }
            
            // Clickable header - Start with getting the rect first
            Rect headerRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.helpBox, GUILayout.Height(24));
            
            // Draw button-like background FIRST
            if (Event.current.type == EventType.Repaint)
            {
                bool isHovered = headerRect.Contains(Event.current.mousePosition);
                bool isPressed = isHovered && Event.current.type == EventType.MouseDown;
                
                Color bgColor = isPressed ? Styles.HeaderButtonPressedColor :
                               isHovered ? Styles.HeaderButtonHoverColor :
                               Styles.HeaderButtonColor;
                
                // Draw background
                var bgRect = new Rect(headerRect.x + 1, headerRect.y + 1, headerRect.width - 2, headerRect.height - 2);
                EditorGUI.DrawRect(bgRect, bgColor);
                
                // Draw border
                Color borderColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.1f, 0.1f, 0.1f, 1f)
                    : new Color(0.5f, 0.5f, 0.5f, 1f);
                    
                // Top border
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 1), borderColor);
                // Bottom border
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1), borderColor);
                // Left border
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 1, headerRect.height), borderColor);
                // Right border
                EditorGUI.DrawRect(new Rect(headerRect.xMax - 1, headerRect.y, 1, headerRect.height), borderColor);
            }
            
            // Draw content AFTER background using GUI methods
            var iconContent = EditorGUIUtility.IconContent("d_Toolbar Plus");
            var iconRect = new Rect(headerRect.x + 4, headerRect.y + 2, 20, 20);
            GUI.Label(iconRect, iconContent);
            
            var textRect = new Rect(headerRect.x + 28, headerRect.y + 2, headerRect.width - 32, 20);
            GUI.Label(textRect, "Add Timeline", EditorStyles.boldLabel);
            
            // Make entire header clickable
            if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
            {
                // Scan for available timelines
                ScanAvailableTimelines();
                
                GenericMenu menu = new GenericMenu();
                
                if (sceneTimelineDirectors.Count > 0)
                {
                    foreach (var director in sceneTimelineDirectors)
                    {
                        // Skip if already added
                        if (recordingQueueDirectors.Contains(director))
                            continue;
                            
                        var timelineName = director.playableAsset != null ? director.playableAsset.name : director.name;
                        menu.AddItem(new GUIContent($"📽️ {timelineName}"), false, () => {
                            AddTimelineDirector(director);
                        });
                    }
                    
                    if (menu.GetItemCount() == 0)
                    {
                        menu.AddDisabledItem(new GUIContent("All timelines already added"));
                    }
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("No timelines found in scene"));
                }
                
                menu.ShowAsContext();
                Event.current.Use();
            }
            
            // Show hover effect and cursor
            if (headerRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(headerRect, MouseCursor.Link);
            }
            
            // Begin horizontal scroll view for the entire column content
            leftColumnScrollPos = EditorGUILayout.BeginScrollView(leftColumnScrollPos, 
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // マトリクスビュー風のリスト表示
            EditorGUILayout.BeginVertical("RL Background", GUILayout.ExpandWidth(true));
            
            if (recordingQueueDirectors.Count > 0)
            {
                // Ensure GUI is enabled for timeline selection
                bool previousGUIState = GUI.enabled;
                GUI.enabled = true;
                
                for (int i = 0; i < recordingQueueDirectors.Count; i++)
                {
                    bool isSelected = selectedDirectorIndices.Contains(i);
                    bool isCurrentForRecorder = (i == currentTimelineIndexForRecorder);
                    
                    // リストアイテム
                    var controlRect = EditorGUILayout.BeginHorizontal("RL Element", GUILayout.Height(20), GUILayout.ExpandWidth(true));
                    
                    // 正確なRectを取得
                    Rect itemRect = controlRect;
                    
                    // マウスホバーとクリックの処理
                    bool isHover = itemRect.Contains(Event.current.mousePosition);
                    
                    // チェックボックスの領域を定義
                    Rect checkboxRect = new Rect(itemRect.x + 4, itemRect.y + 2, 16, 16);
                    
                    // チェックボックス以外の領域でクリックされた場合のみ、タイムラインを選択
                    if (Event.current.type == EventType.MouseDown && isHover && !checkboxRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 0) // Left click
                        {
                            // Save current selection before switching
                            if (currentTimelineIndexForRecorder >= 0)
                            {
                                timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = selectedRecorderIndex;
                            }
                            
                            currentTimelineIndexForRecorder = i;
                            
                            // Restore previous selection for this timeline
                            if (timelineSelectedRecorderIndices.TryGetValue(i, out int savedIndex))
                            {
                                selectedRecorderIndex = savedIndex;
                            }
                            else
                            {
                                selectedRecorderIndex = -1;
                            }
                            
                            Event.current.Use();
                        }
                        else if (Event.current.button == 1) // Right click
                        {
                            GenericMenu menu = new GenericMenu();
                            int index = i; // Capture index for closure
                            
                            menu.AddItem(new GUIContent("Remove Timeline"), false, () => {
                                RemoveTimeline(index);
                            });
                            
                            menu.ShowAsContext();
                            Event.current.Use();
                        }
                    }
                    
                    // 選択状態の背景色
                    if (Event.current.type == EventType.Repaint)
                    {
                        // 偶数行に薄い背景色を適用（ゼブラストライプ）
                        if (i % 2 == 0)
                        {
                            EditorGUI.DrawRect(itemRect, Styles.AlternateRowColor);
                        }
                        
                        if (isCurrentForRecorder)
                        {
                            // アクティブな選択（レコーダー設定用）- 強調された青色のハイライト
                            var selectionRect = new Rect(itemRect.x + 1, itemRect.y, itemRect.width - 2, itemRect.height);
                            EditorGUI.DrawRect(selectionRect, Styles.ActiveSelectionColor);
                            
                            // 左側にアクセントバーを追加
                            var accentRect = new Rect(itemRect.x + 1, itemRect.y, 3, itemRect.height);
                            EditorGUI.DrawRect(accentRect, new Color(0.2f, 0.6f, 1f, 1f));
                        }
                        else if (isHover)
                        {
                            // ホバー
                            EditorGUI.DrawRect(itemRect, Styles.HoverColor);
                        }
                        
                        // 下部に区切り線を追加
                        Color separatorColor = EditorGUIUtility.isProSkin 
                            ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                            : new Color(0.35f, 0.35f, 0.35f, 0.5f);
                        Rect bottomBorder = new Rect(itemRect.x, itemRect.yMax - 1, itemRect.width, 1);
                        EditorGUI.DrawRect(bottomBorder, separatorColor);
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
                                // Save current selection before switching
                                if (currentTimelineIndexForRecorder >= 0)
                                {
                                    timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = selectedRecorderIndex;
                                }
                                
                                currentTimelineIndexForRecorder = i;
                                
                                // Restore previous selection for this timeline
                                if (timelineSelectedRecorderIndices.TryGetValue(i, out int savedIndex))
                                {
                                    selectedRecorderIndex = savedIndex;
                                }
                                else
                                {
                                    selectedRecorderIndex = -1;
                                }
                            }
                        }
                        else
                        {
                            selectedDirectorIndices.Remove(i);
                            // If we removed the current timeline, select another one
                            if (currentTimelineIndexForRecorder == i && selectedDirectorIndices.Count > 0)
                            {
                                // Save current selection before switching
                                timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = selectedRecorderIndex;
                                
                                currentTimelineIndexForRecorder = selectedDirectorIndices[0];
                                
                                // Restore previous selection for the new timeline
                                if (timelineSelectedRecorderIndices.TryGetValue(currentTimelineIndexForRecorder, out int savedIndex))
                                {
                                    selectedRecorderIndex = savedIndex;
                                }
                                else
                                {
                                    selectedRecorderIndex = -1;
                                }
                            }
                        }
                    }
                    
                    GUILayout.Space(24); // Checkboxのスペース
                    
                    // Draw vertical separator after checkbox
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color separatorColor = EditorGUIUtility.isProSkin 
                            ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                            : new Color(0.6f, 0.6f, 0.6f, 0.5f);
                        Rect separatorRect = new Rect(itemRect.x + 28, itemRect.y + 2, 1, itemRect.height - 4);
                        EditorGUI.DrawRect(separatorRect, separatorColor);
                    }
                    
                    GUILayout.Space(8); // Separator後のスペース
                    
                    // Timeline name
                    string timelineName = recordingQueueDirectors[i] != null ? recordingQueueDirectors[i].gameObject.name : "<Missing>";
                    
                    // Always show Timeline Asset icon
                    var timelineIcon = EditorGUIUtility.IconContent("TimelineAsset Icon");
                    if (timelineIcon == null || timelineIcon.image == null)
                    {
                        timelineIcon = EditorGUIUtility.IconContent("UnityEngine.Timeline.TimelineAsset Icon");
                    }
                    
                    // Draw Timeline icon on the left
                    GUILayout.Label(timelineIcon, GUILayout.Width(20), GUILayout.Height(20));
                    
                    // Reduce margin between icons
                    GUILayout.Space(-2);
                    
                    // Create content for timeline name with optional marker icon
                    GUIContent timelineContent;
                    bool showMarkerIcon = false;
                    
                    if (useSignalEmitterTiming && recordingQueueDirectors[i] != null)
                    {
                        var timelineAsset = recordingQueueDirectors[i].playableAsset as TimelineAsset;
                        if (timelineAsset != null)
                        {
                            var recordingRange = SignalEmitterRecordControl.GetRecordingRangeFromSignalsWithFallback(
                                timelineAsset, startTimingName, endTimingName, true);
                            
                            if (recordingRange.isValid)
                            {
                                bool hasSignalTrack = SignalEmitterRecordControl.HasSignalTrackWithValidEmitters(
                                    timelineAsset, startTimingName, endTimingName);
                                
                                // SignalEmitterで実際に期間が検知された場合のみマーカーを表示
                                showMarkerIcon = hasSignalTrack;
                                
                                string rangeText;
                                if (showTimingInFrames)
                                {
                                    int startFrame = Mathf.RoundToInt((float)(recordingRange.startTime * frameRate));
                                    int endFrame = Mathf.RoundToInt((float)(recordingRange.endTime * frameRate));
                                    rangeText = $"({startFrame}f-{endFrame}f)";
                                }
                                else
                                {
                                    rangeText = $"({recordingRange.startTime:F1}s-{recordingRange.endTime:F1}s)";
                                }
                                
                                string tooltip = hasSignalTrack 
                                    ? $"Signal Track with emitters '{startTimingName}' and '{endTimingName}' found"
                                    : $"Using SignalEmitters '{startTimingName}' and '{endTimingName}'";
                                
                                timelineContent = new GUIContent($"{rangeText} {timelineName}", tooltip);
                            }
                            else
                            {
                                timelineContent = new GUIContent($" {timelineName} (No Signals)", 
                                    $"SignalEmitters '{startTimingName}' or '{endTimingName}' not found");
                            }
                        }
                        else
                        {
                            timelineContent = new GUIContent($" {timelineName}");
                        }
                    }
                    else
                    {
                        timelineContent = new GUIContent($" {timelineName}");
                    }
                    
                    // Draw marker icon if SignalEmitter is detected
                    if (showMarkerIcon)
                    {
                        // Use SignalAsset icon for detected SignalEmitters
                        var markerIcon = EditorGUIUtility.IconContent("SignalAsset Icon");
                        GUILayout.Label(markerIcon, GUILayout.Width(20), GUILayout.Height(20));
                    }
                    else
                    {
                        // Add minimal space to align text when no marker icon
                        GUILayout.Space(20);
                    }
                    
                    GUIStyle nameStyle = isCurrentForRecorder ? EditorStyles.boldLabel : Styles.StandardListItem;
                    EditorGUILayout.LabelField(timelineContent, nameStyle, GUILayout.ExpandWidth(true));
                    
                    // Draw vertical separator before duration
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color separatorColor = EditorGUIUtility.isProSkin 
                            ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                            : new Color(0.6f, 0.6f, 0.6f, 0.5f);
                        Rect separatorRect = new Rect(itemRect.x + itemRect.width - 60, itemRect.y + 2, 1, itemRect.height - 4);
                        EditorGUI.DrawRect(separatorRect, separatorColor);
                    }
                    
                    // Show duration
                    var director = recordingQueueDirectors[i];
                    if (director != null)
                    {
                        var timeline = director.playableAsset as TimelineAsset;
                        if (timeline != null)
                        {
                            EditorGUILayout.LabelField($"{timeline.duration:F2}s", GUILayout.Width(50));
                        }
                    }
                    
                    // No remove button - use right-click menu instead
                    
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
            
            // Clickable header - Start with getting the rect first
            Rect headerRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.helpBox, GUILayout.Height(24));
            
            // Draw button-like background FIRST
            if (Event.current.type == EventType.Repaint)
            {
                bool isHovered = headerRect.Contains(Event.current.mousePosition);
                bool isPressed = isHovered && Event.current.type == EventType.MouseDown;
                
                Color bgColor = isPressed ? Styles.HeaderButtonPressedColor :
                               isHovered ? Styles.HeaderButtonHoverColor :
                               Styles.HeaderButtonColor;
                
                // Draw background
                var bgRect = new Rect(headerRect.x + 1, headerRect.y + 1, headerRect.width - 2, headerRect.height - 2);
                EditorGUI.DrawRect(bgRect, bgColor);
                
                // Draw border
                Color borderColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.1f, 0.1f, 0.1f, 1f)
                    : new Color(0.5f, 0.5f, 0.5f, 1f);
                    
                // Top border
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 1), borderColor);
                // Bottom border
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1), borderColor);
                // Left border
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 1, headerRect.height), borderColor);
                // Right border
                EditorGUI.DrawRect(new Rect(headerRect.xMax - 1, headerRect.y, 1, headerRect.height), borderColor);
            }
            
            // Draw content AFTER background using GUI methods
            var addRecorderIcon = EditorGUIUtility.IconContent("d_Toolbar Plus");
            var addRecorderIconRect = new Rect(headerRect.x + 4, headerRect.y + 2, 20, 20);
            GUI.Label(addRecorderIconRect, addRecorderIcon);
            
            var addRecorderTextRect = new Rect(headerRect.x + 28, headerRect.y + 2, headerRect.width - 32, 20);
            GUI.Label(addRecorderTextRect, "Add Recorder", EditorStyles.boldLabel);
            
            // Make entire header clickable
            if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("🎬 Movie"), false, () => AddRecorder(RecorderSettingsType.Movie));
                menu.AddItem(new GUIContent("🖼️ Image Sequence"), false, () => AddRecorder(RecorderSettingsType.Image));
                menu.AddItem(new GUIContent("🌈 AOV Image Sequence"), false, () => AddRecorder(RecorderSettingsType.AOV));
                menu.AddItem(new GUIContent("🎭 Animation Clip"), false, () => AddRecorder(RecorderSettingsType.Animation));
                menu.AddItem(new GUIContent("🗂️ FBX"), false, () => AddRecorder(RecorderSettingsType.FBX));
                menu.AddItem(new GUIContent("📦 Alembic"), false, () => AddRecorder(RecorderSettingsType.Alembic));
                menu.ShowAsContext();
                Event.current.Use();
            }
            
            // Show hover effect and cursor
            if (headerRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(headerRect, MouseCursor.Link);
            }
            
            // Begin horizontal scroll view for the entire column content
            centerColumnScrollPos = EditorGUILayout.BeginScrollView(centerColumnScrollPos,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            // Show take number for current timeline
            if (currentTimelineIndexForRecorder >= 0 && currentTimelineIndexForRecorder < recordingQueueDirectors.Count)
            {
                var currentDirector = recordingQueueDirectors[currentTimelineIndexForRecorder];
                if (currentDirector != null && settings != null)
                {
                    EditorGUILayout.Space(Styles.StandardSpacing);
                    
                    // Timeline-specific Take number
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Take Number:", EditorStyles.miniBoldLabel, GUILayout.Width(85));
                    
                    int currentTake = settings.GetTimelineTakeNumber(currentTimelineIndexForRecorder);
                    EditorGUI.BeginChangeCheck();
                    int newTake = EditorGUILayout.IntField(currentTake, GUILayout.Width(50));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // 値の範囲をチェック（1以上）
                        newTake = Mathf.Max(1, newTake);
                        settings.SetTimelineTakeNumber(currentTimelineIndexForRecorder, newTake);
                        
                        // UI更新を強制
                        Repaint();
                        SaveSettings();
                    }
                    
                    EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true)); // スペーサー
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.Space(Styles.StandardSpacing);
                EditorGUILayout.LabelField("Select a timeline from the left column to configure its recorders.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }
            
            // Get the recorder config for the current timeline
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            
            EditorGUILayout.Space(Styles.StandardSpacing);
            
            // マトリクスビュー風のリスト表示
            EditorGUILayout.BeginVertical("RL Background", GUILayout.ExpandWidth(true));
            
            for (int i = 0; i < currentConfig.RecorderItems.Count; i++)
            {
                var item = currentConfig.RecorderItems[i];
                
                bool isSelected = (i == selectedRecorderIndex);
                
                // リストアイテム
                var controlRect = EditorGUILayout.BeginHorizontal("RL Element", GUILayout.Height(20), GUILayout.ExpandWidth(true));
                
                // 正確なRectを取得
                Rect itemRect = controlRect;
                
                // マウスホバーとクリックの処理
                bool isHover = itemRect.Contains(Event.current.mousePosition);
                if (Event.current.type == EventType.MouseDown && isHover)
                {
                    selectedRecorderIndex = i;
                    // Save selection for current timeline
                    if (currentTimelineIndexForRecorder >= 0)
                    {
                        timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = i;
                    }
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
                
                // 選択状態の背景色
                if (Event.current.type == EventType.Repaint)
                {
                    // 偶数行に薄い背景色を適用（ゼブラストライプ）
                    if (i % 2 == 0)
                    {
                        EditorGUI.DrawRect(itemRect, Styles.AlternateRowColor);
                    }
                    
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
                    
                    // 下部に区切り線を追加
                    Color separatorColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                        : new Color(0.35f, 0.35f, 0.35f, 0.5f);
                    Rect bottomBorder = new Rect(itemRect.x, itemRect.yMax - 1, itemRect.width, 1);
                    EditorGUI.DrawRect(bottomBorder, separatorColor);
                }
                
                // Enable checkbox
                EditorGUI.BeginChangeCheck();
                item.enabled = EditorGUI.Toggle(new Rect(itemRect.x + 4, itemRect.y + 2, 16, 16), item.enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    // 変更を反映
                }
                
                GUILayout.Space(24); // Checkboxのスペース
                
                // Draw vertical separator after checkbox
                if (Event.current.type == EventType.Repaint)
                {
                    Color separatorColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                        : new Color(0.6f, 0.6f, 0.6f, 0.5f);
                    Rect separatorRect = new Rect(itemRect.x + 28, itemRect.y + 2, 1, itemRect.height - 4);
                    EditorGUI.DrawRect(separatorRect, separatorColor);
                }
                
                GUILayout.Space(8); // Separator後のスペース
                
                // Icon based on recorder type
                GUIContent iconContent = GetRecorderIconContent(item.recorderType);
                GUI.Label(new Rect(itemRect.x + 38, itemRect.y + 2, 16, 16), iconContent);
                GUILayout.Space(Styles.IconWidth + 8);
                
                // Draw vertical separator after icon
                if (Event.current.type == EventType.Repaint)
                {
                    Color separatorColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                        : new Color(0.6f, 0.6f, 0.6f, 0.5f);
                    Rect separatorRect = new Rect(itemRect.x + 62, itemRect.y + 2, 1, itemRect.height - 4);
                    EditorGUI.DrawRect(separatorRect, separatorColor);
                }
                
                GUILayout.Space(8); // Separator後のスペース
                
                // Recorder name
                EditorGUILayout.LabelField(item.name, Styles.StandardListItem, GUILayout.ExpandWidth(true));
                
                // 右クリックメニュー対応（既存の機能を維持）
                if (Event.current.type == EventType.ContextClick && isHover)
                {
                    GenericMenu menu = new GenericMenu();
                    int index = i;
                    
                    // Copy to All option (only if multiple timelines are selected)
                    if (selectedDirectorIndices.Count > 1)
                    {
                        menu.AddItem(new GUIContent("Copy to All Timelines"), false, () => {
                            ApplySingleRecorderToAllTimelines(index);
                        });
                        menu.AddSeparator("");
                    }
                    
                    menu.AddItem(new GUIContent("削除"), false, () => {
                        currentConfig.RecorderItems.RemoveAt(index);
                        if (selectedRecorderIndex >= index) selectedRecorderIndex--;
                        // Update saved selection
                        if (currentTimelineIndexForRecorder >= 0)
                        {
                            timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = selectedRecorderIndex;
                        }
                    });
                    menu.AddItem(new GUIContent("複製"), false, () => {
                        var duplicatedItem = item.DeepCopy();
                        duplicatedItem.name = item.name + " Copy";
                        currentConfig.RecorderItems.Insert(index + 1, duplicatedItem);
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("上に移動"), index > 0, () => {
                        var temp = currentConfig.RecorderItems[index];
                        currentConfig.RecorderItems[index] = currentConfig.RecorderItems[index - 1];
                        currentConfig.RecorderItems[index - 1] = temp;
                        selectedRecorderIndex = index - 1;
                        // Update saved selection
                        if (currentTimelineIndexForRecorder >= 0)
                        {
                            timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = selectedRecorderIndex;
                        }
                    });
                    menu.AddItem(new GUIContent("下に移動"), index < currentConfig.RecorderItems.Count - 1, () => {
                        var temp = currentConfig.RecorderItems[index];
                        currentConfig.RecorderItems[index] = currentConfig.RecorderItems[index + 1];
                        currentConfig.RecorderItems[index + 1] = temp;
                        selectedRecorderIndex = index + 1;
                        // Update saved selection
                        if (currentTimelineIndexForRecorder >= 0)
                        {
                            timelineSelectedRecorderIndices[currentTimelineIndexForRecorder] = selectedRecorderIndex;
                        }
                    });
                    menu.ShowAsContext();
                    Event.current.Use();
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
            
            // Inspector風のヘッダー
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("Settings"), GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.Label("Recorder Settings", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
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
            
            // Content wrapper that expands with column width
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
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
            if (currentTimelineIndexForRecorder >= 0 && currentTimelineIndexForRecorder < recordingQueueDirectors.Count)
            {
                var director = recordingQueueDirectors[currentTimelineIndexForRecorder];
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
                MultiTimelineRecorderLogger.LogWarning("[MultiTimelineRecorder] No timeline selected for recorder configuration");
                return;
            }
            
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            var item = MultiRecorderConfig.CreateDefaultRecorder(type);
            
            // Apply global settings
            if (currentConfig.useGlobalResolution)
            {
                item.width = width;
                item.height = height;
            }
            item.frameRate = frameRate;
            
            currentConfig.AddRecorder(item);
            
            // Auto-select the newly added recorder
            selectedRecorderIndex = currentConfig.RecorderItems.Count - 1;
        }
        
        
        
        
        
        
        
        // ========== 欠落していたメソッドの復元 ==========
        
        private void ScanTimelines()
        {
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] ScanTimelines called");
            recordingQueueDirectors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Found {allDirectors.Length} total PlayableDirectors");
            
            foreach (var director in allDirectors)
            {
                if (director != null && director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    recordingQueueDirectors.Add(director);
                    MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Added director: {director.name}");
                }
                else if (director != null)
                {
                    MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Skipped director: {director.name} (asset: {director.playableAsset?.GetType().Name ?? "null"})");
                }
            }
            
            // Remove any null entries that might have been destroyed
            recordingQueueDirectors.RemoveAll(d => d == null || d.gameObject == null);
            
            recordingQueueDirectors.Sort((a, b) => {
                if (a == null || a.gameObject == null) return 1;
                if (b == null || b.gameObject == null) return -1;
                return a.gameObject.name.CompareTo(b.gameObject.name);
            });
            
            if (selectedDirectorIndex >= recordingQueueDirectors.Count)
            {
                selectedDirectorIndex = 0;
            }
            
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] ScanTimelines completed - Found {recordingQueueDirectors.Count} valid directors");
        }
        
        private void ScanAvailableTimelines()
        {
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] ScanAvailableTimelines called");
            sceneTimelineDirectors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            
            foreach (var director in allDirectors)
            {
                if (director != null && director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    sceneTimelineDirectors.Add(director);
                }
            }
            
            // Remove any null entries
            sceneTimelineDirectors.RemoveAll(d => d == null || d.gameObject == null);
            
            // Sort by name
            sceneTimelineDirectors.Sort((a, b) => {
                if (a == null || a.gameObject == null) return 1;
                if (b == null || b.gameObject == null) return -1;
                return a.gameObject.name.CompareTo(b.gameObject.name);
            });
            
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Found {sceneTimelineDirectors.Count} available timelines");
        }
        
        private void AddTimelineDirector(PlayableDirector director)
        {
            if (director == null || recordingQueueDirectors.Contains(director))
                return;
                
            recordingQueueDirectors.Add(director);
            
            // Add to selected indices
            int newIndex = recordingQueueDirectors.Count - 1;
            selectedDirectorIndices.Add(newIndex);
            
            // If this is the first timeline, set it as current
            if (recordingQueueDirectors.Count == 1)
            {
                currentTimelineIndexForRecorder = 0;
                selectedDirectorIndex = 0;
            }
            
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Added timeline: {director.gameObject.name}");
        }
        
        private void RemoveTimeline(int index)
        {
            if (index < 0 || index >= recordingQueueDirectors.Count)
                return;
                
            recordingQueueDirectors.RemoveAt(index);
            
            // Update selected indices
            selectedDirectorIndices.Remove(index);
            for (int j = 0; j < selectedDirectorIndices.Count; j++)
            {
                if (selectedDirectorIndices[j] > index)
                    selectedDirectorIndices[j]--;
            }
            
            // Update current timeline index if needed
            if (currentTimelineIndexForRecorder == index)
            {
                currentTimelineIndexForRecorder = selectedDirectorIndices.Count > 0 ? selectedDirectorIndices[0] : -1;
            }
            else if (currentTimelineIndexForRecorder > index)
            {
                currentTimelineIndexForRecorder--;
            }
            
            // Update selected recorder indices
            if (timelineSelectedRecorderIndices.ContainsKey(index))
            {
                timelineSelectedRecorderIndices.Remove(index);
            }
            
            // Reindex the dictionary
            var newIndices = new Dictionary<int, int>();
            foreach (var kvp in timelineSelectedRecorderIndices)
            {
                int newKey = kvp.Key > index ? kvp.Key - 1 : kvp.Key;
                newIndices[newKey] = kvp.Value;
            }
            timelineSelectedRecorderIndices = newIndices;
            
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Removed timeline at index {index}");
        }
        
        private void OnEditorUpdate()
        {
            // OnPlayModeStateChanged handles Play Mode transitions now
            // This method can be used for other update tasks if needed
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Play Mode state changed: {state}");
            
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Entered Play Mode");
                
                // レンダリングが進行中の場合、PlayModeTimelineRendererを作成
                bool isRendering = EditorPrefs.GetBool("STR_IsRendering", false);
                
                MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] STR_IsRendering: {isRendering}");
                
                if (isRendering)
                {
                    MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Creating PlayModeTimelineRenderer GameObject");
                    currentState = RecordState.Recording;
                    statusMessage = "Recording in Play Mode...";
                    
                    // レンダリングデータを準備
                    string directorName = EditorPrefs.GetString("STR_DirectorName", "");
                    string tempAssetPath = EditorPrefs.GetString("STR_TempAssetPath", "");
                    float duration = EditorPrefs.GetFloat("STR_Duration", 0f);
                    int frameRate = EditorPrefs.GetInt("STR_FrameRate", 24);
                    int preRollFrames = EditorPrefs.GetInt("STR_PreRollFrames", 0);
                    
                    // 診断情報をログ出力
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Play Mode diagnostic info:");
                    MultiTimelineRecorderLogger.Log($"  - DirectorName: {directorName}");
                    MultiTimelineRecorderLogger.Log($"  - TempAssetPath: {tempAssetPath}");
                    MultiTimelineRecorderLogger.Log($"  - Duration: {duration}");
                    MultiTimelineRecorderLogger.Log($"  - FrameRate: {frameRate}");
                    MultiTimelineRecorderLogger.Log($"  - PreRollFrames: {preRollFrames}");
                    
                    // Render Timelineをロード
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Attempting to load timeline from: {tempAssetPath}");
                    
                    // AssetDatabase refresh to ensure latest state
                    AssetDatabase.Refresh();
                    
                    var renderTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
                    if (renderTimeline == null)
                    {
                        MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Failed to load timeline from: {tempAssetPath}");
                        
                        // Check if file exists
                        if (System.IO.File.Exists(tempAssetPath))
                        {
                            MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] File exists but couldn't load as TimelineAsset");
                        }
                        else
                        {
                            MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] File does not exist at path: {tempAssetPath}");
                        }
                        
                        currentState = RecordState.Error;
                        statusMessage = "Failed to load recording timeline";
                        
                        // Clear rendering flag
                        EditorPrefs.SetBool("STR_IsRendering", false);
                        EditorPrefs.SetBool("STR_IsRenderingInProgress", false);
                        EditorPrefs.SetString("STR_Status", "Error: Timeline load failed");
                        
                        return;
                    }
                    
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Successfully loaded timeline: {renderTimeline.name}");
                    
                    // Verify the loaded timeline has recorder tracks
                    MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] === Verifying loaded timeline in Play Mode ===");
                    var loadedTracks = renderTimeline.GetOutputTracks();
                    int totalTracks = 0;
                    int recorderTracks = 0;
                    foreach (var track in loadedTracks)
                    {
                        totalTracks++;
                        if (track is UnityEditor.Recorder.Timeline.RecorderTrack)
                        {
                            recorderTracks++;
                            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Found RecorderTrack in loaded timeline: {track.name}");
                        }
                    }
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Loaded timeline has {totalTracks} total tracks, {recorderTracks} RecorderTracks");
                    
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
                        MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] PlayModeTimelineRenderer successfully created");
                    }
                    else
                    {
                        MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] Failed to create PlayModeTimelineRenderer");
                    }
                    
                    // EditorPrefsをクリア
                    EditorPrefs.SetBool("STR_IsRendering", false);
                    
                    // このEditorWindowでは進行状況の監視のみ行う
                    MonitorRenderingProgress();
                }
                else
                {
                    MultiTimelineRecorderLogger.LogWarning("[MultiTimelineRecorder] STR_IsRendering is false - PlayModeTimelineRenderer will not be created");
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Exiting Play Mode");
                
                // Check if we were recording and reset state
                if (currentState == RecordState.Recording || currentState == RecordState.WaitingForPlayMode)
                {
                    MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Recording was interrupted by user stopping Play Mode");
                    currentState = RecordState.Idle;
                    statusMessage = "Recording stopped by user";
                    renderProgress = 0f;
                    
                    // Clear rendering flags
                    EditorPrefs.SetBool("STR_IsRendering", false);
                    EditorPrefs.SetBool("STR_IsRenderingInProgress", false);
                    EditorPrefs.SetBool("STR_IsRenderingComplete", false);
                    EditorPrefs.DeleteKey("STR_Progress");
                    EditorPrefs.DeleteKey("STR_Status");
                    EditorPrefs.DeleteKey("STR_CurrentTime");
                }
                
                // Take Numberインクリメントフラグをチェック
                if (EditorPrefs.GetBool("STR_IncrementTakeNumber", false))
                {
                    MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Incrementing take numbers as requested");
                    
                    // Take番号をインクリメント
                    if (settings != null && selectedDirectorIndices.Count > 0)
                    {
                        foreach (int idx in selectedDirectorIndices)
                        {
                            settings.IncrementTimelineTakeNumber(idx);
                            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Incremented take number for timeline index {idx}");
                        }
                        
                        // 設定を保存してUIを更新
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        SaveSettings();
                        
                        // UI更新を強制
                        Repaint();
                    }
                    
                    // フラグをクリア
                    EditorPrefs.DeleteKey("STR_IncrementTakeNumber");
                }
                
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
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Starting rendering progress monitoring");
            
            // EditorWindowではコルーチンは使用できないため、
            // EditorApplication.updateを使用して進行状況を監視
            EditorApplication.update += OnRecordingProgressUpdate;
        }
        
        private void DrawRecordControls()
        {
            // Center the buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            bool canRecord = currentState == RecordState.Idle && recordingQueueDirectors.Count > 0 && !EditorApplication.isPlaying;
            
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
            
            // Record button with icon and color
            GUI.enabled = canRecord;
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red for recording
            
            GUIContent recordContent = new GUIContent(" Start Recording", EditorGUIUtility.IconContent("d_PlayButton").image);
            if (GUILayout.Button(recordContent, GUILayout.Height(30), GUILayout.Width(150)))
            {
                StartRecording();
            }
            
            // Stop button with icon
            GUI.enabled = currentState == RecordState.Recording || EditorApplication.isPlaying;
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Gray for stop
            
            GUIContent stopContent = new GUIContent(" Stop Recording", EditorGUIUtility.IconContent("d_Pause").image);
            if (GUILayout.Button(stopContent, GUILayout.Height(30), GUILayout.Width(150)))
            {
                StopRecording();
            }
            
            GUI.backgroundColor = originalColor;
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // Add Reset button if stuck in WaitingForPlayMode
            if (currentState == RecordState.WaitingForPlayMode && !EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Recorder is stuck in WaitingForPlayMode state.", EditorStyles.centeredGreyMiniLabel);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset State", GUILayout.Height(20), GUILayout.Width(100)))
                {
                    currentState = RecordState.Idle;
                    renderCoroutine = null;
                    statusMessage = "State reset to Idle";
                    MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] State manually reset to Idle");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawStatusSection()
        {
            showStatusSection = EditorGUILayout.Foldout(showStatusSection, "Status", true);
            if (!showStatusSection) return;
            
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
                    if (directorIdx < recordingQueueDirectors.Count)
                    {
                        timelineName = recordingQueueDirectors[directorIdx].gameObject.name;
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
        
        /// <summary>
        /// SignalEmitter設定のUI描画 (TODO-282)
        /// </summary>
        private void DrawSignalEmitterSettings()
        {
            // SignalEmitter設定のヘッダー
            EditorGUILayout.BeginHorizontal();
            // Use SignalAsset icon
            var signalIcon = EditorGUIUtility.IconContent("SignalAsset Icon");
            var headerContent = new GUIContent(" Signal Emitter Timing", signalIcon.image);
            EditorGUILayout.LabelField(headerContent, EditorStyles.boldLabel, GUILayout.Width(170));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // SignalEmitter使用のトグル
            var enableContent = new GUIContent("Enable:", "Enable SignalEmitter markers for recording time range control. Recording will be limited to the time range between Start and End signal emitters.");
            EditorGUILayout.LabelField(enableContent, GUILayout.Width(50));
            bool newUseSignalEmitter = EditorGUILayout.Toggle(useSignalEmitterTiming, GUILayout.Width(20));
            if (newUseSignalEmitter != useSignalEmitterTiming)
            {
                useSignalEmitterTiming = newUseSignalEmitter;
                // 設定変更時にバリデーションを実行
                ValidateSignalEmitterSettingsForSelectedTimelines();
            }
            
            if (useSignalEmitterTiming)
            {
                EditorGUILayout.Space(15);
                
                // 開始タイミング名
                var startContent = new GUIContent("Start:", "Name of the SignalEmitter that marks the recording start time");
                EditorGUILayout.LabelField(startContent, GUILayout.Width(40));
                string newStartTiming = EditorGUILayout.TextField(startTimingName, GUILayout.Width(80));
                if (newStartTiming != startTimingName)
                {
                    startTimingName = newStartTiming;
                    ValidateSignalEmitterSettingsForSelectedTimelines();
                }
                
                EditorGUILayout.Space(10);
                
                // 終了タイミング名
                var endContent = new GUIContent("End:", "Name of the SignalEmitter that marks the recording end time");
                EditorGUILayout.LabelField(endContent, GUILayout.Width(30));
                string newEndTiming = EditorGUILayout.TextField(endTimingName, GUILayout.Width(80));
                if (newEndTiming != endTimingName)
                {
                    endTimingName = newEndTiming;
                    ValidateSignalEmitterSettingsForSelectedTimelines();
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            
            // 時間表示形式の切り替え
            if (useSignalEmitterTiming)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                var displayContent = new GUIContent("Display:", "Toggle between frame numbers (24f) and seconds (1.0s) for timing display");
                EditorGUILayout.LabelField(displayContent, GUILayout.Width(50));
                
                string buttonText = showTimingInFrames ? "Switch to Seconds" : "Switch to Frames";
                var buttonContent = new GUIContent(buttonText, "Switch between frame and seconds display format");
                if (GUILayout.Button(buttonContent, GUILayout.Width(120)))
                {
                    showTimingInFrames = !showTimingInFrames;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawDebugSettings()
        {
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Developer & Debug Settings", true);
            if (!showDebugSettings) return;
            
            EditorGUILayout.BeginVertical("HelpBox");
            
            // Settings Management Section
            EditorGUILayout.LabelField("Settings Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Settings ScriptableObject reference
            var newSettings = EditorGUILayout.ObjectField("Settings Asset", settings, typeof(MultiTimelineRecorderSettings), false) as MultiTimelineRecorderSettings;
            if (newSettings != settings)
            {
                settings = newSettings;
                if (settings != null)
                {
                    LoadSettings();
                }
            }
            
            if (settings != null)
            {
                EditorGUILayout.Space(5);
                
                // Configuration management buttons - vertical layout with fixed width
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Timeline Configs", GUILayout.Width(160)))
                {
                    if (EditorUtility.DisplayDialog("Clear Timeline Configurations", 
                        "This will clear all timeline-specific recorder configurations. Are you sure?", 
                        "Clear", "Cancel"))
                    {
                        ClearAllTimelineRecorderConfigs();
                        MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Cleared all timeline recorder configurations");
                    }
                }
                
                if (GUILayout.Button("Clear All Configs", GUILayout.Width(120)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Configurations", 
                        "This will clear ALL recorder configurations (global and timeline-specific). Are you sure?", 
                        "Clear", "Cancel"))
                    {
                        settings.multiRecorderConfig.RecorderItems.Clear();
                        settings.timelineRecorderConfigEntries.Clear();
                        timelineRecorderConfigs.Clear();
                        multiRecorderConfig.RecorderItems.Clear();
                        SaveSettings();
                        MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Cleared all recorder configurations");
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Settings Asset", GUILayout.Width(140)))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            // Separator
            EditorGUILayout.Space(10);
            Rect separatorRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
            EditorGUILayout.Space(10);
            
            // Debug Tools Section
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Debug mode toggle
            EditorGUI.BeginChangeCheck();
            debugMode = EditorGUILayout.Toggle("Keep Generated Assets", debugMode);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("STR_DebugMode", debugMode);
            }
            
            // SignalEmitter debug mode toggle
            EditorGUI.BeginChangeCheck();
            bool signalEmitterDebugMode = EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false);
            signalEmitterDebugMode = EditorGUILayout.Toggle("SignalEmitter Debug Logs", signalEmitterDebugMode);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("MTR_SignalEmitterDebugMode", signalEmitterDebugMode);
            }
            
            if (signalEmitterDebugMode)
            {
                EditorGUILayout.LabelField("SignalEmitter detection logs will be shown in Console.", EditorStyles.miniLabel);
            }
            
            // Debug button to scan SignalEmitters
            if (currentTimelineIndexForRecorder >= 0 && currentTimelineIndexForRecorder < recordingQueueDirectors.Count)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Debug: Scan SignalEmitters in Selected Timeline", GUILayout.Width(280)))
                {
                    var director = recordingQueueDirectors[currentTimelineIndexForRecorder];
                    if (director != null)
                    {
                        var timelineAsset = director.playableAsset as TimelineAsset;
                        if (timelineAsset != null)
                        {
                            Debug.Log($"=== Scanning SignalEmitters in Timeline: {timelineAsset.name} ===");
                            SignalEmitterRecordControl.DebugLogAllSignalEmitters(timelineAsset);
                            
                            // Also check for the specified signal emitters
                            if (useSignalEmitterTiming)
                            {
                                Debug.Log($"=== Checking for specified signals ===");
                                Debug.Log($"Start timing name: '{startTimingName}'");
                                Debug.Log($"End timing name: '{endTimingName}'");
                                
                                var startSignal = SignalEmitterRecordControl.FindSignalEmitterByName(timelineAsset, startTimingName);
                                var endSignal = SignalEmitterRecordControl.FindSignalEmitterByName(timelineAsset, endTimingName);
                                
                                Debug.Log($"Start signal found: {startSignal.HasValue}");
                                Debug.Log($"End signal found: {endSignal.HasValue}");
                                
                                bool hasSignalTrackWithEmitters = SignalEmitterRecordControl.HasSignalTrackWithValidEmitters(
                                    timelineAsset, startTimingName, endTimingName);
                                Debug.Log($"Has Signal Track with valid emitters: {hasSignalTrackWithEmitters}");
                            }
                        }
                    }
                }
            }
            
            if (debugMode)
            {
                EditorGUILayout.LabelField("Generated Timeline assets will not be deleted after rendering.", EditorStyles.miniLabel);
                
                // Show last generated asset if available
                if (!string.IsNullOrEmpty(lastGeneratedAssetPath))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Last Generated Asset:", EditorStyles.miniBoldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(lastGeneratedAssetPath, EditorStyles.miniLabel);
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
                            MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder] Could not find asset at: {lastGeneratedAssetPath}");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Debug asset management buttons
                EditorGUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Clean Debug Assets", GUILayout.Width(130)))
                {
                    CleanDebugAssets();
                }
                
                if (GUILayout.Button("Open Temp Folder", GUILayout.Width(120)))
                {
                    string tempPath = Application.dataPath + "/MultiTimelineRecorder/Temp";
                    if (Directory.Exists(tempPath))
                    {
                        EditorUtility.RevealInFinder(tempPath);
                    }
                    else
                    {
                        MultiTimelineRecorderLogger.LogWarning("[MultiTimelineRecorder] Temp folder does not exist.");
                    }
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }
        
        // ========== その他の必要なメソッド ==========
        
        private void CleanDebugAssets()
        {
            if (EditorUtility.DisplayDialog("Clean Debug Assets",
                "This will delete all [DEBUG] prefixed assets in the Temp folder. Continue?",
                "Yes", "Cancel"))
            {
                string tempDir = "Assets/MultiTimelineRecorder/Temp";
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
                    
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Cleaned {deletedCount} debug assets and objects.");
                    
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
            MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] === StartRecording called ===");
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Current state: {currentState}");
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Selected directors: {recordingQueueDirectors.Count}");
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Selected index: {selectedDirectorIndex}");
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Is Playing: {EditorApplication.isPlaying}");
            
            // SignalEmitter設定の情報を表示 (TODO-282)
            if (useSignalEmitterTiming)
            {
                MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] 🎯 SignalEmitter Mode ENABLED: '{startTimingName}' → '{endTimingName}'");
                
                // 選択されたTimelineでSignalEmitterの状態を表示
                int validTimelines = 0;
                int fallbackTimelines = 0;
                foreach (var director in recordingQueueDirectors)
                {
                    if (director?.playableAsset is TimelineAsset timelineAsset)
                    {
                        bool hasSignalEmitters = SignalEmitterRecordControl.HasValidSignalEmitters(timelineAsset, startTimingName, endTimingName);
                        var recordingRange = SignalEmitterRecordControl.GetRecordingRangeFromSignalsWithFallback(
                            timelineAsset, startTimingName, endTimingName, true);
                        
                        if (hasSignalEmitters)
                        {
                            validTimelines++;
                            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder]   🎯 {director.gameObject.name}: SignalEmitter range {recordingRange.startTime:F1}s → {recordingRange.endTime:F1}s ({recordingRange.duration:F1}s)");
                        }
                        else
                        {
                            fallbackTimelines++;
                            MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder]   ⚠️  {director.gameObject.name}: No SignalEmitters '{startTimingName}'/'{endTimingName}' - using full timeline ({recordingRange.duration:F1}s)");
                        }
                    }
                }
                
                if (fallbackTimelines > 0)
                {
                    MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder] {fallbackTimelines} timeline(s) will use full duration due to missing SignalEmitters");
                }
                
                if (validTimelines > 0 && fallbackTimelines > 0)
                {
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Summary: {validTimelines} with SignalEmitters, {fallbackTimelines} using fallback");
                }
            }
            else
            {
                MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Standard Mode: Recording full Timeline duration");
            }
            
            if (renderCoroutine != null)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Stopping existing coroutine");
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
            }
            
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Starting new coroutine");
            renderCoroutine = EditorCoroutineUtility.StartCoroutine(RenderTimelineCoroutine(), this);
        }
        
        private void StopRecording()
        {
            MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] StopRecording called");
            
            if (renderCoroutine != null)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Stopping render coroutine");
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                renderCoroutine = null;
            }
            
            if (recordingDirector != null)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Stopping rendering director");
                try
                {
                    recordingDirector.Stop();
                }
                catch (System.Exception e)
                {
                    MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder] Error stopping director: {e.Message}");
                }
            }
            
            // Exit Play Mode if active
            if (EditorApplication.isPlaying)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Exiting play mode");
                EditorApplication.isPlaying = false;
            }
            
            // CleanupRenderingを安全に実行
            try
            {
                CleanupRendering();
            }
            catch (System.Exception e)
            {
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Error during cleanup: {e.Message}");
            }
            
            currentState = RecordState.Idle;
            statusMessage = "Recording stopped by user";
            MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] StopRecording completed");
        }
        
        private void OnRecordingProgressUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Rendering progress monitoring ended");
                EditorApplication.update -= OnRecordingProgressUpdate;
                
                // Play Mode終了時の最終状態チェック
                if (EditorPrefs.GetBool("STR_IsRenderingComplete", false))
                {
                    currentState = RecordState.Complete;
                    statusMessage = "Recording complete!";
                    renderProgress = 1f;
                    EditorPrefs.DeleteKey("STR_IsRenderingComplete");
                    
                    // Take番号のインクリメントはPlayModeTimelineRendererが1秒後に行うように変更したためここでは行わない
                    // インクリメントはOnPlayModeStateChangedのExitingPlayModeで処理される
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
                    MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Progress update: {progress:F3} - {status}");
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
                
                // Set flag to increment take number when exiting play mode
                EditorPrefs.SetBool("STR_IncrementTakeNumber", true);
                MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Set flag to increment take numbers when exiting play mode");
                
                // UIを更新
                Repaint();
                
                // 監視を停止
                EditorApplication.update -= OnRecordingProgressUpdate;
                
                // Automatically stop play mode after recording completion
                MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Recording completed - automatically stopping Play Mode");
                EditorApplication.isPlaying = false;
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
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] CleanupRendering started");
            
            // renderTimelineのクリーンアップ
            if (renderTimeline != null && !debugMode)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Deleting render timeline asset");
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
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Destroying rendering director");
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
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Destroying RenderingData GameObject");
                DestroyImmediate(renderingDataGO);
            }
            
            // クリーンアップ: PlayModeTimelineRendererオブジェクトを削除
            var rendererGO = GameObject.Find("[PlayModeTimelineRenderer]");
            if (rendererGO != null)
            {
                MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Destroying PlayModeTimelineRenderer GameObject");
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
            
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] CleanupRendering completed");
        }
        
        private IEnumerator RenderTimelineCoroutine()
        {
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] RenderTimelineCoroutine started");
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Selected directors count: {recordingQueueDirectors.Count}");
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Selected index: {selectedDirectorIndex}");
            
            currentState = RecordState.Preparing;
            statusMessage = "Preparing...";
            renderProgress = 0f;
            
            // Reset progress tracking
            currentRecordingTimelineIndex = 0;
            totalTimelinesToRecord = selectedDirectorIndices.Count;
            
            // Validate selection
            if (recordingQueueDirectors.Count == 0)
            {
                currentState = RecordState.Error;
                statusMessage = "No timelines available";
                MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] No timelines available");
                yield break;
            }
            
            List<PlayableDirector> directorsToRender = new List<PlayableDirector>();
            float totalTimelineDuration = 0f;
            
            // Collect selected directors based on selectedDirectorIndices
            if (selectedDirectorIndices.Count == 0)
            {
                currentState = RecordState.Error;
                statusMessage = "No timelines selected";
                MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] No timelines selected");
                yield break;
            }
            
            // Collect selected directors
            foreach (int idx in selectedDirectorIndices)
            {
                if (idx >= 0 && idx < recordingQueueDirectors.Count)
                {
                    var director = recordingQueueDirectors[idx];
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
                MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] No valid timelines in selection");
                yield break;
            }
            
            // Add margins to total duration
            if (directorsToRender.Count > 1)
            {
                float marginTime = (directorsToRender.Count - 1) * timelineMarginFrames / (float)frameRate;
                totalTimelineDuration += marginTime;
                MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] === Starting multi-timeline render: {directorsToRender.Count} timelines, total duration: {totalTimelineDuration:F2}s ===");
            }
            else
            {
                MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] === Starting render for: {directorsToRender[0].gameObject.name} ===");
                MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Timeline duration: {totalTimelineDuration}");
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
            
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Creating recording timeline...");
            renderTimeline = null;
            try
            {
                MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Creating timeline for {directorsToRender.Count} directors");
                
                if (directorsToRender.Count > 1)
                {
                    MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Calling CreateRenderTimelineMultiple...");
                    renderTimeline = CreateRenderTimelineMultiple(directorsToRender);
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] CreateRenderTimelineMultiple returned: {(renderTimeline != null ? "success" : "null")}");
                }
                else
                {
                    MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] Calling CreateRenderTimeline for single timeline...");
                    var selectedDirector = directorsToRender[0];
                    var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
                    renderTimeline = CreateRenderTimeline(selectedDirector, originalTimeline);
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] CreateRenderTimeline returned: {(renderTimeline != null ? "success" : "null")}");
                }
            }
            catch (System.Exception e)
            {
                currentState = RecordState.Error;
                statusMessage = $"Failed to create timeline: {e.Message}";
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Failed to create recording timeline: {e}");
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Exception details: {e.StackTrace}");
                
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
                MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] CreateRenderTimeline returned null");
                
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
            
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Saving assets...");
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(tempAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            // Verify asset was saved
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Verifying saved asset...");
            var verifyAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
            if (verifyAsset == null)
            {
                currentState = RecordState.Error;
                statusMessage = "Failed to save Timeline asset";
                MultiTimelineRecorderLogger.LogError($"[MultiTimelineRecorder] Failed to verify saved asset at: {tempAssetPath}");
                
                // Restore original playOnAwake values
                foreach (var kvp in originalPlayOnAwakeValues)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
                yield break;
            }
            MultiTimelineRecorderLogger.LogVerbose($"[MultiTimelineRecorder] Asset verified successfully: {verifyAsset.name}");
            
            // Wait to ensure asset is fully saved
            yield return new WaitForSeconds(0.5f);
            
            // Enter Play Mode
            currentState = RecordState.WaitingForPlayMode;
            statusMessage = "Starting Unity Play Mode...";
            
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] === Current Play Mode state: {EditorApplication.isPlaying} ===");
            
            if (!EditorApplication.isPlaying)
            {
                MultiTimelineRecorderLogger.Log("[MultiTimelineRecorder] === Entering Play Mode... ===");
                
                // アセットパスが有効か確認
                MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Saving tempAssetPath to EditorPrefs: {tempAssetPath}");
                if (string.IsNullOrEmpty(tempAssetPath))
                {
                    MultiTimelineRecorderLogger.LogError("[MultiTimelineRecorder] tempAssetPath is null or empty!");
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
                MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Verified EditorPrefs STR_TempAssetPath before Play Mode: {verifyPath}");
                
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
                
                // Copy recorder items from global config if this is a new timeline config
                // This ensures that new timelines have the default recorder settings
                if (multiRecorderConfig != null && multiRecorderConfig.RecorderItems != null)
                {
                    MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Copying {multiRecorderConfig.RecorderItems.Count} recorders from global config to timeline {timelineIndex}");
                    foreach (var item in multiRecorderConfig.RecorderItems)
                    {
                        newConfig.AddRecorder(item.DeepCopy());
                    }
                }
                else
                {
                    MultiTimelineRecorderLogger.LogWarning($"[MultiTimelineRecorder] Global multiRecorderConfig is null or has no items for timeline {timelineIndex}");
                }
                
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
                MultiTimelineRecorderLogger.LogWarning("[MultiTimelineRecorder] No recorder configuration to apply");
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
                    targetConfig.globalOutputPath = sourceConfig.globalOutputPath;
                }
            }
            
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Copied recorder settings to {selectedDirectorIndices.Count - 1} other timelines");
            EditorUtility.DisplayDialog("Copy Complete", 
                $"Recorder settings have been copied to {selectedDirectorIndices.Count - 1} other timeline{(selectedDirectorIndices.Count - 1 > 1 ? "s" : "")}.", 
                "OK");
        }
        
        /// <summary>
        /// Apply a single recorder to all selected timelines
        /// </summary>
        private void ApplySingleRecorderToAllTimelines(int recorderIndex)
        {
            if (currentTimelineIndexForRecorder < 0 || !timelineRecorderConfigs.ContainsKey(currentTimelineIndexForRecorder))
            {
                MultiTimelineRecorderLogger.LogWarning("[MultiTimelineRecorder] No recorder configuration to apply");
                return;
            }
            
            var sourceConfig = timelineRecorderConfigs[currentTimelineIndexForRecorder];
            if (recorderIndex < 0 || recorderIndex >= sourceConfig.RecorderItems.Count)
            {
                MultiTimelineRecorderLogger.LogWarning("[MultiTimelineRecorder] Invalid recorder index");
                return;
            }
            
            var sourceRecorder = sourceConfig.RecorderItems[recorderIndex];
            int appliedCount = 0;
            
            foreach (int timelineIndex in selectedDirectorIndices)
            {
                if (timelineIndex != currentTimelineIndexForRecorder)
                {
                    var targetConfig = GetTimelineRecorderConfig(timelineIndex);
                    
                    // Check if a recorder with the same name already exists
                    var existingIndex = targetConfig.RecorderItems.FindIndex(r => r.name == sourceRecorder.name);
                    
                    if (existingIndex >= 0)
                    {
                        // Replace existing recorder
                        targetConfig.RecorderItems[existingIndex] = MultiRecorderConfig.CloneRecorderItem(sourceRecorder);
                    }
                    else
                    {
                        // Add new recorder
                        var clonedItem = MultiRecorderConfig.CloneRecorderItem(sourceRecorder);
                        targetConfig.AddRecorder(clonedItem);
                    }
                    
                    appliedCount++;
                }
            }
            
            MultiTimelineRecorderLogger.Log($"[MultiTimelineRecorder] Copied recorder '{sourceRecorder.name}' to {appliedCount} other timelines");
            EditorUtility.DisplayDialog("Copy Complete", 
                $"Recorder '{sourceRecorder.name}' has been copied to {appliedCount} other timeline{(appliedCount > 1 ? "s" : "")}.", 
                "OK");
        }
        
        /// <summary>
        /// Adapter class to bridge MultiRecorderConfig.RecorderConfigItem with IRecorderSettingsHost
        /// </summary>
        private class MultiRecorderConfigItemHost : IRecorderSettingsHost
        {
            private MultiRecorderConfig.RecorderConfigItem item;
            private MultiTimelineRecorder renderer;
            
            public MultiRecorderConfigItemHost(MultiRecorderConfig.RecorderConfigItem item, MultiTimelineRecorder renderer)
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
            public bool useGlobalResolution 
            { 
                get => renderer.GetTimelineRecorderConfig(renderer.currentTimelineIndexForRecorder).useGlobalResolution; 
                set 
                { 
                    renderer.GetTimelineRecorderConfig(renderer.currentTimelineIndexForRecorder).useGlobalResolution = value;
                    // Sync with global values when enabled
                    if (value)
                    {
                        item.width = renderer.width;
                        item.height = renderer.height;
                    }
                } 
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
            public RecorderTakeMode takeMode { get => item.takeMode; set => item.takeMode = value; }
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
            
            // Helper method to get timeline's take number
            public int GetTimelineTakeNumber()
            {
                if (renderer.settings != null && renderer.currentTimelineIndexForRecorder >= 0)
                {
                    return renderer.settings.GetTimelineTakeNumber(renderer.currentTimelineIndexForRecorder);
                }
                return 1;
            }
        }
        
        #region Settings Management
        
        /// <summary>
        /// 設定をロード
        /// </summary>
        private void LoadSettings()
        {
            settings = MultiTimelineRecorderSettings.LoadOrCreateSettings();
            
            // 設定から値を復元
            frameRate = settings.frameRate;
            width = settings.width;
            height = settings.height;
            fileName = settings.fileName;
            globalOutputPath = settings.globalOutputPath;
            takeNumber = settings.takeNumber;
            preRollFrames = settings.preRollFrames;
            cameraTag = settings.cameraTag;
            outputResolution = settings.outputResolution;
            
            selectedDirectorIndex = settings.selectedDirectorIndex;
            selectedDirectorIndices = new List<int>(settings.selectedDirectorIndices);
            timelineMarginFrames = settings.timelineMarginFrames;
            
            multiRecorderConfig = settings.multiRecorderConfig;
            timelineRecorderConfigs = settings.GetTimelineRecorderConfigs();
            
            // Validate and clean up timeline recorder configs to prevent duplicates
            ValidateTimelineRecorderConfigs();
            
            // Debug log for GameObject references
            if (multiRecorderConfig != null && multiRecorderConfig.RecorderItems != null)
            {
                foreach (var item in multiRecorderConfig.RecorderItems)
                {
                    if (item.recorderType == RecorderSettingsType.FBX && item.fbxConfig != null)
                    {
                        MultiTimelineRecorderLogger.LogVerbose($"[LoadSettings] FBX Recorder '{item.name}' - targetGameObject: {(item.fbxConfig.targetGameObject != null ? item.fbxConfig.targetGameObject.name : "null")}");
                    }
                    else if (item.recorderType == RecorderSettingsType.Alembic && item.alembicConfig != null)
                    {
                        MultiTimelineRecorderLogger.LogVerbose($"[LoadSettings] Alembic Recorder '{item.name}' - targetGameObject: {(item.alembicConfig.targetGameObject != null ? item.alembicConfig.targetGameObject.name : "null")}");
                    }
                    else if (item.recorderType == RecorderSettingsType.Animation && item.animationConfig != null)
                    {
                        MultiTimelineRecorderLogger.LogVerbose($"[LoadSettings] Animation Recorder '{item.name}' - targetGameObject: {(item.animationConfig.targetGameObject != null ? item.animationConfig.targetGameObject.name : "null")}");
                    }
                }
            }
            
            leftColumnWidth = settings.leftColumnWidth;
            centerColumnWidth = settings.centerColumnWidth;
            
            debugMode = settings.debugMode;
            showStatusSection = settings.showStatusSection;
            showDebugSettings = settings.showDebugSettings;
            
            // SignalEmitter設定の読み込み (TODO-282)
            useSignalEmitterTiming = settings.useSignalEmitterTiming;
            startTimingName = settings.startTimingName;
            endTimingName = settings.endTimingName;
            showTimingInFrames = settings.showTimingInFrames;
            
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Settings loaded");
        }
        
        /// <summary>
        /// 設定を保存
        /// </summary>
        private void SaveSettings()
        {
            if (settings == null) return;
            
            // 設定に値を保存
            settings.frameRate = frameRate;
            settings.width = width;
            settings.height = height;
            settings.fileName = fileName;
            settings.globalOutputPath = globalOutputPath;
            settings.takeNumber = takeNumber;
            settings.preRollFrames = preRollFrames;
            settings.cameraTag = cameraTag;
            settings.outputResolution = outputResolution;
            
            settings.selectedDirectorIndex = selectedDirectorIndex;
            settings.selectedDirectorIndices = new List<int>(selectedDirectorIndices);
            settings.timelineMarginFrames = timelineMarginFrames;
            
            settings.multiRecorderConfig = multiRecorderConfig;
            settings.SetTimelineRecorderConfigs(timelineRecorderConfigs);
            
            // Debug log for GameObject references before saving
            if (multiRecorderConfig != null && multiRecorderConfig.RecorderItems != null)
            {
                foreach (var item in multiRecorderConfig.RecorderItems)
                {
                    if (item.recorderType == RecorderSettingsType.FBX && item.fbxConfig != null)
                    {
                        MultiTimelineRecorderLogger.LogVerbose($"[SaveSettings] FBX Recorder '{item.name}' - targetGameObject: {(item.fbxConfig.targetGameObject != null ? item.fbxConfig.targetGameObject.name : "null")}");
                    }
                    else if (item.recorderType == RecorderSettingsType.Alembic && item.alembicConfig != null)
                    {
                        MultiTimelineRecorderLogger.LogVerbose($"[SaveSettings] Alembic Recorder '{item.name}' - targetGameObject: {(item.alembicConfig.targetGameObject != null ? item.alembicConfig.targetGameObject.name : "null")}");
                    }
                    else if (item.recorderType == RecorderSettingsType.Animation && item.animationConfig != null)
                    {
                        MultiTimelineRecorderLogger.LogVerbose($"[SaveSettings] Animation Recorder '{item.name}' - targetGameObject: {(item.animationConfig.targetGameObject != null ? item.animationConfig.targetGameObject.name : "null")}");
                    }
                }
            }
            
            settings.leftColumnWidth = leftColumnWidth;
            settings.centerColumnWidth = centerColumnWidth;
            
            settings.debugMode = debugMode;
            settings.showStatusSection = showStatusSection;
            settings.showDebugSettings = showDebugSettings;
            
            // SignalEmitter設定の保存 (TODO-282)
            settings.useSignalEmitterTiming = useSignalEmitterTiming;
            settings.startTimingName = startTimingName;
            settings.endTimingName = endTimingName;
            settings.showTimingInFrames = showTimingInFrames;
            
            // 保存
            settings.Save();
            
            MultiTimelineRecorderLogger.LogVerbose("[MultiTimelineRecorder] Settings saved");
        }
        
        /// <summary>
        /// 選択されたTimelineでSignalEmitter設定をバリデーション (TODO-282)
        /// </summary>
        private void ValidateSignalEmitterSettingsForSelectedTimelines()
        {
            if (!useSignalEmitterTiming) return;
            
            // 選択されたTimelineがない場合は何もしない
            if (recordingQueueDirectors == null || recordingQueueDirectors.Count == 0) return;
            
            bool allValid = true;
            var invalidTimelines = new List<string>();
            
            foreach (var director in recordingQueueDirectors)
            {
                if (director?.playableAsset is TimelineAsset timelineAsset)
                {
                    bool isValid = SignalEmitterRecordControl.ValidateSignalEmitterSettings(
                        timelineAsset, startTimingName, endTimingName);
                    
                    if (!isValid)
                    {
                        allValid = false;
                        invalidTimelines.Add(timelineAsset.name);
                    }
                }
            }
            
            if (!allValid)
            {
                Debug.LogWarning($"SignalEmitter validation failed for timelines: {string.Join(", ", invalidTimelines)}");
            }
        }
        
        /// <summary>
        /// Validate and clean up timeline recorder configurations to prevent duplicates
        /// </summary>
        private void ValidateTimelineRecorderConfigs()
        {
            if (timelineRecorderConfigs == null) return;
            
            foreach (var kvp in timelineRecorderConfigs)
            {
                var config = kvp.Value;
                if (config == null || config.RecorderItems == null) continue;
                
                // Remove duplicate recorders with the same name
                var uniqueRecorders = new List<MultiRecorderConfig.RecorderConfigItem>();
                var seenNames = new HashSet<string>();
                
                foreach (var item in config.RecorderItems)
                {
                    if (item == null) continue;
                    
                    // Ensure unique names
                    string baseName = item.name;
                    string uniqueName = baseName;
                    int counter = 1;
                    
                    while (seenNames.Contains(uniqueName))
                    {
                        uniqueName = $"{baseName} ({counter})";
                        counter++;
                    }
                    
                    if (uniqueName != item.name)
                    {
                        item.name = uniqueName;
                        MultiTimelineRecorderLogger.LogWarning($"[ValidateTimelineRecorderConfigs] Renamed duplicate recorder to: {uniqueName}");
                    }
                    
                    seenNames.Add(uniqueName);
                    uniqueRecorders.Add(item);
                }
                
                // Update the config with cleaned list
                if (uniqueRecorders.Count != config.RecorderItems.Count)
                {
                    MultiTimelineRecorderLogger.Log($"[ValidateTimelineRecorderConfigs] Cleaned up recorder list for timeline {kvp.Key}: {config.RecorderItems.Count} -> {uniqueRecorders.Count}");
                    config.RecorderItems.Clear();
                    config.RecorderItems.AddRange(uniqueRecorders);
                }
            }
        }
        
        /// <summary>
        /// Clear all timeline-specific recorder configurations
        /// </summary>
        private void ClearAllTimelineRecorderConfigs()
        {
            if (timelineRecorderConfigs != null)
            {
                timelineRecorderConfigs.Clear();
            }
            
            if (settings != null && settings.timelineRecorderConfigEntries != null)
            {
                settings.timelineRecorderConfigEntries.Clear();
                SaveSettings();
            }
            
            // Re-initialize empty configs for currently selected timelines
            foreach (int index in selectedDirectorIndices)
            {
                GetTimelineRecorderConfig(index);
            }
        }
        
        #endregion
        
    }
}
