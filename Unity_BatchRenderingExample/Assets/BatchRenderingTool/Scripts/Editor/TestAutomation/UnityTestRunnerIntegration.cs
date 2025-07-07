using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Test Runner APIとの統合
    /// 実際のテスト結果を取得してNUnit XML形式でエクスポート
    /// </summary>
    public class UnityTestRunnerIntegration : ICallbacks
    {
        private static UnityTestRunnerIntegration instance;
        private static TestRunnerApi testRunnerApi;
        
        private List<ITestResultAdaptor> testResults = new List<ITestResultAdaptor>();
        private ITestResultAdaptor rootResult;
        private bool isRunning = false;
        
        /// <summary>
        /// 初期化
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (instance == null)
            {
                instance = new UnityTestRunnerIntegration();
                testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                testRunnerApi.RegisterCallbacks(instance);
            }
        }
        
        /// <summary>
        /// テストを実行してXMLをエクスポート
        /// </summary>
        [MenuItem("Window/Batch Rendering Tool/Run Tests and Export XML")]
        public static void RunTestsAndExportXML()
        {
            if (instance.isRunning)
            {
                EditorUtility.DisplayDialog("実行中", "テストは既に実行中です。", "OK");
                return;
            }
            
            // 保存先を選択
            var path = EditorUtility.SaveFilePanel(
                "Save Test Results",
                "Assets/BatchRenderingTool/Tests/Reports",
                $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}",
                "xml"
            );
            
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            // テスト実行後にエクスポート
            instance.RunAllTestsWithCallback(() =>
            {
                instance.ExportResultsToXML(path);
            });
        }
        
        /// <summary>
        /// 最新のテスト結果をXMLエクスポート
        /// </summary>
        [MenuItem("Window/Batch Rendering Tool/Export Last Test Results as XML")]
        public static void ExportLastTestResultsAsXML()
        {
            if (instance.rootResult == null)
            {
                EditorUtility.DisplayDialog("エラー", "エクスポートするテスト結果がありません。\nまずテストを実行してください。", "OK");
                return;
            }
            
            var path = EditorUtility.SaveFilePanel(
                "Export Test Results",
                "Assets/BatchRenderingTool/Tests/Reports",
                $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}",
                "xml"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                instance.ExportResultsToXML(path);
            }
        }
        
        /// <summary>
        /// すべてのテストを実行
        /// </summary>
        private void RunAllTestsWithCallback(Action onComplete)
        {
            isRunning = true;
            testResults.Clear();
            
            var filter = new Filter()
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };
            
            // コールバックを設定
            EditorApplication.delayCall += () =>
            {
                testRunnerApi.Execute(new ExecutionSettings(filter));
                
                // 完了を待つ
                EditorApplication.update += WaitForCompletion;
                void WaitForCompletion()
                {
                    if (!isRunning)
                    {
                        EditorApplication.update -= WaitForCompletion;
                        onComplete?.Invoke();
                    }
                }
            };
        }
        
        /// <summary>
        /// テスト結果をNUnit XMLフォーマットでエクスポート
        /// </summary>
        private void ExportResultsToXML(string path)
        {
            try
            {
                var xmlContent = GenerateNUnitXMLFromResults();
                File.WriteAllText(path, xmlContent);
                
                UnityEngine.Debug.Log($"[TestRunnerIntegration] テスト結果をエクスポートしました: {path}");
                EditorUtility.DisplayDialog("エクスポート完了", $"テスト結果をエクスポートしました:\n{path}", "OK");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[TestRunnerIntegration] エクスポートエラー: {e.Message}");
                EditorUtility.DisplayDialog("エラー", $"エクスポート中にエラーが発生しました:\n{e.Message}", "OK");
            }
        }
        
        /// <summary>
        /// 実際のテスト結果からNUnit XMLを生成
        /// </summary>
        private string GenerateNUnitXMLFromResults()
        {
            if (rootResult == null)
            {
                throw new InvalidOperationException("テスト結果がありません");
            }
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };
            
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                xmlWriter.WriteStartDocument();
                
                // ルート要素
                WriteTestRun(xmlWriter, rootResult);
                
                xmlWriter.WriteEndDocument();
                return stringWriter.ToString();
            }
        }
        
        /// <summary>
        /// test-run要素を書き込み
        /// </summary>
        private void WriteTestRun(XmlWriter writer, ITestResultAdaptor result)
        {
            writer.WriteStartElement("test-run");
            writer.WriteAttributeString("id", "2");
            writer.WriteAttributeString("testcasecount", CountTestCases(result).ToString());
            writer.WriteAttributeString("result", ConvertTestStatus(result.TestStatus));
            writer.WriteAttributeString("total", CountTestCases(result).ToString());
            writer.WriteAttributeString("passed", CountPassed(result).ToString());
            writer.WriteAttributeString("failed", CountFailed(result).ToString());
            writer.WriteAttributeString("inconclusive", CountInconclusive(result).ToString());
            writer.WriteAttributeString("skipped", CountSkipped(result).ToString());
            writer.WriteAttributeString("asserts", result.AssertCount.ToString());
            writer.WriteAttributeString("engine-version", "3.5.0.0");
            writer.WriteAttributeString("clr-version", Environment.Version.ToString());
            writer.WriteAttributeString("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("duration", result.Duration.ToString("F3"));
            
            // コマンドライン
            writer.WriteStartElement("command-line");
            writer.WriteCData($"Unity Test Runner - {Application.unityVersion}");
            writer.WriteEndElement();
            
            // 環境情報
            WriteEnvironment(writer);
            
            // テストスイートまたはテストケース
            if (result.Test.IsSuite)
            {
                WriteTestSuite(writer, result);
            }
            else
            {
                WriteTestCase(writer, result);
            }
            
            writer.WriteEndElement(); // test-run
        }
        
        /// <summary>
        /// 環境情報を書き込み
        /// </summary>
        private void WriteEnvironment(XmlWriter writer)
        {
            writer.WriteStartElement("environment");
            writer.WriteAttributeString("framework-version", Application.unityVersion);
            writer.WriteAttributeString("os-version", SystemInfo.operatingSystem);
            writer.WriteAttributeString("platform", Application.platform.ToString());
            writer.WriteAttributeString("cwd", Directory.GetCurrentDirectory());
            writer.WriteAttributeString("machine-name", SystemInfo.deviceName);
            writer.WriteAttributeString("user", Environment.UserName);
            writer.WriteAttributeString("user-domain", Environment.UserDomainName);
            writer.WriteEndElement();
        }
        
        /// <summary>
        /// test-suite要素を書き込み
        /// </summary>
        private void WriteTestSuite(XmlWriter writer, ITestResultAdaptor result)
        {
            writer.WriteStartElement("test-suite");
            writer.WriteAttributeString("type", GetSuiteType(result));
            writer.WriteAttributeString("id", result.Test.Id);
            writer.WriteAttributeString("name", result.Test.Name);
            writer.WriteAttributeString("fullname", result.Test.FullName);
            writer.WriteAttributeString("runstate", result.Test.RunState.ToString());
            writer.WriteAttributeString("testcasecount", CountTestCases(result).ToString());
            writer.WriteAttributeString("result", ConvertTestStatus(result.TestStatus));
            writer.WriteAttributeString("site", result.ResultState ?? "Test");
            writer.WriteAttributeString("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("duration", result.Duration.ToString("F6"));
            writer.WriteAttributeString("total", CountTestCases(result).ToString());
            writer.WriteAttributeString("passed", CountPassed(result).ToString());
            writer.WriteAttributeString("failed", CountFailed(result).ToString());
            writer.WriteAttributeString("warnings", "0");
            writer.WriteAttributeString("inconclusive", CountInconclusive(result).ToString());
            writer.WriteAttributeString("skipped", CountSkipped(result).ToString());
            writer.WriteAttributeString("asserts", result.AssertCount.ToString());
            
            // 子要素（テストケースまたはサブスイート）
            foreach (var child in result.Children)
            {
                if (child.Test.IsSuite)
                {
                    WriteTestSuite(writer, child);
                }
                else
                {
                    WriteTestCase(writer, child);
                }
            }
            
            writer.WriteEndElement(); // test-suite
        }
        
        /// <summary>
        /// test-case要素を書き込み
        /// </summary>
        private void WriteTestCase(XmlWriter writer, ITestResultAdaptor result)
        {
            writer.WriteStartElement("test-case");
            writer.WriteAttributeString("id", result.Test.Id);
            writer.WriteAttributeString("name", result.Test.Name);
            writer.WriteAttributeString("fullname", result.Test.FullName);
            writer.WriteAttributeString("methodname", result.Test.Name);
            writer.WriteAttributeString("classname", GetClassNameFromFullName(result.Test.FullName));
            writer.WriteAttributeString("runstate", result.Test.RunState.ToString());
            writer.WriteAttributeString("seed", "0");
            writer.WriteAttributeString("result", ConvertTestStatus(result.TestStatus));
            writer.WriteAttributeString("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("duration", result.Duration.ToString("F6"));
            writer.WriteAttributeString("asserts", result.AssertCount.ToString());
            
            // 出力
            if (!string.IsNullOrEmpty(result.Output))
            {
                writer.WriteStartElement("output");
                writer.WriteCData(result.Output);
                writer.WriteEndElement();
            }
            
            // 失敗情報
            if (result.TestStatus == TestStatus.Failed)
            {
                writer.WriteStartElement("failure");
                
                if (!string.IsNullOrEmpty(result.Message))
                {
                    writer.WriteStartElement("message");
                    writer.WriteCData(result.Message);
                    writer.WriteEndElement();
                }
                
                if (!string.IsNullOrEmpty(result.StackTrace))
                {
                    writer.WriteStartElement("stack-trace");
                    writer.WriteCData(result.StackTrace);
                    writer.WriteEndElement();
                }
                
                writer.WriteEndElement(); // failure
            }
            
            // スキップ情報
            if (result.TestStatus == TestStatus.Skipped)
            {
                writer.WriteStartElement("reason");
                writer.WriteStartElement("message");
                writer.WriteCData(result.Message ?? "Test was skipped");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement(); // test-case
        }
        
        // ICallbacks実装
        public void RunStarted(ITestAdaptor testsToRun)
        {
            UnityEngine.Debug.Log("[TestRunnerIntegration] テスト実行を開始しました");
            isRunning = true;
            testResults.Clear();
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            UnityEngine.Debug.Log($"[TestRunnerIntegration] テスト実行が完了しました - {ConvertTestStatus(result.TestStatus)}");
            rootResult = result;
            isRunning = false;
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            // 個別のテスト開始
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            testResults.Add(result);
        }
        
        // ヘルパーメソッド
        private string ConvertTestStatus(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed: return "Passed";
                case TestStatus.Failed: return "Failed";
                case TestStatus.Skipped: return "Skipped";
                case TestStatus.Inconclusive: return "Inconclusive";
                default: return "Unknown";
            }
        }
        
        private string GetSuiteType(ITestResultAdaptor result)
        {
            if (result.Test.FullName.Contains(".dll"))
                return "Assembly";
            if (result.Test.Categories != null && result.Test.Categories.Any())
                return "TestFixture";
            return "TestSuite";
        }
        
        private int CountTestCases(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return 1;
            
            return result.Children.Sum(child => CountTestCases(child));
        }
        
        private int CountPassed(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Passed ? 1 : 0;
            
            return result.Children.Sum(child => CountPassed(child));
        }
        
        private int CountFailed(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Failed ? 1 : 0;
            
            return result.Children.Sum(child => CountFailed(child));
        }
        
        private int CountSkipped(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Skipped ? 1 : 0;
            
            return result.Children.Sum(child => CountSkipped(child));
        }
        
        private int CountInconclusive(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Inconclusive ? 1 : 0;
            
            return result.Children.Sum(child => CountInconclusive(child));
        }
        
        private string GetClassNameFromFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;
            
            // 最後のドットの位置を探す
            int lastDotIndex = fullName.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return fullName.Substring(0, lastDotIndex);
            }
            
            return fullName;
        }
    }
}