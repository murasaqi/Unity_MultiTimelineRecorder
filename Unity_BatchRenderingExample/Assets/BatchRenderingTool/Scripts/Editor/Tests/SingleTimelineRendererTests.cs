using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using BatchRenderingTool.Editor;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class SingleTimelineRendererTests
    {
        private GameObject testGameObject;
        private PlayableDirector testDirector;
        private TimelineAsset testTimeline;

        [SetUp]
        public void Setup()
        {
            UnityEngine.Debug.Log("SingleTimelineRendererTests - Setup開始");
            
            // テスト用のGameObjectとPlayableDirectorを作成
            testGameObject = new GameObject("TestDirector");
            testDirector = testGameObject.AddComponent<PlayableDirector>();
            
            // テスト用のTimelineAssetを作成
            testTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            testDirector.playableAsset = testTimeline;
            
            UnityEngine.Debug.Log("SingleTimelineRendererTests - Setup完了");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Debug.Log("SingleTimelineRendererTests - TearDown開始");
            
            // テスト用オブジェクトをクリーンアップ
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testTimeline != null)
            {
                Object.DestroyImmediate(testTimeline);
            }
            
            UnityEngine.Debug.Log("SingleTimelineRendererTests - TearDown完了");
        }

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            UnityEngine.Debug.Log("Constructor_InitializesDefaultValues - テスト開始");
            
            var renderer = ScriptableObject.CreateInstance<SingleTimelineRenderer>();
            
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.filePath);
            Assert.AreEqual("Recordings", renderer.filePath);
            Assert.AreEqual(1920, renderer.OutputWidth);
            Assert.AreEqual(1080, renderer.OutputHeight);
            Assert.AreEqual(24, renderer.FrameRate);
            Assert.AreEqual(RecorderSettingsHelper.ImageFormat.PNG, renderer.ImageFormat);
            
            UnityEngine.Debug.Log("Constructor_InitializesDefaultValues - テスト完了");
            
            // EditorWindowのクリーンアップ
            Object.DestroyImmediate(renderer);
        }

        [Test]
        public void GetAllPlayableDirectors_ReturnsActiveDirectors()
        {
            UnityEngine.Debug.Log("GetAllPlayableDirectors_ReturnsActiveDirectors - テスト開始");
            
            var renderer = ScriptableObject.CreateInstance<SingleTimelineRenderer>();
            var directors = renderer.GetAllPlayableDirectors();
            
            Assert.IsNotNull(directors);
            Assert.Contains(testDirector, directors);
            
            UnityEngine.Debug.Log($"GetAllPlayableDirectors_ReturnsActiveDirectors - 検出されたDirector数: {directors.Count}");
            UnityEngine.Debug.Log("GetAllPlayableDirectors_ReturnsActiveDirectors - テスト完了");
            
            // EditorWindowのクリーンアップ
            Object.DestroyImmediate(renderer);
        }

        [Test]
        public void SetSelectedDirector_UpdatesSelection()
        {
            UnityEngine.Debug.Log("SetSelectedDirector_UpdatesSelection - テスト開始");
            
            var renderer = ScriptableObject.CreateInstance<SingleTimelineRenderer>();
            renderer.SetSelectedDirector(testDirector);
            
            Assert.AreEqual(testDirector, renderer.GetSelectedDirector());
            
            UnityEngine.Debug.Log("SetSelectedDirector_UpdatesSelection - テスト完了");
            
            // EditorWindowのクリーンアップ
            Object.DestroyImmediate(renderer);
        }

        [Test]
        public void ValidateSettings_WithValidSettings_ReturnsTrue()
        {
            UnityEngine.Debug.Log("ValidateSettings_WithValidSettings_ReturnsTrue - テスト開始");
            
            var renderer = ScriptableObject.CreateInstance<SingleTimelineRenderer>();
            renderer.SetSelectedDirector(testDirector);
            
            bool isValid = renderer.ValidateSettings(out string errorMessage);
            
            Assert.IsTrue(isValid, $"Expected validation to succeed but got error: '{errorMessage}'");
            Assert.IsTrue(string.IsNullOrEmpty(errorMessage), $"Expected empty error message but got: '{errorMessage}'");
            
            UnityEngine.Debug.Log("ValidateSettings_WithValidSettings_ReturnsTrue - テスト完了");
            
            // EditorWindowのクリーンアップ
            Object.DestroyImmediate(renderer);
        }

        [Test]
        public void ValidateSettings_WithoutDirector_ReturnsFalse()
        {
            UnityEngine.Debug.Log("ValidateSettings_WithoutDirector_ReturnsFalse - テスト開始");
            
            // 一時的にtestDirectorを破棄して、Directorがない状態を作る
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
                testGameObject = null;
                testDirector = null;
            }
            
            var renderer = ScriptableObject.CreateInstance<SingleTimelineRenderer>();
            
            // 手動でScanTimelinesを呼び出して、Directorがない状態を確実にする
            renderer.GetAllPlayableDirectors().Clear();
            
            bool isValid = renderer.ValidateSettings(out string errorMessage);
            
            Assert.IsFalse(isValid);
            Assert.IsNotEmpty(errorMessage);
            Assert.IsTrue(errorMessage.Contains("Timeline"));
            
            UnityEngine.Debug.Log($"ValidateSettings_WithoutDirector_ReturnsFalse - エラーメッセージ: {errorMessage}");
            UnityEngine.Debug.Log("ValidateSettings_WithoutDirector_ReturnsFalse - テスト完了");
            
            // EditorWindowのクリーンアップ
            Object.DestroyImmediate(renderer);
            
            // testDirectorを再作成（TearDownで期待されているため）
            testGameObject = new GameObject("TestDirector");
            testDirector = testGameObject.AddComponent<PlayableDirector>();
        }
    }
}