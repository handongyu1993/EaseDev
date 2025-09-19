# Unity MCP Generator - 项目总结

## 🎯 项目简介

Unity MCP Generator 是一个强大的工具，允许你通过自然语言命令（如"我想做一个登录界面"）自动在Unity项目中生成完整的UI界面、游戏系统和功能代码。

## ✨ 核心功能

### 🗣️ 自然语言理解
- **中英文支持**: 理解中文和英文命令
- **智能解析**: 自动识别功能类型、样式和配置
- **灵活表达**: 支持多种表达方式

### 🎨 智能代码生成
- **完整功能**: 生成UI、逻辑脚本、资源文件
- **Unity标准**: 遵循Unity最佳实践和命名规范
- **可定制化**: 支持颜色、尺寸、布局等样式定制

### 📋 丰富模板库
- **UI界面**: 登录、注册、主菜单、背包、商店等
- **游戏系统**: 背包系统、战斗系统、存档系统等
- **可扩展**: 支持自定义模板添加

## 📁 生成的文件示例

### 登录界面 ("我想做一个登录界面")
```
Assets/
├── UI/
│   └── LoginPanel.prefab          # 登录界面预制件
└── Scripts/
    └── UI/
        └── LoginManager.cs        # 登录管理脚本
```

### 背包系统 ("我想做一个背包系统")
```
Assets/
├── Scripts/
│   ├── Systems/
│   │   └── InventorySystem.cs     # 背包系统核心
│   └── Items/
│       ├── Item.cs                # 物品基类
│       ├── ItemData.cs            # 物品数据
│       └── ItemDatabase.cs        # 物品数据库
└── Resources/
    └── ItemDatabase.asset         # 物品数据库资源
```

## 🚀 快速开始

### 1. 环境准备
```bash
# 确保已安装 Node.js (>=14)
node --version

# 安装依赖
npm install
```

### 2. 配置Claude Desktop
在Claude Desktop配置文件中添加：
```json
{
  "mcpServers": {
    "unity-generator": {
      "command": "node",
      "args": ["path/to/EaseDev/src/index.js"],
      "cwd": "path/to/EaseDev"
    }
  }
}
```

### 3. 启动服务器
```bash
# Unix/Linux/macOS
./start_mcp.sh

# Windows
start_mcp.bat

# 或直接启动
npm start
```

### 4. 在Claude中使用
```
请使用unity-generator工具生成Unity功能：
描述: "我想做一个登录界面"
项目路径: "/path/to/your/unity/project"
```

## 📋 支持的命令类型

### 基础命令
- `"我想做一个登录界面"` → 生成登录UI + 管理脚本
- `"我想做一个背包系统"` → 生成完整背包系统
- `"我想做一个主菜单"` → 生成主菜单UI

### 样式定制命令
- `"我想做一个红色的登录界面"` → 红色主题
- `"我想做一个大字体的主菜单"` → 大号字体
- `"我想做一个竖直布局的背包界面"` → 竖直布局

### 复合样式命令
- `"我想做一个左对齐的蓝色设置页面"`
- `"我想做一个网格布局的商店界面"`

## 🎨 支持的样式修饰符

| 类型 | 支持的值 |
|------|----------|
| **颜色** | 红色、蓝色、绿色、黄色、紫色、黑色、白色 |
| **尺寸** | 小、中、大、迷你、巨大 |
| **位置** | 左、右、上、下、中间 |
| **布局** | 竖直、水平、网格 |

## 🔧 技术架构

### 核心组件
1. **CommandParser** - 自然语言命令解析器
2. **TemplateManager** - 模板管理和存储
3. **UnityGenerator** - Unity项目文件生成器
4. **MCP Server** - Model Context Protocol 服务器

### 技术栈
- **后端**: Node.js + MCP SDK
- **模板引擎**: 自定义模板系统
- **Unity集成**: 标准Unity项目结构
- **协议**: Model Context Protocol (MCP)

## 📊 项目统计

- **模板数量**: 10+ 预置模板
- **支持语言**: 中文、英文
- **生成文件类型**: C#脚本、预制件、ScriptableObject
- **Unity版本**: 2022.3.62f1 (兼容其他版本)

## 🛠️ 开发和扩展

### 添加新模板
1. 在 `TemplateManager.js` 中定义模板
2. 在 `CommandParser.js` 中添加关键词
3. 重启MCP服务器

### 自定义样式处理
修改 `UnityGenerator.js` 中的 `applyCommandToTemplate` 方法

## 📝 项目文件结构

```
EaseDev/
├── Assets/                        # Unity项目资源
├── src/                          # MCP工具源码
│   ├── index.js                  # MCP服务器入口
│   ├── parsers/CommandParser.js  # 命令解析器
│   ├── templates/TemplateManager.js # 模板管理器
│   └── generators/UnityGenerator.js # 代码生成器
├── package.json                  # Node.js项目配置
├── start_mcp.sh/.bat            # 启动脚本
├── test_mcp.js                  # 测试脚本
├── README.md                    # 详细说明
├── CLAUDE.md                    # Claude Code 配置
└── claude_custom_instructions.md # Claude Desktop 配置
```

## 🎉 使用场景

### 游戏开发初期
快速搭建基础UI和系统架构

### 原型开发
快速创建功能原型进行测试

### 学习Unity
通过生成的代码学习Unity开发规范

### 项目模板化
为团队创建统一的开发模板

## ⚡ 性能和限制

### 优势
- 秒级生成完整功能
- 标准化代码结构
- 减少重复性工作
- 降低学习成本

### 限制
- 生成的是基础模板，需要进一步定制
- 复杂逻辑需要手动实现
- 依赖Unity项目结构规范

## 🔮 未来规划

- [ ] 支持更多UI组件类型
- [ ] 增加移动端适配模板
- [ ] 支持3D场景生成
- [ ] 集成更多Unity包
- [ ] 支持自定义主题系统

## 🤝 贡献指南

欢迎提交Issue和Pull Request！

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 发起 Pull Request

## 📞 支持和反馈

如有问题或建议，请创建Issue或通过以下方式联系：

- **项目仓库**: [GitHub链接]
- **问题反馈**: [Issues链接]
- **功能建议**: [Discussions链接]

---

**🎮 Happy Unity Development with AI! 🤖**