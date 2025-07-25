# Design Document

## Overview

Unity Multi Timeline Recorderの完全リファクタリング設計では、現在のモノリシックな構造を完全に廃止し、保守性、拡張性、テスト可能性を重視した新しいモジュラーアーキテクチャに置き換えます。この設計は、UIでの複雑なRecordジョブとタスクの作成・管理機能を根本的に改善し、API化を実現することを目標としています。

### 🆕 新アーキテクチャ vs 🔄 旧実装

**新アーキテクチャ (NEW) - 推奨**:
- `Editor/Core/Services/` - ビジネスロジックサービス
- `Editor/Core/Models/` - データモデル
- `Editor/UI/Controllers/` - UIコントローラー
- `Editor/UI/` - UIビュー

**旧実装 (LEGACY) - 移行中**:
- `Editor/MultiTimelineRecorder.cs` - モノリシックな実装（4000行超）

⚠️ **重要**: 新機能開発は新アーキテクチャを使用してください。旧実装は段階的に新アーキテクチャに移行中です。

### 現在のコードベース分析

以下の根本的な問題点が特定されました：
- `MultiTimelineRecorder.cs`が4000行を超える巨大なクラス
- UIロジックとビジネスロジックの密結合
- 設定管理の分散
- テストが困難な構造
- 新機能追加時の影響範囲の不明確さ

## Architecture

### 全体アーキテクチャパターン

**Service-Oriented Architecture + MVC** パターンを採用し、以下のシンプルな構造を実装します：

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   EditorWindow  │  │   UI Components │  │ Controllers  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                     Service Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Recording Svc   │  │ Configuration   │  │  Timeline    │ │
│  │                 │  │    Service      │  │   Service    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                      Data Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │     Models      │  │   Repositories  │  │  Factories   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 設計原則

- **実用性重視**: 過度な抽象化を避け、Unity環境での実装しやすさを優先
- **完全置換**: 既存コードを完全に新しいアーキテクチャに置き換える設計
- **テスト可能性**: 主要なビジネスロジックをテスト可能にする最小限の分離
- **拡張性**: 新機能追加時の影響範囲を限定
- **API優先**: UIに依存しないコアAPIを中心とした設計

## Data Management Structure

### Column-Based Data Management

Unity Multi Timeline Recorderは3カラムレイアウトで構成されており、各カラムが異なるデータ階層を管理しています：

#### Timelineカラムで管理されるもの
**データ構造**: `List<PlayableDirector> recordingQueueDirectors` + `List<int> selectedDirectorIndices`

- **Timeline選択状態**: どのTimelineが録画対象として選択されているか
- **Timeline識別情報**: `TimelineDirectorInfo`クラスによる永続化
  - `gameObjectName`: TimelineのGameObject名
  - `gameObjectPath`: Hierarchy内のパス
  - `assetName`: TimelineAssetの名前
- **Timeline有効/無効状態**: チェックボックスによる個別制御
- **現在のTimeline指定**: `currentTimelineIndexForRecorder`による設定対象Timeline
- **SignalEmitter情報**: 各Timelineの録画開始/終了マーカー
- **Timeline期間情報**: 秒数/フレーム数での表示切り替え

#### Recorderカラムで管理されるもの
**データ構造**: `Dictionary<int, MultiRecorderConfig> timelineRecorderConfigs`

- **Timeline固有のRecorder一覧**: 各TimelineごとのRecorder設定リスト
- **Recorder基本情報**:
  - `name`: Recorder表示名
  - `enabled`: Recorder有効/無効状態
  - `recorderType`: RecorderSettingsType（Movie, Image, AOV, Animation, Alembic, FBX）
- **Timeline固有の共通設定**（全Recorderに反映）:
  - `timelineTakeNumber`: Timeline固有のTake番号（`timelineTakeNumbers`による管理）
  - `timelinePreRollFrames`: Timeline固有のPre-rollフレーム数（将来実装予定）
- **Recorder選択状態**: `selectedRecorderIndex`による現在の編集対象
- **Recorderアイコン表示**: タイプ別の視覚的識別

#### RecorderSettingsで管理されるもの
**データ構造**: `MultiRecorderConfig.RecorderConfigItem`

- **出力設定**:
  - `fileName`: ファイル名テンプレート（ワイルドカード対応）
  - `outputPath`: 出力パス設定（OutputPathSettings）
  - `takeNumber`: Recorder固有のTake番号
  - `takeMode`: Take番号管理モード（RecordersTake/ClipTake）

- **品質・形式設定**:
  - `width`, `height`: 解像度設定
  - `frameRate`: フレームレート（グローバル制約あり）
  - `imageFormat`: 画像形式（PNG, JPG, EXR等）
  - `imageQuality`, `jpegQuality`: 品質設定
  - `captureAlpha`: アルファチャンネル取得
  - `exrCompression`: EXR圧縮設定

- **入力ソース設定**:
  - `imageSourceType`: 入力ソースタイプ（GameView, TargetCamera, RenderTexture）
  - `imageTargetCamera`: 対象カメラ（GameObjectReference経由）
  - `imageRenderTexture`: RenderTexture参照

- **レコーダータイプ別専用設定**:
  - `movieConfig`: MovieRecorderSettingsConfig
  - `aovConfig`: AOVRecorderSettingsConfig  
  - `alembicConfig`: AlembicRecorderSettingsConfig
  - `animationConfig`: AnimationRecorderSettingsConfig
  - `fbxConfig`: FBXRecorderSettingsConfig

### データ階層の関係性

```
Scene
├── Timeline 1 (PlayableDirector)
│   ├── Timeline固有設定（全Recorderに反映）
│   │   ├── timelineTakeNumber: Timeline固有Take番号
│   │   └── timelinePreRollFrames: Timeline固有Pre-rollフレーム数（将来実装）
│   ├── Recorder A (RecorderConfigItem)
│   │   ├── 出力設定 (fileName, outputPath, takeNumber)
│   │   ├── 品質設定 (resolution, format, quality)
│   │   └── 入力設定 (sourceType, camera, renderTexture)
│   └── Recorder B (RecorderConfigItem)
│       └── [同様の設定構造]
├── Timeline 2 (PlayableDirector)
│   ├── Timeline固有設定（全Recorderに反映）
│   └── [Timeline固有のRecorder設定群]
└── Global Settings
    ├── frameRate (全Recorderで統一)
    ├── globalOutputPath (共通出力パス)
    └── wildcardSettings (テンプレート管理)
```

### 設定の永続化と復元

- **シーン固有設定**: `SceneSpecificSettings`クラスによるシーンごとの設定保存
- **GameObject参照管理**: `GameObjectReference`クラスによる安全な参照保持
- **設定の自動復元**: シーン変更時の自動的な設定復元
- **設定の検証**: 参照切れや設定不整合の自動検出

## Components and Interfaces

### 1. Service Layer (Core Business Logic)

