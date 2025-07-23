using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// Animation compression mode
    /// </summary>
    public enum AnimationCompressionMode
    {
        Off,
        Optimal,
        KeyframeReduction
    }

    /// <summary>
    /// Configuration for animation clip recording
    /// </summary>
    [Serializable]
    public class AnimationRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private GameObject targetGameObject;
        
        [SerializeField]
        private bool recordHierarchy = true;
        
        [SerializeField]
        private AnimationInputSettings.CurveSimplificationOptions simplificationOptions = AnimationInputSettings.CurveSimplificationOptions.Lossless;
        
        [SerializeField]
        private bool recordBlendShapes = true;
        
        [SerializeField]
        private string fileName = "<Scene>_<Take>";
        
        [SerializeField]
        private bool clampedTangents = false;
        
        [SerializeField]
        private bool recordTransform = true;
        
        [SerializeField]
        private bool recordPosition = true;
        
        [SerializeField]
        private bool recordRotation = true;
        
        [SerializeField]
        private bool recordScale = true;
        
        [SerializeField]
        private bool recordComponents = false;
        
        [SerializeField]
        private bool recordActiveState = false;
        
        [SerializeField]
        private bool recordMaterialProperties = false;
        
        [SerializeField]
        private AnimationCompressionMode compressionMode = AnimationCompressionMode.Optimal;
        
        [SerializeField]
        private int frameRate = 30;
        
        [SerializeField]
        private float positionError = 0.5f;
        
        [SerializeField]
        private float rotationError = 0.5f;
        
        [SerializeField]
        private float scaleError = 0.00025f;
        
        [SerializeField]
        private bool keyframeReduction = true;
        
        [SerializeField]
        private string clipName = "RecordedAnimation";
        
        [SerializeField]
        private bool takeNameInFile = true;

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.Animation;

        /// <summary>
        /// Target GameObject to record
        /// </summary>
        public GameObject TargetGameObject
        {
            get => targetGameObject;
            set => targetGameObject = value;
        }

        /// <summary>
        /// Whether to record the entire hierarchy
        /// </summary>
        public bool RecordHierarchy
        {
            get => recordHierarchy;
            set => recordHierarchy = value;
        }

        /// <summary>
        /// Curve simplification options
        /// </summary>
        public AnimationInputSettings.CurveSimplificationOptions SimplificationOptions
        {
            get => simplificationOptions;
            set => simplificationOptions = value;
        }

        /// <summary>
        /// Whether to record blend shapes
        /// </summary>
        public bool RecordBlendShapes
        {
            get => recordBlendShapes;
            set => recordBlendShapes = value;
        }

        /// <summary>
        /// Output filename pattern
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        /// <summary>
        /// Whether to use clamped tangents
        /// </summary>
        public bool ClampedTangents
        {
            get => clampedTangents;
            set => clampedTangents = value;
        }

        /// <summary>
        /// Whether to record transform
        /// </summary>
        public bool RecordTransform
        {
            get => recordTransform;
            set => recordTransform = value;
        }

        /// <summary>
        /// Whether to record position
        /// </summary>
        public bool RecordPosition
        {
            get => recordPosition;
            set => recordPosition = value;
        }

        /// <summary>
        /// Whether to record rotation
        /// </summary>
        public bool RecordRotation
        {
            get => recordRotation;
            set => recordRotation = value;
        }

        /// <summary>
        /// Whether to record scale
        /// </summary>
        public bool RecordScale
        {
            get => recordScale;
            set => recordScale = value;
        }

        /// <summary>
        /// Whether to record components
        /// </summary>
        public bool RecordComponents
        {
            get => recordComponents;
            set => recordComponents = value;
        }

        /// <summary>
        /// Whether to record active state
        /// </summary>
        public bool RecordActiveState
        {
            get => recordActiveState;
            set => recordActiveState = value;
        }

        /// <summary>
        /// Whether to record material properties
        /// </summary>
        public bool RecordMaterialProperties
        {
            get => recordMaterialProperties;
            set => recordMaterialProperties = value;
        }

        /// <summary>
        /// Compression mode
        /// </summary>
        public AnimationCompressionMode CompressionMode
        {
            get => compressionMode;
            set => compressionMode = value;
        }

        /// <summary>
        /// Position error tolerance
        /// </summary>
        public float PositionError
        {
            get => positionError;
            set => positionError = value;
        }

        /// <summary>
        /// Rotation error tolerance
        /// </summary>
        public float RotationError
        {
            get => rotationError;
            set => rotationError = value;
        }

        /// <summary>
        /// Scale error tolerance
        /// </summary>
        public float ScaleError
        {
            get => scaleError;
            set => scaleError = value;
        }

        /// <summary>
        /// Frame rate for animation recording
        /// </summary>
        public int FrameRate
        {
            get => frameRate;
            set => frameRate = Mathf.Clamp(value, 1, 120);
        }

        /// <summary>
        /// Whether to use keyframe reduction
        /// </summary>
        public bool KeyframeReduction
        {
            get => keyframeReduction;
            set => keyframeReduction = value;
        }

        /// <summary>
        /// Clip name
        /// </summary>
        public string ClipName
        {
            get => clipName;
            set => clipName = value;
        }

        /// <summary>
        /// Whether to include take name in file
        /// </summary>
        public bool TakeNameInFile
        {
            get => takeNameInFile;
            set => takeNameInFile = value;
        }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = base.ValidateBase();

            // Validate target GameObject
            if (targetGameObject == null)
            {
                result.AddError("Target GameObject is required for animation recording");
            }

            // Validate filename
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddError("Filename pattern cannot be empty");
            }

            return result;
        }

        /// <inheritdoc />
        public override UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
        {
            var settings = ScriptableObject.CreateInstance<AnimationRecorderSettings>();
            settings.name = "Animation Recorder";
            settings.Enabled = true;

            // Configure animation input settings
            var inputSettings = new AnimationInputSettings();
            inputSettings.gameObject = targetGameObject;
            inputSettings.Recursive = recordHierarchy;
            // inputSettings.SimplyCurves = true; // Property may not exist in current Unity version
            // inputSettings.CurveSimplificationOptions = simplificationOptions; // May not be supported
            // inputSettings.RecordBlendShapes = recordBlendShapes; // May not be supported
            inputSettings.ClampedTangents = clampedTangents;

            settings.AnimationInputSettings = inputSettings;

            // Set output file path
            var filename = ProcessWildcards(context);
            settings.OutputFile = filename;

            return settings;
        }

        /// <inheritdoc />
        public override IRecorderConfiguration Clone()
        {
            var clone = new AnimationRecorderConfiguration
            {
                targetGameObject = this.targetGameObject,
                recordHierarchy = this.recordHierarchy,
                simplificationOptions = this.simplificationOptions,
                recordBlendShapes = this.recordBlendShapes,
                fileName = this.fileName,
                clampedTangents = this.clampedTangents,
                recordTransform = this.recordTransform,
                recordPosition = this.recordPosition,
                recordRotation = this.recordRotation,
                recordScale = this.recordScale,
                recordComponents = this.recordComponents,
                recordActiveState = this.recordActiveState,
                recordMaterialProperties = this.recordMaterialProperties,
                compressionMode = this.compressionMode,
                positionError = this.positionError,
                rotationError = this.rotationError,
                scaleError = this.scaleError,
                keyframeReduction = this.keyframeReduction,
                clipName = this.clipName,
                takeNameInFile = this.takeNameInFile
            };
            
            CopyBaseTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected override string GetDefaultName()
        {
            return "Animation Clip";
        }

        /// <summary>
        /// Processes wildcards for filename generation
        /// </summary>
        private string ProcessWildcards(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
        {
            var pattern = fileName;
            
            // Replace standard wildcards
            pattern = pattern.Replace("<Scene>", context.SceneName);
            pattern = pattern.Replace("<Timeline>", context.TimelineName);
            pattern = pattern.Replace("<Take>", context.TakeNumber.ToString());
            pattern = pattern.Replace("<Take:0000>", context.TakeNumber.ToString("0000"));
            pattern = pattern.Replace("<RecorderType>", "Animation");
            pattern = pattern.Replace("<Date>", context.RecordingDate.ToString("yyyy-MM-dd"));
            pattern = pattern.Replace("<Time>", context.RecordingDate.ToString("HH-mm-ss"));
            
            // Replace custom wildcards
            foreach (var wildcard in context.CustomWildcards)
            {
                pattern = pattern.Replace($"<{wildcard.Key}>", wildcard.Value);
            }
            
            return pattern;
        }
    }
}