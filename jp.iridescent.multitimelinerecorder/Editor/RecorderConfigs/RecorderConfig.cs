using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Represents a single recorder configuration with all its settings
    /// </summary>
    [Serializable]
    public class RecorderConfig
    {
        // Basic settings
        public bool enabled = true;
        public string configName = "New Recorder";
        public RecorderSettingsType recorderType = RecorderSettingsType.Image;
        
        // Common settings
        public int frameRate = 24;
        public int width = 1920;
        public int height = 1080;
        public string fileName = "<Scene>_<Recorder>_<Take>";
        public string filePath = "Recordings";
        public int takeNumber = 1;
        public string cameraTag = "MainCamera";
        public OutputResolution outputResolution = OutputResolution.HD1080p;
        
        // Image recorder settings
        public ImageRecorderSettings.ImageRecorderOutputFormat imageOutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        public bool imageCaptureAlpha = false;
        public int jpegQuality = 75;
        public CompressionUtility.EXRCompressionType exrCompression = CompressionUtility.EXRCompressionType.None;
        
        // Movie recorder settings
        public MovieRecorderSettings.VideoRecorderOutputFormat movieOutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        public VideoBitrateMode movieQuality = VideoBitrateMode.High;
        public bool movieCaptureAudio = false;
        public bool movieCaptureAlpha = false;
        public int movieBitrate = 15;
        public AudioBitRateMode audioBitrate = AudioBitRateMode.High;
        public MovieRecorderPreset moviePreset = MovieRecorderPreset.HighQuality1080p;
        public bool useMoviePreset = false;
        
        // Movie encoder settings
        public bool useProResEncoder = false;
        public ProResEncoderSettings.OutputFormat proResFormat = ProResEncoderSettings.OutputFormat.ProRes422HQ;
        public CoreEncoderSettings.OutputCodec coreCodec = CoreEncoderSettings.OutputCodec.MP4;
        public CoreEncoderSettings.VideoEncodingQuality coreEncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High;
        
        // AOV recorder settings
        public AOVType selectedAOVTypes = AOVType.Depth | AOVType.Normal | AOVType.Albedo;
        public AOVOutputFormat aovOutputFormat = AOVOutputFormat.EXR16;
        public AOVPreset aovPreset = AOVPreset.Compositing;
        public bool useAOVPreset = false;
        public bool useMultiPartEXR = true;
        public AOVColorSpace aovColorSpace = AOVColorSpace.Linear;
        public AOVCompression aovCompression = AOVCompression.Zip;
        
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
        public GameObject animationTargetGameObject = null;
        public AnimationRecordingScope animationRecordingScope = AnimationRecordingScope.SingleGameObject;
        public AnimationInterpolationMode animationInterpolationMode = AnimationInterpolationMode.Linear;
        public AnimationCompressionLevel animationCompressionLevel = AnimationCompressionLevel.Medium;
        public bool animationIncludeChildren = true;
        public bool animationClampedTangents = true;
        public bool animationRecordBlendShapes = false;
        public float animationPositionError = 0.5f;
        public float animationRotationError = 0.5f;
        public float animationScaleError = 0.5f;
        public AnimationExportPreset animationPreset = AnimationExportPreset.SimpleTransform;
        public bool useAnimationPreset = false;
        
        // FBX recorder settings
        public GameObject fbxTargetGameObject = null;
        public FBXRecordedComponent fbxRecordedComponent = FBXRecordedComponent.Camera;
        public bool fbxRecordHierarchy = true;
        public bool fbxClampedTangents = true;
        public FBXAnimationCompressionLevel fbxAnimationCompression = FBXAnimationCompressionLevel.Lossy;
        public bool fbxExportGeometry = true;
        public Transform fbxTransferAnimationSource = null;
        public Transform fbxTransferAnimationDest = null;
        public FBXExportPreset fbxPreset = FBXExportPreset.AnimationExport;
        public bool useFBXPreset = false;
        
        // UI state
        public bool foldout = true;
        
        /// <summary>
        /// Creates a default RecorderConfig with the specified type
        /// </summary>
        public static RecorderConfig CreateDefault(RecorderSettingsType type)
        {
            var config = new RecorderConfig
            {
                recorderType = type,
                configName = GetDefaultName(type),
                fileName = "<Scene>_<Recorder>_<Take>"
            };
            
            // Set type-specific defaults
            switch (type)
            {
                case RecorderSettingsType.Image:
                case RecorderSettingsType.AOV:
                    // Ensure <Frame> wildcard is present
                    if (!config.fileName.Contains("<Frame>"))
                    {
                        config.fileName = config.fileName.Replace(".", "_<Frame>.");
                        if (!config.fileName.Contains("<Frame>"))
                        {
                            config.fileName += "_<Frame>";
                        }
                    }
                    break;
            }
            
            return config;
        }
        
        /// <summary>
        /// Gets the default name for a recorder type
        /// </summary>
        private static string GetDefaultName(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Image:
                    return "Image Sequence";
                case RecorderSettingsType.Movie:
                    return "Movie";
                case RecorderSettingsType.AOV:
                    return "AOV Passes";
                case RecorderSettingsType.Alembic:
                    return "Alembic Export";
                case RecorderSettingsType.Animation:
                    return "Animation Clip";
                case RecorderSettingsType.FBX:
                    return "FBX Export";
                default:
                    return "Recorder";
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this RecorderConfig
        /// </summary>
        public RecorderConfig Clone()
        {
            // Use JSON serialization for deep copy
            var json = JsonUtility.ToJson(this);
            var clone = new RecorderConfig();
            JsonUtility.FromJsonOverwrite(json, clone);
            
            // Manually copy object references that don't serialize well
            clone.alembicTargetGameObject = alembicTargetGameObject;
            clone.animationTargetGameObject = animationTargetGameObject;
            clone.fbxTargetGameObject = fbxTargetGameObject;
            clone.fbxTransferAnimationSource = fbxTransferAnimationSource;
            clone.fbxTransferAnimationDest = fbxTransferAnimationDest;
            
            return clone;
        }
        
        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = null;
            
            if (!enabled)
                return true; // Skip validation for disabled configs
            
            // Common validation
            if (string.IsNullOrEmpty(fileName))
            {
                errorMessage = $"{configName}: File name is empty";
                return false;
            }
            
            if (frameRate <= 0)
            {
                errorMessage = $"{configName}: Invalid frame rate";
                return false;
            }
            
            if (width <= 0 || height <= 0)
            {
                errorMessage = $"{configName}: Invalid resolution";
                return false;
            }
            
            // Type-specific validation
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                case RecorderSettingsType.AOV:
                    if (!fileName.Contains("<Frame>"))
                    {
                        errorMessage = $"{configName}: Image sequence requires <Frame> wildcard";
                        return false;
                    }
                    break;
                    
                case RecorderSettingsType.Alembic:
                    if (alembicExportScope == AlembicExportScope.TargetGameObject && alembicTargetGameObject == null)
                    {
                        errorMessage = $"{configName}: Alembic target GameObject not set";
                        return false;
                    }
                    break;
                    
                case RecorderSettingsType.Animation:
                    if ((animationRecordingScope == AnimationRecordingScope.SingleGameObject ||
                         animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren) &&
                        animationTargetGameObject == null)
                    {
                        errorMessage = $"{configName}: Animation target GameObject not set";
                        return false;
                    }
                    break;
                    
                case RecorderSettingsType.FBX:
                    if (fbxTargetGameObject == null)
                    {
                        errorMessage = $"{configName}: FBX target GameObject not set";
                        return false;
                    }
                    break;
            }
            
            return true;
        }
    }
}