#### Core Services
```csharp
// 録画実行サービス（現在のMultiTimelineRecorderの主要機能）
public class RecordingService
{
    public RecordingResult ExecuteRecording(List<PlayableDirector> timelines, RecordingConfiguration config)
    {
        // フレームレートの統一性を検証
        ValidateFrameRateConsistency(config);
        
        // 現在のCreateRenderTimelineMultiple相当の処理
        var renderTimeline = CreateRenderTimeline(timelines, config);
        return ExecuteUnityRecorder(renderTimeline);
    }
    
    private void ValidateFrameRateConsistency(RecordingConfiguration config)
    {
        // 全レコーダーが同じフレームレートを使用することを確認
        // Timeline制約により、異なるフレームレートは設定不可
        var globalFrameRate = config.GlobalFrameRate;
        
        foreach (var timelineConfig in config.TimelineConfigs)
        {
            foreach (var recorderConfig in timelineConfig.RecorderConfigs)
            {
                // レコーダー設定作成時にグローバルフレームレートを適用
                recorderConfig.ApplyGlobalFrameRate(globalFrameRate);
            }
        }
    }
    
    public void CancelRecording(string jobId) { }
    public RecordingProgress GetProgress(string jobId) { }
}

// タイムライン管理サービス
public class TimelineService
{
    public List<PlayableDirector> ScanAvailableTimelines()
    {
        // 現在のScanTimelines相当の処理
    }
    
    public ValidationResult ValidateTimeline(PlayableDirector director) { }
}

// 設定管理サービス
public class ConfigurationService
{
    public void SaveConfiguration(RecordingConfiguration config) { }
    public RecordingConfiguration LoadConfiguration() { }
    public void SaveSceneSettings(string scenePath, SceneSettings settings) { }
    
    // ユーザーカスタマイズ設定管理
    public void SaveGlobalSettings(GlobalRecordingSettings settings) { }
    public GlobalRecordingSettings LoadGlobalSettings() { }
    public void SaveWildcardTemplates(WildcardTemplateSettings templates) { }
    public WildcardTemplateSettings LoadWildcardTemplates() { }
    public void SaveCustomWildcards(CustomWildcardSettings wildcards) { }
    public CustomWildcardSettings LoadCustomWildcards() { }
    
    // テンプレート管理
    public void AddTemplatePreset(TemplatePreset preset) { }
    public void RemoveTemplatePreset(string name) { }
    public List<TemplatePreset> GetTemplatePresets() { }
    public void ResetToDefaultTemplates() { }
    
    // カスタムワイルドカード管理
    public void AddCustomWildcard(string wildcard, string value) { }
    public void RemoveCustomWildcard(string wildcard) { }
    public Dictionary<string, string> GetCustomWildcards() { }
    
    // GameObject参照管理
    public void SaveGameObjectReferences(RecordingConfiguration config) { }
    public void RestoreGameObjectReferences(RecordingConfiguration config) { }
    public void ValidateGameObjectReferences(RecordingConfiguration config) { }
}

// レコーダー設定ファクトリー（既存のRecorderSettingsFactoryを拡張）
public class RecorderConfigurationFactory
{
    private readonly ConfigurationService _configService;
    
    public IRecorderConfiguration CreateConfiguration(RecorderSettingsType type)
    {
        // 既存のファクトリーメソッドを活用
        var config = CreateBasicConfiguration(type);
        
        // ユーザーカスタマイズされたデフォルトテンプレートを適用
        var templates = _configService.LoadWildcardTemplates();
        if (templates.DefaultTemplates.TryGetValue(type, out string template))
        {
            config.OutputPath = template;
        }
        
        return config;
    }
    
    public string GetDefaultTemplate(RecorderSettingsType type)
    {
        var templates = _configService.LoadWildcardTemplates();
        return templates.DefaultTemplates.TryGetValue(type, out string template) 
            ? template 
            : GetBuiltInDefaultTemplate(type);
    }
}

// 拡張されたワイルドカードプロセッサー
public class EnhancedWildcardProcessor
{
    private readonly WildcardRegistry _wildcardRegistry;
    
    public EnhancedWildcardProcessor(WildcardRegistry wildcardRegistry)
    {
        _wildcardRegistry = wildcardRegistry;
    }
    
    public string ProcessWildcards(string template, WildcardContext context)
    {
        string result = template;
        
        // Multi Timeline Recorderが処理するワイルドカードのみを処理
        // Unity Recorderのワイルドカード（<Take>, <Frame>, <Scene>, <Recorder>, <AOVType>, <GameObject>, <Product>, <Resolution>, <Date>, <Time>）は
        // そのまま文字列として保持し、Unity Recorder Clipに受け渡す
        foreach (var wildcard in _wildcardRegistry.MultiTimelineRecorderWildcards)
        {
            result = ProcessMultiTimelineRecorderWildcard(result, wildcard.Key, context);
        }
        
        // ユーザー定義カスタムワイルドカードの処理
        foreach (var customWildcard in _wildcardRegistry.CustomWildcards)
        {
            if (customWildcard.Value.ProcessingType == WildcardProcessingType.Custom)
            {
                result = result.Replace(customWildcard.Key, customWildcard.Value.CustomValue ?? "");
            }
        }
        
        // 結果の文字列にはUnity Recorderワイルドカードが残っており、
        // これらはUnity Recorder Clipに設定されてUnity Recorderによって最終的に処理される
        return result;
    }
    
    private string ProcessMultiTimelineRecorderWildcard(string template, string wildcard, WildcardContext context)
    {
        switch (wildcard)
        {
            case "<Timeline>":
                return template.Replace(wildcard, context.TimelineName ?? "Timeline");
                
            case "<TimelineTake>":
                int timelineTakeValue = context.TimelineTakeNumber ?? context.TakeNumber;
                return template.Replace(wildcard, timelineTakeValue.ToString("D3"));
                
            case "<RecorderTake>":
                return template.Replace(wildcard, context.TakeNumber.ToString());
                
            case "<RecorderName>":
                return template.Replace(wildcard, context.RecorderDisplayName ?? context.RecorderName ?? "Recorder");
                
            default:
                return template;
        }
    }
    
    public List<string> GetAvailableWildcards()
    {
        var wildcards = new List<string>();
        
        // Unity Recorderワイルドカード（パススルー）
        wildcards.AddRange(_wildcardRegistry.UnityRecorderWildcards.Keys);
        
        // Multi Timeline Recorderワイルドカード
        wildcards.AddRange(_wildcardRegistry.MultiTimelineRecorderWildcards.Keys);
        
        // カスタムワイルドカード
        wildcards.AddRange(_wildcardRegistry.CustomWildcards.Keys);
        
        return wildcards;
    }
    
    public Dictionary<string, List<WildcardDefinition>> GetCategorizedWildcards()
    {
        return _wildcardRegistry.GetWildcardsByCategory();
    }
    
    public bool IsUnityRecorderWildcard(string wildcard)
    {
        return _wildcardRegistry.UnityRecorderWildcards.ContainsKey(wildcard);
    }
    
    public bool IsMultiTimelineRecorderWildcard(string wildcard)
    {
        return _wildcardRegistry.MultiTimelineRecorderWildcards.ContainsKey(wildcard);
    }
    
    public bool ShouldPreserveForUnityRecorder(string template)
    {
        // Unity Recorderが処理するワイルドカードが含まれている場合はtrue
        foreach (var unityWildcard in _wildcardRegistry.UnityRecorderWildcards.Keys)
        {
            if (template.Contains(unityWildcard))
            {
                return true;
            }
        }
        return false;
    }
}
```

#### Data Models
```csharp
// 録画設定（現在のMultiRecorderConfigを整理）
public class RecordingConfiguration
{
    public int FrameRate { get; set; }
    public Resolution Resolution { get; set; }
    public string OutputPath { get; set; }
    public List<TimelineRecorderConfig> TimelineConfigs { get; set; }
    public GlobalSettings GlobalSettings { get; set; }
}

// タイムライン固有の設定
public class TimelineRecorderConfig
{
    public PlayableDirector Director { get; set; }
    public bool IsEnabled { get; set; }
    public List<IRecorderConfiguration> RecorderConfigs { get; set; }
}

// レコーダー設定の統一インターフェース（既存の構造を活用）
public interface IRecorderConfiguration
{
    string Name { get; set; }
    RecorderSettingsType Type { get; }
    bool IsEnabled { get; set; }
    ValidationResult Validate();
    RecorderSettings CreateUnityRecorderSettings(WildcardContext context);
}
```

