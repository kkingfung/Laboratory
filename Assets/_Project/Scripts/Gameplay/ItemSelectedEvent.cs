namespace Laboratory.Gameplay.Inventory
{
    #region Event Classes

    /// <summary>
    /// Event triggered when an item is selected in the inventory.
    /// </summary>
    public class ItemSelectedEvent
    {
        public ItemData SelectedItem { get; set; }

        public ItemSelectedEvent(ItemData selectedItem)
        {
            SelectedItem = selectedItem;
        }
    }

    #endregion
}
