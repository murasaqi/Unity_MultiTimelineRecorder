using System.IO;
using UnityEngine;

namespace BatchRenderingTool
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
            
            // Already absolute path
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);
            
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
            string combined = Path.Combine(parts);
            return combined.Replace('\\', '/');
        }
    }
}