### 2. UI Layer (Controllers & Views)

#### Controllers
```csharp
// メインウィンドウコントローラー（現在のMultiTimelineRecorderを分割）
public class MainWindowController
{
    private readonly RecordingService _recordingService;
    private readonly TimelineService _timelineService;
    private readonly ConfigurationService _configService;
    
    public void StartRecording()
    {
        var config = _configService.LoadConfiguration();
        var timelines = GetSelectedTimelines();
        _recordingService.ExecuteRecording(timelines, config);
    }
    
    public void AddTimeline(PlayableDirector director) { }
    public void RemoveTimeline(PlayableDirector director) { }
    public void UpdateConfiguration(RecordingConfiguration config) { }
}

// レコーダー設定コントローラー
public class RecorderConfigurationController
{
    private readonly RecorderConfigurationFactory _factory;
    
    public void AddRecorder(RecorderSettingsType type) { }
    public void RemoveRecorder(string recorderId) { }
    public void UpdateRecorderConfig(IRecorderConfiguration config) { }
}
```

#### View Components（既存のUI構造を活用）
```csharp
// メインウィンドウビュー（現在のOnGUIを整理）
public class MainWindowView : EditorWindow
{
    private MainWindowController _controller;
    
    private void OnGUI()
    {
        DrawGlobalSettings();
        DrawTimelineSelection();
        DrawRecorderConfiguration();
        DrawRecordingControls();
    }
    
    private void DrawTimelineSelection() { /* 現在のDrawTimelineSelectionColumn */ }
    private void DrawRecorderConfiguration() { /* 現在のDrawRecorderListColumn */ }
}

// 設定UI コンポーネント（既存のエディターを活用）
public class RecorderConfigurationView
{
    public void DrawImageRecorderSettings(ImageRecorderConfiguration config) { }
    public void DrawMovieRecorderSettings(MovieRecorderConfiguration config) { }
    // 他のレコーダータイプ...
}

// ユーザーカスタマイズ設定UI
public class CustomizationSettingsWindow : EditorWindow
{
    private readonly ConfigurationService _configService;
    private WildcardTemplateSettings _templateSettings;
    private CustomWildcardSettings _wildcardSettings;
    
    public void DrawTemplateSettings()
    {
        EditorGUILayout.LabelField("Default Templates", EditorStyles.boldLabel);
        
        foreach (var recorderType in Enum.GetValues(typeof(RecorderSettingsType)))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(recorderType.ToString(), GUILayout.Width(100));
            
            string currentTemplate = _templateSettings.DefaultTemplates[recorderType];
            string newTemplate = EditorGUILayout.TextField(currentTemplate);
            
            if (newTemplate != currentTemplate)
            {
                _templateSettings.DefaultTemplates[recorderType] = newTemplate;
            }
            
            // プリセット選択ボタン
            if (GUILayout.Button("Presets", GUILayout.Width(60)))
            {
                ShowTemplatePresetsMenu(recorderType);
            }
            
            // リセットボタン
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                _templateSettings.DefaultTemplates[recorderType] = GetBuiltInDefaultTemplate(recorderType);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        // テンプレートプリセット管理
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Template Presets", EditorStyles.boldLabel);
        
        foreach (var preset in _templateSettings.TemplatePresets)
        {
            EditorGUILayout.BeginHorizontal();
            preset.Name = EditorGUILayout.TextField(preset.Name, GUILayout.Width(100));
            preset.Template = EditorGUILayout.TextField(preset.Template);
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                _templateSettings.TemplatePresets.Remove(preset);
                break;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button("Add New Preset"))
        {
            _templateSettings.TemplatePresets.Add(new TemplatePreset("New Preset", ""));
        }
    }
    
    public void DrawCustomWildcardSettings()
    {
        EditorGUILayout.LabelField("Custom Wildcards", EditorStyles.boldLabel);
        
        // 既存のカスタムワイルドカード編集
        var wildcardsToRemove = new List<string>();
        foreach (var wildcard in _wildcardSettings.CustomWildcards)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(wildcard.Key, GUILayout.Width(100));
            
            string newValue = EditorGUILayout.TextField(wildcard.Value);
            if (newValue != wildcard.Value)
            {
                _wildcardSettings.CustomWildcards[wildcard.Key] = newValue;
            }
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                wildcardsToRemove.Add(wildcard.Key);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        // 削除処理
        foreach (var key in wildcardsToRemove)
        {
            _wildcardSettings.CustomWildcards.Remove(key);
        }
        
        // 新しいカスタムワイルドカード追加
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add New:", GUILayout.Width(70));
        
        string newWildcardName = EditorGUILayout.TextField("", GUILayout.Width(100));
        string newWildcardValue = EditorGUILayout.TextField("");
        
        if (GUILayout.Button("Add", GUILayout.Width(50)) && 
            !string.IsNullOrEmpty(newWildcardName) && 
            !string.IsNullOrEmpty(newWildcardValue))
        {
            string formattedName = newWildcardName.StartsWith("<") ? newWildcardName : $"<{newWildcardName}>";
            if (!formattedName.EndsWith(">"))
            {
                formattedName += ">";
            }
            
            _wildcardSettings.CustomWildcards[formattedName] = newWildcardValue;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // ワイルドカード表示名のカスタマイズ
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Wildcard Display Names", EditorStyles.boldLabel);
        
        foreach (var displayName in _wildcardSettings.WildcardDisplayNames.ToList())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(displayName.Key, GUILayout.Width(100));
            
            string newDisplayName = EditorGUILayout.TextField(displayName.Value);
            if (newDisplayName != displayName.Value)
            {
                _wildcardSettings.WildcardDisplayNames[displayName.Key] = newDisplayName;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void ShowTemplatePresetsMenu(RecorderSettingsType recorderType)
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (var preset in _templateSettings.TemplatePresets)
        {
            menu.AddItem(new GUIContent(preset.Name), false, () => {
                _templateSettings.DefaultTemplates[recorderType] = preset.Template;
            });
        }
        
        menu.ShowAsContext();
    }
}
```

## Data Models

### 設定データモデル

