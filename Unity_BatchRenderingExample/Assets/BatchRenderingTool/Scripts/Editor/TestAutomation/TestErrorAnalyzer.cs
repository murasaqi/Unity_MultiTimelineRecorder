using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// テストエラーを分析し、原因と解決策を提案
    /// </summary>
    public static class TestErrorAnalyzer
    {
        public class ErrorAnalysis
        {
            public string errorType;
            public string category;
            public string possibleCause;
            public List<string> suggestedFixes = new List<string>();
            public int severity; // 1-5 (5が最も深刻)
            public bool isKnownIssue;
        }
        
        /// <summary>
        /// 失敗したテストを分析
        /// </summary>
        public static List<ErrorAnalysis> AnalyzeFailures(List<TestRunnerAutomation.FailedTest> failedTests)
        {
            var analyses = new List<ErrorAnalysis>();
            
            foreach (var failed in failedTests)
            {
                var analysis = AnalyzeError(failed);
                if (analysis != null)
                {
                    analyses.Add(analysis);
                }
            }
            
            return analyses;
        }
        
        /// <summary>
        /// 個別のエラーを分析
        /// </summary>
        private static ErrorAnalysis AnalyzeError(TestRunnerAutomation.FailedTest failed)
        {
            var analysis = new ErrorAnalysis();
            
            // エラーメッセージからパターンを検出
            if (failed.message.Contains("NullReferenceException"))
            {
                analysis.errorType = "NullReferenceException";
                analysis.category = "実行時エラー";
                analysis.possibleCause = "オブジェクトが初期化されていない、またはnullになっている";
                analysis.suggestedFixes.Add("Setup()メソッドで必要なオブジェクトを初期化する");
                analysis.suggestedFixes.Add("nullチェックを追加する");
                analysis.suggestedFixes.Add("[SetUp]属性が正しく設定されているか確認");
                analysis.severity = 4;
            }
            else if (failed.message.Contains("Expected") && failed.message.Contains("But was"))
            {
                analysis.errorType = "アサーションエラー";
                analysis.category = "テストロジックエラー";
                analysis.possibleCause = "期待値と実際の値が一致しない";
                
                // 期待値と実際の値を抽出
                var expectedMatch = Regex.Match(failed.message, @"Expected:\s*(.+)");
                var actualMatch = Regex.Match(failed.message, @"But was:\s*(.+)");
                
                if (expectedMatch.Success && actualMatch.Success)
                {
                    var expected = expectedMatch.Groups[1].Value.Trim();
                    var actual = actualMatch.Groups[1].Value.Trim();
                    
                    if (expected == "True" && actual == "False")
                    {
                        analysis.suggestedFixes.Add("条件式を確認し、trueを返すように修正");
                        analysis.suggestedFixes.Add("テストの前提条件が正しいか確認");
                    }
                    else if (expected == "<empty>" && actual == "null")
                    {
                        analysis.suggestedFixes.Add("空文字列(\"\")とnullの違いを確認");
                        analysis.suggestedFixes.Add("String.Emptyまたは\"\"を使用");
                    }
                    else
                    {
                        analysis.suggestedFixes.Add($"値が{expected}になるようにロジックを修正");
                        analysis.suggestedFixes.Add("テストデータが正しいか確認");
                    }
                }
                
                analysis.severity = 3;
            }
            else if (failed.message.Contains("ArgumentException"))
            {
                analysis.errorType = "ArgumentException";
                analysis.category = "引数エラー";
                analysis.possibleCause = "メソッドに不正な引数が渡されている";
                analysis.suggestedFixes.Add("引数の値と型を確認");
                analysis.suggestedFixes.Add("nullや空文字列の処理を確認");
                analysis.suggestedFixes.Add("引数の検証ロジックを確認");
                analysis.severity = 3;
            }
            else if (failed.message.Contains("TimeoutException") || failed.message.Contains("timed out"))
            {
                analysis.errorType = "タイムアウト";
                analysis.category = "パフォーマンス問題";
                analysis.possibleCause = "処理が指定時間内に完了しない";
                analysis.suggestedFixes.Add("タイムアウト時間を延長");
                analysis.suggestedFixes.Add("処理を最適化");
                analysis.suggestedFixes.Add("非同期処理の待機条件を確認");
                analysis.severity = 2;
            }
            else if (failed.message.Contains("FileNotFoundException"))
            {
                analysis.errorType = "FileNotFoundException";
                analysis.category = "リソースエラー";
                analysis.possibleCause = "必要なファイルが見つからない";
                analysis.suggestedFixes.Add("ファイルパスが正しいか確認");
                analysis.suggestedFixes.Add("テスト環境にファイルが存在するか確認");
                analysis.suggestedFixes.Add("相対パスではなく絶対パスを使用");
                analysis.severity = 3;
            }
            else if (failed.message.Contains("compile"))
            {
                analysis.errorType = "コンパイルエラー";
                analysis.category = "ビルドエラー";
                analysis.possibleCause = "コードにシンタックスエラーがある";
                analysis.suggestedFixes.Add("コンパイルエラーを修正");
                analysis.suggestedFixes.Add("依存関係を確認");
                analysis.suggestedFixes.Add("アセンブリ定義を確認");
                analysis.severity = 5;
            }
            else
            {
                // デフォルトの分析
                analysis.errorType = "不明なエラー";
                analysis.category = "その他";
                analysis.possibleCause = "エラーの詳細を確認してください";
                analysis.suggestedFixes.Add("スタックトレースを確認");
                analysis.suggestedFixes.Add("ログを詳細に出力して調査");
                analysis.severity = 2;
            }
            
            // 既知の問題かチェック
            analysis.isKnownIssue = CheckKnownIssue(failed);
            
            return analysis;
        }
        
        /// <summary>
        /// 既知の問題かチェック
        /// </summary>
        private static bool CheckKnownIssue(TestRunnerAutomation.FailedTest failed)
        {
            // 既知の問題パターン
            var knownPatterns = new[]
            {
                "Unity Test Framework",
                "UnityEngine.TestRunner",
                "nunit.framework"
            };
            
            return knownPatterns.Any(pattern => 
                failed.message.Contains(pattern) || 
                (failed.stackTrace != null && failed.stackTrace.Contains(pattern)));
        }
        
        /// <summary>
        /// エラーの要約を生成
        /// </summary>
        public static string GenerateErrorSummary(List<ErrorAnalysis> analyses)
        {
            var summary = "エラー分析サマリー:\n\n";
            
            // エラータイプ別に集計
            var errorGroups = analyses.GroupBy(a => a.errorType);
            foreach (var group in errorGroups.OrderByDescending(g => g.Count()))
            {
                summary += $"- {group.Key}: {group.Count()}件\n";
            }
            
            summary += "\n最も深刻な問題:\n";
            
            // 深刻度の高い順に表示
            var severIssues = analyses.OrderByDescending(a => a.severity).Take(3);
            foreach (var issue in severIssues)
            {
                summary += $"- [{issue.severity}/5] {issue.errorType}: {issue.possibleCause}\n";
            }
            
            return summary;
        }
        
        /// <summary>
        /// 修正提案をまとめて取得
        /// </summary>
        public static List<string> GetAllSuggestedFixes(List<ErrorAnalysis> analyses)
        {
            var allFixes = new HashSet<string>();
            
            foreach (var analysis in analyses)
            {
                foreach (var fix in analysis.suggestedFixes)
                {
                    allFixes.Add(fix);
                }
            }
            
            return allFixes.OrderBy(f => f).ToList();
        }
        
        /// <summary>
        /// エラーパターンの統計を取得
        /// </summary>
        public static Dictionary<string, int> GetErrorStatistics(List<TestRunnerAutomation.TestResult> results)
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var result in results)
            {
                foreach (var failed in result.failedTests)
                {
                    var analysis = AnalyzeError(failed);
                    if (analysis != null)
                    {
                        if (stats.ContainsKey(analysis.errorType))
                        {
                            stats[analysis.errorType]++;
                        }
                        else
                        {
                            stats[analysis.errorType] = 1;
                        }
                    }
                }
            }
            
            return stats;
        }
    }
}