using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool
{
    /// <summary>
    /// Helper class for Unity Recorder-style UI components
    /// </summary>
    public static class RecorderUIHelper
    {
        private static readonly Color HeaderBackgroundColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color HeaderBackgroundColorPro = new Color(0.3f, 0.3f, 0.3f);
        
        /// <summary>
        /// Draws a Unity Recorder-style foldout header
        /// </summary>
        public static bool DrawHeaderFoldout(bool isExpanded, string label)
        {
            var rect = GUILayoutUtility.GetRect(16f, 17f, GUILayout.ExpandWidth(true));
            var backgroundColor = EditorGUIUtility.isProSkin ? HeaderBackgroundColorPro : HeaderBackgroundColor;
            
            // Draw background
            EditorGUI.DrawRect(rect, backgroundColor);
            
            // Create clickable area
            var clickArea = rect;
            clickArea.x += 16f;
            clickArea.width -= 16f;
            
            // Draw foldout
            var foldoutRect = rect;
            foldoutRect.x += 3f;
            foldoutRect.width = 13f;
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, GUIContent.none);
            
            // Draw label
            var labelRect = rect;
            labelRect.x += 20f;
            labelRect.width -= 20f;
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            EditorGUI.LabelField(labelRect, label, style);
            
            // Handle click on header
            if (Event.current.type == EventType.MouseDown && clickArea.Contains(Event.current.mousePosition))
            {
                isExpanded = !isExpanded;
                Event.current.Use();
            }
            
            return isExpanded;
        }
        
        /// <summary>
        /// Begins a standard indented section
        /// </summary>
        public static void BeginIndentedSection()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical();
        }
        
        /// <summary>
        /// Ends a standard indented section
        /// </summary>
        public static void EndIndentedSection()
        {
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// Draws a property field with optional help box
        /// </summary>
        public static void DrawPropertyWithHelp(string label, string helpText, MessageType messageType, System.Action drawProperty)
        {
            drawProperty?.Invoke();
            
            if (!string.IsNullOrEmpty(helpText))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(helpText, messageType);
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draws a separator line
        /// </summary>
        public static void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            var color = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.Space(2);
        }
    }
}