```csharp
// グローバル設定
[Serializable]
public class GlobalRecordingSettings
{
    // フレームレート（全レコーダーで統一 - Timeline制約）
    public int GlobalFrameRate { get; set; } = 24;
    
    // デフォルト設定（レコーダー作成時の初期値）
    public Resolution DefaultResolution { get; set; } = new Resolution(1920, 1080);
    public OutputPathConfiguration DefaultOutputPath { get; set; }
    public bool DebugMode { get; set; } = false;
    
    // 統一されたワイルドカード・テンプレート管理
    public WildcardManagementSettings WildcardManagement { get; set; } = new WildcardManagementSettings();
}

// 統一されたワイルドカード・テンプレート管理システム
[Serializable]
public class WildcardManagementSettings
{
    // ワイルドカード定義（標準 + カスタム）
    public WildcardRegistry WildcardRegistry { get; set; } = new WildcardRegistry();
    
    // テンプレート管理
    public TemplateRegistry TemplateRegistry { get; set; } = new TemplateRegistry();
}

// ワイルドカード統合管理
[Serializable]
public class WildcardRegistry
{
    // Unity Recorderが処理するワイルドカード（パススルー必須）
    // これらの文字列はそのままUnity Recorder Clipに受け渡される
    public Dictionary<string, WildcardDefinition> UnityRecorderWildcards { get; set; } = new Dictionary<string, WildcardDefinition>
    {
        { "<Take>", new WildcardDefinition("<Take>", "Take Number", "Unity Recorder", "Unity Recorder standard take number - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Frame>", new WildcardDefinition("<Frame>", "Frame Number", "Unity Recorder", "Frame number (4-digit zero-padded) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Product>", new WildcardDefinition("<Product>", "Product Name", "Unity Recorder", "Application product name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Resolution>", new WildcardDefinition("<Resolution>", "Resolution", "Unity Recorder", "Output resolution (WxH) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Date>", new WildcardDefinition("<Date>", "Date", "Unity Recorder", "Current date (YYYYMMDD) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Time>", new WildcardDefinition("<Time>", "Time", "Unity Recorder", "Current time (HHMMSS) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Scene>", new WildcardDefinition("<Scene>", "Scene Name", "Unity Recorder", "Current scene name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<Recorder>", new WildcardDefinition("<Recorder>", "Recorder Name", "Unity Recorder", "Recorder configuration name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<AOVType>", new WildcardDefinition("<AOVType>", "AOV Type", "Unity Recorder", "AOV pass type name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
        { "<GameObject>", new WildcardDefinition("<GameObject>", "GameObject", "Unity Recorder", "Target GameObject name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) }
    };
    
    // Multi Timeline Recorderで追加されたワイルドカード（Unity Recorderにはない独自機能）
    public Dictionary<string, WildcardDefinition> MultiTimelineRecorderWildcards { get; set; } = new Dictionary<string, WildcardDefinition>
    {
        { "<Timeline>", new WildcardDefinition("<Timeline>", "Timeline Name", "Multi Timeline Recorder", "Timeline asset name - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) },
        { "<TimelineTake>", new WildcardDefinition("<TimelineTake>", "Timeline Take", "Multi Timeline Recorder", "Timeline-specific take number (3-digit) - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) },
        { "<RecorderTake>", new WildcardDefinition("<RecorderTake>", "Recorder Take", "Multi Timeline Recorder", "Recorder-specific take number - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) },
        { "<RecorderName>", new WildcardDefinition("<RecorderName>", "Recorder Display Name", "Multi Timeline Recorder", "Recorder display name - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) }
    };
    
    // ユーザー定義カスタムワイルドカード
    public Dictionary<string, WildcardDefinition> CustomWildcards { get; set; } = new Dictionary<string, WildcardDefinition>();
    
    // 全ワイルドカードを取得
    public Dictionary<string, WildcardDefinition> GetAllWildcards()
    {
        var all = new Dictionary<string, WildcardDefinition>(StandardWildcards);
        foreach (var custom in CustomWildcards)
        {
            all[custom.Key] = custom.Value;
        }
        return all;
    }
    
    // カテゴリ別ワイルドカード取得
    public Dictionary<string, List<WildcardDefinition>> GetWildcardsByCategory()
    {
        var categorized = new Dictionary<string, List<WildcardDefinition>>();
        
        foreach (var wildcard in GetAllWildcards().Values)
        {
            if (!categorized.ContainsKey(wildcard.Category))
            {
                categorized[wildcard.Category] = new List<WildcardDefinition>();
            }
            categorized[wildcard.Category].Add(wildcard);
        }
        
        return categorized;
    }
}

// テンプレート統合管理
[Serializable]
public class TemplateRegistry
{
    // レコーダータイプ別デフォルトテンプレート
    // Unity Recorderワイルドカード（<Scene>, <Take>, <Frame>等）とMulti Timeline Recorderワイルドカード（<Timeline>, <TimelineTake>等）を組み合わせ
    public Dictionary<RecorderSettingsType, string> DefaultTemplates { get; set; } = new Dictionary<RecorderSettingsType, string>
    {
        { RecorderSettingsType.Image, "Recordings/<Scene>_<Timeline>_<TimelineTake>/<Scene>_<Timeline>_<TimelineTake>_<Frame>" },
        { RecorderSettingsType.Movie, "Recordings/<Scene>_<Timeline>_<TimelineTake>" },
        { RecorderSettingsType.Animation, "Assets/Animations/<Scene>_<Timeline>_<TimelineTake>" },
        { RecorderSettingsType.Alembic, "Recordings/<Scene>_<Timeline>_<TimelineTake>" },
        { RecorderSettingsType.AOV, "Recordings/<Scene>_<Timeline>_<TimelineTake>_<AOVType>/<AOVType>_<Frame>" },
        { RecorderSettingsType.FBX, "Recordings/<Scene>_<Timeline>_<TimelineTake>" }
    };
    
    // テンプレートプリセット
    public List<TemplatePreset> TemplatePresets { get; set; } = new List<TemplatePreset>
    {
        new TemplatePreset("By Timeline", "Recordings/<Timeline>", "Organize by timeline name"),
        new TemplatePreset("By Scene", "Recordings/<Scene>/<Timeline>", "Organize by scene, then timeline"),
        new TemplatePreset("By Date", "Recordings/<Date>/<Timeline>", "Organize by date, then timeline"),
        new TemplatePreset("Detailed", "Recordings/<Date>/<Scene>/<Timeline>_<Take>", "Detailed organization with date and scene"),
        new TemplatePreset("Production", "Output/<Product>/<Scene>/<Timeline>_v<Take>", "Production-ready organization")
    };
    
    // カスタムテンプレート（ユーザー定義）
    public List<TemplatePreset> CustomTemplates { get; set; } = new List<TemplatePreset>();
    
    // 全テンプレートプリセット取得
    public List<TemplatePreset> GetAllTemplatePresets()
    {
        var all = new List<TemplatePreset>(TemplatePresets);
        all.AddRange(CustomTemplates);
        return all;
    }
}

// ワイルドカード処理タイプ
public enum WildcardProcessingType
{
    UnityRecorder,           // Unity Recorderが処理（パススルー必須）
    MultiTimelineRecorder,   // Multi Timeline Recorderが処理
    Custom                   // ユーザー定義カスタムワイルドカード
}

// ワイルドカード定義
[Serializable]
public class WildcardDefinition
{
    public string Wildcard { get; set; }                    // "<Scene>"
    public string DisplayName { get; set; }                 // "Scene Name"
    public string Category { get; set; }                    // "Basic"
    public string Description { get; set; }                 // "Current scene name"
    public bool IsBuiltIn { get; set; }                     // 標準ワイルドカードかどうか
    public WildcardProcessingType ProcessingType { get; set; } // 処理タイプ
    public string CustomValue { get; set; }                 // カスタムワイルドカードの場合の固定値
    
    public WildcardDefinition() { }
    
    public WildcardDefinition(string wildcard, string displayName, string category, string description, bool isBuiltIn, WildcardProcessingType processingType, string customValue = null)
    {
        Wildcard = wildcard;
        DisplayName = displayName;
        Category = category;
        Description = description;
        IsBuiltIn = isBuiltIn;
        ProcessingType = processingType;
        CustomValue = customValue;
    }
}

// テンプレートプリセット
[Serializable]
public class TemplatePreset
{
    public string Name { get; set; }
    public string Template { get; set; }
    public string Description { get; set; }
    
    public TemplatePreset(string name, string template, string description = "")
    {
        Name = name;
        Template = template;
        Description = description;
    }
}

// シーン固有設定
[Serializable]
public class SceneRecordingSettings
{
    public string ScenePath { get; set; }
    public List<string> SavedJobIds { get; set; } = new List<string>();
    public Dictionary<string, int> TimelineTakeNumbers { get; set; } = new Dictionary<string, int>();
}

// レコーダー設定の基底クラス
[Serializable]
public abstract class RecorderConfigurationBase : IRecorderConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public RecorderSettingsType Type { get; protected set; }
    public bool IsEnabled { get; set; } = true;
    public int TakeNumber { get; set; } = 1;
    
    // フレームレートは個別設定不可（Timeline制約）
    // グローバル設定から取得される
    
    public abstract ValidationResult Validate();
    public abstract RecorderSettings CreateUnityRecorderSettings(WildcardContext context, int globalFrameRate);
}

// GameObject参照管理（シーン内オブジェクトとのバインディング保持）
[Serializable]
public class GameObjectReference
{
    public string GameObjectName { get; set; }
    public string ScenePath { get; set; }
    public string HierarchyPath { get; set; }  // Transform階層パス
    public int InstanceId { get; set; }        // 一時的な識別用
    
    // シーン再読み込み後の参照復元
    public GameObject ResolveReference()
    {
        // 1. InstanceIdで検索（同一セッション内）
        // 2. HierarchyPathで検索（階層パスによる特定）
        // 3. GameObjectNameで検索（フォールバック）
    }
}

// 具体的なレコーダー設定クラス
[Serializable]
public class ImageRecorderConfiguration : RecorderConfigurationBase
{
    public ImageRecorderSettings.ImageRecorderOutputFormat OutputFormat { get; set; }
    public bool CaptureAlpha { get; set; }
    public ImageRecorderSourceType SourceType { get; set; }
    
    // GameObject参照の安全な保持
    public GameObjectReference TargetCameraReference { get; set; }
    public GameObjectReference RenderTextureSourceReference { get; set; }
    
    // 実行時の実際の参照（非シリアライズ）
    [NonSerialized]
    public Camera TargetCamera;
    [NonSerialized] 
    public RenderTexture RenderTexture;
    
    public int JpegQuality { get; set; } = 75;
    public ImageRecorderSettings.EXRCompressionType ExrCompression { get; set; }
    
    // 参照の復元
    public void RestoreGameObjectReferences()
    {
        if (TargetCameraReference != null)
        {
            var cameraObject = TargetCameraReference.ResolveReference();
            TargetCamera = cameraObject?.GetComponent<Camera>();
        }
        
        if (RenderTextureSourceReference != null)
        {
            var rtObject = RenderTextureSourceReference.ResolveReference();
            // RenderTextureの復元ロジック
        }
    }
    
    public override ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (SourceType == ImageRecorderSourceType.TargetCamera && TargetCamera == null)
        {
            result.AddError("Target camera is required when using TargetCamera source type");
        }
        
        if (SourceType == ImageRecorderSourceType.RenderTexture && RenderTexture == null)
        {
            result.AddError("Render texture is required when using RenderTexture source type");
        }
        
        return result;
    }
}
```

