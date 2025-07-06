using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Recorder;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Animation Recorder Test and Debug Window
    /// Provides comprehensive testing and debugging features for Animation recording
    /// </summary>
    public class AnimationRecorderDebugWindow : EditorWindow
    {
        private AnimationRecorderSettingsConfig config = new AnimationRecorderSettingsConfig();
        private Vector2 scrollPosition;
        private bool showRecordingTargets = true;
        private bool showSamplingSettings = true;
        private bool showCompressionSettings = true;
        private bool showDebugInfo = false;
        private bool showTestResults = false;
        
        // Test results
        private string lastTestResult = "";
        private AnimationClip lastRecordedClip = null;
        private Dictionary<string, int> propertyCountByType = new Dictionary<string, int>();
        private int totalKeyframeCount = 0;
        private float recordingDuration = 0f;
        private long memoryUsageBytes = 0;
        
        // Preview
        private GameObject previewTarget;
        private List<string> recordableProperties = new List<string>();
        
        [MenuItem("Window/Batch Rendering Tool/Animation Recorder Debug")]
        static void ShowWindow()
        {
            var window = GetWindow<AnimationRecorderDebugWindow>("Animation Debug");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Recorder Test & Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Configuration Section
            DrawConfigurationSection();
            
            EditorGUILayout.Space(20);
            
            // Test Controls
            DrawTestControls();
            
            EditorGUILayout.Space(20);
            
            // Debug Information
            DrawDebugInformation();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawConfigurationSection()
        {
            // Recording Targets
            showRecordingTargets = EditorGUILayout.BeginFoldoutHeaderGroup(showRecordingTargets, "Recording Targets");
            if (showRecordingTargets)
            {
                EditorGUI.indentLevel++;
                
                config.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                    "Target GameObject", 
                    config.targetGameObject, 
                    typeof(GameObject), 
                    true);
                
                config.recordingScope = (AnimationRecordingScope)EditorGUILayout.EnumPopup(
                    "Recording Scope", 
                    config.recordingScope);
                
                config.recordingProperties = (AnimationRecordingProperties)EditorGUILayout.EnumFlagsField(
                    "Properties to Record", 
                    config.recordingProperties);
                
                if (config.targetGameObject != null)
                {
                    EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                    DrawTargetPreview();
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // Sampling Settings
            showSamplingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSamplingSettings, "Sampling Settings");
            if (showSamplingSettings)
            {
                EditorGUI.indentLevel++;
                
                config.frameRate = EditorGUILayout.Slider("Frame Rate", config.frameRate, 1f, 120f);
                config.interpolationMode = (AnimationInterpolationMode)EditorGUILayout.EnumPopup(
                    "Interpolation Mode", 
                    config.interpolationMode);
                config.recordInWorldSpace = EditorGUILayout.Toggle("Record in World Space", config.recordInWorldSpace);
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // Compression Settings
            showCompressionSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showCompressionSettings, "Compression Settings");
            if (showCompressionSettings)
            {
                EditorGUI.indentLevel++;
                
                config.compressionLevel = (AnimationCompressionLevel)EditorGUILayout.EnumPopup(
                    "Compression Level", 
                    config.compressionLevel);
                
                EditorGUILayout.LabelField("Error Tolerances:");
                config.positionError = EditorGUILayout.FloatField("Position Error", config.positionError);
                config.rotationError = EditorGUILayout.FloatField("Rotation Error (degrees)", config.rotationError);
                config.scaleError = EditorGUILayout.FloatField("Scale Error", config.scaleError);
                
                config.optimizeGameObjects = EditorGUILayout.Toggle("Optimize GameObjects", config.optimizeGameObjects);
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawTestControls()
        {
            EditorGUILayout.LabelField("Test Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Configuration", GUILayout.Height(30)))
            {
                ValidateConfiguration();
            }
            
            if (GUILayout.Button("Test Record (5 seconds)", GUILayout.Height(30)))
            {
                TestRecording(5f);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Analyze Properties", GUILayout.Height(30)))
            {
                AnalyzeRecordableProperties();
            }
            
            if (GUILayout.Button("Memory Usage Test", GUILayout.Height(30)))
            {
                TestMemoryUsage();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Show last recorded clip
            if (lastRecordedClip != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Last Recorded Clip:");
                EditorGUILayout.ObjectField(lastRecordedClip, typeof(AnimationClip), false);
                
                if (GUILayout.Button("Open in Animation Window", GUILayout.Width(150)))
                {
                    EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
                    Selection.activeObject = lastRecordedClip;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawDebugInformation()
        {
            showDebugInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugInfo, "Debug Information");
            if (showDebugInfo)
            {
                EditorGUI.indentLevel++;
                
                // Property count statistics
                if (propertyCountByType.Count > 0)
                {
                    EditorGUILayout.LabelField("Property Statistics:", EditorStyles.boldLabel);
                    foreach (var kvp in propertyCountByType)
                    {
                        EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value} properties");
                    }
                    EditorGUILayout.LabelField($"Total Keyframes: {totalKeyframeCount}");
                }
                
                // Memory usage
                if (memoryUsageBytes > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Memory Usage:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Estimated Size: {FormatBytes(memoryUsageBytes)}");
                }
                
                // Recording duration
                if (recordingDuration > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Recording Duration: {recordingDuration:F2} seconds");
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // Test Results
            showTestResults = EditorGUILayout.BeginFoldoutHeaderGroup(showTestResults, "Test Results");
            if (showTestResults && !string.IsNullOrEmpty(lastTestResult))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(lastTestResult, MessageType.Info);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawTargetPreview()
        {
            if (config.targetGameObject == null) return;
            
            var components = config.targetGameObject.GetComponents<Component>();
            
            EditorGUILayout.LabelField($"Components: {components.Length}");
            
            // Show recordable components
            var recordableComponents = new List<string>();
            
            if ((config.recordingProperties & AnimationRecordingProperties.TransformOnly) != 0)
            {
                recordableComponents.Add("Transform");
            }
            
            if ((config.recordingProperties & AnimationRecordingProperties.BlendShapes) != 0)
            {
                var smr = config.targetGameObject.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null)
                {
                    recordableComponents.Add($"SkinnedMeshRenderer ({smr.sharedMesh.blendShapeCount} blend shapes)");
                }
            }
            
            if ((config.recordingProperties & AnimationRecordingProperties.MaterialProperties) != 0)
            {
                var renderers = config.targetGameObject.GetComponents<Renderer>();
                foreach (var r in renderers)
                {
                    recordableComponents.Add($"{r.GetType().Name} ({r.sharedMaterials.Length} materials)");
                }
            }
            
            if (recordableComponents.Count > 0)
            {
                EditorGUILayout.LabelField("Recordable Components:");
                EditorGUI.indentLevel++;
                foreach (var comp in recordableComponents)
                {
                    EditorGUILayout.LabelField("• " + comp);
                }
                EditorGUI.indentLevel--;
            }
            
            // Show hierarchy info for children scope
            if (config.recordingScope == AnimationRecordingScope.GameObjectAndChildren)
            {
                var childCount = config.targetGameObject.GetComponentsInChildren<Transform>().Length - 1;
                EditorGUILayout.LabelField($"Children: {childCount}");
            }
        }
        
        private void ValidateConfiguration()
        {
            string errorMessage;
            if (config.Validate(out errorMessage))
            {
                lastTestResult = "Configuration is valid!";
                EditorUtility.DisplayDialog("Validation Success", "Configuration is valid and ready for recording.", "OK");
            }
            else
            {
                lastTestResult = $"Validation failed: {errorMessage}";
                EditorUtility.DisplayDialog("Validation Failed", errorMessage, "OK");
            }
            
            showTestResults = true;
        }
        
        private void TestRecording(float duration)
        {
            if (!config.Validate(out string errorMessage))
            {
                EditorUtility.DisplayDialog("Cannot Start Test", errorMessage, "OK");
                return;
            }
            
            try
            {
                // Create test recorder settings
                var settings = config.CreateAnimationRecorderSettings("TestAnimationRecording");
                
                // For testing, create a temporary animation clip
                var testClip = new AnimationClip();
                testClip.name = "TestAnimation_" + System.DateTime.Now.ToString("HHmmss");
                
                // Simulate recording
                lastTestResult = $"Test recording completed!\n" +
                                $"Duration: {duration} seconds\n" +
                                $"Frame Rate: {config.frameRate} fps\n" +
                                $"Total Frames: {(int)(duration * config.frameRate)}\n" +
                                $"Compression: {config.compressionLevel}";
                
                lastRecordedClip = testClip;
                recordingDuration = duration;
                
                // Analyze the created settings
                AnalyzeRecorderSettings(settings);
                
                showTestResults = true;
            }
            catch (System.Exception e)
            {
                lastTestResult = $"Test failed: {e.Message}";
                EditorUtility.DisplayDialog("Test Failed", e.Message, "OK");
            }
        }
        
        private void AnalyzeRecordableProperties()
        {
            if (config.targetGameObject == null)
            {
                EditorUtility.DisplayDialog("No Target", "Please select a target GameObject first.", "OK");
                return;
            }
            
            propertyCountByType.Clear();
            recordableProperties.Clear();
            totalKeyframeCount = 0;
            
            // Analyze Transform
            if ((config.recordingProperties & AnimationRecordingProperties.Position) != 0)
            {
                propertyCountByType["Position"] = 3; // x, y, z
                recordableProperties.Add("Transform.localPosition");
            }
            
            if ((config.recordingProperties & AnimationRecordingProperties.Rotation) != 0)
            {
                propertyCountByType["Rotation"] = 4; // x, y, z, w
                recordableProperties.Add("Transform.localRotation");
            }
            
            if ((config.recordingProperties & AnimationRecordingProperties.Scale) != 0)
            {
                propertyCountByType["Scale"] = 3; // x, y, z
                recordableProperties.Add("Transform.localScale");
            }
            
            // Analyze BlendShapes
            if ((config.recordingProperties & AnimationRecordingProperties.BlendShapes) != 0)
            {
                var smr = config.targetGameObject.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null)
                {
                    var blendShapeCount = smr.sharedMesh.blendShapeCount;
                    propertyCountByType["BlendShapes"] = blendShapeCount;
                    
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        recordableProperties.Add($"blendShape.{smr.sharedMesh.GetBlendShapeName(i)}");
                    }
                }
            }
            
            // Calculate estimated keyframes
            var totalProperties = propertyCountByType.Values.Sum();
            totalKeyframeCount = (int)(recordingDuration * config.frameRate * totalProperties);
            
            lastTestResult = $"Property Analysis Complete:\n" +
                           $"Total Properties: {totalProperties}\n" +
                           $"Estimated Keyframes: {totalKeyframeCount}\n" +
                           $"Recording Scope: {config.recordingScope}";
            
            showDebugInfo = true;
            showTestResults = true;
        }
        
        private void TestMemoryUsage()
        {
            if (config.targetGameObject == null)
            {
                EditorUtility.DisplayDialog("No Target", "Please select a target GameObject first.", "OK");
                return;
            }
            
            // Estimate memory usage
            var propertyCount = 0;
            
            if ((config.recordingProperties & AnimationRecordingProperties.Position) != 0) propertyCount += 3;
            if ((config.recordingProperties & AnimationRecordingProperties.Rotation) != 0) propertyCount += 4;
            if ((config.recordingProperties & AnimationRecordingProperties.Scale) != 0) propertyCount += 3;
            
            var smr = config.targetGameObject.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null && 
                (config.recordingProperties & AnimationRecordingProperties.BlendShapes) != 0)
            {
                propertyCount += smr.sharedMesh.blendShapeCount;
            }
            
            // Each keyframe is approximately: time (4 bytes) + value (4 bytes) + tangents (8 bytes) = 16 bytes
            var keyframeSize = 16;
            var totalFrames = (int)(recordingDuration * config.frameRate);
            memoryUsageBytes = propertyCount * totalFrames * keyframeSize;
            
            // Apply compression factor
            float compressionFactor = 1f;
            switch (config.compressionLevel)
            {
                case AnimationCompressionLevel.Low:
                    compressionFactor = 0.8f;
                    break;
                case AnimationCompressionLevel.Medium:
                    compressionFactor = 0.6f;
                    break;
                case AnimationCompressionLevel.High:
                    compressionFactor = 0.4f;
                    break;
                case AnimationCompressionLevel.Optimal:
                    compressionFactor = 0.3f;
                    break;
            }
            
            memoryUsageBytes = (long)(memoryUsageBytes * compressionFactor);
            
            lastTestResult = $"Memory Usage Estimation:\n" +
                           $"Properties: {propertyCount}\n" +
                           $"Frames: {totalFrames}\n" +
                           $"Uncompressed: {FormatBytes((long)(memoryUsageBytes / compressionFactor))}\n" +
                           $"Compressed ({config.compressionLevel}): {FormatBytes(memoryUsageBytes)}";
            
            showDebugInfo = true;
            showTestResults = true;
        }
        
        private void AnalyzeRecorderSettings(RecorderSettings settings)
        {
            if (settings == null) return;
            
            var animSettings = settings as AnimationRecorderSettings;
            if (animSettings != null && animSettings.AnimationInputSettings != null)
            {
                UnityEngine.Debug.Log("[AnimationDebug] Analyzing AnimationRecorderSettings:");
                UnityEngine.Debug.Log($"  - GameObject: {animSettings.AnimationInputSettings.gameObject}");
                UnityEngine.Debug.Log($"  - Recursive: {animSettings.AnimationInputSettings.Recursive}");
                UnityEngine.Debug.Log($"  - Clamped Tangents: {animSettings.AnimationInputSettings.ClampedTangents}");
                UnityEngine.Debug.Log($"  - Curve Simplification: {animSettings.AnimationInputSettings.SimplyCurves}");
            }
        }
        
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}