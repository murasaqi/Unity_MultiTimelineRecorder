using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Output path location types (similar to Unity Recorder)
    /// </summary>
    public enum OutputPathLocation
    {
        Project,        // Relative to project (Assets/../)
        Persistent,     // Application.persistentDataPath
        Temporary,      // Application.temporaryDataPath
        Absolute        // Absolute path
    }
    
    /// <summary>
    /// Output path mode for individual recorders
    /// </summary>
    public enum RecorderPathMode
    {
        UseGlobal,      // Use global settings as-is
        RelativeToGlobal, // Use path relative to global settings
        Custom          // Use completely custom path
    }
    
    /// <summary>
    /// Settings for output path configuration
    /// </summary>
    [Serializable]
    public class OutputPathSettings
    {
        public OutputPathLocation location = OutputPathLocation.Project;
        public string path = "Recordings/<Timeline>";
        public RecorderPathMode pathMode = RecorderPathMode.UseGlobal;
        public string customPath = "";
        
        /// <summary>
        /// Get the full resolved path
        /// </summary>
        public string GetResolvedPath(WildcardContext context = null)
        {
            string basePath = GetBasePath();
            string relativePath = context != null ? WildcardProcessor.ProcessWildcards(path, context) : path;
            
            // Combine paths based on location
            string fullPath;
            if (location == OutputPathLocation.Absolute)
            {
                fullPath = relativePath;
            }
            else
            {
                // Check if path contains wildcards to avoid Path.Combine errors
                if (relativePath.Contains("<") || relativePath.Contains(">"))
                {
                    // For paths with wildcards, use simple string concatenation
                    fullPath = basePath + "/" + relativePath;
                }
                else
                {
                    fullPath = Path.Combine(basePath, relativePath);
                }
            }
            
            // Normalize path separators
            fullPath = fullPath.Replace('\\', '/');
            
            return fullPath;
        }
        
        /// <summary>
        /// Get base path based on location setting
        /// </summary>
        public string GetBasePath()
        {
            switch (location)
            {
                case OutputPathLocation.Project:
                    // Get path relative to project (parent of Assets folder)
                    string projectPath = Path.GetDirectoryName(Application.dataPath);
                    return projectPath;
                    
                case OutputPathLocation.Persistent:
                    return Application.persistentDataPath;
                    
                case OutputPathLocation.Temporary:
                    return Application.temporaryCachePath;
                    
                case OutputPathLocation.Absolute:
                    return "";
                    
                default:
                    return Path.GetDirectoryName(Application.dataPath);
            }
        }
        
        /// <summary>
        /// Clone settings
        /// </summary>
        public OutputPathSettings Clone()
        {
            return new OutputPathSettings
            {
                location = this.location,
                path = this.path,
                pathMode = this.pathMode,
                customPath = this.customPath
            };
        }
    }
    
    /// <summary>
    /// Helper class for output path UI
    /// </summary>
    public static class OutputPathSettingsUI
    {
        // Consistent label width for all UI elements
        private const float LABEL_WIDTH = 100f;
        public static void DrawOutputPathUI(OutputPathSettings settings, string label = "Output Path", string timelineName = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Location dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(LABEL_WIDTH));
            
            OutputPathLocation newLocation = (OutputPathLocation)EditorGUILayout.EnumPopup(
                settings.location, GUILayout.Width(100));
            
            if (newLocation != settings.location)
            {
                settings.location = newLocation;
                
                // Reset path when switching to/from absolute
                // Check for wildcards to avoid path operation errors
                bool hasWildcards = settings.path.Contains("<") || settings.path.Contains(">");
                
                if (newLocation == OutputPathLocation.Absolute && !hasWildcards && !Path.IsPathRooted(settings.path))
                {
                    string projectPath = Path.GetDirectoryName(Application.dataPath);
                    settings.path = Path.Combine(projectPath, settings.path);
                }
                else if (settings.location != OutputPathLocation.Absolute && !hasWildcards && Path.IsPathRooted(settings.path))
                {
                    settings.path = "Recordings";
                }
            }
            
            // Path field
            settings.path = EditorGUILayout.TextField(settings.path);
            
            // Browse button
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                // Get the base path without wildcards for the folder dialog
                string dialogPath = settings.GetBasePath();
                if (settings.location == OutputPathLocation.Absolute && !string.IsNullOrEmpty(settings.path))
                {
                    // For absolute paths, try to use the path if it doesn't contain wildcards
                    if (!settings.path.Contains("<") && !settings.path.Contains(">"))
                    {
                        dialogPath = settings.path;
                    }
                }
                
                string selectedPath = EditorUtility.OpenFolderPanel(
                    "Select Output Folder", 
                    dialogPath, 
                    "");
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (settings.location == OutputPathLocation.Absolute)
                    {
                        settings.path = selectedPath;
                    }
                    else
                    {
                        // Convert to relative path
                        string basePath = settings.GetBasePath();
                        if (selectedPath.StartsWith(basePath))
                        {
                            settings.path = selectedPath.Substring(basePath.Length + 1);
                        }
                        else
                        {
                            // If outside base path, switch to absolute
                            settings.location = OutputPathLocation.Absolute;
                            settings.path = selectedPath;
                        }
                    }
                }
            }
            
            // Wildcards button
            if (GUILayout.Button("Wildcards", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                ShowPathWildcardsMenu(settings);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Show resolved path with proper wrapping
            var sampleContext = new WildcardContext
            {
                SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                TimelineName = timelineName ?? "Timeline",
                RecorderName = "Recorder",
                TakeNumber = 1,
                Width = 1920,
                Height = 1080
            };
            string resolvedPath = settings.GetResolvedPath(sampleContext);
            EditorGUILayout.Space(2);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Path Preview:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(resolvedPath, EditorStyles.wordWrappedLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        public static void DrawRecorderPathUI(OutputPathSettings globalSettings, OutputPathSettings recorderSettings, string label = "Output Path", string timelineName = "Timeline", string recorderName = "Recorder")
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Path mode selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path Mode:", GUILayout.Width(LABEL_WIDTH));
            var previousMode = recorderSettings.pathMode;
            recorderSettings.pathMode = (RecorderPathMode)EditorGUILayout.EnumPopup(recorderSettings.pathMode);
            
            // Handle mode change
            if (previousMode != recorderSettings.pathMode)
            {
                OnPathModeChanged(globalSettings, recorderSettings, previousMode);
                GUI.changed = true;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            switch (recorderSettings.pathMode)
            {
                case RecorderPathMode.UseGlobal:
                    // Show global path preview with proper wrapping
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Using Global Path:", EditorStyles.boldLabel);
                        // Create context with actual timeline and recorder names
                        var sampleContext = new WildcardContext
                        {
                            TimelineName = timelineName,
                            RecorderName = recorderName,
                            TakeNumber = 1,
                            Width = 1920,
                            Height = 1080
                        };
                        string previewPath = globalSettings.GetResolvedPath(sampleContext);
                        EditorGUILayout.LabelField(previewPath, EditorStyles.wordWrappedLabel);
                    }
                    break;
                    
                case RecorderPathMode.RelativeToGlobal:
                    // Show relative path input
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Relative Path:", GUILayout.Width(LABEL_WIDTH));
                    recorderSettings.customPath = EditorGUILayout.TextField(recorderSettings.customPath);
                    
                    if (GUILayout.Button("Wildcards", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        ShowRelativePathWildcardsMenu(recorderSettings);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(2);
                    
                    // Show combined path preview using OutputPathManager with actual context
                    var sampleContextRelative = new WildcardContext
                    {
                        TimelineName = timelineName,
                        RecorderName = recorderName,
                        TakeNumber = 1,
                        Width = 1920,
                        Height = 1080
                    };
                    
                    // Get resolved paths with wildcards processed
                    string resolvedGlobalPath = globalSettings.GetResolvedPath(sampleContextRelative);
                    string resolvedRelativePath = WildcardProcessor.ProcessWildcards(recorderSettings.customPath, sampleContextRelative);
                    
                    // Combine paths
                    string combinedPath;
                    if (resolvedGlobalPath.Contains("/") || resolvedRelativePath.Contains("/"))
                    {
                        combinedPath = resolvedGlobalPath + "/" + resolvedRelativePath;
                    }
                    else
                    {
                        combinedPath = System.IO.Path.Combine(resolvedGlobalPath, resolvedRelativePath);
                    }
                    
                    // Show combined path preview with proper wrapping
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Path Preview:", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(combinedPath, EditorStyles.wordWrappedLabel);
                    }
                    break;
                    
                case RecorderPathMode.Custom:
                    // Show full custom path UI with consistent layout
                    EditorGUILayout.Space(2);
                    DrawCustomPathUI(recorderSettings, timelineName, recorderName);
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private static void ShowPathWildcardsMenu(OutputPathSettings settings)
        {
            GenericMenu menu = new GenericMenu();
            
            // Basic wildcards - insert at cursor position if possible
            var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            
            Action<string> insertWildcard = (wildcard) => {
                if (textEditor != null && textEditor.hasSelection)
                {
                    // Replace selection
                    int start = Mathf.Min(textEditor.selectIndex, textEditor.cursorIndex);
                    int end = Mathf.Max(textEditor.selectIndex, textEditor.cursorIndex);
                    settings.path = settings.path.Substring(0, start) + wildcard + settings.path.Substring(end);
                }
                else if (textEditor != null)
                {
                    // Insert at cursor
                    int pos = textEditor.cursorIndex;
                    settings.path = settings.path.Insert(pos, wildcard);
                }
                else
                {
                    // Append to end
                    settings.path += "/" + wildcard;
                }
                GUI.changed = true;
                GUI.FocusControl(null); // Clear focus to force immediate update
                GUIUtility.keyboardControl = 0; // Reset keyboard control
                GUIUtility.ExitGUI(); // Force immediate GUI update
            };
            
            // Basic wildcards
            menu.AddItem(new GUIContent("<Scene>"), false, () => insertWildcard("<Scene>"));
            menu.AddItem(new GUIContent("<Timeline>"), false, () => insertWildcard("<Timeline>"));
            menu.AddItem(new GUIContent("<Take>"), false, () => insertWildcard("<Take>"));
            menu.AddItem(new GUIContent("<TimelineTake>"), false, () => insertWildcard("<TimelineTake>"));
            menu.AddItem(new GUIContent("<Date>"), false, () => insertWildcard("<Date>"));
            menu.AddItem(new GUIContent("<Time>"), false, () => insertWildcard("<Time>"));
            menu.AddItem(new GUIContent("<Resolution>"), false, () => insertWildcard("<Resolution>"));
            menu.AddItem(new GUIContent("<Recorder>"), false, () => insertWildcard("<Recorder>"));
            
            menu.AddSeparator("");
            
            // Example patterns
            menu.AddItem(new GUIContent("Examples/By Timeline: Recordings/<Timeline>"), false, () => 
            {
                settings.path = "Recordings/<Timeline>";
                GUI.changed = true;
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                GUIUtility.ExitGUI();
            });
            menu.AddItem(new GUIContent("Examples/By Scene: Recordings/<Scene>/<Timeline>"), false, () => 
            {
                settings.path = "Recordings/<Scene>/<Timeline>";
                GUI.changed = true;
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                GUIUtility.ExitGUI();
            });
            menu.AddItem(new GUIContent("Examples/By Date: Recordings/<Date>/<Timeline>"), false, () => 
            {
                settings.path = "Recordings/<Date>/<Timeline>";
                GUI.changed = true;
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                GUIUtility.ExitGUI();
            });
            
            menu.ShowAsContext();
        }
        
        private static void ShowRelativePathWildcardsMenu(OutputPathSettings settings)
        {
            GenericMenu menu = new GenericMenu();
            
            var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            
            Action<string> insertWildcard = (wildcard) => {
                if (textEditor != null && textEditor.hasSelection)
                {
                    // Replace selection
                    int start = Mathf.Min(textEditor.selectIndex, textEditor.cursorIndex);
                    int end = Mathf.Max(textEditor.selectIndex, textEditor.cursorIndex);
                    settings.customPath = settings.customPath.Substring(0, start) + wildcard + settings.customPath.Substring(end);
                }
                else if (textEditor != null)
                {
                    // Insert at cursor
                    int pos = textEditor.cursorIndex;
                    settings.customPath = settings.customPath.Insert(pos, wildcard);
                }
                else
                {
                    // Append to end
                    settings.customPath += wildcard;
                }
                GUI.changed = true;
                GUI.FocusControl(null); // Clear focus to force immediate update
                GUIUtility.keyboardControl = 0; // Reset keyboard control
                GUIUtility.ExitGUI(); // Force immediate GUI update
            };
            
            // Basic wildcards
            menu.AddItem(new GUIContent("<Timeline>"), false, () => insertWildcard("<Timeline>"));
            menu.AddItem(new GUIContent("<Recorder>"), false, () => insertWildcard("<Recorder>"));
            menu.AddItem(new GUIContent("<Take>"), false, () => insertWildcard("<Take>"));
            menu.AddItem(new GUIContent("<TimelineTake>"), false, () => insertWildcard("<TimelineTake>"));
            menu.AddItem(new GUIContent("<Date>"), false, () => insertWildcard("<Date>"));
            
            menu.ShowAsContext();
        }
        
        private static void DrawCustomPathUI(OutputPathSettings settings, string timelineName = "Timeline", string recorderName = "Recorder")
        {
            // Location dropdown with consistent layout
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Location:", GUILayout.Width(LABEL_WIDTH));
            
            OutputPathLocation newLocation = (OutputPathLocation)EditorGUILayout.EnumPopup(
                settings.location, GUILayout.Width(100));
            
            if (newLocation != settings.location)
            {
                settings.location = newLocation;
                
                // Reset path when switching to/from absolute
                // Check for wildcards to avoid path operation errors
                bool hasWildcards = settings.path.Contains("<") || settings.path.Contains(">");
                
                if (newLocation == OutputPathLocation.Absolute && !hasWildcards && !Path.IsPathRooted(settings.path))
                {
                    string projectPath = Path.GetDirectoryName(Application.dataPath);
                    settings.path = Path.Combine(projectPath, settings.path);
                }
                else if (settings.location != OutputPathLocation.Absolute && !hasWildcards && Path.IsPathRooted(settings.path))
                {
                    settings.path = "Recordings";
                }
            }
            
            // Path field
            settings.path = EditorGUILayout.TextField(settings.path);
            
            // Browse button
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                // Get the base path without wildcards for the folder dialog
                string dialogPath = settings.GetBasePath();
                if (settings.location == OutputPathLocation.Absolute && !string.IsNullOrEmpty(settings.path))
                {
                    // For absolute paths, try to use the path if it doesn't contain wildcards
                    if (!settings.path.Contains("<") && !settings.path.Contains(">"))
                    {
                        dialogPath = settings.path;
                    }
                }
                
                string selectedPath = EditorUtility.OpenFolderPanel(
                    "Select Output Folder", 
                    dialogPath, 
                    "");
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (settings.location == OutputPathLocation.Absolute)
                    {
                        settings.path = selectedPath;
                    }
                    else
                    {
                        // Convert to relative path
                        string basePath = settings.GetBasePath();
                        if (selectedPath.StartsWith(basePath))
                        {
                            settings.path = selectedPath.Substring(basePath.Length + 1);
                        }
                        else
                        {
                            // If outside base path, switch to absolute
                            settings.location = OutputPathLocation.Absolute;
                            settings.path = selectedPath;
                        }
                    }
                }
            }
            
            // Wildcards button
            if (GUILayout.Button("Wildcards", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                ShowPathWildcardsMenu(settings);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Show resolved path with proper wrapping
            EditorGUILayout.Space(2);
            using (new EditorGUI.IndentLevelScope())
            {
                var sampleContext = new WildcardContext
                {
                    SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    TimelineName = timelineName,
                    RecorderName = recorderName,
                    TakeNumber = 1,
                    Width = 1920,
                    Height = 1080
                };
                string resolvedPath = settings.GetResolvedPath(sampleContext);
                EditorGUILayout.LabelField("Path Preview:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(resolvedPath, EditorStyles.wordWrappedLabel);
            }
        }
        
        /// <summary>
        /// Handles path mode changes and updates settings appropriately
        /// </summary>
        private static void OnPathModeChanged(OutputPathSettings globalSettings, OutputPathSettings recorderSettings, RecorderPathMode previousMode)
        {
            switch (recorderSettings.pathMode)
            {
                case RecorderPathMode.UseGlobal:
                    // Clear custom paths when switching to global
                    recorderSettings.customPath = "";
                    recorderSettings.path = "";
                    break;
                    
                case RecorderPathMode.RelativeToGlobal:
                    // Initialize with a sensible default if empty
                    if (string.IsNullOrEmpty(recorderSettings.customPath))
                    {
                        recorderSettings.customPath = "<Recorder>";
                    }
                    break;
                    
                case RecorderPathMode.Custom:
                    // If switching from UseGlobal, copy the global path as starting point
                    if (previousMode == RecorderPathMode.UseGlobal && string.IsNullOrEmpty(recorderSettings.path))
                    {
                        recorderSettings.path = globalSettings.path;
                        recorderSettings.location = globalSettings.location;
                    }
                    break;
            }
        }
    }
}