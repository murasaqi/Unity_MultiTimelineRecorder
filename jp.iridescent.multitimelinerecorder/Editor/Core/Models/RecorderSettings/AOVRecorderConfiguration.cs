using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// AOV (Arbitrary Output Variables) types
    /// </summary>
    public enum AOVType
    {
        Beauty,
        Depth,
        Normal,
        Motion,
        Albedo,
        Specular,
        Smoothness,
        AmbientOcclusion,
        Emission,
        Alpha,
        DirectDiffuse,
        DirectSpecular,
        IndirectDiffuse,
        Reflection,
        Custom
    }

    /// <summary>
    /// Configuration for AOV (Arbitrary Output Variables) recording
    /// </summary>
    [Serializable]
    public class AOVRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private AOVType aovType = AOVType.Beauty;
        
        [SerializeField]
        private string customAOVName = "";
        
        [SerializeField]
        private UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat outputFormat = 
            UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
        
        [SerializeField]
        private bool captureAlpha = true;
        
        [SerializeField]
        private ImageRecorderSourceType sourceType = ImageRecorderSourceType.TargetCamera;
        
        [SerializeField]
        private string cameraTag = "MainCamera";
        
        [SerializeField]
        private bool flipVertical = false;
        
        [SerializeField]
        private string fileName = "<Scene>_<Timeline>_<AOVType>_<Take>";
        
        [SerializeField]
        private int superSampling = 1;
        
        [SerializeField]
        private bool recordTransparency = true;

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.AOV;

        /// <summary>
        /// Type of AOV to record
        /// </summary>
        public AOVType AOVType
        {
            get => aovType;
            set => aovType = value;
        }

        /// <summary>
        /// Custom AOV name when AOVType is Custom
        /// </summary>
        public string CustomAOVName
        {
            get => customAOVName;
            set => customAOVName = value;
        }

        /// <summary>
        /// Output format for the AOV images
        /// </summary>
        public UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat OutputFormat
        {
            get => outputFormat;
            set => outputFormat = value;
        }

        /// <summary>
        /// Whether to capture alpha channel
        /// </summary>
        public bool CaptureAlpha
        {
            get => captureAlpha;
            set => captureAlpha = value;
        }

        /// <summary>
        /// Source type for recording
        /// </summary>
        public ImageRecorderSourceType SourceType
        {
            get => sourceType;
            set => sourceType = value;
        }

        /// <summary>
        /// Camera tag to use for recording
        /// </summary>
        public string CameraTag
        {
            get => cameraTag;
            set => cameraTag = value;
        }

        /// <summary>
        /// Whether to flip the image vertically
        /// </summary>
        public bool FlipVertical
        {
            get => flipVertical;
            set => flipVertical = value;
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
        /// Super sampling level (1, 2, 4, 8, 16)
        /// </summary>
        public int SuperSampling
        {
            get => superSampling;
            set => superSampling = Mathf.Clamp(value, 1, 16);
        }

        /// <summary>
        /// Whether to record transparency
        /// </summary>
        public bool RecordTransparency
        {
            get => recordTransparency;
            set => recordTransparency = value;
        }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = base.ValidateBase();

            // Validate filename
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddError("Filename pattern cannot be empty");
            }

            // Validate custom AOV name when using custom type
            if (aovType == AOVType.Custom && string.IsNullOrWhiteSpace(customAOVName))
            {
                result.AddError("Custom AOV name is required when AOV type is Custom");
            }

            // Validate super sampling
            if (superSampling != 1 && superSampling != 2 && superSampling != 4 && 
                superSampling != 8 && superSampling != 16)
            {
                result.AddWarning($"Super sampling should be 1, 2, 4, 8, or 16. Current value: {superSampling}");
            }

            // Validate output format for AOV
            if (outputFormat != UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR &&
                outputFormat != UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG)
            {
                result.AddWarning("EXR or PNG format is recommended for AOV recording");
            }

            return result;
        }

        /// <inheritdoc />
        public override UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
        {
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = $"AOV Recorder - {GetAOVName()}";
            settings.Enabled = true;

            // Configure output format
            settings.OutputFormat = outputFormat;
            settings.CaptureAlpha = captureAlpha;

            // Configure image input settings
            var inputSettings = new GameViewInputSettings();
            inputSettings.FlipFinalOutput = flipVertical;
            
            if (sourceType == ImageRecorderSourceType.TargetCamera)
            {
                var cameraInputSettings = new CameraInputSettings();
                cameraInputSettings.Source = ImageSource.MainCamera;
                if (!string.IsNullOrEmpty(cameraTag))
                {
                    cameraInputSettings.Source = ImageSource.TaggedCamera;
                    cameraInputSettings.CameraTag = cameraTag;
                }
                cameraInputSettings.FlipFinalOutput = flipVertical;
                cameraInputSettings.CaptureUI = false;
                settings.imageInputSettings = cameraInputSettings;
            }
            else
            {
                settings.imageInputSettings = inputSettings;
            }

            // Set output file path
            var filename = ProcessWildcards(context);
            settings.OutputFile = filename;

            return settings;
        }

        /// <inheritdoc />
        public override IRecorderConfiguration Clone()
        {
            var clone = new AOVRecorderConfiguration
            {
                aovType = this.aovType,
                customAOVName = this.customAOVName,
                outputFormat = this.outputFormat,
                captureAlpha = this.captureAlpha,
                sourceType = this.sourceType,
                cameraTag = this.cameraTag,
                flipVertical = this.flipVertical,
                fileName = this.fileName,
                superSampling = this.superSampling,
                recordTransparency = this.recordTransparency
            };
            
            CopyBaseTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected override string GetDefaultName()
        {
            return $"AOV - {GetAOVName()}";
        }

        /// <summary>
        /// Gets the display name for the current AOV type
        /// </summary>
        private string GetAOVName()
        {
            return aovType == AOVType.Custom ? customAOVName : aovType.ToString();
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
            pattern = pattern.Replace("<RecorderType>", "AOV");
            pattern = pattern.Replace("<AOVType>", GetAOVName());
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