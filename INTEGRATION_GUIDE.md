# Unity MCP Generator - Claude Code 集成指南

## ✅ 集成状态

Unity MCP Generator 现在已经完全准备好在 Claude Code 中使用了！

## 🚀 立即使用

### 方式1: 通过工具函数调用 (推荐)

我已经创建了直接的Python工具函数，你现在可以立即使用：

```python
# 在Claude Code中直接调用
python3 -c "
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_tool import generate_unity_feature
print(generate_unity_feature('我想做一个登录界面'))
"
```

### 方式2: 通过Bash命令

你也可以直接使用Node.js脚本：

```bash
# 生成Unity功能
node /Users/handongyu/work/unity/EaseDev/src/generate.js "我想做一个登录界面" "/Users/handongyu/work/unity/EaseDev"

# 列出模板
node /Users/handongyu/work/unity/EaseDev/src/list-templates.js

# 创建模板
node /Users/handongyu/work/unity/EaseDev/src/create-template.js "custom_ui" '{"name": "Custom UI", "description": "自定义界面"}'
```

## 🎯 功能演示

### 生成登录界面
```bash
# 命令
python3 -c "
import sys; sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_tool import generate_unity_feature
print(generate_unity_feature('我想做一个登录界面'))
"

# 预期输出
✅ Successfully generated Login UI

📁 生成的文件:
- Scripts/UI/LoginManager.cs
- UI/LoginPanel.prefab
- SceneConfigs/CanvasConfig.json

🎯 功能类型: login_ui
📋 模板: Login UI
```

### 列出可用模板
```bash
# 命令
python3 -c "
import sys; sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_tool import list_unity_templates
print(list_unity_templates())
"

# 预期输出
🎯 Unity MCP Generator - 可用模板 (4个):
- Inventory System (inventory_system): 完整的背包系统
- Inventory UI (inventory_ui): 背包界面模板
- Login UI (login_ui): 登录界面模板
- Main Menu UI (main_menu_ui): 主菜单界面
```

## 📋 支持的命令

### UI界面类
- `"我想做一个登录界面"` → 生成登录UI + 管理脚本
- `"我想做一个背包界面"` → 生成背包UI + 管理脚本
- `"我想做一个主菜单"` → 生成主菜单UI + 管理脚本

### 样式定制类
- `"我想做一个红色的登录界面"` → 红色主题登录界面
- `"我想做一个大字体的主菜单"` → 大号字体主菜单
- `"我想做一个竖直布局的背包界面"` → 竖直布局背包界面

### 完整系统类
- `"我想做一个背包系统"` → 完整的背包管理系统

## 🔧 高级集成

### 为Claude Code创建快捷函数

创建 `~/.claude_code_functions.py`:

```python
#!/usr/bin/env python3
import sys
sys.path.append('/Users/handongyu/work/unity/EaseDev')
from unity_mcp_tool import generate_unity_feature, list_unity_templates

def unity_gen(description):
    """快捷Unity生成函数"""
    return generate_unity_feature(description)

def unity_templates():
    """快捷模板列表函数"""
    return list_unity_templates()

# 使用示例:
# python3 -c "exec(open('~/.claude_code_functions.py').read()); print(unity_gen('我想做一个登录界面'))"
```

### 环境变量设置

添加到 `~/.bashrc` 或 `~/.zshrc`:

```bash
# Unity MCP Generator
export UNITY_MCP_PATH="/Users/handongyu/work/unity/EaseDev"
export UNITY_PROJECT_PATH="/Users/handongyu/work/unity/EaseDev"

# 快捷别名
alias unity-gen='python3 -c "import sys; sys.path.append(\"$UNITY_MCP_PATH\"); from unity_mcp_tool import generate_unity_feature; print(generate_unity_feature(input(\"输入描述: \")))"'
alias unity-list='python3 -c "import sys; sys.path.append(\"$UNITY_MCP_PATH\"); from unity_mcp_tool import list_unity_templates; print(list_unity_templates())"'
```

## 🎮 实际使用示例

### 创建一个完整的游戏UI系统

```bash
# 1. 生成登录界面
python3 -c "import sys; sys.path.append('/Users/handongyu/work/unity/EaseDev'); from unity_mcp_tool import generate_unity_feature; print(generate_unity_feature('我想做一个登录界面'))"

# 2. 生成主菜单
python3 -c "import sys; sys.path.append('/Users/handongyu/work/unity/EaseDev'); from unity_mcp_tool import generate_unity_feature; print(generate_unity_feature('我想做一个主菜单'))"

# 3. 生成背包系统
python3 -c "import sys; sys.path.append('/Users/handongyu/work/unity/EaseDev'); from unity_mcp_tool import generate_unity_feature; print(generate_unity_feature('我想做一个背包系统'))"

# 4. 生成背包界面
python3 -c "import sys; sys.path.append('/Users/handongyu/work/unity/EaseDev'); from unity_mcp_tool import generate_unity_feature; print(generate_unity_feature('我想做一个背包界面'))"
```

## 🔍 生成文件预览

生成的文件会按照Unity最佳实践组织：

```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── LoginManager.cs      # 登录界面管理
│   │   ├── MainMenuManager.cs   # 主菜单管理
│   │   └── InventoryManager.cs  # 背包界面管理
│   ├── Systems/
│   │   └── InventorySystem.cs   # 背包系统核心
│   └── Items/
│       ├── Item.cs              # 物品基类
│       ├── ItemData.cs          # 物品数据
│       └── ItemDatabase.cs      # 物品数据库
├── UI/
│   ├── LoginPanel.prefab        # 登录界面预制件
│   ├── MainMenuPanel.prefab     # 主菜单预制件
│   └── InventoryPanel.prefab    # 背包界面预制件
├── Resources/
│   └── ItemDatabase.asset       # 物品数据库资源
└── SceneConfigs/
    └── CanvasConfig.json        # Canvas配置
```

## ✅ 验证集成

运行此命令验证集成是否成功：

```bash
python3 /Users/handongyu/work/unity/EaseDev/unity_mcp_tool.py
```

如果看到模板列表和生成测试结果，说明集成成功！

---

**🎉 现在你可以通过自然语言在Claude Code中快速生成Unity功能了！**

只需要在Claude Code中使用Python命令或Bash命令调用相应的工具函数即可。