# Unity Multi Timeline Recorder - 外部制御対応リファクタリングプラン

## 📋 概要

このドキュメントは、Unity Multi Timeline RecorderをUI以外から制御可能にするためのリファクタリングプランです。将来的なMCP（Model Context Protocol）統合を視野に入れつつ、段階的に実装可能な設計を提案します。

### 目的
- **主目的**: EditorウィンドウUI以外からレンダリングを制御可能にする
- **副次目的**: 
  - コードの保守性向上
  - 自動化ワークフローへの対応
  - 将来的なMCP統合への準備

### 前提条件
- UnityEditorは常時起動していることを前提
- 完全なヘッドレスサーバーではなく、Editor内で動作
- 既存のUI機能は維持しながら段階的に移行

## 🔍 現状分析

### 現在のアーキテクチャの問題点

1. **高結合度**
   - `MultiTimelineRecorder.cs`（1000行以上）にUI描画とビジネスロジックが混在
   - 新機能追加時の影響範囲が大きい

2. **外部制御の困難さ**
   - すべての操作がGUIイベント駆動
   - プログラマティックな操作が不可能

3. **拡張性の制限**
   - 新しいレコーダータイプ追加時に複数箇所の変更が必要
   - enumベースの型管理による硬直性

### 現在のデータ構造
```csharp
// 設定はSerializableクラスで管理
[Serializable]
public class RecorderConfig
{
    public bool enabled = true;
    public string configName = "New Recorder";
    public RecorderSettingsType recorderType;
    // ... 多数のプロパティ
}
```

## 🏗️ 設計方針

### 基本方針
1. **段階的移行**: 既存機能を維持しながら新機能を追加
2. **関心の分離**: UI、ビジネスロジック、データ層を明確に分離
3. **インターフェース駆動**: 依存性注入を活用した疎結合設計
4. **非同期優先**: 長時間実行される処理は非同期化

### アーキテクチャパターン
```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Presentation   │     │   Application   │     │      Core       │
│   Layer         │────▶│     Layer       │────▶│     Layer       │
│ (UI/API/CLI)    │     │  (Controllers)  │     │   (Services)    │
└─────────────────┘     └─────────────────┘     └─────────────────┘
         │                       │                        │
         └───────────────────────┴────────────────────────┘
                                 │
                          ┌──────▼──────┐
                          │    Data     │
                          │   Layer     │
                          └─────────────┘
```

## 📐 詳細設計

### 1. コアインターフェース定義

```csharp
// Core/Interfaces/ITimelineRenderingEngine.cs
namespace Unity.MultiTimelineRecorder.Core
{
    public interface ITimelineRenderingEngine
    {
        // イベント
        event Action<RenderingProgress> ProgressChanged;
        event Action<RenderingResult> RenderingCompleted;
        event Action<string> ErrorOccurred;
        
        // メソッド
        Task<string> StartRenderingAsync(RenderingRequest request);
        void StopRendering(string taskId);
        RenderingStatus GetStatus(string taskId);
        List<RenderingTask> GetActiveTasks();
    }
    
    // データモデル
    public class RenderingRequest
    {
        public List<string> TimelineAssetPaths { get; set; }
        public List<RecorderConfig> RecorderConfigs { get; set; }
        public GlobalSettings GlobalSettings { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
    
    public class RenderingProgress
    {
        public string TaskId { get; set; }
        public float Percentage { get; set; }
        public string CurrentTimeline { get; set; }
        public int CurrentFrame { get; set; }
        public int TotalFrames { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }
    
    public class RenderingResult
    {
        public string TaskId { get; set; }
        public bool Success { get; set; }
        public List<string> OutputFiles { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Statistics { get; set; }
    }
}
```

### 2. 外部制御インターフェース

#### 2.1 ファイル監視システム
```csharp
// ExternalControl/FileWatcherController.cs
[InitializeOnLoad]
public static class FileWatcherController
{
    private static FileSystemWatcher watcher;
    private static string watchFolder;
    
    // コマンドファイル形式
    public class RenderCommand
    {
        public string commandId;
        public string action; // "render", "cancel", "status"
        public RenderingRequest request;
        public CommandOptions options;
    }
    
    // ファイル配置でレンダリング開始
    // MTR_Commands/
    //   ├── command_001.json     (コマンドファイル)
    //   ├── command_001.status   (ステータスファイル: PROCESSING/COMPLETED/ERROR)
    //   ├── command_001.result   (結果ファイル)
    //   └── command_001.log      (ログファイル)
}
```

#### 2.2 ローカルHTTPサーバー
```csharp
// ExternalControl/LocalHttpServer.cs
public class LocalHttpServer
{
    // エンドポイント設計
    // POST   /api/render      - レンダリング開始
    // GET    /api/status      - サーバーステータス
    // GET    /api/task/{id}   - タスク状態取得
    // DELETE /api/task/{id}   - タスクキャンセル
    // GET    /api/tasks       - アクティブタスク一覧
    
    private static readonly int port = 7890;
    private HttpListener listener;
}
```

### 3. レンダリングエンジン実装

