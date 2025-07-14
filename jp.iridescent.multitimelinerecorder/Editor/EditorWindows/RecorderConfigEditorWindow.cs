using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;

namespace Unity.MultiTimelineRecorder.RecorderConfigEditors
{
    /// <summary>
    /// レコーダー設定を編集するためのポップアップウィンドウ
    /// </summary>
    public class RecorderConfigEditorWindow : EditorWindow
    {
        private MultiRecorderConfig.RecorderConfigItem configItem;
        private System.Action onConfigChanged;
        private Vector2 scrollPosition;
        
        public static void ShowWindow(MultiRecorderConfig.RecorderConfigItem item, System.Action onChanged)
        {
            var window = CreateInstance<RecorderConfigEditorWindow>();
            window.configItem = item;
            window.onConfigChanged = onChanged;
            window.titleContent = new GUIContent($"Edit {item.name}");
            
            // Set window size based on recorder type
            var size = GetWindowSizeForType(item.recorderType);
            window.minSize = size;
            window.maxSize = size;
            
            window.ShowUtility();
        }
        
        private static Vector2 GetWindowSizeForType(RecorderSettingsType type)
        {
            return type switch
            {
                RecorderSettingsType.Movie => new Vector2(400, 500),
                RecorderSettingsType.Animation => new Vector2(400, 600),
                RecorderSettingsType.Alembic => new Vector2(400, 550),
                RecorderSettingsType.AOV => new Vector2(450, 700),
                RecorderSettingsType.FBX => new Vector2(400, 600),
                _ => new Vector2(400, 400)
            };
        }
        
