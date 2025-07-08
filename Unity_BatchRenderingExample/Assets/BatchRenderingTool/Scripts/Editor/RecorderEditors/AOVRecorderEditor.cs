using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Editor for AOV Recorder settings following Unity Recorder's standard UI
    /// </summary>
    public class AOVRecorderEditor : RecorderSettingsEditorBase
    {
        private Vector2 aovScrollPosition;
        
        public AOVRecorderEditor(IRecorderSettingsHost host)
        {
            this.host = host;
        }
        
        protected override void DrawInputSettings()
        {
            base.DrawInputSettings();
            
            // HDRP check
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                EditorGUILayout.HelpBox("AOV Recording requires HDRP (High Definition Render Pipeline)", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "AOV Recording (Experimental): Unity Recorder 5.1.2 does not have native AOV support. " +
                    "This tool provides a fallback implementation using ImageRecorderSettings. " +
                    "For full AOV functionality, consider using HDRP Custom Pass with render targets.", 
                    MessageType.Warning);
            }
            
            // Resolution settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            // Preset selection
            host.useAOVPreset = EditorGUILayout.Toggle("Use Preset", host.useAOVPreset);
            
            if (host.useAOVPreset)
            {
                host.aovPreset = (AOVPreset)EditorGUILayout.EnumPopup("Preset", host.aovPreset);
                
                if (host.aovPreset != AOVPreset.Custom)
                {
                    var config = GetPresetConfig(host.aovPreset);
                    
                    // Apply preset values
                    host.selectedAOVTypes = config.selectedAOVs;
                    host.aovOutputFormat = config.outputFormat;
                    host.width = config.width;
                    host.height = config.height;
                    
                    // Show preset info
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.IntField("Width", config.width);
                        EditorGUILayout.IntField("Height", config.height);
                        EditorGUILayout.EnumPopup("Format", config.outputFormat);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            if (!host.useAOVPreset || host.aovPreset == AOVPreset.Custom)
            {
                host.width = EditorGUILayout.IntField("Width", host.width);
                host.height = EditorGUILayout.IntField("Height", host.height);
                
                // Resolution presets
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("HD", GUILayout.Width(40)))
                {
                    host.width = 1920;
                    host.height = 1080;
                }
                if (GUILayout.Button("2K", GUILayout.Width(40)))
                {
                    host.width = 2048;
                    host.height = 1080;
                }
                if (GUILayout.Button("4K", GUILayout.Width(40)))
                {
                    host.width = 3840;
                    host.height = 2160;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // AOV Type selection
            EditorGUILayout.LabelField("AOV Types", EditorStyles.boldLabel);
            
            // Quick select buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                var allTypes = System.Enum.GetValues(typeof(AOVType)).Cast<AOVType>()
                    .Where(t => t != AOVType.None);
                host.selectedAOVTypes = AOVType.None;
                foreach (var type in allTypes)
                {
                    host.selectedAOVTypes |= type;
                }
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                host.selectedAOVTypes = AOVType.None;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // AOV type grid
            var aovTypes = System.Enum.GetValues(typeof(AOVType)).Cast<AOVType>()
                .Where(t => t != AOVType.None).ToList();
            
            // Group AOV types by category
            var geometryTypes = new[] { AOVType.Depth, AOVType.DepthNormalized, AOVType.Normal, AOVType.MotionVectors };
            var lightingTypes = new[] { AOVType.DirectDiffuse, AOVType.DirectSpecular, 
                                       AOVType.IndirectDiffuse, AOVType.IndirectSpecular, 
                                       AOVType.Emissive, AOVType.Shadow, AOVType.ContactShadows };
            var materialTypes = new[] { AOVType.Albedo, AOVType.Specular, AOVType.Smoothness, 
                                       AOVType.AmbientOcclusion, AOVType.Metal };
            
            EditorGUILayout.Space(5);
            
            // Draw categories
            DrawAOVCategory("Geometry", geometryTypes);
            DrawAOVCategory("Lighting", lightingTypes);
            DrawAOVCategory("Material Properties", materialTypes);
            
            // Output format
            EditorGUILayout.Space(10);
            host.aovOutputFormat = (AOVOutputFormat)EditorGUILayout.EnumPopup("Output Format", host.aovOutputFormat);
            
            if (host.aovOutputFormat != AOVOutputFormat.EXR16 && host.aovOutputFormat != AOVOutputFormat.EXR32)
            {
                EditorGUILayout.HelpBox("EXR format is recommended for AOV outputs to preserve full dynamic range", MessageType.Info);
            }
        }
        
        private void DrawAOVCategory(string categoryName, AOVType[] types)
        {
            EditorGUILayout.LabelField(categoryName, EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            
            int columns = 2;
            int currentColumn = 0;
            
            EditorGUILayout.BeginHorizontal();
            foreach (var type in types)
            {
                if (currentColumn >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentColumn = 0;
                }
                
                bool isSelected = (host.selectedAOVTypes & type) != 0;
                bool newSelected = EditorGUILayout.ToggleLeft(
                    ObjectNames.NicifyVariableName(type.ToString()), 
                    isSelected, 
                    GUILayout.Width(150)
                );
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        host.selectedAOVTypes |= type;
                    else
                        host.selectedAOVTypes &= ~type;
                }
                
                currentColumn++;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        protected override string GetFileExtension()
        {
            return host.aovOutputFormat switch
            {
                AOVOutputFormat.PNG => "png",
                AOVOutputFormat.PNG16 => "png",
                AOVOutputFormat.EXR16 => "exr",
                AOVOutputFormat.EXR32 => "exr",
                AOVOutputFormat.TGA => "tga",
                AOVOutputFormat.JPEG => "jpg",
                _ => "exr"
            };
        }
        
        protected override string GetRecorderName()
        {
            return "AOV";
        }
        
        private AOVRecorderSettingsConfig GetPresetConfig(AOVPreset preset)
        {
            return preset switch
            {
                AOVPreset.Compositing => AOVRecorderSettingsConfig.Presets.GetCompositing(),
                AOVPreset.GeometryOnly => AOVRecorderSettingsConfig.Presets.GetGeometryOnly(),
                AOVPreset.LightingOnly => AOVRecorderSettingsConfig.Presets.GetLightingOnly(),
                AOVPreset.MaterialProperties => AOVRecorderSettingsConfig.Presets.GetMaterialProperties(),
                _ => AOVRecorderSettingsConfig.Presets.GetCompositing()
            };
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (host.width <= 0 || host.height <= 0)
            {
                errorMessage = "Width and height must be greater than 0";
                return false;
            }
            
            if (host.selectedAOVTypes == AOVType.None)
            {
                errorMessage = "At least one AOV type must be selected";
                return false;
            }
            
            if (string.IsNullOrEmpty(host.fileName))
            {
                errorMessage = "File name cannot be empty";
                return false;
            }
            
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                errorMessage = "AOV Recording requires HDRP";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        protected override RecorderSettingsType GetRecorderType()
        {
            return RecorderSettingsType.AOV;
        }
    }
}