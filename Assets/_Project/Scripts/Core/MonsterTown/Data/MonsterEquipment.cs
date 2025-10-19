using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Equipment.Types;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Equipment system for Monster Town creatures
    /// Manages equipment slots and bonuses for monsters in town activities
    /// </summary>
    [System.Serializable]
    public struct MonsterEquipment
    {
        [Header("Equipment Slots")]
        public EquipmentSlot Head;
        public EquipmentSlot Body;
        public EquipmentSlot Accessory;
        public EquipmentSlot Weapon;

        [Header("Equipment Stats")]
        public int TotalEquipmentLevel;
        public float EquipmentRating;

        /// <summary>
        /// Create empty equipment loadout
        /// </summary>
        public static MonsterEquipment CreateEmpty()
        {
            return new MonsterEquipment
            {
                Head = EquipmentSlot.Empty(),
                Body = EquipmentSlot.Empty(),
                Accessory = EquipmentSlot.Empty(),
                Weapon = EquipmentSlot.Empty(),
                TotalEquipmentLevel = 0,
                EquipmentRating = 0f
            };
        }

        /// <summary>
        /// Create basic starter equipment
        /// </summary>
        public static MonsterEquipment CreateStarter()
        {
            return new MonsterEquipment
            {
                Head = EquipmentSlot.CreateBasic("Basic Hat", EquipmentType.Head, 1),
                Body = EquipmentSlot.CreateBasic("Simple Vest", EquipmentType.Body, 1),
                Accessory = EquipmentSlot.Empty(),
                Weapon = EquipmentSlot.Empty(),
                TotalEquipmentLevel = 2,
                EquipmentRating = 0.1f
            };
        }

        /// <summary>
        /// Equip an item to appropriate slot
        /// </summary>
        public bool TryEquipItem(EquipmentItem item)
        {
            switch (item.Type)
            {
                case EquipmentType.Head:
                    Head = EquipmentSlot.FromItem(item);
                    break;
                case EquipmentType.Body:
                    Body = EquipmentSlot.FromItem(item);
                    break;
                case EquipmentType.Accessory:
                    Accessory = EquipmentSlot.FromItem(item);
                    break;
                case EquipmentType.Weapon:
                    Weapon = EquipmentSlot.FromItem(item);
                    break;
                default:
                    return false;
            }

            UpdateEquipmentStats();
            return true;
        }

        /// <summary>
        /// Unequip item from specific slot
        /// </summary>
        public bool TryUnequipSlot(EquipmentType slotType)
        {
            switch (slotType)
            {
                case EquipmentType.Head:
                    Head = EquipmentSlot.Empty();
                    break;
                case EquipmentType.Body:
                    Body = EquipmentSlot.Empty();
                    break;
                case EquipmentType.Accessory:
                    Accessory = EquipmentSlot.Empty();
                    break;
                case EquipmentType.Weapon:
                    Weapon = EquipmentSlot.Empty();
                    break;
                default:
                    return false;
            }

            UpdateEquipmentStats();
            return true;
        }

        /// <summary>
        /// Calculate equipment bonus for specific activity
        /// </summary>
        public float GetActivityBonus(ActivityType activityType)
        {
            float totalBonus = 0f;

            totalBonus += Head.GetActivityBonus(activityType);
            totalBonus += Body.GetActivityBonus(activityType);
            totalBonus += Accessory.GetActivityBonus(activityType);
            totalBonus += Weapon.GetActivityBonus(activityType);

            // Set bonus if multiple pieces equipped
            int equippedCount = GetEquippedCount();
            if (equippedCount >= 2)
            {
                totalBonus *= 1.1f; // 10% set bonus for 2+ pieces
            }
            if (equippedCount >= 4)
            {
                totalBonus *= 1.2f; // Additional 20% for full set
            }

            return totalBonus;
        }

        /// <summary>
        /// Get combined stat bonuses from all equipment
        /// </summary>
        public EquipmentStatBonuses GetStatBonuses()
        {
            var bonuses = new EquipmentStatBonuses();

            bonuses.Add(Head.GetStatBonuses());
            bonuses.Add(Body.GetStatBonuses());
            bonuses.Add(Accessory.GetStatBonuses());
            bonuses.Add(Weapon.GetStatBonuses());

            return bonuses;
        }

        /// <summary>
        /// Get number of equipped items
        /// </summary>
        public int GetEquippedCount()
        {
            int count = 0;
            if (!Head.IsEmpty) count++;
            if (!Body.IsEmpty) count++;
            if (!Accessory.IsEmpty) count++;
            if (!Weapon.IsEmpty) count++;
            return count;
        }

        /// <summary>
        /// Check if equipment loadout is complete
        /// </summary>
        public bool IsFullyEquipped()
        {
            return GetEquippedCount() == 4;
        }

        /// <summary>
        /// Get equipment durability (average across all items)
        /// </summary>
        public float GetAverageDurability()
        {
            float totalDurability = 0f;
            int equippedCount = 0;

            if (!Head.IsEmpty) { totalDurability += Head.Durability; equippedCount++; }
            if (!Body.IsEmpty) { totalDurability += Body.Durability; equippedCount++; }
            if (!Accessory.IsEmpty) { totalDurability += Accessory.Durability; equippedCount++; }
            if (!Weapon.IsEmpty) { totalDurability += Weapon.Durability; equippedCount++; }

            return equippedCount > 0 ? totalDurability / equippedCount : 0f;
        }

        /// <summary>
        /// Apply wear to all equipment
        /// </summary>
        public void ApplyWear(float wearAmount = 1f)
        {
            if (!Head.IsEmpty) Head = Head.ApplyWear(wearAmount);
            if (!Body.IsEmpty) Body = Body.ApplyWear(wearAmount);
            if (!Accessory.IsEmpty) Accessory = Accessory.ApplyWear(wearAmount);
            if (!Weapon.IsEmpty) Weapon = Weapon.ApplyWear(wearAmount);

            UpdateEquipmentStats();
        }

        /// <summary>
        /// Repair all equipment
        /// </summary>
        public void RepairAll()
        {
            if (!Head.IsEmpty) Head = Head.Repair();
            if (!Body.IsEmpty) Body = Body.Repair();
            if (!Accessory.IsEmpty) Accessory = Accessory.Repair();
            if (!Weapon.IsEmpty) Weapon = Weapon.Repair();

            UpdateEquipmentStats();
        }

        /// <summary>
        /// Get all equipped items as a list
        /// </summary>
        public List<EquipmentSlot> GetEquippedItems()
        {
            var items = new List<EquipmentSlot>();

            if (!Head.IsEmpty) items.Add(Head);
            if (!Body.IsEmpty) items.Add(Body);
            if (!Accessory.IsEmpty) items.Add(Accessory);
            if (!Weapon.IsEmpty) items.Add(Weapon);

            return items;
        }

        /// <summary>
        /// Update calculated equipment stats
        /// </summary>
        private void UpdateEquipmentStats()
        {
            TotalEquipmentLevel = 0;
            float totalRating = 0f;
            int count = 0;

            if (!Head.IsEmpty) { TotalEquipmentLevel += Head.Level; totalRating += Head.GetRating(); count++; }
            if (!Body.IsEmpty) { TotalEquipmentLevel += Body.Level; totalRating += Body.GetRating(); count++; }
            if (!Accessory.IsEmpty) { TotalEquipmentLevel += Accessory.Level; totalRating += Accessory.GetRating(); count++; }
            if (!Weapon.IsEmpty) { TotalEquipmentLevel += Weapon.Level; totalRating += Weapon.GetRating(); count++; }

            EquipmentRating = count > 0 ? totalRating / count : 0f;
        }

        /// <summary>
        /// Get equipment summary for display
        /// </summary>
        public override string ToString()
        {
            return $"Equipment: {GetEquippedCount()}/4 equipped, Level {TotalEquipmentLevel}, Rating {EquipmentRating:F1}";
        }
    }

    /// <summary>
    /// Individual equipment slot
    /// </summary>
    [System.Serializable]
    public struct EquipmentSlot
    {
        public string ItemId;
        public string Name;
        public EquipmentType Type;
        public EquipmentRarity Rarity;
        public int Level;
        public float Durability; // 0-100
        public bool IsEmpty => string.IsNullOrEmpty(ItemId);

        // Stat bonuses
        public float StrengthBonus;
        public float AgilityBonus;
        public float IntelligenceBonus;
        public float VitalityBonus;
        public float CreativityBonus;
        public float SocialBonus;

        // Activity bonuses
        public List<ActivityType> ActivityBonuses;

        public static EquipmentSlot Empty()
        {
            return new EquipmentSlot
            {
                ItemId = "",
                Name = "",
                Type = EquipmentType.Head,
                Rarity = EquipmentRarity.Common,
                Level = 0,
                Durability = 0f,
                ActivityBonuses = new List<ActivityType>()
            };
        }

        public static EquipmentSlot CreateBasic(string name, EquipmentType type, int level)
        {
            return new EquipmentSlot
            {
                ItemId = System.Guid.NewGuid().ToString(),
                Name = name,
                Type = type,
                Rarity = EquipmentRarity.Common,
                Level = level,
                Durability = 100f,
                StrengthBonus = level * 0.05f,
                AgilityBonus = level * 0.05f,
                IntelligenceBonus = level * 0.05f,
                VitalityBonus = level * 0.05f,
                CreativityBonus = level * 0.05f,
                SocialBonus = level * 0.05f,
                ActivityBonuses = new List<ActivityType>()
            };
        }

        public static EquipmentSlot FromItem(EquipmentItem item)
        {
            var slot = new EquipmentSlot
            {
                ItemId = item.ItemId,
                Name = item.Name,
                Type = item.Type,
                Rarity = item.Rarity,
                Level = item.Level,
                Durability = 100f,
                ActivityBonuses = new List<ActivityType>(item.ActivityBonuses)
            };

            // Convert stat bonuses
            foreach (var bonus in item.StatBonuses)
            {
                switch (bonus.Key)
                {
                    case StatType.Strength:
                        slot.StrengthBonus = bonus.Value;
                        break;
                    case StatType.Agility:
                        slot.AgilityBonus = bonus.Value;
                        break;
                    case StatType.Intelligence:
                        slot.IntelligenceBonus = bonus.Value;
                        break;
                    case StatType.Vitality:
                        slot.VitalityBonus = bonus.Value;
                        break;
                    case StatType.Social:
                        slot.SocialBonus = bonus.Value;
                        break;
                }
            }

            return slot;
        }

        public float GetActivityBonus(ActivityType activityType)
        {
            if (IsEmpty) return 0f;

            float baseBonus = ActivityBonuses.Contains(activityType) ? Level * 0.1f : 0f;
            float durabilityMultiplier = Durability / 100f;

            return baseBonus * durabilityMultiplier;
        }

        public EquipmentStatBonuses GetStatBonuses()
        {
            if (IsEmpty) return new EquipmentStatBonuses();

            float durabilityMultiplier = Durability / 100f;

            return new EquipmentStatBonuses
            {
                Strength = StrengthBonus * durabilityMultiplier,
                Agility = AgilityBonus * durabilityMultiplier,
                Intelligence = IntelligenceBonus * durabilityMultiplier,
                Vitality = VitalityBonus * durabilityMultiplier,
                Creativity = CreativityBonus * durabilityMultiplier,
                Social = SocialBonus * durabilityMultiplier
            };
        }

        public float GetRating()
        {
            if (IsEmpty) return 0f;

            float baseRating = Level * (int)Rarity;
            float durabilityFactor = Durability / 100f;

            return baseRating * durabilityFactor;
        }

        public EquipmentSlot ApplyWear(float wearAmount)
        {
            var worn = this;
            worn.Durability = Mathf.Max(0f, Durability - wearAmount);
            return worn;
        }

        public EquipmentSlot Repair()
        {
            var repaired = this;
            repaired.Durability = 100f;
            return repaired;
        }
    }

    /// <summary>
    /// Combined stat bonuses from equipment
    /// </summary>
    [System.Serializable]
    public struct EquipmentStatBonuses
    {
        public float Strength;
        public float Agility;
        public float Intelligence;
        public float Vitality;
        public float Creativity;
        public float Social;

        public void Add(EquipmentStatBonuses other)
        {
            Strength += other.Strength;
            Agility += other.Agility;
            Intelligence += other.Intelligence;
            Vitality += other.Vitality;
            Creativity += other.Creativity;
            Social += other.Social;
        }

        public float GetTotalBonus()
        {
            return Strength + Agility + Intelligence + Vitality + Creativity + Social;
        }
    }
}