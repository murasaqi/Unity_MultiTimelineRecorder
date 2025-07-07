using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System.Linq;

namespace BatchRenderingTool
{
    /// <summary>
    /// Single Timeline Renderer V2 - Supports multiple recorders per timeline
    /// </summary>
    public class SingleTimelineRendererV2 : EditorWindow, IRecorderSettingsHost
    {
        // Static instance tracking
        private static SingleTimelineRendererV2 instance;
        
        public enum RenderState
        {
            Idle,
            Preparing,
            PreparingAssets,
            SavingAssets,
            WaitingForPlayMode,
            InitializingInPlayMode,
            Rendering,
            Complete,
            Error
        }
        
        // UI State
        private RenderState currentState = RenderState.Idle;
        private string statusMessage = "Ready to render";
        private float renderProgress = 0f;
        
        // Debug settings
        private bool debugKeepTimeline = false;
        
        // Timeline selection
        private List<PlayableDirector> availableDirectors = new List<PlayableDirector>();
        private int selectedDirectorIndex = 0;
        
        // Recorder configurations
        private List<RecorderConfig> recorderConfigs = new List<RecorderConfig>();
        private List<RecorderConfigEditor> recorderEditors = new List<RecorderConfigEditor>();
        
        // Common settings
        public int frameRate = 24;
        public int preRollFrames = 0;
        
        // Rendering objects
        private TimelineAsset renderTimeline;
        private GameObject renderingGameObject;
        private PlayableDirector renderingDirector;
        private EditorCoroutine renderCoroutine;
        private string tempAssetPath;
        
        // Scroll position for the UI
        private Vector2 scrollPosition;
        
        // Properties for easy access
        public PlayableDirector selectedDirector => 
            availableDirectors != null && selectedDirectorIndex >= 0 && selectedDirectorIndex < availableDirectors.Count 
            ? availableDirectors[selectedDirectorIndex] 
            : null;
        
        // IRecorderSettingsHost interface implementation
        // These are not used directly in V2 but needed for interface compatibility
        int IRecorderSettingsHost.frameRate { get => frameRate; set => frameRate = value; }
        int IRecorderSettingsHost.width { get => 1920; set { } }
        int IRecorderSettingsHost.height { get => 1080; set { } }
        string IRecorderSettingsHost.fileName { get => ""; set { } }
        string IRecorderSettingsHost.filePath { get => ""; set { } }
        int IRecorderSettingsHost.takeNumber { get => 1; set { } }
        
