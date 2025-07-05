using System.Collections;
using System.Collections.Generic;
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
    /// Multi Timeline Renderer - Renders multiple timelines sequentially with advanced UI
    /// </summary>
    public class MultiTimelineRenderer : EditorWindow, IRecorderSettingsHost
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

        public enum ErrorHandling
        {
            StopOnError,
            ContinueOnError
        }

        [System.Serializable]
        public class TimelineRenderItem
        {
            public PlayableDirector director;
            public bool selected = true;
            public string outputFolderName;
            public RenderState status = RenderState.Idle;
            public string statusMessage = "";
            public float progress = 0f;
        }

        // UI State
        private RenderState currentState = RenderState.Idle;
        private string globalStatusMessage = "Ready to render";
        private float globalProgress = 0f;
        
        // Timeline selection
        private List<TimelineRenderItem> renderItems = new List<TimelineRenderItem>();
        private Vector2 timelineScrollPosition;
        
        // Common render settings
        private RecorderSettingsType recorderType = RecorderSettingsType.Image;
        private RecorderSettingsType previousRecorderType = RecorderSettingsType.Image;
        public int frameRate = 24;
        public int width = 1920;
        public int height = 1080;
        private string outputPath = "Recordings";
        private ErrorHandling errorHandling = ErrorHandling.StopOnError;
        
        // Image recorder settings
        public ImageRecorderSettings.ImageRecorderOutputFormat imageOutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        public bool imageCaptureAlpha = false;
        public int jpegQuality = 75;
        public ImageRecorderSettings.EXRCompressionType exrCompression = ImageRecorderSettings.EXRCompressionType.None;
        
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
        public AnimationRecordingScope animationRecordingScope = AnimationRecordingScope.SingleGameObject;
        public GameObject animationTargetGameObject = null;
        public AnimationInterpolationMode animationInterpolationMode = AnimationInterpolationMode.Linear;
        public AnimationCompressionLevel animationCompressionLevel = AnimationCompressionLevel.Medium;
        public AnimationRecordingType animationRecordingType = AnimationRecordingType.TransformOnly;
        public bool animationIncludeChildren = true;
        public bool animationClampedTangents = true;
        public bool animationRecordBlendShapes = false;
        public AnimationCompression animationCompression = AnimationCompression.Off;
        public float animationPositionError = 0.5f;
        public float animationRotationError = 0.5f;
        public float animationScaleError = 0.5f;
        public AnimationRecordingPreset animationPreset = AnimationRecordingPreset.TransformHierarchy;
        public bool useAnimationPreset = false;
        
        // Rendering state
        private int currentRenderIndex = -1;
        private int totalRenderCount = 0;
        private int completedRenderCount = 0;
        private int errorRenderCount = 0;
        
        // Rendering objects (reused from SingleTimelineRenderer)
        private TimelineAsset renderTimeline;
        private GameObject renderingGameObject;
        private PlayableDirector renderingDirector;
        private EditorCoroutine renderCoroutine;
        private string tempAssetPath;
        
        // UI helpers
        private bool showAdvancedSettings = false;
        
        // UI Editor instances
        private RecorderSettingsEditorBase currentRecorderEditor;
        
        // File name with wildcard support
        public string fileName = "Recordings/<Scene>_<Take>";
        public int takeNumber = 1;
        
        [MenuItem("Window/Batch Rendering Tool/Multi Timeline Renderer")]
        public static MultiTimelineRenderer ShowWindow()
        {
            var window = GetWindow<MultiTimelineRenderer>();
            window.titleContent = new GUIContent("Multi Timeline Renderer");
            window.minSize = new Vector2(500, 600);
            return window;
        }
        
        private void OnEnable()
        {
            ScanTimelines();
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Multi Timeline Renderer", EditorStyles.boldLabel);
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
            
            // Header with controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Available Timelines: {renderItems.Count}");
            
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                foreach (var item in renderItems)
                    item.selected = true;
            }
            
            if (GUILayout.Button("Deselect All", GUILayout.Width(80)))
            {
                foreach (var item in renderItems)
                    item.selected = false;
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                ScanTimelines();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Timeline list with checkboxes
            if (renderItems.Count > 0)
            {
                timelineScrollPosition = EditorGUILayout.BeginScrollView(timelineScrollPosition, GUILayout.Height(200));
                
                for (int i = 0; i < renderItems.Count; i++)
                {
                    var item = renderItems[i];
                    EditorGUILayout.BeginHorizontal();
                    
                    // Checkbox
                    item.selected = EditorGUILayout.Toggle(item.selected, GUILayout.Width(20));
                    
                    // Timeline name
                    string displayName = item.director != null ? item.director.gameObject.name : "Missing";
                    EditorGUILayout.LabelField(displayName, GUILayout.Width(200));
                    
                    // Duration
                    if (item.director != null && item.director.playableAsset is TimelineAsset timeline)
                    {
                        EditorGUILayout.LabelField($"{timeline.duration:F2}s", GUILayout.Width(60));
                    }
                    
                    // Status indicator
                    if (item.status != RenderState.Idle)
                    {
                        Color originalColor = GUI.color;
                        switch (item.status)
                        {
                            case RenderState.Complete:
                                GUI.color = Color.green;
                                break;
                            case RenderState.Error:
                                GUI.color = Color.red;
                                break;
                            case RenderState.Rendering:
                                GUI.color = Color.yellow;
                                break;
                        }
                        EditorGUILayout.LabelField(item.status.ToString(), GUILayout.Width(80));
                        GUI.color = originalColor;
                    }
                    
                    // Move up/down buttons
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        var temp = renderItems[i];
                        renderItems[i] = renderItems[i - 1];
                        renderItems[i - 1] = temp;
                    }
                    
                    GUI.enabled = i < renderItems.Count - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        var temp = renderItems[i];
                        renderItems[i] = renderItems[i + 1];
                        renderItems[i + 1] = temp;
                    }
                    
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Progress bar for rendering items
                    if (item.status == RenderState.Rendering && item.progress > 0)
                    {
                        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(15)), 
                                            item.progress, $"{(int)(item.progress * 100)}%");
                    }
                }
                
                EditorGUILayout.EndScrollView();
                
                // Summary
                int selectedCount = renderItems.Count(item => item.selected);
                EditorGUILayout.HelpBox($"Selected: {selectedCount} timelines", MessageType.Info);
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
                
                // Update file name template when recorder type changes
                fileName = WildcardProcessor.GetDefaultTemplate(recorderType);
                
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
            
            EditorGUILayout.Space(5);
            
            // Advanced settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                errorHandling = (ErrorHandling)EditorGUILayout.EnumPopup("Error Handling:", errorHandling);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
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
                _ => null
            };
        }
        
        // IRecorderSettingsHost implementation
        public PlayableDirector selectedDirector
        {
            get
            {
                // Return the first selected director for recorder settings
                var selectedItem = renderItems.FirstOrDefault(item => item.selected && item.director != null);
                return selectedItem?.director;
            }
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool hasSelected = renderItems.Any(item => item.selected);
            GUI.enabled = currentState == RenderState.Idle && hasSelected && !EditorApplication.isPlaying && RecorderSettingsFactory.IsRecorderTypeSupported(recorderType);
            
            if (GUILayout.Button("Start Batch Rendering", GUILayout.Height(30)))
            {
                StartBatchRendering();
            }
            
            GUI.enabled = currentState != RenderState.Idle || EditorApplication.isPlaying;
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
            
            // Global status
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
            
            EditorGUILayout.LabelField($"Message: {globalStatusMessage}");
            
            // Progress info
            if (currentState == RenderState.Rendering || currentState == RenderState.Complete)
            {
                EditorGUILayout.LabelField($"Progress: {completedRenderCount}/{totalRenderCount} completed, {errorRenderCount} errors");
                
                // Global progress bar
                float progress = totalRenderCount > 0 ? (float)completedRenderCount / totalRenderCount : 0f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), 
                                    progress, $"Total Progress: {(int)(progress * 100)}%");
                
                // Current timeline info
                if (currentRenderIndex >= 0 && currentRenderIndex < renderItems.Count)
                {
                    var currentItem = renderItems[currentRenderIndex];
                    if (currentItem.director != null)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"Currently rendering: {currentItem.director.gameObject.name}");
                        
                        if (renderingDirector != null)
                        {
                            EditorGUILayout.LabelField($"Timeline progress: {renderingDirector.time:F2}/{renderingDirector.duration:F2}");
                        }
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ScanTimelines()
        {
            renderItems.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsOfType<PlayableDirector>();
            
            foreach (var director in allDirectors)
            {
                if (director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    var item = new TimelineRenderItem
                    {
                        director = director,
                        selected = true,
                        outputFolderName = SanitizeFileName(director.gameObject.name),
                        status = RenderState.Idle,
                        statusMessage = "",
                        progress = 0f
                    };
                    renderItems.Add(item);
                }
            }
            
            renderItems.Sort((a, b) => a.director.gameObject.name.CompareTo(b.director.gameObject.name));
        }
        
        private void StartBatchRendering()
        {
            Debug.Log("[MultiTimelineRenderer] Starting batch rendering");
            
            // Reset states
            currentRenderIndex = -1;
            completedRenderCount = 0;
            errorRenderCount = 0;
            totalRenderCount = renderItems.Count(item => item.selected);
            
            // Reset item states
            foreach (var item in renderItems)
            {
                if (item.selected)
                {
                    item.status = RenderState.Idle;
                    item.statusMessage = "";
                    item.progress = 0f;
                }
            }
            
            if (renderCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(renderCoroutine);
            }
            
            renderCoroutine = EditorCoroutineUtility.StartCoroutine(BatchRenderCoroutine(), this);
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
            
            CleanupCurrentRendering();
            
            currentState = RenderState.Idle;
            globalStatusMessage = "Rendering stopped by user";
            
            // Update current item status if any
            if (currentRenderIndex >= 0 && currentRenderIndex < renderItems.Count)
            {
                var item = renderItems[currentRenderIndex];
                if (item.status == RenderState.Rendering)
                {
                    item.status = RenderState.Error;
                    item.statusMessage = "Stopped by user";
                }
            }
        }
        
        private IEnumerator BatchRenderCoroutine()
        {
            Debug.Log("[MultiTimelineRenderer] BatchRenderCoroutine started");
            
            currentState = RenderState.Preparing;
            globalStatusMessage = "Preparing batch render...";
            
            // Find selected items
            var selectedItems = renderItems.Where(item => item.selected).ToList();
            
            if (selectedItems.Count == 0)
            {
                currentState = RenderState.Error;
                globalStatusMessage = "No timelines selected";
                yield break;
            }
            
            // Disable Play On Awake for all selected directors
            Dictionary<PlayableDirector, bool> originalPlayOnAwakeSettings = new Dictionary<PlayableDirector, bool>();
            foreach (var item in selectedItems)
            {
                if (item.director != null)
                {
                    originalPlayOnAwakeSettings[item.director] = item.director.playOnAwake;
                    item.director.playOnAwake = false;
                }
            }
            
            // Enter Play Mode once
            currentState = RenderState.WaitingForPlayMode;
            globalStatusMessage = "Starting Unity Play Mode...";
            
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[MultiTimelineRenderer] Entering Play Mode...");
                
                // Store batch render data
                EditorPrefs.SetBool("MTR_IsBatchRendering", true);
                EditorPrefs.SetInt("MTR_TotalCount", selectedItems.Count);
                EditorPrefs.SetString("MTR_OutputPath", outputPath);
                EditorPrefs.SetInt("MTR_FrameRate", frameRate);
                EditorPrefs.SetInt("MTR_Width", width);
                EditorPrefs.SetInt("MTR_Height", height);
                EditorPrefs.SetString("MTR_RecorderType", recorderType.ToString());
                EditorPrefs.SetString("MTR_ImageOutputFormat", imageOutputFormat.ToString());
                EditorPrefs.SetString("MTR_MovieOutputFormat", movieOutputFormat.ToString());
                EditorPrefs.SetString("MTR_MovieQuality", movieQuality.ToString());
                EditorPrefs.SetBool("MTR_MovieCaptureAudio", movieCaptureAudio);
                EditorPrefs.SetBool("MTR_MovieCaptureAlpha", movieCaptureAlpha);
                EditorPrefs.SetBool("MTR_ImageCaptureAlpha", imageCaptureAlpha);
                EditorPrefs.SetString("MTR_MoviePreset", moviePreset.ToString());
                EditorPrefs.SetBool("MTR_UseMoviePreset", useMoviePreset);
                EditorPrefs.SetString("MTR_ErrorHandling", errorHandling.ToString());
                
                // Store AOV settings
                EditorPrefs.SetInt("MTR_SelectedAOVTypes", (int)selectedAOVTypes);
                EditorPrefs.SetString("MTR_AOVOutputFormat", aovOutputFormat.ToString());
                EditorPrefs.SetString("MTR_AOVPreset", aovPreset.ToString());
                EditorPrefs.SetBool("MTR_UseAOVPreset", useAOVPreset);
                
                // Store Alembic settings
                EditorPrefs.SetInt("MTR_AlembicExportTargets", (int)alembicExportTargets);
                EditorPrefs.SetString("MTR_AlembicExportScope", alembicExportScope.ToString());
                EditorPrefs.SetString("MTR_AlembicHandedness", alembicHandedness.ToString());
                EditorPrefs.SetFloat("MTR_AlembicScaleFactor", alembicScaleFactor);
                EditorPrefs.SetFloat("MTR_AlembicFrameRate", alembicFrameRate);
                EditorPrefs.SetBool("MTR_AlembicFlattenHierarchy", alembicFlattenHierarchy);
                EditorPrefs.SetString("MTR_AlembicPreset", alembicPreset.ToString());
                EditorPrefs.SetBool("MTR_UseAlembicPreset", useAlembicPreset);
                
                // Store Animation settings
                EditorPrefs.SetString("MTR_AnimationRecordingProperties", animationRecordingProperties.ToString());
                EditorPrefs.SetString("MTR_AnimationRecordingScope", animationRecordingScope.ToString());
                EditorPrefs.SetString("MTR_AnimationInterpolationMode", animationInterpolationMode.ToString());
                EditorPrefs.SetString("MTR_AnimationCompressionLevel", animationCompressionLevel.ToString());
                EditorPrefs.SetBool("MTR_AnimationIncludeChildren", animationIncludeChildren);
                EditorPrefs.SetBool("MTR_AnimationClampedTangents", animationClampedTangents);
                EditorPrefs.SetBool("MTR_AnimationRecordBlendShapes", animationRecordBlendShapes);
                EditorPrefs.SetString("MTR_AnimationPreset", animationPreset.ToString());
                EditorPrefs.SetBool("MTR_UseAnimationPreset", useAnimationPreset);
                
                // Store file name with wildcards
                EditorPrefs.SetString("MTR_FileName", fileName);
                EditorPrefs.SetInt("MTR_TakeNumber", takeNumber);
                
                // Store selected items
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    var item = selectedItems[i];
                    EditorPrefs.SetString($"MTR_Item_{i}_Name", item.director.gameObject.name);
                    EditorPrefs.SetString($"MTR_Item_{i}_OutputFolder", item.outputFolderName);
                }
                
                EditorApplication.isPlaying = true;
                // The coroutine will be interrupted here, but OnEditorUpdate will continue in Play Mode
            }
            
            // Restore Play On Awake settings when done
            foreach (var kvp in originalPlayOnAwakeSettings)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.playOnAwake = kvp.Value;
                }
            }
        }
        
        private void OnEditorUpdate()
        {
            // Continue batch rendering in Play Mode
            if (EditorApplication.isPlaying && currentState == RenderState.WaitingForPlayMode)
            {
                if (EditorPrefs.GetBool("MTR_IsBatchRendering", false))
                {
                    EditorPrefs.SetBool("MTR_IsBatchRendering", false);
                    
                    // Start the Play Mode batch rendering
                    if (renderCoroutine == null)
                    {
                        renderCoroutine = EditorCoroutineUtility.StartCoroutine(ContinueBatchRenderingInPlayMode(), this);
                    }
                }
            }
        }
        
        private IEnumerator ContinueBatchRenderingInPlayMode()
        {
            Debug.Log("[MultiTimelineRenderer] Continuing batch rendering in Play Mode");
            
            currentState = RenderState.Rendering;
            globalStatusMessage = "Batch rendering in progress...";
            
            // Retrieve stored data
            int totalCount = EditorPrefs.GetInt("MTR_TotalCount", 0);
            string storedOutputPath = EditorPrefs.GetString("MTR_OutputPath", "Recordings");
            int storedFrameRate = EditorPrefs.GetInt("MTR_FrameRate", 24);
            int storedWidth = EditorPrefs.GetInt("MTR_Width", 1920);
            int storedHeight = EditorPrefs.GetInt("MTR_Height", 1080);
            
            // Restore recorder type and settings
            string recorderTypeString = EditorPrefs.GetString("MTR_RecorderType", "Image");
            System.Enum.TryParse<RecorderSettingsType>(recorderTypeString, out recorderType);
            
            string imageFormatString = EditorPrefs.GetString("MTR_ImageOutputFormat", "PNG");
            System.Enum.TryParse<ImageRecorderSettings.ImageRecorderOutputFormat>(imageFormatString, out imageOutputFormat);
            
            string movieFormatString = EditorPrefs.GetString("MTR_MovieOutputFormat", "MP4");
            System.Enum.TryParse<MovieRecorderSettings.VideoRecorderOutputFormat>(movieFormatString, out movieOutputFormat);
            
            string movieQualityString = EditorPrefs.GetString("MTR_MovieQuality", "High");
            System.Enum.TryParse<VideoBitrateMode>(movieQualityString, out movieQuality);
            
            movieCaptureAudio = EditorPrefs.GetBool("MTR_MovieCaptureAudio", false);
            movieCaptureAlpha = EditorPrefs.GetBool("MTR_MovieCaptureAlpha", false);
            imageCaptureAlpha = EditorPrefs.GetBool("MTR_ImageCaptureAlpha", false);
            
            string moviePresetString = EditorPrefs.GetString("MTR_MoviePreset", "HighQuality1080p");
            System.Enum.TryParse<MovieRecorderPreset>(moviePresetString, out moviePreset);
            useMoviePreset = EditorPrefs.GetBool("MTR_UseMoviePreset", false);
            
            string errorHandlingString = EditorPrefs.GetString("MTR_ErrorHandling", "StopOnError");
            System.Enum.TryParse<ErrorHandling>(errorHandlingString, out var storedErrorHandling);
            
            // Restore AOV settings
            selectedAOVTypes = (AOVType)EditorPrefs.GetInt("MTR_SelectedAOVTypes", (int)(AOVType.Depth | AOVType.Normal | AOVType.Albedo));
            string aovOutputFormatString = EditorPrefs.GetString("MTR_AOVOutputFormat", "EXR16");
            System.Enum.TryParse<AOVOutputFormat>(aovOutputFormatString, out aovOutputFormat);
            string aovPresetString = EditorPrefs.GetString("MTR_AOVPreset", "Compositing");
            System.Enum.TryParse<AOVPreset>(aovPresetString, out aovPreset);
            useAOVPreset = EditorPrefs.GetBool("MTR_UseAOVPreset", false);
            
            // Restore Alembic settings
            alembicExportTargets = (AlembicExportTargets)EditorPrefs.GetInt("MTR_AlembicExportTargets", (int)(AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform));
            string alembicExportScopeString = EditorPrefs.GetString("MTR_AlembicExportScope", "EntireScene");
            System.Enum.TryParse<AlembicExportScope>(alembicExportScopeString, out alembicExportScope);
            string alembicHandednessString = EditorPrefs.GetString("MTR_AlembicHandedness", "Left");
            System.Enum.TryParse<AlembicHandedness>(alembicHandednessString, out alembicHandedness);
            alembicScaleFactor = EditorPrefs.GetFloat("MTR_AlembicScaleFactor", 1f);
            alembicFrameRate = EditorPrefs.GetFloat("MTR_AlembicFrameRate", 24f);
            alembicFlattenHierarchy = EditorPrefs.GetBool("MTR_AlembicFlattenHierarchy", false);
            string alembicPresetString = EditorPrefs.GetString("MTR_AlembicPreset", "AnimationExport");
            System.Enum.TryParse<AlembicExportPreset>(alembicPresetString, out alembicPreset);
            useAlembicPreset = EditorPrefs.GetBool("MTR_UseAlembicPreset", false);
            
            // Restore Animation settings
            string animationRecordingPropertiesString = EditorPrefs.GetString("MTR_AnimationRecordingProperties", "TransformOnly");
            System.Enum.TryParse<AnimationRecordingProperties>(animationRecordingPropertiesString, out animationRecordingProperties);
            string animationRecordingScopeString = EditorPrefs.GetString("MTR_AnimationRecordingScope", "SingleGameObject");
            System.Enum.TryParse<AnimationRecordingScope>(animationRecordingScopeString, out animationRecordingScope);
            string animationInterpolationModeString = EditorPrefs.GetString("MTR_AnimationInterpolationMode", "Linear");
            System.Enum.TryParse<AnimationInterpolationMode>(animationInterpolationModeString, out animationInterpolationMode);
            string animationCompressionLevelString = EditorPrefs.GetString("MTR_AnimationCompressionLevel", "Medium");
            System.Enum.TryParse<AnimationCompressionLevel>(animationCompressionLevelString, out animationCompressionLevel);
            animationIncludeChildren = EditorPrefs.GetBool("MTR_AnimationIncludeChildren", true);
            animationClampedTangents = EditorPrefs.GetBool("MTR_AnimationClampedTangents", true);
            animationRecordBlendShapes = EditorPrefs.GetBool("MTR_AnimationRecordBlendShapes", false);
            string animationPresetString = EditorPrefs.GetString("MTR_AnimationPreset", "TransformHierarchy");
            System.Enum.TryParse<AnimationRecordingPreset>(animationPresetString, out animationPreset);
            useAnimationPreset = EditorPrefs.GetBool("MTR_UseAnimationPreset", false);
            
            // Restore file name with wildcards
            fileName = EditorPrefs.GetString("MTR_FileName", "Recordings/<Scene>_<Take>");
            takeNumber = EditorPrefs.GetInt("MTR_TakeNumber", 1);
            
            // Wait for Play Mode to fully initialize
            yield return null;
            yield return null;
            
            // Process each timeline
            for (int i = 0; i < totalCount; i++)
            {
                currentRenderIndex = i;
                
                string directorName = EditorPrefs.GetString($"MTR_Item_{i}_Name", "");
                string outputFolder = EditorPrefs.GetString($"MTR_Item_{i}_OutputFolder", "");
                
                // Clean up EditorPrefs for this item
                EditorPrefs.DeleteKey($"MTR_Item_{i}_Name");
                EditorPrefs.DeleteKey($"MTR_Item_{i}_OutputFolder");
                
                if (string.IsNullOrEmpty(directorName))
                {
                    Debug.LogError($"[MultiTimelineRenderer] Missing director name for item {i}");
                    errorRenderCount++;
                    continue;
                }
                
                // Update status for current item
                var currentItem = renderItems.FirstOrDefault(item => 
                    item.director != null && item.director.gameObject.name == directorName);
                
                if (currentItem != null)
                {
                    currentItem.status = RenderState.Rendering;
                    currentItem.statusMessage = "Rendering...";
                    currentItem.progress = 0f;
                }
                
                // Find the director
                var targetDirector = GameObject.Find(directorName)?.GetComponent<PlayableDirector>();
                
                if (targetDirector == null)
                {
                    Debug.LogError($"[MultiTimelineRenderer] Failed to find director: {directorName}");
                    errorRenderCount++;
                    
                    if (currentItem != null)
                    {
                        currentItem.status = RenderState.Error;
                        currentItem.statusMessage = "Director not found";
                    }
                    
                    if (storedErrorHandling == ErrorHandling.StopOnError)
                    {
                        globalStatusMessage = $"Error: Failed to find director {directorName}";
                        break;
                    }
                    
                    continue;
                }
                
                // Render this timeline
                bool success = false;
                string errorMessage = "";
                
                // Create render timeline
                var originalTimeline = targetDirector.playableAsset as TimelineAsset;
                if (originalTimeline != null)
                {
                    bool hasException = false;
                    System.Exception caughtException = null;
                    
                    try
                    {
                        renderTimeline = CreateRenderTimeline(targetDirector, originalTimeline, 
                                                             outputFolder, storedOutputPath,
                                                             storedFrameRate, storedWidth, storedHeight);
                        
                        if (renderTimeline != null)
                        {
                            // Create rendering objects
                            renderingGameObject = new GameObject($"{directorName}_Renderer");
                            renderingDirector = renderingGameObject.AddComponent<PlayableDirector>();
                            renderingDirector.playableAsset = renderTimeline;
                            renderingDirector.playOnAwake = false;
                            
                            // Set bindings
                            SetupRenderTimelineBindings(renderingDirector, renderTimeline, targetDirector);
                            
                            // Start rendering
                            renderingDirector.time = 0;
                            renderingDirector.Play();
                        }
                    }
                    catch (System.Exception e)
                    {
                        hasException = true;
                        caughtException = e;
                        errorMessage = e.Message;
                        Debug.LogError($"[MultiTimelineRenderer] Error rendering {directorName}: {e}");
                    }
                    
                    // Process exception if any
                    if (hasException)
                    {
                        // Exception was already logged, just continue
                    }
                    else if (renderTimeline != null && renderingDirector != null)
                    {
                        // Monitor progress outside of try-catch
                        float renderTimeout = (float)originalTimeline.duration + 10.0f;
                        float renderTime = 0;
                        
                        while (renderingDirector != null && renderingDirector.state == PlayState.Playing && renderTime < renderTimeout)
                        {
                            float progress = (float)(renderingDirector.time / renderingDirector.duration);
                            
                            if (currentItem != null)
                            {
                                currentItem.progress = progress;
                            }
                            
                            renderTime += Time.deltaTime;
                            yield return null;
                        }
                        
                        if (renderTime >= renderTimeout)
                        {
                            errorMessage = "Rendering timeout";
                        }
                        else
                        {
                            success = true;
                        }
                    }
                }
                else
                {
                    errorMessage = "Invalid timeline asset";
                }
                
                // Update status
                if (success)
                {
                    completedRenderCount++;
                    if (currentItem != null)
                    {
                        currentItem.status = RenderState.Complete;
                        currentItem.statusMessage = "Completed";
                        currentItem.progress = 1f;
                    }
                }
                else
                {
                    errorRenderCount++;
                    if (currentItem != null)
                    {
                        currentItem.status = RenderState.Error;
                        currentItem.statusMessage = errorMessage;
                    }
                    
                    if (storedErrorHandling == ErrorHandling.StopOnError)
                    {
                        globalStatusMessage = $"Error rendering {directorName}: {errorMessage}";
                        break;
                    }
                }
                
                // Cleanup current rendering
                CleanupCurrentRendering();
                
                // Small delay between renders
                yield return new WaitForSeconds(0.5f);
            }
            
            // Clean up remaining EditorPrefs
            EditorPrefs.DeleteKey("MTR_TotalCount");
            EditorPrefs.DeleteKey("MTR_OutputPath");
            EditorPrefs.DeleteKey("MTR_FrameRate");
            EditorPrefs.DeleteKey("MTR_Width");
            EditorPrefs.DeleteKey("MTR_Height");
            EditorPrefs.DeleteKey("MTR_RecorderType");
            EditorPrefs.DeleteKey("MTR_ImageOutputFormat");
            EditorPrefs.DeleteKey("MTR_MovieOutputFormat");
            EditorPrefs.DeleteKey("MTR_MovieQuality");
            EditorPrefs.DeleteKey("MTR_MovieCaptureAudio");
            EditorPrefs.DeleteKey("MTR_MovieCaptureAlpha");
            EditorPrefs.DeleteKey("MTR_ImageCaptureAlpha");
            EditorPrefs.DeleteKey("MTR_MoviePreset");
            EditorPrefs.DeleteKey("MTR_UseMoviePreset");
            EditorPrefs.DeleteKey("MTR_ErrorHandling");
            
            // Update final status
            currentState = (errorRenderCount > 0 && errorHandling == ErrorHandling.StopOnError) ? 
                          RenderState.Error : RenderState.Complete;
            
            globalStatusMessage = $"Batch rendering complete! Rendered: {completedRenderCount}/{totalCount}, Errors: {errorRenderCount}";
            
            // Exit Play Mode
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }
        
        private TimelineAsset CreateRenderTimeline(PlayableDirector originalDirector, TimelineAsset originalTimeline,
                                                   string outputFolderName, string baseOutputPath,
                                                   int fps, int resWidth, int resHeight)
        {
            // Create timeline
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = $"{originalDirector.gameObject.name}_RenderTimeline";
            timeline.editorSettings.frameRate = fps;
            
            // Save as temporary asset
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (!AssetDatabase.IsValidFolder(tempDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                {
                    AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                }
                AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
            }
            
            tempAssetPath = $"{tempDir}/{timeline.name}_{System.DateTime.Now.Ticks}.playable";
            AssetDatabase.CreateAsset(timeline, tempAssetPath);
            
            // Create control track
            var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
            var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
            controlClip.displayName = originalDirector.gameObject.name;
            controlClip.start = 0;
            controlClip.duration = originalTimeline.duration;
            
            var controlAsset = controlClip.asset as ControlPlayableAsset;
            
            // Set up the exposed name for runtime binding
            string exposedName = UnityEditor.GUID.Generate().ToString();
            controlAsset.sourceGameObject.exposedName = exposedName;
            controlAsset.sourceGameObject.defaultValue = originalDirector.gameObject;
            
            // Configure control asset properties
            controlAsset.updateDirector = true;
            controlAsset.updateParticle = true;
            controlAsset.updateITimeControl = true;
            controlAsset.searchHierarchy = false;
            controlAsset.active = true;
            controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
            
            // Create recorder settings based on type
            RecorderSettings recorderSettings = null;
            
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    recorderSettings = CreateImageRecorderSettingsForBatch(outputFolderName, fps, resWidth, resHeight);
                    break;
                    
                case RecorderSettingsType.Movie:
                    recorderSettings = CreateMovieRecorderSettingsForBatch(outputFolderName, fps, resWidth, resHeight);
                    break;
                    
                case RecorderSettingsType.AOV:
                    // For AOV, we need to handle multiple recorder settings (one per AOV type)
                    // For now, we'll create a placeholder and handle the multi-AOV case later
                    recorderSettings = CreateAOVRecorderSettingsForBatch(outputFolderName, fps, resWidth, resHeight);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    recorderSettings = CreateAlembicRecorderSettingsForBatch(outputFolderName, fps, resWidth, resHeight);
                    break;
                    
                case RecorderSettingsType.Animation:
                    recorderSettings = CreateAnimationRecorderSettingsForBatch(outputFolderName, fps, resWidth, resHeight);
                    break;
                    
                default:
                    Debug.LogError($"[MultiTimelineRenderer] Unsupported recorder type: {recorderType}");
                    return null;
            }
            
            if (recorderSettings == null)
            {
                Debug.LogError("[MultiTimelineRenderer] Failed to create recorder settings");
                return null;
            }
            
            // Configure output path
            string outputFile = $"{baseOutputPath}/{outputFolderName}/{outputFolderName}";
            RecorderSettingsHelper.ConfigureOutputPath(recorderSettings, outputFile, recorderType);
            
            // Save recorder settings as sub-asset
            AssetDatabase.AddObjectToAsset(recorderSettings, timeline);
            
            // Create recorder track and clip
            var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Recorder Track");
            var recorderClip = recorderTrack.CreateClip<RecorderClip>();
            recorderClip.displayName = $"Record {originalDirector.gameObject.name}";
            recorderClip.start = 0;
            recorderClip.duration = originalTimeline.duration;
            
            var recorderAsset = recorderClip.asset as RecorderClip;
            recorderAsset.settings = recorderSettings;
            
            // Use RecorderClipUtility to ensure proper initialization
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            // Save everything
            EditorUtility.SetDirty(controlAsset);
            EditorUtility.SetDirty(recorderAsset);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            
            return timeline;
        }
        
        private void SetupRenderTimelineBindings(PlayableDirector renderDirector, TimelineAsset renderTimeline, PlayableDirector targetDirector)
        {
            // Set bindings for all tracks
            foreach (var output in renderTimeline.outputs)
            {
                if (output.sourceObject is ControlTrack track)
                {
                    // Bind the ControlTrack to the target director's GameObject
                    renderDirector.SetGenericBinding(track, targetDirector.gameObject);
                    
                    // Also ensure the control clips are properly configured
                    foreach (var clip in track.GetClips())
                    {
                        var clipAsset = clip.asset as ControlPlayableAsset;
                        if (clipAsset != null)
                        {
                            string clipExposedName = clipAsset.sourceGameObject.exposedName.ToString();
                            if (!string.IsNullOrEmpty(clipExposedName))
                            {
                                // Set the reference value for exposed properties
                                renderDirector.SetReferenceValue(clipExposedName, targetDirector.gameObject);
                            }
                        }
                    }
                }
            }
            
            // Force rebuild the playable graph
            renderDirector.RebuildGraph();
        }
        
        private void CleanupCurrentRendering()
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
        
        private void OnDestroy()
        {
            StopRendering();
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void Update()
        {
            if (currentState == RenderState.Rendering)
            {
                Repaint();
            }
        }
        
        private RecorderSettings CreateImageRecorderSettingsForBatch(string name, int fps, int width, int height)
        {
            Debug.Log($"[MultiTimelineRenderer] Creating Image recorder settings: {name}");
            
            var settings = RecorderClipUtility.CreateProperImageRecorderSettings($"{name}_Recorder");
            settings.Enabled = true;
            settings.OutputFormat = imageOutputFormat;
            settings.CaptureAlpha = imageCaptureAlpha;
            settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
            settings.FrameRate = fps;
            settings.CapFrameRate = true;
            
            settings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
            };
            
            Debug.Log($"[MultiTimelineRenderer] Image settings created: {width}x{height}@{fps}fps, Format: {imageOutputFormat}");
            
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettingsForBatch(string name, int fps, int width, int height)
        {
            Debug.Log($"[MultiTimelineRenderer] Creating Movie recorder settings: {name}");
            
            MovieRecorderSettings settings = null;
            
            if (useMoviePreset && moviePreset != MovieRecorderPreset.Custom)
            {
                Debug.Log($"[MultiTimelineRenderer] Using movie preset: {moviePreset}");
                
                // Create with preset
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings($"{name}_Recorder", moviePreset);
                // Override resolution if needed
                if (moviePreset == MovieRecorderPreset.Custom)
                {
                    settings.ImageInputSettings = new GameViewInputSettings
                    {
                        OutputWidth = width,
                        OutputHeight = height
                    };
                }
            }
            else
            {
                Debug.Log("[MultiTimelineRenderer] Using custom movie settings");
                
                // Create with custom settings
                var config = new MovieRecorderSettingsConfig
                {
                    outputFormat = movieOutputFormat,
                    videoBitrateMode = movieQuality,
                    captureAudio = movieCaptureAudio,
                    captureAlpha = movieCaptureAlpha,
                    width = width,
                    height = height,
                    frameRate = fps,
                    capFrameRate = true
                };
                
                string errorMessage;
                if (!config.Validate(out errorMessage))
                {
                    Debug.LogError($"[MultiTimelineRenderer] Invalid movie configuration: {errorMessage}");
                    return null;
                }
                
                settings = RecorderSettingsFactory.CreateMovieRecorderSettings($"{name}_Recorder", config);
            }
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                Debug.Log($"[MultiTimelineRenderer] Movie settings created: {width}x{height}@{fps}fps, Format: {movieOutputFormat}");
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAOVRecorderSettingsForBatch(string name, int fps, int width, int height)
        {
            Debug.Log($"[MultiTimelineRenderer] Creating AOV recorder settings: {name}");
            
            // Check if HDRP is available
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(RecorderSettingsType.AOV))
            {
                Debug.LogError("[MultiTimelineRenderer] AOV Recorder requires HDRP package");
                return null;
            }
            
            // Create AOV configuration
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = selectedAOVTypes,
                outputFormat = aovOutputFormat,
                width = width,
                height = height,
                frameRate = fps
            };
            
            if (useAOVPreset && aovPreset != AOVPreset.Custom)
            {
                Debug.Log($"[MultiTimelineRenderer] Using AOV preset: {aovPreset}");
                
                // Get preset configuration
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
                
                // Override resolution and framerate with our settings
                config.width = width;
                config.height = height;
                config.frameRate = fps;
            }
            
            // Validate configuration
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                Debug.LogError($"[MultiTimelineRenderer] Invalid AOV configuration: {errorMessage}");
                return null;
            }
            
            // For multi-timeline renderer, we'll use the single AOV creation for now
            // TODO: In the future, this should handle multiple AOV passes
            var settings = RecorderSettingsFactory.CreateAOVRecorderSettings($"{name}_Recorder");
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                
                int aovCount = System.Linq.Enumerable.Cast<AOVType>(System.Enum.GetValues(typeof(AOVType)))
                    .Count(t => t != AOVType.None && (selectedAOVTypes & t) != 0);
                    
                Debug.Log($"[MultiTimelineRenderer] AOV settings created: {width}x{height}@{fps}fps, Format: {aovOutputFormat}, {aovCount} AOV types selected");
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAlembicRecorderSettingsForBatch(string name, int fps, int width, int height)
        {
            Debug.Log($"[MultiTimelineRenderer] Creating Alembic recorder settings: {name}");
            
            // Check if Alembic is available
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(RecorderSettingsType.Alembic))
            {
                Debug.LogError("[MultiTimelineRenderer] Alembic Recorder requires Unity Alembic package");
                return null;
            }
            
            // Create Alembic configuration
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = alembicExportTargets,
                exportScope = alembicExportScope,
                targetGameObject = alembicTargetGameObject,
                frameRate = alembicFrameRate,
                scaleFactor = alembicScaleFactor,
                handedness = alembicHandedness,
                timeSamplingMode = AlembicTimeSamplingMode.Uniform,
                samplesPerFrame = 1,
                swapYZ = false,
                flipFaces = false,
                exportUVs = true,
                exportNormals = true,
                exportVertexColors = true,
                exportVisibility = true,
                assumeUnchangedTopology = true,
                flattenHierarchy = alembicFlattenHierarchy,
                includeInactiveMeshes = false
            };
            
            if (useAlembicPreset && alembicPreset != AlembicExportPreset.Custom)
            {
                Debug.Log($"[MultiTimelineRenderer] Using Alembic preset: {alembicPreset}");
                
                // Get preset configuration
                config = AlembicRecorderSettingsConfig.GetPreset(alembicPreset);
                
                // Override framerate with our settings
                config.frameRate = alembicFrameRate;
                
                // For Multi Timeline Renderer, use entire scene by default
                config.exportScope = AlembicExportScope.EntireScene;
            }
            
            // Validate configuration
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                Debug.LogError($"[MultiTimelineRenderer] Invalid Alembic configuration: {errorMessage}");
                return null;
            }
            
            // Create Alembic settings
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings($"{name}_Recorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                settings.FrameRate = fps;
                settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
                settings.CapFrameRate = true;
                
                Debug.Log($"[MultiTimelineRenderer] Alembic settings created: {fps}fps, Export: {alembicExportTargets}, Scope: {alembicExportScope}");
            }
            
            return settings;
        }
        
        private RecorderSettings CreateAnimationRecorderSettingsForBatch(string name, int fps, int width, int height)
        {
            Debug.Log($"[MultiTimelineRenderer] Creating Animation recorder settings: {name}");
            
            // Create Animation configuration
            var config = new AnimationRecorderSettingsConfig
            {
                recordingProperties = animationRecordingProperties,
                recordingScope = animationRecordingScope,
                targetGameObject = animationTargetGameObject,
                frameRate = fps,
                compressionLevel = animationCompressionLevel,
                interpolationMode = animationInterpolationMode,
                includeChildren = animationIncludeChildren,
                clampedTangents = animationClampedTangents,
                recordBlendShapes = animationRecordBlendShapes
            };
            
            if (useAnimationPreset && animationPreset != AnimationRecordingPreset.Custom)
            {
                Debug.Log($"[MultiTimelineRenderer] Using Animation preset: {animationPreset}");
                
                // Get preset configuration
                config = AnimationRecorderSettingsConfig.GetPreset(animationPreset);
                
                // Override framerate with our settings
                config.frameRate = fps;
            }
            
            // Validate configuration
            string errorMessage;
            if (!config.Validate(out errorMessage))
            {
                Debug.LogError($"[MultiTimelineRenderer] Invalid Animation configuration: {errorMessage}");
                return null;
            }
            
            // Create Animation settings
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings($"{name}_Recorder", config);
            
            if (settings != null)
            {
                settings.Enabled = true;
                settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                settings.FrameRate = fps;
                settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
                settings.CapFrameRate = true;
                
                Debug.Log($"[MultiTimelineRenderer] Animation settings created: {fps}fps, Properties: {animationRecordingProperties}, Scope: {animationRecordingScope}");
            }
            
            return settings;
        }
    }
}