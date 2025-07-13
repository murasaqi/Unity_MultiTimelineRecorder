using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;

namespace Unity.MultiTimelineRecorder.Utilities
{
    /// <summary>
    /// SignalEmitter情報を管理する構造体
    /// </summary>
    [Serializable]
    public struct SignalTimingInfo
    {
        public double time;
        public string signalName;
        public SignalAsset signalAsset;
        public TrackAsset parentTrack;
        
        public SignalTimingInfo(double time, string signalName, SignalAsset signalAsset, TrackAsset parentTrack)
        {
            this.time = time;
            this.signalName = signalName;
            this.signalAsset = signalAsset;
            this.parentTrack = parentTrack;
        }
        
        public override string ToString()
        {
            return $"Signal: {signalName} at {time:F2}s";
        }
    }
    
    /// <summary>
    /// Recording期間の設定
    /// </summary>
    [Serializable]
    public struct RecordingRange
    {
        public double startTime;
        public double endTime;
        public double duration => endTime - startTime;
        public bool isValid => startTime >= 0 && endTime > startTime;
        
        public RecordingRange(double startTime, double endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
        }
        
        public static RecordingRange Invalid => new RecordingRange(-1, -1);
    }
    
    /// <summary>
    /// SignalEmitterによるRecord期間制御のユーティリティクラス
    /// TODO-282: SignalEmitterによるRecord期間の指定機能
    /// </summary>
    public static class SignalEmitterRecordControl
    {
        /// <summary>
        /// TimelineAsset内のすべてのSignalEmitterを取得
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <returns>SignalEmitterの情報リスト</returns>
        public static List<SignalTimingInfo> GetAllSignalEmitters(TimelineAsset timelineAsset)
        {
            var signals = new List<SignalTimingInfo>();
            
            if (timelineAsset == null) return signals;
            
            // [MTR]を含むトラックを優先的に処理
            var allTracks = timelineAsset.GetOutputTracks().ToList();
            var mtrTracks = allTracks.Where(t => t.name.Contains("[MTR]")).ToList();
            var otherTracks = allTracks.Where(t => !t.name.Contains("[MTR]")).ToList();
            
            // デバッグ情報
            if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
            {
                if (mtrTracks.Count > 0)
                {
                    Debug.Log($"[DEBUG] Found {mtrTracks.Count} [MTR] tracks:");
                    foreach (var track in mtrTracks)
                    {
                        Debug.Log($"  - {track.name}");
                    }
                }
            }
            
            // [MTR]トラックを先に処理
            foreach (var track in mtrTracks.Concat(otherTracks))
            {
                var markers = track.GetMarkers().ToArray();
                foreach (var marker in markers)
                {
                    if (marker is SignalEmitter signalEmitter)
                    {
                        // SignalEmitterのTimeline上での表示名を取得
                        string signalName = GetSignalEmitterDisplayName(signalEmitter, marker);
                        
                        signals.Add(new SignalTimingInfo(
                            signalEmitter.time,
                            signalName,
                            signalEmitter.asset,
                            track
                        ));
                        
                        // [MTR]トラックからのSignalEmitterを優先
                        if (track.name.Contains("[MTR]"))
                        {
                            if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                            {
                                Debug.Log($"[DEBUG] Found SignalEmitter in [MTR] track: '{signalName}' at {signalEmitter.time:F3}s");
                            }
                        }
                    }
                }
            }
            
            // MarkerTrack上のマーカーもチェック（[MTR]トラックがない場合のフォールバック）
            if (timelineAsset.markerTrack != null && mtrTracks.Count == 0)
            {
                var markers = timelineAsset.markerTrack.GetMarkers().ToArray();
                foreach (var marker in markers)
                {
                    if (marker is SignalEmitter signalEmitter)
                    {
                        // SignalEmitterのTimeline上での表示名を取得
                        string signalName = GetSignalEmitterDisplayName(signalEmitter, marker);
                        
                        signals.Add(new SignalTimingInfo(
                            signalEmitter.time,
                            signalName,
                            signalEmitter.asset,
                            timelineAsset.markerTrack
                        ));
                    }
                }
            }
            
            // 時間順にソート
            signals.Sort((a, b) => a.time.CompareTo(b.time));
            
            return signals;
        }
        
