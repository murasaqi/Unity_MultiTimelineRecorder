using System;
using System.IO;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Validator for Image Recorder configurations
    /// </summary>
    public class ImageRecorderValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;

        public ImageRecorderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(IRecorderConfiguration configuration)
        {
            var result = new ValidationResult();
            
            if (configuration is not ImageRecorderConfiguration imageConfig)
            {
                result.AddError("Invalid configuration type for ImageRecorderValidator");
                return result;
            }

            // Validate resolution
            if (imageConfig.Width <= 0 || imageConfig.Height <= 0)
            {
                result.AddError("Image resolution must be positive");
            }
            else if (imageConfig.Width > 8192 || imageConfig.Height > 8192)
            {
                result.AddWarning("Very high resolution may cause performance issues");
            }

            // Validate JPEG quality
            if (imageConfig.OutputFormat == UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                if (imageConfig.Quality < 0.1f || imageConfig.Quality > 1.0f)
                {
                    result.AddError("JPEG quality must be between 0.1 and 1.0");
                }
                
                if (imageConfig.CaptureAlpha)
                {
                    result.AddWarning("JPEG format does not support alpha channel");
                }
            }

            // Validate source-specific settings
            switch (imageConfig.SourceType)
            {
                case ImageRecorderSourceType.TargetCamera:
                    if (imageConfig.TargetCamera == null)
                    {
                        result.AddError("Target camera is required when using TargetCamera source");
                    }
                    break;
                    
                case ImageRecorderSourceType.RenderTexture:
                    if (imageConfig.RenderTexture == null)
                    {
                        result.AddError("Render texture is required when using RenderTexture source");
                    }
                    break;
            }

            return result;
        }
    }

    /// <summary>
    /// Validator for Movie Recorder configurations
    /// </summary>
    public class MovieRecorderValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;

        public MovieRecorderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(IRecorderConfiguration configuration)
        {
            var result = new ValidationResult();
            
            if (configuration is not MovieRecorderConfiguration movieConfig)
            {
                result.AddError("Invalid configuration type for MovieRecorderValidator");
                return result;
            }

            // Validate video quality
            if (movieConfig.Quality < 0.1f || movieConfig.Quality > 1.0f)
            {
                result.AddError("Video quality must be between 0.1 and 1.0");
            }

            // Validate bitrate for ProRes
            if (movieConfig.OutputFormat == UnityEditor.Recorder.MovieRecorderSettings.VideoRecorderOutputFormat.MOV)
            {
                if (movieConfig.VideoBitRateMode == UnityEditor.Recorder.VideoBitrateMode.High)
                {
                    result.AddWarning("High bitrate with ProRes format will create very large files");
                }
            }

            // Validate audio settings
            if (movieConfig.CaptureAudio && movieConfig.AudioCodec == UnityEditor.Recorder.AudioRecorderSettings.AudioCodec.None)
            {
                result.AddWarning("Audio capture is enabled but no audio codec is selected");
            }

            return result;
        }
    }

    /// <summary>
    /// Validator for Animation Recorder configurations
    /// </summary>
    public class AnimationRecorderValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;

        public AnimationRecorderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(IRecorderConfiguration configuration)
        {
            var result = new ValidationResult();
            
            if (configuration is not AnimationRecorderConfiguration animConfig)
            {
                result.AddError("Invalid configuration type for AnimationRecorderValidator");
                return result;
            }

            // Validate target GameObject
            if (animConfig.TargetGameObject == null)
            {
                result.AddError("Target GameObject is required for animation recording");
            }

            // Validate recording options
            if (!animConfig.RecordTransform && !animConfig.RecordComponents && !animConfig.RecordActiveState)
            {
                result.AddWarning("No recording options selected - animation clip will be empty");
            }

            // Validate frame rate
            if (animConfig.FrameRate < 1 || animConfig.FrameRate > 120)
            {
                result.AddError("Animation frame rate must be between 1 and 120");
            }

            // Validate error tolerances for keyframe reduction
            if (animConfig.KeyframeReduction)
            {
                if (animConfig.PositionError <= 0)
                {
                    result.AddWarning("Position error tolerance should be positive for effective keyframe reduction");
                }
                if (animConfig.RotationError <= 0)
                {
                    result.AddWarning("Rotation error tolerance should be positive for effective keyframe reduction");
                }
                if (animConfig.ScaleError <= 0)
                {
                    result.AddWarning("Scale error tolerance should be positive for effective keyframe reduction");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Validator for Alembic Recorder configurations
    /// </summary>
    public class AlembicRecorderValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;

        public AlembicRecorderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(IRecorderConfiguration configuration)
        {
            var result = new ValidationResult();
            
            if (configuration is not AlembicRecorderConfiguration alembicConfig)
            {
                result.AddError("Invalid configuration type for AlembicRecorderValidator");
                return result;
            }

            // Validate target GameObject
            if (alembicConfig.TargetGameObject == null)
            {
                result.AddError("Target GameObject is required for Alembic recording");
            }

            // Validate scale factor
            if (alembicConfig.ScaleFactor <= 0)
            {
                result.AddError("Scale factor must be positive");
            }
            else if (Math.Abs(alembicConfig.ScaleFactor - 1.0f) > 0.001f)
            {
                result.AddWarning($"Scale factor {alembicConfig.ScaleFactor} will modify the exported geometry scale");
            }

            // Validate capture options
            if (!alembicConfig.CaptureTransform && !alembicConfig.CaptureMeshRenderer && 
                !alembicConfig.CaptureSkinnedMeshRenderer && !alembicConfig.CaptureCamera)
            {
                result.AddWarning("No capture options selected - Alembic file will be empty");
            }

            // Check for Alembic package
            if (!IsAlembicPackageInstalled())
            {
                result.AddError("Alembic for Unity package is not installed. Install it from Package Manager.");
            }

            return result;
        }

        private bool IsAlembicPackageInstalled()
        {
            // Check if Alembic package is installed
            // This is a simplified check - actual implementation would query Package Manager
            return Type.GetType("UnityEngine.Recorder.AlembicRecorderSettings, Unity.Recorder.Alembic") != null;
        }
    }

    /// <summary>
    /// Validator for FBX Recorder configurations
    /// </summary>
    public class FBXRecorderValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;

        public FBXRecorderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(IRecorderConfiguration configuration)
        {
            var result = new ValidationResult();
            
            if (configuration is not FBXRecorderConfiguration fbxConfig)
            {
                result.AddError("Invalid configuration type for FBXRecorderValidator");
                return result;
            }

            // Validate target GameObject
            if (fbxConfig.TargetGameObject == null)
            {
                result.AddError("Target GameObject is required for FBX recording");
            }

            // Validate export options
            if (!fbxConfig.ExportMeshes && !fbxConfig.ExportAnimation)
            {
                result.AddWarning("Neither meshes nor animations are set to export - FBX file will be empty");
            }

            // Validate unit scale
            if (fbxConfig.UnitScale <= 0)
            {
                result.AddError("Unit scale must be positive");
            }

            // Check for FBX Exporter package
            if (!IsFBXExporterPackageInstalled())
            {
                result.AddError("FBX Exporter package is not installed. Install it from Package Manager.");
            }

            return result;
        }

        private bool IsFBXExporterPackageInstalled()
        {
            // Check if FBX Exporter package is installed
            // This is a simplified check - actual implementation would query Package Manager
            return Type.GetType("UnityEditor.Formats.Fbx.Exporter.FbxExporter, Unity.Formats.Fbx.Editor") != null;
        }
    }

    /// <summary>
    /// Validator for AOV Recorder configurations
    /// </summary>
    public class AOVRecorderValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;

        public AOVRecorderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(IRecorderConfiguration configuration)
        {
            var result = new ValidationResult();
            
            if (configuration is not AOVRecorderConfiguration aovConfig)
            {
                result.AddError("Invalid configuration type for AOVRecorderValidator");
                return result;
            }

            // Validate custom AOV name
            if (aovConfig.AOVType == AOVType.Custom && string.IsNullOrWhiteSpace(aovConfig.CustomAOVName))
            {
                result.AddError("Custom AOV name is required when AOV type is Custom");
            }

            // Validate output format for AOV
            if (aovConfig.OutputFormat != UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR &&
                aovConfig.OutputFormat != UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG)
            {
                result.AddWarning("EXR format is recommended for AOV passes to preserve full dynamic range");
            }

            // Validate super sampling
            var validSuperSampling = new[] { 1, 2, 4, 8, 16 };
            if (!Array.Exists(validSuperSampling, x => x == aovConfig.SuperSampling))
            {
                result.AddWarning($"Super sampling should be 1, 2, 4, 8, or 16. Current value: {aovConfig.SuperSampling}");
            }

            // Validate source type
            if (aovConfig.SourceType == ImageRecorderSourceType.TargetCamera && 
                string.IsNullOrEmpty(aovConfig.CameraTag))
            {
                result.AddWarning("Camera tag should be specified when using TargetCamera source");
            }

            // Check render pipeline compatibility
            var renderPipeline = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
            if (renderPipeline == null)
            {
                result.AddWarning("AOV recording works best with Scriptable Render Pipeline (URP/HDRP)");
            }

            return result;
        }
    }
}