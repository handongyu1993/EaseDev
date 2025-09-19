using UnityEngine;
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
}