/**
 * Unity命令解析器
 * 解析自然语言命令并转换为结构化的Unity生成指令
 */
export class UnityCommandParser {
  constructor() {
    // 预定义的功能模式匹配
    this.patterns = {
      // UI界面相关
      ui: [
        { keywords: ['登录', '登陆', 'login'], type: 'login_ui', category: 'ui' },
        { keywords: ['注册', 'register', '注册页面'], type: 'register_ui', category: 'ui' },
        { keywords: ['主菜单', 'main menu', '主界面'], type: 'main_menu_ui', category: 'ui' },
        { keywords: ['设置', 'settings', '设置页面'], type: 'settings_ui', category: 'ui' },
        { keywords: ['背包', 'inventory', '物品栏'], type: 'inventory_ui', category: 'ui' },
        { keywords: ['商店', 'shop', 'store'], type: 'shop_ui', category: 'ui' },
        { keywords: ['聊天', 'chat', '聊天框'], type: 'chat_ui', category: 'ui' },
        { keywords: ['血条', 'health bar', 'hp'], type: 'health_bar_ui', category: 'ui' },
        { keywords: ['小地图', 'minimap', '地图'], type: 'minimap_ui', category: 'ui' },
      ],

      // 游戏系统相关
      system: [
        { keywords: ['背包系统', 'inventory system'], type: 'inventory_system', category: 'system' },
        { keywords: ['战斗系统', 'combat system', '战斗'], type: 'combat_system', category: 'system' },
        { keywords: ['商店系统', 'shop system'], type: 'shop_system', category: 'system' },
        { keywords: ['任务系统', 'quest system', '任务'], type: 'quest_system', category: 'system' },
        { keywords: ['存档系统', 'save system', '保存'], type: 'save_system', category: 'system' },
        { keywords: ['音效系统', 'audio system', '音频'], type: 'audio_system', category: 'system' },
        { keywords: ['输入系统', 'input system', '控制'], type: 'input_system', category: 'system' },
        { keywords: ['角色控制', 'player controller'], type: 'player_controller', category: 'system' },
      ],

      // 场景相关
      scene: [
        { keywords: ['游戏场景', 'game scene', '关卡'], type: 'game_scene', category: 'scene' },
        { keywords: ['菜单场景', 'menu scene'], type: 'menu_scene', category: 'scene' },
        { keywords: ['加载场景', 'loading scene'], type: 'loading_scene', category: 'scene' },
      ],

      // 特效相关
      effects: [
        { keywords: ['粒子特效', 'particle effect'], type: 'particle_effect', category: 'effects' },
        { keywords: ['音效', 'sound effect'], type: 'sound_effect', category: 'effects' },
        { keywords: ['动画', 'animation'], type: 'animation', category: 'effects' },
      ]
    };

    // 样式和风格关键词
    this.styleKeywords = {
      style: ['简约', 'simple', '现代', 'modern', '科幻', 'sci-fi', '卡通', 'cartoon', '暗色', 'dark', '亮色', 'light'],
      color: ['红色', 'red', '蓝色', 'blue', '绿色', 'green', '黄色', 'yellow', '紫色', 'purple', '黑色', 'black', '白色', 'white'],
      size: ['小', 'small', '中', 'medium', '大', 'large', '迷你', 'mini', '巨大', 'huge']
    };
  }

  /**
   * 解析用户输入的命令
   * @param {string} command - 用户输入的自然语言命令
   * @returns {Object} 解析后的命令对象
   */
  async parse(command) {
    const lowerCommand = command.toLowerCase();

    // 匹配功能类型
    const matchedFeature = this.matchFeature(lowerCommand);

    // 提取样式信息
    const styleInfo = this.extractStyleInfo(lowerCommand);

    // 提取配置参数
    const config = this.extractConfig(lowerCommand);

    const result = {
      originalCommand: command,
      type: matchedFeature.type,
      category: matchedFeature.category,
      confidence: matchedFeature.confidence,
      style: styleInfo,
      config: config,
      timestamp: new Date().toISOString()
    };

    console.error('Parsed command:', result);
    return result;
  }

