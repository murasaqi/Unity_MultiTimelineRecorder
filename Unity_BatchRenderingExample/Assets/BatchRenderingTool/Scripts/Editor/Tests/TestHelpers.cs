using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.IO;
using NUnit.Framework;

namespace BatchRenderingTool.Editor.Tests
{
    /// <summary>
    /// テスト用のヘルパーメソッドを提供するクラス
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// テスト用のTimelineAssetを作成
        /// </summary>
        public static TimelineAsset CreateTestTimeline(string name = "TestTimeline")
        {
            UnityEngine.Debug.Log($"TestHelpers - テスト用Timeline作成: {name}");
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = name;
            return timeline;
        }

        /// <summary>
        /// テスト用のPlayableDirectorを持つGameObjectを作成
        /// </summary>
        public static GameObject CreateTestDirectorGameObject(string name = "TestDirector")
        {
            UnityEngine.Debug.Log($"TestHelpers - テスト用Director GameObject作成: {name}");
            var go = new GameObject(name);
            var director = go.AddComponent<PlayableDirector>();
            return go;
        }

        /// <summary>
        /// テスト用のRecorderSettings構成情報を作成
        /// </summary>
        public class TestRecorderConfig
        {
            public string OutputFolder { get; set; } = "TestOutput";
            public int OutputWidth { get; set; } = 1280;
            public int OutputHeight { get; set; } = 720;
            public int FrameRate { get; set; } = 24;
            public RecorderSettingsHelper.ImageFormat ImageFormat { get; set; } = RecorderSettingsHelper.ImageFormat.PNG;
        }
        
        public static TestRecorderConfig CreateTestRecorderConfig()
        {
            UnityEngine.Debug.Log("TestHelpers - テスト用RecorderConfig作成");
            return new TestRecorderConfig();
        }

        /// <summary>
        /// テスト用の出力フォルダを作成
        /// </summary>
        public static string CreateTestOutputFolder(string baseName = "TestOutput")
        {
            string testFolder = Path.Combine(Application.dataPath, "..", baseName);
            if (!Directory.Exists(testFolder))
            {
                Directory.CreateDirectory(testFolder);
                UnityEngine.Debug.Log($"TestHelpers - テスト出力フォルダ作成: {testFolder}");
            }
            return testFolder;
        }

        /// <summary>
        /// テスト用の出力フォルダをクリーンアップ
        /// </summary>
        public static void CleanupTestOutputFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                UnityEngine.Debug.Log($"TestHelpers - テスト出力フォルダ削除: {folderPath}");
            }
        }

        /// <summary>
        /// EditorのPlayModeを安全に変更
        /// </summary>
        public static void SafeExitPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                UnityEngine.Debug.Log("TestHelpers - PlayMode終了");
            }
        }

        /// <summary>
        /// Timeline内にテスト用のトラックを追加
        /// </summary>
        public static T AddTestTrack<T>(TimelineAsset timeline, string trackName) where T : TrackAsset, new()
        {
            UnityEngine.Debug.Log($"TestHelpers - テスト用トラック追加: {trackName} (Type: {typeof(T).Name})");
            var track = timeline.CreateTrack<T>(null, trackName);
            return track;
        }

        /// <summary>
        /// RecorderClipのモックを作成するためのヘルパー
        /// </summary>
        public static TimelineClip CreateMockRecorderClip(TimelineAsset timeline, double duration = 5.0)
        {
            UnityEngine.Debug.Log($"TestHelpers - モックRecorderClip作成 (Duration: {duration}s)");
            
            // AnimationTrackを使用してモッククリップを作成
            var track = timeline.CreateTrack<AnimationTrack>(null, "MockRecorderTrack");
            var clip = track.CreateDefaultClip();
            clip.duration = duration;
            clip.displayName = "MockRecorderClip";
            
            return clip;
        }
    }

    /// <summary>
    /// テスト実行時の共通セットアップを提供する基底クラス
    /// </summary>
    public abstract class BatchRenderingTestBase
    {
        protected GameObject testGameObject;
        protected PlayableDirector testDirector;
        protected TimelineAsset testTimeline;
        protected string testOutputFolder;

        [SetUp]
        public virtual void BaseSetup()
        {
            UnityEngine.Debug.Log($"{GetType().Name} - 基底セットアップ開始");
            
            // PlayModeを確実に終了
            TestHelpers.SafeExitPlayMode();
            
            // テスト用オブジェクトを作成
            testGameObject = TestHelpers.CreateTestDirectorGameObject();
            testDirector = testGameObject.GetComponent<PlayableDirector>();
            testTimeline = TestHelpers.CreateTestTimeline();
            testDirector.playableAsset = testTimeline;
            
            // テスト用出力フォルダを作成
            testOutputFolder = TestHelpers.CreateTestOutputFolder($"TestOutput_{System.Guid.NewGuid()}");
            
            UnityEngine.Debug.Log($"{GetType().Name} - 基底セットアップ完了");
        }

        [TearDown]
        public virtual void BaseTearDown()
        {
            UnityEngine.Debug.Log($"{GetType().Name} - 基底クリーンアップ開始");
            
            // PlayModeを確実に終了
            TestHelpers.SafeExitPlayMode();
            
            // テスト用オブジェクトをクリーンアップ
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testTimeline != null)
            {
                Object.DestroyImmediate(testTimeline);
            }
            
            // テスト用出力フォルダをクリーンアップ
            if (!string.IsNullOrEmpty(testOutputFolder))
            {
                TestHelpers.CleanupTestOutputFolder(testOutputFolder);
            }
            
            UnityEngine.Debug.Log($"{GetType().Name} - 基底クリーンアップ完了");
        }
    }
}