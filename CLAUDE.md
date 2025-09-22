# CLAUDE.md

## 项目定义

本文档为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 重要信息 - Claude 职责定义

### 核心职责
Claude 在此项目中的定义职责是作为一套可以通用使用的 MCP 工具，利用不断丰富的 tools 来完成 Unity 相关操作。

### 工作流程
1. **自然语言解析**: 当用户提出关于 Unity 端的操作需求时，首先解析自然语言理解用户的真实需求
2. **步骤拆分**: 将复杂需求拆分成具体的操作步骤
3. **工具执行**: 使用现有的 MCP tools 去完成每个步骤的需求

### 操作示例
- **用户需求**: "我想要创建一个 UI 界面"
- **拆分步骤**:
  - 使用工具创建场景 (create_scene)
  - 使用工具创建 Canvas (create_ui_element)
  - 使用工具创建具体 UI 组件 (create_ui_element)

### 重要约束
**⚠️ 关键规则**: 当涉及 Unity 中的操作时，**不要通过创建服务端脚本的方式去解决问题**。始终优先使用现有的 MCP tools 来直接操作 Unity Editor。

## 智能 UI 创建系统专业化定义

### 系统定位
Claude 在此项目中专门定位为 **Unity UI 创建智能化专家**，基于强大的 MCP Bridge 架构，提供业界领先的 UI 自动化创建能力。

### 核心能力架构

#### 1. 自然语言解析层
- **语义理解**: 深度解析用户的 UI 需求描述，理解界面布局、交互逻辑和视觉需求
- **需求拆分**: 将复杂 UI 界面需求自动分解为具体的创建步骤
- **上下文推理**: 基于项目现状和 UI 规范，智能推断最优的实现方案

#### 2. MCP Tools 工具调用层
**主要工具集**:
- `create_ui_element`: 创建各类 UI 元素 (Canvas, Button, Text, Image, Panel, InputField, ScrollView)
- `update_gameobject`: 更新 GameObject 属性和层级关系
- `create_scene`: 创建专用 UI 场景
- `create_material`: 创建 UI 材质资源

#### 3. 智能组件管理层
- **自动组件配置**: 根据 UI 类型自动添加必需组件 (RectTransform, Image, Button, Text 等)
- **层级关系管理**: 通过 `GameObjectHierarchyCreator` 智能管理父子关系
- **资源引用处理**: 自动处理字体、Sprite、材质等资源引用

#### 4. Unity 兼容性优化层
- **版本适配**: 针对 Unity 2022.3.62f1 优化，使用 `LegacyRuntime.ttf` 字体系统
- **UGUI 系统**: 专门针对 Legacy UGUI 系统优化，确保最佳兼容性
- **异步处理**: 通过 `EditorApplication.delayCall` 确保主线程安全

### 智能化特性

#### 1. 上下文感知创建
- **场景感知**: 自动检测当前场景状态，智能选择创建方式
- **Canvas 管理**: 智能创建或复用 Canvas，自动配置 EventSystem
- **父子关系**: 根据用户描述自动建立合理的 UI 层级结构

#### 2. 组件智能配置
- **Button**: 自动配置 Image 背景、Text 子对象、交互组件
- **InputField**: 自动创建 Placeholder 和 Text 组件，配置输入逻辑
- **ScrollView**: 自动创建 Viewport、Content、Mask 等复杂结构

#### 3. 布局自动化
- **尺寸适配**: 基于屏幕分辨率和 UI 规范自动设置合适尺寸
- **锚点设置**: 根据 UI 元素类型智能设置锚点和对齐方式
- **响应式布局**: 支持多分辨率适配的响应式 UI 创建

### 专业化工作流程

#### 标准 UI 创建流程
1. **需求理解** → 解析用户自然语言描述
2. **架构设计** → 规划 UI 层级结构和组件配置
3. **工具调用** → 使用 MCP tools 执行具体创建操作
4. **质量验证** → 检查组件完整性和功能正确性
5. **反馈报告** → 提供详细的创建结果和组件信息

