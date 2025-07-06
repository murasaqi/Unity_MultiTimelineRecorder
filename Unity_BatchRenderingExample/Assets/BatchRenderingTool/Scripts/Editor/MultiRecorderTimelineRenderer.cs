using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.EditorCoroutines.Editor;

namespace BatchRenderingTool
{
    /// <summary>
    /// 一つのTimelineに対して複数のレコーダーを設定して書き出すツール
    /// </summary>
    public class MultiRecorderTimelineRenderer : EditorWindow
    {
        // Timeline selection
        private List<PlayableDirector> availableDirectors = new List<PlayableDirector>();
        private int selectedDirectorIndex = 0;
        
        // Multi recorder configuration
        private MultiRecorderConfig multiRecorderConfig = new MultiRecorderConfig();
        
        // Rendering state
        private enum RenderState
        {
            Idle,
            Preparing,
            WaitingForPlayMode,
            Rendering,
            Complete,
            Error
        }
        
        private RenderState currentState = RenderState.Idle;
        private string statusMessage = "Ready";
        private float renderProgress = 0f;
        private int currentRecorderIndex = 0;
        
        // Pre-roll frames
        private int preRollFrames = 0;
        
        // Rendering objects
        private TimelineAsset renderTimeline;
        private GameObject renderingGameObject;
        private PlayableDirector renderingDirector;
        private EditorCoroutine renderCoroutine;
        private List<string> tempAssetPaths = new List<string>();
        
        // UI state
        private Vector2 scrollPosition;
        private bool showGlobalSettings = true;
        private bool showRecorderList = true;
        private List<bool> recorderFoldouts = new List<bool>();
        private int selectedRecorderForEdit = -1;
        
        // Drag and drop
        private int draggedRecorderIndex = -1;
        
        [MenuItem("Window/Batch Rendering Tool/Multi Recorder Timeline Renderer")]
        public static void ShowWindow()
        {
            var window = GetWindow<MultiRecorderTimelineRenderer>("Multi Recorder Timeline");
            window.minSize = new Vector2(400, 600);
        }
        
        private void OnEnable()
        {
            BatchRenderingToolLogger.Log("[MultiRecorderTimelineRenderer] Window enabled");
            ScanTimelines();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // デフォルトのレコーダーを追加
            if (multiRecorderConfig.RecorderItems.Count == 0)
            {
                multiRecorderConfig.AddRecorder(MultiRecorderConfig.CreateDefaultRecorder(RecorderSettingsType.Movie));
                recorderFoldouts.Add(true);
            }
        }
        
