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
            inputFoldout = RecorderUIHelper.DrawHeaderFoldout(inputFoldout, "Input");
            if (inputFoldout)
            {
                RecorderUIHelper.BeginIndentedSection();
                DrawInputSettings();
                RecorderUIHelper.EndIndentedSection();
            }
            
            RecorderUIHelper.DrawSeparator();
            
            // Output Format section
            outputFormatFoldout = RecorderUIHelper.DrawHeaderFoldout(outputFormatFoldout, "Output Format");
            if (outputFormatFoldout)
            {
                RecorderUIHelper.BeginIndentedSection();
                DrawOutputFormatSettings();
                RecorderUIHelper.EndIndentedSection();
            }
            
            RecorderUIHelper.DrawSeparator();
            
            // Output File section
            outputFileFoldout = RecorderUIHelper.DrawHeaderFoldout(outputFileFoldout, "Output File");
            if (outputFileFoldout)
            {
                RecorderUIHelper.BeginIndentedSection();
                DrawOutputFileSettings();
                RecorderUIHelper.EndIndentedSection();
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
            host.fileName = EditorGUILayout.TextField("File Name", host.fileName);
            
            // Wildcards dropdown button
            if (GUILayout.Button("+ Wildcards ▼", GUILayout.Width(100)))
            {
                ShowWildcardsMenu();
            }
            EditorGUILayout.EndHorizontal();
            
            // Path field with browse button
            EditorGUILayout.BeginHorizontal();
            
            // Path dropdown
            string[] pathOptions = { "Project", "Absolute" };
            int pathIndex = host.filePath.StartsWith(Application.dataPath) ? 1 : 0;
            int newPathIndex = EditorGUILayout.Popup("Path", pathIndex, pathOptions, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100));
            
            // Path text field
            if (newPathIndex == 0) // Project relative
            {
                // Ensure relative path
                if (pathIndex == 1)
                {
                    host.filePath = PathUtility.GetProjectRelativePath(host.filePath);
                }
                host.filePath = EditorGUILayout.TextField(host.filePath);
            }
            else // Absolute path
            {
                if (pathIndex == 0) // Converting from relative to absolute
                {
                    host.filePath = PathUtility.GetAbsolutePath(host.filePath);
                }
                host.filePath = EditorGUILayout.TextField(host.filePath);
            }
            
            // Browse button
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedPath = EditorUtility.SaveFolderPanel("Select Output Folder", host.filePath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (newPathIndex == 0)
                    {
                        // Convert to project relative path
                        host.filePath = PathUtility.GetProjectRelativePath(selectedPath);
                    }
                    else
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
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Insert wildcard at cursor position
        /// </summary>
        protected void InsertWildcard(string wildcard)
        {
            // Simple append for now - in a real implementation, would insert at cursor
            host.fileName += wildcard;
        }
        
        /// <summary>
        /// Get full output path for preview
        /// </summary>
        protected virtual string GetFullOutputPath()
        {
            // Get absolute path using PathUtility
            string absolutePath = PathUtility.GetAbsolutePath(host.filePath);
            
            // Process wildcards for preview
            var context = new WildcardContext(host.takeNumber, host.width, host.height);
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
        /// Validates the current settings
        /// </summary>
        public virtual bool ValidateSettings(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }
}