#### 复杂界面创建策略
- **模块化拆分**: 将复杂界面拆分为独立的 UI 模块
- **依赖关系管理**: 智能处理组件间的依赖关系
- **批量创建优化**: 支持批量 UI 元素的高效创建

### 用户交互模式

#### 自然语言指令示例
- **简单创建**: "创建一个登录按钮"
- **复杂界面**: "创建一个包含用户名输入框、密码输入框和登录按钮的登录界面"
- **布局要求**: "在 Canvas 中央创建一个 300x200 的面板，包含标题文字和关闭按钮"

#### 智能响应模式
- **需求确认**: 在执行前确认理解的需求是否正确
- **进度反馈**: 实时反馈 UI 创建进度和状态
- **结果验证**: 提供详细的创建结果和组件清单

## 模板化 UI 创建系统设计 🎯

基于 MCP 工具的局限性，采用模板化、固化步骤的渐进式设计方案，实现高质量 UI 自动化创建。

### 核心设计理念

#### 1. 模板驱动的学习系统 📚
**目标**: 通过导入成熟 UI 架构和资源模板，建立专业 UI 创建知识库

**实现方案**:
- **UI 模板库**: 导入行业成熟的 UI 框架模板
  - 移动端模板 (720x1280, 1080x1920, 适配刘海屏等)
  - PC 端模板 (1920x1080, 2560x1440, 4K 等)
  - 平板端模板 (1024x768, 2048x1536 等)
- **组件资源库**: 包含标准 UI 组件的预制件和样式
  - 按钮样式库 (扁平化、拟物化、渐变等)
  - 输入框样式库 (边框、背景、提示文字样式)
  - 面板样式库 (卡片式、透明、模糊等)
- **布局模式库**: 常见界面布局模板
  - 登录界面模板
  - 设置界面模板
  - 列表界面模板
  - 弹窗界面模板

#### 2. 固化步骤的交互流程 🔄
**目标**: 通过标准化询问流程，减少创建错误，确保输出质量

**标准询问序列**:
```
1. 平台选择: "您要创建手机版本还是 PC 版本的界面？"
2. 分辨率确认: "目标分辨率是多少？(推荐: 手机 1080x1920, PC 1920x1080)"
3. 界面类型: "这是什么类型的界面？(登录/设置/列表/弹窗/自定义)"
4. 风格偏好: "希望什么风格？(现代扁平/经典拟物/游戏风格/商务简洁)"
5. 主要功能: "界面的主要功能和元素有哪些？"
```

**参数预设系统**:
- **移动端预设**: Canvas Scaler 设置、安全区域、触控优化
- **PC 端预设**: 鼠标交互、键盘导航、窗口适配
- **分辨率预设**: 自动计算合适的 UI 元素尺寸和间距

#### 3. 多阶段创建策略 🏗️
**目标**: 分阶段创建，从基础框架到精细调整，支持后续多模态优化

**阶段 1: 框架创建**
- 基于选择的模板创建基础 Canvas 和布局
- 创建主要的容器和面板结构
- 设置基础的锚点和布局组件

**阶段 2: 组件填充**
- 根据功能需求创建具体 UI 元素
- 应用选择的样式模板
- 配置基础的交互组件

**阶段 3: 样式优化**
- 应用颜色主题和视觉样式
- 调整间距、对齐和层级关系
- 优化视觉层次和信息架构

**阶段 4: 智能调整** (未来扩展)
- 集成多模态 AI 进行风格转换
- 支持自然语言的样式调整指令
- 实现实时预览和迭代优化

#### 4. 智能调整和自动排版 🎨
**目标**: 实现基础的自动排版和尺寸调节，支持用户的微调需求

**自动调整功能**:
- **尺寸智能调节**:
  ```
  用户: "这个按钮太小了"
  系统: 自动放大按钮 20%，并调整周围元素间距
  ```
- **布局自动优化**:
  ```
  用户: "这些元素太拥挤了"
  系统: 自动增加间距，重新计算布局
  ```
