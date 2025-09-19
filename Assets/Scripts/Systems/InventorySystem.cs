using UnityEngine;
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
}