using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine.SceneManagement;

namespace BatchRenderingTool.Debug
{
    /// <summary>
    /// Alembicレコーダーのテストとデバッグ機能を提供するツール
    /// </summary>
    public class AlembicDebugTool : EditorWindow
    {
        private AlembicRecorderSettingsConfig testConfig;
        private GameObject testTargetObject;
        private Vector2 scrollPosition;
        
        // デバッグ情報
        private bool packageCheckFoldout = true;
        private bool exportPreviewFoldout = true;
        private bool debugInfoFoldout = true;
        private bool testExportFoldout = true;
        
        // パッケージ情報
        private bool isAlembicPackageAvailable = false;
        private string alembicPackageVersion = "Not found";
        private List<string> availableAlembicTypes = new List<string>();
        
        // エクスポート対象プレビュー
        private List<GameObject> previewObjects = new List<GameObject>();
        private Dictionary<GameObject, ComponentInfo> componentInfoCache = new Dictionary<GameObject, ComponentInfo>();
        
        // テストエクスポート設定
        private int testFrameCount = 10;
        private string testOutputPath = "Assets/AlembicTest";
        private bool showAdvancedSettings = false;
        
        // エラーログ
        private List<string> errorLogs = new List<string>();
        
        private class ComponentInfo
        {
            public int meshCount = 0;
            public int skinnedMeshCount = 0;
            public int cameraCount = 0;
            public int particleCount = 0;
            public int lightCount = 0;
            public int totalVertexCount = 0;
            public int totalTriangleCount = 0;
        }
        
