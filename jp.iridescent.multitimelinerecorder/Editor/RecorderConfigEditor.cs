using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;
using Unity.MultiTimelineRecorder.RecorderEditors;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Editor UI for RecorderConfig
    /// </summary>
    public class RecorderConfigEditor
    {
        private RecorderConfig config;
        private RecorderSettingsEditorBase recorderEditor;
        private IRecorderSettingsHost host;
        
        // UI state
        private static GUIStyle deleteButtonStyle;
        private static GUIStyle moveButtonStyle;
        private static GUIStyle headerStyle;
        
        public RecorderConfigEditor(RecorderConfig config, IRecorderSettingsHost host)
        {
            this.config = config;
            this.host = new RecorderConfigHost(config, host);
            UpdateRecorderEditor();
        }
        
        /// <summary>
        /// Draws the recorder configuration UI
        /// </summary>
        public bool DrawRecorderConfig(int index, int totalCount)
        {
            InitializeStyles();
            
            bool shouldDelete = false;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            // Foldout with custom name
            config.foldout = EditorGUILayout.Foldout(config.foldout, "", true);
            
            // Enable checkbox
            config.enabled = EditorGUILayout.Toggle(config.enabled, GUILayout.Width(20));
            
            // Config name
            GUI.enabled = config.enabled;
            config.configName = EditorGUILayout.TextField(config.configName, headerStyle);
            GUI.enabled = true;
            
            // Move buttons
            GUI.enabled = index > 0;
            if (GUILayout.Button("▲", moveButtonStyle, GUILayout.Width(25)))
            {
                return true; // Signal to move up
            }
            GUI.enabled = index < totalCount - 1;
            if (GUILayout.Button("▼", moveButtonStyle, GUILayout.Width(25)))
            {
                return true; // Signal to move down
            }
            GUI.enabled = true;
            
            // Delete button
            if (GUILayout.Button("✕", deleteButtonStyle, GUILayout.Width(25)))
            {
                shouldDelete = EditorUtility.DisplayDialog(
                    "Delete Recorder",
                    $"Are you sure you want to delete '{config.configName}'?",
                    "Delete",
                    "Cancel");
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Content
            if (config.foldout && config.enabled)
            {
                EditorGUI.indentLevel++;
                DrawRecorderSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            
            return shouldDelete;
        }
        
        private void DrawRecorderSettings()
        {
            // Recorder type selection
            RecorderSettingsType newType = (RecorderSettingsType)EditorGUILayout.EnumPopup("Recorder Type", config.recorderType);
            if (newType != config.recorderType)
            {
                config.recorderType = newType;
                
                // Update file name pattern when type changes
                config.fileName = "<Scene>_<Recorder>_<Take>";
                
                // Ensure <Frame> wildcard for sequence types
                if ((newType == RecorderSettingsType.Image || newType == RecorderSettingsType.AOV) 
                    && !config.fileName.Contains("<Frame>"))
                {
                    if (config.fileName.Contains("."))
                    {
                        int lastDotIndex = config.fileName.LastIndexOf('.');
                        config.fileName = config.fileName.Substring(0, lastDotIndex) + "_<Frame>" + config.fileName.Substring(lastDotIndex);
                    }
                    else
                    {
                        config.fileName += "_<Frame>";
                    }
                }
                
                UpdateRecorderEditor();
            }
            
            // Check if recorder type is supported
            if (!RecorderSettingsFactory.IsRecorderTypeSupported(config.recorderType))
            {
                string reason = GetUnsupportedReason(config.recorderType);
                EditorGUILayout.HelpBox(reason, MessageType.Error);
                return;
            }
            
            // Frame rate
            config.frameRate = EditorGUILayout.IntField("Frame Rate", config.frameRate);
            
            EditorGUILayout.Space(5);
            
            // Use recorder editor for specific settings
            if (recorderEditor != null)
            {
                recorderEditor.DrawRecorderSettings();
            }
        }
        
        private void UpdateRecorderEditor()
        {
            recorderEditor = config.recorderType switch
            {
                RecorderSettingsType.Image => new ImageRecorderEditor(host),
                RecorderSettingsType.Movie => new MovieRecorderEditor(host),
                RecorderSettingsType.AOV => new AOVRecorderEditor(host),
                RecorderSettingsType.Alembic => new AlembicRecorderEditor(host),
                RecorderSettingsType.Animation => new AnimationRecorderEditor(host),
                RecorderSettingsType.FBX => new FBXRecorderEditor(host),
                _ => null
            };
        }
        
        private string GetUnsupportedReason(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.AOV:
                    return "AOV Recorder requires HDRP package to be installed";
                case RecorderSettingsType.Alembic:
                    return "Alembic Recorder requires Unity Alembic package to be installed";
                default:
                    return $"{type} recorder is not available";
            }
        }
        
        private static void InitializeStyles()
        {
            if (deleteButtonStyle == null)
            {
                deleteButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    normal = { textColor = Color.red },
                    hover = { textColor = Color.white },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (moveButtonStyle == null)
            {
                moveButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 10,
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }
            
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }
        }
        
        /// <summary>
        /// Adapter class to bridge RecorderConfig with IRecorderSettingsHost
        /// </summary>
        private class RecorderConfigHost : IRecorderSettingsHost
        {
            private RecorderConfig config;
            private IRecorderSettingsHost parentHost;
            
            public RecorderConfigHost(RecorderConfig config, IRecorderSettingsHost parentHost)
            {
                this.config = config;
                this.parentHost = parentHost;
            }
            
            // IRecorderSettingsHost implementation
            public PlayableDirector selectedDirector => parentHost.selectedDirector;
            
            public int frameRate { get => config.frameRate; set => config.frameRate = value; }
            public int width { get => config.width; set => config.width = value; }
            public int height { get => config.height; set => config.height = value; }
            public bool useGlobalResolution { get => false; set { /* RecorderConfig always uses local resolution */ } }
            public string fileName { get => config.fileName; set => config.fileName = value; }
            public string filePath { get => config.filePath; set => config.filePath = value; }
            public int takeNumber { get => config.takeNumber; set => config.takeNumber = value; }
            public RecorderTakeMode takeMode { get => RecorderTakeMode.ClipTake; set { /* RecorderConfig doesn't support take mode */ } }
            public string cameraTag { get => config.cameraTag; set => config.cameraTag = value; }
            public OutputResolution outputResolution { get => config.outputResolution; set => config.outputResolution = value; }
            
            // Image settings
            public ImageRecorderSettings.ImageRecorderOutputFormat imageOutputFormat { get => config.imageOutputFormat; set => config.imageOutputFormat = value; }
            public bool imageCaptureAlpha { get => config.imageCaptureAlpha; set => config.imageCaptureAlpha = value; }
            public int jpegQuality { get => config.jpegQuality; set => config.jpegQuality = value; }
            public CompressionUtility.EXRCompressionType exrCompression { get => config.exrCompression; set => config.exrCompression = value; }
            
            // Movie settings
            public MovieRecorderSettings.VideoRecorderOutputFormat movieOutputFormat { get => config.movieOutputFormat; set => config.movieOutputFormat = value; }
            public VideoBitrateMode movieQuality { get => config.movieQuality; set => config.movieQuality = value; }
            public bool movieCaptureAudio { get => config.movieCaptureAudio; set => config.movieCaptureAudio = value; }
            public bool movieCaptureAlpha { get => config.movieCaptureAlpha; set => config.movieCaptureAlpha = value; }
            public int movieBitrate { get => config.movieBitrate; set => config.movieBitrate = value; }
            public AudioBitRateMode audioBitrate { get => config.audioBitrate; set => config.audioBitrate = value; }
            public MovieRecorderPreset moviePreset { get => config.moviePreset; set => config.moviePreset = value; }
            public bool useMoviePreset { get => config.useMoviePreset; set => config.useMoviePreset = value; }
            
            // AOV settings
            public AOVType selectedAOVTypes { get => config.selectedAOVTypes; set => config.selectedAOVTypes = value; }
            public AOVOutputFormat aovOutputFormat { get => config.aovOutputFormat; set => config.aovOutputFormat = value; }
            public AOVPreset aovPreset { get => config.aovPreset; set => config.aovPreset = value; }
            public bool useAOVPreset { get => config.useAOVPreset; set => config.useAOVPreset = value; }
            public bool useMultiPartEXR { get => config.useMultiPartEXR; set => config.useMultiPartEXR = value; }
            public AOVColorSpace aovColorSpace { get => config.aovColorSpace; set => config.aovColorSpace = value; }
            public AOVCompression aovCompression { get => config.aovCompression; set => config.aovCompression = value; }
            
            // Alembic settings
            public AlembicExportTargets alembicExportTargets { get => config.alembicExportTargets; set => config.alembicExportTargets = value; }
            public AlembicExportScope alembicExportScope { get => config.alembicExportScope; set => config.alembicExportScope = value; }
            public GameObject alembicTargetGameObject { get => config.alembicTargetGameObject; set => config.alembicTargetGameObject = value; }
            public AlembicHandedness alembicHandedness { get => config.alembicHandedness; set => config.alembicHandedness = value; }
            public float alembicWorldScale { get => config.alembicWorldScale; set => config.alembicWorldScale = value; }
            public float alembicFrameRate { get => config.alembicFrameRate; set => config.alembicFrameRate = value; }
            public AlembicTimeSamplingType alembicTimeSamplingType { get => config.alembicTimeSamplingType; set => config.alembicTimeSamplingType = value; }
            public bool alembicIncludeChildren { get => config.alembicIncludeChildren; set => config.alembicIncludeChildren = value; }
            public bool alembicFlattenHierarchy { get => config.alembicFlattenHierarchy; set => config.alembicFlattenHierarchy = value; }
            public AlembicExportPreset alembicPreset { get => config.alembicPreset; set => config.alembicPreset = value; }
            public bool useAlembicPreset { get => config.useAlembicPreset; set => config.useAlembicPreset = value; }
            
            // Animation settings
            public GameObject animationTargetGameObject { get => config.animationTargetGameObject; set => config.animationTargetGameObject = value; }
            public AnimationRecordingScope animationRecordingScope { get => config.animationRecordingScope; set => config.animationRecordingScope = value; }
            public bool animationIncludeChildren { get => config.animationIncludeChildren; set => config.animationIncludeChildren = value; }
            public bool animationClampedTangents { get => config.animationClampedTangents; set => config.animationClampedTangents = value; }
            public bool animationRecordBlendShapes { get => config.animationRecordBlendShapes; set => config.animationRecordBlendShapes = value; }
            public float animationPositionError { get => config.animationPositionError; set => config.animationPositionError = value; }
            public float animationRotationError { get => config.animationRotationError; set => config.animationRotationError = value; }
            public float animationScaleError { get => config.animationScaleError; set => config.animationScaleError = value; }
            public AnimationExportPreset animationPreset { get => config.animationPreset; set => config.animationPreset = value; }
            public bool useAnimationPreset { get => config.useAnimationPreset; set => config.useAnimationPreset = value; }
            
            // FBX settings
            public GameObject fbxTargetGameObject { get => config.fbxTargetGameObject; set => config.fbxTargetGameObject = value; }
            public FBXRecordedComponent fbxRecordedComponent { get => config.fbxRecordedComponent; set => config.fbxRecordedComponent = value; }
            public bool fbxRecordHierarchy { get => config.fbxRecordHierarchy; set => config.fbxRecordHierarchy = value; }
            public bool fbxClampedTangents { get => config.fbxClampedTangents; set => config.fbxClampedTangents = value; }
            public FBXAnimationCompressionLevel fbxAnimationCompression { get => config.fbxAnimationCompression; set => config.fbxAnimationCompression = value; }
            public bool fbxExportGeometry { get => config.fbxExportGeometry; set => config.fbxExportGeometry = value; }
            public Transform fbxTransferAnimationSource { get => config.fbxTransferAnimationSource; set => config.fbxTransferAnimationSource = value; }
            public Transform fbxTransferAnimationDest { get => config.fbxTransferAnimationDest; set => config.fbxTransferAnimationDest = value; }
            public FBXExportPreset fbxPreset { get => config.fbxPreset; set => config.fbxPreset = value; }
            public bool useFBXPreset { get => config.useFBXPreset; set => config.useFBXPreset = value; }
        }
    }
}