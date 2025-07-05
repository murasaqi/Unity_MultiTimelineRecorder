using UnityEngine;
using UnityEditor;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Editor UI for FBX Recorder settings
    /// </summary>
    public class FBXRecorderEditor : RecorderSettingsEditorBase
    {
        public FBXRecorderEditor(IRecorderSettingsHost host)
        {
            this.host = host;
        }
        
        public override void DrawRecorderSettings()
        {
            EditorGUILayout.LabelField("FBX Recorder Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Preset selection
            EditorGUILayout.Space(5);
            host.useFBXPreset = EditorGUILayout.Toggle("Use Preset", host.useFBXPreset);
            
            if (host.useFBXPreset)
            {
                host.fbxPreset = (FBXExportPreset)EditorGUILayout.EnumPopup("Preset", host.fbxPreset);
                
                if (host.fbxPreset != FBXExportPreset.Custom)
                {
                    EditorGUILayout.HelpBox(GetPresetDescription(host.fbxPreset), MessageType.Info);
                }
            }
            
            EditorGUILayout.Space(5);
            
            // Custom settings (always show, but disable if using preset)
            using (new EditorGUI.DisabledScope(host.useFBXPreset && host.fbxPreset != FBXExportPreset.Custom))
            {
                // Export options
                EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
                
                host.fbxExportGeometry = EditorGUILayout.Toggle("Export Geometry", host.fbxExportGeometry);
                
                EditorGUILayout.Space(5);
                
                // Animation transfer options
                EditorGUILayout.LabelField("Animation Transfer (Optional)", EditorStyles.boldLabel);
                
                host.fbxTransferAnimationSource = (Transform)EditorGUILayout.ObjectField(
                    "Source Transform", 
                    host.fbxTransferAnimationSource, 
                    typeof(Transform), 
                    true);
                
                host.fbxTransferAnimationDest = (Transform)EditorGUILayout.ObjectField(
                    "Destination Transform", 
                    host.fbxTransferAnimationDest, 
                    typeof(Transform), 
                    true);
                
                if (host.fbxTransferAnimationSource != null || host.fbxTransferAnimationDest != null)
                {
                    EditorGUILayout.HelpBox(
                        "Animation transfer allows you to retarget animation from one transform hierarchy to another. " +
                        "Both source and destination must be set for this feature to work.", 
                        MessageType.Info);
                }
            }
            
            EditorGUILayout.Space(5);
            
            // Output resolution
            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            host.width = EditorGUILayout.IntField("Width", host.width);
            host.height = EditorGUILayout.IntField("Height", host.height);
            
            EditorGUILayout.Space(5);
            
            // Frame rate
            host.frameRate = EditorGUILayout.IntField("Frame Rate", host.frameRate);
            
            EditorGUILayout.EndVertical();
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (!base.ValidateSettings(out errorMessage))
                return false;
            
            // FBX-specific validation
            if (host.fbxTransferAnimationSource != null && host.fbxTransferAnimationDest == null)
            {
                errorMessage = "Animation destination transform must be set when source is specified";
                return false;
            }
            
            if (host.fbxTransferAnimationSource == null && host.fbxTransferAnimationDest != null)
            {
                errorMessage = "Animation source transform must be set when destination is specified";
                return false;
            }
            
            if (host.fbxTransferAnimationSource != null && host.fbxTransferAnimationDest != null)
            {
                if (host.fbxTransferAnimationSource == host.fbxTransferAnimationDest)
                {
                    errorMessage = "Animation source and destination cannot be the same transform";
                    return false;
                }
            }
            
            return true;
        }
        
        private string GetPresetDescription(FBXExportPreset preset)
        {
            switch (preset)
            {
                case FBXExportPreset.AnimationExport:
                    return "Export animation data only (no geometry)";
                case FBXExportPreset.ModelExport:
                    return "Export geometry/models only (with animation if present)";
                case FBXExportPreset.ModelAndAnimation:
                    return "Export both geometry and animation data";
                default:
                    return "Custom settings";
            }
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // FBX doesn't have multiple output formats, but we show export settings here
            EditorGUILayout.LabelField("Format", "FBX Binary (.fbx)");
            
            EditorGUILayout.Space(5);
            
            // Show current export settings summary
            if (host.fbxExportGeometry)
            {
                EditorGUILayout.LabelField("Export Type", "Geometry + Animation");
            }
            else
            {
                EditorGUILayout.LabelField("Export Type", "Animation Only");
            }
            
            if (host.fbxTransferAnimationSource != null && host.fbxTransferAnimationDest != null)
            {
                EditorGUILayout.LabelField("Animation Transfer", "Enabled");
            }
        }
        
        protected override string GetFileExtension()
        {
            return "fbx";
        }
        
        protected override string GetRecorderName()
        {
            return "FBX";
        }
        
        protected override string GetTargetGameObjectName()
        {
            // Return the animation transfer destination if set
            if (host.fbxTransferAnimationDest != null)
            {
                return host.fbxTransferAnimationDest.name;
            }
            return base.GetTargetGameObjectName();
        }
    }
}