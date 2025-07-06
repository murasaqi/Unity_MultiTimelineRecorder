using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Recorder;

namespace BatchRenderingTool.Debug
{
    /// <summary>
    /// Unity Recorderの機能をチェックするツール
    /// </summary>
    public class UnityRecorderCapabilityChecker : EditorWindow
    {
        private Vector2 scrollPosition;
        private string debugInfo = "";
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Unity Recorder Capabilities")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityRecorderCapabilityChecker>("Recorder Capabilities");
            window.CheckCapabilities();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.HelpBox("Unity Recorder Capability Checker", MessageType.Info);
            
            if (GUILayout.Button("Refresh Capabilities", GUILayout.Height(30)))
            {
                CheckCapabilities();
            }
            
            EditorGUILayout.Space();
            
            // Display debug info
            EditorGUILayout.TextArea(debugInfo, GUILayout.ExpandHeight(true));
            
            EditorGUILayout.EndScrollView();
        }
        
        private void CheckCapabilities()
        {
            debugInfo = "=== Unity Recorder Capabilities ===\n\n";
            
            // 1. Unity Recorder version
            debugInfo += "1. Unity Recorder Info:\n";
            var recorderAssembly = typeof(RecorderSettings).Assembly;
            debugInfo += $"   - Assembly: {recorderAssembly.GetName().Name}\n";
            debugInfo += $"   - Version: {recorderAssembly.GetName().Version}\n";
            
            // Get Unity Recorder package info
            var packageInfo = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .FirstOrDefault(p => p.name == "com.unity.recorder");
            if (packageInfo != null)
            {
                debugInfo += $"   - Package Version: {packageInfo.version}\n";
            }
            
            // 2. Available RecorderSettings types
            debugInfo += "\n2. Available RecorderSettings Types:\n";
            var recorderSettingsTypes = recorderAssembly.GetTypes()
                .Where(t => typeof(RecorderSettings).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();
            
            foreach (var type in recorderSettingsTypes)
            {
                debugInfo += $"   - {type.Name}";
                
                // Check if it's AOV related
                if (type.Name.Contains("AOV") || type.Name.Contains("Aov"))
                {
                    debugInfo += " <-- AOV FOUND!";
                }
                
                debugInfo += "\n";
            }
            
            debugInfo += $"\n   Total: {recorderSettingsTypes.Count} recorder types\n";
            
            // 3. Check for HDRP specific recorders
            debugInfo += "\n3. HDRP-Related Recorder Types:\n";
            var hdrpRelatedTypes = recorderSettingsTypes
                .Where(t => t.Name.Contains("HDRP") || t.Name.Contains("HD") || 
                           t.Name.Contains("AOV") || t.Name.Contains("Render"))
                .ToList();
            
            if (hdrpRelatedTypes.Any())
            {
                foreach (var type in hdrpRelatedTypes)
                {
                    debugInfo += $"   - {type.FullName}\n";
                    
                    // Check for AOV-specific methods or properties
                    var aovProperties = type.GetProperties()
                        .Where(p => p.Name.Contains("AOV") || p.Name.Contains("Pass"))
                        .ToList();
                    
                    if (aovProperties.Any())
                    {
                        debugInfo += "     AOV Properties:\n";
                        foreach (var prop in aovProperties)
                        {
                            debugInfo += $"       - {prop.Name} ({prop.PropertyType.Name})\n";
                        }
                    }
                }
            }
            else
            {
                debugInfo += "   - No HDRP-specific recorder types found\n";
            }
            
            // 4. Check for Input Settings types
            debugInfo += "\n4. Input Settings Types:\n";
            var inputSettingsTypes = recorderAssembly.GetTypes()
                .Where(t => t.Name.EndsWith("InputSettings") && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();
            
            foreach (var type in inputSettingsTypes)
            {
                debugInfo += $"   - {type.Name}\n";
            }
            
            // 5. Check for Render Texture Input Settings
            debugInfo += "\n5. Render Texture Related Input Settings:\n";
            var renderTextureInputType = recorderAssembly.GetType("UnityEditor.Recorder.Input.RenderTextureInputSettings");
            if (renderTextureInputType != null)
            {
                debugInfo += $"   - RenderTextureInputSettings found ✓\n";
                
                // Check properties
                var properties = renderTextureInputType.GetProperties()
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToList();
                
                debugInfo += "     Properties:\n";
                foreach (var prop in properties)
                {
                    debugInfo += $"       - {prop.Name} ({prop.PropertyType.Name})\n";
                }
            }
            else
            {
                debugInfo += "   - RenderTextureInputSettings NOT found ✗\n";
            }
            
            // 6. Check ImageRecorderSettings for AOV capabilities
            debugInfo += "\n6. ImageRecorderSettings AOV Capabilities:\n";
            var imageRecorderType = typeof(ImageRecorderSettings);
            var aovRelatedMembers = imageRecorderType.GetMembers()
                .Where(m => m.Name.Contains("AOV") || m.Name.Contains("Pass") || 
                           m.Name.Contains("Layer") || m.Name.Contains("Render"))
                .ToList();
            
            if (aovRelatedMembers.Any())
            {
                foreach (var member in aovRelatedMembers)
                {
                    debugInfo += $"   - {member.MemberType}: {member.Name}\n";
                }
            }
            else
            {
                debugInfo += "   - No AOV-related members found in ImageRecorderSettings\n";
            }
            
            // 7. Alternative approach for AOV
            debugInfo += "\n7. Alternative AOV Approach:\n";
            debugInfo += "   Unity Recorder may support AOV through:\n";
            debugInfo += "   - Multiple ImageRecorderSettings instances\n";
            debugInfo += "   - Custom RenderTexture targets\n";
            debugInfo += "   - Camera target texture switching\n";
            debugInfo += "   - HDRP Custom Pass integration\n";
            
            // 8. Recommendation
            debugInfo += "\n8. Recommendation:\n";
            if (!recorderSettingsTypes.Any(t => t.Name.Contains("AOV")))
            {
                debugInfo += "   ⚠️ No dedicated AOVRecorderSettings found\n";
                debugInfo += "   ✓ Use ImageRecorderSettings with custom render textures\n";
                debugInfo += "   ✓ Implement AOV through HDRP Custom Pass + Recorder\n";
            }
            
            Repaint();
        }
    }
}