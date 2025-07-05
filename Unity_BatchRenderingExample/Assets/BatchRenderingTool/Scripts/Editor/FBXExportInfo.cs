using UnityEditor;
using System.Linq;

namespace BatchRenderingTool
{
    /// <summary>
    /// Utility class for FBX export information and validation
    /// </summary>
    public static class FBXExportInfo
    {
        private static bool? fbxPackageAvailable;
        
        /// <summary>
        /// Check if FBX package is available
        /// </summary>
        public static bool IsFBXPackageAvailable()
        {
            if (!fbxPackageAvailable.HasValue)
            {
                // Check if the FBX Exporter package is installed
                var packageInfo = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    .FirstOrDefault(p => p.name == "com.unity.formats.fbx");
                
                fbxPackageAvailable = packageInfo != null;
                
                if (!fbxPackageAvailable.Value)
                {
                    // Also check if the type exists directly
                    var fbxRecorderType = System.Type.GetType("UnityEditor.Formats.Fbx.Exporter.FbxRecorderSettings, Unity.Formats.Fbx.Runtime.Editor");
                    fbxPackageAvailable = fbxRecorderType != null;
                }
                
                BatchRenderingToolLogger.LogVerbose($"[FBXExportInfo] FBX package available: {fbxPackageAvailable.Value}");
            }
            
            return fbxPackageAvailable.Value;
        }
        
        /// <summary>
        /// Clear the cached availability status (useful for testing)
        /// </summary>
        public static void ClearCache()
        {
            fbxPackageAvailable = null;
        }
    }
}