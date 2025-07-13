# Unity Multi Timeline Recorder 実装ガイド

## 🚀 段階的実装プラン

### **Phase 1: 基盤インターフェースの設計**

#### 1.1 コアインターフェースの定義

```csharp
// Editor/Core/Interfaces/IRecorderPlugin.cs
namespace Unity.MultiTimelineRecorder.Core
{
    public interface IRecorderPlugin
    {
        string Name { get; }
        string DisplayName { get; }
        string Description { get; }
        Version Version { get; }
        Type ConfigType { get; }
        Type EditorType { get; }
        bool IsSupported { get; }
        
        IRecorderConfig CreateDefaultConfig();
        IRecorderEditor CreateEditor(IRecorderConfig config);
        bool ValidateConfig(IRecorderConfig config, out ValidationResult result);
        RecorderSettings CreateRecorderSettings(IRecorderConfig config);
    }
}
```

```csharp
// Editor/Core/Interfaces/IRecorderConfig.cs
namespace Unity.MultiTimelineRecorder.Core
{
    public interface IRecorderConfig
    {
        string Name { get; set; }
        bool Enabled { get; set; }
        ValidationResult Validate();
        void ApplyDefaults();
        IRecorderConfig Clone();
        void Serialize(SerializationContext context);
        void Deserialize(SerializationContext context);
    }
}
```

```csharp
// Editor/Core/Interfaces/IRecorderEditor.cs
namespace Unity.MultiTimelineRecorder.Core
{
    public interface IRecorderEditor
    {
        void OnGUI(Rect position);
        float GetPropertyHeight();
        bool HasPreviewGUI();
        void OnPreviewGUI(Rect position);
        void OnInspectorUpdate();
    }
}
```

#### 1.2 基底クラスの実装

```csharp
// Editor/Core/Base/BaseRecorderConfig.cs
namespace Unity.MultiTimelineRecorder.Core
{
    [Serializable]
    public abstract class BaseRecorderConfig : IRecorderConfig
    {
        [SerializeField] protected string name = "New Recorder";
        [SerializeField] protected bool enabled = true;
        
        public virtual string Name 
        { 
            get => name; 
            set => name = value; 
        }
        
        public virtual bool Enabled 
        { 
            get => enabled; 
            set => enabled = value; 
        }
        
        public abstract ValidationResult Validate();
        public abstract void ApplyDefaults();
        public abstract IRecorderConfig Clone();
        
        public virtual void Serialize(SerializationContext context)
        {
            context.WriteValue("name", name);
            context.WriteValue("enabled", enabled);
        }
        
        public virtual void Deserialize(SerializationContext context)
        {
            name = context.ReadValue<string>("name", "New Recorder");
            enabled = context.ReadValue<bool>("enabled", true);
        }
    }
}
```

### **Phase 2: プラグインマネージャーの実装**

#### 2.1 プラグイン管理システム

```csharp
// Editor/Core/RecorderPluginManager.cs
namespace Unity.MultiTimelineRecorder.Core
{
    public static class RecorderPluginManager
    {
        private static Dictionary<string, IRecorderPlugin> plugins = new();
        private static bool initialized = false;
        
        public static IReadOnlyDictionary<string, IRecorderPlugin> Plugins => plugins;
        
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (initialized) return;
            
            DiscoverPlugins();
            initialized = true;
        }
        
        private static void DiscoverPlugins()
        {
            var pluginTypes = TypeCache.GetTypesDerivedFrom<IRecorderPlugin>()
                .Where(t => !t.IsAbstract && !t.IsInterface);
                
            foreach (var type in pluginTypes)
            {
                try
                {
                    var plugin = (IRecorderPlugin)Activator.CreateInstance(type);
                    if (plugin.IsSupported)
                    {
                        plugins[plugin.Name] = plugin;
                        Debug.Log($"Registered recorder plugin: {plugin.DisplayName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to register plugin {type.Name}: {ex.Message}");
                }
            }
        }
        
        public static IRecorderPlugin GetPlugin(string name)
        {
            return plugins.TryGetValue(name, out var plugin) ? plugin : null;
        }
        
        public static T GetPlugin<T>() where T : class, IRecorderPlugin
        {
            return plugins.Values.OfType<T>().FirstOrDefault();
        }
        
        public static IRecorderConfig CreateConfig(string pluginName)
        {
            var plugin = GetPlugin(pluginName);
            return plugin?.CreateDefaultConfig();
        }
        
        public static IRecorderEditor CreateEditor(string pluginName, IRecorderConfig config)
        {
            var plugin = GetPlugin(pluginName);
            return plugin?.CreateEditor(config);
        }
    }
}
```

