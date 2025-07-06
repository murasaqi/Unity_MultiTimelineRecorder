using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Test Runner自動実行ツール
    /// Unity Natural MCPを使用してテストの自動実行を管理します
    /// </summary>
    public class TestRunnerAutomation : EditorWindow
    {
        // テスト実行設定
        [Serializable]
        public class TestRunConfiguration
        {
            public string assemblyName;
            public bool enabled = true;
            public TestMode testMode;
            public List<string> categories = new List<string>();
            public List<string> testNames = new List<string>();
            public int retryCount = 0;
            public bool continueOnFailure = true;
        }

        public enum TestMode
        {
            EditMode,
            PlayMode
        }

        [Serializable]
        public class TestResult
        {
            public string assemblyName;
            public TestMode testMode;
            public int passCount;
            public int failCount;
            public int skipCount;
            public List<FailedTest> failedTests = new List<FailedTest>();
            public DateTime executionTime;
            public float duration;
            public bool success;
        }

        [Serializable]
        public class FailedTest
        {
            public string name;
            public string fullName;
            public string message;
            public string stackTrace;
            public float duration;
        }

        private List<TestRunConfiguration> testConfigurations = new List<TestRunConfiguration>();
        private List<TestResult> testResults = new List<TestResult>();
        private Vector2 scrollPosition;
        private bool isRunning = false;
        private int currentTestIndex = 0;
        private string statusMessage = "";
        
        // UI表示設定
        private bool showConfigurations = true;
        private bool showResults = true;
        private bool showFailedDetails = false;
        
        // 自動実行設定
        private bool autoRunOnCompile = false;
        private bool autoRetryFailed = false;
        private int maxRetryCount = 3;

        [MenuItem("Window/Batch Rendering Tool/Test Runner Automation")]
        public static void ShowWindow()
        {
            var window = GetWindow<TestRunnerAutomation>("Test Runner Automation");
            window.minSize = new Vector2(600, 400);
            window.LoadConfigurations();
        }

        private void OnEnable()
        {
            LoadConfigurations();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SaveConfigurations();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space();

            if (showConfigurations)
            {
                DrawTestConfigurations();
                EditorGUILayout.Space();
            }

            if (showResults)
            {
                DrawTestResults();
                EditorGUILayout.Space();
            }

            DrawStatusBar();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Unity Test Runner Automation", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 実行ボタン
            EditorGUI.BeginDisabledGroup(isRunning);
            if (GUILayout.Button("Run All Tests", GUILayout.Width(120)))
            {
                StartTestExecution();
            }
            EditorGUI.EndDisabledGroup();

            // 停止ボタン
            EditorGUI.BeginDisabledGroup(!isRunning);
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
            {
                StopTestExecution();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // 自動実行設定
            EditorGUILayout.BeginHorizontal();
            autoRunOnCompile = EditorGUILayout.Toggle("Auto Run on Compile", autoRunOnCompile);
            autoRetryFailed = EditorGUILayout.Toggle("Auto Retry Failed", autoRetryFailed);
            if (autoRetryFailed)
            {
                maxRetryCount = EditorGUILayout.IntField("Max Retry", maxRetryCount, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTestConfigurations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            showConfigurations = EditorGUILayout.Foldout(showConfigurations, "Test Configurations", true);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Scan Assemblies", GUILayout.Width(100)))
            {
                ScanTestAssemblies();
            }
            
            if (GUILayout.Button("Add", GUILayout.Width(40)))
            {
                AddTestConfiguration();
            }
            EditorGUILayout.EndHorizontal();

            if (showConfigurations)
            {
                for (int i = 0; i < testConfigurations.Count; i++)
                {
                    DrawTestConfigurationItem(i);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTestConfigurationItem(int index)
        {
            var config = testConfigurations[index];
            
            EditorGUILayout.BeginHorizontal();
            
            config.enabled = EditorGUILayout.Toggle(config.enabled, GUILayout.Width(20));
            config.assemblyName = EditorGUILayout.TextField(config.assemblyName, GUILayout.Width(200));
            config.testMode = (TestMode)EditorGUILayout.EnumPopup(config.testMode, GUILayout.Width(100));
            
            if (GUILayout.Button("Configure", GUILayout.Width(70)))
            {
                ShowConfigurationWindow(config);
            }
            
            if (GUILayout.Button("Run", GUILayout.Width(40)))
            {
                RunSingleTest(config);
            }
            
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                testConfigurations.RemoveAt(index);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTestResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            showResults = EditorGUILayout.Foldout(showResults, "Test Results", true);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear Results", GUILayout.Width(100)))
            {
                testResults.Clear();
            }
            
            if (GUILayout.Button("Export Report", GUILayout.Width(100)))
            {
                ExportTestReport();
            }
            EditorGUILayout.EndHorizontal();

            if (showResults && testResults.Count > 0)
            {
                DrawResultsSummary();
                
                foreach (var result in testResults)
                {
                    DrawTestResultItem(result);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResultsSummary()
        {
            int totalPass = testResults.Sum(r => r.passCount);
            int totalFail = testResults.Sum(r => r.failCount);
            int totalSkip = testResults.Sum(r => r.skipCount);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Total Results: ", EditorStyles.boldLabel);
            
            GUI.color = Color.green;
            GUILayout.Label($"Pass: {totalPass}", GUILayout.Width(80));
            
            GUI.color = totalFail > 0 ? Color.red : Color.gray;
            GUILayout.Label($"Fail: {totalFail}", GUILayout.Width(80));
            
            GUI.color = Color.yellow;
            GUILayout.Label($"Skip: {totalSkip}", GUILayout.Width(80));
            
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawTestResultItem(TestResult result)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // ヘッダー
            EditorGUILayout.BeginHorizontal();
            
            GUI.color = result.success ? Color.green : Color.red;
            GUILayout.Label(result.success ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;
            
            GUILayout.Label($"{result.assemblyName} ({result.testMode})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{result.executionTime:yyyy-MM-dd HH:mm:ss}", GUILayout.Width(150));
            GUILayout.Label($"{result.duration:F2}s", GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            
            // 統計
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            
            GUI.color = Color.green;
            GUILayout.Label($"Pass: {result.passCount}", GUILayout.Width(80));
            
            GUI.color = result.failCount > 0 ? Color.red : Color.gray;
            GUILayout.Label($"Fail: {result.failCount}", GUILayout.Width(80));
            
            GUI.color = Color.yellow;
            GUILayout.Label($"Skip: {result.skipCount}", GUILayout.Width(80));
            
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // 失敗詳細
            if (result.failedTests.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                showFailedDetails = EditorGUILayout.Foldout(showFailedDetails, "Failed Tests");
                EditorGUILayout.EndHorizontal();
                
                if (showFailedDetails)
                {
                    foreach (var failed in result.failedTests)
                    {
                        DrawFailedTestDetail(failed);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawFailedTestDetail(FailedTest failed)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(40);
            
            GUI.color = Color.red;
            GUILayout.Label(failed.name, EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.LabelField("Message:", failed.message, EditorStyles.wordWrappedLabel);
            
            if (!string.IsNullOrEmpty(failed.stackTrace))
            {
                if (GUILayout.Button("Show Stack Trace", GUILayout.Width(120)))
                {
                    Debug.LogError($"{failed.fullName}\n{failed.message}\n{failed.stackTrace}");
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (isRunning)
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"Running: {statusMessage}", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = Color.white;
                GUILayout.Label($"Ready", EditorStyles.miniLabel);
            }
            
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            
            if (testResults.Count > 0)
            {
                var lastResult = testResults.LastOrDefault();
                if (lastResult != null)
                {
                    GUILayout.Label($"Last run: {lastResult.executionTime:HH:mm:ss}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void StartTestExecution()
        {
            if (isRunning) return;
            
            isRunning = true;
            currentTestIndex = 0;
            testResults.Clear();
            
            Debug.Log("[TestRunnerAutomation] テスト実行を開始します");
        }

        private void StopTestExecution()
        {
            isRunning = false;
            currentTestIndex = 0;
            statusMessage = "Stopped";
            
            Debug.Log("[TestRunnerAutomation] テスト実行を停止しました");
        }

        private void OnEditorUpdate()
        {
            if (!isRunning) return;
            
            // 現在のテストを実行
            if (currentTestIndex < testConfigurations.Count)
            {
                var config = testConfigurations[currentTestIndex];
                if (config.enabled)
                {
                    ExecuteTest(config);
                }
                else
                {
                    currentTestIndex++;
                }
            }
            else
            {
                // すべてのテストが完了
                CompleteTestExecution();
            }
        }

        private void RunSingleTest(TestRunConfiguration config)
        {
            Debug.Log($"[TestRunnerAutomation] 単体テスト実行: {config.assemblyName} ({config.testMode})");
            
            isRunning = true;
            statusMessage = $"実行中: {config.assemblyName}";
            
            ExecuteTest(config, () =>
            {
                isRunning = false;
                statusMessage = "完了";
            });
        }
        
        private void ExecuteTest(TestRunConfiguration config, Action onComplete = null)
        {
            statusMessage = $"実行中: {config.assemblyName} ({config.testMode})";
            
            // コンパイルエラーをチェック
            if (TestExecutionEngine.CheckCompileErrors(out var errors))
            {
                Debug.LogError($"[TestRunnerAutomation] コンパイルエラーが検出されました: {string.Join("\n", errors)}");
                AddErrorResult(config, "コンパイルエラー", string.Join("\n", errors));
                currentTestIndex++;
                onComplete?.Invoke();
                return;
            }
            
            // テストモードに応じて実行
            if (config.testMode == TestMode.EditMode)
            {
                TestExecutionEngine.RunEditModeTests(config, result =>
                {
                    HandleTestResult(result, config);
                    onComplete?.Invoke();
                }, UpdateProgress);
            }
            else
            {
                TestExecutionEngine.RunPlayModeTests(config, result =>
                {
                    HandleTestResult(result, config);
                    onComplete?.Invoke();
                }, UpdateProgress);
            }
        }
        
        private void HandleTestResult(TestResult result, TestRunConfiguration config)
        {
            testResults.Add(result);
            
            // 失敗時のリトライ処理
            if (!result.success && autoRetryFailed && config.retryCount < maxRetryCount)
            {
                config.retryCount++;
                Debug.Log($"[TestRunnerAutomation] テスト失敗。リトライ {config.retryCount}/{maxRetryCount}");
                return; // リトライのため、インデックスを進めない
            }
            
            // エラー時の処理
            if (!result.success && !config.continueOnFailure)
            {
                StopTestExecution();
                Debug.LogError($"[TestRunnerAutomation] テスト失敗のため実行を停止しました: {config.assemblyName}");
                return;
            }
            
            currentTestIndex++;
            config.retryCount = 0; // リトライカウントをリセット
        }
        
        private void UpdateProgress(string message, float progress)
        {
            statusMessage = message;
            Repaint();
        }
        
        private void CompleteTestExecution()
        {
            isRunning = false;
            statusMessage = "テスト実行完了";
            
            // 結果サマリーをログ出力
            int totalPass = testResults.Sum(r => r.passCount);
            int totalFail = testResults.Sum(r => r.failCount);
            int totalSkip = testResults.Sum(r => r.skipCount);
            
            Debug.Log($"[TestRunnerAutomation] テスト実行完了 - Pass: {totalPass}, Fail: {totalFail}, Skip: {totalSkip}");
            
            // 失敗があった場合は詳細を出力
            foreach (var result in testResults.Where(r => !r.success))
            {
                Debug.LogError($"[TestRunnerAutomation] 失敗: {result.assemblyName} - {result.failCount}個のテストが失敗");
                foreach (var failed in result.failedTests)
                {
                    Debug.LogError($"  - {failed.name}: {failed.message}");
                }
            }
            
            // 自動レポート生成
            if (testResults.Any(r => !r.success))
            {
                var reportPath = $"Assets/BatchRenderingTool/Tests/Reports/TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                SaveTestReport(reportPath);
            }
        }
        
        private void AddErrorResult(TestRunConfiguration config, string errorType, string errorMessage)
        {
            testResults.Add(new TestResult
            {
                assemblyName = config.assemblyName,
                testMode = config.testMode,
                executionTime = DateTime.Now,
                duration = 0,
                success = false,
                failCount = 1,
                failedTests = new List<FailedTest>
                {
                    new FailedTest
                    {
                        name = errorType,
                        fullName = errorType,
                        message = errorMessage,
                        stackTrace = "",
                        duration = 0
                    }
                }
            });
        }
        
        private void SaveTestReport(string path)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                var report = new
                {
                    timestamp = DateTime.Now,
                    results = testResults,
                    summary = new
                    {
                        totalTests = testResults.Count,
                        totalPass = testResults.Sum(r => r.passCount),
                        totalFail = testResults.Sum(r => r.failCount),
                        totalSkip = testResults.Sum(r => r.skipCount),
                        success = testResults.All(r => r.success)
                    }
                };
                
                var json = JsonUtility.ToJson(report, true);
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"[TestRunnerAutomation] テストレポートを保存しました: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestRunnerAutomation] レポート保存エラー: {e.Message}");
            }
        }

        private void ScanTestAssemblies()
        {
            testConfigurations.Clear();
            
            // Editor Tests
            testConfigurations.Add(new TestRunConfiguration
            {
                assemblyName = "BatchRenderingTool.Editor.Tests",
                testMode = TestMode.EditMode,
                enabled = true
            });
            
            // Runtime Tests
            testConfigurations.Add(new TestRunConfiguration
            {
                assemblyName = "BatchRenderingTool.Runtime.Tests",
                testMode = TestMode.PlayMode,
                enabled = true
            });
            
            Debug.Log($"[TestRunnerAutomation] {testConfigurations.Count}個のテストアセンブリを検出しました");
        }

        private void AddTestConfiguration()
        {
            testConfigurations.Add(new TestRunConfiguration());
        }

        private void ShowConfigurationWindow(TestRunConfiguration config)
        {
            TestConfigurationWindow.ShowWindow(config, updatedConfig =>
            {
                // 設定が更新されたら保存
                SaveConfigurations();
                Repaint();
            });
        }

        private void ExportTestReport()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("HTML レポート"), false, () => 
            {
                var path = EditorUtility.SaveFilePanel("Export HTML Report", "Assets/BatchRenderingTool/Tests/Reports", "TestReport", "html");
                if (!string.IsNullOrEmpty(path))
                {
                    TestReportGenerator.GenerateReport(testResults, path, TestReportGenerator.ReportFormat.Html);
                    if (EditorUtility.DisplayDialog("レポート生成完了", "HTMLレポートを生成しました。ブラウザで開きますか？", "開く", "閉じる"))
                    {
                        System.Diagnostics.Process.Start(path);
                    }
                }
            });
            
            menu.AddItem(new GUIContent("Markdown レポート"), false, () => 
            {
                var path = EditorUtility.SaveFilePanel("Export Markdown Report", "Assets/BatchRenderingTool/Tests/Reports", "TestReport", "md");
                if (!string.IsNullOrEmpty(path))
                {
                    TestReportGenerator.GenerateReport(testResults, path, TestReportGenerator.ReportFormat.Markdown);
                }
            });
            
            menu.AddItem(new GUIContent("JSON レポート"), false, () => 
            {
                var path = EditorUtility.SaveFilePanel("Export JSON Report", "Assets/BatchRenderingTool/Tests/Reports", "TestReport", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    TestReportGenerator.GenerateReport(testResults, path, TestReportGenerator.ReportFormat.Json);
                }
            });
            
            menu.AddItem(new GUIContent("CSV レポート"), false, () => 
            {
                var path = EditorUtility.SaveFilePanel("Export CSV Report", "Assets/BatchRenderingTool/Tests/Reports", "TestReport", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    TestReportGenerator.GenerateReport(testResults, path, TestReportGenerator.ReportFormat.Csv);
                }
            });
            
            menu.ShowAsContext();
        }

        private void LoadConfigurations()
        {
            var json = EditorPrefs.GetString("TestRunnerAutomation.Configurations", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<ConfigurationWrapper>(json);
                    if (wrapper != null && wrapper.configurations != null)
                    {
                        testConfigurations = wrapper.configurations;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TestRunnerAutomation] 設定の読み込みに失敗しました: {e.Message}");
                }
            }
            
            if (testConfigurations.Count == 0)
            {
                ScanTestAssemblies();
            }
        }

        private void SaveConfigurations()
        {
            var wrapper = new ConfigurationWrapper { configurations = testConfigurations };
            var json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString("TestRunnerAutomation.Configurations", json);
        }

        [Serializable]
        private class ConfigurationWrapper
        {
            public List<TestRunConfiguration> configurations;
        }
    }
}