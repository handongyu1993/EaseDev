# Unity MCP Generator

一个强大的Unity MCP (Model Context Protocol) 工具，可以通过自然语言命令自动生成Unity界面、系统和功能代码。

## 🚀 功能特点

- **自然语言解析**: 理解中文和英文命令，如"我想做一个登录界面"
- **智能模板系统**: 预置多种常用Unity功能模板
- **自动代码生成**: 生成完整的C#脚本、UI预制件和资源文件
- **样式定制**: 支持颜色、尺寸、布局等样式定制
- **Unity集成**: 直接在Unity项目中创建文件和配置

## 📦 支持的功能类型

### UI界面 (UI)
- ✅ 登录界面 (Login UI)
- ✅ 注册界面 (Register UI)
- ✅ 主菜单 (Main Menu)
- ✅ 设置页面 (Settings UI)
- ✅ 背包界面 (Inventory UI)
- ✅ 商店界面 (Shop UI)
- 🔄 聊天界面 (Chat UI)
- 🔄 血条界面 (Health Bar UI)

### 游戏系统 (System)
- ✅ 背包系统 (Inventory System)
- 🔄 战斗系统 (Combat System)
- 🔄 商店系统 (Shop System)
- 🔄 任务系统 (Quest System)
- 🔄 存档系统 (Save System)
- 🔄 音效系统 (Audio System)

### 场景 (Scene)
- 🔄 游戏场景 (Game Scene)
- 🔄 菜单场景 (Menu Scene)
- 🔄 加载场景 (Loading Scene)

## 🛠️ 安装和配置

### 1. 安装依赖
```bash
npm install
```

### 2. 配置Claude Desktop

在Claude Desktop的配置文件中添加MCP服务器配置：

**Windows**: `%APPDATA%\\Claude\\claude_desktop_config.json`
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Linux**: `~/.config/claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "unity-generator": {
      "command": "node",
      "args": ["路径到你的项目/EaseDev/src/index.js"],
      "cwd": "路径到你的项目/EaseDev"
    }
  }
}
```

### 3. 启动服务器（开发模式）
```bash
npm run dev
```

## 📖 使用方法

### 在Claude中使用

配置完成后，在Claude对话中可以使用以下方式：

```
请使用unity-generator工具生成Unity功能：
- 描述: "我想做一个登录界面"
- 项目路径: "/path/to/your/unity/project"
```

### 命令示例

#### 基础命令
- `"我想做一个登录界面"` - 生成标准登录界面
- `"我想做一个背包系统"` - 生成完整背包系统
- `"我想做一个主菜单"` - 生成主菜单界面

#### 带样式的命令
- `"我想做一个红色的登录界面"` - 生成红色主题登录界面
- `"我想做一个大号字体的主菜单"` - 生成大字体主菜单
- `"我想做一个竖直布局的背包界面"` - 生成竖直布局背包界面

#### 复杂命令
- `"我想做一个左对齐的蓝色设置页面"` - 多样式组合
- `"我想做一个网格布局的商店界面"` - 指定布局类型

### 支持的样式修饰符

- **颜色**: 红色、蓝色、绿色、黄色、紫色、黑色、白色
- **尺寸**: 小、中、大、迷你、巨大
- **位置**: 左、右、上、下、中间
- **布局**: 竖直、水平、网格

## 🔧 API 参考

### MCP 工具

#### `generate_unity_feature`
生成Unity功能和界面

**参数:**
- `description` (string): 功能描述
- `projectPath` (string): Unity项目路径

**示例:**
```json
{
  "description": "我想做一个登录界面",
  "projectPath": "/Users/username/UnityProject"
}
```

#### `list_available_templates`
列出所有可用模板

#### `create_custom_template`
创建自定义模板

**参数:**
- `templateName` (string): 模板名称
- `templateData` (object): 模板配置数据

## 📁 项目结构

```
EaseDev/
├── src/
│   ├── index.js              # MCP服务器主文件
│   ├── parsers/
│   │   └── CommandParser.js  # 自然语言命令解析器
│   ├── templates/
│   │   ├── TemplateManager.js # 模板管理器
│   │   └── data/             # 模板数据文件
│   ├── generators/
│   │   └── UnityGenerator.js # Unity项目生成器
│   └── utils/                # 工具函数
├── package.json              # 项目配置
└── README.md                # 说明文档
```

## 🎯 生成的文件

当你执行命令后，工具会在你的Unity项目中生成：

### 登录界面示例
```
Assets/
├── UI/
│   ├── LoginPanel.prefab     # 登录界面预制件
│   └── LoginPanel.prefab.meta
├── Scripts/
│   └── UI/
│       ├── LoginManager.cs  # 登录管理脚本
│       └── LoginManager.cs.meta
```

### 背包系统示例
```
Assets/
├── Scripts/
│   ├── Systems/
│   │   ├── InventorySystem.cs     # 背包系统主脚本
│   │   └── InventorySystem.cs.meta
│   └── Items/
│       ├── Item.cs                # 物品基类
│       ├── ItemData.cs            # 物品数据
│       ├── ItemDatabase.cs        # 物品数据库
│       └── *.meta
└── Resources/
    ├── ItemDatabase.asset         # 物品数据库资源
    └── ItemDatabase.asset.meta
```

## 🔄 开发和自定义

### 添加新模板

1. 在 `TemplateManager.js` 中添加新的模板定义
2. 在 `CommandParser.js` 中添加对应的关键词匹配
3. 重启MCP服务器

### 自定义样式处理

在 `UnityGenerator.js` 的 `applyCommandToTemplate` 方法中添加新的样式处理逻辑。

## 🐛 故障排除

### 常见问题

1. **MCP服务器无法启动**
   - 检查Node.js版本是否 >= 14
   - 确认package.json中的依赖已正确安装

2. **Unity项目路径错误**
   - 确保路径指向Unity项目根目录（包含Assets文件夹）
   - 使用绝对路径而非相对路径

3. **生成的文件有语法错误**
   - 检查模板中的代码语法
   - 确认Unity版本兼容性

### 调试模式

启动服务器时查看控制台输出：
```bash
npm run dev
```

## 📄 许可证

MIT License

## 🤝 贡献

欢迎提交Issue和Pull Request！

## 🎉 致谢

感谢Unity社区和MCP协议的开发者们！