## Error Handling

### 統一エラーハンドリング戦略

```csharp
// カスタム例外階層
public abstract class RecordingException : Exception
{
    public string ErrorCode { get; }
    public RecordingException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class RecordingConfigurationException : RecordingException
{
    public RecordingConfigurationException(string message) 
        : base("RECORDING_CONFIG_ERROR", message) { }
}

public class RecordingExecutionException : RecordingException
{
    public RecordingExecutionException(string message) 
        : base("RECORDING_EXECUTION_ERROR", message) { }
}

// エラーハンドリングサービス
public interface IErrorHandlingService
{
    void HandleError(Exception exception);
    void HandleError(Exception exception, string context);
    Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName);
}

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;

    public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (RecordingException ex)
        {
            _logger.LogError($"Recording error in {operationName}: {ex.Message}");
            await _notificationService.NotifyErrorAsync(new RecordingError(ex.ErrorCode, ex.Message));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error in {operationName}: {ex.Message}");
            await _notificationService.NotifyErrorAsync(new RecordingError("UNEXPECTED_ERROR", "An unexpected error occurred"));
            throw new RecordingExecutionException($"Unexpected error in {operationName}: {ex.Message}");
        }
    }
}
```

### ログ機能の統一

```csharp
// ログレベルとカテゴリ
public enum LogLevel
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error
}

public enum LogCategory
{
    General,
    Recording,
    Configuration,
    UI,
    FileSystem
}

// ログサービス
public interface ILogger
{
    void Log(LogLevel level, LogCategory category, string message);
    void LogVerbose(string message, LogCategory category = LogCategory.General);
    void LogDebug(string message, LogCategory category = LogCategory.General);
    void LogInfo(string message, LogCategory category = LogCategory.General);
    void LogWarning(string message, LogCategory category = LogCategory.General);
    void LogError(string message, LogCategory category = LogCategory.General);
}

public class UnityConsoleLogger : ILogger
{
    public void Log(LogLevel level, LogCategory category, string message)
    {
        var formattedMessage = $"[{category}] {message}";
        
        switch (level)
        {
            case LogLevel.Error:
                Debug.LogError(formattedMessage);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            default:
                Debug.Log(formattedMessage);
                break;
        }
    }
}
```

## Testing Strategy

### テスト方針

- **Service Layer**: 主要なビジネスロジックに対する単体テスト
- **Configuration**: 設定の検証とシリアライゼーションのテスト
- **Integration**: Unity Recorderとの統合テスト（手動テスト中心）

### テスト例
```csharp
[Test]
public void RecordingService_ValidConfiguration_ExecutesSuccessfully()
{
    // Arrange
    var service = new RecordingService();
    var config = CreateValidConfiguration();
    var timelines = CreateTestTimelines();

    // Act
    var result = service.ExecuteRecording(timelines, config);

    // Assert
    Assert.IsTrue(result.IsSuccess);
}

[Test]
public void ConfigurationService_SaveAndLoad_PreservesData()
{
    // Arrange
    var service = new ConfigurationService();
    var originalConfig = CreateTestConfiguration();

    // Act
    service.SaveConfiguration(originalConfig);
    var loadedConfig = service.LoadConfiguration();

    // Assert
    Assert.AreEqual(originalConfig.FrameRate, loadedConfig.FrameRate);
}
```

## API Design (Future Implementation)

### プログラマティックAPI

将来的なAPI化を見据えて、UIに依存しないコアAPIを設計します：

```csharp
// 公開API（将来的な外部制御用）
public static class MultiTimelineRecorderAPI
{
    public static RecordingResult ExecuteRecording(RecordingConfiguration config)
    {
        var service = new RecordingService();
        return service.ExecuteRecording(config.GetSelectedTimelines(), config);
    }
    
    public static RecordingConfiguration CreateConfiguration()
    {
        return new RecordingConfiguration();
    }
    
    public static void SaveConfiguration(RecordingConfiguration config, string path = null)
    {
        var configService = new ConfigurationService();
        configService.SaveConfiguration(config);
    }
}

// 設定ビルダー（流暢なAPI）
public class RecordingConfigurationBuilder
{
    private RecordingConfiguration _config = new RecordingConfiguration();
    
    public RecordingConfigurationBuilder WithFrameRate(int frameRate)
    {
        _config.FrameRate = frameRate;
        return this;
    }
    
    public RecordingConfigurationBuilder AddTimeline(PlayableDirector director)
    {
        _config.TimelineConfigs.Add(new TimelineRecorderConfig { Director = director });
        return this;
    }
    
    public RecordingConfiguration Build() => _config;
}
```

## Legacy System Analysis

### 旧システムの機能一覧

現在のMultiTimelineRecorderで提供されている機能を網羅的に分析し、新システムでの実装を保証します：

