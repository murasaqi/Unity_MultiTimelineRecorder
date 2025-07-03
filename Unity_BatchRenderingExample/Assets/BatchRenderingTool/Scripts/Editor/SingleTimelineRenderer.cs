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
        
        // Render settings
        private int frameRate = 24;
        private int width = 1920;
        private int height = 1080;
        private ImageRecorderSettings.ImageRecorderOutputFormat outputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        private string outputPath = "Recordings";
        
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
                    directorNames[i] = availableDirectors[i].gameObject.name;
                }
                
                selectedDirectorIndex = EditorGUILayout.Popup("Select Timeline:", selectedDirectorIndex, directorNames);
                
                var selectedDirector = availableDirectors[selectedDirectorIndex];
                var timeline = selectedDirector.playableAsset as TimelineAsset;
                if (timeline != null)
                {
                    EditorGUILayout.LabelField($"Duration: {timeline.duration:F2} seconds");
                    EditorGUILayout.LabelField($"Frame Count: {(int)(timeline.duration * frameRate)}");
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
            
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            width = EditorGUILayout.IntField("Width:", width);
            height = EditorGUILayout.IntField("Height:", height);
            outputFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", outputFormat);
            
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
            
            if (availableDirectors.Count > 0 && selectedDirectorIndex < availableDirectors.Count)
            {
                var directorName = availableDirectors[selectedDirectorIndex].gameObject.name;
                var sanitized = SanitizeFileName(directorName);
                EditorGUILayout.HelpBox($"Output: {outputPath}/{sanitized}/{sanitized}_<Frame>.{outputFormat.ToString().ToLower()}", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = currentState == RenderState.Idle && availableDirectors.Count > 0 && !EditorApplication.isPlaying;
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
                if (director.playableAsset != null && director.playableAsset is TimelineAsset)
                {
                    availableDirectors.Add(director);
                }
            }
            
            availableDirectors.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));
            
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
                
                EditorApplication.isPlaying = true;
                // The coroutine will be interrupted here, but OnEditorUpdate will continue in Play Mode
            }
        }
        
        private TimelineAsset CreateRenderTimeline(PlayableDirector originalDirector, TimelineAsset originalTimeline)
        {
            // Create timeline
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = $"{originalDirector.gameObject.name}_RenderTimeline";
            timeline.editorSettings.frameRate = frameRate;
            
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
            
            // Create recorder settings
            var sanitizedName = SanitizeFileName(originalDirector.gameObject.name);
            var recorderSettings = RecorderClipUtility.CreateProperImageRecorderSettings($"{sanitizedName}_Recorder");
            recorderSettings.Enabled = true;
            recorderSettings.OutputFormat = outputFormat;
            recorderSettings.CaptureAlpha = outputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
            recorderSettings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
            recorderSettings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
            recorderSettings.FrameRate = frameRate;
            recorderSettings.CapFrameRate = true;
            
            // Configure output path
            string fullOutputPath = outputPath;
            if (!System.IO.Path.IsPathRooted(fullOutputPath))
            {
                fullOutputPath = System.IO.Path.Combine(Application.dataPath, "..", outputPath);
            }
            
            string finalPath = System.IO.Path.Combine(fullOutputPath, sanitizedName);
            finalPath = System.IO.Path.GetFullPath(finalPath);
            
            if (!System.IO.Directory.Exists(finalPath))
            {
                System.IO.Directory.CreateDirectory(finalPath);
            }
            
            recorderSettings.OutputFile = $"{finalPath}/{sanitizedName}_<Frame>";
            recorderSettings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
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
            
            // Retrieve exposed name
            string exposedName = EditorPrefs.GetString("STR_ExposedName", "");
            
            // Clean up EditorPrefs
            EditorPrefs.DeleteKey("STR_DirectorName");
            EditorPrefs.DeleteKey("STR_TempAssetPath");
            EditorPrefs.DeleteKey("STR_Duration");
            EditorPrefs.DeleteKey("STR_ExposedName");
            
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
                    var controlTrack = renderTimeline.GetOutputTracks().OfType<ControlTrack>().FirstOrDefault();
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
                statusMessage = $"Rendering complete! Output saved to: {outputPath}/{directorName}";
                renderProgress = 1f;
                
                Debug.Log($"[SingleTimelineRenderer] Rendering complete for {directorName}");
            }
            
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
        
    }
}