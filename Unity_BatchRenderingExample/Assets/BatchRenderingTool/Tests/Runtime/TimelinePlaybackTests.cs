using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BatchRenderingTool.Runtime.Tests
{
    [TestFixture]
    public class TimelinePlaybackTests
    {
        private GameObject testGameObject;
        private PlayableDirector testDirector;
        private TimelineAsset testTimeline;

        [SetUp]
        public void Setup()
        {
            Debug.Log("TimelinePlaybackTests - Setup開始");
            
            // テスト用のGameObjectとPlayableDirectorを作成
            testGameObject = new GameObject("TestPlaybackDirector");
            testDirector = testGameObject.AddComponent<PlayableDirector>();
            
            // テスト用のTimelineAssetを作成
            testTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            testDirector.playableAsset = testTimeline;
            
            // Timelineに簡単なトラックを追加
            var animationTrack = testTimeline.CreateTrack<AnimationTrack>(null, "TestAnimationTrack");
            
            Debug.Log("TimelinePlaybackTests - Setup完了");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("TimelinePlaybackTests - TearDown開始");
            
            // テスト用オブジェクトをクリーンアップ
            if (testGameObject != null)
            {
                Object.Destroy(testGameObject);
            }
            
            if (testTimeline != null)
            {
                Object.Destroy(testTimeline);
            }
            
            Debug.Log("TimelinePlaybackTests - TearDown完了");
        }

        [UnityTest]
        public IEnumerator Timeline_CanPlayAndStop()
        {
            Debug.Log("Timeline_CanPlayAndStop - テスト開始");
            
            // Timelineを再生
            testDirector.Play();
            Assert.AreEqual(PlayState.Playing, testDirector.state);
            Debug.Log("Timeline_CanPlayAndStop - 再生状態確認");
            
            // 1フレーム待つ
            yield return null;
            
            // Timelineを停止
            testDirector.Stop();
            Assert.AreEqual(PlayState.Paused, testDirector.state);
            Debug.Log("Timeline_CanPlayAndStop - 停止状態確認");
            
            Debug.Log("Timeline_CanPlayAndStop - テスト完了");
        }

        [UnityTest]
        public IEnumerator Timeline_TimeProgresses()
        {
            Debug.Log("Timeline_TimeProgresses - テスト開始");
            
            // 初期時間を記録
            testDirector.time = 0;
            testDirector.Play();
            
            double initialTime = testDirector.time;
            Debug.Log($"Timeline_TimeProgresses - 初期時間: {initialTime}");
            
            // 数フレーム待つ
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
            
            double currentTime = testDirector.time;
            Debug.Log($"Timeline_TimeProgresses - 現在時間: {currentTime}");
            
            // 時間が進んでいることを確認
            Assert.Greater(currentTime, initialTime);
            
            testDirector.Stop();
            Debug.Log("Timeline_TimeProgresses - テスト完了");
        }

        [Test]
        public void PlayableDirector_HasValidTimelineAsset()
        {
            Debug.Log("PlayableDirector_HasValidTimelineAsset - テスト開始");
            
            Assert.IsNotNull(testDirector.playableAsset);
            Assert.IsInstanceOf<TimelineAsset>(testDirector.playableAsset);
            Assert.AreEqual(testTimeline, testDirector.playableAsset);
            
            Debug.Log("PlayableDirector_HasValidTimelineAsset - テスト完了");
        }

        [Test]
        public void Timeline_HasExpectedTracks()
        {
            Debug.Log("Timeline_HasExpectedTracks - テスト開始");
            
            var tracks = new System.Collections.Generic.List<TrackAsset>();
            foreach (var track in testTimeline.GetOutputTracks())
            {
                tracks.Add(track);
            }
            
            Assert.AreEqual(1, tracks.Count);
            Assert.IsInstanceOf<AnimationTrack>(tracks[0]);
            Assert.AreEqual("TestAnimationTrack", tracks[0].name);
            
            Debug.Log($"Timeline_HasExpectedTracks - トラック数: {tracks.Count}");
            Debug.Log("Timeline_HasExpectedTracks - テスト完了");
        }
    }
}