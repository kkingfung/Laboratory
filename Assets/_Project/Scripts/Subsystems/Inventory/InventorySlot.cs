using UnityEngine;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Represents a single slot in the inventory system
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity;
        [SerializeField] private int slotIndex;
        
        public ItemData Item => item;
        public int Quantity 
        { 
            get => quantity;
            set => quantity = value;
        }
        public int SlotIndex => slotIndex;
        public bool IsEmpty => item == null || quantity <= 0;
        
        public InventorySlot(int index)
        {
            slotIndex = index;
            item = null;
            quantity = 0;
        }
        
        public InventorySlot(ItemData itemData, int qty, int index)
        {
            item = itemData;
            quantity = qty;
            slotIndex = index;
        }
        
        public bool CanAddItem(ItemData itemToAdd, int quantityToAdd)
        {
            if (IsEmpty) return true;
            if (item != itemToAdd) return false;
            
            int maxStack = item.MaxStackSize > 0 ? item.MaxStackSize : 999;
            return quantity + quantityToAdd <= maxStack;
        }
        
        public bool TryAddItem(ItemData itemToAdd, int quantityToAdd)
        {
            if (!CanAddItem(itemToAdd, quantityToAdd)) return false;
            
            if (IsEmpty)
            {
                item = itemToAdd;
                quantity = quantityToAdd;
            }
            else
            {
                quantity += quantityToAdd;
            }
            
            return true;
        }
        
        public bool TryRemoveQuantity(int quantityToRemove)
        {
            if (quantityToRemove <= 0 || quantityToRemove > quantity) return false;
            
            quantity -= quantityToRemove;
            if (quantity <= 0)
            {
                Clear();
            }
            
            return true;
        }
        
        public void Clear()
        {
            item = null;
            quantity = 0;
        }
        
        public void SetItem(ItemData newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }
    }
}
