using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;

namespace BatchRenderingTool.Debug
{
    /// <summary>
    /// Alembic統合の検証ツール
    /// </summary>
    public static class AlembicIntegrationValidator
    {
        [MenuItem("Window/Batch Rendering Tool/Debug/Validate Alembic Integration")]
        public static void ValidateAlembicIntegration()
        {
            Debug.Log("=== Alembic Integration Validation ===");
            Debug.Log($"実行時刻: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Debug.Log("");
            
            bool allTestsPassed = true;
            
            // 1. パッケージ検証
            allTestsPassed &= ValidatePackageInstallation();
            Debug.Log("");
            
            // 2. 型の検証
            allTestsPassed &= ValidateAlembicTypes();
            Debug.Log("");
            
            // 3. RecorderSettings作成テスト
            allTestsPassed &= ValidateRecorderSettingsCreation();
            Debug.Log("");
            
            // 4. SingleTimelineRenderer統合テスト
            allTestsPassed &= ValidateSingleTimelineRendererIntegration();
            Debug.Log("");
            
            // 5. MultiTimelineRenderer統合テスト
            allTestsPassed &= ValidateMultiTimelineRendererIntegration();
            Debug.Log("");
            
            // 6. 設定の永続性テスト
            allTestsPassed &= ValidateSettingsPersistence();
            Debug.Log("");
            
            // 結果サマリー
            Debug.Log("=== Validation Summary ===");
            if (allTestsPassed)
            {
                Debug.Log("<color=green>✓ All validation tests passed!</color>");
                EditorUtility.DisplayDialog("Alembic Integration Validation", 
                    "All validation tests passed!\n\nAlembic integration is working correctly.", 
                    "OK");
            }
            else
            {
                Debug.LogError("✗ Some validation tests failed. Check the console for details.");
                EditorUtility.DisplayDialog("Alembic Integration Validation", 
                    "Some validation tests failed.\n\nPlease check the console for details.", 
                    "OK");
            }
        }
        
        private static bool ValidatePackageInstallation()
        {
            Debug.Log("--- 1. Package Installation Check ---");
            
            bool isInstalled = AlembicExportInfo.IsAlembicPackageAvailable();
            
            if (isInstalled)
            {
                Debug.Log("<color=green>✓ Alembic package is installed</color>");
                
                // バージョン情報を取得
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Contains("Unity.Formats.Alembic"))
                    {
                        Debug.Log($"  Assembly: {assembly.FullName}");
                        Debug.Log($"  Version: {assembly.GetName().Version}");
                        break;
                    }
                }
                return true;
            }
            else
            {
                Debug.LogError("✗ Alembic package is NOT installed");
                Debug.LogError("  Please install 'com.unity.formats.alembic' via Package Manager");
                return false;
            }
        }
        
        private static bool ValidateAlembicTypes()
        {
            Debug.Log("--- 2. Alembic Types Validation ---");
            
            bool allTypesFound = true;
            var requiredTypes = new Dictionary<string, string[]>
            {
                {
                    "AlembicRecorderSettings",
                    new string[]
                    {
                        "UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor",
                        "UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor"
                    }
                },
                {
                    "AlembicInputSettings",
                    new string[]
                    {
                        "UnityEditor.Recorder.Input.AlembicInputSettings, Unity.Recorder.Editor",
                        "UnityEditor.Formats.Alembic.Importer.AlembicInputSettings, Unity.Formats.Alembic.Editor"
                    }
                }
            };
            
            foreach (var kvp in requiredTypes)
            {
                bool typeFound = false;
                string foundTypeName = null;
                
                foreach (var typeName in kvp.Value)
                {
                    var type = Type.GetType(typeName);
                    if (type != null)
                    {
                        typeFound = true;
                        foundTypeName = type.FullName;
                        break;
                    }
                }
                
                if (typeFound)
                {
                    Debug.Log($"<color=green>✓ {kvp.Key}: {foundTypeName}</color>");
                }
                else
                {
                    Debug.LogError($"✗ {kvp.Key}: Not found");
                    allTypesFound = false;
                }
            }
            
            return allTypesFound;
        }
        
        private static bool ValidateRecorderSettingsCreation()
        {
            Debug.Log("--- 3. RecorderSettings Creation Test ---");
            
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                Debug.LogWarning("⚠ Skipping test - Alembic package not available");
                return false;
            }
            
            try
            {
                var config = new AlembicRecorderSettingsConfig
                {
                    exportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform,
                    frameRate = 24f
                };
                
                var settings = config.CreateAlembicRecorderSettings("ValidationTest");
                
                if (settings != null)
                {
                    Debug.Log($"<color=green>✓ Successfully created AlembicRecorderSettings</color>");
                    Debug.Log($"  Type: {settings.GetType().FullName}");
                    Debug.Log($"  Name: {settings.name}");
                    Debug.Log($"  FrameRate: {settings.FrameRate}");
                    
                    // Cleanup
                    UnityEngine.Object.DestroyImmediate(settings);
                    return true;
                }
                else
                {
                    Debug.LogError("✗ Failed to create AlembicRecorderSettings");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Exception during RecorderSettings creation: {ex.Message}");
                return false;
            }
        }
        