        // Dummy implementations for interface requirements
        ImageRecorderSettings.ImageRecorderOutputFormat IRecorderSettingsHost.imageOutputFormat { get => ImageRecorderSettings.ImageRecorderOutputFormat.PNG; set { } }
        bool IRecorderSettingsHost.imageCaptureAlpha { get => false; set { } }
        int IRecorderSettingsHost.jpegQuality { get => 75; set { } }
        CompressionUtility.EXRCompressionType IRecorderSettingsHost.exrCompression { get => CompressionUtility.EXRCompressionType.None; set { } }
        MovieRecorderSettings.VideoRecorderOutputFormat IRecorderSettingsHost.movieOutputFormat { get => MovieRecorderSettings.VideoRecorderOutputFormat.MP4; set { } }
        VideoBitrateMode IRecorderSettingsHost.movieQuality { get => VideoBitrateMode.High; set { } }
        bool IRecorderSettingsHost.movieCaptureAudio { get => false; set { } }
        bool IRecorderSettingsHost.movieCaptureAlpha { get => false; set { } }
        int IRecorderSettingsHost.movieBitrate { get => 15; set { } }
        AudioBitRateMode IRecorderSettingsHost.audioBitrate { get => AudioBitRateMode.High; set { } }
        MovieRecorderPreset IRecorderSettingsHost.moviePreset { get => MovieRecorderPreset.HighQuality1080p; set { } }
        bool IRecorderSettingsHost.useMoviePreset { get => false; set { } }
        AOVType IRecorderSettingsHost.selectedAOVTypes { get => AOVType.Depth; set { } }
        AOVOutputFormat IRecorderSettingsHost.aovOutputFormat { get => AOVOutputFormat.EXR16; set { } }
        AOVPreset IRecorderSettingsHost.aovPreset { get => AOVPreset.Compositing; set { } }
        bool IRecorderSettingsHost.useAOVPreset { get => false; set { } }
        AlembicExportTargets IRecorderSettingsHost.alembicExportTargets { get => AlembicExportTargets.MeshRenderer; set { } }
        AlembicExportScope IRecorderSettingsHost.alembicExportScope { get => AlembicExportScope.EntireScene; set { } }
        GameObject IRecorderSettingsHost.alembicTargetGameObject { get => null; set { } }
        AlembicHandedness IRecorderSettingsHost.alembicHandedness { get => AlembicHandedness.Left; set { } }
        float IRecorderSettingsHost.alembicWorldScale { get => 1f; set { } }
        float IRecorderSettingsHost.alembicFrameRate { get => 24f; set { } }
        AlembicTimeSamplingType IRecorderSettingsHost.alembicTimeSamplingType { get => AlembicTimeSamplingType.Uniform; set { } }
        bool IRecorderSettingsHost.alembicIncludeChildren { get => true; set { } }
        bool IRecorderSettingsHost.alembicFlattenHierarchy { get => false; set { } }
        AlembicExportPreset IRecorderSettingsHost.alembicPreset { get => AlembicExportPreset.AnimationExport; set { } }
        bool IRecorderSettingsHost.useAlembicPreset { get => false; set { } }
        GameObject IRecorderSettingsHost.animationTargetGameObject { get => null; set { } }
        AnimationRecordingScope IRecorderSettingsHost.animationRecordingScope { get => AnimationRecordingScope.SingleGameObject; set { } }
        bool IRecorderSettingsHost.animationIncludeChildren { get => true; set { } }
        bool IRecorderSettingsHost.animationClampedTangents { get => true; set { } }
        bool IRecorderSettingsHost.animationRecordBlendShapes { get => false; set { } }
        float IRecorderSettingsHost.animationPositionError { get => 0.5f; set { } }
        float IRecorderSettingsHost.animationRotationError { get => 0.5f; set { } }
        float IRecorderSettingsHost.animationScaleError { get => 0.5f; set { } }
        AnimationExportPreset IRecorderSettingsHost.animationPreset { get => AnimationExportPreset.SimpleTransform; set { } }
        bool IRecorderSettingsHost.useAnimationPreset { get => false; set { } }
        GameObject IRecorderSettingsHost.fbxTargetGameObject { get => null; set { } }
        bool IRecorderSettingsHost.fbxRecordHierarchy { get => true; set { } }
        bool IRecorderSettingsHost.fbxClampedTangents { get => true; set { } }
        FBXAnimationCompressionLevel IRecorderSettingsHost.fbxAnimationCompression { get => FBXAnimationCompressionLevel.Lossy; set { } }
        bool IRecorderSettingsHost.fbxExportGeometry { get => true; set { } }
        Transform IRecorderSettingsHost.fbxTransferAnimationSource { get => null; set { } }
        Transform IRecorderSettingsHost.fbxTransferAnimationDest { get => null; set { } }
        FBXExportPreset IRecorderSettingsHost.fbxPreset { get => FBXExportPreset.AnimationExport; set { } }
        bool IRecorderSettingsHost.useFBXPreset { get => false; set { } }
        
        [MenuItem("Window/Batch Rendering Tool/Single Timeline Renderer V2")]
        public static SingleTimelineRendererV2 ShowWindow()
        {
            var window = GetWindow<SingleTimelineRendererV2>();
            window.titleContent = new GUIContent("Single Timeline Renderer V2");
            window.minSize = new Vector2(500, 600);
            instance = window;
            return window;
        }
        