#### コア機能
1. **複数タイムライン同時録画**
   - 複数のPlayableDirectorを選択して一括録画
   - 一つのPlayableDirectorごとに複数のRecorderを設定可能
   - タイムライン毎の個別設定管理も可能
   - バッチ処理による効率的な録画実行
   

2. **多様な出力形式サポート**
   - Movie録画: MP4, MOV, WebM形式
   - Image Sequence: PNG, JPG, EXR形式  
   - Animation Clip録画
   - Alembic形式エクスポート
   - FBX形式エクスポート
   - AOV (Arbitrary Output Variables) 録画

3. **高度なパス管理**
   - ワイルドカード対応のファイル名生成
   - テイク番号の自動管理
   - シーン名、タイムライン名の自動挿入
   - カスタムパス設定

#### UI機能
1. **タイムライン選択UI**
   - シーン内タイムラインの自動検索・表示
   - チェックボックスによる選択/非選択
   - タイムライン情報の表示（名前、長さ、状態）

2. **レコーダー設定UI**
   - レコーダータイプ毎の専用設定パネル
   - 動的な設定項目表示
   - リアルタイム設定検証

3. **録画制御UI**
   - 録画開始/停止ボタン
   - 進捗表示とキャンセル機能
   - エラー表示とログ出力

#### 設定管理機能
1. **設定の永続化**
   - シーン毎の設定保存
   - グローバル設定の管理
   - 設定のインポート/エクスポート

2. **設定検証**
   - 録画前の設定チェック
   - 無効な設定の警告表示
   - 自動修正提案

### 旧システムのワークフロー

#### 基本録画ワークフロー
```
1. Unity Editorでツールを開く
   ↓
2. シーン内のタイムラインを自動検索
   ↓
3. 録画対象タイムラインを選択
   ↓
4. 各タイムラインのレコーダー設定を構成
   ↓
5. 出力パスとファイル名を設定
   ↓
6. 録画実行（バッチ処理）
   ↓
7. 進捗監視とエラーハンドリング
   ↓
8. 録画完了とファイル出力確認
```

#### 設定管理ワークフロー
```
1. 設定の作成・編集
   ↓
2. 設定の検証
   ↓
3. 設定の保存（シーン毎/グローバル）
   ↓
4. 設定の読み込み・復元
```

### 旧システムのUI構成

#### メインウィンドウレイアウト
```
┌─────────────────────────────────────────────────────────┐
│                    Global Settings                      │
│  ┌─────────────────┐  ┌─────────────────────────────────┐ │
│  │ Frame Rate      │  │ Resolution                      │ │
│  │ Output Path     │  │ Debug Options                   │ │
│  └─────────────────┘  └─────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│                  Timeline Selection                     │
│  ☑ Timeline_01 (Duration: 10.5s)                      │
│  ☐ Timeline_02 (Duration: 5.2s)                       │
│  ☑ Timeline_03 (Duration: 8.7s)                       │
├─────────────────────────────────────────────────────────┤
│                 Recorder Configuration                  │
│  ┌─ Movie Recorder ────────────────────────────────────┐ │
│  │ Format: MP4    Quality: High    Bitrate: 5000      │ │
│  └─────────────────────────────────────────────────────┘ │
│  ┌─ Image Recorder ────────────────────────────────────┐ │
│  │ Format: PNG    Alpha: Yes       Quality: 100       │ │
│  └─────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│                Recording Controls                       │
│  [Start Recording]  [Stop]  [Progress: ████░░░ 60%]   │
└─────────────────────────────────────────────────────────┘
```

### 3カラムレイアウトの詳細仕様

現在のシステムの**Timeline、Recorder、RecorderSettings**の3カラム構成は非常に効果的であり、完全に維持する必要があります：

#### 左カラム: Timeline Selection Column
```csharp
// 現在の実装で維持すべき機能
- "Add Timeline" クリッカブルヘッダー（プラスアイコン付き）
- ドロップダウンメニューでシーン内タイムライン選択
- チェックボックスによる複数タイムライン選択
- "Enable All" / "Disable All" 一括操作ボタン
- タイムライン情報表示：
  * タイムラインアイコン
  * SignalEmitter検出時のマーカーアイコン
  * タイムライン名
  * 録画期間表示（SignalEmitter対応時は期間表示）
  * 右クリックメニューでタイムライン削除
- 現在選択中のタイムライン強調表示（青色ハイライト + アクセントバー）
- ゼブラストライプ表示
- スクロール対応
```

#### 中央カラム: Recorder List Column
```csharp
// 現在の実装で維持すべき機能
- "Add Recorder" クリッカブルヘッダー（プラスアイコン付き）
- レコーダータイプ選択メニュー（絵文字アイコン付き）:
  * 🎬 Movie
  * 🖼️ Image Sequence  
  * 🌈 AOV Image Sequence
  * 🎭 Animation Clip
  * 🗂️ FBX
  * 📦 Alembic
- タイムライン固有のTake番号表示・編集
- "Enable All" / "Disable All" 一括操作ボタン
- レコーダーリスト表示：
  * チェックボックスによる有効/無効切り替え
  * レコーダータイプアイコン
  * 編集可能なレコーダー名
  * 選択状態のハイライト表示
- 右クリックメニュー：
  * "Apply to All Selected Timelines" （複数タイムライン選択時）- 設定処理時間短縮機能
  * "削除"
  * "複製"
  * "上に移動" / "下に移動"
- スクロール対応

// 重要な機能特性
- タイムライン毎に独立したレコーダー設定管理
- レコーダー設定の全タイムライン一括適用機能
- 効率的な設定ワークフロー
```

#### 右カラム: Recorder Detail Column
```csharp
// 現在の実装で維持すべき機能
- Inspector風ヘッダー（設定アイコン + "Recorder Settings"）
- レコーダータイプ表示（背景色付きヘッダー）
- レコーダー名編集フィールド
- レコーダータイプ固有の設定UI：
  * ImageRecorderEditor
  * MovieRecorderEditor
  * AOVRecorderEditor
  * AnimationRecorderEditor
  * AlembicRecorderEditor
  * FBXRecorderEditor
- Output Path設定（下部配置）：
  * Path Mode選択（UseGlobal/RelativeToGlobal/Custom）- グローバル設定継承機能
  * Location設定（Project/Persistent/Temporary/Absolute）
  * パス入力フィールド
  * ブラウズボタン（...）
  * Wildcardsボタン
  * Path Preview表示
- スクロール対応

// 重要な機能特性
- Recorderごとの完全に独立した個別設定
- グローバル設定からの継承オプション（UseGlobal）
- 相対パス設定によるグローバル設定の部分継承（RelativeToGlobal）
- 完全カスタム設定（Custom）
```

### ワイルドカード機能の詳細仕様

現在のシステムの高度なワイルドカード機能を完全に継承：

```csharp
// サポートするワイルドカード
public static class Wildcards
{
    public const string Take = "<Take>";           // Unity Recorder標準
    public const string RecorderTake = "<RecorderTake>";  // レコーダー固有Take
    public const string Scene = "<Scene>";        // シーン名
    public const string Frame = "<Frame>";        // フレーム番号（4桁ゼロパディング）
    public const string Time = "<Time>";          // 時刻（HHmmss）
    public const string Resolution = "<Resolution>"; // 解像度（1920x1080）
    public const string Date = "<Date>";          // 日付（yyyyMMdd）
    public const string Product = "<Product>";    // プロダクト名
    public const string AOVType = "<AOVType>";    // AOVタイプ名
    public const string Recorder = "<Recorder>";  // レコーダー名
    public const string GameObject = "<GameObject>"; // ゲームオブジェクト名
    public const string Timeline = "<Timeline>";  // タイムライン名
    public const string TimelineTake = "<TimelineTake>"; // タイムライン固有Take（3桁）
    public const string RecorderName = "<RecorderName>"; // レコーダー表示名
}

// デフォルトテンプレート
- Image: "Recordings/<Scene>_<Take>/<Scene>_<Take>_<Frame>"
- Movie: "Recordings/<Scene>_<Take>"
- Animation: "Assets/Animations/<Scene>_<Take>"
- Alembic: "Recordings/<Scene>_<Take>"
- AOV: "Recordings/<Scene>_<Take>_<AOVType>/<AOVType>_<Frame>"
```

