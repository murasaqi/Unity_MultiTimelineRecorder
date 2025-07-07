# Unity Test Runner MCP Integration

Unity Test Runner標準のExport Result機能をUnity Natural MCP経由で使用するためのシンプルな実装です。

## 使い方

### メニューから実行
- **Tools > MCP Test Runner > Export Test Results**
  - Test Runner標準のExport Resultを実行

- **Tools > MCP Test Runner > Run Tests and Export Results**
  - テスト実行後に自動でExport

### MCPコマンド
```
test:export         # Export Resultを実行
test:run-and-export # テスト実行後にExport
```

## 出力
Unity Test Runner標準のNUnit XML形式

## ファイル構成
- SimpleMCPTestRunner.cs - メニューインターフェース
- SimpleMCPCommands.cs - コマンドインターフェース
- TestRunnerExportAutomation.cs - Export Result自動化実装