```csharp
// Core/Services/TimelineRenderingEngine.cs
public class TimelineRenderingEngine : ITimelineRenderingEngine
{
    private readonly ConcurrentDictionary<string, RenderingTask> activeTasks;
    private readonly IRecorderFactory recorderFactory;
    private readonly ITimelineService timelineService;
    
    public async Task<string> StartRenderingAsync(RenderingRequest request)
    {
        // 1. リクエスト検証
        ValidateRequest(request);
        
        // 2. タスクID生成
        var taskId = Guid.NewGuid().ToString();
        
        // 3. タスク作成
        var task = new RenderingTask
        {
            Id = taskId,
            Request = request,
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.Now
        };
        
        activeTasks[taskId] = task;
        
        // 4. 非同期でレンダリング実行
        _ = Task.Run(() => ExecuteRenderingAsync(task));
        
        return taskId;
    }
    
    private async Task ExecuteRenderingAsync(RenderingTask task)
    {
        try
        {
            task.Status = TaskStatus.Running;
            
            // Timelineアセットの読み込み
            var timelines = await LoadTimelinesAsync(task.Request.TimelineAssetPaths);
            
            // レコーダーの準備
            var recorders = PrepareRecorders(task.Request.RecorderConfigs);
            
            // PlayModeで実行
            await ExecuteInPlayMode(task, timelines, recorders);
            
            task.Status = TaskStatus.Completed;
        }
        catch (Exception ex)
        {
            task.Status = TaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            ErrorOccurred?.Invoke($"Task {task.Id} failed: {ex.Message}");
        }
    }
}
```

## 📅 実装計画

### Phase 1: 基盤整備（1-2週間）

#### タスク
1. **プロジェクト構造の整理**
   ```
   Editor/
   ├── Core/
   │   ├── Interfaces/
   │   ├── Services/
   │   └── Models/
   ├── ExternalControl/
   │   ├── FileWatcher/
   │   ├── HttpServer/
   │   └── Common/
   ├── UI/
   │   ├── Windows/
   │   └── Components/
   └── Legacy/  (既存コードを一時的に配置)
   ```

2. **コアインターフェースの定義**
   - `ITimelineRenderingEngine`
   - `IRecorderService`
   - `ITimelineService`

3. **データモデルの整理**
   - JSON互換性を考慮した設計
   - Unity特有の型（GameObject等）の抽象化

### Phase 2: レンダリングエンジンの実装（2週間）

1. **TimelineRenderingEngineの実装**
   - 非同期処理の実装
   - 進捗追跡機能
   - エラーハンドリング

2. **既存ロジックの移植**
   - `MultiTimelineRecorder.cs`からビジネスロジックを抽出
   - `PlayModeTimelineRenderer`との統合

3. **テストの作成**
   - ユニットテスト
   - 統合テスト

### Phase 3: 外部制御機能の実装（1-2週間）

1. **ファイル監視システム**
   - FileSystemWatcherの実装
   - コマンドファイル処理
   - ステータス管理

2. **HTTPサーバー**
   - RESTful APIの実装
   - 認証機能（オプション）
   - CORS対応

3. **ステータスウィンドウ**
   - リアルタイム進捗表示
   - タスク管理UI

### Phase 4: UI層のリファクタリング（1週間）

1. **MVVMパターンの適用**
   - ViewModelの作成
   - データバインディング

2. **既存UIとの統合**
   - レガシーコードとの共存
   - 段階的な機能移行

## 🔧 実装例

### Python クライアント例

```python
# mtr_client.py
import requests
import json
import time
from pathlib import Path

class MultiTimelineRecorderClient:
    def __init__(self, mode="http", base_url="http://localhost:7890"):
        self.mode = mode
        self.base_url = base_url
        self.command_dir = Path("../MTR_Commands")
        
    def render(self, config):
        """設定に基づいてレンダリングを開始"""
        if self.mode == "http":
            return self._render_via_http(config)
        else:
            return self._render_via_file(config)
    
    def _render_via_http(self, config):
        """HTTP API経由でレンダリング"""
        response = requests.post(
            f"{self.base_url}/api/render",
            json=config,
            headers={"Content-Type": "application/json"}
        )
        return response.json()
    
    def _render_via_file(self, config):
        """ファイル監視経由でレンダリング"""
        command_id = f"cmd_{int(time.time() * 1000)}"
        
        # コマンドファイルを作成
        command = {
            "commandId": command_id,
            "action": "render",
            "request": config
        }
        
        command_path = self.command_dir / f"{command_id}.json"
        command_path.write_text(json.dumps(command, indent=2))
        
        # 完了を待つ
        return self._wait_for_completion(command_id)
    
    def _wait_for_completion(self, command_id):
        """レンダリング完了を待機"""
        status_path = self.command_dir / f"{command_id}.status"
        result_path = self.command_dir / f"{command_id}.result"
        
        while True:
            if status_path.exists():
                status = status_path.read_text().strip()
                
                if status == "COMPLETED" and result_path.exists():
                    return json.loads(result_path.read_text())
                elif status == "ERROR":
                    error_path = self.command_dir / f"{command_id}.error"
                    if error_path.exists():
                        raise Exception(error_path.read_text())
                    
            time.sleep(0.5)

# 使用例
def main():
    client = MultiTimelineRecorderClient(mode="file")
    
    # レンダリング設定
    config = {
        "timelineAssetPaths": [
            "Assets/Timelines/Scene1_Camera.playable",
            "Assets/Timelines/Scene1_Lights.playable"
        ],
        "recorderConfigs": [{
            "enabled": True,
            "configName": "Main Camera Recording",
            "recorderType": "Movie",
            "movieOutputFormat": "MP4",
            "width": 1920,
            "height": 1080,
            "frameRate": 30,
            "fileName": "Scene1_<Date>_<Time>",
            "filePath": "Recordings/AutoRender"
        }],
        "globalSettings": {
            "overwriteExisting": True,
            "createOutputDirectory": True
        }
    }
    
    # レンダリング実行
    try:
        result = client.render(config)
        print(f"Rendering completed successfully!")
        print(f"Output files: {result['outputFiles']}")
    except Exception as e:
        print(f"Rendering failed: {e}")

if __name__ == "__main__":
    main()
```

