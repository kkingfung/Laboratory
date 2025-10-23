using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Equipment.Types;
using EquipmentItem = Laboratory.Core.MonsterTown.Equipment;

namespace Laboratory.Core.Equipment
{
    /// <summary>
    /// Equipment UI Manager - Handles equipment interface for monsters
    /// Provides inventory, equipment slots, and upgrade interfaces
    /// </summary>
    public class EquipmentUI : MonoBehaviour
    {
        [Header("🎮 UI References")]
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private Transform equipmentSlotsParent;
        [SerializeField] private Transform inventoryParent;
        [SerializeField] private GameObject equipmentSlotPrefab;
        [SerializeField] private GameObject inventoryItemPrefab;

        [Header("📊 Monster Info Display")]
        [SerializeField] private Text monsterNameText;
        [SerializeField] private Text monsterLevelText;
        [SerializeField] private Slider[] statBars; // For displaying stat bonuses

        [Header("🔧 Equipment Details")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Text itemStatsText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button upgradeButton;

        // Runtime data
        private Monster _currentMonster;
        private EquipmentManager _equipmentManager;
        private List<EquipmentSlotUI> _equipmentSlots = new();
        private List<InventoryItemUI> _inventoryItems = new();
        private EquipmentItem _selectedEquipment;

        #region UI Initialization

        public void InitializeEquipmentUI(EquipmentManager equipmentManager)
        {
            _equipmentManager = equipmentManager;
            SetupEventListeners();
            equipmentPanel.SetActive(false);
            detailsPanel.SetActive(false);
        }

        private void SetupEventListeners()
        {
            if (equipButton != null)
                equipButton.onClick.AddListener(EquipSelectedItem);

            if (unequipButton != null)
                unequipButton.onClick.AddListener(UnequipSelectedItem);

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(UpgradeSelectedItem);
        }

        #endregion

        #region Monster Equipment Display

        /// <summary>
        /// Show equipment interface for a specific monster
        /// </summary>
        public void ShowMonsterEquipment(Monster monster)
        {
            _currentMonster = monster;
            equipmentPanel.SetActive(true);

            UpdateMonsterInfo();
            UpdateEquipmentSlots();
            UpdateInventoryDisplay();
            UpdateStatDisplay();
        }

        /// <summary>
        /// Hide equipment interface
        /// </summary>
        public void HideEquipmentUI()
        {
            equipmentPanel.SetActive(false);
            detailsPanel.SetActive(false);
            _currentMonster = null;
            _selectedEquipment = null;
        }

        private void UpdateMonsterInfo()
        {
            if (_currentMonster == null) return;

            if (monsterNameText != null)
                monsterNameText.text = _currentMonster.Name;

            if (monsterLevelText != null)
                monsterLevelText.text = $"Level {_currentMonster.Level}";
        }

        #endregion

        #region Equipment Slots Display

        private void UpdateEquipmentSlots()
        {
            // Clear existing slots
            foreach (var slot in _equipmentSlots)
            {
                if (slot != null && slot.gameObject != null)
                    DestroyImmediate(slot.gameObject);
            }
            _equipmentSlots.Clear();

            if (_currentMonster == null || equipmentSlotsParent == null) return;

            // Create slots for each equipment type
            var equipmentTypes = System.Enum.GetValues(typeof(EquipmentType));
            foreach (EquipmentType equipmentType in equipmentTypes)
            {
                CreateEquipmentSlot(equipmentType);
            }

            // Populate slots with equipped items
            var equippedItems = _equipmentManager.GetMonsterEquipment(_currentMonster);
            foreach (var equipment in equippedItems)
            {
                if (equipment.IsEquipped)
                {
                    var slot = _equipmentSlots.Find(s => s.SlotType == (Laboratory.Core.Equipment.EquipmentType)equipment.Type);
                    slot?.SetEquipment(equipment);
                }
            }
        }

        private void CreateEquipmentSlot(EquipmentType slotType)
        {
            if (equipmentSlotPrefab == null) return;

            var slotObject = Instantiate(equipmentSlotPrefab, equipmentSlotsParent);
            var slotUI = slotObject.GetComponent<EquipmentSlotUI>();

            if (slotUI == null)
                slotUI = slotObject.AddComponent<EquipmentSlotUI>();

            slotUI.InitializeSlot(slotType, this);
            _equipmentSlots.Add(slotUI);
        }

        #endregion

        #region Inventory Display

        private void UpdateInventoryDisplay()
        {
            // Clear existing inventory items
            foreach (var item in _inventoryItems)
            {
                if (item != null && item.gameObject != null)
                    DestroyImmediate(item.gameObject);
            }
            _inventoryItems.Clear();

            if (_currentMonster == null || inventoryParent == null) return;

            // Show all available equipment for this monster
            var allEquipment = _equipmentManager.GetMonsterEquipment(_currentMonster);
            foreach (var equipment in allEquipment)
            {
                if (!equipment.IsEquipped) // Only show unequipped items in inventory
                {
                    CreateInventoryItem(equipment);
                }
            }
        }

        private void CreateInventoryItem(EquipmentItem equipment)
        {
            if (inventoryItemPrefab == null) return;

            var itemObject = Instantiate(inventoryItemPrefab, inventoryParent);
            var itemUI = itemObject.GetComponent<InventoryItemUI>();

            if (itemUI == null)
                itemUI = itemObject.AddComponent<InventoryItemUI>();

            itemUI.InitializeItem(equipment, this);
            _inventoryItems.Add(itemUI);
        }

        #endregion

        #region Equipment Actions

        /// <summary>
        /// Select an equipment item for details/actions
        /// </summary>
        public void SelectEquipment(EquipmentItem equipment)
        {
            _selectedEquipment = equipment;
            ShowEquipmentDetails(equipment);
        }

        private void ShowEquipmentDetails(EquipmentItem equipment)
        {
            if (equipment == null || detailsPanel == null) return;

            detailsPanel.SetActive(true);

            if (itemNameText != null)
                itemNameText.text = $"{equipment.Name} (Level {equipment.Level})";

            if (itemDescriptionText != null)
                itemDescriptionText.text = equipment.Description;

            if (itemStatsText != null)
                itemStatsText.text = GenerateStatsText(equipment);

            // Update button states
            if (equipButton != null)
                equipButton.gameObject.SetActive(!equipment.IsEquipped);

            if (unequipButton != null)
                unequipButton.gameObject.SetActive(equipment.IsEquipped);

            if (upgradeButton != null)
                upgradeButton.gameObject.SetActive(true);
        }

        private string GenerateStatsText(EquipmentItem equipment)
        {
            var statsText = "";

            // Stat bonuses
            if (equipment.StatBonuses.Count > 0)
            {
                statsText += "Stat Bonuses:\n";
                foreach (var statBonus in equipment.StatBonuses)
                {
                    statsText += $"  {statBonus.Key}: +{statBonus.Value:F0}\n";
                }
            }

            // Activity bonuses
            if (equipment.ActivityBonuses.Count > 0)
            {
                statsText += "\nActivity Bonuses:\n";
                foreach (var activityBonus in equipment.ActivityBonuses)
                {
                    statsText += $"  {activityBonus}\n";
                }
            }

            // Rarity info
            statsText += $"\nRarity: {equipment.Rarity}";

            return statsText;
        }

        private void EquipSelectedItem()
        {
            if (_selectedEquipment == null || _currentMonster == null) return;

            bool success = _equipmentManager.EquipItem(_currentMonster, _selectedEquipment.ItemId);
            if (success)
            {
                UpdateEquipmentSlots();
                UpdateInventoryDisplay();
                UpdateStatDisplay();
                ShowEquipmentDetails(_selectedEquipment); // Refresh details
            }
        }

        private void UnequipSelectedItem()
        {
            if (_selectedEquipment == null || _currentMonster == null) return;

            bool success = _equipmentManager.UnequipItem(_currentMonster, _selectedEquipment.ItemId);
            if (success)
            {
                UpdateEquipmentSlots();
                UpdateInventoryDisplay();
                UpdateStatDisplay();
                ShowEquipmentDetails(_selectedEquipment); // Refresh details
            }
        }

        private void UpgradeSelectedItem()
        {
            if (_selectedEquipment == null || _currentMonster == null) return;

            bool success = _equipmentManager.UpgradeEquipment(_currentMonster, _selectedEquipment.ItemId);
            if (success)
            {
                ShowEquipmentDetails(_selectedEquipment); // Refresh details
                UpdateStatDisplay();
            }
        }

        #endregion

        #region Stat Display

        private void UpdateStatDisplay()
        {
            if (_currentMonster == null || statBars == null) return;

            // This would show equipment bonuses on the monster's base stats
            // For now, just show base stats
            var stats = _currentMonster.Stats;
            var statValues = new float[]
            {
                stats.strength, stats.agility, stats.vitality, stats.speed,
                stats.intelligence, stats.adaptability, stats.social, stats.charisma
            };

            for (int i = 0; i < Mathf.Min(statBars.Length, statValues.Length); i++)
            {
                if (statBars[i] != null)
                {
                    statBars[i].value = statValues[i] / 100f; // Normalize to 0-1
                }
            }
        }

        #endregion
    }

