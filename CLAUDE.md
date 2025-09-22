# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D project (EaseDev) built with Unity 2022.3.62f1. The project includes:
1. **Unity Project**: Standard Unity 3D project with sample scene and Unity packages
2. **Unity MCP Generator**: A powerful MCP (Model Context Protocol) tool for automatic Unity feature generation through natural language commands

## Project Structure

### Unity Project Files
- **Assets/**: Contains all Unity assets including scenes, scripts, materials, etc.
  - **Scenes/**: Unity scene files (currently contains SampleScene.unity)
- **ProjectSettings/**: Unity project configuration files
- **Packages/**: Unity package dependencies and custom packages
- **Library/**: Auto-generated Unity cache and build files (do not edit)
- **Logs/**: Unity editor and build logs
- **Temp/**: Temporary Unity files (auto-generated)
- **UserSettings/**: User-specific Unity editor settings

### MCP Generator Files
- **src/**: MCP server source code
  - **index.js**: Main MCP server entry point
  - **parsers/CommandParser.js**: Natural language command parser
  - **templates/TemplateManager.js**: Unity feature templates manager
  - **generators/UnityGenerator.js**: Unity project file generator
- **package.json**: Node.js project configuration
- **README.md**: Detailed usage instructions
- **claude_custom_instructions.md**: Claude Desktop configuration guide

## Development Commands

### Unity Project Commands

#### Building the Project
Unity projects are typically built through the Unity Editor GUI or command line:
- **Unity Editor**: File → Build Settings → Build
- **Command Line**: Use Unity's batch mode with `-buildTarget` parameter

#### Testing
- Unity Test Framework is available (com.unity.test-framework@1.1.33)
- **Run Tests**: Window → General → Test Runner in Unity Editor
- **Command Line Testing**: Use Unity batch mode with `-runTests` parameter

#### Code Coverage
- Code Coverage package is available (com.unity.testtools.codecoverage@1.2.6)
- **Access**: Window → Analysis → Code Coverage

### MCP Generator Commands

#### Setup and Installation
```bash
# Install MCP server dependencies
npm install

# Start MCP server in development mode
npm run dev

# Start MCP server in production mode
npm start
```

#### Using the MCP Tool
After configuring Claude Desktop (see claude_custom_instructions.md):

**Generate Unity Features:**
```
Use the unity-generator tool to create a login interface for project path "/Users/handongyu/work/unity/EaseDev"
```

**List Available Templates:**
```
Use the unity-generator tool to list all available Unity templates
```

**Supported Commands:**
- "我想做一个登录界面" (Create login interface)
- "我想做一个背包系统" (Create inventory system)
- "我想做一个红色的主菜单" (Create red-themed main menu)
- "我想做一个竖直布局的商店界面" (Create vertical layout shop UI)

### Unity MCP Bridge Commands

The Unity MCP Bridge provides direct WebSocket API access to Unity Editor functionality:

**Connection**: Unity MCP Bridge runs on `ws://localhost:8766` when Unity Editor is running.

**Available Tools:**
- `unity.create_gameobject` - Create GameObjects with primitives, positioning, etc.
- `unity.update_gameobject` - Update GameObject properties
- `unity.get_gameobject` - Get GameObject information
- `unity.create_prefab` - Create prefabs from GameObjects

**Example Usage:**
```javascript
// Create a cube named "test" at position (1,1,1)
const request = {
    id: "1",
    method: "unity.create_gameobject",
    params: {
        name: "test",
        primitiveType: "Cube",
        position: { x: 1, y: 1, z: 1 }
    }
};
```

**Testing Command:**
```bash
# Create and run test script
node test_mcp_create_cube.mjs
```

## Key Unity Packages

The project includes these notable packages:
- **Visual Scripting** (1.9.4): Node-based scripting system
- **TextMeshPro** (3.0.7): Advanced text rendering
- **Timeline** (1.7.7): Animation and sequence management
- **UI System** (1.0.0): Unity's UI framework
- **Test Framework** (1.1.33): Unit testing framework
- **Code Coverage** (1.2.6): Code coverage analysis

## Architecture Notes

This is a fresh Unity project with minimal customization:
- No custom C# scripts yet in the Assets folder
- Standard Unity project structure
- Uses default Unity packages for core functionality
- Solution file (EaseDev.sln) is minimal with no custom projects

## Unity-Specific Considerations

- **Meta Files**: Every asset in Unity has a corresponding .meta file for import settings
- **Assembly Definitions**: No custom assembly definitions are present yet
- **Build System**: Unity uses its own build system (Unity Bee) rather than MSBuild
- **Scene Management**: Currently has one sample scene that serves as the main scene
- **Version Control**: No .gitignore present - recommend adding Unity-specific .gitignore

## MCP Tool Integration

### Generated File Organization
The MCP tool follows Unity best practices for file organization:
- **UI Scripts**: `Assets/Scripts/UI/` - UI management scripts
- **System Scripts**: `Assets/Scripts/Systems/` - Game system logic
- **Item Scripts**: `Assets/Scripts/Items/` - Item and inventory related code
- **UI Prefabs**: `Assets/UI/` - User interface prefabs
- **Resources**: `Assets/Resources/` - ScriptableObject assets

### Code Generation Standards
- **Namespace**: All generated scripts use `EaseDev.[Category]` namespace
- **Naming**: PascalCase for classes, camelCase for fields
- **Unity Events**: Uses UnityEvent system for UI interactions
- **Serialization**: Uses Unity's [Header] and [SerializeField] attributes
- **Error Handling**: Includes null checks and validation

### Template Customization
- Templates support style modifications (color, size, layout)
- Command parsing handles both Chinese and English input
- Generated code includes comprehensive comments and documentation

## Development Workflow

1. **Open Project**: Open the project folder in Unity Hub or Unity Editor
2. **Script Development**: Create C# scripts in the Assets folder
3. **Testing**: Use Unity Test Runner for unit and integration tests
4. **Building**: Build through Unity Editor or command line batch mode
5. **Version Control**: Exclude Library/, Temp/, and UserSettings/ folders from version control