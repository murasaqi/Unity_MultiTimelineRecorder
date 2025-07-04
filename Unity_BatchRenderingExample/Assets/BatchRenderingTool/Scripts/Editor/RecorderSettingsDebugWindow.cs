using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.EditorCoroutines.Editor;

namespace BatchRenderingTool
{
    /// <summary>
    /// Debug window for testing RecorderSettings creation and configuration
    /// </summary>
    public class RecorderSettingsDebugWindow : EditorWindow
    {
        private RecorderSettingsType selectedType = RecorderSettingsType.Image;
        private RecorderSettings currentSettings = null;
        private string testOutputPath = "TestRecordings";
        private string statusMessage = "";
        private MessageType statusType = MessageType.Info;
        
        // Movie-specific settings
        private MovieRecorderSettings.VideoRecorderOutputFormat movieFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        private VideoBitrateMode videoBitrateMode = VideoBitrateMode.High;
        private bool captureAudio = false;
        private MovieRecorderPreset moviePreset = MovieRecorderPreset.HighQuality1080p;
        private bool usePreset = true;
        
        // Image-specific settings
        private ImageRecorderSettings.ImageRecorderOutputFormat imageFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        private bool captureAlpha = false;
        
        // Common settings
        private int outputWidth = 1920;
        private int outputHeight = 1080;
        private int frameRate = 24;
        
        // AOV-specific settings
        private AOVType selectedAOVTypes = AOVType.None;
        private AOVOutputFormat aovOutputFormat = AOVOutputFormat.EXR16;
        private AOVPreset aovPreset = AOVPreset.Custom;
        private bool useAOVPreset = false;
        private string customPassName = "";
        private Vector2 aovScrollPosition;
        
        // Alembic-specific settings
        private AlembicExportTargets alembicExportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform;
        private AlembicExportScope alembicExportScope = AlembicExportScope.EntireScene;
        private GameObject alembicTargetGameObject = null;
        private AlembicHandedness alembicHandedness = AlembicHandedness.Left;
        private float alembicScaleFactor = 1f;
        private int alembicSamplesPerFrame = 1;
        private AlembicExportPreset alembicPreset = AlembicExportPreset.Custom;
        private bool useAlembicPreset = false;
        private bool alembicExportUVs = true;
        private bool alembicExportNormals = true;
        private bool alembicSwapYZ = false;
        
        // Animation-specific settings
        private AnimationRecordingProperties animationRecordingProperties = AnimationRecordingProperties.TransformOnly;
        private AnimationRecordingScope animationRecordingScope = AnimationRecordingScope.SingleGameObject;
        private GameObject animationTargetGameObject = null;
        private AnimationInterpolationMode animationInterpolationMode = AnimationInterpolationMode.Linear;
        private AnimationCompressionLevel animationCompressionLevel = AnimationCompressionLevel.Medium;
        private AnimationExportPreset animationPreset = AnimationExportPreset.Custom;
        private bool useAnimationPreset = false;
        private bool animationRecordInWorldSpace = false;
        private bool animationTreatAsHumanoid = false;
        private bool animationRecordRootMotion = true;
        private bool animationOptimizeGameObjects = true;
        private Vector2 animationPropertiesScrollPosition;
        
        // Test render state
        private bool isTestRendering = false;
        private EditorCoroutine testRenderCoroutine = null;
        private float testRenderProgress = 0f;
        private float testDuration = 5f; // Test duration in seconds
        
