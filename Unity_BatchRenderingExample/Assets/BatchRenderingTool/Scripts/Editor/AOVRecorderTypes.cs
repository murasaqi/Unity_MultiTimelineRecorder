using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Supported AOV (Arbitrary Output Variables) types for HDRP rendering
    /// </summary>
    [Flags]
    public enum AOVType
    {
        None = 0,
        
        // Geometry passes
        Depth = 1 << 0,
        DepthNormalized = 1 << 1,
        Normal = 1 << 2,
        MotionVectors = 1 << 3,
        
        // Material properties
        Albedo = 1 << 4,
        Specular = 1 << 5,
        Smoothness = 1 << 6,
        AmbientOcclusion = 1 << 7,
        Metal = 1 << 8,
        
        // Lighting passes
        DirectDiffuse = 1 << 9,
        DirectSpecular = 1 << 10,
        IndirectDiffuse = 1 << 11,
        IndirectSpecular = 1 << 12,
        Emissive = 1 << 13,
        Reflection = 1 << 14,
        Refraction = 1 << 15,
        
        // Additional passes
        Shadow = 1 << 16,
        ContactShadows = 1 << 17,
        ScreenSpaceReflection = 1 << 18,
        Alpha = 1 << 19,
        Beauty = 1 << 20,
        
        // Custom/Debug
        CustomPass = 1 << 21
    }
    
    /// <summary>
    /// AOV output format options
    /// </summary>
    public enum AOVOutputFormat
    {
        EXR16 = 0,  // 16-bit float EXR
        EXR32 = 1,  // 32-bit float EXR
        PNG = 2,    // PNG format (with alpha support)
        JPEG = 3    // JPEG format (no alpha support)
    }
    
    /// <summary>
    /// Helper class for AOV type information
    /// </summary>
    public static class AOVTypeInfo
    {
        public class AOVInfo
        {
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public bool RequiresHDRP { get; set; }
            public AOVOutputFormat RecommendedFormat { get; set; }
        }
        
        private static readonly Dictionary<AOVType, AOVInfo> aovInfoMap = new Dictionary<AOVType, AOVInfo>
        {
            { AOVType.Depth, new AOVInfo 
                { 
                    DisplayName = "Depth", 
                    Description = "Camera depth buffer (linear depth)", 
                    Category = "Geometry",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR32
                }
            },
            { AOVType.DepthNormalized, new AOVInfo 
                { 
                    DisplayName = "Depth (Normalized)", 
                    Description = "Normalized depth buffer (0-1 range)", 
                    Category = "Geometry",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Normal, new AOVInfo 
                { 
                    DisplayName = "World Normal", 
                    Description = "World space normal vectors", 
                    Category = "Geometry",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.MotionVectors, new AOVInfo 
                { 
                    DisplayName = "Motion Vectors", 
                    Description = "Per-pixel motion vectors for motion blur", 
                    Category = "Geometry",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Albedo, new AOVInfo 
                { 
                    DisplayName = "Albedo", 
                    Description = "Base color without lighting", 
                    Category = "Material",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Specular, new AOVInfo 
                { 
                    DisplayName = "Specular", 
                    Description = "Specular reflection component", 
                    Category = "Material",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Smoothness, new AOVInfo 
                { 
                    DisplayName = "Smoothness", 
                    Description = "Surface smoothness/roughness", 
                    Category = "Material",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.AmbientOcclusion, new AOVInfo 
                { 
                    DisplayName = "Ambient Occlusion", 
                    Description = "Ambient occlusion pass", 
                    Category = "Material",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Metal, new AOVInfo 
                { 
                    DisplayName = "Metallic", 
                    Description = "Metallic surface property", 
                    Category = "Material",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.DirectDiffuse, new AOVInfo 
                { 
                    DisplayName = "Direct Diffuse", 
                    Description = "Direct lighting diffuse component", 
                    Category = "Lighting",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.DirectSpecular, new AOVInfo 
                { 
                    DisplayName = "Direct Specular", 
                    Description = "Direct lighting specular component", 
                    Category = "Lighting",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.IndirectDiffuse, new AOVInfo 
                { 
                    DisplayName = "Indirect Diffuse", 
                    Description = "Indirect lighting diffuse (GI)", 
                    Category = "Lighting",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.IndirectSpecular, new AOVInfo 
                { 
                    DisplayName = "Indirect Specular", 
                    Description = "Indirect lighting specular (reflections)", 
                    Category = "Lighting",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Emissive, new AOVInfo 
                { 
                    DisplayName = "Emissive", 
                    Description = "Emissive lighting component", 
                    Category = "Lighting",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.Shadow, new AOVInfo 
                { 
                    DisplayName = "Shadows", 
                    Description = "Shadow mask", 
                    Category = "Additional",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.ContactShadows, new AOVInfo 
                { 
                    DisplayName = "Contact Shadows", 
                    Description = "Screen space contact shadows", 
                    Category = "Additional",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.ScreenSpaceReflection, new AOVInfo 
                { 
                    DisplayName = "SSR", 
                    Description = "Screen space reflections", 
                    Category = "Additional",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            },
            { AOVType.CustomPass, new AOVInfo 
                { 
                    DisplayName = "Custom Pass", 
                    Description = "User-defined custom render pass", 
                    Category = "Custom",
                    RequiresHDRP = true,
                    RecommendedFormat = AOVOutputFormat.EXR16
                }
            }
        };
        
        /// <summary>
        /// Get display information for an AOV type
        /// </summary>
        public static AOVInfo GetInfo(AOVType type)
        {
            return aovInfoMap.ContainsKey(type) ? aovInfoMap[type] : null;
        }
        
        /// <summary>
        /// Get all AOV types grouped by category
        /// </summary>
        public static Dictionary<string, List<AOVType>> GetAOVsByCategory()
        {
            var result = new Dictionary<string, List<AOVType>>();
            
            foreach (var kvp in aovInfoMap)
            {
                var category = kvp.Value.Category;
                if (!result.ContainsKey(category))
                {
                    result[category] = new List<AOVType>();
                }
                result[category].Add(kvp.Key);
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if Unity has HDRP package installed
        /// </summary>
        public static bool IsHDRPAvailable()
        {
            // Unity 6では、プリプロセッサディレクティブが異なる場合があるため、
            // より確実な方法でHDRPの存在を確認
            
            #if UNITY_PIPELINE_HDRP || UNITY_HDRP
            return true;
            #else
            // HDRPアセンブリの存在を確認
            var hdrpType = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipeline, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdrpType != null)
            {
                return true;
            }
            
            // GraphicsSettingsでの確認
            var graphicsSettings = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (graphicsSettings != null && graphicsSettings.GetType().FullName.Contains("HighDefinition"))
            {
                return true;
            }
            
            return false;
            #endif
        }
    }
}