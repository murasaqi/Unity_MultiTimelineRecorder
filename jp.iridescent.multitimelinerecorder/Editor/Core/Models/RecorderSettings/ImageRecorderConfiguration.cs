using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// Configuration for image sequence recording
    /// </summary>
    [Serializable]
    public class ImageRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private ImageRecorderSettings.ImageRecorderOutputFormat outputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        
        [SerializeField]
        private bool captureAlpha = false;
        
        [SerializeField]
        private ImageRecorderSourceType sourceType = ImageRecorderSourceType.GameView;
        
        [SerializeField]
        private Camera targetCamera;
        
        [SerializeField]
        private RenderTexture renderTexture;
        
        [SerializeField]
        private int jpegQuality = 75;
        
        [SerializeField]
        private ImageRecorderSettings.EXRCompressionType exrCompression = ImageRecorderSettings.EXRCompressionType.Zip;

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.Image;

        /// <summary>
        /// Output format for images
        /// </summary>
        public ImageRecorderSettings.ImageRecorderOutputFormat OutputFormat
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
        /// Target camera for recording (when using TargetCamera source)
        /// </summary>
        public Camera TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        /// <summary>
        /// Render texture for recording (when using RenderTexture source)
        /// </summary>
        public RenderTexture RenderTexture
        {
            get => renderTexture;
            set => renderTexture = value;
        }

        /// <summary>
        /// JPEG quality (1-100)
        /// </summary>
        public int JpegQuality
        {
            get => jpegQuality;
            set => jpegQuality = Mathf.Clamp(value, 1, 100);
        }

        /// <summary>
        /// EXR compression type
        /// </summary>
        public ImageRecorderSettings.EXRCompressionType ExrCompression
        {
            get => exrCompression;
            set => exrCompression = value;
        }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = base.ValidateBase();

            // Validate source-specific requirements
            switch (sourceType)
            {
                case ImageRecorderSourceType.TargetCamera:
                    if (targetCamera == null)
                    {
                        result.AddError("Target camera is required when using TargetCamera source type");
                    }
                    break;
                    
                case ImageRecorderSourceType.RenderTexture:
                    if (renderTexture == null)
                    {
                        result.AddError("Render texture is required when using RenderTexture source type");
                    }
                    break;
            }

            // Validate format-specific settings
            if (outputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                if (jpegQuality < 1 || jpegQuality > 100)
                {
                    result.AddError($"JPEG quality must be between 1 and 100. Current value: {jpegQuality}");
                }
                
                if (captureAlpha)
                {
                    result.AddWarning("JPEG format does not support alpha channel. Alpha capture will be ignored.");
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override RecorderSettings CreateUnityRecorderSettings(WildcardContext context)
        {
            var settings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.name = "Image Recorder";
            settings.enabled = true;

            // Set output format
            settings.imageSettings = new ImageRecorderSettings.ImageRecorderOutput
            {
                imageFormat = outputFormat,
                jpegQuality = jpegQuality,
                exrCompression = exrCompression,
                captureAlpha = captureAlpha
            };

            // Configure input source
            switch (sourceType)
            {
                case ImageRecorderSourceType.GameView:
                    settings.imageInputSettings = new GameViewInputSettings
                    {
                        OutputWidth = context.CustomWildcards.ContainsKey("Width") 
                            ? int.Parse(context.CustomWildcards["Width"]) 
                            : 1920,
                        OutputHeight = context.CustomWildcards.ContainsKey("Height") 
                            ? int.Parse(context.CustomWildcards["Height"]) 
                            : 1080
                    };
                    break;
                    
                case ImageRecorderSourceType.TargetCamera:
                    var cameraInputSettings = new CameraInputSettings
                    {
                        Source = targetCamera,
                        OutputWidth = context.CustomWildcards.ContainsKey("Width") 
                            ? int.Parse(context.CustomWildcards["Width"]) 
                            : 1920,
                        OutputHeight = context.CustomWildcards.ContainsKey("Height") 
                            ? int.Parse(context.CustomWildcards["Height"]) 
                            : 1080,
                        CaptureUI = false
                    };
                    settings.imageInputSettings = cameraInputSettings;
                    break;
                    
                case ImageRecorderSourceType.RenderTexture:
                    settings.imageInputSettings = new RenderTextureInputSettings
                    {
                        RenderTexture = renderTexture
                    };
                    break;
            }

            // Set output file path
            var filename = ProcessWildcards(context);
            settings.OutputFile = filename;

            return settings;
        }

        /// <inheritdoc />
        public override IRecorderConfiguration Clone()
        {
            var clone = new ImageRecorderConfiguration
            {
                outputFormat = this.outputFormat,
                captureAlpha = this.captureAlpha,
                sourceType = this.sourceType,
                targetCamera = this.targetCamera,
                renderTexture = this.renderTexture,
                jpegQuality = this.jpegQuality,
                exrCompression = this.exrCompression
            };
            
            CopyBaseTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected override string GetDefaultName()
        {
            return $"Image Sequence ({outputFormat})";
        }

        /// <summary>
        /// Processes wildcards for filename generation
        /// </summary>
        private string ProcessWildcards(WildcardContext context)
        {
            var pattern = "<Scene>_<Timeline>_Image_Take<Take:0000>";
            
            // Replace standard wildcards
            pattern = pattern.Replace("<Scene>", context.SceneName);
            pattern = pattern.Replace("<Timeline>", context.TimelineName);
            pattern = pattern.Replace("<Take>", context.TakeNumber.ToString());
            pattern = pattern.Replace("<Take:0000>", context.TakeNumber.ToString("0000"));
            pattern = pattern.Replace("<RecorderType>", "Image");
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