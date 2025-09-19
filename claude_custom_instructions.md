# Unity MCP 工具配置

在 Claude Desktop 的配置文件中添加以下配置以启用Unity MCP工具：

## Claude Desktop 配置

在 `claude_desktop_config.json` 中添加：

```json
{
  "mcpServers": {
    "unity-generator": {
      "command": "node",
      "args": ["path/to/your/EaseDev/src/index.js"],
      "cwd": "path/to/your/EaseDev"
    }
  }
}
```

## 使用方法

配置完成后，你可以在Claude对话中使用以下命令：

### 生成Unity功能

```
请使用unity-generator工具，根据描述"我想做一个登录界面"生成Unity功能，项目路径是"/Users/handongyu/work/unity/EaseDev"
```

### 查看可用模板

```
使用unity-generator工具列出所有可用的Unity模板
```

### 支持的功能类型

- **UI界面**: 登录界面、注册界面、主菜单、设置页面、背包界面、商店界面等
- **游戏系统**: 背包系统、战斗系统、商店系统、任务系统、存档系统等
- **场景**: 游戏场景、菜单场景、加载场景等
- **特效**: 粒子特效、音效、动画等

### 命令示例

1. `"我想做一个红色的登录界面"` - 生成红色主题的登录界面
2. `"我想做一个大号字体的主菜单"` - 生成大字体的主菜单
3. `"我想做一个竖直布局的背包界面"` - 生成竖直布局的背包界面
4. `"我想做一个完整的背包系统"` - 生成包含逻辑的背包系统