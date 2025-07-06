using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Natural MCP用のシンプルなコマンドインターフェース
    /// Unity Test Runner標準機能のみを使用
    /// </summary>
    public static class SimpleMCPCommands
    {
        /// <summary>
        /// MCPコマンドを実行
        /// </summary>
        public static void Execute(string command)
        {
            Debug.Log($"[SimpleMCPCommands] コマンド実行: {command}");
            
            switch (command.ToLower())
            {
                case "test:export":
                    ExportTestResults();
                    break;
                    
                case "test:run-and-export":
                    RunTestsAndExport();
                    break;
                    
                case "test:help":
                    ShowHelp();
                    break;
                    
                default:
                    Debug.LogError($"[SimpleMCPCommands] 不明なコマンド: {command}");
                    ShowHelp();
                    break;
            }
        }
        
        /// <summary>
        /// Test Runner標準のExport Result機能を実行
        /// </summary>
        private static void ExportTestResults()
        {
            Debug.Log("[SimpleMCPCommands] Unity Test Runner標準のExport Resultを実行します");
            
            try
            {
                TestRunnerExportAutomation.AutoExportTestResults();
                Debug.Log("[SimpleMCPCommands] Export Result実行を開始しました");
                Debug.Log("[SimpleMCPCommands] 保存ダイアログが表示される場合があります");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleMCPCommands] Export Result実行エラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// テストを実行してからTest Runner標準のExport Result機能を実行
        /// </summary>
        private static void RunTestsAndExport()
        {
            Debug.Log("[SimpleMCPCommands] テストを実行してからExport Resultを実行します");
            
            try
            {
                TestRunnerExportAutomation.RunTestsAndAutoExport();
                Debug.Log("[SimpleMCPCommands] テスト実行とExport Resultを開始しました");
                Debug.Log("[SimpleMCPCommands] テスト完了後に自動的にExport Resultが実行されます");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleMCPCommands] テスト実行エラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// ヘルプを表示
        /// </summary>
        private static void ShowHelp()
        {
            Debug.Log("========================================");
            Debug.Log("Unity Natural MCP テストコマンド（標準機能版）");
            Debug.Log("========================================");
            Debug.Log("利用可能なコマンド:");
            Debug.Log("  test:export         - Unity Test RunnerのExport Resultを実行");
            Debug.Log("  test:run-and-export - テスト実行後にExport Resultを実行");
            Debug.Log("  test:help          - このヘルプを表示");
            Debug.Log("");
            Debug.Log("注意事項:");
            Debug.Log("- Export時に保存ダイアログが表示される場合があります");
            Debug.Log("- Unity Test Runner標準のNUnit XML形式で出力されます");
            Debug.Log("- Test Runnerウィンドウが自動的に開く場合があります");
            Debug.Log("========================================");
        }
        
        /// <summary>
        /// MCPからの直接呼び出し用メニュー
        /// </summary>
        [MenuItem("Tools/MCP Test Runner/Execute Export Command")]
        public static void ExecuteExportCommand()
        {
            Execute("test:export");
        }
        
        [MenuItem("Tools/MCP Test Runner/Execute Run and Export Command")]
        public static void ExecuteRunAndExportCommand()
        {
            Execute("test:run-and-export");
        }
        
        [MenuItem("Tools/MCP Test Runner/Show Help")]
        public static void ShowHelpMenu()
        {
            Execute("test:help");
        }
    }
}