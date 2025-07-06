using UnityEngine;
using UnityEditor;

namespace BatchRenderingTool
{
    /// <summary>
    /// Helper utility for Unity Recorder wildcards and file naming
    /// </summary>
    public static class WildcardHelpUtility
    {
        /// <summary>
        /// Get help text for wildcards
        /// </summary>
        public static string GetWildcardHelpText()
        {
            return @"Unity Recorder Wildcards:
• <Frame> - Frame number (0001, 0002, etc.)
• <Take> - Take number (increments each recording)
• <Scene> - Current scene name
• <Project> - Project name
• <Product> - Product name from settings
• <Date> - Recording date (yyyy-MM-dd)
• <Time> - Recording time (hh-mm-ss)
• <Resolution> - Output resolution (e.g., 1920x1080)
• <Recorder> - Recorder type (Image, Movie, etc.)

Example: MyScene_<Recorder>_<Take>_<Frame>
Result: MyScene_Image_001_0001.png";
        }
        
        /// <summary>
        /// Get recommended filename patterns for different recorder types
        /// </summary>
        public static string GetRecommendedPattern(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Image:
                    return "<Scene>_<Recorder>_<Take>_<Frame>";
                    
                case RecorderSettingsType.Movie:
                    return "<Scene>_<Recorder>_<Take>";
                    
                case RecorderSettingsType.AOV:
                    return "<Scene>_<Take>_<Frame>"; // Frame wildcard is required for AOV sequences
                    
                case RecorderSettingsType.Alembic:
                case RecorderSettingsType.FBX:
                    return "<Scene>_<Recorder>_<Take>";
                    
                case RecorderSettingsType.Animation:
                    return "<Scene>_Animation_<Take>";
                    
                default:
                    return "<Scene>_<Recorder>_<Take>";
            }
        }
        
        /// <summary>
        /// Show wildcard help popup
        /// </summary>
        public static void ShowWildcardHelp()
        {
            EditorUtility.DisplayDialog(
                "Unity Recorder Wildcards",
                GetWildcardHelpText(),
                "OK"
            );
        }
        
        /// <summary>
        /// Draw wildcard help GUI
        /// </summary>
        public static void DrawWildcardHelpGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Name", GUILayout.Width(100));
            
            // Add help button
            if (GUILayout.Button("?", GUILayout.Width(20)))
            {
                ShowWildcardHelp();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Validate filename pattern
        /// </summary>
        public static bool ValidateFilenamePattern(string pattern, RecorderSettingsType type, out string error)
        {
            error = "";
            
            if (string.IsNullOrEmpty(pattern))
            {
                error = "Filename pattern cannot be empty";
                return false;
            }
            
            // Check for image sequence without frame wildcard
            if (type == RecorderSettingsType.Image && !pattern.Contains("<Frame>"))
            {
                error = "Image sequence must include <Frame> wildcard to avoid overwriting files";
                return false;
            }
            
            // Check for invalid characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (pattern.Contains(c.ToString()) && c != '<' && c != '>')
                {
                    error = $"Filename contains invalid character: {c}";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get example output for a filename pattern
        /// </summary>
        public static string GetExampleOutput(string pattern, RecorderSettingsType type)
        {
            var context = new WildcardContext(1, 1920, 1080);
            context.SceneName = "SampleScene";
            context.TimelineName = "MyTimeline";
            
            string example = WildcardProcessor.ProcessWildcards(pattern, context);
            
            // Add appropriate extension
            switch (type)
            {
                case RecorderSettingsType.Image:
                    example = example.Replace("<Frame>", "0001") + ".png";
                    break;
                case RecorderSettingsType.Movie:
                    example += ".mp4";
                    break;
                case RecorderSettingsType.AOV:
                    example += "_AOV_Depth_0001.exr";
                    break;
                case RecorderSettingsType.Alembic:
                    example += ".abc";
                    break;
                case RecorderSettingsType.Animation:
                    example += ".anim";
                    break;
                case RecorderSettingsType.FBX:
                    example += ".fbx";
                    break;
            }
            
            return example;
        }
    }
}