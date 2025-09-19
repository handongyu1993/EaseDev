#!/usr/bin/env node

import { TemplateManager } from './templates/TemplateManager.js';

/**
 * 命令行工具：创建自定义模板
 * 用法: node create-template.js "template_name" '{"name": "Custom", "description": "Custom template"}'
 */
async function main() {
  const templateName = process.argv[2];
  const templateDataStr = process.argv[3];

  if (!templateName) {
    console.error(JSON.stringify({
      error: "缺少模板名称参数"
    }));
    process.exit(1);
  }

  if (!templateDataStr) {
    console.error(JSON.stringify({
      error: "缺少模板数据参数"
    }));
    process.exit(1);
  }

  try {
    const templateManager = new TemplateManager();
    const templateData = JSON.parse(templateDataStr);

    // 创建模板
    const result = await templateManager.createTemplate(templateName, templateData);

    // 输出结果
    console.log(JSON.stringify({
      success: result.success,
      path: result.path,
      message: `模板 '${templateName}' 创建成功`
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