using System;
using System.IO;
using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Manages output path resolution and relationships between global and recorder-specific paths
    /// </summary>
    public static class OutputPathManager
    {
        /// <summary>
        /// Resolves the final output path for a recorder based on its configuration
        /// </summary>
        /// <param name="globalPath">The global output path settings</param>
        /// <param name="recorderPath">The recorder-specific output path settings</param>
        /// <returns>The resolved output path</returns>
        public static string ResolveRecorderPath(OutputPathSettings globalPath, OutputPathSettings recorderPath)
        {
            if (globalPath == null)
                throw new ArgumentNullException(nameof(globalPath));
            if (recorderPath == null)
                throw new ArgumentNullException(nameof(recorderPath));

            switch (recorderPath.pathMode)
            {
                case RecorderPathMode.UseGlobal:
                    return globalPath.GetResolvedPath();

                case RecorderPathMode.RelativeToGlobal:
                    string basePath = globalPath.GetResolvedPath();
                    string relativePath = recorderPath.customPath;
                    
                    // Handle paths with wildcards
                    if (ContainsWildcards(basePath) || ContainsWildcards(relativePath))
                    {
                        // For paths with wildcards, use simple string concatenation
                        return basePath + "/" + relativePath;
                    }
                    else
                    {
                        // For regular paths, use Path.Combine
                        return Path.GetFullPath(Path.Combine(basePath, relativePath));
                    }

                case RecorderPathMode.Custom:
                    return recorderPath.GetResolvedPath();

                default:
                    throw new ArgumentOutOfRangeException(nameof(recorderPath.pathMode));
            }
        }

        /// <summary>
        /// Gets the effective output path settings for a recorder
        /// </summary>
        /// <param name="globalPath">The global output path settings</param>
        /// <param name="recorderPath">The recorder-specific output path settings</param>
        /// <returns>The effective output path settings to use</returns>
        public static OutputPathSettings GetEffectivePathSettings(OutputPathSettings globalPath, OutputPathSettings recorderPath)
        {
            if (recorderPath == null || recorderPath.pathMode == RecorderPathMode.UseGlobal)
                return globalPath;
            
            return recorderPath;
        }

        /// <summary>
        /// Validates output path settings
        /// </summary>
        /// <param name="settings">The settings to validate</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidatePathSettings(OutputPathSettings settings, out string errorMessage)
        {
            errorMessage = null;

            if (settings == null)
            {
                errorMessage = "Output path settings cannot be null";
                return false;
            }

            if (string.IsNullOrEmpty(settings.path) && settings.pathMode != RecorderPathMode.UseGlobal)
            {
                errorMessage = "Output path cannot be empty";
                return false;
            }

            // Check for invalid characters (excluding wildcards)
            string pathToCheck = settings.path;
            if (!string.IsNullOrEmpty(pathToCheck))
            {
                // Remove wildcards temporarily for validation
                pathToCheck = RemoveWildcards(pathToCheck);
                
                char[] invalidChars = Path.GetInvalidPathChars();
                foreach (char c in invalidChars)
                {
                    if (pathToCheck.Contains(c.ToString()))
                    {
                        errorMessage = $"Output path contains invalid character: {c}";
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a copy of output path settings with independent values
        /// </summary>
        /// <param name="source">The source settings to copy</param>
        /// <returns>A new independent copy of the settings</returns>
        public static OutputPathSettings ClonePathSettings(OutputPathSettings source)
        {
            if (source == null)
                return new OutputPathSettings();

            return new OutputPathSettings
            {
                pathMode = source.pathMode,
                location = source.location,
                path = source.path,
                customPath = source.customPath
            };
        }

        /// <summary>
        /// Checks if a path contains wildcards
        /// </summary>
        private static bool ContainsWildcards(string path)
        {
            return !string.IsNullOrEmpty(path) && (path.Contains("<") || path.Contains(">"));
        }

        /// <summary>
        /// Removes wildcards from a path for validation purposes
        /// </summary>
        private static string RemoveWildcards(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Remove all wildcard patterns
            string result = path;
            string[] wildcards = { "<Scene>", "<Timeline>", "<Take>", "<Date>", "<Time>", 
                                 "<Resolution>", "<Recorder>", "<Frame>", "<GameObject>", "<Product>" };
            
            foreach (string wildcard in wildcards)
            {
                result = result.Replace(wildcard, "");
            }

            return result;
        }

        /// <summary>
        /// Gets a display-friendly path for UI purposes
        /// </summary>
        /// <param name="globalPath">The global output path settings</param>
        /// <param name="recorderPath">The recorder-specific output path settings</param>
        /// <returns>A formatted path string for display</returns>
        public static string GetDisplayPath(OutputPathSettings globalPath, OutputPathSettings recorderPath)
        {
            string resolvedPath = ResolveRecorderPath(globalPath, recorderPath);
            
            // Truncate long paths for display
            if (resolvedPath.Length > 60)
            {
                return "..." + resolvedPath.Substring(resolvedPath.Length - 57);
            }
            
            return resolvedPath;
        }
    }
}