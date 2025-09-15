using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Laboratory.Core.DI;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Handles saving and loading inventory data
    /// </summary>
    public class InventorySaveManager : MonoBehaviour
    {
        #region Fields
        
        private const string SaveFileName = "inventory_save.json";
        private const string SaveFolderName = "SaveData";
        
        [Header("Save Settings")]
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 30f; // seconds
        [SerializeField] private bool encryptSaveData = false;
        
        private IInventorySystem _inventorySystem;
        private string _savePath;
        private float _lastAutoSaveTime;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeSavePath();
        }
        
        private void Start()
        {
            // Try to resolve inventory system from DI container
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IInventorySystem>(out _inventorySystem);
            }
            
            // Fallback: find inventory system in scene
            if (_inventorySystem == null)
            {
                _inventorySystem = FindObjectOfType<EnhancedInventorySystem>();
            }
            
            if (_inventorySystem == null)
            {
                Debug.LogError("[InventorySaveManager] No inventory system found. Cannot save/load inventory data.");
            }
        }
        
        private void Update()
        {
            if (autoSaveEnabled && Time.time - _lastAutoSaveTime >= autoSaveInterval)
            {
                SaveInventory();
                _lastAutoSaveTime = Time.time;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Saves the current inventory state to disk
        /// </summary>
        /// <returns>True if save was successful, false otherwise</returns>
        public bool SaveInventory() 
        {
            try
            {
                if (_inventorySystem == null)
                {
                    Debug.LogError("[InventorySaveManager] Cannot save inventory: No inventory system found.");
                    return false;
                }
                
                var saveData = CreateSaveData();
                var json = JsonUtility.ToJson(saveData, true);
                
                if (encryptSaveData)
                {
                    json = EncryptData(json);
                }
                
                // Ensure save directory exists
                var directory = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_savePath, json);
                
                Debug.Log($"[InventorySaveManager] Inventory saved successfully to {_savePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveManager] Failed to save inventory: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Loads inventory data from disk
        /// </summary>
        /// <returns>True if load was successful, false otherwise</returns>
        public bool LoadInventory() 
        {
            try
            {
                if (_inventorySystem == null)
                {
                    Debug.LogError("[InventorySaveManager] Cannot load inventory: No inventory system found.");
                    return false;
                }
                
                if (!File.Exists(_savePath))
                {
                    Debug.LogWarning($"[InventorySaveManager] Save file not found at {_savePath}. Starting with empty inventory.");
                    return false;
                }
                
                var json = File.ReadAllText(_savePath);
                
                if (encryptSaveData)
                {
                    json = DecryptData(json);
                }
                
                var saveData = JsonUtility.FromJson<InventorySaveGameData>(json);
                
                if (saveData == null)
                {
                    Debug.LogError("[InventorySaveManager] Failed to deserialize save data.");
                    return false;
                }
                
                ApplySaveData(saveData);
                
                Debug.Log($"[InventorySaveManager] Inventory loaded successfully from {_savePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveManager] Failed to load inventory: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <returns>True if save file exists</returns>
        public bool SaveFileExists()
        {
            return File.Exists(_savePath);
        }
        
        /// <summary>
        /// Deletes the save file
        /// </summary>
        /// <returns>True if deletion was successful</returns>
        public bool DeleteSaveFile()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                    Debug.Log($"[InventorySaveManager] Save file deleted: {_savePath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveManager] Failed to delete save file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current save file path
        /// </summary>
        /// <returns>Full path to the save file</returns>
        public string GetSavePath()
        {
            return _savePath;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes the save file path
        /// </summary>
        private void InitializeSavePath()
        {
            var saveFolder = Path.Combine(Application.persistentDataPath, SaveFolderName);
            _savePath = Path.Combine(saveFolder, SaveFileName);
        }
        
        /// <summary>
        /// Creates save data from current inventory state
        /// </summary>
        /// <returns>Serializable save data</returns>
        private InventorySaveGameData CreateSaveData()
        {
            var saveData = new InventorySaveGameData
            {
                saveVersion = 1,
                saveDateTime = DateTime.Now.ToBinary(),
                maxSlots = _inventorySystem.MaxSlots,
                slots = new List<SerializableSlot>()
            };
            
            var allSlots = _inventorySystem.GetAllSlots();
            foreach (var slot in allSlots)
            {
                if (slot?.Item != null)
                {
                    var serializableSlot = new SerializableSlot
                    {
                        slotIndex = slot.SlotIndex,
                        itemId = slot.Item.ItemID,
                        quantity = slot.Quantity
                    };
                    saveData.slots.Add(serializableSlot);
                }
            }
            
            return saveData;
        }
        
        /// <summary>
        /// Applies loaded save data to the inventory system
        /// </summary>
        /// <param name="saveData">The save data to apply</param>
        private void ApplySaveData(InventorySaveGameData saveData)
        {
            // Clear current inventory
            _inventorySystem.ClearInventory();
            
            // Load items from save data
            foreach (var slot in saveData.slots)
            {
                var itemData = LoadItemDataById(slot.itemId);
                if (itemData != null)
                {
                    _inventorySystem.AddItem(itemData, slot.quantity);
                }
                else
                {
                    Debug.LogWarning($"[InventorySaveManager] Could not find ItemData for ID: {slot.itemId}");
                }
            }
        }
        
        /// <summary>
        /// Loads ItemData by ID from Resources or Addressables
        /// </summary>
        /// <param name="itemId">The item ID to load</param>
        /// <returns>The loaded ItemData or null if not found</returns>
        private ItemData LoadItemDataById(string itemId)
        {
            // Try loading from Resources first (simple approach)
            var itemData = Resources.Load<ItemData>($"Items/{itemId}");
            if (itemData != null)
            {
                return itemData;
            }
            
            // TODO: Add Addressables support if using Addressable Assets
            // This would require async loading, so consider implementing async save/load methods
            
            Debug.LogWarning($"[InventorySaveManager] ItemData not found in Resources/Items/ for ID: {itemId}");
            return null;
        }
        
        /// <summary>
        /// Simple encryption for save data (not cryptographically secure)
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <returns>Encrypted data</returns>
        private string EncryptData(string data)
        {
            // Simple XOR encryption (for basic obfuscation only)
            var key = "LabInventoryKey";
            var result = new System.Text.StringBuilder();
            
            for (int i = 0; i < data.Length; i++)
            {
                result.Append((char)(data[i] ^ key[i % key.Length]));
            }
            
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result.ToString()));
        }
        
        /// <summary>
        /// Simple decryption for save data
        /// </summary>
        /// <param name="encryptedData">Encrypted data to decrypt</param>
        /// <returns>Decrypted data</returns>
        private string DecryptData(string encryptedData)
        {
            try
            {
                var data = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(encryptedData));
                var key = "LabInventoryKey";
                var result = new System.Text.StringBuilder();
                
                for (int i = 0; i < data.Length; i++)
                {
                    result.Append((char)(data[i] ^ key[i % key.Length]));
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveManager] Failed to decrypt data: {ex.Message}");
                throw;
            }
        }
        
        #endregion
    }
}
