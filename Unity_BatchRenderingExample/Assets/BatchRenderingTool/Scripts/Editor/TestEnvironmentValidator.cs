using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace BatchRenderingTool.Editor
{
    /// <summary>
    /// テスト環境の検証とデバッグ情報を提供
    /// </summary>
    public static class TestEnvironmentValidator
    {
        [MenuItem("Window/Batch Rendering Tool/Validate Test Environment")]
        public static void ValidateTestEnvironment()
        {
            Debug.Log("=== Batch Rendering Tool テスト環境検証 ===");
            
            bool allValid = true;
            
            // 1. コンパイルエラーのチェック
            Debug.Log("\n[1] コンパイルエラーチェック:");
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("✗ コンパイルエラーが存在します");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ コンパイルエラーなし");
            }
            
            // 2. テストアセンブリの存在確認
            Debug.Log("\n[2] テストアセンブリ確認:");
            string[] testAssemblyNames = {
                "BatchRenderingTool.Editor.Tests",
                "BatchRenderingTool.Runtime.Tests"
            };
            
            foreach (var assemblyName in testAssemblyNames)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    var testFixtures = assembly.GetTypes()
                        .Where(t => t.GetCustomAttributes(typeof(NUnit.Framework.TestFixtureAttribute), false).Length > 0)
                        .ToList();
                    
                    var testMethods = testFixtures
                        .SelectMany(t => t.GetMethods())
                        .Where(m => m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false).Length > 0)
                        .Count();
                    
                    Debug.Log($"✓ {assemblyName}: {testFixtures.Count} テストクラス, {testMethods} テストメソッド");
                    
                    // テストクラスの詳細を表示
                    foreach (var fixture in testFixtures)
                    {
                        var methods = fixture.GetMethods()
                            .Where(m => m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false).Length > 0)
                            .ToList();
                        Debug.Log($"  - {fixture.Name}: {methods.Count} テスト");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"△ {assemblyName}: {e.Message}");
                }
            }
            
            // 3. 依存アセンブリの確認
            Debug.Log("\n[3] 依存アセンブリ確認:");
            string[] requiredAssemblies = {
                "BatchRenderingTool.Runtime",
                "BatchRenderingTool.Editor",
                "Unity.Timeline",
                "Unity.Timeline.Editor",
                "Unity.Recorder.Editor",
                "Unity.EditorCoroutines.Editor",
                "nunit.framework"
            };
            
            foreach (var assemblyName in requiredAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    Debug.Log($"✓ {assemblyName} ロード成功");
                }
                catch
                {
                    Debug.LogError($"✗ {assemblyName} ロード失敗");
                    allValid = false;
                }
            }
            
            // 4. Unity Test Runnerの状態確認
            Debug.Log("\n[4] Unity Test Runner状態:");
            var testRunnerWindow = Resources.FindObjectsOfTypeAll(System.Type.GetType("UnityEditor.TestTools.TestRunner.TestRunnerWindow, UnityEditor.TestRunner")).FirstOrDefault();
            if (testRunnerWindow != null)
            {
                Debug.Log("✓ Test Runnerウィンドウが利用可能");
            }
            else
            {
                Debug.Log("△ Test Runnerウィンドウが開かれていません (Window > General > Test Runner)");
            }
            
            // 5. テスト用GameObjectの作成可能性確認
            Debug.Log("\n[5] テスト環境動作確認:");
            try
            {
                var testGO = new GameObject("_TestEnvironmentCheck");
                var director = testGO.AddComponent<UnityEngine.Playables.PlayableDirector>();
                var timeline = ScriptableObject.CreateInstance<UnityEngine.Timeline.TimelineAsset>();
                director.playableAsset = timeline;
                
                Debug.Log("✓ PlayableDirector と TimelineAsset の作成成功");
                
                // クリーンアップ
                Object.DestroyImmediate(testGO);
                Object.DestroyImmediate(timeline);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ テスト環境動作確認失敗: {e.Message}");
                allValid = false;
            }
            
            // 6. RecorderSettings の作成確認
            Debug.Log("\n[6] RecorderSettings作成確認:");
            try
            {
                var imageSettings = RecorderSettingsFactory.CreateImageRecorderSettings("TestRecorder");
                if (imageSettings != null)
                {
                    Debug.Log("✓ ImageRecorderSettings 作成成功");
                    Object.DestroyImmediate(imageSettings);
                }
                else
                {
                    Debug.LogError("✗ ImageRecorderSettings 作成失敗");
                    allValid = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ RecorderSettings作成エラー: {e.Message}");
                allValid = false;
            }
            
            // 結果サマリー
            Debug.Log("\n=== 検証結果 ===");
            if (allValid)
            {
                Debug.Log("✓ すべての検証項目をパスしました。テスト実行可能です。");
                Debug.Log("テストを実行するには:");
                Debug.Log("1. Window > General > Test Runner を開く");
                Debug.Log("2. EditMode タブを選択");
                Debug.Log("3. BatchRenderingTool.Editor.Tests を展開");
                Debug.Log("4. Run All または個別のテストを実行");
            }
            else
            {
                Debug.LogError("✗ 一部の検証項目で問題が見つかりました。上記のエラーを確認してください。");
            }
        }
        
        [MenuItem("Window/Batch Rendering Tool/Create Test Scene")]
        public static void CreateTestScene()
        {
            Debug.Log("テストシーン作成中...");
            
            // テスト用のGameObjectとコンポーネントを作成
            var testDirector = new GameObject("TestDirector");
            var director = testDirector.AddComponent<UnityEngine.Playables.PlayableDirector>();
            var timeline = ScriptableObject.CreateInstance<UnityEngine.Timeline.TimelineAsset>();
            timeline.name = "TestTimeline";
            director.playableAsset = timeline;
            
            // タイムラインにトラックを追加
            var animTrack = timeline.CreateTrack<UnityEngine.Timeline.AnimationTrack>(null, "TestAnimationTrack");
            
            Debug.Log("✓ テストシーンを作成しました");
            Debug.Log("  - GameObject: TestDirector");
            Debug.Log("  - Timeline: TestTimeline");
            Debug.Log("  - Track: TestAnimationTrack");
            
            // シーンビューにフォーカス
            Selection.activeGameObject = testDirector;
            EditorGUIUtility.PingObject(testDirector);
        }
    }
}