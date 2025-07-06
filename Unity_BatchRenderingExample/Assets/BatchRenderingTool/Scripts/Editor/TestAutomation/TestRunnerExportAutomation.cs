using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Test RunnerのExport Resultボタンの動作を自動化
    /// </summary>
    public static class TestRunnerExportAutomation
    {
        private static ITestResultAdaptor lastTestResult;
        private static bool isWaitingForResults = false;
        
        /// <summary>
        /// Test RunnerのExport Resultボタンと同じ動作を実行
        /// </summary>
        public static void AutoExportTestResults()
        {
            try
            {
                // Unity Test Runnerの内部クラスにアクセス
                var testRunnerWindowType = Type.GetType("UnityEditor.TestTools.TestRunner.TestRunnerWindow, UnityEditor.TestRunner");
                if (testRunnerWindowType == null)
                {
                    Debug.LogError("[TestRunnerExportAutomation] Test Runner Windowクラスが見つかりません");
                    return;
                }
                
                // Test Runner Windowのインスタンスを取得または作成
                var windows = Resources.FindObjectsOfTypeAll(testRunnerWindowType);
                object testRunnerWindow = null;
                
                if (windows.Length > 0)
                {
                    testRunnerWindow = windows[0];
                }
                else
                {
                    // ウィンドウが開いていない場合は開く
                    var showMethod = testRunnerWindowType.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public);
                    if (showMethod != null)
                    {
                        showMethod.Invoke(null, null);
                        windows = Resources.FindObjectsOfTypeAll(testRunnerWindowType);
                        if (windows.Length > 0)
                        {
                            testRunnerWindow = windows[0];
                        }
                    }
                }
                
                if (testRunnerWindow == null)
                {
                    Debug.LogError("[TestRunnerExportAutomation] Test Runner Windowのインスタンスを取得できませんでした");
                    return;
                }
                
                // Export Resultメソッドを呼び出す
                InvokeExportResult(testRunnerWindow, testRunnerWindowType);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestRunnerExportAutomation] エラー: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// テストを実行してから結果をエクスポート
        /// </summary>
        public static void RunTestsAndAutoExport()
        {
            Debug.Log("[TestRunnerExportAutomation] テストを実行してから結果をエクスポートします");
            
            // Test Runner APIのコールバックを登録
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestResultCallback());
            
            // すべてのテストを実行
            var filter = new Filter()
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };
            
            isWaitingForResults = true;
            api.Execute(new ExecutionSettings(filter));
            
            Debug.Log("[TestRunnerExportAutomation] テスト実行を開始しました。完了後に自動的にエクスポートされます。");
        }
        
        /// <summary>
        /// Export Resultメソッドを実行
        /// </summary>
        private static void InvokeExportResult(object testRunnerWindow, Type windowType)
        {
            try
            {
                // まず、m_SelectedTestTypesフィールドを取得してテスト結果があるか確認
                var selectedTestTypesField = windowType.GetField("m_SelectedTestTypes", BindingFlags.Instance | BindingFlags.NonPublic);
                if (selectedTestTypesField == null)
                {
                    Debug.LogWarning("[TestRunnerExportAutomation] m_SelectedTestTypesフィールドが見つかりません");
                }
                
                // ExportTestResults メソッドを探す（Unity 2021以降）
                var exportMethod = windowType.GetMethod("ExportTestResults", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                
                if (exportMethod == null)
                {
                    // Unity 2020以前の場合
                    exportMethod = windowType.GetMethod("ExportResults", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                
                if (exportMethod == null)
                {
                    // さらに古いバージョンまたは異なる名前の場合
                    var methods = windowType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var method in methods)
                    {
                        if (method.Name.Contains("Export") && method.Name.Contains("Result"))
                        {
                            exportMethod = method;
                            break;
                        }
                    }
                }
                
                if (exportMethod != null)
                {
                    Debug.Log($"[TestRunnerExportAutomation] Export Resultメソッドを実行します: {exportMethod.Name}");
                    
                    // 保存ダイアログを表示せずに自動保存する場合
                    var defaultPath = Path.Combine(Application.dataPath, "..", "TestResults");
                    if (!Directory.Exists(defaultPath))
                    {
                        Directory.CreateDirectory(defaultPath);
                    }
                    
                    var fileName = $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                    var fullPath = Path.Combine(defaultPath, fileName);
                    
                    // メソッドのパラメータを確認
                    var parameters = exportMethod.GetParameters();
                    if (parameters.Length == 0)
                    {
                        // パラメータなしの場合（保存ダイアログが表示される）
                        exportMethod.Invoke(testRunnerWindow, null);
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        // パスを指定できる場合
                        exportMethod.Invoke(testRunnerWindow, new object[] { fullPath });
                    }
                    else
                    {
                        Debug.LogWarning($"[TestRunnerExportAutomation] 予期しないパラメータ: {parameters.Length}個");
                        exportMethod.Invoke(testRunnerWindow, null);
                    }
                    
                    Debug.Log($"[TestRunnerExportAutomation] エクスポート完了: {fullPath}");
                }
                else
                {
                    // 直接的な方法が失敗した場合、GUI操作をシミュレート
                    SimulateExportButton(testRunnerWindow, windowType);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestRunnerExportAutomation] Export Result実行エラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// Export Resultボタンのクリックをシミュレート
        /// </summary>
        private static void SimulateExportButton(object testRunnerWindow, Type windowType)
        {
            try
            {
                // Test Runner内部のExportボタン処理を探す
                var guiMethod = windowType.GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (guiMethod != null)
                {
                    Debug.Log("[TestRunnerExportAutomation] GUI操作によるエクスポートを試みます");
                    
                    // EditorUtility.SaveFilePanelを使用して保存先を指定
                    var path = EditorUtility.SaveFilePanel(
                        "Save Test Results",
                        "Assets/BatchRenderingTool/Tests/Reports",
                        $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}",
                        "xml"
                    );
                    
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Test Runnerの結果を取得してXMLとして保存
                        if (lastTestResult != null)
                        {
                            SaveTestResultAsXML(lastTestResult, path);
                            Debug.Log($"[TestRunnerExportAutomation] テスト結果を保存しました: {path}");
                        }
                        else
                        {
                            Debug.LogWarning("[TestRunnerExportAutomation] エクスポートする最新のテスト結果がありません");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestRunnerExportAutomation] GUI操作エラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// テスト結果をXMLとして保存（Test Runner標準形式）
        /// </summary>
        private static void SaveTestResultAsXML(ITestResultAdaptor result, string path)
        {
            // Unity Test RunnerのXMLエクスポート処理を使用
            var xmlExporter = Type.GetType("UnityEditor.TestTools.TestRunner.Api.XmlResultWriter, UnityEditor.TestRunner");
            if (xmlExporter != null)
            {
                try
                {
                    var writer = Activator.CreateInstance(xmlExporter);
                    var writeMethod = xmlExporter.GetMethod("WriteResultToFile", BindingFlags.Instance | BindingFlags.Public);
                    if (writeMethod != null)
                    {
                        writeMethod.Invoke(writer, new object[] { result, path });
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TestRunnerExportAutomation] XmlResultWriter使用エラー: {e.Message}");
                }
            }
            
            // フォールバック: カスタムXML生成
            TestRunnerXMLExporter.ExportFromTestRunner(Path.GetDirectoryName(path));
        }
        
        /// <summary>
        /// Test Runner APIコールバック
        /// </summary>
        private class TestResultCallback : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("[TestRunnerExportAutomation] テスト実行開始");
            }
            
            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log("[TestRunnerExportAutomation] テスト実行完了");
                lastTestResult = result;
                
                if (isWaitingForResults)
                {
                    isWaitingForResults = false;
                    // テスト完了後に自動的にエクスポート
                    EditorApplication.delayCall += () =>
                    {
                        AutoExportTestResults();
                    };
                }
            }
            
            public void TestStarted(ITestAdaptor test)
            {
                // 個別のテスト開始
            }
            
            public void TestFinished(ITestResultAdaptor result)
            {
                // 個別のテスト終了
            }
        }
    }
}