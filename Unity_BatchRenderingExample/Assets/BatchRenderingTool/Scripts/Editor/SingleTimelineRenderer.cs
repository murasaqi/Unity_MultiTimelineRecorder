using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System.Linq;

namespace BatchRenderingTool
{
    /// <summary>
    /// Single Timeline Renderer - Renders one timeline at a time with a simple UI
    /// </summary>
    public class SingleTimelineRenderer : EditorWindow
    {
        public enum RenderState
        {
            Idle,
            Preparing,
            WaitingForPlayMode,
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
        private int frameRate = 24;
        private int width = 1920;
        private int height = 1080;
        private string outputFile = "";
        private int takeNumber = 1;
        
        // Image recorder settings
        private ImageRecorderSettings.ImageRecorderOutputFormat imageOutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        private bool imageCaptureAlpha = false;
        
        // Movie recorder settings
        private MovieRecorderSettings.VideoRecorderOutputFormat movieOutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        private VideoBitrateMode movieQuality = VideoBitrateMode.High;
        private bool movieCaptureAudio = false;
        private bool movieCaptureAlpha = false;
        private MovieRecorderPreset moviePreset = MovieRecorderPreset.HighQuality1080p;
        private bool useMoviePreset = false;
        
        // AOV recorder settings
        private AOVType selectedAOVTypes = AOVType.Depth | AOVType.Normal | AOVType.Albedo;
        private AOVOutputFormat aovOutputFormat = AOVOutputFormat.EXR16;
        private AOVPreset aovPreset = AOVPreset.Compositing;
        private bool useAOVPreset = false;
        
        // Alembic recorder settings
        private AlembicExportTargets alembicExportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform;
        private AlembicExportScope alembicExportScope = AlembicExportScope.EntireScene;
        private GameObject alembicTargetGameObject = null;
        private AlembicHandedness alembicHandedness = AlembicHandedness.Left;
        private float alembicScaleFactor = 1f;
        private AlembicExportPreset alembicPreset = AlembicExportPreset.AnimationExport;
        private bool useAlembicPreset = false;
        
        // Animation recorder settings
        private AnimationRecordingProperties animationRecordingProperties = AnimationRecordingProperties.TransformOnly;
        private AnimationRecordingScope animationRecordingScope = AnimationRecordingScope.SingleGameObject;
        private GameObject animationTargetGameObject = null;
        private AnimationInterpolationMode animationInterpolationMode = AnimationInterpolationMode.Linear;
        private AnimationCompressionLevel animationCompressionLevel = AnimationCompressionLevel.Medium;
        private AnimationExportPreset animationPreset = AnimationExportPreset.CharacterAnimation;
        private bool useAnimationPreset = false;
        
        // Rendering objects
        private TimelineAsset renderTimeline;
        private GameObject renderingGameObject;
        private PlayableDirector renderingDirector;
        private EditorCoroutine renderCoroutine;
        private string tempAssetPath;
        
        [MenuItem("Window/Batch Rendering Tool/Single Timeline Renderer")]
        public static SingleTimelineRenderer ShowWindow()
        {
            var window = GetWindow<SingleTimelineRenderer>();
            window.titleContent = new GUIContent("Single Timeline Renderer");
            window.minSize = new Vector2(400, 450);
            return window;
        }
        
        private void OnEnable()
        {
            ScanTimelines();
            EditorApplication.update += OnEditorUpdate;
            
            // Initialize output file with default template if empty
            if (string.IsNullOrEmpty(outputFile))
            {
                outputFile = WildcardProcessor.GetDefaultTemplate(recorderType);
            }
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Single Timeline Renderer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            DrawTimelineSelection();
            EditorGUILayout.Space(10);
            
            DrawRenderSettings();
            EditorGUILayout.Space(10);
            
            DrawOutputSettings();
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
                ScanTimelines();
            }
            EditorGUILayout.EndHorizontal();
            
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
                
                // Update output file template when recorder type changes
                outputFile = WildcardProcessor.GetDefaultTemplate(recorderType);
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
            
            // Type-specific settings
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    DrawImageRecorderSettings();
                    break;
                    
                case RecorderSettingsType.Movie:
                    DrawMovieRecorderSettings();
                    break;
                    
                case RecorderSettingsType.AOV:
                    DrawAOVRecorderSettings();
                    break;
                    
                case RecorderSettingsType.Alembic:
                    DrawAlembicRecorderSettings();
                    break;
                    
                case RecorderSettingsType.Animation:
                    DrawAnimationRecorderSettings();
                    break;
                    
                default:
                    EditorGUILayout.HelpBox("Unknown recorder type", MessageType.Error);
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawImageRecorderSettings()
        {
            EditorGUILayout.LabelField("Image Settings", EditorStyles.miniBoldLabel);
            
            width = EditorGUILayout.IntField("Width:", width);
            height = EditorGUILayout.IntField("Height:", height);
            imageOutputFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", imageOutputFormat);
            imageCaptureAlpha = EditorGUILayout.Toggle("Capture Alpha:", imageCaptureAlpha);
            
            if (imageCaptureAlpha && imageOutputFormat != ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                EditorGUILayout.HelpBox("Alpha channel is best supported with EXR format", MessageType.Info);
            }
        }
        
