# Unity Multi Timeline Recorder スケーラビリティ向上リファクタリングプラン

## 📋 現在のアーキテクチャの問題点

### 1. **モノリシックな設計**
- `MultiTimelineRecorder.cs`が単一ファイルで多くの責任を持っている
- UIロジック、ビジネスロジック、設定管理が混在

### 2. **重複コードとパターンの反復**
- 各レコーダータイプ（Image、Movie、FBX等）で類似の実装が重複
- 新しいレコーダータイプの追加時にボイラープレートコードが必要

### 3. **拡張性の制限**
- `RecorderSettingsType`がハードコードされたenum
- 新機能追加時にコア部分の変更が必要

### 4. **密結合**
- コンフィグレーション、エディタ、設定が密結合
- 単体テストが困難

## 🔧 提案するリファクタリング戦略

### **フェーズ1: アーキテクチャの分離**

#### 1.1 **責任の分離**
```
Editor/
├── Core/                           # コアロジック
│   ├── IRecorderPlugin.cs          # プラグインインターフェース
│   ├── RecorderPluginManager.cs    # プラグイン管理
│   ├── RecordingOrchestrator.cs    # 録画制御
│   └── ValidationEngine.cs         # 検証エンジン
├── UI/                            # UI専用
│   ├── Windows/
│   │   └── MultiTimelineRecorderWindow.cs
│   ├── Panels/
│   │   ├── TimelineListPanel.cs
│   │   ├── RecorderConfigPanel.cs
│   │   └── ProgressPanel.cs
│   └── Controls/
│       └── RecorderSettingsControl.cs
├── Plugins/                       # レコーダープラグイン
│   ├── Base/
│   │   ├── BaseRecorderPlugin.cs
│   │   └── BaseRecorderEditor.cs
│   ├── ImageRecorderPlugin/
│   ├── MovieRecorderPlugin/
│   └── FBXRecorderPlugin/
└── Services/                      # ビジネスサービス
    ├── SettingsService.cs
    ├── PathService.cs
    └── ValidationService.cs
```

#### 1.2 **プラグインアーキテクチャの導入**
```csharp
public interface IRecorderPlugin
{
    string Name { get; }
    string DisplayName { get; }
    Type ConfigType { get; }
    Type EditorType { get; }
    bool IsSupported { get; }
    
    RecorderSettings CreateSettings();
    IRecorderEditor CreateEditor();
    bool ValidateConfig(object config, out string[] errors);
}
```

### **フェーズ2: プラグインシステムの実装**

#### 2.1 **動的プラグイン発見**
- Reflection-based plugin discovery
- Assembly scanning for `IRecorderPlugin`実装
- プラグインの動的ロード/アンロード

#### 2.2 **設定システムの統一**
```csharp
public abstract class BaseRecorderConfig
{
    public abstract bool Validate(out ValidationResult result);
    public abstract void ApplyDefaults();
    public abstract RecorderSettings ToRecorderSettings();
}
```

### **フェーズ3: UIの分離とモジュール化**

#### 3.1 **MVPパターンの採用**
```csharp
// View
public interface IMultiTimelineRecorderView
{
    void ShowProgress(float progress);
    void ShowError(string message);
    void UpdateTimelineList(IEnumerable<TimelineInfo> timelines);
}

// Presenter
public class MultiTimelineRecorderPresenter
{
    private readonly IMultiTimelineRecorderView view;
    private readonly IRecordingService recordingService;
    private readonly ISettingsService settingsService;
}
```

#### 3.2 **UIの再利用可能コンポーネント化**
- Generic recorder settings UI components
- Reusable validation UI
- Progress tracking components

### **フェーズ4: 拡張機能の追加**

#### 4.1 **イベントシステム**
```csharp
public static class RecorderEvents
{
    public static event Action<RecordingStartedArgs> RecordingStarted;
    public static event Action<RecordingCompletedArgs> RecordingCompleted;
    public static event Action<RecordingProgressArgs> RecordingProgress;
    public static event Action<ValidationFailedArgs> ValidationFailed;
}
```

#### 4.2 **設定プリセットシステム**
- 設定の保存/読み込み
- プリセットの共有機能
- デフォルト設定の管理

### **フェーズ5: パフォーマンス最適化**

#### 5.1 **非同期処理の改善**
- より効率的なコルーチン管理
- バックグラウンド処理の最適化
- メモリ使用量の削減

#### 5.2 **バッチ処理の最適化**
- 並列処理の導入
- キューベースの処理システム
- 中断/再開機能

## 🎯 実装優先順位

### **優先度 高**
1. コアインターフェースの定義 (`IRecorderPlugin`, `BaseRecorderConfig`)
2. 既存コードのリファクタリング (責任分離)
3. プラグインマネージャーの実装

### **優先度 中**
4. UI分離とMVPパターン導入
5. イベントシステム実装
6. 設定プリセット機能

### **優先度 低**
7. パフォーマンス最適化
8. 高度な並列処理
9. 拡張機能（プラグイン市場等）

## 📊 期待される効果

### **開発効率向上**
- 新しいレコーダータイプの追加が容易（プラグインとして）
- コードの再利用性向上
- 単体テスト可能な設計

### **保守性向上**
- 責任が明確に分離されたコード
- 依存関係の明確化
- バグ修正時の影響範囲の限定

### **拡張性向上**
- サードパーティによるプラグイン開発可能
- 新機能の追加時にコアコードの変更不要
- 設定やUIのカスタマイズが容易

## 📝 実装ガイド

### **段階的な移行戦略**
1. **Phase 1**: 既存コードの分析とインターフェース設計
2. **Phase 2**: コアインターフェースの実装と既存コードの移行
3. **Phase 3**: プラグインシステムの実装
4. **Phase 4**: UI層の分離とMVPパターン導入
5. **Phase 5**: テストの実装と最適化

### **破壊的変更の最小化**
- 既存APIの後方互換性維持
- 段階的な移行パス提供
- 十分なテストカバレッジ確保

このリファクタリングプランにより、パッケージの開発がよりスケーラブルになり、新機能の追加や保守が大幅に改善されます。