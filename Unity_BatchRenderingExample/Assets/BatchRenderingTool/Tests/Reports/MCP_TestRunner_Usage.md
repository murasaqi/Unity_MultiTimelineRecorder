# Unity Natural MCP Test Runner - 使用方法

## 概要
Unity Test Runner標準のExport Result機能のみを使用するシンプルなMCPインターフェースです。

## 実装ファイル

### SimpleMCPTestRunner.cs
Unity Natural MCP用のシンプルなテストランナー。Test Runner標準機能のみを使用。

### SimpleMCPCommands.cs
MCPコマンドインターフェース。以下のコマンドをサポート：
- `test:export` - Unity Test RunnerのExport Resultを実行
- `test:run-and-export` - テスト実行後にExport Resultを実行
- `test:help` - ヘルプを表示

### TestRunnerExportAutomation.cs
Unity Test RunnerのExport Resultボタンの動作を自動化する内部実装。

## 使用方法

### Unityエディタのメニューから
1. **Tools > MCP Test Runner > Export Test Results**
   - 最新のテスト結果をExport Result標準機能でエクスポート
   
2. **Tools > MCP Test Runner > Run Tests and Export Results**
   - テストを実行してから自動的にExport Resultを実行

### MCPコマンドから
```bash
# テスト結果をエクスポート
test:export

# テストを実行してからエクスポート
test:run-and-export

# ヘルプを表示
test:help
```

## 出力形式
Unity Test Runner標準のNUnit XML形式で出力されます。

## 注意事項
- Export時に保存ダイアログが表示される場合があります
- Test Runnerウィンドウが自動的に開く場合があります
- Unity Test Runner標準機能のみを使用しているため、カスタムレポートは生成されません

## テスト実行結果（最新）

### 実行日時: 2025-01-06

### テスト統計
- **総テスト数**: 55
- **成功**: 51 (92.7%)
- **失敗**: 4 (7.3%)
  - Edit Mode: 3件失敗
  - Play Mode: 1件失敗

### 失敗テスト
1. AOVRecorderSettingsConfig関連 (2件) - HDRP依存
2. FBXRecorderSettings - フレームレート設定エラー
3. Timeline_TimeProgresses - Play Mode時間進行エラー

### エクスポート結果
Unity Test Runner標準のExport Result機能により、NUnit XML形式でテスト結果が出力されます。