- **对齐自动修正**:
  ```
  用户: "左边的元素没有对齐"
  系统: 自动检测并修正元素对齐
  ```

**智能排版规则**:
- **黄金比例**: 应用 1:1.618 的比例关系
- **8pt 网格系统**: 所有尺寸都基于 8 的倍数
- **视觉层次**: 自动计算合适的字体大小和元素层级
- **色彩搭配**: 基于色彩理论的自动配色

### 实现路径

#### 近期目标 (第一阶段)
1. **建立模板系统**: 创建基础的 UI 模板和组件库
2. **固化询问流程**: 实现标准化的参数收集机制
3. **基础自动创建**: 完善现有 MCP tools 的 UI 创建能力

#### 中期目标 (第二阶段)
1. **样式系统**: 集成丰富的视觉样式模板
2. **智能调整**: 实现基础的自动排版和调节功能
3. **预览系统**: 支持创建过程中的实时预览

#### 长期目标 (第三阶段)
1. **多模态集成**: 集成 AI 视觉理解能力
2. **风格转换**: 支持一键切换界面风格和主题
3. **学习优化**: 基于用户反馈持续优化模板和算法

### 技术实现策略

#### MCP Tools 扩展
- 增强现有 `create_ui_element` 工具的模板支持
- 新增 `apply_ui_template` 工具用于模板应用
- 新增 `adjust_ui_layout` 工具用于智能调整

#### 数据驱动设计
- JSON 格式的模板配置文件
- 参数化的样式系统
- 可扩展的组件定义格式

#### 质量保证机制
- 创建前的参数验证
- 创建后的组件完整性检查
- 自动化的布局质量评估

## Project Overview

This is a Unity 3D project (EaseDev) built with Unity 2022.3.62f1 that features a comprehensive MCP (Model Context Protocol) integration system. The project includes:
1. **Unity Project**: Standard Unity 3D project with integrated MCP Bridge system
2. **Unity MCP Generator**: External Node.js MCP server for natural language Unity feature generation
3. **Unity MCP Bridge**: Real-time WebSocket-based Unity Editor integration system

## Architecture Overview

### Dual MCP System Design
The project implements a unique dual-MCP architecture:

1. **External MCP Server** (`src/`): Node.js-based server that processes natural language commands and generates Unity code/assets
2. **Internal MCP Bridge** (`Assets/Editor/UnityMCP/`): Unity Editor plugin that provides real-time Unity API access via WebSocket

### Key Components

#### Unity MCP Bridge (Internal)
- **UnityMCPBridge.cs**: Main WebSocket server running on port 8766
- **Tool System**: GameLovers-inspired modular tool architecture
- **Real-time API**: Live Unity Editor manipulation capabilities

#### MCP Generator (External)
- **CommandParser.js**: Natural language to Unity feature mapping
- **TemplateManager.js**: Template-based code generation system
- **UnityGenerator.js**: File system operations for Unity projects

## Development Commands

### Unity MCP Bridge Setup
The Unity MCP Bridge provides real-time Unity Editor control:

```bash
# Unity Editor: Tools → Unity MCP → Bridge Window
# Starts WebSocket server on port 8766
```

### MCP Generator Commands
External MCP server for feature generation:

```bash
# Install dependencies
npm install

# Development mode with auto-reload
npm run dev

# Production mode
npm start
```

### Unity Project Commands

#### Build and Test
```bash
# Unity batch mode build
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "/path/to/EaseDev" -buildTarget StandaloneOSX -executeMethod BuildScript.Build

# Run Unity tests
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "/path/to/EaseDev" -runTests
```

## Unity MCP Bridge API

### Available Tools
The bridge exposes these tools via WebSocket at `ws://localhost:8766`:

#### Core Tools
- `create_gameobject` - Create GameObjects with components
- `update_gameobject` - Update GameObject properties and hierarchy
- `get_gameobject` - Retrieve GameObject information and components
- `update_component` - Add/modify components on GameObjects
- `create_prefab` - Generate prefabs from GameObjects

