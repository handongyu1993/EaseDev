import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Unity模板管理器
 * 负责管理和提供各种Unity功能的模板配置
 */
export class TemplateManager {
  constructor() {
    this.templatesDir = path.join(__dirname, 'data');
    this.initializeTemplates();
  }

  /**
   * 初始化模板系统
   */
  async initializeTemplates() {
    await fs.ensureDir(this.templatesDir);
    await this.createDefaultTemplates();
  }

  /**
   * 创建默认模板
   */
  async createDefaultTemplates() {
    const defaultTemplates = {
      // 登录界面模板
      login_ui: {
        name: 'Login UI',
        description: '登录界面模板',
        category: 'ui',
        files: [
          {
            path: 'UI/LoginPanel.prefab',
            type: 'prefab',
            content: this.getLoginUITemplate()
          },
          {
            path: 'Scripts/UI/LoginManager.cs',
            type: 'script',
            content: this.getLoginScriptTemplate()
          }
        ],
        dependencies: ['Unity.UI', 'Unity.Events'],
        sceneSetup: {
          createCanvas: true,
          canvasSettings: {
            renderMode: 'ScreenSpaceOverlay',
            scaler: 'ScaleWithScreenSize'
          }
        }
      },

      // 背包界面模板
      inventory_ui: {
        name: 'Inventory UI',
        description: '背包界面模板',
        category: 'ui',
        files: [
          {
            path: 'UI/InventoryPanel.prefab',
            type: 'prefab',
            content: this.getInventoryUITemplate()
          },
          {
            path: 'Scripts/UI/InventoryManager.cs',
            type: 'script',
            content: this.getInventoryScriptTemplate()
          },
          {
            path: 'Scripts/Items/Item.cs',
            type: 'script',
            content: this.getItemScriptTemplate()
          }
        ],
        dependencies: ['Unity.UI'],
        sceneSetup: {
          createCanvas: true
        }
      },

      // 背包系统模板
      inventory_system: {
        name: 'Inventory System',
        description: '完整的背包系统',
        category: 'system',
        files: [
          {
            path: 'Scripts/Systems/InventorySystem.cs',
            type: 'script',
            content: this.getInventorySystemTemplate()
          },
          {
            path: 'Scripts/Items/ItemData.cs',
            type: 'script',
            content: this.getItemDataTemplate()
          },
          {
            path: 'Scripts/Items/ItemDatabase.cs',
            type: 'script',
            content: this.getItemDatabaseTemplate()
          }
        ],
        dependencies: ['System.Serializable'],
        resources: [
          {
            path: 'Resources/ItemDatabase.asset',
            type: 'scriptableobject',
            content: '{}'
          }
        ]
      },

      // 主菜单界面模板
      main_menu_ui: {
        name: 'Main Menu UI',
        description: '主菜单界面',
        category: 'ui',
        files: [
          {
            path: 'UI/MainMenuPanel.prefab',
            type: 'prefab',
            content: this.getMainMenuUITemplate()
          },
          {
            path: 'Scripts/UI/MainMenuManager.cs',
            type: 'script',
            content: this.getMainMenuScriptTemplate()
          }
        ],
        dependencies: ['Unity.UI', 'UnityEngine.SceneManagement'],
        sceneSetup: {
          createCanvas: true,
          createEventSystem: true
        }
      }
    };

    // 保存默认模板到文件
    for (const [key, template] of Object.entries(defaultTemplates)) {
      const templatePath = path.join(this.templatesDir, `${key}.json`);
      await fs.writeJSON(templatePath, template, { spaces: 2 });
    }
  }

  /**
   * 获取模板
   * @param {string} templateType - 模板类型
   * @returns {Object} 模板对象
   */
  async getTemplate(templateType) {
    const templatePath = path.join(this.templatesDir, `${templateType}.json`);

    if (await fs.pathExists(templatePath)) {
      return await fs.readJSON(templatePath);
    }

    throw new Error(`Template not found: ${templateType}`);
  }

  /**
   * 列出所有可用模板
   * @returns {Array} 模板列表
   */
  async listTemplates() {
    const files = await fs.readdir(this.templatesDir);
    const templates = [];

    for (const file of files) {
      if (path.extname(file) === '.json') {
        const templatePath = path.join(this.templatesDir, file);
        const template = await fs.readJSON(templatePath);
        templates.push({
          id: path.basename(file, '.json'),
          name: template.name,
          description: template.description,
          category: template.category
        });
      }
    }

    return templates;
  }

  /**
   * 创建自定义模板
   * @param {string} name - 模板名称
   * @param {Object} templateData - 模板数据
   * @returns {Object} 创建结果
   */
  async createTemplate(name, templateData) {
    const templatePath = path.join(this.templatesDir, `${name}.json`);
    await fs.writeJSON(templatePath, templateData, { spaces: 2 });

    return {
      path: templatePath,
      success: true
    };
  }