        [MenuItem("Window/Batch Rendering Tool/Debug/Alembic Debug Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<AlembicDebugTool>("Alembic Debug Tool");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            testConfig = new AlembicRecorderSettingsConfig();
            CheckAlembicPackage();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Alembic Debug Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // パッケージチェック
            DrawPackageCheck();
            
            EditorGUILayout.Space();
            
            // エクスポート対象プレビュー
            DrawExportPreview();
            
            EditorGUILayout.Space();
            
            // テストエクスポート
            DrawTestExport();
            
            EditorGUILayout.Space();
            
            // デバッグ情報表示
            DrawDebugInfo();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// パッケージチェックセクションを描画
        /// </summary>
        private void DrawPackageCheck()
        {
            packageCheckFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(packageCheckFoldout, "Package Check");
            if (packageCheckFoldout)
            {
                EditorGUI.indentLevel++;
                
                // パッケージ状態
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Alembic Package:", GUILayout.Width(120));
                if (isAlembicPackageAvailable)
                {
                    EditorGUILayout.LabelField("✓ Installed", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
                }
                else
                {
                    EditorGUILayout.LabelField("✗ Not Found", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                }
                EditorGUILayout.EndHorizontal();
                
                // バージョン情報
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Version:", GUILayout.Width(120));
                EditorGUILayout.LabelField(alembicPackageVersion);
                EditorGUILayout.EndHorizontal();
                
                // 再チェックボタン
                if (GUILayout.Button("Re-check Package"))
                {
                    CheckAlembicPackage();
                }
                
                // 利用可能な型のリスト
                if (availableAlembicTypes.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Available Types:", EditorStyles.boldLabel);
                    foreach (var typeName in availableAlembicTypes)
                    {
                        EditorGUILayout.LabelField($"  • {typeName}", EditorStyles.miniLabel);
                    }
                }
                
                // パッケージがない場合の警告
                if (!isAlembicPackageAvailable)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "Alembic package is not installed. Please install 'com.unity.formats.alembic' package via Package Manager.",
                        MessageType.Error);
                    
                    if (GUILayout.Button("Open Package Manager"))
                    {
                        UnityEditor.PackageManager.UI.Window.Open("com.unity.formats.alembic");
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// エクスポート対象プレビューセクションを描画
        /// </summary>
        private void DrawExportPreview()
        {
            exportPreviewFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(exportPreviewFoldout, "Export Preview");
            if (exportPreviewFoldout)
            {
                EditorGUI.indentLevel++;
                
                // エクスポート設定
                EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
                
                testConfig.exportScope = (AlembicExportScope)EditorGUILayout.EnumPopup("Export Scope", testConfig.exportScope);
                
                if (testConfig.exportScope == AlembicExportScope.TargetGameObject)
                {
                    testTargetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", testTargetObject, typeof(GameObject), true);
                    testConfig.targetGameObject = testTargetObject;
                }
                
                testConfig.exportTargets = (AlembicExportTargets)EditorGUILayout.EnumFlagsField("Export Targets", testConfig.exportTargets);
                
                EditorGUILayout.Space();
                
                // プレビュー更新ボタン
                if (GUILayout.Button("Update Preview"))
                {
                    UpdateExportPreview();
                }
                
                // オブジェクトリスト
                if (previewObjects.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Objects to Export: {previewObjects.Count}", EditorStyles.boldLabel);
                    
                    // 統計情報
                    var totalInfo = new ComponentInfo();
                    foreach (var info in componentInfoCache.Values)
                    {
                        totalInfo.meshCount += info.meshCount;
                        totalInfo.skinnedMeshCount += info.skinnedMeshCount;
                        totalInfo.cameraCount += info.cameraCount;
                        totalInfo.particleCount += info.particleCount;
                        totalInfo.lightCount += info.lightCount;
                        totalInfo.totalVertexCount += info.totalVertexCount;
                        totalInfo.totalTriangleCount += info.totalTriangleCount;
                    }
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Total Statistics:");
                    EditorGUILayout.LabelField($"  Meshes: {totalInfo.meshCount}");
                    EditorGUILayout.LabelField($"  Skinned Meshes: {totalInfo.skinnedMeshCount}");
                    EditorGUILayout.LabelField($"  Cameras: {totalInfo.cameraCount}");
                    EditorGUILayout.LabelField($"  Particles: {totalInfo.particleCount}");
                    EditorGUILayout.LabelField($"  Lights: {totalInfo.lightCount}");
                    EditorGUILayout.LabelField($"  Total Vertices: {totalInfo.totalVertexCount:N0}");
                    EditorGUILayout.LabelField($"  Total Triangles: {totalInfo.totalTriangleCount:N0}");
                    EditorGUILayout.EndVertical();
                    
                    // オブジェクトごとの詳細
                    EditorGUILayout.Space();
                    foreach (var obj in previewObjects.Take(10)) // 最初の10個のみ表示
                    {
                        if (obj != null && componentInfoCache.TryGetValue(obj, out var info))
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField(obj, typeof(GameObject), true, GUILayout.Width(200));
                            
                            var details = new List<string>();
                            if (info.meshCount > 0) details.Add($"M:{info.meshCount}");
                            if (info.skinnedMeshCount > 0) details.Add($"SM:{info.skinnedMeshCount}");
                            if (info.cameraCount > 0) details.Add($"C:{info.cameraCount}");
                            if (info.particleCount > 0) details.Add($"P:{info.particleCount}");
                            if (info.lightCount > 0) details.Add($"L:{info.lightCount}");
                            
                            EditorGUILayout.LabelField(string.Join(", ", details));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    if (previewObjects.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... and {previewObjects.Count - 10} more objects");
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// テストエクスポートセクションを描画
        /// </summary>
        private void DrawTestExport()
        {
            testExportFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(testExportFoldout, "Test Export");
            if (testExportFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Test Export Settings", EditorStyles.boldLabel);
                
                testFrameCount = EditorGUILayout.IntSlider("Frame Count", testFrameCount, 1, 120);
                testConfig.frameRate = EditorGUILayout.FloatField("Frame Rate", testConfig.frameRate);
                
                EditorGUILayout.BeginHorizontal();
                testOutputPath = EditorGUILayout.TextField("Output Path", testOutputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.SaveFolderPanel("Select Output Folder", testOutputPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        testOutputPath = FileUtil.GetProjectRelativePath(selectedPath);
                        if (string.IsNullOrEmpty(testOutputPath))
                            testOutputPath = selectedPath;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 詳細設定
                showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
                if (showAdvancedSettings)
                {
                    EditorGUI.indentLevel++;
                    testConfig.scaleFactor = EditorGUILayout.FloatField("Scale Factor", testConfig.scaleFactor);
                    testConfig.handedness = (AlembicHandedness)EditorGUILayout.EnumPopup("Handedness", testConfig.handedness);
                    testConfig.samplesPerFrame = EditorGUILayout.IntSlider("Samples Per Frame", testConfig.samplesPerFrame, 1, 5);
                    testConfig.exportUVs = EditorGUILayout.Toggle("Export UVs", testConfig.exportUVs);
                    testConfig.exportNormals = EditorGUILayout.Toggle("Export Normals", testConfig.exportNormals);
                    testConfig.exportVertexColors = EditorGUILayout.Toggle("Export Vertex Colors", testConfig.exportVertexColors);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // テストエクスポートボタン
                using (new EditorGUI.DisabledScope(!isAlembicPackageAvailable))
                {
                    if (GUILayout.Button("Run Test Export", GUILayout.Height(30)))
                    {
                        RunTestExport();
                    }
                }
                
                // エラーログ表示
                if (errorLogs.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Error Logs:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foreach (var error in errorLogs)
                    {
                        EditorGUILayout.LabelField(error, EditorStyles.wordWrappedLabel);
                    }
                    EditorGUILayout.EndVertical();
                    
                    if (GUILayout.Button("Clear Logs"))
                    {
                        errorLogs.Clear();
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// デバッグ情報セクションを描画
        /// </summary>
        private void DrawDebugInfo()
        {
            debugInfoFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(debugInfoFoldout, "Debug Information");
            if (debugInfoFoldout)
            {
                EditorGUI.indentLevel++;
                
                // メモリ使用量
                EditorGUILayout.LabelField("Memory Usage", EditorStyles.boldLabel);
                var totalMemory = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                EditorGUILayout.LabelField($"Total Memory: {totalMemory:F2} MB");
                
                // パフォーマンス予測
                if (previewObjects.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Performance Estimation", EditorStyles.boldLabel);
                    
                    var totalInfo = componentInfoCache.Values.Aggregate(new ComponentInfo(), (acc, info) =>
                    {
                        acc.totalVertexCount += info.totalVertexCount;
                        acc.totalTriangleCount += info.totalTriangleCount;
                        return acc;
                    });
                    
                    // 推定ファイルサイズ（非常に大まかな推定）
                    float estimatedSizeMB = (totalInfo.totalVertexCount * 36 + totalInfo.totalTriangleCount * 12) * testFrameCount / (1024f * 1024f);
                    EditorGUILayout.LabelField($"Estimated File Size: ~{estimatedSizeMB:F1} MB");
                    
                    // 推定処理時間（非常に大まかな推定）
                    float estimatedTimeSec = totalInfo.totalVertexCount * testFrameCount / 100000f;
                    EditorGUILayout.LabelField($"Estimated Export Time: ~{estimatedTimeSec:F1} seconds");
                }
                
                // Unity Alembic API情報
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Alembic API Information", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Analyze Alembic API"))
                {
                    AnalyzeAlembicAPI();
                }
                
                // 他ソフトでの読み込みテスト手順
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Import Test Guide", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("1. Export test file using this tool");
                EditorGUILayout.LabelField("2. Import in target software:");
                EditorGUILayout.LabelField("   • Maya: File → Import");
                EditorGUILayout.LabelField("   • Blender: File → Import → Alembic");
                EditorGUILayout.LabelField("   • Houdini: File → Import → Alembic Scene");
                EditorGUILayout.LabelField("3. Check for:");
                EditorGUILayout.LabelField("   • Correct geometry");
                EditorGUILayout.LabelField("   • Animation playback");
                EditorGUILayout.LabelField("   • Scale and orientation");
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Alembicパッケージをチェック
        /// </summary>
        private void CheckAlembicPackage()
        {
            isAlembicPackageAvailable = AlembicExportInfo.IsAlembicPackageAvailable();
            availableAlembicTypes.Clear();
            
            // パッケージバージョンを取得
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AlembicDebugTool).Assembly);
            if (packageInfo != null)
            {
                alembicPackageVersion = packageInfo.version;
            }
            
            // Alembic関連の型をチェック
            var alembicTypeNames = new string[]
            {
                "UnityEditor.Recorder.AlembicRecorderSettings",
                "UnityEngine.Formats.Alembic.Importer.AlembicStreamPlayer",
                "UnityEditor.Formats.Alembic.Exporter.AlembicExporter"
            };
            
            foreach (var typeName in alembicTypeNames)
            {
                var type = System.Type.GetType(typeName + ", Unity.Formats.Alembic.Editor");
                if (type == null)
                    type = System.Type.GetType(typeName + ", Unity.Recorder.Editor");
                
                if (type != null)
                {
                    availableAlembicTypes.Add(type.FullName);
                }
            }
            
            // Alembic アセンブリを検索
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Alembic"))
                {
                    alembicPackageVersion = assembly.GetName().Version.ToString();
                    break;
                }
            }
        }
        
        /// <summary>
        /// エクスポートプレビューを更新
        /// </summary>
        private void UpdateExportPreview()
        {
            previewObjects.Clear();
            componentInfoCache.Clear();
            
            // 設定に基づいてオブジェクトを収集
            previewObjects = testConfig.GetExportObjects();
            
            // 各オブジェクトのコンポーネント情報を収集
            foreach (var obj in previewObjects)
            {
                if (obj == null) continue;
                
                var info = new ComponentInfo();
                
                // 自身と子オブジェクトのコンポーネントを収集
                var allTransforms = obj.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    // メッシュ
                    var meshFilter = t.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        info.meshCount++;
                        info.totalVertexCount += meshFilter.sharedMesh.vertexCount;
                        info.totalTriangleCount += meshFilter.sharedMesh.triangles.Length / 3;
                    }
                    
                    // スキンメッシュ
                    var skinnedMesh = t.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
                    {
                        info.skinnedMeshCount++;
                        info.totalVertexCount += skinnedMesh.sharedMesh.vertexCount;
                        info.totalTriangleCount += skinnedMesh.sharedMesh.triangles.Length / 3;
                    }
                    
                    // その他のコンポーネント
                    if (t.GetComponent<Camera>() != null) info.cameraCount++;
                    if (t.GetComponent<ParticleSystem>() != null) info.particleCount++;
                    if (t.GetComponent<Light>() != null) info.lightCount++;
                }
                
                componentInfoCache[obj] = info;
            }
        }
        
        /// <summary>
        /// テストエクスポートを実行
        /// </summary>
        private void RunTestExport()
        {
            errorLogs.Clear();
            
            try
            {
                Debug.Log("[AlembicDebugTool] Starting test export...");
                
                // 出力ディレクトリを作成
                if (!Directory.Exists(testOutputPath))
                {
                    Directory.CreateDirectory(testOutputPath);
                }
                
                // タイムスタンプ付きファイル名
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"AlembicTest_{timestamp}";
                
                // AlembicRecorderSettingsを作成
                var settings = testConfig.CreateAlembicRecorderSettings(fileName);
                if (settings == null)
                {
                    errorLogs.Add("Failed to create AlembicRecorderSettings");
                    return;
                }
                
                // 出力パスを設定
                RecorderSettingsHelper.SetOutputPath(settings, testOutputPath, fileName);
                
                // フレーム範囲を設定
                settings.StartFrame = 0;
                settings.EndFrame = testFrameCount - 1;
                
                Debug.Log($"[AlembicDebugTool] Export settings created. Output: {Path.Combine(testOutputPath, fileName)}.abc");
                
                // レコーダーコントローラーを作成して実行
                var controller = new RecorderController(settings);
                controller.PrepareRecording();
                
                if (controller.StartRecording())
                {
                    Debug.Log("[AlembicDebugTool] Recording started successfully");
                    
                    // Note: 実際のUnity Editorでは、Play Modeで実行する必要があります
                    EditorUtility.DisplayDialog("Test Export", 
                        $"Alembic recorder has been configured.\n\n" +
                        $"Output: {Path.Combine(testOutputPath, fileName)}.abc\n" +
                        $"Frames: {testFrameCount}\n\n" +
                        "Please enter Play Mode to start recording.", 
                        "OK");
                }
                else
                {
                    errorLogs.Add("Failed to start recording");
                }
            }
            catch (Exception ex)
            {
                errorLogs.Add($"Export failed: {ex.Message}");
                Debug.LogError($"[AlembicDebugTool] Export failed: {ex}");
            }
        }
        
        /// <summary>
        /// Alembic APIを分析
        /// </summary>
        private void AnalyzeAlembicAPI()
        {
            Debug.Log("=== Alembic API Analysis ===");
            
            // AlembicRecorderSettings型を探す
            System.Type alembicRecorderSettingsType = null;
            string[] possibleTypes = new string[]
            {
                "UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor",
                "UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor"
            };
            
            foreach (var typeName in possibleTypes)
            {
                alembicRecorderSettingsType = System.Type.GetType(typeName);
                if (alembicRecorderSettingsType != null)
                {
                    Debug.Log($"Found AlembicRecorderSettings: {alembicRecorderSettingsType.FullName}");
                    break;
                }
            }
            
            if (alembicRecorderSettingsType == null)
            {
                Debug.LogError("AlembicRecorderSettings type not found");
                return;
            }
            
            // プロパティとフィールドを分析
            Debug.Log("\n--- Properties ---");
            var properties = alembicRecorderSettingsType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                Debug.Log($"Property: {prop.Name} (Type: {prop.PropertyType.Name}, CanWrite: {prop.CanWrite})");
            }
            
            Debug.Log("\n--- Fields ---");
            var fields = alembicRecorderSettingsType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Debug.Log($"Field: {field.Name} (Type: {field.FieldType.Name})");
            }
            
            Debug.Log("\n--- Methods ---");
            var methods = alembicRecorderSettingsType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (!method.IsSpecialName) // プロパティのゲッター/セッターを除外
                {
                    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Debug.Log($"Method: {method.ReturnType.Name} {method.Name}({parameters})");
                }
            }
            
            Debug.Log("\n=== Analysis Complete ===");
        }
    }
}