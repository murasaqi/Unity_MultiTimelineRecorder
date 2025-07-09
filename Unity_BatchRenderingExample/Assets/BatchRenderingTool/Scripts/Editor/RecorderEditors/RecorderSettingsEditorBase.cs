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
        
        // セクション見出しのスタイル
        private static class SectionStyles
        {
            public static readonly Color HeaderBackgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)  // Pro Skin: 暗い背景
                : new Color(0.8f, 0.8f, 0.8f, 1f);     // Light Skin: 明るいグレー
                
            public static readonly Color LineColor = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 1f)      // Pro Skin: 暗いライン
                : new Color(0.6f, 0.6f, 0.6f, 1f);     // Light Skin: 明るいライン
                
            public static GUIStyle HeaderLabel => new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
        }
        
        /// <summary>
        /// Draws a section header with background and optional foldout
        /// </summary>
        protected bool DrawSectionHeader(string title, bool foldout = true, bool isFoldable = true)
        {
            EditorGUILayout.Space(2);
            
            // セクションヘッダーの背景を描画
            Rect headerRect = EditorGUILayout.GetControlRect(false, 20);
            if (Event.current.type == EventType.Repaint)
            {
                // 背景を描画
                EditorGUI.DrawRect(headerRect, SectionStyles.HeaderBackgroundColor);
                
                // 下線を描画
                Rect lineRect = new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1);
                EditorGUI.DrawRect(lineRect, SectionStyles.LineColor);
            }
            
            // インデントを調整してヘッダーを描画
            headerRect.x += 4;
            headerRect.width -= 8;
            
            if (isFoldable)
            {
                return EditorGUI.Foldout(headerRect, foldout, title, true, SectionStyles.HeaderLabel);
            }
            else
            {
                GUI.Label(headerRect, title, SectionStyles.HeaderLabel);
                return true;
            }
        }
        
        /// <summary>
        /// Draws the complete recorder settings UI
        /// </summary>
        public virtual void DrawRecorderSettings()
        {
            // Input section
            inputFoldout = DrawSectionHeader("Input", inputFoldout);
            if (inputFoldout)
            {
                EditorGUI.indentLevel++;
                DrawInputSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Output Format section
            outputFormatFoldout = DrawSectionHeader("Output Format", outputFormatFoldout);
            if (outputFormatFoldout)
            {
                EditorGUI.indentLevel++;
                DrawOutputFormatSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Output File section
            outputFileFoldout = DrawSectionHeader("Output File", outputFileFoldout);
            if (outputFileFoldout)
            {
                EditorGUI.indentLevel++;
                DrawOutputFileSettings();
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draws a simple separator line
        /// </summary>
        protected void DrawSeparator()
        {
            EditorGUILayout.Space(3);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, SectionStyles.LineColor);
            }
            EditorGUILayout.Space(3);
        }
        
        /// <summary>
        /// Draws a subsection header without background
        /// </summary>
        protected void DrawSubsectionHeader(string title)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
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
            
            // Show example output with processed filename
            string exampleFileName = GetFullOutputPath();
            EditorGUILayout.LabelField($"Example: {exampleFileName}", EditorStyles.miniLabel);
            
            // Path settings are now handled by OutputPathSettingsUI in SingleTimelineRenderer
            // This prevents duplicate path UI elements
            
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
            
            // Return just the processed filename for preview
            return processedFileName;
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