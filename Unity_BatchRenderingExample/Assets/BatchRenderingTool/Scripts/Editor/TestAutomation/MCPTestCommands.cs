using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Unity Natural MCP経由で実行可能なテストコマンド集
    /// コマンドライン引数やMCPクライアントから呼び出し可能
    /// </summary>
    public static class MCPTestCommands
    {
        /// <summary>
        /// コマンドライン引数を解析して適切なテストコマンドを実行
        /// </summary>
        public static void ExecuteCommand(string command, Dictionary<string, string> args = null)
        {
            if (args == null) args = new Dictionary<string, string>();
            
            UnityEngine.Debug.Log($"[MCPTestCommands] コマンド実行: {command}");
            
            switch (command.ToLower())
            {
                case "test:all":
                    ExecuteAllTests(args);
                    break;
                    
                case "test:edit":
                    ExecuteEditModeTests(args);
                    break;
                    
                case "test:play":
                    ExecutePlayModeTests(args);
                    break;
                    
                case "test:export":
                    ExportTestResults(args);
                    break;
                    
                case "test:export:xml":
                    ExportTestResultsAsXML(args);
                    break;
                    
                case "test:analyze":
                    AnalyzeTestFailures(args);
                    break;
                    
                case "test:clean":
                    CleanTestReports(args);
                    break;
                    
                case "test:status":
                    ShowTestStatus(args);
                    break;
                    
                default:
                    UnityEngine.Debug.LogError($"[MCPTestCommands] 不明なコマンド: {command}");
                    ShowHelp();
                    break;
            }
        }
        
        /// <summary>
        /// すべてのテストを実行
        /// </summary>
        private static void ExecuteAllTests(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] すべてのテストを実行します");
            
            // オプション解析
            bool exportReport = GetBoolArg(args, "export", true);
            bool analyzeFails = GetBoolArg(args, "analyze", true);
            string outputPath = GetStringArg(args, "output", "Assets/BatchRenderingTool/Tests/Reports");
            
            // テスト実行
            UnityNaturalMCPTestRunner.RunAllTestsAndExportReport();
            
            // 失敗分析
            if (analyzeFails)
            {
                AnalyzeLatestTestFailures();
            }
        }
        
        /// <summary>
        /// Edit Modeテストのみ実行
        /// </summary>
        private static void ExecuteEditModeTests(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] Edit Modeテストを実行します");
            
            // フィルター設定
            string[] assemblies = GetArrayArg(args, "assemblies", new[] { "BatchRenderingTool.Editor.Tests" });
            string[] categories = GetArrayArg(args, "categories", null);
            
            // テスト実行
            UnityNaturalMCPTestRunner.RunEditModeTestsOnly();
        }
        
        /// <summary>
        /// Play Modeテストのみ実行
        /// </summary>
        private static void ExecutePlayModeTests(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] Play Modeテストを実行します");
            
            // フィルター設定
            string[] assemblies = GetArrayArg(args, "assemblies", new[] { "BatchRenderingTool.Runtime.Tests" });
            
            // テスト実行
            UnityNaturalMCPTestRunner.RunPlayModeTestsOnly();
        }
        
        /// <summary>
        /// テスト結果をエクスポート
        /// </summary>
        private static void ExportTestResults(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] テスト結果をエクスポートします");
            
            // フォーマット指定
            string format = GetStringArg(args, "format", "all");
            string outputPath = GetStringArg(args, "output", "Assets/BatchRenderingTool/Tests/Reports");
            
            // エクスポート実行
            UnityNaturalMCPTestRunner.ExportTestReportsOnly();
            
            // 結果をログ出力
            LogExportedFiles(outputPath);
        }
        
        /// <summary>
        /// テスト結果をNUnit XML形式でエクスポート（Unity Test Runner標準形式）
        /// </summary>
        private static void ExportTestResultsAsXML(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] テスト結果をNUnit XML形式でエクスポートします");
            
            // 出力パスの設定
            string outputPath = GetStringArg(args, "output", null);
            bool autoPath = GetBoolArg(args, "auto", true);
            bool useNativeExport = GetBoolArg(args, "native", true);
            
            if (autoPath || string.IsNullOrEmpty(outputPath))
            {
                // 自動パス生成
                var directory = "Assets/BatchRenderingTool/Tests/Reports";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                outputPath = Path.Combine(directory, $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            }
            
            // Unity Test Runner APIを使用してエクスポート
            try
            {
                // Unity Test RunnerのExport Resultボタンを自動化
                if (useNativeExport)
                {
                    UnityEngine.Debug.Log("[MCPTestCommands] Unity Test RunnerのExport Resultボタンを自動実行します");
                    
                    // 最新のテスト結果があるか確認
                    if (!HasTestResults())
                    {
                        UnityEngine.Debug.LogWarning("[MCPTestCommands] エクスポートするテスト結果がありません。");
                        
                        // テストを実行してからエクスポート
                        if (GetBoolArg(args, "runfirst", true))
                        {
                            UnityEngine.Debug.Log("[MCPTestCommands] テストを実行してからエクスポートします");
                            TestRunnerExportAutomation.RunTestsAndAutoExport();
                            return;
                        }
                    }
                    else
                    {
                        // 既存の結果をExport Resultボタン経由でエクスポート
                        TestRunnerExportAutomation.AutoExportTestResults();
                    }
                }
                else
                {
                    // 従来の方法でエクスポート（互換性のため残す）
                    UnityEngine.Debug.Log("[MCPTestCommands] カスタムXMLエクスポーターを使用します");
                    TestRunnerXMLExporter.ExportFromTestRunner(Path.GetDirectoryName(outputPath));
                }
                
                UnityEngine.Debug.Log($"[MCPTestCommands] XML形式でエクスポート処理を開始しました");
                
                // エクスポート結果の確認（少し待機）
                EditorApplication.delayCall += () =>
                {
                    // 統計情報を出力
                    var exportedFiles = FindRecentlyExportedXMLFiles();
                    if (exportedFiles.Any())
                    {
                        foreach (var file in exportedFiles)
                        {
                            LogXMLExportSummary(file);
                        }
                    }
                };
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MCPTestCommands] XMLエクスポートエラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// テスト失敗を分析
        /// </summary>
        private static void AnalyzeTestFailures(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] テスト失敗を分析します");
            
            // 最新のテスト結果を取得
            var results = GetLatestTestResults();
            if (results == null || results.Count == 0)
            {
                UnityEngine.Debug.LogWarning("分析するテスト結果がありません");
                return;
            }
            
            // 失敗テストを収集
            var allFailures = results.SelectMany(r => r.failedTests).ToList();
            if (allFailures.Count == 0)
            {
                UnityEngine.Debug.Log("失敗したテストはありません");
                return;
            }
            
            // エラー分析
            var analyses = TestErrorAnalyzer.AnalyzeFailures(allFailures);
            
            // 分析結果を出力
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("テスト失敗分析レポート");
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log(TestErrorAnalyzer.GenerateErrorSummary(analyses));
            
            // 修正提案
            UnityEngine.Debug.Log("\n推奨される修正:");
            var fixes = TestErrorAnalyzer.GetAllSuggestedFixes(analyses);
            foreach (var fix in fixes)
            {
                UnityEngine.Debug.Log($"  • {fix}");
            }
            
            // 統計情報
            var stats = TestErrorAnalyzer.GetErrorStatistics(results);
            UnityEngine.Debug.Log("\nエラータイプ別統計:");
            foreach (var stat in stats.OrderByDescending(s => s.Value))
            {
                UnityEngine.Debug.Log($"  • {stat.Key}: {stat.Value}件");
            }
        }
        
        /// <summary>
        /// 古いテストレポートをクリーンアップ
        /// </summary>
        private static void CleanTestReports(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] 古いテストレポートをクリーンアップします");
            
            int daysToKeep = GetIntArg(args, "days", 7);
            string reportPath = GetStringArg(args, "path", "Assets/BatchRenderingTool/Tests/Reports");
            
            if (!Directory.Exists(reportPath))
            {
                UnityEngine.Debug.LogWarning($"レポートディレクトリが存在しません: {reportPath}");
                return;
            }
            
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var files = Directory.GetFiles(reportPath, "TestReport_*.*");
            int deletedCount = 0;
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                        UnityEngine.Debug.Log($"削除: {Path.GetFileName(file)}");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"ファイル削除エラー: {file} - {e.Message}");
                    }
                }
            }
            
            UnityEngine.Debug.Log($"{deletedCount}個の古いレポートを削除しました");
        }
        
        /// <summary>
        /// テスト実行状態を表示
        /// </summary>
        private static void ShowTestStatus(Dictionary<string, string> args)
        {
            UnityEngine.Debug.Log("[MCPTestCommands] テスト実行状態を確認します");
            
            // アセンブリ情報
            UnityEngine.Debug.Log("\n登録されているテストアセンブリ:");
            UnityEngine.Debug.Log("  • BatchRenderingTool.Editor.Tests (Edit Mode)");
            UnityEngine.Debug.Log("  • BatchRenderingTool.Runtime.Tests (Play Mode)");
            
            // 最新の結果
            var latestResults = GetLatestTestResults();
            if (latestResults != null && latestResults.Count > 0)
            {
                UnityEngine.Debug.Log("\n最新のテスト結果:");
                foreach (var result in latestResults)
                {
                    var status = result.success ? "✅ 成功" : "❌ 失敗";
                    UnityEngine.Debug.Log($"  • {result.assemblyName}: {status} (Pass: {result.passCount}, Fail: {result.failCount})");
                }
            }
            
            // レポートファイル
            var reportPath = "Assets/BatchRenderingTool/Tests/Reports";
            if (Directory.Exists(reportPath))
            {
                var reports = Directory.GetFiles(reportPath, "TestReport_*.html")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Take(5)
                    .ToList();
                
                if (reports.Any())
                {
                    UnityEngine.Debug.Log($"\n最近のレポート ({reports.Count}件):");
                    foreach (var report in reports)
                    {
                        var info = new FileInfo(report);
                        UnityEngine.Debug.Log($"  • {Path.GetFileName(report)} ({info.CreationTime:yyyy-MM-dd HH:mm})");
                    }
                }
            }
        }
        
        /// <summary>
        /// ヘルプを表示
        /// </summary>
        private static void ShowHelp()
        {
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("Unity Natural MCP テストコマンド");
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("利用可能なコマンド:");
            UnityEngine.Debug.Log("  test:all         - すべてのテストを実行");
            UnityEngine.Debug.Log("  test:edit        - Edit Modeテストのみ実行");
            UnityEngine.Debug.Log("  test:play        - Play Modeテストのみ実行");
            UnityEngine.Debug.Log("  test:export      - テスト結果をエクスポート（全形式）");
            UnityEngine.Debug.Log("  test:export:xml  - NUnit XML形式でエクスポート（Test Runner標準）");
            UnityEngine.Debug.Log("  test:analyze     - テスト失敗を分析");
            UnityEngine.Debug.Log("  test:clean       - 古いレポートを削除");
            UnityEngine.Debug.Log("  test:status      - テスト状態を表示");
            UnityEngine.Debug.Log("\nオプション例:");
            UnityEngine.Debug.Log("  --export=true    レポートをエクスポート");
            UnityEngine.Debug.Log("  --format=html    出力フォーマット指定");
            UnityEngine.Debug.Log("  --output=path    出力パス指定");
            UnityEngine.Debug.Log("  --runfirst=true  テスト実行後にエクスポート");
            UnityEngine.Debug.Log("  --native=true    Unity Test RunnerのExport Resultボタンを自動化（デフォルト）");
            UnityEngine.Debug.Log("  --native=false   カスタムXMLエクスポーターを使用");
            UnityEngine.Debug.Log("  --days=7         保持日数指定");
            UnityEngine.Debug.Log("========================================");
        }
        
        // ヘルパーメソッド
        private static bool GetBoolArg(Dictionary<string, string> args, string key, bool defaultValue)
        {
            if (args.ContainsKey(key))
            {
                return bool.Parse(args[key]);
            }
            return defaultValue;
        }
        
        private static string GetStringArg(Dictionary<string, string> args, string key, string defaultValue)
        {
            return args.ContainsKey(key) ? args[key] : defaultValue;
        }
        
        private static int GetIntArg(Dictionary<string, string> args, string key, int defaultValue)
        {
            if (args.ContainsKey(key) && int.TryParse(args[key], out int value))
            {
                return value;
            }
            return defaultValue;
        }
        
        private static string[] GetArrayArg(Dictionary<string, string> args, string key, string[] defaultValue)
        {
            if (args.ContainsKey(key))
            {
                return args[key].Split(',');
            }
            return defaultValue;
        }
        
        private static void AnalyzeLatestTestFailures()
        {
            ExecuteCommand("test:analyze", new Dictionary<string, string>());
        }
        
        private static void LogExportedFiles(string outputPath)
        {
            if (!Directory.Exists(outputPath)) return;
            
            var files = Directory.GetFiles(outputPath, "TestReport_*.*")
                .Where(f => new FileInfo(f).CreationTime > DateTime.Now.AddMinutes(-1))
                .ToList();
            
            if (files.Any())
            {
                UnityEngine.Debug.Log($"\nエクスポートされたファイル:");
                foreach (var file in files)
                {
                    UnityEngine.Debug.Log($"  • {Path.GetFileName(file)}");
                }
            }
        }
        
        private static List<TestRunnerAutomation.TestResult> GetLatestTestResults()
        {
            // 実際のMCP実装では、保存された結果を読み込む
            // ここではダミーデータを返す
            return TestResultExporter.CreateTestResultsFromLastRun();
        }
        
        private static bool HasTestResults()
        {
            // テスト結果の存在を確認
            // 実際のMCP実装では、Unity Test Runner APIから確認
            return true; // 簡易実装
        }
        
        private static void LogXMLExportSummary(string xmlPath)
        {
            if (!File.Exists(xmlPath)) return;
            
            try
            {
                // XMLファイルから統計情報を読み取り
                var fileInfo = new FileInfo(xmlPath);
                UnityEngine.Debug.Log($"[MCPTestCommands] XMLエクスポート完了:");
                UnityEngine.Debug.Log($"  ファイル: {Path.GetFileName(xmlPath)}");
                UnityEngine.Debug.Log($"  サイズ: {fileInfo.Length / 1024.0:F2} KB");
                UnityEngine.Debug.Log($"  作成日時: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                
                // XML内容の簡易解析
                var xmlContent = File.ReadAllText(xmlPath);
                if (xmlContent.Contains("test-run"))
                {
                    // 簡易的な統計抽出
                    var passedMatch = System.Text.RegularExpressions.Regex.Match(xmlContent, @"passed=""(\d+)""");
                    var failedMatch = System.Text.RegularExpressions.Regex.Match(xmlContent, @"failed=""(\d+)""");
                    var totalMatch = System.Text.RegularExpressions.Regex.Match(xmlContent, @"total=""(\d+)""");
                    
                    if (passedMatch.Success && failedMatch.Success && totalMatch.Success)
                    {
                        UnityEngine.Debug.Log($"  テスト結果: Total={totalMatch.Groups[1].Value}, Passed={passedMatch.Groups[1].Value}, Failed={failedMatch.Groups[1].Value}");
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[MCPTestCommands] XML統計情報の読み取りエラー: {e.Message}");
            }
        }
        
        private static List<string> FindRecentlyExportedXMLFiles()
        {
            var xmlFiles = new List<string>();
            
            // 通常のエクスポート先を確認
            var directories = new[]
            {
                "Assets/BatchRenderingTool/Tests/Reports",
                "TestResults",
                Path.Combine(Application.dataPath, "..", "TestResults")
            };
            
            foreach (var dir in directories)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.xml")
                        .Where(f => new FileInfo(f).CreationTime > DateTime.Now.AddMinutes(-1))
                        .OrderByDescending(f => new FileInfo(f).CreationTime)
                        .ToList();
                    
                    xmlFiles.AddRange(files);
                }
            }
            
            return xmlFiles.Distinct().ToList();
        }
    }
}