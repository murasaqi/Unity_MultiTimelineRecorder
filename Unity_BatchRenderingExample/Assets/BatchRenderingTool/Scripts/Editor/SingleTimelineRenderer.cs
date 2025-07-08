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
    public class SingleTimelineRenderer : EditorWindow, IRecorderSettingsHost
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
        
        // Common render settings
        private RecorderSettingsType recorderType = RecorderSettingsType.Image;
        private RecorderSettingsType previousRecorderType = RecorderSettingsType.Image;
        
        // Multi-recorder configuration
        private MultiRecorderConfig multiRecorderConfig = new MultiRecorderConfig();
        private bool useMultiRecorder = false;
        private Vector2 multiRecorderScrollPos;
        private int selectedRecorderIndex = -1; // Currently selected recorder for detail editing
        private Vector2 detailPanelScrollPos;
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
            window.minSize = new Vector2(400, 450);
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
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] OnEnable completed - Directors: {availableDirectors.Count}, State: {currentState}, DebugMode: {debugMode}");
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
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
                
                selectedDirectorIndex = EditorGUILayout.Popup("Select Timeline:", selectedDirectorIndex, directorNames);
                
                // Validate selected index and director
                if (selectedDirectorIndex >= availableDirectors.Count)
                {
                    selectedDirectorIndex = 0;
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
            // Three-column layout
            EditorGUILayout.BeginHorizontal();
            
            // Left column - Timeline selection (①Timeline の追加)
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawTimelineSelectionColumn();
            EditorGUILayout.EndVertical();
            
            // Separator
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
            
            // Center column - Recorder list (②Recorder の追加)
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawRecorderListColumn();
            EditorGUILayout.EndVertical();
            
            // Separator
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
            
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
            
            // Pre-roll frames
            preRollFrames = EditorGUILayout.IntField("Pre-roll Frames:", preRollFrames);
            if (preRollFrames < 0) preRollFrames = 0;
            
            if (preRollFrames > 0)
            {
                float preRollSeconds = preRollFrames / (float)frameRate;
                EditorGUILayout.HelpBox($"Timeline will run at frame 0 for {preRollSeconds:F2} seconds before recording starts.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTimelineSelectionColumn()
        {
            // Header with add button
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("①Timeline の追加", headerStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Add Timeline button
            if (GUILayout.Button("+ Add Timeline", GUILayout.Height(25)))
            {
                EditorGUILayout.HelpBox("Timeline addition is managed through Unity's Timeline window", MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
            
            // Timeline list
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (availableDirectors.Count > 0)
            {
                for (int i = 0; i < availableDirectors.Count; i++)
                {
                    bool isSelected = (i == selectedDirectorIndex);
                    if (isSelected)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
                    }
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Checkbox for selection
                    bool wasSelected = isSelected;
                    bool nowSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(20));
                    if (nowSelected && !wasSelected)
                    {
                        selectedDirectorIndex = i;
                    }
                    
                    // Timeline name
                    string timelineName = availableDirectors[i] != null ? availableDirectors[i].gameObject.name : "<Missing>";
                    EditorGUILayout.LabelField(timelineName);
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (isSelected)
                    {
                        GUI.backgroundColor = Color.white;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No timelines found in the scene.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRecorderListColumn()
        {
            // Header with add button
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("②Recorder の追加", headerStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Add Recorder button
            if (GUILayout.Button("+ Add Recorder", GUILayout.Height(25)))
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
            
            // Recorder list
            multiRecorderScrollPos = EditorGUILayout.BeginScrollView(multiRecorderScrollPos);
            
            for (int i = 0; i < multiRecorderConfig.RecorderItems.Count; i++)
            {
                var item = multiRecorderConfig.RecorderItems[i];
                
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
                
                // Recorder name - clickable
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                if (isSelected) labelStyle.fontStyle = FontStyle.Bold;
                
                if (GUILayout.Button(item.name, labelStyle, GUILayout.ExpandWidth(true)))
                {
                    selectedRecorderIndex = i;
                    GUI.FocusControl(null);
                }
                
                // Delete button
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    multiRecorderConfig.RecorderItems.RemoveAt(i);
                    if (selectedRecorderIndex >= i) selectedRecorderIndex--;
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawRecorderDetailColumn()
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("③各Recorder の設定", headerStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (selectedRecorderIndex < 0 || selectedRecorderIndex >= multiRecorderConfig.RecorderItems.Count)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Select a recorder from the list to edit its settings.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                return;
            }
            
            var item = multiRecorderConfig.RecorderItems[selectedRecorderIndex];
            
            detailPanelScrollPos = EditorGUILayout.BeginScrollView(detailPanelScrollPos);
            
            // Recorder Type header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string recorderTypeName = GetRecorderTypeName(item.recorderType);
            EditorGUILayout.LabelField("Recorder Type", recorderTypeName, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Input section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("▼ Input", EditorStyles.boldLabel);
            DrawRecorderInputSettings(item);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Output Format section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("▼ Output Format", EditorStyles.boldLabel);
            DrawRecorderFormatSettings(item);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Output File section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("▼ Output File", EditorStyles.boldLabel);
            item.fileName = EditorGUILayout.TextField("File Name", item.fileName);
            
            // Wildcards button
            if (GUILayout.Button("+ Wildcards ▼", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                ShowWildcardsMenu(item);
            }
            
            EditorGUILayout.LabelField("Path", multiRecorderConfig.globalOutputPath);
            item.takeNumber = EditorGUILayout.IntField("Take Number", item.takeNumber);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
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
        
        private void DrawRecorderInputSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            switch (item.recorderType)
            {
                case RecorderSettingsType.Animation:
                case RecorderSettingsType.FBX:
                case RecorderSettingsType.Alembic:
                    GameObject newTarget = (GameObject)EditorGUILayout.ObjectField("GameObject", 
                        item.animationConfig?.targetGameObject ?? item.fbxConfig?.targetGameObject ?? item.alembicConfig?.targetGameObject, 
                        typeof(GameObject), true);
                    
                    if (item.animationConfig != null) item.animationConfig.targetGameObject = newTarget;
                    if (item.fbxConfig != null) item.fbxConfig.targetGameObject = newTarget;
                    if (item.alembicConfig != null) item.alembicConfig.targetGameObject = newTarget;
                    
                    // Type-specific options
                    if (item.recorderType == RecorderSettingsType.Animation)
                    {
                        item.animationConfig.includeChildren = EditorGUILayout.Toggle("Record Hierarchy", item.animationConfig.includeChildren);
                        item.animationConfig.clampedTangents = EditorGUILayout.Toggle("Clamped Tangents", item.animationConfig.clampedTangents);
                        item.animationConfig.compressionLevel = (AnimationCompressionLevel)EditorGUILayout.EnumPopup("Anim. Compression", item.animationConfig.compressionLevel);
                    }
                    else if (item.recorderType == RecorderSettingsType.FBX)
                    {
                        item.fbxConfig.recordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", item.fbxConfig.recordHierarchy);
                        item.fbxConfig.clampedTangents = EditorGUILayout.Toggle("Clamped Tangents", item.fbxConfig.clampedTangents);
                    }
                    else if (item.recorderType == RecorderSettingsType.Alembic)
                    {
                        item.alembicConfig.includeChildren = EditorGUILayout.Toggle("Include Children", item.alembicConfig.includeChildren);
                        item.alembicConfig.scaleFactor = EditorGUILayout.FloatField("Scale Factor", item.alembicConfig.scaleFactor);
                    }
                    
                    // Warning if no GameObject assigned
                    if (newTarget == null)
                    {
                        EditorGUILayout.HelpBox("⚠️ No assigned game object to record", MessageType.Warning);
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                case RecorderSettingsType.Image:
                case RecorderSettingsType.AOV:
                    // Camera/rendering settings
                    EditorGUILayout.LabelField("Source", "Game View");
                    if (!multiRecorderConfig.useGlobalResolution)
                    {
                        item.width = EditorGUILayout.IntField("Width", item.width);
                        item.height = EditorGUILayout.IntField("Height", item.height);
                    }
                    break;
            }
        }
        
        private void DrawRecorderFormatSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            switch (item.recorderType)
            {
                case RecorderSettingsType.Animation:
                    EditorGUILayout.LabelField("Format", "Animation Clip");
                    break;
                    
                case RecorderSettingsType.FBX:
                    EditorGUILayout.LabelField("Format", "FBX");
                    break;
                    
                case RecorderSettingsType.Alembic:
                    EditorGUILayout.LabelField("Format", "Alembic");
                    break;
                    
                case RecorderSettingsType.Movie:
                    item.movieConfig.outputFormat = (MovieRecorderSettings.VideoRecorderOutputFormat)
                        EditorGUILayout.EnumPopup("Format", item.movieConfig.outputFormat);
                    item.movieConfig.videoBitrateMode = (VideoBitrateMode)
                        EditorGUILayout.EnumPopup("Quality", item.movieConfig.videoBitrateMode);
                    break;
                    
                case RecorderSettingsType.Image:
                    item.imageFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)
                        EditorGUILayout.EnumPopup("Format", item.imageFormat);
                    if (item.imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
                    {
                        item.jpegQuality = EditorGUILayout.IntSlider("Quality", item.jpegQuality, 1, 100);
                    }
                    break;
                    
                case RecorderSettingsType.AOV:
                    item.aovConfig.outputFormat = (AOVOutputFormat)
                        EditorGUILayout.EnumPopup("Format", item.aovConfig.outputFormat);
                    break;
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
        
        private void DrawRecorderDetailPanel()
        {
            EditorGUILayout.LabelField("Recorder Details", EditorStyles.boldLabel);
            
            if (selectedRecorderIndex < 0 || selectedRecorderIndex >= multiRecorderConfig.RecorderItems.Count)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Select a recorder from the list to edit its settings.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                return;
            }
            
            var item = multiRecorderConfig.RecorderItems[selectedRecorderIndex];
            
            detailPanelScrollPos = EditorGUILayout.BeginScrollView(detailPanelScrollPos);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Basic settings
            EditorGUILayout.LabelField($"Editing: {item.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            item.enabled = EditorGUILayout.Toggle("Enabled", item.enabled);
            item.name = EditorGUILayout.TextField("Name:", item.name);
            item.fileName = EditorGUILayout.TextField("File Name:", item.fileName);
            item.takeNumber = EditorGUILayout.IntField("Take Number:", item.takeNumber);
            
            EditorGUILayout.Space(10);
            
            // Resolution settings
            if (!multiRecorderConfig.useGlobalResolution)
            {
                EditorGUILayout.LabelField("Resolution", EditorStyles.miniBoldLabel);
                item.width = EditorGUILayout.IntField("Width:", item.width);
                item.height = EditorGUILayout.IntField("Height:", item.height);
                EditorGUILayout.Space(10);
            }
            
            // Type-specific detailed settings
            EditorGUILayout.LabelField("Recorder Settings", EditorStyles.miniBoldLabel);
            
            switch (item.recorderType)
            {
                case RecorderSettingsType.Image:
                    DrawImageRecorderDetailSettings(item);
                    break;
                    
                case RecorderSettingsType.Movie:
                    DrawMovieRecorderDetailSettings(item);
                    break;
                    
                case RecorderSettingsType.AOV:
                    DrawAOVRecorderDetailSettings(item);
                    break;
                    
                case RecorderSettingsType.FBX:
                    DrawFBXRecorderDetailSettings(item);
                    break;
                    
                case RecorderSettingsType.Animation:
                    DrawAnimationRecorderDetailSettings(item);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    DrawAlembicRecorderDetailSettings(item);
                    break;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        
        private void AddRecorder(RecorderSettingsType type)
        {
            var item = MultiRecorderConfig.CreateDefaultRecorder(type);
            
            // Apply global settings
            if (multiRecorderConfig.useGlobalResolution)
            {
                item.width = multiRecorderConfig.globalWidth;
                item.height = multiRecorderConfig.globalHeight;
            }
            item.frameRate = frameRate;
            
            multiRecorderConfig.AddRecorder(item);
            
            // Auto-select the newly added recorder
            selectedRecorderIndex = multiRecorderConfig.RecorderItems.Count - 1;
        }
        
        private void DrawImageRecorderDetailSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            item.imageFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)
                EditorGUILayout.EnumPopup("Output Format:", item.imageFormat);
            
            item.captureAlpha = EditorGUILayout.Toggle("Capture Alpha", item.captureAlpha);
            
            if (item.imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                item.jpegQuality = EditorGUILayout.IntSlider("JPEG Quality:", item.jpegQuality, 1, 100);
            }
            else if (item.imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                item.exrCompression = (CompressionUtility.EXRCompressionType)
                    EditorGUILayout.EnumPopup("EXR Compression:", item.exrCompression);
            }
        }
        
        private void DrawMovieRecorderDetailSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            EditorGUILayout.LabelField("Video", EditorStyles.miniBoldLabel);
            item.movieConfig.outputFormat = (MovieRecorderSettings.VideoRecorderOutputFormat)
                EditorGUILayout.EnumPopup("Output Format:", item.movieConfig.outputFormat);
                
            item.movieConfig.videoBitrateMode = (VideoBitrateMode)
                EditorGUILayout.EnumPopup("Quality:", item.movieConfig.videoBitrateMode);
                
            if (item.movieConfig.videoBitrateMode == VideoBitrateMode.Custom)
            {
                item.movieConfig.customBitrate = EditorGUILayout.IntField("Bitrate (Mbps):", item.movieConfig.customBitrate);
            }
            
            item.movieConfig.captureAlpha = EditorGUILayout.Toggle("Capture Alpha", item.movieConfig.captureAlpha);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Audio", EditorStyles.miniBoldLabel);
            item.movieConfig.captureAudio = EditorGUILayout.Toggle("Capture Audio", item.movieConfig.captureAudio);
            if (item.movieConfig.captureAudio)
            {
                item.movieConfig.audioBitrate = (AudioBitRateMode)
                    EditorGUILayout.EnumPopup("Audio Quality:", item.movieConfig.audioBitrate);
            }
        }
        
        private void DrawAOVRecorderDetailSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            EditorGUILayout.LabelField("AOV Selection", EditorStyles.miniBoldLabel);
            
            item.aovConfig.includeBeauty = EditorGUILayout.Toggle("Beauty", item.aovConfig.includeBeauty);
            item.aovConfig.includeDepth = EditorGUILayout.Toggle("Depth", item.aovConfig.includeDepth);
            item.aovConfig.includeNormal = EditorGUILayout.Toggle("Normal", item.aovConfig.includeNormal);
            item.aovConfig.includeMotionVectors = EditorGUILayout.Toggle("Motion Vectors", item.aovConfig.includeMotionVectors);
            
            EditorGUILayout.Space(5);
            item.aovConfig.outputFormat = (AOVOutputFormat)
                EditorGUILayout.EnumPopup("Output Format:", item.aovConfig.outputFormat);
                
            item.aovConfig.useMultiPartEXR = EditorGUILayout.Toggle("Multi-Part EXR", item.aovConfig.useMultiPartEXR);
            
            item.aovConfig.compression = (AOVCompression)
                EditorGUILayout.EnumPopup("Compression:", item.aovConfig.compression);
        }
        
        private void DrawAnimationRecorderDetailSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            item.animationConfig.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Target GameObject", item.animationConfig.targetGameObject, typeof(GameObject), true);
                
            item.animationConfig.recordingScope = (AnimationRecordingScope)
                EditorGUILayout.EnumPopup("Recording Scope:", item.animationConfig.recordingScope);
                
            item.animationConfig.includeChildren = EditorGUILayout.Toggle("Include Children", item.animationConfig.includeChildren);
            item.animationConfig.clampedTangents = EditorGUILayout.Toggle("Clamped Tangents", item.animationConfig.clampedTangents);
            item.animationConfig.recordBlendShapes = EditorGUILayout.Toggle("Record Blend Shapes", item.animationConfig.recordBlendShapes);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Compression", EditorStyles.miniBoldLabel);
            item.animationConfig.compressionLevel = (AnimationCompressionLevel)
                EditorGUILayout.EnumPopup("Compression Level:", item.animationConfig.compressionLevel);
                
            if (item.animationConfig.compressionLevel != AnimationCompressionLevel.None)
            {
                item.animationConfig.positionError = EditorGUILayout.FloatField("Position Error:", item.animationConfig.positionError);
                item.animationConfig.rotationError = EditorGUILayout.FloatField("Rotation Error:", item.animationConfig.rotationError);
                item.animationConfig.scaleError = EditorGUILayout.FloatField("Scale Error:", item.animationConfig.scaleError);
            }
        }
        
        private void DrawFBXRecorderDetailSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            item.fbxConfig.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Target GameObject", item.fbxConfig.targetGameObject, typeof(GameObject), true);
                
            item.fbxConfig.recordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", item.fbxConfig.recordHierarchy);
            item.fbxConfig.exportGeometry = EditorGUILayout.Toggle("Export Geometry", item.fbxConfig.exportGeometry);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Animation", EditorStyles.miniBoldLabel);
            item.fbxConfig.clampedTangents = EditorGUILayout.Toggle("Clamped Tangents", item.fbxConfig.clampedTangents);
            item.fbxConfig.animationCompression = (FBXAnimationCompressionLevel)
                EditorGUILayout.EnumPopup("Compression:", item.fbxConfig.animationCompression);
                
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Transfer Animation (Optional)", EditorStyles.miniBoldLabel);
            item.fbxConfig.transferAnimationSource = (Transform)EditorGUILayout.ObjectField(
                "Source", item.fbxConfig.transferAnimationSource, typeof(Transform), true);
            item.fbxConfig.transferAnimationDest = (Transform)EditorGUILayout.ObjectField(
                "Destination", item.fbxConfig.transferAnimationDest, typeof(Transform), true);
        }
        
        private void DrawAlembicRecorderDetailSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            item.alembicConfig.exportScope = (AlembicExportScope)
                EditorGUILayout.EnumPopup("Export Scope:", item.alembicConfig.exportScope);
                
            if (item.alembicConfig.exportScope == AlembicExportScope.TargetGameObject)
            {
                item.alembicConfig.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                    "Target GameObject", item.alembicConfig.targetGameObject, typeof(GameObject), true);
            }
            
            EditorGUILayout.Space(5);
            item.alembicConfig.exportTargets = (AlembicExportTargets)
                EditorGUILayout.EnumFlagsField("Export Targets:", item.alembicConfig.exportTargets);
                
            item.alembicConfig.includeChildren = EditorGUILayout.Toggle("Include Children", item.alembicConfig.includeChildren);
            item.alembicConfig.flattenHierarchy = EditorGUILayout.Toggle("Flatten Hierarchy", item.alembicConfig.flattenHierarchy);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Format", EditorStyles.miniBoldLabel);
            item.alembicConfig.scaleFactor = EditorGUILayout.FloatField("Scale Factor:", item.alembicConfig.scaleFactor);
            item.alembicConfig.handedness = (AlembicHandedness)
                EditorGUILayout.EnumPopup("Handedness:", item.alembicConfig.handedness);
                
            item.alembicConfig.timeSamplingType = (AlembicTimeSamplingType)
                EditorGUILayout.EnumPopup("Time Sampling:", item.alembicConfig.timeSamplingType);
        }
        