        private void OnEnable()
        {
            BatchRenderingToolLogger.LogVerbose("[SingleTimelineRendererV2] OnEnable called");
            instance = this;
            
            // Load debug settings
            debugKeepTimeline = EditorPrefs.GetBool("STRV2_DebugKeepTimeline", false);
            
            // Initialize with one default recorder if empty
            if (recorderConfigs.Count == 0)
            {
                recorderConfigs.Add(RecorderConfig.CreateDefault(RecorderSettingsType.Image));
                recorderEditors.Add(new RecorderConfigEditor(recorderConfigs[0], this));
            }
            else
            {
                // Rebuild editors for existing configs
                recorderEditors.Clear();
                foreach (var config in recorderConfigs)
                {
                    recorderEditors.Add(new RecorderConfigEditor(config, this));
                }
            }
            
            // Reset state if not in Play Mode
            if (!EditorApplication.isPlaying)
            {
                currentState = RenderState.Idle;
                renderCoroutine = null;
            }
            
            ScanTimelines();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
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
            EditorGUILayout.LabelField("Single Timeline Renderer V2 - Multi-Recorder Support", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            try
            {
                DrawTimelineSelection();
                EditorGUILayout.Space(10);
                
                DrawCommonSettings();
                EditorGUILayout.Space(10);
                
                DrawRecorderConfigurations();
                EditorGUILayout.Space(10);
                
                DrawRenderControls();
                EditorGUILayout.Space(10);
                
                DrawStatusSection();
                EditorGUILayout.Space(10);
                
                DrawDebugSettings();
            }
            finally
            {
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
                ScanTimelines();
            }
            EditorGUILayout.EndHorizontal();
            
            if (availableDirectors.Count > 0)
            {
                string[] directorNames = availableDirectors
                    .Select(d => d != null && d.gameObject != null ? d.gameObject.name : "<Missing>")
                    .ToArray();
                
                selectedDirectorIndex = EditorGUILayout.Popup("Select Timeline:", selectedDirectorIndex, directorNames);
                
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
        
        private void DrawCommonSettings()
        {
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            
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
        }
        
        private void DrawRecorderConfigurations()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Recorder Configurations", EditorStyles.boldLabel);
            
            // Add recorder button
            if (GUILayout.Button("+ Add Recorder", GUILayout.Width(120)))
            {
                ShowAddRecorderMenu();
            }
            EditorGUILayout.EndHorizontal();
            
            if (recorderConfigs.Count == 0)
            {
                EditorGUILayout.HelpBox("No recorders configured. Click 'Add Recorder' to add one.", MessageType.Info);
                return;
            }
            
            // Draw each recorder configuration
            for (int i = 0; i < recorderConfigs.Count; i++)
            {
                EditorGUILayout.Space(5);
                
                bool shouldDelete = recorderEditors[i].DrawRecorderConfig(i, recorderConfigs.Count);
                
                // Handle move up/down
                if (Event.current.type == EventType.Used && GUI.changed)
                {
                    if (i > 0 && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        // Move up
                        (recorderConfigs[i], recorderConfigs[i - 1]) = (recorderConfigs[i - 1], recorderConfigs[i]);
                        (recorderEditors[i], recorderEditors[i - 1]) = (recorderEditors[i - 1], recorderEditors[i]);
                        GUI.changed = false;
                    }
                    else if (i < recorderConfigs.Count - 1)
                    {
                        // Move down
                        (recorderConfigs[i], recorderConfigs[i + 1]) = (recorderConfigs[i + 1], recorderConfigs[i]);
                        (recorderEditors[i], recorderEditors[i + 1]) = (recorderEditors[i + 1], recorderEditors[i]);
                        GUI.changed = false;
                    }
                }
                
                // Handle delete
                if (shouldDelete)
                {
                    recorderConfigs.RemoveAt(i);
                    recorderEditors.RemoveAt(i);
                    i--;
                }
            }
        }
        
        private void ShowAddRecorderMenu()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Image Sequence"), false, () => AddRecorder(RecorderSettingsType.Image));
            menu.AddItem(new GUIContent("Movie"), false, () => AddRecorder(RecorderSettingsType.Movie));
            
            if (RecorderSettingsFactory.IsRecorderTypeSupported(RecorderSettingsType.AOV))
            {
                menu.AddItem(new GUIContent("AOV Passes"), false, () => AddRecorder(RecorderSettingsType.AOV));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("AOV Passes (Requires HDRP)"));
            }
            
            if (RecorderSettingsFactory.IsRecorderTypeSupported(RecorderSettingsType.Alembic))
            {
                menu.AddItem(new GUIContent("Alembic"), false, () => AddRecorder(RecorderSettingsType.Alembic));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Alembic (Package not installed)"));
            }
            
            menu.AddItem(new GUIContent("Animation Clip"), false, () => AddRecorder(RecorderSettingsType.Animation));
            
            if (RecorderSettingsFactory.IsRecorderTypeSupported(RecorderSettingsType.FBX))
            {
                menu.AddItem(new GUIContent("FBX"), false, () => AddRecorder(RecorderSettingsType.FBX));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("FBX (Package not installed)"));
            }
            
