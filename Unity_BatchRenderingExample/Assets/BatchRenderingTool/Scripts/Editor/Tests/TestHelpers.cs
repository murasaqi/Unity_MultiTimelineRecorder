using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.IO;

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
            Debug.Log($"TestHelpers - テスト用Timeline作成: {name}");
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = name;
            return timeline;
        }

        /// <summary>
        /// テスト用のPlayableDirectorを持つGameObjectを作成
        /// </summary>
        public static GameObject CreateTestDirectorGameObject(string name = "TestDirector")
        {
            Debug.Log($"TestHelpers - テスト用Director GameObject作成: {name}");
            var go = new GameObject(name);
            var director = go.AddComponent<PlayableDirector>();
            return go;
        }

        /// <summary>
        /// テスト用のRecorderSettingsを作成
        /// </summary>
        public static RecorderSettingsHelper CreateTestRecorderSettings()
        {
            Debug.Log("TestHelpers - テスト用RecorderSettings作成");
            return new RecorderSettingsHelper
            {
                OutputFolder = "TestOutput",
                OutputWidth = 1280,
                OutputHeight = 720,
                FrameRate = 24,
                ImageFormat = RecorderSettingsHelper.ImageFormat.PNG
            };
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
                Debug.Log($"TestHelpers - テスト出力フォルダ作成: {testFolder}");
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
                Debug.Log($"TestHelpers - テスト出力フォルダ削除: {folderPath}");
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
                Debug.Log("TestHelpers - PlayMode終了");
            }
        }

        /// <summary>
        /// Timeline内にテスト用のトラックを追加
        /// </summary>
        public static T AddTestTrack<T>(TimelineAsset timeline, string trackName) where T : TrackAsset
        {
            Debug.Log($"TestHelpers - テスト用トラック追加: {trackName} (Type: {typeof(T).Name})");
            var track = timeline.CreateTrack<T>(null, trackName);
            return track;
        }

        /// <summary>
        /// RecorderClipのモックを作成するためのヘルパー
        /// </summary>
        public static TimelineClip CreateMockRecorderClip(TimelineAsset timeline, double duration = 5.0)
        {
            Debug.Log($"TestHelpers - モックRecorderClip作成 (Duration: {duration}s)");
            
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
            Debug.Log($"{GetType().Name} - 基底セットアップ開始");
            
            // PlayModeを確実に終了
            TestHelpers.SafeExitPlayMode();
            
            // テスト用オブジェクトを作成
            testGameObject = TestHelpers.CreateTestDirectorGameObject();
            testDirector = testGameObject.GetComponent<PlayableDirector>();
            testTimeline = TestHelpers.CreateTestTimeline();
            testDirector.playableAsset = testTimeline;
            
            // テスト用出力フォルダを作成
            testOutputFolder = TestHelpers.CreateTestOutputFolder($"TestOutput_{System.Guid.NewGuid()}");
            
            Debug.Log($"{GetType().Name} - 基底セットアップ完了");
        }

        [TearDown]
        public virtual void BaseTearDown()
        {
            Debug.Log($"{GetType().Name} - 基底クリーンアップ開始");
            
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
            
            Debug.Log($"{GetType().Name} - 基底クリーンアップ完了");
        }
    }
}