using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace BatchRenderingTool.Editor
{
    /// <summary>
    /// Unity Test Runnerを簡単に実行するためのエディタウィンドウ
    /// </summary>
    public class TestRunner : EditorWindow
    {
        private TestRunnerApi testRunnerApi;
        private bool runEditorTests = true;
        private bool runPlaymodeTests = true;
        private Vector2 scrollPosition;
        private string lastTestResult = "";

        [MenuItem("Window/Batch Rendering Tool/Test Runner")]
        public static void ShowWindow()
        {
            var window = GetWindow<TestRunner>("Batch Rendering Test Runner");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            Debug.Log("TestRunner - ウィンドウ有効化");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Rendering Tool - Test Runner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawTestOptions();
            EditorGUILayout.Space();
            
            DrawTestButtons();
            EditorGUILayout.Space();
            
            DrawTestResults();
        }

        private void DrawTestOptions()
        {
            EditorGUILayout.LabelField("テストオプション", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                runEditorTests = EditorGUILayout.Toggle("Editor Testsを実行", runEditorTests);
                runPlaymodeTests = EditorGUILayout.Toggle("Playmode Testsを実行", runPlaymodeTests);
            }
        }

        private void DrawTestButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = runEditorTests || runPlaymodeTests;
                
                if (GUILayout.Button("選択したテストを実行", GUILayout.Height(30)))
                {
                    RunTests();
                }
                
                GUI.enabled = true;
                
                if (GUILayout.Button("Test Runnerウィンドウを開く", GUILayout.Height(30)))
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Editor Testsのみ実行"))
                {
                    RunEditorTestsOnly();
                }
                
                if (GUILayout.Button("Playmode Testsのみ実行"))
                {
                    RunPlaymodeTestsOnly();
                }
            }
        }

        private void DrawTestResults()
        {
            EditorGUILayout.LabelField("テスト結果", EditorStyles.boldLabel);
            
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, EditorStyles.helpBox, GUILayout.Height(150)))
            {
                scrollPosition = scrollView.scrollPosition;
                
                if (string.IsNullOrEmpty(lastTestResult))
                {
                    EditorGUILayout.LabelField("テストがまだ実行されていません。");
                }
                else
                {
                    EditorGUILayout.TextArea(lastTestResult, EditorStyles.wordWrappedLabel);
                }
            }
            
            if (GUILayout.Button("結果をクリア"))
            {
                lastTestResult = "";
                Debug.Log("TestRunner - テスト結果クリア");
            }
        }

        private void RunTests()
        {
            Debug.Log("TestRunner - テスト実行開始");
            lastTestResult = "テスト実行中...\n";
            
            if (runEditorTests)
            {
                lastTestResult += "Editor Tests実行中...\n";
                RunEditorTests();
            }
            
            if (runPlaymodeTests)
            {
                lastTestResult += "Playmode Tests実行中...\n";
                RunPlaymodeTests();
            }
            
            Repaint();
        }

        private void RunEditorTestsOnly()
        {
            Debug.Log("TestRunner - Editor Testsのみ実行");
            lastTestResult = "Editor Tests実行中...\n";
            RunEditorTests();
            Repaint();
        }

        private void RunPlaymodeTestsOnly()
        {
            Debug.Log("TestRunner - Playmode Testsのみ実行");
            lastTestResult = "Playmode Tests実行中...\n";
            RunPlaymodeTests();
            Repaint();
        }

        private void RunEditorTests()
        {
            var filter = new Filter()
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "BatchRenderingTool.Editor.Tests" }
            };
            
            testRunnerApi.Execute(new ExecutionSettings(filter));
            Debug.Log("TestRunner - Editor Tests実行コマンド送信");
            
            lastTestResult += "Editor Testsを実行しました。結果はTest Runnerウィンドウで確認してください。\n";
        }

        private void RunPlaymodeTests()
        {
            var filter = new Filter()
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "BatchRenderingTool.Runtime.Tests" }
            };
            
            testRunnerApi.Execute(new ExecutionSettings(filter));
            Debug.Log("TestRunner - Playmode Tests実行コマンド送信");
            
            lastTestResult += "Playmode Testsを実行しました。結果はTest Runnerウィンドウで確認してください。\n";
        }
    }

    /// <summary>
    /// テスト用のメニューアイテムを提供
    /// </summary>
    public static class TestRunnerMenuItems
    {
        [MenuItem("Window/Batch Rendering Tool/Run All Tests")]
        public static void RunAllTests()
        {
            Debug.Log("TestRunnerMenuItems - 全テスト実行");
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            
            // Editor Tests実行
            var editorFilter = new Filter()
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "BatchRenderingTool.Editor.Tests" }
            };
            testRunnerApi.Execute(new ExecutionSettings(editorFilter));
            
            // Playmode Tests実行
            var playmodeFilter = new Filter()
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "BatchRenderingTool.Runtime.Tests" }
            };
            testRunnerApi.Execute(new ExecutionSettings(playmodeFilter));
        }

        [MenuItem("Window/Batch Rendering Tool/Run Editor Tests Only")]
        public static void RunEditorTestsOnly()
        {
            Debug.Log("TestRunnerMenuItems - Editor Testsのみ実行");
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter()
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "BatchRenderingTool.Editor.Tests" }
            };
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }

        [MenuItem("Window/Batch Rendering Tool/Run Playmode Tests Only")]
        public static void RunPlaymodeTestsOnly()
        {
            Debug.Log("TestRunnerMenuItems - Playmode Testsのみ実行");
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter()
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "BatchRenderingTool.Runtime.Tests" }
            };
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
    }
}