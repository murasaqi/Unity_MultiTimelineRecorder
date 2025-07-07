# Project Structure

```
Assets/
├── BatchRenderingTool/     # Core batch rendering implementation
│   ├── Scripts/
│   │   ├── Editor/         # Editor scripts and tools
│   │   │   ├── RecorderSettingsConfigs/  # Configurations for different recorder types
│   │   │   ├── RecorderEditors/          # Custom editors for recorder settings
│   │   │   ├── Debug/                    # Debug tools and validators
│   │   │   ├── TestAutomation/           # Test automation and MCP integration
│   │   │   ├── Tests/                    # Unit tests
│   │   │   └── Workarounds/              # Patches and workarounds
│   │   └── Runtime/        # Runtime scripts
│   ├── Tests/              # Test reports and logs
│   └── DevPlane/           # Development plans and documentation
├── HDRPDefaultResources/   # HDRP configuration and assets
├── SampleSceneAssets/      # Demo assets (controllers, materials, textures)
└── Scenes/                 # Contains SampleScene with HDRP setup
```