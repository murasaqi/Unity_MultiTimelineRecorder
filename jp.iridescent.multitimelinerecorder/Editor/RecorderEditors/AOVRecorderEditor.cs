using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Unity.MultiTimelineRecorder.RecorderEditors
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
            // Input section
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Source (fixed to Tagged Camera for AOV)
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", "Tagged Camera");
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
            
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
            
            // Camera section
            EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Camera selection dropdown
            host.cameraTag = EditorGUILayout.TagField("Camera", host.cameraTag ?? "MainCamera");
            
            EditorGUI.indentLevel--;
            
            // Call base to handle resolution settings
            base.DrawInputSettings();
            
            // Frame Rate
            EditorGUILayout.Space(5);
            host.frameRate = EditorGUILayout.IntField("Frame Rate", host.frameRate);
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // AOV Type selection
            EditorGUILayout.LabelField("Arbitrary Output Variables (AOVs)", EditorStyles.boldLabel);
            
            // Following Unity Recorder's AOV category structure
            var beautyType = new[] { AOVType.Beauty };
            var materialTypes = new[] { AOVType.Albedo, AOVType.Alpha, AOVType.Metal, 
                                       AOVType.Smoothness, AOVType.Specular };
            var lightingTypes = new[] { AOVType.DirectDiffuse, AOVType.DirectSpecular, 
                                       AOVType.Emissive, AOVType.IndirectDiffuse, 
                                       AOVType.Reflection, AOVType.Refraction };
            var utilityTypes = new[] { AOVType.AmbientOcclusion, AOVType.Depth, AOVType.DepthNormalized,
                                      AOVType.MotionVectors, AOVType.Normal };
            
            EditorGUILayout.Space(5);
            
            // Draw categories with Unity Recorder style
            DrawAOVCategoryWithToggle("Beauty", beautyType);
            DrawAOVCategoryWithToggle("Material Properties", materialTypes);
            DrawAOVCategoryWithToggle("Lighting", lightingTypes);
            DrawAOVCategoryWithToggle("Utility", utilityTypes);
            
            // Multi-part EXR option (similar to Unity Recorder)
            EditorGUILayout.Space(10);
            if (host.aovOutputFormat == AOVOutputFormat.EXR16 || host.aovOutputFormat == AOVOutputFormat.EXR32)
            {
                host.useMultiPartEXR = EditorGUILayout.Toggle("Multi-part file", host.useMultiPartEXR);
                if (host.useMultiPartEXR)
                {
                    EditorGUILayout.HelpBox("All selected AOVs will be saved in a single multi-part EXR file", MessageType.Info);
                }
            }
            
            // Output format
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Output Format", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Format dropdown
            host.aovOutputFormat = (AOVOutputFormat)EditorGUILayout.EnumPopup("Format", host.aovOutputFormat);
            
            // Color space (for non-EXR formats)
            if (host.aovOutputFormat != AOVOutputFormat.EXR16 && host.aovOutputFormat != AOVOutputFormat.EXR32)
            {
                host.aovColorSpace = (AOVColorSpace)EditorGUILayout.EnumPopup("Color Space", host.aovColorSpace);
                EditorGUILayout.HelpBox("EXR format is recommended for AOV outputs to preserve full dynamic range", MessageType.Info);
            }
            
            // Compression settings for EXR
            if (host.aovOutputFormat == AOVOutputFormat.EXR16 || host.aovOutputFormat == AOVOutputFormat.EXR32)
            {
                host.aovCompression = (AOVCompression)EditorGUILayout.EnumPopup("Compression", host.aovCompression);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawAOVCategoryWithToggle(string categoryName, AOVType[] types)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Calculate if all, none, or some are selected (for mixed state)
            int selectedCount = 0;
            foreach (var type in types)
            {
                if ((host.selectedAOVTypes & type) != 0)
                    selectedCount++;
            }
            
            bool allSelected = selectedCount == types.Length;
            bool noneSelected = selectedCount == 0;
            bool mixedSelection = !allSelected && !noneSelected;
            
            // Draw category toggle with mixed state support
            EditorGUI.showMixedValue = mixedSelection;
            bool categoryToggle = EditorGUILayout.Toggle(allSelected, GUILayout.Width(15));
            EditorGUI.showMixedValue = false;
            
            // Category label
            EditorGUILayout.LabelField(categoryName, EditorStyles.boldLabel);
            
            EditorGUILayout.EndHorizontal();
            
            // Handle category toggle change
            if (categoryToggle != allSelected && !mixedSelection)
            {
                foreach (var type in types)
                {
                    if (categoryToggle)
                        host.selectedAOVTypes |= type;
                    else
                        host.selectedAOVTypes &= ~type;
                }
            }
            else if (mixedSelection && categoryToggle)
            {
                // When clicking mixed state, select all
                foreach (var type in types)
                {
                    host.selectedAOVTypes |= type;
                }
            }
            
            // Draw individual AOV toggles
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
                
                // Create proper AOV display name
                string displayName = GetAOVDisplayName(type);
                
                bool newSelected = EditorGUILayout.ToggleLeft(
                    displayName, 
                    isSelected, 
                    GUILayout.Width(180)
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
            
            // Fill remaining columns with empty space
            while (currentColumn < columns)
            {
                GUILayout.Label("", GUILayout.Width(180));
                currentColumn++;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        private string GetAOVDisplayName(AOVType type)
        {
            return type switch
            {
                AOVType.DepthNormalized => "Depth (Normalized)",
                AOVType.DirectDiffuse => "Direct Diffuse",
                AOVType.DirectSpecular => "Direct Specular",
                AOVType.IndirectDiffuse => "Indirect Diffuse",
                AOVType.IndirectSpecular => "Indirect Specular",
                AOVType.MotionVectors => "Motion Vectors",
                AOVType.AmbientOcclusion => "Ambient Occlusion",
                AOVType.ContactShadows => "Contact Shadows",
                _ => ObjectNames.NicifyVariableName(type.ToString())
            };
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
        
        private void ApplyResolutionPreset(OutputResolution preset)
        {
            switch (preset)
            {
                case OutputResolution.HD720p:
                    host.width = 1280;
                    host.height = 720;
                    break;
                case OutputResolution.HD1080p:
                    host.width = 1920;
                    host.height = 1080;
                    break;
                case OutputResolution.UHD4K:
                    host.width = 3840;
                    host.height = 2160;
                    break;
                case OutputResolution.UHD8K:
                    host.width = 7680;
                    host.height = 4320;
                    break;
            }
        }
    }
}