  // ========== 模板内容生成方法 ==========

  getLoginUITemplate() {
    return `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1234567890123456789
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1234567890123456790}
  - component: {fileID: 1234567890123456791}
  - component: {fileID: 1234567890123456792}
  m_Layer: 5
  m_Name: LoginPanel
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1`;
  }

  getLoginScriptTemplate() {
    return `using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace EaseDev.UI
{
    public class LoginManager : MonoBehaviour
    {
        [Header("UI References")]
        public InputField usernameInput;
        public InputField passwordInput;
        public Button loginButton;
        public Button registerButton;
        public Text messageText;

        [Header("Events")]
        public UnityEvent<string, string> OnLoginAttempt;
        public UnityEvent OnRegisterClicked;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 绑定按钮事件
            loginButton.onClick.AddListener(OnLoginButtonClicked);
            registerButton.onClick.AddListener(OnRegisterButtonClicked);

            // 设置初始状态
            messageText.text = "";
            usernameInput.text = "";
            passwordInput.text = "";

            // 添加输入验证
            usernameInput.onValueChanged.AddListener(OnInputChanged);
            passwordInput.onValueChanged.AddListener(OnInputChanged);
        }

        private void OnLoginButtonClicked()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();

            if (ValidateInput(username, password))
            {
                OnLoginAttempt?.Invoke(username, password);
                ShowMessage("正在登录...", Color.blue);
            }
        }

        private void OnRegisterButtonClicked()
        {
            OnRegisterClicked?.Invoke();
        }

        private bool ValidateInput(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                ShowMessage("用户名不能为空", Color.red);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("密码不能为空", Color.red);
                return false;
            }

            if (password.Length < 6)
            {
                ShowMessage("密码长度至少6位", Color.red);
                return false;
            }

            return true;
        }

        private void OnInputChanged(string value)
        {
            // 清除错误信息
            if (messageText.color == Color.red)
            {
                messageText.text = "";
            }
        }

        public void ShowMessage(string message, Color color)
        {
            messageText.text = message;
            messageText.color = color;
        }

        public void OnLoginSuccess()
        {
            ShowMessage("登录成功！", Color.green);
        }

        public void OnLoginFailed(string error)
        {
            ShowMessage($"登录失败: {error}", Color.red);
        }
    }
}`;
  }

  getInventoryUITemplate() {
    return `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1234567890123456789
GameObject:
  m_ObjectHideFlags: 0
  m_Name: InventoryPanel
  m_Component:
  - component: {fileID: 1234567890123456790}
  - component: {fileID: 1234567890123456791}
  m_Layer: 5
  m_TagString: Untagged`;
  }

  getInventoryScriptTemplate() {
    return `using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace EaseDev.UI
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("UI References")]
        public Transform itemGrid;
        public GameObject itemSlotPrefab;
        public Text itemDescriptionText;
        public Image itemIconImage;

        [Header("Settings")]
        public int inventorySize = 20;

        private List<GameObject> itemSlots = new List<GameObject>();
        private Item selectedItem;

        private void Start()
        {
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            // 创建背包格子
            for (int i = 0; i < inventorySize; i++)
            {
                GameObject slot = Instantiate(itemSlotPrefab, itemGrid);
                slot.name = $"ItemSlot_{i}";
                itemSlots.Add(slot);

                // 为每个格子添加点击事件
                Button slotButton = slot.GetComponent<Button>();
                if (slotButton != null)
                {
                    int index = i; // 捕获局部变量
                    slotButton.onClick.AddListener(() => OnSlotClicked(index));
                }
            }

            ClearSelection();
        }

        private void OnSlotClicked(int slotIndex)
        {
            // 处理格子点击逻辑
            Debug.Log($"Clicked slot {slotIndex}");
        }

        public void AddItem(Item item, int quantity = 1)
        {
            // 查找空的或相同物品的格子
            for (int i = 0; i < itemSlots.Count; i++)
            {
                // 实现添加物品逻辑
            }
        }

        public void RemoveItem(int slotIndex, int quantity = 1)
        {
            // 实现移除物品逻辑
        }

        private void ClearSelection()
        {
            selectedItem = null;
            itemDescriptionText.text = "选择一个物品查看详情";
            itemIconImage.sprite = null;
            itemIconImage.color = new Color(1, 1, 1, 0);
        }
    }
}`;
  }

  getItemScriptTemplate() {
    return `using UnityEngine;

namespace EaseDev.Items
{
    [System.Serializable]
    public class Item
    {
        public int id;
        public string itemName;
        public string description;
        public Sprite icon;
        public ItemType type;
        public int maxStackSize = 1;
        public int value;

        public Item(int id, string name, string description, ItemType type)
        {
            this.id = id;
            this.itemName = name;
            this.description = description;
            this.type = type;
        }
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material,
        Quest,
        Misc
    }
}`;
  }

