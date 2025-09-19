#!/usr/bin/env node

import { UnityCommandParser } from './src/parsers/CommandParser.js';
import { TemplateManager } from './src/templates/TemplateManager.js';
import { UnityGenerator } from './src/generators/UnityGenerator.js';

/**
 * Unity MCP 工具测试脚本
 * 演示如何使用MCP工具生成Unity功能
 */
async function testUnityMCPTool() {
  console.log('🚀 Unity MCP Generator Test');
  console.log('============================\n');

  const parser = new UnityCommandParser();
  const templateManager = new TemplateManager();
  const generator = new UnityGenerator();

  // 初始化模板系统
  console.log('📦 Initializing template system...');
  await templateManager.initializeTemplates();

  // 测试命令解析
  console.log('🔍 Testing command parsing...\n');

  const testCommands = [
    '我想做一个登录界面',
    '我想做一个红色的背包界面',
    '我想做一个竖直布局的主菜单',
    '我想做一个背包系统',
    'I want to create a shop UI',
    '我想做一个大号字体的设置页面'
  ];

  for (const command of testCommands) {
    console.log(`📝 Command: "${command}"`);
    const parsed = await parser.parse(command);
    console.log(`   Type: ${parsed.type}`);
    console.log(`   Category: ${parsed.category}`);
    console.log(`   Confidence: ${(parsed.confidence * 100).toFixed(1)}%`);
    if (parsed.style && Object.keys(parsed.style).length > 0) {
      console.log(`   Style: ${JSON.stringify(parsed.style)}`);
    }
    if (parsed.config && Object.keys(parsed.config).length > 0) {
      console.log(`   Config: ${JSON.stringify(parsed.config)}`);
    }
    console.log('');
  }

  // 测试模板列表
  console.log('📋 Available templates:');
  const templates = await templateManager.listTemplates();
  templates.forEach(template => {
    console.log(`   - ${template.name} (${template.id}): ${template.description}`);
  });

  console.log('\n🎯 Simulated Generation Test');
  console.log('=============================\n');

  // 模拟生成过程（不实际创建文件）
  const testCommand = await parser.parse('我想做一个登录界面');
  console.log(`🔨 Simulating generation for: "${testCommand.originalCommand}"`);

  try {
    const template = await templateManager.getTemplate(testCommand.type);
    console.log(`✅ Template found: ${template.name}`);
    console.log(`📁 Files to be generated:`);
    template.files?.forEach(file => {
      console.log(`   - ${file.path} (${file.type})`);
    });

    if (template.resources) {
      console.log(`📦 Resources to be generated:`);
      template.resources.forEach(resource => {
        console.log(`   - ${resource.path} (${resource.type})`);
      });
    }

    console.log(`🔧 Dependencies required: ${template.dependencies?.join(', ') || 'None'}`);

  } catch (error) {
    console.log(`❌ Error: ${error.message}`);
  }

  console.log('\n✅ Test completed successfully!');
  console.log('\n📚 To use this tool:');
  console.log('1. Configure Claude Desktop (see claude_custom_instructions.md)');
  console.log('2. Start the MCP server: npm start');
  console.log('3. Use commands in Claude like:');
  console.log('   "Use the unity-generator tool to create a login interface"');
}

// 运行测试
testUnityMCPTool().catch(console.error);