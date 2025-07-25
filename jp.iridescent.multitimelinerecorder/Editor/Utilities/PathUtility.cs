using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Utility class for handling file paths in Unity projects
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// Get the project root directory (parent of Assets folder)
        /// </summary>
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        
        /// <summary>
        /// Convert a path to absolute path, handling Unity project-relative paths correctly
        /// </summary>
        public static string GetAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            // Check if path contains wildcards
            bool hasWildcards = path.Contains('<') || path.Contains('>');
            
            // Skip Path.IsPathRooted check for paths with wildcards to avoid exception
            if (hasWildcards)
            {
                // For paths with wildcards, check if it starts with a drive letter or UNC path
                if (path.Length >= 2 && path[1] == ':' || path.StartsWith("\\\\") || path.StartsWith("//"))
                {
                    return path;
                }
                // Otherwise treat as relative path
                if (path.StartsWith("Assets"))
                {
                    return CombineAndNormalize(ProjectRoot, path);
                }
                return CombineAndNormalize(ProjectRoot, path);
            }
            
            // Already absolute path
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }
            
            // Handle paths starting with "Assets"
            if (path.StartsWith("Assets"))
            {
                // This is a project-relative path starting from Assets folder
                return Path.GetFullPath(Path.Combine(ProjectRoot, path));
            }
            
            // Other relative paths are relative to project root
            return Path.GetFullPath(Path.Combine(ProjectRoot, path));
        }
        
        /// <summary>
        /// Convert an absolute path to project-relative path if possible
        /// </summary>
        public static string GetProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;
            
            string fullPath = Path.GetFullPath(absolutePath);
            string projectRoot = ProjectRoot;
            
            // Check if path is within project
            if (fullPath.StartsWith(projectRoot))
            {
                // Remove project root and leading separator
                string relativePath = fullPath.Substring(projectRoot.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    relativePath = relativePath.Substring(1);
                
                return relativePath.Replace('\\', '/');
            }
            
            // Path is outside project, return as-is
            return absolutePath;
        }
        
        /// <summary>
        /// Ensure directory exists for the given path
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            
            // Skip directory creation for paths with wildcards
            if (path.Contains('<') || path.Contains('>'))
                return;
            
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        /// <summary>
        /// Combine path parts and normalize the result
        /// </summary>
        public static string CombineAndNormalize(params string[] parts)
        {
            // Check if any part contains wildcards (< or > characters)
            bool hasWildcards = false;
            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part) && (part.Contains('<') || part.Contains('>')))
                {
                    hasWildcards = true;
                    break;
                }
            }
            
            string combined;
            if (hasWildcards)
            {
                // Use simple string concatenation for paths with wildcards
                // Path.Combine would throw exception on < and > characters
                combined = string.Join("/", parts.Where(p => !string.IsNullOrEmpty(p)));
            }
            else
            {
                // Use Path.Combine for regular paths
                combined = Path.Combine(parts);
            }
            
            return combined.Replace('\\', '/');
        }
    }
}