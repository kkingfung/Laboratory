namespace Laboratory.Gameplay.Inventory
{
    #region Event Records

    /// <summary>
    /// Event triggered when an item is selected in the inventory.
    /// </summary>
    public record ItemSelectedEvent(ItemData SelectedItem);
}
