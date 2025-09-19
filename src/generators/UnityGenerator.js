import fs from 'fs-extra';
import path from 'path';

/**
 * Unity项目生成器
 * 负责根据模板和命令生成Unity项目文件
 */
export class UnityGenerator {
  constructor() {
    this.createdFiles = [];
  }

  /**
   * 生成Unity功能
   * @param {Object} params - 生成参数
   * @param {Object} params.command - 解析后的命令
   * @param {Object} params.template - 模板配置
   * @param {string} params.projectPath - Unity项目路径
   * @returns {Object} 生成结果
   */
  async generate({ command, template, projectPath }) {
    this.createdFiles = [];

    try {
      // 验证项目路径
      await this.validateUnityProject(projectPath);

      // 创建必要的目录结构
      await this.createDirectories(projectPath, template);

      // 生成脚本文件
      await this.generateScripts(projectPath, template, command);

      // 生成预制件和资源
      await this.generateAssets(projectPath, template, command);

      // 设置场景（如果需要）
      if (template.sceneSetup) {
        await this.setupScene(projectPath, template.sceneSetup, command);
      }

      // 更新项目配置
      await this.updateProjectSettings(projectPath, template);

      return {
        success: true,
        createdFiles: this.createdFiles,
        message: `Successfully generated ${template.name}`
      };

    } catch (error) {
      return {
        success: false,
        error: error.message,
        createdFiles: this.createdFiles
      };
    }
  }

  /**
   * 验证Unity项目
   * @param {string} projectPath - 项目路径
   */
  async validateUnityProject(projectPath) {
    const assetsPath = path.join(projectPath, 'Assets');
    const projectSettingsPath = path.join(projectPath, 'ProjectSettings');

    if (!await fs.pathExists(assetsPath)) {
      throw new Error(`Invalid Unity project: Assets folder not found at ${assetsPath}`);
    }

    if (!await fs.pathExists(projectSettingsPath)) {
      throw new Error(`Invalid Unity project: ProjectSettings folder not found at ${projectSettingsPath}`);
    }
  }

  /**
   * 创建目录结构
   * @param {string} projectPath - 项目路径
   * @param {Object} template - 模板配置
   */
  async createDirectories(projectPath, template) {
    const directories = new Set();

    // 从文件路径提取目录
    template.files?.forEach(file => {
      const dir = path.dirname(file.path);
      if (dir !== '.') {
        directories.add(path.join(projectPath, 'Assets', dir));
      }
    });

    // 创建资源目录
    if (template.resources) {
      template.resources.forEach(resource => {
        const dir = path.dirname(resource.path);
        if (dir !== '.') {
          directories.add(path.join(projectPath, 'Assets', dir));
        }
      });
    }

    // 创建所有目录
    for (const dir of directories) {
      await fs.ensureDir(dir);
      console.error(`Created directory: ${dir}`);
    }
  }

  /**
   * 生成脚本文件
   * @param {string} projectPath - 项目路径
   * @param {Object} template - 模板配置
   * @param {Object} command - 命令对象
   */
  async generateScripts(projectPath, template, command) {
    if (!template.files) return;

    for (const file of template.files) {
      if (file.type === 'script') {
        const fullPath = path.join(projectPath, 'Assets', file.path);

        // 应用命令样式和配置
        let content = this.applyCommandToTemplate(file.content, command);

        // 确保目录存在
        await fs.ensureDir(path.dirname(fullPath));

        // 写入文件
        await fs.writeFile(fullPath, content, 'utf8');

        // 生成对应的.meta文件
        await this.generateMetaFile(fullPath, 'script');

        this.createdFiles.push(file.path);
        console.error(`Generated script: ${file.path}`);
      }
    }
  }

  /**
   * 生成资源文件
   * @param {string} projectPath - 项目路径
   * @param {Object} template - 模板配置
   * @param {Object} command - 命令对象
   */
  async generateAssets(projectPath, template, command) {
    if (!template.files) return;

    for (const file of template.files) {
      if (file.type === 'prefab') {
        const fullPath = path.join(projectPath, 'Assets', file.path);

        // 应用命令样式
        let content = this.applyCommandToTemplate(file.content, command);

        await fs.ensureDir(path.dirname(fullPath));
        await fs.writeFile(fullPath, content, 'utf8');
        await this.generateMetaFile(fullPath, 'prefab');

        this.createdFiles.push(file.path);
        console.error(`Generated prefab: ${file.path}`);
      }
    }

    // 生成ScriptableObject资源
    if (template.resources) {
      for (const resource of template.resources) {
        const fullPath = path.join(projectPath, 'Assets', resource.path);

        await fs.ensureDir(path.dirname(fullPath));
        await fs.writeFile(fullPath, resource.content, 'utf8');
        await this.generateMetaFile(fullPath, resource.type);

        this.createdFiles.push(resource.path);
        console.error(`Generated resource: ${resource.path}`);
      }
    }
  }

  /**
   * 设置场景
   * @param {string} projectPath - 项目路径
   * @param {Object} sceneSetup - 场景设置
   * @param {Object} command - 命令对象
   */
  async setupScene(projectPath, sceneSetup, command) {
    // 这里可以生成场景文件或修改现有场景
    // 暂时创建一个简单的场景配置文件
    if (sceneSetup.createCanvas) {
      const sceneConfigPath = path.join(projectPath, 'Assets', 'SceneConfigs');
      await fs.ensureDir(sceneConfigPath);

      const canvasConfig = {
        renderMode: sceneSetup.canvasSettings?.renderMode || 'ScreenSpaceOverlay',
        scaler: sceneSetup.canvasSettings?.scaler || 'ScaleWithScreenSize',
        referenceResolution: { x: 1920, y: 1080 },
        matchMode: 'MatchWidthOrHeight'
      };

      const configPath = path.join(sceneConfigPath, 'CanvasConfig.json');
      await fs.writeJSON(configPath, canvasConfig, { spaces: 2 });
      this.createdFiles.push('SceneConfigs/CanvasConfig.json');

      console.error('Generated canvas configuration');
    }
  }