        private void DrawMovieRecorderSettings()
        {
            EditorGUILayout.LabelField("Movie Settings", EditorStyles.miniBoldLabel);
            
            useMoviePreset = EditorGUILayout.Toggle("Use Preset:", useMoviePreset);
            
            if (useMoviePreset)
            {
                moviePreset = (MovieRecorderPreset)EditorGUILayout.EnumPopup("Preset:", moviePreset);
                
                // Show preset info and apply values
                var presetConfig = MovieRecorderSettingsConfig.GetPreset(moviePreset);
                EditorGUILayout.HelpBox(
                    $"Resolution: {presetConfig.width}x{presetConfig.height}\n" +
                    $"Frame Rate: {presetConfig.frameRate} fps\n" +
                    $"Format: {presetConfig.outputFormat}\n" +
                    $"Quality: {presetConfig.videoBitrateMode}",
                    MessageType.Info
                );
                
                // Apply preset values
                if (moviePreset != MovieRecorderPreset.Custom)
                {
                    width = presetConfig.width;
                    height = presetConfig.height;
                    frameRate = presetConfig.frameRate;
                    movieOutputFormat = presetConfig.outputFormat;
                    movieQuality = presetConfig.videoBitrateMode;
                    movieCaptureAudio = presetConfig.captureAudio;
                    movieCaptureAlpha = presetConfig.captureAlpha;
                }
            }
            else
            {
                width = EditorGUILayout.IntField("Width:", width);
                height = EditorGUILayout.IntField("Height:", height);
                movieOutputFormat = (MovieRecorderSettings.VideoRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", movieOutputFormat);
                movieQuality = (VideoBitrateMode)EditorGUILayout.EnumPopup("Quality:", movieQuality);
                movieCaptureAudio = EditorGUILayout.Toggle("Capture Audio:", movieCaptureAudio);
                movieCaptureAlpha = EditorGUILayout.Toggle("Capture Alpha:", movieCaptureAlpha);
            }
            
            // Platform-specific warnings
            if (movieOutputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MOV)
            {
                #if !UNITY_EDITOR_OSX
                EditorGUILayout.HelpBox("MOV format with ProRes is only available on macOS", MessageType.Warning);
                #endif
            }
            
            if (movieCaptureAlpha && movieOutputFormat != MovieRecorderSettings.VideoRecorderOutputFormat.MOV && 
                movieOutputFormat != MovieRecorderSettings.VideoRecorderOutputFormat.WebM)
            {
                EditorGUILayout.HelpBox("Alpha channel is only supported with MOV or WebM formats", MessageType.Warning);
            }
        }
        
        private void DrawAOVRecorderSettings()
        {
            EditorGUILayout.LabelField("AOV Settings", EditorStyles.miniBoldLabel);
            
            useAOVPreset = EditorGUILayout.Toggle("Use Preset:", useAOVPreset);
            
            if (useAOVPreset)
            {
                aovPreset = (AOVPreset)EditorGUILayout.EnumPopup("Preset:", aovPreset);
                
                // Apply preset
                if (aovPreset != AOVPreset.Custom)
                {
                    var config = AOVRecorderSettingsConfig.Presets.GetCompositing();
                    switch (aovPreset)
                    {
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
                    
                    selectedAOVTypes = config.selectedAOVs;
                    aovOutputFormat = config.outputFormat;
                    width = config.width;
                    height = config.height;
                }
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("AOV Types:", EditorStyles.miniBoldLabel);
            
            // Display AOV checkboxes in a compact grid
            var aovTypes = System.Linq.Enumerable.Cast<AOVType>(System.Enum.GetValues(typeof(AOVType)))
                .Where(t => t != AOVType.None).ToList();
            int columnCount = 2;
            int currentColumn = 0;
            
            if (aovTypes.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var aovType in aovTypes)
                {
                    if (currentColumn >= columnCount)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentColumn = 0;
                    }
                    
                    bool isSelected = (selectedAOVTypes & aovType) != 0;
                    bool newSelected = EditorGUILayout.ToggleLeft(aovType.ToString(), isSelected, GUILayout.Width(150));
                    
                    if (newSelected != isSelected)
                    {
                        if (newSelected)
                            selectedAOVTypes |= aovType;
                        else
                            selectedAOVTypes &= ~aovType;
                    }
                    
                    currentColumn++;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            aovOutputFormat = (AOVOutputFormat)EditorGUILayout.EnumPopup("Output Format:", aovOutputFormat);
            
            if (!useAOVPreset)
            {
                width = EditorGUILayout.IntField("Width:", width);
                height = EditorGUILayout.IntField("Height:", height);
            }
        }
        
        private void DrawAlembicRecorderSettings()
        {
            EditorGUILayout.LabelField("Alembic Settings", EditorStyles.miniBoldLabel);
            
            useAlembicPreset = EditorGUILayout.Toggle("Use Preset:", useAlembicPreset);
            
            if (useAlembicPreset)
            {
                alembicPreset = (AlembicExportPreset)EditorGUILayout.EnumPopup("Preset:", alembicPreset);
                
                // Apply preset
                if (alembicPreset != AlembicExportPreset.Custom)
                {
                    var config = AlembicRecorderSettingsConfig.GetPreset(alembicPreset);
                    alembicExportTargets = config.exportTargets;
                    alembicExportScope = config.exportScope;
                    alembicHandedness = config.handedness;
                    alembicScaleFactor = config.scaleFactor;
                }
            }
            
            EditorGUILayout.Space(5);
            alembicExportScope = (AlembicExportScope)EditorGUILayout.EnumPopup("Export Scope:", alembicExportScope);
            
            if (alembicExportScope == AlembicExportScope.TargetGameObject)
            {
                alembicTargetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject:", alembicTargetGameObject, typeof(GameObject), true);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Export Targets:", EditorStyles.miniBoldLabel);
            
            // Export target checkboxes
            var exportTargets = System.Linq.Enumerable.Cast<AlembicExportTargets>(System.Enum.GetValues(typeof(AlembicExportTargets)))
                .Where(t => t != AlembicExportTargets.None);
            foreach (var target in exportTargets)
            {
                bool isSelected = (alembicExportTargets & target) != 0;
                bool newSelected = EditorGUILayout.ToggleLeft(target.ToString(), isSelected);
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        alembicExportTargets |= target;
                    else
                        alembicExportTargets &= ~target;
                }
            }
            
            if (!useAlembicPreset)
            {
                EditorGUILayout.Space(5);
                alembicHandedness = (AlembicHandedness)EditorGUILayout.EnumPopup("Handedness:", alembicHandedness);
                alembicScaleFactor = EditorGUILayout.FloatField("Scale Factor:", alembicScaleFactor);
            }
        }
        
        private void DrawAnimationRecorderSettings()
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.miniBoldLabel);
            
            useAnimationPreset = EditorGUILayout.Toggle("Use Preset:", useAnimationPreset);
            
            if (useAnimationPreset)
            {
                animationPreset = (AnimationExportPreset)EditorGUILayout.EnumPopup("Preset:", animationPreset);
                
                // Apply preset
                if (animationPreset != AnimationExportPreset.Custom)
                {
                    var config = AnimationRecorderSettingsConfig.GetPreset(animationPreset);
                    animationRecordingProperties = config.recordingProperties;
                    animationRecordingScope = config.recordingScope;
                    animationInterpolationMode = config.interpolationMode;
                    animationCompressionLevel = config.compressionLevel;
                }
            }
            
            EditorGUILayout.Space(5);
            animationRecordingScope = (AnimationRecordingScope)EditorGUILayout.EnumPopup("Recording Scope:", animationRecordingScope);
            
            if (animationRecordingScope == AnimationRecordingScope.SingleGameObject ||
                animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren)
            {
                animationTargetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject:", animationTargetGameObject, typeof(GameObject), true);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Recording Properties:", EditorStyles.miniBoldLabel);
            
            // Quick selection buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Transform Only", GUILayout.Height(20)))
            {
                animationRecordingProperties = AnimationRecordingProperties.TransformOnly;
            }
            if (GUILayout.Button("All Properties", GUILayout.Height(20)))
            {
                animationRecordingProperties = AnimationRecordingProperties.AllProperties;
            }
            EditorGUILayout.EndHorizontal();
            
            // Property checkboxes
            var properties = System.Linq.Enumerable.Cast<AnimationRecordingProperties>(System.Enum.GetValues(typeof(AnimationRecordingProperties)))
                .Where(p => p != AnimationRecordingProperties.None && 
                           p != AnimationRecordingProperties.TransformOnly && 
                           p != AnimationRecordingProperties.TransformAndBlendShapes &&
                           p != AnimationRecordingProperties.AllProperties);
            
            foreach (var prop in properties)
            {
                bool isSelected = (animationRecordingProperties & prop) != 0;
                bool newSelected = EditorGUILayout.ToggleLeft(prop.ToString(), isSelected);
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        animationRecordingProperties |= prop;
                    else
                        animationRecordingProperties &= ~prop;
                }
            }
            