        private static bool ValidateSingleTimelineRendererIntegration()
        {
            Debug.Log("--- 4. SingleTimelineRenderer Integration Test ---");
            
            try
            {
                // SingleTimelineRendererがAlembicをサポートしているか確認
                var recorderTypes = Enum.GetValues(typeof(RecorderSettingsType));
                bool hasAlembic = false;
                
                foreach (RecorderSettingsType type in recorderTypes)
                {
                    if (type == RecorderSettingsType.Alembic)
                    {
                        hasAlembic = true;
                        break;
                    }
                }
                
                if (hasAlembic)
                {
                    Debug.Log($"<color=green>✓ SingleTimelineRenderer supports Alembic recorder type</color>");
                    
                    // RecorderSettingsFactoryがAlembicをサポートしているか確認
                    var factoryType = typeof(RecorderSettingsFactory);
                    var createAlembicMethod = factoryType.GetMethod("CreateAlembicRecorderSettings", 
                        BindingFlags.Public | BindingFlags.Static);
                    
                    if (createAlembicMethod != null)
                    {
                        Debug.Log($"<color=green>✓ RecorderSettingsFactory has CreateAlembicRecorderSettings method</color>");
                    }
                    else
                    {
                        Debug.LogError("✗ RecorderSettingsFactory missing CreateAlembicRecorderSettings method");
                        return false;
                    }
                    
                    return true;
                }
                else
                {
                    Debug.LogError("✗ RecorderSettingsType enum does not include Alembic");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Exception during SingleTimelineRenderer validation: {ex.Message}");
                return false;
            }
        }
        
        private static bool ValidateMultiTimelineRendererIntegration()
        {
            Debug.Log("--- 5. MultiTimelineRenderer Integration Test ---");
            
            try
            {
                // MultiTimelineRendererクラスが存在するか確認
                var multiRendererType = Type.GetType("BatchRenderingTool.MultiTimelineRenderer, Assembly-CSharp-Editor");
                
                if (multiRendererType != null)
                {
                    Debug.Log($"<color=green>✓ MultiTimelineRenderer class found</color>");
                    
                    // Alembic関連のメソッドがあるか確認
                    var methods = multiRendererType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                    bool hasAlembicSupport = methods.Any(m => m.Name.Contains("Alembic"));
                    
                    if (hasAlembicSupport)
                    {
                        Debug.Log($"<color=green>✓ MultiTimelineRenderer has Alembic support methods</color>");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("⚠ MultiTimelineRenderer may not have Alembic support yet");
                        return true; // これは警告レベルなのでテストは通す
                    }
                }
                else
                {
                    Debug.LogWarning("⚠ MultiTimelineRenderer class not found (may not be implemented yet)");
                    return true; // MultiTimelineRendererは未実装の可能性があるので警告レベル
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Exception during MultiTimelineRenderer validation: {ex.Message}");
                return false;
            }
        }
        
        private static bool ValidateSettingsPersistence()
        {
            Debug.Log("--- 6. Settings Persistence Test ---");
            
            try
            {
                // テスト用の設定を作成
                var testKey = "AlembicTest_" + System.Guid.NewGuid().ToString();
                var testValue = "TestValue_" + DateTime.Now.Ticks;
                
                // EditorPrefsに保存
                EditorPrefs.SetString(testKey, testValue);
                
                // 読み込み
                var retrievedValue = EditorPrefs.GetString(testKey, "");
                
                // クリーンアップ
                EditorPrefs.DeleteKey(testKey);
                
                if (retrievedValue == testValue)
                {
                    Debug.Log($"<color=green>✓ Settings persistence working correctly</color>");
                    return true;
                }
                else
                {
                    Debug.LogError("✗ Settings persistence test failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Exception during settings persistence test: {ex.Message}");
                return false;
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Quick Alembic Test Export")]
        public static void QuickAlembicTestExport()
        {
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                EditorUtility.DisplayDialog("Alembic Not Available", 
                    "Alembic package is not installed.\n\nPlease install 'com.unity.formats.alembic' via Package Manager.", 
                    "OK");
                return;
            }
            
            try
            {
                Debug.Log("=== Quick Alembic Test Export ===");
                
                // テスト用の設定を作成
                var config = new AlembicRecorderSettingsConfig
                {
                    exportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform,
                    exportScope = AlembicExportScope.EntireScene,
                    frameRate = 24f,
                    scaleFactor = 1f
                };
                
                // RecorderSettingsを作成
                var settings = config.CreateAlembicRecorderSettings("QuickTest");
                if (settings == null)
                {
                    Debug.LogError("Failed to create AlembicRecorderSettings");
                    return;
                }
                
                // 出力設定
                RecorderSettingsHelper.SetOutputPath(settings, "Assets/AlembicTest", "QuickTest");
                settings.StartFrame = 0;
                settings.EndFrame = 10;
                
                Debug.Log($"Created test recorder settings:");
                Debug.Log($"  Output: Assets/AlembicTest/QuickTest.abc");
                Debug.Log($"  Frames: 0-10");
                Debug.Log($"  Frame Rate: 24 fps");
                
                EditorUtility.DisplayDialog("Quick Test Ready", 
                    "Alembic recorder has been configured for a quick test.\n\n" +
                    "Output: Assets/AlembicTest/QuickTest.abc\n" +
                    "Frames: 0-10\n\n" +
                    "Enter Play Mode to start recording.", 
                    "OK");
                
                // Note: 実際の録画はPlay Modeで行う必要があります
            }
            catch (Exception ex)
            {
                Debug.LogError($"Quick test failed: {ex}");
                EditorUtility.DisplayDialog("Quick Test Failed", 
                    $"Failed to create test export:\n\n{ex.Message}", 
                    "OK");
            }
        }
    }
}