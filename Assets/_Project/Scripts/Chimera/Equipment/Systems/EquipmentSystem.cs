using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Profiling;
using UnityEngine;
using Laboratory.Chimera.Activities;
using System.Collections.Generic;

namespace Laboratory.Chimera.Equipment
{
    /// <summary>
    /// Main ECS system for equipment management
    /// Handles equipping, unequipping, bonuses, and durability
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EquipmentSystem : SystemBase
    {
        private Dictionary<int, EquipmentConfig> _equipmentDatabase;

        // Entity command buffer system for optimized deferred operations
        private EndSimulationEntityCommandBufferSystem _endSimulationECBSystem;

        private static readonly ProfilerMarker s_ProcessEquipRequestsMarker =
            new ProfilerMarker("Equipment.ProcessEquipRequests");
        private static readonly ProfilerMarker s_UpdateBonusCacheMarker =
            new ProfilerMarker("Equipment.UpdateBonusCache");
        private static readonly ProfilerMarker s_UpdateDurabilityMarker =
            new ProfilerMarker("Equipment.UpdateDurability");

        protected override void OnCreate()
        {
            _equipmentDatabase = new Dictionary<int, EquipmentConfig>();

            // Get entity command buffer system for optimized deferred operations
            _endSimulationECBSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            // Load all equipment configurations
            LoadEquipmentDatabase();

            // Create singleton entity for system data
            var singletonEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(singletonEntity, new EquipmentSystemData
            {
                isInitialized = true,
                totalEquipmentTypes = _equipmentDatabase.Count,
                totalItemsInCirculation = 0,
                currentTime = 0f
            });

            Debug.Log($"Equipment System initialized with {_equipmentDatabase.Count} equipment types");
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Update system data
            UpdateSystemData(currentTime);

            // Process equipment requests
            using (s_ProcessEquipRequestsMarker.Auto())
            {
                ProcessEquipRequests(currentTime);
                ProcessUnequipRequests(currentTime);
                ProcessInventoryRequests(currentTime);
            }

            // Update bonus caches
            using (s_UpdateBonusCacheMarker.Auto())
            {
                UpdateEquipmentBonuses(currentTime);
            }

            // Update durability
            using (s_UpdateDurabilityMarker.Auto())
            {
                UpdateDurability(currentTime);
            }
        }

        /// <summary>
        /// Loads equipment database from Resources
        /// </summary>
        private void LoadEquipmentDatabase()
        {
            var configs = Resources.LoadAll<EquipmentConfig>("Configs/Equipment");
            foreach (var config in configs)
            {
                if (_equipmentDatabase.ContainsKey(config.itemId))
                {
                    Debug.LogWarning($"Duplicate equipment ID: {config.itemId} ({config.itemName})");
                    continue;
                }

                _equipmentDatabase[config.itemId] = config;
            }

            if (_equipmentDatabase.Count == 0)
            {
                Debug.LogWarning("No equipment configurations found in Resources/Configs/Equipment/");
            }
        }

        /// <summary>
        /// Updates system singleton data
        /// </summary>
        private void UpdateSystemData(float currentTime)
        {
            foreach (var systemData in SystemAPI.Query<RefRW<EquipmentSystemData>>())
            {
                systemData.ValueRW.currentTime = currentTime;
            }
        }

        /// <summary>
        /// Processes requests to equip items
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessEquipRequests(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<EquipItemRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var itemId = request.ValueRO.itemId;
                var targetSlot = request.ValueRO.targetSlot;

                // Validate target entity exists
                if (!EntityManager.Exists(targetEntity))
                {
                    Debug.LogWarning($"Target entity for equip request does not exist");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure target has inventory
                if (!EntityManager.HasBuffer<EquipmentInventoryElement>(targetEntity))
                {
                    Debug.LogWarning($"Target entity has no inventory");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Find item in inventory
                var inventory = EntityManager.GetBuffer<EquipmentInventoryElement>(targetEntity);
                int itemIndex = FindItemInInventory(inventory, itemId);

                if (itemIndex < 0)
                {
                    Debug.LogWarning($"Item {itemId} not found in inventory");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get item data
                var item = inventory[itemIndex].item;

                // Check if item can be equipped in target slot
                if (item.slot != targetSlot)
                {
                    Debug.LogWarning($"Item {item.itemName} cannot be equipped in slot {targetSlot}");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Equip the item
                EquipItem(targetEntity, item, targetSlot, ecb);

                // Mark item as equipped in inventory
                var updatedItem = item;
                updatedItem.isEquipped = true;
                inventory[itemIndex] = new EquipmentInventoryElement { item = updatedItem };

                Debug.Log($"Equipped {item.itemName} to slot {targetSlot}");

                // Remove request
                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Processes requests to unequip items
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessUnequipRequests(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<UnequipItemRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var targetSlot = request.ValueRO.targetSlot;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Unequip the item
                UnequipItem(targetEntity, targetSlot, ecb);

                Debug.Log($"Unequipped slot {targetSlot}");

                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Processes inventory add/remove requests
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessInventoryRequests(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            // Process add requests
            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<AddItemRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var itemId = request.ValueRO.itemId;
                var quantity = request.ValueRO.quantity;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get or create equipment config
                if (!_equipmentDatabase.TryGetValue(itemId, out var config))
                {
                    Debug.LogWarning($"Unknown item ID: {itemId}");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure inventory buffer exists
                if (!EntityManager.HasBuffer<EquipmentInventoryElement>(targetEntity))
                {
                    ecb.AddBuffer<EquipmentInventoryElement>(targetEntity);
                }

                // Add items to inventory
                var inventory = EntityManager.GetBuffer<EquipmentInventoryElement>(targetEntity);
                for (int i = 0; i < quantity; i++)
                {
                    inventory.Add(new EquipmentInventoryElement
                    {
                        item = config.ToEquipmentItem()
                    });
                }

                Debug.Log($"Added {quantity}x {config.itemName} to inventory");

                ecb.DestroyEntity(entity);
            }

            // Process remove requests
            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<RemoveItemRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var itemId = request.ValueRO.itemId;
                var quantity = request.ValueRO.quantity;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (!EntityManager.HasBuffer<EquipmentInventoryElement>(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Remove items from inventory
                var inventory = EntityManager.GetBuffer<EquipmentInventoryElement>(targetEntity);
                int removed = 0;

                for (int i = inventory.Length - 1; i >= 0 && removed < quantity; i--)
                {
                    if (inventory[i].item.itemId == itemId && !inventory[i].item.isEquipped)
                    {
                        inventory.RemoveAt(i);
                        removed++;
                    }
                }

                Debug.Log($"Removed {removed}x item {itemId} from inventory");

                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Updates equipment bonus caches for all entities with equipment
        /// </summary>
        private void UpdateEquipmentBonuses(float currentTime)
        {
            foreach (var (equippedItems, bonusCache) in
                SystemAPI.Query<RefRO<EquippedItemsComponent>, RefRW<EquipmentBonusCache>>())
            {
                // Recalculate bonuses from equipped items
                var newCache = CalculateTotalBonuses(in equippedItems.ValueRO);
                newCache.lastUpdateTime = currentTime;

                bonusCache.ValueRW = newCache;
            }
        }

        /// <summary>
        /// Updates durability for equipped items after activity completion
        /// Called when activities complete to reduce durability
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void UpdateDurability(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            // Check for completed activities and reduce durability
            foreach (var (result, equippedItems, entity) in
                SystemAPI.Query<RefRO<ActivityResultComponent>, RefRO<EquippedItemsComponent>>()
                .WithEntityAccess())
            {
                // Durability loss only for consumables and items with durability
                if (EntityManager.HasBuffer<EquipmentInventoryElement>(entity))
                {
                    var inventory = EntityManager.GetBuffer<EquipmentInventoryElement>(entity);

                    // Reduce durability for all equipped items
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.headSlotItemId, 1);
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.bodySlotItemId, 1);
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.handsSlotItemId, 1);
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.feetSlotItemId, 1);
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.accessory1SlotItemId, 1);
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.accessory2SlotItemId, 1);
                    ReduceEquippedItemDurability(inventory, equippedItems.ValueRO.toolSlotItemId, 1);
                }
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Reduces durability for a specific equipped item
        /// </summary>
        private void ReduceEquippedItemDurability(DynamicBuffer<EquipmentInventoryElement> inventory, int itemId, int amount)
        {
            if (itemId == 0) return;

            for (int i = 0; i < inventory.Length; i++)
            {
                if (inventory[i].item.itemId == itemId && inventory[i].item.isEquipped)
                {
                    var item = inventory[i].item;

                    // Only reduce durability if item has durability system
                    if (item.maxDurability > 0)
                    {
                        item.currentDurability = Mathf.Max(0, item.currentDurability - amount);

                        // If durability reaches 0, item breaks
                        if (item.currentDurability <= 0)
                        {
                            Debug.LogWarning($"Equipment {item.itemName} broke due to durability loss!");
                            // Item remains in inventory but loses bonuses until repaired
                            // Could trigger auto-unequip here if desired
                        }

                        inventory[i] = new EquipmentInventoryElement { item = item };
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Equips an item to the specified slot
        /// </summary>
        private void EquipItem(Entity targetEntity, EquipmentItem item, EquipmentSlot slot, EntityCommandBuffer ecb)
        {
            // Ensure equipped items component exists
            if (!EntityManager.HasComponent<EquippedItemsComponent>(targetEntity))
            {
                ecb.AddComponent(targetEntity, new EquippedItemsComponent());
            }

            // Update equipped items component
            var equipped = EntityManager.GetComponentData<EquippedItemsComponent>(targetEntity);

            switch (slot)
            {
                case EquipmentSlot.Head:
                    equipped.headSlotItemId = item.itemId;
                    break;
                case EquipmentSlot.Body:
                    equipped.bodySlotItemId = item.itemId;
                    break;
                case EquipmentSlot.Hands:
                    equipped.handsSlotItemId = item.itemId;
                    break;
                case EquipmentSlot.Feet:
                    equipped.feetSlotItemId = item.itemId;
                    break;
                case EquipmentSlot.Accessory1:
                    equipped.accessory1SlotItemId = item.itemId;
                    break;
                case EquipmentSlot.Accessory2:
                    equipped.accessory2SlotItemId = item.itemId;
                    break;
                case EquipmentSlot.Tool:
                    equipped.toolSlotItemId = item.itemId;
                    break;
            }

            EntityManager.SetComponentData(targetEntity, equipped);

            // Ensure bonus cache exists
            if (!EntityManager.HasComponent<EquipmentBonusCache>(targetEntity))
            {
                ecb.AddComponent<EquipmentBonusCache>(targetEntity);
            }

            // Bonus cache will be updated in UpdateEquipmentBonuses
        }

        /// <summary>
        /// Unequips an item from the specified slot
        /// </summary>
        private void UnequipItem(Entity targetEntity, EquipmentSlot slot, EntityCommandBuffer ecb)
        {
            if (!EntityManager.HasComponent<EquippedItemsComponent>(targetEntity))
                return;

            var equipped = EntityManager.GetComponentData<EquippedItemsComponent>(targetEntity);
            int unequippedItemId = 0;

            switch (slot)
            {
                case EquipmentSlot.Head:
                    unequippedItemId = equipped.headSlotItemId;
                    equipped.headSlotItemId = 0;
                    break;
                case EquipmentSlot.Body:
                    unequippedItemId = equipped.bodySlotItemId;
                    equipped.bodySlotItemId = 0;
                    break;
                case EquipmentSlot.Hands:
                    unequippedItemId = equipped.handsSlotItemId;
                    equipped.handsSlotItemId = 0;
                    break;
                case EquipmentSlot.Feet:
                    unequippedItemId = equipped.feetSlotItemId;
                    equipped.feetSlotItemId = 0;
                    break;
                case EquipmentSlot.Accessory1:
                    unequippedItemId = equipped.accessory1SlotItemId;
                    equipped.accessory1SlotItemId = 0;
                    break;
                case EquipmentSlot.Accessory2:
                    unequippedItemId = equipped.accessory2SlotItemId;
                    equipped.accessory2SlotItemId = 0;
                    break;
                case EquipmentSlot.Tool:
                    unequippedItemId = equipped.toolSlotItemId;
                    equipped.toolSlotItemId = 0;
                    break;
            }

            EntityManager.SetComponentData(targetEntity, equipped);

            // Mark item as unequipped in inventory
            if (unequippedItemId != 0 && EntityManager.HasBuffer<EquipmentInventoryElement>(targetEntity))
            {
                var inventory = EntityManager.GetBuffer<EquipmentInventoryElement>(targetEntity);
                for (int i = 0; i < inventory.Length; i++)
                {
                    if (inventory[i].item.itemId == unequippedItemId)
                    {
                        var item = inventory[i].item;
                        item.isEquipped = false;
                        inventory[i] = new EquipmentInventoryElement { item = item };
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates total bonuses from all equipped items
        /// </summary>
        private EquipmentBonusCache CalculateTotalBonuses(in EquippedItemsComponent equippedItems)
        {
            var cache = new EquipmentBonusCache
            {
                experienceMultiplier = 1.0f,
                currencyMultiplier = 1.0f
            };

            // Process each equipped slot
            ProcessItemBonuses(equippedItems.headSlotItemId, ref cache);
            ProcessItemBonuses(equippedItems.bodySlotItemId, ref cache);
            ProcessItemBonuses(equippedItems.handsSlotItemId, ref cache);
            ProcessItemBonuses(equippedItems.feetSlotItemId, ref cache);
            ProcessItemBonuses(equippedItems.accessory1SlotItemId, ref cache);
            ProcessItemBonuses(equippedItems.accessory2SlotItemId, ref cache);
            ProcessItemBonuses(equippedItems.toolSlotItemId, ref cache);

            return cache;
        }

        /// <summary>
        /// Processes bonuses from a single item
        /// </summary>
        private void ProcessItemBonuses(int itemId, ref EquipmentBonusCache cache)
        {
            if (itemId == 0 || !_equipmentDatabase.TryGetValue(itemId, out var config))
                return;

            // Apply stat bonuses
            if ((config.statBonusType & StatBonusType.Strength) != 0)
                cache.strengthBonus += config.statBonusValue;
            if ((config.statBonusType & StatBonusType.Agility) != 0)
                cache.agilityBonus += config.statBonusValue;
            if ((config.statBonusType & StatBonusType.Intelligence) != 0)
                cache.intelligenceBonus += config.statBonusValue;
            if ((config.statBonusType & StatBonusType.Vitality) != 0)
                cache.vitalityBonus += config.statBonusValue;
            if ((config.statBonusType & StatBonusType.Social) != 0)
                cache.socialBonus += config.statBonusValue;
            if ((config.statBonusType & StatBonusType.Adaptability) != 0)
                cache.adaptabilityBonus += config.statBonusValue;

            // Apply activity bonuses
            switch (config.activityBonus)
            {
                case ActivityType.Racing:
                    cache.racingBonus += config.activityBonusValue;
                    break;
                case ActivityType.Combat:
                    cache.combatBonus += config.activityBonusValue;
                    break;
                case ActivityType.Puzzle:
                    cache.puzzleBonus += config.activityBonusValue;
                    break;
                case ActivityType.Strategy:
                    cache.strategyBonus += config.activityBonusValue;
                    break;
                case ActivityType.Music:
                    cache.rhythmBonus += config.activityBonusValue;
                    break;
                case ActivityType.Adventure:
                    cache.adventureBonus += config.activityBonusValue;
                    break;
                case ActivityType.Platforming:
                    cache.platformingBonus += config.activityBonusValue;
                    break;
                case ActivityType.Crafting:
                    cache.craftingBonus += config.activityBonusValue;
                    break;
                case ActivityType.None:
                    // Apply to all activities
                    cache.racingBonus += config.activityBonusValue;
                    cache.combatBonus += config.activityBonusValue;
                    cache.puzzleBonus += config.activityBonusValue;
                    break;
            }
        }

        /// <summary>
        /// Finds item index in inventory
        /// </summary>
        private int FindItemInInventory(DynamicBuffer<EquipmentInventoryElement> inventory, int itemId)
        {
            for (int i = 0; i < inventory.Length; i++)
            {
                if (inventory[i].item.itemId == itemId && !inventory[i].item.isEquipped)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets equipment configuration by ID
        /// </summary>
        public EquipmentConfig GetEquipmentConfig(int itemId)
        {
            _equipmentDatabase.TryGetValue(itemId, out var config);
            return config;
        }

        /// <summary>
        /// Creates an equip request entity
        /// </summary>
        public Entity CreateEquipRequest(Entity targetEntity, int itemId, EquipmentSlot slot)
        {
            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new EquipItemRequest
            {
                targetEntity = targetEntity,
                itemId = itemId,
                targetSlot = slot,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            return requestEntity;
        }

        /// <summary>
        /// Creates an add item request entity
        /// </summary>
        public Entity CreateAddItemRequest(Entity targetEntity, int itemId, int quantity = 1)
        {
            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new AddItemRequest
            {
                targetEntity = targetEntity,
                itemId = itemId,
                quantity = quantity,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            return requestEntity;
        }
    }
}
