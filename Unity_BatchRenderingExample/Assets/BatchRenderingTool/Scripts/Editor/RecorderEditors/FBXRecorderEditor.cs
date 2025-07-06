using UnityEngine;
using UnityEditor;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Editor UI for FBX Recorder settings
    /// </summary>
    public class FBXRecorderEditor : RecorderSettingsEditorBase
    {
        private bool showInputSettings = true;
        private bool showOutputFormatSettings = true;
        private bool showOutputFileSettings = true;
        
        public FBXRecorderEditor(IRecorderSettingsHost host)
        {
            this.host = host;
        }
        
        public override void DrawRecorderSettings()
        {
            EditorGUILayout.LabelField("FBX Recorder Settings", EditorStyles.boldLabel);
            
            // Frame rate settings at the top
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Frame Rate", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Playback", GUILayout.Width(100));
            EditorGUILayout.LabelField("Constant", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target (Timeline FPS)", GUILayout.Width(100));
            host.frameRate = EditorGUILayout.IntField(host.frameRate, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Input section (foldout)
            showInputSettings = EditorGUILayout.Foldout(showInputSettings, "Input", true);
            if (showInputSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                
                // GameObject selection
                host.fbxTargetGameObject = (GameObject)EditorGUILayout.ObjectField(
                    "GameObject", 
                    host.fbxTargetGameObject, 
                    typeof(GameObject), 
                    true);
                
                // Record Hierarchy checkbox
                host.fbxRecordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", host.fbxRecordHierarchy);
                
                // Clamped Tangents checkbox
                host.fbxClampedTangents = EditorGUILayout.Toggle("Clamped Tangents", host.fbxClampedTangents);
                
                // Animation Compression dropdown
                host.fbxAnimationCompression = (FBXAnimationCompressionLevel)EditorGUILayout.EnumPopup(
                    "Anim. Compression", 
                    host.fbxAnimationCompression);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
            
            // Output Format section (foldout)
            showOutputFormatSettings = EditorGUILayout.Foldout(showOutputFormatSettings, "Output Format", true);
            if (showOutputFormatSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                
                // Format display
                EditorGUILayout.LabelField("Format", "FBX");
                
                // Export Geometry checkbox
                host.fbxExportGeometry = EditorGUILayout.Toggle("Export Geometry", host.fbxExportGeometry);
                
                // Transfer Animation section
                EditorGUILayout.LabelField("Transfer Animation", EditorStyles.boldLabel);
                
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
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
            
            // Output File section (foldout)
            showOutputFileSettings = EditorGUILayout.Foldout(showOutputFileSettings, "Output File", true);
            if (showOutputFileSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                
                // File Name with wildcards
                EditorGUILayout.BeginHorizontal();
                host.fileName = EditorGUILayout.TextField("File Name", host.fileName);
                if (GUILayout.Button("+ Wildcards", GUILayout.Width(100)))
                {
                    ShowWildcardsMenu();
                }
                EditorGUILayout.EndHorizontal();
                
                // Path dropdown
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Path");
                if (GUILayout.Button("Assets Folder", "MiniPopup"))
                {
                    ShowPathMenu();
                }
                EditorGUILayout.TextField(host.filePath);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    BrowseOutputPath();
                }
                EditorGUILayout.EndHorizontal();
                
                // Show full path
                string fullPath = GetFullOutputPath();
                EditorGUILayout.HelpBox($"Output: {fullPath}", MessageType.None);
                
                // Take Number
                host.takeNumber = EditorGUILayout.IntField("Take Number", host.takeNumber);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            
            // Warning if no input object is set
            if (host.fbxTargetGameObject == null)
            {
                EditorGUILayout.HelpBox("No input object set", MessageType.Warning);
            }
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
            // This method is now incorporated into the main DrawRecorderSettings method
            // Left empty to maintain compatibility with base class
        }
        
        private void ShowWildcardsMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("GameObject"), false, () => host.fileName += "<GameObject>");
            menu.AddItem(new GUIContent("Scene"), false, () => host.fileName += "<Scene>");
            menu.AddItem(new GUIContent("Take"), false, () => host.fileName += "<Take>");
            menu.AddItem(new GUIContent("Recorder"), false, () => host.fileName += "<Recorder>");
            menu.AddItem(new GUIContent("Date"), false, () => host.fileName += "<Date>");
            menu.AddItem(new GUIContent("Time"), false, () => host.fileName += "<Time>");
            menu.AddItem(new GUIContent("Frame"), false, () => host.fileName += "<Frame>");
            menu.ShowAsContext();
        }
        
        private void ShowPathMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Assets Folder"), false, () => host.filePath = "Recordings");
            menu.AddItem(new GUIContent("Project Folder"), false, () => host.filePath = "../Recordings");
            menu.AddItem(new GUIContent("Absolute Path"), false, () => 
            {
                string path = EditorUtility.SaveFolderPanel("Select Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    host.filePath = path;
                }
            });
            menu.ShowAsContext();
        }
        
        private void BrowseOutputPath()
        {
            string path = EditorUtility.SaveFolderPanel("Select Output Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to relative path if inside project
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                host.filePath = path;
            }
        }
        
        private string GetFullOutputPath()
        {
            string basePath = host.filePath;
            if (basePath.StartsWith("Assets"))
            {
                basePath = Application.dataPath + basePath.Substring(6);
            }
            else if (!System.IO.Path.IsPathRooted(basePath))
            {
                basePath = System.IO.Path.Combine(Application.dataPath, basePath);
            }
            
            string fileName = ResolveWildcards(host.fileName);
            return System.IO.Path.Combine(basePath, fileName + "." + GetFileExtension());
        }
        
        private string ResolveWildcards(string pattern)
        {
            string result = pattern;
            result = result.Replace("<GameObject>", host.fbxTargetGameObject?.name ?? "None");
            result = result.Replace("<Scene>", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            result = result.Replace("<Take>", host.takeNumber.ToString("000"));
            result = result.Replace("<Recorder>", "FBX");
            result = result.Replace("<Date>", System.DateTime.Now.ToString("yyyy-MM-dd"));
            result = result.Replace("<Time>", System.DateTime.Now.ToString("HH-mm-ss"));
            return result;
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
    }
}