            if (!useAnimationPreset)
            {
                EditorGUILayout.Space(5);
                animationInterpolationMode = (AnimationInterpolationMode)EditorGUILayout.EnumPopup("Interpolation:", animationInterpolationMode);
                animationCompressionLevel = (AnimationCompressionLevel)EditorGUILayout.EnumPopup("Compression:", animationCompressionLevel);
            }
        }
        
        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // File Name field (Unity Recorder standard)
            EditorGUILayout.BeginHorizontal();
            outputFile = EditorGUILayout.TextField("File Name:", outputFile);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string basePath = WildcardProcessor.ExtractBasePath(outputFile);
                string fileName = WildcardProcessor.ExtractFileName(outputFile);
                
                string path = EditorUtility.SaveFolderPanel("Select Output Folder", basePath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    string relativePath = Path.GetRelativePath(Application.dataPath + "/..", path);
                    outputFile = string.IsNullOrEmpty(fileName) ? relativePath : $"{relativePath}/{fileName}";
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Take number field (separate for easy incrementing)
            takeNumber = EditorGUILayout.IntField("Take:", takeNumber);
            takeNumber = Mathf.Max(1, takeNumber);
            
            // Wildcards help
            EditorGUILayout.HelpBox(
                "Wildcards: <Take>, <Scene>, <Frame>, <Time>, <Date>, <Resolution>\n" +
                "Example: Recordings/<Scene>_<Take>/<Scene>_<Frame>",
                MessageType.Info
            );
            
            // Preview output path with wildcards resolved
            if (!string.IsNullOrEmpty(outputFile))
            {
                // Create wildcard context
                var context = new WildcardContext(takeNumber, width, height);
                
                // Validate template
                string validationError;
                if (!WildcardProcessor.ValidateTemplate(outputFile, out validationError))
                {
                    EditorGUILayout.HelpBox(validationError, MessageType.Error);
                }
                else
                {
                    // Process wildcards for preview
                    string processedPath = WildcardProcessor.ProcessWildcards(outputFile, context);
                    
                    // Add file extension based on recorder type
                    string extension = GetFileExtension();
                    if (!string.IsNullOrEmpty(extension))
                    {
                        processedPath += $".{extension}";
                    }
                    
                    // Special handling for image sequences and AOV
                    if (recorderType == RecorderSettingsType.Image)
                    {
                        processedPath = processedPath.Replace("0001", "<Frame>");
                    }
                    else if (recorderType == RecorderSettingsType.AOV)
                    {
                        int aovCount = System.Linq.Enumerable.Cast<AOVType>(System.Enum.GetValues(typeof(AOVType)))
                            .Count(t => t != AOVType.None && (selectedAOVTypes & t) != 0);
                        processedPath += $" ({aovCount} AOV sequences)";
                    }
                    
                    EditorGUILayout.HelpBox($"Output: {processedPath}", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = currentState == RenderState.Idle && availableDirectors.Count > 0 && !EditorApplication.isPlaying && RecorderSettingsFactory.IsRecorderTypeSupported(recorderType);
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
        
        private void ScanTimelines()
        {
            availableDirectors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsOfType<PlayableDirector>();
            
            foreach (var director in allDirectors)
            {
                if (director != null && director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    availableDirectors.Add(director);
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
        }
        
        private void StartRendering()
        {
            Debug.Log("[SingleTimelineRenderer] StartRendering called");
            
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
            Debug.Log("[SingleTimelineRenderer] RenderTimelineCoroutine started");
            
            currentState = RenderState.Preparing;
            statusMessage = "Preparing...";
            renderProgress = 0f;
            
            var selectedDirector = availableDirectors[selectedDirectorIndex];
            if (selectedDirector == null || selectedDirector.gameObject == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected director is null or destroyed";
                Debug.LogError("[SingleTimelineRenderer] Selected director is null or destroyed");
                yield break;
            }
            
            var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
            
            Debug.Log($"[SingleTimelineRenderer] Selected director: {selectedDirector?.gameObject.name}");
            
            if (originalTimeline == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected asset is not a Timeline";
                Debug.LogError("[SingleTimelineRenderer] Selected asset is not a Timeline");
                yield break;
            }
            
            // Store original Play On Awake setting and disable it
            bool originalPlayOnAwake = selectedDirector.playOnAwake;
            selectedDirector.playOnAwake = false;
            
            // Store director info for later use in Play Mode
            string directorName = selectedDirector.gameObject.name;
            float timelineDuration = (float)originalTimeline.duration;
            
            Debug.Log($"[SingleTimelineRenderer] Timeline duration: {timelineDuration}, PlayOnAwake was: {originalPlayOnAwake}");
            
            // Create render timeline BEFORE entering Play Mode
            currentState = RenderState.Preparing;
            statusMessage = "Creating render timeline...";
            
            try
            {
                renderTimeline = CreateRenderTimeline(selectedDirector, originalTimeline);
                if (renderTimeline == null)
                {
                    currentState = RenderState.Error;
                    statusMessage = "Failed to create render timeline";
                    Debug.LogError("[SingleTimelineRenderer] Failed to create render timeline");
                    selectedDirector.playOnAwake = originalPlayOnAwake;
                    yield break;
                }
                
                Debug.Log($"[SingleTimelineRenderer] Created render timeline at: {tempAssetPath}");
            }
            catch (System.Exception e)
            {
                currentState = RenderState.Error;
                statusMessage = $"Error creating timeline: {e.Message}";
                Debug.LogError($"[SingleTimelineRenderer] Error creating timeline: {e}");
                selectedDirector.playOnAwake = originalPlayOnAwake;
                yield break;
            }
            
            // Force save assets before entering Play Mode
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Wait a bit to ensure asset is saved
            yield return new WaitForSeconds(0.1f);
            
            // Enter Play Mode
            currentState = RenderState.WaitingForPlayMode;
            statusMessage = "Starting Unity Play Mode...";
            
            Debug.Log($"[SingleTimelineRenderer] Current Play Mode state: {EditorApplication.isPlaying}");
            
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[SingleTimelineRenderer] Entering Play Mode...");
                // Store necessary data for Play Mode
                EditorPrefs.SetString("STR_DirectorName", directorName);
                EditorPrefs.SetString("STR_TempAssetPath", tempAssetPath);
                EditorPrefs.SetFloat("STR_Duration", timelineDuration);
                EditorPrefs.SetBool("STR_IsRendering", true);
                EditorPrefs.SetInt("STR_TakeNumber", takeNumber);
                EditorPrefs.SetString("STR_OutputFile", outputFile);
                EditorPrefs.SetInt("STR_RecorderType", (int)recorderType);
                
                EditorApplication.isPlaying = true;
                // The coroutine will be interrupted here, but OnEditorUpdate will continue in Play Mode
            }
        }
        
        private TimelineAsset CreateRenderTimeline(PlayableDirector originalDirector, TimelineAsset originalTimeline)
        {
            Debug.Log($"[SingleTimelineRenderer] CreateRenderTimeline started - Director: {originalDirector.gameObject.name}, Timeline: {originalTimeline.name}");
            
            // Create timeline
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            if (timeline == null)
            {
                Debug.LogError("[SingleTimelineRenderer] Failed to create TimelineAsset instance");
                return null;
            }
            timeline.name = $"{originalDirector.gameObject.name}_RenderTimeline";
            timeline.editorSettings.frameRate = frameRate;
            Debug.Log($"[SingleTimelineRenderer] Created TimelineAsset: {timeline.name}, frameRate: {frameRate}");
            
            // Save as temporary asset
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (!AssetDatabase.IsValidFolder(tempDir))
            {
                Debug.Log("[SingleTimelineRenderer] Creating temp directory...");
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                {
                    AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                    Debug.Log("[SingleTimelineRenderer] Created BatchRenderingTool folder");
                }
                AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
                Debug.Log("[SingleTimelineRenderer] Created Temp folder");
            }
            
            tempAssetPath = $"{tempDir}/{timeline.name}_{System.DateTime.Now.Ticks}.playable";
            Debug.Log($"[SingleTimelineRenderer] Creating asset at: {tempAssetPath}");
            try
            {
                AssetDatabase.CreateAsset(timeline, tempAssetPath);
                Debug.Log($"[SingleTimelineRenderer] Successfully created asset at: {tempAssetPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SingleTimelineRenderer] Failed to create asset: {e.Message}");
                return null;
            }
            
            // Create control track
            Debug.Log("[SingleTimelineRenderer] Creating ControlTrack...");
            var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
            if (controlTrack == null)
            {
                Debug.LogError("[SingleTimelineRenderer] Failed to create ControlTrack");
                return null;
            }
            Debug.Log("[SingleTimelineRenderer] ControlTrack created successfully");
            
            var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
            if (controlClip == null)
            {
                Debug.LogError("[SingleTimelineRenderer] Failed to create ControlClip");
                return null;
            }
            Debug.Log("[SingleTimelineRenderer] ControlClip created successfully");
            controlClip.displayName = originalDirector.gameObject.name;
            controlClip.start = 0;
            controlClip.duration = originalTimeline.duration;
            
            var controlAsset = controlClip.asset as ControlPlayableAsset;
            
            // CRITICAL: Set up the exposed name for runtime binding
            string exposedName = UnityEditor.GUID.Generate().ToString();
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
            var processedFileName = WildcardProcessor.ProcessWildcards(outputFile, context);
            List<RecorderSettings> recorderSettingsList = new List<RecorderSettings>();
            
            Debug.Log($"[SingleTimelineRenderer] Creating recorder settings for type: {recorderType}");
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    Debug.Log("[SingleTimelineRenderer] Creating ImageRecorderSettings...");
                    var imageSettings = CreateImageRecorderSettings(processedFileName);
                    if (imageSettings != null)
                    {
                        recorderSettingsList.Add(imageSettings);
                        Debug.Log($"[SingleTimelineRenderer] ImageRecorderSettings created: {imageSettings.GetType().Name}");
                    }
                    else
                    {
                        Debug.LogError("[SingleTimelineRenderer] CreateImageRecorderSettings returned null");
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    var movieSettings = CreateMovieRecorderSettings(processedFileName);
                    if (movieSettings != null) recorderSettingsList.Add(movieSettings);
                    break;
                    
                case RecorderSettingsType.AOV:
                    var aovSettingsList = CreateAOVRecorderSettings(processedFileName);
                    if (aovSettingsList != null) recorderSettingsList.AddRange(aovSettingsList);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    var alembicSettings = CreateAlembicRecorderSettings(processedFileName);
                    if (alembicSettings != null) recorderSettingsList.Add(alembicSettings);
                    break;
                    
                case RecorderSettingsType.Animation:
                    var animationSettings = CreateAnimationRecorderSettings(processedFileName);
                    if (animationSettings != null) recorderSettingsList.Add(animationSettings);
                    break;
                    
                default:
                    Debug.LogError($"[SingleTimelineRenderer] Unsupported recorder type: {recorderType}");
                    return null;
            }
            
            if (recorderSettingsList.Count == 0)
            {
                Debug.LogError($"[SingleTimelineRenderer] Failed to create recorder settings for type: {recorderType}");
                return null;
            }
            Debug.Log($"[SingleTimelineRenderer] Created {recorderSettingsList.Count} recorder settings");
            
            // For AOV, we might have multiple settings, but for now use the first one for the main recorder track
            RecorderSettings recorderSettings = recorderSettingsList[0];
            
            // Save all recorder settings as sub-assets
            foreach (var settings in recorderSettingsList)
            {
                AssetDatabase.AddObjectToAsset(settings, timeline);
            }
            
            // Create recorder track and clip
            Debug.Log("[SingleTimelineRenderer] Creating RecorderTrack...");
            var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Recorder Track");
            if (recorderTrack == null)
            {
                Debug.LogError("[SingleTimelineRenderer] Failed to create RecorderTrack");
                return null;
            }
            Debug.Log("[SingleTimelineRenderer] RecorderTrack created successfully");
            var recorderClip = recorderTrack.CreateClip<RecorderClip>();
            recorderClip.displayName = $"Record {originalDirector.gameObject.name}";
            recorderClip.start = 0;
            recorderClip.duration = originalTimeline.duration;
            
            var recorderAsset = recorderClip.asset as RecorderClip;
            recorderAsset.settings = recorderSettings;
            
            // Use RecorderClipUtility to ensure proper initialization
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            // Save everything including ControlTrack settings
            EditorUtility.SetDirty(controlAsset);
            EditorUtility.SetDirty(recorderAsset);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Log for debugging
            Debug.Log($"[SingleTimelineRenderer] Created timeline with ControlTrack, exposed name: {exposedName}");
            
            return timeline;
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
            
            if (!string.IsNullOrEmpty(tempAssetPath) && AssetDatabase.LoadAssetAtPath<Object>(tempAssetPath) != null)
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
                    
                default:
                    return "";
            }
        }
        
        private void OnDestroy()
        {
            StopRendering();
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            // Continue rendering process in Play Mode
            if (EditorApplication.isPlaying && currentState == RenderState.WaitingForPlayMode)
            {
                if (EditorPrefs.GetBool("STR_IsRendering", false))
                {
                    EditorPrefs.SetBool("STR_IsRendering", false);
                    
                    // Start the Play Mode rendering process
                    if (renderCoroutine == null)
                    {
                        renderCoroutine = EditorCoroutineUtility.StartCoroutine(ContinueRenderingInPlayMode(), this);
                    }
                }
            }
        }
        
        private IEnumerator ContinueRenderingInPlayMode()
        {
            Debug.Log("[SingleTimelineRenderer] Continuing rendering in Play Mode");
            
            currentState = RenderState.Rendering;
            statusMessage = "Setting up rendering in Play Mode...";
            
            // Retrieve stored data
            string directorName = EditorPrefs.GetString("STR_DirectorName", "");
            string storedTempPath = EditorPrefs.GetString("STR_TempAssetPath", "");
            float timelineDuration = EditorPrefs.GetFloat("STR_Duration", 0f);
            int storedTakeNumber = EditorPrefs.GetInt("STR_TakeNumber", 1);
            string storedOutputFile = EditorPrefs.GetString("STR_OutputFile", "");
            RecorderSettingsType storedRecorderType = (RecorderSettingsType)EditorPrefs.GetInt("STR_RecorderType", 0);
            
            // Retrieve exposed name
            string exposedName = EditorPrefs.GetString("STR_ExposedName", "");
            
            // Use stored values
            takeNumber = storedTakeNumber;
            outputFile = storedOutputFile;
            recorderType = storedRecorderType;
            
            // Store original background execution setting
            bool originalRunInBackground = Application.runInBackground;
            int originalCaptureFramerate = Time.captureFramerate;
            
            // Enable background execution to prevent stopping when focus is lost
            Application.runInBackground = true;
            Time.captureFramerate = frameRate;
            Debug.Log($"[SingleTimelineRenderer] Enabled background execution. CaptureFramerate: {frameRate}");
            
            // Clean up EditorPrefs
            EditorPrefs.DeleteKey("STR_DirectorName");
            EditorPrefs.DeleteKey("STR_TempAssetPath");
            EditorPrefs.DeleteKey("STR_Duration");
            EditorPrefs.DeleteKey("STR_ExposedName");
            EditorPrefs.DeleteKey("STR_TakeNumber");
            EditorPrefs.DeleteKey("STR_OutputFile");
            EditorPrefs.DeleteKey("STR_RecorderType");
            
            // Wait a frame for Play Mode to fully initialize
            yield return null;
            yield return null; // Extra frame for safety
            
            // Find the director in Play Mode
            statusMessage = "Finding director in Play Mode...";
            Debug.Log($"[SingleTimelineRenderer] Looking for director: {directorName}");
            
            // Find the target director in Play Mode
            var targetDirector = GameObject.Find(directorName)?.GetComponent<PlayableDirector>();
            
            if (targetDirector == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Failed to find target director in Play Mode";
                Debug.LogError($"[SingleTimelineRenderer] Failed to find director: {directorName}");
                EditorApplication.isPlaying = false;
                yield break;
            }
            
            Debug.Log($"[SingleTimelineRenderer] Found target director: {targetDirector.gameObject.name}");
            
            // Verify timeline asset
            var targetTimeline = targetDirector.playableAsset as TimelineAsset;
            if (targetTimeline == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Target director's playable asset is not a Timeline";
                Debug.LogError("[SingleTimelineRenderer] Target director's playable asset is not a Timeline");
                EditorApplication.isPlaying = false;
                yield break;
            }
            
            bool hasError = false;
            string errorMessage = "";
            
            try
            {
                // Load the pre-created render timeline
                renderTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(storedTempPath);
                if (renderTimeline == null)
                {
                    hasError = true;
                    errorMessage = "Failed to load render timeline";
                    Debug.LogError($"[SingleTimelineRenderer] Failed to load render timeline from: {storedTempPath}");
                }
                
                if (!hasError)
                {
                    Debug.Log($"[SingleTimelineRenderer] Loaded render timeline: {renderTimeline.name}");
                    
                    // Create rendering GameObject and Director
                    renderingGameObject = new GameObject($"{directorName}_Renderer");
                    renderingDirector = renderingGameObject.AddComponent<PlayableDirector>();
                    renderingDirector.playableAsset = renderTimeline;
                    renderingDirector.playOnAwake = false;
                    
                    // Set bindings for all tracks
                    foreach (var output in renderTimeline.outputs)
                    {
                        if (output.sourceObject is ControlTrack track)
                        {
                            // Bind the ControlTrack to the target director's GameObject
                            renderingDirector.SetGenericBinding(track, targetDirector.gameObject);
                            Debug.Log($"[SingleTimelineRenderer] Set ControlTrack binding to {targetDirector.gameObject.name}");
                            
                            // Also ensure the control clips are properly configured
                            foreach (var clip in track.GetClips())
                            {
                                var clipAsset = clip.asset as ControlPlayableAsset;
                                if (clipAsset != null)
                                {
                                    // Use the stored exposed name if available
                                    string clipExposedName = clipAsset.sourceGameObject.exposedName.ToString();
                                    if (string.IsNullOrEmpty(clipExposedName) && !string.IsNullOrEmpty(exposedName))
                                    {
                                        clipExposedName = exposedName;
                                    }
                                    
                                    if (!string.IsNullOrEmpty(clipExposedName))
                                    {
                                        // Set the reference value for exposed properties
                                        renderingDirector.SetReferenceValue(clipExposedName, targetDirector.gameObject);
                                        Debug.Log($"[SingleTimelineRenderer] Set reference value for exposed name: {clipExposedName}");
                                    }
                                }
                            }
                        }
                    }
                    
                    // Force rebuild the playable graph to ensure bindings are applied
                    renderingDirector.RebuildGraph();
                    
                    // Verify the binding
                    var controlTrack = System.Linq.Enumerable.FirstOrDefault(
                        System.Linq.Enumerable.OfType<ControlTrack>(renderTimeline.GetOutputTracks()));
                    if (controlTrack != null)
                    {
                        var boundObject = renderingDirector.GetGenericBinding(controlTrack);
                        if (boundObject != null)
                        {
                            Debug.Log($"[SingleTimelineRenderer] Verified ControlTrack is bound to: {(boundObject as GameObject)?.name}");
                        }
                        else
                        {
                            Debug.LogWarning("[SingleTimelineRenderer] ControlTrack binding verification failed!");
                        }
                    }
                    
                    // Start timeline playback
                    statusMessage = "Rendering in progress...";
                    renderingDirector.time = 0;
                    renderingDirector.Play();
                }
            }
            catch (System.Exception e)
            {
                hasError = true;
                errorMessage = e.Message;
            }
            
            if (hasError)
            {
                currentState = RenderState.Error;
                statusMessage = $"Error: {errorMessage}";
                Debug.LogError($"[SingleTimelineRenderer] Error in Play Mode: {errorMessage}");
                EditorApplication.isPlaying = false;
                yield break;
            }
            
            // Monitor progress with timeout
            float renderTimeout = timelineDuration + 10.0f; // Duration + 10 seconds buffer
            float renderTime = 0;
            
            while (renderingDirector != null && renderingDirector.state == PlayState.Playing && renderTime < renderTimeout)
            {
                // Check if Unity Editor is paused and unpause it
                if (EditorApplication.isPaused)
                {
                    Debug.LogWarning("[SingleTimelineRenderer] Editor was paused, unpausing...");
                    EditorApplication.isPaused = false;
                }
                
                renderProgress = (float)(renderingDirector.time / renderingDirector.duration);
                renderTime += Time.deltaTime;
                yield return null;
            }
            
            if (renderTime >= renderTimeout)
            {
                hasError = true;
                errorMessage = $"Rendering timeout after {renderTime} seconds";
                Debug.LogWarning($"[SingleTimelineRenderer] {errorMessage}");
            }
            
            // Complete
            if (!hasError && currentState == RenderState.Rendering)
            {
                currentState = RenderState.Complete;
                
                // Create appropriate completion message based on recorder type
                var context = new WildcardContext(takeNumber, width, height);
                string processedPath = WildcardProcessor.ProcessWildcards(outputFile, context);
                string extension = GetFileExtension();
                
                string outputInfo = $"Output saved to: {processedPath}.{extension}";
                
                if (recorderType == RecorderSettingsType.AOV)
                {
                    int aovCount = System.Linq.Enumerable.Cast<AOVType>(System.Enum.GetValues(typeof(AOVType)))
                        .Count(t => t != AOVType.None && (selectedAOVTypes & t) != 0);
                    outputInfo = $"{aovCount} AOV sequences saved to: {processedPath}";
                }
                
                statusMessage = $"Rendering complete! {outputInfo}";
                renderProgress = 1f;
                
                Debug.Log($"[SingleTimelineRenderer] Rendering complete for {directorName}");
            }
            
            // Restore original settings
            Application.runInBackground = originalRunInBackground;
            Time.captureFramerate = originalCaptureFramerate;
            Debug.Log("[SingleTimelineRenderer] Restored original background execution settings");
            
            // Exit Play Mode
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            
            // Wait for Play Mode to exit
            while (EditorApplication.isPlaying)
            {
                yield return null;
            }
            
            // Wait a bit before cleanup
            yield return new WaitForSeconds(0.5f);
            CleanupRendering();
        }
        
        private void Update()
        {
            if (currentState == RenderState.Rendering && renderingDirector != null)
            {
                Repaint();
            }
        }
        
        // Public properties for testing
        public string OutputFile => outputFile;
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
            
            if (width <= 0 || height <= 0)
            {
                errorMessage = "Invalid output resolution";
                return false;
            }
            
            if (frameRate <= 0)
            {
                errorMessage = "Invalid frame rate";
                return false;
            }
            
            if (string.IsNullOrEmpty(outputFile))
            {
                errorMessage = "Output file template is empty";
                return false;
            }
            
            // Validate file template
            string templateError;
            if (!WildcardProcessor.ValidateTemplate(outputFile, out templateError))
            {
                errorMessage = templateError;
                return false;
            }
            
            return true;
        }
        
        private RecorderSettings CreateImageRecorderSettings(string outputFile)
        {
            Debug.Log($"[SingleTimelineRenderer] CreateImageRecorderSettings called with outputFile: {outputFile}");
            var settings = RecorderClipUtility.CreateProperImageRecorderSettings("ImageRecorder");
            if (settings == null)
            {
                Debug.LogError("[SingleTimelineRenderer] RecorderClipUtility.CreateProperImageRecorderSettings returned null");
                return null;
            }
            Debug.Log($"[SingleTimelineRenderer] Created settings of type: {settings.GetType().FullName}");
            settings.Enabled = true;
            settings.OutputFormat = imageOutputFormat;
            settings.CaptureAlpha = imageCaptureAlpha;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
            settings.FrameRate = frameRate;
            settings.CapFrameRate = true;
            
            // Configure output path
            Debug.Log($"[SingleTimelineRenderer] Configuring output path: {outputFile}");
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputFile, RecorderSettingsType.Image);
            Debug.Log($"[SingleTimelineRenderer] Output path configured successfully");
            
            Debug.Log($"[SingleTimelineRenderer] Setting image input settings: {width}x{height}");
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
            };
            
            Debug.Log("[SingleTimelineRenderer] ImageRecorderSettings created successfully");
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettings(string outputFile)
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
                    Debug.LogError($"[SingleTimelineRenderer] Invalid movie configuration: {errorMessage}");
                    return null;
                }
                
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings("MovieRecorder", config);
            }
            
