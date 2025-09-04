using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Represents a single inventory item with basic properties.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public string name = "";
        public Sprite icon;
        public int quantity = 1;
        public Laboratory.Gameplay.Inventory.ItemData itemData;
        public int slotIndex = -1;
        
        /// <summary>
        /// Gets the item data associated with this inventory item.
        /// </summary>
        public Laboratory.Gameplay.Inventory.ItemData ItemData => itemData;
        
        /// <summary>
        /// Gets the current quantity of this item.
        /// </summary>
        public int Quantity
        {
            get => quantity;
            set => quantity = Mathf.Max(0, value);
        }
        
        /// <summary>
        /// Gets the slot index where this item is stored.
        /// </summary>
        public int SlotIndex
        {
            get => slotIndex;
            set => slotIndex = value;
        }
        
        /// <summary>
        /// Constructor for creating an inventory item from item data.
        /// </summary>
        public InventoryItem(Laboratory.Gameplay.Inventory.ItemData data, int qty = 1, int slot = -1)
        {
            itemData = data;
            name = data?.ItemName ?? "";
            icon = data?.Icon;
            quantity = qty;
            slotIndex = slot;
        }
        
        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public InventoryItem() { }
    }

    /// <summary>
    /// Represents a single inventory slot in the UI.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public Button slotButton;
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI quantityText;
    }

    /// <summary>
    /// UI component for managing inventory display and item selection.
    /// Handles item visualization, slot management, and selection events.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        #region Fields

        [Header("UI Elements")]
        [SerializeField] private List<InventorySlot> slots = new();

        private List<InventoryItem> currentItems = new();
        private int _selectedIndex = -1;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when an item is selected. Passes the index of the selected item.
        /// </summary>
        public event Action<int> OnItemSelected;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize inventory slots and setup button listeners.
        /// </summary>
        private void Awake()
        {
            SetupSlotHandlers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the inventory UI with current items.
        /// </summary>
        /// <param name="items">List of inventory items to display</param>
        public void UpdateInventory(List<InventoryItem> items)
        {
            currentItems = items;
            _selectedIndex = -1;

            RefreshAllSlots();
        }

        /// <summary>
        /// Initialize the inventory UI with the inventory system.
        /// </summary>
        /// <param name="inventorySystem">The inventory system to connect to</param>
        public void Initialize(object inventorySystem)
        {
            // Initialize connection to inventory system
            // For now, this is a placeholder for the interface
        }

        /// <summary>
        /// Gets the currently selected item, or null if none selected.
        /// </summary>
        /// <returns>Selected inventory item or null</returns>
        public InventoryItem GetSelectedItem()
        {
            if (_selectedIndex < 0 || _selectedIndex >= currentItems.Count) 
                return null;
            
            return currentItems[_selectedIndex];
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Setup button listeners for all inventory slots.
        /// </summary>
        private void SetupSlotHandlers()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                int index = i; // Capture loop variable
                slots[i].slotButton.onClick.AddListener(() => SelectItem(index));
            }
        }

        /// <summary>
        /// Refresh all slot displays with current inventory data.
        /// </summary>
        private void RefreshAllSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < currentItems.Count)
                {
                    PopulateSlot(i, currentItems[i]);
                }
                else
                {
                    ClearSlot(i);
                }
            }
        }

        /// <summary>
        /// Populate a specific slot with item data.
        /// </summary>
        /// <param name="index">Slot index</param>
        /// <param name="item">Item to display</param>
        private void PopulateSlot(int index, InventoryItem item)
        {
            var slot = slots[index];
            
            slot.itemIcon.sprite = item.icon;
            slot.itemIcon.enabled = item.icon != null;
            slot.itemNameText.text = item.name;
            slot.quantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
            slot.slotButton.interactable = true;
        }

        /// <summary>
        /// Clear a specific slot of all item data.
        /// </summary>
        /// <param name="index">Slot index to clear</param>
        private void ClearSlot(int index)
        {
            var slot = slots[index];
            
            slot.itemIcon.sprite = null;
            slot.itemIcon.enabled = false;
            slot.itemNameText.text = "";
            slot.quantityText.text = "";
            slot.slotButton.interactable = false;
        }

        /// <summary>
        /// Handle item selection from slot interaction.
        /// </summary>
        /// <param name="index">Index of selected slot</param>
        private void SelectItem(int index)
        {
            if (index < 0 || index >= currentItems.Count) return;

            _selectedIndex = index;
            OnItemSelected?.Invoke(index);
            
            // TODO: Add visual highlight for selected slot if needed
        }

        #endregion
    }
}
