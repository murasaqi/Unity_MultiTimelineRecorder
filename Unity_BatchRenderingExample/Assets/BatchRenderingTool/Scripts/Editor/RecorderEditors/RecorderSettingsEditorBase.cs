using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Base class for all recorder settings editors
    /// Follows Unity Recorder's standard UI pattern
    /// </summary>
    public abstract class RecorderSettingsEditorBase
    {
        protected IRecorderSettingsHost host;
        protected bool inputFoldout = true;
        protected bool outputFormatFoldout = true;
        protected bool outputFileFoldout = true;
        
        /// <summary>
        /// Draws the complete recorder settings UI
        /// </summary>
        public virtual void DrawRecorderSettings()
        {
            // Input section
            inputFoldout = EditorGUILayout.Foldout(inputFoldout, "Input");
            if (inputFoldout)
            {
                EditorGUI.indentLevel++;
                DrawInputSettings();
                EditorGUI.indentLevel--;
            }
            
            GUILayout.Space(10);
            
            // Output Format section
            outputFormatFoldout = EditorGUILayout.Foldout(outputFormatFoldout, "Output Format");
            if (outputFormatFoldout)
            {
                EditorGUI.indentLevel++;
                DrawOutputFormatSettings();
                EditorGUI.indentLevel--;
            }
            
            GUILayout.Space(10);
            
            // Output File section
            outputFileFoldout = EditorGUILayout.Foldout(outputFileFoldout, "Output File");
            if (outputFileFoldout)
            {
                EditorGUI.indentLevel++;
                DrawOutputFileSettings();
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draws the input settings specific to this recorder type
        /// </summary>
        protected virtual void DrawInputSettings()
        {
            EditorGUILayout.LabelField("Source", "Game View");
        }
        
        /// <summary>
        /// Draws the output format settings specific to this recorder type
        /// </summary>
        protected abstract void DrawOutputFormatSettings();
        
        /// <summary>
        /// Draws the output file settings
        /// </summary>
        protected virtual void DrawOutputFileSettings()
        {
            // File Name field with wildcards button
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("FileNameField");
            string newFileName = EditorGUILayout.TextField("File Name", host.fileName);
            if (newFileName != host.fileName)
            {
                host.fileName = newFileName;
                
                // Auto-add <Frame> for image sequence types if missing
                RecorderSettingsType currentType = GetRecorderType();
                if ((currentType == RecorderSettingsType.Image || currentType == RecorderSettingsType.AOV) 
                    && !host.fileName.Contains("<Frame>"))
                {
                    // Add <Frame> before extension if present, otherwise at the end
                    if (host.fileName.Contains("."))
                    {
                        int lastDotIndex = host.fileName.LastIndexOf('.');
                        host.fileName = host.fileName.Substring(0, lastDotIndex) + "_<Frame>" + host.fileName.Substring(lastDotIndex);
                    }
                    else
                    {
                        host.fileName += "_<Frame>";
                    }
                }
                
                GUI.changed = true;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Show example output
            EditorGUILayout.LabelField("Example: Scene1_Image_001.png", EditorStyles.miniLabel);
            
            // Path field with browse button
            EditorGUILayout.BeginHorizontal();
            
            // Path dropdown
            string[] pathOptions = { "Project", "Absolute" };
            int currentPathIndex = System.IO.Path.IsPathRooted(host.filePath) ? 1 : 0;
            int newPathIndex = EditorGUILayout.Popup("Path", currentPathIndex, pathOptions, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100));
            
            // Convert path if dropdown selection changed
            if (newPathIndex != currentPathIndex)
            {
                if (newPathIndex == 0) // Convert to project relative
                {
                    host.filePath = PathUtility.GetProjectRelativePath(host.filePath);
                }
                else // Convert to absolute
                {
                    host.filePath = PathUtility.GetAbsolutePath(host.filePath);
                }
            }
            
            // Path text field
            host.filePath = EditorGUILayout.TextField(host.filePath);
            
            // Browse button
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string currentAbsolutePath = PathUtility.GetAbsolutePath(host.filePath);
                string selectedPath = EditorUtility.SaveFolderPanel("Select Output Folder", currentAbsolutePath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (newPathIndex == 0) // Project relative mode
                    {
                        // Convert to project relative path
                        host.filePath = PathUtility.GetProjectRelativePath(selectedPath);
                    }
                    else // Absolute mode
                    {
                        host.filePath = selectedPath;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Full path preview (read-only)
            using (new EditorGUI.DisabledScope(true))
            {
                string fullPath = GetFullOutputPath();
                EditorGUILayout.TextField(" ", fullPath);
            }
            
            // Take number
            host.takeNumber = EditorGUILayout.IntField("Take Number", host.takeNumber);
        }
        
        /// <summary>
        /// Show wildcards popup menu
        /// </summary>
        protected void ShowWildcardsMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("<Scene>"), false, () => InsertWildcard("<Scene>"));
            menu.AddItem(new GUIContent("<Take>"), false, () => InsertWildcard("<Take>"));
            menu.AddItem(new GUIContent("<Recorder>"), false, () => InsertWildcard("<Recorder>"));
            menu.AddItem(new GUIContent("<Time>"), false, () => InsertWildcard("<Time>"));
            menu.AddItem(new GUIContent("<Frame>"), false, () => InsertWildcard("<Frame>"));
            menu.AddItem(new GUIContent("<Resolution>"), false, () => InsertWildcard("<Resolution>"));
            menu.AddItem(new GUIContent("<Product>"), false, () => InsertWildcard("<Product>"));
            menu.AddItem(new GUIContent("<Date>"), false, () => InsertWildcard("<Date>"));
            
            // Add context-specific wildcards
            bool addedSeparator = false;
            
            // Add Timeline wildcard if available
            if (GetTimelineName() != null)
            {
                if (!addedSeparator)
                {
                    menu.AddSeparator("");
                    addedSeparator = true;
                }
                menu.AddItem(new GUIContent("<Timeline>"), false, () => InsertWildcard("<Timeline>"));
            }
            
            // Add GameObject wildcard if available
            if (GetTargetGameObjectName() != null)
            {
                if (!addedSeparator)
                {
                    menu.AddSeparator("");
                    addedSeparator = true;
                }
                menu.AddItem(new GUIContent("<GameObject>"), false, () => InsertWildcard("<GameObject>"));
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Insert wildcard at cursor position
        /// </summary>
        protected void InsertWildcard(string wildcard)
        {
            // Simple append for now - in a real implementation, would insert at cursor
            host.fileName += wildcard;
            
            // Force GUI to update immediately
            GUI.FocusControl(null);
            GUI.changed = true;
            
            // Request repaint of the window
            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
        }
        
        /// <summary>
        /// Get full output path for preview
        /// </summary>
        protected virtual string GetFullOutputPath()
        {
            // Get absolute path using PathUtility
            string absolutePath = PathUtility.GetAbsolutePath(host.filePath);
            
            // Process wildcards for preview
            var context = new WildcardContext(host.takeNumber, host.width, host.height)
            {
                RecorderName = GetRecorderName(),
                GameObjectName = GetTargetGameObjectName(),
                TimelineName = GetTimelineName()
            };
            string processedFileName = WildcardProcessor.ProcessWildcards(host.fileName, context);
            
            // Add file extension based on recorder type
            string extension = GetFileExtension();
            if (!string.IsNullOrEmpty(extension) && !processedFileName.EndsWith("." + extension))
            {
                processedFileName += "." + extension;
            }
            
            // Combine and normalize the full path
            return PathUtility.CombineAndNormalize(absolutePath, processedFileName);
        }
        
        /// <summary>
        /// Get file extension for the current recorder type
        /// </summary>
        protected abstract string GetFileExtension();
        
        /// <summary>
        /// Get recorder name for wildcard processing
        /// </summary>
        protected abstract string GetRecorderName();
        
        /// <summary>
        /// Get target GameObject name for wildcard processing
        /// Override in recorders that have target GameObjects
        /// </summary>
        protected virtual string GetTargetGameObjectName()
        {
            return null;
        }
        
        /// <summary>
        /// Get Timeline name for wildcard processing
        /// </summary>
        protected virtual string GetTimelineName()
        {
            if (host.selectedDirector != null && host.selectedDirector.playableAsset != null)
            {
                return host.selectedDirector.playableAsset.name;
            }
            return null;
        }
        
        /// <summary>
        /// Validates the current settings
        /// </summary>
        public virtual bool ValidateSettings(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
        
        /// <summary>
        /// Get the recorder type for this editor
        /// </summary>
        protected abstract RecorderSettingsType GetRecorderType();
    }
}