### C# スクリプト例

```csharp
// 他のUnityプロジェクトから制御する例
using System.Net.Http;
using System.Text;
using UnityEngine;

public class MultiTimelineRecorderRemoteControl : MonoBehaviour
{
    private readonly HttpClient httpClient = new HttpClient();
    private const string MTR_URL = "http://localhost:7890";
    
    [ContextMenu("Trigger Remote Rendering")]
    public async void TriggerRemoteRendering()
    {
        var config = new
        {
            timelineAssetPaths = new[] {
                "Assets/Timelines/TestScene.playable"
            },
            recorderConfigs = new[] {
                new {
                    enabled = true,
                    recorderType = "Movie",
                    movieOutputFormat = "MP4"
                }
            }
        };
        
        var json = JsonUtility.ToJson(config);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            var response = await httpClient.PostAsync($"{MTR_URL}/api/render", content);
            var result = await response.Content.ReadAsStringAsync();
            Debug.Log($"Rendering started: {result}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to trigger rendering: {e}");
        }
    }
}
```

## 🔄 移行戦略

### 既存プロジェクトへの影響を最小化

1. **後方互換性の維持**
   - 既存のScriptableObjectベースの設定を維持
   - 新旧両方のAPIを一定期間サポート

2. **段階的な機能追加**
   - Phase 1-2: 内部リファクタリング（ユーザーには透明）
   - Phase 3: 外部制御機能の追加（オプトイン）
   - Phase 4: UI更新（既存UIと共存）

3. **設定の移行パス**
   ```csharp
   // 既存設定から新形式への変換
   public static RenderingRequest ConvertLegacyConfig(MultiRecorderConfig legacy)
   {
       return new RenderingRequest
       {
           TimelineAssetPaths = legacy.timelines.Select(AssetDatabase.GetAssetPath).ToList(),
           RecorderConfigs = legacy.recorderConfigs,
           // ... その他の変換
       };
   }
   ```

## 📊 成功指標

### 技術的指標
- **コードメトリクス**
  - 単一クラスの行数: 500行以下
  - メソッドの循環的複雑度: 10以下
  - テストカバレッジ: 70%以上

- **パフォーマンス**
  - API応答時間: 100ms以内
  - メモリ使用量: 現状比±10%以内
  - レンダリング速度: 現状と同等

### 機能的指標
- **外部制御**
  - コマンドライン実行: 成功率95%以上
  - API経由の制御: 成功率99%以上
  - エラーハンドリング: すべてのエラーが適切に報告される

- **互換性**
  - 既存プロジェクト: 変更なしで動作
  - 新機能: ドキュメント化された手順で有効化可能

## 🚀 将来の拡張性

### MCP統合への準備
```csharp
// 将来的なMCPツール定義
[MCPTool("unity_timeline_render")]
public class UnityTimelineRenderTool : IMCPTool
{
    private readonly ITimelineRenderingEngine engine;
    
    public async Task<MCPResponse> Execute(MCPRequest request)
    {
        // 既存のRenderingRequestに変換
        var renderRequest = ConvertFromMCP(request);
        var taskId = await engine.StartRenderingAsync(renderRequest);
        
        return new MCPResponse
        {
            Success = true,
            Data = new { taskId }
        };
    }
}
```

### クラウドレンダリング対応
- レンダリングエンジンのインターフェースは変更不要
- 実装クラスの差し替えで対応可能

### プラグインシステム
- カスタムレコーダーの動的読み込み
- サードパーティ拡張のサポート

## 📝 まとめ

このリファクタリングプランにより、Unity Multi Timeline Recorderは以下の特徴を持つツールに進化します：

1. **柔軟な制御方法**: UI、API、ファイル監視の3つの制御方法
2. **高い保守性**: 明確に分離されたアーキテクチャ
3. **将来性**: MCP統合やクラウド対応への道筋
4. **後方互換性**: 既存ユーザーへの影響を最小化

段階的な実装により、リスクを抑えながら着実に目標を達成できます。