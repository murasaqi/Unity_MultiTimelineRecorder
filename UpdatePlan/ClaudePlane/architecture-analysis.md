# Unity Multi Timeline Recorder アーキテクチャ分析

## 📁 プロジェクト構造

```
Unity_MultiTimelineRecorder/
└── jp.iridescent.multitimelinerecorder/    # Unity Package
    ├── Runtime/                             # Runtime scripts (3 files)
    │   ├── PlayModeTimelineRenderer.cs
    │   ├── RecorderSettingsType.cs
    │   └── RenderingData.cs
    ├── Editor/                              # Editor scripts
    │   ├── MultiTimelineRecorder.cs         # メインエディタウィンドウ
    │   ├── RecorderConfigs/                 # 設定クラス (8 files)
    │   ├── RecorderEditors/                 # エディタUI (7 files)
    │   ├── Settings/                        # 設定管理 (1 file)
    │   ├── Utilities/                       # ユーティリティ (6 files)
    │   └── Interfaces/                      # インターフェース (1 file)
    ├── Documentation~/                      # Documentation
    ├── Samples~/                            # Sample assets
    ├── package.json                         # Package manifest
    ├── README.md                            # Package documentation
    ├── LICENSE                              # MIT License
    └── CHANGELOG.md                         # Version history
```

## 🔍 現在のアーキテクチャ分析

### **パッケージ基本情報**
- **名前**: `jp.iridescent.multitimelinerecorder`
- **バージョン**: `1.0.0`
- **Unity要件**: `2021.3+`
- **主要依存関係**: 
  - `com.unity.recorder` (5.1.2)
  - `com.unity.timeline` (1.6.0)
  - `com.unity.editorcoroutines` (1.0.0)

### **主要コンポーネント**

#### 1. **コアエディタ** (`MultiTimelineRecorder.cs`)
- **役割**: メインのエディタウィンドウ
- **問題点**: 単一ファイルに多数の責任が集中
- **行数**: 約1000行以上と推定
- **責任**: UI描画、設定管理、録画制御、イベント処理

#### 2. **設定システム** (`RecorderConfigs/`)
```
RecorderConfigs/
├── RecorderConfig.cs              # 基底設定クラス
├── MultiRecorderConfig.cs         # 複数レコーダー管理
├── ImageRecorderSettingsConfig.cs # 画像設定
├── MovieRecorderSettingsConfig.cs # 動画設定
├── FBXRecorderSettingsConfig.cs   # FBX設定
├── AlembicRecorderSettingsConfig.cs # Alembic設定
├── AnimationRecorderSettingsConfig.cs # アニメーション設定
└── AOVRecorderSettingsConfig.cs   # AOV設定
```

#### 3. **エディタUIシステム** (`RecorderEditors/`)
```
RecorderEditors/
├── RecorderSettingsEditorBase.cs  # 基底エディタクラス
├── ImageRecorderEditor.cs         # 画像エディタ
├── MovieRecorderEditor.cs         # 動画エディタ
├── FBXRecorderEditor.cs           # FBXエディタ
├── AlembicRecorderEditor.cs       # Alembicエディタ
├── AnimationRecorderEditor.cs     # アニメーションエディタ
└── AOVRecorderEditor.cs           # AOVエディタ
```

#### 4. **ユーティリティシステム** (`Utilities/`)
```
Utilities/
├── PathUtility.cs                 # パス処理
├── OutputPathManager.cs           # 出力パス管理
├── OutputPathSettings.cs          # パス設定
├── RecorderClipUtility.cs         # クリップユーティリティ
├── SignalEmitterRecordControl.cs  # シグナル制御
└── WildcardProcessor.cs           # ワイルドカード処理
```

#### 5. **ランタイムコンポーネント** (`Runtime/`)
```
Runtime/
├── RecorderSettingsType.cs       # レコーダータイプ列挙
├── RenderingData.cs               # レンダリングデータ
└── PlayModeTimelineRenderer.cs    # プレイモードレンダラー
```

## 🔧 アーキテクチャパターンの分析

### **現在の設計パターン**

#### 1. **Factory Pattern** (部分的)
- `RecorderSettingsFactory.cs`でレコーダー設定を生成
- 各設定タイプに対応した生成メソッド

#### 2. **Strategy Pattern** (部分的)
- 各レコーダータイプで異なる録画戦略を実装
- `RecorderSettingsEditorBase`を基底とした継承構造

#### 3. **Observer Pattern** (限定的)
- Unity標準のエディタイベントに依存
- カスタムイベントシステムは未実装

### **コードの結合度分析**

#### **高結合度の領域**
1. **UI層とビジネスロジック層**
   - `MultiTimelineRecorder.cs`内でUI描画と録画制御が混在
   - 設定変更時の即座なUI更新ロジック

2. **設定クラス間の依存関係**
   - `MultiRecorderConfig`が各設定クラスを直接参照
   - 型安全性は確保されているが拡張性に課題

3. **エディタとコンフィグの相互依存**
   - エディタクラスが特定のコンフィグ型に強く依存
   - 新しいレコーダータイプ追加時に複数箇所の変更が必要

### **拡張性の評価**

#### **良い点**
- ✅ 各レコーダータイプが独立したクラス構造
- ✅ 基底クラスによる共通機能の実装
- ✅ ユーティリティクラスの適切な分離

#### **改善点**
- ❌ ハードコードされた`RecorderSettingsType`enum
- ❌ 新レコーダータイプ追加時の影響範囲が広い
- ❌ プラグインシステムの欠如
- ❌ 依存性注入の未活用

## 📊 コード品質指標

### **推定メトリクス**
- **総ファイル数**: ~30ファイル
- **総行数**: ~5,000-7,000行
- **循環複雑度**: 中〜高（特にメインエディタ）
- **結合度**: 高
- **凝集度**: 中

### **テスト可能性**
- **単体テスト**: 困難（密結合のため）
- **統合テスト**: 可能（Unity Test Framework使用）
- **モックの利用**: 限定的

## 🎯 スケーラビリティの課題

### **水平スケーリング**
- 新しいレコーダータイプの追加コスト: **高**
- サードパーティ拡張: **困難**
- プラグインエコシステム: **未対応**

### **垂直スケーリング**
- 機能追加時の影響範囲: **広範囲**
- 保守性: **中**
- リファクタリング容易性: **低**

## 💡 改善の方向性

### **短期的改善**
1. 責任の分離（UI/ビジネスロジック）
2. インターフェースベースの設計導入
3. 依存性注入の部分的導入

### **中期的改善**
1. プラグインアーキテクチャの導入
2. イベント駆動アーキテクチャの実装
3. 設定システムの汎用化

### **長期的改善**
1. マイクロサービス的な分離
2. 拡張可能なAPIの提供
3. サードパーティエコシステムの構築

この分析を基に、次のフェーズでは具体的なリファクタリング計画を策定します。