using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// Video format options
    /// </summary>
    public enum VideoFormat
    {
        MP4,
        MOV,
        WebM
    }
    
    /// <summary>
    /// Video codec options
    /// </summary>
    public enum VideoCodec
    {
        H264,
        H265,
        VP8,
        VP9,
        ProRes
    }
    
    /// <summary>
    /// Audio codec options
    /// </summary>
    public enum AudioCodec
    {
        AAC,
        PCM,
        Vorbis
    }
    
    /// <summary>
    /// Bitrate mode options
    /// </summary>
    public enum BitrateMode
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// Aspect ratio mode options
    /// </summary>
    public enum AspectRatioMode
    {
        Custom,
        AspectRatio_16_9,
        AspectRatio_4_3,
        AspectRatio_21_9
    }

    /// <summary>
    /// Configuration for movie recording
    /// </summary>
    [Serializable]
    public class MovieRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private MovieRecorderSettings.VideoRecorderOutputFormat outputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        
        // Note: encodingProfile removed as it's not a separate property in Unity Recorder
        
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
        
        [SerializeField]
        private VideoFormat format = VideoFormat.MP4;
        
        [SerializeField]
        private VideoCodec codec = VideoCodec.H264;
        
        [SerializeField]
        private BitrateMode bitrateMode = BitrateMode.Medium;
        
        [SerializeField]
        private int width = 1920;
        
        [SerializeField]
        private int height = 1080;
        
        [SerializeField]
        private AspectRatioMode aspectRatio = AspectRatioMode.AspectRatio_16_9;
        
        [SerializeField]
        private int frameRate = 30;
        
        [SerializeField]
        private string fileName = "<Scene>_<Take>";
        
        [SerializeField]
        private bool includeUI = false;
        
        [SerializeField]
        private AudioCodec audioCodec = AudioCodec.AAC;
        
        [SerializeField]
        private int audioBitrate = 192;
        
        [SerializeField]
        private bool useMotionBlur = false;
        
        [SerializeField]
        private float shutterAngle = 180;

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.Movie;

        /// <summary>
        /// Output format for movie
        /// </summary>
        public MovieRecorderSettings.VideoRecorderOutputFormat OutputFormat
        {
            get => outputFormat;
            set => outputFormat = value;
        }

        // EncodingProfile property removed - not used in Unity Recorder API

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

        /// <summary>
        /// Video format
        /// </summary>
        public VideoFormat Format
        {
            get => format;
            set => format = value;
        }

        /// <summary>
        /// Video codec
        /// </summary>
        public VideoCodec Codec
        {
            get => codec;
            set => codec = value;
        }

        /// <summary>
        /// Bitrate mode
        /// </summary>
        public BitrateMode BitrateMode
        {
            get => bitrateMode;
            set => bitrateMode = value;
        }

        /// <summary>
        /// Width in pixels
        /// </summary>
        public int Width
        {
            get => width;
            set => width = Math.Max(1, value);
        }

        /// <summary>
        /// Height in pixels
        /// </summary>
        public int Height
        {
            get => height;
            set => height = Math.Max(1, value);
        }

        /// <summary>
        /// Aspect ratio mode
        /// </summary>
        public AspectRatioMode AspectRatio
        {
            get => aspectRatio;
            set => aspectRatio = value;
        }

        /// <summary>
        /// Frame rate
        /// </summary>
        public int FrameRate
        {
            get => frameRate;
            set => frameRate = Mathf.Clamp(value, 1, 120);
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
        /// Whether to include UI
        /// </summary>
        public bool IncludeUI
        {
            get => includeUI;
            set => includeUI = value;
        }

        /// <summary>
        /// Audio codec
        /// </summary>
        public AudioCodec AudioCodec
        {
            get => audioCodec;
            set => audioCodec = value;
        }

        /// <summary>
        /// Audio bitrate in kbps
        /// </summary>
        public int AudioBitrate
        {
            get => audioBitrate;
            set => audioBitrate = Math.Max(32, value);
        }

        /// <summary>
        /// Whether to use motion blur
        /// </summary>
        public bool UseMotionBlur
        {
            get => useMotionBlur;
            set => useMotionBlur = value;
        }

        /// <summary>
        /// Shutter angle in degrees
        /// </summary>
        public float ShutterAngle
        {
            get => shutterAngle;
            set => shutterAngle = Mathf.Clamp(value, 0f, 360f);
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
            if (outputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MP4 && captureAlpha)
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
        public override UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
        {
            var settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            settings.name = "Movie Recorder";
            settings.Enabled = true;

            // Set output format
            settings.OutputFormat = outputFormat;
            settings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
            
            // Configure video settings
            // Note: MovieRecorderSettings properties are set directly on the settings object
            
            // Apply quality setting based on format
            if (outputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MP4)
            {
                // MP4 uses bitrate
                var bitrate = Mathf.Lerp(1, 50, quality); // 1-50 Mbps range
                settings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
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
                    var cameraInputSettings = new CameraInputSettings();
                    cameraInputSettings.OutputWidth = context.CustomWildcards.ContainsKey("Width") 
                        ? int.Parse(context.CustomWildcards["Width"]) 
                        : 1920;
                    cameraInputSettings.OutputHeight = context.CustomWildcards.ContainsKey("Height") 
                        ? int.Parse(context.CustomWildcards["Height"]) 
                        : 1080;
                    cameraInputSettings.CaptureUI = false;
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
                captureAlpha = this.captureAlpha,
                quality = this.quality,
                sourceType = this.sourceType,
                targetCamera = this.targetCamera,
                renderTexture = this.renderTexture,
                captureAudio = this.captureAudio,
                format = this.format,
                codec = this.codec,
                bitrateMode = this.bitrateMode,
                width = this.width,
                height = this.height,
                aspectRatio = this.aspectRatio,
                frameRate = this.frameRate,
                fileName = this.fileName,
                includeUI = this.includeUI,
                audioCodec = this.audioCodec,
                audioBitrate = this.audioBitrate,
                useMotionBlur = this.useMotionBlur,
                shutterAngle = this.shutterAngle
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
        private string ProcessWildcards(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
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