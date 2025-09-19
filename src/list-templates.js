#!/usr/bin/env node

import { TemplateManager } from './templates/TemplateManager.js';

/**
 * 命令行工具：列出可用模板
 * 用法: node list-templates.js
 */
async function main() {
  try {
    const templateManager = new TemplateManager();

    // 初始化模板系统
    await templateManager.initializeTemplates();

    // 获取模板列表
    const templates = await templateManager.listTemplates();

    // 输出JSON格式结果
    console.log(JSON.stringify(templates, null, 2));

  } catch (error) {
    console.error(JSON.stringify({
      error: error.message
    }));
    process.exit(1);
  }
}

main().catch(error => {
  console.error(JSON.stringify({
    error: error.message
  }));
  process.exit(1);
});