### SignalEmitter機能の詳細仕様

現在のシステムの高度なSignalEmitter機能を完全に継承：

```csharp
// SignalEmitter制御機能
- useSignalEmitterTiming: bool フラグ
- startTimingName: string 開始シグナル名（デフォルト: "pre"）
- endTimingName: string 終了シグナル名（デフォルト: "post"）
- showTimingInFrames: bool 表示形式切り替え（秒数/フレーム数）

// SignalEmitter検出機能
- [MTR]トラック優先検索
- MarkerTrackフォールバック
- SignalEmitter表示名の自動取得
- 録画期間の自動計算
- フォールバック機能（SignalEmitter未検出時は全期間）

// UI表示機能
- タイムライン一覧でのSignalEmitterマーカー表示
- 録画期間の表示（秒数またはフレーム数）
- SignalEmitter設定パネル
```

### レコーダー設定の詳細仕様

各レコーダータイプの詳細設定を完全に継承：

```csharp
// Image Recorder
- OutputFormat: PNG/JPEG/EXR
- CaptureAlpha: bool
- SourceType: GameView/TargetCamera/RenderTexture
- JpegQuality: int (1-100)
- ExrCompression: None/RLE/ZIP/PIZ

// Movie Recorder
- OutputFormat: MP4/MOV/WebM
- VideoBitRateMode: Low/Medium/High/Custom
- CaptureAudio: bool
- CaptureAlpha: bool
- AudioBitRateMode: Low/Medium/High/VeryHigh

// AOV Recorder
- selectedAOVs: AOVType flags (Beauty/Albedo/Normal/Depth等)
- outputFormat: PNG/JPEG/EXR16/EXR32
- IsMultiPartEXR: bool
- FlipVertical: bool

// Animation Recorder
- recordingProperties: TransformOnly/All
- recordingScope: SingleGameObject/Hierarchy
- compressionLevel: None/Low/Medium/High
- interpolationMode: Linear/Constant

// Alembic Recorder
- exportTargets: MeshRenderer/Transform/Camera等
- exportScope: EntireScene/SelectedObjects
- scaleFactor: float
- handedness: Left/Right
- timeSamplingType: Uniform/Acyclic

// FBX Recorder
- recordHierarchy: bool
- clampedTangents: bool
- animationCompression: None/Keyframe/Lossy
- exportGeometry: bool
- transferAnimationSource/Dest: GameObject
```

### 重要な機能特性

#### 1. Recorderごとの個別設定機能
```csharp
// タイムライン毎に独立したレコーダー設定管理
public class TimelineRecorderConfig
{
    public PlayableDirector Director { get; set; }
    public bool IsEnabled { get; set; }
    public List<IRecorderConfiguration> RecorderConfigs { get; set; } // 個別設定（フレームレート除く）
}

// 各レコーダーの設定範囲
- 解像度: レコーダー毎に個別設定可能
- 出力形式: レコーダー毎に個別設定可能  
- 品質設定: レコーダー毎に個別設定可能
- 出力パス: レコーダー毎に個別設定可能
- フレームレート: 全レコーダーで統一（Timeline制約により個別設定不可）
- タイムライン間で設定が干渉しない
- レコーダータイプ毎の専用設定UI
```

#### 2. 設定の全タイムライン反映機能（効率化）
```csharp
// "Apply to All Selected Timelines" 機能による設定処理時間短縮
public void ApplyRecorderToSelectedTimelines(int recorderIndex)
{
    var sourceConfig = GetTimelineRecorderConfig(currentTimelineIndexForRecorder);
    var sourceRecorder = sourceConfig.RecorderItems[recorderIndex];
    
    // 選択された全タイムラインに同じレコーダー設定を適用
    foreach (var timelineIndex in selectedDirectorIndices)
    {
        if (timelineIndex != currentTimelineIndexForRecorder)
        {
            var targetConfig = GetTimelineRecorderConfig(timelineIndex);
            
            // 同じRecorder Nameが存在するかチェック
            var existingRecorderIndex = targetConfig.RecorderItems.FindIndex(r => r.name == sourceRecorder.name);
            
            if (existingRecorderIndex >= 0)
            {
                // 同じ名前のレコーダーが存在する場合は上書き
                targetConfig.RecorderItems[existingRecorderIndex] = sourceRecorder.DeepCopy();
                MultiTimelineRecorderLogger.Log($"[ApplyRecorderToSelectedTimelines] Overwritten existing recorder '{sourceRecorder.name}' in timeline {timelineIndex}");
            }
            else
            {
                // 存在しない場合は新規追加
                var duplicatedRecorder = sourceRecorder.DeepCopy();
                targetConfig.RecorderItems.Add(duplicatedRecorder);
                MultiTimelineRecorderLogger.Log($"[ApplyRecorderToSelectedTimelines] Added new recorder '{sourceRecorder.name}' to timeline {timelineIndex}");
            }
        }
    }
}

// 設定反映の詳細動作
- 同じRecorder Name: 既存設定を上書き（設定の統一）
- 異なるRecorder Name: 新規レコーダーとして追加
- 大量のタイムラインに同じ設定を適用する際の時間短縮
- 右クリックメニューから一括適用
- 複数タイムライン選択時のみ表示
- 設定作業の大幅な効率化
```

#### 3. グローバル設定の継承機能
```csharp
// 3段階の設定継承システム
public enum RecorderPathMode
{
    UseGlobal,          // グローバル設定をそのまま使用
    RelativeToGlobal,   // グローバル設定を基準とした相対パス
    Custom              // 完全にカスタム設定
}

// グローバル設定からの柔軟な継承
- UseGlobal: 全てのパス設定をグローバルから継承
- RelativeToGlobal: グローバルパスに相対パスを追加
- Custom: 完全に独立したカスタム設定

// 設定の一貫性と柔軟性の両立
- デフォルトはグローバル設定継承で一貫性確保
- 必要に応じて個別カスタマイズ可能
- プロジェクト全体の設定管理が容易

// フレームレートの統一管理
- フレームレート: Timeline使用時は全レコーダーで統一（Timeline制約）
- 解像度: レコーダー毎に個別設定可能
- 出力形式: レコーダー毎に個別設定可能
```

### 継承すべきUI要素

1. **3カラムレイアウト構成**
   - 左：Timeline選択、中央：Recorder一覧、右：Recorder詳細設定
   - ドラッグ可能なスプリッター（最小/最大幅制限付き）
   - 各カラムの独立スクロール

2. **統一されたUIスタイル**
   - Unity標準エディタスタイル準拠
   - Pro/Light Skin対応
   - 一貫したカラーパレット（選択色、ホバー色、背景色）
   - ゼブラストライプ表示

3. **直感的な操作**
   - クリッカブルヘッダー（プラスアイコン付き）
   - チェックボックスによる選択
   - 右クリックコンテキストメニュー
   - ドラッグ&ドロップ対応（並び替え）

