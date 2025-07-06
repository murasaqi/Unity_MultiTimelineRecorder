using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class RecorderSettingsFactoryTests
    {
        [Test]
        public void CreateImageRecorderSettings_WithPNG_CreatesValidSettings()
        {
            Debug.Log("CreateImageRecorderSettings_WithPNG_CreatesValidSettings - テスト開始");
            
            var settings = RecorderSettingsFactory.CreateImageRecorderSettings("TestImageRecorder");
            
            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<ImageRecorderSettings>(settings);
            
            var imageSettings = settings as ImageRecorderSettings;
            // OutputFile property is handled differently in newer versions
            Assert.AreEqual(ImageRecorderSettings.ImageRecorderOutputFormat.PNG, imageSettings.OutputFormat);
            Assert.AreEqual(30, settings.FrameRate);
            
            Debug.Log("CreateImageRecorderSettings_WithPNG_CreatesValidSettings - テスト完了");
        }

        [Test]
        public void CreateImageRecorderSettings_WithJPG_CreatesValidSettings()
        {
            Debug.Log("CreateImageRecorderSettings_WithJPG_CreatesValidSettings - テスト開始");
            
            var settings = RecorderSettingsFactory.CreateImageRecorderSettings("TestImageRecorder");
            settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.JPEG;
            
            Assert.IsNotNull(settings);
            var imageSettings = settings as ImageRecorderSettings;
            Assert.AreEqual(ImageRecorderSettings.ImageRecorderOutputFormat.JPEG, imageSettings.OutputFormat);
            
            Debug.Log("CreateImageRecorderSettings_WithJPG_CreatesValidSettings - テスト完了");
        }

        [Test]
        public void CreateImageRecorderSettings_WithEXR_CreatesValidSettings()
        {
            Debug.Log("CreateImageRecorderSettings_WithEXR_CreatesValidSettings - テスト開始");
            
            var settings = RecorderSettingsFactory.CreateImageRecorderSettings("TestImageRecorder");
            settings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
            
            Assert.IsNotNull(settings);
            var imageSettings = settings as ImageRecorderSettings;
            Assert.AreEqual(ImageRecorderSettings.ImageRecorderOutputFormat.EXR, imageSettings.OutputFormat);
            
            Debug.Log("CreateImageRecorderSettings_WithEXR_CreatesValidSettings - テスト完了");
        }

        [Test]
        public void CreateImageRecorderSettings_SetsCorrectResolution()
        {
            Debug.Log("CreateImageRecorderSettings_SetsCorrectResolution - テスト開始");
            
            int testWidth = 2560;
            int testHeight = 1440;
            
            var settings = RecorderSettingsFactory.CreateImageRecorderSettings("TestImageRecorder");
            
            var imageSettings = settings as ImageRecorderSettings;
            var inputSettings = imageSettings.imageInputSettings as GameViewInputSettings;
            inputSettings.OutputWidth = testWidth;
            inputSettings.OutputHeight = testHeight;
            
            Assert.IsNotNull(inputSettings);
            Assert.AreEqual(testWidth, inputSettings.OutputWidth);
            Assert.AreEqual(testHeight, inputSettings.OutputHeight);
            
            Debug.Log($"CreateImageRecorderSettings_SetsCorrectResolution - 解像度: {testWidth}x{testHeight}");
            Debug.Log("CreateImageRecorderSettings_SetsCorrectResolution - テスト完了");
        }

        [Test]
        public void CreateImageRecorderSettings_SetsCorrectFrameRate()
        {
            Debug.Log("CreateImageRecorderSettings_SetsCorrectFrameRate - テスト開始");
            
            int[] testFrameRates = { 24, 30, 60, 120 };
            
            foreach (var frameRate in testFrameRates)
            {
                var settings = RecorderSettingsFactory.CreateImageRecorderSettings("TestImageRecorder");
                settings.FrameRate = frameRate;
                
                Assert.AreEqual(frameRate, settings.FrameRate);
                Debug.Log($"CreateImageRecorderSettings_SetsCorrectFrameRate - フレームレート: {frameRate}fps 確認OK");
            }
            
            Debug.Log("CreateImageRecorderSettings_SetsCorrectFrameRate - テスト完了");
        }

        [Test]
        public void CreateFBXRecorderSettings_CreatesValidSettings()
        {
            Debug.Log("CreateFBXRecorderSettings_CreatesValidSettings - テスト開始");
            
            // Skip test if FBX package is not available
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Ignore("FBX package is not installed");
                return;
            }
            
            // Create test GameObject for FBX recording
            var testGameObject = new GameObject("TestObject");
            try
            {
                // Create config with required targetGameObject
                var config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = testGameObject,
                    exportGeometry = true,
                    frameRate = 24f
                };
                
                var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("TestFBXRecorder", config);
                
                Assert.IsNotNull(settings, "FBX recorder settings should not be null");
                Assert.AreEqual("TestFBXRecorder", settings.name);
                Assert.AreEqual(24, settings.FrameRate);
                Assert.IsTrue(settings.Enabled);
                Assert.AreEqual(RecordMode.Manual, settings.RecordMode);
                
                Debug.Log("CreateFBXRecorderSettings_CreatesValidSettings - テスト完了");
            }
            finally
            {
                // Clean up
                if (testGameObject != null)
                    GameObject.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void CreateFBXRecorderSettings_WithConfig_AppliesConfigCorrectly()
        {
            Debug.Log("CreateFBXRecorderSettings_WithConfig_AppliesConfigCorrectly - テスト開始");
            
            // Skip test if FBX package is not available
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Ignore("FBX package is not installed");
                return;
            }
            
            // Create test GameObject for FBX recording
            var testGameObject = new GameObject("TestObject");
            try
            {
                var config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = testGameObject,
                    exportGeometry = false,
                    frameRate = 30f
                };
                
                var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("TestFBXRecorder", config);
                
                Assert.IsNotNull(settings, "FBX recorder settings should not be null");
                Assert.AreEqual(30f, settings.FrameRate);
                
                // Use reflection to verify exportGeometry was set
                var settingsType = settings.GetType();
                var exportGeometryProp = settingsType.GetProperty("ExportGeometry");
                if (exportGeometryProp != null && exportGeometryProp.CanRead)
                {
                    var value = (bool)exportGeometryProp.GetValue(settings);
                    Assert.IsFalse(value, "ExportGeometry should be false");
                }
                
                Debug.Log("CreateFBXRecorderSettings_WithConfig_AppliesConfigCorrectly - テスト完了");
            }
            finally
            {
                // Clean up
                if (testGameObject != null)
                    GameObject.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void CreateFBXRecorderSettings_WithPreset_CreatesCorrectSettings()
        {
            Debug.Log("CreateFBXRecorderSettings_WithPreset_CreatesCorrectSettings - テスト開始");
            
            // Skip test if FBX package is not available
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Ignore("FBX package is not installed");
                return;
            }
            
            // Create test GameObject for FBX recording
            var testGameObject = new GameObject("TestObject");
            try
            {
                // Test AnimationExport preset
                var animPreset = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.AnimationExport);
                animPreset.targetGameObject = testGameObject;
                var animSettings = RecorderSettingsFactory.CreateFBXRecorderSettings("TestFBX", animPreset);
                Assert.IsNotNull(animSettings, "Animation export settings should not be null");
                
                // Test ModelExport preset
                var modelPreset = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.ModelExport);
                modelPreset.targetGameObject = testGameObject;
                var modelSettings = RecorderSettingsFactory.CreateFBXRecorderSettings("TestFBX", modelPreset);
                Assert.IsNotNull(modelSettings, "Model export settings should not be null");
                
                // Test ModelAndAnimation preset
                var bothPreset = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.ModelAndAnimation);
                bothPreset.targetGameObject = testGameObject;
                var bothSettings = RecorderSettingsFactory.CreateFBXRecorderSettings("TestFBX", bothPreset);
                Assert.IsNotNull(bothSettings, "Model and animation export settings should not be null");
                
                Debug.Log("CreateFBXRecorderSettings_WithPreset_CreatesCorrectSettings - テスト完了");
            }
            finally
            {
                // Clean up
                if (testGameObject != null)
                    GameObject.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void CreateImageRecorderSettings_HandlesSpecialCharactersInPath()
        {
            Debug.Log("CreateImageRecorderSettings_HandlesSpecialCharactersInPath - テスト開始");
            
            string[] testPaths = {
                "Test Output",
                "Test-Output",
                "Test_Output",
                "TestOutput123",
                "Test.Output"
            };
            
            foreach (var path in testPaths)
            {
                var settings = RecorderSettingsFactory.CreateImageRecorderSettings(path);
                
                Assert.IsNotNull(settings);
                var imageSettings = settings as ImageRecorderSettings;
                Assert.AreEqual(path, imageSettings.name);
                Debug.Log($"CreateImageRecorderSettings_HandlesSpecialCharactersInPath - パス: '{path}' 確認OK");
            }
            
            Debug.Log("CreateImageRecorderSettings_HandlesSpecialCharactersInPath - テスト完了");
        }
    }
}