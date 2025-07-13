# リファクタリング計画: Unity Multi Timeline Recorder

## 1. 目的

このリファクタリングの目的は、`jp.iridescent.multitimelinerecorder` パッケージのコードベースを、**保守性**、**拡張性**、**堅牢性**の高いモダンなアーキテクチャに刷新することです。これにより、将来的な機能追加（例: 新しいレコーダーの種類、クラウド連携）や、Unityバージョンの更新への追従が容易になります。

## 2. 現状の課題

`MultiTimelineRecorder.cs` (3500行以上) という単一の巨大クラスに、以下の責務が集中・密結合していることが主な課題です。

*   **UI描画ロジック:** `OnGUI` で描画されるすべての要素（リスト、ボタン、設定項目）。
*   **状態管理:** `RecordState` enumによる複雑な状態遷移。
*   **ビジネスロジック:** タイムラインの合成、レコーダーの設定、記録プロセスの制御。
*   **データ永続化:** `EditorPrefs` や `ScriptableObject` を使った設定の保存・読み込み。
*   **イベントハンドリング:** `EditorApplication` の各種イベントへの応答。

この「巨大なクラス（God Class）」問題は、コードの可読性を著しく下げ、修正影響範囲の特定を困難にし、バグの温床となります。

## 3. リファクタリング戦略: 責務の分離と依存関係の整理

戦略の核心は、**関心の分離 (Separation of Concerns)** と **依存関係逆転の原則 (Dependency Inversion Principle)** です。責務ごとにクラスを分割し、クラス間の通信は具象クラスへの直接依存ではなく、インターフェースやイベントを介して行います。

### フェーズ 1: アーキテクチャの再設計とクラス分割

巨大な `MultiTimelineRecorder.cs` を、責務に基づいた複数の小さなクラスへ分割します。

#### 3.1. UI (View) の分離

`EditorWindow` の描画ロジックを、関心事ごとに独立したUIコンポーネント（View）クラスに分割します。

*   **`MultiTimelineRecorderWindow.cs`**:
    *   責務: `EditorWindow` の生存管理、各Viewのインスタンス化とレイアウト。
    *   `OnGUI` では各Viewの描画メソッドを呼び出すだけにします。
*   **`TimelineSelectionView.cs`**:
    *   責務: 左カラムの「タイムラインリスト」の描画とユーザー操作（追加、削除、選択）の受付。
*   **`RecorderListView.cs`**:
    *   責務: 中央カラムの「レコーダーリスト」の描画とユーザー操作（追加、削除、選択、有効化/無効化）の受付。
*   **`RecorderDetailView.cs`**:
    *   責務: 右カラムの「レコーダー設定詳細」の描画。内部で `RecorderSettingsEditorBase` の派生クラスを呼び出します。
*   **`GlobalSettingsView.cs`**:
    *   責務: 解像度やフレームレートなどのグローバル設定UIの描画。

#### 3.2. 制御 (Controller) の分離

記録プロセス全体のフロー制御と状態管理を専門のコントローラークラスに分離します。

*   **`RecordingController.cs`**:
    *   責務: `RecordState` の管理、記録開始/停止のトリガー、`TimelineAsset` の生成・加工、`PlayModeTimelineRenderer` へのデータ受け渡しなど、中核となるビジネスロジック全体を管理します。
    *   `EditorApplication.playModeStateChanged` などのUnityエディタのイベントを購読し、記録プロセスを制御します。
*   **`PlayModeStateBridge.cs`**:
    *   責務: `EditorPrefs` を利用した再生モードをまたぐ状態（「現在記録中か」など）の保存・復元処理をカプセル化します。これにより、危険な `EditorPrefs` への直接アクセスを他クラスから隠蔽します。

#### 3.3. データ (Model) の整理

データ構造と永続化ロジックを明確に分離します。

*   **`MultiTimelineRecorderSettings.cs` (`ScriptableObject`)**:
    *   責務: プロジェクトで唯一の永続化される設定アセット。グローバル設定、タイムラインごとのレコーダー設定、UIの状態（カラム幅など）をすべて保持します。
    *   `MultiTimelineRecorderWindow` 内で直接 `Dictionary` を持つのではなく、すべての設定データはこの `ScriptableObject` を通じて読み書きします。
