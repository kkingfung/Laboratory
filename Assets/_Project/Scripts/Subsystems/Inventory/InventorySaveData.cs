using System;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Serializable data structure for inventory saves
    /// </summary>
    [Serializable]
    public class InventorySaveGameData
    {
        public int saveVersion;
        public long saveDateTime;
        public int maxSlots;
        public List<SerializableSlot> slots;
    }
    
    /// <summary>
    /// Serializable inventory slot data
    /// </summary>
    [Serializable]
    public class SerializableSlot
    {
        public int slotIndex;
        public string itemId;
        public int quantity;
    }
}