        /// <summary>
        /// SignalEmitterのTimeline上での表示名を取得
        /// </summary>
        private static string GetSignalEmitterDisplayName(SignalEmitter signalEmitter, IMarker marker)
        {
            string signalName = null;
            
            try
            {
                // Unity内部のマーカー表示名を取得する様々な方法を試す
                var notificationType = marker.GetType();
                
                // 1. フィールドとしてのdisplayName, name, labelを探す
                var fields = notificationType.GetFields(System.Reflection.BindingFlags.Public | 
                                                       System.Reflection.BindingFlags.NonPublic | 
                                                       System.Reflection.BindingFlags.Instance);
                
                // より具体的な名前を優先
                string[] fieldPriority = { "m_displayName", "displayName", "m_name", "name", "m_label", "label" };
                
                foreach (var fieldName in fieldPriority)
                {
                    var field = fields.FirstOrDefault(f => f.Name == fieldName);
                    if (field != null)
                    {
                        var value = field.GetValue(marker) as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            signalName = value;
                            if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                            {
                                Debug.Log($"[DEBUG] Found display name in field '{fieldName}': '{value}'");
                            }
                            break;
                        }
                    }
                }
                
                // 2. プロパティとしてのdisplayName, name, labelを探す
                if (string.IsNullOrEmpty(signalName))
                {
                    var properties = notificationType.GetProperties(System.Reflection.BindingFlags.Public | 
                                                                   System.Reflection.BindingFlags.NonPublic | 
                                                                   System.Reflection.BindingFlags.Instance);
                    
                    string[] propPriority = { "displayName", "name", "label" };
                    
                    foreach (var propName in propPriority)
                    {
                        var prop = properties.FirstOrDefault(p => p.Name.ToLower() == propName.ToLower());
                        if (prop != null && prop.CanRead)
                        {
                            try
                            {
                                var value = prop.GetValue(marker) as string;
                                if (!string.IsNullOrEmpty(value))
                                {
                                    signalName = value;
                                    if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                                    {
                                        Debug.Log($"[DEBUG] Found display name in property '{propName}': '{value}'");
                                    }
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // エラーが発生した場合はデバッグログを出力
                if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                {
                    Debug.LogWarning($"[DEBUG] Failed to get display name for SignalEmitter at {signalEmitter.time:F3}s: {ex.Message}");
                }
            }
            
            // displayName が取得できなかった場合は SignalAsset 名を使用
            if (string.IsNullOrEmpty(signalName))
            {
                signalName = signalEmitter.asset?.name ?? $"SignalEmitter_{signalEmitter.time:F3}s";
            }
            
            return signalName;
        }
        
        /// <summary>
        /// 検出されたSignalEmitterの詳細をデバッグ出力
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        public static void DebugLogAllSignalEmitters(TimelineAsset timelineAsset)
        {
            var signals = GetAllSignalEmitters(timelineAsset);
            Debug.Log($"=== SignalEmitter Detection Debug ===");
            Debug.Log($"Timeline: {(timelineAsset != null ? timelineAsset.name : "NULL")}");
            Debug.Log($"Found {signals.Count} SignalEmitters:");
            
            for (int i = 0; i < signals.Count; i++)
            {
                var signal = signals[i];
                Debug.Log($"  [{i}] SignalAsset Name: '{signal.signalName}' | Time: {signal.time:F3}s | Track: {signal.parentTrack?.name ?? "MarkerTrack"}");
                
                // SignalAssetの詳細情報もデバッグ出力
                if (signal.signalAsset != null)
                {
                    Debug.Log($"      SignalAsset: {signal.signalAsset.name} (Type: {signal.signalAsset.GetType().Name})");
                }
                else
                {
                    Debug.Log($"      SignalAsset: NULL");
                }
            }
            
            if (signals.Count == 0)
            {
                Debug.Log("  (No SignalEmitters found)");
                
                // Timeline構造のデバッグ情報
                if (timelineAsset != null)
                {
                    var tracks = timelineAsset.GetOutputTracks().ToArray();
                    Debug.Log($"  Timeline has {tracks.Length} output tracks");
                    Debug.Log($"  Timeline has MarkerTrack: {timelineAsset.markerTrack != null}");
                    
                    foreach (var track in tracks)
                    {
                        var markerCount = track.GetMarkers().Count();
                        Debug.Log($"    Track '{track.name}': {markerCount} markers");
                    }
                    
                    if (timelineAsset.markerTrack != null)
                    {
                        var markerTrackMarkerCount = timelineAsset.markerTrack.GetMarkers().Count();
                        Debug.Log($"    MarkerTrack: {markerTrackMarkerCount} markers");
                    }
                }
            }
            Debug.Log($"=====================================");
        }
        
        /// <summary>
        /// 指定した名前のSignalEmitterを検索
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="signalName">検索するシグナル名</param>
        /// <returns>見つかったSignalEmitterの情報（見つからない場合はnull）</returns>
        public static SignalTimingInfo? FindSignalEmitterByName(TimelineAsset timelineAsset, string signalName)
        {
            var signals = GetAllSignalEmitters(timelineAsset);
            
            // DisplayName での完全一致検索（大文字小文字区別なし）
            foreach (var signal in signals)
            {
                if (string.Equals(signal.signalName, signalName, StringComparison.OrdinalIgnoreCase))
                {
                    return signal;
                }
            }
            
            // デバッグ情報出力
            if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
            {
                Debug.Log($"[DEBUG] FindSignalEmitterByName: Searching for '{signalName}'");
                Debug.Log($"[DEBUG] Available signals:");
                foreach (var signal in signals)
                {
                    Debug.Log($"  - '{signal.signalName}' at {signal.time:F3}s");
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// SignalEmitterモード用のRecording期間を計算（フォールバック対応）
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="startTimingName">開始タイミングのシグナル名</param>
        /// <param name="endTimingName">終了タイミングのシグナル名</param>
        /// <param name="allowFallback">SignalEmitterが見つからない場合にフルタイムライン期間を返すか</param>
        /// <returns>Recording期間の設定</returns>
        public static RecordingRange GetRecordingRangeFromSignalsWithFallback(
            TimelineAsset timelineAsset, 
            string startTimingName, 
            string endTimingName, 
            bool allowFallback = true)
        {
            if (timelineAsset == null || string.IsNullOrEmpty(startTimingName) || string.IsNullOrEmpty(endTimingName))
            {
                return allowFallback ? GetFullTimelineRange(timelineAsset) : RecordingRange.Invalid;
            }
            
            var startSignal = FindSignalEmitterByName(timelineAsset, startTimingName);
            var endSignal = FindSignalEmitterByName(timelineAsset, endTimingName);
            
            // 両方のSignalEmitterが見つかった場合はSignal準拠
            if (startSignal.HasValue && endSignal.HasValue)
            {
                var startTime = startSignal.Value.time;
                var endTime = endSignal.Value.time;
                
                if (endTime <= startTime)
                {
                    Debug.LogWarning($"Invalid recording range. End time ({endTime}) must be greater than start time ({startTime})");
                    return allowFallback ? GetFullTimelineRange(timelineAsset) : RecordingRange.Invalid;
                }
                
                if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                {
                    Debug.Log($"SignalEmitter range found: {startTime:F3}s - {endTime:F3}s (Start: {startTimingName}, End: {endTimingName})");
                }
                return new RecordingRange(startTime, endTime);
            }
            
            // SignalEmitterが見つからない場合
            if (allowFallback)
            {
                if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                {
                    Debug.LogWarning($"SignalEmitter not found. Start: {startTimingName}, End: {endTimingName} - Using full timeline range");
                    DebugLogAllSignalEmitters(timelineAsset);
                }
                return GetFullTimelineRange(timelineAsset);
            }
            else
            {
                if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
                {
                    Debug.LogWarning($"SignalEmitter not found. Start: {startTimingName}, End: {endTimingName}");
                    DebugLogAllSignalEmitters(timelineAsset);
                }
                return RecordingRange.Invalid;
            }
        }
        
        /// <summary>
        /// 指定した名前のSignalEmitterが実際に検出されるかチェック
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="startTimingName">開始タイミングのシグナル名</param>
        /// <param name="endTimingName">終了タイミングのシグナル名</param>
        /// <returns>両方のSignalEmitterが検出された場合true</returns>
        public static bool HasValidSignalEmitters(TimelineAsset timelineAsset, string startTimingName, string endTimingName)
        {
            if (timelineAsset == null || string.IsNullOrEmpty(startTimingName) || string.IsNullOrEmpty(endTimingName))
            {
                return false;
            }
            
            var startSignal = FindSignalEmitterByName(timelineAsset, startTimingName);
            var endSignal = FindSignalEmitterByName(timelineAsset, endTimingName);
            
            bool hasValid = startSignal.HasValue && endSignal.HasValue;
            
            // デバッグモードの場合のみログ出力（毎フレーム出力を防ぐため）
            if (UnityEditor.EditorPrefs.GetBool("MTR_SignalEmitterDebugMode", false))
            {
                Debug.Log($"[DEBUG] HasValidSignalEmitters({timelineAsset.name}): start='{startTimingName}' found={startSignal.HasValue}, end='{endTimingName}' found={endSignal.HasValue}, result={hasValid}");
            }
            
            return hasValid;
        }
        
        /// <summary>
        /// Timeline内にSignal Trackが存在するかチェック
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <returns>Signal Trackが存在する場合true</returns>
        public static bool HasSignalTrack(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null) return false;
            
            // Check all output tracks for SignalTrack type
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track.GetType().Name == "SignalTrack")
                {
                    return true;
                }
            }
            
            // Also check if MarkerTrack exists (SignalEmitters are placed on MarkerTrack)
            if (timelineAsset.markerTrack != null)
            {
                // Check if MarkerTrack has any SignalEmitters
                var markers = timelineAsset.markerTrack.GetMarkers().ToArray();
                foreach (var marker in markers)
                {
                    if (marker is SignalEmitter)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Signal Trackが存在し、指定した名前のSignalEmitterが2つ検出されるかチェック
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="startTimingName">開始タイミングのシグナル名</param>
        /// <param name="endTimingName">終了タイミングのシグナル名</param>
        /// <returns>Signal Trackが存在し、両方のSignalEmitterが検出された場合true</returns>
        public static bool HasSignalTrackWithValidEmitters(TimelineAsset timelineAsset, string startTimingName, string endTimingName)
        {
            if (timelineAsset == null || string.IsNullOrEmpty(startTimingName) || string.IsNullOrEmpty(endTimingName))
            {
                return false;
            }
            
            // Check if both SignalEmitters with specified names exist
            bool hasValidEmitters = HasValidSignalEmitters(timelineAsset, startTimingName, endTimingName);
            
            // If we have valid emitters, we consider it as having a Signal Track
            // (since SignalEmitters can only exist on Signal Tracks or MarkerTracks)
            return hasValidEmitters;
        }
        
        /// <summary>
        /// タイムラインの全期間を取得
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <returns>タイムライン全期間</returns>
        public static RecordingRange GetFullTimelineRange(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                return RecordingRange.Invalid;
            }
            
            return new RecordingRange(0, timelineAsset.duration);
        }
        
        /// <summary>
        /// 開始・終了タイミング名からRecording期間を計算（従来互換）
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="startTimingName">開始タイミングのシグナル名</param>
        /// <param name="endTimingName">終了タイミングのシグナル名</param>
        /// <returns>Recording期間の設定</returns>
        public static RecordingRange GetRecordingRangeFromSignals(
            TimelineAsset timelineAsset, 
            string startTimingName, 
            string endTimingName)
        {
            if (timelineAsset == null || string.IsNullOrEmpty(startTimingName) || string.IsNullOrEmpty(endTimingName))
            {
                return RecordingRange.Invalid;
            }
            
            var startSignal = FindSignalEmitterByName(timelineAsset, startTimingName);
            var endSignal = FindSignalEmitterByName(timelineAsset, endTimingName);
            
            if (!startSignal.HasValue || !endSignal.HasValue)
            {
                Debug.LogWarning($"SignalEmitter not found. Start: {startTimingName}, End: {endTimingName}");
                DebugLogAllSignalEmitters(timelineAsset);
                return RecordingRange.Invalid;
            }
            
            var startTime = startSignal.Value.time;
            var endTime = endSignal.Value.time;
            
            if (endTime <= startTime)
            {
                Debug.LogWarning($"Invalid recording range. End time ({endTime}) must be greater than start time ({startTime})");
                return RecordingRange.Invalid;
            }
            
            return new RecordingRange(startTime, endTime);
        }
        
        /// <summary>
        /// Control ClipをSignalEmitterの期間に合わせてクロップする
        /// </summary>
        /// <param name="controlClip">対象のControl Clip</param>
        /// <param name="recordingRange">Recording期間</param>
        /// <param name="frameRate">Timeline のフレームレート</param>
        public static void CropControlClipToRange(TimelineClip controlClip, RecordingRange recordingRange, double frameRate = 30.0)
        {
            if (controlClip == null || !recordingRange.isValid)
            {
                Debug.LogWarning("Invalid parameters for cropping control clip");
                return;
            }
            
            // Control Clipの開始時間と継続時間を設定
            controlClip.start = recordingRange.startTime;
            controlClip.duration = recordingRange.duration;
            
            // クリップ内のコンテンツもクロップ（必要に応じて）
            if (controlClip.asset is ControlPlayableAsset controlAsset)
            {
                // ControlPlayableAssetの場合の処理
                // 必要に応じてアセット固有のプロパティを調整
            }
            
            Debug.Log($"Control Clip cropped to range: {recordingRange.startTime:F2}s - {recordingRange.endTime:F2}s (Duration: {recordingRange.duration:F2}s)");
        }
        
        /// <summary>
        /// RecorderClipのタイミングをControl Clipに同期させる
        /// </summary>
        /// <param name="recorderClip">対象のRecorder Clip</param>
        /// <param name="controlClip">参照するControl Clip</param>
        /// <param name="preRollFrames">Pre-Rollフレーム数</param>
        /// <param name="frameRate">フレームレート</param>
        public static void SyncRecorderClipToControlClip(
            TimelineClip recorderClip, 
            TimelineClip controlClip, 
            int preRollFrames = 0, 
            double frameRate = 30.0)
        {
            if (recorderClip == null || controlClip == null)
            {
                Debug.LogWarning("Invalid clips for synchronization");
                return;
            }
            
            // Pre-Rollを考慮した開始時間の計算
            var preRollTime = preRollFrames / frameRate;
            var adjustedStartTime = Math.Max(0, controlClip.start - preRollTime);
            
            // RecorderClipのタイミングを設定
            recorderClip.start = adjustedStartTime;
            recorderClip.duration = controlClip.duration + preRollTime;
            
            // RecorderClipアセットの設定調整
            var recorderAsset = recorderClip.asset as RecorderClip;
            if (recorderAsset != null)
            {
                // Recorder設定の調整（必要に応じて）
                // 例：出力フレーム範囲の設定など
            }
            
            Debug.Log($"Recorder Clip synchronized: Start={recorderClip.start:F2}s, Duration={recorderClip.duration:F2}s (Pre-Roll: {preRollTime:F2}s)");
        }
        
        /// <summary>
        /// SignalEmitterの設定情報を表示（デバッグ用）
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        public static void DebugPrintSignalEmitters(TimelineAsset timelineAsset)
        {
            var signals = GetAllSignalEmitters(timelineAsset);
            
            Debug.Log($"=== SignalEmitters in Timeline: {timelineAsset.name} ===");
            Debug.Log($"Total SignalEmitters found: {signals.Count}");
            
            foreach (var signal in signals)
            {
                Debug.Log($"  {signal}");
            }
            
            if (signals.Count == 0)
            {
                Debug.Log("  No SignalEmitters found in this Timeline.");
            }
        }
        
        /// <summary>
        /// SignalEmitter設定の妥当性をチェック
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="startTimingName">開始タイミング名</param>
        /// <param name="endTimingName">終了タイミング名</param>
        /// <returns>設定が有効かどうか</returns>
        public static bool ValidateSignalEmitterSettings(
            TimelineAsset timelineAsset, 
            string startTimingName, 
            string endTimingName)
        {
            if (timelineAsset == null)
            {
                Debug.LogError("TimelineAsset is null");
                return false;
            }
            
            if (string.IsNullOrEmpty(startTimingName) || string.IsNullOrEmpty(endTimingName))
            {
                Debug.LogError("Signal timing names cannot be empty");
                return false;
            }
            
            var recordingRange = GetRecordingRangeFromSignals(timelineAsset, startTimingName, endTimingName);
            
            if (!recordingRange.isValid)
            {
                Debug.LogError($"Invalid recording range for signals: {startTimingName} -> {endTimingName}");
                return false;
            }
            
            Debug.Log($"Valid SignalEmitter configuration: {startTimingName} ({recordingRange.startTime:F2}s) -> {endTimingName} ({recordingRange.endTime:F2}s)");
            return true;
        }
    }
}