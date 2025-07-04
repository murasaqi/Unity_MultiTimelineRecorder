using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Helper class for RecorderSettings configuration and validation
    /// </summary>
    public static class RecorderSettingsHelper
    {
        /// <summary>
        /// Supported image output formats
        /// </summary>
        public enum ImageFormat
        {
            PNG,
            JPG,
            EXR
        }
        /// <summary>
        /// Configure output path for any RecorderSettings
        /// </summary>
        public static void ConfigureOutputPath(RecorderSettings settings, string basePath, string timelineName, RecorderSettingsType type)
        {
            string fullOutputPath = basePath;
            if (!Path.IsPathRooted(fullOutputPath))
            {
                fullOutputPath = Path.Combine(Application.dataPath, "..", basePath);
            }
            
            string sanitizedName = SanitizeFileName(timelineName);
            string finalPath = Path.Combine(fullOutputPath, sanitizedName);
            finalPath = Path.GetFullPath(finalPath);
            
            if (!Directory.Exists(finalPath))
            {
                Directory.CreateDirectory(finalPath);
            }
            
            // Set output file based on recorder type
            switch (type)
            {
                case RecorderSettingsType.Image:
                    var imageSettings = settings as ImageRecorderSettings;
                    if (imageSettings != null)
                    {
                        imageSettings.OutputFile = $"{finalPath}/{sanitizedName}_<Frame>";
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    var movieSettings = settings as MovieRecorderSettings;
                    if (movieSettings != null)
                    {
                        movieSettings.OutputFile = $"{finalPath}/{sanitizedName}";
                    }
                    break;
                    
                case RecorderSettingsType.AOV:
                    // AOV output paths are handled differently as they may have multiple outputs
                    // For now, use a similar pattern to image sequence
                    if (settings is ImageRecorderSettings aovSettings)
                    {
                        // Extract AOV type from settings name if possible
                        string aovSuffix = "";
                        if (settings.name.Contains("_AOV_"))
                        {
                            var parts = settings.name.Split(new[] { "_AOV_" }, StringSplitOptions.None);
                            if (parts.Length > 1)
                            {
                                aovSuffix = $"_{parts[1]}";
                            }
                        }
                        aovSettings.OutputFile = $"{finalPath}/{sanitizedName}{aovSuffix}_<Frame>";
                    }
                    break;
                    
                case RecorderSettingsType.Alembic:
                    // Alembic uses a single .abc file
                    // Placeholder: actual AlembicRecorderSettings would have its own output property
                    if (settings is ImageRecorderSettings alembicPlaceholder)
                    {
                        alembicPlaceholder.OutputFile = $"{finalPath}/{sanitizedName}";
                    }
                    break;
                    
                case RecorderSettingsType.Animation:
                    // Animation uses a single .anim file
                    var animationSettings = settings as AnimationRecorderSettings;
                    if (animationSettings != null)
                    {
                        animationSettings.OutputFile = $"{finalPath}/{sanitizedName}";
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Validate RecorderSettings configuration
        /// </summary>
        public static bool ValidateRecorderSettings(RecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (settings == null)
            {
                errorMessage = "RecorderSettings is null";
                return false;
            }
            
            if (!settings.Enabled)
            {
                errorMessage = "RecorderSettings is not enabled";
                return false;
            }
            
            // Type-specific validation
            if (settings is MovieRecorderSettings movieSettings)
            {
                return ValidateMovieRecorderSettings(movieSettings, out errorMessage);
            }
            else if (settings is ImageRecorderSettings imageSettings)
            {
                return ValidateImageRecorderSettings(imageSettings, out errorMessage);
            }
            else if (IsAOVRecorderSettings(settings))
            {
                return ValidateAOVRecorderSettings(settings, out errorMessage);
            }
            else if (IsAlembicRecorderSettings(settings))
            {
                return ValidateAlembicRecorderSettings(settings, out errorMessage);
            }
            else if (IsAnimationRecorderSettings(settings))
            {
                return ValidateAnimationRecorderSettings(settings, out errorMessage);
            }
            
            return true;
        }
        
        /// <summary>
        /// Validate MovieRecorderSettings
        /// </summary>
        private static bool ValidateMovieRecorderSettings(MovieRecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check codec support
            if (!IsCodecSupported(settings.OutputFormat))
            {
                errorMessage = $"Codec {settings.OutputFormat} is not supported on this platform";
                return false;
            }
            
            // Validate resolution
            var inputSettings = settings.ImageInputSettings as GameViewInputSettings;
            if (inputSettings != null)
            {
                if (inputSettings.OutputWidth <= 0 || inputSettings.OutputHeight <= 0)
                {
                    errorMessage = "Invalid output resolution";
                    return false;
                }
                
                // Check if resolution is too high
                if (inputSettings.OutputWidth > 4096 || inputSettings.OutputHeight > 4096)
                {
                    errorMessage = "Output resolution exceeds maximum supported (4096x4096)";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Validate ImageRecorderSettings
        /// </summary>
        private static bool ValidateImageRecorderSettings(ImageRecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Validate resolution
            var inputSettings = settings.imageInputSettings as GameViewInputSettings;
            if (inputSettings != null)
            {
                if (inputSettings.OutputWidth <= 0 || inputSettings.OutputHeight <= 0)
                {
                    errorMessage = "Invalid output resolution";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if a video codec is supported on the current platform
        /// </summary>
        private static bool IsCodecSupported(MovieRecorderSettings.VideoRecorderOutputFormat format)
        {
            // In Unity, all codecs are generally supported in the editor
            // This is a placeholder for platform-specific checks
            switch (format)
            {
                case MovieRecorderSettings.VideoRecorderOutputFormat.MP4:
                case MovieRecorderSettings.VideoRecorderOutputFormat.WebM:
                    return true;
                    
                case MovieRecorderSettings.VideoRecorderOutputFormat.MOV:
                    // MOV with ProRes is primarily for macOS
                    #if UNITY_EDITOR_OSX
                    return true;
                    #else
                    return false;
                    #endif
                    
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Get file extension for recorder type and format
        /// </summary>
        public static string GetFileExtension(RecorderSettings settings)
        {
            if (settings is ImageRecorderSettings imageSettings)
            {
                switch (imageSettings.OutputFormat)
                {
                    case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                        return "png";
                    case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                        return "jpg";
                    case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                        return "exr";
                    default:
                        return "png";
                }
            }
            else if (settings is MovieRecorderSettings movieSettings)
            {
                switch (movieSettings.OutputFormat)
                {
                    case MovieRecorderSettings.VideoRecorderOutputFormat.MP4:
                        return "mp4";
                    case MovieRecorderSettings.VideoRecorderOutputFormat.MOV:
                        return "mov";
                    case MovieRecorderSettings.VideoRecorderOutputFormat.WebM:
                        return "webm";
                    default:
                        return "mp4";
                }
            }
            else if (IsAlembicRecorderSettings(settings))
            {
                return "abc";
            }
            else if (IsAnimationRecorderSettings(settings))
            {
                return "anim";
            }
            
            return "";
        }
        
        /// <summary>
        /// Check if settings is an AOV recorder (placeholder check)
        /// </summary>
        public static bool IsAOVRecorderSettings(RecorderSettings settings)
        {
            // Since Unity Recorder's AOV API might not be directly accessible,
            // we check by name pattern or type name
            return settings != null && 
                   (settings.name.Contains("AOV") || 
                    settings.GetType().Name.Contains("AOV"));
        }
        
        /// <summary>
        /// Validate AOVRecorderSettings
        /// </summary>
        private static bool ValidateAOVRecorderSettings(RecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check if HDRP is available
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                errorMessage = "AOV Recorder requires HDRP package";
                return false;
            }
            
            // For now, use basic validation similar to ImageRecorderSettings
            if (settings is ImageRecorderSettings imageSettings)
            {
                return ValidateImageRecorderSettings(imageSettings, out errorMessage);
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if settings is an Alembic recorder (placeholder check)
        /// </summary>
        public static bool IsAlembicRecorderSettings(RecorderSettings settings)
        {
            // Since Unity Recorder's Alembic API might not be directly accessible,
            // we check by name pattern or type name
            return settings != null && 
                   (settings.name.Contains("Alembic") || 
                    settings.GetType().Name.Contains("Alembic"));
        }
        
        /// <summary>
        /// Validate AlembicRecorderSettings
        /// </summary>
        private static bool ValidateAlembicRecorderSettings(RecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check if Alembic package is available
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                errorMessage = "Alembic Recorder requires Unity Alembic package";
                return false;
            }
            
            // For now, use basic validation similar to ImageRecorderSettings
            if (settings is ImageRecorderSettings imageSettings)
            {
                return ValidateImageRecorderSettings(imageSettings, out errorMessage);
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if settings is an Animation recorder (placeholder check)
        /// </summary>
        public static bool IsAnimationRecorderSettings(RecorderSettings settings)
        {
            // Check if it's an AnimationRecorderSettings instance
            return settings is AnimationRecorderSettings;
        }
        
        /// <summary>
        /// Validate AnimationRecorderSettings
        /// </summary>
        private static bool ValidateAnimationRecorderSettings(RecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            var animationSettings = settings as AnimationRecorderSettings;
            if (animationSettings == null)
            {
                errorMessage = "Invalid AnimationRecorderSettings";
                return false;
            }
            
            // Validate that we have input settings
            if (animationSettings.AnimationInputSettings == null)
            {
                errorMessage = "Animation input settings are not configured";
                return false;
            }
            
            // Validate target GameObject
            if (animationSettings.AnimationInputSettings.gameObject == null)
            {
                errorMessage = "No target GameObject specified for animation recording";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Sanitize filename for safe file system usage
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            
            // Additional characters to replace
            string additionalInvalidChars = "()[]{}";
            
            // Replace all invalid characters
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            foreach (char c in additionalInvalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            // Replace multiple underscores with single underscore
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }
            
            // Trim underscores from start and end
            sanitized = sanitized.Trim('_');
            
            // Ensure the result is not empty
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "Timeline";
            }
            
            return sanitized;
        }
        
        /// <summary>
        /// Create a test RecorderSettings for validation
        /// </summary>
        public static RecorderSettings CreateTestRecorderSettings(RecorderSettingsType type)
        {
            var settings = RecorderSettingsFactory.CreateRecorderSettings(type, $"Test_{type}");
            
            if (settings is MovieRecorderSettings movieSettings)
            {
                // Configure for a short test
                movieSettings.FrameRate = 24;
                movieSettings.CapFrameRate = true;
            }
            
            return settings;
        }
    }
}