using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Serializable]
    public class InventorySlot
    {
        public Button slotButton;
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI quantityText;
    }

    [Header("UI Elements")]
    [SerializeField] private List<InventorySlot> slots = new();

    public event Action<int>? OnItemSelected; // index of selected item

    private List<InventoryItem> currentItems = new();

    private int selectedIndex = -1;

    private void Awake()
    {
        // Setup button listeners
        for (int i = 0; i < slots.Count; i++)
        {
            int index = i;
            slots[i].slotButton.onClick.AddListener(() => SelectItem(index));
        }
    }

    /// <summary>
    /// Updates the inventory UI with current items.
    /// </summary>
    public void UpdateInventory(List<InventoryItem> items)
    {
        currentItems = items;
        selectedIndex = -1;

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < items.Count)
            {
                var item = items[i];
                slots[i].itemIcon.sprite = item.icon;
                slots[i].itemIcon.enabled = item.icon != null;
                slots[i].itemNameText.text = item.name;
                slots[i].quantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
                slots[i].slotButton.interactable = true;
            }
            else
            {
                ClearSlot(i);
            }
        }
    }

    private void ClearSlot(int index)
    {
        slots[index].itemIcon.sprite = null;
        slots[index].itemIcon.enabled = false;
        slots[index].itemNameText.text = "";
        slots[index].quantityText.text = "";
        slots[index].slotButton.interactable = false;
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= currentItems.Count) return;

        selectedIndex = index;
        OnItemSelected?.Invoke(index);
        // You can highlight selected slot here if needed
    }

    /// <summary>
    /// Gets the currently selected item, or null if none selected.
    /// </summary>
    public InventoryItem? GetSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= currentItems.Count) return null;
        return currentItems[selectedIndex];
    }
}

/// <summary>
/// Example inventory item class; replace with your own implementation.
/// </summary>
[Serializable]
public class InventoryItem
{
    public string name = "";
    public Sprite icon = null!;
    public int quantity = 1;

    // Add other fields like item ID, description, effects, etc.
}
