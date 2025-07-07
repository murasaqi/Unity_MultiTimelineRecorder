using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;

namespace BatchRenderingTool.Debug
{
    /// <summary>
    /// FBXレコーダーの問題を特定するためのテストツール
    /// </summary>
    public class FBXTimelineTest : EditorWindow
    {
        private GameObject targetGameObject;
        private string testResult = "Test not run";
        
        [MenuItem("Window/Batch Rendering Tool/Debug/FBX Timeline Test")]
        public static void ShowWindow()
        {
            GetWindow<FBXTimelineTest>("FBX Timeline Test");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("FBX Timeline Recording Test", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            targetGameObject = EditorGUILayout.ObjectField("Target GameObject", targetGameObject, typeof(GameObject), true) as GameObject;
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Test FBX Recording with Simple Timeline", GUILayout.Height(30)))
            {
                TestSimpleFBXRecording();
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Test Result:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(testResult, MessageType.Info);
        }
        
        private void TestSimpleFBXRecording()
        {
            if (targetGameObject == null)
            {
                testResult = "Please select a target GameObject";
                return;
            }
            
            try
            {
                testResult = "Creating test Timeline...";
                
                // シンプルなTimelineを作成
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = "FBX_Test_Timeline";
                
                // RecorderTrackを作成
                var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, "Test Recorder Track");
                
                // RecorderClipを作成
                var recorderClip = recorderTrack.CreateClip<RecorderClip>();
                recorderClip.displayName = "FBX Test Recording";
                recorderClip.start = 0;
                recorderClip.duration = 2.0; // 2秒の録画
                
                // FBX RecorderSettingsを作成
                var fbxSettings = RecorderClipUtility.CreateProperFBXRecorderSettings("FBX_Test");
                if (fbxSettings == null)
                {
                    testResult = "Failed to create FBX Recorder Settings - FBX package may not be installed";
                    return;
                }
                
                testResult = $"Created FBX settings of type: {fbxSettings.GetType().FullName}";
                
                // 設定を適用
                var recorderAsset = recorderClip.asset as RecorderClip;
                recorderAsset.settings = fbxSettings;
                
                // FBX設定を確認
                var settingsType = fbxSettings.GetType();
                var animInputProp = settingsType.GetProperty("AnimationInputSettings");
                if (animInputProp != null)
                {
                    var animInput = animInputProp.GetValue(fbxSettings);
                    if (animInput != null)
                    {
                        var gameObjectProp = animInput.GetType().GetProperty("gameObject");
                        if (gameObjectProp != null)
                        {
                            gameObjectProp.SetValue(animInput, targetGameObject);
                            testResult += $"\nSet target GameObject to: {targetGameObject.name}";
                        }
                    }
                }
                
                // 設定を有効化
                fbxSettings.Enabled = true;
                fbxSettings.RecordMode = RecordMode.Manual;
                fbxSettings.FrameRate = 30;
                fbxSettings.CapFrameRate = true;
                
                // Output pathを設定
                RecorderSettingsHelper.ConfigureOutputPath(fbxSettings, "Recordings/FBXTest", "TestFBX", RecorderSettingsType.FBX);
                
                // Timelineを保存
                string tempPath = "Assets/BatchRenderingTool/Temp/FBX_Test_Timeline.playable";
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool/Temp"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                    {
                        AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                    }
                    AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
                }
                
                AssetDatabase.CreateAsset(timeline, tempPath);
                AssetDatabase.SaveAssets();
                
                testResult += $"\nTimeline created at: {tempPath}";
                testResult += "\nTest complete - check the Timeline in the Project window";
                testResult += "\nTry playing this Timeline with a PlayableDirector to see if FBX recording works";
                
                // Pingで表示
                EditorGUIUtility.PingObject(timeline);
            }
            catch (Exception e)
            {
                testResult = $"Error during test: {e.Message}\n{e.StackTrace}";
            }
        }
    }
}