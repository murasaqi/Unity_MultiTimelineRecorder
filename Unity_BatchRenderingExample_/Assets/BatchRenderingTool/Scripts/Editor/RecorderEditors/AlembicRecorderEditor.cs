using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Editor for Alembic Recorder settings following Unity Recorder's standard UI
    /// </summary>
    public class AlembicRecorderEditor : RecorderSettingsEditorBase
    {
        public AlembicRecorderEditor(IRecorderSettingsHost host)
        {
            this.host = host;
        }
        
        protected override void DrawInputSettings()
        {
            base.DrawInputSettings();
            
            // Alembic package check
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                EditorGUILayout.HelpBox("Alembic package is not installed. Please install it via Package Manager.", MessageType.Error);
            }
            
            // Export scope
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Export Scope", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            host.alembicExportScope = (AlembicExportScope)EditorGUILayout.EnumPopup("Scope", host.alembicExportScope);
            
            if (host.alembicExportScope == AlembicExportScope.TargetGameObject)
            {
                host.alembicTargetGameObject = (GameObject)EditorGUILayout.ObjectField(
                    "Target GameObject", 
                    host.alembicTargetGameObject, 
                    typeof(GameObject), 
                    true
                );
                
                if (host.alembicTargetGameObject == null)
                {
                    EditorGUILayout.HelpBox("Please select a GameObject to export", MessageType.Warning);
                }
            }
            
            // Preset selection
            EditorGUILayout.Space(5);
            host.useAlembicPreset = EditorGUILayout.Toggle("Use Preset", host.useAlembicPreset);
            
            if (host.useAlembicPreset)
            {
                host.alembicPreset = (AlembicExportPreset)EditorGUILayout.EnumPopup("Preset", host.alembicPreset);
                
                if (host.alembicPreset != AlembicExportPreset.Custom)
                {
                    var config = AlembicRecorderSettingsConfig.GetPreset(host.alembicPreset);
                    
                    // Apply preset values
                    host.alembicExportTargets = config.exportTargets;
                    host.alembicFrameRate = config.frameRate;
                    host.alembicTimeSamplingType = (AlembicTimeSamplingType)config.timeSamplingMode;
                    host.alembicWorldScale = config.scaleFactor;
                    host.alembicHandedness = config.handedness;
                    host.alembicFlattenHierarchy = config.flattenHierarchy;
                    
                    // Show preset info
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.FloatField("Frame Rate", config.frameRate);
                        EditorGUILayout.EnumPopup("Time Sampling", config.timeSamplingMode);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // Export targets
            EditorGUILayout.LabelField("Export Targets", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            // Individual target toggles
            var targets = System.Enum.GetValues(typeof(AlembicExportTargets)).Cast<AlembicExportTargets>()
                .Where(t => t != AlembicExportTargets.None);
            
            foreach (var target in targets)
            {
                bool isSelected = (host.alembicExportTargets & target) != 0;
                bool newSelected = EditorGUILayout.Toggle(
                    ObjectNames.NicifyVariableName(target.ToString()), 
                    isSelected
                );
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        host.alembicExportTargets |= target;
                    else
                        host.alembicExportTargets &= ~target;
                }
            }
            
            EditorGUI.indentLevel--;
            
            // Time sampling settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Time Sampling", EditorStyles.boldLabel);
            
            if (!host.useAlembicPreset || host.alembicPreset == AlembicExportPreset.Custom)
            {
                host.alembicFrameRate = EditorGUILayout.FloatField("Frame Rate", host.alembicFrameRate);
                host.alembicTimeSamplingType = (AlembicTimeSamplingType)EditorGUILayout.EnumPopup(
                    "Sampling Type", 
                    host.alembicTimeSamplingType
                );
                
                if (host.alembicTimeSamplingType == AlembicTimeSamplingType.Uniform)
                {
                    EditorGUILayout.HelpBox("Uniform sampling exports at regular intervals", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Acyclic sampling exports only when changes occur", MessageType.Info);
                }
            }
            
            // Transform settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Transform Settings", EditorStyles.boldLabel);
            
            if (!host.useAlembicPreset || host.alembicPreset == AlembicExportPreset.Custom)
            {
                host.alembicWorldScale = EditorGUILayout.FloatField("World Scale", host.alembicWorldScale);
                host.alembicHandedness = (AlembicHandedness)EditorGUILayout.EnumPopup("Handedness", host.alembicHandedness);
                
                if (host.alembicHandedness == AlembicHandedness.Left)
                {
                    EditorGUILayout.HelpBox("Left-handed coordinate system (Unity default)", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Right-handed coordinate system (Maya, Houdini default)", MessageType.None);
                }
            }
            
            // Hierarchy settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);
            
            host.alembicIncludeChildren = EditorGUILayout.Toggle("Include Children", host.alembicIncludeChildren);
            host.alembicFlattenHierarchy = EditorGUILayout.Toggle("Flatten Hierarchy", host.alembicFlattenHierarchy);
            
            if (host.alembicFlattenHierarchy)
            {
                EditorGUILayout.HelpBox("Hierarchy will be flattened to a single level", MessageType.Info);
            }
        }
        
        protected override string GetFileExtension()
        {
            return "abc";
        }
        
        protected override string GetRecorderName()
        {
            return "Alembic";
        }
        
        protected override string GetTargetGameObjectName()
        {
            return host.alembicTargetGameObject != null ? host.alembicTargetGameObject.name : null;
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                errorMessage = "Alembic package is not installed";
                return false;
            }
            
            if (host.alembicExportTargets == AlembicExportTargets.None)
            {
                errorMessage = "At least one export target must be selected";
                return false;
            }
            
            if (host.alembicExportScope == AlembicExportScope.TargetGameObject && 
                host.alembicTargetGameObject == null)
            {
                errorMessage = "Target GameObject must be specified";
                return false;
            }
            
            if (host.alembicFrameRate <= 0)
            {
                errorMessage = "Frame rate must be greater than 0";
                return false;
            }
            
            if (host.alembicWorldScale <= 0)
            {
                errorMessage = "World scale must be greater than 0";
                return false;
            }
            
            if (string.IsNullOrEmpty(host.fileName))
            {
                errorMessage = "File name cannot be empty";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        protected override RecorderSettingsType GetRecorderType()
        {
            return RecorderSettingsType.Alembic;
        }
    }
}