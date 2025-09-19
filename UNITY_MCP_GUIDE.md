# Unity MCP Server - 完整集成指南

## 🎯 项目概述

Unity MCP Server 是一个真正的Unity Editor集成工具，通过MCP (Model Context Protocol) 协议与Unity Editor实时通信，实现：

- **直接Unity操作**：创建场景、GameObject、UI等
- **实时状态获取**：获取场景信息、控制台日志等
- **自然语言控制**：通过Claude等AI助手控制Unity
- **双向通信**：Unity ↔ MCP Server ↔ Claude Code

## 🏗️ 架构设计

```
Claude Code  ←→  Unity MCP Server  ←→  Unity Editor
    (AI)            (Python)           (WebSocket/TCP)
```

### 核心组件

1. **Unity Editor Bridge** (`SimpleUnityMCP.cs`)
   - 在Unity Editor内运行
   - 提供TCP服务器接收外部命令
   - 执行Unity操作并返回结果

2. **MCP Server** (`unity_mcp_server.py`)
   - Python MCP服务器
   - 连接Unity Editor和Claude Code
   - 翻译MCP命令为Unity操作

3. **Claude Code集成**
   - 通过MCP协议调用Unity功能
   - 支持自然语言命令

## 🚀 快速开始

### 第一步：启动Unity Editor Bridge

1. **打开Unity Editor**
   ```bash
   # 打开EaseDev项目
   open -a Unity /Users/handongyu/work/unity/EaseDev
   ```

2. **打开Unity MCP Bridge窗口**
   ```
   Unity Editor → Tools → Unity MCP → Simple Bridge
   ```

3. **启动服务器**
   - 在Bridge窗口中点击 **"Start Server"**
   - 确认看到 "Server Status: Running"
   - 记住端口号(默认8765)

### 第二步：测试MCP服务器

```bash
# 测试Unity MCP服务器
python3 unity_mcp_server.py --test
```

如果看到连接成功信息，说明配置正确！

### 第三步：在Claude Code中使用

现在你可以在Claude Code中使用Unity MCP工具了！

## 🎮 支持的Unity操作

### 1. 创建新场景
```python
python3 -c "
import asyncio
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer

async def test():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_create_scene', {'sceneName': 'MyNewScene'})
    print(result['content'][0]['text'])

asyncio.run(test())
"
```

### 2. 创建GameObject
```python
python3 -c "
import asyncio
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer

async def test():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_create_gameobject', {'name': 'Player'})
    print(result['content'][0]['text'])

asyncio.run(test())
"
```

### 3. 创建UI Canvas
```python
python3 -c "
import asyncio
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer

async def test():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_create_ui_canvas', {})
    print(result['content'][0]['text'])

asyncio.run(test())
"
```

### 4. 获取场景信息
```python
python3 -c "
import asyncio
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer

async def test():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_get_scene_info', {})
    print(result['content'][0]['text'])

asyncio.run(test())
"
```

### 5. 选择GameObject
```python
python3 -c "
import asyncio
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer

async def test():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_select_gameobject', {'name': 'Main Camera'})
    print(result['content'][0]['text'])

asyncio.run(test())
"
```

## 🔧 完整工作流程示例

### 创建一个游戏场景

```bash
# 1. 创建新场景
python3 -c "
import asyncio
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer

async def create_game_scene():
    server = UnityMCPServer()

    print('🎬 Creating new scene...')
    result = await server.execute_unity_command('unity_create_scene', {'sceneName': 'GameScene'})
    print(result['content'][0]['text'])

    print('\n🎮 Creating Player GameObject...')
    result = await server.execute_unity_command('unity_create_gameobject', {'name': 'Player'})
    print(result['content'][0]['text'])

    print('\n🖼️ Creating UI Canvas...')
    result = await server.execute_unity_command('unity_create_ui_canvas', {})
    print(result['content'][0]['text'])

    print('\n📋 Getting scene info...')
    result = await server.execute_unity_command('unity_get_scene_info', {})
    print(result['content'][0]['text'])

asyncio.run(create_game_scene())
"
```

## 📋 可用MCP工具列表

| 工具名称 | 功能描述 | 参数 |
|---------|----------|------|
| `unity_create_scene` | 创建新Unity场景 | `sceneName` (可选) |
| `unity_create_gameobject` | 创建GameObject | `name` (可选), `parent` (可选) |
| `unity_create_ui_canvas` | 创建UI Canvas和EventSystem | 无 |
| `unity_get_scene_info` | 获取当前场景信息 | 无 |
| `unity_select_gameobject` | 选择GameObject | `name` (必需) |
| `unity_execute_menu` | 执行Unity菜单命令 | `menuPath` (必需) |
| `unity_get_console_logs` | 获取控制台日志 | 无 |

## 🔍 故障排除

### 常见问题

#### 1. "无法连接到Unity Editor"
**解决方案:**
1. 确保Unity Editor已打开
2. 确保已打开 Tools → Unity MCP → Simple Bridge 窗口
3. 确认服务器状态为 "Running"
4. 检查端口8765是否被占用

#### 2. "Connection refused"
**解决方案:**
1. 重启Unity MCP Bridge服务器
2. 检查防火墙设置
3. 尝试更改端口号

#### 3. "Module not found"
**解决方案:**
```bash
# 重新安装依赖
python3 install_deps.py

# 或手动安装
pip3 install websockets asyncio
```

### 调试模式

启用详细日志：
```bash
# 运行测试模式查看详细信息
python3 unity_mcp_server.py --test
```

## 🎯 在Claude Code中的实际使用

### 创建Unity功能的自然语言命令

你现在可以在Claude Code中这样使用：

**"帮我在Unity中创建一个新的游戏场景"**
```python
python3 -c "
import asyncio, sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer
async def main():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_create_scene', {'sceneName': 'NewGameScene'})
    print(result['content'][0]['text'])
asyncio.run(main())
"
```

**"创建一个玩家角色GameObject"**
```python
python3 -c "
import asyncio, sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer
async def main():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_create_gameobject', {'name': 'Player'})
    print(result['content'][0]['text'])
asyncio.run(main())
"
```

**"创建UI界面系统"**
```python
python3 -c "
import asyncio, sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_server import UnityMCPServer
async def main():
    server = UnityMCPServer()
    result = await server.execute_unity_command('unity_create_ui_canvas', {})
    print(result['content'][0]['text'])
asyncio.run(main())
"
```

## 🎉 成功验证

如果一切正常，你应该能看到：

1. **Unity Editor中**: 新创建的场景、GameObject、UI等
2. **Unity MCP Bridge窗口中**: 连接和命令执行日志
3. **Claude Code中**: 成功执行的命令反馈

## 🔮 扩展功能

这个Unity MCP Server支持轻松扩展：

1. **添加新的Unity操作** - 在 `SimpleUnityMCP.cs` 中添加新方法
2. **支持更多参数** - 扩展现有命令的参数处理
3. **集成其他Unity功能** - 如Animation、Physics等
4. **自定义MCP工具** - 在 `unity_mcp_server.py` 中添加新工具

---

**🎮 现在你拥有了一个完整的Unity MCP集成系统！**

通过自然语言在Claude Code中直接控制Unity Editor，实现真正的AI驱动游戏开发工作流程！