  /**
   * 匹配功能类型
   * @param {string} command - 小写命令字符串
   * @returns {Object} 匹配结果
   */
  matchFeature(command) {
    let bestMatch = { type: 'unknown', category: 'unknown', confidence: 0 };

    // 遍历所有模式
    for (const category in this.patterns) {
      for (const pattern of this.patterns[category]) {
        let matchScore = 0;
        let keywordCount = 0;

        for (const keyword of pattern.keywords) {
          if (command.includes(keyword)) {
            keywordCount++;
            // 精确匹配得分更高
            matchScore += keyword.length === command.trim().length ? 2 : 1;
          }
        }

        if (keywordCount > 0) {
          const confidence = (matchScore / pattern.keywords.length) * (keywordCount / pattern.keywords.length);
          if (confidence > bestMatch.confidence) {
            bestMatch = {
              type: pattern.type,
              category: pattern.category,
              confidence: confidence
            };
          }
        }
      }
    }

    return bestMatch;
  }

  /**
   * 提取样式信息
   * @param {string} command - 命令字符串
   * @returns {Object} 样式信息对象
   */
  extractStyleInfo(command) {
    const style = {};

    // 提取样式关键词
    for (const styleType in this.styleKeywords) {
      for (const keyword of this.styleKeywords[styleType]) {
        if (command.includes(keyword)) {
          if (!style[styleType]) {
            style[styleType] = [];
          }
          style[styleType].push(keyword);
        }
      }
    }

    return style;
  }

  /**
   * 提取配置参数
   * @param {string} command - 命令字符串
   * @returns {Object} 配置对象
   */
  extractConfig(command) {
    const config = {};

    // 提取数字参数
    const numberMatches = command.match(/(\d+)/g);
    if (numberMatches) {
      config.numbers = numberMatches.map(Number);
    }

    // 提取位置信息
    const positionKeywords = ['左', 'left', '右', 'right', '上', 'top', '下', 'bottom', '中间', 'center'];
    for (const pos of positionKeywords) {
      if (command.includes(pos)) {
        if (!config.position) {
          config.position = [];
        }
        config.position.push(pos);
      }
    }

    // 提取布局信息
    const layoutKeywords = ['竖直', 'vertical', '水平', 'horizontal', '网格', 'grid'];
    for (const layout of layoutKeywords) {
      if (command.includes(layout)) {
        config.layout = layout;
        break;
      }
    }

    return config;
  }

  /**
   * 添加自定义模式
   * @param {string} category - 类别
   * @param {Object} pattern - 模式对象
   */
  addCustomPattern(category, pattern) {
    if (!this.patterns[category]) {
      this.patterns[category] = [];
    }
    this.patterns[category].push(pattern);
  }

  /**
   * 获取所有支持的功能类型
   * @returns {Array} 功能类型数组
   */
  getSupportedFeatures() {
    const features = [];

    for (const category in this.patterns) {
      for (const pattern of this.patterns[category]) {
        features.push({
          type: pattern.type,
          category: pattern.category,
          keywords: pattern.keywords,
          description: this.getFeatureDescription(pattern.type)
        });
      }
    }

    return features;
  }

  /**
   * 获取功能描述
   * @param {string} type - 功能类型
   * @returns {string} 功能描述
   */
  getFeatureDescription(type) {
    const descriptions = {
      'login_ui': '创建登录界面，包含用户名密码输入框和登录按钮',
      'register_ui': '创建注册界面，包含用户信息输入和验证',
      'main_menu_ui': '创建主菜单界面，包含游戏开始、设置等选项',
      'settings_ui': '创建设置界面，包含音量、画质等配置选项',
      'inventory_ui': '创建背包界面，包含物品网格和详情显示',
      'shop_ui': '创建商店界面，包含商品列表和购买功能',
      'inventory_system': '创建完整的背包系统，包含物品管理逻辑',
      'combat_system': '创建战斗系统，包含伤害计算和技能释放',
      'save_system': '创建存档系统，支持游戏进度保存和读取',
      'game_scene': '创建游戏场景，包含基础环境和摄像机设置'
    };

    return descriptions[type] || '暂无描述';
  }
}