            settings.Enabled = true;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            
            // Configure output path
            RecorderSettingsHelper.ConfigureOutputPath(settings, outputFile, RecorderSettingsType.Movie);
            
            return settings;
        }
        
        private List<RecorderSettings> CreateAOVRecorderSettings(string outputFile)
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
                Debug.LogError($"[SingleTimelineRenderer] Invalid AOV configuration: {errorMessage}");
                return null;
            }
            
            var settingsList = RecorderSettingsFactory.CreateAOVRecorderSettings("AOVRecorder", config);
            
            // Configure output path for each AOV setting
            foreach (var settings in settingsList)
            {
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputFile, RecorderSettingsType.AOV);
            }
            
            return settingsList;
        }
        
        private RecorderSettings CreateAlembicRecorderSettings(string outputFile)
        {
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
            }
            
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                Debug.LogError($"[SingleTimelineRenderer] Invalid Alembic configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings("AlembicRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputFile, RecorderSettingsType.Alembic);
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAnimationRecorderSettings(string outputFile)
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
                Debug.LogError($"[SingleTimelineRenderer] Invalid Animation configuration: {errorMessage}");
                return null;
            }
            
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings("AnimationRecorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                RecorderSettingsHelper.ConfigureOutputPath(settings, outputFile, RecorderSettingsType.Animation);
            }
            
            return settings;
        }
        
    }
}