#### 2.2 プラグイン実装例

```csharp
// Editor/Plugins/ImageRecorderPlugin.cs
namespace Unity.MultiTimelineRecorder.Plugins
{
    [RecorderPlugin("image", "Image Recorder")]
    public class ImageRecorderPlugin : IRecorderPlugin
    {
        public string Name => "image";
        public string DisplayName => "Image Recorder";
        public string Description => "Records image sequences";
        public Version Version => new Version(1, 0, 0);
        public Type ConfigType => typeof(ImageRecorderConfig);
        public Type EditorType => typeof(ImageRecorderEditor);
        public bool IsSupported => true;
        
        public IRecorderConfig CreateDefaultConfig()
        {
            var config = new ImageRecorderConfig();
            config.ApplyDefaults();
            return config;
        }
        
        public IRecorderEditor CreateEditor(IRecorderConfig config)
        {
            return new ImageRecorderEditor(config as ImageRecorderConfig);
        }
        
        public bool ValidateConfig(IRecorderConfig config, out ValidationResult result)
        {
            result = config.Validate();
            return result.IsValid;
        }
        
        public RecorderSettings CreateRecorderSettings(IRecorderConfig config)
        {
            var imageConfig = config as ImageRecorderConfig;
            // Unity RecorderSettingsの生成ロジック
            return new ImageRecorderSettings();
        }
    }
}
```

### **Phase 3: UI層の分離**

#### 3.1 MVPパターンの実装

```csharp
// Editor/UI/Interfaces/IMultiTimelineRecorderView.cs
namespace Unity.MultiTimelineRecorder.UI
{
    public interface IMultiTimelineRecorderView
    {
        event Action<TimelineAsset> TimelineAdded;
        event Action<int> TimelineRemoved;
        event Action RecordingStartRequested;
        event Action RecordingStopRequested;
        
        void ShowProgress(float progress, string status);
        void ShowError(string message);
        void ShowSuccess(string message);
        void UpdateTimelineList(IEnumerable<TimelineInfo> timelines);
        void UpdateRecorderConfigs(IEnumerable<IRecorderConfig> configs);
        void SetRecordingState(bool isRecording);
    }
}
```

```csharp
// Editor/UI/Presenters/MultiTimelineRecorderPresenter.cs
namespace Unity.MultiTimelineRecorder.UI
{
    public class MultiTimelineRecorderPresenter
    {
        private readonly IMultiTimelineRecorderView view;
        private readonly IRecordingService recordingService;
        private readonly ISettingsService settingsService;
        
        public MultiTimelineRecorderPresenter(
            IMultiTimelineRecorderView view,
            IRecordingService recordingService,
            ISettingsService settingsService)
        {
            this.view = view;
            this.recordingService = recordingService;
            this.settingsService = settingsService;
            
            SubscribeToViewEvents();
            SubscribeToServiceEvents();
        }
        
        private void SubscribeToViewEvents()
        {
            view.TimelineAdded += OnTimelineAdded;
            view.TimelineRemoved += OnTimelineRemoved;
            view.RecordingStartRequested += OnRecordingStartRequested;
            view.RecordingStopRequested += OnRecordingStopRequested;
        }
        
        private void SubscribeToServiceEvents()
        {
            recordingService.ProgressChanged += OnRecordingProgressChanged;
            recordingService.RecordingCompleted += OnRecordingCompleted;
            recordingService.ErrorOccurred += OnRecordingError;
        }
        
        private async void OnRecordingStartRequested()
        {
            try
            {
                var settings = settingsService.GetCurrentSettings();
                var validationResult = ValidateSettings(settings);
                
                if (!validationResult.IsValid)
                {
                    view.ShowError(validationResult.ErrorMessage);
                    return;
                }
                
                view.SetRecordingState(true);
                await recordingService.StartRecordingAsync(settings);
            }
            catch (Exception ex)
            {
                view.ShowError($"Recording failed: {ex.Message}");
                view.SetRecordingState(false);
            }
        }
        
        // その他のイベントハンドラー...
    }
}
```

