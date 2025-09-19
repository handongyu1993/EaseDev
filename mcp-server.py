#!/usr/bin/env python3
"""
Unity MCP Generator Server for Claude Code
Provides Unity feature generation tools via MCP protocol
"""

import asyncio
import json
import sys
from typing import Any, Dict, List
from pathlib import Path
import subprocess
import os

# 检查是否有mcp模块，如果没有就使用基础实现
try:
    from mcp.server import Server, NotificationOptions
    from mcp.server.models import InitializationOptions
    from mcp.types import (
        Resource, Tool, TextContent, ImageContent, EmbeddedResource,
        LoggingLevel, EmptyResult
    )
    MCP_AVAILABLE = True
except ImportError:
    MCP_AVAILABLE = False
    # 基础MCP实现
    class Server:
        def __init__(self, name: str, version: str):
            self.name = name
            self.version = version
            self.tools = []
            self.handlers = {}

        def list_tools_handler(self, handler):
            self.handlers['list_tools'] = handler
            return handler

        def call_tool_handler(self, handler):
            self.handlers['call_tool'] = handler
            return handler

        async def run(self, read_stream, write_stream, initialization_options=None):
            # 简单的stdio MCP实现
            while True:
                try:
                    line = await read_stream.readline()
                    if not line:
                        break

                    data = json.loads(line.decode().strip())
                    response = await self.handle_request(data)

                    if response:
                        write_stream.write(json.dumps(response).encode() + b'\n')
                        await write_stream.drain()

                except Exception as e:
                    error_response = {
                        "jsonrpc": "2.0",
                        "id": data.get('id'),
                        "error": {"code": -32603, "message": str(e)}
                    }
                    write_stream.write(json.dumps(error_response).encode() + b'\n')
                    await write_stream.drain()

        async def handle_request(self, data):
            method = data.get('method')
            params = data.get('params', {})
            request_id = data.get('id')

            if method == 'tools/list':
                result = await self.handlers['list_tools']()
                return {
                    "jsonrpc": "2.0",
                    "id": request_id,
                    "result": {"tools": result}
                }
            elif method == 'tools/call':
                result = await self.handlers['call_tool'](params)
                return {
                    "jsonrpc": "2.0",
                    "id": request_id,
                    "result": result
                }

            return None

class UnityMCPServer:
    def __init__(self):
        self.server = Server("unity-generator", "1.0.0")
        self.project_root = Path(__file__).parent
        self.setup_handlers()

    def setup_handlers(self):
        @self.server.list_tools_handler()
        async def handle_list_tools():
            """Return list of available Unity generation tools"""
            return [
                {
                    "name": "generate_unity_feature",
                    "description": "根据自然语言描述生成Unity界面和功能",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "description": {
                                "type": "string",
                                "description": "功能描述，如'登录界面'、'背包系统'等"
                            },
                            "projectPath": {
                                "type": "string",
                                "description": "Unity项目路径"
                            }
                        },
                        "required": ["description", "projectPath"]
                    }
                },
                {
                    "name": "list_unity_templates",
                    "description": "列出所有可用的Unity模板",
                    "inputSchema": {
                        "type": "object",
                        "properties": {}
                    }
                },
                {
                    "name": "create_unity_template",
                    "description": "创建自定义Unity模板",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "templateName": {
                                "type": "string",
                                "description": "模板名称"
                            },
                            "templateData": {
                                "type": "object",
                                "description": "模板数据配置"
                            }
                        },
                        "required": ["templateName", "templateData"]
                    }
                }
            ]

        @self.server.call_tool_handler()
        async def handle_call_tool(params):
            """Handle tool execution"""
            name = params.get("name")
            arguments = params.get("arguments", {})

            if name == "generate_unity_feature":
                return await self.generate_unity_feature(arguments)
            elif name == "list_unity_templates":
                return await self.list_unity_templates()
            elif name == "create_unity_template":
                return await self.create_unity_template(arguments)
            else:
                raise Exception(f"Unknown tool: {name}")

    async def generate_unity_feature(self, args):
        """Generate Unity feature based on natural language description"""
        description = args.get("description", "")
        project_path = args.get("projectPath", str(self.project_root))

        try:
            # 调用Node.js生成器
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
                output = json.loads(result.stdout)
                return {
                    "content": [
                        {
                            "type": "text",
                            "text": f"✅ 成功生成Unity功能: {description}\\n\\n生成的文件:\\n" +
                                   "\\n".join(f"- {file}" for file in output.get('createdFiles', []))
                        }
                    ]
                }
            else:
                return {
                    "content": [
                        {
                            "type": "text",
                            "text": f"❌ 生成失败: {result.stderr}"
                        }
                    ]
                }

        except Exception as e:
            return {
                "content": [
                    {
                        "type": "text",
                        "text": f"❌ 执行错误: {str(e)}"
                    }
                ]
            }

    async def list_unity_templates(self):
        """List all available Unity templates"""
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
                template_list = "\\n".join(
                    f"- {t['name']} ({t['id']}): {t['description']}"
                    for t in templates
                )

                return {
                    "content": [
                        {
                            "type": "text",
                            "text": f"🎯 可用的Unity模板:\\n\\n{template_list}"
                        }
                    ]
                }
            else:
                return {
                    "content": [
                        {
                            "type": "text",
                            "text": f"❌ 获取模板失败: {result.stderr}"
                        }
                    ]
                }

        except Exception as e:
            return {
                "content": [
                    {
                        "type": "text",
                        "text": f"❌ 执行错误: {str(e)}"
                    }
                ]
            }

    async def create_unity_template(self, args):
        """Create custom Unity template"""
        template_name = args.get("templateName", "")
        template_data = args.get("templateData", {})

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
                return {
                    "content": [
                        {
                            "type": "text",
                            "text": f"✅ 模板 '{template_name}' 创建成功"
                        }
                    ]
                }
            else:
                return {
                    "content": [
                        {
                            "type": "text",
                            "text": f"❌ 模板创建失败: {result.stderr}"
                        }
                    ]
                }

        except Exception as e:
            return {
                "content": [
                    {
                        "type": "text",
                        "text": f"❌ 执行错误: {str(e)}"
                    }
                ]
            }

async def main():
    """Main entry point"""
    server = UnityMCPServer()

    if MCP_AVAILABLE:
        # 使用标准MCP
        async with Server("unity-generator", "1.0.0") as server_instance:
            await server_instance.run(
                sys.stdin.buffer,
                sys.stdout.buffer,
                InitializationOptions(
                    server_name="unity-generator",
                    server_version="1.0.0"
                )
            )
    else:
        # 使用自定义MCP实现
        await server.server.run(sys.stdin, sys.stdout)

if __name__ == "__main__":
    print("🚀 Unity MCP Generator Server starting...", file=sys.stderr)
    asyncio.run(main())