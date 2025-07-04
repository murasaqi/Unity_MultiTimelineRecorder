using System;
using System.Collections.Generic;
using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Animation recording property types
    /// </summary>
    [Flags]
    public enum AnimationRecordingProperties
    {
        None = 0,
        
        // Transform properties
        Position = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,
        
        // Renderer properties
        BlendShapes = 1 << 3,
        MaterialProperties = 1 << 4,
        
        // Component properties
        LightProperties = 1 << 5,
        CameraProperties = 1 << 6,
        
        // Custom properties
        CustomProperties = 1 << 7,
        
        // Presets
        TransformOnly = Position | Rotation | Scale,
        TransformAndBlendShapes = Position | Rotation | Scale | BlendShapes,
        AllProperties = Position | Rotation | Scale | BlendShapes | MaterialProperties | LightProperties | CameraProperties | CustomProperties
    }
    
    /// <summary>
    /// Animation curve interpolation mode
    /// </summary>
    public enum AnimationInterpolationMode
    {
        Linear,
        Smooth,
        Flat,
        Stepped
    }
    
    /// <summary>
    /// Animation compression settings
    /// </summary>
    public enum AnimationCompressionLevel
    {
        None,        // No compression
        Low,         // Minimal compression
        Medium,      // Balanced compression
        High,        // Aggressive compression
        Optimal      // Auto-optimized
    }
    
    /// <summary>
    /// Animation recording scope
    /// </summary>
    public enum AnimationRecordingScope
    {
        SingleGameObject,      // Record single GameObject
        GameObjectAndChildren, // Record GameObject and all children
        SelectedHierarchy,     // Record selected objects
        CustomSelection       // Record manually selected objects
    }
    
    /// <summary>
    /// Helper class for animation recording information
    /// </summary>
    public static class AnimationRecordingInfo
    {
        public class PropertyInfo
        {
            public string DisplayName { get; set; }
            public string PropertyPath { get; set; }
            public Type PropertyType { get; set; }
            public string Description { get; set; }
        }
        
        private static readonly Dictionary<AnimationRecordingProperties, PropertyInfo> propertyInfoMap = new Dictionary<AnimationRecordingProperties, PropertyInfo>
        {
            { AnimationRecordingProperties.Position, new PropertyInfo 
                { 
                    DisplayName = "Position", 
                    PropertyPath = "m_LocalPosition",
                    PropertyType = typeof(Vector3),
                    Description = "Record object position (local space)"
                }
            },
            { AnimationRecordingProperties.Rotation, new PropertyInfo 
                { 
                    DisplayName = "Rotation", 
                    PropertyPath = "m_LocalRotation",
                    PropertyType = typeof(Quaternion),
                    Description = "Record object rotation (local space)"
                }
            },
            { AnimationRecordingProperties.Scale, new PropertyInfo 
                { 
                    DisplayName = "Scale", 
                    PropertyPath = "m_LocalScale",
                    PropertyType = typeof(Vector3),
                    Description = "Record object scale"
                }
            },
            { AnimationRecordingProperties.BlendShapes, new PropertyInfo 
                { 
                    DisplayName = "Blend Shapes", 
                    PropertyPath = "blendShape",
                    PropertyType = typeof(float),
                    Description = "Record blend shape weights"
                }
            },
            { AnimationRecordingProperties.MaterialProperties, new PropertyInfo 
                { 
                    DisplayName = "Material Properties", 
                    PropertyPath = "material",
                    PropertyType = typeof(Material),
                    Description = "Record material property changes"
                }
            },
            { AnimationRecordingProperties.LightProperties, new PropertyInfo 
                { 
                    DisplayName = "Light Properties", 
                    PropertyPath = "m_Intensity",
                    PropertyType = typeof(float),
                    Description = "Record light intensity and color"
                }
            },
            { AnimationRecordingProperties.CameraProperties, new PropertyInfo 
                { 
                    DisplayName = "Camera Properties", 
                    PropertyPath = "field of view",
                    PropertyType = typeof(float),
                    Description = "Record camera FOV and other properties"
                }
            },
            { AnimationRecordingProperties.CustomProperties, new PropertyInfo 
                { 
                    DisplayName = "Custom Properties", 
                    PropertyPath = "custom",
                    PropertyType = typeof(object),
                    Description = "Record user-defined properties"
                }
            }
        };
        
        /// <summary>
        /// Get property information
        /// </summary>
        public static PropertyInfo GetPropertyInfo(AnimationRecordingProperties property)
        {
            return propertyInfoMap.ContainsKey(property) ? propertyInfoMap[property] : null;
        }
        
        /// <summary>
        /// Get compression settings recommendation
        /// </summary>
        public static class CompressionPresets
        {
            public static float GetPositionErrorTolerance(AnimationCompressionLevel level)
            {
                switch (level)
                {
                    case AnimationCompressionLevel.None: return 0f;
                    case AnimationCompressionLevel.Low: return 0.001f;
                    case AnimationCompressionLevel.Medium: return 0.01f;
                    case AnimationCompressionLevel.High: return 0.05f;
                    case AnimationCompressionLevel.Optimal: return 0.005f;
                    default: return 0.01f;
                }
            }
            
            public static float GetRotationErrorTolerance(AnimationCompressionLevel level)
            {
                switch (level)
                {
                    case AnimationCompressionLevel.None: return 0f;
                    case AnimationCompressionLevel.Low: return 0.1f;
                    case AnimationCompressionLevel.Medium: return 0.5f;
                    case AnimationCompressionLevel.High: return 1f;
                    case AnimationCompressionLevel.Optimal: return 0.25f;
                    default: return 0.5f;
                }
            }
            
            public static float GetScaleErrorTolerance(AnimationCompressionLevel level)
            {
                switch (level)
                {
                    case AnimationCompressionLevel.None: return 0f;
                    case AnimationCompressionLevel.Low: return 0.001f;
                    case AnimationCompressionLevel.Medium: return 0.01f;
                    case AnimationCompressionLevel.High: return 0.05f;
                    case AnimationCompressionLevel.Optimal: return 0.005f;
                    default: return 0.01f;
                }
            }
        }
    }
    
    /// <summary>
    /// Animation export preset types
    /// </summary>
    public enum AnimationExportPreset
    {
        Custom,
        CharacterAnimation,
        CameraAnimation,
        SimpleTransform,
        ComplexAnimation
    }
}