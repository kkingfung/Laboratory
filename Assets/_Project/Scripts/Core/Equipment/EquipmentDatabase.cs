using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.MonsterTown;
using ActivityType = Laboratory.Core.Activities.Types.ActivityType;

namespace Laboratory.Core.Equipment
{
    /// <summary>
    /// Equipment Database - ScriptableObject containing all equipment configurations
    /// Designer-friendly database for creating and managing equipment items
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment Database", menuName = "Chimera/Equipment Database", order = 10)]
    public class EquipmentDatabase : ScriptableObject
    {
        [Header("🎒 Equipment Collections")]
        [SerializeField] private EquipmentConfig[] weapons = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] armor = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] accessories = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] tools = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] vehicles = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] instruments = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] gadgets = new EquipmentConfig[0];
        [SerializeField] private EquipmentConfig[] boosts = new EquipmentConfig[0];

        [Header("📊 Database Statistics")]
        [SerializeField, ReadOnly] private int totalEquipmentCount = 0;
        [SerializeField, ReadOnly] private int commonCount = 0;
        [SerializeField, ReadOnly] private int uncommonCount = 0;
        [SerializeField, ReadOnly] private int rareCount = 0;
        [SerializeField, ReadOnly] private int epicCount = 0;
        [SerializeField, ReadOnly] private int legendaryCount = 0;

        /// <summary>
        /// Get all equipment configurations
        /// </summary>
        public EquipmentConfig[] AllEquipment
        {
            get
            {
                var allEquipment = new List<EquipmentConfig>();
                allEquipment.AddRange(weapons);
                allEquipment.AddRange(armor);
                allEquipment.AddRange(accessories);
                allEquipment.AddRange(tools);
                allEquipment.AddRange(vehicles);
                allEquipment.AddRange(instruments);
                allEquipment.AddRange(gadgets);
                allEquipment.AddRange(boosts);
                return allEquipment.ToArray();
            }
        }

        /// <summary>
        /// Get equipment by type
        /// </summary>
        public EquipmentConfig[] GetEquipmentByType(EquipmentType type)
        {
            return type switch
            {
                EquipmentType.WeaponMelee or EquipmentType.WeaponRanged => weapons,
                EquipmentType.ArmorHeavy or EquipmentType.ArmorLight => armor,
                EquipmentType.RhythmAccessory or EquipmentType.ExperienceMultiplier or EquipmentType.StatusProtection => accessories,
                EquipmentType.CraftingTools or EquipmentType.PrecisionInstruments => tools,
                EquipmentType.SpeedBoots or EquipmentType.TurboBooster => vehicles,
                EquipmentType.InstrumentWind or EquipmentType.InstrumentString or EquipmentType.InstrumentPercussion => instruments,
                EquipmentType.TacticalVisor or EquipmentType.LogicProcessor => gadgets,
                EquipmentType.EnergyCore or EquipmentType.HealthBooster or EquipmentType.EfficiencyBooster => boosts,
                _ => new EquipmentConfig[0]
            };
        }

        /// <summary>
        /// Get equipment by rarity
        /// </summary>
        public EquipmentConfig[] GetEquipmentByRarity(EquipmentRarity rarity)
        {
            var filtered = new List<EquipmentConfig>();
            foreach (var equipment in AllEquipment)
            {
                if (equipment.Rarity == rarity)
                {
                    filtered.Add(equipment);
                }
            }
            return filtered.ToArray();
        }

        /// <summary>
        /// Get equipment that provides bonuses for specific activity
        /// </summary>
        public EquipmentConfig[] GetEquipmentForActivity(ActivityType activityType)
        {
            var filtered = new List<EquipmentConfig>();
            foreach (var equipment in AllEquipment)
            {
                if (equipment.ActivityBonuses.Contains(activityType))
                {
                    filtered.Add(equipment);
                }
            }
            return filtered.ToArray();
        }

        /// <summary>
        /// Find equipment by ID
        /// </summary>
        public EquipmentConfig FindEquipmentById(string itemId)
        {
            foreach (var equipment in AllEquipment)
            {
                if (equipment.ItemId == itemId)
                {
                    return equipment;
                }
            }
            return null;
        }

        #region Database Validation and Statistics

        private void OnValidate()
        {
            UpdateStatistics();
            ValidateDatabase();
        }

        private void UpdateStatistics()
        {
            var allEquipment = AllEquipment;
            totalEquipmentCount = allEquipment.Length;

            commonCount = 0;
            uncommonCount = 0;
            rareCount = 0;
            epicCount = 0;
            legendaryCount = 0;

            foreach (var equipment in allEquipment)
            {
                switch (equipment.Rarity)
                {
                    case EquipmentRarity.Common: commonCount++; break;
                    case EquipmentRarity.Uncommon: uncommonCount++; break;
                    case EquipmentRarity.Rare: rareCount++; break;
                    case EquipmentRarity.Epic: epicCount++; break;
                    case EquipmentRarity.Legendary: legendaryCount++; break;
                }
            }
        }

        private void ValidateDatabase()
        {
            var issues = new List<string>();
            var usedIds = new HashSet<string>();

            foreach (var equipment in AllEquipment)
            {
                // Check for duplicate IDs
                if (!string.IsNullOrEmpty(equipment.ItemId))
                {
                    if (usedIds.Contains(equipment.ItemId))
                    {
                        issues.Add($"Duplicate ID: {equipment.ItemId}");
                    }
                    else
                    {
                        usedIds.Add(equipment.ItemId);
                    }
                }
                else
                {
                    issues.Add($"Empty ItemId in equipment: {equipment.Name}");
                }

                // Check for empty names
                if (string.IsNullOrEmpty(equipment.Name))
                {
                    issues.Add($"Empty name for equipment with ID: {equipment.ItemId}");
                }

                // Validate stat bonuses
                foreach (var statBonus in equipment.StatBonuses)
                {
                    if (statBonus.Value < 0)
                    {
                        issues.Add($"Negative stat bonus in {equipment.Name}: {statBonus.Key} = {statBonus.Value}");
                    }
                }
            }

            if (issues.Count > 0)
            {
                Debug.LogWarning($"Equipment Database Validation Issues:\n{string.Join("\n", issues)}");
            }
        }

        #endregion

        #region Default Equipment Creation

        /// <summary>
        /// Create default equipment for testing and initial game state
        /// </summary>
        [ContextMenu("Generate Default Equipment")]
        public void GenerateDefaultEquipment()
        {
            // Racing Equipment
            weapons = new EquipmentConfig[]
            {
                CreateDefaultEquipment("SpeedBoots", "Speed Boots", "Lightweight boots that enhance running speed",
                    EquipmentType.SpeedBoots, EquipmentRarity.Common, StatType.Speed, 15f, ActivityType.Racing),

                CreateDefaultEquipment("TurboEngineKit", "Turbo Engine Kit", "High-performance engine modifications",
                    EquipmentType.TurboBooster, EquipmentRarity.Rare, StatType.Speed, 30f, ActivityType.Racing),

                CreateDefaultEquipment("AerodynamicSuit", "Aerodynamic Suit", "Reduces air resistance during high-speed movement",
                    EquipmentType.ArmorLight, EquipmentRarity.Uncommon, StatType.Agility, 20f, ActivityType.Racing)
            };

            // Combat Equipment
            armor = new EquipmentConfig[]
            {
                CreateDefaultEquipment("CombatArmor", "Combat Armor", "Provides protection in battle",
                    EquipmentType.ArmorHeavy, EquipmentRarity.Common, StatType.Vitality, 20f, ActivityType.Combat),

                CreateDefaultEquipment("BattleAxe", "Battle Axe", "Heavy weapon for devastating attacks",
                    EquipmentType.WeaponMelee, EquipmentRarity.Uncommon, StatType.Strength, 25f, ActivityType.Combat),

                CreateDefaultEquipment("WarriorShield", "Warrior Shield", "Legendary shield of ancient warriors",
                    EquipmentType.Shield, EquipmentRarity.Epic, StatType.Vitality, 40f, ActivityType.Combat)
            };

            // Intelligence Equipment
            accessories = new EquipmentConfig[]
            {
                CreateDefaultEquipment("ThinkingCap", "Thinking Cap", "Enhances cognitive abilities",
                    EquipmentType.ThinkingCap, EquipmentRarity.Common, StatType.Intelligence, 18f, ActivityType.Puzzle),

                CreateDefaultEquipment("WisdomTome", "Wisdom Tome", "Ancient book containing vast knowledge",
                    EquipmentType.StrategicAnalyzer, EquipmentRarity.Rare, StatType.Intelligence, 35f, ActivityType.Strategy),

                CreateDefaultEquipment("ScholarRobes", "Scholar Robes", "Robes worn by great thinkers",
                    EquipmentType.ArmorLight, EquipmentRarity.Uncommon, StatType.Intelligence, 22f, ActivityType.Puzzle)
            };

            // Music Equipment
            instruments = new EquipmentConfig[]
            {
                CreateDefaultEquipment("RhythmGloves", "Rhythm Gloves", "Gloves that enhance musical timing",
                    EquipmentType.RhythmAccessory, EquipmentRarity.Common, StatType.Agility, 16f, ActivityType.Music),

                CreateDefaultEquipment("MagicalLyre", "Magical Lyre", "Enchanted instrument with perfect pitch",
                    EquipmentType.InstrumentString, EquipmentRarity.Legendary, StatType.Social, 50f, ActivityType.Music),

                CreateDefaultEquipment("ConductorBaton", "Conductor Baton", "Allows precise musical control",
                    EquipmentType.InstrumentPercussion, EquipmentRarity.Rare, StatType.Intelligence, 28f, ActivityType.Music)
            };

            Debug.Log("Default equipment generated successfully!");
        }

        private EquipmentConfig CreateDefaultEquipment(string id, string name, string description,
            EquipmentType type, EquipmentRarity rarity, StatType statType, float statValue, ActivityType activityType)
        {
            var config = new EquipmentConfig
            {
                ItemId = id,
                Name = name,
                Description = description,
                Type = type,
                Rarity = rarity,
                StatBonuses = new Dictionary<StatType, float> { { statType, statValue } },
                ActivityBonuses = new List<ActivityType> { activityType }
            };

            return config;
        }

        #endregion
    }

    /// <summary>
    /// ReadOnly attribute for inspector display
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer for ReadOnly attribute
    /// </summary>
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}