  /**
   * 更新项目设置
   * @param {string} projectPath - 项目路径
   * @param {Object} template - 模板配置
   */
  async updateProjectSettings(projectPath, template) {
    // 如果模板需要特定的依赖，可以在这里处理
    if (template.dependencies && template.dependencies.length > 0) {
      console.error(`Template requires dependencies: ${template.dependencies.join(', ')}`);
    }
  }

  /**
   * 应用命令到模板
   * @param {string} template - 模板内容
   * @param {Object} command - 命令对象
   * @returns {string} 处理后的内容
   */
  applyCommandToTemplate(template, command) {
    let content = template;

    // 应用样式修改
    if (command.style) {
      // 颜色相关的替换
      if (command.style.color) {
        const colorMap = {
          '红色': '#FF0000',
          'red': '#FF0000',
          '蓝色': '#0000FF',
          'blue': '#0000FF',
          '绿色': '#00FF00',
          'green': '#00FF00'
        };

        command.style.color.forEach(color => {
          if (colorMap[color]) {
            content = content.replace(/Color\.white/g, `new Color(${this.hexToRgb(colorMap[color])})`);
          }
        });
      }

      // 尺寸相关的替换
      if (command.style.size) {
        command.style.size.forEach(size => {
          switch (size) {
            case '大':
            case 'large':
              content = content.replace(/fontSize = 14/g, 'fontSize = 24');
              break;
            case '小':
            case 'small':
              content = content.replace(/fontSize = 14/g, 'fontSize = 10');
              break;
          }
        });
      }
    }

    // 应用配置修改
    if (command.config) {
      // 位置相关的替换
      if (command.config.position) {
        command.config.position.forEach(pos => {
          switch (pos) {
            case '左':
            case 'left':
              content = content.replace(/TextAnchor\.MiddleCenter/g, 'TextAnchor.MiddleLeft');
              break;
            case '右':
            case 'right':
              content = content.replace(/TextAnchor\.MiddleCenter/g, 'TextAnchor.MiddleRight');
              break;
          }
        });
      }

      // 布局相关的替换
      if (command.config.layout) {
        switch (command.config.layout) {
          case '竖直':
          case 'vertical':
            content = content.replace(/LayoutGroup/g, 'VerticalLayoutGroup');
            break;
          case '水平':
          case 'horizontal':
            content = content.replace(/LayoutGroup/g, 'HorizontalLayoutGroup');
            break;
          case '网格':
          case 'grid':
            content = content.replace(/LayoutGroup/g, 'GridLayoutGroup');
            break;
        }
      }
    }

    return content;
  }

  /**
   * 生成Unity .meta文件
   * @param {string} filePath - 文件路径
   * @param {string} type - 文件类型
   */
  async generateMetaFile(filePath, type) {
    const metaPath = filePath + '.meta';
    const guid = this.generateGUID();

    let metaContent;
    switch (type) {
      case 'script':
        metaContent = this.getScriptMetaTemplate(guid);
        break;
      case 'prefab':
        metaContent = this.getPrefabMetaTemplate(guid);
        break;
      case 'scriptableobject':
        metaContent = this.getScriptableObjectMetaTemplate(guid);
        break;
      default:
        metaContent = this.getDefaultMetaTemplate(guid);
    }

    await fs.writeFile(metaPath, metaContent, 'utf8');
  }

  /**
   * 生成Unity GUID
   * @returns {string} GUID字符串
   */
  generateGUID() {
    return 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'.replace(/[x]/g, function() {
      return (Math.random() * 16 | 0).toString(16);
    });
  }

  /**
   * 脚本meta文件模板
   * @param {string} guid - GUID
   * @returns {string} meta文件内容
   */
  getScriptMetaTemplate(guid) {
    return `fileFormatVersion: 2
guid: ${guid}
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant: `;
  }

  /**
   * 预制件meta文件模板
   * @param {string} guid - GUID
   * @returns {string} meta文件内容
   */
  getPrefabMetaTemplate(guid) {
    return `fileFormatVersion: 2
guid: ${guid}
PrefabImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant: `;
  }

  /**
   * ScriptableObject meta文件模板
   * @param {string} guid - GUID
   * @returns {string} meta文件内容
   */
  getScriptableObjectMetaTemplate(guid) {
    return `fileFormatVersion: 2
guid: ${guid}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant: `;
  }

  /**
   * 默认meta文件模板
   * @param {string} guid - GUID
   * @returns {string} meta文件内容
   */
  getDefaultMetaTemplate(guid) {
    return `fileFormatVersion: 2
guid: ${guid}
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant: `;
  }

  /**
   * 将十六进制颜色转换为Unity Color
   * @param {string} hex - 十六进制颜色
   * @returns {string} Unity Color字符串
   */
  hexToRgb(hex) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (result) {
      const r = parseInt(result[1], 16) / 255;
      const g = parseInt(result[2], 16) / 255;
      const b = parseInt(result[3], 16) / 255;
      return `${r}f, ${g}f, ${b}f, 1f`;
    }
    return '1f, 1f, 1f, 1f';
  }
}