# Unity Timeline Batch Rendering Tool

複数のTimelineを一括でレンダリングするUnityエディタ拡張ツールです。

## 機能

- シーン内の全PlayableDirector（Timeline）を自動検出
- 各Timelineを順番に自動レンダリング
- 出力形式の柔軟な設定（PNG/JPG/EXR、解像度、FPS）
- Timeline名に基づいた自動ファイル命名

## 使い方

1. **ツールを開く**
   - メニューから `Window > Batch Rendering Tool` を選択
   - デバッグ用: `Window > Batch Rendering Tool > Debug Window` を選択

2. **Timelineの準備**
   - シーンにPlayableDirectorコンポーネントを持つGameObjectを配置
   - 各PlayableDirectorにTimelineアセットを割り当て
   - GameObject名がレンダリング時のファイル名になります（例: Cut1, Cut2, Cut3）

3. **レンダリング設定**
   - Frame Rate: 24/30/60 FPSから選択
   - Resolution: Full HD/4K/カスタム解像度
   - Output Format: PNG/JPG/EXR連番
   - Output Path: 出力先フォルダを指定

4. **レンダリング実行**
   - "Start Batch Rendering"ボタンをクリック
   - 各Timelineが順番にレンダリングされます
   - 進捗状況がリアルタイムで表示されます

## 出力ファイル構造

```
指定した出力パス/
├── Cut1/
│   ├── Cut1_0001.exr
│   ├── Cut1_0002.exr
│   └── ...
├── Cut2/
│   ├── Cut2_0001.exr
│   ├── Cut2_0002.exr
│   └── ...
└── Cut3/
    ├── Cut3_0001.exr
    ├── Cut3_0002.exr
    └── ...
```

## 技術仕様

- **RecorderTrackを使用した高精度レンダリング**
  - 動的にレンダリング用Timelineを生成
  - ControlTrackで元のTimelineを制御
  - RecorderTrackで録画を管理
- Unity Recorder APIのTimeline統合機能を活用
- EditorCoroutineによる非同期処理
- PlayableDirector APIによるTimeline制御

## トラブルシューティング

- **Timelineが検出されない場合**
  - PlayableDirectorコンポーネントが存在するか確認
  - Timelineアセットが正しく割り当てられているか確認
  - "Refresh"ボタンで再スキャン

- **レンダリングが開始されない場合**
  - Unity RecorderとEditor Coroutinesパッケージがインストールされているか確認
  - Package Managerから必要なパッケージをインストール