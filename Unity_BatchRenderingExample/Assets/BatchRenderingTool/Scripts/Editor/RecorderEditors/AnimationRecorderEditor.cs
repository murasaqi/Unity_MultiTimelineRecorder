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
                    host.animationPreset = (AnimationRecordingPreset)EditorGUILayout.EnumPopup("Preset", host.animationPreset);
                    
                    if (host.animationPreset != AnimationRecordingPreset.Custom)
                    {
                        var config = AnimationRecorderSettingsConfig.GetPreset(host.animationPreset);
                        
                        // Apply preset values
                        host.animationRecordingType = config.recordingType;
                        host.animationIncludeChildren = config.includeChildren;
                        host.animationClampedTangents = config.clampedTangents;
                        host.animationRecordBlendShapes = config.recordBlendShapes;
                        
                        // Show preset info
                        EditorGUI.indentLevel++;
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.EnumPopup("Recording Type", config.recordingType);
                            EditorGUILayout.Toggle("Include Children", config.includeChildren);
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
            
            if (!host.useAnimationPreset || host.animationPreset == AnimationRecordingPreset.Custom)
            {
                host.animationRecordingType = (AnimationRecordingType)EditorGUILayout.EnumPopup(
                    "Recording Type", 
                    host.animationRecordingType
                );
                
                // Type-specific help
                switch (host.animationRecordingType)
                {
                    case AnimationRecordingType.TransformOnly:
                        EditorGUILayout.HelpBox("Records position, rotation, and scale only", MessageType.Info);
                        break;
                    case AnimationRecordingType.AllProperties:
                        EditorGUILayout.HelpBox("Records all animated properties including materials and components", MessageType.Info);
                        break;
                    case AnimationRecordingType.HumanoidRig:
                        EditorGUILayout.HelpBox("Records humanoid rig animation for retargeting", MessageType.Info);
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
            
            if (host.animationRecordingType == AnimationRecordingType.AllProperties)
            {
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
            }
            
            // Compression settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
            
            host.animationCompression = (AnimationCompression)EditorGUILayout.EnumPopup(
                "Compression", 
                host.animationCompression
            );
            
            if (host.animationCompression != AnimationCompression.Off)
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
            
            // File name with wildcard support
            EditorGUILayout.BeginHorizontal();
            host.fileName = EditorGUILayout.TextField("File Name", host.fileName);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.SaveFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Ensure path is within Assets
                    if (!path.StartsWith(Application.dataPath))
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "Animation clips must be saved within the Assets folder", "OK");
                    }
                    else
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                        host.fileName = path + "/" + System.IO.Path.GetFileName(host.fileName);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Ensure path starts with Assets/
            if (!string.IsNullOrEmpty(host.fileName) && !host.fileName.StartsWith("Assets/"))
            {
                host.fileName = "Assets/" + host.fileName;
            }
            
            // Wildcard help
            EditorGUILayout.HelpBox(
                "Wildcards: <Scene>, <Take>, <Time>\n" +
                "Example: Assets/Animations/<Scene>_<Take>",
                MessageType.Info
            );
            
            // Preview
            EditorGUILayout.Space(5);
            var processor = new WildcardProcessor();
            var previewPath = processor.ProcessWildcards(
                host.fileName + ".anim",
                host.selectedDirector?.name ?? "Timeline",
                null,
                host.takeNumber
            );
            
            EditorGUILayout.LabelField("Preview", previewPath, EditorStyles.miniLabel);
            
            // Validation
            if (!string.IsNullOrEmpty(host.fileName) && !host.fileName.StartsWith("Assets/"))
            {
                EditorGUILayout.HelpBox("Path must start with 'Assets/'", MessageType.Error);
            }
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
            
            if (host.animationRecordingType == AnimationRecordingType.HumanoidRig)
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