            menu.ShowAsContext();
        }
        
        private void AddRecorder(RecorderSettingsType type)
        {
            var config = RecorderConfig.CreateDefault(type);
            
            // Generate unique name if needed
            int counter = 2;
            string baseName = config.configName;
            while (recorderConfigs.Any(c => c.configName == config.configName))
            {
                config.configName = $"{baseName} {counter}";
                counter++;
            }
            
            recorderConfigs.Add(config);
            recorderEditors.Add(new RecorderConfigEditor(config, this));
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool canRender = currentState == RenderState.Idle && 
                           availableDirectors.Count > 0 && 
                           !EditorApplication.isPlaying &&
                           recorderConfigs.Count > 0 &&
                           recorderConfigs.Any(c => c.enabled);
            
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
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDebugSettings()
        {
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            bool newDebugKeepTimeline = EditorGUILayout.Toggle("Keep Generated Timeline", debugKeepTimeline);
            if (newDebugKeepTimeline != debugKeepTimeline)
            {
                debugKeepTimeline = newDebugKeepTimeline;
                EditorPrefs.SetBool("STRV2_DebugKeepTimeline", debugKeepTimeline);
            }
            
            if (debugKeepTimeline)
            {
                EditorGUILayout.HelpBox("Debug Mode: Generated timeline will be preserved after rendering.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ScanTimelines()
        {
            availableDirectors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            
            foreach (var director in allDirectors)
            {
                if (director != null && director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    availableDirectors.Add(director);
                }
            }
            
            availableDirectors.RemoveAll(d => d == null || d.gameObject == null);
            availableDirectors.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));
            
            if (selectedDirectorIndex >= availableDirectors.Count)
            {
                selectedDirectorIndex = 0;
            }
        }
        
        private void StartRendering()
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRendererV2] === StartRendering called ===");
            
            // Validate all enabled recorders
            foreach (var config in recorderConfigs.Where(c => c.enabled))
            {
                string error;
                if (!config.Validate(out error))
                {
                    currentState = RenderState.Error;
                    statusMessage = error;
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Validation failed: {error}");
                    return;
                }
            }
            
            if (renderCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
            }
            
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
            currentState = RenderState.Preparing;
            statusMessage = "Preparing...";
            renderProgress = 0f;
            
            // Validate selection
            if (availableDirectors.Count == 0 || selectedDirectorIndex < 0 || selectedDirectorIndex >= availableDirectors.Count)
            {
                currentState = RenderState.Error;
                statusMessage = "No timeline selected";
                yield break;
            }
            
            var selectedDirector = availableDirectors[selectedDirectorIndex];
            if (selectedDirector == null || selectedDirector.gameObject == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected director is null or destroyed";
                yield break;
            }
            
            var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
            if (originalTimeline == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected asset is not a Timeline";
                yield break;
            }
            
            // Store original settings
            bool originalPlayOnAwake = selectedDirector.playOnAwake;
            selectedDirector.playOnAwake = false;
            
            string directorName = selectedDirector.gameObject.name;
            float timelineDuration = (float)originalTimeline.duration;
            
            // Create render timeline
            currentState = RenderState.PreparingAssets;
            statusMessage = "Creating render timeline...";
            
            try
            {
                renderTimeline = CreateRenderTimeline(selectedDirector, originalTimeline);
                if (renderTimeline == null)
                {
                    currentState = RenderState.Error;
                    statusMessage = "Failed to create render timeline";
                    selectedDirector.playOnAwake = originalPlayOnAwake;
                    yield break;
                }
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRendererV2] Successfully created render timeline at: {tempAssetPath}");
            }
            catch (Exception e)
            {
                currentState = RenderState.Error;
                statusMessage = $"Error creating timeline: {e.Message}";
                BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Error: {e}");
                selectedDirector.playOnAwake = originalPlayOnAwake;
                yield break;
            }
            
