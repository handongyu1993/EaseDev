using UnityEngine;

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
}