        private void OnGUI()
        {
            if (configItem == null)
            {
                Close();
                return;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Header
            EditorGUILayout.LabelField($"{configItem.recorderType} Recorder Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Common settings
            DrawCommonSettings();
            
            EditorGUILayout.Space();
            
            // Type-specific settings
            switch (configItem.recorderType)
            {
                case RecorderSettingsType.Image:
                    DrawImageSettings();
                    break;
                case RecorderSettingsType.Movie:
                    DrawMovieSettings();
                    break;
                case RecorderSettingsType.Animation:
                    DrawAnimationSettings();
                    break;
                case RecorderSettingsType.Alembic:
                    DrawAlembicSettings();
                    break;
                case RecorderSettingsType.AOV:
                    DrawAOVSettings();
                    break;
                case RecorderSettingsType.FBX:
                    DrawFBXSettings();
                    break;
            }
            
            EditorGUILayout.Space();
            
            // Buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Apply", GUILayout.Height(30)))
            {
                string errorMessage;
                if (configItem.Validate(out errorMessage))
                {
                    onConfigChanged?.Invoke();
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Error", errorMessage, "OK");
                }
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawCommonSettings()
        {
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            
            configItem.name = EditorGUILayout.TextField("Name", configItem.name);
            configItem.fileName = EditorGUILayout.TextField("File Name", configItem.fileName);
            configItem.takeNumber = EditorGUILayout.IntField("Take Number", configItem.takeNumber);
            
            // Show wildcards help
            if (GUILayout.Button("Wildcards", EditorStyles.miniButton))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("<Scene>"), false, () => InsertWildcard("<Scene>"));
                menu.AddItem(new GUIContent("<Take>"), false, () => InsertWildcard("<Take>"));
                menu.AddItem(new GUIContent("<Frame>"), false, () => InsertWildcard("<Frame>"));
                menu.AddItem(new GUIContent("<Time>"), false, () => InsertWildcard("<Time>"));
                menu.AddItem(new GUIContent("<Resolution>"), false, () => InsertWildcard("<Resolution>"));
                
                if (configItem.recorderType == RecorderSettingsType.Animation || 
                    configItem.recorderType == RecorderSettingsType.Alembic ||
                    configItem.recorderType == RecorderSettingsType.FBX)
                {
                    menu.AddItem(new GUIContent("<GameObject>"), false, () => InsertWildcard("<GameObject>"));
                }
                
                if (configItem.recorderType == RecorderSettingsType.AOV)
                {
                    menu.AddItem(new GUIContent("<AOVType>"), false, () => InsertWildcard("<AOVType>"));
                }
                
                menu.ShowAsContext();
            }
            
            EditorGUILayout.Space();
            
            // Resolution and frame rate (if not using global)
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            
            configItem.width = EditorGUILayout.IntField("Width", configItem.width);
            configItem.height = EditorGUILayout.IntField("Height", configItem.height);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button("HD", GUILayout.Width(40)))
            {
                configItem.width = 1920;
                configItem.height = 1080;
            }
            if (GUILayout.Button("2K", GUILayout.Width(40)))
            {
                configItem.width = 2048;
                configItem.height = 1080;
            }
            if (GUILayout.Button("4K", GUILayout.Width(40)))
            {
                configItem.width = 3840;
                configItem.height = 2160;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            configItem.frameRate = EditorGUILayout.IntSlider("Frame Rate", configItem.frameRate, 1, 120);
            configItem.capFrameRate = EditorGUILayout.Toggle("Cap Frame Rate", configItem.capFrameRate);
        }
        
        private void InsertWildcard(string wildcard)
        {
            configItem.fileName += wildcard;
            
            // Force GUI to update immediately
            GUI.changed = true;
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            GUIUtility.ExitGUI();
        }
        
        private void DrawImageSettings()
        {
            EditorGUILayout.LabelField("Image Settings", EditorStyles.boldLabel);
            
            configItem.imageFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)
                EditorGUILayout.EnumPopup("Format", configItem.imageFormat);
                
            if (configItem.imageFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                configItem.imageQuality = EditorGUILayout.IntSlider("JPEG Quality", configItem.imageQuality, 1, 100);
            }
        }
        
        private void DrawMovieSettings()
        {
            EditorGUILayout.LabelField("Movie Settings", EditorStyles.boldLabel);
            
            var movieConfig = configItem.movieConfig;
            
            // Video settings
            EditorGUILayout.Space();
            movieConfig.outputFormat = (MovieRecorderSettings.VideoRecorderOutputFormat)
                EditorGUILayout.EnumPopup("Video Format", movieConfig.outputFormat);
                
            movieConfig.videoBitrateMode = (VideoBitrateMode)
                EditorGUILayout.EnumPopup("Quality", movieConfig.videoBitrateMode);
                
            if (movieConfig.videoBitrateMode == VideoBitrateMode.Low)
            {
                movieConfig.customBitrate = EditorGUILayout.IntField("Bitrate (kbps)", movieConfig.customBitrate);
            }
            
            movieConfig.captureAlpha = EditorGUILayout.Toggle("Capture Alpha", movieConfig.captureAlpha);
            
            // Audio settings
            EditorGUILayout.Space();
            movieConfig.captureAudio = EditorGUILayout.Toggle("Capture Audio", movieConfig.captureAudio);
            if (movieConfig.captureAudio)
            {
                movieConfig.audioBitrate = (AudioBitRateMode)EditorGUILayout.EnumPopup("Audio Quality", movieConfig.audioBitrate);
            }
        }
        
        private void DrawAnimationSettings()
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            
            var animConfig = configItem.animationConfig;
            
            // Target
            animConfig.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Target GameObject", animConfig.targetGameObject, typeof(GameObject), true);
                
            animConfig.recordingScope = (AnimationRecordingScope)
                EditorGUILayout.EnumPopup("Recording Scope", animConfig.recordingScope);
                
            // Note: recordingScope handles hierarchy recording
            
            // Properties
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Properties", EditorStyles.miniLabel);
            
            animConfig.recordingProperties = (AnimationRecordingProperties)
                EditorGUILayout.EnumFlagsField("Record", animConfig.recordingProperties);
            
            // Compression
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Compression", EditorStyles.miniLabel);
            
            animConfig.compressionLevel = (AnimationCompressionLevel)
                EditorGUILayout.EnumPopup("Compression", animConfig.compressionLevel);
            
            if (animConfig.compressionLevel != AnimationCompressionLevel.None)
            {
                animConfig.positionError = EditorGUILayout.FloatField("Position Error", animConfig.positionError);
                animConfig.rotationError = EditorGUILayout.FloatField("Rotation Error", animConfig.rotationError);
                animConfig.scaleError = EditorGUILayout.FloatField("Scale Error", animConfig.scaleError);
            }
        }
        
        private void DrawAlembicSettings()
        {
            EditorGUILayout.LabelField("Alembic Settings", EditorStyles.boldLabel);
            
            var alembicConfig = configItem.alembicConfig;
            
            // Export scope
            alembicConfig.exportScope = (AlembicExportScope)
                EditorGUILayout.EnumPopup("Export Scope", alembicConfig.exportScope);
                
            if (alembicConfig.exportScope == AlembicExportScope.TargetGameObject)
            {
                alembicConfig.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                    "Target GameObject", alembicConfig.targetGameObject, typeof(GameObject), true);
            }
            
            // Export targets
            EditorGUILayout.Space();
            alembicConfig.exportTargets = (AlembicExportTargets)
                EditorGUILayout.EnumFlagsField("Export", alembicConfig.exportTargets);
                
