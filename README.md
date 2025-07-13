# Unity Multi Timeline Recorder

[English](#english) | [日本語](#japanese)

---

## English

### Overview

Unity Multi Timeline Recorder is a comprehensive tool for batch recording multiple Unity Timeline assets with various output formats. This package extends Unity's built-in Recorder functionality to support simultaneous recording of multiple timelines with different configurations.

### Features

- **Multi-Timeline Batch Recording**: Record multiple Unity Timeline assets in a single batch operation
- **Multiple Output Formats**: 
  - Movie (MP4, MOV, WebM)
  - Image Sequences (PNG, JPG, EXR)
  - Animation Clips
  - Alembic
  - FBX
  - AOV (Arbitrary Output Variables)
- **Signal Emitter Timing Control**: Use Timeline Signal Emitters to precisely control recording start and end times
- **Flexible Recording Settings**: Configure each recorder independently with specific settings
- **Path Management**: Advanced path management with wildcard support
- **Play Mode Support**: Record timelines in Play Mode with real-time monitoring
- **Progress Tracking**: Visual progress indicators for batch operations

### Repository Structure

```
Unity_MultiTimelineRecorder/
├── jp.iridescent.multitimelinerecorder/    # Unity Package
│   ├── Runtime/                             # Runtime scripts
│   ├── Editor/                              # Editor scripts
│   ├── Documentation~/                      # Documentation
│   ├── Samples~/                            # Sample assets
│   ├── package.json                         # Package manifest
│   ├── README.md                            # Package documentation
│   ├── LICENSE                              # MIT License
│   └── CHANGELOG.md                         # Version history
└── Unity_MultiTimelineRecorder_Demo~/       # Demo project
    ├── Assets/                              # Demo assets
    ├── ProjectSettings/                     # Unity project settings
    └── ...                                  # Other Unity project files
```

### Installation

#### Package Installation via Unity Package Manager

1. Open the Package Manager window (Window > Package Manager)
2. Click the + button in the top-left corner
3. Select "Add package from git URL..."
4. Enter: `https://github.com/murasaqi/Unity_MultiTimelineRecorder.git?path=jp.iridescent.multitimelinerecorder`
5. Click Add

#### Demo Project Setup

1. Clone this repository: 
   ```bash
   git clone https://github.com/murasaqi/Unity_MultiTimelineRecorder.git
   ```
2. Open Unity Hub
3. Click "Add" and select the `Unity_MultiTimelineRecorder_Demo~` folder
4. Open the project with Unity 2021.3 or later

### Requirements

- Unity 2021.3 or later
- Unity Timeline package (com.unity.timeline) 1.6.0+
- Unity Recorder package (com.unity.recorder) 4.0.0+
- Unity Editor Coroutines package (com.unity.editorcoroutines) 1.0.0+

### Quick Start

1. Open the Multi Timeline Recorder window: **Window > Multi Timeline Recorder**
2. Add Timeline assets to record by clicking the "+" button
3. Configure recording settings for each timeline
4. **Optional**: Enable Signal Emitter timing control for precise recording ranges:
   - Check "Enable" in the Signal Emitter Timing section
   - Set start and end timing names (e.g., "pre" and "post")
   - Add Signal Emitters to your Timeline tracks with matching names
   - Use the Frame/Seconds display toggle to view timing in your preferred format
5. Click "Start Recording" to begin the batch process

### License

This project is licensed under the MIT License - see the [LICENSE](jp.iridescent.multitimelinerecorder/LICENSE) file for details.

### Author

**Murasaqi**
- GitHub: [@murasaqi](https://github.com/murasaqi)

---

## Japanese

### 概要

Unity Multi Timeline Recorderは、複数のUnity Timelineアセットを様々な出力形式でバッチ録画するための包括的なツールです。このパッケージは、Unityの内蔵Recorder機能を拡張し、異なる設定で複数のタイムラインの同時録画をサポートします。

### 機能

- **マルチタイムライン バッチ録画**: 複数のUnity Timelineアセットを単一のバッチ操作で録画
- **複数の出力形式**: 
  - 動画 (MP4, MOV, WebM)
  - 画像シーケンス (PNG, JPG, EXR)
  - アニメーションクリップ
  - Alembic
  - FBX
  - AOV (Arbitrary Output Variables)
- **Signal Emitterタイミング制御**: Timeline Signal Emitterを使用して録画開始・終了時刻を精密に制御
- **柔軟な録画設定**: 各レコーダーを独立して特定の設定で構成
- **パス管理**: ワイルドカードサポートによる高度なパス管理
- **プレイモードサポート**: リアルタイム監視でのプレイモード録画
- **進捗追跡**: バッチ操作の視覚的進捗インジケーター

### リポジトリ構造

```
Unity_MultiTimelineRecorder/
├── jp.iridescent.multitimelinerecorder/    # Unityパッケージ
│   ├── Runtime/                             # ランタイムスクリプト
│   ├── Editor/                              # エディタースクリプト
│   ├── Documentation~/                      # ドキュメント
│   ├── Samples~/                            # サンプルアセット
│   ├── package.json                         # パッケージマニフェスト
│   ├── README.md                            # パッケージドキュメント
│   ├── LICENSE                              # MITライセンス
│   └── CHANGELOG.md                         # バージョン履歴
└── Unity_MultiTimelineRecorder_Demo~/       # デモプロジェクト
    ├── Assets/                              # デモアセット
    ├── ProjectSettings/                     # Unityプロジェクト設定
    └── ...                                  # その他のUnityプロジェクトファイル
```

### インストール

#### Unity Package Manager経由でのパッケージインストール

1. Package Managerウィンドウを開く (Window > Package Manager)
2. 左上の+ボタンをクリック
3. "Add package from git URL..."を選択
4. 入力: `https://github.com/murasaqi/Unity_MultiTimelineRecorder.git?path=jp.iridescent.multitimelinerecorder`
5. Addをクリック

#### デモプロジェクトのセットアップ

1. このリポジトリをクローン: 
   ```bash
   git clone https://github.com/murasaqi/Unity_MultiTimelineRecorder.git
   ```
2. Unity Hubを開く
3. "Add"をクリックして`Unity_MultiTimelineRecorder_Demo~`フォルダを選択
4. Unity 2021.3以降でプロジェクトを開く

### 必要要件

- Unity 2021.3以降
- Unity Timelineパッケージ (com.unity.timeline) 1.6.0+
- Unity Recorderパッケージ (com.unity.recorder) 4.0.0+
- Unity Editor Coroutinesパッケージ (com.unity.editorcoroutines) 1.0.0+

### クイックスタート

1. Multi Timeline Recorderウィンドウを開く: **Window > Multi Timeline Recorder**
2. "+"ボタンをクリックして録画するTimelineアセットを追加
3. 各タイムラインの録画設定を構成
4. **オプション**: 精密な録画範囲指定のためのSignal Emitterタイミング制御を有効化:
   - Signal Emitter Timingセクションの"Enable"をチェック
   - 開始・終了タイミング名を設定（例："pre"と"post"）
   - Timelineトラックに対応する名前のSignal Emitterを追加
   - Frame/秒数表示切り替えを使用してお好みの形式でタイミングを表示
5. "Start Recording"をクリックしてバッチプロセスを開始

### ライセンス

このプロジェクトはMITライセンスの下でライセンスされています - 詳細は[LICENSE](jp.iridescent.multitimelinerecorder/LICENSE)ファイルを参照してください。

### 作者

**Murasaqi**
- GitHub: [@murasaqi](https://github.com/murasaqi)

---

## Support

- **Issues**: [GitHub Issues](https://github.com/murasaqi/Unity_MultiTimelineRecorder/issues)
- **Discussions**: [GitHub Discussions](https://github.com/murasaqi/Unity_MultiTimelineRecorder/discussions)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.