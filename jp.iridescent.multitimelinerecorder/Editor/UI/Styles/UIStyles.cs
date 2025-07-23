using UnityEngine;
using UnityEditor;

namespace MultiTimelineRecorder.UI.Styles
{
    /// <summary>
    /// Centralized UI styles for Multi Timeline Recorder
    /// </summary>
    public static class UIStyles
    {
        // Colors
        public static readonly Color SelectionColor = EditorGUIUtility.isProSkin 
            ? new Color(0.22f, 0.44f, 0.69f, 0.5f)
            : new Color(0.31f, 0.57f, 0.87f, 0.5f);
            
        public static readonly Color ActiveSelectionColor = EditorGUIUtility.isProSkin
            ? new Color(0.28f, 0.55f, 0.87f, 0.6f)
            : new Color(0.38f, 0.64f, 0.94f, 0.6f);
            
        public static readonly Color HoverColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.05f)
            : new Color(0f, 0f, 0f, 0.05f);
        
        public static readonly Color ColumnBackgroundColor = EditorGUIUtility.isProSkin 
            ? new Color(0.22f, 0.22f, 0.22f, 0.3f)
            : new Color(0.9f, 0.9f, 0.9f, 0.3f);
            
        public static readonly Color ListBackgroundColor = EditorGUIUtility.isProSkin
            ? new Color(0.25f, 0.25f, 0.25f, 0.5f)
            : new Color(0.95f, 0.95f, 0.95f, 0.5f);
            
        public static readonly Color AlternateRowColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.02f)
            : new Color(0f, 0f, 0f, 0.02f);
            
        public static readonly Color ErrorColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        public static readonly Color WarningColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        public static readonly Color SuccessColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        
        // Dimensions
        public const float CheckboxWidth = 20f;
        public const float IconWidth = 25f;
        public const float DeleteButtonWidth = 20f;
        public const float MinButtonWidth = 200f;
        public const float MinListItemWidth = 200f;
        public const float StandardSpacing = 5f;
        public const float SectionSpacing = 10f;
        public const int HeaderFontSize = 14;
        public const float ListItemHeight = 22f;
        public const float FieldLabelWidth = 150f;
        
        // GUIStyles
        private static GUIStyle _headerLabel;
        public static GUIStyle HeaderLabel
        {
            get
            {
                if (_headerLabel == null)
                {
                    _headerLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = HeaderFontSize,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 5, 0, 0)
                    };
                }
                return _headerLabel;
            }
        }
        
        private static GUIStyle _selectableLabel;
        public static GUIStyle SelectableLabel
        {
            get
            {
                if (_selectableLabel == null)
                {
                    _selectableLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(4, 4, 2, 2)
                    };
                }
                return _selectableLabel;
            }
        }
        
        private static GUIStyle _selectedLabel;
        public static GUIStyle SelectedLabel
        {
            get
            {
                if (_selectedLabel == null)
                {
                    _selectedLabel = new GUIStyle(SelectableLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                    };
                }
                return _selectedLabel;
            }
        }
        
        private static GUIStyle _listItemBackground;
        public static GUIStyle ListItemBackground
        {
            get
            {
                if (_listItemBackground == null)
                {
                    _listItemBackground = new GUIStyle("CN Box")
                    {
                        padding = new RectOffset(4, 4, 4, 4),
                        margin = new RectOffset(0, 0, 2, 2)
                    };
                }
                return _listItemBackground;
            }
        }
        
        private static GUIStyle _columnBackground;
        public static GUIStyle ColumnBackground
        {
            get
            {
                if (_columnBackground == null)
                {
                    _columnBackground = new GUIStyle()
                    {
                        normal = { background = MakeColorTexture(ColumnBackgroundColor) }
                    };
                }
                return _columnBackground;
            }
        }
        
        private static GUIStyle _headerBackground;
        public static GUIStyle HeaderBackground
        {
            get
            {
                if (_headerBackground == null)
                {
                    _headerBackground = new GUIStyle(EditorStyles.toolbar)
                    {
                        padding = new RectOffset(5, 5, 0, 0)
                    };
                }
                return _headerBackground;
            }
        }
        
        private static GUIStyle _statusBarBackground;
        public static GUIStyle StatusBarBackground
        {
            get
            {
                if (_statusBarBackground == null)
                {
                    _statusBarBackground = new GUIStyle(EditorStyles.toolbar);
                }
                return _statusBarBackground;
            }
        }
        
        private static GUIStyle _statusLabel;
        public static GUIStyle StatusLabel
        {
            get
            {
                if (_statusLabel == null)
                {
                    _statusLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 5, 0, 0)
                    };
                }
                return _statusLabel;
            }
        }
        
        private static GUIStyle _sectionHeader;
        public static GUIStyle SectionHeader
        {
            get
            {
                if (_sectionHeader == null)
                {
                    _sectionHeader = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        margin = new RectOffset(0, 0, 5, 5)
                    };
                }
                return _sectionHeader;
            }
        }
        
        private static GUIStyle _miniButtonLeft;
        public static GUIStyle MiniButtonLeft
        {
            get
            {
                if (_miniButtonLeft == null)
                {
                    _miniButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft);
                }
                return _miniButtonLeft;
            }
        }
        
        private static GUIStyle _miniButtonMid;
        public static GUIStyle MiniButtonMid
        {
            get
            {
                if (_miniButtonMid == null)
                {
                    _miniButtonMid = new GUIStyle(EditorStyles.miniButtonMid);
                }
                return _miniButtonMid;
            }
        }
        
        private static GUIStyle _miniButtonRight;
        public static GUIStyle MiniButtonRight
        {
            get
            {
                if (_miniButtonRight == null)
                {
                    _miniButtonRight = new GUIStyle(EditorStyles.miniButtonRight);
                }
                return _miniButtonRight;
            }
        }
        
        private static GUIStyle _validationBox;
        public static GUIStyle ValidationBox
        {
            get
            {
                if (_validationBox == null)
                {
                    _validationBox = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(10, 10, 10, 10),
                        margin = new RectOffset(5, 5, 5, 5)
                    };
                }
                return _validationBox;
            }
        }
        
        // Helper methods
        private static Texture2D MakeColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        
        public static void DrawSelectionRect(Rect rect, bool isSelected, bool isHovered)
        {
            if (isSelected)
            {
                EditorGUI.DrawRect(rect, ActiveSelectionColor);
            }
            else if (isHovered)
            {
                EditorGUI.DrawRect(rect, HoverColor);
            }
        }
        
        public static void DrawAlternatingBackground(Rect rect, int index)
        {
            if (index % 2 == 1)
            {
                EditorGUI.DrawRect(rect, AlternateRowColor);
            }
        }
        
        public static void DrawSeparator(float height = 1f)
        {
            var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin 
                ? new Color(0.15f, 0.15f, 0.15f) 
                : new Color(0.6f, 0.6f, 0.6f));
        }
        
        public static void DrawHorizontalLine()
        {
            GUILayout.Space(2);
            DrawSeparator();
            GUILayout.Space(2);
        }
    }
}