# Unity Test Runner テスト環境構築ガイド

## 概要
Batch Rendering ToolプロジェクトのUnity Test Runner環境が構築されました。

## フォルダ構造

```
Assets/BatchRenderingTool/
├── Scripts/
│   ├── BatchRenderingTool.Runtime.asmdef (ランタイムコード用)
│   └── Editor/
│       ├── BatchRenderingTool.Editor.asmdef (エディターコード用)
│       └── Tests/
│           ├── BatchRenderingTool.Editor.Tests.asmdef (エディターテスト用)
│           ├── SingleTimelineRendererTests.cs
│           ├── RecorderSettingsFactoryTests.cs
│           └── TestHelpers.cs
└── Tests/
    └── Runtime/
        ├── BatchRenderingTool.Runtime.Tests.asmdef (プレイモードテスト用)
        └── TimelinePlaybackTests.cs
```

## テストの実行方法

### 1. Unity Test Runnerウィンドウから実行
- メニュー: Window → General → Test Runner
- EditModeタブとPlayModeタブでそれぞれのテストを確認・実行

### 2. Batch Rendering Toolのカスタムテストランナーから実行
- メニュー: Window → Batch Rendering Tool → Test Runner
- GUI上で実行したいテストタイプを選択して実行

### 3. メニューから直接実行
- Window → Batch Rendering Tool → Run All Tests (全テスト実行)
- Window → Batch Rendering Tool → Run Editor Tests Only (エディターテストのみ)
- Window → Batch Rendering Tool → Run Playmode Tests Only (プレイモードテストのみ)

## アセンブリ定義ファイル構成

1. **BatchRenderingTool.Runtime.asmdef**
   - ランタイムコード用のメインアセンブリ
   - Timeline、Recorder等の必要な参照を含む

2. **BatchRenderingTool.Editor.asmdef**
   - エディターコード用のアセンブリ
   - Editor専用の機能とランタイムアセンブリへの参照を含む

3. **BatchRenderingTool.Editor.Tests.asmdef**
   - エディターテスト用のアセンブリ
   - NUnit framework、TestRunner、必要なアセンブリへの参照を含む

4. **BatchRenderingTool.Runtime.Tests.asmdef**
   - プレイモードテスト用のアセンブリ
   - ランタイムでのテストに必要な参照を含む

## テスト作成のガイドライン

### Editorテスト
```csharp
using NUnit.Framework;
using BatchRenderingTool.Editor;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class MyEditorTests
    {
        [Test]
        public void MyTest()
        {
            // テストコード
            Assert.IsTrue(true);
        }
    }
}
```

### Playmodeテスト
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BatchRenderingTool.Runtime.Tests
{
    [TestFixture]
    public class MyPlaymodeTests
    {
        [UnityTest]
        public IEnumerator MyAsyncTest()
        {
            // 非同期テストコード
            yield return null;
            Assert.IsTrue(true);
        }
    }
}
```

## テストヘルパー
`TestHelpers.cs`には、テスト用の便利なメソッドが含まれています：
- CreateTestTimeline() - テスト用TimelineAsset作成
- CreateTestDirectorGameObject() - テスト用PlayableDirector作成
- CreateTestRecorderSettings() - テスト用RecorderSettings作成
- CreateTestOutputFolder() / CleanupTestOutputFolder() - テスト用フォルダ管理

また、`BatchRenderingTestBase`クラスを継承することで、共通のセットアップ/クリーンアップ処理を利用できます。

## Debug.Logの活用
各テストメソッドには適切なDebug.Logが実装されており、テストの実行状況を確認できます。
これにより、テストが失敗した場合の原因究明が容易になります。