            // Save assets
            currentState = RenderState.SavingAssets;
            statusMessage = "Saving timeline asset...";
            yield return null;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(tempAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            // Verify asset
            var verifyAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
            if (verifyAsset == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Failed to save Timeline asset";
                selectedDirector.playOnAwake = originalPlayOnAwake;
                yield break;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Enter Play Mode
            currentState = RenderState.WaitingForPlayMode;
            statusMessage = "Starting Unity Play Mode...";
            
            if (!EditorApplication.isPlaying)
            {
                // Store data for Play Mode
                EditorPrefs.SetString("STRV2_DirectorName", directorName);
                EditorPrefs.SetString("STRV2_TempAssetPath", tempAssetPath);
                EditorPrefs.SetFloat("STRV2_Duration", timelineDuration);
                EditorPrefs.SetBool("STRV2_IsRendering", true);
                EditorPrefs.SetInt("STRV2_FrameRate", frameRate);
                EditorPrefs.SetInt("STRV2_PreRollFrames", preRollFrames);
                
                EditorApplication.isPlaying = true;
            }
        }
        
        private TimelineAsset CreateRenderTimeline(PlayableDirector originalDirector, TimelineAsset originalTimeline)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRendererV2] CreateRenderTimeline started");
            
            try
            {
                // Create timeline
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                if (timeline == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRendererV2] Failed to create TimelineAsset");
                    return null;
                }
                
                timeline.name = $"{originalDirector.gameObject.name}_RenderTimeline_V2";
                timeline.editorSettings.frameRate = frameRate;
                
                // Save as temporary asset
                string tempDir = debugKeepTimeline 
                    ? "Assets/BatchRenderingTool/Debug" 
                    : "Assets/BatchRenderingTool/Temp";
                
