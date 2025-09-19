#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ErrorCode,
  ListToolsRequestSchema,
  McpError,
} from '@modelcontextprotocol/sdk/types.js';

import { UnityCommandParser } from './parsers/CommandParser.js';
import { UnityGenerator } from './generators/UnityGenerator.js';
import { TemplateManager } from './templates/TemplateManager.js';

class UnityMCPServer {
  constructor() {
    this.server = new Server(
      {
        name: 'unity-generator',
        version: '1.0.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.commandParser = new UnityCommandParser();
    this.unityGenerator = new UnityGenerator();
    this.templateManager = new TemplateManager();

    this.setupToolHandlers();
  }

  setupToolHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      return {
        tools: [
          {
            name: 'generate_unity_feature',
            description: '根据自然语言描述生成Unity界面和功能',
            inputSchema: {
              type: 'object',
              properties: {
                description: {
                  type: 'string',
                  description: '功能描述，如"登录界面"、"背包系统"等',
                },
                projectPath: {
                  type: 'string',
                  description: 'Unity项目路径',
                },
              },
              required: ['description', 'projectPath'],
            },
          },
          {
            name: 'list_available_templates',
            description: '列出所有可用的Unity模板',
            inputSchema: {
              type: 'object',
              properties: {},
            },
          },
          {
            name: 'create_custom_template',
            description: '创建自定义Unity模板',
            inputSchema: {
              type: 'object',
              properties: {
                templateName: {
                  type: 'string',
                  description: '模板名称',
                },
                templateData: {
                  type: 'object',
                  description: '模板数据配置',
                },
              },
              required: ['templateName', 'templateData'],
            },
          },
        ],
      };
    });

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        switch (name) {
          case 'generate_unity_feature':
            return await this.handleGenerateFeature(args);

          case 'list_available_templates':
            return await this.handleListTemplates();

          case 'create_custom_template':
            return await this.handleCreateTemplate(args);

          default:
            throw new McpError(
              ErrorCode.MethodNotFound,
              `Unknown tool: ${name}`
            );
        }
      } catch (error) {
        throw new McpError(
          ErrorCode.InternalError,
          `Error executing tool ${name}: ${error.message}`
        );
      }
    });
  }

  async handleGenerateFeature(args) {
    const { description, projectPath } = args;

    // 1. 解析命令
    const parsedCommand = await this.commandParser.parse(description);

    // 2. 获取对应模板
    const template = await this.templateManager.getTemplate(parsedCommand.type);

    // 3. 生成Unity资源
    const result = await this.unityGenerator.generate({
      command: parsedCommand,
      template: template,
      projectPath: projectPath
    });

    return {
      content: [
        {
          type: 'text',
          text: `Successfully generated Unity feature: ${description}\\n\\nFiles created:\\n${result.createdFiles.join('\\n')}`,
        },
      ],
    };
  }

  async handleListTemplates() {
    const templates = await this.templateManager.listTemplates();

    return {
      content: [
        {
          type: 'text',
          text: `Available Unity templates:\\n${templates.map(t => `- ${t.name}: ${t.description}`).join('\\n')}`,
        },
      ],
    };
  }

  async handleCreateTemplate(args) {
    const { templateName, templateData } = args;

    const result = await this.templateManager.createTemplate(templateName, templateData);

    return {
      content: [
        {
          type: 'text',
          text: `Template "${templateName}" created successfully at: ${result.path}`,
        },
      ],
    };
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('Unity MCP Server running on stdio');
  }
}

const server = new UnityMCPServer();
server.run().catch(console.error);