#### 3.2 再利用可能なUIコンポーネント

```csharp
// Editor/UI/Components/RecorderConfigPanel.cs
namespace Unity.MultiTimelineRecorder.UI.Components
{
    public class RecorderConfigPanel
    {
        private IRecorderConfig config;
        private IRecorderEditor editor;
        private bool foldout = true;
        
        public void SetConfig(IRecorderConfig config)
        {
            this.config = config;
            var plugin = RecorderPluginManager.GetPlugin(config.GetType().Name);
            this.editor = plugin?.CreateEditor(config);
        }
        
        public void OnGUI(Rect position)
        {
            if (config == null) return;
            
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            foldout = EditorGUI.Foldout(headerRect, foldout, config.Name);
            
            if (foldout && editor != null)
            {
                var contentRect = new Rect(
                    position.x, 
                    position.y + EditorGUIUtility.singleLineHeight,
                    position.width, 
                    editor.GetPropertyHeight());
                    
                editor.OnGUI(contentRect);
            }
        }
        
        public float GetHeight()
        {
            if (!foldout || editor == null)
                return EditorGUIUtility.singleLineHeight;
                
            return EditorGUIUtility.singleLineHeight + editor.GetPropertyHeight();
        }
    }
}
```

### **Phase 4: サービス層の実装**

#### 4.1 録画サービス

```csharp
// Editor/Services/IRecordingService.cs
namespace Unity.MultiTimelineRecorder.Services
{
    public interface IRecordingService
    {
        event Action<float, string> ProgressChanged;
        event Action<RecordingResult> RecordingCompleted;
        event Action<Exception> ErrorOccurred;
        
        bool IsRecording { get; }
        Task<RecordingResult> StartRecordingAsync(RecordingSettings settings);
        void StopRecording();
        void PauseRecording();
        void ResumeRecording();
    }
}
```

```csharp
// Editor/Services/RecordingService.cs
namespace Unity.MultiTimelineRecorder.Services
{
    public class RecordingService : IRecordingService
    {
        public event Action<float, string> ProgressChanged;
        public event Action<RecordingResult> RecordingCompleted;
        public event Action<Exception> ErrorOccurred;
        
        public bool IsRecording { get; private set; }
        
        private CancellationTokenSource cancellationTokenSource;
        
        public async Task<RecordingResult> StartRecordingAsync(RecordingSettings settings)
        {
            if (IsRecording)
                throw new InvalidOperationException("Recording is already in progress");
            
            IsRecording = true;
            cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                var result = await ExecuteRecordingAsync(settings, cancellationTokenSource.Token);
                RecordingCompleted?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                throw;
            }
            finally
            {
                IsRecording = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }
        
        private async Task<RecordingResult> ExecuteRecordingAsync(
            RecordingSettings settings, 
            CancellationToken cancellationToken)
        {
            var results = new List<RecordingItemResult>();
            
            for (int i = 0; i < settings.Items.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                var item = settings.Items[i];
                var progress = (float)i / settings.Items.Count;
                
                ProgressChanged?.Invoke(progress, $"Recording {item.Name}...");
                
                var itemResult = await RecordItemAsync(item, cancellationToken);
                results.Add(itemResult);
            }
            
            return new RecordingResult(results);
        }
        
        // その他のメソッド実装...
    }
}
```

