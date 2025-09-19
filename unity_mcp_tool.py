#!/usr/bin/env python3
"""
Unity MCP Tool - Direct integration for Claude Code
Provides Unity generation functions that can be called directly
"""

import subprocess
import json
import sys
import os
from pathlib import Path

class UnityMCPTool:
    """Unity MCP Generator Tool for Claude Code"""

    def __init__(self, project_root=None):
        self.project_root = Path(project_root) if project_root else Path(__file__).parent

    def generate_unity_feature(self, description: str, project_path: str = None) -> dict:
        """
        生成Unity功能

        Args:
            description: 功能描述，如"我想做一个登录界面"
            project_path: Unity项目路径

        Returns:
            dict: 生成结果
        """
        if not project_path:
            project_path = str(self.project_root)

        try:
            cmd = [
                "node",
                str(self.project_root / "src" / "generate.js"),
                description,
                project_path
            ]

            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                cwd=str(self.project_root)
            )

            if result.returncode == 0:
                return json.loads(result.stdout)
            else:
                return {
                    "success": False,
                    "error": result.stderr or "生成失败"
                }

        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    def list_unity_templates(self) -> dict:
        """
        列出所有可用的Unity模板

        Returns:
            dict: 模板列表
        """
        try:
            cmd = ["node", str(self.project_root / "src" / "list-templates.js")]

            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                cwd=str(self.project_root)
            )

            if result.returncode == 0:
                templates = json.loads(result.stdout)
                return {
                    "success": True,
                    "templates": templates,
                    "count": len(templates)
                }
            else:
                return {
                    "success": False,
                    "error": result.stderr or "获取模板失败"
                }

        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    def create_unity_template(self, template_name: str, template_data: dict) -> dict:
        """
        创建自定义Unity模板

        Args:
            template_name: 模板名称
            template_data: 模板数据

        Returns:
            dict: 创建结果
        """
        try:
            cmd = [
                "node",
                str(self.project_root / "src" / "create-template.js"),
                template_name,
                json.dumps(template_data)
            ]

            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                cwd=str(self.project_root)
            )

            if result.returncode == 0:
                return json.loads(result.stdout)
            else:
                return {
                    "success": False,
                    "error": result.stderr or "模板创建失败"
                }

        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

# 全局工具实例
_unity_tool = UnityMCPTool("/Users/handongyu/work/unity/EaseDev")

# 导出函数供Claude Code直接调用
def generate_unity_feature(description: str, project_path: str = "/Users/handongyu/work/unity/EaseDev") -> str:
    """
    生成Unity功能 - Claude Code工具函数

    Args:
        description: 功能描述，支持中英文，如:
                   - "我想做一个登录界面"
                   - "我想做一个红色的背包界面"
                   - "我想做一个竖直布局的主菜单"
                   - "我想做一个背包系统"
        project_path: Unity项目路径，默认为当前项目

    Returns:
        str: 生成结果的文本描述
    """
    result = _unity_tool.generate_unity_feature(description, project_path)

    if result.get("success"):
        files = result.get("createdFiles", [])
        message = result.get("message", "生成成功")

        files_text = "\\n".join(f"- {file}" for file in files) if files else "无文件生成"

        return f"""✅ {message}

📁 生成的文件:
{files_text}

🎯 功能类型: {result.get('command', {}).get('type', '未知')}
📋 模板: {result.get('template', {}).get('name', '未知')}"""
    else:
        error = result.get("error", "未知错误")
        return f"❌ 生成失败: {error}"

def list_unity_templates() -> str:
    """
    列出所有可用的Unity模板 - Claude Code工具函数

    Returns:
        str: 模板列表的文本描述
    """
    result = _unity_tool.list_unity_templates()

    if result.get("success"):
        templates = result.get("templates", [])
        count = result.get("count", 0)

        if not templates:
            return "📋 暂无可用模板"

        template_text = "\\n".join(
            f"- {t['name']} ({t['id']}): {t['description']}"
            for t in templates
        )

        return f"""🎯 Unity MCP Generator - 可用模板 ({count}个):

{template_text}

💡 使用方法:
- 基础: "我想做一个登录界面"
- 样式: "我想做一个红色的背包界面"
- 布局: "我想做一个竖直布局的主菜单"
- 系统: "我想做一个背包系统\""""
    else:
        error = result.get("error", "未知错误")
        return f"❌ 获取模板失败: {error}"

def create_unity_template(template_name: str, template_data: dict) -> str:
    """
    创建自定义Unity模板 - Claude Code工具函数

    Args:
        template_name: 模板名称
        template_data: 模板数据配置

    Returns:
        str: 创建结果的文本描述
    """
    result = _unity_tool.create_unity_template(template_name, template_data)

    if result.get("success"):
        return f"✅ 模板 '{template_name}' 创建成功"
    else:
        error = result.get("error", "未知错误")
        return f"❌ 模板创建失败: {error}"

if __name__ == "__main__":
    # 测试功能
    print("🧪 Unity MCP Tool 测试")
    print("===================")

    # 测试列出模板
    print("\\n📋 模板列表:")
    print(list_unity_templates())

    # 测试生成功能
    print("\\n🔨 生成测试:")
    print(generate_unity_feature("我想做一个登录界面"))