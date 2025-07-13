# Unity Multi Timeline Recorder

[English](README.md) | **日本語**

---

## 概要

Unity Multi Timeline Recorderは、複数のUnity Timelineアセットを様々な出力形式でバッチ録画するための包括的なツールです。このパッケージは、Unityの内蔵Recorder機能を拡張し、異なる設定で複数のタイムラインの同時録画をサポートします。

## 機能

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

## リポジトリ構造

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

## インストール

### Unity Package Manager経由でのパッケージインストール

1. Package Managerウィンドウを開く (Window > Package Manager)
2. 左上の+ボタンをクリック
3. "Add package from git URL..."を選択
4. 入力: `https://github.com/murasaqi/Unity_MultiTimelineRecorder.git?path=jp.iridescent.multitimelinerecorder`
5. Addをクリック

### デモプロジェクトのセットアップ

1. このリポジトリをクローン: 
   ```bash
   git clone https://github.com/murasaqi/Unity_MultiTimelineRecorder.git
   ```
2. Unity Hubを開く
3. "Add"をクリックして`Unity_MultiTimelineRecorder_Demo~`フォルダを選択
4. Unity 2021.3以降でプロジェクトを開く

## 必要要件

- Unity 2021.3以降
- Unity Timelineパッケージ (com.unity.timeline) 1.6.0+
- Unity Recorderパッケージ (com.unity.recorder) 4.0.0+
- Unity Editor Coroutinesパッケージ (com.unity.editorcoroutines) 1.0.0+

## クイックスタート

1. Multi Timeline Recorderウィンドウを開く: **Window > Multi Timeline Recorder**
2. "+"ボタンをクリックして録画するTimelineアセットを追加
3. 各タイムラインの録画設定を構成
4. **オプション**: 精密な録画範囲指定のためのSignal Emitterタイミング制御を有効化:
   - Signal Emitter Timingセクションの"Enable"をチェック
   - 開始・終了タイミング名を設定（例："pre"と"post"）
   - Timelineトラックに対応する名前のSignal Emitterを追加
   - Frame/秒数表示切り替えを使用してお好みの形式でタイミングを表示
5. "Start Recording"をクリックしてバッチプロセスを開始

## ライセンス

このプロジェクトはMITライセンスの下でライセンスされています - 詳細は[LICENSE](jp.iridescent.multitimelinerecorder/LICENSE)ファイルを参照してください。

## 作者

**Murasaqi**
- GitHub: [@murasaqi](https://github.com/murasaqi)

---

## サポート

- **Issues**: [GitHub Issues](https://github.com/murasaqi/Unity_MultiTimelineRecorder/issues)
- **Discussions**: [GitHub Discussions](https://github.com/murasaqi/Unity_MultiTimelineRecorder/discussions)

## コントリビューション

コントリビューションを歓迎します！お気軽にPull Requestを送信してください。