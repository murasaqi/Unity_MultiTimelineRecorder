using System;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Natural MCP用のシンプルなテストランナー
    /// Unity Test Runner標準のExport Result機能のみを使用
    /// </summary>
    public static class SimpleMCPTestRunner
    {
        /// <summary>
        /// Unity Test RunnerのExport Resultボタンを押す（標準機能）
        /// </summary>
        [MenuItem("Tools/MCP Test Runner/Export Test Results")]
        public static void ExportTestResults()
        {
            Debug.Log("[SimpleMCPTestRunner] Unity Test RunnerのExport Resultを実行します");
            TestRunnerExportAutomation.AutoExportTestResults();
        }
        
        /// <summary>
        /// テストを実行してからExport Resultボタンを押す（標準機能）
        /// </summary>
        [MenuItem("Tools/MCP Test Runner/Run Tests and Export Results")]
        public static void RunTestsAndExportResults()
        {
            Debug.Log("[SimpleMCPTestRunner] テストを実行してからExport Resultを実行します");
            TestRunnerExportAutomation.RunTestsAndAutoExport();
        }
    }
}