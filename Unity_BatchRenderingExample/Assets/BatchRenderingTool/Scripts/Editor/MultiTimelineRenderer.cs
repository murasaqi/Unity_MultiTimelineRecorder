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
    /// Multi Timeline Renderer - Renders multiple timelines sequentially with advanced UI
    /// </summary>
    public class MultiTimelineRenderer : EditorWindow
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
        
        // Render settings
        private int frameRate = 24;
        private int width = 1920;
        private int height = 1080;
        private ImageRecorderSettings.ImageRecorderOutputFormat outputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        private string outputPath = "Recordings";
        private ErrorHandling errorHandling = ErrorHandling.StopOnError;
        
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
            
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            width = EditorGUILayout.IntField("Width:", width);
            height = EditorGUILayout.IntField("Height:", height);
            outputFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", outputFormat);
            
            EditorGUILayout.Space(5);
            
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                errorHandling = (ErrorHandling)EditorGUILayout.EnumPopup("Error Handling:", errorHandling);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("Output Path:", outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", outputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = Path.GetRelativePath(Application.dataPath + "/..", path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox($"Each timeline will be rendered to: {outputPath}/<TimelineName>/", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool hasSelected = renderItems.Any(item => item.selected);
            GUI.enabled = currentState == RenderState.Idle && hasSelected && !EditorApplication.isPlaying;
            
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
                EditorPrefs.SetString("MTR_OutputFormat", outputFormat.ToString());
                EditorPrefs.SetString("MTR_ErrorHandling", errorHandling.ToString());
                
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
            string formatString = EditorPrefs.GetString("MTR_OutputFormat", "PNG");
            string errorHandlingString = EditorPrefs.GetString("MTR_ErrorHandling", "StopOnError");
            
            // Parse enums
            System.Enum.TryParse<ImageRecorderSettings.ImageRecorderOutputFormat>(formatString, out var storedFormat);
            System.Enum.TryParse<ErrorHandling>(errorHandlingString, out var storedErrorHandling);
            
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
                                                             storedFrameRate, storedWidth, storedHeight, storedFormat);
                        
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
            EditorPrefs.DeleteKey("MTR_OutputFormat");
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
                                                   int fps, int resWidth, int resHeight, 
                                                   ImageRecorderSettings.ImageRecorderOutputFormat format)
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
            
            // Create recorder settings
            var recorderSettings = RecorderClipUtility.CreateProperImageRecorderSettings($"{outputFolderName}_Recorder");
            recorderSettings.Enabled = true;
            recorderSettings.OutputFormat = format;
            recorderSettings.CaptureAlpha = format == ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
            recorderSettings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            recorderSettings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
            recorderSettings.FrameRate = fps;
            recorderSettings.CapFrameRate = true;
            
            // Configure output path
            string fullOutputPath = baseOutputPath;
            if (!System.IO.Path.IsPathRooted(fullOutputPath))
            {
                fullOutputPath = System.IO.Path.Combine(Application.dataPath, "..", baseOutputPath);
            }
            
            string finalPath = System.IO.Path.Combine(fullOutputPath, outputFolderName);
            finalPath = System.IO.Path.GetFullPath(finalPath);
            
            if (!System.IO.Directory.Exists(finalPath))
            {
                System.IO.Directory.CreateDirectory(finalPath);
            }
            
            recorderSettings.OutputFile = $"{finalPath}/{outputFolderName}_<Frame>";
            recorderSettings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = resWidth,
                OutputHeight = resHeight
            };
            
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
    }
}