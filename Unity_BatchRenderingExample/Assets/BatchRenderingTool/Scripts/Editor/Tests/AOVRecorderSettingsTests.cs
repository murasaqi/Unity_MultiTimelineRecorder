using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class AOVRecorderSettingsTests
    {
        [Test]
        public void AOVTypeInfo_IsHDRPAvailable_ReturnsCorrectValue()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] IsHDRPAvailable - テスト開始");
            
            bool isAvailable = AOVTypeInfo.IsHDRPAvailable();
            
            // このプロジェクトはHDRP 17.0.3を使用しているため、HDRPは常に利用可能
            Assert.IsTrue(isAvailable, "HDRP should be available in this HDRP project");
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] HDRP available: {isAvailable}");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] IsHDRPAvailable - テスト完了");
        }
        
        [Test]
        public void AOVTypeInfo_GetInfo_ReturnsValidInfoForAllTypes()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] GetInfo_ReturnsValidInfoForAllTypes - テスト開始");
            
            var aovTypes = System.Enum.GetValues(typeof(AOVType));
            int validInfoCount = 0;
            
            foreach (AOVType aovType in aovTypes)
            {
                if (aovType == AOVType.None) continue;
                
                var info = AOVTypeInfo.GetInfo(aovType);
                if (info != null)
                {
                    Assert.IsNotNull(info.DisplayName, $"DisplayName should not be null for {aovType}");
                    Assert.IsNotNull(info.Description, $"Description should not be null for {aovType}");
                    Assert.IsNotNull(info.Category, $"Category should not be null for {aovType}");
                    Assert.IsTrue(info.RequiresHDRP, $"RequiresHDRP should be true for {aovType}");
                    validInfoCount++;
                    
                    UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] {aovType}: {info.DisplayName} - {info.Category}");
                }
            }
            
            Assert.Greater(validInfoCount, 0, "At least one AOV type should have valid info");
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] Valid info found for {validInfoCount} AOV types");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] GetInfo_ReturnsValidInfoForAllTypes - テスト完了");
        }
        
        [Test]
        public void AOVTypeInfo_GetAOVsByCategory_ReturnsGroupedAOVs()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] GetAOVsByCategory - テスト開始");
            
            var aovsByCategory = AOVTypeInfo.GetAOVsByCategory();
            
            Assert.IsNotNull(aovsByCategory, "GetAOVsByCategory should return a valid dictionary");
            Assert.Greater(aovsByCategory.Count, 0, "Should have at least one category");
            
            // 期待されるカテゴリを確認
            var expectedCategories = new[] { "Geometry", "Material", "Lighting", "Additional" };
            
            foreach (var category in expectedCategories)
            {
                if (aovsByCategory.ContainsKey(category))
                {
                    Assert.Greater(aovsByCategory[category].Count, 0, $"Category '{category}' should have at least one AOV type");
                    UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] Category '{category}' has {aovsByCategory[category].Count} AOV types");
                }
            }
            
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] GetAOVsByCategory - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_Validate_WithoutHDRP_ReturnsFalse()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithoutHDRP - テスト開始");
            
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.Depth | AOVType.Normal,
                width = 1920,
                height = 1080,
                frameRate = 24
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                Assert.IsFalse(isValid, "Should be invalid without HDRP");
                Assert.IsTrue(errorMessage.Contains("HDRP"), "Error message should mention HDRP requirement");
            }
            else
            {
                Assert.IsTrue(isValid, "Should be valid with HDRP");
                Assert.IsTrue(string.IsNullOrEmpty(errorMessage), "Error message should be empty with HDRP");
            }
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] Validation result: {isValid}, Message: {errorMessage}");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithoutHDRP - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_Validate_WithNoSelectedAOVs_ReturnsFalse()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithNoSelectedAOVs - テスト開始");
            
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.None,
                width = 1920,
                height = 1080,
                frameRate = 24
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Should be invalid with no selected AOVs");
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] エラーメッセージ: {errorMessage}");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithNoSelectedAOVs - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_Validate_WithInvalidResolution_ReturnsFalse()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithInvalidResolution - テスト開始");
            
            // Skip if HDRP is not available
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                Assert.Ignore("HDRP is not installed, skipping resolution validation test");
                return;
            }
            
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.Depth,
                width = 0,
                height = 0,
                frameRate = 24
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Should be invalid with zero resolution");
            Assert.IsTrue(errorMessage.Contains("resolution") || errorMessage.Contains("Invalid resolution"), $"Error message should mention resolution. Actual: '{errorMessage}'");
            
            // 最大解像度テスト
            config.width = 10000;
            config.height = 10000;
            isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Should be invalid with excessive resolution");
            Assert.IsTrue(errorMessage.Contains("exceeds maximum"), "Error message should mention maximum resolution");
            
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithInvalidResolution - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_Validate_WithCustomPassButNoName_ReturnsFalse()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithCustomPassButNoName - テスト開始");
            
            // Skip if HDRP is not available
            if (!AOVTypeInfo.IsHDRPAvailable())
            {
                Assert.Ignore("HDRP is not installed, skipping custom pass validation test");
                return;
            }
            
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.CustomPass,
                width = 1920,
                height = 1080,
                frameRate = 24,
                customPassName = "" // 空
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Should be invalid with CustomPass selected but no name provided");
            Assert.IsTrue(errorMessage.Contains("Custom pass name") || errorMessage.Contains("custom pass name"), $"Error message should mention custom pass name. Actual: '{errorMessage}'");
            
            // 名前を設定して再テスト
            config.customPassName = "MyCustomPass";
            isValid = config.Validate(out errorMessage);
            
            // HDRPがない場合はHDRPエラーになるため、その場合は別のエラーを期待
            if (AOVTypeInfo.IsHDRPAvailable())
            {
                Assert.IsTrue(isValid, "Should be valid with custom pass name provided");
            }
            
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Validate_WithCustomPassButNoName - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_GetSelectedAOVsList_ReturnsCorrectList()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] GetSelectedAOVsList - テスト開始");
            
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.Depth | AOVType.Normal | AOVType.Albedo
            };
            
            var selectedList = config.GetSelectedAOVsList();
            
            Assert.IsNotNull(selectedList, "Selected list should not be null");
            Assert.AreEqual(3, selectedList.Count, "Should have 3 selected AOVs");
            Assert.IsTrue(selectedList.Contains(AOVType.Depth), "Should contain Depth");
            Assert.IsTrue(selectedList.Contains(AOVType.Normal), "Should contain Normal");
            Assert.IsTrue(selectedList.Contains(AOVType.Albedo), "Should contain Albedo");
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] Selected AOVs: {string.Join(", ", selectedList)}");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] GetSelectedAOVsList - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_Clone_CreatesIdenticalCopy()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Clone_CreatesIdenticalCopy - テスト開始");
            
            var original = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.Depth | AOVType.Normal,
                outputFormat = AOVOutputFormat.EXR32,
                compressionEnabled = false,
                width = 2048,
                height = 1080,
                frameRate = 30,
                capFrameRate = false,
                flipVertical = true,
                customPassName = "TestPass"
            };
            
            var clone = original.Clone();
            
            Assert.IsNotNull(clone, "Clone should not be null");
            Assert.AreNotSame(original, clone, "Clone should be a different instance");
            Assert.AreEqual(original.selectedAOVs, clone.selectedAOVs, "Selected AOVs should match");
            Assert.AreEqual(original.outputFormat, clone.outputFormat, "Output format should match");
            Assert.AreEqual(original.compressionEnabled, clone.compressionEnabled, "Compression enabled should match");
            Assert.AreEqual(original.width, clone.width, "Width should match");
            Assert.AreEqual(original.height, clone.height, "Height should match");
            Assert.AreEqual(original.frameRate, clone.frameRate, "Frame rate should match");
            Assert.AreEqual(original.capFrameRate, clone.capFrameRate, "Cap frame rate should match");
            Assert.AreEqual(original.flipVertical, clone.flipVertical, "Flip vertical should match");
            Assert.AreEqual(original.customPassName, clone.customPassName, "Custom pass name should match");
            
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Clone_CreatesIdenticalCopy - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_Presets_ReturnValidConfigurations()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Presets_ReturnValidConfigurations - テスト開始");
            
            // Compositingプリセット
            var compositing = AOVRecorderSettingsConfig.Presets.GetCompositing();
            Assert.IsNotNull(compositing, "Compositing preset should not be null");
            Assert.IsTrue(compositing.selectedAOVs.HasFlag(AOVType.Albedo), "Compositing should include Albedo");
            Assert.IsTrue(compositing.selectedAOVs.HasFlag(AOVType.DirectDiffuse), "Compositing should include DirectDiffuse");
            Assert.AreEqual(AOVOutputFormat.EXR16, compositing.outputFormat, "Compositing should use EXR16");
            
            // GeometryOnlyプリセット
            var geometry = AOVRecorderSettingsConfig.Presets.GetGeometryOnly();
            Assert.IsNotNull(geometry, "GeometryOnly preset should not be null");
            Assert.IsTrue(geometry.selectedAOVs.HasFlag(AOVType.Depth), "GeometryOnly should include Depth");
            Assert.IsTrue(geometry.selectedAOVs.HasFlag(AOVType.Normal), "GeometryOnly should include Normal");
            Assert.AreEqual(AOVOutputFormat.EXR32, geometry.outputFormat, "GeometryOnly should use EXR32");
            
            // LightingOnlyプリセット
            var lighting = AOVRecorderSettingsConfig.Presets.GetLightingOnly();
            Assert.IsNotNull(lighting, "LightingOnly preset should not be null");
            Assert.IsTrue(lighting.selectedAOVs.HasFlag(AOVType.DirectDiffuse), "LightingOnly should include DirectDiffuse");
            Assert.IsFalse(lighting.selectedAOVs.HasFlag(AOVType.Albedo), "LightingOnly should not include Albedo");
            
            // MaterialPropertiesプリセット
            var material = AOVRecorderSettingsConfig.Presets.GetMaterialProperties();
            Assert.IsNotNull(material, "MaterialProperties preset should not be null");
            Assert.IsTrue(material.selectedAOVs.HasFlag(AOVType.Albedo), "MaterialProperties should include Albedo");
            Assert.IsTrue(material.selectedAOVs.HasFlag(AOVType.Metal), "MaterialProperties should include Metal");
            
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] All presets validated successfully");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] Presets_ReturnValidConfigurations - テスト完了");
        }
        
        [Test]
        public void AOVRecorderSettingsConfig_CreateAOVRecorderSettings_CreatesMultipleSettings()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] CreateAOVRecorderSettings - テスト開始");
            
            var config = new AOVRecorderSettingsConfig
            {
                selectedAOVs = AOVType.Depth | AOVType.Normal | AOVType.Albedo,
                outputFormat = AOVOutputFormat.EXR16,
                width = 1920,
                height = 1080,
                frameRate = 24
            };
            
            var settingsList = config.CreateAOVRecorderSettings("TestAOV");
            
            Assert.IsNotNull(settingsList, "Settings list should not be null");
            Assert.AreEqual(3, settingsList.Count, "Should create 3 recorder settings for 3 selected AOVs");
            
            foreach (var settings in settingsList)
            {
                Assert.IsNotNull(settings, "Each settings should not be null");
                Assert.IsTrue(settings.name.Contains("AOV"), "Settings name should contain 'AOV'");
                Assert.AreEqual(24, settings.FrameRate, "Frame rate should match config");
                Assert.IsTrue(settings.Enabled, "Settings should be enabled");
            }
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] Created {settingsList.Count} AOV recorder settings");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] CreateAOVRecorderSettings - テスト完了");
        }
        
        [Test]
        public void AOVType_Flags_CanCombineMultipleTypes()
        {
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] AOVType_Flags - テスト開始");
            
            var combined = AOVType.Depth | AOVType.Normal | AOVType.Albedo;
            
            Assert.IsTrue(combined.HasFlag(AOVType.Depth), "Should include Depth");
            Assert.IsTrue(combined.HasFlag(AOVType.Normal), "Should include Normal");
            Assert.IsTrue(combined.HasFlag(AOVType.Albedo), "Should include Albedo");
            Assert.IsFalse(combined.HasFlag(AOVType.Specular), "Should not include Specular");
            
            // ビット演算のテスト
            Assert.AreNotEqual(AOVType.None, combined, "Combined should not equal None");
            Assert.AreNotEqual(0, (int)combined, "Combined should have non-zero value");
            
            UnityEngine.Debug.Log($"[AOVRecorderSettingsTests] Combined flags value: {combined} ({(int)combined})");
            UnityEngine.Debug.Log("[AOVRecorderSettingsTests] AOVType_Flags - テスト完了");
        }
    }
}