4. **情報の可視性**
   - アイコンによる視覚的識別
   - リアルタイムプレビュー表示
   - 状態の明確な表示（選択、有効/無効、エラー）
   - ツールチップによる詳細情報

## GameObject Reference Management

### シーン内オブジェクトとのバインディング保持戦略

新システムでは、シーン内のGameObjectとの参照を安全に保持し、シーン再読み込みやプロジェクト再起動後も正確に復元する機能を実装します：

```csharp
// GameObject参照管理サービス
public class GameObjectReferenceService
{
    public GameObjectReference CreateReference(GameObject gameObject)
    {
        if (gameObject == null) return null;
        
        return new GameObjectReference
        {
            GameObjectName = gameObject.name,
            ScenePath = gameObject.scene.path,
            HierarchyPath = GetHierarchyPath(gameObject.transform),
            InstanceId = gameObject.GetInstanceID()
        };
    }
    
    public GameObject ResolveReference(GameObjectReference reference)
    {
        if (reference == null) return null;
        
        // 1. InstanceIdによる高速検索（同一セッション内）
        var obj = EditorUtility.InstanceIDToObject(reference.InstanceId) as GameObject;
        if (obj != null && obj.scene.path == reference.ScenePath)
        {
            return obj;
        }
        
        // 2. 階層パスによる検索（より確実）
        obj = FindGameObjectByHierarchyPath(reference.HierarchyPath, reference.ScenePath);
        if (obj != null)
        {
            return obj;
        }
        
        // 3. 名前による検索（フォールバック）
        return FindGameObjectByName(reference.GameObjectName, reference.ScenePath);
    }
    
    private string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        var parent = transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}

// 参照復元の自動化
public class ReferenceRestorationService
{
    public void RestoreAllReferences(RecordingConfiguration config)
    {
        foreach (var timelineConfig in config.TimelineConfigs)
        {
            foreach (var recorderConfig in timelineConfig.RecorderConfigs)
            {
                recorderConfig.RestoreGameObjectReferences();
            }
        }
    }
    
    // シーン変更時の自動復元
    public void OnSceneChanged()
    {
        var currentConfig = ConfigurationService.LoadConfiguration();
        RestoreAllReferences(currentConfig);
    }
}
```

### 参照保持の対象オブジェクト

```csharp
// 各レコーダータイプで保持すべきGameObject参照
public abstract class RecorderConfigurationBase : IRecorderConfiguration
{
    // 共通のGameObject参照
    public List<GameObjectReference> ReferencedGameObjects { get; set; } = new List<GameObjectReference>();
    
    // 参照復元の抽象メソッド
    public abstract void RestoreGameObjectReferences();
    
    // 参照の保存
    protected void SaveGameObjectReference(GameObject gameObject, string referenceKey)
    {
        var reference = GameObjectReferenceService.CreateReference(gameObject);
        if (reference != null)
        {
            ReferencedGameObjects.RemoveAll(r => r.ReferenceKey == referenceKey);
            reference.ReferenceKey = referenceKey;
            ReferencedGameObjects.Add(reference);
        }
    }
}

// 具体例：Animation Recorderの場合
public class AnimationRecorderConfiguration : RecorderConfigurationBase
{
    public GameObjectReference TargetGameObjectReference { get; set; }
    public GameObjectReference RootGameObjectReference { get; set; }
    
    [NonSerialized]
    public GameObject TargetGameObject;
    [NonSerialized]
    public GameObject RootGameObject;
    
    public override void RestoreGameObjectReferences()
    {
        var referenceService = new GameObjectReferenceService();
        
        TargetGameObject = referenceService.ResolveReference(TargetGameObjectReference);
        RootGameObject = referenceService.ResolveReference(RootGameObjectReference);
        
        // 復元失敗時の警告
        if (TargetGameObjectReference != null && TargetGameObject == null)
        {
            MultiTimelineRecorderLogger.LogWarning($"Failed to restore target GameObject reference: {TargetGameObjectReference.GameObjectName}");
        }
    }
}
```

## UI/UX Design Improvements

### 改善されたUI設計原則

1. **情報階層の明確化**: 重要な情報を視覚的に強調
2. **操作フローの最適化**: 直感的な操作順序
3. **エラー表示の改善**: 分かりやすいエラーメッセージと解決策の提示
4. **レスポンシブデザイン**: 異なる画面サイズへの対応

### UI継承と改善戦略

```csharp
// UI要素の継承管理
public class UIInheritanceManager
{
    public List<UIElement> GetEffectiveUIElements()
    {
        // 旧システムから継承すべきUI要素を特定
        var legacyElements = IdentifyUsefulLegacyElements();
        var improvedElements = ApplyUIImprovements(legacyElements);
        return improvedElements;
    }
    
    private List<UIElement> IdentifyUsefulLegacyElements()
    {
        // 旧システムの有効なUI要素を特定
        // - タイムライン選択UI
        // - レコーダー設定パネル
        // - 進捗表示
        // - ファイル出力設定
    }
}

// 改善されたレイアウト管理
public class ImprovedLayoutManager
{
    public void ApplyLogicalGrouping(UIContainer container)
    {
        // 関連する機能をグループ化
        var timelineGroup = CreateTimelineSelectionGroup();
        var recorderGroup = CreateRecorderConfigurationGroup();
        var outputGroup = CreateOutputSettingsGroup();
        var controlGroup = CreateRecordingControlGroup();
        
        container.AddGroups(timelineGroup, recorderGroup, outputGroup, controlGroup);
    }
    
    public void OptimizeInformationHierarchy(UIContainer container)
    {
        // 情報の重要度に基づいた視覚的階層を構築
        container.SetPrimaryActions(new[] { "Start Recording", "Stop Recording" });
        container.SetSecondaryActions(new[] { "Add Timeline", "Configure Recorder" });
        container.SetTertiaryActions(new[] { "Advanced Settings", "Debug Options" });
    }
}
```

### エラー表示の改善

```csharp
// 改善されたエラー表示システム
public class ImprovedErrorDisplayService
{
    public void DisplayError(RecordingError error, UIContext context)
    {
        var errorPanel = new ErrorPanel
        {
            Title = GetUserFriendlyTitle(error.ErrorCode),
            Message = GetUserFriendlyMessage(error.Message),
            SuggestedActions = GetSuggestedActions(error),
            Severity = GetSeverityLevel(error)
        };
        
        // エラーの重要度に応じた表示方法
        switch (errorPanel.Severity)
        {
            case ErrorSeverity.Critical:
                ShowModalDialog(errorPanel);
                break;
            case ErrorSeverity.Warning:
                ShowInlineWarning(errorPanel, context);
                break;
            case ErrorSeverity.Info:
                ShowStatusMessage(errorPanel);
                break;
        }
    }
    
    private List<string> GetSuggestedActions(RecordingError error)
    {
        // エラーに対する具体的な解決策を提示
        return error.ErrorCode switch
        {
            "TIMELINE_NOT_FOUND" => new[] { "タイムラインアセットを選択してください", "プロジェクト内でタイムラインを検索" },
            "INVALID_OUTPUT_PATH" => new[] { "有効な出力パスを指定してください", "デフォルトパスを使用" },
            "RECORDER_CONFIG_ERROR" => new[] { "レコーダー設定を確認してください", "デフォルト設定にリセット" },
            _ => new[] { "詳細なログを確認してください" }
        };
    }
}
```

この設計により、現在のモノリシックな構造を段階的にリファクタリングし、保守性、拡張性、テスト可能性を大幅に向上させることができます。また、旧システムとの互換性を保ちながら、UI/UXの大幅な改善と将来的なAPI化への道筋も明確になります。