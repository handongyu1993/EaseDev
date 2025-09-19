#!/usr/bin/env python3
"""
Unity MCP Server - Real Unity Editor Integration
Communicates directly with Unity Editor through WebSocket
"""

import asyncio
import json
import sys
import websockets
from typing import Any, Dict, List
from pathlib import Path

# MCP server implementation
try:
    from mcp.server import Server
    from mcp.server.models import InitializationOptions
    from mcp.types import Tool, TextContent
    MCP_AVAILABLE = True
except ImportError:
    MCP_AVAILABLE = False
    print("MCP module not available, using basic implementation", file=sys.stderr)

class UnityMCPServer:
    def __init__(self):
        self.websocket = None
        self.unity_connected = False
        self.unity_host = "localhost"
        self.unity_port = 8765

        if MCP_AVAILABLE:
            self.server = Server("unity-mcp", "1.0.0")
        else:
            self.server = None

        self.setup_tools()

    def setup_tools(self):
        """Setup MCP tools for Unity operations"""
        self.tools = [
            {
                "name": "unity_create_scene",
                "description": "在Unity中创建新场景",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "sceneName": {
                            "type": "string",
                            "description": "场景名称",
                            "default": "NewScene"
                        }
                    }
                }
            },
            {
                "name": "unity_create_gameobject",
                "description": "在Unity场景中创建GameObject",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "name": {
                            "type": "string",
                            "description": "GameObject名称",
                            "default": "GameObject"
                        },
                        "parent": {
                            "type": "string",
                            "description": "父对象名称（可选）"
                        }
                    }
                }
            },
            {
                "name": "unity_create_ui_canvas",
                "description": "创建UI Canvas和EventSystem",
                "inputSchema": {
                    "type": "object",
                    "properties": {}
                }
            },
            {
                "name": "unity_get_scene_info",
                "description": "获取当前场景信息和所有GameObject",
                "inputSchema": {
                    "type": "object",
                    "properties": {}
                }
            },
            {
                "name": "unity_select_gameobject",
                "description": "在Unity编辑器中选择GameObject",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "name": {
                            "type": "string",
                            "description": "要选择的GameObject名称"
                        }
                    },
                    "required": ["name"]
                }
            },
            {
                "name": "unity_execute_menu",
                "description": "执行Unity编辑器菜单命令",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "menuPath": {
                            "type": "string",
                            "description": "菜单路径，如 'GameObject/Create Empty'"
                        }
                    },
                    "required": ["menuPath"]
                }
            },
            {
                "name": "unity_get_console_logs",
                "description": "获取Unity控制台日志",
                "inputSchema": {
                    "type": "object",
                    "properties": {}
                }
            }
        ]

        if MCP_AVAILABLE and self.server:
            @self.server.list_tools()
            async def list_tools():
                return self.tools

            @self.server.call_tool()
            async def call_tool(name: str, arguments: dict):
                return await self.execute_unity_command(name, arguments)

    async def connect_to_unity(self):
        """连接到Unity Editor WebSocket服务器"""
        try:
            uri = f"ws://{self.unity_host}:{self.unity_port}"
            self.websocket = await websockets.connect(uri)
            self.unity_connected = True
            print(f"✅ Connected to Unity Editor at {uri}", file=sys.stderr)
            return True
        except Exception as e:
            print(f"❌ Failed to connect to Unity Editor: {e}", file=sys.stderr)
            self.unity_connected = False
            return False

    async def send_unity_command(self, method: str, params: dict = None) -> dict:
        """向Unity发送MCP命令"""
        if not self.unity_connected or not self.websocket:
            return {
                "success": False,
                "error": "Not connected to Unity Editor. Please start Unity and open the MCP Bridge window."
            }

        try:
            message = {
                "jsonrpc": "2.0",
                "id": f"cmd_{asyncio.get_event_loop().time()}",
                "method": method,
                "params": params or {}
            }

            await self.websocket.send(json.dumps(message))
            response = await self.websocket.recv()

            result = json.loads(response)
            return result.get("result", result)

        except websockets.exceptions.ConnectionClosed:
            self.unity_connected = False
            return {
                "success": False,
                "error": "Connection to Unity Editor lost"
            }
        except Exception as e:
            return {
                "success": False,
                "error": f"Communication error: {str(e)}"
            }

    async def execute_unity_command(self, name: str, arguments: dict) -> dict:
        """执行Unity命令并返回结果"""

        # Map MCP tool names to Unity methods
        method_mapping = {
            "unity_create_scene": "unity.create_scene",
            "unity_create_gameobject": "unity.create_gameobject",
            "unity_create_ui_canvas": "unity.create_ui_canvas",
            "unity_get_scene_info": "unity.get_scene_info",
            "unity_select_gameobject": "unity.select_gameobject",
            "unity_execute_menu": "unity.execute_menu_item",
            "unity_get_console_logs": "unity.get_console_logs"
        }

        unity_method = method_mapping.get(name)
        if not unity_method:
            return {
                "content": [
                    {
                        "type": "text",
                        "text": f"❌ Unknown Unity command: {name}"
                    }
                ]
            }

        # Ensure connection to Unity
        if not self.unity_connected:
            await self.connect_to_unity()

        if not self.unity_connected:
            return {
                "content": [
                    {
                        "type": "text",
                        "text": "❌ 无法连接到Unity Editor。请确保：\n1. Unity编辑器已打开\n2. 在Unity中打开 Tools → Unity MCP → Bridge Window\n3. 点击 'Start Server' 启动WebSocket服务器"
                    }
                ]
            }

        # Execute Unity command
        result = await self.send_unity_command(unity_method, arguments)

        # Format response
        if result.get("success"):
            # Success response
            text = f"✅ {name} executed successfully\n\n"

            if name == "unity_create_scene":
                text += f"📋 Scene Name: {result.get('sceneName', 'Unknown')}\n"
                text += f"📂 Scene Path: {result.get('scenePath', 'Not saved')}"

            elif name == "unity_create_gameobject":
                text += f"🎮 GameObject: {result.get('objectName', 'Unknown')}\n"
                text += f"🆔 Instance ID: {result.get('instanceId', 'Unknown')}"

            elif name == "unity_create_ui_canvas":
                text += f"🖼️ Canvas: {result.get('canvasName', 'Canvas')}\n"
                text += f"🆔 Instance ID: {result.get('instanceId', 'Unknown')}\n"
                text += "✨ EventSystem created automatically"

            elif name == "unity_get_scene_info":
                text += f"📋 Scene: {result.get('sceneName', 'Unknown')}\n"
                text += f"📂 Path: {result.get('scenePath', 'Not saved')}\n"
                text += f"🔄 Loaded: {result.get('isLoaded', False)}\n"
                text += f"✏️ Modified: {result.get('isDirty', False)}\n\n"

                gameObjects = result.get('gameObjects', [])
                if gameObjects:
                    text += f"🎮 GameObjects ({len(gameObjects)}):\n"
                    for obj in gameObjects:
                        status = "✅" if obj.get('activeInHierarchy') else "❌"
                        text += f"{status} {obj.get('name', 'Unknown')} (ID: {obj.get('instanceId', 'Unknown')})\n"
                else:
                    text += "📝 No GameObjects in scene"

            elif name == "unity_select_gameobject":
                text += f"🎯 Selected: {result.get('objectName', 'Unknown')}"

            elif name == "unity_execute_menu":
                text += f"📋 Menu: {result.get('menuPath', 'Unknown')}"

            elif name == "unity_get_console_logs":
                logs = result.get('logs', [])
                text += f"📜 Console Logs ({len(logs)} entries):\n"
                for log in logs[-10:]:  # Show last 10 logs
                    text += f"• {log}\n"
        else:
            # Error response
            error = result.get("error", "Unknown error")
            text = f"❌ {name} failed: {error}"

        return {
            "content": [
                {
                    "type": "text",
                    "text": text
                }
            ]
        }

    async def run_standalone(self):
        """独立运行模式，用于测试"""
        print("🚀 Unity MCP Server (Standalone Mode)", file=sys.stderr)
        print("=" * 40, file=sys.stderr)

        # Try to connect to Unity
        connected = await self.connect_to_unity()
        if not connected:
            print("⚠️  Unity Editor not connected. Start Unity and MCP Bridge to test.", file=sys.stderr)

        # Interactive testing
        while True:
            try:
                print("\n📋 Available commands:")
                for i, tool in enumerate(self.tools):
                    print(f"{i+1}. {tool['name']} - {tool['description']}")
                print("0. Quit")

                choice = input("\n🎯 Choose command (number): ").strip()

                if choice == "0":
                    break

                try:
                    tool_index = int(choice) - 1
                    if 0 <= tool_index < len(self.tools):
                        tool = self.tools[tool_index]

                        # Get parameters if needed
                        params = {}
                        schema = tool.get('inputSchema', {})
                        properties = schema.get('properties', {})

                        if properties:
                            print(f"\n📝 Parameters for {tool['name']}:")
                            for prop_name, prop_info in properties.items():
                                default = prop_info.get('default', '')
                                desc = prop_info.get('description', '')
                                value = input(f"  {prop_name} ({desc}) [{default}]: ").strip()
                                if value:
                                    params[prop_name] = value
                                elif default:
                                    params[prop_name] = default

                        # Execute command
                        print(f"\n🚀 Executing {tool['name']}...")
                        result = await self.execute_unity_command(tool['name'], params)

                        # Display result
                        content = result.get('content', [])
                        if content:
                            print(f"\n📄 Result:")
                            for item in content:
                                if item.get('type') == 'text':
                                    print(item.get('text', ''))
                    else:
                        print("❌ Invalid choice")

                except ValueError:
                    print("❌ Please enter a valid number")

            except KeyboardInterrupt:
                print("\n\n👋 Goodbye!")
                break
            except Exception as e:
                print(f"\n❌ Error: {e}")

async def main():
    server = UnityMCPServer()

    if len(sys.argv) > 1 and sys.argv[1] == "--test":
        # Standalone testing mode
        await server.run_standalone()
    else:
        # MCP server mode
        if MCP_AVAILABLE and server.server:
            async with server.server:
                await server.server.run()
        else:
            print("MCP not available, running in test mode", file=sys.stderr)
            await server.run_standalone()

if __name__ == "__main__":
    asyncio.run(main())