#!/usr/bin/env node

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { WebSocket } from 'ws';
import * as z from 'zod';

// Initialize the MCP server
const server = new McpServer({
    name: "Unity Generator Server",
    version: "1.0.0"
}, {
    capabilities: {
        tools: {},
    },
});

class UnityWebSocketClient {
  constructor() {
    this.webSocket = null;
    this.wsPort = 8766;
  }

  async connect() {
    try {
      if (this.webSocket && this.webSocket.readyState === WebSocket.OPEN) {
        return true;
      }

      this.webSocket = new WebSocket(`ws://localhost:${this.wsPort}`);

      return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => {
          reject(new Error('Unity WebSocket connection timeout'));
        }, 5000);

        this.webSocket.onopen = () => {
          clearTimeout(timeout);
          console.error(`Connected to Unity WebSocket on port ${this.wsPort}`);
          resolve(true);
        };

        this.webSocket.onerror = (error) => {
          clearTimeout(timeout);
          console.error('Unity WebSocket connection error:', error);
          reject(error);
        };

        this.webSocket.onclose = (code, reason) => {
          console.error(`Unity WebSocket connection closed: ${code} - ${reason}`);
          this.webSocket = null;
        };
      });
    } catch (error) {
      console.error('Failed to connect to Unity:', error);
      throw error;
    }
  }

  async sendMessage(method, params = {}) {
    if (!this.webSocket || this.webSocket.readyState !== WebSocket.OPEN) {
      await this.connect();
    }

    const messageId = `mcp-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const message = {
      id: messageId,
      method: method,
      params: params
    };

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error(`Unity communication timeout for method: ${method}`));
      }, 10000);

      const handleMessage = (event) => {
        try {
          const response = JSON.parse(event.data);
          if (response.id === messageId) {
            clearTimeout(timeout);
            this.webSocket.removeEventListener('message', handleMessage);

            if (response.error) {
              reject(new Error(JSON.stringify(response.error)));
            } else {
              resolve(response.result || response);
            }
          }
        } catch (error) {
          clearTimeout(timeout);
          this.webSocket.removeEventListener('message', handleMessage);
          reject(error);
        }
      };

      this.webSocket.addEventListener('message', handleMessage);

      try {
        this.webSocket.send(JSON.stringify(message));
      } catch (error) {
        clearTimeout(timeout);
        this.webSocket.removeEventListener('message', handleMessage);
        reject(new Error(`Failed to send message to Unity: ${error.message}`));
      }
    });
  }
}

// Create Unity WebSocket client
const unityClient = new UnityWebSocketClient();

// Register tools using the new SDK API
server.tool('generate_unity_feature',
  '根据自然语言描述生成Unity界面和功能',
  {
    description: z.string().describe('功能描述，如"登录界面"、"背包系统"等'),
    projectPath: z.string().describe('Unity项目路径'),
  },
  async (params) => {
    try {
      // Simple feature generation simulation
      const { description, projectPath } = params;

      return {
        content: [
          {
            type: 'text',
            text: `Unity feature generation started for: ${description}\\nProject path: ${projectPath}\\nFeature would be generated based on the description.`,
          },
        ],
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: `Error generating Unity feature: ${error.message}`,
          },
        ],
      };
    }
  }
);

server.tool('list_available_templates',
  '列出所有可用的Unity模板',
  {},
  async () => {
    return {
      content: [
        {
          type: 'text',
          text: 'Available Unity templates:\\n- Login Interface (登录界面)\\n- Inventory System (背包系统)\\n- Main Menu (主菜单)\\n- Shop Interface (商店界面)',
        },
      ],
    };
  }
);

server.tool('get_unity_console_logs',
  '获取Unity编辑器控制面板输出日志',
  {
    logType: z.string().optional().describe('日志类型: all(全部), editor(编辑器), compiler(编译器), import(资源导入)'),
    lines: z.number().optional().describe('返回的日志行数'),
  },
  async (params) => {
    try {
      const result = await unityClient.sendMessage('get_console_logs', {
        logType: params.logType || '',
        offset: 0,
        limit: params.lines || 20,
        includeStackTrace: false
      });

      if (result.success && result.data) {
        const logs = result.data.logs || [];
        let logText = `🔍 Unity控制台日志 (共 ${result.data.totalCount} 条)\\n\\n`;

        if (logs.length === 0) {
          logText += "暂无日志记录";
        } else {
          logText += logs.map(log =>
            `[${log.timestamp}] ${log.type}: ${log.message}`
          ).join('\\n\\n');
        }

        return {
          content: [
            {
              type: 'text',
              text: logText,
            },
          ],
        };
      } else {
        throw new Error(result.message || 'Unknown error');
      }
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: `❌ 获取Unity控制台日志失败: ${error.message}\\n\\n请确保:\\n1. Unity编辑器已打开\\n2. Unity MCP Bridge窗口已启动 (Tools > Unity MCP > Bridge Window)\\n3. WebSocket服务器正在运行在端口 ${unityClient.wsPort}`,
          },
        ],
      };
    }
  }
);

// Server startup function
async function startServer() {
    try {
        const stdioTransport = new StdioServerTransport();
        await server.connect(stdioTransport);
        console.error('Unity MCP Server running on stdio');

        // Try to connect to Unity on startup
        try {
            await unityClient.connect();
        } catch (error) {
            console.error('Initial Unity connection failed, will retry when needed');
        }
    } catch (error) {
        console.error('Failed to start server:', error);
        process.exit(1);
    }
}

// Start the server
startServer();

// Handle shutdown
process.on('SIGINT', async () => {
    console.error('Shutting down...');
    process.exit(0);
});

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
    console.error('Uncaught exception:', error);
});

// Handle unhandled promise rejections
process.on('unhandledRejection', (reason) => {
    console.error('Unhandled rejection:', reason);
});