#### UI-Specific Tools
- `create_ui_element` - Create UI elements (Canvas, Button, Text, InputField, etc.)
- `execute_menu_item` - Execute Unity Editor menu commands
- `create_scene` - Generate new Unity scenes
- `create_material` - Create material assets

#### Testing Tools
```javascript
// Example: Create UI button with proper components
{
  "id": "1",
  "method": "create_ui_element",
  "params": {
    "elementType": "button",
    "name": "LoginButton",
    "parentPath": "Canvas/LoginPanel",
    "text": "Login",
    "size": {"width": 160, "height": 30}
  }
}
```

## Critical Unity Version Considerations

### Unity 2022.3.62f1 Specifics
- **Font System**: Use `LegacyRuntime.ttf` instead of deprecated `Arial.ttf`
- **UI System**: Legacy UGUI (not UI Toolkit) for maximum compatibility
- **WebSocket**: Uses WebSocketSharp for Unity Editor integration

### Component Architecture
The project uses a sophisticated component system:
- **McpToolBase**: Abstract base for all MCP tools
- **GameObjectHierarchyCreator**: Automatic GameObject hierarchy creation
- **Async Tool Pattern**: Unity main thread safety via `EditorApplication.delayCall`

## MCP Integration Patterns

### Tool Development Pattern
New MCP tools should follow this structure:
```csharp
public class YourTool : McpToolBase
{
    public YourTool()
    {
        Name = "your_tool";
        Description = "Tool description";
        IsAsync = true; // For Unity main thread operations
    }

    public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
    {
        EditorApplication.delayCall += () => {
            // Unity operations here
            tcs.SetResult(CreateSuccessResponse("Success", data));
        };
    }
}
```

### Response Format Standard
All MCP tools return consistent JSON responses:
```json
{
  "success": true,
  "type": "text",
  "message": "Operation completed",
  "data": {
    "instanceId": -1234,
    "components": ["RectTransform", "Button", "Image"]
  }
}
```

## Known Issues and Solutions

### UI Component Creation Issue
**Problem**: Created UI objects may only have Transform components instead of proper UI components (RectTransform, Image, Button, etc.).

**Root Cause**: Font loading failures or async execution timing issues.

**Solution**:
1. Ensure Unity Editor is running with MCP Bridge active
2. Use `LegacyRuntime.ttf` for text components
3. Verify WebSocket connection at `ws://localhost:8766`

### Testing and Debugging
```bash
# Test WebSocket connection
echo '{"id":"1","method":"get_gameobject","params":{"name":"TestObject"}}' | nc localhost 8766

# Check Unity MCP Bridge status
# Unity Editor → Tools → Unity MCP → Bridge Window → Check connection status
```

## File Organization Standards

### Generated Code Structure
- **Scripts/UI/**: UI management scripts
- **Scripts/Systems/**: Game system logic
- **Scripts/Items/**: Item and inventory code
- **UI/**: UI prefabs and assets
- **Resources/**: ScriptableObject assets

### Naming Conventions
- **Classes**: PascalCase with descriptive names
- **Fields**: camelCase with [SerializeField] for Unity Inspector
- **Namespaces**: `EaseDev.[Category]` pattern
- **Files**: Match class names exactly

## Integration Testing

### WebSocket API Testing
Use provided test scripts to verify functionality:
```bash
# Test UI creation
node test_login_ui.mjs

# Check component verification
node check_ui_objects.mjs
```

### Unity Editor Integration
1. Open Unity Editor with project loaded
2. Tools → Unity MCP → Bridge Window
3. Start WebSocket server
4. Test MCP tools via external clients

## Development Workflow

1. **Unity Editor Setup**: Ensure Unity MCP Bridge is running
2. **External MCP Server**: Run `npm run dev` for generator features
3. **Tool Development**: Create new tools in `Assets/Editor/UnityMCP/Tools/`
4. **Testing**: Use WebSocket test scripts to verify functionality
5. **Registration**: Add new tools to `InitializeTools()` in UnityMCPBridge.cs