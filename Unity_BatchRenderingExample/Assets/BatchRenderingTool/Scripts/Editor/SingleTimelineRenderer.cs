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
    /// Single Timeline Renderer - Renders one timeline at a time with a simple UI
    /// </summary>
    public partial class SingleTimelineRenderer : EditorWindow, IRecorderSettingsHost
    {
        // Static instance tracking
        private static SingleTimelineRenderer instance;
        
        public enum RenderState
        {
            Idle,
            Preparing,
            PreparingAssets,      // アセット準備中
            SavingAssets,         // アセット保存中  
            WaitingForPlayMode,
            InitializingInPlayMode, // Play Mode内での初期化中
            Rendering,
            Complete,
            Error
        }
        
        // UI State
        private RenderState currentState = RenderState.Idle;
        private string statusMessage = "Ready to render";
        private float renderProgress = 0f;
        
        // Timeline selection
        private List<PlayableDirector> availableDirectors = new List<PlayableDirector>();
        private int selectedDirectorIndex = 0;
        
        // Multiple timeline selection support
        private bool isMultiTimelineMode = false;
        private List<int> selectedDirectorIndices = new List<int>();
        private int timelineMarginFrames = 30; // Margin frames between timelines for safety
        
        // Common render settings
        private RecorderSettingsType recorderType = RecorderSettingsType.Image;
        private RecorderSettingsType previousRecorderType = RecorderSettingsType.Image;
        
        // Multi-recorder configuration
        private MultiRecorderConfig multiRecorderConfig = new MultiRecorderConfig();
        private bool useMultiRecorder = false;
        private Vector2 multiRecorderScrollPos;
        private int selectedRecorderIndex = -1; // Currently selected recorder for detail editing
        private Vector2 detailPanelScrollPos;
        
        // Timeline-specific recorder configurations
        private Dictionary<int, MultiRecorderConfig> timelineRecorderConfigs = new Dictionary<int, MultiRecorderConfig>();
        private int currentTimelineIndexForRecorder = -1; // Currently selected timeline for recorder configuration
        
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
        public string filePath = "Recordings"; // Output path
        public int takeNumber = 1;
        public int preRollFrames = 0; // Pre-roll frames for simulation warm-up
        public string cameraTag = "MainCamera";
        public OutputResolution outputResolution = OutputResolution.HD1080p;
        
        // Debug settings
        public bool debugMode = false; // Keep generated assets for debugging
        private string lastGeneratedAssetPath = null; // Track the last generated asset
        
        // Image recorder settings
        public ImageRecorderSettings.ImageRecorderOutputFormat imageOutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        public bool imageCaptureAlpha = false;
        public int jpegQuality = 75;
        public CompressionUtility.EXRCompressionType exrCompression = CompressionUtility.EXRCompressionType.None;
        
        // Movie recorder settings
        public MovieRecorderSettings.VideoRecorderOutputFormat movieOutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        public VideoBitrateMode movieQuality = VideoBitrateMode.High;
        public bool movieCaptureAudio = false;
        public bool movieCaptureAlpha = false;
        public int movieBitrate = 15;
        public AudioBitRateMode audioBitrate = AudioBitRateMode.High;
        public MovieRecorderPreset moviePreset = MovieRecorderPreset.HighQuality1080p;
        public bool useMoviePreset = false;
        
        // AOV recorder settings
        public AOVType selectedAOVTypes = AOVType.Depth | AOVType.Normal | AOVType.Albedo;
        public AOVOutputFormat aovOutputFormat = AOVOutputFormat.EXR16;
        public AOVPreset aovPreset = AOVPreset.Compositing;
        public bool useAOVPreset = false;
        public bool useMultiPartEXR = true;
        public AOVColorSpace aovColorSpace = AOVColorSpace.Linear;
        public AOVCompression aovCompression = AOVCompression.Zip;
        
        // Alembic recorder settings
        public AlembicExportTargets alembicExportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform;
        public AlembicExportScope alembicExportScope = AlembicExportScope.EntireScene;
        public GameObject alembicTargetGameObject = null;
        public AlembicHandedness alembicHandedness = AlembicHandedness.Left;
        public float alembicScaleFactor = 1f;
        public float alembicWorldScale = 1f;
        public float alembicFrameRate = 24f;
        public AlembicTimeSamplingType alembicTimeSamplingType = AlembicTimeSamplingType.Uniform;
        public bool alembicIncludeChildren = true;
        public bool alembicFlattenHierarchy = false;
        public AlembicExportPreset alembicPreset = AlembicExportPreset.AnimationExport;
        public bool useAlembicPreset = false;
        
        // Animation recorder settings
        public AnimationRecordingProperties animationRecordingProperties = AnimationRecordingProperties.TransformOnly;
        public GameObject animationTargetGameObject = null;
        public AnimationRecordingScope animationRecordingScope = AnimationRecordingScope.SingleGameObject;
        public AnimationInterpolationMode animationInterpolationMode = AnimationInterpolationMode.Linear;
        public AnimationCompressionLevel animationCompressionLevel = AnimationCompressionLevel.Medium;
        public bool animationIncludeChildren = true;
        public bool animationClampedTangents = true;
        public bool animationRecordBlendShapes = false;
        // AnimationCompression is handled by AnimationCompressionLevel
        public float animationPositionError = 0.5f;
        public float animationRotationError = 0.5f;
        public float animationScaleError = 0.5f;
        public AnimationExportPreset animationPreset = AnimationExportPreset.SimpleTransform;
        public bool useAnimationPreset = false;
        
        // FBX recorder settings
        public GameObject fbxTargetGameObject = null;
        public FBXRecordedComponent fbxRecordedComponent = FBXRecordedComponent.Camera;
        public bool fbxRecordHierarchy = true;
        public bool fbxClampedTangents = true;
        public FBXAnimationCompressionLevel fbxAnimationCompression = FBXAnimationCompressionLevel.Lossy;
        public bool fbxExportGeometry = true;
        public Transform fbxTransferAnimationSource = null;
        public Transform fbxTransferAnimationDest = null;
        public FBXExportPreset fbxPreset = FBXExportPreset.AnimationExport;
        public bool useFBXPreset = false;
        
        // Rendering objects
        private TimelineAsset renderTimeline;
        private GameObject renderingGameObject;
        private PlayableDirector renderingDirector;
        private EditorCoroutine renderCoroutine;
        private string tempAssetPath;
        
        // Progress tracking
        private float renderStartTime;
        private float lastReportedProgress = -1f;
        
        // UI Editor instances
        private RecorderSettingsEditorBase currentRecorderEditor;
        
        // Scroll position for the UI
        private Vector2 scrollPosition;
        
        // Properties for easy access
        public PlayableDirector selectedDirector => 
            availableDirectors != null && selectedDirectorIndex >= 0 && selectedDirectorIndex < availableDirectors.Count 
            ? availableDirectors[selectedDirectorIndex] 
            : null;
        
        // IRecorderSettingsHost interface implementation properties
        int IRecorderSettingsHost.frameRate { get => frameRate; set => frameRate = value; }
        int IRecorderSettingsHost.width { get => width; set => width = value; }
        int IRecorderSettingsHost.height { get => height; set => height = value; }
        string IRecorderSettingsHost.fileName { get => fileName; set => fileName = value; }
        string IRecorderSettingsHost.filePath { get => filePath; set => filePath = value; }
        int IRecorderSettingsHost.takeNumber { get => takeNumber; set => takeNumber = value; }
        string IRecorderSettingsHost.cameraTag { get => cameraTag; set => cameraTag = value; }
        OutputResolution IRecorderSettingsHost.outputResolution { get => outputResolution; set => outputResolution = value; }
        ImageRecorderSettings.ImageRecorderOutputFormat IRecorderSettingsHost.imageOutputFormat { get => imageOutputFormat; set => imageOutputFormat = value; }
        bool IRecorderSettingsHost.imageCaptureAlpha { get => imageCaptureAlpha; set => imageCaptureAlpha = value; }
        int IRecorderSettingsHost.jpegQuality { get => jpegQuality; set => jpegQuality = value; }
        CompressionUtility.EXRCompressionType IRecorderSettingsHost.exrCompression { get => exrCompression; set => exrCompression = value; }
        MovieRecorderSettings.VideoRecorderOutputFormat IRecorderSettingsHost.movieOutputFormat { get => movieOutputFormat; set => movieOutputFormat = value; }
        VideoBitrateMode IRecorderSettingsHost.movieQuality { get => movieQuality; set => movieQuality = value; }
        bool IRecorderSettingsHost.movieCaptureAudio { get => movieCaptureAudio; set => movieCaptureAudio = value; }
        bool IRecorderSettingsHost.movieCaptureAlpha { get => movieCaptureAlpha; set => movieCaptureAlpha = value; }
        int IRecorderSettingsHost.movieBitrate { get => movieBitrate; set => movieBitrate = value; }
        AudioBitRateMode IRecorderSettingsHost.audioBitrate { get => audioBitrate; set => audioBitrate = value; }
        MovieRecorderPreset IRecorderSettingsHost.moviePreset { get => moviePreset; set => moviePreset = value; }
        bool IRecorderSettingsHost.useMoviePreset { get => useMoviePreset; set => useMoviePreset = value; }
        AOVType IRecorderSettingsHost.selectedAOVTypes { get => selectedAOVTypes; set => selectedAOVTypes = value; }
        AOVOutputFormat IRecorderSettingsHost.aovOutputFormat { get => aovOutputFormat; set => aovOutputFormat = value; }
        AOVPreset IRecorderSettingsHost.aovPreset { get => aovPreset; set => aovPreset = value; }
        bool IRecorderSettingsHost.useAOVPreset { get => useAOVPreset; set => useAOVPreset = value; }
        bool IRecorderSettingsHost.useMultiPartEXR { get => useMultiPartEXR; set => useMultiPartEXR = value; }
        AOVColorSpace IRecorderSettingsHost.aovColorSpace { get => aovColorSpace; set => aovColorSpace = value; }
        AOVCompression IRecorderSettingsHost.aovCompression { get => aovCompression; set => aovCompression = value; }
        AlembicExportTargets IRecorderSettingsHost.alembicExportTargets { get => alembicExportTargets; set => alembicExportTargets = value; }
        AlembicExportScope IRecorderSettingsHost.alembicExportScope { get => alembicExportScope; set => alembicExportScope = value; }
        GameObject IRecorderSettingsHost.alembicTargetGameObject { get => alembicTargetGameObject; set => alembicTargetGameObject = value; }
        AlembicHandedness IRecorderSettingsHost.alembicHandedness { get => alembicHandedness; set => alembicHandedness = value; }
        float IRecorderSettingsHost.alembicWorldScale { get => alembicWorldScale; set => alembicWorldScale = value; }
        float IRecorderSettingsHost.alembicFrameRate { get => alembicFrameRate; set => alembicFrameRate = value; }
        AlembicTimeSamplingType IRecorderSettingsHost.alembicTimeSamplingType { get => alembicTimeSamplingType; set => alembicTimeSamplingType = value; }
        bool IRecorderSettingsHost.alembicIncludeChildren { get => alembicIncludeChildren; set => alembicIncludeChildren = value; }
        bool IRecorderSettingsHost.alembicFlattenHierarchy { get => alembicFlattenHierarchy; set => alembicFlattenHierarchy = value; }
        AlembicExportPreset IRecorderSettingsHost.alembicPreset { get => alembicPreset; set => alembicPreset = value; }
        bool IRecorderSettingsHost.useAlembicPreset { get => useAlembicPreset; set => useAlembicPreset = value; }
        GameObject IRecorderSettingsHost.animationTargetGameObject { get => animationTargetGameObject; set => animationTargetGameObject = value; }
        AnimationRecordingScope IRecorderSettingsHost.animationRecordingScope { get => animationRecordingScope; set => animationRecordingScope = value; }
        bool IRecorderSettingsHost.animationIncludeChildren { get => animationIncludeChildren; set => animationIncludeChildren = value; }
        bool IRecorderSettingsHost.animationClampedTangents { get => animationClampedTangents; set => animationClampedTangents = value; }
        bool IRecorderSettingsHost.animationRecordBlendShapes { get => animationRecordBlendShapes; set => animationRecordBlendShapes = value; }
        float IRecorderSettingsHost.animationPositionError { get => animationPositionError; set => animationPositionError = value; }
        float IRecorderSettingsHost.animationRotationError { get => animationRotationError; set => animationRotationError = value; }
        float IRecorderSettingsHost.animationScaleError { get => animationScaleError; set => animationScaleError = value; }
        AnimationExportPreset IRecorderSettingsHost.animationPreset { get => animationPreset; set => animationPreset = value; }
        bool IRecorderSettingsHost.useAnimationPreset { get => useAnimationPreset; set => useAnimationPreset = value; }
        GameObject IRecorderSettingsHost.fbxTargetGameObject { get => fbxTargetGameObject; set => fbxTargetGameObject = value; }
        FBXRecordedComponent IRecorderSettingsHost.fbxRecordedComponent { get => fbxRecordedComponent; set => fbxRecordedComponent = value; }
        bool IRecorderSettingsHost.fbxRecordHierarchy { get => fbxRecordHierarchy; set => fbxRecordHierarchy = value; }
        bool IRecorderSettingsHost.fbxClampedTangents { get => fbxClampedTangents; set => fbxClampedTangents = value; }
        FBXAnimationCompressionLevel IRecorderSettingsHost.fbxAnimationCompression { get => fbxAnimationCompression; set => fbxAnimationCompression = value; }
        bool IRecorderSettingsHost.fbxExportGeometry { get => fbxExportGeometry; set => fbxExportGeometry = value; }
        Transform IRecorderSettingsHost.fbxTransferAnimationSource { get => fbxTransferAnimationSource; set => fbxTransferAnimationSource = value; }
        Transform IRecorderSettingsHost.fbxTransferAnimationDest { get => fbxTransferAnimationDest; set => fbxTransferAnimationDest = value; }
        FBXExportPreset IRecorderSettingsHost.fbxPreset { get => fbxPreset; set => fbxPreset = value; }
        bool IRecorderSettingsHost.useFBXPreset { get => useFBXPreset; set => useFBXPreset = value; }
        
        [MenuItem("Window/Batch Rendering Tool/Single Timeline Renderer")]
        public static SingleTimelineRenderer ShowWindow()
        {
            var window = GetWindow<SingleTimelineRenderer>();
            window.titleContent = new GUIContent("Single Timeline Renderer");
            window.minSize = new Vector2(300, 450);  // Smaller minimum width to allow for flexible column sizing
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
                currentState = RenderState.Idle;
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
            if ((recorderType == RecorderSettingsType.Image || recorderType == RecorderSettingsType.AOV) 
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
            
            // Initialize file path if empty
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = "Recordings";
            }
            
            // Initialize recorder editor
            if (currentRecorderEditor == null)
            {
                UpdateRecorderEditor();
            }
            
            // Play Mode内でレンダリング中かチェック
            if (EditorApplication.isPlaying && EditorPrefs.GetBool("STR_IsRendering", false))
            {
                // Play Mode内でPlayModeTimelineRendererが処理中
                currentState = RenderState.Rendering;
                statusMessage = "Rendering in Play Mode...";
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
            
            EditorGUILayout.LabelField("Single Timeline Renderer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // Start scroll view
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            try
            {
                DrawTimelineSelection();
                EditorGUILayout.Space(10);
                
                DrawRenderSettings();
                EditorGUILayout.Space(10);
                
                DrawRenderControls();
                EditorGUILayout.Space(10);
                
                DrawStatusSection();
                EditorGUILayout.Space(10);
                
                DrawDebugSettings();
            }
            finally
            {
                // Always end scroll view even if an exception occurs
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void DrawTimelineSelection()
        {
            
            EditorGUILayout.LabelField("Timeline Selection", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Multiple timeline mode toggle for Single Recorder Mode only
            if (!useMultiRecorder)
            {
                EditorGUI.BeginChangeCheck();
                isMultiTimelineMode = EditorGUILayout.Toggle("Multiple Timeline Mode", isMultiTimelineMode);
                if (EditorGUI.EndChangeCheck())
                {
                    // Clear selection when switching modes
                    selectedDirectorIndices.Clear();
                    selectedDirectorIndex = 0;
                    
                    // Initialize with first timeline if switching off multiple mode
                    if (!isMultiTimelineMode && availableDirectors.Count > 0)
                    {
                        selectedDirectorIndices.Add(0);
                    }
                }
                
                if (isMultiTimelineMode)
                {
                    timelineMarginFrames = EditorGUILayout.IntField("Margin Frames", timelineMarginFrames);
                    if (timelineMarginFrames < 0) timelineMarginFrames = 0;
                    
                    if (timelineMarginFrames > 0)
                    {
                        float marginSeconds = timelineMarginFrames / (float)frameRate;
                        EditorGUILayout.HelpBox($"Will add {marginSeconds:F2} seconds between each timeline for safe rendering.", MessageType.Info);
                    }
                }
                
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Available Timelines: {availableDirectors.Count}");
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Manual refresh requested");
                ScanTimelines();
            }
            EditorGUILayout.EndHorizontal();
            
            // Debug info
            if (availableDirectors.Count == 0)
            {
                if (GUILayout.Button("Debug: Show all PlayableDirectors"))
                {
                    var allDirectors = GameObject.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
                    BatchRenderingToolLogger.LogVerbose($"[DEBUG] Total PlayableDirectors in scene: {allDirectors.Length}");
                    foreach (var dir in allDirectors)
                    {
                        BatchRenderingToolLogger.LogVerbose($"[DEBUG] - {dir.name}: playableAsset={dir.playableAsset?.name ?? "null"} (Type: {dir.playableAsset?.GetType().Name ?? "null"})");
                    }
                }
            }
            
            if (availableDirectors.Count > 0)
            {
                if (useMultiRecorder || isMultiTimelineMode)
                {
                    // Multiple timeline selection mode
                    EditorGUILayout.LabelField("Select Timelines:", EditorStyles.miniBoldLabel);
                    
                    // Ensure GUI is enabled for timeline selection
                    bool previousGUIState = GUI.enabled;
                    GUI.enabled = true;
                    
                    // Display checkboxes for each timeline
                    for (int i = 0; i < availableDirectors.Count; i++)
                    {
                        if (availableDirectors[i] != null && availableDirectors[i].gameObject != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            bool isSelected = selectedDirectorIndices.Contains(i);
                            bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                            
                            if (newSelection != isSelected)
                            {
                                if (newSelection)
                                {
                                    selectedDirectorIndices.Add(i);
                                }
                                else
                                {
                                    selectedDirectorIndices.Remove(i);
                                }
                                
                                // Sort to maintain order
                                selectedDirectorIndices.Sort();
                            }
                            
                            EditorGUILayout.LabelField(availableDirectors[i].gameObject.name);
                            
                            var timeline = availableDirectors[i].playableAsset as TimelineAsset;
                            if (timeline != null)
                            {
                                EditorGUILayout.LabelField($"({timeline.duration:F2}s)", GUILayout.Width(60));
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    // Show total duration
                    if (selectedDirectorIndices.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        float totalDuration = 0f;
                        foreach (int idx in selectedDirectorIndices)
                        {
                            if (idx >= 0 && idx < availableDirectors.Count)
                            {
                                var timeline = availableDirectors[idx].playableAsset as TimelineAsset;
                                if (timeline != null)
                                {
                                    totalDuration += (float)timeline.duration;
                                }
                            }
                        }
                        
                        // Add margins
                        if (selectedDirectorIndices.Count > 1)
                        {
                            float marginTime = (selectedDirectorIndices.Count - 1) * timelineMarginFrames / (float)frameRate;
                            totalDuration += marginTime;
                        }
                        
                        EditorGUILayout.LabelField($"Total Duration: {totalDuration:F2} seconds", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Selected Timelines: {selectedDirectorIndices.Count}");
                    }
                    
                    // Restore previous GUI state
                    GUI.enabled = previousGUIState;
                }
                else
                {
                    // Single timeline selection mode (existing code)
                    string[] directorNames = new string[availableDirectors.Count];
                    for (int i = 0; i < availableDirectors.Count; i++)
                    {
                        if (availableDirectors[i] != null && availableDirectors[i].gameObject != null)
                        {
                            directorNames[i] = availableDirectors[i].gameObject.name;
                        }
                        else
                        {
                            directorNames[i] = "<Missing>";
                        }
                    }
                    
                    int previousIndex = selectedDirectorIndex;
                    selectedDirectorIndex = EditorGUILayout.Popup("Select Timeline:", selectedDirectorIndex, directorNames);
                    
                    // Validate selected index and director
                    if (selectedDirectorIndex >= availableDirectors.Count)
                    {
                        selectedDirectorIndex = 0;
                    }
                    
                    // Update selectedDirectorIndices when selection changes
                    if (previousIndex != selectedDirectorIndex)
                    {
                        selectedDirectorIndices.Clear();
                        selectedDirectorIndices.Add(selectedDirectorIndex);
                    }
                    
                    var selectedDirector = availableDirectors[selectedDirectorIndex];
                    if (selectedDirector != null && selectedDirector.gameObject != null)
                    {
                        var timeline = selectedDirector.playableAsset as TimelineAsset;
                        if (timeline != null)
                        {
                            EditorGUILayout.LabelField($"Duration: {timeline.duration:F2} seconds");
                            EditorGUILayout.LabelField($"Frame Count: {(int)(timeline.duration * frameRate)}");
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No timelines found in the scene.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRenderSettings()
        {
            EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Multi-recorder mode toggle
            useMultiRecorder = EditorGUILayout.Toggle("Multi-Recorder Mode", useMultiRecorder);
            
            EditorGUILayout.Space(5);
            
            if (useMultiRecorder)
            {
                DrawMultiRecorderSettings();
            }
            else
            {
                DrawSingleRecorderSettings();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSingleRecorderSettings()
        {
            // Recorder type selection
            RecorderSettingsType newRecorderType = (RecorderSettingsType)EditorGUILayout.EnumPopup("Recorder Type:", recorderType);
            
            // Handle recorder type change
            if (newRecorderType != recorderType)
            {
                previousRecorderType = recorderType;
                recorderType = newRecorderType;
                
                // Update default file name when recorder type changes
                fileName = "<Scene>_<Recorder>_<Take>";
                
                // Create appropriate editor instance
                UpdateRecorderEditor();
            }
            
            // Ensure <Frame> wildcard is present for image sequence types
            if ((recorderType == RecorderSettingsType.Image || recorderType == RecorderSettingsType.AOV) 
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
            
            // Check if recorder type is supported
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(recorderType))
            {
                string reason = "";
                switch (recorderType)
                {
                    case RecorderSettingsType.AOV:
                        reason = "AOV Recorder requires HDRP package to be installed";
                        break;
                    case RecorderSettingsType.Alembic:
                        reason = "Alembic Recorder requires Unity Alembic package to be installed";
                        break;
                    default:
                        reason = $"{recorderType} recorder is not available";
                        break;
                }
                EditorGUILayout.HelpBox(reason, MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.Space(5);
            
            // Common settings
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            
            // Pre-roll frames for simulation warm-up
            EditorGUILayout.Space(5);
            preRollFrames = EditorGUILayout.IntField("Pre-roll Frames:", preRollFrames);
            if (preRollFrames < 0) preRollFrames = 0;
            
            if (preRollFrames > 0)
            {
                float preRollSeconds = preRollFrames / (float)frameRate;
                EditorGUILayout.HelpBox($"Timeline will run at frame 0 for {preRollSeconds:F2} seconds before recording starts. " +
                    "This allows physics simulations (cloth, particles, etc.) to stabilize.", MessageType.Info);
            }
            
            // Use new recorder editor system
            if (currentRecorderEditor == null)
            {
                UpdateRecorderEditor();
            }
            
            EditorGUILayout.Space(5);
            
            // Draw recorder-specific UI using new editor system
            if (currentRecorderEditor != null)
            {
                currentRecorderEditor.DrawRecorderSettings();
            }
        }
        
        private void DrawMultiRecorderSettings()
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
            
            // Global settings at bottom
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Global Settings", EditorStyles.miniBoldLabel);
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            
            // Timeline margin frames (for multiple timeline selection)
            if (selectedDirectorIndices.Count > 1)
            {
                timelineMarginFrames = EditorGUILayout.IntField("Timeline Margin Frames:", timelineMarginFrames);
                if (timelineMarginFrames < 0) timelineMarginFrames = 0;
                
                if (timelineMarginFrames > 0)
                {
                    float marginSeconds = timelineMarginFrames / (float)frameRate;
                    EditorGUILayout.HelpBox($"Will add {marginSeconds:F2} seconds between each timeline for safe rendering.", MessageType.Info);
                }
            }
            
            // Pre-roll frames
            preRollFrames = EditorGUILayout.IntField("Pre-roll Frames:", preRollFrames);
            if (preRollFrames < 0) preRollFrames = 0;
            
            if (preRollFrames > 0)
            {
                float preRollSeconds = preRollFrames / (float)frameRate;
                EditorGUILayout.HelpBox($"Timeline will run at frame 0 for {preRollSeconds:F2} seconds before recording starts.", MessageType.Info);
            }
            
            // Summary of recorder configurations
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Timeline Recorder Summary:", EditorStyles.miniBoldLabel);
            int totalRecorders = 0;
            foreach (int idx in selectedDirectorIndices)
            {
                var config = GetTimelineRecorderConfig(idx);
                totalRecorders += config.GetEnabledRecorders().Count;
            }
            EditorGUILayout.LabelField($"Total Active Recorders: {totalRecorders} across {selectedDirectorIndices.Count} timelines");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTimelineSelectionColumn()
        {
            // Begin horizontal scroll view for the entire column
            leftColumnScrollPos = EditorGUILayout.BeginScrollView(leftColumnScrollPos, 
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            // Header with add button
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("①Timeline 選択", headerStyle, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Refresh button
            if (GUILayout.Button("Refresh Timelines", GUILayout.Height(25), GUILayout.MinWidth(200)))
            {
                ScanTimelines();
            }
            
            EditorGUILayout.Space(5);
            
            // Timeline list
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(200));
            if (availableDirectors.Count > 0)
            {
                // Ensure GUI is enabled for timeline selection
                bool previousGUIState = GUI.enabled;
                GUI.enabled = true;
                
                for (int i = 0; i < availableDirectors.Count; i++)
                {
                    bool isSelected = selectedDirectorIndices.Contains(i);
                    bool isCurrentForRecorder = (i == currentTimelineIndexForRecorder);
                    
                    if (isCurrentForRecorder)
                    {
                        // Highlight the timeline currently selected for recorder configuration
                        GUI.backgroundColor = new Color(0.8f, 0.5f, 0.3f, 0.5f);
                    }
                    else if (isSelected)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
                    }
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Checkbox for selection
                    bool nowSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    if (nowSelected != isSelected)
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
                    
                    // Timeline name (clickable)
                    string timelineName = availableDirectors[i] != null ? availableDirectors[i].gameObject.name : "<Missing>";
                    
                    if (isCurrentForRecorder)
                    {
                        GUIStyle boldStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
                        if (GUILayout.Button(timelineName, boldStyle, GUILayout.MinWidth(150)))
                        {
                            // Already selected, do nothing
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(timelineName, EditorStyles.label, GUILayout.MinWidth(150)))
                        {
                            // Select this timeline for recorder configuration
                            currentTimelineIndexForRecorder = i;
                            selectedRecorderIndex = -1; // Reset recorder selection
                        }
                    }
                    
                    // Show duration
                    if (availableDirectors[i] != null)
                    {
                        var timeline = availableDirectors[i].playableAsset as TimelineAsset;
                        if (timeline != null)
                        {
                            EditorGUILayout.LabelField($"({timeline.duration:F2}s)", GUILayout.Width(60));
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (isSelected)
                    {
                        GUI.backgroundColor = Color.white;
                    }
                }
                
                // Show total duration and margin info
                if (selectedDirectorIndices.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    float totalDuration = 0f;
                    foreach (int idx in selectedDirectorIndices)
                    {
                        if (idx >= 0 && idx < availableDirectors.Count)
                        {
                            var timeline = availableDirectors[idx].playableAsset as TimelineAsset;
                            if (timeline != null)
                            {
                                totalDuration += (float)timeline.duration;
                            }
                        }
                    }
                    
                    // Add margins
                    if (selectedDirectorIndices.Count > 1)
                    {
                        float marginTime = (selectedDirectorIndices.Count - 1) * timelineMarginFrames / (float)frameRate;
                        totalDuration += marginTime;
                        EditorGUILayout.LabelField($"Margin: {timelineMarginFrames} frames", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.LabelField($"Total: {totalDuration:F2}s", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Selected: {selectedDirectorIndices.Count}", EditorStyles.miniLabel);
                    
                    EditorGUILayout.EndVertical();
                }
                
                // Restore GUI state
                GUI.enabled = previousGUIState;
            }
            else
            {
                EditorGUILayout.HelpBox("No timelines found in the scene.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawRecorderListColumn()
        {
            // Begin horizontal scroll view for the entire column
            centerColumnScrollPos = EditorGUILayout.BeginScrollView(centerColumnScrollPos,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
            // Header with add button
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("②Recorder 設定", headerStyle, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();
            
            // Show current timeline name
            if (currentTimelineIndexForRecorder >= 0 && currentTimelineIndexForRecorder < availableDirectors.Count)
            {
                var currentDirector = availableDirectors[currentTimelineIndexForRecorder];
                if (currentDirector != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Timeline:", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField(currentDirector.gameObject.name, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Select a timeline from the left column to configure its recorders.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            // Get the recorder config for the current timeline
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            
            EditorGUILayout.Space(5);
            
            // Add Recorder button
            if (GUILayout.Button("+ Add Recorder", GUILayout.Height(25), GUILayout.MinWidth(200)))
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
            
            EditorGUILayout.Space(5);
            
            // Apply to all timelines button
            if (GUILayout.Button("Apply to All Selected Timelines", GUILayout.Height(25), GUILayout.MinWidth(200)))
            {
                ApplyRecorderSettingsToAllTimelines();
            }
            
            EditorGUILayout.Space(5);
            
            // Recorder list with minimum width
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(200));
            
            for (int i = 0; i < currentConfig.RecorderItems.Count; i++)
            {
                var item = currentConfig.RecorderItems[i];
                
                bool isSelected = (i == selectedRecorderIndex);
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
                }
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                // Enable checkbox
                item.enabled = EditorGUILayout.Toggle(item.enabled, GUILayout.Width(20));
                
                // Icon based on recorder type
                string icon = GetRecorderIcon(item.recorderType);
                EditorGUILayout.LabelField(icon, GUILayout.Width(25));
                
                // Recorder name - clickable with minimum width
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                if (isSelected) labelStyle.fontStyle = FontStyle.Bold;
                
                if (GUILayout.Button(item.name, labelStyle, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true)))
                {
                    selectedRecorderIndex = i;
                    GUI.FocusControl(null);
                }
                
                // Delete button
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    currentConfig.RecorderItems.RemoveAt(i);
                    if (selectedRecorderIndex >= i) selectedRecorderIndex--;
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawRecorderDetailColumn()
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("③Recorder 詳細設定", headerStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Get the current timeline's recorder config
            if (currentTimelineIndexForRecorder < 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Select a timeline from the left column first.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                return;
            }
            
            var currentConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
            
            if (selectedRecorderIndex < 0 || selectedRecorderIndex >= currentConfig.RecorderItems.Count)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Select a recorder from the list to edit its settings.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
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
            
            // Recorder Type header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string recorderTypeName = GetRecorderTypeName(item.recorderType);
            EditorGUILayout.LabelField("Recorder Type", recorderTypeName, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Check if recorder type is supported
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(item.recorderType))
            {
                string reason = GetUnsupportedReason(item.recorderType);
                EditorGUILayout.HelpBox(reason, MessageType.Error);
                EditorGUILayout.EndVertical(); // End content wrapper
                EditorGUILayout.EndScrollView();
                return;
            }
            
            // Use RecorderEditor to draw all settings
            if (editor != null)
            {
                editor.DrawRecorderSettings();
            }
            else
            {
                EditorGUILayout.HelpBox("Recorder editor not available for this type.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical(); // End content wrapper
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draws a vertical splitter that can be dragged to resize columns
        /// </summary>
        private void DrawVerticalSplitter(ref float columnWidth, ref bool isDragging)
        {
            // Get the rect for the splitter
            Rect splitterRect = GUILayoutUtility.GetRect(splitterWidth, 1, GUILayout.ExpandHeight(true));
            
            // Determine color based on state
            Color splitterColor;
            if (isDragging)
            {
                splitterColor = new Color(0.2f, 0.5f, 0.8f, 0.8f); // Blue when dragging
            }
            else if (splitterRect.Contains(Event.current.mousePosition))
            {
                splitterColor = new Color(0.6f, 0.6f, 0.6f, 0.8f); // Lighter when hovering
            }
            else
            {
                splitterColor = new Color(0.4f, 0.4f, 0.4f, 0.3f); // Default color
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
        
        private string GetRecorderIcon(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Movie: return "🎬";
                case RecorderSettingsType.Image: return "🖼️";
                case RecorderSettingsType.AOV: return "🌈";
                case RecorderSettingsType.Animation: return "🎭";
                case RecorderSettingsType.FBX: return "🗂️";
                case RecorderSettingsType.Alembic: return "📦";
                default: return "📹";
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
                    currentState = RenderState.Rendering;
                    statusMessage = "Rendering in Play Mode...";
                    
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
                        
                        currentState = RenderState.Error;
                        statusMessage = "Failed to load render timeline";
                        
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
                
                // 状態はすでにOnRenderingProgressUpdateで更新されているため、
                // ここでは最終的なクリーンアップのみ行う
                
                // 監視を停止
                EditorApplication.update -= OnRenderingProgressUpdate;
                
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
        
        private void UpdateRecorderEditor()
        {
            currentRecorderEditor = recorderType switch
            {
                RecorderSettingsType.Image => new ImageRecorderEditor(this),
                RecorderSettingsType.Movie => new MovieRecorderEditor(this),
                RecorderSettingsType.AOV => new AOVRecorderEditor(this),
                RecorderSettingsType.Alembic => new AlembicRecorderEditor(this),
                RecorderSettingsType.Animation => new AnimationRecorderEditor(this),
                RecorderSettingsType.FBX => new FBXRecorderEditor(this),
                _ => null
            };
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
            EditorApplication.update += OnRenderingProgressUpdate;
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool canRender = currentState == RenderState.Idle && availableDirectors.Count > 0 && !EditorApplication.isPlaying;
            
            // Validate timeline selection
            canRender = canRender && selectedDirectorIndices.Count > 0;
            
            // Additional validation for multi-recorder mode
            if (useMultiRecorder)
            {
                canRender = canRender && multiRecorderConfig.GetEnabledRecorders().Count > 0;
            }
            else
            {
                canRender = canRender && RecorderSettingsFactory.IsRecorderTypeSupported(recorderType);
            }
            
            // Add Reset button if stuck in WaitingForPlayMode
            if (currentState == RenderState.WaitingForPlayMode && !EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Renderer is stuck in WaitingForPlayMode state. Click Reset to fix.", MessageType.Warning);
                if (GUILayout.Button("Reset State", GUILayout.Height(25)))
                {
                    currentState = RenderState.Idle;
                    renderCoroutine = null;
                    statusMessage = "State reset to Idle";
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] State manually reset to Idle");
                }
            }
            
            GUI.enabled = canRender;
            if (GUILayout.Button("Start Rendering", GUILayout.Height(30)))
            {
                StartRendering();
            }
            
            GUI.enabled = currentState == RenderState.Rendering || EditorApplication.isPlaying;
            if (GUILayout.Button("Stop Rendering", GUILayout.Height(30)))
            {
                StopRendering();
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Status message
            Color originalColor = GUI.color;
            switch (currentState)
            {
                case RenderState.Error:
                    GUI.color = Color.red;
                    break;
                case RenderState.Complete:
                    GUI.color = Color.green;
                    break;
                case RenderState.Rendering:
                    GUI.color = Color.yellow;
                    break;
            }
            
            EditorGUILayout.LabelField($"State: {currentState}");
            GUI.color = originalColor;
            
            EditorGUILayout.LabelField($"Message: {statusMessage}");
            
            // Progress bar
            if (currentState == RenderState.Rendering || currentState == RenderState.PreparingAssets || currentState == RenderState.WaitingForPlayMode)
            {
                // 美しいプログレスバー
                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
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
                EditorGUILayout.HelpBox("Debug Mode Active: Generated Timeline assets and GameObjects will not be deleted after rendering.", MessageType.Info);
                
                // Show last generated asset if available
                if (!string.IsNullOrEmpty(lastGeneratedAssetPath))
                {
                    EditorGUILayout.Space(5);
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
                EditorGUILayout.Space(10);
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
        
        private void StartRendering()
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === StartRendering called ===");
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
        
        private void StopRendering()
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] StopRendering called");
            
            if (renderCoroutine != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Stopping render coroutine");
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                renderCoroutine = null;
            }
            
            if (renderingDirector != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Stopping rendering director");
                try
                {
                    renderingDirector.Stop();
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
            
            currentState = RenderState.Idle;
            statusMessage = "Rendering stopped by user";
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] StopRendering completed");
        }
        
        private void OnRenderingProgressUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Rendering progress monitoring ended");
                EditorApplication.update -= OnRenderingProgressUpdate;
                
                // Play Mode終了時の最終状態チェック
                if (EditorPrefs.GetBool("STR_IsRenderingComplete", false))
                {
                    currentState = RenderState.Complete;
                    statusMessage = "Rendering complete!";
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
                currentState = RenderState.Complete;
                statusMessage = "Rendering complete!";
                renderProgress = 1f;
                
                // UIを更新
                Repaint();
                
                // 監視を停止
                EditorApplication.update -= OnRenderingProgressUpdate;
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
            
            // renderingDirectorのクリーンアップ
            if (renderingDirector != null)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Destroying rendering director");
                if (renderingDirector.gameObject != null)
                {
                    DestroyImmediate(renderingDirector.gameObject);
                }
                renderingDirector = null;
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
            
            currentState = RenderState.Preparing;
            statusMessage = "Preparing...";
            renderProgress = 0f;
            
            // Validate selection
            if (availableDirectors.Count == 0)
            {
                currentState = RenderState.Error;
                statusMessage = "No timelines available";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] No timelines available");
                yield break;
            }
            
            List<PlayableDirector> directorsToRender = new List<PlayableDirector>();
            float totalTimelineDuration = 0f;
            
            // Collect selected directors based on selectedDirectorIndices
            if (selectedDirectorIndices.Count == 0)
            {
                currentState = RenderState.Error;
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
                currentState = RenderState.Error;
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
            currentState = RenderState.PreparingAssets;
            statusMessage = "Creating render timeline...";
            yield return null; // Allow UI to update
            
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating render timeline...");
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
                currentState = RenderState.Error;
                statusMessage = $"Failed to create timeline: {e.Message}";
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create render timeline: {e}");
                
                // Restore original playOnAwake values
                foreach (var kvp in originalPlayOnAwakeValues)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
                yield break;
            }
            
            if (renderTimeline == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Failed to create render timeline";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] CreateRenderTimeline returned null");
                
                // Restore original playOnAwake values
                foreach (var kvp in originalPlayOnAwakeValues)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
                yield break;
            }
            
            // Save assets and verify
            currentState = RenderState.SavingAssets;
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
                currentState = RenderState.Error;
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
            currentState = RenderState.WaitingForPlayMode;
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
                    currentState = RenderState.Error;
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
                EditorPrefs.SetInt("STR_RecorderType", (int)recorderType);
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
                // Copy global settings from the main multiRecorderConfig
                newConfig.useGlobalResolution = multiRecorderConfig.useGlobalResolution;
                newConfig.globalWidth = multiRecorderConfig.globalWidth;
                newConfig.globalHeight = multiRecorderConfig.globalHeight;
                newConfig.globalOutputPath = multiRecorderConfig.globalOutputPath;
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
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Applied recorder settings to {selectedDirectorIndices.Count - 1} timelines");
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
                get => renderer.multiRecorderConfig.useGlobalResolution ? renderer.multiRecorderConfig.globalWidth : item.width; 
                set => item.width = value; 
            }
            public int height 
            { 
                get => renderer.multiRecorderConfig.useGlobalResolution ? renderer.multiRecorderConfig.globalHeight : item.height; 
                set => item.height = value; 
            }
            public string fileName { get => item.fileName; set => item.fileName = value; }
            public string filePath { get => renderer.multiRecorderConfig.globalOutputPath; set => renderer.multiRecorderConfig.globalOutputPath = value; }
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
