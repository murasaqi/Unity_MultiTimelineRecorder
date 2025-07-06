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
        private static bool isRenderingInProgress = false;
        
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
        public int frameRate = 24;
        public int width = 1920;
        public int height = 1080;
        private string outputFile = "";
        public string fileName = "<Recorder>_<Take>"; // File name with wildcard support
        public string filePath = "Recordings"; // Output path
        public int takeNumber = 1;
        public int preRollFrames = 0; // Pre-roll frames for simulation warm-up
        
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
        
        // UI Editor instances
        private RecorderSettingsEditorBase currentRecorderEditor;
        
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
                isRenderingInProgress = false;
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Reset to Idle state");
            }
            
            ScanTimelines();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Initialize file name with default template if empty
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "<Recorder>_<Take>";
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
            
            // MonoBehaviourベースの新しい実装では、Play Mode遷移時の複雑な処理は不要
            if (EditorApplication.isPlaying && EditorPrefs.GetBool("STR_IsRendering", false))
            {
                // Play Mode内でTimelineRendererが処理中
                currentState = RenderState.WaitingForPlayMode;
                statusMessage = "Rendering in Play Mode...";
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Detected rendering in progress in Play Mode");
            }
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] OnEnable completed - Directors: {availableDirectors.Count}, State: {currentState}");
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
            
            DrawTimelineSelection();
            EditorGUILayout.Space(10);
            
            DrawRenderSettings();
            EditorGUILayout.Space(10);
            
            DrawRenderControls();
            EditorGUILayout.Space(10);
            
            DrawStatusSection();
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
                    var allDirectors = GameObject.FindObjectsOfType<PlayableDirector>();
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
            
            // Recorder type selection
            RecorderSettingsType newRecorderType = (RecorderSettingsType)EditorGUILayout.EnumPopup("Recorder Type:", recorderType);
            
            // Handle recorder type change
            if (newRecorderType != recorderType)
            {
                previousRecorderType = recorderType;
                recorderType = newRecorderType;
                
                // Update default file name when recorder type changes
                // but keep the path unchanged
                fileName = "<Recorder>_<Take>";
                
                // Create appropriate editor instance
                UpdateRecorderEditor();
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
            
            EditorGUILayout.EndVertical();
            
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
        
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool canRender = currentState == RenderState.Idle && availableDirectors.Count > 0 && !EditorApplication.isPlaying && RecorderSettingsFactory.IsRecorderTypeSupported(recorderType);
            
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
            if (currentState == RenderState.Rendering)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), renderProgress, $"{(int)(renderProgress * 100)}%");
                
                if (renderingDirector != null)
                {
                    EditorGUILayout.LabelField($"Time: {renderingDirector.time:F2}/{renderingDirector.duration:F2}");
                }
            }
            
            // Debug controls
            if (currentState != RenderState.Idle)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Force Reset", GUILayout.Width(100)))
                {
                    BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Force reset from state: {currentState}");
                    currentState = RenderState.Idle;
                    statusMessage = "Reset to Idle";
                    
                    if (renderCoroutine != null)
                    {
                        EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                        renderCoroutine = null;
                    }
                    
                    CleanupRendering();
                }
                
                if (GUILayout.Button("Clear EditorPrefs", GUILayout.Width(120)))
                {
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Clearing all EditorPrefs");
                    EditorPrefs.DeleteKey("STR_DirectorName");
                    EditorPrefs.DeleteKey("STR_TempAssetPath");
                    EditorPrefs.DeleteKey("STR_Duration");
                    EditorPrefs.DeleteKey("STR_ExposedName");
                    EditorPrefs.DeleteKey("STR_TakeNumber");
                    EditorPrefs.DeleteKey("STR_OutputFile");
                    EditorPrefs.DeleteKey("STR_RecorderType");
                    EditorPrefs.SetBool("STR_IsRendering", false);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ScanTimelines()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] ScanTimelines called");
            availableDirectors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsOfType<PlayableDirector>();
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
            if (renderCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                renderCoroutine = null;
            }
            
            if (renderingDirector != null)
            {
                renderingDirector.Stop();
            }
            
            // Exit Play Mode if active
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            
            CleanupRendering();
            
            currentState = RenderState.Idle;
            statusMessage = "Rendering stopped by user";
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
            
            if (selectedDirectorIndex < 0 || selectedDirectorIndex >= availableDirectors.Count)
            {
                currentState = RenderState.Error;
                statusMessage = "Invalid timeline selection";
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid selection index: {selectedDirectorIndex} (count: {availableDirectors.Count})");
                yield break;
            }
            
            var selectedDirector = availableDirectors[selectedDirectorIndex];
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Selected director: {selectedDirector?.name ?? "null"}");
            
            if (selectedDirector == null || selectedDirector.gameObject == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected director is null or destroyed";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Selected director is null or destroyed");
                yield break;
            }
            
            var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Selected director: {selectedDirector?.gameObject.name}");
            
            if (originalTimeline == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected asset is not a Timeline";
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Selected asset is not a Timeline");
                yield break;
            }
            
            // Store original Play On Awake setting and disable it
            bool originalPlayOnAwake = selectedDirector.playOnAwake;
            selectedDirector.playOnAwake = false;
            
            // Store director info for later use in Play Mode
            string directorName = selectedDirector.gameObject.name;
            float timelineDuration = (float)originalTimeline.duration;
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Timeline duration: {timelineDuration}, PlayOnAwake was: {originalPlayOnAwake}");
            
            // Create render timeline BEFORE entering Play Mode
            currentState = RenderState.PreparingAssets;
            statusMessage = "Creating render timeline...";
            
            try
            {
                renderTimeline = CreateRenderTimeline(selectedDirector, originalTimeline);
                if (renderTimeline == null)
                {
                    currentState = RenderState.Error;
                    statusMessage = "Failed to create render timeline";
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create render timeline");
                    selectedDirector.playOnAwake = originalPlayOnAwake;
                    yield break;
                }
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Successfully created render timeline at: {tempAssetPath} ===");
            }
            catch (System.Exception e)
            {
                currentState = RenderState.Error;
                statusMessage = $"Error creating timeline: {e.Message}";
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Error creating timeline: {e}");
                selectedDirector.playOnAwake = originalPlayOnAwake;
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
                selectedDirector.playOnAwake = originalPlayOnAwake;
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
                // Store necessary data for Play Mode
                EditorPrefs.SetString("STR_DirectorName", directorName);
                EditorPrefs.SetString("STR_TempAssetPath", tempAssetPath);
                EditorPrefs.SetFloat("STR_Duration", timelineDuration);
                EditorPrefs.SetBool("STR_IsRendering", true);
                EditorPrefs.SetInt("STR_TakeNumber", takeNumber);
                EditorPrefs.SetString("STR_OutputFile", fileName);
                EditorPrefs.SetInt("STR_RecorderType", (int)recorderType);
                EditorPrefs.SetInt("STR_FrameRate", frameRate);
                EditorPrefs.SetInt("STR_PreRollFrames", preRollFrames);
                
                // MonoBehaviourベースのレンダラーを使用するフラグ
                EditorPrefs.SetBool("STR_UseMonoBehaviour", true);
                
                // Set static flag
                isRenderingInProgress = true;
                
                EditorApplication.isPlaying = true;
                // Play Modeに入ると、TimelineRendererが自動的に処理を引き継ぐ
            }
        }
        
        private TimelineAsset CreateRenderTimeline(PlayableDirector originalDirector, TimelineAsset originalTimeline)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateRenderTimeline started - Director: {originalDirector.gameObject.name}, Timeline: {originalTimeline.name} ===");
            
            try
            {
                // Create timeline
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            if (timeline == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create TimelineAsset instance");
                return null;
            }
            timeline.name = $"{originalDirector.gameObject.name}_RenderTimeline";
            timeline.editorSettings.frameRate = frameRate;
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Created TimelineAsset: {timeline.name}, frameRate: {frameRate} ===");
            
            // Save as temporary asset
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (!AssetDatabase.IsValidFolder(tempDir))
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating temp directory...");
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                {
                    AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Created BatchRenderingTool folder");
                }
                AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Created Temp folder");
            }
            
            tempAssetPath = $"{tempDir}/{timeline.name}_{System.DateTime.Now.Ticks}.playable";
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating asset at: {tempAssetPath}");
            try
            {
                AssetDatabase.CreateAsset(timeline, tempAssetPath);
                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Successfully created asset at: {tempAssetPath}");
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create asset: {e.Message}");
                return null;
            }
            
            // Create control track
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating ControlTrack...");
            var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
            if (controlTrack == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create ControlTrack");
                return null;
            }
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] ControlTrack created successfully");
            
            // Calculate pre-roll time
            float preRollTime = preRollFrames > 0 ? preRollFrames / (float)frameRate : 0f;
            string exposedName = UnityEditor.GUID.Generate().ToString();
            
            if (preRollFrames > 0)
            {
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Pre-roll enabled: {preRollFrames} frames ({preRollTime:F2} seconds) ===");
            }
            
            if (preRollFrames > 0)
            {
                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating pre-roll clip for {preRollFrames} frames ({preRollTime:F2} seconds)");
                
                // Create pre-roll clip (holds at frame 0)
                var preRollClip = controlTrack.CreateClip<ControlPlayableAsset>();
                if (preRollClip == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create pre-roll ControlClip");
                    return null;
                }
                
                preRollClip.displayName = $"{originalDirector.gameObject.name} (Pre-roll)";
                preRollClip.start = 0;
                preRollClip.duration = preRollTime;
                
                var preRollAsset = preRollClip.asset as ControlPlayableAsset;
                preRollAsset.sourceGameObject.exposedName = exposedName;
                preRollAsset.sourceGameObject.defaultValue = originalDirector.gameObject;
                preRollAsset.updateDirector = true;
                preRollAsset.updateParticle = true;
                preRollAsset.updateITimeControl = true;
                preRollAsset.searchHierarchy = false;
                preRollAsset.active = true;
                preRollAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Active;
                
                // IMPORTANT: Set the clip to hold at frame 0
                // The pre-roll clip will play the director at the beginning (0-0 range)
                preRollClip.clipIn = 0;
                preRollClip.timeScale = 0.0001; // Virtually freeze time
                
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Pre-roll ControlClip created successfully");
            }
            
            // Create main playback clip
            var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
            if (controlClip == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create main ControlClip");
                return null;
            }
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Main ControlClip created successfully");
            controlClip.displayName = originalDirector.gameObject.name;
            controlClip.start = preRollTime;
            controlClip.duration = originalTimeline.duration;
            
            var controlAsset = controlClip.asset as ControlPlayableAsset;
            
            // CRITICAL: Set up the exposed name for runtime binding
            controlAsset.sourceGameObject.exposedName = exposedName;
            
            // IMPORTANT: We need to set the source game object reference for the UI to work properly
            // This will be serialized with the asset
            controlAsset.sourceGameObject.defaultValue = originalDirector.gameObject;
            
            // Store the exposed name for later use in Play Mode
            EditorPrefs.SetString("STR_ExposedName", exposedName);
            
            // Configure control asset properties
            controlAsset.updateDirector = true;
            controlAsset.updateParticle = true;
            controlAsset.updateITimeControl = true;
            controlAsset.searchHierarchy = false;
            controlAsset.active = true;
            controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
            
            // Important: We'll set the bindings on the PlayableDirector after creating it
            
            // Create recorder settings based on type
            var context = new WildcardContext(takeNumber, width, height);
            context.TimelineName = originalDirector.gameObject.name;
            
            // Set GameObject name for Alembic export
            if (recorderType == RecorderSettingsType.Alembic && alembicExportScope == AlembicExportScope.TargetGameObject && alembicTargetGameObject != null)
            {
                context.GameObjectName = alembicTargetGameObject.name;
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Setting GameObject wildcard to: {alembicTargetGameObject.name} ===");
            }
            
            var processedFileName = WildcardProcessor.ProcessWildcards(fileName, context);
            var processedFilePath = filePath; // Path doesn't need wildcard processing
            List<RecorderSettings> recorderSettingsList = new List<RecorderSettings>();
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating recorder settings for type: {recorderType}");
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating ImageRecorderSettings...");
                    var imageSettings = CreateImageRecorderSettings(processedFilePath, processedFileName);
                    if (imageSettings != null)
                    {
                        recorderSettingsList.Add(imageSettings);
                        BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] ImageRecorderSettings created: {imageSettings.GetType().Name}");
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] CreateImageRecorderSettings returned null");
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    var movieSettings = CreateMovieRecorderSettings(processedFilePath, processedFileName);
                    if (movieSettings != null) recorderSettingsList.Add(movieSettings);
                    break;
                    
                case RecorderSettingsType.AOV:
                    var aovSettingsList = CreateAOVRecorderSettings(processedFilePath, processedFileName);
                    if (aovSettingsList != null) recorderSettingsList.AddRange(aovSettingsList);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Creating Alembic settings with file: {processedFileName} ===");
                    var alembicSettings = CreateAlembicRecorderSettings(processedFilePath, processedFileName);
                    if (alembicSettings != null) recorderSettingsList.Add(alembicSettings);
                    break;
                    
                case RecorderSettingsType.Animation:
                    var animationSettings = CreateAnimationRecorderSettings(processedFilePath, processedFileName);
                    if (animationSettings != null) recorderSettingsList.Add(animationSettings);
                    break;
                    
                case RecorderSettingsType.FBX:
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Creating FBX settings with file: {processedFileName} ===");
                    var fbxSettings = CreateFBXRecorderSettings(processedFilePath, processedFileName);
                    if (fbxSettings != null) recorderSettingsList.Add(fbxSettings);
                    break;
                    
                default:
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Unsupported recorder type: {recorderType}");
                    return null;
            }
            
            if (recorderSettingsList.Count == 0)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create recorder settings for type: {recorderType}");
                return null;
            }
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Created {recorderSettingsList.Count} recorder settings");
            
            // For AOV, we might have multiple settings, but for now use the first one for the main recorder track
            RecorderSettings recorderSettings = recorderSettingsList[0];
            
            // Save all recorder settings as sub-assets
            foreach (var settings in recorderSettingsList)
            {
                AssetDatabase.AddObjectToAsset(settings, timeline);
            }
            
            // Create recorder track and clip
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === Creating RecorderTrack... ===");
            var recorderTrack = timeline.CreateTrack<UnityEditor.Recorder.Timeline.RecorderTrack>(null, "Recorder Track");
            if (recorderTrack == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create RecorderTrack");
                return null;
            }
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === RecorderTrack created successfully ===");
            var recorderClip = recorderTrack.CreateClip<UnityEditor.Recorder.Timeline.RecorderClip>();
            if (recorderClip == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create RecorderClip");
                return null;
            }
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === RecorderClip created successfully ===");
            
            recorderClip.displayName = $"Record {originalDirector.gameObject.name}";
            recorderClip.start = preRollTime;
            recorderClip.duration = originalTimeline.duration;
            
            var recorderAsset = recorderClip.asset as UnityEditor.Recorder.Timeline.RecorderClip;
            if (recorderAsset == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to get RecorderClip asset");
                return null;
            }
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] RecorderClip asset type: {recorderAsset.GetType().FullName}");
            
            recorderAsset.settings = recorderSettings;
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Assigned RecorderSettings of type: {recorderSettings.GetType().FullName}");
            
            // For Alembic Recorder, ensure the UI reflects the correct settings
            if (recorderType == RecorderSettingsType.Alembic)
            {
                ApplyAlembicSettingsToRecorderClip(recorderAsset, recorderSettings);
            }
            
            // Use RecorderClipUtility to ensure proper initialization
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            // For Alembic, ensure the timeline asset has the correct settings before saving
            if (recorderType == RecorderSettingsType.Alembic && recorderAsset.settings != null)
            {
                // Force refresh the RecorderClip's internal state
                var settingsField = recorderAsset.GetType().GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
                if (settingsField != null)
                {
                    settingsField.SetValue(recorderAsset, recorderSettings);
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Force set m_Settings field on RecorderClip");
                }
                
                // Log the actual settings to verify
                var actualSettings = recorderAsset.settings;
                if (actualSettings != null)
                {
                    var actualType = actualSettings.GetType();
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] RecorderClip settings type: {actualType.FullName}");
                    
                    // Check if TargetBranch is set correctly
                    var targetBranchProp = actualType.GetProperty("TargetBranch");
                    if (targetBranchProp != null && targetBranchProp.CanRead)
                    {
                        var targetValue = targetBranchProp.GetValue(actualSettings);
                        BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] TargetBranch value in RecorderClip: {targetValue}");
                    }
                    
                    // Check Scope value
                    var scopeProp = actualType.GetProperty("Scope");
                    if (scopeProp != null && scopeProp.CanRead)
                    {
                        var scopeValue = scopeProp.GetValue(actualSettings);
                        BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Scope value in RecorderClip: {scopeValue}");
                    }
                }
                
                // Ensure the asset is marked as dirty
                EditorUtility.SetDirty(recorderAsset.settings);
            }
            
            // Save everything including ControlTrack settings
            EditorUtility.SetDirty(controlAsset);
            EditorUtility.SetDirty(recorderAsset);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Log for debugging
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Created timeline with ControlTrack, exposed name: {exposedName}");
            
            return timeline;
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Exception in CreateRenderTimeline: {e.Message}");
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Stack trace: {e.StackTrace}");
                return null;
            }
        }
        
        private void CleanupRendering()
        {
            if (renderingDirector != null)
            {
                renderingDirector.Stop();
            }
            
            if (renderingGameObject != null)
            {
                DestroyImmediate(renderingGameObject);
                renderingGameObject = null;
            }
            
            if (!string.IsNullOrEmpty(tempAssetPath) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tempAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(tempAssetPath);
            }
            
            renderTimeline = null;
            renderingDirector = null;
            tempAssetPath = null;
            
            AssetDatabase.Refresh();
        }
        
        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            
            // Additional characters to replace
            string additionalInvalidChars = "()[]{}";
            
            // Replace all invalid characters
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            foreach (char c in additionalInvalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            // Replace multiple underscores with single underscore
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }
            
            // Trim underscores from start and end
            sanitized = sanitized.Trim('_');
            
            // Ensure the result is not empty
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "Timeline";
            }
            
            return sanitized;
        }
        
        private string GetFileExtension()
        {
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    switch (imageOutputFormat)
                    {
                        case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                            return "png";
                        case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                            return "jpg";
                        case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                            return "exr";
                        default:
                            return "png";
                    }
                    
                case RecorderSettingsType.Movie:
                    return movieOutputFormat.ToString().ToLower();
                    
                case RecorderSettingsType.AOV:
                    return aovOutputFormat == AOVOutputFormat.PNG16 ? "png" : "exr";
                    
                case RecorderSettingsType.Alembic:
                    return "abc";
                    
                case RecorderSettingsType.Animation:
                    return "anim";
                    
                case RecorderSettingsType.FBX:
                    return "fbx";
                    
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// Apply Alembic settings to RecorderClip to ensure UI reflects correct values
        /// </summary>
        private void ApplyAlembicSettingsToRecorderClip(UnityEditor.Recorder.Timeline.RecorderClip recorderClip, RecorderSettings settings)
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === Applying Alembic settings to RecorderClip ===");
            
            try
            {
                // Use reflection to access internal properties of RecorderClip
                var clipType = recorderClip.GetType();
                var settingsType = settings.GetType();
                
                // The RecorderClip has internal serialized fields that need to be updated
                // These fields are used by the Timeline UI to display the settings
                
                // Try to find and update the internal GameObject reference field
                if (alembicExportScope == AlembicExportScope.TargetGameObject && alembicTargetGameObject != null)
                {
                    // Look for fields that might store the target GameObject reference
                    var fields = clipType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] RecorderClip field: {field.Name} ({field.FieldType})");
                        
                        // Try to find fields related to GameObject or target
                        if (field.FieldType == typeof(GameObject) || 
                            field.Name.ToLower().Contains("target") || 
                            field.Name.ToLower().Contains("gameobject") ||
                            field.Name.ToLower().Contains("branch"))
                        {
                            field.SetValue(recorderClip, alembicTargetGameObject);
                            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Set RecorderClip field {field.Name} to {alembicTargetGameObject.name}");
                        }
                    }
                    
                    // Also check properties
                    var properties = clipType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    foreach (var prop in properties)
                    {
                        if (prop.CanWrite && (prop.PropertyType == typeof(GameObject) || 
                            prop.Name.ToLower().Contains("target") || 
                            prop.Name.ToLower().Contains("gameobject") ||
                            prop.Name.ToLower().Contains("branch")))
                        {
                            try
                            {
                                prop.SetValue(recorderClip, alembicTargetGameObject);
                                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Set RecorderClip property {prop.Name} to {alembicTargetGameObject.name}");
                            }
                            catch (Exception e)
                            {
                                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Could not set property {prop.Name}: {e.Message}");
                            }
                        }
                    }
                }
                
                // Force the RecorderClip to update its internal state
                EditorUtility.SetDirty(recorderClip);
                
                // Try to trigger any internal update methods
                var updateMethod = clipType.GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    updateMethod.Invoke(recorderClip, null);
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Called OnValidate on RecorderClip");
                }
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogWarning($"[SingleTimelineRenderer] Failed to apply Alembic settings to RecorderClip: {e.Message}");
            }
        }
        
        private void OnDestroy()
        {
            StopRendering();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            if (instance == this)
            {
                instance = null;
                isRenderingInProgress = false;
            }
        }
        
        private void OnEditorUpdate()
        {
            // OnPlayModeStateChanged handles Play Mode transitions now
            // This method can be used for other update tasks if needed
        }
        
        // ContinueRenderingInPlayMode メソッドは削除 - MonoBehaviourベースの実装に置き換え
        
        private void Update()
        {
            if (currentState == RenderState.Rendering && renderingDirector != null)
            {
                Repaint();
            }
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Play Mode state changed: {state}");
            
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Entered Play Mode");
                
                // レンダリングが進行中の場合、TimelineRendererを作成
                bool isRendering = EditorPrefs.GetBool("STR_IsRendering", false);
                bool useMonoBehaviour = EditorPrefs.GetBool("STR_UseMonoBehaviour", false);
                
                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] STR_IsRendering: {isRendering}, STR_UseMonoBehaviour: {useMonoBehaviour}");
                
                if (isRendering)
                {
                    BatchRenderingToolLogger.Log("[SingleTimelineRenderer] Creating PlayModeTimelineRenderer GameObject");
                    currentState = RenderState.Rendering;
                    statusMessage = "Rendering in Play Mode...";
                    
                    // レンダリングデータを準備
                    string directorName = EditorPrefs.GetString("STR_DirectorName", "");
                    string tempAssetPath = EditorPrefs.GetString("STR_TempAssetPath", "");
                    float duration = EditorPrefs.GetFloat("STR_Duration", 0f);
                    string exposedName = EditorPrefs.GetString("STR_ExposedName", "");
                    int frameRate = EditorPrefs.GetInt("STR_FrameRate", 24);
                    int preRollFrames = EditorPrefs.GetInt("STR_PreRollFrames", 0);
                    
                    // Render Timelineをロード
                    var renderTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
                    if (renderTimeline == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to load timeline from: {tempAssetPath}");
                        currentState = RenderState.Error;
                        statusMessage = "Failed to load render timeline";
                        return;
                    }
                    
                    // レンダリングデータを持つGameObjectを作成
                    var dataGO = new GameObject("[RenderingData]");
                    var renderingData = dataGO.AddComponent<RenderingData>();
                    renderingData.directorName = directorName;
                    renderingData.renderTimeline = renderTimeline;
                    renderingData.duration = duration;
                    renderingData.exposedName = exposedName;
                    renderingData.frameRate = frameRate;
                    renderingData.preRollFrames = preRollFrames;
                    
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
                
                // レンダリング完了を確認
                if (EditorPrefs.GetBool("STR_RenderingComplete", false))
                {
                    string completionStatus = EditorPrefs.GetString("STR_RenderingStatus", "Unknown");
                    
                    if (completionStatus.Contains("complete"))
                    {
                        currentState = RenderState.Complete;
                        statusMessage = completionStatus;
                        renderProgress = 1f;
                    }
                    else
                    {
                        currentState = RenderState.Error;
                        statusMessage = completionStatus;
                    }
                    
                    EditorPrefs.DeleteKey("STR_RenderingComplete");
                    EditorPrefs.DeleteKey("STR_RenderingStatus");
                }
                else
                {
                    currentState = RenderState.Idle;
                    statusMessage = "Play Mode exited";
                }
                
                // クリーンアップ
                if (renderCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                    renderCoroutine = null;
                }
                
                CleanupRendering();
                isRenderingInProgress = false;
                EditorPrefs.SetBool("STR_IsRendering", false);
                EditorPrefs.SetBool("STR_UseMonoBehaviour", false);
            }
        }
        
        private void MonitorRenderingProgress()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Starting rendering progress monitoring");
            
            // EditorWindowではコルーチンは使用できないため、
            // EditorApplication.updateを使用して進行状況を監視
            EditorApplication.update += OnRenderingProgressUpdate;
        }
        
        private void OnRenderingProgressUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Rendering progress monitoring ended");
                EditorApplication.update -= OnRenderingProgressUpdate;
            }
        }
        
        public void UpdateRenderProgress(float progress, string message)
        {
            renderProgress = progress;
            statusMessage = message;
            Repaint();
        }
        
        // Public properties for testing
        public string OutputFile => fileName;
        public int OutputWidth => width;
        public int OutputHeight => height;
        public int FrameRate => frameRate;
        public RecorderSettingsHelper.ImageFormat ImageFormat
        {
            get
            {
                switch (imageOutputFormat)
                {
                    case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                        return RecorderSettingsHelper.ImageFormat.PNG;
                    case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                        return RecorderSettingsHelper.ImageFormat.JPG;
                    case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                        return RecorderSettingsHelper.ImageFormat.EXR;
                    default:
                        return RecorderSettingsHelper.ImageFormat.PNG;
                }
            }
        }
        
        // Public methods for testing
        public List<PlayableDirector> GetAllPlayableDirectors()
        {
            return new List<PlayableDirector>(availableDirectors);
        }
        
        public void SetSelectedDirector(PlayableDirector director)
        {
            int index = availableDirectors.IndexOf(director);
            if (index >= 0)
            {
                selectedDirectorIndex = index;
            }
            else
            {
                // Add director if not in list
                availableDirectors.Add(director);
                selectedDirectorIndex = availableDirectors.Count - 1;
            }
        }
        
        public PlayableDirector GetSelectedDirector()
        {
            if (selectedDirectorIndex >= 0 && selectedDirectorIndex < availableDirectors.Count)
            {
                return availableDirectors[selectedDirectorIndex];
            }
            return null;
        }
        
        public bool ValidateSettings(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (availableDirectors.Count == 0 || selectedDirectorIndex < 0 || selectedDirectorIndex >= availableDirectors.Count)
            {
                errorMessage = "No Timeline selected";
                return false;
            }
            
            var director = availableDirectors[selectedDirectorIndex];
            if (director == null || director.gameObject == null || director.playableAsset == null || !(director.playableAsset is TimelineAsset))
            {
                errorMessage = "Selected director does not have a valid Timeline";
                return false;
            }
            
            if (frameRate <= 0)
            {
                errorMessage = "Invalid frame rate";
                return false;
            }
            
            // Use recorder editor for validation
            if (currentRecorderEditor == null)
            {
                UpdateRecorderEditor();
            }
            
            if (currentRecorderEditor != null)
            {
                return currentRecorderEditor.ValidateSettings(out errorMessage);
            }
            
            // Fallback validation
            if (width <= 0 || height <= 0)
            {
                errorMessage = "Invalid output resolution";
                return false;
            }
            
            if (string.IsNullOrEmpty(fileName))
            {
                errorMessage = "Output file template is empty";
                return false;
            }
            
            // Validate file template
            string templateError;
            if (!WildcardProcessor.ValidateTemplate(fileName, out templateError))
            {
                errorMessage = templateError;
                return false;
            }
            
            return true;
        }
        
        private RecorderSettings CreateImageRecorderSettings(string outputPath, string outputFileName)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateImageRecorderSettings called with path: {outputPath}, filename: {outputFileName} ===");
            var settings = RecorderClipUtility.CreateProperImageRecorderSettings("ImageRecorder");
            if (settings == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] RecorderClipUtility.CreateProperImageRecorderSettings returned null");
                return null;
            }
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Created settings of type: {settings.GetType().FullName} ===");
            settings.Enabled = true;
            settings.OutputFormat = imageOutputFormat;
            settings.CaptureAlpha = imageCaptureAlpha;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
            
            // Configure output path
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Configuring output path: {outputPath}, filename: {outputFileName}");
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Image);
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Output path configured successfully");
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Setting image input settings: {width}x{height}");
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
            };
            
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] ImageRecorderSettings created successfully");
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettings(string outputPath, string outputFileName)
        {
            MovieRecorderSettings settings = null;
            
            if (useMoviePreset && moviePreset != MovieRecorderPreset.Custom)
            {
                // Create with preset
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", moviePreset);
            }
            else
            {
                // Create with custom settings
                var config = new MovieRecorderSettingsConfig
                {
                    outputFormat = movieOutputFormat,
                    videoBitrateMode = movieQuality,
                    captureAudio = movieCaptureAudio,
                    captureAlpha = movieCaptureAlpha,
                    width = width,
                    height = height,
                    frameRate = frameRate,
                    capFrameRate = true
                };
                
                string errorMessage;
                if (!config.Validate(out errorMessage))
                {
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid movie configuration: {errorMessage}");
                    return null;
                }
                
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", config);
            }
            
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            
            // Configure output path
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Movie);
            
            return settings;
        }
        
        private List<RecorderSettings> CreateAOVRecorderSettings(string outputPath, string outputFileName)
        {
            AOVRecorderSettingsConfig config = null;
            
            if (useAOVPreset && aovPreset != AOVPreset.Custom)
            {
                // Use preset configuration
                switch (aovPreset)
                {
                    case AOVPreset.Compositing:
                        config = AOVRecorderSettingsConfig.Presets.GetCompositing();
                        break;
                    case AOVPreset.GeometryOnly:
                        config = AOVRecorderSettingsConfig.Presets.GetGeometryOnly();
                        break;
                    case AOVPreset.LightingOnly:
                        config = AOVRecorderSettingsConfig.Presets.GetLightingOnly();
                        break;
                    case AOVPreset.MaterialProperties:
                        config = AOVRecorderSettingsConfig.Presets.GetMaterialProperties();
                        break;
                }
            }
            else
            {
                // Create custom configuration
                config = new AOVRecorderSettingsConfig
                {
                    selectedAOVs = selectedAOVTypes,
                    outputFormat = aovOutputFormat,
                    width = width,
                    height = height,
                    frameRate = frameRate,
                    capFrameRate = true
                };
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid AOV configuration: {errorMessage}");
                return null;
            }
            
            var settingsList = RecorderSettingsFactory.CreateAOVRecorderSettings("AOVRecorder", config);
            
            // Configure output path for each AOV setting
            foreach (var settings in settingsList)
            {
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.AOV);
            }
            
            return settingsList;
        }
        
        private RecorderSettings CreateAlembicRecorderSettings(string outputPath, string outputFileName)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateAlembicRecorderSettings called with path: {outputPath}, fileName: {outputFileName} ===");
            
            AlembicRecorderSettingsConfig config = null;
            
            if (useAlembicPreset && alembicPreset != AlembicExportPreset.Custom)
            {
                config = AlembicRecorderSettingsConfig.GetPreset(alembicPreset);
            }
            else
            {
                // Create custom configuration
                config = new AlembicRecorderSettingsConfig
                {
                    exportTargets = alembicExportTargets,
                    exportScope = alembicExportScope,
                    targetGameObject = alembicTargetGameObject,
                    handedness = alembicHandedness,
                    scaleFactor = alembicScaleFactor,
                    frameRate = frameRate,
                    samplesPerFrame = 1, // Default to 1 sample per frame
                    exportUVs = true,
                    exportNormals = true
                };
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Alembic config: scope={alembicExportScope}, targetGameObject={alembicTargetGameObject?.name ?? "null"} ===");
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid Alembic configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings("AlembicRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Alembic);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAnimationRecorderSettings(string outputPath, string outputFileName)
        {
            AnimationRecorderSettingsConfig config = null;
            
            if (useAnimationPreset && animationPreset != AnimationExportPreset.Custom)
            {
                config = AnimationRecorderSettingsConfig.GetPreset(animationPreset);
                // Override target GameObject if needed
                if ((animationRecordingScope == AnimationRecordingScope.SingleGameObject ||
                     animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren) &&
                    animationTargetGameObject != null)
                {
                    config.targetGameObject = animationTargetGameObject;
                }
            }
            else
            {
                // Create custom configuration
                config = new AnimationRecorderSettingsConfig
                {
                    recordingProperties = animationRecordingProperties,
                    recordingScope = animationRecordingScope,
                    targetGameObject = animationTargetGameObject,
                    interpolationMode = animationInterpolationMode,
                    compressionLevel = animationCompressionLevel,
                    frameRate = frameRate,
                    recordInWorldSpace = false,
                    treatAsHumanoid = false,
                    optimizeGameObjects = true
                };
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid Animation configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings("AnimationRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Animation);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateFBXRecorderSettings(string outputPath, string outputFileName)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateFBXRecorderSettings called with path: {outputPath}, fileName: {outputFileName} ===");
            
            FBXRecorderSettingsConfig config = null;
            
            if (useFBXPreset && fbxPreset != FBXExportPreset.Custom)
            {
                config = FBXRecorderSettingsConfig.GetPreset(fbxPreset);
            }
            else
            {
                // Create custom configuration
                config = new FBXRecorderSettingsConfig
                {
                    exportGeometry = fbxExportGeometry,
                    transferAnimationSource = fbxTransferAnimationSource,
                    transferAnimationDest = fbxTransferAnimationDest,
                    frameRate = frameRate
                };
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === FBX config: exportGeometry={fbxExportGeometry}, transferSource={fbxTransferAnimationSource?.name ?? "null"}, transferDest={fbxTransferAnimationDest?.name ?? "null"} ===");
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Invalid FBX configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("FBXRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.FBX);
            }
            
            return settings;
        }
        
    }
}