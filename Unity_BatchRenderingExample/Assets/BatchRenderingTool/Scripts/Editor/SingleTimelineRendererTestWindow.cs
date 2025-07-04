using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;
using System.Collections.Generic;
using System.Linq;

namespace BatchRenderingTool
{
    public class SingleTimelineRendererTestWindow : EditorWindow
    {
        private SingleTimelineRenderer renderer;
        private Vector2 scrollPosition;
        private string testLog = "";
        
        // Test parameters
        private PlayableDirector testDirector;
        private string testOutputFile = "TestOutput/test_<Take>";
        
        [MenuItem("Window/Batch Rendering Tool/Single Timeline Renderer Test Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SingleTimelineRendererTestWindow>("Single Timeline Renderer Test");
            window.minSize = new Vector2(600, 800);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Get reference to SingleTimelineRenderer window
            var windows = Resources.FindObjectsOfTypeAll<SingleTimelineRenderer>();
            if (windows.Length > 0)
            {
                renderer = windows[0];
                LogMessage("[TEST] Found SingleTimelineRenderer window", true);
            }
            else
            {
                LogMessage("[TEST] SingleTimelineRenderer window not found. Please open it first.", false);
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Single Timeline Renderer Test Window", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Renderer reference check
            if (renderer == null)
            {
                EditorGUILayout.HelpBox("SingleTimelineRenderer window not found. Please open it from Window > Batch Rendering Tool > Single Timeline Renderer", MessageType.Error);
                if (GUILayout.Button("Open Single Timeline Renderer"))
                {
                    SingleTimelineRenderer.ShowWindow();
                    OnEnable();
                }
                return;
            }
            
            // Main test sections
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawDirectorTests();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawTimelineCreationTests();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawRecorderSettingsTests();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawRenderFlowTests();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Test log
            EditorGUILayout.LabelField("Test Log:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(testLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear Log"))
            {
                testLog = "";
            }
        }
        
        private void DrawDirectorTests()
        {
            EditorGUILayout.LabelField("Director Tests", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test: Refresh Available Directors"))
            {
                LogMessage("[TEST] Starting RefreshAvailableDirectors test...", true);
                
                var directors = renderer.GetAllPlayableDirectors();
                LogMessage($"[TEST] Found {directors.Count} directors", true);
                
                foreach (var director in directors)
                {
                    if (director != null)
                    {
                        var timeline = director.playableAsset as TimelineAsset;
                        LogMessage($"  - {director.gameObject.name}: Timeline={timeline?.name ?? "null"}, Duration={timeline?.duration ?? 0}", true);
                    }
                }
            }
            
            if (GUILayout.Button("Test: Validate Selected Director"))
            {
                LogMessage("[TEST] Starting Validate Selected Director test...", true);
                
                var selectedDirector = renderer.GetSelectedDirector();
                if (selectedDirector == null)
                {
                    LogMessage("[TEST] No director selected", false);
                }
                else
                {
                    LogMessage($"[TEST] Selected director: {selectedDirector.gameObject.name}", true);
                    var timeline = selectedDirector.playableAsset as TimelineAsset;
                    LogMessage($"  - Timeline: {timeline?.name ?? "null"}", timeline != null);
                    LogMessage($"  - Duration: {timeline?.duration ?? 0}", true);
                    LogMessage($"  - PlayOnAwake: {selectedDirector.playOnAwake}", true);
                }
            }
            
            // Test director selection
            testDirector = (PlayableDirector)EditorGUILayout.ObjectField("Test Director:", testDirector, typeof(PlayableDirector), true);
            if (testDirector != null && GUILayout.Button("Set Test Director"))
            {
                renderer.SetSelectedDirector(testDirector);
                LogMessage($"[TEST] Set test director: {testDirector.gameObject.name}", true);
            }
        }
        
        private void DrawTimelineCreationTests()
        {
            EditorGUILayout.LabelField("Timeline Creation Tests", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test: Validate Output Settings"))
            {
                LogMessage("[TEST] Starting Validate Output Settings test...", true);
                
                string errorMessage;
                bool isValid = renderer.ValidateSettings(out errorMessage);
                
                LogMessage($"[TEST] Validation result: {isValid}", isValid);
                if (!isValid)
                {
                    LogMessage($"  - Error: {errorMessage}", false);
                }
                else
                {
                    LogMessage($"  - Output: {renderer.OutputFile}", true);
                    LogMessage($"  - Resolution: {renderer.OutputWidth}x{renderer.OutputHeight}", true);
                    LogMessage($"  - Frame Rate: {renderer.FrameRate}", true);
                    LogMessage($"  - Format: {renderer.ImageFormat}", true);
                }
            }
            
            if (GUILayout.Button("Test: Check Temp Directory"))
            {
                LogMessage("[TEST] Checking temp directory...", true);
                
                string tempDir = "Assets/BatchRenderingTool/Temp";
                bool exists = AssetDatabase.IsValidFolder(tempDir);
                
                LogMessage($"[TEST] Temp directory exists: {exists}", exists);
                if (!exists)
                {
                    LogMessage("[TEST] Creating temp directory...", true);
                    if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                    {
                        AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                    }
                    AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
                    AssetDatabase.Refresh();
                    LogMessage("[TEST] Temp directory created", true);
                }
            }
        }
        
        private void DrawRecorderSettingsTests()
        {
            EditorGUILayout.LabelField("Recorder Settings Tests", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test: Create Image Recorder Settings"))
            {
                LogMessage("[TEST] Starting Create Image Recorder Settings test...", true);
                
                try
                {
                    var settings = RecorderClipUtility.CreateProperImageRecorderSettings("TestImageRecorder");
                    if (settings != null)
                    {
                        LogMessage("[TEST] Successfully created ImageRecorderSettings", true);
                        LogMessage($"  - Type: {settings.GetType().FullName}", true);
                        LogMessage($"  - Name: {settings.name}", true);
                        LogMessage($"  - RecordMode: {settings.RecordMode}", true);
                        DestroyImmediate(settings);
                    }
                    else
                    {
                        LogMessage("[TEST] Failed to create ImageRecorderSettings - returned null", false);
                    }
                }
                catch (System.Exception e)
                {
                    LogMessage($"[TEST] Exception creating ImageRecorderSettings: {e.Message}", false);
                    LogMessage($"  - Stack trace: {e.StackTrace}", false);
                }
            }
            
            if (GUILayout.Button("Test: Create Timeline with Recorder"))
            {
                LogMessage("[TEST] Starting Create Timeline with Recorder test...", true);
                
                var selectedDirector = renderer.GetSelectedDirector();
                if (selectedDirector == null)
                {
                    LogMessage("[TEST] No director selected", false);
                    return;
                }
                
                var originalTimeline = selectedDirector.playableAsset as TimelineAsset;
                if (originalTimeline == null)
                {
                    LogMessage("[TEST] Selected director has no timeline", false);
                    return;
                }
                
                try
                {
                    // Create test timeline
                    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                    timeline.name = "TestTimeline";
                    
                    // Create control track
                    var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Test Control Track");
                    LogMessage("[TEST] Created ControlTrack", true);
                    
                    // Create recorder track
                    var recorderTrack = timeline.CreateTrack<UnityEditor.Recorder.Timeline.RecorderTrack>(null, "Test Recorder Track");
                    LogMessage("[TEST] Created RecorderTrack", true);
                    
                    // Clean up
                    DestroyImmediate(timeline);
                    LogMessage("[TEST] Timeline creation test completed successfully", true);
                }
                catch (System.Exception e)
                {
                    LogMessage($"[TEST] Exception creating timeline: {e.Message}", false);
                    LogMessage($"  - Stack trace: {e.StackTrace}", false);
                }
            }
        }
        
        private void DrawRenderFlowTests()
        {
            EditorGUILayout.LabelField("Render Flow Tests", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test: Full Validation"))
            {
                LogMessage("[TEST] Starting Full Validation test...", true);
                
                // 1. Check if renderer window exists
                if (renderer == null)
                {
                    LogMessage("[TEST] Renderer window not found", false);
                    return;
                }
                LogMessage("[TEST] Renderer window found", true);
                
                // 2. Validate settings
                string errorMessage;
                bool isValid = renderer.ValidateSettings(out errorMessage);
                LogMessage($"[TEST] Settings validation: {isValid}", isValid);
                if (!isValid)
                {
                    LogMessage($"  - Error: {errorMessage}", false);
                    return;
                }
                
                // 3. Check Unity Recorder package
                var recorderPackage = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    .FirstOrDefault(p => p.name == "com.unity.recorder");
                if (recorderPackage == null)
                {
                    LogMessage("[TEST] Unity Recorder package not found", false);
                    return;
                }
                LogMessage($"[TEST] Unity Recorder package found: v{recorderPackage.version}", true);
                
                // 4. Test recorder types
                System.Type[] recorderTypes = new System.Type[]
                {
                    typeof(UnityEditor.Recorder.Timeline.RecorderTrack),
                    typeof(UnityEditor.Recorder.Timeline.RecorderClip),
                    typeof(UnityEditor.Recorder.ImageRecorderSettings)
                };
                
                foreach (var type in recorderTypes)
                {
                    LogMessage($"[TEST] Type {type.Name} available: {type != null}", type != null);
                }
                
                LogMessage("[TEST] Full validation completed", true);
            }
            
            testOutputFile = EditorGUILayout.TextField("Test Output File:", testOutputFile);
            
            if (GUILayout.Button("Test: Start Rendering (Debug Mode)"))
            {
                LogMessage("[TEST] Starting debug render...", true);
                
                if (renderer == null)
                {
                    LogMessage("[TEST] Renderer not available", false);
                    return;
                }
                
                // Validate first
                string errorMessage;
                if (!renderer.ValidateSettings(out errorMessage))
                {
                    LogMessage($"[TEST] Validation failed: {errorMessage}", false);
                    return;
                }
                
                LogMessage("[TEST] Validation passed, starting render...", true);
                
                // Monitor console for errors during rendering
                Application.logMessageReceived += OnLogMessageReceived;
                
                // Trigger rendering through reflection
                var startRenderingMethod = renderer.GetType().GetMethod("StartRendering", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startRenderingMethod != null)
                {
                    startRenderingMethod.Invoke(renderer, null);
                }
                else
                {
                    LogMessage("[TEST] Could not find StartRendering method", false);
                    return;
                }
                
                LogMessage("[TEST] Render started - check Console for detailed logs", true);
            }
            
            if (GUILayout.Button("Stop Monitoring"))
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                LogMessage("[TEST] Stopped monitoring console logs", true);
            }
        }
        
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (logString.Contains("[SingleTimelineRenderer]") || 
                logString.Contains("[RecorderClipUtility]") ||
                logString.Contains("Timeline") ||
                logString.Contains("Recorder"))
            {
                bool isError = type == LogType.Error || type == LogType.Exception;
                LogMessage($"[CONSOLE {type}] {logString}", !isError);
                
                if (isError && !string.IsNullOrEmpty(stackTrace))
                {
                    LogMessage($"  Stack: {stackTrace.Split('\n')[0]}", false);
                }
            }
        }
        
        private void LogMessage(string message, bool success)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string coloredMessage = success 
                ? $"<color=green>{timestamp} {message}</color>" 
                : $"<color=red>{timestamp} {message}</color>";
            
            testLog += coloredMessage + "\n";
            
            // Also log to Unity console
            if (success)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
            
            // Limit log size
            string[] lines = testLog.Split('\n');
            if (lines.Length > 100)
            {
                testLog = string.Join("\n", lines.Skip(lines.Length - 100));
            }
            
            Repaint();
        }
    }
}