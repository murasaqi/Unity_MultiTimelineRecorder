using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BatchRenderingTool
{
    /// <summary>
    /// Processes wildcards in file paths for Unity Recorder
    /// </summary>
    public static class WildcardProcessor
    {
        public static class Wildcards
        {
            public const string Take = "<Take>";
            public const string Scene = "<Scene>";
            public const string Frame = "<Frame>";
            public const string Time = "<Time>";
            public const string Resolution = "<Resolution>";
            public const string Date = "<Date>";
            public const string Product = "<Product>";
            public const string AOVType = "<AOVType>";
        }
        
        /// <summary>
        /// Process wildcards in a file path template
        /// </summary>
        public static string ProcessWildcards(string template, WildcardContext context)
        {
            if (string.IsNullOrEmpty(template))
                return template;
                
            string result = template;
            
            // Replace wildcards
            result = result.Replace(Wildcards.Take, $"Take{context.TakeNumber:D2}");
            result = result.Replace(Wildcards.Scene, context.SceneName);
            result = result.Replace(Wildcards.Frame, context.FrameNumber.HasValue ? context.FrameNumber.Value.ToString("D4") : "0001");
            result = result.Replace(Wildcards.Time, DateTime.Now.ToString("HHmmss"));
            result = result.Replace(Wildcards.Date, DateTime.Now.ToString("yyyyMMdd"));
            result = result.Replace(Wildcards.Resolution, $"{context.Width}x{context.Height}");
            result = result.Replace(Wildcards.Product, Application.productName);
            result = result.Replace(Wildcards.AOVType, context.AOVType ?? "AOV");
            
            return result;
        }
        
        /// <summary>
        /// Simple version of ProcessWildcards for convenience
        /// </summary>
        public static string ProcessWildcards(string template, string sceneName, string frameNumber, int takeNumber)
        {
            var context = new WildcardContext
            {
                SceneName = sceneName ?? SceneManager.GetActiveScene().name,
                FrameNumber = frameNumber != null ? int.Parse(frameNumber) : (int?)null,
                TakeNumber = takeNumber
            };
            
            return ProcessWildcards(template, context);
        }
        
        /// <summary>
        /// Process wildcards for AOV outputs
        /// </summary>
        public static string ProcessAOVWildcards(string template, string sceneName, string frameNumber, int takeNumber, string aovType)
        {
            var context = new WildcardContext
            {
                SceneName = sceneName ?? SceneManager.GetActiveScene().name,
                FrameNumber = frameNumber != null ? int.Parse(frameNumber) : (int?)null,
                TakeNumber = takeNumber,
                AOVType = aovType
            };
            
            return ProcessWildcards(template, context);
        }
        
        /// <summary>
        /// Get default file name template for recorder type
        /// </summary>
        public static string GetDefaultTemplate(RecorderSettingsType type)
        {
            switch (type)
            {
                case RecorderSettingsType.Image:
                    return $"Recordings/{Wildcards.Scene}_{Wildcards.Take}/{Wildcards.Scene}_{Wildcards.Take}_{Wildcards.Frame}";
                    
                case RecorderSettingsType.Movie:
                    return $"Recordings/{Wildcards.Scene}_{Wildcards.Take}";
                    
                case RecorderSettingsType.Animation:
                    return $"Assets/Animations/{Wildcards.Scene}_{Wildcards.Take}";
                    
                case RecorderSettingsType.Alembic:
                    return $"Recordings/{Wildcards.Scene}_{Wildcards.Take}";
                    
                case RecorderSettingsType.AOV:
                    return $"Recordings/{Wildcards.Scene}_{Wildcards.Take}_{Wildcards.AOVType}/{Wildcards.AOVType}_{Wildcards.Frame}";
                    
                default:
                    return $"Recordings/{Wildcards.Scene}_{Wildcards.Take}/{Wildcards.Scene}_{Wildcards.Take}";
            }
        }
        
        /// <summary>
        /// Extract base path from a template (excluding filename)
        /// </summary>
        public static string ExtractBasePath(string template)
        {
            if (string.IsNullOrEmpty(template))
                return "";
                
            int lastSlash = template.LastIndexOf('/');
            if (lastSlash >= 0)
                return template.Substring(0, lastSlash);
                
            return "";
        }
        
        /// <summary>
        /// Extract filename from a template (excluding path)
        /// </summary>
        public static string ExtractFileName(string template)
        {
            if (string.IsNullOrEmpty(template))
                return "";
                
            int lastSlash = template.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < template.Length - 1)
                return template.Substring(lastSlash + 1);
                
            return template;
        }
        
        /// <summary>
        /// Validate a file name template
        /// </summary>
        public static bool ValidateTemplate(string template, out string error)
        {
            error = null;
            
            if (string.IsNullOrEmpty(template))
            {
                error = "File name template cannot be empty";
                return false;
            }
            
            // Check for invalid characters (excluding wildcards)
            string testPath = ProcessWildcards(template, new WildcardContext
            {
                TakeNumber = 1,
                SceneName = "TestScene",
                FrameNumber = 1,
                Width = 1920,
                Height = 1080
            });
            
            try
            {
                // Test if the path is valid
                Path.GetFullPath(testPath);
            }
            catch (Exception e)
            {
                error = $"Invalid path: {e.Message}";
                return false;
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Context for wildcard processing
    /// </summary>
    public class WildcardContext
    {
        public int TakeNumber { get; set; } = 1;
        public string SceneName { get; set; } = SceneManager.GetActiveScene().name;
        public int? FrameNumber { get; set; }
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public string AOVType { get; set; }
        
        public WildcardContext() { }
        
        public WildcardContext(int takeNumber, int width, int height)
        {
            TakeNumber = takeNumber;
            Width = width;
            Height = height;
        }
    }
}