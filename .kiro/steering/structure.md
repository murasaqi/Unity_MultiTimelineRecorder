# Project Structure

## Repository Layout
```
Unity_MultiTimelineRecorder/
├── jp.iridescent.multitimelinerecorder/    # Main Unity Package
├── Unity_MultiTimelineRecorder_Demo~/      # Demo Unity Project
├── README.md                               # Main documentation
├── README.ja.md                           # Japanese documentation
└── .kiro/                                 # Kiro configuration
```

## Unity Package Structure (`jp.iridescent.multitimelinerecorder/`)
```
jp.iridescent.multitimelinerecorder/
├── Runtime/                               # Runtime scripts
│   ├── Core/                             # Core runtime functionality
│   ├── Unity.MultiTimelineRecorder.Runtime.asmdef
│   └── *.cs                              # Runtime classes
├── Editor/                               # Editor-only scripts
│   ├── Core/                             # Core editor functionality
│   ├── EditorWindows/                    # Custom editor windows
│   ├── UI/                               # UI components
│   ├── RecorderConfigs/                  # Recorder configuration classes
│   ├── RecorderConfigEditors/            # Custom property drawers
│   ├── RecorderEditors/                  # Custom editors
│   ├── Settings/                         # Settings management
│   ├── Utilities/                        # Helper utilities
│   ├── Resources/                        # Editor resources
│   ├── ExternalControl/                  # External API control
│   ├── Interfaces/                       # Interface definitions
│   ├── Unity.MultiTimelineRecorder.Editor.asmdef
│   └── *.cs                              # Editor classes
├── Tests/                                # Unit tests
│   └── Editor/                           # Editor tests
├── Documentation~/                       # Package documentation
├── Samples~/                             # Sample assets
├── package.json                          # Package manifest
├── README.md                             # Package readme
├── LICENSE                               # MIT license
└── CHANGELOG.md                          # Version history
```

## Code Organization Patterns

### Naming Conventions
- **Namespaces**: `Unity.MultiTimelineRecorder.{Runtime|Editor}`
- **Assembly Names**: `Unity.MultiTimelineRecorder.{Runtime|Editor}`
- **File Names**: PascalCase matching class names
- **Folder Names**: PascalCase for organization

### Architecture Patterns
- **Separation of Concerns**: Clear Runtime/Editor separation
- **Factory Pattern**: `RecorderSettingsFactory` for creating recorder configurations
- **Interface-Based Design**: Interfaces folder for extensibility
- **Modular Recorder Types**: Separate files for each recorder type (Movie, Image, Animation, etc.)

### File Organization Rules
- **One Class Per File**: Each class in its own file
- **Logical Grouping**: Related functionality grouped in subfolders
- **Meta Files**: Unity .meta files alongside all assets
- **Assembly Definitions**: Proper assembly separation with explicit references

## Demo Project Structure (`Unity_MultiTimelineRecorder_Demo~/`)
- Standard Unity project layout
- Used for testing and demonstration
- Contains sample Timeline assets and configurations
- Recordings output to `Recordings/` folder