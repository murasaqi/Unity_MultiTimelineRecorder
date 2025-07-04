using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using System.Reflection;

namespace BatchRenderingTool
{
    public static class CompilationTest
    {
        [MenuItem("Window/Batch Rendering Tool/Test Compilation")]
        public static void TestMovieRecorderSettingsAPI()
        {
            Debug.Log("=== Testing MovieRecorderSettings API ===");
            
            // Create an instance
            var settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            
            // Get the type
            var type = settings.GetType();
            Debug.Log($"Type: {type.FullName}");
            
            // List all public properties
            Debug.Log("\nPublic Properties:");
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                Debug.Log($"  - {prop.Name} ({prop.PropertyType.Name})");
            }
            
            // Check for VideoBitrateMode property
            var videoBitrateProp = type.GetProperty("VideoBitrateMode");
            if (videoBitrateProp != null)
            {
                Debug.Log($"\nVideoBitrateMode property found: {videoBitrateProp.PropertyType.FullName}");
            }
            else
            {
                Debug.Log("\nVideoBitrateMode property NOT found in MovieRecorderSettings");
            }
            
            // Check for VideoBitRateMode property (different casing)
            var videoBitRateProp = type.GetProperty("VideoBitRateMode");
            if (videoBitRateProp != null)
            {
                Debug.Log($"\nVideoBitRateMode property found: {videoBitRateProp.PropertyType.FullName}");
            }
            else
            {
                Debug.Log("\nVideoBitRateMode property NOT found in MovieRecorderSettings");
            }
            
            // Clean up
            Object.DestroyImmediate(settings);
            
            Debug.Log("\n=== Test Complete ===");
        }
    }
}