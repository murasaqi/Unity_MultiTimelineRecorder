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
        public void CreateImageSequenceRecorderSettings_WithPNG_CreatesValidSettings()
        {
            Debug.Log("CreateImageSequenceRecorderSettings_WithPNG_CreatesValidSettings - テスト開始");
            
            var settings = RecorderSettingsFactory.CreateImageSequenceRecorderSettings(
                "TestOutput",
                1920,
                1080,
                30,
                RecorderSettingsHelper.ImageFormat.PNG
            );
            
            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<ImageSequenceRecorderSettings>(settings);
            
            var imageSettings = settings as ImageSequenceRecorderSettings;
            Assert.AreEqual("TestOutput/<Frame>", imageSettings.OutputFile);
            Assert.AreEqual(ImageRecorderSettings.ImageRecorderOutputFormat.PNG, imageSettings.OutputFormat);
            Assert.AreEqual(30, settings.FrameRate);
            
            Debug.Log("CreateImageSequenceRecorderSettings_WithPNG_CreatesValidSettings - テスト完了");
        }

        [Test]
        public void CreateImageSequenceRecorderSettings_WithJPG_CreatesValidSettings()
        {
            Debug.Log("CreateImageSequenceRecorderSettings_WithJPG_CreatesValidSettings - テスト開始");
            
            var settings = RecorderSettingsFactory.CreateImageSequenceRecorderSettings(
                "TestOutput",
                1280,
                720,
                24,
                RecorderSettingsHelper.ImageFormat.JPG
            );
            
            Assert.IsNotNull(settings);
            var imageSettings = settings as ImageSequenceRecorderSettings;
            Assert.AreEqual(ImageRecorderSettings.ImageRecorderOutputFormat.JPEG, imageSettings.OutputFormat);
            
            Debug.Log("CreateImageSequenceRecorderSettings_WithJPG_CreatesValidSettings - テスト完了");
        }

        [Test]
        public void CreateImageSequenceRecorderSettings_WithEXR_CreatesValidSettings()
        {
            Debug.Log("CreateImageSequenceRecorderSettings_WithEXR_CreatesValidSettings - テスト開始");
            
            var settings = RecorderSettingsFactory.CreateImageSequenceRecorderSettings(
                "TestOutput",
                3840,
                2160,
                60,
                RecorderSettingsHelper.ImageFormat.EXR
            );
            
            Assert.IsNotNull(settings);
            var imageSettings = settings as ImageSequenceRecorderSettings;
            Assert.AreEqual(ImageRecorderSettings.ImageRecorderOutputFormat.EXR, imageSettings.OutputFormat);
            
            Debug.Log("CreateImageSequenceRecorderSettings_WithEXR_CreatesValidSettings - テスト完了");
        }

        [Test]
        public void CreateImageSequenceRecorderSettings_SetsCorrectResolution()
        {
            Debug.Log("CreateImageSequenceRecorderSettings_SetsCorrectResolution - テスト開始");
            
            int testWidth = 2560;
            int testHeight = 1440;
            
            var settings = RecorderSettingsFactory.CreateImageSequenceRecorderSettings(
                "TestOutput",
                testWidth,
                testHeight,
                30,
                RecorderSettingsHelper.ImageFormat.PNG
            );
            
            var imageSettings = settings as ImageSequenceRecorderSettings;
            var inputSettings = imageSettings.ImageInputSettings as GameViewInputSettings;
            
            Assert.IsNotNull(inputSettings);
            Assert.AreEqual(testWidth, inputSettings.OutputWidth);
            Assert.AreEqual(testHeight, inputSettings.OutputHeight);
            
            Debug.Log($"CreateImageSequenceRecorderSettings_SetsCorrectResolution - 解像度: {testWidth}x{testHeight}");
            Debug.Log("CreateImageSequenceRecorderSettings_SetsCorrectResolution - テスト完了");
        }

        [Test]
        public void CreateImageSequenceRecorderSettings_SetsCorrectFrameRate()
        {
            Debug.Log("CreateImageSequenceRecorderSettings_SetsCorrectFrameRate - テスト開始");
            
            int[] testFrameRates = { 24, 30, 60, 120 };
            
            foreach (var frameRate in testFrameRates)
            {
                var settings = RecorderSettingsFactory.CreateImageSequenceRecorderSettings(
                    "TestOutput",
                    1920,
                    1080,
                    frameRate,
                    RecorderSettingsHelper.ImageFormat.PNG
                );
                
                Assert.AreEqual(frameRate, settings.FrameRate);
                Debug.Log($"CreateImageSequenceRecorderSettings_SetsCorrectFrameRate - フレームレート: {frameRate}fps 確認OK");
            }
            
            Debug.Log("CreateImageSequenceRecorderSettings_SetsCorrectFrameRate - テスト完了");
        }

        [Test]
        public void CreateImageSequenceRecorderSettings_HandlesSpecialCharactersInPath()
        {
            Debug.Log("CreateImageSequenceRecorderSettings_HandlesSpecialCharactersInPath - テスト開始");
            
            string[] testPaths = {
                "Test Output",
                "Test-Output",
                "Test_Output",
                "TestOutput123",
                "Test.Output"
            };
            
            foreach (var path in testPaths)
            {
                var settings = RecorderSettingsFactory.CreateImageSequenceRecorderSettings(
                    path,
                    1920,
                    1080,
                    30,
                    RecorderSettingsHelper.ImageFormat.PNG
                );
                
                Assert.IsNotNull(settings);
                var imageSettings = settings as ImageSequenceRecorderSettings;
                Assert.IsTrue(imageSettings.OutputFile.StartsWith(path));
                Debug.Log($"CreateImageSequenceRecorderSettings_HandlesSpecialCharactersInPath - パス: '{path}' 確認OK");
            }
            
            Debug.Log("CreateImageSequenceRecorderSettings_HandlesSpecialCharactersInPath - テスト完了");
        }
    }
}