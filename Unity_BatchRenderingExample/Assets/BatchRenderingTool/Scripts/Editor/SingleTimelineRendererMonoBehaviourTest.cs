using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BatchRenderingTool
{
    /// <summary>
    /// Single Timeline RendererのMonoBehaviourベース実装をテストするためのウィンドウ
    /// </summary>
    public class SingleTimelineRendererMonoBehaviourTest : EditorWindow
    {
        private PlayableDirector testDirector;
        private string outputPath = "Recordings/Test_<Take>";
        private int frameRate = 24;
        private int width = 1920;
        private int height = 1080;
        
        [MenuItem("Window/Batch Rendering Tool/Test/MonoBehaviour Renderer Test")]
        public static void ShowWindow()
        {
            var window = GetWindow<SingleTimelineRendererMonoBehaviourTest>();
            window.titleContent = new GUIContent("MonoBehaviour Renderer Test");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("MonoBehaviour Renderer Test", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "このウィンドウは、MonoBehaviourベースの新しいレンダリング実装をテストします。\n" +
                "1. PlayableDirectorを選択\n" +
                "2. 設定を確認\n" +
                "3. Test Renderボタンをクリック",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Director選択
            testDirector = (PlayableDirector)EditorGUILayout.ObjectField(
                "Test Director:", testDirector, typeof(PlayableDirector), true);
            
            // 設定
            EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("Output Path:", outputPath);
            frameRate = EditorGUILayout.IntField("Frame Rate:", frameRate);
            width = EditorGUILayout.IntField("Width:", width);
            height = EditorGUILayout.IntField("Height:", height);
            
            EditorGUILayout.Space(10);
            
            // テストボタン
            GUI.enabled = testDirector != null && !EditorApplication.isPlaying;
            if (GUILayout.Button("Test Render with MonoBehaviour", GUILayout.Height(30)))
            {
                TestMonoBehaviourRender();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            // デバッグ情報
            EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"Is Playing: {EditorApplication.isPlaying}");
            
            // EditorPrefsの状態
            bool isRendering = EditorPrefs.GetBool("STR_IsRendering", false);
            bool useMonoBehaviour = EditorPrefs.GetBool("STR_UseMonoBehaviour", false);
            
            EditorGUILayout.LabelField($"STR_IsRendering: {isRendering}");
            EditorGUILayout.LabelField($"STR_UseMonoBehaviour: {useMonoBehaviour}");
            
            if (GUILayout.Button("Clear All Flags"))
            {
                ClearAllFlags();
            }
            
            if (EditorApplication.isPlaying)
            {
                // Play Mode中のTimelineRendererを探す
                var renderer = GameObject.FindObjectOfType<PlayModeTimelineRenderer>();
                if (renderer != null)
                {
                    EditorGUILayout.LabelField("TimelineRenderer Found!", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("No TimelineRenderer in scene");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void TestMonoBehaviourRender()
        {
            Debug.Log("[MonoBehaviourTest] Starting test render");
            
            if (testDirector == null || testDirector.playableAsset == null)
            {
                Debug.LogError("[MonoBehaviourTest] Invalid director or timeline");
                return;
            }
            
            var timeline = testDirector.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                Debug.LogError("[MonoBehaviourTest] Director's playable asset is not a Timeline");
                return;
            }
            
            // SingleTimelineRendererの動作をシミュレート
            // まず、必要な情報をEditorPrefsに保存
            EditorPrefs.SetString("STR_DirectorName", testDirector.gameObject.name);
            EditorPrefs.SetFloat("STR_Duration", (float)timeline.duration);
            EditorPrefs.SetBool("STR_IsRendering", true);
            EditorPrefs.SetBool("STR_UseMonoBehaviour", true);
            EditorPrefs.SetInt("STR_FrameRate", frameRate);
            
            // 簡易的なテスト用Timeline作成
            var tempTimeline = CreateTestTimeline(testDirector, timeline);
            if (tempTimeline != null)
            {
                string tempPath = AssetDatabase.GetAssetPath(tempTimeline);
                EditorPrefs.SetString("STR_TempAssetPath", tempPath);
                EditorPrefs.SetString("STR_ExposedName", "TestExposedName");
                
                Debug.Log($"[MonoBehaviourTest] Created test timeline at: {tempPath}");
                
                // Play Modeに入る
                Debug.Log("[MonoBehaviourTest] Entering Play Mode...");
                EditorApplication.isPlaying = true;
            }
        }
        
        private TimelineAsset CreateTestTimeline(PlayableDirector director, TimelineAsset originalTimeline)
        {
            // テスト用の簡易Timeline作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "TestRenderTimeline";
            
            // Tempフォルダに保存
            string tempDir = "Assets/BatchRenderingTool/Temp";
            if (!AssetDatabase.IsValidFolder(tempDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                {
                    AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                }
                AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
            }
            
            string tempPath = $"{tempDir}/TestTimeline_{System.DateTime.Now.Ticks}.playable";
            AssetDatabase.CreateAsset(timeline, tempPath);
            
            // ControlTrackを追加
            var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Test Control Track");
            var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
            controlClip.duration = originalTimeline.duration;
            
            var controlAsset = controlClip.asset as ControlPlayableAsset;
            controlAsset.sourceGameObject.exposedName = "TestExposedName";
            controlAsset.sourceGameObject.defaultValue = director.gameObject;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return timeline;
        }
        
        private void ClearAllFlags()
        {
            EditorPrefs.DeleteKey("STR_DirectorName");
            EditorPrefs.DeleteKey("STR_TempAssetPath");
            EditorPrefs.DeleteKey("STR_Duration");
            EditorPrefs.DeleteKey("STR_ExposedName");
            EditorPrefs.DeleteKey("STR_FrameRate");
            EditorPrefs.SetBool("STR_IsRendering", false);
            EditorPrefs.SetBool("STR_UseMonoBehaviour", false);
            EditorPrefs.DeleteKey("STR_RenderingComplete");
            EditorPrefs.DeleteKey("STR_RenderingStatus");
            
            Debug.Log("[MonoBehaviourTest] All flags cleared");
        }
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log("[MonoBehaviourTest] Entered Play Mode");
                
                // MonoBehaviourベースのレンダリングが有効な場合
                if (EditorPrefs.GetBool("STR_UseMonoBehaviour", false))
                {
                    Debug.Log("[MonoBehaviourTest] Creating TimelineRenderer GameObject");
                    var rendererGO = new GameObject("[TimelineRenderer Test]");
                    rendererGO.AddComponent<PlayModeTimelineRenderer>();
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("[MonoBehaviourTest] Exiting Play Mode");
                
                // 結果を確認
                if (EditorPrefs.GetBool("STR_RenderingComplete", false))
                {
                    string status = EditorPrefs.GetString("STR_RenderingStatus", "Unknown");
                    Debug.Log($"[MonoBehaviourTest] Rendering completed with status: {status}");
                }
            }
        }
    }
}