*   **`RecorderConfig` 派生クラス群**:
    *   現状維持。各レコーダーの設定を保持するデータコンテナとしての責務に専念させます。

### フェーズ 2: クラス間連携の再設計

分割したクラス間の結合を疎にするため、通信方法をイベントベースに移行します。

1.  **UI → Controller (イベント通知)**:
    *   Viewクラス（UI）はユーザー操作を受け付けると、`Action` や `event` を使って「イベント」を発行します。
    *   例: `TimelineSelectionView` がタイムライン選択の変更を検知すると `OnTimelineSelected(PlayableDirector director)` イベントを発行します。
    *   `RecordingController` はこれらのイベントを購読し、適切な処理を実行します。
2.  **Controller → UI (データバインディング)**:
    *   `RecordingController` や `MultiTimelineRecorderSettings` が管理する状態やデータが変更された場合、それらもイベントを発行します。
    *   例: `RecordingController` の状態が `Recording` に変わると `OnStateChanged(RecordState newState)` イベントを発行します。
    *   各Viewクラスはこれらのイベントを購読し、自身の表示を更新します（例: ボタンを無効化する、ステータスメッセージを変更する）。

この設計により、UIはビジネスロジックを知る必要がなく、ビジネスロジックもUIの実装に依存しなくなります。

### フェーズ 3: アセンブリの再定義

コードのモジュール性を高め、再利用性とコンパイル速度を向上させます。

*   **`Unity.MultiTimelineRecorder.Runtime`**:
    *   内容: `PlayModeTimelineRenderer.cs` など、再生モードで動作するコンポーネント。
    *   依存: なし（またはUnityのコアモジュールのみ）。
*   **`Unity.MultiTimelineRecorder.Editor.Core`**:
    *   内容: `MultiTimelineRecorderSettings.cs`、`RecorderConfig` 派生クラスなど、エディタ時のみ必要だがUIに直接依存しないデータ構造。
    *   依存: `...Runtime`。
*   **`Unity.MultiTimelineRecorder.Editor`**:
    *   内容: `MultiTimelineRecorderWindow.cs`、すべてのViewクラス、`RecordingController`、`RecorderSettingsEditorBase` 派生クラスなど、UIとエディタロジックのすべて。
    *   依存: `...Editor.Core`, `...Runtime`。

この構成により、依存関係が `Editor` -> `Editor.Core` -> `Runtime` という一方向になり、クリーンなアーキテクチャが実現します。

## 4. 実行計画（ステップ・バイ・ステップ）

1.  **準備:**
    *   Unity Test Framework を導入し、既存の記録機能（特に各レコーダーの出力）に対する基本的な統合テストを作成します。これにより、リファクタリングによるデグレードを防止します。
2.  **フェーズ1 (クラス分割) の実施:**
    *   提案されたクラス (`TimelineSelectionView.cs` など) の空ファイルを作成します。
    *   `MultiTimelineRecorder.cs` から、関連するコード（メソッド、変数）を新しく作成した各クラスに少しずつ移動させます。まずは単純なUI描画コードの移動から始めます。
    *   `MultiTimelineRecorderSettings.cs` を中心的なデータストアとして再定義し、設定の読み書きがすべてこのクラスを経由するように修正します。
3.  **フェーズ2 (イベント化) の実施:**
    *   クラス間の直接メソッド呼び出しを、`Action`/`event` を使った通知に置き換えていきます。
4.  **フェーズ3 (アセンブリ分割) の実施:**
    *   新しいフォルダ構造を作成し、ファイルを移動させ、`asmdef` ファイルを3つ作成・設定します。
5.  **クリーンアップとテスト:**
    *   元の巨大な `MultiTimelineRecorder.cs` が、責務がほとんどなくなったことを確認し、不要なコードを削除します。
    *   最初に作成したテストがすべてパスすることを確認します。手動テストも実施し、すべての機能が以前と同様に動作することを保証します。
