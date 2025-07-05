using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Editor for Animation Recorder settings following Unity Recorder's standard UI
    /// </summary>
    public class AnimationRecorderEditor : RecorderSettingsEditorBase
    {
        public AnimationRecorderEditor(IRecorderSettingsHost host)
        {
            this.host = host;
        }
        
        protected override void DrawInputSettings()
        {
            base.DrawInputSettings();
            
            // Target GameObject selection
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Recording Target", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            host.animationTargetGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Target GameObject", 
                host.animationTargetGameObject, 
                typeof(GameObject), 
                true
            );
            
            if (host.animationTargetGameObject == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject to record its animation", MessageType.Warning);
            }
            else
            {
                // Show target info
                var animator = host.animationTargetGameObject.GetComponent<Animator>();
                if (animator != null)
                {
                    EditorGUILayout.HelpBox($"Animator found: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "No controller")}", MessageType.Info);
                }
                
                // Preset selection
                EditorGUILayout.Space(5);
                host.useAnimationPreset = EditorGUILayout.Toggle("Use Preset", host.useAnimationPreset);
                
                if (host.useAnimationPreset)
                {
                    host.animationPreset = (AnimationExportPreset)EditorGUILayout.EnumPopup("Preset", host.animationPreset);
                    
                    if (host.animationPreset != AnimationExportPreset.Custom)
                    {
                        var config = AnimationRecorderSettingsConfig.GetPreset(host.animationPreset);
                        
                        // Apply preset values
                        host.animationRecordingScope = config.recordingScope;
                        
                        // Show preset info
                        EditorGUI.indentLevel++;
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.EnumPopup("Recording Scope", config.recordingScope);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // Recording options
            EditorGUILayout.LabelField("Recording Options", EditorStyles.boldLabel);
            
            if (!host.useAnimationPreset || host.animationPreset == AnimationExportPreset.Custom)
            {
                host.animationRecordingScope = (AnimationRecordingScope)EditorGUILayout.EnumPopup(
                    "Recording Scope", 
                    host.animationRecordingScope
                );
                
                // Scope-specific help
                switch (host.animationRecordingScope)
                {
                    case AnimationRecordingScope.SingleGameObject:
                        EditorGUILayout.HelpBox("Records the selected GameObject only", MessageType.Info);
                        break;
                    case AnimationRecordingScope.GameObjectAndChildren:
                        EditorGUILayout.HelpBox("Records the selected GameObject and all its children", MessageType.Info);
                        break;
                    case AnimationRecordingScope.SelectedHierarchy:
                        EditorGUILayout.HelpBox("Records the currently selected hierarchy in the scene", MessageType.Info);
                        break;
                    case AnimationRecordingScope.CustomSelection:
                        EditorGUILayout.HelpBox("Records a custom selection of GameObjects", MessageType.Info);
                        break;
                }
                
                EditorGUILayout.Space(5);
                host.animationIncludeChildren = EditorGUILayout.Toggle("Include Children", host.animationIncludeChildren);
                
                if (host.animationIncludeChildren)
                {
                    EditorGUILayout.HelpBox("All child GameObjects will be included in the animation", MessageType.None);
                }
            }
            
            // Advanced settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            
            host.animationClampedTangents = EditorGUILayout.Toggle("Clamped Tangents", host.animationClampedTangents);
            
            // Always show blend shapes option
            host.animationRecordBlendShapes = EditorGUILayout.Toggle("Record Blend Shapes", host.animationRecordBlendShapes);
            
            if (host.animationRecordBlendShapes && host.animationTargetGameObject != null)
                {
                    var skinnedMeshRenderers = host.animationTargetGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                    int blendShapeCount = 0;
                    foreach (var smr in skinnedMeshRenderers)
                    {
                        if (smr.sharedMesh != null)
                        {
                            blendShapeCount += smr.sharedMesh.blendShapeCount;
                        }
                    }
                    
                    if (blendShapeCount > 0)
                    {
                        EditorGUILayout.HelpBox($"Found {blendShapeCount} blend shapes to record", MessageType.Info);
                    }
                }
            
            // Compression settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
            
            // Note: The interface doesn't have animationCompression field, it uses compression settings via error tolerances
            EditorGUILayout.HelpBox("Compression is controlled by error tolerance values below", MessageType.Info);
            
            // Always show error tolerance fields as they control compression
            {
                host.animationPositionError = EditorGUILayout.FloatField("Position Error", host.animationPositionError);
                host.animationRotationError = EditorGUILayout.FloatField("Rotation Error", host.animationRotationError);
                host.animationScaleError = EditorGUILayout.FloatField("Scale Error", host.animationScaleError);
            }
        }
        
        protected override void DrawOutputFileSettings()
        {
            // Animation clips must be saved in Assets folder
            EditorGUILayout.HelpBox("Animation clips must be saved within the Assets folder", MessageType.Info);
            
            // Override base implementation for animations
            base.DrawOutputFileSettings();
            
            // Ensure path starts with Assets/
            if (!string.IsNullOrEmpty(host.filePath) && !host.filePath.StartsWith("Assets"))
            {
                host.filePath = "Assets/Animations";
            }
        }
        
        protected override string GetFileExtension()
        {
            return "anim";
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (host.animationTargetGameObject == null)
            {
                errorMessage = "Target GameObject must be specified";
                return false;
            }
            
            if (string.IsNullOrEmpty(host.fileName))
            {
                errorMessage = "File name cannot be empty";
                return false;
            }
            
            if (!host.fileName.StartsWith("Assets/"))
            {
                errorMessage = "Animation clips must be saved in the Assets folder";
                return false;
            }
            
            // Check if recording scope requires GameObject validation
            if (host.animationRecordingScope == AnimationRecordingScope.SingleGameObject || 
                host.animationRecordingScope == AnimationRecordingScope.GameObjectAndChildren)
            {
                var animator = host.animationTargetGameObject.GetComponent<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    errorMessage = "Humanoid recording requires a GameObject with a humanoid Animator";
                    return false;
                }
            }
            
            errorMessage = null;
            return true;
        }
    }
}