            alembicConfig.flattenHierarchy = EditorGUILayout.Toggle("Flatten Hierarchy", alembicConfig.flattenHierarchy);
            
            // Format settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Format", EditorStyles.miniLabel);
            
            alembicConfig.scaleFactor = EditorGUILayout.FloatField("Scale Factor", alembicConfig.scaleFactor);
            alembicConfig.handedness = (AlembicHandedness)
                EditorGUILayout.EnumPopup("Handedness", alembicConfig.handedness);
                
            // Time sampling
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Time Sampling", EditorStyles.miniLabel);
            
            alembicConfig.timeSamplingMode = (AlembicTimeSamplingMode)
                EditorGUILayout.EnumPopup("Type", alembicConfig.timeSamplingMode);
                
            if (alembicConfig.timeSamplingMode != AlembicTimeSamplingMode.Uniform)
            {
                alembicConfig.frameRate = EditorGUILayout.FloatField("Sample Rate", alembicConfig.frameRate);
            }
        }
        
        private void DrawAOVSettings()
        {
            EditorGUILayout.LabelField("AOV Settings", EditorStyles.boldLabel);
            
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                EditorGUILayout.HelpBox("AOV Recording requires HDRP", MessageType.Error);
                return;
            }
            
            var aovConfig = configItem.aovConfig;
            
            // AOV type selection
            EditorGUILayout.LabelField("AOV Types", EditorStyles.miniLabel);
            
            // Quick select buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All", EditorStyles.miniButton))
            {
                aovConfig.selectedAOVs = ~AOVType.None;
            }
            if (GUILayout.Button("None", EditorStyles.miniButton))
            {
                aovConfig.selectedAOVs = AOVType.None;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // AOV categories
            var categories = AOVTypeInfo.GetAOVsByCategory();
            foreach (var category in categories)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(category.Key, EditorStyles.miniLabel);
                
                EditorGUI.indentLevel++;
                foreach (var aovType in category.Value)
                {
                    bool isSelected = (aovConfig.selectedAOVs & aovType) != 0;
                    bool newSelected = EditorGUILayout.ToggleLeft(
                        AOVTypeInfo.GetInfo(aovType)?.DisplayName ?? aovType.ToString(),
                        isSelected);
                        
                    if (newSelected != isSelected)
                    {
                        if (newSelected)
                            aovConfig.selectedAOVs |= aovType;
                        else
                            aovConfig.selectedAOVs &= ~aovType;
                    }
                }
                EditorGUI.indentLevel--;
            }
            
            // Output format
            EditorGUILayout.Space();
            aovConfig.outputFormat = (AOVOutputFormat)
                EditorGUILayout.EnumPopup("Output Format", aovConfig.outputFormat);
                
            aovConfig.compressionEnabled = EditorGUILayout.Toggle("Compression", aovConfig.compressionEnabled);
            aovConfig.flipVertical = EditorGUILayout.Toggle("Flip Vertical", aovConfig.flipVertical);
            
            // Custom pass
            if ((aovConfig.selectedAOVs & AOVType.CustomPass) != 0)
            {
                EditorGUILayout.Space();
                aovConfig.customPassName = EditorGUILayout.TextField("Custom Pass Name", aovConfig.customPassName);
            }
        }
        
        private void DrawFBXSettings()
        {
            EditorGUILayout.LabelField("FBX Settings", EditorStyles.boldLabel);
            
            var fbxConfig = configItem.fbxConfig;
            
            // Target
            fbxConfig.targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Target GameObject", fbxConfig.targetGameObject, typeof(GameObject), true);
                
            fbxConfig.recordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", fbxConfig.recordHierarchy);
            fbxConfig.exportGeometry = EditorGUILayout.Toggle("Export Geometry", fbxConfig.exportGeometry);
            
            // Animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.miniLabel);
            
            fbxConfig.animationCompression = (FBXAnimationCompressionLevel)
                EditorGUILayout.EnumPopup("Compression", fbxConfig.animationCompression);
                
            fbxConfig.clampedTangents = EditorGUILayout.Toggle("Clamped Tangents", fbxConfig.clampedTangents);
            
            // Transfer animation
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transfer Animation (Optional)", EditorStyles.miniLabel);
            
            fbxConfig.transferAnimationSource = (Transform)EditorGUILayout.ObjectField(
                "Source", fbxConfig.transferAnimationSource, typeof(Transform), true);
                
            fbxConfig.transferAnimationDest = (Transform)EditorGUILayout.ObjectField(
                "Destination", fbxConfig.transferAnimationDest, typeof(Transform), true);
        }
    }
}