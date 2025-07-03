using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BatchRenderingTool
{
    public class BatchRenderingStepByStepWindow : EditorWindow
    {
        private enum ExecutionStep
        {
            NotStarted,
            ScanningTimelines,
            CreatingRenderTimeline,
            CreatingControlTrack,
            CreatingRecorderSettings,
            CreatingRecorderTrack,
            CreatingRecorderClip,
            SettingUpPlayableDirector,
            PlayingTimeline,
            Completed
        }
        
        private List<PlayableDirector> directors = new List<PlayableDirector>();
        private int selectedDirectorIndex = 0;
        private ExecutionStep currentStep = ExecutionStep.NotStarted;
        
        // Temporary assets
        private TimelineAsset renderTimeline;
        private GameObject renderingGameObject;
        private PlayableDirector renderingDirector;
        private ImageRecorderSettings recorderSettings;
        private ControlTrack controlTrack;
        private RecorderTrack recorderTrack;
        private TimelineClip controlClip;
        private TimelineClip recorderClip;
        private string controlExposedName = "";
        
        // Debug info
        private string lastError = "";
        private List<string> executionLog = new List<string>();
        private Vector2 logScrollPosition;
        private string currentStepInfo = "";
        private string logTextArea = "";
        
        // Settings
        private int frameRate = 24;
        private int width = 1920;
        private int height = 1080;
        private ImageRecorderSettings.ImageRecorderOutputFormat outputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        private string outputPath = "Recordings";
        
        // Inspector
        private UnityEngine.Object inspectedObject;
        private Vector2 inspectorScrollPosition;
        
        [MenuItem("Window/Batch Rendering Tool/Step-by-Step Debugger")]
        public static void ShowWindow()
        {
            BatchRenderingStepByStepWindow window = GetWindow<BatchRenderingStepByStepWindow>();
            window.titleContent = new GUIContent("Batch Rendering Step-by-Step");
            window.minSize = new Vector2(800, 600);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Controls
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            DrawControlPanel();
            EditorGUILayout.EndVertical();
            
            // Right panel - Inspector
            EditorGUILayout.BeginVertical();
            DrawInspectorPanel();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawControlPanel()
        {
            EditorGUILayout.LabelField("Batch Rendering Step-by-Step Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // Settings
            DrawSettingsSection();
            EditorGUILayout.Space(10);
            
            // Step execution
            DrawStepExecutionSection();
            EditorGUILayout.Space(10);
            
            // Execution log
            DrawExecutionLogSection();
        }
        
        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            width = EditorGUILayout.IntField("Width:", width);
            height = EditorGUILayout.IntField("Height:", height);
            outputFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", outputFormat);
            outputPath = EditorGUILayout.TextField("Output Path:", outputPath);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStepExecutionSection()
        {
            EditorGUILayout.LabelField("Step Execution", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Current step info in selectable text area
            UpdateCurrentStepInfo();
            EditorGUILayout.LabelField("Current Status:");
            EditorGUILayout.TextArea(currentStepInfo, GUILayout.Height(60));
            
            if (GUILayout.Button("Copy Status", GUILayout.Width(100)))
            {
                GUIUtility.systemCopyBuffer = currentStepInfo;
            }
            
            EditorGUILayout.Space(5);
            
            // Step buttons
            GUI.enabled = currentStep == ExecutionStep.NotStarted;
            if (GUILayout.Button("1. Scan Timelines"))
            {
                ExecuteStep(ExecutionStep.ScanningTimelines);
            }
            
            if (directors.Count > 0 && currentStep >= ExecutionStep.ScanningTimelines)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Found {directors.Count} timelines:");
                string[] directorNames = directors.Select(d => d.gameObject.name).ToArray();
                selectedDirectorIndex = EditorGUILayout.Popup("Select Timeline:", selectedDirectorIndex, directorNames);
                EditorGUILayout.Space(5);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.ScanningTimelines && directors.Count > 0;
            if (GUILayout.Button("2. Create Render Timeline"))
            {
                ExecuteStep(ExecutionStep.CreatingRenderTimeline);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.CreatingRenderTimeline && renderTimeline != null;
            if (GUILayout.Button("3. Create Control Track"))
            {
                ExecuteStep(ExecutionStep.CreatingControlTrack);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.CreatingControlTrack;
            if (GUILayout.Button("4. Create Recorder Settings"))
            {
                ExecuteStep(ExecutionStep.CreatingRecorderSettings);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.CreatingRecorderSettings && recorderSettings != null;
            if (GUILayout.Button("5. Create Recorder Track"))
            {
                ExecuteStep(ExecutionStep.CreatingRecorderTrack);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.CreatingRecorderTrack && recorderTrack != null;
            if (GUILayout.Button("6. Create Recorder Clip"))
            {
                ExecuteStep(ExecutionStep.CreatingRecorderClip);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.CreatingRecorderClip && recorderClip != null;
            if (GUILayout.Button("7. Setup Playable Director"))
            {
                ExecuteStep(ExecutionStep.SettingUpPlayableDirector);
            }
            
            GUI.enabled = currentStep >= ExecutionStep.SettingUpPlayableDirector && renderingDirector != null;
            if (GUILayout.Button("8. Play Timeline"))
            {
                ExecuteStep(ExecutionStep.PlayingTimeline);
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset"))
            {
                ResetAll();
            }
            
            GUI.enabled = renderingDirector != null && renderingDirector.state == PlayState.Playing;
            if (GUILayout.Button("Stop Playback"))
            {
                if (renderingDirector != null)
                {
                    renderingDirector.Stop();
                    LogStep("Playback stopped");
                }
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawExecutionLogSection()
        {
            EditorGUILayout.LabelField("Execution Log", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Update log text area
            UpdateLogTextArea();
            
            // Selectable text area for logs
            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(logTextArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Log"))
            {
                GUIUtility.systemCopyBuffer = logTextArea;
            }
            if (GUILayout.Button("Clear Log"))
            {
                executionLog.Clear();
                logTextArea = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInspectorPanel()
        {
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Asset inspection buttons
            EditorGUILayout.LabelField("Inspect Assets:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Timeline") && renderTimeline != null)
            {
                inspectedObject = renderTimeline;
                InspectObject(renderTimeline, "Render Timeline");
            }
            if (GUILayout.Button("Control Track") && controlTrack != null)
            {
                inspectedObject = controlTrack;
                InspectObject(controlTrack, "Control Track");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Recorder Track") && recorderTrack != null)
            {
                inspectedObject = recorderTrack;
                InspectObject(recorderTrack, "Recorder Track");
            }
            if (GUILayout.Button("Recorder Settings") && recorderSettings != null)
            {
                inspectedObject = recorderSettings;
                InspectObject(recorderSettings, "Recorder Settings");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Control Clip") && controlClip != null)
            {
                var controlAsset = controlClip.asset as ControlPlayableAsset;
                inspectedObject = controlAsset;
                InspectObject(controlAsset, "Control Clip Asset");
            }
            if (GUILayout.Button("Recorder Clip") && recorderClip != null)
            {
                var recorderAsset = recorderClip.asset as RecorderClip;
                inspectedObject = recorderAsset;
                InspectRecorderClip(recorderAsset);
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Playable Director") && renderingDirector != null)
            {
                inspectedObject = renderingDirector;
                InspectPlayableDirector();
            }
            
            EditorGUILayout.Space(10);
            
            // Object field
            if (inspectedObject != null)
            {
                EditorGUILayout.ObjectField("Inspecting:", inspectedObject, typeof(UnityEngine.Object), false);
            }
            
            // Inspection details
            inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);
            DrawInspectorDetails();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInspectorDetails()
        {
            if (inspectedObject == null) return;
            
            EditorGUILayout.LabelField("Details:", EditorStyles.boldLabel);
            
            if (inspectedObject is RecorderClip)
            {
                DrawRecorderClipDetails(inspectedObject as RecorderClip);
            }
            else if (inspectedObject is ImageRecorderSettings)
            {
                DrawImageRecorderSettingsDetails(inspectedObject as ImageRecorderSettings);
            }
            else if (inspectedObject is TimelineAsset)
            {
                DrawTimelineDetails(inspectedObject as TimelineAsset);
            }
            else if (inspectedObject is PlayableDirector)
            {
                DrawPlayableDirectorDetails(inspectedObject as PlayableDirector);
            }
        }
        
        private void DrawRecorderClipDetails(RecorderClip clip)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"Type: {clip.GetType().FullName}");
            details.AppendLine($"Settings: {(clip.settings != null ? clip.settings.GetType().Name : "null")}");
            
            if (clip.settings != null)
            {
                details.AppendLine($"Settings Type: {clip.settings.GetType().FullName}");
                details.AppendLine($"Enabled: {clip.settings.Enabled}");
                
                if (clip.settings is ImageRecorderSettings imgSettings)
                {
                    details.AppendLine($"Output Format: {imgSettings.OutputFormat}");
                    details.AppendLine($"Output File: {imgSettings.OutputFile}");
                }
            }
            
            details.AppendLine();
            details.AppendLine("Internal Fields (via Reflection):");
            
            var type = clip.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(clip);
                    details.AppendLine($"{field.Name}: {value?.ToString() ?? "null"}");
                }
                catch { }
            }
            
            EditorGUILayout.TextArea(details.ToString(), GUILayout.ExpandHeight(true));
            
            if (GUILayout.Button("Copy Details", GUILayout.Width(100)))
            {
                GUIUtility.systemCopyBuffer = details.ToString();
            }
        }
        
        private void DrawImageRecorderSettingsDetails(ImageRecorderSettings settings)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"Output Format: {settings.OutputFormat}");
            details.AppendLine($"Output File: {settings.OutputFile}");
            details.AppendLine($"Capture Alpha: {settings.CaptureAlpha}");
            
            if (settings.imageInputSettings != null)
            {
                details.AppendLine($"Resolution: {settings.imageInputSettings.OutputWidth}x{settings.imageInputSettings.OutputHeight}");
            }
            
            EditorGUILayout.TextArea(details.ToString());
            
            if (GUILayout.Button("Copy Details", GUILayout.Width(100)))
            {
                GUIUtility.systemCopyBuffer = details.ToString();
            }
        }
        
        private void DrawTimelineDetails(TimelineAsset timeline)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"Duration: {timeline.duration}");
            details.AppendLine($"Frame Rate: {timeline.editorSettings.frameRate}");
            details.AppendLine($"Track Count: {timeline.outputTrackCount}");
            details.AppendLine();
            details.AppendLine("Tracks:");
            
            foreach (var track in timeline.GetOutputTracks())
            {
                details.AppendLine($"- {track.name} ({track.GetType().Name})");
            }
            
            EditorGUILayout.TextArea(details.ToString());
            
            if (GUILayout.Button("Copy Details", GUILayout.Width(100)))
            {
                GUIUtility.systemCopyBuffer = details.ToString();
            }
        }
        
        private void DrawPlayableDirectorDetails(PlayableDirector director)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"State: {director.state}");
            details.AppendLine($"Time: {director.time:F2}/{director.duration:F2}");
            details.AppendLine($"Timeline: {(director.playableAsset != null ? director.playableAsset.name : "null")}");
            
            if (renderTimeline != null)
            {
                details.AppendLine();
                details.AppendLine("Bindings:");
                foreach (var output in renderTimeline.outputs)
                {
                    var binding = director.GetGenericBinding(output.sourceObject);
                    details.AppendLine($"{output.sourceObject?.name}: {(binding != null ? binding.ToString() : "null")}");
                }
            }
            
            EditorGUILayout.TextArea(details.ToString());
            
            if (GUILayout.Button("Copy Details", GUILayout.Width(100)))
            {
                GUIUtility.systemCopyBuffer = details.ToString();
            }
        }
        
        private void ExecuteStep(ExecutionStep step)
        {
            lastError = "";
            
            try
            {
                switch (step)
                {
                    case ExecutionStep.ScanningTimelines:
                        ScanTimelines();
                        break;
                    case ExecutionStep.CreatingRenderTimeline:
                        CreateRenderTimeline();
                        break;
                    case ExecutionStep.CreatingControlTrack:
                        CreateControlTrack();
                        break;
                    case ExecutionStep.CreatingRecorderSettings:
                        CreateRecorderSettings();
                        break;
                    case ExecutionStep.CreatingRecorderTrack:
                        CreateRecorderTrack();
                        break;
                    case ExecutionStep.CreatingRecorderClip:
                        CreateRecorderClip();
                        break;
                    case ExecutionStep.SettingUpPlayableDirector:
                        SetupPlayableDirector();
                        break;
                    case ExecutionStep.PlayingTimeline:
                        PlayTimeline();
                        break;
                }
                
                if (string.IsNullOrEmpty(lastError))
                {
                    currentStep = step;
                }
            }
            catch (System.Exception e)
            {
                lastError = $"Error in {step}: {e.Message}";
                LogStep($"Exception: {e}");
            }
            
            Repaint();
        }
        
        private void ScanTimelines()
        {
            directors.Clear();
            PlayableDirector[] allDirectors = GameObject.FindObjectsOfType<PlayableDirector>();
            
            foreach (var director in allDirectors)
            {
                if (director.playableAsset != null)
                {
                    directors.Add(director);
                }
            }
            
            directors.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));
            LogStep($"Found {directors.Count} timelines");
        }
        
        private void CreateRenderTimeline()
        {
            if (renderTimeline != null)
            {
                DestroyImmediate(renderTimeline);
            }
            
            var selectedDirector = directors[selectedDirectorIndex];
            renderTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            renderTimeline.name = $"{selectedDirector.gameObject.name}_RenderTimeline";
            renderTimeline.editorSettings.frameRate = frameRate;
            
            // Save as temporary asset
            string tempPath = $"Assets/BatchRenderingTool/Temp/StepByStep_{renderTimeline.name}.playable";
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (!AssetDatabase.IsValidFolder(tempDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                {
                    AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                }
                AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
            }
            
            AssetDatabase.CreateAsset(renderTimeline, tempPath);
            AssetDatabase.SaveAssets();
            
            LogStep($"Created render timeline: {renderTimeline.name}");
            InspectObject(renderTimeline, "Render Timeline");
        }
        
        private void CreateControlTrack()
        {
            var selectedDirector = directors[selectedDirectorIndex];
            var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
            
            controlTrack = renderTimeline.CreateTrack<ControlTrack>(null, "Control Track");
            controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
            controlClip.displayName = selectedDirector.gameObject.name;
            controlClip.start = 0;
            controlClip.duration = originalTimeline.duration;
            
            var controlAsset = controlClip.asset as ControlPlayableAsset;
            // Store the exposed name for later binding
            string exposedName = UnityEditor.GUID.Generate().ToString();
            controlAsset.sourceGameObject.exposedName = exposedName;
            
            // Don't set prefabGameObject - it should be resolved at runtime via binding
            // controlAsset.prefabGameObject = selectedDirector.gameObject;
            
            controlAsset.updateDirector = true;
            controlAsset.updateParticle = true;
            controlAsset.updateITimeControl = true;
            controlAsset.searchHierarchy = false;
            controlAsset.active = true;
            controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
            
            // Store exposed name for later use
            controlExposedName = exposedName;
            
            EditorUtility.SetDirty(controlAsset);
            EditorUtility.SetDirty(renderTimeline);
            AssetDatabase.SaveAssets();
            
            LogStep($"Created control track and clip with exposed name: {exposedName}");
            InspectObject(controlAsset, "Control Clip Asset");
        }
        
        private void CreateRecorderSettings()
        {
            var selectedDirector = directors[selectedDirectorIndex];
            
            // Sanitize the name for file system use
            string sanitizedName = SanitizeFileName(selectedDirector.gameObject.name);
            
            // Use RecorderClipUtility to create properly initialized settings
            recorderSettings = RecorderClipUtility.CreateProperImageRecorderSettings($"{sanitizedName}_Recorder");
            recorderSettings.Enabled = true;
            recorderSettings.OutputFormat = outputFormat;
            recorderSettings.CaptureAlpha = outputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
            
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
            
            // Unity Recorder uses wildcard syntax, not actual file path
            recorderSettings.OutputFile = $"{finalPath}/{sanitizedName}_<Frame>";
            
            recorderSettings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
            };
            
            // Save as sub-asset
            AssetDatabase.AddObjectToAsset(recorderSettings, renderTimeline);
            EditorUtility.SetDirty(recorderSettings);
            AssetDatabase.SaveAssets();
            
            LogStep($"Created recorder settings: {recorderSettings.OutputFile}");
            InspectObject(recorderSettings, "Recorder Settings");
        }
        
        private void CreateRecorderTrack()
        {
            recorderTrack = renderTimeline.CreateTrack<RecorderTrack>(null, "Recorder Track");
            
            EditorUtility.SetDirty(renderTimeline);
            AssetDatabase.SaveAssets();
            
            LogStep("Created recorder track");
            InspectObject(recorderTrack, "Recorder Track");
        }
        
        private void CreateRecorderClip()
        {
            var selectedDirector = directors[selectedDirectorIndex];
            var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
            
            recorderClip = recorderTrack.CreateClip<RecorderClip>();
            recorderClip.displayName = $"Record {selectedDirector.gameObject.name}";
            recorderClip.start = 0;
            recorderClip.duration = originalTimeline.duration;
            
            // Get the RecorderClip asset
            var recorderAsset = recorderClip.asset as RecorderClip;
            
            // Assign the settings immediately
            recorderAsset.settings = recorderSettings;
            
            // Mark as dirty and save
            EditorUtility.SetDirty(recorderAsset);
            EditorUtility.SetDirty(renderTimeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            LogStep("Created recorder clip with settings");
            InspectRecorderClip(recorderAsset);
        }
        
        private void SetupPlayableDirector()
        {
            if (renderingGameObject != null)
            {
                DestroyImmediate(renderingGameObject);
            }
            
            var selectedDirector = directors[selectedDirectorIndex];
            renderingGameObject = new GameObject($"{selectedDirector.gameObject.name}_RenderController");
            renderingDirector = renderingGameObject.AddComponent<PlayableDirector>();
            renderingDirector.playableAsset = renderTimeline;
            
            // IMPORTANT: Set binding AFTER assigning the playable asset
            // Method 1: Set generic binding directly on the control track
            renderingDirector.SetGenericBinding(controlTrack, selectedDirector.gameObject);
            
            // Method 2: Also try setting via the track outputs
            foreach (var output in renderTimeline.outputs)
            {
                if (output.sourceObject == controlTrack)
                {
                    renderingDirector.SetGenericBinding(output.sourceObject, selectedDirector.gameObject);
                    LogStep($"Set binding for output: {output.streamName}");
                }
            }
            
            // Method 3: Set exposed reference if available
            if (!string.IsNullOrEmpty(controlExposedName))
            {
                renderingDirector.SetReferenceValue(controlExposedName, selectedDirector.gameObject);
                LogStep($"Set exposed reference: {controlExposedName}");
            }
            
            // Force rebuild to apply bindings
            renderingDirector.RebuildGraph();
            
            // Verify binding
            var binding = renderingDirector.GetGenericBinding(controlTrack);
            LogStep($"Setup playable director - Binding result: {(binding != null ? binding.name : "NULL")}");
            InspectPlayableDirector();
        }
        
        private void PlayTimeline()
        {
            Selection.activeObject = null;
            
            renderingDirector.time = 0;
            renderingDirector.Play();
            
            LogStep("Started timeline playback");
            
            EditorApplication.update += UpdatePlayback;
        }
        
        private void UpdatePlayback()
        {
            if (renderingDirector != null && renderingDirector.state == PlayState.Playing)
            {
                Repaint();
            }
            else
            {
                EditorApplication.update -= UpdatePlayback;
                if (renderingDirector != null)
                {
                    LogStep($"Playback completed at time: {renderingDirector.time:F2}");
                    currentStep = ExecutionStep.Completed;
                }
            }
        }
        
        private void InspectObject(UnityEngine.Object obj, string label)
        {
            LogStep($"Inspecting: {label}");
        }
        
        private void InspectRecorderClip(RecorderClip clip)
        {
            inspectedObject = clip;
            LogStep($"Inspecting RecorderClip - Settings Type: {(clip.settings != null ? clip.settings.GetType().Name : "null")}");
        }
        
        private void InspectPlayableDirector()
        {
            LogStep($"Inspecting PlayableDirector - State: {renderingDirector.state}");
        }
        
        private void LogStep(string message)
        {
            executionLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[StepByStep] {message}");
            
            if (executionLog.Count > 100)
            {
                executionLog.RemoveAt(0);
            }
        }
        
        private void ResetAll()
        {
            if (renderingDirector != null)
            {
                renderingDirector.Stop();
            }
            
            if (renderingGameObject != null)
            {
                DestroyImmediate(renderingGameObject);
            }
            
            if (renderTimeline != null)
            {
                string path = AssetDatabase.GetAssetPath(renderTimeline);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            
            renderTimeline = null;
            renderingGameObject = null;
            renderingDirector = null;
            recorderSettings = null;
            controlTrack = null;
            recorderTrack = null;
            controlClip = null;
            recorderClip = null;
            controlExposedName = "";
            inspectedObject = null;
            currentStep = ExecutionStep.NotStarted;
            lastError = "";
            
            AssetDatabase.Refresh();
            LogStep("Reset all");
        }
        
        private void UpdateCurrentStepInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Current Step: {currentStep}");
            
            if (!string.IsNullOrEmpty(lastError))
            {
                info.AppendLine($"ERROR: {lastError}");
            }
            
            if (directors.Count > 0)
            {
                info.AppendLine($"Timelines Found: {directors.Count}");
                if (selectedDirectorIndex < directors.Count)
                {
                    info.AppendLine($"Selected: {directors[selectedDirectorIndex].gameObject.name}");
                }
            }
            
            currentStepInfo = info.ToString();
        }
        
        private void UpdateLogTextArea()
        {
            logTextArea = string.Join("\n", executionLog);
        }
        
        private string SanitizeFileName(string fileName)
        {
            // Remove or replace invalid characters for file names
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
            EditorApplication.update -= UpdatePlayback;
            ResetAll();
        }
    }
}