using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using System.Collections.Generic;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class AlembicRecorderSettingsTests
    {
        private GameObject testGameObject;
        private GameObject testChild;
        
        [SetUp]
        public void Setup()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Setup - テスト開始");
            
            // テスト用GameObjectを作成
            testGameObject = new GameObject("TestAlembicObject");
            testChild = new GameObject("TestChild");
            testChild.transform.parent = testGameObject.transform;
            
            // テスト用コンポーネントを追加
            var meshFilter = testGameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = UnityEngine.Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            testGameObject.AddComponent<MeshRenderer>();
            
            testChild.AddComponent<Camera>();
        }
        
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] TearDown - テスト終了");
            
            if (testGameObject != null)
                GameObject.DestroyImmediate(testGameObject);
        }
        
        [Test]
        public void AlembicPackage_IsAvailable()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] AlembicPackage_IsAvailable - テスト開始");
            
            bool isAvailable = AlembicExportInfo.IsAlembicPackageAvailable();
            UnityEngine.Debug.Log($"[AlembicRecorderSettingsTests] Alembic package available: {isAvailable}");
            
            if (!isAvailable)
            {
                Assert.Ignore("Alembic package is not installed. Skipping test.");
            }
            else
            {
                Assert.IsTrue(isAvailable, "Alembic package should be available");
            }
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] AlembicPackage_IsAvailable - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_Validate_WithValidConfig_ReturnsTrue()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithValidConfig - テスト開始");
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform,
                exportScope = AlembicExportScope.EntireScene,
                frameRate = 24f,
                scaleFactor = 1f,
                samplesPerFrame = 1
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                Assert.IsFalse(isValid, "Should be invalid when Alembic package is not available");
                Assert.IsTrue(errorMessage.Contains("Alembic package"), "Error message should mention Alembic package");
            }
            else
            {
                Assert.IsTrue(isValid, "Valid config should return true");
                Assert.IsTrue(string.IsNullOrEmpty(errorMessage), "Error message should be empty for valid config");
            }
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithValidConfig - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_Validate_WithInvalidFrameRate_ReturnsFalse()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithInvalidFrameRate - テスト開始");
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.MeshRenderer,
                frameRate = 0f // Invalid
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Config with zero frame rate should be invalid");
            Assert.IsFalse(string.IsNullOrEmpty(errorMessage), "Error message should not be empty");
            
            UnityEngine.Debug.Log($"[AlembicRecorderSettingsTests] エラーメッセージ: {errorMessage}");
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithInvalidFrameRate - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_Validate_WithNoExportTargets_ReturnsFalse()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithNoExportTargets - テスト開始");
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.None, // Invalid
                frameRate = 24f
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Config with no export targets should be invalid");
            Assert.IsTrue(errorMessage.Contains("export target"), "Error message should mention export target");
            
            UnityEngine.Debug.Log($"[AlembicRecorderSettingsTests] エラーメッセージ: {errorMessage}");
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithNoExportTargets - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_Validate_WithTargetGameObjectScope_RequiresGameObject()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithTargetGameObjectScope - テスト開始");
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.MeshRenderer,
                exportScope = AlembicExportScope.TargetGameObject,
                targetGameObject = null, // Invalid when scope is TargetGameObject
                frameRate = 24f
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Config with TargetGameObject scope but no GameObject should be invalid");
            Assert.IsTrue(errorMessage.Contains("Target GameObject"), "Error message should mention Target GameObject");
            
            // 有効なGameObjectを設定
            config.targetGameObject = testGameObject;
            isValid = config.Validate(out errorMessage);
            
            if (AlembicExportInfo.IsAlembicPackageAvailable())
            {
                Assert.IsTrue(isValid, "Config with valid GameObject should be valid");
            }
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Validate_WithTargetGameObjectScope - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_GetExportObjects_EntireScene_ReturnsAllRootObjects()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetExportObjects_EntireScene - テスト開始");
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportScope = AlembicExportScope.EntireScene
            };
            
            var objects = config.GetExportObjects();
            Assert.IsNotNull(objects, "Export objects list should not be null");
            Assert.Greater(objects.Count, 0, "Should return at least one object from scene");
            
            UnityEngine.Debug.Log($"[AlembicRecorderSettingsTests] Found {objects.Count} root objects in scene");
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetExportObjects_EntireScene - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_GetExportObjects_TargetGameObject_ReturnsSpecificObject()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetExportObjects_TargetGameObject - テスト開始");
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportScope = AlembicExportScope.TargetGameObject,
                targetGameObject = testGameObject
            };
            
            var objects = config.GetExportObjects();
            Assert.IsNotNull(objects, "Export objects list should not be null");
            Assert.AreEqual(1, objects.Count, "Should return exactly one object");
            Assert.AreEqual(testGameObject, objects[0], "Should return the target GameObject");
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetExportObjects_TargetGameObject - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_Clone_CreatesIdenticalCopy()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Clone_CreatesIdenticalCopy - テスト開始");
            
            var original = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Camera,
                exportScope = AlembicExportScope.TargetGameObject,
                targetGameObject = testGameObject,
                frameRate = 30f,
                scaleFactor = 2f,
                handedness = AlembicHandedness.Right,
                exportUVs = false,
                exportNormals = false
            };
            
            var clone = original.Clone();
            
            Assert.IsNotNull(clone, "Clone should not be null");
            Assert.AreNotSame(original, clone, "Clone should be a different instance");
            Assert.AreEqual(original.exportTargets, clone.exportTargets, "Export targets should match");
            Assert.AreEqual(original.exportScope, clone.exportScope, "Export scope should match");
            Assert.AreEqual(original.targetGameObject, clone.targetGameObject, "Target GameObject should match");
            Assert.AreEqual(original.frameRate, clone.frameRate, "Frame rate should match");
            Assert.AreEqual(original.scaleFactor, clone.scaleFactor, "Scale factor should match");
            Assert.AreEqual(original.handedness, clone.handedness, "Handedness should match");
            Assert.AreEqual(original.exportUVs, clone.exportUVs, "Export UVs should match");
            Assert.AreEqual(original.exportNormals, clone.exportNormals, "Export normals should match");
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] Clone_CreatesIdenticalCopy - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_GetPreset_AnimationExport_ReturnsCorrectConfig()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetPreset_AnimationExport - テスト開始");
            
            var config = AlembicRecorderSettingsConfig.GetPreset(AlembicExportPreset.AnimationExport);
            
            Assert.IsNotNull(config, "Animation export preset should not be null");
            Assert.IsTrue((config.exportTargets & AlembicExportTargets.Transform) != 0, "Should include Transform export");
            Assert.AreEqual(AlembicExportScope.SelectedHierarchy, config.exportScope, "Should use SelectedHierarchy scope");
            Assert.AreEqual(1, config.samplesPerFrame, "Should use 1 sample per frame");
            Assert.IsFalse(config.assumeUnchangedTopology, "Should not assume unchanged topology for animation");
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetPreset_AnimationExport - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_GetPreset_FullSceneExport_ReturnsCorrectConfig()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetPreset_FullSceneExport - テスト開始");
            
            var config = AlembicRecorderSettingsConfig.GetPreset(AlembicExportPreset.FullSceneExport);
            
            Assert.IsNotNull(config, "Full scene export preset should not be null");
            Assert.IsTrue((config.exportTargets & AlembicExportTargets.MeshRenderer) != 0, "Should include MeshRenderer");
            Assert.IsTrue((config.exportTargets & AlembicExportTargets.Camera) != 0, "Should include Camera");
            Assert.IsTrue((config.exportTargets & AlembicExportTargets.Light) != 0, "Should include Light");
            Assert.AreEqual(AlembicExportScope.EntireScene, config.exportScope, "Should use EntireScene scope");
            Assert.IsTrue(config.includeInactiveMeshes, "Should include inactive meshes");
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] GetPreset_FullSceneExport - テスト完了");
        }
        
        [Test]
        public void AlembicRecorderSettingsConfig_CreateAlembicRecorderSettings_CreatesValidSettings()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] CreateAlembicRecorderSettings - テスト開始");
            
            // Skip test if Alembic package is not available
            if (!AlembicExportInfo.IsAlembicPackageAvailable())
            {
                Assert.Ignore("Alembic package is not installed");
                return;
            }
            
            var config = new AlembicRecorderSettingsConfig
            {
                exportTargets = AlembicExportTargets.MeshRenderer,
                frameRate = 30f,
                scaleFactor = 1f
            };
            
            var settings = config.CreateAlembicRecorderSettings("TestAlembic");
            
            Assert.IsNotNull(settings, "Created settings should not be null");
            Assert.AreEqual("TestAlembic", settings.name, "Settings name should match");
            Assert.AreEqual(30f, settings.FrameRate, "Frame rate should match config");
            Assert.IsTrue(settings.Enabled, "Settings should be enabled");
            Assert.AreEqual(RecordMode.Manual, settings.RecordMode, "Should use Manual record mode");
            
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] CreateAlembicRecorderSettings - テスト完了");
        }
        
        [Test]
        public void AlembicExportTargets_Flags_CanCombineMultipleTargets()
        {
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] AlembicExportTargets_Flags - テスト開始");
            
            var combined = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Camera | AlembicExportTargets.Transform;
            
            Assert.IsTrue((combined & AlembicExportTargets.MeshRenderer) != 0, "Should include MeshRenderer");
            Assert.IsTrue((combined & AlembicExportTargets.Camera) != 0, "Should include Camera");
            Assert.IsTrue((combined & AlembicExportTargets.Transform) != 0, "Should include Transform");
            Assert.IsFalse((combined & AlembicExportTargets.Light) != 0, "Should not include Light");
            
            UnityEngine.Debug.Log($"[AlembicRecorderSettingsTests] Combined flags value: {combined}");
            UnityEngine.Debug.Log("[AlembicRecorderSettingsTests] AlembicExportTargets_Flags - テスト完了");
        }
    }
}