        private void OnDisable()
        {
            BatchRenderingToolLogger.Log("[MultiRecorderTimelineRenderer] Window disabled");
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CleanupRendering();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Header
            EditorGUILayout.LabelField("Multi Recorder Timeline Renderer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Record a single Timeline with multiple output formats simultaneously. " +
                "Add multiple recorders and configure each one independently.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Timeline selection
            DrawTimelineSelection();
            
            EditorGUILayout.Space();
            
            // Global settings
            DrawGlobalSettings();
            
            EditorGUILayout.Space();
            
            // Recorder list
            DrawRecorderList();
            
            EditorGUILayout.Space();
            
            // Render controls
            DrawRenderControls();
            
            EditorGUILayout.Space();
            
            // Status
            DrawStatusSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawTimelineSelection()
        {
            EditorGUILayout.LabelField("Timeline Selection", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (availableDirectors.Count == 0)
            {
                EditorGUILayout.HelpBox("No PlayableDirectors with Timeline found in the scene.", MessageType.Warning);
            }
            else
            {
                var directorNames = availableDirectors
                    .Select(d => d != null && d.gameObject != null ? d.gameObject.name : "null")
                    .ToArray();
                    
                selectedDirectorIndex = EditorGUILayout.Popup("Target Timeline", selectedDirectorIndex, directorNames);
                
                var selectedDirector = selectedDirectorIndex >= 0 && selectedDirectorIndex < availableDirectors.Count 
                    ? availableDirectors[selectedDirectorIndex] : null;
                    
                if (selectedDirector != null && selectedDirector.playableAsset != null)
                {
                    EditorGUILayout.LabelField("Duration", $"{selectedDirector.duration:F2} seconds");
                    EditorGUILayout.LabelField("Frame Count", $"{(int)(selectedDirector.duration * 24)} frames (at 24 fps)");
                }
            }
            
            if (GUILayout.Button("Refresh Timelines"))
            {
                ScanTimelines();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGlobalSettings()
        {
            showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "Global Settings", true);
            
            if (showGlobalSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                multiRecorderConfig.globalOutputPath = EditorGUILayout.TextField("Base Output Path", multiRecorderConfig.globalOutputPath);
                
                EditorGUILayout.Space(5);
                
                // Global resolution
                multiRecorderConfig.useGlobalResolution = EditorGUILayout.Toggle("Use Global Resolution", multiRecorderConfig.useGlobalResolution);
                if (multiRecorderConfig.useGlobalResolution)
                {
                    EditorGUI.indentLevel++;
                    multiRecorderConfig.globalWidth = EditorGUILayout.IntField("Width", multiRecorderConfig.globalWidth);
                    multiRecorderConfig.globalHeight = EditorGUILayout.IntField("Height", multiRecorderConfig.globalHeight);
                    EditorGUI.indentLevel--;
                }
                
                // Global frame rate
                multiRecorderConfig.useGlobalFrameRate = EditorGUILayout.Toggle("Use Global Frame Rate", multiRecorderConfig.useGlobalFrameRate);
                if (multiRecorderConfig.useGlobalFrameRate)
                {
                    EditorGUI.indentLevel++;
                    multiRecorderConfig.globalFrameRate = EditorGUILayout.IntSlider("Frame Rate", multiRecorderConfig.globalFrameRate, 1, 120);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(5);
                
                // Pre-roll frames
                preRollFrames = EditorGUILayout.IntField("Pre-roll Frames", preRollFrames);
                if (preRollFrames > 0)
                {
                    EditorGUILayout.HelpBox($"Timeline will wait {preRollFrames} frames at frame 0 before recording starts.", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawRecorderList()
        {
            EditorGUILayout.BeginHorizontal();
            showRecorderList = EditorGUILayout.Foldout(showRecorderList, "Recorder List", true);
            
            GUILayout.FlexibleSpace();
            
            // Add recorder button
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                ShowAddRecorderMenu();
            }
            
            // Preset button
            if (GUILayout.Button("Presets", GUILayout.Width(60)))
            {
                ShowPresetMenu();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (showRecorderList)
            {
                var recorders = multiRecorderConfig.RecorderItems;
                
                if (recorders.Count == 0)
                {
                    EditorGUILayout.HelpBox("No recorders configured. Click '+' to add a recorder.", MessageType.Info);
                }
                else
                {
                    // Draw each recorder
                    for (int i = 0; i < recorders.Count; i++)
                    {
                        DrawRecorderItem(i);
                    }
                }
            }
        }
        
        private void DrawRecorderItem(int index)
        {
            var item = multiRecorderConfig.RecorderItems[index];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            // Enable checkbox
            item.enabled = EditorGUILayout.Toggle(item.enabled, GUILayout.Width(20));
            
            // Foldout
            if (index >= recorderFoldouts.Count)
            {
                recorderFoldouts.Add(false);
            }
            recorderFoldouts[index] = EditorGUILayout.Foldout(recorderFoldouts[index], 
                $"{item.name} ({item.recorderType})", true);
            
            GUILayout.FlexibleSpace();
            
            // Move up
            GUI.enabled = index > 0;
            if (GUILayout.Button("↑", GUILayout.Width(25)))
            {
                multiRecorderConfig.MoveRecorder(index, index - 1);
                var foldout = recorderFoldouts[index];
                recorderFoldouts.RemoveAt(index);
                recorderFoldouts.Insert(index - 1, foldout);
            }
            
            // Move down
            GUI.enabled = index < multiRecorderConfig.RecorderItems.Count - 1;
            if (GUILayout.Button("↓", GUILayout.Width(25)))
            {
                multiRecorderConfig.MoveRecorder(index, index + 1);
                var foldout = recorderFoldouts[index];
                recorderFoldouts.RemoveAt(index);
                recorderFoldouts.Insert(index + 1, foldout);
            }
            
            GUI.enabled = true;
            
            // Delete
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Recorder", 
                    $"Are you sure you want to delete '{item.name}'?", "Delete", "Cancel"))
                {
                    multiRecorderConfig.RemoveRecorder(index);
                    recorderFoldouts.RemoveAt(index);
                    return;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Details
            if (recorderFoldouts[index])
            {
                EditorGUI.indentLevel++;
                
                // Basic settings
                item.name = EditorGUILayout.TextField("Name", item.name);
                item.recorderType = (RecorderSettingsType)EditorGUILayout.EnumPopup("Type", item.recorderType);
                
                EditorGUILayout.Space(5);
                
                // Output settings
                EditorGUILayout.LabelField("Output", EditorStyles.miniLabel);
                item.fileName = EditorGUILayout.TextField("File Name", item.fileName);
                item.takeNumber = EditorGUILayout.IntField("Take Number", item.takeNumber);
                
                // Resolution (if not using global)
                if (!multiRecorderConfig.useGlobalResolution)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Resolution", EditorStyles.miniLabel);
                    item.width = EditorGUILayout.IntField("Width", item.width);
                    item.height = EditorGUILayout.IntField("Height", item.height);
                }
                
                // Frame rate (if not using global)
                if (!multiRecorderConfig.useGlobalFrameRate)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Frame Rate", EditorStyles.miniLabel);
                    item.frameRate = EditorGUILayout.IntSlider("FPS", item.frameRate, 1, 120);
                    item.capFrameRate = EditorGUILayout.Toggle("Cap Frame Rate", item.capFrameRate);
                }
                
                // Type-specific settings
                EditorGUILayout.Space(5);
                DrawRecorderTypeSpecificSettings(item);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRecorderTypeSpecificSettings(MultiRecorderConfig.RecorderConfigItem item)
        {
            switch (item.recorderType)
            {
                case RecorderSettingsType.Image:
                    EditorGUILayout.LabelField("Image Settings", EditorStyles.miniLabel);
                    item.imageFormat = (ImageRecorderSettingsConfig.ImageFormat)
                        EditorGUILayout.EnumPopup("Format", item.imageFormat);
                    if (item.imageFormat == ImageRecorderSettingsConfig.ImageFormat.JPEG)
                    {
                        item.imageQuality = EditorGUILayout.IntSlider("Quality", item.imageQuality, 1, 100);
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    EditorGUILayout.LabelField("Movie Settings", EditorStyles.miniLabel);
                    if (GUILayout.Button("Configure Movie Settings"))
                    {
                        RecorderConfigEditors.RecorderConfigEditorWindow.ShowWindow(item, () => Repaint());
                    }
                    break;
                    
                case RecorderSettingsType.Animation:
                    EditorGUILayout.LabelField("Animation Settings", EditorStyles.miniLabel);
                    if (GUILayout.Button("Configure Animation Settings"))
                    {
                        RecorderConfigEditors.RecorderConfigEditorWindow.ShowWindow(item, () => Repaint());
                    }
                    break;
                    
                case RecorderSettingsType.Alembic:
                    EditorGUILayout.LabelField("Alembic Settings", EditorStyles.miniLabel);
                    if (GUILayout.Button("Configure Alembic Settings"))
                    {
                        RecorderConfigEditors.RecorderConfigEditorWindow.ShowWindow(item, () => Repaint());
                    }
                    break;
                    
                case RecorderSettingsType.AOV:
                    EditorGUILayout.LabelField("AOV Settings", EditorStyles.miniLabel);
                    if (!AOVTypeInfo.IsHDRPAvailable())
                    {
                        EditorGUILayout.HelpBox("AOV requires HDRP", MessageType.Warning);
                    }
                    else if (GUILayout.Button("Configure AOV Settings"))
                    {
                        RecorderConfigEditors.RecorderConfigEditorWindow.ShowWindow(item, () => Repaint());
                    }
                    break;
                    
                case RecorderSettingsType.FBX:
                    EditorGUILayout.LabelField("FBX Settings", EditorStyles.miniLabel);
                    if (GUILayout.Button("Configure FBX Settings"))
                    {
                        RecorderConfigEditors.RecorderConfigEditorWindow.ShowWindow(item, () => Repaint());
                    }
                    break;
            }
        }
        
        private void ShowAddRecorderMenu()
        {
            var menu = new GenericMenu();
            
            foreach (RecorderSettingsType type in Enum.GetValues(typeof(RecorderSettingsType)))
            {
                var recorderType = type;
                menu.AddItem(new GUIContent(recorderType.ToString()), false, () =>
                {
                    var newRecorder = MultiRecorderConfig.CreateDefaultRecorder(recorderType);
                    multiRecorderConfig.AddRecorder(newRecorder);
                    recorderFoldouts.Add(true);
                });
            }
            
            menu.ShowAsContext();
        }
        
        private void ShowPresetMenu()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Basic (Movie + Images)"), false, () =>
            {
                ApplyPreset(MultiRecorderConfig.Presets.CreateBasicPreset());
            });
            
            menu.AddItem(new GUIContent("Animation Production"), false, () =>
            {
                ApplyPreset(MultiRecorderConfig.Presets.CreateAnimationPreset());
            });
            
            menu.AddItem(new GUIContent("Compositing"), false, () =>
            {
                ApplyPreset(MultiRecorderConfig.Presets.CreateCompositingPreset());
            });
            
            menu.ShowAsContext();
        }
        
        private void ApplyPreset(MultiRecorderConfig preset)
        {
            if (EditorUtility.DisplayDialog("Apply Preset",
                "This will replace all current recorder settings. Continue?",
                "Apply", "Cancel"))
            {
                multiRecorderConfig = preset;
                recorderFoldouts.Clear();
                for (int i = 0; i < multiRecorderConfig.RecorderItems.Count; i++)
                {
                    recorderFoldouts.Add(false);
                }
            }
        }
        
        private void DrawRenderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool canRender = currentState == RenderState.Idle && 
                            availableDirectors.Count > 0 && 
                            !EditorApplication.isPlaying &&
                            multiRecorderConfig.GetEnabledRecorders().Count > 0;
            
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
            
            if (!canRender && currentState == RenderState.Idle)
            {
                if (multiRecorderConfig.GetEnabledRecorders().Count == 0)
                {
                    EditorGUILayout.HelpBox("No recorders are enabled. Enable at least one recorder to start rendering.", MessageType.Warning);
                }
            }
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
            
            // Progress
            if (currentState == RenderState.Rendering)
            {
                var enabledRecorders = multiRecorderConfig.GetEnabledRecorders();
                if (currentRecorderIndex < enabledRecorders.Count)
                {
                    EditorGUILayout.LabelField($"Current Recorder: {enabledRecorders[currentRecorderIndex].name}");
                }
                
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)), 
                    renderProgress, 
                    $"{(int)(renderProgress * 100)}%");
                
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
            var directors = FindObjectsOfType<PlayableDirector>();
            
            foreach (var director in directors)
            {
                if (director.playableAsset is TimelineAsset)
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
            statusMessage = "Preparing multi-recorder rendering...";
            renderProgress = 0f;
            currentRecorderIndex = 0;
            
            // Validate
            var enabledRecorders = multiRecorderConfig.GetEnabledRecorders();
            if (enabledRecorders.Count == 0)
            {
                currentState = RenderState.Error;
                statusMessage = "No enabled recorders";
                yield break;
            }
            
            var selectedDirector = availableDirectors[selectedDirectorIndex];
            if (selectedDirector == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected director is null";
                yield break;
            }
            
            var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
            if (originalTimeline == null)
            {
                currentState = RenderState.Error;
                statusMessage = "Selected director has no timeline";
                yield break;
            }
            
            // Validate all recorder settings
            for (int i = 0; i < enabledRecorders.Count; i++)
            {
                string errorMessage;
                if (!enabledRecorders[i].Validate(out errorMessage))
                {
                    currentState = RenderState.Error;
                    statusMessage = $"Recorder '{enabledRecorders[i].name}' validation failed: {errorMessage}";
                    yield break;
                }
            }
            
            BatchRenderingToolLogger.Log($"[MultiRecorderTimelineRenderer] Starting multi-recorder rendering with {enabledRecorders.Count} recorders");
            
            // Create render timeline with all recorder tracks
            try
            {
                statusMessage = "Creating render timeline...";
                yield return null;
                
                if (!CreateMultiRecorderTimeline(selectedDirector, originalTimeline, enabledRecorders))
                {
                    currentState = RenderState.Error;
                    statusMessage = "Failed to create render timeline";
                    yield break;
                }
                
                BatchRenderingToolLogger.Log("[MultiRecorderTimelineRenderer] Render timeline created successfully");
            }
            catch (Exception e)
            {
                currentState = RenderState.Error;
                statusMessage = $"Error creating timeline: {e.Message}";
                BatchRenderingToolLogger.LogError($"[MultiRecorderTimelineRenderer] Exception: {e}");
                yield break;
            }
            
            // Save timeline to temporary asset
            tempAssetPaths.Clear();
            var tempPath = $"Assets/BatchRenderingTool/Temp/MultiRecorderTimeline_{System.Guid.NewGuid()}.playable";
            
            // Ensure directory exists
            var tempDir = System.IO.Path.GetDirectoryName(tempPath);
            if (!AssetDatabase.IsValidFolder(tempDir))
            {
                System.IO.Directory.CreateDirectory(tempDir);
                AssetDatabase.Refresh();
            }
            
            AssetDatabase.CreateAsset(renderTimeline, tempPath);
            tempAssetPaths.Add(tempPath);
            
            // Store rendering data in EditorPrefs for Play Mode
            EditorPrefs.SetBool("MRT_IsRendering", true);
            EditorPrefs.SetString("MRT_DirectorName", selectedDirector.gameObject.name);
            EditorPrefs.SetString("MRT_TempAssetPath", tempPath);
            EditorPrefs.SetFloat("MRT_Duration", (float)originalTimeline.duration);
            EditorPrefs.SetString("MRT_ExposedName", "SourceTimeline");
            EditorPrefs.SetInt("MRT_FrameRate", multiRecorderConfig.useGlobalFrameRate ? 
                multiRecorderConfig.globalFrameRate : enabledRecorders[0].frameRate);
            EditorPrefs.SetInt("MRT_PreRollFrames", preRollFrames);
            EditorPrefs.SetInt("MRT_RecorderCount", enabledRecorders.Count);
            
            // Enter Play Mode
            currentState = RenderState.WaitingForPlayMode;
            statusMessage = "Entering Play Mode...";
            EditorApplication.isPlaying = true;
            
            BatchRenderingToolLogger.Log("[MultiRecorderTimelineRenderer] Waiting for Play Mode...");
            yield break;
        }
        
        private bool CreateMultiRecorderTimeline(PlayableDirector selectedDirector, TimelineAsset originalTimeline, 
            List<MultiRecorderConfig.RecorderConfigItem> recorders)
        {
            // Create new timeline
            renderTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            renderTimeline.name = "MultiRecorderRenderTimeline";
            
            // Create control track for original timeline
            var controlTrack = renderTimeline.CreateTrack<ControlTrack>(null, "Original Timeline Control");
            var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
            controlClip.duration = originalTimeline.duration;
            controlClip.displayName = "Original Timeline";
            
            var controlAsset = controlClip.asset as ControlPlayableAsset;
            controlAsset.updateDirector = false;
            controlAsset.updateParticle = true;
            controlAsset.updateITimeControl = true;
            controlAsset.searchHierarchy = false;
            controlAsset.active = true;
            controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
            
            // Set exposed reference
            var exposedName = "SourceTimeline";
            renderTimeline.SetReferenceValue(exposedName, selectedDirector);
            controlAsset.sourceGameObject = new ExposedReference<GameObject> { exposedName = exposedName };
            
            // Pre-roll implementation
            if (preRollFrames > 0)
            {
                var preRollTime = preRollFrames / (double)(multiRecorderConfig.useGlobalFrameRate ? 
                    multiRecorderConfig.globalFrameRate : recorders[0].frameRate);
                controlClip.start = preRollTime;
                controlClip.duration = originalTimeline.duration;
            }
            
            // Create recorder tracks for each enabled recorder
            double startTime = preRollFrames > 0 ? controlClip.start : 0;
            
            foreach (var recorderItem in recorders)
            {
                if (!CreateRecorderTrackForItem(recorderItem, startTime, originalTimeline.duration))
                {
                    BatchRenderingToolLogger.LogError($"[MultiRecorderTimelineRenderer] Failed to create recorder track for {recorderItem.name}");
                    return false;
                }
            }
            
            renderTimeline.SetReferenceValue(exposedName, selectedDirector);
            
            return true;
        }
        
        private bool CreateRecorderTrackForItem(MultiRecorderConfig.RecorderConfigItem item, double startTime, double duration)
        {
            try
            {
                var recorderTrack = renderTimeline.CreateTrack<RecorderTrack>(null, $"Recorder: {item.name}");
                var recorderClip = recorderTrack.CreateDefaultClip();
                recorderClip.start = startTime;
                recorderClip.duration = duration;
                recorderClip.displayName = item.name;
                
                // Create recorder settings based on type
                RecorderSettings settings = null;
                
                // Apply global settings if enabled
                int width = multiRecorderConfig.useGlobalResolution ? multiRecorderConfig.globalWidth : item.width;
                int height = multiRecorderConfig.useGlobalResolution ? multiRecorderConfig.globalHeight : item.height;
                int frameRate = multiRecorderConfig.useGlobalFrameRate ? multiRecorderConfig.globalFrameRate : item.frameRate;
                
                // Create wildcard context
                var wildcardContext = new WildcardContext
                {
                    SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    Take = item.takeNumber,
                    FrameRate = frameRate,
                    Width = width,
                    Height = height
                };
                
                // Process file name with wildcards
                string processedFileName = WildcardProcessor.ProcessWildcards(item.fileName, wildcardContext);
                processedFileName = System.IO.Path.Combine(multiRecorderConfig.globalOutputPath, processedFileName);
                
                switch (item.recorderType)
                {
                    case RecorderSettingsType.Image:
                        settings = CreateImageRecorderSettings(item, processedFileName, width, height, frameRate);
                        break;
                        
                    case RecorderSettingsType.Movie:
                        settings = CreateMovieRecorderSettings(item, processedFileName, width, height, frameRate);
                        break;
                        
                    case RecorderSettingsType.Animation:
                        settings = CreateAnimationRecorderSettings(item, processedFileName, frameRate);
                        break;
                        
                    case RecorderSettingsType.Alembic:
                        settings = CreateAlembicRecorderSettings(item, processedFileName, frameRate);
                        break;
                        
                    case RecorderSettingsType.AOV:
                        // AOV creates multiple settings
                        var aovSettingsList = RecorderSettingsFactory.CreateAOVRecorderSettings(processedFileName, item.aovConfig);
                        if (aovSettingsList.Count > 0)
                        {
                            settings = aovSettingsList[0]; // Use first one for the main track
                            // TODO: Handle multiple AOV outputs
                        }
                        break;
                        
                    case RecorderSettingsType.FBX:
                        settings = CreateFBXRecorderSettings(item, processedFileName, frameRate);
                        break;
                }
                
                if (settings == null)
                {
                    BatchRenderingToolLogger.LogError($"[MultiRecorderTimelineRenderer] Failed to create settings for {item.recorderType}");
                    return false;
                }
                
                // Apply settings to clip
                var recorderClipSettings = recorderClip.asset as RecorderClip;
                recorderClipSettings.settings = settings;
                
                return true;
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogError($"[MultiRecorderTimelineRenderer] Exception creating recorder track: {e}");
                return false;
            }
        }
        
        private RecorderSettings CreateImageRecorderSettings(MultiRecorderConfig.RecorderConfigItem item, 
            string fileName, int width, int height, int frameRate)
        {
            var config = new ImageRecorderSettingsConfig
            {
                imageFormat = item.imageFormat,
                jpegQuality = item.imageQuality,
                width = width,
                height = height,
                frameRate = frameRate
            };
            
            var settings = RecorderSettingsFactory.CreateImageRecorderSettings(item.name, config);
            RecorderSettingsHelper.ConfigureOutputPath(settings, fileName);
            
            return settings;
        }
        
        private RecorderSettings CreateMovieRecorderSettings(MultiRecorderConfig.RecorderConfigItem item,
            string fileName, int width, int height, int frameRate)
        {
            item.movieConfig.width = width;
            item.movieConfig.height = height;
            item.movieConfig.frameRate = frameRate;
            
            var settings = RecorderSettingsFactory.CreateMovieRecorderSettings(item.name, item.movieConfig);
            RecorderSettingsHelper.ConfigureOutputPath(settings, fileName);
            
            return settings;
        }
        
        private RecorderSettings CreateAnimationRecorderSettings(MultiRecorderConfig.RecorderConfigItem item,
            string fileName, int frameRate)
        {
            item.animationConfig.frameRate = frameRate;
            var settings = RecorderSettingsFactory.CreateAnimationRecorderSettings(item.name, item.animationConfig);
            RecorderSettingsHelper.ConfigureOutputPath(settings, fileName);
            
            return settings;
        }
        
        private RecorderSettings CreateAlembicRecorderSettings(MultiRecorderConfig.RecorderConfigItem item,
            string fileName, int frameRate)
        {
            item.alembicConfig.frameRate = frameRate;
            var settings = RecorderSettingsFactory.CreateAlembicRecorderSettings(item.name, item.alembicConfig);
            RecorderSettingsHelper.ConfigureOutputPath(settings, fileName);
            
            return settings;
        }
        
        private RecorderSettings CreateFBXRecorderSettings(MultiRecorderConfig.RecorderConfigItem item,
            string fileName, int frameRate)
        {
            item.fbxConfig.frameRate = frameRate;
            var settings = RecorderSettingsFactory.CreateFBXRecorderSettings(item.name, item.fbxConfig);
            RecorderSettingsHelper.ConfigureOutputPath(settings, fileName);
            
            return settings;
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            BatchRenderingToolLogger.LogVerbose($"[MultiRecorderTimelineRenderer] Play Mode state changed: {state}");
            
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BatchRenderingToolLogger.LogVerbose("[MultiRecorderTimelineRenderer] Entered Play Mode");
                
                // Check if we're supposed to be rendering
                bool isRendering = EditorPrefs.GetBool("MRT_IsRendering", false);
                
                if (isRendering)
                {
                    BatchRenderingToolLogger.Log("[MultiRecorderTimelineRenderer] Creating PlayModeTimelineRenderer GameObject");
                    currentState = RenderState.Rendering;
                    statusMessage = "Rendering with multiple recorders...";
                    
                    // Load rendering data
                    string directorName = EditorPrefs.GetString("MRT_DirectorName", "");
                    string tempAssetPath = EditorPrefs.GetString("MRT_TempAssetPath", "");
                    float duration = EditorPrefs.GetFloat("MRT_Duration", 0f);
                    string exposedName = EditorPrefs.GetString("MRT_ExposedName", "");
                    int frameRate = EditorPrefs.GetInt("MRT_FrameRate", 24);
                    int preRollFrames = EditorPrefs.GetInt("MRT_PreRollFrames", 0);
                    int recorderCount = EditorPrefs.GetInt("MRT_RecorderCount", 0);
                    
                    // Load render timeline
                    var renderTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
                    if (renderTimeline == null)
                    {
                        BatchRenderingToolLogger.LogError($"[MultiRecorderTimelineRenderer] Failed to load timeline from: {tempAssetPath}");
                        currentState = RenderState.Error;
                        statusMessage = "Failed to load render timeline";
                        return;
                    }
                    
                    // Create rendering data GameObject
                    var dataGO = new GameObject("[MultiRecorderRenderingData]");
                    var renderingData = dataGO.AddComponent<RenderingData>();
                    renderingData.directorName = directorName;
                    renderingData.renderTimeline = renderTimeline;
                    renderingData.duration = duration;
                    renderingData.exposedName = exposedName;
                    renderingData.frameRate = frameRate;
                    renderingData.preRollFrames = preRollFrames;
                    
                    // Create PlayModeTimelineRenderer GameObject
                    var rendererGO = new GameObject("[MultiRecorderPlayModeRenderer]");
                    var renderer = rendererGO.AddComponent<PlayModeTimelineRenderer>();
                    
                    if (renderer != null)
                    {
                        BatchRenderingToolLogger.Log($"[MultiRecorderTimelineRenderer] PlayModeTimelineRenderer created with {recorderCount} recorders");
                        statusMessage = $"Rendering {recorderCount} outputs simultaneously...";
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[MultiRecorderTimelineRenderer] Failed to create PlayModeTimelineRenderer");
                        currentState = RenderState.Error;
                        statusMessage = "Failed to create renderer";
                    }
                    
                    // Clear EditorPrefs
                    EditorPrefs.SetBool("MRT_IsRendering", false);
                    
                    // Monitor progress
                    MonitorRenderingProgress();
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                BatchRenderingToolLogger.LogVerbose("[MultiRecorderTimelineRenderer] Exiting Play Mode");
                
                // Check if rendering completed successfully
                if (EditorPrefs.GetBool("MRT_RenderingComplete", false))
                {
                    currentState = RenderState.Complete;
                    statusMessage = "Multi-recorder rendering completed successfully!";
                    renderProgress = 1f;
                    
                    EditorPrefs.SetBool("MRT_RenderingComplete", false);
                }
                else if (currentState == RenderState.Rendering)
                {
                    // Rendering was interrupted
                    currentState = RenderState.Error;
                    statusMessage = "Rendering was interrupted";
                }
                
                // Cleanup
                CleanupRendering();
            }
        }
        
        private void MonitorRenderingProgress()
        {
            // Find the PlayModeTimelineRenderer to monitor progress
            renderingGameObject = GameObject.Find("[MultiRecorderPlayModeRenderer]");
            if (renderingGameObject != null)
            {
                var renderer = renderingGameObject.GetComponent<PlayModeTimelineRenderer>();
                if (renderer != null)
                {
                    renderingDirector = renderer.GetRenderingDirector();
                }
            }
        }
        
        private void CleanupRendering()
        {
            // Cleanup temporary assets
            foreach (var path in tempAssetPaths)
            {
                if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            tempAssetPaths.Clear();
            
            // Destroy rendering objects
            if (renderingGameObject != null)
            {
                DestroyImmediate(renderingGameObject);
            }
            
            renderTimeline = null;
            renderingDirector = null;
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