                if (!AssetDatabase.IsValidFolder(tempDir))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                    {
                        AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                    }
                    AssetDatabase.CreateFolder("Assets/BatchRenderingTool", 
                        debugKeepTimeline ? "Debug" : "Temp");
                }
                
                tempAssetPath = debugKeepTimeline 
                    ? $"{tempDir}/{originalDirector.gameObject.name}_RenderTimeline_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.playable"
                    : $"{tempDir}/{timeline.name}_{System.DateTime.Now.Ticks}.playable";
                
                AssetDatabase.CreateAsset(timeline, tempAssetPath);
                
                // Create control track
                var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
                if (controlTrack == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRendererV2] Failed to create ControlTrack");
                    return null;
                }
                
                // Calculate pre-roll time
                float preRollTime = preRollFrames > 0 ? preRollFrames / (float)frameRate : 0f;
                string exposedName = UnityEditor.GUID.Generate().ToString();
                
                // Create pre-roll clip if needed
                if (preRollFrames > 0)
                {
                    var preRollClip = controlTrack.CreateClip<ControlPlayableAsset>();
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
                    
                    preRollClip.clipIn = 0;
                    preRollClip.timeScale = 0.0001; // Virtually freeze time
                }
                
                // Create main playback clip
                var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
                controlClip.displayName = originalDirector.gameObject.name;
                controlClip.start = preRollTime;
                controlClip.duration = originalTimeline.duration;
                
                var controlAsset = controlClip.asset as ControlPlayableAsset;
                controlAsset.sourceGameObject.exposedName = exposedName;
                controlAsset.sourceGameObject.defaultValue = originalDirector.gameObject;
                controlAsset.updateDirector = true;
                controlAsset.updateParticle = true;
                controlAsset.updateITimeControl = true;
                controlAsset.searchHierarchy = false;
                controlAsset.active = true;
                
                // Store exposed name
                EditorPrefs.SetString("STRV2_ExposedName", exposedName);
                
                // Create recorder tracks and clips for each enabled recorder config
                int recorderIndex = 0;
                foreach (var config in recorderConfigs.Where(c => c.enabled))
                {
                    BatchRenderingToolLogger.Log($"[SingleTimelineRendererV2] Creating recorder track for {config.configName}");
                    
                    var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, $"Recorder Track {recorderIndex + 1}");
                    if (recorderTrack == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Failed to create RecorderTrack for {config.configName}");
                        continue;
                    }
                    
                    // Create recorder clip
                    var recorderClip = recorderTrack.CreateClip<RecorderClip>();
                    recorderClip.displayName = $"Record {config.configName}";
                    recorderClip.start = preRollTime;
                    recorderClip.duration = originalTimeline.duration;
                    
                    // Create recorder settings
                    var recorderSettings = CreateRecorderSettings(config, originalDirector.gameObject.name);
                    if (recorderSettings == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Failed to create settings for {config.configName}");
                        continue;
                    }
                    
                    // Add settings as sub-asset
                    AssetDatabase.AddObjectToAsset(recorderSettings, timeline);
                    
                    // Set recorder clip settings
                    var recorderAsset = recorderClip.asset as RecorderClip;
                    recorderAsset.settings = recorderSettings;
                    
                    // Apply type-specific patches
                    if (config.recorderType == RecorderSettingsType.FBX)
                    {
                        Patches.FBXRecorderPatch.ValidateFBXRecorderClip(recorderClip);
                    }
                    
                    recorderIndex++;
                }
                
                // Save everything
                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRendererV2] Created timeline with {recorderIndex} recorder tracks");
                
                return timeline;
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Exception in CreateRenderTimeline: {e}");
                return null;
            }
        }
        
        private RecorderSettings CreateRecorderSettings(RecorderConfig config, string timelineName)
        {
            // Create wildcard context
            var context = new WildcardContext(config.takeNumber, config.width, config.height)
            {
                TimelineName = timelineName,
                RecorderName = config.configName
            };
            
            // Set GameObject name for targeted exports
            if ((config.recorderType == RecorderSettingsType.Alembic && config.alembicTargetGameObject != null) ||
                (config.recorderType == RecorderSettingsType.Animation && config.animationTargetGameObject != null) ||
                (config.recorderType == RecorderSettingsType.FBX && config.fbxTargetGameObject != null))
            {
                GameObject targetGO = config.recorderType switch
                {
                    RecorderSettingsType.Alembic => config.alembicTargetGameObject,
                    RecorderSettingsType.Animation => config.animationTargetGameObject,
                    RecorderSettingsType.FBX => config.fbxTargetGameObject,
                    _ => null
                };
                
                if (targetGO != null)
                {
                    context.GameObjectName = targetGO.name;
                }
            }
            
            var processedFileName = WildcardProcessor.ProcessWildcards(config.fileName, context);
            var processedFilePath = config.filePath;
            
            // Create settings based on type
            RecorderSettings settings = null;
            
            switch (config.recorderType)
            {
                case RecorderSettingsType.Image:
                    settings = CreateImageRecorderSettings(config, processedFilePath, processedFileName);
                    break;
                    
                case RecorderSettingsType.Movie:
                    settings = CreateMovieRecorderSettings(config, processedFilePath, processedFileName);
                    break;
                    
                case RecorderSettingsType.AOV:
                    var aovSettingsList = CreateAOVRecorderSettings(config, processedFilePath, processedFileName);
                    if (aovSettingsList != null && aovSettingsList.Count > 0)
                    {
                        settings = aovSettingsList[0]; // Use first for main track
                        // TODO: Handle multiple AOV passes
                    }
                    break;
                    
                case RecorderSettingsType.Alembic:
                    settings = CreateAlembicRecorderSettings(config, processedFilePath, processedFileName);
                    break;
                    
                case RecorderSettingsType.Animation:
                    settings = CreateAnimationRecorderSettings(config, processedFilePath, processedFileName);
                    break;
                    
                case RecorderSettingsType.FBX:
                    settings = CreateFBXRecorderSettings(config, processedFilePath, processedFileName);
                    break;
            }
            
            return settings;
        }
        
        // Recorder-specific creation methods
        private RecorderSettings CreateImageRecorderSettings(RecorderConfig config, string outputPath, string outputFileName)
        {
            var settings = RecorderClipUtility.CreateProperImageRecorderSettings("ImageRecorder");
            if (settings == null) return null;
            
            settings.Enabled = true;
            settings.OutputFormat = config.imageOutputFormat;
            settings.CaptureAlpha = config.imageCaptureAlpha;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
            settings.FrameRate = config.frameRate;
            settings.CapFrameRate = true;
            
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Image);
            
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = config.width,
                OutputHeight = config.height
            };
            
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettings(RecorderConfig config, string outputPath, string outputFileName)
        {
            MovieRecorderSettings settings = null;
            
            if (config.useMoviePreset && config.moviePreset != MovieRecorderPreset.Custom)
            {
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", config.moviePreset);
            }
            else
            {
                var movieConfig = new MovieRecorderSettingsConfig
                {
                    outputFormat = config.movieOutputFormat,
                    videoBitrateMode = config.movieQuality,
                    captureAudio = config.movieCaptureAudio,
                    captureAlpha = config.movieCaptureAlpha,
                    width = config.width,
                    height = config.height,
                    frameRate = config.frameRate,
                    capFrameRate = true
                };
                
                string errorMessage;
                if (!movieConfig.Validate(out errorMessage))
                {
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Invalid movie configuration: {errorMessage}");
                    return null;
                }
                
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", movieConfig);
            }
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Movie);
            }
            
            return settings;
        }
        
        private List<RecorderSettings> CreateAOVRecorderSettings(RecorderConfig config, string outputPath, string outputFileName)
        {
            AOVRecorderSettingsConfig aovConfig = null;
            
            if (config.useAOVPreset && config.aovPreset != AOVPreset.Custom)
            {
                aovConfig = config.aovPreset switch
                {
                    AOVPreset.Compositing => AOVRecorderSettingsConfig.Presets.GetCompositing(),
                    AOVPreset.GeometryOnly => AOVRecorderSettingsConfig.Presets.GetGeometryOnly(),
                    AOVPreset.LightingOnly => AOVRecorderSettingsConfig.Presets.GetLightingOnly(),
                    AOVPreset.MaterialProperties => AOVRecorderSettingsConfig.Presets.GetMaterialProperties(),
                    _ => null
                };
            }
            else
            {
                aovConfig = new AOVRecorderSettingsConfig
                {
                    selectedAOVs = config.selectedAOVTypes,
                    outputFormat = config.aovOutputFormat,
                    width = config.width,
                    height = config.height,
                    frameRate = config.frameRate,
                    capFrameRate = true
                };
            }
            
            string errorMessage;
            if (!aovConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Invalid AOV configuration: {errorMessage}");
                return null;
            }
            
            var settingsList = RecorderSettingsFactory.CreateAOVRecorderSettings("AOVRecorder", aovConfig);
            
            foreach (var settings in settingsList)
            {
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.AOV);
            }
            
            return settingsList;
        }
        
        private RecorderSettings CreateAlembicRecorderSettings(RecorderConfig config, string outputPath, string outputFileName)
        {
            AlembicRecorderSettingsConfig alembicConfig = null;
            
            if (config.useAlembicPreset && config.alembicPreset != AlembicExportPreset.Custom)
            {
                alembicConfig = AlembicRecorderSettingsConfig.GetPreset(config.alembicPreset);
            }
            else
            {
                alembicConfig = new AlembicRecorderSettingsConfig
                {
                    exportTargets = config.alembicExportTargets,
                    exportScope = config.alembicExportScope,
                    targetGameObject = config.alembicTargetGameObject,
                    handedness = config.alembicHandedness,
                    scaleFactor = config.alembicScaleFactor,
                    frameRate = config.frameRate,
                    samplesPerFrame = 1,
                    exportUVs = true,
                    exportNormals = true
                };
            }
            
            string errorMessage;
            if (!alembicConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Invalid Alembic configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings("AlembicRecorder", alembicConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Alembic);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAnimationRecorderSettings(RecorderConfig config, string outputPath, string outputFileName)
        {
            AnimationRecorderSettingsConfig animConfig = null;
            
            if (config.useAnimationPreset && config.animationPreset != AnimationExportPreset.Custom)
            {
                animConfig = AnimationRecorderSettingsConfig.GetPreset(config.animationPreset);
                if ((config.animationRecordingScope == AnimationRecordingScope.SingleGameObject ||
                     config.animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren) &&
                    config.animationTargetGameObject != null)
                {
                    animConfig.targetGameObject = config.animationTargetGameObject;
                }
            }
            else
            {
                animConfig = new AnimationRecorderSettingsConfig
                {
                    recordingProperties = config.animationRecordingProperties,
                    recordingScope = config.animationRecordingScope,
                    targetGameObject = config.animationTargetGameObject,
                    interpolationMode = config.animationInterpolationMode,
                    compressionLevel = config.animationCompressionLevel,
                    frameRate = config.frameRate,
                    recordInWorldSpace = false,
                    treatAsHumanoid = false,
                    optimizeGameObjects = true
                };
            }
            
            string errorMessage;
            if (!animConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Invalid Animation configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings("AnimationRecorder", animConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.Animation);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateFBXRecorderSettings(RecorderConfig config, string outputPath, string outputFileName)
        {
            if (config.fbxTargetGameObject == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRendererV2] FBX Recorder requires a target GameObject");
                return null;
            }
            
            FBXRecorderSettingsConfig fbxConfig = null;
            
            if (config.useFBXPreset && config.fbxPreset != FBXExportPreset.Custom)
            {
                fbxConfig = FBXRecorderSettingsConfig.GetPreset(config.fbxPreset);
                fbxConfig.targetGameObject = config.fbxTargetGameObject;
            }
            else
            {
                fbxConfig = new FBXRecorderSettingsConfig
                {
                    targetGameObject = config.fbxTargetGameObject,
                    recordHierarchy = config.fbxRecordHierarchy,
                    clampedTangents = config.fbxClampedTangents,
                    animationCompression = config.fbxAnimationCompression,
                    exportGeometry = config.fbxExportGeometry,
                    transferAnimationSource = config.fbxTransferAnimationSource,
                    transferAnimationDest = config.fbxTransferAnimationDest,
                    frameRate = config.frameRate
                };
            }
            
            string errorMessage;
            if (!fbxConfig.Validate(out errorMessage))
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRendererV2] Invalid FBX configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("FBXRecorder", fbxConfig);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputPath, outputFileName, RecorderSettingsType.FBX);
            }
            
            return settings;
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
            
            // Handle timeline asset
            if (!string.IsNullOrEmpty(tempAssetPath) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tempAssetPath) != null)
            {
                if (debugKeepTimeline)
                {
                    BatchRenderingToolLogger.Log($"[SingleTimelineRendererV2] Debug Mode: Timeline preserved at: {tempAssetPath}");
                }
                else
                {
                    AssetDatabase.DeleteAsset(tempAssetPath);
                }
            }
            
            renderTimeline = null;
            renderingDirector = null;
            
            if (!debugKeepTimeline)
            {
                tempAssetPath = null;
            }
            
            AssetDatabase.Refresh();
        }
        
        private void OnEditorUpdate()
        {
            // Handle Play Mode monitoring if needed
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                bool isRendering = EditorPrefs.GetBool("STRV2_IsRendering", false);
                if (isRendering)
                {
                    currentState = RenderState.Rendering;
                    statusMessage = "Rendering in Play Mode...";
                    
                    // Create rendering data GameObject for PlayModeTimelineRenderer
                    string directorName = EditorPrefs.GetString("STRV2_DirectorName", "");
                    string tempAssetPath = EditorPrefs.GetString("STRV2_TempAssetPath", "");
                    
                    var renderTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
                    if (renderTimeline != null)
                    {
                        var dataGO = new GameObject("[RenderingDataV2]");
                        var renderingData = dataGO.AddComponent<RenderingData>();
                        renderingData.directorName = directorName;
                        renderingData.renderTimeline = renderTimeline;
                        renderingData.duration = EditorPrefs.GetFloat("STRV2_Duration", 0f);
                        renderingData.exposedName = EditorPrefs.GetString("STRV2_ExposedName", "");
                        renderingData.frameRate = EditorPrefs.GetInt("STRV2_FrameRate", 24);
                        renderingData.preRollFrames = EditorPrefs.GetInt("STRV2_PreRollFrames", 0);
                        
                        var rendererGO = new GameObject("[PlayModeTimelineRendererV2]");
                        var renderer = rendererGO.AddComponent<PlayModeTimelineRenderer>();
                        
                        BatchRenderingToolLogger.Log("[SingleTimelineRendererV2] PlayModeTimelineRenderer created for multi-recorder rendering");
                    }
                    
                    EditorPrefs.SetBool("STRV2_IsRendering", false);
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (EditorPrefs.GetBool("STRV2_RenderingComplete", false))
                {
                    string completionStatus = EditorPrefs.GetString("STRV2_RenderingStatus", "Unknown");
                    
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
                    
                    EditorPrefs.DeleteKey("STRV2_RenderingComplete");
                    EditorPrefs.DeleteKey("STRV2_RenderingStatus");
                }
                else
                {
                    currentState = RenderState.Idle;
                    statusMessage = "Play Mode exited";
                }
                
                if (renderCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(renderCoroutine);
                    renderCoroutine = null;
                }
                
                CleanupRendering();
                EditorPrefs.SetBool("STRV2_IsRendering", false);
            }
        }
    }
}