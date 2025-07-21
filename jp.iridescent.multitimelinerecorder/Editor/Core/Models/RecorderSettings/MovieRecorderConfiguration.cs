using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// Configuration for movie recording
    /// </summary>
    [Serializable]
    public class MovieRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private MovieRecorderSettings.VideoRecorderFormat outputFormat = MovieRecorderSettings.VideoRecorderFormat.MP4;
        
        [SerializeField]
        private MovieRecorderSettings.VideoRecorderOutputFormat encodingProfile = MovieRecorderSettings.VideoRecorderOutputFormat.High;
        
        [SerializeField]
        private bool captureAlpha = false;
        
        [SerializeField]
        private float quality = 0.75f;
        
        [SerializeField]
        private ImageRecorderSourceType sourceType = ImageRecorderSourceType.GameView;
        
        [SerializeField]
        private Camera targetCamera;
        
        [SerializeField]
        private RenderTexture renderTexture;
        
        [SerializeField]
        private bool captureAudio = true;

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.Movie;

        /// <summary>
        /// Output format for movie
        /// </summary>
        public MovieRecorderSettings.VideoRecorderFormat OutputFormat
        {
            get => outputFormat;
            set => outputFormat = value;
        }

        /// <summary>
        /// Encoding profile
        /// </summary>
        public MovieRecorderSettings.VideoRecorderOutputFormat EncodingProfile
        {
            get => encodingProfile;
            set => encodingProfile = value;
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
        /// Video quality (0.0 to 1.0)
        /// </summary>
        public float Quality
        {
            get => quality;
            set => quality = Mathf.Clamp01(value);
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
        /// Whether to capture audio
        /// </summary>
        public bool CaptureAudio
        {
            get => captureAudio;
            set => captureAudio = value;
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
            if (outputFormat == MovieRecorderSettings.VideoRecorderFormat.MP4 && captureAlpha)
            {
                result.AddWarning("MP4 format does not support alpha channel. Consider using WebM format.");
            }

            // Validate quality
            if (quality < 0.0f || quality > 1.0f)
            {
                result.AddError($"Quality must be between 0.0 and 1.0. Current value: {quality}");
            }

            return result;
        }

        /// <inheritdoc />
        public override RecorderSettings CreateUnityRecorderSettings(WildcardContext context)
        {
            var settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            settings.name = "Movie Recorder";
            settings.enabled = true;

            // Set output format
            settings.OutputFormat = outputFormat;
            settings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
            
            // Configure video settings
            var videoSettings = new MovieRecorderSettings.VideoRecorderSettings
            {
                Format = outputFormat,
                OutputFormat = encodingProfile,
                CaptureAlpha = captureAlpha
            };
            
            // Apply quality setting based on format
            if (outputFormat == MovieRecorderSettings.VideoRecorderFormat.MP4)
            {
                // MP4 uses bitrate
                var bitrate = Mathf.Lerp(1, 50, quality); // 1-50 Mbps range
                settings.VideoBitRateMode = UnityEditor.VideoBitrateMode.Custom;
            }
            else
            {
                // WebM uses quality directly
                settings.VideoQuality = quality;
            }

            // Configure audio settings
            settings.CaptureAudio = captureAudio;

            // Configure input source
            switch (sourceType)
            {
                case ImageRecorderSourceType.GameView:
                    settings.ImageInputSettings = new GameViewInputSettings
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
                    settings.ImageInputSettings = cameraInputSettings;
                    break;
                    
                case ImageRecorderSourceType.RenderTexture:
                    settings.ImageInputSettings = new RenderTextureInputSettings
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
            var clone = new MovieRecorderConfiguration
            {
                outputFormat = this.outputFormat,
                encodingProfile = this.encodingProfile,
                captureAlpha = this.captureAlpha,
                quality = this.quality,
                sourceType = this.sourceType,
                targetCamera = this.targetCamera,
                renderTexture = this.renderTexture,
                captureAudio = this.captureAudio
            };
            
            CopyBaseTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected override string GetDefaultName()
        {
            return $"Movie ({outputFormat})";
        }

        /// <summary>
        /// Processes wildcards for filename generation
        /// </summary>
        private string ProcessWildcards(WildcardContext context)
        {
            var pattern = "<Scene>_<Timeline>_Movie_Take<Take:0000>";
            
            // Replace standard wildcards
            pattern = pattern.Replace("<Scene>", context.SceneName);
            pattern = pattern.Replace("<Timeline>", context.TimelineName);
            pattern = pattern.Replace("<Take>", context.TakeNumber.ToString());
            pattern = pattern.Replace("<Take:0000>", context.TakeNumber.ToString("0000"));
            pattern = pattern.Replace("<RecorderType>", "Movie");
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