  getInventorySystemTemplate() {
    return `using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EaseDev.Systems
{
    [System.Serializable]
    public class InventorySystem : MonoBehaviour
    {
        [Header("Settings")]
        public int maxSlots = 20;

        private List<ItemSlot> slots = new List<ItemSlot>();

        public static InventorySystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSlots();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new ItemSlot());
            }
        }

        public bool AddItem(Item item, int quantity = 1)
        {
            // 尝试堆叠到现有物品
            var existingSlot = slots.FirstOrDefault(slot =>
                !slot.IsEmpty &&
                slot.Item.id == item.id &&
                slot.Quantity < item.maxStackSize);

            if (existingSlot != null)
            {
                int canAdd = Mathf.Min(quantity, item.maxStackSize - existingSlot.Quantity);
                existingSlot.Quantity += canAdd;
                quantity -= canAdd;

                if (quantity <= 0)
                    return true;
            }

            // 寻找空槽位
            var emptySlot = slots.FirstOrDefault(slot => slot.IsEmpty);
            if (emptySlot != null && quantity > 0)
            {
                emptySlot.SetItem(item, quantity);
                return true;
            }

            return false; // 背包已满
        }

        public bool RemoveItem(int itemId, int quantity = 1)
        {
            var slot = slots.FirstOrDefault(s => !s.IsEmpty && s.Item.id == itemId);
            if (slot != null)
            {
                slot.Quantity -= quantity;
                if (slot.Quantity <= 0)
                {
                    slot.Clear();
                }
                return true;
            }
            return false;
        }

        public int GetItemCount(int itemId)
        {
            return slots.Where(s => !s.IsEmpty && s.Item.id == itemId)
                       .Sum(s => s.Quantity);
        }

        [System.Serializable]
        public class ItemSlot
        {
            public Item Item { get; private set; }
            public int Quantity { get; set; }
            public bool IsEmpty => Item == null;

            public void SetItem(Item item, int quantity)
            {
                Item = item;
                Quantity = quantity;
            }

            public void Clear()
            {
                Item = null;
                Quantity = 0;
            }
        }
    }
}`;
  }

  getItemDataTemplate() {
    return `using UnityEngine;

namespace EaseDev.Items
{
    [CreateAssetMenu(fileName = "New Item", menuName = "EaseDev/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public int id;
        public string itemName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;

        [Header("Properties")]
        public ItemType type;
        public int maxStackSize = 1;
        public int value;
        public bool isConsumable;

        [Header("Stats")]
        public int damage;
        public int defense;
        public int healAmount;

        public Item CreateItem()
        {
            return new Item(id, itemName, description, type)
            {
                icon = this.icon,
                maxStackSize = this.maxStackSize,
                value = this.value
            };
        }
    }
}`;
  }

  getItemDatabaseTemplate() {
    return `using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EaseDev.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "EaseDev/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Header("Items")]
        public List<ItemData> items = new List<ItemData>();

        public static ItemDatabase Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
        }

        public ItemData GetItem(int id)
        {
            return items.FirstOrDefault(item => item.id == id);
        }

        public ItemData GetItem(string itemName)
        {
            return items.FirstOrDefault(item => item.itemName == itemName);
        }

        public List<ItemData> GetItemsByType(ItemType type)
        {
            return items.Where(item => item.type == type).ToList();
        }

        public void AddItem(ItemData item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
            }
        }

        public bool RemoveItem(int id)
        {
            var item = GetItem(id);
            if (item != null)
            {
                items.Remove(item);
                return true;
            }
            return false;
        }
    }
}`;
  }

  getMainMenuUITemplate() {
    return `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1234567890123456789
GameObject:
  m_ObjectHideFlags: 0
  m_Name: MainMenuPanel
  m_Component:
  - component: {fileID: 1234567890123456790}
  m_Layer: 5
  m_TagString: Untagged`;
  }

  getMainMenuScriptTemplate() {
    return `using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EaseDev.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        public Button startGameButton;
        public Button settingsButton;
        public Button creditsButton;
        public Button quitButton;

        [Header("Panels")]
        public GameObject settingsPanel;
        public GameObject creditsPanel;

        [Header("Settings")]
        public string gameSceneName = "GameScene";

        private void Start()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            // 绑定按钮事件
            startGameButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);
            creditsButton.onClick.AddListener(OpenCredits);
            quitButton.onClick.AddListener(QuitGame);

            // 初始化面板状态
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void StartGame()
        {
            Debug.Log("Starting game...");
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings()
        {
            Debug.Log("Opening settings...");
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void OpenCredits()
        {
            Debug.Log("Opening credits...");
            if (creditsPanel != null)
                creditsPanel.SetActive(true);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void ClosePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}`;
  }
}