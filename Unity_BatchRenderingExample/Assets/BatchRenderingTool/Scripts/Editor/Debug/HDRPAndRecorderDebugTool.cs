using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace BatchRenderingTool.Debug
{
    /// <summary>
    /// HDRPとRecorderの検出状況をデバッグするためのツール
    /// </summary>
    public class HDRPAndRecorderDebugTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private string debugInfo = "";
        
        [MenuItem("Window/Batch Rendering Tool/Debug/HDRP and Recorder Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<HDRPAndRecorderDebugTool>("HDRP/Recorder Debug");
            window.CheckStatus();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.HelpBox("HDRP and Unity Recorder Detection Debug Tool", MessageType.Info);
            
            if (GUILayout.Button("Refresh Status", GUILayout.Height(30)))
            {
                CheckStatus();
            }
            
            EditorGUILayout.Space();
            
            // Display debug info
            EditorGUILayout.TextArea(debugInfo, GUILayout.ExpandHeight(true));
            
            EditorGUILayout.EndScrollView();
        }
        
        private void CheckStatus()
        {
            debugInfo = "=== HDRP and Recorder Debug Info ===\n\n";
            
            // 1. Check preprocessor directives
            debugInfo += "1. Preprocessor Directives:\n";
            #if UNITY_PIPELINE_HDRP
            debugInfo += "   - UNITY_PIPELINE_HDRP: DEFINED ✓\n";
            #else
            debugInfo += "   - UNITY_PIPELINE_HDRP: NOT DEFINED ✗\n";
            #endif
            
            #if UNITY_HDRP
            debugInfo += "   - UNITY_HDRP: DEFINED ✓\n";
            #else
            debugInfo += "   - UNITY_HDRP: NOT DEFINED ✗\n";
            #endif
            
            #if HDRP_AVAILABLE
            debugInfo += "   - HDRP_AVAILABLE: DEFINED ✓\n";
            #else
            debugInfo += "   - HDRP_AVAILABLE: NOT DEFINED ✗\n";
            #endif
            
            // 2. Check HDRP assembly
            debugInfo += "\n2. HDRP Assembly Check:\n";
            var hdrpType = Type.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipeline, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdrpType != null)
            {
                debugInfo += "   - HDRenderPipeline type found ✓\n";
                debugInfo += $"   - Assembly: {hdrpType.Assembly.FullName}\n";
            }
            else
            {
                debugInfo += "   - HDRenderPipeline type NOT found ✗\n";
            }
            
            // 3. Check current render pipeline
            debugInfo += "\n3. Current Render Pipeline:\n";
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline != null)
            {
                debugInfo += $"   - Pipeline Asset: {currentPipeline.name}\n";
                debugInfo += $"   - Type: {currentPipeline.GetType().FullName}\n";
                debugInfo += $"   - Is HDRP: {currentPipeline.GetType().FullName.Contains("HighDefinition")} ";
                debugInfo += currentPipeline.GetType().FullName.Contains("HighDefinition") ? "✓\n" : "✗\n";
            }
            else
            {
                debugInfo += "   - No render pipeline asset set ✗\n";
            }
            
            // 4. Check loaded assemblies for HDRP
            debugInfo += "\n4. HDRP Assemblies in Domain:\n";
            var hdrpAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.Contains("HighDefinition"))
                .ToList();
            
            if (hdrpAssemblies.Any())
            {
                foreach (var assembly in hdrpAssemblies)
                {
                    debugInfo += $"   - {assembly.GetName().Name} ✓\n";
                }
            }
            else
            {
                debugInfo += "   - No HDRP assemblies found ✗\n";
            }
            
            // 5. Check Unity Recorder types
            debugInfo += "\n5. Unity Recorder Types:\n";
            
            // Check for standard recorder types
            var recorderTypes = new[]
            {
                "UnityEditor.Recorder.ImageRecorderSettings",
                "UnityEditor.Recorder.MovieRecorderSettings",
                "UnityEditor.Recorder.AnimationRecorderSettings",
                "UnityEditor.Recorder.AOVRecorderSettings",
                "UnityEditor.Recorder.HDRPRecorderSettings"
            };
            
            foreach (var typeName in recorderTypes)
            {
                var type = Type.GetType(typeName + ", Unity.Recorder.Editor");
                if (type != null)
                {
                    debugInfo += $"   - {typeName}: Found ✓\n";
                }
                else
                {
                    debugInfo += $"   - {typeName}: NOT Found ✗\n";
                }
            }
            
            // 6. Search for all RecorderSettings types
            debugInfo += "\n6. All RecorderSettings Types Found:\n";
            var recorderAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Unity.Recorder.Editor");
            
            if (recorderAssembly != null)
            {
                var recorderSettingsTypes = recorderAssembly.GetTypes()
                    .Where(t => t.Name.EndsWith("RecorderSettings") && !t.IsAbstract)
                    .OrderBy(t => t.Name)
                    .ToList();
                
                foreach (var type in recorderSettingsTypes)
                {
                    debugInfo += $"   - {type.Name}\n";
                }
                
                debugInfo += $"\n   Total RecorderSettings types: {recorderSettingsTypes.Count}\n";
            }
            else
            {
                debugInfo += "   - Unity.Recorder.Editor assembly not found ✗\n";
            }
            
            // 7. Check package versions
            debugInfo += "\n7. Package Versions:\n";
            var packageInfo = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Where(p => p.name.Contains("render-pipelines") || p.name.Contains("recorder"))
                .ToList();
            
            foreach (var package in packageInfo)
            {
                debugInfo += $"   - {package.name}: v{package.version}\n";
            }
            
            // 8. Unity version
            debugInfo += $"\n8. Unity Version: {Application.unityVersion}\n";
            
            // 9. Final recommendation
            debugInfo += "\n9. Recommendation:\n";
            if (currentPipeline != null && currentPipeline.GetType().FullName.Contains("HighDefinition"))
            {
                debugInfo += "   - HDRP is properly installed and active ✓\n";
                debugInfo += "   - Use runtime type checking instead of preprocessor directives\n";
            }
            else
            {
                debugInfo += "   - HDRP is not active or not properly configured ✗\n";
            }
            
            Repaint();
        }
    }
}