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
        
        protected override void DrawInputSettings()
        {
            // Source label (matches Unity Recorder UI)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source", GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.LabelField("GameObject", EditorStyles.label);
            EditorGUILayout.EndHorizontal();
            
            // GameObject selection with highlighting when null
            if (host.fbxTargetGameObject == null)
            {
                EditorGUILayout.HelpBox("FBX Recorder requires a Target GameObject. Please select the GameObject to record animations from.", MessageType.Error);
            }
            
            var previousColor = GUI.backgroundColor;
            if (host.fbxTargetGameObject == null)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Light red for missing GameObject
            }
            
            host.fbxTargetGameObject = (GameObject)EditorGUILayout.ObjectField(
                "GameObject", 
                host.fbxTargetGameObject, 
                typeof(GameObject), 
                true);
            
            GUI.backgroundColor = previousColor;
            
            // Recorded Components dropdown
            host.fbxRecordedComponent = (FBXRecordedComponent)EditorGUILayout.EnumPopup(
                "Recorded Components", 
                host.fbxRecordedComponent);
            
            // Record Hierarchy checkbox
            host.fbxRecordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", host.fbxRecordHierarchy);
            
            // Clamped Tangents checkbox
            host.fbxClampedTangents = EditorGUILayout.Toggle("Clamped Tangents", host.fbxClampedTangents);
            
            // Animation Compression dropdown
            host.fbxAnimationCompression = (FBXAnimationCompressionLevel)EditorGUILayout.EnumPopup(
                "Anim. Compression", 
                host.fbxAnimationCompression);
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // Format label (matches Unity Recorder UI)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format", GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.LabelField("FBX", EditorStyles.label);
            EditorGUILayout.EndHorizontal();
            
            // Export Geometry checkbox
            host.fbxExportGeometry = EditorGUILayout.Toggle("Export Geometry", host.fbxExportGeometry);
            
            // Transfer Animation section with indentation
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Transfer Animation");
            
            EditorGUI.indentLevel++;
            
            host.fbxTransferAnimationSource = (Transform)EditorGUILayout.ObjectField(
                "Source", 
                host.fbxTransferAnimationSource, 
                typeof(Transform), 
                true);
            
            host.fbxTransferAnimationDest = (Transform)EditorGUILayout.ObjectField(
                "Destination", 
                host.fbxTransferAnimationDest, 
                typeof(Transform), 
                true);
            
            EditorGUI.indentLevel--;
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (!base.ValidateSettings(out errorMessage))
                return false;
            
            // FBX-specific validation
            if (host.fbxTargetGameObject == null)
            {
                errorMessage = "FBX Recorder Error: No Target GameObject selected.\n\n" +
                               "Please select a GameObject to record animations from.\n" +
                               "The selected GameObject's Transform or Camera component will be recorded.";
                return false;
            }
            
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
            // Return the target GameObject name
            if (host.fbxTargetGameObject != null)
            {
                return host.fbxTargetGameObject.name;
            }
            // Fall back to animation transfer destination if set
            if (host.fbxTransferAnimationDest != null)
            {
                return host.fbxTransferAnimationDest.name;
            }
            return base.GetTargetGameObjectName();
        }
        
        protected override RecorderSettingsType GetRecorderType()
        {
            return RecorderSettingsType.FBX;
        }
    }
}