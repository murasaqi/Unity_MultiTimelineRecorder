using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// テストレポート生成クラス
    /// </summary>
    public static class TestReportGenerator
    {
        public enum ReportFormat
        {
            Json,
            Html,
            Markdown,
            Csv
        }
        
        /// <summary>
        /// テストレポートを生成
        /// </summary>
        public static void GenerateReport(
            List<TestRunnerAutomation.TestResult> results,
            string outputPath,
            ReportFormat format = ReportFormat.Html)
        {
            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                switch (format)
                {
                    case ReportFormat.Json:
                        GenerateJsonReport(results, outputPath);
                        break;
                    case ReportFormat.Html:
                        GenerateHtmlReport(results, outputPath);
                        break;
                    case ReportFormat.Markdown:
                        GenerateMarkdownReport(results, outputPath);
                        break;
                    case ReportFormat.Csv:
                        GenerateCsvReport(results, outputPath);
                        break;
                }
                
                Debug.Log($"[TestReportGenerator] レポートを生成しました: {outputPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestReportGenerator] レポート生成エラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// JSONレポートを生成
        /// </summary>
        private static void GenerateJsonReport(List<TestRunnerAutomation.TestResult> results, string outputPath)
        {
            var report = new
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                environment = new
                {
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    systemInfo = SystemInfo.operatingSystem
                },
                summary = new
                {
                    totalAssemblies = results.Count,
                    totalTests = results.Sum(r => r.passCount + r.failCount + r.skipCount),
                    totalPass = results.Sum(r => r.passCount),
                    totalFail = results.Sum(r => r.failCount),
                    totalSkip = results.Sum(r => r.skipCount),
                    successRate = CalculateSuccessRate(results),
                    totalDuration = results.Sum(r => r.duration)
                },
                results = results.Select(r => new
                {
                    assembly = r.assemblyName,
                    mode = r.testMode.ToString(),
                    executionTime = r.executionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    duration = r.duration,
                    pass = r.passCount,
                    fail = r.failCount,
                    skip = r.skipCount,
                    success = r.success,
                    failures = r.failedTests.Select(f => new
                    {
                        name = f.name,
                        fullName = f.fullName,
                        message = f.message,
                        stackTrace = f.stackTrace,
                        duration = f.duration
                    })
                })
            };
            
            var json = JsonUtility.ToJson(report, true);
            File.WriteAllText(outputPath, json);
        }
        
        /// <summary>
        /// HTMLレポートを生成
        /// </summary>
        private static void GenerateHtmlReport(List<TestRunnerAutomation.TestResult> results, string outputPath)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Unity Test Report</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
                .container { max-width: 1200px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
                h1, h2, h3 { color: #333; }
                .summary { background-color: #f0f0f0; padding: 15px; border-radius: 5px; margin-bottom: 20px; }
                .summary-stats { display: flex; justify-content: space-around; margin-top: 10px; }
                .stat { text-align: center; }
                .stat-value { font-size: 24px; font-weight: bold; }
                .stat-label { color: #666; font-size: 14px; }
                .pass { color: #4CAF50; }
                .fail { color: #F44336; }
                .skip { color: #FF9800; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
                th { background-color: #f2f2f2; font-weight: bold; }
                tr:hover { background-color: #f5f5f5; }
                .test-result { margin-bottom: 30px; }
                .failed-test { background-color: #ffebee; padding: 10px; margin: 10px 0; border-radius: 4px; border-left: 4px solid #F44336; }
                .failed-test-name { font-weight: bold; color: #c62828; }
                .failed-test-message { margin: 5px 0; color: #666; }
                .stack-trace { font-family: monospace; font-size: 12px; background-color: #f5f5f5; padding: 10px; margin-top: 5px; border-radius: 4px; overflow-x: auto; }
                .chart { margin: 20px 0; }
                .progress-bar { width: 100%; height: 30px; background-color: #e0e0e0; border-radius: 15px; overflow: hidden; }
                .progress-fill { height: 100%; display: flex; }
                .progress-pass { background-color: #4CAF50; }
                .progress-fail { background-color: #F44336; }
                .progress-skip { background-color: #FF9800; }
            </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='container'>");
            
            // ヘッダー
            html.AppendLine("<h1>Unity Test Report</h1>");
            html.AppendLine($"<p>生成日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"<p>Unity Version: {Application.unityVersion}</p>");
            
            // サマリー
            var totalTests = results.Sum(r => r.passCount + r.failCount + r.skipCount);
            var totalPass = results.Sum(r => r.passCount);
            var totalFail = results.Sum(r => r.failCount);
            var totalSkip = results.Sum(r => r.skipCount);
            var successRate = CalculateSuccessRate(results);
            
            html.AppendLine("<div class='summary'>");
            html.AppendLine("<h2>テスト結果サマリー</h2>");
            html.AppendLine("<div class='summary-stats'>");
            html.AppendLine($"<div class='stat'><div class='stat-value'>{totalTests}</div><div class='stat-label'>総テスト数</div></div>");
            html.AppendLine($"<div class='stat'><div class='stat-value pass'>{totalPass}</div><div class='stat-label'>成功</div></div>");
            html.AppendLine($"<div class='stat'><div class='stat-value fail'>{totalFail}</div><div class='stat-label'>失敗</div></div>");
            html.AppendLine($"<div class='stat'><div class='stat-value skip'>{totalSkip}</div><div class='stat-label'>スキップ</div></div>");
            html.AppendLine($"<div class='stat'><div class='stat-value'>{successRate:F1}%</div><div class='stat-label'>成功率</div></div>");
            html.AppendLine("</div>");
            
            // プログレスバー
            if (totalTests > 0)
            {
                var passPercent = (totalPass * 100.0 / totalTests);
                var failPercent = (totalFail * 100.0 / totalTests);
                var skipPercent = (totalSkip * 100.0 / totalTests);
                
                html.AppendLine("<div class='chart'>");
                html.AppendLine("<div class='progress-bar'>");
                html.AppendLine("<div class='progress-fill'>");
                if (passPercent > 0) html.AppendLine($"<div class='progress-pass' style='width: {passPercent}%'></div>");
                if (failPercent > 0) html.AppendLine($"<div class='progress-fail' style='width: {failPercent}%'></div>");
                if (skipPercent > 0) html.AppendLine($"<div class='progress-skip' style='width: {skipPercent}%'></div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
            }
            
            html.AppendLine("</div>");
            
            // テスト結果テーブル
            html.AppendLine("<h2>アセンブリ別結果</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>アセンブリ</th><th>モード</th><th>成功</th><th>失敗</th><th>スキップ</th><th>実行時間</th><th>ステータス</th></tr>");
            
            foreach (var result in results)
            {
                var status = result.success ? "<span class='pass'>✓ 成功</span>" : "<span class='fail'>✗ 失敗</span>";
                html.AppendLine($"<tr>");
                html.AppendLine($"<td>{result.assemblyName}</td>");
                html.AppendLine($"<td>{result.testMode}</td>");
                html.AppendLine($"<td class='pass'>{result.passCount}</td>");
                html.AppendLine($"<td class='fail'>{result.failCount}</td>");
                html.AppendLine($"<td class='skip'>{result.skipCount}</td>");
                html.AppendLine($"<td>{result.duration:F2}s</td>");
                html.AppendLine($"<td>{status}</td>");
                html.AppendLine($"</tr>");
            }
            
            html.AppendLine("</table>");
            
            // 失敗テストの詳細
            var failedResults = results.Where(r => r.failedTests.Count > 0).ToList();
            if (failedResults.Any())
            {
                html.AppendLine("<h2>失敗テストの詳細</h2>");
                
                foreach (var result in failedResults)
                {
                    html.AppendLine($"<div class='test-result'>");
                    html.AppendLine($"<h3>{result.assemblyName} ({result.testMode})</h3>");
                    
                    foreach (var failed in result.failedTests)
                    {
                        html.AppendLine("<div class='failed-test'>");
                        html.AppendLine($"<div class='failed-test-name'>{failed.name}</div>");
                        html.AppendLine($"<div class='failed-test-message'>{EscapeHtml(failed.message)}</div>");
                        
                        if (!string.IsNullOrEmpty(failed.stackTrace))
                        {
                            html.AppendLine($"<div class='stack-trace'>{EscapeHtml(failed.stackTrace)}</div>");
                        }
                        
                        html.AppendLine("</div>");
                    }
                    
                    html.AppendLine("</div>");
                }
            }
            
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            File.WriteAllText(outputPath, html.ToString());
        }
        
        /// <summary>
        /// Markdownレポートを生成
        /// </summary>
        private static void GenerateMarkdownReport(List<TestRunnerAutomation.TestResult> results, string outputPath)
        {
            var md = new StringBuilder();
            
            md.AppendLine("# Unity Test Report");
            md.AppendLine();
            md.AppendLine($"生成日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine($"Unity Version: {Application.unityVersion}");
            md.AppendLine();
            
            // サマリー
            var totalTests = results.Sum(r => r.passCount + r.failCount + r.skipCount);
            var totalPass = results.Sum(r => r.passCount);
            var totalFail = results.Sum(r => r.failCount);
            var totalSkip = results.Sum(r => r.skipCount);
            var successRate = CalculateSuccessRate(results);
            
            md.AppendLine("## テスト結果サマリー");
            md.AppendLine();
            md.AppendLine($"- **総テスト数**: {totalTests}");
            md.AppendLine($"- **成功**: {totalPass} ✅");
            md.AppendLine($"- **失敗**: {totalFail} ❌");
            md.AppendLine($"- **スキップ**: {totalSkip} ⏭️");
            md.AppendLine($"- **成功率**: {successRate:F1}%");
            md.AppendLine();
            
            // アセンブリ別結果
            md.AppendLine("## アセンブリ別結果");
            md.AppendLine();
            md.AppendLine("| アセンブリ | モード | 成功 | 失敗 | スキップ | 実行時間 | ステータス |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            
            foreach (var result in results)
            {
                var status = result.success ? "✅ 成功" : "❌ 失敗";
                md.AppendLine($"| {result.assemblyName} | {result.testMode} | {result.passCount} | {result.failCount} | {result.skipCount} | {result.duration:F2}s | {status} |");
            }
            
            md.AppendLine();
            
            // 失敗テストの詳細
            var failedResults = results.Where(r => r.failedTests.Count > 0).ToList();
            if (failedResults.Any())
            {
                md.AppendLine("## 失敗テストの詳細");
                md.AppendLine();
                
                foreach (var result in failedResults)
                {
                    md.AppendLine($"### {result.assemblyName} ({result.testMode})");
                    md.AppendLine();
                    
                    foreach (var failed in result.failedTests)
                    {
                        md.AppendLine($"#### ❌ {failed.name}");
                        md.AppendLine();
                        md.AppendLine("**エラーメッセージ:**");
                        md.AppendLine("```");
                        md.AppendLine(failed.message);
                        md.AppendLine("```");
                        
                        if (!string.IsNullOrEmpty(failed.stackTrace))
                        {
                            md.AppendLine();
                            md.AppendLine("**スタックトレース:**");
                            md.AppendLine("```");
                            md.AppendLine(failed.stackTrace);
                            md.AppendLine("```");
                        }
                        
                        md.AppendLine();
                    }
                }
            }
            
            File.WriteAllText(outputPath, md.ToString());
        }
        
        /// <summary>
        /// CSVレポートを生成
        /// </summary>
        private static void GenerateCsvReport(List<TestRunnerAutomation.TestResult> results, string outputPath)
        {
            var csv = new StringBuilder();
            
            // ヘッダー
            csv.AppendLine("Assembly,Mode,Pass,Fail,Skip,Duration,Status,ExecutionTime");
            
            // データ
            foreach (var result in results)
            {
                csv.AppendLine($"{result.assemblyName},{result.testMode},{result.passCount},{result.failCount},{result.skipCount},{result.duration:F2},{(result.success ? "Success" : "Failed")},{result.executionTime:yyyy-MM-dd HH:mm:ss}");
            }
            
            File.WriteAllText(outputPath, csv.ToString());
            
            // 失敗テストの詳細を別ファイルに出力
            var failedTests = results.SelectMany(r => r.failedTests.Select(f => new { Assembly = r.assemblyName, Mode = r.testMode, Failed = f })).ToList();
            if (failedTests.Any())
            {
                var failedCsv = new StringBuilder();
                failedCsv.AppendLine("Assembly,Mode,TestName,Message,Duration");
                
                foreach (var test in failedTests)
                {
                    var message = test.Failed.message.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
                    failedCsv.AppendLine($"{test.Assembly},{test.Mode},\"{test.Failed.name}\",\"{message}\",{test.Failed.duration:F2}");
                }
                
                var failedPath = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "_failures.csv");
                File.WriteAllText(failedPath, failedCsv.ToString());
            }
        }
        
        /// <summary>
        /// 成功率を計算
        /// </summary>
        private static double CalculateSuccessRate(List<TestRunnerAutomation.TestResult> results)
        {
            var totalTests = results.Sum(r => r.passCount + r.failCount);
            if (totalTests == 0) return 100.0;
            
            var totalPass = results.Sum(r => r.passCount);
            return (totalPass * 100.0) / totalTests;
        }
        
        /// <summary>
        /// HTMLエスケープ
        /// </summary>
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}