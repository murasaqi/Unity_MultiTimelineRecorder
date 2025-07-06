using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// テスト設定の詳細ウィンドウ
    /// </summary>
    public class TestConfigurationWindow : EditorWindow
    {
        private TestRunnerAutomation.TestRunConfiguration configuration;
        private Action<TestRunnerAutomation.TestRunConfiguration> onSave;
        private Vector2 scrollPosition;
        
        // UI状態
        private bool showCategories = true;
        private bool showTestNames = true;
        private bool showAdvanced = false;
        
        // 一時的な入力フィールド
        private string newCategory = "";
        private string newTestName = "";
        
        public static void ShowWindow(TestRunnerAutomation.TestRunConfiguration config, 
            Action<TestRunnerAutomation.TestRunConfiguration> saveCallback)
        {
            var window = GetWindow<TestConfigurationWindow>(true, "Test Configuration", true);
            window.configuration = config;
            window.onSave = saveCallback;
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            if (configuration == null)
            {
                Close();
                return;
            }
            
            EditorGUILayout.BeginVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawBasicSettings();
            EditorGUILayout.Space();
            
            DrawCategoryFilter();
            EditorGUILayout.Space();
            
            DrawTestNameFilter();
            EditorGUILayout.Space();
            
            DrawAdvancedSettings();
            EditorGUILayout.Space();
            
            DrawButtons();
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBasicSettings()
        {
            GUILayout.Label("基本設定", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            configuration.assemblyName = EditorGUILayout.TextField("アセンブリ名", configuration.assemblyName);
            configuration.testMode = (TestRunnerAutomation.TestMode)EditorGUILayout.EnumPopup("テストモード", configuration.testMode);
            configuration.enabled = EditorGUILayout.Toggle("有効", configuration.enabled);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCategoryFilter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            showCategories = EditorGUILayout.Foldout(showCategories, "カテゴリフィルター", true);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("すべてクリア", GUILayout.Width(80)))
            {
                configuration.categories.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            if (showCategories)
            {
                // 既存のカテゴリ
                for (int i = 0; i < configuration.categories.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    configuration.categories[i] = EditorGUILayout.TextField(configuration.categories[i]);
                    
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        configuration.categories.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // 新規追加
                EditorGUILayout.BeginHorizontal();
                newCategory = EditorGUILayout.TextField(newCategory);
                
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newCategory));
                if (GUILayout.Button("追加", GUILayout.Width(50)))
                {
                    configuration.categories.Add(newCategory);
                    newCategory = "";
                    GUI.FocusControl(null);
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox(
                    "カテゴリ名を指定すると、該当するカテゴリのテストのみが実行されます。\n" +
                    "空の場合はすべてのテストが実行されます。",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTestNameFilter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            showTestNames = EditorGUILayout.Foldout(showTestNames, "テスト名フィルター", true);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("すべてクリア", GUILayout.Width(80)))
            {
                configuration.testNames.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            if (showTestNames)
            {
                // 既存のテスト名
                for (int i = 0; i < configuration.testNames.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    configuration.testNames[i] = EditorGUILayout.TextField(configuration.testNames[i]);
                    
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        configuration.testNames.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // 新規追加
                EditorGUILayout.BeginHorizontal();
                newTestName = EditorGUILayout.TextField(newTestName);
                
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newTestName));
                if (GUILayout.Button("追加", GUILayout.Width(50)))
                {
                    configuration.testNames.Add(newTestName);
                    newTestName = "";
                    GUI.FocusControl(null);
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox(
                    "テスト名を指定すると、該当するテストのみが実行されます。\n" +
                    "フルネーム（NameSpace.ClassName.MethodName）で指定してください。\n" +
                    "正規表現も使用できます。",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAdvancedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "詳細設定", true);
            
            if (showAdvanced)
            {
                configuration.retryCount = EditorGUILayout.IntField("リトライ回数", configuration.retryCount);
                configuration.retryCount = Mathf.Max(0, configuration.retryCount);
                
                configuration.continueOnFailure = EditorGUILayout.Toggle("失敗時も継続", configuration.continueOnFailure);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.HelpBox(
                    "リトライ回数: テスト失敗時の再実行回数\n" +
                    "失敗時も継続: テストが失敗しても次のテストを実行",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("キャンセル", GUILayout.Width(100)))
            {
                Close();
            }
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存", GUILayout.Width(100)))
            {
                SaveAndClose();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void SaveAndClose()
        {
            onSave?.Invoke(configuration);
            Close();
        }
    }
}