        [MenuItem("Window/Batch Rendering Tool/Recorder Settings Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<RecorderSettingsDebugWindow>();
            window.titleContent = new GUIContent("Recorder Settings Debug");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Recorder Settings Debug Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            DrawRecorderTypeSelection();
            EditorGUILayout.Space(10);
            
            DrawTypeSpecificSettings();
            EditorGUILayout.Space(10);
            
            DrawCommonSettings();
            EditorGUILayout.Space(10);
            
            DrawActions();
            EditorGUILayout.Space(10);
            
            DrawStatus();
            EditorGUILayout.Space(10);
            
            DrawCurrentSettingsInfo();
        }
        
        private void Update()
        {
            if (isTestRendering)
            {
                Repaint();
            }
        }
        
        private void DrawRecorderTypeSelection()
        {
            EditorGUILayout.LabelField("Recorder Type", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var newType = (RecorderSettingsType)EditorGUILayout.EnumPopup("Type:", selectedType);
            if (newType != selectedType)
            {
                selectedType = newType;
                currentSettings = null; // Clear current settings when type changes
            }
            
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(selectedType))
            {
                EditorGUILayout.HelpBox($"{selectedType} recorder is not yet implemented", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTypeSpecificSettings()
        {
            EditorGUILayout.LabelField("Type-Specific Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            switch (selectedType)
            {
                case RecorderSettingsType.Movie:
                    DrawMovieSettings();
                    break;
                    
                case RecorderSettingsType.Image:
                    DrawImageSettings();
                    break;
                    
                case RecorderSettingsType.AOV:
                    DrawAOVSettings();
                    break;
                    
                case RecorderSettingsType.Alembic:
                    DrawAlembicSettings();
                    break;
                    
                case RecorderSettingsType.Animation:
                    DrawAnimationSettings();
                    break;
                    
                default:
                    EditorGUILayout.LabelField("No specific settings for this type");
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawMovieSettings()
        {
            usePreset = EditorGUILayout.Toggle("Use Preset:", usePreset);
            
            if (usePreset)
            {
                moviePreset = (MovieRecorderPreset)EditorGUILayout.EnumPopup("Preset:", moviePreset);
                
                // Show preset info
                var presetConfig = MovieRecorderSettingsConfig.GetPreset(moviePreset);
                EditorGUILayout.HelpBox(
                    $"Resolution: {presetConfig.width}x{presetConfig.height}\n" +
                    $"Frame Rate: {presetConfig.frameRate} fps\n" +
                    $"Format: {presetConfig.outputFormat}\n" +
                    $"Quality: {presetConfig.videoBitrateMode}\n" +
                    $"Audio: {(presetConfig.captureAudio ? "Yes" : "No")}\n" +
                    $"Alpha: {(presetConfig.captureAlpha ? "Yes" : "No")}",
                    MessageType.Info
                );
                
                // Update values from preset for display
                if (moviePreset != MovieRecorderPreset.Custom)
                {
                    movieFormat = presetConfig.outputFormat;
                    videoBitrateMode = presetConfig.videoBitrateMode;
                    captureAudio = presetConfig.captureAudio;
                    outputWidth = presetConfig.width;
                    outputHeight = presetConfig.height;
                    frameRate = presetConfig.frameRate;
                    captureAlpha = presetConfig.captureAlpha;
                }
            }
            else
            {
                movieFormat = (MovieRecorderSettings.VideoRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", movieFormat);
                videoBitrateMode = (VideoBitrateMode)EditorGUILayout.EnumPopup("Quality:", videoBitrateMode);
                captureAudio = EditorGUILayout.Toggle("Capture Audio:", captureAudio);
                captureAlpha = EditorGUILayout.Toggle("Capture Alpha:", captureAlpha);
            }
            
            if (movieFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MOV)
            {
                #if !UNITY_EDITOR_OSX
                EditorGUILayout.HelpBox("MOV format with ProRes is only available on macOS", MessageType.Warning);
                #endif
            }
            
            if (captureAlpha && movieFormat != MovieRecorderSettings.VideoRecorderOutputFormat.MOV && 
                movieFormat != MovieRecorderSettings.VideoRecorderOutputFormat.WebM)
            {
                EditorGUILayout.HelpBox("Alpha channel is only supported with MOV (ProRes 4444) or WebM formats", MessageType.Warning);
            }
        }
        
        private void DrawImageSettings()
        {
            imageFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)EditorGUILayout.EnumPopup("Format:", imageFormat);
            captureAlpha = EditorGUILayout.Toggle("Capture Alpha:", captureAlpha);
            
            if (captureAlpha && imageFormat != ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                EditorGUILayout.HelpBox("Alpha channel is best supported with EXR format", MessageType.Info);
            }
        }
        
        private void DrawAOVSettings()
        {
            // Check HDRP availability
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                EditorGUILayout.HelpBox("AOV Recorder requires HDRP (High Definition Render Pipeline) package", MessageType.Error);
                return;
            }
            
            useAOVPreset = EditorGUILayout.Toggle("Use Preset:", useAOVPreset);
            
            if (useAOVPreset)
            {
                aovPreset = (AOVPreset)EditorGUILayout.EnumPopup("Preset:", aovPreset);
                
                // Apply preset
                if (aovPreset != AOVPreset.Custom)
                {
                    ApplyAOVPreset(aovPreset);
                }
                
                EditorGUILayout.Space(5);
            }
            
            // Output format
            aovOutputFormat = (AOVOutputFormat)EditorGUILayout.EnumPopup("Output Format:", aovOutputFormat);
            
            // Custom pass name (if custom pass is selected)
            if ((selectedAOVTypes & AOVType.CustomPass) == AOVType.CustomPass)
            {
                customPassName = EditorGUILayout.TextField("Custom Pass Name:", customPassName);
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Select AOV Types:", EditorStyles.boldLabel);
            
            // Display selected count
            int selectedCount = Enum.GetValues(typeof(AOVType))
                .Cast<AOVType>()
                .Where(aov => aov != AOVType.None && (selectedAOVTypes & aov) == aov)
                .Count();
            EditorGUILayout.LabelField($"Selected: {selectedCount} AOVs");
            
            // AOV type selection with categories
            aovScrollPosition = EditorGUILayout.BeginScrollView(aovScrollPosition, GUILayout.Height(200));
            
            var aovsByCategory = AOVTypeInfo.GetAOVsByCategory();
            foreach (var category in aovsByCategory)
            {
                EditorGUILayout.LabelField(category.Key, EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                
                foreach (var aovType in category.Value)
                {
                    var info = AOVTypeInfo.GetInfo(aovType);
                    if (info != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        bool isSelected = (selectedAOVTypes & aovType) == aovType;
                        bool newSelected = EditorGUILayout.ToggleLeft(
                            new GUIContent(info.DisplayName, info.Description),
                            isSelected
                        );
                        
                        if (newSelected != isSelected)
                        {
                            if (newSelected)
                                selectedAOVTypes |= aovType;
                            else
                                selectedAOVTypes &= ~aovType;
                        }
                        
                        // Show recommended format
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(
                            info.RecommendedFormat.ToString(),
                            GUILayout.Width(60)
                        );
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            // Quick selection buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (AOVType aov in Enum.GetValues(typeof(AOVType)))
                {
                    if (aov != AOVType.None)
                        selectedAOVTypes |= aov;
                }
            }
            if (GUILayout.Button("Clear All"))
            {
                selectedAOVTypes = AOVType.None;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void ApplyAOVPreset(AOVPreset preset)
        {
            AOVRecorderSettingsConfig config = null;
            
            switch (preset)
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
            
            if (config != null)
            {
                selectedAOVTypes = config.selectedAOVs;
                aovOutputFormat = config.outputFormat;
                outputWidth = config.width;
                outputHeight = config.height;
                frameRate = config.frameRate;
            }
        }
        
        private void DrawAlembicSettings()
        {
            // Check Alembic availability
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                EditorGUILayout.HelpBox("Alembic Recorder requires Unity Alembic package (com.unity.formats.alembic)", MessageType.Error);
                return;
            }
            
            useAlembicPreset = EditorGUILayout.Toggle("Use Preset:", useAlembicPreset);
            
            if (useAlembicPreset)
            {
                alembicPreset = (AlembicExportPreset)EditorGUILayout.EnumPopup("Preset:", alembicPreset);
                
                // Apply preset
                if (alembicPreset != AlembicExportPreset.Custom)
                {
                    ApplyAlembicPreset(alembicPreset);
                }
                
                EditorGUILayout.Space(5);
            }
            
            // Export scope
            alembicExportScope = (AlembicExportScope)EditorGUILayout.EnumPopup("Export Scope:", alembicExportScope);
            
            // Target GameObject (if needed)
            if (alembicExportScope == AlembicExportScope.TargetGameObject)
            {
                alembicTargetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject:", alembicTargetGameObject, typeof(GameObject), true);
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Export Targets:", EditorStyles.boldLabel);
            
            // Export target checkboxes
            EditorGUI.indentLevel++;
            
            foreach (AlembicExportTargets target in Enum.GetValues(typeof(AlembicExportTargets)))
            {
                if (target == AlembicExportTargets.None) continue;
                
                bool isSelected = (alembicExportTargets & target) == target;
                bool newSelected = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        AlembicExportInfo.GetExportTargetDisplayName(target),
                        AlembicExportInfo.GetExportTargetDescription(target)
                    ),
                    isSelected
                );
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        alembicExportTargets |= target;
                    else
                        alembicExportTargets &= ~target;
                }
            }
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Transform Settings:", EditorStyles.boldLabel);
            
            alembicHandedness = (AlembicHandedness)EditorGUILayout.EnumPopup("Coordinate System:", alembicHandedness);
            alembicScaleFactor = EditorGUILayout.FloatField("Scale Factor:", alembicScaleFactor);
            alembicSwapYZ = EditorGUILayout.Toggle("Swap Y/Z Axis:", alembicSwapYZ);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sampling Settings:", EditorStyles.boldLabel);
            
            alembicSamplesPerFrame = EditorGUILayout.IntSlider("Samples Per Frame:", alembicSamplesPerFrame, 1, 5);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Geometry Settings:", EditorStyles.boldLabel);
            
            alembicExportUVs = EditorGUILayout.Toggle("Export UVs:", alembicExportUVs);
            alembicExportNormals = EditorGUILayout.Toggle("Export Normals:", alembicExportNormals);
            
            // Show info about export
            EditorGUILayout.HelpBox(
                $"Will export as .abc file\n" +
                $"Frame Rate: {frameRate} fps\n" +
                $"Coordinate System: {alembicHandedness}\n" +
                $"Scale: {alembicScaleFactor}x",
                MessageType.Info
            );
        }
        
        private void ApplyAlembicPreset(AlembicExportPreset preset)
        {
            var config = AlembicRecorderSettingsConfig.GetPreset(preset);
            if (config != null)
            {
                alembicExportTargets = config.exportTargets;
                alembicExportScope = config.exportScope;
                alembicHandedness = config.handedness;
                alembicScaleFactor = config.scaleFactor;
                alembicSamplesPerFrame = config.samplesPerFrame;
                alembicExportUVs = config.exportUVs;
                alembicExportNormals = config.exportNormals;
                alembicSwapYZ = config.swapYZ;
                frameRate = (int)config.frameRate;
            }
        }
        
        private void DrawAnimationSettings()
        {
            useAnimationPreset = EditorGUILayout.Toggle("Use Preset:", useAnimationPreset);
            
            if (useAnimationPreset)
            {
                animationPreset = (AnimationExportPreset)EditorGUILayout.EnumPopup("Preset:", animationPreset);
                
                // Apply preset
                if (animationPreset != AnimationExportPreset.Custom)
                {
                    ApplyAnimationPreset(animationPreset);
                }
                
                EditorGUILayout.Space(5);
            }
            
            // Recording scope
            animationRecordingScope = (AnimationRecordingScope)EditorGUILayout.EnumPopup("Recording Scope:", animationRecordingScope);
            
            // Target GameObject (if needed)
            if (animationRecordingScope == AnimationRecordingScope.SingleGameObject ||
                animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren)
            {
                animationTargetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject:", animationTargetGameObject, typeof(GameObject), true);
                
                // Check for humanoid
                if (animationTargetGameObject != null)
                {
                    var animator = animationTargetGameObject.GetComponent<Animator>();
                    if (animator != null && animator.isHuman)
                    {
                        animationTreatAsHumanoid = EditorGUILayout.Toggle("Treat as Humanoid:", animationTreatAsHumanoid);
                        if (animationTreatAsHumanoid)
                        {
                            animationRecordRootMotion = EditorGUILayout.Toggle("Record Root Motion:", animationRecordRootMotion);
                        }
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Recording Properties:", EditorStyles.boldLabel);
            
            // Recording properties checkboxes in scrollview
            animationPropertiesScrollPosition = EditorGUILayout.BeginScrollView(animationPropertiesScrollPosition, GUILayout.Height(120));
            
            EditorGUI.indentLevel++;
            
            // Transform properties
            EditorGUILayout.LabelField("Transform:", EditorStyles.miniBoldLabel);
            bool hasPosition = (animationRecordingProperties & AnimationRecordingProperties.Position) != 0;
            bool hasRotation = (animationRecordingProperties & AnimationRecordingProperties.Rotation) != 0;
            bool hasScale = (animationRecordingProperties & AnimationRecordingProperties.Scale) != 0;
            
            hasPosition = EditorGUILayout.ToggleLeft("Position", hasPosition);
            hasRotation = EditorGUILayout.ToggleLeft("Rotation", hasRotation);
            hasScale = EditorGUILayout.ToggleLeft("Scale", hasScale);
            
            if (hasPosition) animationRecordingProperties |= AnimationRecordingProperties.Position;
            else animationRecordingProperties &= ~AnimationRecordingProperties.Position;
            
            if (hasRotation) animationRecordingProperties |= AnimationRecordingProperties.Rotation;
            else animationRecordingProperties &= ~AnimationRecordingProperties.Rotation;
            
            if (hasScale) animationRecordingProperties |= AnimationRecordingProperties.Scale;
            else animationRecordingProperties &= ~AnimationRecordingProperties.Scale;
            
            // Additional properties
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Additional Properties:", EditorStyles.miniBoldLabel);
            
            bool hasBlendShapes = (animationRecordingProperties & AnimationRecordingProperties.BlendShapes) != 0;
            bool hasMaterialProps = (animationRecordingProperties & AnimationRecordingProperties.MaterialProperties) != 0;
            bool hasLightProps = (animationRecordingProperties & AnimationRecordingProperties.LightProperties) != 0;
            bool hasCameraProps = (animationRecordingProperties & AnimationRecordingProperties.CameraProperties) != 0;
            
            hasBlendShapes = EditorGUILayout.ToggleLeft("Blend Shapes", hasBlendShapes);
            hasMaterialProps = EditorGUILayout.ToggleLeft("Material Properties", hasMaterialProps);
            hasLightProps = EditorGUILayout.ToggleLeft("Light Properties", hasLightProps);
            hasCameraProps = EditorGUILayout.ToggleLeft("Camera Properties", hasCameraProps);
            
            if (hasBlendShapes) animationRecordingProperties |= AnimationRecordingProperties.BlendShapes;
            else animationRecordingProperties &= ~AnimationRecordingProperties.BlendShapes;
            
            if (hasMaterialProps) animationRecordingProperties |= AnimationRecordingProperties.MaterialProperties;
            else animationRecordingProperties &= ~AnimationRecordingProperties.MaterialProperties;
            
            if (hasLightProps) animationRecordingProperties |= AnimationRecordingProperties.LightProperties;
            else animationRecordingProperties &= ~AnimationRecordingProperties.LightProperties;
            
            if (hasCameraProps) animationRecordingProperties |= AnimationRecordingProperties.CameraProperties;
            else animationRecordingProperties &= ~AnimationRecordingProperties.CameraProperties;
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndScrollView();
            
            // Quick selection buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Transform Only"))
            {
                animationRecordingProperties = AnimationRecordingProperties.TransformOnly;
            }
            if (GUILayout.Button("All Properties"))
            {
                animationRecordingProperties = AnimationRecordingProperties.AllProperties;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sampling Settings:", EditorStyles.boldLabel);
            
            animationInterpolationMode = (AnimationInterpolationMode)EditorGUILayout.EnumPopup("Interpolation:", animationInterpolationMode);
            animationRecordInWorldSpace = EditorGUILayout.Toggle("Record in World Space:", animationRecordInWorldSpace);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Compression Settings:", EditorStyles.boldLabel);
            
            animationCompressionLevel = (AnimationCompressionLevel)EditorGUILayout.EnumPopup("Compression Level:", animationCompressionLevel);
            animationOptimizeGameObjects = EditorGUILayout.Toggle("Optimize GameObjects:", animationOptimizeGameObjects);
            
            // Show compression tolerances based on level
            if (animationCompressionLevel != AnimationCompressionLevel.None)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Position Error: {AnimationRecordingInfo.CompressionPresets.GetPositionErrorTolerance(animationCompressionLevel):F3}");
                EditorGUILayout.LabelField($"Rotation Error: {AnimationRecordingInfo.CompressionPresets.GetRotationErrorTolerance(animationCompressionLevel):F1}°");
                EditorGUILayout.LabelField($"Scale Error: {AnimationRecordingInfo.CompressionPresets.GetScaleErrorTolerance(animationCompressionLevel):F3}");
                EditorGUI.indentLevel--;
            }
            
            // Show info about export
            EditorGUILayout.HelpBox(
                $"Will export as .anim file\n" +
                $"Frame Rate: {frameRate} fps\n" +
                $"Recording: {GetRecordingPropertiesString()}\n" +
                $"Compression: {animationCompressionLevel}",
                MessageType.Info
            );
        }
        
        private void ApplyAnimationPreset(AnimationExportPreset preset)
        {
            var config = AnimationRecorderSettingsConfig.GetPreset(preset);
            if (config != null)
            {
                animationRecordingProperties = config.recordingProperties;
                animationRecordingScope = config.recordingScope;
                animationInterpolationMode = config.interpolationMode;
                animationCompressionLevel = config.compressionLevel;
                animationRecordInWorldSpace = config.recordInWorldSpace;
                animationTreatAsHumanoid = config.treatAsHumanoid;
                animationRecordRootMotion = config.recordRootMotion;
                animationOptimizeGameObjects = config.optimizeGameObjects;
                frameRate = (int)config.frameRate;
            }
        }
        
        private string GetRecordingPropertiesString()
        {
            if (animationRecordingProperties == AnimationRecordingProperties.None)
                return "None";
            if (animationRecordingProperties == AnimationRecordingProperties.TransformOnly)
                return "Transform Only";
            if (animationRecordingProperties == AnimationRecordingProperties.AllProperties)
                return "All Properties";
            
            var properties = new List<string>();
            foreach (AnimationRecordingProperties prop in Enum.GetValues(typeof(AnimationRecordingProperties)))
            {
                if (prop != AnimationRecordingProperties.None && 
                    prop != AnimationRecordingProperties.TransformOnly &&
                    prop != AnimationRecordingProperties.TransformAndBlendShapes &&
                    prop != AnimationRecordingProperties.AllProperties &&
                    (animationRecordingProperties & prop) != 0)
                {
                    properties.Add(prop.ToString());
                }
            }
            
            return string.Join(", ", properties);
        }
        
        private void DrawCommonSettings()
        {
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Disable fields when using movie preset
            bool isUsingMoviePreset = selectedType == RecorderSettingsType.Movie && usePreset && moviePreset != MovieRecorderPreset.Custom;
            EditorGUI.BeginDisabledGroup(isUsingMoviePreset);
            
            outputWidth = EditorGUILayout.IntField("Width:", outputWidth);
            outputHeight = EditorGUILayout.IntField("Height:", outputHeight);
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            
            EditorGUI.EndDisabledGroup();
            
            if (isUsingMoviePreset)
            {
                EditorGUILayout.HelpBox("Resolution and frame rate are controlled by the selected preset", MessageType.Info);
            }
            
            EditorGUILayout.BeginHorizontal();
            testOutputPath = EditorGUILayout.TextField("Output Path:", testOutputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", testOutputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    testOutputPath = Path.GetRelativePath(Application.dataPath + "/..", path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = RecorderSettingsFactory.IsRecorderTypeSupported(selectedType) && !isTestRendering;
            
            if (GUILayout.Button("Create Settings", GUILayout.Height(30)))
            {
                CreateSettings();
            }
            
            GUI.enabled = currentSettings != null && !isTestRendering;
            
            if (GUILayout.Button("Validate Settings", GUILayout.Height(30)))
            {
                ValidateSettings();
            }
            
            GUI.enabled = currentSettings != null && !isTestRendering && !EditorApplication.isPlaying;
            
            if (GUILayout.Button($"Test Render ({testDuration}s)", GUILayout.Height(30)))
            {
                TestRender();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // Test duration slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test Duration:", GUILayout.Width(100));
            testDuration = EditorGUILayout.Slider(testDuration, 1f, 30f);
            EditorGUILayout.EndHorizontal();
            
            // Test render progress
            if (isTestRendering)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), testRenderProgress, $"Test Rendering... {(int)(testRenderProgress * 100)}%");
                
                if (GUILayout.Button("Stop Test", GUILayout.Height(25)))
                {
                    StopTestRender();
                }
            }
            
            GUI.enabled = currentSettings != null;
            
            if (GUILayout.Button("Clear Settings", GUILayout.Height(25)))
            {
                ClearSettings();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }
        
        private void DrawCurrentSettingsInfo()
        {
            EditorGUILayout.LabelField("Current Settings Info", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (currentSettings == null)
            {
                EditorGUILayout.LabelField("No settings created");
            }
            else
            {
                EditorGUILayout.LabelField($"Type: {RecorderSettingsFactory.DetectRecorderType(currentSettings)}");
                EditorGUILayout.LabelField($"Name: {currentSettings.name}");
                EditorGUILayout.LabelField($"Enabled: {currentSettings.Enabled}");
                EditorGUILayout.LabelField($"Frame Rate: {currentSettings.FrameRate}");
                
                if (RecorderSettingsHelper.IsAOVRecorderSettings(currentSettings))
                {
                    EditorGUILayout.LabelField($"Type: AOV Recorder");
                    if (currentSettings.name.Contains("_AOV_"))
                    {
                        var parts = currentSettings.name.Split(new[] { "_AOV_" }, System.StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            EditorGUILayout.LabelField($"AOV Type: {parts[1]}");
                        }
                    }
                    if (currentSettings is ImageRecorderSettings aovImageSettings)
                    {
                        EditorGUILayout.LabelField($"Format: {aovImageSettings.OutputFormat}");
                        EditorGUILayout.LabelField($"Output: {aovImageSettings.OutputFile}");
                    }
                }
                else if (currentSettings is MovieRecorderSettings movieSettings)
                {
                    EditorGUILayout.LabelField($"Format: {movieSettings.OutputFormat}");
                    EditorGUILayout.LabelField($"Audio: {movieSettings.CaptureAudio}");
                    EditorGUILayout.LabelField($"Output: {movieSettings.OutputFile}");
                }
                else if (currentSettings is ImageRecorderSettings imageSettings)
                {
                    EditorGUILayout.LabelField($"Format: {imageSettings.OutputFormat}");
                    EditorGUILayout.LabelField($"Alpha: {imageSettings.CaptureAlpha}");
                    EditorGUILayout.LabelField($"Output: {imageSettings.OutputFile}");
                }
                else if (RecorderSettingsHelper.IsAlembicRecorderSettings(currentSettings))
                {
                    EditorGUILayout.LabelField($"Type: Alembic Recorder");
                    EditorGUILayout.LabelField($"Output: {currentSettings.name}.abc");
                    if (currentSettings is ImageRecorderSettings alembicPlaceholder)
                    {
                        EditorGUILayout.LabelField($"Format: Alembic (.abc)");
                        EditorGUILayout.LabelField($"Placeholder Mode: Yes");
                    }
                }
                else if (RecorderSettingsHelper.IsAnimationRecorderSettings(currentSettings))
                {
                    EditorGUILayout.LabelField($"Type: Animation Recorder");
                    EditorGUILayout.LabelField($"Output: {currentSettings.name}.anim");
                    if (currentSettings is ImageRecorderSettings animationPlaceholder)
                    {
                        EditorGUILayout.LabelField($"Format: Animation (.anim)");
                        EditorGUILayout.LabelField($"Placeholder Mode: Yes");
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void CreateSettings()
        {
            try
            {
                string settingsName = $"Debug_{selectedType}_{DateTime.Now.Ticks}";
                
                if (selectedType == RecorderSettingsType.Movie && usePreset && moviePreset != MovieRecorderPreset.Custom)
                {
                    // Create with preset
                    currentSettings = RecorderSettingsFactory.CreateMovieRecorderSettings(settingsName, moviePreset);
                }
                else
                {
                    // Create with default or custom settings
                    if (selectedType == RecorderSettingsType.AOV)
                    {
                        // Create AOV settings
                        var aovConfig = new AOVRecorderSettingsConfig
                        {
                            selectedAOVs = selectedAOVTypes,
                            outputFormat = aovOutputFormat,
                            width = outputWidth,
                            height = outputHeight,
                            frameRate = frameRate,
                            capFrameRate = true,
                            customPassName = customPassName
                        };
                        
                        // Validate configuration
                        string aovError;
                        if (!aovConfig.Validate(out aovError))
                        {
                            SetStatus($"AOV configuration error: {aovError}", MessageType.Error);
                            return;
                        }
                        
                        // Create multiple settings for AOV
                        var aovSettingsList = RecorderSettingsFactory.CreateAOVRecorderSettings(settingsName, aovConfig);
                        if (aovSettingsList != null && aovSettingsList.Count > 0)
                        {
                            // For debug window, use the first AOV setting
                            currentSettings = aovSettingsList[0];
                            SetStatus($"Created {aovSettingsList.Count} AOV RecorderSettings", MessageType.Info);
                        }
                        else
                        {
                            SetStatus("Failed to create AOV settings", MessageType.Error);
                            return;
                        }
                    }
                    else if (selectedType == RecorderSettingsType.Alembic)
                    {
                        // Create Alembic settings
                        var alembicConfig = new AlembicRecorderSettingsConfig
                        {
                            exportTargets = alembicExportTargets,
                            exportScope = alembicExportScope,
                            targetGameObject = alembicTargetGameObject,
                            handedness = alembicHandedness,
                            scaleFactor = alembicScaleFactor,
                            samplesPerFrame = alembicSamplesPerFrame,
                            frameRate = frameRate,
                            exportUVs = alembicExportUVs,
                            exportNormals = alembicExportNormals,
                            swapYZ = alembicSwapYZ
                        };
                        
                        // Validate configuration
                        string alembicError;
                        if (!alembicConfig.Validate(out alembicError))
                        {
                            SetStatus($"Alembic configuration error: {alembicError}", MessageType.Error);
                            return;
                        }
                        
                        currentSettings = RecorderSettingsFactory.CreateAlembicRecorderSettings(settingsName, alembicConfig);
                        if (currentSettings == null)
                        {
                            SetStatus("Failed to create Alembic settings", MessageType.Error);
                            return;
                        }
                    }
                    else if (selectedType == RecorderSettingsType.Animation)
                    {
                        // Create Animation settings
                        var animationConfig = new AnimationRecorderSettingsConfig
                        {
                            recordingProperties = animationRecordingProperties,
                            recordingScope = animationRecordingScope,
                            targetGameObject = animationTargetGameObject,
                            interpolationMode = animationInterpolationMode,
                            compressionLevel = animationCompressionLevel,
                            frameRate = frameRate,
                            recordInWorldSpace = animationRecordInWorldSpace,
                            treatAsHumanoid = animationTreatAsHumanoid,
                            recordRootMotion = animationRecordRootMotion,
                            optimizeGameObjects = animationOptimizeGameObjects
                        };
                        
                        // Validate configuration
                        string animationError;
                        if (!animationConfig.Validate(out animationError))
                        {
                            SetStatus($"Animation configuration error: {animationError}", MessageType.Error);
                            return;
                        }
                        
                        currentSettings = RecorderSettingsFactory.CreateAnimationRecorderSettings(settingsName, animationConfig);
                        if (currentSettings == null)
                        {
                            SetStatus("Failed to create Animation settings", MessageType.Error);
                            return;
                        }
                    }
                    else
                    {
                        currentSettings = RecorderSettingsFactory.CreateRecorderSettings(selectedType, settingsName);
                    }
                    
                    // Apply custom settings
                    if (currentSettings is MovieRecorderSettings movieSettings)
                    {
                        if (!usePreset)
                        {
                            // Create custom config
                            var config = new MovieRecorderSettingsConfig
                            {
                                outputFormat = movieFormat,
                                videoBitrateMode = videoBitrateMode,
                                captureAudio = captureAudio,
                                captureAlpha = captureAlpha,
                                width = outputWidth,
                                height = outputHeight,
                                frameRate = frameRate,
                                capFrameRate = true
                            };
                            
                            // Validate and apply
                            string errorMessage;
                            if (config.Validate(out errorMessage))
                            {
                                config.ApplyToSettings(movieSettings);
                            }
                            else
                            {
                                SetStatus($"Configuration warning: {errorMessage}", MessageType.Warning);
                            }
                        }
                    }
                    else if (currentSettings is ImageRecorderSettings imageSettings)
                    {
                        imageSettings.OutputFormat = imageFormat;
                        imageSettings.CaptureAlpha = captureAlpha;
                        imageSettings.imageInputSettings = new GameViewInputSettings
                        {
                            OutputWidth = outputWidth,
                            OutputHeight = outputHeight
                        };
                        
                        // Set frame rate
                        imageSettings.FrameRate = frameRate;
                    }
                }
                
                // Configure output path
                string outputFile = $"{testOutputPath}/DebugTest";
                RecorderSettingsHelper.ConfigureOutputPath(currentSettings, outputFile, selectedType);
                
                SetStatus($"{selectedType} RecorderSettings created successfully", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"Failed to create settings: {e.Message}", MessageType.Error);
                Debug.LogError($"RecorderSettingsDebugWindow: {e}");
            }
        }
        
        private void ValidateSettings()
        {
            if (currentSettings == null) return;
            
            string errorMessage;
            bool isValid = RecorderSettingsHelper.ValidateRecorderSettings(currentSettings, out errorMessage);
            
            if (isValid)
            {
                SetStatus("Settings are valid", MessageType.Info);
            }
            else
            {
                SetStatus($"Validation failed: {errorMessage}", MessageType.Error);
            }
        }
        
        private void TestRender()
        {
            if (currentSettings == null)
            {
                SetStatus("No settings to test", MessageType.Warning);
                return;
            }
            
            // Find a PlayableDirector in the scene
            var director = FindObjectOfType<PlayableDirector>();
            if (director == null)
            {
                SetStatus("No PlayableDirector found in scene for test", MessageType.Warning);
                return;
            }
            
            if (director.playableAsset == null || !(director.playableAsset is TimelineAsset))
            {
                SetStatus("PlayableDirector doesn't have a valid Timeline", MessageType.Warning);
                return;
            }
            
            // Start test render
            isTestRendering = true;
            testRenderProgress = 0f;
            SetStatus($"Starting {testDuration}s test render with {director.gameObject.name}...", MessageType.Info);
            
            if (testRenderCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(testRenderCoroutine);
            }
            
            testRenderCoroutine = EditorCoroutineUtility.StartCoroutine(TestRenderCoroutine(director), this);
        }
        
        private IEnumerator TestRenderCoroutine(PlayableDirector director)
        {
            var timeline = director.playableAsset as TimelineAsset;
            float originalDuration = (float)timeline.duration;
            
            // Create test timeline
            var testTimeline = CreateTestTimeline(director, timeline);
            if (testTimeline == null)
            {
                SetStatus("Failed to create test timeline", MessageType.Error);
                isTestRendering = false;
                yield break;
            }
            
            // Save original state
            bool originalPlayOnAwake = director.playOnAwake;
            director.playOnAwake = false;
            
            // Enter Play Mode
            EditorApplication.isPlaying = true;
            
            // Wait for Play Mode
            while (!EditorApplication.isPlaying)
            {
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Find director in Play Mode
            director = GameObject.Find(director.gameObject.name)?.GetComponent<PlayableDirector>();
            if (director == null)
            {
                SetStatus("Failed to find director in Play Mode", MessageType.Error);
                EditorApplication.isPlaying = false;
                isTestRendering = false;
                yield break;
            }
            
            // Create test renderer
            var testGameObject = new GameObject("TestRenderer");
            var testDirector = testGameObject.AddComponent<PlayableDirector>();
            testDirector.playableAsset = testTimeline;
            testDirector.playOnAwake = false;
            
            // Bind control track
            foreach (var output in testTimeline.outputs)
            {
                if (output.sourceObject is ControlTrack track)
                {
                    testDirector.SetGenericBinding(track, director.gameObject);
                }
            }
            
            // Start playback
            testDirector.time = 0;
            testDirector.Play();
            
            // Monitor progress
            float elapsedTime = 0;
            while (testDirector.state == PlayState.Playing && elapsedTime < testDuration + 2f)
            {
                testRenderProgress = Mathf.Clamp01((float)(testDirector.time / testDuration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Cleanup
            if (testDirector != null)
            {
                testDirector.Stop();
                GameObject.DestroyImmediate(testGameObject);
            }
            
            // Exit Play Mode
            EditorApplication.isPlaying = false;
            
            while (EditorApplication.isPlaying)
            {
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Restore state
            director = FindObjectOfType<PlayableDirector>();
            if (director != null)
            {
                director.playOnAwake = originalPlayOnAwake;
            }
            
            // Cleanup test timeline
            CleanupTestTimeline();
            
            // Complete
            isTestRendering = false;
            testRenderProgress = 1f;
            
            string outputInfo = selectedType == RecorderSettingsType.Movie ? 
                $"Test movie saved to: {testOutputPath}/TestRender" : 
                $"Test images saved to: {testOutputPath}/TestRender";
                
            SetStatus($"Test render complete! {outputInfo}", MessageType.Info);
        }
        
        private TimelineAsset CreateTestTimeline(PlayableDirector director, TimelineAsset originalTimeline)
        {
            try
            {
                // Create timeline
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = "TestRenderTimeline";
                timeline.editorSettings.frameRate = frameRate;
                
                // Save as temporary asset
                string tempPath = "Assets/BatchRenderingTool/Temp/TestTimeline_" + DateTime.Now.Ticks + ".playable";
                string tempDir = Path.GetDirectoryName(tempPath);
                
                if (!AssetDatabase.IsValidFolder(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                    AssetDatabase.Refresh();
                }
                
                AssetDatabase.CreateAsset(timeline, tempPath);
                
                // Create control track
                var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
                var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
                controlClip.displayName = director.gameObject.name;
                controlClip.start = 0;
                controlClip.duration = testDuration;
                
                var controlAsset = controlClip.asset as ControlPlayableAsset;
                string exposedName = UnityEditor.GUID.Generate().ToString();
                controlAsset.sourceGameObject.exposedName = exposedName;
                controlAsset.sourceGameObject.defaultValue = director.gameObject;
                controlAsset.updateDirector = true;
                
                // Create recorder track
                var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Recorder Track");
                var recorderClip = recorderTrack.CreateClip<RecorderClip>();
                recorderClip.displayName = "Test Recording";
                recorderClip.start = 0;
                recorderClip.duration = testDuration;
                
                var recorderAsset = recorderClip.asset as RecorderClip;
                recorderAsset.settings = currentSettings;
                
                // Ensure recorder type is set
                RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, currentSettings);
                
                // Save
                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();
                
                return timeline;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create test timeline: {e}");
                return null;
            }
        }
        
        private void CleanupTestTimeline()
        {
            // Find and delete test timeline assets
            string[] testAssets = AssetDatabase.FindAssets("TestRenderTimeline t:TimelineAsset", new[] { "Assets/BatchRenderingTool/Temp" });
            foreach (string guid in testAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }
            
            AssetDatabase.Refresh();
        }
        
        private void StopTestRender()
        {
            if (testRenderCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(testRenderCoroutine);
                testRenderCoroutine = null;
            }
            
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            
            isTestRendering = false;
            testRenderProgress = 0f;
            SetStatus("Test render stopped", MessageType.Warning);
        }
        
        private void ClearSettings()
        {
            currentSettings = null;
            SetStatus("Settings cleared", MessageType.Info);
        }
        
        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }
    }
}