    #region Equipment Slot UI Component

    /// <summary>
    /// Individual equipment slot UI component
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image slotIcon;
        [SerializeField] private Image equipmentIcon;
        [SerializeField] private Text slotLabel;

        public EquipmentType SlotType { get; private set; }
        private EquipmentItem _currentEquipment;
        private EquipmentUI _parentUI;

        public void InitializeSlot(EquipmentType slotType, EquipmentUI parentUI)
        {
            SlotType = slotType;
            _parentUI = parentUI;

            if (slotLabel != null)
                slotLabel.text = slotType.ToString();

            ClearSlot();
        }

        public void SetEquipment(EquipmentItem equipment)
        {
            _currentEquipment = equipment;

            if (equipmentIcon != null)
            {
                equipmentIcon.gameObject.SetActive(true);
                // Would set actual icon here if we had sprite references
                equipmentIcon.color = GetRarityColor((Laboratory.Core.Equipment.EquipmentRarity)equipment.Rarity);
            }
        }

        public void ClearSlot()
        {
            _currentEquipment = null;

            if (equipmentIcon != null)
                equipmentIcon.gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_currentEquipment != null && _parentUI != null)
            {
                _parentUI.SelectEquipment(_currentEquipment);
            }
        }

        private Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => Color.white,
                EquipmentRarity.Uncommon => Color.green,
                EquipmentRarity.Rare => Color.blue,
                EquipmentRarity.Epic => new Color(0.5f, 0f, 1f), // Purple
                EquipmentRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }
    }

    #endregion

    #region Inventory Item UI Component

    /// <summary>
    /// Individual inventory item UI component
    /// </summary>
    public class InventoryItemUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private Text itemName;
        [SerializeField] private Text itemLevel;

        private EquipmentItem _equipment;
        private EquipmentUI _parentUI;

        public void InitializeItem(EquipmentItem equipment, EquipmentUI parentUI)
        {
            _equipment = equipment;
            _parentUI = parentUI;

            if (itemName != null)
                itemName.text = equipment.Name;

            if (itemLevel != null)
                itemLevel.text = $"Lv.{equipment.Level}";

            if (itemIcon != null)
            {
                itemIcon.color = GetRarityColor((Laboratory.Core.Equipment.EquipmentRarity)equipment.Rarity);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_equipment != null && _parentUI != null)
            {
                _parentUI.SelectEquipment(_equipment);
            }
        }

        private Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => Color.white,
                EquipmentRarity.Uncommon => Color.green,
                EquipmentRarity.Rare => Color.blue,
                EquipmentRarity.Epic => new Color(0.5f, 0f, 1f),
                EquipmentRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }
    }

    #endregion
}