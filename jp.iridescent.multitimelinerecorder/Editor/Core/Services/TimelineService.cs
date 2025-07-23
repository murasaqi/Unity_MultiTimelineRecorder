using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service responsible for timeline discovery, validation, and management
    /// </summary>
    public class TimelineService : ITimelineService
    {
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private readonly Dictionary<PlayableDirector, TimelineState> _timelineStates;

        public TimelineService(MultiTimelineRecorder.Core.Interfaces.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timelineStates = new Dictionary<PlayableDirector, TimelineState>();
        }

        /// <inheritdoc />
        public List<PlayableDirector> ScanAvailableTimelines(bool includeInactive = false)
        {
            _logger.LogInfo("Scanning for available timelines in all loaded scenes", LogCategory.Timeline);
            
            var directors = new List<PlayableDirector>();
            
            // Scan all loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    var sceneDirectors = ScanSceneTimelines(scene.path, includeInactive);
                    directors.AddRange(sceneDirectors);
                }
            }

            // Also scan prefab stage if active
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var prefabDirectors = GetDirectorsFromRootObjects(
                    new[] { prefabStage.prefabContentsRoot }, 
                    includeInactive);
                directors.AddRange(prefabDirectors);
                _logger.LogInfo($"Found {prefabDirectors.Count} directors in prefab stage", LogCategory.Timeline);
            }

            _logger.LogInfo($"Total directors found: {directors.Count}", LogCategory.Timeline);
            return directors;
        }

        /// <inheritdoc />
        public List<PlayableDirector> ScanSceneTimelines(string scenePath, bool includeInactive = false)
        {
            var directors = new List<PlayableDirector>();

            if (string.IsNullOrEmpty(scenePath))
            {
                // Scan active scene
                var activeScene = SceneManager.GetActiveScene();
                directors = GetDirectorsFromRootObjects(activeScene.GetRootGameObjects(), includeInactive);
                _logger.LogInfo($"Found {directors.Count} directors in active scene: {activeScene.name}", LogCategory.Timeline);
            }
            else
            {
                // Load and scan specific scene
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                {
                    directors = GetDirectorsFromRootObjects(scene.GetRootGameObjects(), includeInactive);
                    _logger.LogInfo($"Found {directors.Count} directors in scene: {scene.name}", LogCategory.Timeline);
                }
                else
                {
                    _logger.LogWarning($"Scene not loaded or invalid: {scenePath}", LogCategory.Timeline);
                }
            }

            return directors;
        }

        /// <inheritdoc />
        public ValidationResult ValidateTimeline(PlayableDirector director)
        {
            var result = new ValidationResult();

            if (director == null)
            {
                result.AddError("PlayableDirector is null");
                return result;
            }

            // Check if director has a playable asset
            if (director.playableAsset == null)
            {
                result.AddError("PlayableDirector has no playable asset assigned");
                return result;
            }

            // Check if it's a Timeline asset
            var timeline = director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                result.AddError("Playable asset is not a Timeline");
                return result;
            }

            // Check duration
            if (director.duration <= 0)
            {
                result.AddError("Timeline has zero or negative duration");
            }

            // Check for tracks
            var tracks = timeline.GetOutputTracks().ToList();
            if (tracks.Count == 0)
            {
                result.AddWarning("Timeline has no tracks");
            }

            // Check for recorder tracks (which should be excluded)
            var recorderTracks = tracks.OfType<RecorderTrack>().ToList();
            if (recorderTracks.Any())
            {
                result.AddWarning($"Timeline contains {recorderTracks.Count} recorder track(s) which will be ignored");
            }

            // Check bindings
            var unboundTracks = 0;
            foreach (var track in tracks)
            {
                if (track is GroupTrack || track is RecorderTrack) continue;
                
                var binding = director.GetGenericBinding(track);
                if (binding == null)
                {
                    unboundTracks++;
                }
            }

            if (unboundTracks > 0)
            {
                result.AddWarning($"{unboundTracks} track(s) have no bindings");
            }

            // Check GameObject status
            if (!director.gameObject.activeInHierarchy)
            {
                result.AddWarning("Timeline GameObject is not active in hierarchy");
            }

            return result;
        }

        /// <inheritdoc />
        public TimelineInfo GetTimelineInfo(PlayableDirector director)
        {
            if (director == null || director.playableAsset == null)
            {
                return null;
            }

            var timeline = director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                return null;
            }

            var info = new TimelineInfo
            {
                Name = director.name,
                Path = GetGameObjectPath(director.gameObject),
                Duration = director.duration,
                FrameRate = timeline.editorSettings.frameRate,
                FrameCount = Mathf.CeilToInt((float)(director.duration * timeline.editorSettings.frameRate))
            };

            // Get track types
            var tracks = timeline.GetOutputTracks();
            foreach (var track in tracks)
            {
                var trackType = track.GetType().Name;
                if (!info.TrackTypes.Contains(trackType))
                {
                    info.TrackTypes.Add(trackType);
                }

                // Check for signals
                if (track is SignalTrack)
                {
                    info.HasSignals = true;
                }
            }

            return info;
        }

        /// <inheritdoc />
        public void PrepareTimelineForRecording(PlayableDirector director)
        {
            if (director == null) return;

            _logger.LogDebug($"Preparing timeline for recording: {director.name}", LogCategory.Timeline);

            // Save current state
            var state = new TimelineState
            {
                Time = director.time,
                ExtrapolationMode = director.extrapolationMode,
                InitialTime = director.initialTime,
                WrapMode = director.timeUpdateMode
            };

            _timelineStates[director] = state;

            // Prepare for recording
            director.time = 0;
            director.initialTime = 0;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            director.Evaluate();
        }

        /// <inheritdoc />
        public void RestoreTimelineAfterRecording(PlayableDirector director)
        {
            if (director == null) return;

            _logger.LogDebug($"Restoring timeline after recording: {director.name}", LogCategory.Timeline);

            if (_timelineStates.TryGetValue(director, out var state))
            {
                director.time = state.Time;
                director.extrapolationMode = state.ExtrapolationMode;
                director.initialTime = state.InitialTime;
                director.timeUpdateMode = state.WrapMode;
                director.Evaluate();

                _timelineStates.Remove(director);
            }
        }

        /// <summary>
        /// Gets directors from root game objects
        /// </summary>
        private List<PlayableDirector> GetDirectorsFromRootObjects(GameObject[] rootObjects, bool includeInactive)
        {
            var directors = new List<PlayableDirector>();

            foreach (var root in rootObjects)
            {
                if (root == null) continue;
                
                if (includeInactive)
                {
                    directors.AddRange(root.GetComponentsInChildren<PlayableDirector>(true));
                }
                else
                {
                    directors.AddRange(root.GetComponentsInChildren<PlayableDirector>());
                }
            }

            // Filter out directors without timeline assets
            return directors.Where(d => 
                d != null && 
                d.playableAsset != null && 
                d.playableAsset is TimelineAsset).ToList();
        }

        /// <summary>
        /// Gets the full path of a GameObject in the hierarchy
        /// </summary>
        private string GetGameObjectPath(GameObject gameObject)
        {
            if (gameObject == null) return string.Empty;

            var path = gameObject.name;
            var parent = gameObject.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            // Add scene name if available
            if (gameObject.scene.IsValid())
            {
                path = gameObject.scene.name + "/" + path;
            }

            return path;
        }

        /// <summary>
        /// Internal class for storing timeline state
        /// </summary>
        private class TimelineState
        {
            public double Time { get; set; }
            public DirectorWrapMode ExtrapolationMode { get; set; }
            public double InitialTime { get; set; }
            public DirectorUpdateMode WrapMode { get; set; }
        }
    }
}