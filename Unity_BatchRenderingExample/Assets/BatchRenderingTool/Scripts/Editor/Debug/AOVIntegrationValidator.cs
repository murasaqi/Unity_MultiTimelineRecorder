using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace BatchRenderingTool.DebugTools
{
    /// <summary>
    /// AOV統合の検証ツール
    /// </summary>
    public static class AOVIntegrationValidator
    {
        [MenuItem("Window/Batch Rendering Tool/Debug/Validate AOV Integration")]
        public static void ValidateAOVIntegration()
        {
            UnityEngine.Debug.Log("=== AOV Integration Validation ===");
            UnityEngine.Debug.Log($"実行時刻: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            UnityEngine.Debug.Log("");
            
            bool allTestsPassed = true;
            
            // 1. HDRPプロジェクト判定
            allTestsPassed &= ValidateHDRPProject();
            UnityEngine.Debug.Log("");
            
            // 2. AOVパッケージ検証
            allTestsPassed &= ValidateAOVPackages();
            UnityEngine.Debug.Log("");
            
            // 3. AOVタイプの自動検出
            allTestsPassed &= ValidateAOVTypeDetection();
            UnityEngine.Debug.Log("");
            
            // 4. RecorderSettings作成テスト
            allTestsPassed &= ValidateAOVRecorderSettingsCreation();
            UnityEngine.Debug.Log("");
            
            // 5. SingleTimelineRenderer統合テスト
            allTestsPassed &= ValidateSingleTimelineRendererAOVIntegration();
            UnityEngine.Debug.Log("");
            
            // 6. EXRサポート検証
            allTestsPassed &= ValidateEXRSupport();
            UnityEngine.Debug.Log("");
            
            // 結果サマリー
            UnityEngine.Debug.Log("=== Validation Summary ===");
            if (allTestsPassed)
            {
                UnityEngine.Debug.Log("<color=green>✓ All AOV validation tests passed!</color>");
                EditorUtility.DisplayDialog("AOV Integration Validation", 
                    "All validation tests passed!\n\nAOV integration is ready for use.", 
                    "OK");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ Some AOV validation tests failed. Check the console for details.");
                EditorUtility.DisplayDialog("AOV Integration Validation", 
                    "Some validation tests failed.\n\nPlease check the console for details.", 
                    "OK");
            }
        }
        
        /// <summary>
        /// HDRPプロジェクトかどうかを検証
        /// </summary>
        private static bool ValidateHDRPProject()
        {
            UnityEngine.Debug.Log("--- 1. HDRP Project Check ---");
            
            bool isHDRP = false;
            string pipelineInfo = "None";
            
            // GraphicsSettingsからレンダーパイプラインを取得
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP != null)
            {
                pipelineInfo = currentRP.GetType().FullName;
                if (pipelineInfo.Contains("HighDefinition"))
                {
                    isHDRP = true;
                }
            }
            
            // プリプロセッサーディレクティブもチェック
            #if UNITY_PIPELINE_HDRP
            if (!isHDRP)
            {
                isHDRP = true;
                UnityEngine.Debug.LogWarning("HDRP preprocessor is defined but Graphics Settings doesn't show HDRP");
            }
            #endif
            
            if (isHDRP)
            {
                UnityEngine.Debug.Log($"<color=green>✓ HDRP is active</color>");
                UnityEngine.Debug.Log($"  Pipeline: {pipelineInfo}");
                
                // HDRPアセットの設定を確認
                if (currentRP != null)
                {
                    var hdrpAssetType = currentRP.GetType();
                    var frameSettingsField = hdrpAssetType.GetField("m_RenderingPathDefaultFrameSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (frameSettingsField != null)
                    {
                        UnityEngine.Debug.Log("  HDRP Asset settings are accessible");
                    }
                }
                
                return true;
            }
            else
            {
                UnityEngine.Debug.LogError("✗ HDRP is NOT active");
                UnityEngine.Debug.Log($"  Current Pipeline: {pipelineInfo}");
                UnityEngine.Debug.LogError("  AOV Recorder requires HDRP. Please configure your project to use HDRP.");
                return false;
            }
        }
        
        /// <summary>
        /// AOV関連パッケージを検証
        /// </summary>
        private static bool ValidateAOVPackages()
        {
            UnityEngine.Debug.Log("--- 2. AOV Package Validation ---");
            
            bool hasUnityRecorder = false;
            bool hasHDRPAOVSupport = false;
            
            // Unity Recorderパッケージをチェック
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Unity.Recorder"))
                {
                    hasUnityRecorder = true;
                    UnityEngine.Debug.Log($"<color=green>✓ Unity Recorder found</color>");
                    UnityEngine.Debug.Log($"  Assembly: {assembly.FullName}");
                    break;
                }
            }
            
            if (!hasUnityRecorder)
            {
                UnityEngine.Debug.LogError("✗ Unity Recorder package not found");
                UnityEngine.Debug.LogError("  Please install Unity Recorder package via Package Manager");
                return false;
            }
            
            // HDRP AOVサポートをチェック
            var aovTypes = new string[]
            {
                "UnityEngine.Rendering.HighDefinition.AOVRequest",
                "UnityEngine.Rendering.HighDefinition.AOVRequestData",
                "UnityEngine.Rendering.HighDefinition.DebugFullScreen"
            };
            
            foreach (var typeName in aovTypes)
            {
                var type = Type.GetType(typeName + ", Unity.RenderPipelines.HighDefinition.Runtime");
                if (type != null)
                {
                    hasHDRPAOVSupport = true;
                    UnityEngine.Debug.Log($"<color=green>✓ HDRP AOV type found: {type.Name}</color>");
                }
            }
            
            if (!hasHDRPAOVSupport)
            {
                UnityEngine.Debug.LogWarning("⚠ HDRP AOV types not fully detected");
                UnityEngine.Debug.Log("  This may be due to Unity version differences");
            }
            
            return hasUnityRecorder;
        }
        
        /// <summary>
        /// AOVタイプの自動検出を検証
        /// </summary>
        private static bool ValidateAOVTypeDetection()
        {
            UnityEngine.Debug.Log("--- 3. AOV Type Detection Test ---");
            
            try
            {
                // カスタムAOVタイプを検証
                var aovTypes = Enum.GetValues(typeof(AOVType));
                int validTypes = 0;
                
                foreach (AOVType aovType in aovTypes)
                {
                    if (aovType == AOVType.None) continue;
                    
                    var info = AOVTypeInfo.GetInfo(aovType);
                    if (info != null)
                    {
                        validTypes++;
                    }
                }
                
                UnityEngine.Debug.Log($"<color=green>✓ Detected {validTypes} AOV types</color>");
                
                // カテゴリ別の検証
                var aovsByCategory = AOVTypeInfo.GetAOVsByCategory();
                foreach (var category in aovsByCategory)
                {
                    UnityEngine.Debug.Log($"  {category.Key}: {category.Value.Count} types");
                }
                
                return validTypes > 0;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"✗ AOV type detection failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// AOVRecorderSettings作成を検証
        /// </summary>
        private static bool ValidateAOVRecorderSettingsCreation()
        {
            UnityEngine.Debug.Log("--- 4. AOV RecorderSettings Creation Test ---");
            
            try
            {
                var config = new AOVRecorderSettingsConfig
                {
                    selectedAOVs = AOVType.Depth | AOVType.Normal,
                    outputFormat = AOVOutputFormat.EXR16,
                    width = 1920,
                    height = 1080,
                    frameRate = 24
                };
                
                // バリデーション
                string errorMessage;
                bool isValid = config.Validate(out errorMessage);
                
                if (!isValid)
                {
                    UnityEngine.Debug.LogWarning($"⚠ AOV config validation failed: {errorMessage}");
                    if (errorMessage.Contains("HDRP"))
                    {
                        UnityEngine.Debug.Log("  This is expected if HDRP is not active");
                        return true; // HDRPがない場合は警告レベルにする
                    }
                    return false;
                }
                
                // RecorderSettings作成
                var settingsList = config.CreateAOVRecorderSettings("ValidationTest");
                
                if (settingsList != null && settingsList.Count > 0)
                {
                    UnityEngine.Debug.Log($"<color=green>✓ Successfully created {settingsList.Count} AOV recorder settings</color>");
                    
                    foreach (var settings in settingsList)
                    {
                        UnityEngine.Debug.Log($"  Created: {settings.name}");
                        // クリーンアップ
                        UnityEngine.Object.DestroyImmediate(settings);
                    }
                    
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError("✗ Failed to create AOV recorder settings");
                    return false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"✗ Exception during AOV settings creation: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// SingleTimelineRendererのAOV統合を検証
        /// </summary>
        private static bool ValidateSingleTimelineRendererAOVIntegration()
        {
            UnityEngine.Debug.Log("--- 5. SingleTimelineRenderer AOV Integration Test ---");
            
            try
            {
                // RecorderSettingsTypeにAOVがあるか確認
                var recorderTypes = Enum.GetValues(typeof(RecorderSettingsType));
                bool hasAOV = false;
                
                foreach (RecorderSettingsType type in recorderTypes)
                {
                    if (type == RecorderSettingsType.AOV)
                    {
                        hasAOV = true;
                        break;
                    }
                }
                
                if (hasAOV)
                {
                    UnityEngine.Debug.Log($"<color=green>✓ SingleTimelineRenderer supports AOV recorder type</color>");
                    
                    // RecorderSettingsFactoryがAOVをサポートしているか確認
                    var factoryType = typeof(RecorderSettingsFactory);
                    var methods = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    
                    bool hasCreateAOVMethod = methods.Any(m => m.Name.Contains("AOV"));
                    if (hasCreateAOVMethod)
                    {
                        UnityEngine.Debug.Log($"<color=green>✓ RecorderSettingsFactory has AOV support methods</color>");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("⚠ RecorderSettingsFactory may need AOV method implementation");
                    }
                    
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError("✗ RecorderSettingsType enum does not include AOV");
                    return false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"✗ Exception during SingleTimelineRenderer validation: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// EXRサポートを検証
        /// </summary>
        private static bool ValidateEXRSupport()
        {
            UnityEngine.Debug.Log("--- 6. EXR Support Validation ---");
            
            try
            {
                // Unity RecorderのEXRサポートを確認
                var imageRecorderType = Type.GetType("UnityEditor.Recorder.ImageRecorderSettings, Unity.Recorder.Editor");
                if (imageRecorderType != null)
                {
                    var outputFormatType = imageRecorderType.GetNestedType("ImageRecorderOutputFormat");
                    if (outputFormatType != null && outputFormatType.IsEnum)
                    {
                        var enumValues = Enum.GetNames(outputFormatType);
                        bool hasEXR = enumValues.Any(v => v.Contains("EXR"));
                        
                        if (hasEXR)
                        {
                            UnityEngine.Debug.Log($"<color=green>✓ EXR format is supported in Unity Recorder</color>");
                            return true;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("✗ EXR format not found in ImageRecorderOutputFormat");
                            return false;
                        }
                    }
                }
                
                UnityEngine.Debug.LogWarning("⚠ Could not verify EXR support");
                return true; // 警告レベル
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"✗ Exception during EXR validation: {ex.Message}");
                return false;
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Quick AOV Test")]
        public static void QuickAOVTest()
        {
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                EditorUtility.DisplayDialog("AOV Not Available", 
                    "AOV Recorder requires HDRP (High Definition Render Pipeline).\n\n" +
                    "Please configure your project to use HDRP.", 
                    "OK");
                return;
            }
            
            try
            {
                UnityEngine.Debug.Log("=== Quick AOV Test ===");
                
                // テスト用の設定を作成
                var config = AOVRecorderSettingsConfig.Presets.GetGeometryOnly();
                config.width = 1920;
                config.height = 1080;
                config.frameRate = 24;
                
                // RecorderSettingsを作成
                var settingsList = config.CreateAOVRecorderSettings("QuickAOVTest");
                
                if (settingsList == null || settingsList.Count == 0)
                {
                    UnityEngine.Debug.LogError("Failed to create AOV recorder settings");
                    return;
                }
                
                UnityEngine.Debug.Log($"Created {settingsList.Count} AOV recorder settings:");
                foreach (var settings in settingsList)
                {
                    UnityEngine.Debug.Log($"  - {settings.name}");
                    
                    // 出力設定
                    RecorderSettingsHelper.SetOutputPath(settings, "Assets/AOVTest", settings.name);
                    settings.StartFrame = 0;
                    settings.EndFrame = 0; // 単一フレーム
                }
                
                EditorUtility.DisplayDialog("Quick AOV Test Ready", 
                    $"AOV recorders have been configured.\n\n" +
                    $"Output: Assets/AOVTest/\n" +
                    $"AOVs: Depth, Normal, Motion Vectors\n" +
                    $"Format: EXR32\n\n" +
                    "Enter Play Mode to start recording.", 
                    "OK");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Quick AOV test failed: {ex}");
                EditorUtility.DisplayDialog("Quick Test Failed", 
                    $"Failed to create AOV test:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Analyze HDRP Settings")]
        public static void AnalyzeHDRPSettings()
        {
            UnityEngine.Debug.Log("=== HDRP Settings Analysis ===");
            
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP == null || !currentRP.GetType().FullName.Contains("HighDefinition"))
            {
                UnityEngine.Debug.LogError("HDRP is not active");
                return;
            }
            
            UnityEngine.Debug.Log($"HDRP Asset Type: {currentRP.GetType().FullName}");
            
            // HDRPアセットの設定を分析
            var assetType = currentRP.GetType();
            var fields = assetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            UnityEngine.Debug.Log("\n--- Relevant HDRP Settings for AOV ---");
            
            foreach (var field in fields)
            {
                if (field.Name.Contains("AOV") || 
                    field.Name.Contains("CustomPass") || 
                    field.Name.Contains("FrameSettings") ||
                    field.Name.Contains("motionVector"))
                {
                    try
                    {
                        var value = field.GetValue(currentRP);
                        UnityEngine.Debug.Log($"{field.Name}: {value}");
                    }
                    catch
                    {
                        UnityEngine.Debug.Log($"{field.Name}: (unable to read)");
                    }
                }
            }
            
            UnityEngine.Debug.Log("\n=== Analysis Complete ===");
        }
    }
}