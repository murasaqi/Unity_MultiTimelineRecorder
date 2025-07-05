using System;
using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Alembic export target types
    /// </summary>
    [Flags]
    public enum AlembicExportTargets
    {
        None = 0,
        MeshRenderer = 1 << 0,
        SkinnedMeshRenderer = 1 << 1,
        Camera = 1 << 2,
        Transform = 1 << 3,
        ParticleSystem = 1 << 4,
        Light = 1 << 5
    }
    
    /// <summary>
    /// Alembic coordinate system handedness
    /// </summary>
    public enum AlembicHandedness
    {
        Left,  // Unity default
        Right  // Maya, Max, etc.
    }
    
    /// <summary>
    /// Alembic time sampling mode
    /// </summary>
    public enum AlembicTimeSamplingMode
    {
        Uniform,      // Regular frame intervals
        Acyclic      // Irregular time samples
    }
    
    /// <summary>
    /// Alembic export scope
    /// </summary>
    public enum AlembicExportScope
    {
        EntireScene,           // Export all objects in scene
        SelectedHierarchy,     // Export selected GameObject and children
        TargetGameObject,      // Export specific GameObject
        CustomSelection        // Export manually selected objects
    }
    
    /// <summary>
    /// Helper class for Alembic export settings information
    /// </summary>
    public static class AlembicExportInfo
    {
        /// <summary>
        /// Check if Unity has Alembic package installed
        /// </summary>
        public static bool IsAlembicPackageAvailable()
        {
            #if ALEMBIC_PACKAGE_AVAILABLE
            return true;
            #else
            // Check multiple possible Alembic types to ensure package is available
            var alembicTypes = new string[]
            {
                "UnityEngine.Formats.Alembic.Importer.AlembicStreamPlayer, Unity.Formats.Alembic.Runtime",
                "UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor",
                "UnityEditor.Formats.Alembic.Exporter.AlembicExporter, Unity.Formats.Alembic.Editor"
            };
            
            foreach (var typeName in alembicTypes)
            {
                var type = System.Type.GetType(typeName);
                if (type != null)
                {
                    return true;
                }
            }
            
            // Also check if the Alembic package assembly is loaded
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Unity.Formats.Alembic"))
                {
                    return true;
                }
            }
            
            return false;
            #endif
        }
        
        /// <summary>
        /// Get display name for export target
        /// </summary>
        public static string GetExportTargetDisplayName(AlembicExportTargets target)
        {
            switch (target)
            {
                case AlembicExportTargets.MeshRenderer:
                    return "Static Meshes";
                case AlembicExportTargets.SkinnedMeshRenderer:
                    return "Skinned Meshes";
                case AlembicExportTargets.Camera:
                    return "Cameras";
                case AlembicExportTargets.Transform:
                    return "Transform Hierarchy";
                case AlembicExportTargets.ParticleSystem:
                    return "Particle Systems";
                case AlembicExportTargets.Light:
                    return "Lights";
                default:
                    return target.ToString();
            }
        }
        
        /// <summary>
        /// Get description for export target
        /// </summary>
        public static string GetExportTargetDescription(AlembicExportTargets target)
        {
            switch (target)
            {
                case AlembicExportTargets.MeshRenderer:
                    return "Export static mesh geometry and vertex colors";
                case AlembicExportTargets.SkinnedMeshRenderer:
                    return "Export animated mesh deformations";
                case AlembicExportTargets.Camera:
                    return "Export camera parameters and animation";
                case AlembicExportTargets.Transform:
                    return "Export object transform hierarchy and animation";
                case AlembicExportTargets.ParticleSystem:
                    return "Export particle positions as point cloud";
                case AlembicExportTargets.Light:
                    return "Export light parameters (limited support)";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// Get recommended settings for different use cases
        /// </summary>
        public static class Presets
        {
            public static AlembicExportTargets GetAnimationExport()
            {
                return AlembicExportTargets.MeshRenderer | 
                       AlembicExportTargets.SkinnedMeshRenderer | 
                       AlembicExportTargets.Transform;
            }
            
            public static AlembicExportTargets GetCameraExport()
            {
                return AlembicExportTargets.Camera | 
                       AlembicExportTargets.Transform;
            }
            
            public static AlembicExportTargets GetFullSceneExport()
            {
                return AlembicExportTargets.MeshRenderer | 
                       AlembicExportTargets.SkinnedMeshRenderer | 
                       AlembicExportTargets.Camera | 
                       AlembicExportTargets.Transform |
                       AlembicExportTargets.Light;
            }
            
            public static AlembicExportTargets GetEffectsExport()
            {
                return AlembicExportTargets.ParticleSystem | 
                       AlembicExportTargets.Transform;
            }
        }
    }
    
    /// <summary>
    /// Alembic export preset types
    /// </summary>
    public enum AlembicExportPreset
    {
        Custom,
        AnimationExport,
        CameraExport,
        FullSceneExport,
        EffectsExport
    }
}