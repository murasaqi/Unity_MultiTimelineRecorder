using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;

namespace BatchRenderingTool.DebugTools
{
    /// <summary>
    /// FBX Recorder Clipの正しい設定方法を分析するツール
    /// </summary>
    public class FBXRecorderAnalyzer : EditorWindow
    {
        private RecorderClip referenceClip;
        private string analysisResult = "";
        private Vector2 scrollPosition;
        
        [MenuItem("Window/Batch Rendering Tool/Debug/FBX Recorder Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<FBXRecorderAnalyzer>("FBX Recorder Analyzer");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("FBX Recorder Clip Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("Drop a working FBX Recorder Clip from Timeline here to analyze its structure", MessageType.Info);
            
            referenceClip = (RecorderClip)EditorGUILayout.ObjectField("Reference Recorder Clip", referenceClip, typeof(RecorderClip), false);
            
            if (GUILayout.Button("Analyze Recorder Clip", GUILayout.Height(30)))
            {
                AnalyzeRecorderClip();
            }
            
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(analysisResult, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        
        private void AnalyzeRecorderClip()
        {
            analysisResult = "";
            
            if (referenceClip == null)
            {
                analysisResult = "Please assign a Recorder Clip to analyze.";
                return;
            }
            
            try
            {
                AddLog("=== FBX Recorder Clip Analysis ===\n");
                
                // Basic info
                AddLog($"Clip Type: {referenceClip.GetType().FullName}");
                
                // Get settings
                var settings = referenceClip.settings;
                if (settings != null)
                {
                    AddLog($"\nSettings Type: {settings.GetType().FullName}");
                    AddLog($"Settings Name: {settings.name}");
                    AddLog($"Enabled: {settings.Enabled}");
                    AddLog($"Record Mode: {settings.RecordMode}");
                    AddLog($"Frame Rate: {settings.FrameRate}");
                    AddLog($"Cap Frame Rate: {settings.CapFrameRate}");
                    
                    // Analyze FBX specific settings
                    if (settings.GetType().Name.Contains("FbxRecorderSettings"))
                    {
                        AnalyzeFBXSettings(settings);
                    }
                }
                else
                {
                    AddLog("\nSettings is NULL!");
                }
                
                // Analyze all fields
                AddLog("\n=== RecorderClip Fields ===");
                var clipType = referenceClip.GetType();
                var fields = clipType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(referenceClip);
                        AddLog($"{field.Name}: {(value != null ? value.ToString() : "null")} ({field.FieldType.Name})");
                    }
                    catch (Exception e)
                    {
                        AddLog($"{field.Name}: [Error reading: {e.Message}]");
                    }
                }
                
                // Analyze properties
                AddLog("\n=== RecorderClip Properties ===");
                var properties = clipType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (prop.CanRead)
                    {
                        try
                        {
                            var value = prop.GetValue(referenceClip);
                            AddLog($"{prop.Name}: {(value != null ? value.ToString() : "null")} ({prop.PropertyType.Name})");
                        }
                        catch (Exception e)
                        {
                            AddLog($"{prop.Name}: [Error reading: {e.Message}]");
                        }
                    }
                }
                
                AddLog("\n=== Analysis Complete ===");
            }
            catch (Exception e)
            {
                AddLog($"\nError during analysis: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void AnalyzeFBXSettings(RecorderSettings settings)
        {
            AddLog("\n=== FBX Specific Settings ===");
            
            var settingsType = settings.GetType();
            
            // Check for AnimationInputSettings
            var animInputProp = settingsType.GetProperty("AnimationInputSettings");
            if (animInputProp != null)
            {
                var animInput = animInputProp.GetValue(settings);
                if (animInput != null)
                {
                    AddLog($"\nAnimationInputSettings Type: {animInput.GetType().FullName}");
                    
                    var animType = animInput.GetType();
                    var animProps = animType.GetProperties();
                    foreach (var prop in animProps)
                    {
                        if (prop.CanRead)
                        {
                            try
                            {
                                var value = prop.GetValue(animInput);
                                AddLog($"  {prop.Name}: {(value != null ? value.ToString() : "null")}");
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    AddLog("\nAnimationInputSettings is NULL");
                }
            }
            
            // Check all FBX-specific properties
            var fbxProps = settingsType.GetProperties();
            foreach (var prop in fbxProps)
            {
                if (prop.Name.Contains("Export") || prop.Name.Contains("Transfer") || prop.Name.Contains("Geometry"))
                {
                    try
                    {
                        var value = prop.GetValue(settings);
                        AddLog($"\n{prop.Name}: {(value != null ? value.ToString() : "null")}");
                    }
                    catch { }
                }
            }
            
            // Check InputSettings field
            var inputSettingsField = settingsType.GetField("m_InputSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inputSettingsField != null)
            {
                var inputSettings = inputSettingsField.GetValue(settings);
                AddLog($"\nm_InputSettings: {(inputSettings != null ? inputSettings.GetType().FullName : "null")}");
            }
        }
        
        private void AddLog(string message)
        {
            analysisResult += message + "\n";
        }
    }
}