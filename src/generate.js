#!/usr/bin/env node

import { UnityCommandParser } from './parsers/CommandParser.js';
import { TemplateManager } from './templates/TemplateManager.js';
import { UnityGenerator } from './generators/UnityGenerator.js';

/**
 * 命令行工具：生成Unity功能
 * 用法: node generate.js "我想做一个登录界面" "/path/to/unity/project"
 */
async function main() {
  const description = process.argv[2];
  const projectPath = process.argv[3];

  if (!description) {
    console.error(JSON.stringify({
      error: "缺少功能描述参数"
    }));
    process.exit(1);
  }

  if (!projectPath) {
    console.error(JSON.stringify({
      error: "缺少Unity项目路径参数"
    }));
    process.exit(1);
  }

  try {
    const parser = new UnityCommandParser();
    const templateManager = new TemplateManager();
    const generator = new UnityGenerator();

    // 初始化模板系统
    await templateManager.initializeTemplates();

    // 解析命令
    const command = await parser.parse(description);

    // 获取模板
    const template = await templateManager.getTemplate(command.type);

    // 生成Unity功能
    const result = await generator.generate({
      command: command,
      template: template,
      projectPath: projectPath
    });

    // 输出结果
    console.log(JSON.stringify({
      success: result.success,
      createdFiles: result.createdFiles,
      message: result.message || `成功生成 ${template.name}`,
      command: command,
      template: {
        name: template.name,
        description: template.description,
        category: template.category
      }
    }, null, 2));

  } catch (error) {
    console.error(JSON.stringify({
      error: error.message,
      success: false
    }));
    process.exit(1);
  }
}

main().catch(error => {
  console.error(JSON.stringify({
    error: error.message,
    success: false
  }));
  process.exit(1);
});