### **Phase 5: イベントシステム**

#### 5.1 イベント定義

```csharp
// Editor/Events/RecorderEvents.cs
namespace Unity.MultiTimelineRecorder.Events
{
    public static class RecorderEvents
    {
        public static event Action<RecordingStartedEventArgs> RecordingStarted;
        public static event Action<RecordingProgressEventArgs> RecordingProgress;
        public static event Action<RecordingCompletedEventArgs> RecordingCompleted;
        public static event Action<RecordingErrorEventArgs> RecordingError;
        public static event Action<ConfigurationChangedEventArgs> ConfigurationChanged;
        
        internal static void RaiseRecordingStarted(RecordingStartedEventArgs args)
        {
            RecordingStarted?.Invoke(args);
        }
        
        internal static void RaiseRecordingProgress(RecordingProgressEventArgs args)
        {
            RecordingProgress?.Invoke(args);
        }
        
        // その他のイベント発火メソッド...
    }
}
```

## 🧪 テスト戦略

### **単体テスト**

```csharp
// Tests/Editor/RecorderPluginManagerTests.cs
namespace Unity.MultiTimelineRecorder.Tests
{
    public class RecorderPluginManagerTests
    {
        [Test]
        public void GetPlugin_WithValidName_ReturnsPlugin()
        {
            // Arrange
            var expectedPlugin = new MockRecorderPlugin();
            
            // Act
            var result = RecorderPluginManager.GetPlugin("mock");
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("mock", result.Name);
        }
        
        [Test]
        public void CreateConfig_WithValidPlugin_ReturnsConfig()
        {
            // Arrange & Act
            var config = RecorderPluginManager.CreateConfig("image");
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsInstanceOf<ImageRecorderConfig>(config);
        }
    }
}
```

### **統合テスト**

```csharp
// Tests/Editor/RecordingIntegrationTests.cs
namespace Unity.MultiTimelineRecorder.Tests
{
    public class RecordingIntegrationTests
    {
        [UnityTest]
        public IEnumerator RecordingWorkflow_WithValidSettings_CompletesSuccessfully()
        {
            // Arrange
            var settings = CreateTestRecordingSettings();
            var service = new RecordingService();
            
            // Act
            var recordingTask = service.StartRecordingAsync(settings);
            
            yield return new WaitUntil(() => recordingTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(recordingTask.Result.IsSuccess);
            Assert.Greater(recordingTask.Result.Items.Count, 0);
        }
    }
}
```

## 📋 移行チェックリスト

### **Phase 1: 基盤準備**
- [ ] コアインターフェース定義
- [ ] 基底クラス実装
- [ ] プラグイン属性システム
- [ ] 基本的な単体テスト

### **Phase 2: プラグインシステム**
- [ ] プラグインマネージャー実装
- [ ] 既存レコーダーのプラグイン化
- [ ] プラグイン登録システム
- [ ] プラグイン検証機能

### **Phase 3: UI分離**
- [ ] MVPパターン実装
- [ ] プレゼンター層作成
- [ ] UIコンポーネント分離
- [ ] イベント駆動UI更新

### **Phase 4: サービス層**
- [ ] 録画サービス実装
- [ ] 設定サービス実装
- [ ] 検証サービス実装
- [ ] 依存性注入システム

### **Phase 5: 最適化**
- [ ] 非同期処理最適化
- [ ] メモリ使用量削減
- [ ] パフォーマンステスト
- [ ] 統合テスト実装

この実装ガイドに従うことで、段階的かつ安全にリファクタリングを進めることができます。