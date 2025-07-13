using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace Unity.MultiTimelineRecorder
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
        /// Configure output path for any RecorderSettings (with separate path and file name)
        /// </summary>
        public static void ConfigureOutputPath(RecorderSettings settings, string filePath, string fileName, RecorderSettingsType type)
        {
            // Combine path and filename
            string outputFile = Path.Combine(filePath, fileName);
            ConfigureOutputPath(settings, outputFile, type);
        }
        
        /// <summary>
        /// Configure output path for any RecorderSettings (legacy method)
        /// </summary>
        public static void ConfigureOutputPath(RecorderSettings settings, string outputFile, RecorderSettingsType type)
        {
            // Normalize the output path
            if (!string.IsNullOrEmpty(outputFile))
            {
                // Get absolute path using PathUtility
                string absolutePath = PathUtility.GetAbsolutePath(outputFile);
                
                // Ensure directory exists
                PathUtility.EnsureDirectoryExists(absolutePath);
                
                // Update outputFile to use the normalized path
                outputFile = absolutePath;
            }
            
            // Set output file based on recorder type
            switch (type)
            {
                case RecorderSettingsType.Image:
                    var imageSettings = settings as ImageRecorderSettings;
                    if (imageSettings != null)
                    {
                        imageSettings.OutputFile = outputFile;
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    var movieSettings = settings as MovieRecorderSettings;
                    if (movieSettings != null)
                    {
                        movieSettings.OutputFile = outputFile;
                    }
                    break;
                    
                case RecorderSettingsType.AOV:
                    // Check if it's actual AOVRecorderSettings
                    if (settings.GetType().Name == "AOVRecorderSettings")
                    {
                        // Handle real AOVRecorderSettings
                        var aovType = settings.GetType();
                        var aovOutputFileProperty = aovType.GetProperty("OutputFile");
                        if (aovOutputFileProperty != null && aovOutputFileProperty.CanWrite)
                        {
                            // Ensure <Frame> wildcard is present for image sequences
                            string aovOutputFile = outputFile;
                            if (!aovOutputFile.Contains("<Frame>"))
                            {
                                // Add <Frame> before the file extension
                                var extension = Path.GetExtension(aovOutputFile);
                                var nameWithoutExt = Path.GetFileNameWithoutExtension(aovOutputFile);
                                var directory = Path.GetDirectoryName(aovOutputFile);
                                
                                if (!string.IsNullOrEmpty(directory))
                                {
                                    aovOutputFile = directory + Path.DirectorySeparatorChar + nameWithoutExt + "_<Frame>" + extension;
                                }
                                else
                                {
                                    aovOutputFile = nameWithoutExt + "_<Frame>" + extension;
                                }
                            }
                            
                            aovOutputFileProperty.SetValue(settings, aovOutputFile);
                            MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] Set AOVRecorderSettings output to: {aovOutputFile}");
                        }
                    }
                    else if (settings is ImageRecorderSettings aovSettings)
                    {
                        // Fallback: Handle ImageRecorderSettings as AOV
                        // Extract AOV type from settings name if possible
                        string aovType = "";
                        if (settings.name.Contains("_AOV_"))
                        {
                            var parts = settings.name.Split(new[] { "_AOV_" }, StringSplitOptions.None);
                            if (parts.Length > 1)
                            {
                                aovType = parts[1];
                            }
                        }
                        
                        // Replace <AOVType> wildcard if present
                        string aovOutputFile = outputFile;
                        if (!string.IsNullOrEmpty(aovType))
                        {
                            aovOutputFile = aovOutputFile.Replace("<AOVType>", aovType);
                        }
                        
                        // Ensure <Frame> wildcard is present for image sequences
                        if (!aovOutputFile.Contains("<Frame>"))
                        {
                            // Add <Frame> before the file extension
                            var extension = Path.GetExtension(aovOutputFile);
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(aovOutputFile);
                            var directory = Path.GetDirectoryName(aovOutputFile);
                            
                            // Combine directory with filename that includes AOV type
                            if (!string.IsNullOrEmpty(directory))
                            {
                                // Use platform-specific path separator and avoid Path.Combine with wildcards
                                aovOutputFile = directory + Path.DirectorySeparatorChar + nameWithoutExt + "_<Frame>" + extension;
                            }
                            else
                            {
                                aovOutputFile = nameWithoutExt + "_<Frame>" + extension;
                            }
                        }
                        
                        aovSettings.OutputFile = aovOutputFile;
                    }
                    break;
                    
                case RecorderSettingsType.Alembic:
                    // Alembic uses a single .abc file
                    MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] === Setting Alembic output file to: {outputFile} ===");
                    
                    // Try to set output file using reflection since AlembicRecorderSettings is not directly accessible
                    var alembicType = settings.GetType();
                    var outputFileProperty = alembicType.GetProperty("OutputFile");
                    if (outputFileProperty != null && outputFileProperty.CanWrite)
                    {
                        outputFileProperty.SetValue(settings, outputFile);
                        MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] === Successfully set Alembic OutputFile property to: {outputFile} ===");
                    }
                    else
                    {
                        // Try alternative property names
                        var fileNameProperty = alembicType.GetProperty("FileName");
                        if (fileNameProperty != null && fileNameProperty.CanWrite)
                        {
                            fileNameProperty.SetValue(settings, outputFile);
                            MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] === Successfully set Alembic FileName property to: {outputFile} ===");
                        }
                        else
                        {
                            MultiTimelineRecorderLogger.LogError($"[RecorderSettingsHelper] === Could not find output file property on AlembicRecorderSettings ===");
                        }
                    }
                    break;
                    
                case RecorderSettingsType.Animation:
                    // Animation uses a single .anim file
                    var animationSettings = settings as AnimationRecorderSettings;
                    if (animationSettings != null)
                    {
                        animationSettings.OutputFile = outputFile;
                        MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsHelper] Animation will be saved to: {outputFile}.anim");
                    }
                    break;
                    
                case RecorderSettingsType.FBX:
                    // FBX uses a single .fbx file
                    MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] === Setting FBX output file to: {outputFile} ===");
                    
                    // Try to set output file using reflection since FbxRecorderSettings is not directly accessible
                    var fbxType = settings.GetType();
                    var fbxOutputFileProperty = fbxType.GetProperty("OutputFile");
                    if (fbxOutputFileProperty != null && fbxOutputFileProperty.CanWrite)
                    {
                        fbxOutputFileProperty.SetValue(settings, outputFile);
                        MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] === Successfully set FBX OutputFile property to: {outputFile} ===");
                    }
                    else
                    {
                        // Try alternative property names
                        var fbxFileNameProperty = fbxType.GetProperty("FileName");
                        if (fbxFileNameProperty != null && fbxFileNameProperty.CanWrite)
                        {
                            fbxFileNameProperty.SetValue(settings, outputFile);
                            MultiTimelineRecorderLogger.Log($"[RecorderSettingsHelper] === Successfully set FBX FileName property to: {outputFile} ===");
                        }
                        else
                        {
                            MultiTimelineRecorderLogger.LogError($"[RecorderSettingsHelper] === Could not find output file property on FbxRecorderSettings ===");
                        }
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
            else if (IsFBXRecorderSettings(settings))
            {
                return ValidateFBXRecorderSettings(settings, out errorMessage);
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
            // Check for AOVRecorderSettings first
            if (settings.GetType().Name == "AOVRecorderSettings")
            {
                // Use reflection to get OutputFormat property
                var outputFormatProp = settings.GetType().GetProperty("OutputFormat");
                if (outputFormatProp != null)
                {
                    var outputFormat = outputFormatProp.GetValue(settings);
                    if (outputFormat != null)
                    {
                        var formatName = outputFormat.ToString();
                        if (formatName.Contains("PNG"))
                            return "png";
                        else if (formatName.Contains("JPEG") || formatName.Contains("JPG"))
                            return "jpg";
                        else if (formatName.Contains("EXR"))
                            return "exr";
                    }
                }
                // Default to EXR for AOV
                return "exr";
            }
            else if (settings is ImageRecorderSettings imageSettings)
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
            else if (IsFBXRecorderSettings(settings))
            {
                return "fbx";
            }
            
            return "";
        }
        
        /// <summary>
        /// Check if settings is an AOV recorder
        /// </summary>
        public static bool IsAOVRecorderSettings(RecorderSettings settings)
        {
            // Check for actual AOVRecorderSettings type
            return settings != null && 
                   (settings.GetType().Name == "AOVRecorderSettings" ||
                    settings.GetType().FullName == "UnityEditor.Recorder.AOVRecorderSettings" ||
                    settings.name.Contains("AOV"));
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
        /// Check if settings is an FBX recorder (placeholder check)
        /// </summary>
        public static bool IsFBXRecorderSettings(RecorderSettings settings)
        {
            // Since Unity Recorder's FBX API might not be directly accessible,
            // we check by name pattern or type name
            return settings != null && 
                   (settings.name.Contains("FBX") || 
                    settings.GetType().Name.Contains("Fbx"));
        }
        
        /// <summary>
        /// Validate FBXRecorderSettings
        /// </summary>
        private static bool ValidateFBXRecorderSettings(RecorderSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check if FBX package is available
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                errorMessage = "FBX Recorder requires Unity FBX package";
                return false;
            }
            
            // Basic validation similar to other recorders
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
        
        /// <summary>
        /// Set output path for recorder settings
        /// </summary>
        public static void SetOutputPath(RecorderSettings settings, string outputPath, string fileName)
        {
            if (settings == null)
            {
                MultiTimelineRecorderLogger.LogError("RecorderSettings is null");
                return;
            }
            
            try
            {
                // Use reflection to set OutputFile properties
                var settingsType = settings.GetType();
                
                // Try to find OutputFile property
                var outputFileProperty = settingsType.GetProperty("OutputFile");
                if (outputFileProperty != null)
                {
                    var outputFile = outputFileProperty.GetValue(settings);
                    if (outputFile != null)
                    {
                        var outputFileType = outputFile.GetType();
                        
                        // Set Directory/Path
                        var directoryProperty = outputFileType.GetProperty("Directory") ?? outputFileType.GetProperty("Path");
                        if (directoryProperty != null && directoryProperty.CanWrite)
                        {
                            directoryProperty.SetValue(outputFile, outputPath);
                        }
                        
                        // Set FileName
                        var fileNameProperty = outputFileType.GetProperty("FileName") ?? outputFileType.GetProperty("BaseFileName");
                        if (fileNameProperty != null && fileNameProperty.CanWrite)
                        {
                            fileNameProperty.SetValue(outputFile, fileName);
                        }
                    }
                }
                
                // Alternative approach for some recorder types
                var fileNameProp = settingsType.GetProperty("FileNameGenerator");
                if (fileNameProp != null)
                {
                    var fileNameGen = fileNameProp.GetValue(settings);
                    if (fileNameGen != null)
                    {
                        var genType = fileNameGen.GetType();
                        
                        // Set FileName
                        var nameProp = genType.GetProperty("FileName");
                        if (nameProp != null && nameProp.CanWrite)
                        {
                            nameProp.SetValue(fileNameGen, fileName);
                        }
                        
                        // Set Path
                        var pathProp = genType.GetProperty("Path");
                        if (pathProp != null && pathProp.CanWrite)
                        {
                            pathProp.SetValue(fileNameGen, outputPath);
                        }
                    }
                }
                
                MultiTimelineRecorderLogger.LogVerbose($"[RecorderSettingsHelper] Set output path: {outputPath}/{fileName}");
            }
            catch (Exception e)
            {
                MultiTimelineRecorderLogger.LogError($"[RecorderSettingsHelper] Failed to set output path: {e.Message}");
            }
        }
    }
}