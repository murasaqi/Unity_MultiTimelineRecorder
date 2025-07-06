using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Natural MCP統合メニュー
    /// テスト実行とExport Resultボタンの自動化を提供
    /// </summary>
    public static class MCPTestIntegration
    {
        /// <summary>
        /// Unity Test RunnerのExport Resultボタンを自動的に押す
        /// </summary>
        [MenuItem("Tools/MCP/Export Test Results (Native)")]
        public static void ExportTestResultsNative()
        {
            UnityEngine.Debug.Log("[MCPTestIntegration] Unity Test RunnerのExport Resultボタンを自動実行します");
            TestRunnerExportAutomation.AutoExportTestResults();
        }
        
        /// <summary>
        /// テストを実行してからExport Resultボタンを自動的に押す
        /// </summary>
        [MenuItem("Tools/MCP/Run Tests and Export (Native)")]
        public static void RunTestsAndExportNative()
        {
            UnityEngine.Debug.Log("[MCPTestIntegration] テストを実行してからExport Resultボタンを自動実行します");
            TestRunnerExportAutomation.RunTestsAndAutoExport();
        }
        
        /// <summary>
        /// MCPコマンドを使用してXMLエクスポート（Native版）
        /// </summary>
        [MenuItem("Tools/MCP/Export XML via Command (Native)")]
        public static void ExportXMLViaMCPCommand()
        {
            UnityEngine.Debug.Log("[MCPTestIntegration] MCPコマンド経由でXMLエクスポート（Native版）を実行します");
            
            var args = new Dictionary<string, string>
            {
                { "native", "true" },
                { "runfirst", "false" }
            };
            
            MCPTestCommands.ExecuteCommand("test:export:xml", args);
        }
        
        /// <summary>
        /// MCPコマンドを使用してXMLエクスポート（カスタム版）
        /// </summary>
        [MenuItem("Tools/MCP/Export XML via Command (Custom)")]
        public static void ExportXMLViaMCPCommandCustom()
        {
            UnityEngine.Debug.Log("[MCPTestIntegration] MCPコマンド経由でXMLエクスポート（カスタム版）を実行します");
            
            var args = new Dictionary<string, string>
            {
                { "native", "false" },
                { "runfirst", "false" }
            };
            
            MCPTestCommands.ExecuteCommand("test:export:xml", args);
        }
        
        /// <summary>
        /// テスト実行と完全なレポート生成（Native Export付き）
        /// </summary>
        [MenuItem("Tools/MCP/Full Test Run with Native Export")]
        public static void FullTestRunWithNativeExport()
        {
            UnityEngine.Debug.Log("[MCPTestIntegration] 完全なテスト実行とネイティブエクスポートを開始します");
            
            // 1. すべてのテストを実行
            UnityNaturalMCPTestRunner.RunAllTestsAndExportReport();
            
            // 2. 実行完了後にネイティブXMLエクスポート
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    UnityEngine.Debug.Log("[MCPTestIntegration] テスト実行が完了しました。ネイティブXMLエクスポートを実行します");
                    TestRunnerExportAutomation.AutoExportTestResults();
                };
            };
        }
        
        /// <summary>
        /// テスト結果の比較検証
        /// </summary>
        [MenuItem("Tools/MCP/Verify Export Methods")]
        public static void VerifyExportMethods()
        {
            UnityEngine.Debug.Log("[MCPTestIntegration] エクスポート方法の比較検証を開始します");
            
            // 1. カスタムエクスポーターでXML生成
            var customPath = "Assets/BatchRenderingTool/Tests/Reports/CustomExport.xml";
            TestRunnerXMLExporter.ExportFromTestRunner(System.IO.Path.GetDirectoryName(customPath));
            UnityEngine.Debug.Log($"[MCPTestIntegration] カスタムエクスポート完了: {customPath}");
            
            // 2. ネイティブエクスポート実行（ユーザーが保存先を選択）
            EditorApplication.delayCall += () =>
            {
                UnityEngine.Debug.Log("[MCPTestIntegration] ネイティブエクスポートを実行します（保存ダイアログが表示されます）");
                TestRunnerExportAutomation.AutoExportTestResults();
                
                UnityEngine.Debug.Log("[MCPTestIntegration] 両方のXMLファイルを比較して、エクスポート方法の違いを確認してください");
            };
        }
        
        /// <summary>
        /// MCPコマンドのヘルプを表示
        /// </summary>
        [MenuItem("Tools/MCP/Show Test Commands Help")]
        public static void ShowTestCommandsHelp()
        {
            MCPTestCommands.ExecuteCommand("help", null);
        }
        
        /// <summary>
        /// テスト状態を確認
        /// </summary>
        [MenuItem("Tools/MCP/Show Test Status")]
        public static void ShowTestStatus()
        {
            MCPTestCommands.ExecuteCommand("test:status", null);
        }
    }
}