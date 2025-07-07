using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Natural MCPを使用してテストを実行するエンジン
    /// </summary>
    public static class TestExecutionEngine
    {
        public delegate void TestExecutionCallback(TestRunnerAutomation.TestResult result);
        public delegate void TestProgressCallback(string message, float progress);
        
        /// <summary>
        /// Edit Modeテストを実行
        /// </summary>
        public static void RunEditModeTests(
            TestRunnerAutomation.TestRunConfiguration config,
            TestExecutionCallback onComplete,
            TestProgressCallback onProgress = null)
        {
            onProgress?.Invoke($"Edit Modeテストを実行中: {config.assemblyName}", 0f);
            
            try
            {
                // Unity Natural MCPのrun_edit_mode_testsをシミュレート
                // 実際のMCP実装では、MCPサーバーにリクエストを送信します
                var startTime = DateTime.Now;
                
                // テスト実行のシミュレーション
                var result = new TestRunnerAutomation.TestResult
                {
                    assemblyName = config.assemblyName,
                    testMode = TestRunnerAutomation.TestMode.EditMode,
                    executionTime = startTime
                };
                
                // TODO: 実際のMCP呼び出しに置き換える
                SimulateTestExecution(config, result, onProgress);
                
                result.duration = (float)(DateTime.Now - startTime).TotalSeconds;
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestExecutionEngine] Edit Modeテスト実行エラー: {e.Message}");
                onComplete?.Invoke(CreateErrorResult(config, e.Message));
            }
        }
        
        /// <summary>
        /// Play Modeテストを実行
        /// </summary>
        public static void RunPlayModeTests(
            TestRunnerAutomation.TestRunConfiguration config,
            TestExecutionCallback onComplete,
            TestProgressCallback onProgress = null)
        {
            onProgress?.Invoke($"Play Modeテストを実行中: {config.assemblyName}", 0f);
            
            try
            {
                var startTime = DateTime.Now;
                
                var result = new TestRunnerAutomation.TestResult
                {
                    assemblyName = config.assemblyName,
                    testMode = TestRunnerAutomation.TestMode.PlayMode,
                    executionTime = startTime
                };
                
                // TODO: 実際のMCP呼び出しに置き換える
                SimulateTestExecution(config, result, onProgress);
                
                result.duration = (float)(DateTime.Now - startTime).TotalSeconds;
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestExecutionEngine] Play Modeテスト実行エラー: {e.Message}");
                onComplete?.Invoke(CreateErrorResult(config, e.Message));
            }
        }
        
        /// <summary>
        /// コンパイルエラーをチェック
        /// </summary>
        public static bool CheckCompileErrors(out List<string> errors)
        {
            errors = new List<string>();
            
            try
            {
                // Unity Natural MCPのget_compile_logsをシミュレート
                // TODO: 実際のMCP呼び出しに置き換える
                
                // 現在のコンパイルエラーをチェック
                if (EditorUtility.scriptCompilationFailed)
                {
                    errors.Add("スクリプトコンパイルエラーが発生しています");
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestExecutionEngine] コンパイルエラーチェック失敗: {e.Message}");
                errors.Add($"エラーチェック失敗: {e.Message}");
                return true;
            }
        }
        
        /// <summary>
        /// 現在のコンソールログを取得
        /// </summary>
        public static List<ConsoleLog> GetCurrentConsoleLogs(
            string[] logTypes = null,
            string filter = "",
            int maxCount = 100)
        {
            var logs = new List<ConsoleLog>();
            
            try
            {
                // Unity Natural MCPのget_current_console_logsをシミュレート
                // TODO: 実際のMCP呼び出しに置き換える
                
                // Unity Editorのログ取得（簡易実装）
                var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntries != null)
                {
                    var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                    var getCountMethod = logEntries.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                    
                    if (getCountMethod != null)
                    {
                        int count = (int)getCountMethod.Invoke(null, null);
                        UnityEngine.Debug.Log($"[TestExecutionEngine] コンソールログ数: {count}");
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestExecutionEngine] ログ取得エラー: {e.Message}");
            }
            
            return logs;
        }
        
        /// <summary>
        /// コンソールログをクリア
        /// </summary>
        public static void ClearConsoleLogs()
        {
            try
            {
                // Unity Natural MCPのclear_console_logsをシミュレート
                var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntries != null)
                {
                    var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                    clearMethod?.Invoke(null, null);
                    UnityEngine.Debug.Log("[TestExecutionEngine] コンソールログをクリアしました");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestExecutionEngine] ログクリアエラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// アセットをリフレッシュ
        /// </summary>
        public static void RefreshAssets()
        {
            try
            {
                // Unity Natural MCPのrefresh_assetsをシミュレート
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[TestExecutionEngine] アセットをリフレッシュしました");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestExecutionEngine] アセットリフレッシュエラー: {e.Message}");
            }
        }
        
        // テスト実行のシミュレーション（実際のMCP実装で置き換え）
        private static void SimulateTestExecution(
            TestRunnerAutomation.TestRunConfiguration config,
            TestRunnerAutomation.TestResult result,
            TestProgressCallback onProgress)
        {
            // テスト実行の進捗をシミュレート
            onProgress?.Invoke("テストを検索中...", 0.2f);
            System.Threading.Thread.Sleep(500);
            
            onProgress?.Invoke("テストを実行中...", 0.5f);
            System.Threading.Thread.Sleep(1000);
            
            onProgress?.Invoke("結果を収集中...", 0.8f);
            System.Threading.Thread.Sleep(500);
            
            // ダミーの結果を生成
            result.passCount = UnityEngine.Random.Range(10, 50);
            result.failCount = UnityEngine.Random.Range(0, 5);
            result.skipCount = UnityEngine.Random.Range(0, 3);
            result.success = result.failCount == 0;
            
            // 失敗テストのダミーデータ
            if (result.failCount > 0)
            {
                for (int i = 0; i < result.failCount; i++)
                {
                    result.failedTests.Add(new TestRunnerAutomation.FailedTest
                    {
                        name = $"TestCase_{i + 1}",
                        fullName = $"{config.assemblyName}.TestClass.TestCase_{i + 1}",
                        message = "Expected: True\nBut was: False",
                        stackTrace = "at TestClass.TestCase() in TestFile.cs:line 42",
                        duration = UnityEngine.Random.Range(0.01f, 0.5f)
                    });
                }
            }
            
            onProgress?.Invoke("完了", 1.0f);
        }
        
        private static TestRunnerAutomation.TestResult CreateErrorResult(
            TestRunnerAutomation.TestRunConfiguration config,
            string errorMessage)
        {
            return new TestRunnerAutomation.TestResult
            {
                assemblyName = config.assemblyName,
                testMode = config.testMode == TestRunnerAutomation.TestMode.EditMode 
                    ? TestRunnerAutomation.TestMode.EditMode 
                    : TestRunnerAutomation.TestMode.PlayMode,
                executionTime = DateTime.Now,
                duration = 0,
                success = false,
                failCount = 1,
                failedTests = new List<TestRunnerAutomation.FailedTest>
                {
                    new TestRunnerAutomation.FailedTest
                    {
                        name = "TestExecutionError",
                        fullName = "TestExecutionError",
                        message = errorMessage,
                        stackTrace = "",
                        duration = 0
                    }
                }
            };
        }
        
        public class ConsoleLog
        {
            public string message;
            public string stackTrace;
            public LogType logType;
            public DateTime timestamp;
        }
    }
}